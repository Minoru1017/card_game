using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// login 場景物件索引：僅走訪該場景階層一次，取代 FindObjectsByType 全專案掃描。
/// </summary>
internal static class LoginSceneGraph
{
    private static Scene cachedScene;
    private static readonly Dictionary<string, List<GameObject>> byName = new Dictionary<string, List<GameObject>>(16);
    private static readonly List<GameObject> allObjects = new List<GameObject>(128);
    private static readonly List<RectTransform> graphicRects = new List<RectTransform>(128);

    public static void Invalidate()
    {
        cachedScene = default;
        byName.Clear();
        allObjects.Clear();
        graphicRects.Clear();
    }

    public static void Ensure(Scene scene)
    {
        if (!scene.IsValid()) return;
        if (cachedScene == scene && allObjects.Count > 0) return;

        Invalidate();
        cachedScene = scene;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            IndexRecursive(roots[i].transform);
    }

    public static GameObject FindNamed(Scene scene, string objectName)
    {
        Ensure(scene);
        if (string.IsNullOrEmpty(objectName)) return null;
        if (!byName.TryGetValue(objectName, out List<GameObject> list) || list.Count == 0) return null;
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go != null && go.scene == scene) return go;
        }
        return null;
    }

    public static GameObject FindClosestByAnchoredX(Scene scene, string objectName, float preferredX)
    {
        Ensure(scene);
        if (string.IsNullOrEmpty(objectName)) return null;
        if (!byName.TryGetValue(objectName, out List<GameObject> list) || list.Count == 0)
            return null;

        GameObject bestButton = null;
        float bestButtonDx = float.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go == null || go.scene != scene) continue;
            if (go.GetComponent<Button>() == null) continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;
            float dx = Mathf.Abs(rt.anchoredPosition.x - preferredX);
            if (bestButton == null || dx < bestButtonDx)
            {
                bestButton = go;
                bestButtonDx = dx;
            }
        }
        if (bestButton != null) return bestButton;

        GameObject best = null;
        float bestDx = float.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go == null || go.scene != scene) continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;
            float dx = Mathf.Abs(rt.anchoredPosition.x - preferredX);
            if (best == null || dx < bestDx)
            {
                best = go;
                bestDx = dx;
            }
        }
        return best;
    }

    public static void DisableDuplicatesExcept(Scene scene, string objectName, GameObject keep)
    {
        Ensure(scene);
        if (string.IsNullOrEmpty(objectName) || keep == null) return;
        if (!byName.TryGetValue(objectName, out List<GameObject> list)) return;
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go == null || go == keep) continue;
            if (go.scene != scene) continue;
            if (go.transform.IsChildOf(keep.transform) || keep.transform.IsChildOf(go.transform)) continue;
            if (!go.activeSelf) continue;
            go.SetActive(false);
        }
    }

    public static void DestroyRuntimeSlotClones(Scene scene)
    {
        Ensure(scene);
        for (int i = allObjects.Count - 1; i >= 0; i--)
        {
            GameObject go = allObjects[i];
            if (go == null) continue;
            if (go.scene != scene) continue;
            string n = go.name;
            if (n.StartsWith("LoginSlotRuntime_Player_", StringComparison.Ordinal) ||
                n.StartsWith("LoginSlotRuntime_Create_", StringComparison.Ordinal))
                UnityEngine.Object.Destroy(go);
        }
        Invalidate();
    }

    public static Canvas FindSceneCanvas(Scene scene)
    {
        Ensure(scene);
        for (int i = 0; i < allObjects.Count; i++)
        {
            GameObject go = allObjects[i];
            if (go == null || go.scene != scene) continue;
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null) continue;
            if (canvas.GetComponentInParent<GlobalNavRuntime>() != null) continue;
            return canvas;
        }
        return null;
    }

    public static void CollectEntranceRectTransforms(Scene scene, List<RectTransform> output)
    {
        if (output == null) return;
        output.Clear();
        Ensure(scene);
        for (int i = 0; i < graphicRects.Count; i++)
        {
            RectTransform rt = graphicRects[i];
            if (rt == null || !rt.gameObject.scene.IsValid()) continue;
            if (rt.gameObject.scene != scene) continue;
            if (!rt.gameObject.activeInHierarchy) continue;
            output.Add(rt);
        }
    }

    private static void IndexRecursive(Transform t)
    {
        if (t == null) return;

        GameObject go = t.gameObject;
        string name = go.name;
        if (!byName.TryGetValue(name, out List<GameObject> list))
        {
            list = new List<GameObject>(2);
            byName[name] = list;
        }
        list.Add(go);
        allObjects.Add(go);

        RectTransform rt = t as RectTransform;
        if (rt != null)
        {
            if (rt.GetComponentInParent<GlobalNavRuntime>() == null &&
                rt.GetComponent<Canvas>() == null &&
                !rt.gameObject.name.StartsWith("SelectionRing_", StringComparison.Ordinal) &&
                (rt.GetComponent<Graphic>() != null || rt.GetComponentInChildren<Graphic>(true) != null))
            {
                graphicRects.Add(rt);
            }
        }

        for (int i = 0; i < t.childCount; i++)
            IndexRecursive(t.GetChild(i));
    }
}

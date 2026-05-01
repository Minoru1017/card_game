using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared helpers for scene-local object search (avoids global GameObject.Find collisions).
/// </summary>
public static class SceneSearchUtil
{
    public static GameObject FindSceneObject(Scene scene, string objectName)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(objectName)) return null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeepChildByName(roots[i] != null ? roots[i].transform : null, objectName);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    public static Transform FindDeepChildByName(Transform root, string exactName)
    {
        if (root == null) return null;
        if (root.name == exactName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChildByName(root.GetChild(i), exactName);
            if (found != null) return found;
        }
        return null;
    }
}

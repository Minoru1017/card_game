#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bakes Buildbeck UI prefabs into <c>Assets/Resources/Buildbeck/UI</c> for runtime instantiation.
/// Unity menu: <c>Tools/Buildbeck/Bake UI Prefabs</c>
/// </summary>
public static class BuildbeckUiPrefabBaker
{
    private const string UiFolder = "Assets/Resources/Buildbeck/UI";

    [MenuItem("Tools/Buildbeck/Bake UI Prefabs", priority = 10)]
    public static void BakeFromMenu() => BakeAll();

    /// <summary>Batch-mode entry: <c>-executeMethod BuildbeckUiPrefabBaker.BakeAll</c></summary>
    public static void BakeAll()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Buildbeck");
        EnsureFolder(UiFolder);

        GameObject temp = new GameObject("BuildbeckBakeTemp");
        try
        {
            // Parent so RectTransforms are valid for prefab save.
            temp.AddComponent<RectTransform>();

            GameObject scrollVp = BuildbeckUiHierarchyBuilder.CreateScrollableGridPanel(
                temp.transform,
                "BakedGrid",
                Vector2.zero,
                Vector2.one,
                new Color(1f, 1f, 1f, 0.03f),
                4);
            SavePrefab(scrollVp, UiFolder + "/BuildbeckScrollGrid.prefab");

            GameObject back = BuildbeckUiHierarchyBuilder.CreateTextButton(
                temp.transform,
                "BackButton",
                "返回",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f),
                new Vector2(160f, 70f));
            SavePrefab(back, UiFolder + "/BuildbeckBackButton.prefab");

            GameObject guideRoot = BuildbeckUiHierarchyBuilder.CreateDeckSlotGuideDotsRoot(temp.transform);
            SavePrefab(guideRoot, UiFolder + "/DeckSlotGuideDotsRoot.prefab");

            GameObject dot = BuildbeckUiHierarchyBuilder.CreateDeckSlotGuideDot(temp.transform, 1);
            SavePrefab(dot, UiFolder + "/DeckSlotGuideDot.prefab");

            Button nav = BuildbeckUiHierarchyBuilder.CreateDeckSlotGuideNavButton(
                temp.GetComponent<RectTransform>(),
                "DeckSlotNavButton",
                "˄",
                BuildbeckUiFonts.ResolveBuildbeckButtonFont());
            SavePrefab(nav.gameObject, UiFolder + "/DeckSlotGuideNavButton.prefab");
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Buildbeck UI prefabs baked to " + UiFolder);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SavePrefab(GameObject instance, string assetPath)
    {
        PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
    }
}
#endif

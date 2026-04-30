using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GlobalNavPrefabBuilder
{
    private const string PrefabFolder = "Assets/Resources/prefabs";
    private const string PrefabPath = PrefabFolder + "/GlobalNavRoot.prefab";

    [MenuItem("Tools/Global Nav/Rebuild Prefab")]
    public static void RebuildPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.LogWarning("GlobalNavPrefabBuilder: prefab already exists, skip rebuild to protect artist changes. Use 'Tools/Global Nav/Force Rebuild Prefab' if you really need regenerate.");
            return;
        }
        BuildPrefabInternal();
    }

    [MenuItem("Tools/Global Nav/Force Rebuild Prefab")]
    public static void ForceRebuildPrefab()
    {
        BuildPrefabInternal();
    }

    private static void BuildPrefabInternal()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "GlobalNav");

        GameObject root = new GameObject("GlobalNavRoot");
        GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject trigger = CreateButton(canvasObj.transform, "TriggerButton", "≡", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(128f, 128f), new Color(0.53f, 0.36f, 0.78f, 0.95f));

        GameObject panel = new GameObject("TabPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1f, 1f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.pivot = new Vector2(1f, 1f);
        prt.anchoredPosition = new Vector2(-28f, -176f);
        prt.sizeDelta = new Vector2(280f, 220f);
        Image pbg = panel.GetComponent<Image>();
        pbg.color = new Color(0.94f, 0.9f, 0.82f, 0.98f);
        pbg.type = Image.Type.Sliced;

        GameObject homeBtnObj = CreateButton(panel.transform, "HomeButton", "回首頁", new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 64f), new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f));
        GameObject playerInfoBtnObj = CreateButton(panel.transform, "PlayerInfoButton", "玩家資訊", new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 64f), new Color(0.35f, 0.56f, 0.34f, 0.98f));
        GameObject closeBtnObj = CreateButton(panel.transform, "CloseButton", "關閉", new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 64f), new Color(0.53f, 0.36f, 0.78f, 0.95f));

        GlobalNavView view = root.AddComponent<GlobalNavView>();
        view.rootCanvas = canvas;
        view.triggerButtonObject = trigger;
        view.tabPanelObject = panel;
        view.triggerButton = trigger.GetComponent<Button>();
        view.homeButton = homeBtnObj.GetComponent<Button>();
        view.playerInfoButton = playerInfoBtnObj.GetComponent<Button>();
        view.closeButton = closeBtnObj.GetComponent<Button>();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GlobalNav prefab rebuilt: " + PrefabPath);
    }

    private static GameObject CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = go.GetComponent<Image>();
        bg.color = bgColor;
        bg.type = Image.Type.Sliced;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return go;
    }
}

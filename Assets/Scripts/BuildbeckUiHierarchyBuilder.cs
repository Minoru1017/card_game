using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-safe UI hierarchy construction for Buildbeck. Used by scaffold fallbacks and Editor prefab baking.
/// </summary>
public static class BuildbeckUiHierarchyBuilder
{
    public static GameObject CreateScrollableGridPanel(
        Transform parent,
        string contentObjectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color tint,
        int columnCount)
    {
        GameObject viewport = new GameObject(contentObjectName + " Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(parent, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = anchorMin;
        vpRt.anchorMax = anchorMax;
        vpRt.offsetMin = new Vector2(16f, 16f);
        vpRt.offsetMax = new Vector2(-16f, -16f);

        Image vpImage = viewport.GetComponent<Image>();
        vpImage.color = tint;

        GameObject content = new GameObject(contentObjectName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160f, 220f);
        grid.spacing = new Vector2(16f, 16f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columnCount);
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewport.GetComponent<ScrollRect>();
        scrollRect.content = contentRt;
        scrollRect.viewport = vpRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return viewport;
    }

    public static GameObject CreateTextButton(
        Transform parent,
        string objectName,
        string labelText,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject btnObj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Image bg = btnObj.GetComponent<Image>();
        bg.color = new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        TMP_FontAsset uiFont = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (uiFont != null) label.font = uiFont;

        return btnObj;
    }

    public static void ApplySaveDeckButtonLayout(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-280f, -94f);
        rt.sizeDelta = new Vector2(180f, 70f);
    }

    public static void ConfigureSaveDeckButtonVisual(GameObject saveBtnObj)
    {
        if (saveBtnObj == null) return;
        RectTransform btnRt = saveBtnObj.GetComponent<RectTransform>();
        if (btnRt == null) return;

        TextMeshProUGUI label = saveBtnObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(18f, 0f);
            labelRt.offsetMax = new Vector2(-58f, 0f);
            label.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            label.text = "儲存";

            TMP_FontAsset uiFont = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
            if (uiFont != null) label.font = uiFont;
        }

        Transform iconSlotTf = saveBtnObj.transform.Find("IconSlot");
        if (iconSlotTf == null)
        {
            GameObject iconSlot = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
            iconSlot.transform.SetParent(saveBtnObj.transform, false);
            RectTransform iconRt = iconSlot.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(1f, 0.5f);
            iconRt.anchorMax = new Vector2(1f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(-26f, 0f);
            iconRt.sizeDelta = new Vector2(24f, 24f);

            Image iconImg = iconSlot.GetComponent<Image>();
            iconImg.color = new Color(1f, 1f, 1f, 0.18f);
            iconImg.raycastTarget = false;
        }
    }

    public static GameObject CreateDeckSlotGuideDotsRoot(Transform canvasTransform)
    {
        GameObject rootObj = new GameObject("DeckSlotGuideDots", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        rootObj.transform.SetParent(canvasTransform, false);
        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0.5f);
        rootRt.anchorMax = new Vector2(1f, 0.5f);
        rootRt.pivot = new Vector2(1f, 0.5f);
        rootRt.anchoredPosition = new Vector2(-72f, 0f);

        VerticalLayoutGroup vlg = rootObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 18f;

        ContentSizeFitter fitter = rootObj.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rootObj;
    }

    public static GameObject CreateDeckSlotGuideDot(Transform parent, int index1Based)
    {
        GameObject dotObj = new GameObject($"Dot_{index1Based}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        dotObj.transform.SetParent(parent, false);
        RectTransform dotRt = dotObj.GetComponent<RectTransform>();
        dotRt.sizeDelta = new Vector2(18f, 18f);
        Image dot = dotObj.GetComponent<Image>();
        dot.color = new Color(1f, 1f, 1f, 0.38f);

        LayoutElement le = dotObj.GetComponent<LayoutElement>();
        le.preferredWidth = 18f;
        le.preferredHeight = 18f;
        return dotObj;
    }

    public static Button CreateDeckSlotGuideNavButton(RectTransform parent, string name, string symbol, TMP_FontAsset tmpFont)
    {
        if (parent == null) return null;
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(26f, 26f);

        Image bg = btnObj.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.85f);
        Button btn = btnObj.GetComponent<Button>();

        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        le.preferredWidth = 26f;
        le.preferredHeight = 26f;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        if (tmpFont != null) label.font = tmpFont;
        label.text = symbol;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        label.raycastTarget = false;
        return btn;
    }
}

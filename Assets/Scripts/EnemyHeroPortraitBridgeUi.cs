using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敵方英雄立繪橋接（第十章）：全螢幕 overlay，v1 以 placeholder 色塊代替立繪。
/// </summary>
public static class EnemyHeroPortraitBridgeUi
{
    private const int OverlaySortOrder = 6000;

    private static readonly Color BackdropColor = new Color(0.04f, 0.05f, 0.07f, 0.96f);
    private static readonly Color PanelColor = new Color(0.11f, 0.13f, 0.18f, 1f);
    private static readonly Color PanelBorder = new Color(0.97f, 0.85f, 0.47f, 0.88f);
    private static readonly Color PortraitFill = new Color(0.88f, 0.52f, 0.38f, 1f);
    private static readonly Color PortraitFrame = new Color(0.38f, 0.28f, 0.24f, 1f);
    private static readonly Color NameColor = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Color BodyColor = new Color(0.92f, 0.95f, 0.99f, 1f);
    private static readonly Color HintColor = new Color(0.72f, 0.76f, 0.82f, 1f);
    private static readonly Color ButtonColor = new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);

    public static void ShowPortraitA(
        Canvas canvas,
        EnemyHeroProfile hero,
        bool isRematch,
        TMP_FontAsset font,
        Action onContinue)
    {
        if (hero == null)
        {
            onContinue?.Invoke();
            return;
        }

        ShowOverlay(
            hero,
            "對手介紹",
            hero.ResolvePortraitALine(isRematch),
            "繼續",
            font,
            onContinue);
    }

    public static void ShowPortraitB(
        Canvas canvas,
        EnemyHeroProfile hero,
        bool isRematch,
        BirdDuelResult duelResult,
        TMP_FontAsset font,
        Action onContinue)
    {
        if (hero == null)
        {
            onContinue?.Invoke();
            return;
        }

        ShowOverlay(
            hero,
            hero.DisplayName,
            hero.ResolvePortraitBLine(isRematch, duelResult),
            "進入對戰",
            font,
            onContinue);
    }

    private static void ShowOverlay(
        EnemyHeroProfile hero,
        string header,
        string body,
        string buttonLabel,
        TMP_FontAsset font,
        Action onContinue)
    {
        GameObject root = new GameObject(
            "EnemyHeroPortraitBridge",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas overlayCanvas = root.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = OverlaySortOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image backdrop = root.AddComponent<Image>();
        backdrop.color = BackdropColor;
        backdrop.raycastTarget = true;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        BirdDuelMobileOverlayLayout.ApplyMobilePanel(panelRt);
        panel.GetComponent<Image>().color = PanelColor;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = PanelBorder;
        outline.effectDistance = new Vector2(2f, -2f);

        CreateAnchoredText(
            panel.transform,
            "Header",
            header,
            40f,
            FontStyles.Bold,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -20f),
            new Vector2(-48f, 52f),
            NameColor,
            font,
            TextAlignmentOptions.Center);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.offsetMin = new Vector2(36f, BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + BirdDuelMobileOverlayLayout.ButtonHeightPrimary + 16f);
        contentRt.offsetMax = new Vector2(-36f, -88f);

        GameObject portraitFrame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
        portraitFrame.transform.SetParent(content.transform, false);
        RectTransform frameRt = portraitFrame.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0f, 1f);
        frameRt.anchorMax = new Vector2(0f, 1f);
        frameRt.pivot = new Vector2(0f, 1f);
        frameRt.anchoredPosition = Vector2.zero;
        frameRt.sizeDelta = new Vector2(176f, 176f);
        portraitFrame.GetComponent<Image>().color = PortraitFrame;

        GameObject portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portrait.transform.SetParent(portraitFrame.transform, false);
        RectTransform portraitRt = portrait.GetComponent<RectTransform>();
        portraitRt.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRt.pivot = new Vector2(0.5f, 0.5f);
        portraitRt.anchoredPosition = Vector2.zero;
        portraitRt.sizeDelta = new Vector2(156f, 156f);
        portrait.GetComponent<Image>().color = PortraitFill;

        string initial = string.IsNullOrEmpty(hero.DisplayName) ? "?" : hero.DisplayName.Substring(0, 1);
        CreateAnchoredText(
            portraitFrame.transform,
            "PortraitInitial",
            initial,
            68f,
            FontStyles.Bold,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            Color.white,
            font,
            TextAlignmentOptions.Center);

        GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform));
        textColumn.transform.SetParent(content.transform, false);
        RectTransform textRt = textColumn.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(204f, 0f);
        textRt.offsetMax = Vector2.zero;

        CreateAnchoredText(
            textColumn.transform,
            "Name",
            hero.DisplayName,
            34f,
            FontStyles.Bold,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, 44f),
            NameColor,
            font,
            TextAlignmentOptions.Left);

        CreateAnchoredText(
            textColumn.transform,
            "Specialty",
            "擅長：" + hero.SpecialtyTagZh,
            24f,
            FontStyles.Normal,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, -52f),
            new Vector2(0f, 32f),
            HintColor,
            font,
            TextAlignmentOptions.Left);

        TextMeshProUGUI bodyTmp = CreateAnchoredText(
            textColumn.transform,
            "Body",
            body,
            28f,
            FontStyles.Normal,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, -96f),
            new Vector2(0f, -8f),
            BodyColor,
            font,
            TextAlignmentOptions.TopLeft);
        bodyTmp.enableWordWrapping = true;
        bodyTmp.lineSpacing = 6f;

        Button continueBtn = CreateContinueButton(panel.transform, buttonLabel, font);
        continueBtn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(root);
            onContinue?.Invoke();
        });
    }

    private static Button CreateContinueButton(Transform panel, string label, TMP_FontAsset font)
    {
        GameObject buttonObj = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(panel, false);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        BirdDuelMobileOverlayLayout.PlaceStackedButton(rt, 0);
        buttonObj.GetComponent<Image>().color = ButtonColor;

        Button btn = buttonObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.52f, 0.34f, 0.30f, 1f);
        colors.pressedColor = new Color(0.36f, 0.22f, 0.20f, 1f);
        btn.colors = colors;

        TextMeshProUGUI txt = CreateAnchoredText(
            buttonObj.transform,
            "Label",
            label,
            BirdDuelMobileOverlayLayout.ButtonFontPrimary,
            FontStyles.Bold,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            Color.white,
            font,
            TextAlignmentOptions.Center);
        txt.raycastTarget = false;
        return btn;
    }

    private static TextMeshProUGUI CreateAnchoredText(
        Transform parent,
        string objName,
        string text,
        float fontSize,
        FontStyles style,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        Color color,
        TMP_FontAsset font,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        else UiFontResolver.ApplyTo(tmp, text);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }
}

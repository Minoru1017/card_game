using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>鬥鳥戰前彈窗共用 UI 建構（手機優先、驚奇感色票）。</summary>
public static class BirdDuelOverlayUiBuild
{
    public static GameObject CreateDimOverlay(Transform parent, int sortOrder, string name)
    {
        GameObject overlay = new GameObject(
            name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = BirdDuelUiColors.Dim;

        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = sortOrder;
        overlay.AddComponent<GraphicRaycaster>();
        return overlay;
    }

    public static GameObject CreateMobilePanel(Transform parent, string name = "Panel")
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);
        BirdDuelMobileOverlayLayout.ApplyMobilePanel(panel.GetComponent<RectTransform>());
        Image panelImg = panel.GetComponent<Image>();
        panelImg.color = BirdDuelUiColors.Panel;
        panelImg.type = Image.Type.Simple;
        panelImg.raycastTarget = true;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = BirdDuelUiColors.PanelEdge;
        outline.effectDistance = new Vector2(0f, -3f);
        return panel;
    }

    public static RectTransform CreateHeaderBand(Transform panel, string badgeText, TMP_FontAsset font)
    {
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        BirdDuelMobileOverlayLayout.StretchTopBand(headerRt, BirdDuelMobileOverlayLayout.HeaderHeight);
        header.GetComponent<Image>().color = BirdDuelUiColors.HeaderBand;

        GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeObj.transform.SetParent(header.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0f);
        badgeRt.anchorMax = new Vector2(0f, 1f);
        badgeRt.pivot = new Vector2(0f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(BirdDuelMobileOverlayLayout.ContentPadH, 0f);
        badgeRt.sizeDelta = new Vector2(200f, 0f);
        TextMeshProUGUI badgeTmp = badgeObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(badgeTmp, font);
        badgeTmp.text = badgeText;
        badgeTmp.fontSize = BirdDuelMobileOverlayLayout.BadgeFontSize;
        badgeTmp.fontStyle = FontStyles.Bold;
        badgeTmp.alignment = TextAlignmentOptions.MidlineLeft;
        badgeTmp.color = BirdDuelUiColors.WonderBadge;
        badgeTmp.raycastTarget = false;

        return headerRt;
    }

    public static TextMeshProUGUI CreateTitle(
        Transform panel,
        string text,
        TMP_FontAsset font,
        float topOffset)
    {
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panel, false);
        RectTransform rt = titleObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -topOffset);
        rt.sizeDelta = new Vector2(-BirdDuelMobileOverlayLayout.ContentPadH * 2f, 52f);
        TextMeshProUGUI tmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(tmp, font);
        tmp.text = text;
        tmp.fontSize = BirdDuelMobileOverlayLayout.TitleFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = BirdDuelUiColors.Ink;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static TextMeshProUGUI CreateInfoCard(
        Transform panel,
        string bodyText,
        TMP_FontAsset font,
        float top,
        float bottom)
    {
        GameObject infoCard = new GameObject("InfoCard", typeof(RectTransform), typeof(Image));
        infoCard.transform.SetParent(panel, false);
        RectTransform infoRt = infoCard.GetComponent<RectTransform>();
        BirdDuelMobileOverlayLayout.StretchHorizontal(infoRt, top, bottom);

        Image infoBg = infoCard.GetComponent<Image>();
        infoBg.color = BirdDuelUiColors.InfoCard;
        infoBg.type = Image.Type.Simple;
        infoBg.raycastTarget = false;

        GameObject bodyObj = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(infoCard.transform, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(20f, 16f);
        bodyRt.offsetMax = new Vector2(-20f, -16f);
        TextMeshProUGUI bodyTmp = bodyObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(bodyTmp, font);
        bodyTmp.text = bodyText;
        bodyTmp.fontSize = BirdDuelMobileOverlayLayout.BodyFontSize;
        bodyTmp.fontStyle = FontStyles.Normal;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.lineSpacing = 6f;
        bodyTmp.color = BirdDuelUiColors.InkSoft;
        bodyTmp.raycastTarget = false;
        return bodyTmp;
    }

    public static Button CreateButton(
        Transform parent,
        string objName,
        string label,
        Color normal,
        Color highlighted,
        Color pressed,
        Color textColor,
        float fontSize,
        TMP_FontAsset font = null)
    {
        GameObject buttonObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        Image img = buttonObj.GetComponent<Image>();
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.raycastTarget = true;

        Button btn = buttonObj.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.colorMultiplier = 1f;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = highlighted;
        colors.disabledColor = BirdDuelUiColors.BtnDisabledBg;
        btn.colors = colors;

        GameObject tmpObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        tmpObj.transform.SetParent(buttonObj.transform, false);
        RectTransform txtRt = tmpObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(12f, 4f);
        txtRt.offsetMax = new Vector2(-12f, -4f);
        TextMeshProUGUI txt = tmpObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(txt, font);
        txt.text = label;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = textColor;
        txt.raycastTarget = false;
        return btn;
    }

    public static Button CreatePrimaryButton(Transform parent, string name, string label, TMP_FontAsset font, bool large = true)
    {
        return CreateButton(
            parent,
            name,
            label,
            BirdDuelUiColors.BtnPrimary,
            BirdDuelUiColors.BtnPrimaryH,
            BirdDuelUiColors.BtnPrimaryP,
            BirdDuelUiColors.BtnPrimaryText,
            large ? BirdDuelMobileOverlayLayout.ButtonFontPrimary : BirdDuelMobileOverlayLayout.ButtonFontSecondary,
            font);
    }

    public static Button CreateSecondaryButton(Transform parent, string name, string label, TMP_FontAsset font)
    {
        return CreateButton(
            parent,
            name,
            label,
            BirdDuelUiColors.BtnSecondary,
            BirdDuelUiColors.BtnSecondaryH,
            BirdDuelUiColors.BtnSecondaryP,
            BirdDuelUiColors.BtnSecondaryText,
            BirdDuelMobileOverlayLayout.ButtonFontSecondary,
            font);
    }

    public static Button CreateGhostBackButton(Transform header, TMP_FontAsset font)
    {
        Button backBtn = CreateButton(
            header,
            "BackBtn",
            "返回",
            BirdDuelUiColors.BtnGhost,
            BirdDuelUiColors.BtnGhostH,
            BirdDuelUiColors.BtnGhostP,
            BirdDuelUiColors.OnDarkText,
            26f);
        RectTransform backRt = backBtn.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(1f, 0.5f);
        backRt.anchorMax = new Vector2(1f, 0.5f);
        backRt.pivot = new Vector2(1f, 0.5f);
        backRt.anchoredPosition = new Vector2(-BirdDuelMobileOverlayLayout.ContentPadH, 0f);
        backRt.sizeDelta = new Vector2(120f, BirdDuelMobileOverlayLayout.ButtonHeightGhost);
        if (font != null)
        {
            TextMeshProUGUI label = backBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.font = font;
        }

        SettingsUiFonts.ApplyTo(backBtn.GetComponentInChildren<TextMeshProUGUI>());
        return backBtn;
    }

    public static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (tmp == null) return;
        if (font != null) tmp.font = font;
        SettingsUiFonts.ApplyTo(tmp);
    }

    public static float ComputeInfoCardTop()
    {
        return BirdDuelMobileOverlayLayout.HeaderHeight
            + 56f
            + BirdDuelMobileOverlayLayout.SectionGap;
    }

    public static float ComputeInfoCardBottom()
    {
        return BirdDuelMobileOverlayLayout.ButtonAreaPadBottom
            + BirdDuelMobileOverlayLayout.ButtonHeightPrimary
            + BirdDuelMobileOverlayLayout.ButtonGap
            + BirdDuelMobileOverlayLayout.ButtonHeightSecondary
            + BirdDuelMobileOverlayLayout.SectionGap;
    }
}

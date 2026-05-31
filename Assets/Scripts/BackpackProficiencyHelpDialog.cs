using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>僅由背包卡牌詳情開啟：卡牌熟練度與 A/B/C 解鎖條件說明彈窗。</summary>
[DisallowMultipleComponent]
public class BackpackProficiencyHelpDialog : MonoBehaviour
{
    private const int OverlaySortOrderBoost = 320;
    private const int HelpUiBuildVersion = 5;
    private const float DialogWidthScreenRatio = 0.94f;
    private const float DialogHeightScreenRatio = 0.9f;
    private const float DialogWidthMaxPx = 1080f;
    private const float DialogHeightMaxPx = 920f;
    private const float DialogScreenMarginPx = 24f;
    private const int TitleFontSize = 42;
    private const int BodyFontSize = 30;
    private const int CloseFontSize = 30;

    private GameObject root;
    private TextMeshProUGUI bodyTmp;
    private RectTransform bodyContentRt;
    private ScrollRect bodyScrollRect;
    private int helpUiBuildVersion;
    private float dimClickIgnoreUntil;

    public bool IsOpen => root != null && root.activeSelf;

    public void Show(Canvas hostCanvas, TMP_FontAsset font)
    {
        if (hostCanvas == null) return;
        EnsureUi(hostCanvas, font);
        if (root == null) return;
        if (bodyTmp != null)
            bodyTmp.text = BuildHelpBodyRichText();
        RefreshBodyScrollLayout();
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        dimClickIgnoreUntil = Time.unscaledTime + 0.2f;
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public void OnDimClicked()
    {
        if (Time.unscaledTime < dimClickIgnoreUntil) return;
        Hide();
    }

    public static string BuildHelpBodyRichText()
    {
        int bThreshold = CardSkillProficiencyService.WinsRequiredForStageB;
        int cThreshold = CardSkillProficiencyService.WinsRequiredForStageC;
        string stageCDifficultyLabel = CardSkillProficiencyService.StageCDifficultyRequirementLabelZh;
        float stepTowardB = 1f / bThreshold;
        var sb = new StringBuilder(1200);

        AppendSectionTitle(sb, "卡牌熟練度");
        AppendBodyLine(sb, "使用該牌組建牌組後 若於對戰勝利 可累積卡牌熟練度 各階段解放條件均有不同");
        AppendBoldLabelLine(sb, "每贏1場");
        AppendLabeledBodyLine(sb, "勝利", $"+{stepTowardB:0.#}");
        AppendLabeledBodyLine(sb, "平手", $"+{stepTowardB * (2f / 3f):0.#}");
        AppendLabeledBodyLine(sb, "敗北", $"+{stepTowardB * (1f / 3f):0.#}");
        AppendBodyLine(sb, "御三家不累加");
        sb.AppendLine();

        AppendSectionTitle(sb, "階段解放條件");
        AppendThresholdLine(
            sb,
            CardSkillRevealStage.BasicB,
            "解放 B 基礎戰技",
            "條件",
            $"熟練進度累積至 {bThreshold} 不限對戰難度",
            null,
            null);
        AppendThresholdLine(
            sb,
            CardSkillRevealStage.FullC,
            "解放 C 完整戰技",
            "條件",
            $"須先達成 B 並於 {stageCDifficultyLabel} 累積 {cThreshold} 場勝利",
            "備註",
            "入門 簡單 之勝利不計入");
        AppendThresholdLine(
            sb,
            CardSkillRevealStage.FullC,
            "御三家",
            null,
            "國王 王后 民兵預設為完整戰技 C 階段 無需累積熟練度",
            null,
            null);
        sb.AppendLine();

        AppendSectionTitle(sb, "三階段說明");
        AppendStageLine(sb, CardSkillRevealStage.LockedA, "A 未解放", "卡面不顯示完整戰技 達標後進入 B");
        AppendStageLine(sb, CardSkillRevealStage.BasicB, "B 基礎", "顯示戰技摘要 達標後進入 C");
        AppendStageLine(sb, CardSkillRevealStage.FullC, "C 完整", "顯示完整戰技與互動說明");
        sb.AppendLine();

        AppendBodyLine(sb, "本說明僅於背包卡牌詳情檢視");
        return sb.ToString().TrimEnd();
    }

    private static void AppendThresholdLine(
        StringBuilder sb,
        CardSkillRevealStage stage,
        string title,
        string conditionLabel,
        string conditionBody,
        string noteLabel,
        string noteBody)
    {
        Color accent = BackpackInspectVisualStyle.StageAccent(stage);
        sb.Append(BackpackInspectVisualStyle.ColorTag(accent));
        sb.Append("<b>").Append(title).Append("</b></color>");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(conditionLabel) && !string.IsNullOrWhiteSpace(conditionBody))
            AppendLabeledBodyLine(sb, conditionLabel, conditionBody);
        else if (!string.IsNullOrWhiteSpace(conditionBody))
            AppendBodyLine(sb, conditionBody);
        if (!string.IsNullOrWhiteSpace(noteLabel) && !string.IsNullOrWhiteSpace(noteBody))
            AppendLabeledBodyLine(sb, noteLabel, noteBody);
        else if (!string.IsNullOrWhiteSpace(noteBody))
            AppendBodyLine(sb, noteBody);
        sb.AppendLine();
    }

    private static void AppendSectionTitle(StringBuilder sb, string title)
    {
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.Ink));
        sb.Append("<b>").Append(title).Append("</b></color>");
        sb.AppendLine();
    }

    private static void AppendBoldLabelLine(StringBuilder sb, string label)
    {
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.Ink));
        sb.Append("<b>").Append(label).Append("</b></color>");
        sb.AppendLine();
    }

    private static void AppendBodyLine(StringBuilder sb, string line)
    {
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.InkSoft));
        sb.Append(line).Append("</color>");
        sb.AppendLine();
    }

    private static void AppendLabeledBodyLine(StringBuilder sb, string label, string body)
    {
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.Ink));
        sb.Append("<b>").Append(label).Append("</b></color> ");
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.InkMuted));
        sb.Append(body).Append("</color>");
        sb.AppendLine();
    }

    private static void AppendStageLine(StringBuilder sb, CardSkillRevealStage stage, string title, string desc)
    {
        Color accent = BackpackInspectVisualStyle.StageAccent(stage);
        sb.Append(BackpackInspectVisualStyle.ColorTag(accent));
        sb.Append("<b>").Append(title).Append("</b></color>");
        sb.AppendLine();
        AppendBodyLine(sb, desc);
    }

    private void EnsureUi(Canvas hostCanvas, TMP_FontAsset font)
    {
        if (root != null && helpUiBuildVersion == HelpUiBuildVersion) return;
        if (root != null)
        {
            Destroy(root);
            root = null;
            bodyTmp = null;
            bodyContentRt = null;
            bodyScrollRect = null;
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;

        root = new GameObject("BackpackProficiencyHelpRoot", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(hostCanvas.transform, false);
        Stretch(root.GetComponent<RectTransform>(), 0, 0, 0, 0);

        Canvas overlay = root.GetComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = hostCanvas.sortingOrder + OverlaySortOrderBoost;
        overlay.renderMode = hostCanvas.renderMode;
        if (hostCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            overlay.worldCamera = hostCanvas.worldCamera;

        Image dim = CreateChildImage(root.transform, "Dim", BackpackInspectUiColors.Dim);
        Stretch(dim.rectTransform, 0, 0, 0, 0);
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(OnDimClicked);

        Image panel = CreateChildImage(root.transform, "Dialog", BackpackInspectUiColors.PagePaper);
        RectTransform panelRt = panel.rectTransform;
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        float panelW = Mathf.Min(
            DialogWidthMaxPx,
            Screen.width * DialogWidthScreenRatio,
            Screen.width - DialogScreenMarginPx * 2f);
        float panelH = Mathf.Min(
            DialogHeightMaxPx,
            Screen.height * DialogHeightScreenRatio,
            Screen.height - DialogScreenMarginPx * 2f);
        panelRt.sizeDelta = new Vector2(panelW, panelH);

        const float footerH = 96f;
        const float titleBandH = 64f;

        TextMeshProUGUI titleTmp = CreateTmp(panelRt, "Title", font, TitleFontSize, FontStyles.Bold,
            BackpackInspectUiColors.Ink, TextAlignmentOptions.Center);
        RectTransform titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -12f);
        titleRt.sizeDelta = new Vector2(-40f, titleBandH);
        titleTmp.text = "卡牌熟練度說明";

        GameObject scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(panelRt, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(24f, footerH + 8f);
        scrollRt.offsetMax = new Vector2(-24f, -(titleBandH + 16f));
        scrollGo.GetComponent<Image>().color = BackpackInspectUiColors.PagePaperInset;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(0f, 0f);
        vpRt.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        bodyContentRt = content.GetComponent<RectTransform>();
        bodyContentRt.anchorMin = new Vector2(0f, 1f);
        bodyContentRt.anchorMax = new Vector2(1f, 1f);
        bodyContentRt.pivot = new Vector2(0.5f, 1f);
        bodyContentRt.anchoredPosition = Vector2.zero;
        bodyContentRt.sizeDelta = new Vector2(0f, 800f);

        bodyTmp = CreateTmp(bodyContentRt, "Body", font, BodyFontSize, FontStyles.Normal,
            BackpackInspectUiColors.InkSoft, TextAlignmentOptions.TopLeft);
        RectTransform bodyRt = bodyTmp.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.anchoredPosition = Vector2.zero;
        bodyRt.sizeDelta = new Vector2(-20f, 0f);
        ContentSizeFitter bodyCsf = bodyTmp.gameObject.AddComponent<ContentSizeFitter>();
        bodyCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        LayoutElement bodyLe = bodyTmp.gameObject.AddComponent<LayoutElement>();
        bodyLe.minHeight = 120f;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.richText = true;
        bodyTmp.overflowMode = TextOverflowModes.Overflow;
        bodyTmp.lineSpacing = 8f;
        bodyTmp.paragraphSpacing = 12f;
        bodyTmp.text = BuildHelpBodyRichText();

        bodyScrollRect = scrollGo.GetComponent<ScrollRect>();
        bodyScrollRect.horizontal = false;
        bodyScrollRect.vertical = true;
        bodyScrollRect.movementType = ScrollRect.MovementType.Clamped;
        bodyScrollRect.scrollSensitivity = 48f;
        bodyScrollRect.inertia = true;
        bodyScrollRect.decelerationRate = 0.12f;
        bodyScrollRect.content = bodyContentRt;
        bodyScrollRect.viewport = vpRt;
        bodyScrollRect.verticalScrollbar = null;
        bodyScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        Image closeImg = CreateChildImage(panelRt, "CloseButton", BackpackInspectUiColors.BtnBack);
        RectTransform closeRt = closeImg.rectTransform;
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0f, 20f);
        closeRt.sizeDelta = new Vector2(280f, 56f);
        Button closeBtn = closeImg.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Hide);
        TextMeshProUGUI closeLbl = CreateTmp(closeRt, "Label", font, CloseFontSize, FontStyles.Bold,
            BackpackInspectUiColors.BtnBackText, TextAlignmentOptions.Center);
        Stretch(closeLbl.rectTransform, 0, 0, 0, 0);
        closeLbl.text = "關閉";

        helpUiBuildVersion = HelpUiBuildVersion;
        root.SetActive(false);
    }

    private static Image CreateChildImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    private static TextMeshProUGUI CreateTmp(
        Transform parent,
        string name,
        TMP_FontAsset font,
        int size,
        FontStyles style,
        Color color,
        TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void RefreshBodyScrollLayout()
    {
        if (bodyTmp == null || bodyContentRt == null) return;

        bodyTmp.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        float textH = Mathf.Max(bodyTmp.preferredHeight + 32f, 200f);
        bodyContentRt.sizeDelta = new Vector2(0f, textH);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContentRt);

        if (bodyScrollRect != null)
            bodyScrollRect.verticalNormalizedPosition = 1f;
    }

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}

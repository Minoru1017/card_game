using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>僅由背包卡牌詳情開啟：卡牌熟練度與 A/B/C 解鎖條件說明彈窗。</summary>
[DisallowMultipleComponent]
public class BackpackProficiencyHelpDialog : MonoBehaviour
{
    private const int OverlaySortOrderBoost = 320;

    private GameObject root;
    private TextMeshProUGUI bodyTmp;
    private float dimClickIgnoreUntil;

    public bool IsOpen => root != null && root.activeSelf;

    public void Show(Canvas hostCanvas, TMP_FontAsset font)
    {
        if (hostCanvas == null) return;
        EnsureUi(hostCanvas, font);
        if (root == null) return;
        if (bodyTmp != null)
            bodyTmp.text = BuildHelpBodyRichText();
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
        int bWins = CardSkillProficiencyService.WinsRequiredForStageB;
        int cWins = CardSkillProficiencyService.WinsRequiredForStageC;
        string normalLabel = CardSkillProficiencyService.NormalDifficultyLabelZh;
        var sb = new StringBuilder(1024);

        AppendSectionTitle(sb, "卡牌熟練度");
        AppendBodyLine(sb, "怪物牌依熟練度逐步解鎖戰技說明，共分三階段");
        sb.AppendLine();

        AppendStageLine(sb, CardSkillRevealStage.LockedA, "A 未解放", "卡面不顯示完整戰技");
        AppendStageLine(sb, CardSkillRevealStage.BasicB, "B 基礎", "簡短戰技摘要");
        AppendStageLine(sb, CardSkillRevealStage.FullC, "C 完整", "完整戰技與互動說明");
        sb.AppendLine();

        AppendSectionTitle(sb, "解鎖條件");
        AppendBodyLine(sb, "以目前牌組中的該怪物牌為準，每局每種卡牌最多計一次");
        AppendBodyLine(sb, $"達 B 不限難度累計進度，滿 {bWins} 點即解鎖 B（勝利每場 +1/{bWins}）");
        AppendBodyLine(sb, $"達 C {normalLabel}難度勝利累計 {cWins} 場");
        sb.AppendLine();

        AppendSectionTitle(sb, "單局結算進度");
        AppendBodyLine(sb, "勝平敗皆會累加，御三家除外");
        AppendBodyLine(sb, $"勝利 增加二分之一（1/{bWins}）");
        AppendBodyLine(sb, $"平手 增加二分之一再乘三分之二（1/{bWins}×2/3）");
        AppendBodyLine(sb, $"敗北 增加二分之一再乘三分之一（1/{bWins}×1/3）");
        sb.AppendLine();

        AppendSectionTitle(sb, "御三家");
        AppendBodyLine(sb, "國王 王后 民兵 預設為 C，不累加熟練度進度");
        sb.AppendLine();

        AppendBodyLine(sb, "進度條與本說明僅於背包卡牌詳情中檢視");
        AppendBodyLine(sb, "法術牌不適用本系統");
        return sb.ToString().TrimEnd();
    }

    private static void AppendSectionTitle(StringBuilder sb, string title)
    {
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.Ink));
        sb.Append("<b>").Append(title).Append("</b></color>");
        sb.AppendLine();
    }

    private static void AppendBodyLine(StringBuilder sb, string line)
    {
        sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectUiColors.InkSoft));
        sb.Append(line).Append("</color>");
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
        if (root != null) return;
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
        panelRt.sizeDelta = new Vector2(
            Mathf.Min(720f, Screen.width - 48f),
            Mathf.Min(640f, Screen.height - 80f));

        TextMeshProUGUI titleTmp = CreateTmp(panelRt, "Title", font, 34, FontStyles.Bold,
            BackpackInspectUiColors.Ink, TextAlignmentOptions.Center);
        RectTransform titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -16f);
        titleRt.sizeDelta = new Vector2(-32f, 44f);
        titleTmp.text = "卡牌熟練度說明";

        GameObject scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(panelRt, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(20f, 88f);
        scrollRt.offsetMax = new Vector2(-20f, -72f);
        scrollGo.GetComponent<Image>().color = BackpackInspectUiColors.PagePaperInset;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        Stretch(vpRt, 8, 8, 8, 8);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 400f);
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bodyTmp = CreateTmp(contentRt, "Body", font, 24, FontStyles.Normal,
            BackpackInspectUiColors.InkSoft, TextAlignmentOptions.TopLeft);
        Stretch(bodyTmp.rectTransform, 12, 12, 12, 12);
        bodyTmp.enableWordWrapping = true;
        bodyTmp.richText = true;
        bodyTmp.overflowMode = TextOverflowModes.Overflow;
        bodyTmp.lineSpacing = 6f;
        bodyTmp.paragraphSpacing = 8f;
        bodyTmp.text = BuildHelpBodyRichText();

        ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.content = contentRt;
        sr.viewport = vpRt;

        Image closeImg = CreateChildImage(panelRt, "CloseButton", BackpackInspectUiColors.BtnBack);
        RectTransform closeRt = closeImg.rectTransform;
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0f, 18f);
        closeRt.sizeDelta = new Vector2(200f, 48f);
        Button closeBtn = closeImg.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Hide);
        TextMeshProUGUI closeLbl = CreateTmp(closeRt, "Label", font, 26, FontStyles.Bold,
            BackpackInspectUiColors.BtnBackText, TextAlignmentOptions.Center);
        Stretch(closeLbl.rectTransform, 0, 0, 0, 0);
        closeLbl.text = "關閉";

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

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>背包卡牌詳情：程式化 UI（色票見 <see cref="BackpackInspectUiColors"/>／BACKPACK_INSPECT_UI_COLOR_SPEC.md）。</summary>
public static class BackpackInspectVisualStyle
{
    public static readonly Color BgTealDeep = BackpackInspectUiColors.ArtWellWash;
    public static readonly Color BgTealGlow = BackpackInspectUiColors.SkyDeep;
    public static readonly Color PanelGlass = BackpackInspectUiColors.PagePaper;
    public static readonly Color DividerSoft = BackpackInspectUiColors.WithAlpha(BackpackInspectUiColors.Ink, 0.12f);

    public static readonly Color FrameOuter = BackpackInspectUiColors.SkyDeep;
    public static readonly Color FrameInner = BackpackInspectUiColors.ArtFrame;
    public static readonly Color FrameHighlight = BackpackInspectUiColors.MainTitle;
    public static readonly Color FrameHighlightDim = BackpackInspectUiColors.WithAlpha(BackpackInspectUiColors.MainTitle, 0.45f);
    public static readonly Color TitleGold = BackpackInspectUiColors.MainTitle;
    public static readonly Color InkBright = BackpackInspectUiColors.Ink;
    public static readonly Color InkMuted = BackpackInspectUiColors.InkMuted;
    public static readonly Color InkDim = BackpackInspectUiColors.InkSoft;

    public static readonly Color StageA = BackpackInspectUiColors.StageA;
    public static readonly Color StageB = BackpackInspectUiColors.StageB;
    public static readonly Color StageC = BackpackInspectUiColors.StageC;
    public static readonly Color LockVeil = BackpackInspectUiColors.WithAlpha(BackpackInspectUiColors.Ink, 0.72f);

    public static readonly Color StatAtk = BackpackInspectUiColors.Ink;
    public static readonly Color StatHp = BackpackInspectUiColors.Ink;
    public static readonly Color StatOwned = BackpackInspectUiColors.Ink;
    public static readonly Color StatDeck = BackpackInspectUiColors.Ink;

    public static class Typography
    {
        public const int MainTitleSize = 68;
        public const int SubtitleSize = 40;
        public const int BodySize = 34;
        public const int HintSize = 26;

        public const float SubtitleLineSpacing = 12f;
        public const float BodyLineSpacing = 18f;
        public const float StatsLineSpacing = 6f;
        public const float BodyParagraphSpacing = 20f;

        public const float InfoPaddingH = 24f;
        public const float InfoPaddingTop = 20f;
        public const float BlockGap = 16f;

        public static readonly Color MainTitle = BackpackInspectUiColors.MainTitle;
        public static readonly Color Subtitle = BackpackInspectUiColors.MintLabel;
        public static readonly Color Body = BackpackInspectUiColors.Ink;
        public static readonly Color BodyOnSkill = BackpackInspectUiColors.InkOnSkill;
        public static readonly Color BodyMuted = BackpackInspectUiColors.InkMuted;
        public static readonly Color Hint = BackpackInspectUiColors.InkMuted;
    }

    public static Color RarityTextColor(CardRarity rarity) => BackpackInspectUiColors.Rarity(rarity);

    public static string ColorTag(Color c) =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">";

    public static string WrapSubtitleRich(string text, bool trailingNewline = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string line = ColorTag(Typography.Subtitle) + "<size=" + Typography.SubtitleSize + ">" + text + "</size></color>";
        return trailingNewline ? line + "\n" : line;
    }

    public static Color StageAccent(CardSkillRevealStage stage)
    {
        switch (stage)
        {
            case CardSkillRevealStage.BasicB: return StageB;
            case CardSkillRevealStage.FullC: return StageC;
            default: return StageA;
        }
    }

    public static string StageGlyph(CardSkillRevealStage stage)
    {
        switch (stage)
        {
            case CardSkillRevealStage.BasicB: return "B";
            case CardSkillRevealStage.FullC: return "C";
            default: return "A";
        }
    }

    public static string StageTitleZh(CardSkillRevealStage stage)
    {
        switch (stage)
        {
            case CardSkillRevealStage.BasicB: return "基礎解放";
            case CardSkillRevealStage.FullC: return "完整規則";
            default: return "戰技覺察";
        }
    }

    public static void CreatePanelBorder(Transform panel)
    {
        Color border = FrameHighlightDim;
        CreateBorderBar(panel, "BorderTop", border, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 2f));
        CreateBorderBar(panel, "BorderBottom", border, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f));
        CreateBorderBar(panel, "BorderLeft", border, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(2f, 0f));
        CreateBorderBar(panel, "BorderRight", border, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(2f, 0f));
    }

    private static void CreateBorderBar(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        bar.transform.SetAsFirstSibling();
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = bar.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    public static Image CreateSolid(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void AddTmpShadow(TextMeshProUGUI tmp, Color color, Vector2 distance)
    {
        if (tmp == null) return;
        Shadow shadow = tmp.gameObject.GetComponent<Shadow>();
        if (shadow == null) shadow = tmp.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}

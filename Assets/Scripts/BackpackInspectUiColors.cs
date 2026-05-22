using UnityEngine;

/// <summary>背包卡牌詳情 UI 配色（<see cref="BACKPACK_INSPECT_UI_COLOR_SPEC.md"/>）。與 <see cref="BattleUiColors"/> 分冊。</summary>
public static class BackpackInspectUiColors
{
    // --- 整合主面：館藏紙（動森主色，減少冷灰分區） ---
    public static readonly Color PagePaper = Hex("#FFF8E7");
    public static readonly Color PagePaperInset = Hex("#F5E6C8");
    public static readonly Color PagePaperMuted = Hex("#E8DCC8");
    public static readonly Color PageDivider = Hex("#E0D4C4");

    // 立繪井：保留少量天藍作「畫框區」點綴，不當全頁底
    public static readonly Color SkyWash = Hex("#C5DCE8");
    public static readonly Color SkyDeep = Hex("#9BB5C8");
    public static readonly Color ArtWellWash = WithAlpha(SkyWash, 0.42f);
    public static readonly Color ArtFrame = Hex("#A8B5A8");

    public static readonly Color Dim = WithAlpha(Hex("#4A4038"), 0.45f);

    // 相容舊 token 名稱（程式／規格對照）
    public static readonly Color PanelLinen = PagePaper;
    public static readonly Color PanelCream = PagePaperInset;
    public static readonly Color PanelMilk = PagePaper;
    public static readonly Color PanelScroll = PageDivider;
    public static readonly Color PanelSkill = PagePaperInset;
    public static readonly Color StatStripBg = PagePaperInset;
    public static readonly Color StatChipBg = PagePaper;

    public static readonly Color Ink = Hex("#5C4033");
    public static readonly Color InkSoft = Hex("#493F3B");
    public static readonly Color InkMuted = Hex("#6B5F58");
    public static readonly Color InkOnSkill = Ink;
    public static readonly Color MainTitle = Hex("#F8D878");
    public static readonly Color MintLabel = InkSoft;

    public static readonly Color BtnBack = PageDivider;
    public static readonly Color BtnBackText = Ink;

    public static readonly Color TabSelectedBg = PagePaper;
    public static readonly Color TabSelectedText = Ink;
    public static readonly Color TabIdleBg = PagePaperMuted;
    public static readonly Color TabIdleText = InkMuted;

    public static readonly Color ProficiencyBg = PagePaperMuted;
    public static readonly Color ProficiencyTrack = WithAlpha(Ink, 0.14f);
    public static readonly Color ProficiencyFill = Hex("#D4A04A");
    public static readonly Color ProficiencyLabel = Ink;
    public static readonly Color ProficiencyStatus = InkMuted;

    public static readonly Color StageA = Hex("#6A9A82");
    public static readonly Color StageB = Hex("#6A8FA8");
    public static readonly Color StageC = Hex("#C49A4A");

    public static Color RarityN => Hex("#8A939C");
    public static Color RarityR => Hex("#6FA878");
    public static Color RaritySr => Hex("#5A8FB8");
    public static Color RaritySsr => Hex("#9A7AB8");
    public static Color RarityUr => Hex("#D4A04A");

    public static Color Rarity(CardRarity rarity) => rarity switch
    {
        CardRarity.R => RarityR,
        CardRarity.SR => RaritySr,
        CardRarity.SSR => RaritySsr,
        CardRarity.UR => RarityUr,
        _ => RarityN
    };

    public static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
        return Color.magenta;
    }

    public static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}

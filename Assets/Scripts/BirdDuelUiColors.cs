using UnityEngine;

/// <summary>
/// 鬥鳥系統 UI 配色：暖奶油底 + 暮光紫羅蘭 + 極光金青點綴。
/// 目標感受：帶點驚奇感的戰前儀式／小遊戲氛圍（銜接 <see cref="BattleUiColors"/> 暖色，但更夢幻）。
/// </summary>
public static class BirdDuelUiColors
{
    // --- Overlay chrome（戰前彈窗）---
    public static readonly Color Dim = WithAlpha(Hex("#1A1028"), 0.74f);
    public static readonly Color Panel = Hex("#FFF6EC");
    public static readonly Color PanelEdge = WithAlpha(Hex("#9B7ED8"), 0.38f);
    public static readonly Color HeaderBand = Hex("#4E3A6E");
    public static readonly Color InfoCard = Hex("#F2EAFB");
    public static readonly Color Ink = Hex("#3D2E48");
    public static readonly Color InkSoft = Hex("#6B5A78");
    public static readonly Color WonderBadge = Hex("#F8D878");
    public static readonly Color WonderGlow = Hex("#7AD4E8");
    public static readonly Color OnDarkText = Hex("#FFF8F0");

    // --- Buttons ---
    public static readonly Color BtnPrimary = Hex("#C9A03A");
    public static readonly Color BtnPrimaryH = Hex("#E0BC52");
    public static readonly Color BtnPrimaryP = Hex("#9A7828");
    public static readonly Color BtnPrimaryText = Hex("#FFF8E7");
    public static readonly Color BtnSecondary = Hex("#7B6B9A");
    public static readonly Color BtnSecondaryH = Hex("#9182B0");
    public static readonly Color BtnSecondaryP = Hex("#5E4F78");
    public static readonly Color BtnSecondaryText = Hex("#F5F0FF");
    public static readonly Color BtnGhost = WithAlpha(OnDarkText, 0.14f);
    public static readonly Color BtnGhostH = WithAlpha(OnDarkText, 0.24f);
    public static readonly Color BtnGhostP = WithAlpha(OnDarkText, 0.08f);
    public static readonly Color BtnDisabledBg = WithAlpha(Hex("#D8CCE8"), 0.55f);

    // --- 鬥鳥場景本體 ---
    public static readonly Color SceneBg = Hex("#1E1632");
    public static readonly Color ScenePanel = WithAlpha(Hex("#2A2240"), 0.96f);
    public static readonly Color Subtitle = Hex("#C8B8DC");
    public static readonly Color ResultLine = Hex("#E8E0F4");

    // --- 手勢按鈕 ---
    public static readonly Color GesturePeck = Hex("#E86A5A");
    public static readonly Color GestureWing = Hex("#5BA8D4");
    public static readonly Color GestureNest = Hex("#F0C14A");
    public static readonly Color GesturePass = Hex("#9B8FAD");

    // --- 對局回饋 ---
    public static readonly Color ScoreFill = Hex("#5DC49A");
    public static readonly Color InsightFill = Hex("#F0C14A");
    public static readonly Color GoodFeedback = Hex("#8FD4A8");
    public static readonly Color OpponentIdle = Hex("#4A3D5C");
    public static readonly Color OpponentName = Hex("#F0D898");
    public static readonly Color BeatPadIdle = WithAlpha(Hex("#E8E0F8"), 0.88f);
    public static readonly Color ShrinkIdle = WithAlpha(WonderGlow, 0.28f);
    public static readonly Color Decisive = Hex("#FF9A4A");
    public static readonly Color FakeScareRing = OnDarkText;

    // --- CD 選擇 overlay ---
    public static readonly Color CdPanelBg = WithAlpha(Hex("#2A2240"), 0.98f);
    public static readonly Color CdPanelAccent = Hex("#3D2F58");
    public static readonly Color CdFrameBg = Hex("#564672");
    public static readonly Color CdSlotIdle = Hex("#3A2E50");
    public static readonly Color CdSlotSelected = Hex("#7B6B9A");
    public static readonly Color CdTextMain = OnDarkText;
    public static readonly Color CdTextMuted = Hex("#B8A8CC");

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

using UnityEngine;
using UnityEngine.UI;

/// <summary>對戰場景 UI 配色（<see cref="BATTLE_UI_COLOR_SPEC.md"/> §1.3 範圍內）。</summary>
public static class BattleUiColors
{
    public static readonly Color Ink = Hex("#5C4033");
    public static readonly Color InkSoft = Hex("#493F3B");
    public static readonly Color PanelCream = Hex("#F5E6C8");
    public static readonly Color PanelCream96 = WithAlpha(PanelCream, 0.96f);
    public static readonly Color PanelMilk = Hex("#FFF8E7");
    public static readonly Color PanelMilk985 = WithAlpha(PanelMilk, 0.985f);
    public static readonly Color PanelScroll = Hex("#E0D4C4");
    public static readonly Color PanelEdge35 = WithAlpha(Hex("#8B7355"), 0.35f);

    public static readonly Color BtnPrimary = Hex("#9A7A55");
    public static readonly Color BtnPrimaryH = Hex("#AE8E66");
    public static readonly Color BtnPrimaryP = Hex("#6F5A3A");
    public static readonly Color BtnPrimaryText = Hex("#FFF8E7");
    public static readonly Color BtnSecondary = Hex("#5A7A8F");
    public static readonly Color BtnSecondaryH = Hex("#6B8FA3");
    public static readonly Color BtnSecondaryP = Hex("#465F6F");
    public static readonly Color BtnSecondaryLight = Hex("#7A95A8");
    public static readonly Color BtnSecondaryLightH = Hex("#8FA9BA");
    public static readonly Color BtnSecondaryLightP = Hex("#627985");
    public static readonly Color BtnSecondaryText = Hex("#E8F2F6");
    /// <summary>Footer action buttons: high-contrast label on tinted backgrounds.</summary>
    public static readonly Color BtnFooterLabelText = Hex("#FFFFFF");
    public static readonly Color BtnDisabledBg = WithAlpha(PanelCream, 0.5f);
    public static readonly Color BtnDisabledText = WithAlpha(Hex("#6B5F58"), 0.7f);

    public static readonly Color AllyHp = Hex("#5F8F72");
    public static readonly Color AllyLabel = Hex("#A8CEC6");
    public static readonly Color FoeHp = Hex("#B8846E");
    public static readonly Color FoeLabel = Hex("#E0AA90");
    public static readonly Color OutlineHp = WithAlpha(Hex("#382624"), 0.55f);

    public static readonly Color TurnBg = WithAlpha(Hex("#3D3835"), 0.85f);
    public static readonly Color TurnPlayer = Hex("#F8D878");
    public static readonly Color TurnEnemy = Hex("#E0AA90");
    public static readonly Color TurnBannerText = Hex("#FFF8E7");

    public static readonly Color DeckTop = Hex("#4A6B7C");
    public static readonly Color DeckShadow = WithAlpha(Hex("#2E3542"), 0.92f);

    /// <summary>教練立繪墊底（letterbox／無立繪時）；淡奶油（PANEL_MILK），與暖棕髮色分離。</summary>
    public static readonly Color CoachPortraitMat = PanelMilk;

    /// <summary>教練立繪外框描邊。</summary>
    public static readonly Color CoachPortraitFrame = PanelEdge35;

    /// <summary>教練面板底；比 <see cref="CoachPortraitMat"/> 略深一階的奶油色。</summary>
    public static readonly Color CoachPanelBg = PanelCream96;

    /// <summary>教練外環光暈填色（BorderGlow 底色）；奶油相近色、明度略低於立繪墊底。</summary>
    public static readonly Color CoachBorderGlowFill = PanelCream;

    /// <summary>教練未讀提示外環脈動（青綠，非咖啡系）。</summary>
    public static readonly Color CoachUnreadPulseGlow = WithAlpha(AllyLabel, 0.88f);

    /// <summary>教練未讀提示外環脈動暗態。</summary>
    public static readonly Color CoachUnreadPulseGlowDim = WithAlpha(AllyHp, 0.42f);

    /// <summary>教練展開提示文字面板底（深棕，與立繪分離）。</summary>
    public static readonly Color CoachHintTextPanelBg = WithAlpha(Hex("#2A2218"), 0.94f);

    /// <summary>教練展開提示文字面板描邊。</summary>
    public static readonly Color CoachHintTextPanelEdge = WithAlpha(Hex("#8B7355"), 0.45f);

    /// <summary>教練展開提示文字（淺色，對比深色底）。</summary>
    public static readonly Color CoachHintText = PanelMilk;

    public static readonly Color Dim = WithAlpha(Hex("#0A0305"), 0.5f);
    public static readonly Color DimHeavy = WithAlpha(Hex("#0A0305"), 0.66f);
    public static readonly Color ShadowUi = WithAlpha(Color.black, 0.45f);

    public static readonly Color HallWine = WithAlpha(Hex("#714847"), 0.96f);
    public static readonly Color HallWine28 = WithAlpha(Hex("#714847"), 0.28f);

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

    public static void ApplyHpOutline(Outline outline)
    {
        if (outline == null) return;
        outline.effectColor = OutlineHp;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    public static bool IsDebugChromeButton(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        string n = objectName.ToLowerInvariant();
        return n.Contains("closedebug") || n == "closebattlepanelbutton";
    }

    public static bool UsesPrimaryButton(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        string n = objectName.ToLowerInvariant();
        if (n.Contains("endturn") || n.Contains("end_turn") || n.Contains("結束回合")) return true;
        if (n.Contains("resume")) return true;
        return false;
    }

    public static void ApplyHallWineButton(Button button)
    {
        if (button == null) return;

        Image img = button.targetGraphic as Image;
        if (img == null) img = button.GetComponent<Image>();
        if (img == null) return;

        Color normal = HallWine;
        Color highlighted = WithAlpha(Hex("#8A5857"), 0.98f);
        Color pressed = WithAlpha(Hex("#5A3A39"), 0.96f);

        // Graphic stays white; tint comes from ColorBlock (avoids double-multiply darkening).
        img.color = Color.white;
        var cb = button.colors;
        cb.normalColor = normal;
        cb.highlightedColor = highlighted;
        cb.pressedColor = pressed;
        cb.selectedColor = highlighted;
        cb.disabledColor = BtnDisabledBg;
        button.colors = cb;

        Text legacyLabel = button.GetComponentInChildren<Text>(true);
        if (legacyLabel != null) legacyLabel.color = BtnPrimaryText;

        TMPro.TextMeshProUGUI tmpLabel = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmpLabel != null)
        {
            tmpLabel.color = BtnFooterLabelText;
            tmpLabel.faceColor = BtnFooterLabelText;
        }
    }

    public static void ApplyButtonStyle(Button button, string objectName)
    {
        if (button == null || IsDebugChromeButton(objectName)) return;

        Image img = button.targetGraphic as Image;
        if (img == null) img = button.GetComponent<Image>();
        if (img == null) return;

        bool primary = UsesPrimaryButton(objectName);
        Color normal = primary ? BtnPrimary : BtnSecondary;
        Color highlighted = primary ? BtnPrimaryH : BtnSecondaryH;
        Color pressed = primary ? BtnPrimaryP : BtnSecondaryP;
        Color disabledBg = BtnDisabledBg;
        Color disabledText = BtnDisabledText;

        img.color = normal;
        var cb = button.colors;
        cb.normalColor = normal;
        cb.highlightedColor = highlighted;
        cb.pressedColor = pressed;
        cb.selectedColor = highlighted;
        cb.disabledColor = disabledBg;
        button.colors = cb;

        Text legacyLabel = button.GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
            legacyLabel.color = primary ? BtnPrimaryText : BtnSecondaryText;

        TMPro.TextMeshProUGUI tmpLabel = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmpLabel != null)
            tmpLabel.color = primary ? BtnPrimaryText : BtnSecondaryText;
    }
}

using TMPro;
using UnityEngine;

/// <summary>Settings 場景 TMP 字型，固定優先使用 Noto Sans TC。</summary>
public static class SettingsUiFonts
{
    private const string CjkProbe = BattleCardTuningPresetDisplay.CjkFontProbe;

    private static TMP_FontAsset cachedParameterFont;

    public static TMP_FontAsset ResolveParameterDetailsFont()
    {
        if (cachedParameterFont != null && SupportsCjk(cachedParameterFont))
            return cachedParameterFont;

        TMP_FontAsset font = UiFontResolver.ResolveUiFont();
        if (font != null && SupportsCjk(font))
        {
            cachedParameterFont = font;
            return cachedParameterFont;
        }

        cachedParameterFont = BuildbeckUiFonts.ResolveCjkFont(CjkProbe);
        if (cachedParameterFont == null)
            Debug.LogWarning("SettingsUiFonts: 找不到支援中文的 TMP 字型，Parameter details 可能無法顯示中文。");
        return cachedParameterFont;
    }

    public static void ApplyTo(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        if (tmp.font != null && SupportsCjk(tmp.font))
        {
            tmp.richText = false;
            return;
        }

        TMP_FontAsset font = ResolveParameterDetailsFont();
        if (font != null)
            tmp.font = font;

        tmp.richText = false;
        tmp.parseCtrlCharacters = true;
    }

    private static bool SupportsCjk(TMP_FontAsset font) =>
        font != null && BuildbeckUiFonts.FontSupportsText(font, CjkProbe);
}

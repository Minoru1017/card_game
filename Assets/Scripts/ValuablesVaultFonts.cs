using TMPro;
using UnityEngine;

/// <summary>貴重品庫 TMP：優先 Noto Sans TC，避免全局導航精簡字型缺標點。</summary>
public static class ValuablesVaultFonts
{
    public static void ApplyTo(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        TMP_FontAsset font = ResolveUIFont();
        if (font != null)
            tmp.font = font;

        tmp.richText = false;
        tmp.parseCtrlCharacters = true;
    }

    public static TMP_FontAsset ResolveUIFont()
    {
        TMP_FontAsset font = SettingsUiFonts.ResolveParameterDetailsFont();
        if (font != null && SupportsVaultGlyphs(font))
            return font;

        font = UiFontResolver.ResolveUiFont();
        if (font != null && SupportsVaultGlyphs(font))
            return font;

        return font;
    }

    public static bool SupportsVaultGlyphs(TMP_FontAsset font) =>
        font != null && BuildbeckUiFonts.FontSupportsText(font, ValuablesVaultDisplay.FontGlyphProbe);
}

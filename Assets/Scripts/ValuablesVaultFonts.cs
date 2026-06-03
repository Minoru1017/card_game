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
        TMP_FontAsset noto = SettingsUiFonts.ResolveParameterDetailsFont();
        if (noto != null && SupportsVaultGlyphs(noto))
            return noto;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset font = fonts[i];
            if (font == null || string.IsNullOrEmpty(font.name)) continue;
            string name = font.name;
            if (name.IndexOf("NotoSansTC", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Noto Sans TC", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (!SupportsVaultGlyphs(font)) continue;
            return font;
        }

        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null && SupportsVaultGlyphs(buildbeck))
            return buildbeck;

        return noto != null ? noto : TMP_Settings.defaultFontAsset;
    }

    public static bool SupportsVaultGlyphs(TMP_FontAsset font) =>
        font != null && BuildbeckUiFonts.FontSupportsText(font, ValuablesVaultDisplay.FontGlyphProbe);
}

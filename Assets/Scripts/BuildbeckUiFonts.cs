using TMPro;
using UnityEngine;

/// <summary>TMP font resolution for Buildbeck UI (CJK-safe labels).</summary>
public static class BuildbeckUiFonts
{
    private static TMP_FontAsset cachedBuildbeckFont;

    public static TMP_FontAsset ResolveBuildbeckButtonFont()
    {
        const string required = "儲存返回";
        if (cachedBuildbeckFont != null && FontSupportsText(cachedBuildbeckFont, required))
            return cachedBuildbeckFont;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset f = fonts[i];
            if (!FontSupportsText(f, required)) continue;
            if (FontNameLikelySupportsCjk(f.name))
            {
                cachedBuildbeckFont = f;
                return cachedBuildbeckFont;
            }
        }

        if (TMP_Settings.defaultFontAsset != null && FontSupportsText(TMP_Settings.defaultFontAsset, required))
        {
            cachedBuildbeckFont = TMP_Settings.defaultFontAsset;
            return cachedBuildbeckFont;
        }

        return null;
    }

    public static bool FontSupportsText(TMP_FontAsset font, string required)
    {
        if (font == null || string.IsNullOrEmpty(required)) return false;
        for (int i = 0; i < required.Length; i++)
        {
            char ch = required[i];
            if (char.IsWhiteSpace(ch)) continue;
            if (!font.HasCharacter(ch, true)) return false;
        }
        return true;
    }

    public static bool FontNameLikelySupportsCjk(string fontAssetName)
    {
        if (string.IsNullOrEmpty(fontAssetName)) return false;
        string n = fontAssetName.ToLowerInvariant();
        return n.Contains("noto") ||
               n.Contains("cjk") ||
               n.Contains("sourcehan") ||
               n.Contains("source han") ||
               n.Contains("jhenghei") ||
               n.Contains("yahei") ||
               n.Contains("pingfang") ||
               n.Contains("heiti") ||
               n.Contains("simhei") ||
               n.Contains("simsun") ||
               n.Contains("msjh") ||
               n.Contains("mingliu");
    }
}

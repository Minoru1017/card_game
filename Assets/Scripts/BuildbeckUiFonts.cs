using TMPro;
using UnityEngine;

/// <summary>TMP font resolution for Buildbeck UI (CJK-safe labels).</summary>
public static class BuildbeckUiFonts
{
    private const string DefaultProbe = "儲存返回";
    private static TMP_FontAsset cachedBuildbeckFont;

    public static TMP_FontAsset ResolveBuildbeckButtonFont() =>
        ResolveCjkFont(DefaultProbe);

    public static TMP_FontAsset ResolveCjkFont(string glyphProbe = DefaultProbe)
    {
        if (cachedBuildbeckFont != null && FontSupportsText(cachedBuildbeckFont, glyphProbe))
            return cachedBuildbeckFont;

        TMP_FontAsset font = UiFontResolver.ResolveUiFont();
        if (font != null && FontSupportsText(font, glyphProbe))
        {
            cachedBuildbeckFont = font;
            return cachedBuildbeckFont;
        }

        return font;
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

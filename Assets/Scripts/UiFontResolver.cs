using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全遊戲 UI 字型解析：優先 Noto Sans TC，避免退回 LiberationSans 造成中文缺字。
/// Legacy UI.Text 則使用同一套 Noto TTF。
/// </summary>
public static class UiFontResolver
{
    private static TMP_FontAsset cachedUiFont;
    private static Font cachedLegacyUiFont;

    public static TMP_FontAsset ResolveUiFont()
    {
        if (cachedUiFont != null)
            return cachedUiFont;

        UiFontLibrary library = UiFontLibrary.Instance;
        if (library != null)
        {
            if (library.CjkFont != null)
            {
                cachedUiFont = library.CjkFont;
                return cachedUiFont;
            }

            if (library.DefaultUiFont != null)
            {
                cachedUiFont = library.DefaultUiFont;
                return cachedUiFont;
            }
        }

        cachedUiFont = TMP_Settings.defaultFontAsset;
        return cachedUiFont;
    }

    public static Font ResolveLegacyUiFont()
    {
        if (cachedLegacyUiFont != null)
            return cachedLegacyUiFont;

        UiFontLibrary library = UiFontLibrary.Instance;
        if (library != null && library.CjkSourceFont != null)
        {
            cachedLegacyUiFont = library.CjkSourceFont;
            return cachedLegacyUiFont;
        }

        cachedLegacyUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return cachedLegacyUiFont;
    }

    public static void ApplyTo(TextMeshProUGUI tmp, string textForGlyphs = null)
    {
        if (tmp == null) return;

        TMP_FontAsset font = ResolveUiFont();
        if (font != null)
            tmp.font = font;

        EnsureGlyphs(font, textForGlyphs ?? tmp.text);
    }

    public static void ApplyTo(Text text)
    {
        if (text == null) return;
        text.font = ResolveLegacyUiFont();
    }

    public static void EnsureGlyphs(TMP_FontAsset font, string text)
    {
        if (font == null || string.IsNullOrEmpty(text))
            return;

        font.TryAddCharacters(text, out _);
    }
}

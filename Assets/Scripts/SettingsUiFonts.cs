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

        TMP_FontAsset noto = FindNotoSansTcFont();
        if (noto != null)
        {
            cachedParameterFont = noto;
            return cachedParameterFont;
        }

        TMP_FontAsset fromLabels = FindFontOnSceneLabels();
        if (fromLabels != null)
        {
            cachedParameterFont = fromLabels;
            return cachedParameterFont;
        }

        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null && SupportsCjk(buildbeck))
        {
            cachedParameterFont = buildbeck;
            return cachedParameterFont;
        }

        Debug.LogWarning("SettingsUiFonts: 找不到支援中文的 TMP 字型，Parameter details 可能無法顯示中文。");
        return null;
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

    private static TMP_FontAsset FindNotoSansTcFont()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset font = fonts[i];
            if (font == null) continue;
            string name = font.name;
            if (string.IsNullOrEmpty(name)) continue;
            if (name.IndexOf("NotoSansTC", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Noto Sans TC", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (!SupportsCjk(font)) continue;
            return font;
        }

        return null;
    }

    private static TMP_FontAsset FindFontOnSceneLabels()
    {
        TextMeshProUGUI[] labels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI label = labels[i];
            if (label == null || label.font == null) continue;
            if (!SupportsCjk(label.font)) continue;
            return label.font;
        }

        return null;
    }
}

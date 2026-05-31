using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Plot scene TMP helpers (choice buttons need explicit light labels on wine buttons).</summary>
public static class PlotUiTextUtil
{
    public static TMP_Text EnsureButtonLabel(Button button, TMP_Text cached, TMP_Text fontSource)
    {
        if (button == null) return null;

        if (cached != null && cached.transform != null && cached.transform.IsChildOf(button.transform))
            return cached;

        TMP_Text found = button.GetComponentInChildren<TMP_Text>(true);
        if (found != null) return found;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(button.transform, false);
        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        found = labelGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(found, fontSource);

        return found;
    }

    public static void ApplyButtonLabel(TMP_Text tmp, string label, TMP_Text fontSource = null)
    {
        if (tmp == null) return;

        ApplyFont(tmp, fontSource);

        tmp.gameObject.SetActive(true);
        tmp.enabled = true;
        tmp.richText = false;

        Color textColor = BattleUiColors.BtnPrimaryText;
        tmp.SetText(label ?? string.Empty);
        tmp.color = textColor;
        tmp.faceColor = textColor;
        tmp.fontSize = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        tmp.transform.SetAsLastSibling();

        tmp.ForceMeshUpdate(true, true);
    }

    private static void ApplyFont(TMP_Text tmp, TMP_Text fontSource)
    {
        if (tmp == null) return;

        if (fontSource != null && fontSource.font != null)
        {
            tmp.font = fontSource.font;
            tmp.fontSharedMaterial = fontSource.font.material;
            return;
        }

        if (tmp is TextMeshProUGUI ugui)
            SettingsUiFonts.ApplyTo(ugui);
    }

    /// <summary>劇情疊層用：套用字型但保留 richText。</summary>
    public static void ApplyFontForRichText(TMP_Text tmp, TMP_Text fontSource)
    {
        if (tmp == null) return;

        if (fontSource != null && fontSource.font != null)
        {
            tmp.font = fontSource.font;
            tmp.fontSharedMaterial = fontSource.fontSharedMaterial;
            return;
        }

        TMP_FontAsset font = SettingsUiFonts.ResolveParameterDetailsFont();
        if (font != null)
            tmp.font = font;
        else if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }
}

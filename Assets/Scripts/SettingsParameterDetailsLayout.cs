using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 將 Parameter details 內 title／Sub-label／content 排版至面板可用區域，並為 content 提供捲動。
/// </summary>
public static class SettingsParameterDetailsLayout
{
    private const string ContentScrollName = "content scroll";
    private const string ContentViewportName = "content viewport";

    private const float HorizontalPad = 28f;
    private const float TopPad = 20f;
    private const float TitleBandHeight = 72f;
    private const float SubtitleBandHeight = 60f;
    private const float BandGap = 10f;
    private const float BottomPad = 24f;

    public static void Apply(
        RectTransform panel,
        TextMeshProUGUI title,
        TextMeshProUGUI subtitle,
        TextMeshProUGUI content)
    {
        if (panel == null) return;

        float subtitleTop = TopPad + TitleBandHeight + BandGap;
        float contentTop = subtitleTop + SubtitleBandHeight + BandGap;

        ApplyHeaderBand(title, TopPad, TitleBandHeight, 30f, 44f);
        ApplyHeaderBand(subtitle, subtitleTop, SubtitleBandHeight, 22f, 30f);
        EnsureContentScrollArea(panel, content, contentTop, BottomPad);
    }

    public static void RefreshAfterTextChanged(TextMeshProUGUI content)
    {
        if (content == null) return;

        RectTransform contentRt = content.rectTransform;
        RectTransform viewport = contentRt.parent as RectTransform;
        content.ForceMeshUpdate();

        float width = viewport != null ? viewport.rect.width - HorizontalPad * 2f : 900f;
        width = Mathf.Max(240f, width);
        Vector2 preferred = content.GetPreferredValues(width, 0f);
        float height = Mathf.Max(160f, preferred.y + content.margin.y + content.margin.w);
        contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        if (viewport != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
    }

    /// <summary>UGUI 對照表建立後，將 ScrollRect 內容改為表格並重算高度。</summary>
    public static void RefreshAfterTableChanged(RectTransform viewport)
    {
        if (viewport == null) return;

        RectTransform table = SettingsParameterComparisonTable.FindTableRoot(viewport);
        if (table == null) return;

        ScrollRect scroll = viewport.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.content = table;

        float width = Mathf.Max(240f, viewport.rect.width);
        table.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        LayoutRebuilder.ForceRebuildLayoutImmediate(table);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
    }

    private static void ApplyHeaderBand(
        TextMeshProUGUI tmp,
        float topInset,
        float bandHeight,
        float fontMin,
        float fontMax)
    {
        if (tmp == null) return;

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(HorizontalPad, -(topInset + bandHeight));
        rt.offsetMax = new Vector2(-HorizontalPad, -topInset);

        SettingsUiFonts.ApplyTo(tmp);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = fontMin;
        tmp.fontSizeMax = fontMax;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.margin = new Vector4(6f, 4f, 6f, 4f);
        tmp.raycastTarget = false;
    }

    private static void EnsureContentScrollArea(
        RectTransform panel,
        TextMeshProUGUI content,
        float topInset,
        float bottomInset)
    {
        if (content == null) return;

        RectTransform scrollRt = FindChildRect(panel, ContentScrollName);
        ScrollRect scroll;
        RectTransform viewportRt;

        if (scrollRt == null)
        {
            GameObject scrollGo = new GameObject(ContentScrollName, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            scrollRt = scrollGo.GetComponent<RectTransform>();
            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.12f);
            scrollBg.raycastTarget = true;

            Mask mask = scrollGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject viewportGo = new GameObject(ContentViewportName, typeof(RectTransform), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            Image viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = Color.clear;
            viewportImg.raycastTarget = true;

            scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            content.rectTransform.SetParent(viewportRt, false);
        }
        else
        {
            scroll = scrollRt.GetComponent<ScrollRect>();
            viewportRt = scroll != null && scroll.viewport != null
                ? scroll.viewport
                : FindChildRect(scrollRt, ContentViewportName);

            if (viewportRt != null && content.transform.parent != viewportRt)
                content.rectTransform.SetParent(viewportRt, false);
        }

        ApplyFillInset(scrollRt, topInset, bottomInset);

        RectTransform contentRt = content.rectTransform;
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;

        SettingsUiFonts.ApplyTo(content);
        content.enableWordWrapping = true;
        content.overflowMode = TextOverflowModes.Overflow;
        content.enableAutoSizing = false;
        content.fontSize = 26f;
        content.lineSpacing = 6f;
        content.paragraphSpacing = 12f;
        content.alignment = TextAlignmentOptions.TopLeft;
        content.verticalAlignment = VerticalAlignmentOptions.Top;
        content.margin = new Vector4(8f, 8f, 12f, 16f);
        content.raycastTarget = false;

        if (scroll != null)
        {
            RectTransform table = SettingsParameterComparisonTable.FindTableRoot(viewportRt);
            scroll.content = table != null ? table : contentRt;
        }
    }

    private static void ApplyFillInset(RectTransform rt, float topInset, float bottomInset)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(HorizontalPad, bottomInset);
        rt.offsetMax = new Vector2(-HorizontalPad, -topInset);
    }

    private static RectTransform FindChildRect(Transform parent, string childName)
    {
        if (parent == null) return null;
        Transform[] all = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && t.name == childName)
                return t as RectTransform;
        }

        return null;
    }
}

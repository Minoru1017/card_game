using UnityEngine;

/// <summary>鬥鳥戰前彈窗：手機優先版面常數與錨點工具。</summary>
public static class BirdDuelMobileOverlayLayout
{
    /// <summary>左右留白比例：面板約佔螢幕寬 68%（橫向手機不會過寬）。</summary>
    public const float MarginH = 0.16f;
    public const float MarginTop = 0.055f;
    public const float MarginBottom = 0.04f;

    public const float HeaderHeight = 64f;
    public const float ContentPadH = 20f;
    public const float SectionGap = 16f;

    public const float TitleFontSize = 38f;
    public const float BadgeFontSize = 22f;
    public const float BodyFontSize = 28f;
    public const float ButtonFontPrimary = 34f;
    public const float ButtonFontSecondary = 30f;
    /// <summary>主操作：約 80px @1080p，符合手機觸控。</summary>
    public const float ButtonHeightPrimary = 80f;
    /// <summary>次操作：略低於主按鈕，仍保留足夠點擊面積。</summary>
    public const float ButtonHeightSecondary = 68f;
    /// <summary>頂欄幽靈按鈕（返回）。</summary>
    public const float ButtonHeightGhost = 52f;
    public const float ButtonGap = 12f;
    public const float ButtonAreaPadBottom = 20f;

    /// <summary>CD 選擇／雙欄 footer 列總高。</summary>
    public const float FooterBarHeight = 116f;
    public const float FooterButtonPadV = 14f;
    public const float SwitchRowHeight = 68f;

    public const float InfoCardMinHeight = 132f;

    /// <summary>面板貼齊螢幕並保留安全邊距（適合直式手機閱讀）。</summary>
    public static void ApplyMobilePanel(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(MarginH, MarginBottom);
        rt.anchorMax = new Vector2(1f - MarginH, 1f - MarginTop);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    public static void StretchTopBand(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
    }

    public static void StretchHorizontal(RectTransform rt, float top, float bottom, float padH = ContentPadH)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(padH, bottom);
        rt.offsetMax = new Vector2(-padH, -top);
    }

    /// <summary>自下而上堆疊全寬按鈕；index 0 = 最下方（主操作）。</summary>
    public static void PlaceStackedButton(
        RectTransform rt,
        int indexFromBottom,
        float primaryHeight = ButtonHeightPrimary,
        float secondaryHeight = ButtonHeightSecondary,
        float gap = ButtonGap,
        float bottomPad = ButtonAreaPadBottom)
    {
        float y = bottomPad;
        for (int i = 0; i < indexFromBottom; i++)
        {
            float h = i == 0 ? primaryHeight : secondaryHeight;
            y += h + gap;
        }

        float height = indexFromBottom == 0 ? primaryHeight : secondaryHeight;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-ContentPadH * 2f, height);
    }

    public static bool PreferPortraitStack()
    {
        if (Screen.height <= 0) return true;
        return (float)Screen.width / Screen.height < 0.92f;
    }
}

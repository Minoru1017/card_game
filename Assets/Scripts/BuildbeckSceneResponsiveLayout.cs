using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Buildbeck 組牌區：依實際螢幕比例（含 iPad Mini 4 等 4:3）重算 Library / Deck 容器位置與尺寸，避免固定 1920 常數重疊或裁切。
/// </summary>
public static class BuildbeckSceneResponsiveLayout
{
    private const string SceneName = "Buildbeck";
    /// <summary>低於此寬高比（如 4:3 ≈ 1.33）視為方屏，啟用緊湊排版。</summary>
    private const float CompactAspectThreshold = 1.6f;
    private const float LibraryDeckHorizontalGapPx = 20f;
    private const float TopUiReservePx = 88f;
    private const float CompactHeightScaleMin = 0.78f;
    private const float CompactWidthScaleMin = 0.72f;

    private static Rect lastSafeArea;
    private static Vector2Int lastScreenSize;

    public readonly struct ResolvedLayout
    {
        public readonly float LibraryX;
        public readonly float LibraryY;
        public readonly float LibraryWidth;
        public readonly float LibraryHeight;
        public readonly float DeckRightInset;
        public readonly float DeckBottom;
        public readonly float DeckWidth;
        public readonly float DeckHeight;

        public ResolvedLayout(
            float libraryX,
            float libraryY,
            float libraryWidth,
            float libraryHeight,
            float deckRightInset,
            float deckBottom,
            float deckWidth,
            float deckHeight)
        {
            LibraryX = libraryX;
            LibraryY = libraryY;
            LibraryWidth = libraryWidth;
            LibraryHeight = libraryHeight;
            DeckRightInset = deckRightInset;
            DeckBottom = deckBottom;
            DeckWidth = deckWidth;
            DeckHeight = deckHeight;
        }
    }

    public static void ApplyIfNeeded(bool force = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
            return;

        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == lastSafeArea && size == lastScreenSize)
            return;

        lastSafeArea = safe;
        lastScreenSize = size;
        ApplyNow(scene);
    }

    public static void ApplyNow(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
            return;

        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        if (deckManager == null)
            return;

        deckManager.ApplyBuildbeckSceneResponsiveLayout();
    }

    public static ResolvedLayout Resolve(Canvas canvas, float deckVerticalTunePx = 0f)
    {
        float libraryX = DeckManager.BuildbeckLibraryGridViewportLeftInsetPx +
                         DeckManager.BuildbeckLibraryGridViewportLeftBreathingPx +
                         DeckManager.BuildbeckLibraryGridViewportCenterPointOffsetXPx +
                         DeckManager.BuildbeckLibraryGridViewportExtraOffsetXPx;
        float libraryY = DeckManager.BuildbeckLibraryGridViewportBottomInsetPx +
                        DeckManager.BuildbeckDeckBuildingContainersVerticalLiftPx;
        float libraryW = DeckManager.BuildbeckLibraryGridViewportWidthPx;
        float libraryH = DeckManager.BuildbeckLibraryGridViewportHeightPx;

        float deckRightInset = DeckManager.BuildbeckDeckViewportRightInsetPx +
                               DeckManager.BuildbeckDeckViewportLeftShiftPx;
        float deckBottom = DeckManager.BuildbeckDeckViewportBottomPx +
                           DeckManager.BuildbeckDeckBuildingContainersVerticalLiftPx +
                           deckVerticalTunePx;
        float deckW = DeckManager.BuildbeckDeckViewportWidthPx;
        float deckH = DeckManager.BuildbeckDeckViewportHeightPx;

        if (canvas == null)
            return new ResolvedLayout(libraryX, libraryY, libraryW, libraryH, deckRightInset, deckBottom, deckW, deckH);

        RectTransform canvasRt = canvas.transform as RectTransform;
        float canvasW = canvasRt != null ? canvasRt.rect.width : MobileUiLayoutPolicy.ReferenceResolution.x;
        float canvasH = canvasRt != null ? canvasRt.rect.height : MobileUiLayoutPolicy.ReferenceResolution.y;
        MobileUiLayoutPolicy.CanvasSafeInsets safe = MobileUiLayoutPolicy.GetCanvasSafeInsets(canvas);

        Vector2 layoutPx = MobileUiLayoutPolicy.GetLayoutPixelSize();
        float screenAspect = layoutPx.x / Mathf.Max(1f, layoutPx.y);
        bool compact = screenAspect < CompactAspectThreshold;

        float availW = Mathf.Max(320f, canvasW - safe.Left - safe.Right);
        float availH = Mathf.Max(320f, canvasH - safe.Bottom - safe.Top);

        libraryX = safe.Left + DeckManager.BuildbeckLibraryGridViewportLeftInsetPx +
                   DeckManager.BuildbeckLibraryGridViewportLeftBreathingPx;
        libraryY = safe.Bottom + DeckManager.BuildbeckLibraryGridViewportBottomInsetPx +
                   DeckManager.BuildbeckDeckBuildingContainersVerticalLiftPx;

        deckRightInset = safe.Right + DeckManager.BuildbeckDeckViewportRightInsetPx;
        deckBottom = safe.Bottom + DeckManager.BuildbeckDeckViewportBottomPx +
                     DeckManager.BuildbeckDeckBuildingContainersVerticalLiftPx +
                     deckVerticalTunePx;

        if (compact)
        {
            deckRightInset = safe.Right + DeckManager.BuildbeckDeckViewportRightInsetPx;

            float maxPairW = availW - libraryX - LibraryDeckHorizontalGapPx - deckRightInset;
            float pairW = libraryW + deckW;
            if (pairW > maxPairW && pairW > 1f)
            {
                float scale = Mathf.Clamp(maxPairW / pairW, CompactWidthScaleMin, 1f);
                libraryW = Mathf.Round(libraryW * scale);
                deckW = Mathf.Round(deckW * scale);
            }

            float maxContainerH = availH - libraryY - TopUiReservePx;
            float tallest = Mathf.Max(libraryH, deckH);
            if (tallest > maxContainerH && tallest > 1f)
            {
                float scale = Mathf.Clamp(maxContainerH / tallest, CompactHeightScaleMin, 1f);
                libraryH = Mathf.Round(libraryH * scale);
                deckH = Mathf.Round(deckH * scale);
            }
        }
        else
        {
            libraryX = safe.Left + DeckManager.BuildbeckLibraryGridViewportLeftInsetPx +
                       DeckManager.BuildbeckLibraryGridViewportLeftBreathingPx +
                       DeckManager.BuildbeckLibraryGridViewportCenterPointOffsetXPx +
                       DeckManager.BuildbeckLibraryGridViewportExtraOffsetXPx;
            deckRightInset = safe.Right + DeckManager.BuildbeckDeckViewportRightInsetPx +
                             DeckManager.BuildbeckDeckViewportLeftShiftPx;
        }

        return new ResolvedLayout(libraryX, libraryY, libraryW, libraryH, deckRightInset, deckBottom, deckW, deckH);
    }
}

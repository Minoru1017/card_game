using UnityEngine;
using UnityEngine.UI;

/// <summary>CanvasScaler 1920×1080；Editor／手機共用「完整放入可視區」match，手機另略縮 UI。</summary>
public static class MobileUiLayoutPolicy
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    /// <summary>1920×1080 中心錨點下，可視區半高（±540）。</summary>
    public const float ReferenceHalfExtentY = 540f;

    /// <summary>Story progress 佈告底欄底板底部內距（參考解析度 px）。</summary>
    public const float HarborBulletinFooterBottomInsetY = 10f;

    /// <summary>手機參考解析度放大 → 等效 UI 縮小（約 1/1.12）。</summary>
    public const float MobileReferenceResolutionScale = 1.12f;

    /// <summary>Editor Play：Game 視窗頂部工具列會吃掉少許高度，略縮 UI 避免上緣裁切。</summary>
    public const float EditorPlayReferenceResolutionScale = 1.05f;

    /// <summary>Editor match 計算時自高度扣除的像素（補償 Game 視窗非客戶區）。</summary>
    public const float EditorLayoutTopInsetPx = 16f;

    public static bool UseMobileLayout =>
#if UNITY_ANDROID || UNITY_IOS
        true;
#else
        false;
#endif

    public static Vector2 GetReferenceResolutionForScaler()
    {
        float scale = 1f;
        if (UseMobileLayout)
            scale = MobileReferenceResolutionScale;
#if UNITY_EDITOR
        else if (Application.isPlaying)
            scale = EditorPlayReferenceResolutionScale;
#endif

        if (scale <= 1.001f)
            return ReferenceResolution;

        return new Vector2(ReferenceResolution.x * scale, ReferenceResolution.y * scale);
    }

    /// <summary>match 計算用像素區（手機取 Safe Area；Editor 為 Game 視窗大小）。</summary>
    public static Vector2 GetLayoutPixelSize()
    {
        float w = Mathf.Max(1, Screen.width);
        float h = Mathf.Max(1, Screen.height);

        if (!UseMobileLayout)
        {
#if UNITY_EDITOR
            if (Application.isPlaying && EditorLayoutTopInsetPx > 0f)
                h = Mathf.Max(1f, h - EditorLayoutTopInsetPx);
#endif
            return new Vector2(w, h);
        }

        Rect sa = Screen.safeArea;
        if (sa.width > 1f && sa.height > 1f)
            return new Vector2(sa.width, sa.height);

        return new Vector2(w, h);
    }

    /// <summary>
    /// 選較小縮放軸（Match Width 或 Height），使 1920×1080 設計完整落在可視區內。
    /// Editor 與手機同一套；不再使用固定的 Match 0.5。
    /// </summary>
    public static float ComputeMatchWidthOrHeight()
    {
        Vector2 layout = GetLayoutPixelSize();
        Vector2 reference = GetReferenceResolutionForScaler();
        float scaleW = layout.x / reference.x;
        float scaleH = layout.y / reference.y;
        return scaleW <= scaleH ? 0f : 1f;
    }

    public static void ApplyCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = GetReferenceResolutionForScaler();
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = ComputeMatchWidthOrHeight();
    }

    public static float PanelWidthInReferenceUnits(float screenRatio = 0.8f, float maxWidth = 1160f) =>
        Mathf.Min(ReferenceResolution.x * screenRatio, maxWidth);

    public static float PanelHeightInReferenceUnits(float screenRatio = 0.82f, float maxHeight = 780f) =>
        Mathf.Min(ReferenceResolution.y * screenRatio, maxHeight);

    /// <summary>Safe Area 轉成 Canvas 本地單位（左／右／下／上）。</summary>
    public readonly struct CanvasSafeInsets
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Bottom;
        public readonly float Top;

        public CanvasSafeInsets(float left, float right, float bottom, float top)
        {
            Left = left;
            Right = right;
            Bottom = bottom;
            Top = top;
        }
    }

    /// <summary>依目前螢幕 Safe Area 計算 Canvas 四邊內距（Editor 僅補頂部）。</summary>
    public static CanvasSafeInsets GetCanvasSafeInsets(Canvas canvas)
    {
        float scale = 1f;
        if (canvas != null && canvas.scaleFactor > 0.01f)
            scale = canvas.scaleFactor;

        float left = 0f;
        float right = 0f;
        float bottom = 0f;
        float top = 0f;

        if (UseMobileLayout)
        {
            Rect safe = Screen.safeArea;
            if (safe.width > 1f && safe.height > 1f)
            {
                left = safe.xMin / scale;
                bottom = safe.yMin / scale;
                right = (Screen.width - safe.xMax) / scale;
                top = (Screen.height - safe.yMax) / scale;
            }
        }

#if UNITY_EDITOR
        if (!UseMobileLayout && Application.isPlaying && EditorLayoutTopInsetPx > 0f)
            top = EditorLayoutTopInsetPx / scale;
#endif

        return new CanvasSafeInsets(left, right, bottom, top);
    }
}

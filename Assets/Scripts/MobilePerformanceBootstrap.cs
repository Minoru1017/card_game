using UnityEngine;

/// <summary>
/// 手機幀率：關閉垂直同步與品質層級帶來的 30fps 鎖定，改以 targetFrameRate 控制。
/// Unity 在 Android 預設 Medium 品質含 vSync=1；GPU 無法穩定 60 時常鎖在 30。
/// </summary>
public static class MobilePerformanceBootstrap
{
    public const int MobileTargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyBeforeFirstScene() => ApplyMobileFramePacing();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyAfterSceneLoad() => ApplyMobileFramePacing();

    /// <summary>在品質切換後呼叫，避免 SetQualityLevel 把 vSync 設回 1。</summary>
    public static void ApplyMobileFramePacing()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = MobileTargetFrameRate;
#endif
    }
}

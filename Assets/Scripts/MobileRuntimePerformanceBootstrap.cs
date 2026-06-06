using UnityEngine;

/// <summary>
/// Mobile startup performance defaults.
/// Keeps behavior deterministic across scenes by applying once before first scene load.
/// </summary>
public static class MobileRuntimePerformanceBootstrap
{
    private const int TargetMobileFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        // 因素1：所有平台（含 Editor / 桌機）一律解除 vSync 並套用玩家儲存的目標 FPS（預設 60），
        // 避免 mobile 預設掉到 30、或桌機被 vSync 卡在螢幕更新率而非穩定 60。
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetMobileFps;
        BattleCardTuningUserSettings.ApplySavedTargetFps();

        if (!Application.isMobilePlatform)
            return;

        // Extra low-risk global switches that usually help mobile GPU/CPU time.
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;
        QualitySettings.particleRaycastBudget = 32;
        QualitySettings.lodBias = Mathf.Min(QualitySettings.lodBias, 0.8f);
        QualitySettings.globalTextureMipmapLimit = Mathf.Max(QualitySettings.globalTextureMipmapLimit, 1);
        QualitySettings.shadows = ShadowQuality.Disable;
    }
}

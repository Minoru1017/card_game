using UnityEngine;

/// <summary>
/// Keeps mobile frame rate closer to 60 by adjusting render scale at runtime.
/// </summary>
public sealed class MobileAdaptiveResolutionController : MonoBehaviour
{
    private const float MinScale = 0.72f;
    private const float MaxScale = 1f;
    private const float DownStep = 0.08f;
    private const float UpStep = 0.04f;
    private const float DownFpsThreshold = 52f;
    private const float UpFpsThreshold = 58f;
    private const float DownCooldownSeconds = 2f;
    private const float UpCooldownSeconds = 3f;
    private const float FpsEmaLerp = 0.08f;

    private static bool spawned;
    private float currentScale = MaxScale;
    private float emaFps = 60f;
    private float nextAdjustAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (spawned || !Application.isMobilePlatform)
            return;

        var go = new GameObject(nameof(MobileAdaptiveResolutionController));
        DontDestroyOnLoad(go);
        go.AddComponent<MobileAdaptiveResolutionController>();
        spawned = true;
    }

    private void Awake()
    {
        ApplyScale(MaxScale);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f)
            return;

        float fps = 1f / dt;
        emaFps = Mathf.Lerp(emaFps, fps, FpsEmaLerp);

        if (Time.unscaledTime < nextAdjustAt)
            return;

        if (emaFps < DownFpsThreshold && currentScale > MinScale)
        {
            ApplyScale(Mathf.Max(MinScale, currentScale - DownStep));
            nextAdjustAt = Time.unscaledTime + DownCooldownSeconds;
            return;
        }

        if (emaFps > UpFpsThreshold && currentScale < MaxScale)
        {
            ApplyScale(Mathf.Min(MaxScale, currentScale + UpStep));
            nextAdjustAt = Time.unscaledTime + UpCooldownSeconds;
        }
    }

    private void ApplyScale(float scale)
    {
        currentScale = Mathf.Clamp(scale, MinScale, MaxScale);
        ScalableBufferManager.ResizeBuffers(currentScale, currentScale);
    }
}

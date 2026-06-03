using UnityEngine;

/// <summary>手機啟動時強制橫向（Landscape Left，與 Editor／1920×1080 版面一致）。</summary>
public static class MobileScreenOrientationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyLandscapeOnMobile()
    {
#if UNITY_ANDROID || UNITY_IOS
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
    }
}

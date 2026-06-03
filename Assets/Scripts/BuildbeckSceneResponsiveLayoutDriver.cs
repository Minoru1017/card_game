using UnityEngine;

/// <summary>Buildbeck：螢幕／Safe Area 變更時重算 Library / Deck 容器排版。</summary>
public sealed class BuildbeckSceneResponsiveLayoutDriver : MonoBehaviour
{
    private static BuildbeckSceneResponsiveLayoutDriver instance;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject(nameof(BuildbeckSceneResponsiveLayoutDriver));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BuildbeckSceneResponsiveLayoutDriver>();
    }

    // Layout refresh is handled by MobileUiCanvasRefreshDriver (single LateUpdate for all scenes).
    private void LateUpdate() { }
}

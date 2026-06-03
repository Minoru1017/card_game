using UnityEngine;

/// <summary>hall 場景：螢幕／Safe Area 變更時重算頂部商店與金幣區排版。</summary>
public sealed class HallSceneResponsiveLayoutDriver : MonoBehaviour
{
    private static HallSceneResponsiveLayoutDriver instance;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject(nameof(HallSceneResponsiveLayoutDriver));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HallSceneResponsiveLayoutDriver>();
    }

    // Layout refresh is handled by MobileUiCanvasRefreshDriver (single LateUpdate for all scenes).
    private void LateUpdate() { }
}

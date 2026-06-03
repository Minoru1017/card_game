using UnityEngine;

/// <summary>Story progress：Safe Area 變更時重算右側關卡側欄排版。</summary>
public sealed class StoryProgressSidebarResponsiveLayoutDriver : MonoBehaviour
{
    private static StoryProgressSidebarResponsiveLayoutDriver instance;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject(nameof(StoryProgressSidebarResponsiveLayoutDriver));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<StoryProgressSidebarResponsiveLayoutDriver>();
    }

    // Layout refresh is handled by MobileUiCanvasRefreshDriver (single LateUpdate for all scenes).
    private void LateUpdate() { }
}

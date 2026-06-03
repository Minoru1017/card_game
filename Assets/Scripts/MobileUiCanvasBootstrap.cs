using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>場景載入後套用 CanvasScaler；Game 視窗／螢幕尺寸變更時重新計算（Editor Play 與手機）。</summary>
public static class MobileUiCanvasBootstrap
{
    private static bool subscribed;
    private static MobileUiCanvasRefreshDriver refreshDriver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        EnsureRefreshDriver();
        ReapplyActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReapplyScene(scene);
    }

    public static void ReapplyActiveScene()
    {
        ReapplyScene(SceneManager.GetActiveScene());
    }

    private static void ReapplyScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < scalers.Length; i++)
        {
            CanvasScaler scaler = scalers[i];
            if (scaler == null || scaler.gameObject.scene != scene)
                continue;

            MobileUiLayoutPolicy.ApplyCanvasScaler(scaler);
        }

        if (scene.name == StoryProgressSession.StoryProgressSceneName)
        {
            StoryProgressFooterLayer.ApplyIfNeeded(force: true);
            StoryProgressSidebarResponsiveLayout.ApplyNow(scene);
        }

        if (scene.name == "hall")
            HallSceneResponsiveLayout.ApplyNow(scene);

        if (scene.name == "Buildbeck")
            BuildbeckSceneResponsiveLayout.ApplyNow(scene);

        if (scene.name == "CardStore")
            CardStoreSceneUi.ApplyNow(scene);
    }

    private static void EnsureRefreshDriver()
    {
        if (refreshDriver != null)
            return;

        GameObject go = new GameObject(nameof(MobileUiCanvasRefreshDriver));
        Object.DontDestroyOnLoad(go);
        refreshDriver = go.AddComponent<MobileUiCanvasRefreshDriver>();
    }

    private sealed class MobileUiCanvasRefreshDriver : MonoBehaviour
    {
        private Vector2Int lastScreenSize;
        private Rect lastSafeArea;

        private void Update()
        {
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
            if (size == lastScreenSize && safe == lastSafeArea)
                return;

            lastScreenSize = size;
            lastSafeArea = safe;
            ReapplyActiveScene();
        }

        private void LateUpdate() => ApplyResponsiveLayoutsIfNeeded();

        private static void ApplyResponsiveLayoutsIfNeeded()
        {
            StoryProgressFooterLayer.ApplyIfNeeded();
            StoryProgressSidebarResponsiveLayout.ApplyIfNeeded();
            HallSceneResponsiveLayout.ApplyIfNeeded();
            BuildbeckSceneResponsiveLayout.ApplyIfNeeded();

            if (SceneManager.GetActiveScene().name == "CardStore")
                CardStoreSceneUi.ApplyNow();
        }
    }
}

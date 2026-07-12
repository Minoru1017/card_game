using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>M-1-3 全屏 overlay 共用：掛在作用中場景最高 Canvas，避免被 Story progress UI 或 GlobalNav 擋住。</summary>
public static class M13StoryOverlayHost
{
    public const int OverlaySortOrder = 6500;

    /// <summary>建立全屏 dim overlay；回傳值為應 Destroy 的根節點。</summary>
    public static GameObject CreateDimOverlay(string name)
    {
        Canvas parent = ResolveParentCanvas();
        if (parent != null)
            return BirdDuelOverlayUiBuild.CreateDimOverlay(parent.transform, OverlaySortOrder, name);

        return CreateStandaloneDimOverlay(name);
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        new GameObject(
            "EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    private static Canvas ResolveParentCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isActiveAndEnabled)
                continue;
            if (!canvas.gameObject.scene.IsValid())
                continue;
            if (activeScene.IsValid() && canvas.gameObject.scene != activeScene)
                continue;
            if (string.Equals(canvas.gameObject.name, "GlobalNavCanvas", System.StringComparison.Ordinal))
                continue;

            if (canvas.sortingOrder < bestOrder)
                continue;

            best = canvas;
            bestOrder = canvas.sortingOrder;
        }

        return best;
    }

    private static GameObject CreateStandaloneDimOverlay(string name)
    {
        GameObject host = new GameObject(name, typeof(RectTransform));
        Canvas canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;
        CanvasScaler scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        host.AddComponent<GraphicRaycaster>();

        return BirdDuelOverlayUiBuild.CreateDimOverlay(host.transform, OverlaySortOrder, "Dim");
    }
}

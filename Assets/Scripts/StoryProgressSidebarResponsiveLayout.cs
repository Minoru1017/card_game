using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Story progress 右側「View Level Flow」側欄（港灣訓練場關卡詳情）：右緣貼 Safe Area，上下避開頂部與佈告底欄。</summary>
public static class StoryProgressSidebarResponsiveLayout
{
    private const float ReferenceSidebarWidth = 1035.7983f;
    private const float ReferenceSidebarMinWidth = 340f;

    private static Rect lastSafeArea;
    private static Vector2Int lastScreenSize;
    private static bool lastFooterVisible;

    public static void ApplyIfNeeded(bool force = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded ||
            scene.name != StoryProgressSession.StoryProgressSceneName)
            return;

        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        bool footerVisible = IsFooterPanelVisible();
        if (!force && safe == lastSafeArea && size == lastScreenSize && footerVisible == lastFooterVisible)
            return;

        lastSafeArea = safe;
        lastScreenSize = size;
        lastFooterVisible = footerVisible;
        ApplyNow(scene, footerVisible);
    }

    public static void ApplyNow(Scene scene, bool? footerVisibleOverride = null)
    {
        if (!scene.IsValid() || !scene.isLoaded ||
            scene.name != StoryProgressSession.StoryProgressSceneName)
            return;

        GameObject sidebarGo = GameObject.Find(StoryProgressLevelCopy.ViewLevelFlowPanelName);
        if (sidebarGo == null || sidebarGo.scene != scene)
            return;

        RectTransform sidebarRt = sidebarGo.GetComponent<RectTransform>();
        if (sidebarRt == null)
            return;

        Canvas canvas = sidebarGo.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Canvas.ForceUpdateCanvases();
        MobileUiLayoutPolicy.CanvasSafeInsets safe = MobileUiLayoutPolicy.GetCanvasSafeInsets(canvas);
        RectTransform canvasRt = canvas.transform as RectTransform;
        float canvasWidth = canvasRt != null ? canvasRt.rect.width : MobileUiLayoutPolicy.ReferenceResolution.x;

        bool footerVisible = footerVisibleOverride ?? IsFooterPanelVisible();
        float bottomInset = ResolveBottomInset(safe, footerVisible);

        float maxWidth = Mathf.Max(ReferenceSidebarMinWidth, canvasWidth - safe.Left - safe.Right);
        float width = Mathf.Min(ReferenceSidebarWidth, maxWidth);

        sidebarRt.anchorMin = new Vector2(1f, 0f);
        sidebarRt.anchorMax = new Vector2(1f, 1f);
        sidebarRt.pivot = new Vector2(1f, 0.5f);
        sidebarRt.localScale = Vector3.one;
        sidebarRt.anchoredPosition = Vector2.zero;
        sidebarRt.offsetMin = new Vector2(-width, bottomInset);
        sidebarRt.offsetMax = new Vector2(-safe.Right, -safe.Top);
    }

    private static float ResolveBottomInset(MobileUiLayoutPolicy.CanvasSafeInsets safe, bool footerVisible)
    {
        if (!footerVisible)
            return safe.Bottom;

        GameObject footerGo = GameObject.Find(StoryProgressFooterLayer.FooterPanelObjectName);
        if (footerGo == null || !footerGo.activeInHierarchy)
            return safe.Bottom + MobileUiLayoutPolicy.HarborBulletinFooterBottomInsetY;

        RectTransform footerRt = footerGo.GetComponent<RectTransform>();
        float footerHeight = footerRt != null ? footerRt.rect.height : 165f;
        if (footerHeight < 1f)
            footerHeight = 165f;

        return safe.Bottom + MobileUiLayoutPolicy.HarborBulletinFooterBottomInsetY + footerHeight;
    }

    private static bool IsFooterPanelVisible()
    {
        GameObject footerGo = GameObject.Find(StoryProgressFooterLayer.FooterPanelObjectName);
        return footerGo != null && footerGo.activeInHierarchy;
    }
}

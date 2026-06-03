using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Story progress 底欄「港灣佈告」底板：依 Safe Area 貼齊畫面下緣，並維持在 Canvas 最上層（僅返回鈕可蓋過）。
/// </summary>
public static class StoryProgressFooterLayer
{
    public const string FooterPanelObjectName = "Panel";

    private static float cachedFooterHeight = -1f;
    private static Rect lastSafeArea;
    private static Vector2Int lastScreenSize;
    private static string lastSceneName;
    private static GameObject cachedPanelGo;
    private static Transform cachedCanvasRoot;
    private static Transform cachedBackButton;

    public static void ApplyIfNeeded(bool force = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded ||
            scene.name != StoryProgressSession.StoryProgressSceneName)
        {
            InvalidateCache();
            return;
        }

        if (lastSceneName != scene.name)
        {
            lastSceneName = scene.name;
            InvalidatePanelCache();
        }

        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == lastSafeArea && size == lastScreenSize && cachedPanelGo != null)
            return;

        lastSafeArea = safe;
        lastScreenSize = size;
        EnsureHarborBulletinOnTop(forceLayout: true);
    }

    public static void EnsureHarborBulletinOnTop(Transform backButton = null, bool forceLayout = false)
    {
        GameObject panelGo = ResolveFooterPanel();
        if (panelGo == null || !panelGo.activeInHierarchy)
            return;

        Transform canvasRoot = panelGo.transform.parent;
        if (canvasRoot == null)
            return;

        if (forceLayout)
            ApplyFooterPanelResponsiveLayout(panelGo.GetComponent<RectTransform>());

        panelGo.transform.SetAsLastSibling();

        Transform back = backButton;
        if (back == null)
            back = ResolveBackButtonOnCanvas(canvasRoot);

        if (back != null && back.parent == canvasRoot && back.gameObject.activeInHierarchy)
            back.SetAsLastSibling();
    }

    private static GameObject ResolveFooterPanel()
    {
        if (cachedPanelGo != null && cachedPanelGo.scene.IsValid() &&
            cachedPanelGo.name == FooterPanelObjectName &&
            IsStoryProgressScene(cachedPanelGo))
            return cachedPanelGo;

        cachedPanelGo = GameObject.Find(FooterPanelObjectName);
        cachedCanvasRoot = cachedPanelGo != null ? cachedPanelGo.transform.parent : null;
        cachedBackButton = null;
        return cachedPanelGo;
    }

    private static Transform ResolveBackButtonOnCanvas(Transform canvasRoot)
    {
        if (cachedBackButton != null && cachedBackButton.parent == canvasRoot)
            return cachedBackButton;

        cachedBackButton = FindBackButtonOnCanvas(canvasRoot);
        return cachedBackButton;
    }

    /// <summary>底欄貼 Safe Area 下緣 + 設計邊距；寬度填滿 Safe Area 水平範圍。</summary>
    private static void ApplyFooterPanelResponsiveLayout(RectTransform panelRt)
    {
        if (panelRt == null)
            return;

        Canvas canvas = panelRt.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        float height = ResolveFooterPanelHeight(panelRt);
        MobileUiLayoutPolicy.CanvasSafeInsets safe = MobileUiLayoutPolicy.GetCanvasSafeInsets(canvas);
        float bottomMargin = MobileUiLayoutPolicy.HarborBulletinFooterBottomInsetY;

        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.localScale = Vector3.one;
        panelRt.offsetMin = new Vector2(safe.Left, safe.Bottom + bottomMargin);
        panelRt.offsetMax = new Vector2(-safe.Right, height);
    }

    private static float ResolveFooterPanelHeight(RectTransform panelRt)
    {
        if (cachedFooterHeight > 1f)
            return cachedFooterHeight;

        float h = panelRt.rect.height;
        if (h < 1f)
            h = panelRt.sizeDelta.y;
        if (h < 1f)
            h = 165f;

        cachedFooterHeight = h;
        return cachedFooterHeight;
    }

    private static Transform FindBackButtonOnCanvas(Transform canvasRoot)
    {
        for (int i = 0; i < canvasRoot.childCount; i++)
        {
            Transform child = canvasRoot.GetChild(i);
            if (child == null) continue;
            if (child.name == ReturnButtonLayout.ObjectName)
                return child;
        }

        return null;
    }

    private static void InvalidatePanelCache()
    {
        cachedPanelGo = null;
        cachedCanvasRoot = null;
        cachedBackButton = null;
    }

    private static void InvalidateCache()
    {
        InvalidatePanelCache();
        lastSceneName = null;
        lastSafeArea = default;
        lastScreenSize = default;
    }

    private static bool IsStoryProgressScene(GameObject go)
    {
        Scene scene = go.scene;
        return scene.IsValid() && scene.isLoaded &&
               scene.name == StoryProgressSession.StoryProgressSceneName;
    }
}

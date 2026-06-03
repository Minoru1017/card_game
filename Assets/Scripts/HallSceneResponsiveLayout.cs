using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>hall 頂部「商店」與「資源顯示區（金幣）」：依 Safe Area 貼齊上緣並保留設計間距。</summary>
public static class HallSceneResponsiveLayout
{
    private const string SceneName = "hall";
    private const string ShopButtonObjectName = "商店";
    private const string ResourceAreaObjectName = "資源顯示區";

    /// <summary>1920×1080 設計稿：商店右緣與資源區左緣間距。</summary>
    private const float ReferenceShopToResourceGapX = 49f;

    private static Vector2 cachedShopSize = Vector2.zero;
    private static Vector2 cachedResourceSize = Vector2.zero;
    private static Rect lastSafeArea;
    private static Vector2Int lastScreenSize;

    public static void ApplyIfNeeded(bool force = false)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
            return;

        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == lastSafeArea && size == lastScreenSize)
            return;

        lastSafeArea = safe;
        lastScreenSize = size;
        ApplyNow(scene);
    }

    public static void ApplyNow(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
            return;

        GameObject resourceGo = GameObject.Find(ResourceAreaObjectName);
        GameObject shopGo = GameObject.Find(ShopButtonObjectName);
        if (resourceGo == null && shopGo == null)
            return;

        Canvas canvas = ResolveCanvas(resourceGo, shopGo);
        if (canvas == null)
            return;

        Canvas.ForceUpdateCanvases();
        MobileUiLayoutPolicy.CanvasSafeInsets safe = MobileUiLayoutPolicy.GetCanvasSafeInsets(canvas);
        RectTransform canvasRt = canvas.transform as RectTransform;
        float canvasWidth = canvasRt != null ? canvasRt.rect.width : MobileUiLayoutPolicy.ReferenceResolution.x;

        if (resourceGo != null && resourceGo.scene == scene)
        {
            RectTransform resourceRt = resourceGo.GetComponent<RectTransform>();
            ApplyTopRightElement(resourceRt, safe, canvasWidth, ref cachedResourceSize);
            if (shopGo != null && shopGo.scene == scene)
                ApplyShopButtonLayout(shopGo.GetComponent<RectTransform>(), resourceRt, safe);
        }
        else if (shopGo != null && shopGo.scene == scene)
        {
            ApplyShopButtonLayout(shopGo.GetComponent<RectTransform>(), null, safe);
        }
    }

    private static void ApplyShopButtonLayout(
        RectTransform shopRt,
        RectTransform resourceRt,
        MobileUiLayoutPolicy.CanvasSafeInsets safe)
    {
        if (shopRt == null)
            return;

        Vector2 shopSize = ResolveSize(shopRt, ref cachedShopSize, new Vector2(175.1364f, 175.1364f));
        shopRt.anchorMin = new Vector2(1f, 1f);
        shopRt.anchorMax = new Vector2(1f, 1f);
        shopRt.pivot = new Vector2(1f, 1f);
        shopRt.localScale = Vector3.one;
        shopRt.sizeDelta = shopSize;

        float shopRight = safe.Right;
        if (resourceRt != null)
        {
            float resourceWidth = resourceRt.rect.width;
            shopRight = safe.Right + resourceWidth + ReferenceShopToResourceGapX;
        }

        shopRt.anchoredPosition = new Vector2(-shopRight, -safe.Top);
    }

    private static void ApplyTopRightElement(
        RectTransform rt,
        MobileUiLayoutPolicy.CanvasSafeInsets safe,
        float canvasWidth,
        ref Vector2 cachedSize)
    {
        if (rt == null)
            return;

        Vector2 size = ResolveSize(rt, ref cachedSize, new Vector2(1038.699f, 175.1364f));
        float maxWidth = Mathf.Max(120f, canvasWidth - safe.Left - safe.Right);
        size.x = Mathf.Min(size.x, maxWidth);
        cachedSize = size;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.localScale = Vector3.one;
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(-safe.Right, -safe.Top);
    }

    private static Vector2 ResolveSize(RectTransform rt, ref Vector2 cache, Vector2 fallback)
    {
        if (cache.x > 1f && cache.y > 1f)
            return cache;

        Vector2 size = rt.rect.size;
        if (size.x < 1f || size.y < 1f)
            size = rt.sizeDelta;
        if (size.x < 1f || size.y < 1f)
            size = fallback;

        cache = size;
        return cache;
    }

    private static Canvas ResolveCanvas(GameObject resourceGo, GameObject shopGo)
    {
        if (resourceGo != null)
        {
            Canvas fromResource = resourceGo.GetComponentInParent<Canvas>();
            if (fromResource != null)
                return fromResource;
        }

        if (shopGo != null)
        {
            Canvas fromShop = shopGo.GetComponentInParent<Canvas>();
            if (fromShop != null)
                return fromShop;
        }

        return Object.FindFirstObjectByType<Canvas>();
    }
}

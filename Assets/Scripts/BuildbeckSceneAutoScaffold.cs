using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Non-destructive Buildbeck UI scaffold.
/// Prefers prefabs under Resources/<see cref="BuildbeckUiResourcePaths"/> (bake via Tools/Buildbeck/Bake UI Prefabs).
/// Falls back to <see cref="BuildbeckUiHierarchyBuilder"/> when prefabs are missing.
/// </summary>
public static class BuildbeckSceneAutoScaffold
{
    private const string SceneName = "Buildbeck";
    private const string RootName = "BuildbeckAutoScaffoldRoot";
    private static EventSystem cachedEventSystem;
    private static DeckManager cachedDeckManager;
    private static SceneLoader cachedSceneLoader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureScaffold()
    {
        EnsureScaffoldNow();
    }

    public static void EnsureScaffoldNow()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas(scene);
        if (canvas == null) return;

        GameObject root = SceneSearchUtil.FindSceneObject(scene, RootName);
        if (root == null)
        {
            root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else if (root.transform.parent != canvas.transform)
        {
            root.transform.SetParent(canvas.transform, false);
        }

        Transform rootT = root.transform;

        EnsurePanel("Library Grid", rootT, new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Color(1f, 1f, 1f, 0.03f));
        EnsurePanel("Deck Grid", rootT, new Vector2(0.55f, 0.18f), new Vector2(1f, 1f), new Color(1f, 1f, 1f, 0.03f));
        EnsureBackButton(rootT);
        EnsureSaveDeckButton(rootT);
        DeckManager dmSave = cachedDeckManager;
        if (dmSave == null) dmSave = Object.FindFirstObjectByType<DeckManager>();
        cachedDeckManager = dmSave;
        if (dmSave != null)
        {
            BuildbeckLayoutAutoBinder.TryWireSaveDeckButton(dmSave);
            BuildbeckLayoutAutoBinder.TryWireDisbandDeckButton(dmSave);
            BuildbeckLayoutAutoBinder.TryWireEditDeckNameButton(dmSave);
            BuildbeckLayoutAutoBinder.TryBindCurrentDeckNameDisplay(dmSave);
        }

        BuildbeckLayoutAutoBinder.TryWireReadyBattleButton();
    }

    private static void EnsureEventSystem()
    {
        if (cachedEventSystem == null)
            cachedEventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (cachedEventSystem != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        cachedEventSystem = Object.FindFirstObjectByType<EventSystem>();
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            Canvas c = roots[i].GetComponentInChildren<Canvas>(true);
            if (c == null) continue;
            c.gameObject.SetActive(true);
            c.enabled = true;
            return c;
        }

        GameObject canvasObj = new GameObject("BuildbeckAutoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void EnsurePanel(string panelName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color tint)
    {
        GameObject existing = SceneSearchUtil.FindSceneObject(SceneManager.GetActiveScene(), panelName);
        if (existing != null)
        {
            EnsureHierarchyActive(existing.transform);
            RectTransform existingRt = existing.GetComponent<RectTransform>();
            if (existingRt != null)
            {
                existingRt.anchorMin = new Vector2(0f, 1f);
                existingRt.anchorMax = new Vector2(1f, 1f);
                existingRt.pivot = new Vector2(0.5f, 1f);
            }
            return;
        }

        if (TryInstantiateScrollGridFromPrefab(panelName, parent, anchorMin, anchorMax, tint))
            return;

        int columns = panelName.Contains("Library") ? 4 : 2;
        BuildbeckUiHierarchyBuilder.CreateScrollableGridPanel(parent, panelName, anchorMin, anchorMax, tint, columns);
    }

    private static bool TryInstantiateScrollGridFromPrefab(
        string panelName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color tint)
    {
        GameObject prefab = Resources.Load<GameObject>(BuildbeckUiResourcePaths.ScrollGrid);
        if (prefab == null) return false;

        GameObject viewport = Object.Instantiate(prefab, parent, false);
        ScrollRect sr = viewport.GetComponent<ScrollRect>();
        if (sr == null || sr.content == null)
        {
            Object.Destroy(viewport);
            return false;
        }

        viewport.name = panelName + " Viewport";
        sr.content.name = panelName;

        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = anchorMin;
        vpRt.anchorMax = anchorMax;
        vpRt.offsetMin = new Vector2(16f, 16f);
        vpRt.offsetMax = new Vector2(-16f, -16f);

        Image vpImage = viewport.GetComponent<Image>();
        if (vpImage != null) vpImage.color = tint;

        GridLayoutGroup grid = sr.content.GetComponent<GridLayoutGroup>();
        if (grid != null)
            grid.constraintCount = panelName.Contains("Library") ? 4 : 2;

        return true;
    }

    private static void EnsureBackButton(Transform parent)
    {
        if (SceneSearchUtil.FindSceneObject(SceneManager.GetActiveScene(), "BackButton") != null) return;

        GameObject prefab = Resources.Load<GameObject>(BuildbeckUiResourcePaths.BackButton);
        GameObject btnObj;
        if (prefab != null)
            btnObj = Object.Instantiate(prefab, parent, false);
        else
            btnObj = BuildbeckUiHierarchyBuilder.CreateTextButton(
                parent,
                "BackButton",
                "返回",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f),
                new Vector2(160f, 70f));

        Button btn = btnObj.GetComponent<Button>();
        SceneLoader loader = cachedSceneLoader;
        if (loader == null) loader = Object.FindFirstObjectByType<SceneLoader>();
        cachedSceneLoader = loader;
        if (btn != null && loader != null)
        {
            btn.onClick.AddListener(loader.EnterPersistent);
        }
    }

    private static void EnsureSaveDeckButton(Transform parent)
    {
        DeckManager dm = cachedDeckManager;
        if (dm == null) dm = Object.FindFirstObjectByType<DeckManager>();
        if (dm != null && dm.saveDeckButton != null)
            return;

        GameObject existing = FindSaveDeckButtonObject();
        if (existing != null)
        {
            BuildbeckUiHierarchyBuilder.ApplySaveDeckButtonLayout(existing.GetComponent<RectTransform>());
            BuildbeckUiHierarchyBuilder.ConfigureSaveDeckButtonVisual(existing);
            return;
        }

        GameObject btn = BuildbeckUiHierarchyBuilder.CreateTextButton(
            parent,
            "SaveDeckButton",
            "儲存",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-110f, -20f),
            new Vector2(180f, 70f));

        BuildbeckUiHierarchyBuilder.ApplySaveDeckButtonLayout(btn.GetComponent<RectTransform>());
        BuildbeckUiHierarchyBuilder.ConfigureSaveDeckButtonVisual(btn);
    }

    private static GameObject FindSaveDeckButtonObject()
    {
        Scene scene = SceneManager.GetActiveScene();
        string[] names =
        {
            "SaveDeckButton",
            "SaveDeck",
            "Save deck button",
            "SaveDeckButtonArt",
            "牌組保存",
            "保存牌組",
        };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject go = SceneSearchUtil.FindSceneObject(scene, names[i]);
            if (go != null && go.GetComponent<Button>() != null) return go;
        }

        return null;
    }

    private static void EnsureHierarchyActive(Transform leaf)
    {
        Transform t = leaf;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }
    }
}

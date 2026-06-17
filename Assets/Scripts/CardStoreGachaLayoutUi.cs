using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// CardStore 雙池版面：左側分頁（金幣池／寶石池），主區＋底部抽選列。
/// 內容區對齊 ResponsiveBaseboard 底板內緣（舊版藍底左右界），4:3 時自然等同 1440 保險寬。
/// </summary>
public static class CardStoreGachaLayoutUi
{
    private const string ContentPlateName = "CardStoreContentPlate";
    private const string PackVideoOverlayName = "CardStorePackVideoOverlay";
    /// <summary>高於 GlobalNavCanvas(6000)，低於 SceneToast。</summary>
    private const int PackVideoOverlaySortOrder = 6500;
    private const float ReferenceContentWidth = 1920f;
    private const float SidebarWidth = 300f;
    private const float SidebarTabMargin = 12f;
    private const float SidebarTabHeight = 140f;
    private const float BottomBarHeight = 228f;

    private static readonly Color ColorMainBg = new Color(0.10f, 0.12f, 0.16f, 0.94f);
    private static readonly Color ColorSidebar = new Color(0.40f, 0.46f, 0.34f, 0.98f);
    private static readonly Color ColorPanel = new Color(0.16f, 0.19f, 0.25f, 0.96f);
    private static readonly Color ColorTabActive = new Color(0.44f, 0.28f, 0.25f, 1f);
    private static readonly Color ColorTabIdle = new Color(0.90f, 0.93f, 0.86f, 1f);
    private static readonly Color ColorTabActiveText = Color.white;
    private static readonly Color ColorTabIdleText = new Color(0.23f, 0.18f, 0.14f, 1f);
    private static readonly Color ColorBodyText = new Color(0.90f, 0.94f, 0.98f, 1f);
    private static readonly Color ColorMutedText = new Color(0.72f, 0.78f, 0.86f, 1f);
    private static readonly Color ColorAction = new Color(0.27f, 0.55f, 0.86f, 1f);
    private static readonly Color ColorActionAlt = new Color(0.30f, 0.78f, 0.45f, 1f);

    private enum StoreTab
    {
        Coin,
        Gem
    }

    private static GameObject layoutBackdrop;
    private static GameObject layoutChrome;
    private static GameObject packVideoOverlay;
    private static Transform packVideoHomeParent;
    private static RectTransform contentPlateRoot;
    private static ResponsiveBaseboardLayout responsiveLayout;
    private static GameObject gemMainPanel;
    private static GameObject coinBottomBar;
    private static GameObject gemBottomBar;
    private static Image coinTabBg;
    private static Image gemTabBg;
    private static TextMeshProUGUI coinTabLabel;
    private static TextMeshProUGUI gemTabLabel;
    private static TextMeshProUGUI coinsLabel;
    private static TextMeshProUGUI gemsLabel;
    private static TextMeshProUGUI gemResultLabel;
    private static TextMeshProUGUI gemOwnedSummary;
    private static Button openPackButton;
    private static StoreTab activeTab = StoreTab.Coin;

    public static void EnsureLayout()
    {
        if (!IsCardStoreScene())
            return;

        if (!IsLayoutAlive())
        {
            PlayerData pd = PlayerData.ResolveCanonical();
            if (pd != null)
                pd.LoadPlayerData();

            layoutBackdrop = null;
            layoutChrome = null;
            contentPlateRoot = null;
            responsiveLayout = null;
            BuildLayout();
        }
        else
        {
            Canvas canvas = ResolveCardStoreCanvas();
            if (canvas != null)
                ResolveContentPlate(canvas);
            RefreshCurrencyLabels();
        }

        SelectTab(activeTab);
        GlobalNavRuntime.RefreshActiveSceneNav();
    }

    public static void RefreshCurrencyLabels()
    {
        PlayerData pd = PlayerData.ResolveCanonical();
        int coins = pd != null ? pd.playerCoins : 0;
        int gems = pd != null ? pd.playerGems : 0;

        if (coinsLabel != null)
            coinsLabel.text = "金幣: " + coins;
        if (gemsLabel != null)
            gemsLabel.text = "寶石: " + gems;
        if (gemOwnedSummary != null)
        {
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            gemOwnedSummary.text = "已解鎖 CD：\n" + PlayerBirdDuelCdState.BuildOwnedSummary(slot);
        }
    }

    private static bool IsCardStoreScene() =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CardStore";

    private static bool IsLayoutAlive() =>
        layoutBackdrop != null && layoutChrome != null && contentPlateRoot != null;

    private static float ResolveMainContentWidth()
    {
        if (contentPlateRoot == null)
            return ReferenceContentWidth - SidebarWidth;
        float plateWidth = contentPlateRoot.rect.width;
        if (plateWidth <= 1f)
            plateWidth = ReferenceContentWidth;
        return Mathf.Max(360f, plateWidth - SidebarWidth);
    }

    private static void BuildLayout()
    {
        CleanupMisplacedUiOnGlobalNav();
        Canvas canvas = ResolveCardStoreCanvas();
        if (canvas == null)
            return;

        contentPlateRoot = ResolveContentPlate(canvas);
        if (contentPlateRoot == null)
            return;

        Canvas.ForceUpdateCanvases();
        float mainContentWidth = ResolveMainContentWidth();

        TMP_FontAsset font = UiFontResolver.ResolveUiFont();
        DestroyLegacySceneUi();
        EnsureCriticalUiUnderContentPlate(contentPlateRoot);

        layoutBackdrop = CreatePanel("CardStorePlateBg", contentPlateRoot, ColorMainBg);
        StretchRect(layoutBackdrop.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        layoutBackdrop.transform.SetAsFirstSibling();

        layoutChrome = new GameObject("CardStoreLayoutChrome", typeof(RectTransform));
        layoutChrome.transform.SetParent(contentPlateRoot, false);
        StretchRect(layoutChrome.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        BuildSidebar(font);
        BuildGemMainPanel(font, mainContentWidth);
        BuildCoinBottomBar(font, mainContentWidth);
        BuildGemBottomBar(font, mainContentWidth);
        LayoutSceneContent();
        layoutChrome.transform.SetAsLastSibling();
        RefreshCurrencyLabels();
    }

    private static RectTransform ResolveContentPlate(Canvas canvas)
    {
        responsiveLayout = canvas.GetComponent<ResponsiveBaseboardLayout>();
        if (responsiveLayout != null)
            responsiveLayout.Apply();

        Transform existing = canvas.transform.Find(ContentPlateName);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(ContentPlateName, typeof(RectTransform));

        if (existing == null)
            go.transform.SetParent(canvas.transform, false);

        float gap = responsiveLayout != null ? responsiveLayout.CurrentBaseboardWidth : 0f;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(gap, 0f);
        rt.offsetMax = new Vector2(-gap, 0f);
        rt.anchoredPosition = Vector2.zero;

        // 底板之下、場景舊物件之上。
        rt.SetSiblingIndex(Mathf.Min(2, canvas.transform.childCount - 1));
        return rt;
    }

    public static void SetCoinPoolInteractable(bool interactable)
    {
        if (openPackButton != null)
            openPackButton.interactable = interactable;
    }

    /// <summary>開包動畫播放時：移到根 Canvas 專用 overlay（高於 GlobalNav），維持原 1920×1080 置中。</summary>
    public static void BringPackVideoToFront()
    {
        Canvas rootCanvas = ResolveCardStoreCanvas();
        GameObject screenGo = FindSceneObject("Screen");
        if (rootCanvas == null || screenGo == null)
            return;

        if (contentPlateRoot == null)
            contentPlateRoot = ResolveContentPlate(rootCanvas);

        GameObject overlay = EnsurePackVideoOverlay(rootCanvas);
        if (packVideoHomeParent == null)
            packVideoHomeParent = screenGo.transform.parent;

        RemoveLegacyScreenOverlayComponents(screenGo);

        RectTransform screenRt = screenGo.GetComponent<RectTransform>();
        screenGo.transform.SetParent(overlay.transform, false);
        ApplyOriginalPackVideoRect(screenRt);
        screenGo.transform.SetAsLastSibling();

        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public static void RestorePackVideoLayer()
    {
        GameObject screenGo = FindSceneObject("Screen");
        if (screenGo == null)
            return;

        Transform home = packVideoHomeParent != null ? packVideoHomeParent : contentPlateRoot;
        if (home != null)
            screenGo.transform.SetParent(home, false);
        packVideoHomeParent = null;

        RemoveLegacyScreenOverlayComponents(screenGo);

        if (packVideoOverlay != null)
            packVideoOverlay.SetActive(false);

        if (contentPlateRoot != null)
        {
            LayoutSceneContent();
            if (layoutChrome != null)
                layoutChrome.transform.SetAsLastSibling();
        }
    }

    private static GameObject EnsurePackVideoOverlay(Canvas rootCanvas)
    {
        if (packVideoOverlay != null)
            return packVideoOverlay;

        Transform existing = rootCanvas.transform.Find(PackVideoOverlayName);
        if (existing != null)
        {
            packVideoOverlay = existing.gameObject;
            return packVideoOverlay;
        }

        packVideoOverlay = new GameObject(PackVideoOverlayName, typeof(RectTransform));
        packVideoOverlay.transform.SetParent(rootCanvas.transform, false);
        StretchRect(packVideoOverlay.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Canvas overlayCanvas = packVideoOverlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = PackVideoOverlaySortOrder;
        packVideoOverlay.AddComponent<GraphicRaycaster>();
        packVideoOverlay.transform.SetAsLastSibling();
        packVideoOverlay.SetActive(false);
        return packVideoOverlay;
    }

    private static void RemoveLegacyScreenOverlayComponents(GameObject screenGo)
    {
        Canvas nested = screenGo.GetComponent<Canvas>();
        if (nested == null)
            return;

        Object.Destroy(nested);
        GraphicRaycaster raycaster = screenGo.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            Object.Destroy(raycaster);
    }

    private static void EnsureCriticalUiUnderContentPlate(RectTransform plateRoot)
    {
        ReparentToContentPlate(plateRoot, "Screen");
        ReparentToContentPlate(plateRoot, "CardPool");
    }

    private static void DestroyLegacySceneUi()
    {
        DestroyIfFound("Open");
        DestroyIfFound("Image");
        DestroyIfFound("背包");

        TextMeshProUGUI[] labels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI tmp = labels[i];
            if (tmp == null || tmp.gameObject.scene.name != "CardStore") continue;
            if (tmp == coinsLabel || tmp == gemsLabel) continue;
            Transform parent = tmp.transform.parent;
            if (parent == null || parent.name != "Canvas") continue;
            if (tmp.name == "Text (TMP)")
                Object.Destroy(tmp.gameObject);
        }
    }

    private static void DestroyIfFound(string objectName)
    {
        GameObject go = FindSceneObject(objectName);
        if (go != null)
            Object.Destroy(go);
    }

    private static Canvas ResolveCardStoreCanvas()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "CardStore")
            return null;

        GameObject canvasGo = SceneSearchUtil.FindSceneObject(scene, "Canvas");
        if (canvasGo != null)
        {
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null)
                return canvas;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Canvas canvas = roots[i].GetComponentInChildren<Canvas>(true);
            if (canvas != null && canvas.gameObject.scene == scene)
                return canvas;
        }

        return null;
    }

    private static void CleanupMisplacedUiOnGlobalNav()
    {
        GlobalNavView nav = Object.FindFirstObjectByType<GlobalNavView>();
        if (nav == null || nav.rootCanvas == null)
            return;

        Transform navRoot = nav.rootCanvas.transform;
        Transform plate = navRoot.Find(ContentPlateName);
        if (plate != null)
            Object.Destroy(plate.gameObject);

        Transform overlay = navRoot.Find(PackVideoOverlayName);
        if (overlay != null)
            Object.Destroy(overlay.gameObject);

        packVideoOverlay = null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "CardStore")
            return null;
        return SceneSearchUtil.FindSceneObject(scene, objectName);
    }

    private static void ReparentToContentPlate(RectTransform plateRoot, string objectName)
    {
        GameObject go = FindSceneObject(objectName);
        if (go == null || go.transform.parent == plateRoot)
            return;
        go.transform.SetParent(plateRoot, false);
    }

    private static void SetActiveIfFound(string name, bool active)
    {
        GameObject go = FindSceneObject(name);
        if (go != null)
            go.SetActive(active);
    }

    private static void BuildSidebar(TMP_FontAsset font)
    {
        GameObject sidebar = CreatePanel("Sidebar", layoutChrome.transform, ColorSidebar);
        RectTransform rt = sidebar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(SidebarWidth, 0f);
        rt.anchoredPosition = Vector2.zero;

        Button coinTab = CreateTabButton(sidebar.transform, font, "CoinTab", "金幣池", 124f, out coinTabBg, out coinTabLabel);
        Button gemTab = CreateTabButton(sidebar.transform, font, "GemTab", "寶石池", -36f, out gemTabBg, out gemTabLabel);

        coinTab.onClick.AddListener(() => SelectTab(StoreTab.Coin));
        gemTab.onClick.AddListener(() => SelectTab(StoreTab.Gem));
    }

    private static void BuildGemMainPanel(TMP_FontAsset font, float mainContentWidth)
    {
        gemMainPanel = CreatePanel("GemMainPanel", contentPlateRoot, new Color(0.12f, 0.14f, 0.19f, 0.40f));
        StretchRect(gemMainPanel.GetComponent<RectTransform>(), SidebarWidth, 0f, 0f, BottomBarHeight);

        CreateText(gemMainPanel.transform, font, "GemTitle", "CD 光碟收藏", 40f, FontStyles.Bold,
            new Vector2(0f, 180f), new Vector2(mainContentWidth - 64f, 56f), ColorBodyText);
        CreateText(gemMainPanel.transform, font, "GemHint",
            "整卡不消耗 · 僅鬥鳥勝利時 draft 偏向所選碟陣營",
            24f, FontStyles.Normal, new Vector2(0f, 120f), new Vector2(mainContentWidth - 32f, 48f), ColorMutedText);

        GameObject summaryGo = CreateText(gemMainPanel.transform, font, "OwnedSummary", "", 26f, FontStyles.Normal,
            new Vector2(0f, -20f), new Vector2(mainContentWidth - 32f, 220f), ColorBodyText);
        gemOwnedSummary = summaryGo.GetComponent<TextMeshProUGUI>();
        gemOwnedSummary.alignment = TextAlignmentOptions.Top;
        gemOwnedSummary.enableWordWrapping = true;
    }

    private static void BuildCoinBottomBar(TMP_FontAsset font, float mainContentWidth)
    {
        coinBottomBar = CreatePanel("CoinBottomBar", layoutChrome.transform, ColorPanel);
        RectTransform rt = coinBottomBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(SidebarWidth, 0f);
        rt.offsetMax = new Vector2(0f, BottomBarHeight);

        CreateText(coinBottomBar.transform, font, "CoinTitle", "卡牌包抽選（金幣）", 30f, FontStyles.Bold,
            new Vector2(0f, 72f), new Vector2(mainContentWidth - 48f, 44f), ColorBodyText);
        CreateText(coinBottomBar.transform, font, "CoinHint", "每包 5 張 · 與 CD 池分開",
            22f, FontStyles.Normal, new Vector2(0f, 38f), new Vector2(mainContentWidth - 48f, 32f), ColorMutedText);

        coinsLabel = CreateAnchoredText(coinBottomBar.transform, font, "Coins", "金幣: 0", 28f, FontStyles.Normal,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(20f, -8f), new Vector2(280f, 40f), ColorBodyText, TextAlignmentOptions.MidlineLeft);

        OpenPackge opener = Object.FindFirstObjectByType<OpenPackge>();
        int packCost = opener != null ? opener.PackCost : 2;
        Button openBtn = CreateAnchoredActionButton(coinBottomBar.transform, font, "OpenPackBtn",
            "開包 " + packCost, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-20f, -8f), new Vector2(220f, 72f), ColorActionAlt);
        if (opener != null)
            openBtn.onClick.AddListener(opener.OnClickOpen);
        openPackButton = openBtn;
    }

    private static void BuildGemBottomBar(TMP_FontAsset font, float mainContentWidth)
    {
        gemBottomBar = CreatePanel("GemBottomBar", layoutChrome.transform, ColorPanel);
        RectTransform rt = gemBottomBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(SidebarWidth, 0f);
        rt.offsetMax = new Vector2(0f, BottomBarHeight);

        CreateText(gemBottomBar.transform, font, "GemBarTitle", "CD 光碟抽選（寶石）", 30f, FontStyles.Bold,
            new Vector2(0f, 72f), new Vector2(mainContentWidth - 32f, 44f), ColorBodyText);
        CreateText(gemBottomBar.transform, font, "GemRates",
            "N58% / R36% / SR6% · 十連至少1R · 80抽必SR · 整卡不消耗",
            20f, FontStyles.Normal, new Vector2(0f, 38f), new Vector2(mainContentWidth - 32f, 32f), ColorMutedText);

        gemsLabel = CreateAnchoredText(gemBottomBar.transform, font, "Gems", "寶石: 0", 28f, FontStyles.Normal,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(20f, -8f), new Vector2(280f, 40f), ColorBodyText, TextAlignmentOptions.MidlineLeft);

        gemResultLabel = CreateAnchoredText(gemBottomBar.transform, font, "GemResult", "", 20f, FontStyles.Italic,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 8f), new Vector2(mainContentWidth - 32f, 48f), ColorMutedText, TextAlignmentOptions.Center);

        Button tenBtn = CreateAnchoredActionButton(gemBottomBar.transform, font, "CdTenBtn",
            "十連 " + BirdDuelCdGachaService.TenPullCost,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-20f, -8f), new Vector2(200f, 72f), ColorAction);
        tenBtn.onClick.AddListener(OnTenCdPull);

        Button singleBtn = CreateAnchoredActionButton(gemBottomBar.transform, font, "CdSingleBtn",
            "單抽 " + BirdDuelCdGachaService.SinglePullCost,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-236f, -8f), new Vector2(200f, 72f), ColorAction);
        singleBtn.onClick.AddListener(OnSingleCdPull);
    }

    private static void LayoutSceneContent()
    {
        RectTransform screenRt = FindRect("Screen");
        RectTransform poolRt = FindRect("CardPool");
        if (screenRt != null)
            ApplyOriginalPackVideoRect(screenRt);
        if (poolRt != null)
            StretchRect(poolRt, SidebarWidth, 0f, 48f, BottomBarHeight);
    }

    /// <summary>場景 Screen 原始尺寸：1920×1080 置中，不隨主區拉伸。</summary>
    private static void ApplyOriginalPackVideoRect(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(ReferenceContentWidth, 1080f);
        rt.localScale = Vector3.one;
    }

    private static void SelectTab(StoreTab tab)
    {
        activeTab = tab;
        bool coin = tab == StoreTab.Coin;

        if (coinBottomBar != null) coinBottomBar.SetActive(coin);
        if (gemBottomBar != null) gemBottomBar.SetActive(!coin);
        if (gemMainPanel != null) gemMainPanel.SetActive(!coin);

        SetActiveIfFound("Screen", coin);
        SetActiveIfFound("CardPool", coin);

        StyleTab(coinTabBg, coinTabLabel, coin);
        StyleTab(gemTabBg, gemTabLabel, !coin);

        if (!coin)
            RefreshCurrencyLabels();
    }

    private static void StyleTab(Image bg, TextMeshProUGUI label, bool selected)
    {
        if (bg != null)
            bg.color = selected ? ColorTabActive : ColorTabIdle;
        if (label != null)
            label.color = selected ? ColorTabActiveText : ColorTabIdleText;
    }

    private static void OnSingleCdPull()
    {
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd == null) return;
        if (pd.playerGems < BirdDuelCdGachaService.SinglePullCost)
        {
            SceneToast.Show($"寶石不足，單抽需要 {BirdDuelCdGachaService.SinglePullCost}（目前 {pd.playerGems}）");
            return;
        }

        if (!BirdDuelCdGachaService.TryPullSingle(pd, out BirdDuelCdGachaService.PullOutcome outcome))
            return;

        RefreshCurrencyLabels();
        if (gemResultLabel != null) gemResultLabel.text = outcome.BuildToastLine();
        SceneToast.Show(outcome.BuildToastLine());
    }

    private static void OnTenCdPull()
    {
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd == null) return;
        if (pd.playerGems < BirdDuelCdGachaService.TenPullCost)
        {
            SceneToast.Show($"寶石不足，十連需要 {BirdDuelCdGachaService.TenPullCost}（目前 {pd.playerGems}）");
            return;
        }

        if (!BirdDuelCdGachaService.TryPullTen(pd, out List<BirdDuelCdGachaService.PullOutcome> outcomes))
            return;

        RefreshCurrencyLabels();
        var lines = new List<string>(outcomes.Count);
        for (int i = 0; i < outcomes.Count; i++)
            lines.Add(outcomes[i].BuildToastLine());
        string summary = string.Join("\n", lines);
        if (gemResultLabel != null) gemResultLabel.text = summary;
        SceneToast.Show("十連完成\n" + summary);
    }

    private static RectTransform FindRect(string objectName)
    {
        GameObject go = FindSceneObject(objectName);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    private static void StretchRect(RectTransform rt, float left, float right, float top, float bottom)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    private static Button CreateTabButton(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string label,
        float y,
        out Image bg,
        out TextMeshProUGUI tmp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        float tabWidth = SidebarWidth - SidebarTabMargin * 2f;
        rt.sizeDelta = new Vector2(tabWidth, SidebarTabHeight);

        bg = go.GetComponent<Image>();
        bg.color = ColorTabIdle;
        bg.raycastTarget = true;
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = bg;

        GameObject textGo = CreateText(go.transform, font, "Label", label, 32f, FontStyles.Bold,
            Vector2.zero, new Vector2(tabWidth - 8f, SidebarTabHeight - 8f), ColorTabIdleText);
        tmp = textGo.GetComponent<TextMeshProUGUI>();
        return btn;
    }

    private static TextMeshProUGUI CreateAnchoredText(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string text,
        float size,
        FontStyles style,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 pos,
        Vector2 dim,
        Color color,
        TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateAnchoredActionButton(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 pos,
        Vector2 size,
        Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = bgColor;
        go.GetComponent<Image>().raycastTarget = true;
        Button btn = go.GetComponent<Button>();

        CreateText(go.transform, font, "Label", label, 24f, FontStyles.Bold,
            Vector2.zero, size, Color.white);
        return btn;
    }

    private static GameObject CreateText(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string text,
        float size,
        FontStyles style,
        Vector2 pos,
        Vector2 dim,
        Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        return go;
    }
}

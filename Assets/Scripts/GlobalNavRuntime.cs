using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class GlobalNavConfigData
{
    public string homeSceneName = "hall";
    public string backpackSceneName = "Persistent";
    public List<string> hideInSceneNameContains = new List<string> { "battle" };
    public float triggerSize = 128f;
    public float triggerTopRightMargin = 28f;
}

/// <summary>
/// Global, cross-scene navigation UI runtime.
/// Single instance + config file driven.
/// </summary>
public class GlobalNavRuntime : MonoBehaviour
{
    private const string RootName = "GlobalNavRuntimeRoot";
    private const string ConfigResourcePath = "GlobalNavConfig";

    private static GlobalNavRuntime instance;
    private static GlobalNavConfigData config;
    private static TMP_FontAsset navLabelFont;

    private GlobalNavView view;
    private GameObject playerInfoOverlayRoot;
    private TextMeshProUGUI playerInfoUuidText;
    private TextMeshProUGUI playerInfoRoleText;
    private TextMeshProUGUI playerInfoStartDateText;
    private TextMeshProUGUI playerInfoCoinsText;
    private TextMeshProUGUI playerInfoDeckSummaryText;
    private TextMeshProUGUI playerInfoHeroSummaryText;
    private TextMeshProUGUI playerInfoWldText;
    private TextMeshProUGUI playerInfoLastResultText;
    private TextMeshProUGUI playerInfoTotalMatchesText;
    private TMP_InputField playerSlotNameInput;
    private Button backpackButton;
    private Button goLoginButton;
    private const float TabPanelRightMargin = 28f;
    private const float TabPanelTopMargin = 176f;
    private const float TabPanelLeftMargin = 24f;
    private const float TabPanelBottomMargin = 24f;

    public static void EnsureInitialized()
    {
        if (instance != null) return;
        config = LoadConfig();

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        instance = root.AddComponent<GlobalNavRuntime>();
        instance.BuildUiRuntime();
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        instance.ApplySceneState(SceneManager.GetActiveScene().name);
    }

    public static bool TryOpenPlayerInfoOverlay()
    {
        EnsureInitialized();
        if (instance == null) return false;
        return instance.OpenPlayerInfoOverlay();
    }

    private static GlobalNavConfigData LoadConfig()
    {
        TextAsset json = Resources.Load<TextAsset>(ConfigResourcePath);
        if (json == null || string.IsNullOrWhiteSpace(json.text))
            return new GlobalNavConfigData();
        try
        {
            GlobalNavConfigData loaded = JsonUtility.FromJson<GlobalNavConfigData>(json.text);
            return loaded ?? new GlobalNavConfigData();
        }
        catch
        {
            return new GlobalNavConfigData();
        }
    }

    private void BuildUiRuntime()
    {
        PlayerProfileCsvService.SetRole("遊戲測試員");

        GameObject uiRoot = new GameObject("GlobalNavUI");
        uiRoot.transform.SetParent(transform, false);

        GameObject canvasObj = new GameObject("GlobalNavCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(uiRoot.transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject trigger = CreateButton(
            canvasObj.transform,
            "TriggerButton",
            "≡",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(28f, -28f),
            new Vector2(128f, 128f),
            new Color(0.53f, 0.36f, 0.78f, 0.95f),
            40f);

        GameObject panel = new GameObject("TabPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.94f, 0.9f, 0.82f, 0.98f);
        panelBg.type = Image.Type.Sliced;

        GameObject homeBtnObj = CreateNavTileButton(
            panel.transform,
            "HomeButton",
            "回首頁",
            new Vector2(0.5f, 0.68f),
            new Vector2(0.5f, 0.68f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f),
            30f);
        GameObject playerInfoBtnObj = CreateNavTileButton(
            panel.transform,
            "PlayerInfoButton",
            "玩家資訊",
            new Vector2(0.5f, 0.48f),
            new Vector2(0.5f, 0.48f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.35f, 0.56f, 0.34f, 0.98f),
            30f);
        GameObject backpackBtnObj = CreateNavTileButton(
            panel.transform,
            "BackpackButton",
            "背包",
            new Vector2(0.5f, 0.28f),
            new Vector2(0.5f, 0.28f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.24f, 0.47f, 0.32f, 0.98f),
            30f);
        GameObject goLoginBtnObj = CreateNavTileButton(
            panel.transform,
            "GoLoginButton",
            "回到登入頁面",
            new Vector2(0.5f, 0.14f),
            new Vector2(0.5f, 0.14f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.42f, 0.37f, 0.22f, 0.98f),
            30f);
        view = uiRoot.AddComponent<GlobalNavView>();
        view.rootCanvas = canvas;
        view.triggerButtonObject = trigger;
        view.tabPanelObject = panel;
        view.triggerButton = trigger.GetComponent<Button>();
        view.homeButton = homeBtnObj.GetComponent<Button>();
        view.playerInfoButton = playerInfoBtnObj.GetComponent<Button>();
        view.closeButton = null;
        backpackButton = backpackBtnObj.GetComponent<Button>();
        goLoginButton = goLoginBtnObj.GetComponent<Button>();

        if (view.rootCanvas != null) view.rootCanvas.sortingOrder = 6000;

        // Apply config-driven trigger size and mirrored-left position.
        if (view.triggerButtonObject != null)
        {
            RectTransform rt = view.triggerButtonObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                float m = Mathf.Max(0f, config.triggerTopRightMargin);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(m, -m);
                float s = Mathf.Max(64f, config.triggerSize);
                rt.sizeDelta = new Vector2(s, s);
            }
        }

        if (view.triggerButton != null)
        {
            view.triggerButton.onClick.RemoveAllListeners();
            view.triggerButton.onClick.AddListener(() =>
            {
                bool willOpen = view.tabPanelObject != null && !view.tabPanelObject.activeSelf;
                SetTabPanelOpen(willOpen);
            });
        }

        ApplyTabPanelLayout();

        if (view.homeButton != null)
        {
            view.homeButton.onClick.RemoveAllListeners();
            view.homeButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadHomeScene();
            });
        }

        if (view.playerInfoButton != null)
        {
            view.playerInfoButton.onClick.RemoveAllListeners();
            view.playerInfoButton.onClick.AddListener(() =>
            {
                TogglePlayerInfoPanel();
                SetTabPanelOpen(false);
            });
        }

        if (backpackButton != null)
        {
            backpackButton.onClick.RemoveAllListeners();
            backpackButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadBackpackScene();
            });
        }

        if (goLoginButton != null)
        {
            goLoginButton.onClick.RemoveAllListeners();
            goLoginButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadLoginScene();
            });
        }

        RefreshNavFontAndApplyToAllTexts();
        SetTabPanelOpen(false);
    }

    private static GameObject CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = go.GetComponent<Image>();
        bg.color = bgColor;
        bg.type = Image.Type.Sliced;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        EnsureNavLabelFont();
        if (navLabelFont != null) tmp.font = navLabelFont;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return go;
    }

    private static GameObject CreateNavTileButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = go.GetComponent<Image>();
        bg.color = bgColor;
        bg.type = Image.Type.Sliced;

        // Upper area reserved for future icon sprite binding.
        GameObject iconSlotObj = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        iconSlotObj.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconSlotObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.62f);
        iconRt.anchorMax = new Vector2(0.5f, 0.62f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(96f, 96f); // square icon slot
        Image iconImg = iconSlotObj.GetComponent<Image>();
        iconImg.color = new Color(1f, 1f, 1f, 0.2f);
        iconImg.type = Image.Type.Sliced;
        iconImg.raycastTarget = false;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        // Move label outside the button, right below it.
        tr.anchorMin = new Vector2(0.5f, 0f);
        tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -8f);
        tr.sizeDelta = new Vector2(size.x + 28f, 40f);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        EnsureNavLabelFont();
        if (navLabelFont != null) tmp.font = navLabelFont;
        tmp.fontSize = fontSize - 2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return go;
    }

    private static void EnsureNavLabelFont()
    {
        if (navLabelFont != null && FontSupportsRequiredGlyphs(navLabelFont)) return;
        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null && FontSupportsRequiredGlyphs(buildbeck))
        {
            navLabelFont = buildbeck;
            return;
        }
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset f = fonts[i];
            if (f == null || string.IsNullOrEmpty(f.name)) continue;
            if (!FontSupportsRequiredGlyphs(f)) continue;
            string n = f.name.ToLowerInvariant();
            if (n.StartsWith("notosanstc") || n.StartsWith("tc") || FontNameLikelySupportsCjk(n))
            {
                navLabelFont = f;
                return;
            }
        }

        TextMeshProUGUI[] tmps = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] == null || tmps[i].font == null) continue;
            if (!FontSupportsRequiredGlyphs(tmps[i].font)) continue;
            if (FontNameLikelySupportsCjk(tmps[i].font.name))
            {
                navLabelFont = tmps[i].font;
                return;
            }
        }

        navLabelFont = TMP_Settings.defaultFontAsset;
    }

    private static bool FontSupportsRequiredGlyphs(TMP_FontAsset font)
    {
        if (font == null) return false;
        const string required = "玩家資訊回首頁登入頁面重置資料儲存名稱";
        for (int i = 0; i < required.Length; i++)
        {
            char ch = required[i];
            if (char.IsWhiteSpace(ch)) continue;
            if (!font.HasCharacter(ch, true)) return false;
        }
        return true;
    }

    private static bool FontNameLikelySupportsCjk(string fontAssetName)
    {
        if (string.IsNullOrEmpty(fontAssetName)) return false;
        string n = fontAssetName.ToLowerInvariant();
        return n.Contains("noto") ||
               n.Contains("cjk") ||
               n.Contains("sourcehansans") ||
               n.Contains("source han") ||
               n.Contains("jhenghei") ||
               n.Contains("yahei") ||
               n.Contains("pingfang") ||
               n.Contains("applesdgothic") ||
               n.Contains("nanum") ||
               n.Contains("mplus") ||
               (n.Contains("han") && (n.Contains("sans") || n.Contains("serif")));
    }

    private void ApplyTabPanelLayout()
    {
        if (view == null || view.tabPanelObject == null) return;
        RectTransform panelRt = view.tabPanelObject.GetComponent<RectTransform>();
        if (panelRt == null) return;

        // Keep panel upper edge alignment, but stretch left and bottom close to screen bounds.
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.offsetMin = new Vector2(TabPanelLeftMargin, TabPanelBottomMargin);
        panelRt.offsetMax = new Vector2(-TabPanelRightMargin, -TabPanelTopMargin);

        ResizeTabButton(view.homeButton, 0.18f, 0.5f);
        ResizeTabButton(view.playerInfoButton, 0.40f, 0.5f);
        ResizeTabButton(backpackButton, 0.62f, 0.5f);
        ResizeTabButton(goLoginButton, 0.84f, 0.5f);
    }

    private static void ResizeTabButton(UnityEngine.UI.Button button, float anchorX, float anchorY)
    {
        if (button == null) return;
        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 200f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.fontSize = 28f;
    }

    private void SetTabPanelOpen(bool open)
    {
        if (view == null || view.tabPanelObject == null) return;
        view.tabPanelObject.SetActive(open);

        Image panelImage = view.tabPanelObject.GetComponent<Image>();
        if (panelImage != null) panelImage.raycastTarget = open;

        if (view.homeButton != null) view.homeButton.interactable = open;
        if (view.playerInfoButton != null) view.playerInfoButton.interactable = open;
        if (backpackButton != null) backpackButton.interactable = open;
        if (goLoginButton != null) goLoginButton.interactable = open;
        if (view.closeButton != null) view.closeButton.interactable = open;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshNavFontAndApplyToAllTexts();
        ApplySceneState(scene.name);
    }

    private void RefreshNavFontAndApplyToAllTexts()
    {
        navLabelFont = null;
        EnsureNavLabelFont();
        if (navLabelFont == null) return;
        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;
            labels[i].font = navLabelFont;
        }
    }

    private void ApplySceneState(string sceneName)
    {
        bool hidden = string.Equals(sceneName, "login", StringComparison.OrdinalIgnoreCase);
        if (config.hideInSceneNameContains != null)
        {
            string lower = (sceneName ?? string.Empty).ToLowerInvariant();
            for (int i = 0; i < config.hideInSceneNameContains.Count; i++)
            {
                string key = config.hideInSceneNameContains[i];
                if (string.IsNullOrEmpty(key)) continue;
                if (lower.Contains(key.ToLowerInvariant()))
                {
                    hidden = true;
                    break;
                }
            }
        }

        if (view != null && view.triggerButtonObject != null) view.triggerButtonObject.SetActive(!hidden);
        SetTabPanelOpen(false);
        if (playerInfoOverlayRoot != null) playerInfoOverlayRoot.SetActive(false);
    }

    private static void TryLoadHomeScene()
    {
        string home = string.IsNullOrWhiteSpace(config != null ? config.homeSceneName : null)
            ? "hall"
            : config.homeSceneName;
        string resolved = ResolveSceneFromBuildSettings(home);
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: home scene not found in Build Settings -> " + home);
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private static void TryLoadLoginScene()
    {
        // Ensure game is not left paused when switching from battle/pause menu.
        Time.timeScale = 1f;
        string resolved = ResolveSceneFromBuildSettings("login");
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: login scene not found in Build Settings -> login");
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private static void TryLoadBackpackScene()
    {
        string preferred = string.IsNullOrWhiteSpace(config != null ? config.backpackSceneName : null)
            ? "Persistent"
            : config.backpackSceneName;
        string resolved = ResolveSceneFromBuildSettings(preferred);
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: backpack scene not found in Build Settings -> " + preferred);
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private void TogglePlayerInfoPanel()
    {
        EnsurePlayerInfoOverlay();
        if (playerInfoOverlayRoot == null) return;
        if (playerInfoOverlayRoot.activeSelf) playerInfoOverlayRoot.SetActive(false);
        else OpenPlayerInfoOverlay();
    }

    private bool OpenPlayerInfoOverlay()
    {
        Input.imeCompositionMode = IMECompositionMode.On;
        EnsurePlayerInfoOverlay();
        if (playerInfoOverlayRoot == null) return false;
        RefreshPlayerInfoOverlayContent();
        playerInfoOverlayRoot.transform.SetAsLastSibling();
        playerInfoOverlayRoot.SetActive(true);
        return true;
    }

    private void RefreshPlayerInfoOverlayContent()
    {
        PlayerProfileCsvService.PlayerProfile p = PlayerProfileCsvService.RefreshProfileFromRuntime();
        PlayerData pd = UnityEngine.Object.FindFirstObjectByType<PlayerData>();
        if (pd != null) pd.LoadPlayerData();
        int coins = pd != null ? pd.playerCoins : (PlayerData.TryGetActiveSlotCoinsFromSave(out int coinsFromSave) ? coinsFromSave : 0);
        string slotName = PlayerData.GetActivePlayerSlotName();
        int slot = pd != null ? Mathf.Clamp(pd.activePlayerSlot, 1, PlayerData.MaxPlayerSlots) : 1;

        if (playerSlotNameInput != null) playerSlotNameInput.text = slotName;

        string uuidShort = string.IsNullOrWhiteSpace(p.uuid)
            ? "-"
            : (p.uuid.Length > 12 ? p.uuid.Substring(0, 8) + "..." : p.uuid);
        if (playerInfoUuidText != null) playerInfoUuidText.text = "UUID: " + uuidShort;
        if (playerInfoRoleText != null) playerInfoRoleText.text = "玩家身份: " + (string.IsNullOrWhiteSpace(p.role) ? "-" : p.role);
        if (playerInfoStartDateText != null) playerInfoStartDateText.text = "開始遊玩日期: " + (string.IsNullOrWhiteSpace(p.startDate) ? "-" : p.startDate);
        if (playerInfoCoinsText != null) playerInfoCoinsText.text = "金幣: " + coins;
        if (playerInfoDeckSummaryText != null) playerInfoDeckSummaryText.text = "持有的牌組: " + (string.IsNullOrWhiteSpace(p.decks) ? "-" : p.decks);
        if (playerInfoHeroSummaryText != null) playerInfoHeroSummaryText.text = "持有的英雄: " + (string.IsNullOrWhiteSpace(p.heroes) ? "-" : p.heroes);
        if (playerInfoWldText != null) playerInfoWldText.text = "W/L/D/Q: " + p.wins + " / " + p.losses + " / " + p.draws + " / " + p.quits;
        if (playerInfoLastResultText != null) playerInfoLastResultText.text = "最近結果: " + (string.IsNullOrWhiteSpace(p.lastResult) ? "-" : p.lastResult);
        if (playerInfoTotalMatchesText != null) playerInfoTotalMatchesText.text = "總場次: " + Mathf.Max(0, p.wins + p.losses + p.draws + p.quits);

        if (playerInfoRoleText != null)
            playerInfoRoleText.text = "玩家身份: " + (string.IsNullOrWhiteSpace(p.role) ? "-" : p.role) + "  (槽位 " + slot + ")";
    }

    private void EnsurePlayerInfoOverlay()
    {
        if (playerInfoOverlayRoot != null || view == null || view.rootCanvas == null) return;

        GameObject root = new GameObject("GlobalPlayerInfoOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(view.rootCanvas.transform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        Image dim = root.GetComponent<Image>();
        dim.color = new Color(0.1f, 0.08f, 0.06f, 0.72f);
        dim.raycastTarget = true;

        float panelWidth = Mathf.Min(Screen.width * 0.8f, 1160f);
        float panelHeight = Mathf.Min(Screen.height * 0.82f, 780f);

        GameObject panel = new GameObject("ProfilePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.94f, 0.9f, 0.84f, 0.99f);
        panelBg.raycastTarget = true;

        GameObject header = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.offsetMin = new Vector2(0f, -84f);
        headerRt.offsetMax = new Vector2(0f, 0f);
        Image headerBg = header.GetComponent<Image>();
        headerBg.color = new Color(0.85f, 0.79f, 0.68f, 0.9f);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(32f, -18f);
        titleRt.sizeDelta = new Vector2(420f, 48f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (navLabelFont != null) titleTmp.font = navLabelFont;
        titleTmp.fontSize = 40f;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = new Color(0.25f, 0.2f, 0.15f, 1f);
        titleTmp.text = "玩家資訊";

        GameObject closeBtnObj = CreateButton(
            header.transform,
            "CloseButton",
            "關閉",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -18f),
            new Vector2(124f, 54f),
            new Color(0.45f, 0.29f, 0.24f, 0.96f),
            28f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(() =>
        {
            if (playerInfoOverlayRoot != null) playerInfoOverlayRoot.SetActive(false);
        });

        GameObject resetBtnObj = CreateButton(
            panel.transform,
            "ResetButton",
            "重置資料",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-160f, -18f),
            new Vector2(150f, 54f),
            new Color(0.6f, 0.24f, 0.22f, 0.96f),
            28f);
        Button resetBtn = resetBtnObj.GetComponent<Button>();
        resetBtn.onClick.RemoveAllListeners();
        resetBtn.onClick.AddListener(() =>
        {
            PlayerProfileCsvService.ResetPlayerProgressLikeBackpack();
            PlayerProfileCsvService.SetRole("遊戲測試員");
            RefreshPlayerInfoOverlayContent();
        });

        GameObject slotSection = new GameObject("SlotSection", typeof(RectTransform), typeof(Image));
        slotSection.transform.SetParent(panel.transform, false);
        RectTransform slotRt = slotSection.GetComponent<RectTransform>();
        slotRt.anchorMin = new Vector2(0f, 1f);
        slotRt.anchorMax = new Vector2(1f, 1f);
        slotRt.pivot = new Vector2(0.5f, 1f);
        slotRt.offsetMin = new Vector2(24f, -258f);
        slotRt.offsetMax = new Vector2(-24f, -98f);
        Image slotBg = slotSection.GetComponent<Image>();
        slotBg.color = new Color(0.97f, 0.95f, 0.9f, 0.95f);

        GameObject slotNameLabel = new GameObject("SlotNameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotNameLabel.transform.SetParent(slotSection.transform, false);
        RectTransform slotNameLabelRt = slotNameLabel.GetComponent<RectTransform>();
        slotNameLabelRt.anchorMin = new Vector2(0f, 1f);
        slotNameLabelRt.anchorMax = new Vector2(0f, 1f);
        slotNameLabelRt.pivot = new Vector2(0f, 1f);
        slotNameLabelRt.anchoredPosition = new Vector2(20f, -18f);
        slotNameLabelRt.sizeDelta = new Vector2(130f, 34f);
        TextMeshProUGUI slotNameLabelTmp = slotNameLabel.GetComponent<TextMeshProUGUI>();
        if (navLabelFont != null) slotNameLabelTmp.font = navLabelFont;
        slotNameLabelTmp.fontSize = 24f;
        slotNameLabelTmp.alignment = TextAlignmentOptions.Left;
        slotNameLabelTmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);
        slotNameLabelTmp.text = "槽位名稱:";

        GameObject inputBgObj = new GameObject("SlotNameInputBg", typeof(RectTransform), typeof(Image));
        inputBgObj.transform.SetParent(slotSection.transform, false);
        RectTransform inputBgRt = inputBgObj.GetComponent<RectTransform>();
        inputBgRt.anchorMin = new Vector2(0f, 1f);
        inputBgRt.anchorMax = new Vector2(0f, 1f);
        inputBgRt.pivot = new Vector2(0f, 1f);
        inputBgRt.anchoredPosition = new Vector2(156f, -14f);
        inputBgRt.sizeDelta = new Vector2(320f, 42f);
        Image inputBg = inputBgObj.GetComponent<Image>();
        inputBg.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(inputBgObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(10f, 6f);
        viewportRt.offsetMax = new Vector2(-10f, -6f);

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(viewportObj.transform, false);
        RectTransform phRt = placeholderObj.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
        if (navLabelFont != null) placeholder.font = navLabelFont;
        placeholder.fontSize = 22f;
        placeholder.color = new Color(0.45f, 0.4f, 0.35f, 0.75f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.richText = false;
        placeholder.text = "玩家槽位名稱";

        GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inputTextObj.transform.SetParent(viewportObj.transform, false);
        RectTransform inputTextRt = inputTextObj.GetComponent<RectTransform>();
        inputTextRt.anchorMin = Vector2.zero;
        inputTextRt.anchorMax = Vector2.one;
        inputTextRt.offsetMin = Vector2.zero;
        inputTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI inputText = inputTextObj.GetComponent<TextMeshProUGUI>();
        if (navLabelFont != null) inputText.font = navLabelFont;
        inputText.fontSize = 22f;
        inputText.color = new Color(0.2f, 0.16f, 0.12f, 1f);
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.richText = false;
        inputText.overflowMode = TextOverflowModes.Overflow;
        inputText.enableWordWrapping = false;

        playerSlotNameInput = inputBgObj.AddComponent<TmpInputFieldImeRedraw>();
        playerSlotNameInput.textViewport = viewportRt;
        playerSlotNameInput.textComponent = inputText;
        playerSlotNameInput.placeholder = placeholder;
        playerSlotNameInput.characterLimit = 24;
        playerSlotNameInput.characterValidation = TMP_InputField.CharacterValidation.None;
        playerSlotNameInput.richText = false;

        GameObject saveNameBtnObj = CreateButton(
            slotSection.transform,
            "SaveSlotNameButton",
            "儲存名稱",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(490f, -14f),
            new Vector2(140f, 42f),
            new Color(0.24f, 0.47f, 0.32f, 0.96f),
            20f);
        Button saveNameBtn = saveNameBtnObj.GetComponent<Button>();
        saveNameBtn.onClick.RemoveAllListeners();
        saveNameBtn.onClick.AddListener(() =>
        {
            string newName = playerSlotNameInput != null ? playerSlotNameInput.text : string.Empty;
            PlayerData.SetActivePlayerSlotName(newName);
            RefreshPlayerInfoOverlayContent();
        });

        playerInfoUuidText = CreateInfoText(slotSection.transform, "UuidText", new Vector2(20f, -66f), new Vector2(panelWidth - 88f, 32f), 20f);
        playerInfoRoleText = CreateInfoText(slotSection.transform, "RoleText", new Vector2(20f, -100f), new Vector2(520f, 30f), 20f);
        playerInfoStartDateText = CreateInfoText(slotSection.transform, "StartDateText", new Vector2(560f, -100f), new Vector2(480f, 30f), 20f);

        GameObject assetSection = new GameObject("AssetSection", typeof(RectTransform), typeof(Image));
        assetSection.transform.SetParent(panel.transform, false);
        RectTransform assetRt = assetSection.GetComponent<RectTransform>();
        assetRt.anchorMin = new Vector2(0f, 1f);
        assetRt.anchorMax = new Vector2(1f, 1f);
        assetRt.pivot = new Vector2(0.5f, 1f);
        assetRt.offsetMin = new Vector2(24f, -460f);
        assetRt.offsetMax = new Vector2(-24f, -270f);
        Image assetBg = assetSection.GetComponent<Image>();
        assetBg.color = new Color(0.97f, 0.95f, 0.9f, 0.95f);

        playerInfoCoinsText = CreateInfoText(assetSection.transform, "CoinsText", new Vector2(20f, -18f), new Vector2(240f, 34f), 24f);
        playerInfoDeckSummaryText = CreateInfoText(assetSection.transform, "DeckSummaryText", new Vector2(20f, -58f), new Vector2(panelWidth - 88f, 56f), 20f);
        playerInfoHeroSummaryText = CreateInfoText(assetSection.transform, "HeroSummaryText", new Vector2(20f, -122f), new Vector2(panelWidth - 88f, 56f), 20f);

        GameObject recordSection = new GameObject("RecordSection", typeof(RectTransform), typeof(Image));
        recordSection.transform.SetParent(panel.transform, false);
        RectTransform recordRt = recordSection.GetComponent<RectTransform>();
        recordRt.anchorMin = new Vector2(0f, 1f);
        recordRt.anchorMax = new Vector2(1f, 1f);
        recordRt.pivot = new Vector2(0.5f, 1f);
        recordRt.offsetMin = new Vector2(24f, -614f);
        recordRt.offsetMax = new Vector2(-24f, -472f);
        Image recordBg = recordSection.GetComponent<Image>();
        recordBg.color = new Color(0.97f, 0.95f, 0.9f, 0.95f);

        playerInfoWldText = CreateInfoText(recordSection.transform, "WldText", new Vector2(20f, -24f), new Vector2(360f, 40f), 30f);
        playerInfoLastResultText = CreateInfoText(recordSection.transform, "LastResultText", new Vector2(400f, -28f), new Vector2(300f, 34f), 24f);
        playerInfoTotalMatchesText = CreateInfoText(recordSection.transform, "TotalMatchesText", new Vector2(20f, -78f), new Vector2(280f, 30f), 20f);

        playerInfoOverlayRoot = root;
        playerInfoOverlayRoot.SetActive(false);
    }

    private TextMeshProUGUI CreateInfoText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (navLabelFont != null) tmp.font = navLabelFont;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.text = string.Empty;
        return tmp;
    }

    private static string ResolveSceneFromBuildSettings(string preferredName)
    {
        if (string.IsNullOrEmpty(preferredName)) return null;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(sceneName, preferredName, StringComparison.OrdinalIgnoreCase))
                return sceneName;
        }
        return null;
    }

}

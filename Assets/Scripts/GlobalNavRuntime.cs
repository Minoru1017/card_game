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
    public string settingsSceneName = "Settings";
    public List<string> hideInSceneNameContains = new List<string> { "battle", "buildbeck", "builddeck" };
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
    private TextMeshProUGUI playerInfoLastResultText;
    private TextMeshProUGUI playerInfoProgressText;
    private TextMeshProUGUI playerInfoRecordTotalText;
    private const int PlayerInfoOverlayLayoutVersion = 2;
    private int builtPlayerInfoLayoutVersion;
    private int playerInfoActiveRecordFilter = 1;
    private readonly Button[] playerInfoRecordFilterButtons = new Button[4];
    private readonly Image[] playerInfoRecordFilterButtonBgs = new Image[4];
    private readonly TextMeshProUGUI[] playerInfoRecordFilterLabels = new TextMeshProUGUI[4];
    private PlayerInfoRecordColumnUi[] playerInfoRecordColumns;
    private static readonly int[] PlayerInfoRecordFilterCodes = { 1, -1, 2, 3 };
    private static readonly string[] PlayerInfoRecordFilterLabels = { "W", "L", "D", "Q" };
    private static readonly Color[] PlayerInfoDifficultyBadgeColors =
    {
        new Color(0.36f, 0.78f, 0.44f, 1f),
        new Color(0.45f, 0.72f, 0.95f, 1f),
        new Color(0.95f, 0.78f, 0.28f, 1f),
        new Color(0.95f, 0.42f, 0.50f, 1f),
        new Color(0.62f, 0.38f, 0.88f, 1f)
    };

    private sealed class PlayerInfoRecordColumnUi
    {
        public Image badgeImage;
        public TextMeshProUGUI badgeText;
        public TextMeshProUGUI countText;
    }

    private const float PlayerInfoPadH = 28f;
    private const float PlayerInfoSectionGap = 18f;
    private const float PlayerInfoLineGap = 10f;
    private const float PlayerInfoHeaderHeight = 76f;
    private const float PlayerInfoFooterHeight = 60f;
    private static readonly Color PlayerInfoTextPrimary = new Color(0.2f, 0.16f, 0.12f, 1f);
    private static readonly Color PlayerInfoTextMuted = new Color(0.48f, 0.42f, 0.36f, 1f);
    private static readonly Color PlayerInfoSectionBg = new Color(0.98f, 0.96f, 0.92f, 0.98f);
    private static readonly Color PlayerInfoSectionTitle = new Color(0.32f, 0.27f, 0.22f, 1f);

    private RectTransform playerInfoScrollContentRt;
    private float playerInfoLayoutY;
    private float playerInfoContentWidth;
    private TMP_InputField playerSlotNameInput;
    private Button backpackButton;
    private Button settingsButton;
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
        GameObject settingsBtnObj = CreateNavTileButton(
            panel.transform,
            "SettingsButton",
            "遊戲設定",
            new Vector2(0.5f, 0.20f),
            new Vector2(0.5f, 0.20f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.38f, 0.32f, 0.58f, 0.98f),
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
        settingsButton = settingsBtnObj.GetComponent<Button>();
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

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadSettingsScene();
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

    private static void ApplyPlayerInfoFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        EnsureNavLabelFont();
        if (navLabelFont != null)
            tmp.font = navLabelFont;
        SettingsUiFonts.ApplyTo(tmp);
    }

    private static void EnsureNavLabelFont()
    {
        if (navLabelFont != null && FontSupportsRequiredGlyphs(navLabelFont)) return;

        TMP_FontAsset settingsFont = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settingsFont != null && FontSupportsRequiredGlyphs(settingsFont))
        {
            navLabelFont = settingsFont;
            return;
        }

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

    private static bool FontSupportsRequiredGlyphs(TMP_FontAsset font) =>
        BuildbeckUiFonts.FontSupportsText(font, PlayerInfoProgressCopy.FontGlyphProbe);

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

        ResizeTabButton(view.homeButton, 0.12f, 0.5f);
        ResizeTabButton(view.playerInfoButton, 0.30f, 0.5f);
        ResizeTabButton(backpackButton, 0.48f, 0.5f);
        ResizeTabButton(settingsButton, 0.66f, 0.5f);
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
        if (settingsButton != null) settingsButton.interactable = open;
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
        bool hidden = string.Equals(sceneName, "login", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, "Buildbeck", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, "Builddeck", StringComparison.OrdinalIgnoreCase);
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

    private static void TryLoadSettingsScene()
    {
        string preferred = string.IsNullOrWhiteSpace(config != null ? config.settingsSceneName : null)
            ? "Settings"
            : config.settingsSceneName;
        string resolved = ResolveSceneFromBuildSettings(preferred);
        if (string.IsNullOrEmpty(resolved))
        {
            Debug.LogError("GlobalNavRuntime: settings scene not found in Build Settings -> " + preferred);
            return;
        }
        SceneManager.LoadScene(resolved);
    }

    private void TogglePlayerInfoPanel()
    {
        EnsurePlayerInfoOverlay();
        if (playerInfoOverlayRoot == null) return;
        if (playerInfoOverlayRoot.activeSelf)
        {
            playerInfoOverlayRoot.SetActive(false);
            RefreshBuildbeckDeckNameLabelsAfterPlayerInfo();
        }
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

    private static void RefreshBuildbeckDeckNameLabelsAfterPlayerInfo()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.IsValid() || !s.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase))
            return;
        GameObject dm = GameObject.Find("DataManager");
        if (dm == null) return;
        DeckManager deck = dm.GetComponent<DeckManager>();
        if (deck != null)
            deck.RefreshBuildbeckDeckNameLabelsIfActive();
    }

    private void RefreshPlayerInfoOverlayContent()
    {
        PlayerProfileCsvService.PlayerProfile p = PlayerProfileCsvService.RefreshProfileFromRuntime();
        PlayerData pd = PlayerData.ResolveCanonical();
        int coins = pd != null ? pd.playerCoins : (PlayerData.TryGetActiveSlotCoinsFromSave(out int coinsFromSave) ? coinsFromSave : 0);
        string slotName = PlayerData.GetActivePlayerSlotName();
        int slot = pd != null ? Mathf.Clamp(pd.activePlayerSlot, 1, PlayerData.MaxPlayerSlots) : 1;

        if (playerSlotNameInput != null) playerSlotNameInput.text = slotName;

        string uuidShort = string.IsNullOrWhiteSpace(p.uuid)
            ? "-"
            : (p.uuid.Length > 12 ? p.uuid.Substring(0, 8) + "..." : p.uuid);
        if (playerInfoUuidText != null)
            playerInfoUuidText.text = uuidShort;
        if (playerInfoRoleText != null)
            playerInfoRoleText.text = PlayerInfoProgressCopy.FormatRoleWithSlot(p.role, slot);
        if (playerInfoProgressText != null)
        {
            playerInfoProgressText.text = PlayerInfoProgressCopy.BuildSummary(slot);
            FitProfileValueTextHeight(playerInfoProgressText, 26f, 28f);
        }
        if (playerInfoStartDateText != null)
            playerInfoStartDateText.text = string.IsNullOrWhiteSpace(p.startDate) ? "-" : p.startDate;
        if (playerInfoCoinsText != null)
            playerInfoCoinsText.text = coins.ToString("N0");
        if (playerInfoDeckSummaryText != null)
        {
            playerInfoDeckSummaryText.text = FormatDeckSummaryForDisplay(p.decks);
            FitProfileValueTextHeight(playerInfoDeckSummaryText, 26f, 30f);
        }
        if (playerInfoHeroSummaryText != null)
            playerInfoHeroSummaryText.text = "無";
        if (playerInfoLastResultText != null)
            playerInfoLastResultText.text = string.IsNullOrWhiteSpace(p.lastResult) ? "-" : p.lastResult;
        RefreshPlayerInfoRecordPanel(p);
        RefreshBuildbeckDeckNameLabelsAfterPlayerInfo();
    }

    private void EnsurePlayerInfoOverlay()
    {
        if (playerInfoOverlayRoot != null && builtPlayerInfoLayoutVersion == PlayerInfoOverlayLayoutVersion)
            return;

        DestroyPlayerInfoOverlayIfAny();
        if (view == null || view.rootCanvas == null) return;

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
        headerRt.offsetMin = new Vector2(0f, -PlayerInfoHeaderHeight);
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
        ApplyPlayerInfoFont(titleTmp);
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

        GameObject footer = new GameObject("FooterBar", typeof(RectTransform), typeof(Image));
        footer.transform.SetParent(panel.transform, false);
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = Vector2.zero;
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.offsetMin = Vector2.zero;
        footerRt.offsetMax = new Vector2(0f, PlayerInfoFooterHeight);
        footer.GetComponent<Image>().color = new Color(0.88f, 0.82f, 0.74f, 0.92f);

        GameObject resetBtnObj = CreateButton(
            footer.transform,
            "ResetButton",
            "重置資料",
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-PlayerInfoPadH, 0f),
            new Vector2(148f, 44f),
            new Color(0.6f, 0.24f, 0.22f, 0.96f),
            22f);
        Button resetBtn = resetBtnObj.GetComponent<Button>();
        resetBtn.onClick.RemoveAllListeners();
        resetBtn.onClick.AddListener(() =>
        {
            PlayerProfileCsvService.ResetPlayerProgressLikeBackpack();
            PlayerProfileCsvService.SetRole("遊戲測試員");
            RefreshPlayerInfoOverlayContent();
        });

        Transform scrollContent = CreatePlayerInfoScrollArea(panel.transform, panelWidth, PlayerInfoHeaderHeight, PlayerInfoFooterHeight);
        playerInfoContentWidth = panelWidth - PlayerInfoPadH * 2f;
        playerInfoLayoutY = -8f;

        const float profileTwoLineRowH = 58f;

        Transform basicBody = CreatePlayerInfoSection(scrollContent, "基本資料", 328f);
        float rowY = -14f;
        CreatePlayerInfoSlotNameRow(basicBody, ref rowY);
        playerInfoUuidText = PlaceProfileField(basicBody, "UUID", "UuidText", ref rowY, profileTwoLineRowH, 19f);
        playerInfoRoleText = PlaceProfileField(basicBody, "玩家身份", "RoleText", ref rowY, profileTwoLineRowH, 19f);
        playerInfoStartDateText = PlaceProfileField(basicBody, "開始遊玩", "StartDateText", ref rowY, profileTwoLineRowH, 19f);

        const int progressLineCount = 7;
        const float progressLineHeight = 28f;
        float progressBlockRowH = 22f + 4f + progressLineCount * progressLineHeight;
        Transform progressBody = CreatePlayerInfoSection(scrollContent, PlayerInfoProgressCopy.SectionTitle, 52f + progressBlockRowH + 16f);
        rowY = -14f;
        playerInfoProgressText = PlaceProfileField(
            progressBody,
            "主線章節",
            "StoryProgressText",
            ref rowY,
            progressBlockRowH,
            19f,
            wrapValue: true,
            valueLineSpacing: 4f);

        int deckLineCount = 5;
        PlayerData layoutPlayerData = PlayerData.ResolveCanonical();
        if (layoutPlayerData != null && layoutPlayerData.deckSlotCount > 0)
            deckLineCount = layoutPlayerData.deckSlotCount;
        const float profileDeckLineHeight = 30f;
        float profileDeckBlockRowH = 22f + 4f + deckLineCount * profileDeckLineHeight;
        float profileAssetSectionH = 52f + profileTwoLineRowH + PlayerInfoLineGap + profileDeckBlockRowH +
                                     PlayerInfoLineGap + profileTwoLineRowH + 24f;

        Transform assetBody = CreatePlayerInfoSection(scrollContent, "資產與收藏", profileAssetSectionH);
        rowY = -14f;
        playerInfoCoinsText = PlaceProfileField(assetBody, "金幣", "CoinsText", ref rowY, profileTwoLineRowH, 21f);
        playerInfoDeckSummaryText = PlaceProfileField(assetBody, "牌組", "DeckSummaryText", ref rowY, profileDeckBlockRowH, 19f, wrapValue: true, valueLineSpacing: 6f);
        playerInfoHeroSummaryText = PlaceProfileField(assetBody, "英雄", "HeroSummaryText", ref rowY, profileTwoLineRowH, 19f);

        Transform recordBody = CreatePlayerInfoSection(scrollContent, "對戰紀錄", 300f);
        rowY = -12f;
        playerInfoLastResultText = PlaceProfileField(recordBody, "最近結果", "LastResultText", ref rowY, profileTwoLineRowH, 19f);
        BuildPlayerInfoRecordPanel(recordBody, ref rowY);

        FinalizePlayerInfoScrollContent();

        playerInfoOverlayRoot = root;
        playerInfoOverlayRoot.SetActive(false);
        builtPlayerInfoLayoutVersion = PlayerInfoOverlayLayoutVersion;
    }

    private void DestroyPlayerInfoOverlayIfAny()
    {
        if (playerInfoOverlayRoot != null)
            Destroy(playerInfoOverlayRoot);

        playerInfoOverlayRoot = null;
        playerInfoUuidText = null;
        playerInfoRoleText = null;
        playerInfoStartDateText = null;
        playerInfoCoinsText = null;
        playerInfoDeckSummaryText = null;
        playerInfoHeroSummaryText = null;
        playerInfoLastResultText = null;
        playerInfoProgressText = null;
        playerInfoRecordTotalText = null;
        playerInfoScrollContentRt = null;
        playerSlotNameInput = null;
        playerInfoRecordColumns = null;
        builtPlayerInfoLayoutVersion = 0;
    }

    private static string FormatDeckSummaryForDisplay(string decks)
    {
        if (string.IsNullOrWhiteSpace(decks)) return "-";
        string[] parts = decks.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return decks.Trim();
        for (int i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim();
        return string.Join("\n", parts);
    }

    private static void FitProfileValueTextHeight(TextMeshProUGUI valueField, float minHeight, float perLineHeight)
    {
        if (valueField == null) return;
        valueField.ForceMeshUpdate();
        int lineCount = valueField.textInfo != null ? Mathf.Max(1, valueField.textInfo.lineCount) : 1;
        float height = Mathf.Max(minHeight, lineCount * perLineHeight);
        RectTransform rt = valueField.rectTransform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private Transform CreatePlayerInfoScrollArea(Transform panel, float panelWidth, float headerHeight, float footerHeight)
    {
        GameObject scrollRoot = new GameObject("ProfileScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollRoot.transform.SetParent(panel, false);
        RectTransform scrollRt = scrollRoot.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(PlayerInfoPadH, footerHeight + 8f);
        scrollRt.offsetMax = new Vector2(-PlayerInfoPadH, -headerHeight - 4f);
        scrollRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollRoot.transform, false);
        RectTransform viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        playerInfoScrollContentRt = content.GetComponent<RectTransform>();
        playerInfoScrollContentRt.anchorMin = new Vector2(0f, 1f);
        playerInfoScrollContentRt.anchorMax = new Vector2(1f, 1f);
        playerInfoScrollContentRt.pivot = new Vector2(0.5f, 1f);
        playerInfoScrollContentRt.anchoredPosition = Vector2.zero;
        playerInfoScrollContentRt.sizeDelta = new Vector2(0f, 900f);

        ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = playerInfoScrollContentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        return content.transform;
    }

    private void FinalizePlayerInfoScrollContent()
    {
        if (playerInfoScrollContentRt == null) return;
        float totalHeight = Mathf.Max(480f, -playerInfoLayoutY + 40f);
        playerInfoScrollContentRt.sizeDelta = new Vector2(0f, totalHeight);
    }

    private Transform CreatePlayerInfoSection(Transform contentRoot, string title, float sectionHeight)
    {
        float sectionWidth = playerInfoContentWidth;
        GameObject section = new GameObject(title + "Section", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(contentRoot, false);
        RectTransform sectionRt = section.GetComponent<RectTransform>();
        sectionRt.anchorMin = new Vector2(0f, 1f);
        sectionRt.anchorMax = new Vector2(1f, 1f);
        sectionRt.pivot = new Vector2(0.5f, 1f);
        sectionRt.anchoredPosition = new Vector2(0f, playerInfoLayoutY);
        sectionRt.sizeDelta = new Vector2(0f, sectionHeight);
        section.GetComponent<Image>().color = PlayerInfoSectionBg;

        GameObject titleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(section.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(16f, -10f);
        titleRt.sizeDelta = new Vector2(sectionWidth - 32f, 30f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(titleTmp);
        titleTmp.text = title;
        titleTmp.fontSize = 22f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = PlayerInfoSectionTitle;
        titleTmp.alignment = TextAlignmentOptions.Left;

        GameObject body = new GameObject("Body", typeof(RectTransform), typeof(RectMask2D));
        body.transform.SetParent(section.transform, false);
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(16f, 12f);
        bodyRt.offsetMax = new Vector2(-16f, -40f);

        playerInfoLayoutY -= sectionHeight + PlayerInfoSectionGap;
        return body.transform;
    }

    private TextMeshProUGUI PlaceProfileField(
        Transform parent,
        string label,
        string valueObjectName,
        ref float rowY,
        float rowHeight,
        float valueFontSize,
        bool wrapValue = false,
        float valueLineSpacing = 2f)
    {
        const float labelHeight = 22f;
        float valueHeight = Mathf.Max(26f, rowHeight - labelHeight - 4f);

        CreateProfileTextLine(parent, valueObjectName + "_Label", ref rowY, labelHeight, 17f, PlayerInfoTextMuted, label, false, false, 0f);
        TextMeshProUGUI valueTmp = CreateProfileTextLine(
            parent,
            valueObjectName,
            ref rowY,
            valueHeight,
            valueFontSize,
            PlayerInfoTextPrimary,
            string.Empty,
            wrapValue,
            false,
            valueLineSpacing);
        rowY -= PlayerInfoLineGap;
        return valueTmp;
    }

    private TextMeshProUGUI CreateProfileTextLine(
        Transform parent,
        string name,
        ref float rowY,
        float lineHeight,
        float fontSize,
        Color color,
        string text,
        bool wrap,
        bool richText,
        float lineSpacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, rowY);
        rt.sizeDelta = new Vector2(0f, lineHeight);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(tmp);
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.richText = richText;
        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.lineSpacing = lineSpacing;
        tmp.paragraphSpacing = wrap ? 4f : 0f;
        tmp.text = text;
        rowY -= lineHeight;
        return tmp;
    }

    private void CreatePlayerInfoSlotNameRow(Transform parent, ref float rowY)
    {
        float rowHeight = 48f;
        GameObject row = new GameObject("SlotNameRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, rowY);
        rowRt.sizeDelta = new Vector2(0f, rowHeight);

        GameObject slotNameLabel = new GameObject("SlotNameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotNameLabel.transform.SetParent(row.transform, false);
        RectTransform slotNameLabelRt = slotNameLabel.GetComponent<RectTransform>();
        slotNameLabelRt.anchorMin = new Vector2(0f, 1f);
        slotNameLabelRt.anchorMax = new Vector2(0f, 1f);
        slotNameLabelRt.pivot = new Vector2(0f, 1f);
        slotNameLabelRt.anchoredPosition = Vector2.zero;
        slotNameLabelRt.sizeDelta = new Vector2(88f, rowHeight);
        TextMeshProUGUI slotNameLabelTmp = slotNameLabel.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(slotNameLabelTmp);
        slotNameLabelTmp.fontSize = 17f;
        slotNameLabelTmp.alignment = TextAlignmentOptions.Left;
        slotNameLabelTmp.color = PlayerInfoTextMuted;
        slotNameLabelTmp.text = "槽位名稱";

        GameObject inputBgObj = new GameObject("SlotNameInputBg", typeof(RectTransform), typeof(Image));
        inputBgObj.transform.SetParent(row.transform, false);
        RectTransform inputBgRt = inputBgObj.GetComponent<RectTransform>();
        inputBgRt.anchorMin = new Vector2(0f, 1f);
        inputBgRt.anchorMax = new Vector2(1f, 1f);
        inputBgRt.pivot = new Vector2(0f, 1f);
        inputBgRt.anchoredPosition = new Vector2(96f, 0f);
        inputBgRt.sizeDelta = new Vector2(-280f, rowHeight);
        Image inputBg = inputBgObj.GetComponent<Image>();
        inputBg.color = Color.white;

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
        ApplyPlayerInfoFont(placeholder);
        placeholder.fontSize = 20f;
        placeholder.color = new Color(0.55f, 0.5f, 0.45f, 0.8f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.richText = false;
        placeholder.text = "輸入名稱";

        GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inputTextObj.transform.SetParent(viewportObj.transform, false);
        RectTransform inputTextRt = inputTextObj.GetComponent<RectTransform>();
        inputTextRt.anchorMin = Vector2.zero;
        inputTextRt.anchorMax = Vector2.one;
        inputTextRt.offsetMin = Vector2.zero;
        inputTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI inputText = inputTextObj.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(inputText);
        inputText.fontSize = 20f;
        inputText.color = PlayerInfoTextPrimary;
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
            row.transform,
            "SaveSlotNameButton",
            "儲存",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(120f, rowHeight),
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

        rowY -= rowHeight + PlayerInfoLineGap;
    }

    private void BuildPlayerInfoRecordPanel(Transform recordBody, ref float rowY)
    {
        const float barHeight = 44f;
        const float columnAreaHeight = 118f;
        const float summaryHeight = 36f;
        float filterTop = rowY;

        GameObject filterBar = new GameObject("RecordFilterBar", typeof(RectTransform), typeof(Image));
        filterBar.transform.SetParent(recordBody, false);
        RectTransform filterBarRt = filterBar.GetComponent<RectTransform>();
        filterBarRt.anchorMin = new Vector2(0f, 1f);
        filterBarRt.anchorMax = new Vector2(1f, 1f);
        filterBarRt.pivot = new Vector2(0.5f, 1f);
        filterBarRt.anchoredPosition = new Vector2(0f, filterTop);
        filterBarRt.sizeDelta = new Vector2(0f, barHeight);
        filterBar.GetComponent<Image>().color = new Color(0.72f, 0.80f, 0.86f, 0.55f);

        int filterCount = PlayerInfoRecordFilterLabels.Length;
        for (int i = 0; i < filterCount; i++)
        {
            int filterCode = PlayerInfoRecordFilterCodes[i];
            string tabLabel = PlayerInfoRecordFilterLabels[i];
            float minX = (float)i / filterCount;
            float maxX = (float)(i + 1) / filterCount;

            GameObject tabObj = new GameObject("Filter_" + tabLabel, typeof(RectTransform), typeof(Image), typeof(Button));
            tabObj.transform.SetParent(filterBar.transform, false);
            RectTransform tabRt = tabObj.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(minX, 0f);
            tabRt.anchorMax = new Vector2(maxX, 1f);
            tabRt.offsetMin = new Vector2(5f, 5f);
            tabRt.offsetMax = new Vector2(-5f, -5f);

            Image tabBg = tabObj.GetComponent<Image>();
            tabBg.color = new Color(1f, 1f, 1f, 0f);
            Button tabBtn = tabObj.GetComponent<Button>();
            tabBtn.onClick.AddListener(() =>
            {
                playerInfoActiveRecordFilter = filterCode;
                RefreshPlayerInfoOverlayContent();
            });

            GameObject tabLabelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            tabLabelObj.transform.SetParent(tabObj.transform, false);
            RectTransform tabLabelRt = tabLabelObj.GetComponent<RectTransform>();
            tabLabelRt.anchorMin = Vector2.zero;
            tabLabelRt.anchorMax = Vector2.one;
            tabLabelRt.offsetMin = Vector2.zero;
            tabLabelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI tabTmp = tabLabelObj.GetComponent<TextMeshProUGUI>();
            if (navLabelFont != null) tabTmp.font = navLabelFont;
            tabTmp.text = tabLabel;
            tabTmp.fontSize = 24f;
            tabTmp.fontStyle = FontStyles.Bold;
            tabTmp.alignment = TextAlignmentOptions.Center;
            tabTmp.color = new Color(0.92f, 0.95f, 0.98f, 1f);
            tabTmp.raycastTarget = false;

            playerInfoRecordFilterButtons[i] = tabBtn;
            playerInfoRecordFilterButtonBgs[i] = tabBg;
            playerInfoRecordFilterLabels[i] = tabTmp;
        }

        rowY -= barHeight + 14f;
        float columnTop = rowY;

        GameObject columnsRoot = new GameObject("DifficultyColumns", typeof(RectTransform));
        columnsRoot.transform.SetParent(recordBody, false);
        RectTransform columnsRt = columnsRoot.GetComponent<RectTransform>();
        columnsRt.anchorMin = new Vector2(0f, 1f);
        columnsRt.anchorMax = new Vector2(1f, 1f);
        columnsRt.pivot = new Vector2(0.5f, 1f);
        columnsRt.anchoredPosition = new Vector2(0f, columnTop);
        columnsRt.sizeDelta = new Vector2(0f, columnAreaHeight);

        int columnCount = PlayerProfileCsvService.StandardDifficultyLabelsZh.Length;
        playerInfoRecordColumns = new PlayerInfoRecordColumnUi[columnCount];

        for (int i = 0; i < columnCount; i++)
        {
            string diffLabel = PlayerProfileCsvService.StandardDifficultyLabelsZh[i];
            float minX = (float)i / columnCount;
            float maxX = (float)(i + 1) / columnCount;

            GameObject colObj = new GameObject("Col_" + diffLabel, typeof(RectTransform));
            colObj.transform.SetParent(columnsRoot.transform, false);
            RectTransform colRt = colObj.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(minX, 0f);
            colRt.anchorMax = new Vector2(maxX, 1f);
            colRt.offsetMin = new Vector2(3f, 0f);
            colRt.offsetMax = new Vector2(-3f, 0f);

            GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(colObj.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.06f, 1f);
            badgeRt.anchorMax = new Vector2(0.94f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -4f);
            badgeRt.sizeDelta = new Vector2(0f, 36f);
            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.color = PlayerInfoDifficultyBadgeColors[i];

            GameObject badgeTextObj = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            RectTransform badgeTextRt = badgeTextObj.GetComponent<RectTransform>();
            badgeTextRt.anchorMin = Vector2.zero;
            badgeTextRt.anchorMax = Vector2.one;
            badgeTextRt.offsetMin = Vector2.zero;
            badgeTextRt.offsetMax = Vector2.zero;
            TextMeshProUGUI badgeTmp = badgeTextObj.GetComponent<TextMeshProUGUI>();
            if (navLabelFont != null) badgeTmp.font = navLabelFont;
            badgeTmp.text = diffLabel;
            badgeTmp.fontSize = 18f;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = Color.white;
            badgeTmp.raycastTarget = false;

            GameObject countObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(colObj.transform, false);
            RectTransform countRt = countObj.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0f, 1f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot = new Vector2(0.5f, 1f);
            countRt.anchoredPosition = new Vector2(0f, -50f);
            countRt.sizeDelta = new Vector2(0f, 44f);
            TextMeshProUGUI countTmp = countObj.GetComponent<TextMeshProUGUI>();
            if (navLabelFont != null) countTmp.font = navLabelFont;
            countTmp.text = "0";
            countTmp.fontSize = 26f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.color = new Color(0.35f, 0.38f, 0.42f, 1f);
            countTmp.raycastTarget = false;

            playerInfoRecordColumns[i] = new PlayerInfoRecordColumnUi
            {
                badgeImage = badgeImg,
                badgeText = badgeTmp,
                countText = countTmp
            };
        }

        rowY -= columnAreaHeight + 12f;
        playerInfoRecordTotalText = CreateProfileTextLine(
            recordBody,
            "RecordFilterTotal",
            ref rowY,
            summaryHeight,
            17f,
            PlayerInfoTextMuted,
            string.Empty,
            true,
            false,
            2f);
        rowY -= PlayerInfoLineGap;
    }

    private void RefreshPlayerInfoRecordPanel(PlayerProfileCsvService.PlayerProfile p)
    {
        if (playerInfoRecordColumns == null || playerInfoRecordColumns.Length == 0) return;

        int[] counts = PlayerProfileCsvService.GetDifficultyCountsForResult(p, playerInfoActiveRecordFilter);
        int total = PlayerProfileCsvService.SumCounts(counts);
        for (int i = 0; i < playerInfoRecordColumns.Length && i < counts.Length; i++)
        {
            if (playerInfoRecordColumns[i]?.countText != null)
                playerInfoRecordColumns[i].countText.text = Mathf.Max(0, counts[i]).ToString();
        }

        for (int i = 0; i < PlayerInfoRecordFilterCodes.Length; i++)
        {
            bool active = PlayerInfoRecordFilterCodes[i] == playerInfoActiveRecordFilter;
            if (playerInfoRecordFilterButtonBgs[i] != null)
                playerInfoRecordFilterButtonBgs[i].color = active ? Color.white : new Color(1f, 1f, 1f, 0f);
            if (playerInfoRecordFilterLabels[i] != null)
                playerInfoRecordFilterLabels[i].color = active
                    ? new Color(0.22f, 0.26f, 0.30f, 1f)
                    : new Color(0.92f, 0.95f, 0.98f, 1f);
        }

        if (playerInfoRecordTotalText != null)
            playerInfoRecordTotalText.text = PlayerProfileCsvService.BuildBattleRecordPanelSummary(p, playerInfoActiveRecordFilter);
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

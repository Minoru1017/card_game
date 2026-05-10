using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class SceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    public string battleSceneName = "BattleSimulation";
    public string persistentSceneName = "Persistent";
    [Header("Deck Check")]
    public PlayerData playerData;
    public CardStore cardStore;
    public Button enterBattleButton;
    public Text noDeckHintLegacyText;
    public TextMeshProUGUI noDeckHintTMPText;

    [Header("Battle Preview (80% modal)")]
    [Tooltip("Pre-battle estimate: enemy uses fixed pool and cycles to player deck size.")]
    [SerializeField] private bool previewUseFixedEnemyDeck = true;
    [SerializeField] private int[] previewFixedEnemyDeckCardIds = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 19, 22, -1, -2, -3 };
    [Range(0, 30)] [SerializeField] private int previewEnemyOverLimitAllowance = 6;
    [Range(0, 20)] [SerializeField] private int previewMinEnemySpellsInDeck = 2;

    private const string NoDeckHintMessage = "尚未組建牌組";
    private GameObject battlePreviewOverlayRoot;
    private TextMeshProUGUI battlePreviewPressureText;
    private TextMeshProUGUI battlePreviewDeckText;
    private TextMeshProUGUI battlePreviewDeckSummaryText;
    private readonly List<Image> battlePreviewMetricBarFills = new List<Image>(3);
    private readonly List<TextMeshProUGUI> battlePreviewMetricValueTexts = new List<TextMeshProUGUI>(3);
    private Coroutine battlePreviewMetricAnimRoutine;
    private Button battlePreviewStartButton;
    private Button battlePreviewGiveUpButton;
    private TMP_FontAsset battlePreviewFontAsset;
    [Header("Runtime UI Sprite Fallback")]
    [Tooltip("Optional sliced sprite for runtime-generated modal/button backgrounds. Leave empty to use plain rectangle.")]
    [SerializeField] private Sprite runtimeRoundedUiSprite;
    private ScrollRect battlePreviewDeckScrollRect;
    private Button battlePreviewDifficultyToggleButton;
    private RectTransform battlePreviewDifficultyRowRt;
    private readonly List<Button> battlePreviewDifficultyButtons = new List<Button>(5);

    private enum BattleDifficultyTier
    {
        Intro,
        Easy,
        Normal,
        Hard,
        Boss
    }

    [Serializable]
    private class DifficultyDesignProfile
    {
        public BattleDifficultyTier tier = BattleDifficultyTier.Normal;
        public string labelZh = "普通";
        public bool locked;
        [Header("Design Index (0-100)")]
        [Range(0, 100)] public int deckStrengthIndex = 50;
        [Range(0, 100)] public int spellRatioIndex = 50;
        [Range(0, 100)] public int overLimitToleranceIndex = 50;
        [Header("Manual Tuning")]
        public bool useFixedDeck = true;
        [Range(0.2f, 1f)] public float revealRatio = 0.5f;
        [Range(0.6f, 1.4f)] public float scoreMultiplier = 1f;
        [Range(-10, 10)] public int overLimitBias;
        [Range(-4, 4)] public int minSpellsBias;
        public int[] fixedDeckOverride;
    }

    [SerializeField] private BattleDifficultyTier selectedDifficultyTier = BattleDifficultyTier.Normal;
    [SerializeField] private bool difficultyButtonsExpanded = true;
    [Header("Difficulty Design Profiles (Designer-tunable)")]
    [SerializeField] private List<DifficultyDesignProfile> difficultyProfiles = new List<DifficultyDesignProfile>
    {
        new DifficultyDesignProfile
        {
            tier = BattleDifficultyTier.Intro,
            labelZh = "入門",
            deckStrengthIndex = 8,
            spellRatioIndex = 8,
            overLimitToleranceIndex = 5,
            revealRatio = 1f,
            scoreMultiplier = 0.72f,
            minSpellsBias = -1,
            overLimitBias = -1,
            useFixedDeck = true
        },
        new DifficultyDesignProfile
        {
            tier = BattleDifficultyTier.Easy,
            labelZh = "簡單",
            deckStrengthIndex = 24,
            spellRatioIndex = 18,
            overLimitToleranceIndex = 20,
            revealRatio = 0.68f,
            scoreMultiplier = 0.82f,
            minSpellsBias = -1,
            overLimitBias = -1,
            useFixedDeck = true
        },
        new DifficultyDesignProfile
        {
            tier = BattleDifficultyTier.Normal,
            labelZh = "普通",
            deckStrengthIndex = 50,
            spellRatioIndex = 45,
            overLimitToleranceIndex = 45,
            revealRatio = 0.50f,
            scoreMultiplier = 1f,
            useFixedDeck = true
        },
        new DifficultyDesignProfile
        {
            tier = BattleDifficultyTier.Hard,
            labelZh = "困難",
            locked = false,
            deckStrengthIndex = 70,
            spellRatioIndex = 65,
            overLimitToleranceIndex = 70,
            revealRatio = 0.42f,
            scoreMultiplier = 1.14f,
            useFixedDeck = true
        },
        new DifficultyDesignProfile
        {
            tier = BattleDifficultyTier.Boss,
            labelZh = "魔王",
            locked = true,
            deckStrengthIndex = 85,
            spellRatioIndex = 80,
            overLimitToleranceIndex = 90,
            revealRatio = 0.35f,
            scoreMultiplier = 1.28f,
            useFixedDeck = true
        }
    };
    private bool pendingUseFixedEnemyDeck;
    private int[] pendingFixedEnemyDeckCardIds;
    private int pendingEnemyOverLimitAllowance;
    private int pendingMinEnemySpellsInDeck;

    private void Start()
    {
        RefreshEnterBattleState();
    }

    private void OnEnable()
    {
        RefreshEnterBattleState();
    }

    // Bind this method to the "進入對戰" button OnClick.
    public void EnterBattle()
    {
        if (playerData == null) playerData = UnityEngine.Object.FindFirstObjectByType<PlayerData>();
        if (playerData != null) playerData.LoadPlayerData(); // always use latest saved deck

        RefreshEnterBattleState();
        if (!HasBuiltDeck())
        {
            ShowNoDeckHint(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogError("SceneLoader: battleSceneName is empty.");
            return;
        }

        ShowBattlePreviewModal();
    }

    private void StartBattleSceneLoad()
    {
        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogError("SceneLoader: battleSceneName is empty.");
            return;
        }
        SceneManager.sceneLoaded -= OnSceneLoadedFixup;
        SceneManager.sceneLoaded += OnSceneLoadedFixup;
        SceneManager.LoadScene(battleSceneName);
    }

    // Bind this method to the "前往 Persistent" button OnClick.
    public void EnterPersistent()
    {
        if (string.IsNullOrWhiteSpace(persistentSceneName))
        {
            Debug.LogError("SceneLoader: persistentSceneName is empty.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(persistentSceneName))
        {
            Debug.LogError("SceneLoader: persistent scene not in Build Settings -> " + persistentSceneName);
            return;
        }

        SceneManager.LoadScene(persistentSceneName);
    }

    public void RefreshEnterBattleState()
    {
        if (playerData == null) playerData = UnityEngine.Object.FindFirstObjectByType<PlayerData>();
        if (playerData != null) playerData.LoadPlayerData();

        bool hasDeck = HasBuiltDeck();
        // IMPORTANT: only control explicitly assigned battle button.
        // Avoid auto-grabbing current GameObject button, which can disable unrelated buttons.
        if (enterBattleButton != null) enterBattleButton.interactable = hasDeck;
        ShowNoDeckHint(!hasDeck);
    }

    private bool HasBuiltDeck()
    {
        if (playerData == null) return false;
        return playerData.GetSelectedDeckTotalCount() > 0;
    }

    private void ShowNoDeckHint(bool show)
    {
        if (noDeckHintLegacyText != null)
        {
            noDeckHintLegacyText.gameObject.SetActive(show);
            if (show) noDeckHintLegacyText.text = NoDeckHintMessage;
        }
        if (noDeckHintTMPText != null)
        {
            noDeckHintTMPText.gameObject.SetActive(show);
            if (show) noDeckHintTMPText.text = NoDeckHintMessage;
        }
    }

    private void OnSceneLoadedFixup(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != battleSceneName) return;
        SceneManager.sceneLoaded -= OnSceneLoadedFixup;

        // Force-enable canvases and normalize scale.
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            c.gameObject.SetActive(true);
            c.enabled = true;
            c.transform.localScale = Vector3.one;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.enabled = true;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographic = true;
        }

        // Ensure battle manager exists to initialize UI/combat.
        BattleSimulationManager manager = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("BattleManager");
            manager = go.AddComponent<BattleSimulationManager>();
            manager.autoStartOnPlay = true;
            Debug.LogWarning("SceneLoader: Auto-created BattleManager in battle scene.");
        }
        if (manager != null)
        {
            manager.QueueRuntimeDifficultyConfig(
                pendingUseFixedEnemyDeck,
                pendingFixedEnemyDeckCardIds,
                pendingEnemyOverLimitAllowance,
                pendingMinEnemySpellsInDeck);
        }
    }

    private void ShowBattlePreviewModal()
    {
        EnsureBattlePreviewUi();
        RefreshBattlePreviewBodyText();
        if (battlePreviewOverlayRoot != null)
        {
            battlePreviewOverlayRoot.transform.SetAsLastSibling();
            battlePreviewOverlayRoot.SetActive(true);
        }
    }

    private void HideBattlePreviewModal()
    {
        if (battlePreviewOverlayRoot != null) battlePreviewOverlayRoot.SetActive(false);
    }

    private void OnBattlePreviewStartClicked()
    {
        BattleDifficultyConfig cfg = BuildDifficultyConfig(selectedDifficultyTier);
        pendingUseFixedEnemyDeck = cfg.UseFixedDeck;
        pendingFixedEnemyDeckCardIds = cfg.FixedDeckIds;
        pendingEnemyOverLimitAllowance = cfg.OverLimitAllowance;
        pendingMinEnemySpellsInDeck = cfg.MinSpellsInDeck;
        HideBattlePreviewModal();
        StartBattleSceneLoad();
    }

    private void OnBattlePreviewGiveUpClicked()
    {
        HideBattlePreviewModal();
    }

    private void EnsureBattlePreviewUi()
    {
        if (battlePreviewOverlayRoot != null) return;

        Canvas parentCanvas = null;
        if (enterBattleButton != null) parentCanvas = enterBattleButton.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (c == null || c.gameObject == null) continue;
                if (!c.gameObject.scene.IsValid()) continue; // Skip DontDestroyOnLoad global overlays.
                if (string.Equals(c.gameObject.name, "GlobalNavCanvas", StringComparison.Ordinal)) continue;
                parentCanvas = c;
                break;
            }
        }
        if (parentCanvas == null)
        {
            Debug.LogError("SceneLoader: cannot build battle preview modal (no Canvas found).");
            return;
        }

        GameObject overlay = new GameObject("BattlePreviewOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(parentCanvas.transform, false);
        // Buildbeck「解散牌組」可能掛巢狀 Canvas（sortingOrder = 父階 +30）；預覽浮窗必須更高才不會被蓋住。
        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = parentCanvas.sortingOrder + 80;
        overlay.AddComponent<GraphicRaycaster>();
        overlay.transform.SetAsLastSibling();

        RectTransform overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        Image overlayBg = overlay.GetComponent<Image>();
        // Soft warm dimmer to match Buildbeck's cozy palette.
        overlayBg.color = new Color(0.18f, 0.12f, 0.1f, 0.56f);
        overlayBg.raycastTarget = true;

        GameObject panelObj = new GameObject("BattlePreviewPanel", typeof(RectTransform), typeof(Image), typeof(Mask));
        panelObj.transform.SetParent(overlay.transform, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(Screen.width * 0.8f, Screen.height * 0.8f);
        Image panelBg = panelObj.GetComponent<Image>();
        if (runtimeRoundedUiSprite != null) panelBg.sprite = runtimeRoundedUiSprite;
        panelBg.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        panelBg.raycastTarget = true;
        panelBg.type = runtimeRoundedUiSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        Mask panelMask = panelObj.GetComponent<Mask>();
        panelMask.showMaskGraphic = true; // Keep panel background visible while clipping child corners.

        GameObject panelBorderObj = new GameObject("PanelBorder", typeof(RectTransform), typeof(Outline));
        panelBorderObj.transform.SetParent(panelObj.transform, false);
        RectTransform panelBorderRt = panelBorderObj.GetComponent<RectTransform>();
        panelBorderRt.anchorMin = Vector2.zero;
        panelBorderRt.anchorMax = Vector2.one;
        panelBorderRt.offsetMin = Vector2.zero;
        panelBorderRt.offsetMax = Vector2.zero;
        Outline panelOutline = panelBorderObj.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.49f, 0.41f, 0.27f, 0.82f);
        panelOutline.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject titleBarObj = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleBarObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleBarRt = titleBarObj.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0f, 1f);
        titleBarRt.anchorMax = new Vector2(1f, 1f);
        titleBarRt.pivot = new Vector2(0.5f, 1f);
        titleBarRt.offsetMin = new Vector2(0f, -94f);
        titleBarRt.offsetMax = new Vector2(0f, 0f);
        Image titleBarImg = titleBarObj.GetComponent<Image>();
        titleBarImg.color = new Color(0.52f, 0.59f, 0.44f, 1f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(24f, -78f);
        titleRt.offsetMax = new Vector2(-24f, -12f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "戰前預覽";
        battlePreviewFontAsset = ResolvePreviewFontAsset();
        if (battlePreviewFontAsset != null) titleTmp.font = battlePreviewFontAsset;
        titleTmp.fontSize = 38f;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.2f, 0.17f, 0.12f, 1f);

        GameObject topDividerObj = new GameObject("TopDivider", typeof(RectTransform), typeof(Image));
        topDividerObj.transform.SetParent(panelObj.transform, false);
        RectTransform topDividerRt = topDividerObj.GetComponent<RectTransform>();
        topDividerRt.anchorMin = new Vector2(0f, 1f);
        topDividerRt.anchorMax = new Vector2(1f, 1f);
        topDividerRt.pivot = new Vector2(0.5f, 1f);
        topDividerRt.offsetMin = new Vector2(24f, -98f);
        topDividerRt.offsetMax = new Vector2(-24f, -96f);
        Image topDividerImg = topDividerObj.GetComponent<Image>();
        topDividerImg.color = new Color(0.65f, 0.56f, 0.38f, 0.92f);

        GameObject chipRowObj = new GameObject("HeaderChipRow", typeof(RectTransform));
        chipRowObj.transform.SetParent(panelObj.transform, false);
        RectTransform chipRowRt = chipRowObj.GetComponent<RectTransform>();
        chipRowRt.anchorMin = new Vector2(1f, 1f);
        chipRowRt.anchorMax = new Vector2(1f, 1f);
        chipRowRt.pivot = new Vector2(1f, 1f);
        chipRowRt.anchoredPosition = new Vector2(-34f, -102f);
        chipRowRt.sizeDelta = new Vector2(538f, 28f);
        CreateHeaderChip(chipRowObj.transform, "ChipThreat", "戰況摘要", new Vector2(0f, 0f), 170f);
        CreateHeaderChip(chipRowObj.transform, "ChipDeck", "敵方情報", new Vector2(184f, 0f), 170f);
        CreateHeaderChip(chipRowObj.transform, "ChipDesign", "戰前準備", new Vector2(368f, 0f), 170f);

        GameObject pressureBlockObj = new GameObject("PressureBlock", typeof(RectTransform), typeof(Image));
        pressureBlockObj.transform.SetParent(panelObj.transform, false);
        RectTransform pressureRt = pressureBlockObj.GetComponent<RectTransform>();
        pressureRt.anchorMin = new Vector2(0f, 0f);
        pressureRt.anchorMax = new Vector2(0.34f, 1f);
        pressureRt.offsetMin = new Vector2(34f, 132f);
        pressureRt.offsetMax = new Vector2(-16f, -132f);
        Image pressureBg = pressureBlockObj.GetComponent<Image>();
        pressureBg.color = new Color(0.88f, 0.92f, 0.83f, 1f);

        GameObject pressureInnerCardObj = new GameObject("PressureInnerCard", typeof(RectTransform), typeof(Image));
        pressureInnerCardObj.transform.SetParent(pressureBlockObj.transform, false);
        RectTransform pressureInnerRt = pressureInnerCardObj.GetComponent<RectTransform>();
        pressureInnerRt.anchorMin = new Vector2(0f, 0f);
        pressureInnerRt.anchorMax = new Vector2(1f, 1f);
        pressureInnerRt.offsetMin = new Vector2(10f, 10f);
        pressureInnerRt.offsetMax = new Vector2(-10f, -10f);
        Image pressureInnerBg = pressureInnerCardObj.GetComponent<Image>();
        pressureInnerBg.color = new Color(0.95f, 0.97f, 0.91f, 1f);

        GameObject deckBlockObj = new GameObject("DeckBlock", typeof(RectTransform), typeof(Image));
        deckBlockObj.transform.SetParent(panelObj.transform, false);
        RectTransform deckBlockRt = deckBlockObj.GetComponent<RectTransform>();
        deckBlockRt.anchorMin = new Vector2(0.34f, 0f);
        deckBlockRt.anchorMax = new Vector2(1f, 1f);
        deckBlockRt.offsetMin = new Vector2(26f, 132f);
        deckBlockRt.offsetMax = new Vector2(-34f, -132f);
        Image deckBg = deckBlockObj.GetComponent<Image>();
        deckBg.color = new Color(0.9f, 0.93f, 0.86f, 1f);

        GameObject deckSummaryCardObj = new GameObject("DeckSummaryCard", typeof(RectTransform), typeof(Image));
        deckSummaryCardObj.transform.SetParent(deckBlockObj.transform, false);
        RectTransform deckSummaryRt = deckSummaryCardObj.GetComponent<RectTransform>();
        deckSummaryRt.anchorMin = new Vector2(0f, 1f);
        deckSummaryRt.anchorMax = new Vector2(1f, 1f);
        deckSummaryRt.pivot = new Vector2(0.5f, 1f);
        deckSummaryRt.offsetMin = new Vector2(10f, -112f);
        deckSummaryRt.offsetMax = new Vector2(-10f, -10f);
        Image deckSummaryBg = deckSummaryCardObj.GetComponent<Image>();
        deckSummaryBg.color = new Color(0.94f, 0.96f, 0.9f, 1f);

        GameObject deckSummaryTmpObj = new GameObject("DeckSummaryLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        deckSummaryTmpObj.transform.SetParent(deckSummaryCardObj.transform, false);
        RectTransform deckSummaryTmpRt = deckSummaryTmpObj.GetComponent<RectTransform>();
        deckSummaryTmpRt.anchorMin = new Vector2(0f, 0f);
        deckSummaryTmpRt.anchorMax = new Vector2(1f, 1f);
        deckSummaryTmpRt.offsetMin = new Vector2(16f, 10f);
        deckSummaryTmpRt.offsetMax = new Vector2(-16f, -10f);
        TextMeshProUGUI deckSummaryTmp = deckSummaryTmpObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) deckSummaryTmp.font = battlePreviewFontAsset;
        deckSummaryTmp.fontSize = 26f;
        deckSummaryTmp.color = new Color(0.27f, 0.34f, 0.21f, 1f);
        deckSummaryTmp.alignment = TextAlignmentOptions.MidlineLeft;
        deckSummaryTmp.text = "揭露: 0/0 | 法術占比: 0%";
        battlePreviewDeckSummaryText = deckSummaryTmp;

        GameObject deckDetailCardObj = new GameObject("DeckDetailCard", typeof(RectTransform), typeof(Image));
        deckDetailCardObj.transform.SetParent(deckBlockObj.transform, false);
        RectTransform deckDetailRt = deckDetailCardObj.GetComponent<RectTransform>();
        deckDetailRt.anchorMin = new Vector2(0f, 0f);
        deckDetailRt.anchorMax = new Vector2(1f, 1f);
        deckDetailRt.offsetMin = new Vector2(10f, 10f);
        deckDetailRt.offsetMax = new Vector2(-10f, -116f);
        Image deckDetailBg = deckDetailCardObj.GetComponent<Image>();
        deckDetailBg.color = new Color(0.95f, 0.97f, 0.92f, 1f);

        GameObject centerDividerObj = new GameObject("CenterDivider", typeof(RectTransform), typeof(Image));
        centerDividerObj.transform.SetParent(panelObj.transform, false);
        RectTransform centerDividerRt = centerDividerObj.GetComponent<RectTransform>();
        centerDividerRt.anchorMin = new Vector2(0.34f, 0f);
        centerDividerRt.anchorMax = new Vector2(0.34f, 1f);
        centerDividerRt.pivot = new Vector2(0.5f, 0.5f);
        centerDividerRt.offsetMin = new Vector2(0f, 132f);
        centerDividerRt.offsetMax = new Vector2(2f, -132f);
        Image centerDividerImg = centerDividerObj.GetComponent<Image>();
        centerDividerImg.color = new Color(0.61f, 0.53f, 0.35f, 0.5f);

        battlePreviewPressureText = CreatePreviewBlockText(pressureInnerCardObj.transform, "PressureText");
        if (battlePreviewPressureText != null)
        {
            RectTransform pressureTextRt = battlePreviewPressureText.rectTransform;
            pressureTextRt.offsetMin = new Vector2(20f, 180f);
            pressureTextRt.offsetMax = new Vector2(-20f, -20f);
        }
        CreatePressureMetricsChart(pressureInnerCardObj.transform);
        battlePreviewDeckText = CreatePreviewScrollableText(deckDetailCardObj.transform, "DeckText");

        battlePreviewDifficultyToggleButton = CreateModalButton(panelObj.transform, "DifficultyToggleButton", "敵方難度");
        RectTransform diffToggleRt = battlePreviewDifficultyToggleButton.GetComponent<RectTransform>();
        diffToggleRt.anchorMin = new Vector2(0f, 0f);
        diffToggleRt.anchorMax = new Vector2(0f, 0f);
        diffToggleRt.pivot = new Vector2(0f, 0f);
        diffToggleRt.anchoredPosition = new Vector2(24f, 20f);
        diffToggleRt.sizeDelta = new Vector2(280f, 90f);
        battlePreviewDifficultyToggleButton.onClick.AddListener(ToggleDifficultyButtonsExpanded);

        GameObject diffRowObj = new GameObject("DifficultyRow", typeof(RectTransform));
        diffRowObj.transform.SetParent(panelObj.transform, false);
        battlePreviewDifficultyRowRt = diffRowObj.GetComponent<RectTransform>();
        battlePreviewDifficultyRowRt.anchorMin = new Vector2(0f, 0f);
        battlePreviewDifficultyRowRt.anchorMax = new Vector2(0f, 0f);
        battlePreviewDifficultyRowRt.pivot = new Vector2(0f, 0f);
        battlePreviewDifficultyRowRt.anchoredPosition = new Vector2(314f, 20f);
        battlePreviewDifficultyRowRt.sizeDelta = new Vector2(1020f, 90f);
        CreateDifficultyButtons(battlePreviewDifficultyRowRt);
        ApplyDifficultyButtonsExpandedState();

        battlePreviewStartButton = CreateModalButton(panelObj.transform, "StartBattleButton", "開始對戰");
        RectTransform startRt = battlePreviewStartButton.GetComponent<RectTransform>();
        startRt.anchorMin = new Vector2(1f, 0f);
        startRt.anchorMax = new Vector2(1f, 0f);
        startRt.pivot = new Vector2(1f, 0f);
        startRt.anchoredPosition = new Vector2(-24f, 20f);
        startRt.sizeDelta = new Vector2(280f, 90f);
        battlePreviewStartButton.onClick.AddListener(OnBattlePreviewStartClicked);

        battlePreviewGiveUpButton = CreateModalButton(panelObj.transform, "GiveUpButton", "放棄");
        RectTransform giveUpRt = battlePreviewGiveUpButton.GetComponent<RectTransform>();
        giveUpRt.anchorMin = new Vector2(1f, 0f);
        giveUpRt.anchorMax = new Vector2(1f, 0f);
        giveUpRt.pivot = new Vector2(1f, 0f);
        giveUpRt.anchoredPosition = new Vector2(-320f, 20f);
        giveUpRt.sizeDelta = new Vector2(280f, 90f);
        battlePreviewGiveUpButton.onClick.AddListener(OnBattlePreviewGiveUpClicked);

        battlePreviewOverlayRoot = overlay;
        battlePreviewOverlayRoot.SetActive(false);
    }

    private TextMeshProUGUI CreatePreviewBlockText(Transform parent, string objName)
    {
        GameObject bodyObj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(parent, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(20f, 20f);
        bodyRt.offsetMax = new Vector2(-20f, -20f);
        TextMeshProUGUI tmp = bodyObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) tmp.font = battlePreviewFontAsset;
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.2f, 0.17f, 0.12f, 1f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        tmp.lineSpacing = 8f;
        return tmp;
    }

    private TextMeshProUGUI CreatePreviewScrollableText(Transform parent, string objName)
    {
        GameObject scrollObj = new GameObject(objName + "ScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(20f, 20f);
        scrollRt.offsetMax = new Vector2(-20f, -20f);

        GameObject viewportObj = new GameObject(objName + "Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = new Vector2(0f, 0f);
        viewportRt.anchorMax = new Vector2(1f, 1f);
        viewportRt.offsetMin = new Vector2(0f, 0f);
        viewportRt.offsetMax = new Vector2(0f, 0f);

        GameObject contentObj = new GameObject(objName + "Content", typeof(RectTransform), typeof(TextMeshProUGUI));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        TextMeshProUGUI tmp = contentObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) tmp.font = battlePreviewFontAsset;
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.2f, 0.17f, 0.12f, 1f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        tmp.lineSpacing = 8f;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.scrollSensitivity = 24f;
        battlePreviewDeckScrollRect = scrollRect;

        // Match DeckManager scrollbar layout/feel for consistency.
        GameObject scrollbarObj = new GameObject("RuntimeVerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(viewportObj.transform, false);
        RectTransform barRt = scrollbarObj.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(1f, 0.5f);
        barRt.sizeDelta = new Vector2(18f, 0f);
        barRt.anchoredPosition = new Vector2(-1f, 0f);
        scrollbarObj.transform.SetAsLastSibling();
        Image barImg = scrollbarObj.GetComponent<Image>();
        barImg.color = new Color(0.42f, 0.31f, 0.28f, 0.45f);
        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(scrollbarObj.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0.75f);
        handleRt.anchorMax = new Vector2(1f, 1f);
        handleRt.offsetMin = new Vector2(2f, 2f);
        handleRt.offsetMax = new Vector2(-2f, -2f);
        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = new Color(0.96f, 0.92f, 0.85f, 0.95f);

        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRt;
        scrollbar.value = 1f;
        scrollbar.size = 0.2f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarSpacing = 0f;

        return tmp;
    }

    private Button CreateModalButton(Transform parent, string objName, string label)
    {
        GameObject buttonObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180f, 58f);
        Image img = buttonObj.GetComponent<Image>();
        if (runtimeRoundedUiSprite != null) img.sprite = runtimeRoundedUiSprite;
        // Buildbeck-like default button tone (wine red).
        img.color = new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);
        img.type = runtimeRoundedUiSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        Button btn = buttonObj.GetComponent<Button>();

        GameObject tmpObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        tmpObj.transform.SetParent(buttonObj.transform, false);
        RectTransform txtRt = tmpObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = tmpObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) txt.font = battlePreviewFontAsset;
        txt.text = label;
        txt.fontSize = 36f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        return btn;
    }

    private void CreateHeaderChip(Transform parent, string objName, string label, Vector2 anchoredPos, float width)
    {
        GameObject chipObj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        chipObj.transform.SetParent(parent, false);
        RectTransform chipRt = chipObj.GetComponent<RectTransform>();
        chipRt.anchorMin = new Vector2(0f, 1f);
        chipRt.anchorMax = new Vector2(0f, 1f);
        chipRt.pivot = new Vector2(0f, 1f);
        chipRt.anchoredPosition = anchoredPos;
        chipRt.sizeDelta = new Vector2(width, 28f);
        Image chipImg = chipObj.GetComponent<Image>();
        chipImg.color = new Color(0.9f, 0.93f, 0.86f, 0.98f);
        chipImg.type = Image.Type.Sliced;

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(chipObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(10f, 0f);
        txtRt.offsetMax = new Vector2(-10f, 0f);
        TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) txt.font = battlePreviewFontAsset;
        txt.text = label;
        txt.fontSize = 18f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.25f, 0.32f, 0.19f, 1f);
    }

    private void CreatePressureMetricsChart(Transform parent)
    {
        battlePreviewMetricBarFills.Clear();
        battlePreviewMetricValueTexts.Clear();

        GameObject chartObj = new GameObject("PressureMetricsChart", typeof(RectTransform));
        chartObj.transform.SetParent(parent, false);
        RectTransform chartRt = chartObj.GetComponent<RectTransform>();
        chartRt.anchorMin = new Vector2(0f, 0f);
        chartRt.anchorMax = new Vector2(1f, 0f);
        chartRt.pivot = new Vector2(0.5f, 0f);
        chartRt.anchoredPosition = new Vector2(0f, 12f);
        chartRt.sizeDelta = new Vector2(-26f, 206f);

        CreateOneMetricBar(chartRt, "Threat", "壓制", 0, new Color(0.50f, 0.82f, 0.50f, 1f));
        CreateOneMetricBar(chartRt, "Burst", "爆發", 1, new Color(1f, 0.80f, 0.42f, 1f));
        CreateOneMetricBar(chartRt, "Tempo", "節奏", 2, new Color(1f, 0.58f, 0.36f, 1f));
    }

    private void CreateOneMetricBar(RectTransform parent, string key, string label, int index, Color fillColor)
    {
        float blockW = 140f;
        float spacing = 18f;
        float x = index * (blockW + spacing);

        GameObject blockObj = new GameObject(key + "MetricBlock", typeof(RectTransform));
        blockObj.transform.SetParent(parent, false);
        RectTransform blockRt = blockObj.GetComponent<RectTransform>();
        blockRt.anchorMin = new Vector2(0f, 0f);
        blockRt.anchorMax = new Vector2(0f, 1f);
        blockRt.pivot = new Vector2(0f, 0f);
        blockRt.anchoredPosition = new Vector2(x, 0f);
        blockRt.sizeDelta = new Vector2(blockW, 0f);

        GameObject frameObj = new GameObject(key + "BarFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(blockObj.transform, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 0f);
        frameRt.anchorMax = new Vector2(0.5f, 1f);
        frameRt.pivot = new Vector2(0.5f, 0f);
        frameRt.anchoredPosition = new Vector2(0f, 30f);
        frameRt.sizeDelta = new Vector2(58f, -64f);
        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.color = new Color(0.82f, 0.78f, 0.68f, 1f);
        frameImg.type = Image.Type.Sliced;

        GameObject fillObj = new GameObject(key + "BarFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(frameObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 0f);
        fillRt.pivot = new Vector2(0.5f, 0f);
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, 4f);
        fillRt.sizeDelta = new Vector2(0f, 1f);
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Sliced;
        battlePreviewMetricBarFills.Add(fillImg);

        GameObject valueObj = new GameObject(key + "ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueObj.transform.SetParent(blockObj.transform, false);
        RectTransform valueRt = valueObj.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0f, 1f);
        valueRt.anchorMax = new Vector2(1f, 1f);
        valueRt.pivot = new Vector2(0.5f, 1f);
        valueRt.anchoredPosition = new Vector2(0f, 0f);
        valueRt.sizeDelta = new Vector2(0f, 32f);
        TextMeshProUGUI valueTmp = valueObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) valueTmp.font = battlePreviewFontAsset;
        valueTmp.text = "0";
        valueTmp.fontSize = 21f;
        valueTmp.alignment = TextAlignmentOptions.Center;
        valueTmp.color = new Color(0.24f, 0.2f, 0.15f, 1f);
        battlePreviewMetricValueTexts.Add(valueTmp);

        GameObject labelObj = new GameObject(key + "LabelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(blockObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 2f);
        labelRt.sizeDelta = new Vector2(0f, 28f);
        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) labelTmp.font = battlePreviewFontAsset;
        labelTmp.text = label;
        labelTmp.fontSize = 19f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = new Color(0.26f, 0.33f, 0.2f, 1f);
    }

    private void RefreshBattlePreviewBodyText()
    {
        if (battlePreviewPressureText == null || battlePreviewDeckText == null) return;
        if (playerData == null) playerData = UnityEngine.Object.FindFirstObjectByType<PlayerData>();
        if (playerData != null) playerData.LoadPlayerData();
        EnsureCardStoreLoadedForPreview();

        BattleDifficultyConfig cfg = BuildDifficultyConfig(selectedDifficultyTier);
        int playerDeckCount = playerData != null ? Mathf.Max(1, playerData.GetSelectedDeckTotalCount()) : 20;
        List<int> predictedDeck = BuildPredictedEnemyDeckKeys(playerDeckCount, cfg);
        int revealCount = Mathf.Clamp(Mathf.FloorToInt(predictedDeck.Count * cfg.RevealRatio), 0, predictedDeck.Count);

        float threat = Mathf.Clamp((30f + cfg.OverLimitAllowance * 5.5f + (cfg.UseFixedDeck ? 8f : 0f)) * cfg.ScoreMultiplier, 0f, 100f);
        float burst = Mathf.Clamp((24f + CountSpellCards(predictedDeck) * 3.8f + cfg.MinSpellsInDeck * 5f) * cfg.ScoreMultiplier, 0f, 100f);
        float tempo = Mathf.Clamp((28f + (predictedDeck.Count >= playerDeckCount ? 14f : 6f) + cfg.OverLimitAllowance * 2.8f) * cfg.ScoreMultiplier, 0f, 100f);
        string tier = (threat * 0.5f + burst * 0.3f + tempo * 0.2f) switch
        {
            >= 80f => "Severe",
            >= 62f => "High",
            >= 40f => "Medium",
            _ => "Low"
        };

        StringBuilder pressureSb = new StringBuilder(512);
        pressureSb.AppendLine("<size=120%><b>戰況摘要</b></size>");
        pressureSb.AppendLine($"<size=112%><b>整體壓力等級: {MapTierToZhSimple(tier)}</b></size>");
        pressureSb.AppendLine($"<color=#43573A>敵方難度: {cfg.LabelZh}</color>");
        pressureSb.AppendLine($"<color=#6C533D>設計指數 - 牌組:{cfg.DeckStrengthIndex} 法術:{cfg.SpellRatioIndex} 超標:{cfg.OverLimitToleranceIndex}</color>");
        battlePreviewPressureText.text = SafePreviewText(pressureSb.ToString());
        RefreshPressureMetricChart(threat, burst, tempo);

        StringBuilder deckSb = new StringBuilder(1024);
        deckSb.AppendLine("<size=120%><b>詳細情報</b></size>");
        deckSb.AppendLine("<size=112%><b>敵方牌組陣容</b></size>");
        deckSb.AppendLine("<color=#43573A>可能出現的牌種</color>");
        deckSb.AppendLine($"已揭露 <b>{revealCount}</b> / <b>{predictedDeck.Count}</b> 張");
        deckSb.AppendLine("<color=#6C533D>* 以下為戰前情報, 實戰可能因抽牌順序不同而變化.</color>");
        deckSb.AppendLine();

        Dictionary<int, int> revealCounts = new Dictionary<int, int>();
        for (int i = 0; i < revealCount; i++)
        {
            int key = predictedDeck[i];
            if (!revealCounts.ContainsKey(key)) revealCounts[key] = 0;
            revealCounts[key]++;
        }
        foreach (var kv in revealCounts)
        {
            deckSb.AppendLine(BuildEnemyDeckRowText(kv.Value, SafePreviewText(ResolveCardNameForPreview(kv.Key))));
        }
        int hiddenCount = predictedDeck.Count - revealCount;
        if (hiddenCount > 0)
            deckSb.AppendLine(BuildEnemyDeckRowText(hiddenCount, "??? (未揭露)"));

        battlePreviewDeckText.text = SafePreviewText(deckSb.ToString());
        if (battlePreviewDeckSummaryText != null)
        {
            int total = Mathf.Max(1, predictedDeck.Count);
            int spellCount = CountSpellCards(predictedDeck);
            int spellRatio = Mathf.Clamp(Mathf.RoundToInt((spellCount * 100f) / total), 0, 100);
            battlePreviewDeckSummaryText.text = $"揭露: <b>{revealCount}</b>/<b>{predictedDeck.Count}</b>   法術占比: <b>{spellRatio}%</b>";
        }
        UpdateDeckScrollContentHeight();
        if (battlePreviewDeckScrollRect != null)
            battlePreviewDeckScrollRect.verticalNormalizedPosition = 1f;
        RefreshDifficultyButtonVisuals();
    }

    private void UpdateDeckScrollContentHeight()
    {
        if (battlePreviewDeckText == null || battlePreviewDeckScrollRect == null) return;
        RectTransform contentRt = battlePreviewDeckText.rectTransform;
        RectTransform viewportRt = battlePreviewDeckScrollRect.viewport;
        if (contentRt == null || viewportRt == null) return;

        Canvas.ForceUpdateCanvases();
        battlePreviewDeckText.ForceMeshUpdate();
        float preferred = Mathf.Max(0f, battlePreviewDeckText.preferredHeight);
        float viewportH = Mathf.Max(0f, viewportRt.rect.height);

        // Keep a small bottom breathing space; never smaller than viewport.
        float targetH = Mathf.Max(viewportH + 4f, preferred + 28f);
        Vector2 size = contentRt.sizeDelta;
        size.y = targetH;
        contentRt.sizeDelta = size;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
    }

    private List<int> BuildPredictedEnemyDeckKeys(int targetCount, BattleDifficultyConfig cfg)
    {
        List<int> result = new List<int>(Mathf.Max(1, targetCount));
        int[] sourceDeck = cfg.FixedDeckIds != null && cfg.FixedDeckIds.Length > 0
            ? cfg.FixedDeckIds
            : previewFixedEnemyDeckCardIds;
        if (!cfg.UseFixedDeck || sourceDeck == null || sourceDeck.Length == 0)
            return result;
        for (int i = 0; i < targetCount; i++)
            result.Add(sourceDeck[i % sourceDeck.Length]);
        return result;
    }

    private static int CountSpellCards(List<int> keys)
    {
        int n = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (DeckCardId.IsSpellKey(keys[i])) n++;
        }
        return n;
    }

    private struct BattleDifficultyConfig
    {
        public string LabelZh;
        public bool UseFixedDeck;
        public int[] FixedDeckIds;
        public int OverLimitAllowance;
        public int MinSpellsInDeck;
        public float RevealRatio;
        public float ScoreMultiplier;
        public int DeckStrengthIndex;
        public int SpellRatioIndex;
        public int OverLimitToleranceIndex;

        public BattleDifficultyConfig(
            string labelZh,
            bool useFixedDeck,
            int[] fixedDeckIds,
            int overLimitAllowance,
            int minSpellsInDeck,
            float revealRatio,
            float scoreMultiplier,
            int deckStrengthIndex,
            int spellRatioIndex,
            int overLimitToleranceIndex)
        {
            LabelZh = labelZh;
            UseFixedDeck = useFixedDeck;
            FixedDeckIds = fixedDeckIds;
            OverLimitAllowance = overLimitAllowance;
            MinSpellsInDeck = minSpellsInDeck;
            RevealRatio = revealRatio;
            ScoreMultiplier = scoreMultiplier;
            DeckStrengthIndex = deckStrengthIndex;
            SpellRatioIndex = spellRatioIndex;
            OverLimitToleranceIndex = overLimitToleranceIndex;
        }
    }

    private BattleDifficultyConfig BuildDifficultyConfig(BattleDifficultyTier tier)
    {
        EnsureDifficultyProfiles();
        DifficultyDesignProfile profile = GetDifficultyProfile(tier);
        if (profile == null)
        {
            int[] fallbackDeck = previewFixedEnemyDeckCardIds != null
                ? (int[])previewFixedEnemyDeckCardIds.Clone()
                : new int[0];
            return new BattleDifficultyConfig("普通", true, fallbackDeck, 6, 2, 0.5f, 1f, 50, 45, 45);
        }

        int overLimit = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 12f, profile.overLimitToleranceIndex / 100f)) + profile.overLimitBias, 0, 30);
        int minSpells = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 4f, profile.spellRatioIndex / 100f)) + profile.minSpellsBias, 0, 20);
        float idxMul = Mathf.Lerp(0.78f, 1.22f, profile.deckStrengthIndex / 100f);
        float scoreMul = Mathf.Clamp(profile.scoreMultiplier * idxMul, 0.6f, 1.45f);
        int[] deckSource = profile.fixedDeckOverride != null && profile.fixedDeckOverride.Length > 0
            ? (int[])profile.fixedDeckOverride.Clone()
            : (previewFixedEnemyDeckCardIds != null ? (int[])previewFixedEnemyDeckCardIds.Clone() : new int[0]);
        bool useFixed = profile.useFixedDeck;

        return new BattleDifficultyConfig(
            string.IsNullOrWhiteSpace(profile.labelZh) ? tier.ToString() : profile.labelZh,
            useFixed,
            deckSource,
            overLimit,
            minSpells,
            Mathf.Clamp(profile.revealRatio, 0.2f, 1f),
            scoreMul,
            profile.deckStrengthIndex,
            profile.spellRatioIndex,
            profile.overLimitToleranceIndex);
    }

    private void CreateDifficultyButtons(Transform parent)
    {
        EnsureDifficultyProfiles();
        battlePreviewDifficultyButtons.Clear();
        CreateOneDifficultyButton(parent, "DiffIntro", GetDifficultyButtonLabel(BattleDifficultyTier.Intro), BattleDifficultyTier.Intro, 0);
        CreateOneDifficultyButton(parent, "DiffEasy", GetDifficultyButtonLabel(BattleDifficultyTier.Easy), BattleDifficultyTier.Easy, 1);
        CreateOneDifficultyButton(parent, "DiffNormal", GetDifficultyButtonLabel(BattleDifficultyTier.Normal), BattleDifficultyTier.Normal, 2);
        CreateOneDifficultyButton(parent, "DiffHard", GetDifficultyButtonLabel(BattleDifficultyTier.Hard), BattleDifficultyTier.Hard, 3);
        CreateOneDifficultyButton(parent, "DiffBoss", GetDifficultyButtonLabel(BattleDifficultyTier.Boss), BattleDifficultyTier.Boss, 4);
        if (IsTierLocked(selectedDifficultyTier))
            selectedDifficultyTier = BattleDifficultyTier.Normal;
        RefreshDifficultyButtonVisuals();
    }

    private void CreateOneDifficultyButton(Transform parent, string objName, string label, BattleDifficultyTier tier, int idx)
    {
        Button btn = CreateModalButton(parent, objName, label);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(idx * 198f, 0f);
        rt.sizeDelta = new Vector2(186f, 90f);
        bool locked = IsTierLocked(tier);
        btn.interactable = !locked;
        btn.onClick.AddListener(() =>
        {
            if (locked) return;
            selectedDifficultyTier = tier;
            RefreshBattlePreviewBodyText();
        });
        battlePreviewDifficultyButtons.Add(btn);
    }

    private void ToggleDifficultyButtonsExpanded()
    {
        difficultyButtonsExpanded = !difficultyButtonsExpanded;
        ApplyDifficultyButtonsExpandedState();
    }

    private void ApplyDifficultyButtonsExpandedState()
    {
        if (battlePreviewDifficultyRowRt != null)
            battlePreviewDifficultyRowRt.gameObject.SetActive(difficultyButtonsExpanded);
    }

    private void RefreshDifficultyButtonVisuals()
    {
        for (int i = 0; i < battlePreviewDifficultyButtons.Count; i++)
        {
            Button btn = battlePreviewDifficultyButtons[i];
            if (btn == null) continue;
            BattleDifficultyTier tier = (BattleDifficultyTier)i;
            bool active = tier == selectedDifficultyTier;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                bool locked = IsTierLocked(tier);
                if (locked) img.color = new Color(0.58f, 0.58f, 0.58f, 0.9f);
                else img.color = active ? Color.white : new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);
            }
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                bool locked = IsTierLocked(tier);
                if (locked) txt.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                else txt.color = active ? Color.black : Color.white;
            }
        }
        if (battlePreviewDifficultyToggleButton != null)
        {
            TextMeshProUGUI txt = battlePreviewDifficultyToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = difficultyButtonsExpanded ? "敵方難度▼" : "敵方難度►";
        }
    }

    private void EnsureDifficultyProfiles()
    {
        if (difficultyProfiles == null) difficultyProfiles = new List<DifficultyDesignProfile>();
        EnsureProfileExists(BattleDifficultyTier.Intro, "入門");
        EnsureProfileExists(BattleDifficultyTier.Easy, "簡單");
        EnsureProfileExists(BattleDifficultyTier.Normal, "普通");
        EnsureProfileExists(BattleDifficultyTier.Hard, "困難", false);
        EnsureProfileExists(BattleDifficultyTier.Boss, "魔王", true);
    }

    private void EnsureProfileExists(BattleDifficultyTier tier, string label, bool locked = false)
    {
        if (GetDifficultyProfile(tier) != null) return;
        difficultyProfiles.Add(new DifficultyDesignProfile
        {
            tier = tier,
            labelZh = label,
            locked = locked,
            deckStrengthIndex = 50,
            spellRatioIndex = 50,
            overLimitToleranceIndex = 50,
            useFixedDeck = true
        });
    }

    private DifficultyDesignProfile GetDifficultyProfile(BattleDifficultyTier tier)
    {
        if (difficultyProfiles == null) return null;
        for (int i = 0; i < difficultyProfiles.Count; i++)
        {
            DifficultyDesignProfile p = difficultyProfiles[i];
            if (p != null && p.tier == tier) return p;
        }
        return null;
    }

    private bool IsTierLocked(BattleDifficultyTier tier)
    {
        DifficultyDesignProfile p = GetDifficultyProfile(tier);
        return p != null && p.locked;
    }

    private string GetDifficultyButtonLabel(BattleDifficultyTier tier)
    {
        DifficultyDesignProfile p = GetDifficultyProfile(tier);
        if (p == null || string.IsNullOrWhiteSpace(p.labelZh)) return tier.ToString();
        return p.labelZh;
    }

    private string ResolveCardNameForPreview(int key)
    {
        if (cardStore != null)
        {
            Card c = cardStore.GetCardById(key);
            if (c != null)
            {
                string zh = string.IsNullOrWhiteSpace(c.cardName) ? ("卡牌#" + key) : c.cardName;
                string en = string.IsNullOrWhiteSpace(c.cardNameEnglish) ? string.Empty : c.cardNameEnglish.Trim();
                return string.IsNullOrEmpty(en) ? zh : (zh + " (" + en + ")");
            }
        }
        if (DeckCardId.IsSpellKey(key))
        {
            int ord = DeckCardId.SpellOrdinalFromKey(key);
            return ord switch
            {
                0 => "火球術 (Fireball)",
                1 => "初級治療 (Lesser Heal)",
                2 => "林可的凝視 (Lin's Gaze)",
                _ => "法術 " + ord
            };
        }
        return "怪物 #" + key;
    }

    private void EnsureCardStoreLoadedForPreview()
    {
        if (cardStore == null) cardStore = UnityEngine.Object.FindFirstObjectByType<CardStore>();
        if (cardStore == null) return;
        if (cardStore.cardList == null || cardStore.cardList.Count == 0)
            cardStore.LoadCardData();
    }

    private static string MapTierToZh(string tier)
    {
        return tier switch
        {
            "Severe" => "危險",
            "High" => "高",
            "Medium" => "中",
            _ => "低"
        };
    }

    private static string MapTierToZhSimple(string tier)
    {
        return tier switch
        {
            "Severe" => "高",
            "High" => "高",
            "Medium" => "中",
            _ => "低"
        };
    }

    private void RefreshPressureMetricChart(float threat, float burst, float tempo)
    {
        if (battlePreviewMetricBarFills.Count < 3 || battlePreviewMetricValueTexts.Count < 3) return;
        float[] targetValues = { threat, burst, tempo };
        if (battlePreviewMetricAnimRoutine != null)
        {
            StopCoroutine(battlePreviewMetricAnimRoutine);
            battlePreviewMetricAnimRoutine = null;
        }
        battlePreviewMetricAnimRoutine = StartCoroutine(CoAnimatePressureMetricChart(targetValues, 0.24f));
    }

    private IEnumerator CoAnimatePressureMetricChart(float[] targetValues, float duration)
    {
        if (battlePreviewMetricBarFills.Count < 3 || battlePreviewMetricValueTexts.Count < 3)
            yield break;
        if (targetValues == null || targetValues.Length < 3)
            yield break;

        for (int i = 0; i < 3; i++)
        {
            if (battlePreviewMetricBarFills[i] == null) continue;
            RectTransform fillRt = battlePreviewMetricBarFills[i] != null
                ? battlePreviewMetricBarFills[i].rectTransform
                : null;
            if (fillRt != null)
            {
                fillRt.anchorMax = new Vector2(1f, 0f);
            }
            if (battlePreviewMetricValueTexts[i] != null)
            {
                battlePreviewMetricValueTexts[i].text = "0";
            }
        }

        float t = 0f;
        float animDuration = Mathf.Clamp(duration, 0.2f, 0.3f);
        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animDuration);
            // Ease-out cubic.
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            for (int i = 0; i < 3; i++)
            {
                float target = Mathf.Clamp(targetValues[i], 0f, 100f);
                float current = target * eased;
                RectTransform fillRt = battlePreviewMetricBarFills[i] != null
                    ? battlePreviewMetricBarFills[i].rectTransform
                    : null;
                if (fillRt != null)
                {
                    fillRt.anchorMax = new Vector2(1f, current / 100f);
                }
                if (battlePreviewMetricValueTexts[i] != null)
                {
                    battlePreviewMetricValueTexts[i].text = Mathf.RoundToInt(current).ToString();
                }
            }
            yield return null;
        }

        for (int i = 0; i < 3; i++)
        {
            float target = Mathf.Clamp(targetValues[i], 0f, 100f);
            RectTransform fillRt = battlePreviewMetricBarFills[i] != null
                ? battlePreviewMetricBarFills[i].rectTransform
                : null;
            if (fillRt != null)
            {
                fillRt.anchorMax = new Vector2(1f, target / 100f);
            }
            if (battlePreviewMetricValueTexts[i] != null)
            {
                battlePreviewMetricValueTexts[i].text = Mathf.RoundToInt(target).ToString();
            }
        }

        battlePreviewMetricAnimRoutine = null;
    }

    private static string BuildEnemyDeckRowText(int count, string cardName)
    {
        // [Icon] slot reserved on the left for future sprite embedding.
        return $"<color=#7A5B3C>[Icon]</color>  <color=#2F271E><b>{count}x</b>  {cardName}</color>";
    }

    /// <summary>
    /// Replace punctuation/symbols that are frequently missing in some CJK font assets.
    /// Keep ASCII-heavy punctuation to reduce tofu squares in runtime.
    /// </summary>
    private static string SafePreviewText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return raw
            .Replace('：', ':')
            .Replace('，', ',')
            .Replace('。', '.')
            .Replace('（', '(')
            .Replace('）', ')')
            .Replace('？', '?')
            .Replace('！', '!')
            .Replace('「', '"')
            .Replace('」', '"')
            .Replace('、', ',')
            .Replace('／', '/')
            .Replace('％', '%')
            .Replace('＋', '+')
            .Replace('－', '-')
            .Replace('～', '~')
            .Replace('…', '.');
    }

    private TMP_FontAsset ResolvePreviewFontAsset()
    {
        TextMeshProUGUI[] tmps = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        TMP_FontAsset fallback = null;
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] == null || tmps[i].font == null) continue;
            if (fallback == null) fallback = tmps[i].font;
            string n = tmps[i].font.name.ToLowerInvariant();
            if (n.Contains("noto") || n.Contains("cjk") || n.Contains("sourcehan") || n.Contains("source han") ||
                n.Contains("jhenghei") || n.Contains("yahei") || n.Contains("pingfang") || n.Contains("han"))
                return tmps[i].font;
        }
        return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
    }
}

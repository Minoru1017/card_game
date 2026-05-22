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
    private readonly List<Image> battlePreviewMetricBarFills = new List<Image>(3);
    private readonly List<TextMeshProUGUI> battlePreviewMetricValueTexts = new List<TextMeshProUGUI>(3);
    private Coroutine battlePreviewMetricAnimRoutine;
    private Button battlePreviewStartButton;
    private Button battlePreviewGiveUpButton;
    private TMP_FontAsset battlePreviewFontAsset;
    [Header("Runtime UI Sprite Fallback")]
    [Tooltip("Optional sliced sprite for runtime-generated modal/button backgrounds. Leave empty to use plain rectangle.")]
    [SerializeField] private Sprite runtimeRoundedUiSprite;
    private const int BattlePreviewLayoutVersion = 4;
    private const float BattlePreviewIntelColumnStart = 0.58f;
    private const float BattlePreviewDifficultyRailWidthPx = 232f;
    private const float BattlePreviewSummaryPaneLeftInsetPx = 244f;
    private int battlePreviewLayoutBuilt;
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
            locked = false,
            deckStrengthIndex = 92,
            spellRatioIndex = 85,
            overLimitToleranceIndex = 96,
            revealRatio = 0.28f,
            scoreMultiplier = 1.38f,
            overLimitBias = 2,
            minSpellsBias = 2,
            useFixedDeck = true,
            fixedDeckOverride = new int[]
            {
                13, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 19, 22, -1, -2, -3
            }
        }
    };
    private bool pendingUseFixedEnemyDeck;
    private int[] pendingFixedEnemyDeckCardIds;
    private int pendingEnemyOverLimitAllowance;
    private int pendingMinEnemySpellsInDeck;
    private EnemyAiPlayStyle pendingEnemyAiPlayStyle = EnemyAiPlayStyle.Greedy;
    private string pendingDifficultyLabelZh = BattleDifficultyRuntime.DefaultLabelZh;

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

    public void RefreshEnterBattleState() => RefreshEnterBattleState(true);

    /// <param name="reloadFromDisk">剛在本場景儲存牌組／改名後請傳 false，避免立刻從舊備份重載覆蓋記憶體。</param>
    public void RefreshEnterBattleState(bool reloadFromDisk)
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (reloadFromDisk && playerData != null) playerData.LoadPlayerData();

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
            string label = string.IsNullOrWhiteSpace(pendingDifficultyLabelZh)
                ? BattleLaunchContext.PeekDifficultyLabelZh()
                : pendingDifficultyLabelZh;
            if (!string.IsNullOrWhiteSpace(label))
                BattleLaunchContext.SetPendingDifficultyLabelZh(label);
            manager.ApplyLaunchContextDifficulty();
            manager.QueueRuntimeDifficultyConfig(
                pendingUseFixedEnemyDeck,
                pendingFixedEnemyDeckCardIds,
                pendingEnemyOverLimitAllowance,
                pendingMinEnemySpellsInDeck,
                pendingEnemyAiPlayStyle,
                label);
            manager.CaptureBattleDifficultyForRecords();
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
        pendingEnemyAiPlayStyle = MapDifficultyToEnemyAiPlayStyle(selectedDifficultyTier);
        pendingDifficultyLabelZh = cfg.LabelZh;
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        HideBattlePreviewModal();
        StartBattleSceneLoad();
    }

    private static EnemyAiPlayStyle MapDifficultyToEnemyAiPlayStyle(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Hard: return EnemyAiPlayStyle.SchemingHard;
            case BattleDifficultyTier.Boss: return EnemyAiPlayStyle.SchemingBoss;
            default: return EnemyAiPlayStyle.Greedy;
        }
    }

    private void OnBattlePreviewGiveUpClicked()
    {
        HideBattlePreviewModal();
    }

    private void EnsureBattlePreviewUi()
    {
        if (battlePreviewOverlayRoot != null)
        {
            if (battlePreviewLayoutBuilt == BattlePreviewLayoutVersion) return;
            DestroyBattlePreviewUi();
        }

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
        chipRowRt.sizeDelta = new Vector2(360f, 28f);
        CreateHeaderChip(chipRowObj.transform, "ChipThreat", "戰況摘要", new Vector2(0f, 0f), 170f);
        CreateHeaderChip(chipRowObj.transform, "ChipIntel", "作戰情報", new Vector2(200f, 0f), 150f);

        const float previewFooterHeight = 100f;
        const float previewContentBottom = previewFooterHeight + 12f;

        GameObject pressureBlockObj = new GameObject("PressureBlock", typeof(RectTransform), typeof(Image));
        pressureBlockObj.transform.SetParent(panelObj.transform, false);
        RectTransform pressureRt = pressureBlockObj.GetComponent<RectTransform>();
        pressureRt.anchorMin = new Vector2(0f, 0f);
        pressureRt.anchorMax = new Vector2(BattlePreviewIntelColumnStart, 1f);
        pressureRt.offsetMin = new Vector2(34f, previewContentBottom);
        pressureRt.offsetMax = new Vector2(-12f, -132f);
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

        GameObject deckBlockObj = new GameObject("IntelBlock", typeof(RectTransform), typeof(Image));
        deckBlockObj.transform.SetParent(panelObj.transform, false);
        RectTransform deckBlockRt = deckBlockObj.GetComponent<RectTransform>();
        deckBlockRt.anchorMin = new Vector2(BattlePreviewIntelColumnStart, 0f);
        deckBlockRt.anchorMax = new Vector2(1f, 1f);
        deckBlockRt.offsetMin = new Vector2(20f, previewContentBottom);
        deckBlockRt.offsetMax = new Vector2(-34f, -132f);
        Image deckBg = deckBlockObj.GetComponent<Image>();
        deckBg.color = new Color(0.9f, 0.93f, 0.86f, 1f);

        GameObject intelInnerCardObj = new GameObject("IntelInnerCard", typeof(RectTransform), typeof(Image));
        intelInnerCardObj.transform.SetParent(deckBlockObj.transform, false);
        RectTransform intelInnerRt = intelInnerCardObj.GetComponent<RectTransform>();
        intelInnerRt.anchorMin = new Vector2(0f, 0f);
        intelInnerRt.anchorMax = new Vector2(1f, 1f);
        intelInnerRt.offsetMin = new Vector2(10f, 10f);
        intelInnerRt.offsetMax = new Vector2(-10f, -10f);
        intelInnerCardObj.GetComponent<Image>().color = new Color(0.95f, 0.97f, 0.92f, 1f);

        GameObject centerDividerObj = new GameObject("CenterDivider", typeof(RectTransform), typeof(Image));
        centerDividerObj.transform.SetParent(panelObj.transform, false);
        RectTransform centerDividerRt = centerDividerObj.GetComponent<RectTransform>();
        centerDividerRt.anchorMin = new Vector2(BattlePreviewIntelColumnStart, 0f);
        centerDividerRt.anchorMax = new Vector2(BattlePreviewIntelColumnStart, 1f);
        centerDividerRt.pivot = new Vector2(0.5f, 0.5f);
        centerDividerRt.offsetMin = new Vector2(0f, previewContentBottom);
        centerDividerRt.offsetMax = new Vector2(2f, -132f);
        centerDividerObj.GetComponent<Image>().color = new Color(0.61f, 0.53f, 0.35f, 0.5f);

        GameObject diffRailObj = new GameObject("DifficultyRail", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        diffRailObj.transform.SetParent(pressureInnerCardObj.transform, false);
        RectTransform diffRailRt = diffRailObj.GetComponent<RectTransform>();
        diffRailRt.anchorMin = new Vector2(0f, 0f);
        diffRailRt.anchorMax = new Vector2(0f, 1f);
        diffRailRt.pivot = new Vector2(0f, 0.5f);
        diffRailRt.anchoredPosition = Vector2.zero;
        diffRailRt.sizeDelta = new Vector2(BattlePreviewDifficultyRailWidthPx, 0f);
        Image diffRailBg = diffRailObj.GetComponent<Image>();
        diffRailBg.color = new Color(0.78f, 0.84f, 0.70f, 1f);
        Outline diffRailOutline = diffRailObj.AddComponent<Outline>();
        diffRailOutline.effectColor = new Color(0.32f, 0.40f, 0.26f, 0.85f);
        diffRailOutline.effectDistance = new Vector2(2f, -2f);
        VerticalLayoutGroup diffVlg = diffRailObj.GetComponent<VerticalLayoutGroup>();
        diffVlg.spacing = 10f;
        diffVlg.padding = new RectOffset(12, 12, 14, 14);
        diffVlg.childAlignment = TextAnchor.UpperCenter;
        diffVlg.childControlWidth = true;
        diffVlg.childControlHeight = true;
        diffVlg.childForceExpandWidth = true;
        diffVlg.childForceExpandHeight = false;

        GameObject diffTitleObj = new GameObject("DifficultyTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        diffTitleObj.transform.SetParent(diffRailObj.transform, false);
        diffTitleObj.GetComponent<LayoutElement>().preferredHeight = 40f;
        TextMeshProUGUI diffTitleTmp = diffTitleObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) diffTitleTmp.font = battlePreviewFontAsset;
        diffTitleTmp.text = "難度";
        diffTitleTmp.fontSize = 30f;
        diffTitleTmp.fontStyle = FontStyles.Bold;
        diffTitleTmp.alignment = TextAlignmentOptions.Center;
        diffTitleTmp.color = new Color(0.22f, 0.28f, 0.18f, 1f);

        CreateDifficultyButtons(diffRailObj.transform);

        GameObject summaryPaneObj = new GameObject("SummaryPane", typeof(RectTransform));
        summaryPaneObj.transform.SetParent(pressureInnerCardObj.transform, false);
        RectTransform summaryPaneRt = summaryPaneObj.GetComponent<RectTransform>();
        summaryPaneRt.anchorMin = Vector2.zero;
        summaryPaneRt.anchorMax = Vector2.one;
        summaryPaneRt.offsetMin = new Vector2(BattlePreviewSummaryPaneLeftInsetPx, 8f);
        summaryPaneRt.offsetMax = new Vector2(-8f, -8f);

        battlePreviewPressureText = CreatePreviewBlockText(summaryPaneObj.transform, "PressureText");
        if (battlePreviewPressureText != null)
        {
            RectTransform pressureTextRt = battlePreviewPressureText.rectTransform;
            pressureTextRt.offsetMin = new Vector2(12f, 178f);
            pressureTextRt.offsetMax = new Vector2(-12f, -8f);
            battlePreviewPressureText.fontSize = 28f;
        }
        CreatePressureMetricsChart(summaryPaneObj.transform);
        battlePreviewDeckText = CreatePreviewBlockText(intelInnerCardObj.transform, "IntelText");

        battlePreviewStartButton = CreateModalButton(panelObj.transform, "StartBattleButton", "開始對戰");
        RectTransform startRt = battlePreviewStartButton.GetComponent<RectTransform>();
        startRt.anchorMin = new Vector2(0.5f, 0f);
        startRt.anchorMax = new Vector2(0.5f, 0f);
        startRt.pivot = new Vector2(0.5f, 0f);
        startRt.anchoredPosition = new Vector2(150f, 16f);
        startRt.sizeDelta = new Vector2(300f, 72f);
        battlePreviewStartButton.onClick.AddListener(OnBattlePreviewStartClicked);

        battlePreviewGiveUpButton = CreateModalButton(panelObj.transform, "GiveUpButton", "放棄");
        RectTransform giveUpRt = battlePreviewGiveUpButton.GetComponent<RectTransform>();
        giveUpRt.anchorMin = new Vector2(0.5f, 0f);
        giveUpRt.anchorMax = new Vector2(0.5f, 0f);
        giveUpRt.pivot = new Vector2(0.5f, 0f);
        giveUpRt.anchoredPosition = new Vector2(-150f, 16f);
        giveUpRt.sizeDelta = new Vector2(300f, 72f);
        battlePreviewGiveUpButton.onClick.AddListener(OnBattlePreviewGiveUpClicked);

        battlePreviewOverlayRoot = overlay;
        battlePreviewLayoutBuilt = BattlePreviewLayoutVersion;
        battlePreviewOverlayRoot.SetActive(false);
    }

    private void DestroyBattlePreviewUi()
    {
        if (battlePreviewMetricAnimRoutine != null)
        {
            StopCoroutine(battlePreviewMetricAnimRoutine);
            battlePreviewMetricAnimRoutine = null;
        }
        if (battlePreviewOverlayRoot != null)
            Destroy(battlePreviewOverlayRoot);
        battlePreviewOverlayRoot = null;
        battlePreviewPressureText = null;
        battlePreviewDeckText = null;
        battlePreviewStartButton = null;
        battlePreviewGiveUpButton = null;
        battlePreviewMetricBarFills.Clear();
        battlePreviewMetricValueTexts.Clear();
        battlePreviewDifficultyButtons.Clear();
        battlePreviewLayoutBuilt = 0;
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

        GameObject panelObj = new GameObject("PressureGaugesPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObj.transform.SetParent(parent, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 10f);
        panelRt.sizeDelta = new Vector2(-16f, 168f);
        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.36f, 0.42f, 0.30f, 0.22f);
        Outline panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.55f, 0.62f, 0.45f, 0.55f);
        panelOutline.effectDistance = new Vector2(1f, -1f);
        VerticalLayoutGroup panelVlg = panelObj.GetComponent<VerticalLayoutGroup>();
        panelVlg.spacing = 12f;
        panelVlg.padding = new RectOffset(14, 14, 12, 12);
        panelVlg.childAlignment = TextAnchor.UpperLeft;
        panelVlg.childControlWidth = true;
        panelVlg.childControlHeight = true;
        panelVlg.childForceExpandWidth = true;
        panelVlg.childForceExpandHeight = false;

        GameObject headerObj = new GameObject("GaugeHeader", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        headerObj.transform.SetParent(panelObj.transform, false);
        headerObj.GetComponent<LayoutElement>().preferredHeight = 28f;
        TextMeshProUGUI headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) headerTmp.font = battlePreviewFontAsset;
        headerTmp.text = "壓力指標";
        headerTmp.fontSize = 22f;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.color = new Color(0.88f, 0.94f, 0.82f, 1f);
        headerTmp.alignment = TextAlignmentOptions.MidlineLeft;

        CreateOnePressureGaugeRow(panelObj.transform, "Threat", "壓制", new Color(0.55f, 0.88f, 0.58f, 1f));
        CreateOnePressureGaugeRow(panelObj.transform, "Burst", "爆發", new Color(1f, 0.78f, 0.38f, 1f));
        CreateOnePressureGaugeRow(panelObj.transform, "Tempo", "節奏", new Color(1f, 0.62f, 0.34f, 1f));
    }

    private void CreateOnePressureGaugeRow(Transform parent, string key, string label, Color fillColor)
    {
        GameObject rowObj = new GameObject(key + "GaugeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObj.transform.SetParent(parent, false);
        LayoutElement rowLe = rowObj.GetComponent<LayoutElement>();
        rowLe.preferredHeight = 44f;
        rowLe.minHeight = 40f;
        HorizontalLayoutGroup rowHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 10f;
        rowHlg.padding = new RectOffset(0, 0, 2, 2);
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = true;

        GameObject labelObj = new GameObject(key + "Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObj.transform.SetParent(rowObj.transform, false);
        labelObj.GetComponent<LayoutElement>().preferredWidth = 52f;
        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) labelTmp.font = battlePreviewFontAsset;
        labelTmp.text = label;
        labelTmp.fontSize = 24f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = new Color(0.90f, 0.95f, 0.86f, 1f);

        GameObject trackObj = new GameObject(key + "Track", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        trackObj.transform.SetParent(rowObj.transform, false);
        LayoutElement trackLe = trackObj.GetComponent<LayoutElement>();
        trackLe.flexibleWidth = 1f;
        trackLe.preferredHeight = 28f;
        trackLe.minHeight = 28f;
        Image trackImg = trackObj.GetComponent<Image>();
        trackImg.color = new Color(0.18f, 0.20f, 0.14f, 0.55f);

        GameObject fillObj = new GameObject(key + "Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(trackObj.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = new Vector2(3f, 4f);
        fillRt.offsetMax = new Vector2(-3f, -4f);
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;
        battlePreviewMetricBarFills.Add(fillImg);

        GameObject shineObj = new GameObject(key + "Shine", typeof(RectTransform), typeof(Image));
        shineObj.transform.SetParent(fillObj.transform, false);
        RectTransform shineRt = shineObj.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.55f);
        shineRt.anchorMax = new Vector2(1f, 1f);
        shineRt.offsetMin = new Vector2(2f, 0f);
        shineRt.offsetMax = new Vector2(-2f, -2f);
        shineObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.28f);

        GameObject valueObj = new GameObject(key + "Value", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        valueObj.transform.SetParent(rowObj.transform, false);
        valueObj.GetComponent<LayoutElement>().preferredWidth = 48f;
        TextMeshProUGUI valueTmp = valueObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) valueTmp.font = battlePreviewFontAsset;
        valueTmp.text = "0";
        valueTmp.fontSize = 26f;
        valueTmp.fontStyle = FontStyles.Bold;
        valueTmp.alignment = TextAlignmentOptions.MidlineRight;
        valueTmp.color = fillColor;
        battlePreviewMetricValueTexts.Add(valueTmp);
    }

    private void RefreshBattlePreviewBodyText()
    {
        if (battlePreviewPressureText == null || battlePreviewDeckText == null) return;
        if (playerData == null) playerData = UnityEngine.Object.FindFirstObjectByType<PlayerData>();
        if (playerData != null) playerData.LoadPlayerData();
        EnsureCardStoreLoadedForPreview();

        BattleDifficultyConfig cfg = BuildDifficultyConfig(selectedDifficultyTier);
        EnemyAiPlayStyle aiStyle = MapDifficultyToEnemyAiPlayStyle(selectedDifficultyTier);
        int playerDeckCount = playerData != null ? Mathf.Max(1, playerData.GetSelectedDeckTotalCount()) : 20;
        List<int> predictedDeck = BuildPredictedEnemyDeckKeys(playerDeckCount, cfg);

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

        int total = Mathf.Max(1, predictedDeck.Count);
        int spellCount = CountSpellCards(predictedDeck);
        int spellRatio = Mathf.Clamp(Mathf.RoundToInt((spellCount * 100f) / total), 0, 100);

        StringBuilder pressureSb = new StringBuilder(512);
        pressureSb.AppendLine($"<size=112%><b>整體壓力: {MapTierToZhSimple(tier)}</b></size>");
        pressureSb.AppendLine($"<color=#43573A>目前選擇: {cfg.LabelZh}</color>");
        pressureSb.AppendLine($"<color=#6C533D>AI: {GetEnemyAiOneLinerZh(aiStyle)}</color>");
        pressureSb.AppendLine($"<color=#6C533D>牌庫 {playerDeckCount} 張 · 法術至少 {cfg.MinSpellsInDeck} · 超額容許 ~{cfg.OverLimitAllowance}</color>");
        battlePreviewPressureText.text = SafePreviewText(pressureSb.ToString());
        RefreshPressureMetricChart(threat, burst, tempo);

        StringBuilder deckSb = new StringBuilder(640);
        deckSb.AppendLine("<size=120%><b>作戰情報</b></size>");
        deckSb.AppendLine($"<color=#43573A><b>出牌風格</b></color>  {GetEnemyAiBriefZh(aiStyle)}");
        deckSb.AppendLine($"<color=#43573A><b>牌庫規模</b></color>  與你的牌組相同 ({playerDeckCount} 張)");
        deckSb.AppendLine(
            $"<color=#43573A><b>構築傾向</b></color>  法術至少 {cfg.MinSpellsInDeck} 張, 構築允許超過 30 張上限約 {cfg.OverLimitAllowance} 張");
        deckSb.AppendLine($"<color=#6C533D>實戰牌組法術約 {spellRatio}%</color>");
        battlePreviewDeckText.text = SafePreviewText(deckSb.ToString());
        RefreshDifficultyButtonVisuals();
    }

    private List<int> BuildPredictedEnemyDeckKeys(int targetCount, BattleDifficultyConfig cfg)
    {
        List<int> result = new List<int>(Mathf.Max(1, targetCount));
        int[] sourceDeck = ResolveEnemySourceDeckKeys(cfg);
        if (!cfg.UseFixedDeck || sourceDeck == null || sourceDeck.Length == 0)
            return result;
        for (int i = 0; i < targetCount; i++)
            result.Add(sourceDeck[i % sourceDeck.Length]);
        return result;
    }

    private int[] ResolveEnemySourceDeckKeys(BattleDifficultyConfig cfg)
    {
        if (!cfg.UseFixedDeck) return null;
        if (cfg.FixedDeckIds != null && cfg.FixedDeckIds.Length > 0)
            return cfg.FixedDeckIds;
        return previewFixedEnemyDeckCardIds;
    }

    private static List<int> BuildSortedUniquePoolKeys(int[] sourceDeck)
    {
        var keys = new List<int>();
        if (sourceDeck == null || sourceDeck.Length == 0) return keys;
        var seen = new HashSet<int>();
        for (int i = 0; i < sourceDeck.Length; i++)
        {
            int key = sourceDeck[i];
            if (seen.Add(key)) keys.Add(key);
        }
        keys.Sort(CompareEnemyPoolKey);
        return keys;
    }

    private static int CompareEnemyPoolKey(int a, int b)
    {
        bool spellA = DeckCardId.IsSpellKey(a);
        bool spellB = DeckCardId.IsSpellKey(b);
        if (spellA != spellB) return spellA ? -1 : 1;
        return a.CompareTo(b);
    }

    private static Dictionary<int, int> CountSourceDeckKeys(int[] sourceDeck)
    {
        var counts = new Dictionary<int, int>();
        if (sourceDeck == null) return counts;
        for (int i = 0; i < sourceDeck.Length; i++)
        {
            int key = sourceDeck[i];
            if (!counts.ContainsKey(key)) counts[key] = 0;
            counts[key]++;
        }
        return counts;
    }

    private static string GetEnemyAiOneLinerZh(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.SchemingHard:
                return "保留高稀有卡, 待時機出牌";
            case EnemyAiPlayStyle.SchemingBoss:
                return "積極囤牌, 高稀有卡更晚出手";
            default:
                return "有牌可出即依優先度打出";
        }
    }

    private static string GetEnemyAiBriefZh(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.SchemingHard:
                return "困難 AI: 傾向保留 SR 以上卡牌, 在場面有利或需要斬殺時才打出.";
            case EnemyAiPlayStyle.SchemingBoss:
                return "魔王 AI: 傾向保留 R 以上卡牌, 囤牌條件比困難更嚴, 回合壓力更大.";
            default:
                return "標準 AI: 每回合在可出牌中選評分最高者立即打出.";
        }
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
        LayoutElement le = btn.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 68f;
        le.minHeight = 60f;
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 68f);
        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.fontSize = 34f;
        bool locked = IsTierLocked(tier);
        btn.interactable = !locked;
        btn.onClick.AddListener(() =>
        {
            if (locked) return;
            selectedDifficultyTier = tier;
            RefreshBattlePreviewBodyText();
            RefreshDifficultyButtonVisuals();
        });
        battlePreviewDifficultyButtons.Add(btn);
    }

    private void RefreshDifficultyButtonVisuals()
    {
        for (int i = 0; i < battlePreviewDifficultyButtons.Count; i++)
        {
            Button btn = battlePreviewDifficultyButtons[i];
            if (btn == null) continue;
            BattleDifficultyTier tier = (BattleDifficultyTier)i;
            bool active = tier == selectedDifficultyTier;
            bool locked = IsTierLocked(tier);
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                if (locked) img.color = new Color(0.58f, 0.58f, 0.58f, 0.9f);
                else if (active) img.color = Color.white;
                else img.color = GetDifficultyTierAccentColor(tier);
            }
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                if (locked) txt.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                else txt.color = active ? new Color(0.12f, 0.1f, 0.08f, 1f) : Color.white;
                if (active) txt.fontStyle = FontStyles.Bold;
                else txt.fontStyle = FontStyles.Normal;
                txt.fontSize = active ? 36f : 34f;
            }

            Outline outline = btn.GetComponent<Outline>();
            if (active && !locked)
            {
                if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.18f, 0.14f, 0.10f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = true;
            }
            else if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }

    private static Color GetDifficultyTierAccentColor(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro:
                return new Color(0.42f, 0.72f, 0.48f, 1f);
            case BattleDifficultyTier.Easy:
                return new Color(0.52f, 0.78f, 0.42f, 1f);
            case BattleDifficultyTier.Normal:
                return new Color(0.92f, 0.76f, 0.28f, 1f);
            case BattleDifficultyTier.Hard:
                return new Color(0.95f, 0.55f, 0.28f, 1f);
            case BattleDifficultyTier.Boss:
                return new Color(0.82f, 0.28f, 0.32f, 1f);
            default:
                return new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);
        }
    }

    private void EnsureDifficultyProfiles()
    {
        if (difficultyProfiles == null) difficultyProfiles = new List<DifficultyDesignProfile>();
        EnsureProfileExists(BattleDifficultyTier.Intro, "入門");
        EnsureProfileExists(BattleDifficultyTier.Easy, "簡單");
        EnsureProfileExists(BattleDifficultyTier.Normal, "普通");
        EnsureProfileExists(BattleDifficultyTier.Hard, "困難", false);
        EnsureProfileExists(BattleDifficultyTier.Boss, "魔王", false);
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
                fillRt.anchorMax = new Vector2(0f, 1f);
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
                    fillRt.anchorMax = new Vector2(current / 100f, 1f);
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
                fillRt.anchorMax = new Vector2(target / 100f, 1f);
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

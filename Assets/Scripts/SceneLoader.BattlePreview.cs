using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public partial class SceneLoader
{
    [Header("Battle Preview (80% modal)")]
    [Tooltip("Pre-battle estimate: enemy uses fixed pool and cycles to player deck size.")]
    [SerializeField] private bool previewUseFixedEnemyDeck = true;
    [SerializeField] private int[] previewFixedEnemyDeckCardIds = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 19, 22, -1, -2, -3 };
    [Range(0, 30)] [SerializeField] private int previewEnemyOverLimitAllowance = 6;
    [Range(0, 20)] [SerializeField] private int previewMinEnemySpellsInDeck = 2;

    private GameObject battlePreviewOverlayRoot;
    private RectTransform battlePreviewPanelRt;
    private Coroutine battlePreviewBossUnlockFxRoutine;
    private bool battlePreviewBossUnlockAnimating;
    private TextMeshProUGUI battlePreviewPressureText;
    private TextMeshProUGUI battlePreviewDeckText;
    private readonly List<Image> battlePreviewMetricBarFills = new List<Image>(3);
    private readonly List<TextMeshProUGUI> battlePreviewMetricValueTexts = new List<TextMeshProUGUI>(3);
    private Coroutine battlePreviewMetricAnimRoutine;
    private Button battlePreviewStartButton;
    private Button battlePreviewGiveUpButton;
    private TMP_FontAsset battlePreviewFontAsset;
    [Header("Battle Preview Panel Art")]
    [Tooltip("戰前預覽主面板底圖（建議拖入 Assets/Resources/UI/pre-war preview）。留空則從 Resources 載入；仍無則退回程式色塊。")]
    [SerializeField] private Sprite battlePreviewPanelSprite;
    [Tooltip("可選：按鈕用 9-slice 圓角底；留空則按鈕為純色矩形。")]
    [SerializeField] private Sprite runtimeRoundedUiSprite;
    [Header("Difficulty Level Art (optional Inspector overrides)")]
    [SerializeField] private Sprite difficultySpriteBasics;
    [SerializeField] private Sprite difficultySpriteEasy;
    [SerializeField] private Sprite difficultySpriteNormal;
    [SerializeField] private Sprite difficultySpriteHard;
    [SerializeField] private Sprite difficultySpriteBoss;
    private const string DefaultBattlePreviewPanelResourcePath = "UI/pre-war preview";
    private static Sprite _cachedDefaultBattlePreviewPanelSprite;
    private static readonly Dictionary<BattleDifficultyTier, Sprite> CachedDifficultyTierSprites =
        new Dictionary<BattleDifficultyTier, Sprite>();
    private const int BattlePreviewLayoutVersion = 25;
    private const string DifficultyLevelResourceRoot = "UI/Difficulty level";
    private static readonly Color BattlePreviewInk = new Color(0.2f, 0.17f, 0.12f, 1f);
    private static readonly Color BattlePreviewInkMuted = new Color(0.35f, 0.30f, 0.22f, 1f);
    private const float AcSelectScalePeak = 1.045f;
    private const float AcSelectScaleRest = 1.018f;
    private const float AcSelectAnimDuration = 0.28f;
    private const float AcDeselectDuration = 0.24f;
    private const float AcSelectRisePortion = 0.58f;
    private const float AcColorLeadMultiplier = 1.12f;
    /// <summary>對齊 <c>pre-war preview</c> 美術稿（1524×883 sprite）的錨點比例。</summary>
    private const float AuthoredArtWidth = 1524f;
    private const float AuthoredArtHeight = 883f;
    private const float AuthoredAnchorContentBottom = (118f + 100f + 32f) / AuthoredArtHeight;
    private const float AuthoredGiveUpAnchorXMin = 0.045f;
    private const float AuthoredGiveUpAnchorXMax = 0.145f;
    private const float AuthoredGiveUpAnchorYMin = 0.805f;
    private const float AuthoredGiveUpAnchorYMax = 0.895f;
    private const float AuthoredIntelAnchorXMin = 0.855f;
    private const float AuthoredIntelAnchorXMax = 0.955f;
    private const float AuthoredIntelAnchorYMin = 0.805f;
    private const float AuthoredIntelAnchorYMax = 0.895f;
    private const float AuthoredHeaderAnchorXMin = 0.16f;
    private const float AuthoredHeaderAnchorXMax = 0.84f;
    private const float AuthoredHeaderAnchorYMin = 0.76f;
    private const float AuthoredHeaderAnchorYMax = 0.89f;
    private const float AuthoredLeftColumnAnchorXMin = 0.07f;
    private const float AuthoredLeftColumnAnchorXMax = 0.26f;
    private const float AuthoredRightColumnAnchorXMin = 0.74f;
    private const float AuthoredRightColumnAnchorXMax = 0.93f;
    private const float AuthoredSideColumnTitleYMin = 0.66f;
    private const float AuthoredSideColumnTitleYMax = 0.74f;
    private const float AuthoredSideColumnDetailYMin = 0.58f;
    private const float AuthoredSideColumnDetailYMax = 0.66f;
    private const float AuthoredPuzzleCenterAnchorXMin = 0.28f;
    private const float AuthoredPuzzleCenterAnchorXMax = 0.72f;
    private const float AuthoredPuzzleHintAnchorYMin = 0.56f;
    private const float AuthoredPuzzleHintAnchorYMax = 0.64f;
    private const float AuthoredDetailScrollAnchorYMin = 0.36f;
    private const float AuthoredDetailScrollAnchorYMax = 0.80f;
    private const float AuthoredGaugeAnchorYMin = 0.02f;
    private const float AuthoredGaugeAnchorYMax = 0.18f;
    private const float AuthoredArchRowContainerXMin = 0.11f;
    private const float AuthoredArchRowContainerXMax = 0.89f;
    private const float AuthoredArchRowSpacing = 2f;
    private const float AuthoredArchButtonHeightScale = 2.5f;
    private const int AuthoredArchRowPaddingH = 0;
    private const float AuthoredArchButtonMinHeightPx = 120f;
    private const float AuthoredArchAnchorYMin = 0.17f;
    private const float AuthoredArchAnchorYMax = 0.53f;
    private const float AuthoredBossRevealAnchorXMin = 0.11f;
    private const float AuthoredBossRevealAnchorXMax = 0.89f;
    private const float AuthoredBossRevealAnchorYMin = 0.14f;
    private const float AuthoredBossRevealAnchorYMax = 0.52f;
    private const float AuthoredStartBattleAnchorXMin = 0.36f;
    private const float AuthoredStartBattleAnchorXMax = 0.64f;
    private const float AuthoredStartBattleAnchorYMin = 0.055f;
    private const float AuthoredStartBattleAnchorYMax = 0.135f;
    private BattleDifficultyTier battlePreviewAuthoredRevealTier = BattleDifficultyTier.Boss;
    private const float AuthoredAnchorContentTop = 1f - (108f / AuthoredArtHeight);
    private const float AuthoredAnchorDiffXMin = 28f / AuthoredArtWidth;
    private const float AuthoredAnchorDiffXMax = (28f + 252f) / AuthoredArtWidth;
    private const float AuthoredAnchorSummaryXMin = 286f / AuthoredArtWidth;
    private const float AuthoredAnchorSummaryXMax = 962f / AuthoredArtWidth;
    private const float AuthoredAnchorIntelXMin = 962f / AuthoredArtWidth;
    private const float AuthoredAnchorIntelXMax = 1f - (30f / AuthoredArtWidth);
    private const float BattlePreviewIntelColumnStart = 0.58f;
    private const float BattlePreviewDifficultyRailWidthPx = 232f;
    private const float BattlePreviewSummaryPaneLeftInsetPx = 244f;
    private int battlePreviewLayoutBuilt;
    private string battlePreviewActivePuzzleId;
    private string battlePreviewLayoutBuiltPuzzleId;
    private bool battlePreviewHarborTrainingMode;
    private ScrollRect battlePreviewTextScroll;
    private RectTransform battlePreviewTextScrollViewport;
    private RectTransform battlePreviewTextScrollContent;
    private readonly List<Button> battlePreviewDifficultyButtons = new List<Button>(5);
    private readonly List<BattleDifficultyTier> battlePreviewDifficultyButtonTiers = new List<BattleDifficultyTier>(5);
    private bool battlePreviewUsesAuthoredPuzzleLayout;
    private bool battlePreviewBossTierUnlocked;
    private int battlePreviewBossUnlockStep;
    private GameObject battlePreviewBossRevealRoot;
    private Button battlePreviewBossTierButton;
    private Button battlePreviewIntelButton;
    private bool battlePreviewDetailVisible;
    private BattleDifficultyTier? battlePreviewFeedbackDifficultyTier;
    private readonly Dictionary<int, Coroutine> battlePreviewDifficultyFeedbackAnimRoutines =
        new Dictionary<int, Coroutine>();
    private GameObject battlePreviewGaugesPanelObj;
    private GameObject battlePreviewAuthoredCopyRoot;
    private GameObject battlePreviewAuthoredDetailRoot;
    private TextMeshProUGUI battlePreviewAuthoredHeaderText;
    private TextMeshProUGUI battlePreviewAuthoredLeftTitleText;
    private TextMeshProUGUI battlePreviewAuthoredLeftDetailText;
    private TextMeshProUGUI battlePreviewAuthoredRightTitleText;
    private TextMeshProUGUI battlePreviewAuthoredRightDetailText;
    private GameObject battlePreviewArchRowRoot;
    private float battlePreviewAuthoredLayoutSy = 1f;
    private TextMeshProUGUI battlePreviewAuthoredPuzzleTitleText;
    private TextMeshProUGUI battlePreviewAuthoredPuzzleHintText;

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
    private void ShowBattlePreviewModal()
    {
        battlePreviewActivePuzzleId = BattlePreviewPuzzleIndex.RollRandomPreviewPuzzleId();
        EnsureBattlePreviewUi();
        if (battlePreviewUsesAuthoredPuzzleLayout)
        {
            ResetBossTierUnlockPuzzle();
            SyncAuthoredArchRowLayout();
        }
        RefreshBattlePreviewBodyText();
        if (battlePreviewOverlayRoot != null)
        {
            battlePreviewOverlayRoot.transform.SetAsLastSibling();
            battlePreviewOverlayRoot.SetActive(true);
            RefreshBattlePreviewTextScrollLayout();
        }
    }

    private void HideBattlePreviewModal()
    {
        if (battlePreviewUsesAuthoredPuzzleLayout)
        {
            ApplyAuthoredPreviewInitialVisibility();
            battlePreviewFeedbackDifficultyTier = null;
            RefreshAuthoredDifficultyAreaVisibility();
        }

        battlePreviewHarborTrainingMode = false;
        if (battlePreviewOverlayRoot != null)
            battlePreviewOverlayRoot.SetActive(false);
    }

    /// <summary>關閉或重開戰前預覽時，回到「訓練提示／難度拱門」初始版面（避免放棄後再開仍殘留對戰情報捲動）。</summary>
    private void ApplyAuthoredPreviewInitialVisibility()
    {
        battlePreviewDetailVisible = false;
        if (battlePreviewAuthoredCopyRoot != null)
            battlePreviewAuthoredCopyRoot.SetActive(true);
        if (battlePreviewAuthoredDetailRoot != null)
            battlePreviewAuthoredDetailRoot.SetActive(false);
        if (battlePreviewGaugesPanelObj != null)
            battlePreviewGaugesPanelObj.SetActive(false);
    }

    private void OnBattlePreviewStartClicked()
    {
        if (battlePreviewHarborTrainingMode)
        {
            ApplyHarborTrainingPendingConfig(selectedDifficultyTier);
        }
        else
        {
            BattleDifficultyConfig cfg = BuildDifficultyConfig(selectedDifficultyTier);
            pendingUseFixedEnemyDeck = cfg.UseFixedDeck;
            pendingFixedEnemyDeckCardIds = cfg.FixedDeckIds;
            pendingEnemyOverLimitAllowance = cfg.OverLimitAllowance;
            pendingMinEnemySpellsInDeck = cfg.MinSpellsInDeck;
            pendingEnemyAiPlayStyle = MapDifficultyToEnemyAiPlayStyle(selectedDifficultyTier);
            pendingDifficultyLabelZh = cfg.LabelZh;
        }

        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        if (battlePreviewHarborTrainingMode)
            BattleLaunchContext.BeginHarborTrainingGroundBattleLaunch();
        HideBattlePreviewModal();
        StartBattleSceneLoad();
    }

    private static EnemyAiPlayStyle MapDifficultyToEnemyAiPlayStyle(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro: return EnemyAiPlayStyle.IntroGreedy;
            case BattleDifficultyTier.Easy: return EnemyAiPlayStyle.EasySpellLean;
            case BattleDifficultyTier.Normal: return EnemyAiPlayStyle.Greedy;
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
            bool layoutCurrent = battlePreviewLayoutBuilt == BattlePreviewLayoutVersion
                && string.Equals(
                    battlePreviewLayoutBuiltPuzzleId,
                    battlePreviewActivePuzzleId,
                    StringComparison.Ordinal);
            if (layoutCurrent) return;
            DestroyBattlePreviewUi();
        }

        Canvas parentCanvas = ResolveBattlePreviewParentCanvas();
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
        Image panelBg = panelObj.GetComponent<Image>();
        Sprite panelSprite = ResolveBattlePreviewPanelSprite();
        bool authoredPanel = panelSprite != null;
        if (authoredPanel)
        {
            float artW = panelSprite.rect.width;
            float artH = panelSprite.rect.height;
            float maxW = Screen.width * 0.8f;
            float maxH = Screen.height * 0.8f;
            float fitScale = Mathf.Min(maxW / artW, maxH / artH);
            panelRt.sizeDelta = new Vector2(artW * fitScale, artH * fitScale);
            panelBg.sprite = panelSprite;
            panelBg.color = Color.white;
            panelBg.type = Image.Type.Simple;
            panelBg.preserveAspect = true;
        }
        else
        {
            panelRt.sizeDelta = new Vector2(Screen.width * 0.8f, Screen.height * 0.8f);
            panelBg.color = new Color(0.96f, 0.92f, 0.84f, 1f);
            panelBg.type = Image.Type.Simple;
        }

        panelBg.raycastTarget = true;
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
        panelOutline.enabled = !authoredPanel;

        GameObject titleBarObj = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
        titleBarObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleBarRt = titleBarObj.GetComponent<RectTransform>();
        titleBarRt.anchorMin = new Vector2(0f, 1f);
        titleBarRt.anchorMax = new Vector2(1f, 1f);
        titleBarRt.pivot = new Vector2(0.5f, 1f);
        titleBarRt.offsetMin = new Vector2(0f, -94f);
        titleBarRt.offsetMax = new Vector2(0f, 0f);
        Image titleBarImg = titleBarObj.GetComponent<Image>();
        titleBarImg.color = authoredPanel
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.52f, 0.59f, 0.44f, 1f);
        titleBarImg.raycastTarget = !authoredPanel;

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
        titleTmp.color = BattlePreviewInk;
        if (authoredPanel) titleObj.SetActive(false);

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
        topDividerObj.SetActive(!authoredPanel);

        GameObject chipRowObj = new GameObject("HeaderChipRow", typeof(RectTransform));
        chipRowObj.transform.SetParent(panelObj.transform, false);
        RectTransform chipRowRt = chipRowObj.GetComponent<RectTransform>();
        chipRowRt.anchorMin = new Vector2(1f, 1f);
        chipRowRt.anchorMax = new Vector2(1f, 1f);
        chipRowRt.pivot = new Vector2(1f, 1f);
        chipRowRt.anchoredPosition = new Vector2(-34f, -102f);
        chipRowRt.sizeDelta = new Vector2(360f, 28f);
        if (!authoredPanel)
        {
            CreateHeaderChip(chipRowObj.transform, "ChipThreat", "戰況摘要", new Vector2(0f, 0f), 170f);
            CreateHeaderChip(chipRowObj.transform, "ChipIntel", "作戰情報", new Vector2(200f, 0f), 150f);
        }
        else
            chipRowObj.SetActive(false);

        float panelW = panelRt.sizeDelta.x;
        float panelH = panelRt.sizeDelta.y;
        float authoredArtW = AuthoredArtWidth;
        float authoredArtH = AuthoredArtHeight;
        if (authoredPanel && panelSprite != null)
        {
            authoredArtW = panelSprite.rect.width;
            authoredArtH = panelSprite.rect.height;
        }
        float layoutSx = authoredPanel ? panelW / authoredArtW : 1f;
        float layoutSy = authoredPanel ? panelH / authoredArtH : 1f;

        const float previewFooterHeight = 100f;
        float previewContentBottom = authoredPanel ? 0f : previewFooterHeight + 12f;
        float previewContentTop = authoredPanel ? 0f : 132f;

        GameObject pressureBlockObj = new GameObject("PressureBlock", typeof(RectTransform), typeof(Image));
        pressureBlockObj.transform.SetParent(panelObj.transform, false);
        RectTransform pressureRt = pressureBlockObj.GetComponent<RectTransform>();
        if (authoredPanel)
        {
            pressureBlockObj.SetActive(false);
        }
        else
        {
            pressureRt.anchorMin = new Vector2(0f, 0f);
            pressureRt.anchorMax = new Vector2(BattlePreviewIntelColumnStart, 1f);
            pressureRt.offsetMin = new Vector2(34f, previewContentBottom);
            pressureRt.offsetMax = new Vector2(-12f, -previewContentTop);
        }

        Image pressureBg = pressureBlockObj.GetComponent<Image>();
        pressureBg.color = authoredPanel
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.88f, 0.92f, 0.83f, 1f);
        pressureBg.raycastTarget = !authoredPanel;

        GameObject pressureInnerCardObj = new GameObject("PressureInnerCard", typeof(RectTransform), typeof(Image));
        pressureInnerCardObj.transform.SetParent(pressureBlockObj.transform, false);
        RectTransform pressureInnerRt = pressureInnerCardObj.GetComponent<RectTransform>();
        pressureInnerRt.anchorMin = Vector2.zero;
        pressureInnerRt.anchorMax = Vector2.one;
        pressureInnerRt.offsetMin = authoredPanel ? Vector2.zero : new Vector2(10f, 10f);
        pressureInnerRt.offsetMax = authoredPanel ? Vector2.zero : new Vector2(-10f, -10f);
        Image pressureInnerBg = pressureInnerCardObj.GetComponent<Image>();
        pressureInnerBg.color = authoredPanel
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.95f, 0.97f, 0.91f, 1f);
        pressureInnerBg.raycastTarget = !authoredPanel;

        GameObject deckBlockObj = new GameObject("IntelBlock", typeof(RectTransform), typeof(Image));
        deckBlockObj.transform.SetParent(panelObj.transform, false);
        RectTransform deckBlockRt = deckBlockObj.GetComponent<RectTransform>();
        if (authoredPanel)
        {
            deckBlockObj.SetActive(false);
        }
        else
        {
            deckBlockRt.anchorMin = new Vector2(BattlePreviewIntelColumnStart, 0f);
            deckBlockRt.anchorMax = new Vector2(1f, 1f);
            deckBlockRt.offsetMin = new Vector2(20f, previewContentBottom);
            deckBlockRt.offsetMax = new Vector2(-34f, -previewContentTop);
        }

        Image deckBg = deckBlockObj.GetComponent<Image>();
        deckBg.color = authoredPanel
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.9f, 0.93f, 0.86f, 1f);
        deckBg.raycastTarget = !authoredPanel;

        GameObject intelInnerCardObj = new GameObject("IntelInnerCard", typeof(RectTransform), typeof(Image));
        intelInnerCardObj.transform.SetParent(deckBlockObj.transform, false);
        RectTransform intelInnerRt = intelInnerCardObj.GetComponent<RectTransform>();
        intelInnerRt.anchorMin = Vector2.zero;
        intelInnerRt.anchorMax = Vector2.one;
        intelInnerRt.offsetMin = authoredPanel
            ? new Vector2(16f * layoutSx, 14f * layoutSy)
            : new Vector2(10f, 10f);
        intelInnerRt.offsetMax = authoredPanel
            ? new Vector2(-14f * layoutSx, -12f * layoutSy)
            : new Vector2(-10f, -10f);
        Image intelInnerBg = intelInnerCardObj.GetComponent<Image>();
        intelInnerBg.color = authoredPanel
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.95f, 0.97f, 0.92f, 1f);
        intelInnerBg.raycastTarget = !authoredPanel;

        GameObject centerDividerObj = new GameObject("CenterDivider", typeof(RectTransform), typeof(Image));
        centerDividerObj.transform.SetParent(panelObj.transform, false);
        RectTransform centerDividerRt = centerDividerObj.GetComponent<RectTransform>();
        centerDividerRt.anchorMin = new Vector2(BattlePreviewIntelColumnStart, 0f);
        centerDividerRt.anchorMax = new Vector2(BattlePreviewIntelColumnStart, 1f);
        centerDividerRt.pivot = new Vector2(0.5f, 0.5f);
        centerDividerRt.offsetMin = new Vector2(0f, previewContentBottom);
        centerDividerRt.offsetMax = new Vector2(2f, -previewContentTop);
        centerDividerObj.GetComponent<Image>().color = new Color(0.61f, 0.53f, 0.35f, 0.5f);
        centerDividerObj.SetActive(!authoredPanel);

        Transform diffRailParent = authoredPanel ? panelObj.transform : pressureInnerCardObj.transform;
        GameObject diffRailObj = new GameObject("DifficultyRail", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        diffRailObj.transform.SetParent(diffRailParent, false);
        RectTransform diffRailRt = diffRailObj.GetComponent<RectTransform>();
        if (authoredPanel)
        {
            diffRailRt.anchorMin = new Vector2(AuthoredAnchorDiffXMin, AuthoredAnchorContentBottom);
            diffRailRt.anchorMax = new Vector2(AuthoredAnchorDiffXMax, AuthoredAnchorContentTop);
            diffRailRt.pivot = new Vector2(0.5f, 0.5f);
            diffRailRt.offsetMin = Vector2.zero;
            diffRailRt.offsetMax = Vector2.zero;
        }
        else
        {
            diffRailRt.anchorMin = new Vector2(0f, 0f);
            diffRailRt.anchorMax = new Vector2(0f, 1f);
            diffRailRt.pivot = new Vector2(0f, 0.5f);
            diffRailRt.anchoredPosition = Vector2.zero;
            diffRailRt.sizeDelta = new Vector2(BattlePreviewDifficultyRailWidthPx, 0f);
        }

        Image diffRailBg = diffRailObj.GetComponent<Image>();
        diffRailBg.color = authoredPanel
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.78f, 0.84f, 0.70f, 1f);
        diffRailBg.raycastTarget = !authoredPanel;
        Outline diffRailOutline = diffRailObj.AddComponent<Outline>();
        diffRailOutline.effectColor = new Color(0.32f, 0.40f, 0.26f, 0.85f);
        diffRailOutline.effectDistance = new Vector2(2f, -2f);
        diffRailOutline.enabled = !authoredPanel;
        VerticalLayoutGroup diffVlg = diffRailObj.GetComponent<VerticalLayoutGroup>();
        diffVlg.spacing = authoredPanel ? 8f : 10f;
        diffVlg.padding = authoredPanel
            ? new RectOffset(6, 6, 4, 8)
            : new RectOffset(12, 12, 14, 14);
        diffVlg.childAlignment = TextAnchor.UpperCenter;
        diffVlg.childControlWidth = true;
        diffVlg.childControlHeight = true;
        diffVlg.childForceExpandWidth = true;
        diffVlg.childForceExpandHeight = false;

        GameObject diffTitleObj = new GameObject("DifficultyTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        diffTitleObj.transform.SetParent(diffRailObj.transform, false);
        diffTitleObj.GetComponent<LayoutElement>().preferredHeight = authoredPanel ? 0f : 40f;
        TextMeshProUGUI diffTitleTmp = diffTitleObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) diffTitleTmp.font = battlePreviewFontAsset;
        diffTitleTmp.text = "難度";
        diffTitleTmp.fontSize = 30f;
        diffTitleTmp.fontStyle = FontStyles.Bold;
        diffTitleTmp.alignment = TextAlignmentOptions.Center;
        diffTitleTmp.color = new Color(0.22f, 0.28f, 0.18f, 1f);
        diffTitleObj.SetActive(!authoredPanel);

        battlePreviewUsesAuthoredPuzzleLayout = authoredPanel;
        if (authoredPanel)
        {
            if (string.IsNullOrEmpty(battlePreviewActivePuzzleId))
                battlePreviewActivePuzzleId = BattlePreviewPuzzleIndex.RollRandomPreviewPuzzleId();
            diffRailObj.SetActive(false);
            CreateAuthoredPuzzleDifficultyButtons(panelObj.transform, layoutSx, layoutSy);
            if (!battlePreviewHarborTrainingMode)
                CreateAuthoredBossTierReveal(panelObj.transform, layoutSx, layoutSy);
        }
        else
            CreateDifficultyButtons(diffRailObj.transform, false, layoutSx, layoutSy);

        Transform summaryParent = authoredPanel ? pressureInnerCardObj.transform : pressureInnerCardObj.transform;
        GameObject summaryPaneObj = new GameObject("SummaryPane", typeof(RectTransform));
        summaryPaneObj.transform.SetParent(summaryParent, false);
        RectTransform summaryPaneRt = summaryPaneObj.GetComponent<RectTransform>();
        summaryPaneRt.anchorMin = Vector2.zero;
        summaryPaneRt.anchorMax = Vector2.one;
        summaryPaneRt.offsetMin = authoredPanel ? Vector2.zero : new Vector2(BattlePreviewSummaryPaneLeftInsetPx, 8f);
        summaryPaneRt.offsetMax = authoredPanel ? Vector2.zero : new Vector2(-8f, -8f);

        const float gaugeReservePx = 178f;
        if (authoredPanel)
        {
            CreateAuthoredPuzzleCopyLayout(panelObj.transform);
            CreateAuthoredBattlePreviewDetailScroll(panelObj.transform);
        }
        else
        {
            battlePreviewTextScroll = null;
            battlePreviewTextScrollViewport = null;
            battlePreviewTextScrollContent = null;
            battlePreviewPressureText = CreatePreviewBlockText(summaryPaneObj.transform, "PressureText");
            if (battlePreviewPressureText != null)
            {
                RectTransform pressureTextRt = battlePreviewPressureText.rectTransform;
                pressureTextRt.offsetMin = new Vector2(12f, gaugeReservePx);
                pressureTextRt.offsetMax = new Vector2(-12f, -8f);
                battlePreviewPressureText.fontSize = 28f;
                battlePreviewPressureText.lineSpacing = 8f;
            }

            battlePreviewDeckText = CreatePreviewBlockText(intelInnerCardObj.transform, "IntelText");
        }

        Transform gaugeParent = authoredPanel ? panelObj.transform : summaryPaneObj.transform;
        CreatePressureMetricsChart(gaugeParent, authoredPanel, layoutSx, layoutSy);

        battlePreviewStartButton = CreateModalButton(panelObj.transform, "StartBattleButton", "開始對戰");
        battlePreviewStartButton.onClick.AddListener(OnBattlePreviewStartClicked);
        battlePreviewGiveUpButton = CreateModalButton(panelObj.transform, "GiveUpButton", "放棄");
        battlePreviewGiveUpButton.onClick.AddListener(OnBattlePreviewGiveUpClicked);

        if (authoredPanel)
        {
            ApplyAuthoredAnchorRect(
                battlePreviewStartButton.GetComponent<RectTransform>(),
                AuthoredStartBattleAnchorXMin,
                AuthoredStartBattleAnchorXMax,
                AuthoredStartBattleAnchorYMin,
                AuthoredStartBattleAnchorYMax);
            ApplyAuthoredAnchorRect(
                battlePreviewGiveUpButton.GetComponent<RectTransform>(),
                AuthoredGiveUpAnchorXMin,
                AuthoredGiveUpAnchorXMax,
                AuthoredGiveUpAnchorYMin,
                AuthoredGiveUpAnchorYMax);
        }
        else
        {
            float footerBtnW = 300f;
            float footerBtnH = 72f;
            float footerBtnY = 16f;
            float footerBtnSpread = 150f;
            RectTransform startRt = battlePreviewStartButton.GetComponent<RectTransform>();
            startRt.anchorMin = new Vector2(0.5f, 0f);
            startRt.anchorMax = new Vector2(0.5f, 0f);
            startRt.pivot = new Vector2(0.5f, 0f);
            startRt.anchoredPosition = new Vector2(footerBtnSpread, footerBtnY);
            startRt.sizeDelta = new Vector2(footerBtnW, footerBtnH);
            RectTransform giveUpRt = battlePreviewGiveUpButton.GetComponent<RectTransform>();
            giveUpRt.anchorMin = new Vector2(0.5f, 0f);
            giveUpRt.anchorMax = new Vector2(0.5f, 0f);
            giveUpRt.pivot = new Vector2(0.5f, 0f);
            giveUpRt.anchoredPosition = new Vector2(-footerBtnSpread, footerBtnY);
            giveUpRt.sizeDelta = new Vector2(footerBtnW, footerBtnH);
        }

        if (authoredPanel)
            ConfigureAuthoredPuzzleChrome(panelObj.transform, layoutSx, layoutSy);

        battlePreviewPanelRt = panelObj.GetComponent<RectTransform>();
        battlePreviewOverlayRoot = overlay;
        battlePreviewLayoutBuilt = BattlePreviewLayoutVersion;
        battlePreviewLayoutBuiltPuzzleId = battlePreviewActivePuzzleId;
        battlePreviewOverlayRoot.SetActive(false);
    }

    private void DestroyBattlePreviewUi()
    {
        if (battlePreviewMetricAnimRoutine != null)
        {
            StopCoroutine(battlePreviewMetricAnimRoutine);
            battlePreviewMetricAnimRoutine = null;
        }
        StopAllAuthoredDifficultyFeedbackAnims();
        StopBossUnlockRevealFx();
        if (battlePreviewOverlayRoot != null)
            Destroy(battlePreviewOverlayRoot);
        battlePreviewOverlayRoot = null;
        battlePreviewPanelRt = null;
        battlePreviewPressureText = null;
        battlePreviewDeckText = null;
        battlePreviewTextScroll = null;
        battlePreviewTextScrollViewport = null;
        battlePreviewTextScrollContent = null;
        battlePreviewStartButton = null;
        battlePreviewGiveUpButton = null;
        battlePreviewMetricBarFills.Clear();
        battlePreviewMetricValueTexts.Clear();
        battlePreviewDifficultyButtons.Clear();
        battlePreviewDifficultyButtonTiers.Clear();
        battlePreviewUsesAuthoredPuzzleLayout = false;
        battlePreviewHarborTrainingMode = false;
        battlePreviewBossRevealRoot = null;
        battlePreviewBossTierButton = null;
        battlePreviewIntelButton = null;
        battlePreviewFeedbackDifficultyTier = null;
        battlePreviewGaugesPanelObj = null;
        battlePreviewAuthoredCopyRoot = null;
        battlePreviewAuthoredDetailRoot = null;
        battlePreviewAuthoredHeaderText = null;
        battlePreviewAuthoredLeftTitleText = null;
        battlePreviewAuthoredLeftDetailText = null;
        battlePreviewAuthoredRightTitleText = null;
        battlePreviewAuthoredRightDetailText = null;
        battlePreviewArchRowRoot = null;
        battlePreviewAuthoredLayoutSy = 1f;
        battlePreviewAuthoredPuzzleTitleText = null;
        battlePreviewAuthoredPuzzleHintText = null;
        battlePreviewLayoutBuilt = 0;
        battlePreviewLayoutBuiltPuzzleId = null;
        _cachedDefaultBattlePreviewPanelSprite = null;
        CachedDifficultyTierSprites.Clear();
    }

    private Canvas ResolveBattlePreviewParentCanvas()
    {
        if (enterBattleButton != null)
        {
            Canvas fromButton = enterBattleButton.GetComponentInParent<Canvas>();
            if (fromButton != null && fromButton.isActiveAndEnabled)
                return fromButton;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        int bestOrder = int.MinValue;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            if (activeScene.IsValid() && c.gameObject.scene != activeScene) continue;
            if (string.Equals(c.gameObject.name, "GlobalNavCanvas", StringComparison.Ordinal)) continue;
            if (c.sortingOrder < bestOrder) continue;
            best = c;
            bestOrder = c.sortingOrder;
        }

        return best;
    }

    private Coroutine battlePreviewBlockedMessageRoutine;

    private void ShowBattlePreviewBlockedMessage(string message)
    {
        Canvas canvas = ResolveBattlePreviewParentCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("SceneLoader: " + message);
            return;
        }

        if (battlePreviewBlockedMessageRoutine != null)
        {
            StopCoroutine(battlePreviewBlockedMessageRoutine);
            battlePreviewBlockedMessageRoutine = null;
        }

        battlePreviewBlockedMessageRoutine = StartCoroutine(CoShowBattlePreviewBlockedMessage(canvas, message));
    }

    private IEnumerator CoShowBattlePreviewBlockedMessage(Canvas canvas, string message)
    {
        GameObject toastRoot = new GameObject("BattlePreviewBlockedToast", typeof(RectTransform));
        toastRoot.transform.SetParent(canvas.transform, false);
        toastRoot.transform.SetAsLastSibling();

        RectTransform toastRt = toastRoot.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.5f, 0.5f);
        toastRt.anchorMax = new Vector2(0.5f, 0.5f);
        toastRt.pivot = new Vector2(0.5f, 0.5f);
        toastRt.sizeDelta = new Vector2(640f, 96f);

        Image bg = toastRoot.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.08f, 0.06f, 0.88f);
        bg.raycastTarget = false;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(toastRoot.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(24f, 12f);
        labelRt.offsetMax = new Vector2(-24f, -12f);

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) tmp.font = battlePreviewFontAsset;
        else SettingsUiFonts.ApplyTo(tmp);
        tmp.text = message;
        tmp.fontSize = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = BattleUiColors.BtnFooterLabelText;
        tmp.raycastTarget = false;

        yield return new WaitForSecondsRealtime(2.2f);
        if (toastRoot != null)
            Destroy(toastRoot);
        battlePreviewBlockedMessageRoutine = null;
    }

    private Sprite ResolveDifficultyTierSprite(BattleDifficultyTier tier)
    {
        Sprite assigned = GetAssignedDifficultyTierSprite(tier);
        if (assigned != null) return assigned;

        if (CachedDifficultyTierSprites.TryGetValue(tier, out Sprite cached) && cached != null)
            return cached;

        string resourceName = GetDifficultyTierResourceName(tier);
        if (string.IsNullOrEmpty(resourceName)) return null;

        string path = DifficultyLevelResourceRoot + "/" + resourceName;
        Sprite loaded = Resources.Load<Sprite>(path);
        if (loaded == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
                loaded = sprites[0];
        }

        if (loaded != null)
            CachedDifficultyTierSprites[tier] = loaded;
        return loaded;
    }

    private Sprite GetAssignedDifficultyTierSprite(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro: return difficultySpriteBasics;
            case BattleDifficultyTier.Easy: return difficultySpriteEasy;
            case BattleDifficultyTier.Normal: return difficultySpriteNormal;
            case BattleDifficultyTier.Hard: return difficultySpriteHard;
            case BattleDifficultyTier.Boss: return difficultySpriteBoss;
            default: return null;
        }
    }

    private static string GetDifficultyTierResourceName(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro: return "Basics";
            case BattleDifficultyTier.Easy: return "Easy";
            case BattleDifficultyTier.Normal: return "Normal";
            case BattleDifficultyTier.Hard: return "Hard";
            case BattleDifficultyTier.Boss: return "Boss";
            default: return null;
        }
    }

    private Sprite ResolveBattlePreviewPanelSprite()
    {
        if (battlePreviewPanelSprite != null) return battlePreviewPanelSprite;
        if (_cachedDefaultBattlePreviewPanelSprite != null) return _cachedDefaultBattlePreviewPanelSprite;

        _cachedDefaultBattlePreviewPanelSprite = Resources.Load<Sprite>(DefaultBattlePreviewPanelResourcePath);
        if (_cachedDefaultBattlePreviewPanelSprite != null)
            return _cachedDefaultBattlePreviewPanelSprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(DefaultBattlePreviewPanelResourcePath);
        if (sprites == null || sprites.Length == 0)
            return null;

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite s = sprites[i];
            if (s == null) continue;
            if (s.name.IndexOf("pre-war", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _cachedDefaultBattlePreviewPanelSprite = s;
                return _cachedDefaultBattlePreviewPanelSprite;
            }
        }

        _cachedDefaultBattlePreviewPanelSprite = sprites[0];
        return _cachedDefaultBattlePreviewPanelSprite;
    }

    private TextMeshProUGUI CreatePreviewBlockText(Transform parent, string objName, bool asScrollChild = false)
    {
        GameObject bodyObj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(parent, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        if (asScrollChild)
        {
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0f, 1f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;
            ContentSizeFitter csf = bodyObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutElement le = bodyObj.AddComponent<LayoutElement>();
            le.minWidth = 280f;
        }
        else
        {
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(20f, 20f);
            bodyRt.offsetMax = new Vector2(-20f, -20f);
        }

        TextMeshProUGUI tmp = bodyObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) tmp.font = battlePreviewFontAsset;
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = asScrollChild ? BattlePreviewInk : new Color(0.2f, 0.17f, 0.12f, 1f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        tmp.lineSpacing = 8f;
        return tmp;
    }

    private static void ApplyAuthoredAnchorRect(
        RectTransform rt,
        float anchorXMin,
        float anchorXMax,
        float anchorYMin,
        float anchorYMax)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(anchorXMin, anchorYMin);
        rt.anchorMax = new Vector2(anchorXMax, anchorYMax);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateAuthoredAnchoredLabel(
        Transform parent,
        string objName,
        float anchorXMin,
        float anchorXMax,
        float anchorYMin,
        float anchorYMax,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject labelObj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(parent, false);
        ApplyAuthoredAnchorRect(
            labelObj.GetComponent<RectTransform>(),
            anchorXMin,
            anchorXMax,
            anchorYMin,
            anchorYMax);
        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) tmp.font = battlePreviewFontAsset;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = BattlePreviewInk;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void CreateAuthoredPuzzleCopyLayout(Transform panel)
    {
        battlePreviewAuthoredCopyRoot = new GameObject("AuthoredPuzzleCopy", typeof(RectTransform));
        battlePreviewAuthoredCopyRoot.transform.SetParent(panel, false);
        ApplyAuthoredAnchorRect(
            battlePreviewAuthoredCopyRoot.GetComponent<RectTransform>(),
            0f,
            1f,
            0f,
            1f);

        battlePreviewAuthoredHeaderText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "HeaderTitle",
            AuthoredHeaderAnchorXMin,
            AuthoredHeaderAnchorXMax,
            AuthoredHeaderAnchorYMin,
            AuthoredHeaderAnchorYMax,
            42f,
            TextAlignmentOptions.Center);
        battlePreviewAuthoredLeftTitleText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "LeftRewardTitle",
            AuthoredLeftColumnAnchorXMin,
            AuthoredLeftColumnAnchorXMax,
            AuthoredSideColumnTitleYMin,
            AuthoredSideColumnTitleYMax,
            26f,
            TextAlignmentOptions.Center);
        battlePreviewAuthoredLeftDetailText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "LeftRewardDetail",
            AuthoredLeftColumnAnchorXMin,
            AuthoredLeftColumnAnchorXMax,
            AuthoredSideColumnDetailYMin,
            AuthoredSideColumnDetailYMax,
            22f,
            TextAlignmentOptions.Center);
        battlePreviewAuthoredRightTitleText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "RightPenaltyTitle",
            AuthoredRightColumnAnchorXMin,
            AuthoredRightColumnAnchorXMax,
            AuthoredSideColumnTitleYMin,
            AuthoredSideColumnTitleYMax,
            26f,
            TextAlignmentOptions.Center);
        battlePreviewAuthoredRightDetailText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "RightPenaltyDetail",
            AuthoredRightColumnAnchorXMin,
            AuthoredRightColumnAnchorXMax,
            AuthoredSideColumnDetailYMin,
            AuthoredSideColumnDetailYMax,
            22f,
            TextAlignmentOptions.Center);
        if (battlePreviewAuthoredLeftDetailText != null)
            battlePreviewAuthoredLeftDetailText.color = BattlePreviewInkMuted;
        if (battlePreviewAuthoredRightDetailText != null)
            battlePreviewAuthoredRightDetailText.color = BattlePreviewInkMuted;
        battlePreviewAuthoredPuzzleTitleText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "PuzzleTitle",
            AuthoredPuzzleCenterAnchorXMin,
            AuthoredPuzzleCenterAnchorXMax,
            AuthoredSideColumnTitleYMin,
            AuthoredSideColumnTitleYMax,
            34f,
            TextAlignmentOptions.Center);
        battlePreviewAuthoredPuzzleHintText = CreateAuthoredAnchoredLabel(
            battlePreviewAuthoredCopyRoot.transform,
            "PuzzleHint",
            AuthoredPuzzleCenterAnchorXMin,
            AuthoredPuzzleCenterAnchorXMax,
            AuthoredPuzzleHintAnchorYMin,
            AuthoredPuzzleHintAnchorYMax,
            26f,
            TextAlignmentOptions.Center);
    }

    private void CreateAuthoredBattlePreviewDetailScroll(Transform panel)
    {
        battlePreviewAuthoredDetailRoot = new GameObject("AuthoredDetailLayer", typeof(RectTransform));
        battlePreviewAuthoredDetailRoot.transform.SetParent(panel, false);
        ApplyAuthoredAnchorRect(
            battlePreviewAuthoredDetailRoot.GetComponent<RectTransform>(),
            0f,
            1f,
            0f,
            1f);
        battlePreviewAuthoredDetailRoot.SetActive(false);

        GameObject viewportObj = new GameObject(
            "TextScrollViewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(RectMask2D),
            typeof(ScrollRect));
        viewportObj.transform.SetParent(battlePreviewAuthoredDetailRoot.transform, false);
        battlePreviewTextScrollViewport = viewportObj.GetComponent<RectTransform>();
        battlePreviewTextScrollViewport.anchorMin = new Vector2(
            AuthoredPuzzleCenterAnchorXMin,
            AuthoredDetailScrollAnchorYMin);
        battlePreviewTextScrollViewport.anchorMax = new Vector2(
            AuthoredPuzzleCenterAnchorXMax,
            AuthoredDetailScrollAnchorYMax);
        battlePreviewTextScrollViewport.offsetMin = Vector2.zero;
        battlePreviewTextScrollViewport.offsetMax = Vector2.zero;
        Image viewportImg = viewportObj.GetComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0f);
        viewportImg.raycastTarget = true;

        GameObject contentObj = new GameObject(
            "TextScrollContent",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        battlePreviewTextScrollContent = contentObj.GetComponent<RectTransform>();
        battlePreviewTextScrollContent.anchorMin = new Vector2(0f, 1f);
        battlePreviewTextScrollContent.anchorMax = new Vector2(1f, 1f);
        battlePreviewTextScrollContent.pivot = new Vector2(0.5f, 1f);
        battlePreviewTextScrollContent.anchoredPosition = Vector2.zero;
        battlePreviewTextScrollContent.sizeDelta = Vector2.zero;
        ContentSizeFitter contentCsf = contentObj.GetComponent<ContentSizeFitter>();
        contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup contentVlg = contentObj.GetComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 12f;
        contentVlg.padding = new RectOffset(16, 16, 14, 14);
        contentVlg.childAlignment = TextAnchor.UpperLeft;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        battlePreviewTextScroll = viewportObj.GetComponent<ScrollRect>();
        battlePreviewTextScroll.content = battlePreviewTextScrollContent;
        battlePreviewTextScroll.viewport = battlePreviewTextScrollViewport;
        battlePreviewTextScroll.horizontal = false;
        battlePreviewTextScroll.vertical = true;
        battlePreviewTextScroll.movementType = ScrollRect.MovementType.Clamped;
        battlePreviewTextScroll.scrollSensitivity = 28f;
        battlePreviewTextScroll.inertia = true;

        battlePreviewPressureText = CreatePreviewBlockText(contentObj.transform, "PressureText", asScrollChild: true);
        if (battlePreviewPressureText != null)
        {
            battlePreviewPressureText.fontSize = 27f;
            battlePreviewPressureText.lineSpacing = 6f;
            battlePreviewPressureText.alignment = TextAlignmentOptions.TopLeft;
        }

        battlePreviewDeckText = CreatePreviewBlockText(contentObj.transform, "IntelText", asScrollChild: true);
        if (battlePreviewDeckText != null)
        {
            battlePreviewDeckText.fontSize = 27f;
            battlePreviewDeckText.lineSpacing = 4f;
            battlePreviewDeckText.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private void RefreshBattlePreviewTextScrollLayout()
    {
        if (battlePreviewTextScrollContent == null || battlePreviewTextScrollViewport == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(battlePreviewTextScrollContent);
        float viewportHeight = battlePreviewTextScrollViewport.rect.height;
        float preferredHeight = LayoutUtility.GetPreferredHeight(battlePreviewTextScrollContent);
        float contentHeight = Mathf.Max(preferredHeight, viewportHeight);
        battlePreviewTextScrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        if (battlePreviewTextScroll != null)
            battlePreviewTextScroll.verticalNormalizedPosition = 1f;
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

    private void CreatePressureMetricsChart(Transform parent, bool transparentChrome, float layoutSx, float layoutSy)
    {
        battlePreviewMetricBarFills.Clear();
        battlePreviewMetricValueTexts.Clear();

        GameObject panelObj = new GameObject("PressureGaugesPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObj.transform.SetParent(parent, false);
        if (transparentChrome)
            battlePreviewGaugesPanelObj = panelObj;
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        if (transparentChrome)
        {
            panelRt.anchorMin = new Vector2(0.08f, AuthoredGaugeAnchorYMin);
            panelRt.anchorMax = new Vector2(0.92f, AuthoredGaugeAnchorYMax);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = Vector2.zero;
        }
        else
        {
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            float gaugeHeight = 168f;
            float gaugeBottom = 10f;
            float gaugeSide = 16f;
            panelRt.anchoredPosition = new Vector2(0f, gaugeBottom);
            panelRt.sizeDelta = new Vector2(-gaugeSide * 2f, gaugeHeight);
        }
        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = transparentChrome
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0.36f, 0.42f, 0.30f, 0.22f);
        panelBg.raycastTarget = !transparentChrome;
        Outline panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.55f, 0.62f, 0.45f, 0.55f);
        panelOutline.effectDistance = new Vector2(1f, -1f);
        panelOutline.enabled = !transparentChrome;
        VerticalLayoutGroup panelVlg = panelObj.GetComponent<VerticalLayoutGroup>();
        panelVlg.spacing = 12f;
        panelVlg.padding = new RectOffset(14, 14, 12, 12);
        panelVlg.childAlignment = transparentChrome ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
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
        headerTmp.color = transparentChrome ? BattlePreviewInk : new Color(0.88f, 0.94f, 0.82f, 1f);
        headerTmp.alignment = transparentChrome
            ? TextAlignmentOptions.Center
            : TextAlignmentOptions.MidlineLeft;

        CreateOnePressureGaugeRow(panelObj.transform, "Threat", "壓制", new Color(0.55f, 0.88f, 0.58f, 1f), transparentChrome);
        CreateOnePressureGaugeRow(panelObj.transform, "Burst", "爆發", new Color(1f, 0.78f, 0.38f, 1f), transparentChrome);
        CreateOnePressureGaugeRow(panelObj.transform, "Tempo", "節奏", new Color(1f, 0.62f, 0.34f, 1f), transparentChrome);
    }

    private void CreateOnePressureGaugeRow(Transform parent, string key, string label, Color fillColor, bool transparentChrome)
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
        labelTmp.color = transparentChrome ? BattlePreviewInk : new Color(0.90f, 0.95f, 0.86f, 1f);

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
        if (battlePreviewUsesAuthoredPuzzleLayout)
        {
            if (!battlePreviewDetailVisible)
            {
                RefreshAuthoredPuzzlePresentationText();
                RefreshDifficultyButtonVisuals();
                return;
            }

            if (battlePreviewPressureText == null || battlePreviewDeckText == null)
                return;
        }
        else if (battlePreviewPressureText == null || battlePreviewDeckText == null)
        {
            return;
        }

        if (playerData == null) playerData = PlayerData.ResolveCanonical();
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
        RefreshBattlePreviewTextScrollLayout();
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
            case EnemyAiPlayStyle.IntroGreedy:
                return "優先出怪, 法術較少";
            case EnemyAiPlayStyle.EasySpellLean:
                return "略偏法術, 即時出牌";
            case EnemyAiPlayStyle.FastAttack:
                return "快攻型, 早出怪壓迫";
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
            case EnemyAiPlayStyle.IntroGreedy:
                return "入門 AI: 以 Greedy 為基礎, 降低法術出牌評分, 較常先上場怪物.";
            case EnemyAiPlayStyle.EasySpellLean:
                return "簡單 AI: 以 Greedy 為基礎, 提高法術出牌評分, 較常施放法術.";
            case EnemyAiPlayStyle.FastAttack:
                return "快攻 AI: 以 Greedy 為基礎, 強烈優先出場怪與直傷法術, 壓迫玩家血線.";
            case EnemyAiPlayStyle.SchemingHard:
                return "困難 AI: 傾向保留 SR 以上卡牌, 在場面有利或需要斬殺時才打出.";
            case EnemyAiPlayStyle.SchemingBoss:
                return "魔王 AI: 傾向保留 R 以上卡牌, 囤牌條件比困難更嚴, 回合壓力更大.";
            default:
                return "普通 AI: 每回合在可出牌中選評分最高者立即打出.";
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

    private void ResetBossTierUnlockPuzzle()
    {
        StopBossUnlockRevealFx();
        battlePreviewBossTierUnlocked = false;
        battlePreviewBossUnlockStep = 0;
        ApplyAuthoredPreviewInitialVisibility();
        battlePreviewFeedbackDifficultyTier = null;
        selectedDifficultyTier = BattleDifficultyTier.Normal;
        RefreshAuthoredDifficultyAreaVisibility();
    }

    /// <summary>美術模式難度鈕：再點同一難度則關閉回饋。回傳 true 表示本次為選取（顯示回饋）。</summary>
    private bool ToggleAuthoredDifficultyFeedback(BattleDifficultyTier tier)
    {
        if (battlePreviewFeedbackDifficultyTier == tier)
        {
            battlePreviewFeedbackDifficultyTier = null;
            return false;
        }

        battlePreviewFeedbackDifficultyTier = tier;
        return true;
    }

    private void RefreshAuthoredPuzzlePresentationText()
    {
        if (battlePreviewAuthoredHeaderText == null) return;

        if (battlePreviewHarborTrainingMode)
        {
            battlePreviewAuthoredHeaderText.text = SafePreviewText(HarborTrainingBattleCopy.PreviewHeaderRich);
            if (battlePreviewAuthoredLeftTitleText != null)
                battlePreviewAuthoredLeftTitleText.text = SafePreviewText(HarborTrainingBattleCopy.PreviewLeftTitleRich);
            if (battlePreviewAuthoredLeftDetailText != null)
                battlePreviewAuthoredLeftDetailText.text = SafePreviewText(HarborTrainingBattleCopy.PreviewLeftDetailRich);
            if (battlePreviewAuthoredRightTitleText != null)
                battlePreviewAuthoredRightTitleText.text = SafePreviewText(HarborTrainingBattleCopy.PreviewRightTitleRich);
            if (battlePreviewAuthoredRightDetailText != null)
                battlePreviewAuthoredRightDetailText.text = SafePreviewText(HarborTrainingBattleCopy.PreviewRightDetailRich);
            if (battlePreviewAuthoredPuzzleTitleText != null)
                battlePreviewAuthoredPuzzleTitleText.text = SafePreviewText(HarborTrainingBattleCopy.PreviewGoalRich);
            if (battlePreviewAuthoredPuzzleHintText != null)
            {
                BattleDifficultyConfig cfg = BuildDifficultyConfig(selectedDifficultyTier);
                battlePreviewAuthoredPuzzleHintText.text = SafePreviewText(
                    "<color=#43573A>目前選擇 " + cfg.LabelZh + " 敵方 AI 快攻型</color>");
            }

            if (battlePreviewGaugesPanelObj != null && !battlePreviewDetailVisible)
                battlePreviewGaugesPanelObj.SetActive(false);
            return;
        }

        battlePreviewAuthoredHeaderText.text = SafePreviewText(BattlePreviewPuzzleIndex.HeaderSelectDifficultyRich);
        if (battlePreviewAuthoredLeftTitleText != null)
            battlePreviewAuthoredLeftTitleText.text = SafePreviewText("<b>" + BattlePreviewPuzzleIndex.LeftColumnTitle + "</b>");
        if (battlePreviewAuthoredLeftDetailText != null)
            battlePreviewAuthoredLeftDetailText.text = SafePreviewText(BattlePreviewPuzzleIndex.LeftColumnDetailPlaceholder);
        if (battlePreviewAuthoredRightTitleText != null)
            battlePreviewAuthoredRightTitleText.text = SafePreviewText("<b>" + BattlePreviewPuzzleIndex.RightColumnTitle + "</b>");
        if (battlePreviewAuthoredRightDetailText != null)
            battlePreviewAuthoredRightDetailText.text = SafePreviewText(BattlePreviewPuzzleIndex.RightColumnDetailPlaceholder);

        string puzzleId = battlePreviewActivePuzzleId;
        if (battlePreviewBossTierUnlocked)
        {
            BattleDifficultyConfig revealCfg = BuildDifficultyConfig(battlePreviewAuthoredRevealTier);
            battlePreviewAuthoredPuzzleTitleText.text = SafePreviewText(
                BattlePreviewPuzzleIndex.GetPuzzleTitleUnlockedRich(puzzleId));
            battlePreviewAuthoredPuzzleHintText.text = SafePreviewText(
                BattlePreviewPuzzleIndex.BuildPuzzleHintUnlockedRich(revealCfg.LabelZh));
        }
        else
        {
            battlePreviewAuthoredPuzzleTitleText.text = SafePreviewText(
                BattlePreviewPuzzleIndex.GetPuzzleTitleLockedRich(puzzleId));
            battlePreviewAuthoredPuzzleHintText.text = SafePreviewText(
                "<color=#6C533D>" + BattlePreviewPuzzleIndex.GetPuzzleHintLocked(puzzleId) + "</color>");
        }

        if (battlePreviewGaugesPanelObj != null && !battlePreviewDetailVisible)
            battlePreviewGaugesPanelObj.SetActive(false);
    }

    private void SetAuthoredArchButtonsVisible(bool visible)
    {
        if (battlePreviewArchRowRoot != null)
            battlePreviewArchRowRoot.SetActive(visible);
        else
        {
            for (int i = 0; i < battlePreviewDifficultyButtons.Count; i++)
            {
                Button btn = battlePreviewDifficultyButtons[i];
                if (btn != null)
                    btn.gameObject.SetActive(visible);
            }
        }
    }

    private void RefreshAuthoredDifficultyAreaVisibility()
    {
        if (!battlePreviewUsesAuthoredPuzzleLayout)
            return;

        if (battlePreviewHarborTrainingMode)
        {
            SetAuthoredArchButtonsVisible(!battlePreviewDetailVisible);
            if (battlePreviewBossRevealRoot != null)
                battlePreviewBossRevealRoot.SetActive(false);
            return;
        }

        if (battlePreviewDetailVisible)
        {
            SetAuthoredArchButtonsVisible(false);
            if (battlePreviewBossRevealRoot != null)
                battlePreviewBossRevealRoot.SetActive(false);
            return;
        }

        if (battlePreviewBossTierUnlocked)
        {
            if (BattlePreviewPuzzleIndex.RevealsHiddenTierInFourthArchSlot(battlePreviewActivePuzzleId))
            {
                SetAuthoredArchButtonsVisible(true);
                if (battlePreviewBossRevealRoot != null)
                    battlePreviewBossRevealRoot.SetActive(false);
            }
            else
            {
                SetAuthoredArchButtonsVisible(false);
                if (battlePreviewBossRevealRoot != null)
                    battlePreviewBossRevealRoot.SetActive(true);
            }
        }
        else
        {
            SetAuthoredArchButtonsVisible(true);
            if (battlePreviewBossRevealRoot != null)
                battlePreviewBossRevealRoot.SetActive(false);
        }
    }

    private void OnAuthoredArchSlotClicked(int archIndex)
    {
        if (archIndex < 0 || archIndex >= battlePreviewDifficultyButtonTiers.Count)
            return;
        OnAuthoredDifficultyTierClicked(battlePreviewDifficultyButtonTiers[archIndex]);
    }

    private void OnAuthoredDifficultyTierClicked(BattleDifficultyTier tier)
    {
        if (battlePreviewBossUnlockAnimating)
            return;

        if (battlePreviewHarborTrainingMode)
        {
            selectedDifficultyTier = tier;
            battlePreviewFeedbackDifficultyTier = tier;
            battlePreviewDetailVisible = true;
            if (battlePreviewAuthoredCopyRoot != null)
                battlePreviewAuthoredCopyRoot.SetActive(false);
            if (battlePreviewAuthoredDetailRoot != null)
                battlePreviewAuthoredDetailRoot.SetActive(true);
            RefreshBattlePreviewBodyText();
            RefreshDifficultyButtonVisuals();
            RefreshAuthoredDifficultyAreaVisibility();
            return;
        }

        bool feedbackActivated = ToggleAuthoredDifficultyFeedback(tier);

        if (battlePreviewBossTierUnlocked)
        {
            if (feedbackActivated)
                selectedDifficultyTier = tier;
            else if (selectedDifficultyTier == tier)
                selectedDifficultyTier = BattleDifficultyTier.Normal;
            RefreshBattlePreviewBodyText();
            RefreshDifficultyButtonVisuals();
            return;
        }

        if (feedbackActivated)
        {
            if (BattlePreviewPuzzleIndex.UsesUnlockClickSequence(battlePreviewActivePuzzleId))
            {
                if (BattlePreviewPuzzleIndex.IsCorrectUnlockStep(
                        battlePreviewActivePuzzleId, tier, battlePreviewBossUnlockStep))
                {
                    battlePreviewBossUnlockStep++;
                    if (BattlePreviewPuzzleIndex.IsUnlockSequenceComplete(battlePreviewBossUnlockStep))
                        UnlockAuthoredRevealTierForPreview();
                }
                else
                    battlePreviewBossUnlockStep = 0;
            }

            selectedDifficultyTier = tier;
        }
        else if (selectedDifficultyTier == tier)
            selectedDifficultyTier = BattleDifficultyTier.Normal;

        RefreshBattlePreviewBodyText();
        RefreshDifficultyButtonVisuals();
    }

    private void UnlockAuthoredRevealTierForPreview()
    {
        if (battlePreviewBossUnlockAnimating)
            return;
        StopBossUnlockRevealFx();
        battlePreviewBossUnlockFxRoutine =
            battlePreviewActivePuzzleId == BattlePreviewPuzzleIndex.Pz02FindHardDifficulty
                ? StartCoroutine(CoUnlockPz02HardFourthArchFx())
                : StartCoroutine(CoUnlockBossTierRevealFx());
    }

    private void CompleteBossTierUnlockState()
    {
        battlePreviewBossTierUnlocked = true;
        if (BattlePreviewPuzzleIndex.RevealsHiddenTierInFourthArchSlot(battlePreviewActivePuzzleId))
            ApplyHardDifficultyToFourthArchSlot();
        battlePreviewFeedbackDifficultyTier = battlePreviewAuthoredRevealTier;
        selectedDifficultyTier = battlePreviewAuthoredRevealTier;
        RefreshAuthoredDifficultyAreaVisibility();
        RefreshBattlePreviewBodyText();
        RefreshDifficultyButtonVisuals();
    }

    private void ApplyHardDifficultyToFourthArchSlot()
    {
        int slotIndex = BattlePreviewPuzzleIndex.Pz02HardUnlockArchSlotIndex;
        if (slotIndex < 0 || slotIndex >= battlePreviewDifficultyButtons.Count)
            return;

        battlePreviewDifficultyButtonTiers[slotIndex] = BattleDifficultyTier.Hard;
        Button btn = battlePreviewDifficultyButtons[slotIndex];
        if (btn == null)
            return;

        Image img = btn.GetComponent<Image>();
        Sprite hardSprite = ResolveDifficultyTierSprite(BattleDifficultyTier.Hard);
        if (img != null && hardSprite != null)
        {
            img.sprite = hardSprite;
            img.color = Color.white;
            img.preserveAspect = true;
        }

        ApplyAuthoredArchButtonLayoutSize(
            btn.GetComponent<LayoutElement>(),
            img,
            battlePreviewAuthoredLayoutSy);
        SyncAuthoredArchRowLayout();
    }

    private void OnBattlePreviewIntelClicked()
    {
        battlePreviewDetailVisible = !battlePreviewDetailVisible;
        if (battlePreviewAuthoredCopyRoot != null)
            battlePreviewAuthoredCopyRoot.SetActive(!battlePreviewDetailVisible);
        if (battlePreviewAuthoredDetailRoot != null)
            battlePreviewAuthoredDetailRoot.SetActive(battlePreviewDetailVisible);
        RefreshAuthoredDifficultyAreaVisibility();
        RefreshBattlePreviewBodyText();
        if (battlePreviewDetailVisible)
            RefreshBattlePreviewTextScrollLayout();
    }

    private void CreateAuthoredPuzzleDifficultyButtons(Transform panel, float layoutSx, float layoutSy)
    {
        EnsureDifficultyProfiles();
        battlePreviewDifficultyButtons.Clear();
        battlePreviewDifficultyButtonTiers.Clear();
        battlePreviewAuthoredLayoutSy = layoutSy;

        battlePreviewArchRowRoot = new GameObject(
            "DifficultyArchRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(CanvasGroup));
        battlePreviewArchRowRoot.transform.SetParent(panel, false);
        ApplyAuthoredAnchorRect(
            battlePreviewArchRowRoot.GetComponent<RectTransform>(),
            AuthoredArchRowContainerXMin,
            AuthoredArchRowContainerXMax,
            AuthoredArchAnchorYMin,
            AuthoredArchAnchorYMax);
        BattlePreviewPuzzleIndex.AuthoredArchSlot[] archSlots =
            BattlePreviewPuzzleIndex.GetArchSlotsForPuzzle(battlePreviewActivePuzzleId);
        for (int i = 0; i < archSlots.Length; i++)
        {
            BattlePreviewPuzzleIndex.AuthoredArchSlot slot = archSlots[i];
            Button btn = CreateAuthoredDifficultyLayoutButton(
                battlePreviewArchRowRoot.transform,
                "DiffArch_" + i + "_" + slot.DisplayTier,
                slot.DisplayTier,
                layoutSy);

            int capturedArchIndex = i;
            btn.onClick.AddListener(() => OnAuthoredArchSlotClicked(capturedArchIndex));
            battlePreviewDifficultyButtons.Add(btn);
            battlePreviewDifficultyButtonTiers.Add(slot.ActualTier);
        }

        SyncAuthoredArchRowLayout();
        RefreshDifficultyButtonVisuals();
    }

    private static float ResolveAuthoredArchButtonHeight(float layoutSy)
    {
        return Mathf.Max(80f, AuthoredArchButtonMinHeightPx * layoutSy);
    }

    private void ApplyAuthoredArchButtonLayoutSize(LayoutElement layoutElement, Image img, float layoutSy)
    {
        if (layoutElement == null) return;
        float archHeight = ResolveAuthoredArchButtonHeight(layoutSy) * AuthoredArchButtonHeightScale;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
        layoutElement.minWidth = 0f;
        layoutElement.preferredHeight = archHeight;
        layoutElement.minHeight = archHeight;
        if (img != null && img.sprite != null)
        {
            Rect spriteRect = img.sprite.rect;
            float aspect = spriteRect.width / Mathf.Max(1f, spriteRect.height);
            layoutElement.preferredWidth = archHeight * aspect;
        }
        else
            layoutElement.preferredWidth = archHeight * 0.72f;
    }

    private void SyncAuthoredArchRowLayout()
    {
        if (battlePreviewArchRowRoot == null) return;

        HorizontalLayoutGroup archRowLayout = battlePreviewArchRowRoot.GetComponent<HorizontalLayoutGroup>();
        if (archRowLayout != null)
        {
            archRowLayout.spacing = AuthoredArchRowSpacing;
            archRowLayout.padding = new RectOffset(AuthoredArchRowPaddingH, AuthoredArchRowPaddingH, 0, 0);
            archRowLayout.childAlignment = TextAnchor.MiddleCenter;
            archRowLayout.childControlWidth = true;
            archRowLayout.childControlHeight = true;
            archRowLayout.childForceExpandWidth = false;
            archRowLayout.childForceExpandHeight = false;
        }

        for (int i = 0; i < battlePreviewDifficultyButtons.Count; i++)
        {
            Button btn = battlePreviewDifficultyButtons[i];
            if (btn == null) continue;
            ApplyAuthoredArchButtonLayoutSize(
                btn.GetComponent<LayoutElement>(),
                btn.GetComponent<Image>(),
                battlePreviewAuthoredLayoutSy);
        }

        RectTransform archRowRt = battlePreviewArchRowRoot.GetComponent<RectTransform>();
        if (archRowRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(archRowRt);
    }

    private Button CreateAuthoredDifficultyLayoutButton(
        Transform parent,
        string objName,
        BattleDifficultyTier tier,
        float layoutSy)
    {
        Sprite sprite = ResolveDifficultyTierSprite(tier);
        GameObject buttonObj = new GameObject(
            objName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObj.transform.SetParent(parent, false);
        LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();

        Image img = buttonObj.GetComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
        }
        else
        {
            img.color = new Color(1f, 1f, 1f, 0.02f);
        }

        img.raycastTarget = true;
        ApplyAuthoredArchButtonLayoutSize(layoutElement, img, layoutSy);
        Button btn = buttonObj.GetComponent<Button>();
        ApplyAuthoredDifficultyButtonColors(btn);
        return btn;
    }

    private void CreateAuthoredBossTierReveal(Transform panel, float layoutSx, float layoutSy)
    {
        battlePreviewAuthoredRevealTier = BattlePreviewPuzzleIndex.GetAuthoredRevealTier(
            battlePreviewActivePuzzleId);

        battlePreviewBossRevealRoot = new GameObject("HiddenTierReveal", typeof(RectTransform));
        battlePreviewBossRevealRoot.transform.SetParent(panel, false);
        RectTransform rootRt = battlePreviewBossRevealRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(AuthoredBossRevealAnchorXMin, AuthoredBossRevealAnchorYMin);
        rootRt.anchorMax = new Vector2(AuthoredBossRevealAnchorXMax, AuthoredBossRevealAnchorYMax);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        battlePreviewBossTierButton = CreateAuthoredDifficultySpriteButton(
            battlePreviewBossRevealRoot.transform,
            "DiffHiddenReveal",
            battlePreviewAuthoredRevealTier,
            0.06f,
            0.94f,
            0.08f,
            0.92f);

        BattleDifficultyTier revealTier = battlePreviewAuthoredRevealTier;
        battlePreviewBossTierButton.onClick.AddListener(() => OnAuthoredDifficultyTierClicked(revealTier));
        battlePreviewBossRevealRoot.SetActive(false);
    }

    private void ConfigureAuthoredPuzzleChrome(Transform panel, float layoutSx, float layoutSy)
    {
        battlePreviewIntelButton = CreateModalButton(panel, "BattleIntelButton", "對戰情報");
        ApplyAuthoredAnchorRect(
            battlePreviewIntelButton.GetComponent<RectTransform>(),
            AuthoredIntelAnchorXMin,
            AuthoredIntelAnchorXMax,
            AuthoredIntelAnchorYMin,
            AuthoredIntelAnchorYMax);
        battlePreviewIntelButton.onClick.AddListener(OnBattlePreviewIntelClicked);

        TextMeshProUGUI giveUpLabel = battlePreviewGiveUpButton.GetComponentInChildren<TextMeshProUGUI>();
        if (giveUpLabel != null) giveUpLabel.fontSize = Mathf.RoundToInt(28f * layoutSy);
        TextMeshProUGUI intelLabel = battlePreviewIntelButton.GetComponentInChildren<TextMeshProUGUI>();
        if (intelLabel != null) intelLabel.fontSize = Mathf.RoundToInt(28f * layoutSy);
        TextMeshProUGUI startLabel = battlePreviewStartButton.GetComponentInChildren<TextMeshProUGUI>();
        if (startLabel != null) startLabel.fontSize = Mathf.RoundToInt(30f * layoutSy);

        if (battlePreviewGaugesPanelObj != null)
            battlePreviewGaugesPanelObj.SetActive(false);
        ApplyAuthoredPreviewInitialVisibility();
    }

    private static string GetDifficultyArchLabel(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro: return "入門級";
            case BattleDifficultyTier.Easy: return "簡單級";
            case BattleDifficultyTier.Normal: return "普通級";
            case BattleDifficultyTier.Hard: return "困難級";
            case BattleDifficultyTier.Boss: return "魔王級";
            default: return tier.ToString();
        }
    }

    private Button CreateAuthoredDifficultySpriteButton(
        Transform parent,
        string objName,
        BattleDifficultyTier tier,
        float anchorXMin,
        float anchorXMax,
        float anchorYMin,
        float anchorYMax)
    {
        Sprite sprite = ResolveDifficultyTierSprite(tier);
        if (sprite == null)
            return CreateTransparentHitButton(parent, objName, string.Empty, anchorXMin, anchorXMax, anchorYMin, anchorYMax);

        GameObject buttonObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorXMin, anchorYMin);
        rt.anchorMax = new Vector2(anchorXMax, anchorYMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = buttonObj.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = true;
        img.type = Image.Type.Simple;
        img.raycastTarget = true;
        Button btn = buttonObj.GetComponent<Button>();
        ApplyAuthoredDifficultyButtonColors(btn);
        return btn;
    }

    private static void ApplyAuthoredDifficultyButtonColors(Button btn)
    {
        if (btn == null) return;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.98f, 0.97f, 0.95f, 1f);
        colors.pressedColor = new Color(0.94f, 0.93f, 0.91f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
    }

    private void StopAllAuthoredDifficultyFeedbackAnims()
    {
        foreach (KeyValuePair<int, Coroutine> entry in battlePreviewDifficultyFeedbackAnimRoutines)
        {
            if (entry.Value != null)
                StopCoroutine(entry.Value);
        }
        battlePreviewDifficultyFeedbackAnimRoutines.Clear();
    }

    private void StopAuthoredDifficultyFeedbackAnim(Button btn)
    {
        if (btn == null) return;
        int id = btn.GetInstanceID();
        if (battlePreviewDifficultyFeedbackAnimRoutines.TryGetValue(id, out Coroutine routine) && routine != null)
            StopCoroutine(routine);
        battlePreviewDifficultyFeedbackAnimRoutines.Remove(id);
    }

    private void PlayAuthoredDifficultyFeedbackAnim(
        Button btn,
        Image img,
        BattleDifficultyTier tier,
        bool selected)
    {
        if (btn == null) return;
        StopAuthoredDifficultyFeedbackAnim(btn);
        Color selectedTint = GetDifficultyTierAccentColor(tier);
        int id = btn.GetInstanceID();
        battlePreviewDifficultyFeedbackAnimRoutines[id] = StartCoroutine(
            selected
                ? CoAcStyleSelectPop(btn.transform, img, selectedTint)
                : CoAcStyleDeselect(btn.transform, img));
    }

    private static float EaseOutSoftBack(float t)
    {
        const float c1 = 0.85f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseInOutSmooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float EvaluateAuthoredSelectScale(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        if (t < AcSelectRisePortion)
        {
            float local = t / AcSelectRisePortion;
            return Mathf.Lerp(1f, AcSelectScalePeak, EaseOutSoftBack(local));
        }

        float settleLocal = (t - AcSelectRisePortion) / (1f - AcSelectRisePortion);
        return Mathf.Lerp(AcSelectScalePeak, AcSelectScaleRest, EaseInOutSmooth(settleLocal));
    }

    private static float EvaluateAuthoredSelectColorT(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime * AcColorLeadMultiplier);
        return EaseInOutSmooth(t);
    }

    private static float EvaluateAuthoredDeselectColorT(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        return EaseInOutSmooth(Mathf.Pow(t, 1.08f));
    }

    private static void LerpAuthoredDifficultyImageTint(Image img, Color from, Color to, float t)
    {
        if (img == null || img.sprite == null) return;
        Color c = Color.Lerp(from, to, Mathf.Clamp01(t));
        c.a = 1f;
        img.color = c;
    }

    private static IEnumerator CoAcStyleSelectPop(Transform target, Image img, Color selectedTint)
    {
        if (target == null) yield break;
        Color tintFrom = img != null && img.sprite != null ? img.color : Color.white;
        float elapsed = 0f;
        while (elapsed < AcSelectAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / AcSelectAnimDuration);
            float scale = EvaluateAuthoredSelectScale(p);
            target.localScale = new Vector3(scale, scale, 1f);
            LerpAuthoredDifficultyImageTint(img, tintFrom, selectedTint, EvaluateAuthoredSelectColorT(p));
            yield return null;
        }

        target.localScale = Vector3.one * AcSelectScaleRest;
        LerpAuthoredDifficultyImageTint(img, selectedTint, selectedTint, 1f);
    }

    private static IEnumerator CoAcStyleDeselect(Transform target, Image img)
    {
        if (target == null) yield break;
        float startScale = target.localScale.x;
        Color tintFrom = img != null && img.sprite != null ? img.color : Color.white;
        float elapsed = 0f;
        while (elapsed < AcDeselectDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / AcDeselectDuration);
            float scale = Mathf.Lerp(startScale, 1f, EaseInOutSmooth(p));
            target.localScale = new Vector3(scale, scale, 1f);
            LerpAuthoredDifficultyImageTint(img, tintFrom, Color.white, EvaluateAuthoredDeselectColorT(p));
            yield return null;
        }

        target.localScale = Vector3.one;
        LerpAuthoredDifficultyImageTint(img, Color.white, Color.white, 1f);
    }

    private void ApplyAuthoredDifficultyFeedbackVisual(
        Button btn,
        Image img,
        BattleDifficultyTier tier,
        bool active,
        bool locked)
    {
        if (btn == null) return;

        Shadow shadow = btn.GetComponent<Shadow>();
        if (shadow != null)
            shadow.enabled = false;
        Outline outline = btn.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;

        if (locked)
        {
            StopAuthoredDifficultyFeedbackAnim(btn);
            if (img != null && img.sprite != null)
                img.color = new Color(0.62f, 0.62f, 0.62f, 0.88f);
            return;
        }

        bool showFeedback = active;
        PlayAuthoredDifficultyFeedbackAnim(btn, img, tier, showFeedback);
    }

    private Button CreateTransparentHitButton(
        Transform parent,
        string objName,
        string label,
        float anchorXMin,
        float anchorXMax,
        float anchorYMin,
        float anchorYMax)
    {
        GameObject buttonObj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorXMin, anchorYMin);
        rt.anchorMax = new Vector2(anchorXMax, anchorYMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = buttonObj.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.01f);
        img.raycastTarget = true;
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
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        txt.raycastTarget = false;
        return btn;
    }

    private void CreateDifficultyButtons(Transform parent, bool authoredPanel, float layoutSx, float layoutSy)
    {
        EnsureDifficultyProfiles();
        battlePreviewDifficultyButtons.Clear();
        battlePreviewDifficultyButtonTiers.Clear();
        CreateOneDifficultyButton(parent, "DiffIntro", GetDifficultyButtonLabel(BattleDifficultyTier.Intro), BattleDifficultyTier.Intro, 0, authoredPanel, layoutSx, layoutSy);
        CreateOneDifficultyButton(parent, "DiffEasy", GetDifficultyButtonLabel(BattleDifficultyTier.Easy), BattleDifficultyTier.Easy, 1, authoredPanel, layoutSx, layoutSy);
        CreateOneDifficultyButton(parent, "DiffNormal", GetDifficultyButtonLabel(BattleDifficultyTier.Normal), BattleDifficultyTier.Normal, 2, authoredPanel, layoutSx, layoutSy);
        CreateOneDifficultyButton(parent, "DiffHard", GetDifficultyButtonLabel(BattleDifficultyTier.Hard), BattleDifficultyTier.Hard, 3, authoredPanel, layoutSx, layoutSy);
        CreateOneDifficultyButton(parent, "DiffBoss", GetDifficultyButtonLabel(BattleDifficultyTier.Boss), BattleDifficultyTier.Boss, 4, authoredPanel, layoutSx, layoutSy);
        if (IsTierLocked(selectedDifficultyTier))
            selectedDifficultyTier = BattleDifficultyTier.Normal;
        RefreshDifficultyButtonVisuals();
    }

    private void CreateOneDifficultyButton(
        Transform parent,
        string objName,
        string label,
        BattleDifficultyTier tier,
        int idx,
        bool authoredPanel,
        float layoutSx,
        float layoutSy)
    {
        Button btn = CreateModalButton(parent, objName, label);
        LayoutElement le = btn.gameObject.AddComponent<LayoutElement>();
        float btnH = authoredPanel ? 62f * layoutSy : 68f;
        le.preferredHeight = btnH;
        le.minHeight = authoredPanel ? 54f * layoutSy : 60f;
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, btnH);
        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.fontSize = authoredPanel ? 32f : 34f;
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
        battlePreviewDifficultyButtonTiers.Add(tier);
    }

    private void RefreshDifficultyButtonVisuals()
    {
        for (int i = 0; i < battlePreviewDifficultyButtons.Count; i++)
        {
            Button btn = battlePreviewDifficultyButtons[i];
            if (btn == null) continue;
            BattleDifficultyTier tier = i < battlePreviewDifficultyButtonTiers.Count
                ? battlePreviewDifficultyButtonTiers[i]
                : (BattleDifficultyTier)i;
            bool active = battlePreviewUsesAuthoredPuzzleLayout
                ? battlePreviewFeedbackDifficultyTier == tier
                : tier == selectedDifficultyTier;
            bool locked = IsTierLocked(tier);
            Image img = btn.GetComponent<Image>();
            bool useSpriteArch = battlePreviewUsesAuthoredPuzzleLayout && img != null && img.sprite != null;
            if (useSpriteArch)
                ApplyAuthoredDifficultyFeedbackVisual(btn, img, tier, active, locked);
            else if (img != null)
            {
                if (battlePreviewUsesAuthoredPuzzleLayout)
                {
                    if (locked) img.color = new Color(0.5f, 0.5f, 0.5f, 0.12f);
                    else if (active) img.color = new Color(1f, 0.92f, 0.55f, 0.28f);
                    else img.color = new Color(1f, 1f, 1f, 0.01f);
                }
                else if (locked) img.color = new Color(0.58f, 0.58f, 0.58f, 0.9f);
                else if (active) img.color = Color.white;
                else img.color = GetDifficultyTierAccentColor(tier);
            }

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null && !useSpriteArch)
            {
                if (locked) txt.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                else txt.color = active ? new Color(0.12f, 0.1f, 0.08f, 1f) : Color.white;
                if (active) txt.fontStyle = FontStyles.Bold;
                else txt.fontStyle = FontStyles.Normal;
                txt.fontSize = active ? 36f : 34f;
            }

            if (!useSpriteArch)
            {
                Outline outline = btn.GetComponent<Outline>();
                if (active && !locked)
                {
                    if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.18f, 0.14f, 0.10f, 0.95f);
                    outline.effectDistance = new Vector2(3f, -3f);
                    outline.enabled = true;
                }
                else if (outline != null)
                    outline.enabled = false;
            }
        }

        if (battlePreviewBossTierButton != null)
        {
            BattleDifficultyTier revealTier = battlePreviewAuthoredRevealTier;
            bool revealActive = battlePreviewUsesAuthoredPuzzleLayout
                ? battlePreviewFeedbackDifficultyTier == revealTier
                : selectedDifficultyTier == revealTier;
            bool revealLocked = IsTierLocked(revealTier);
            Image bossImg = battlePreviewBossTierButton.GetComponent<Image>();
            if (battlePreviewUsesAuthoredPuzzleLayout && bossImg != null && bossImg.sprite != null)
                ApplyAuthoredDifficultyFeedbackVisual(
                    battlePreviewBossTierButton,
                    bossImg,
                    revealTier,
                    revealActive,
                    revealLocked);
            else if (bossImg != null)
            {
                if (revealActive) bossImg.color = new Color(1f, 0.88f, 0.45f, 0.35f);
                else bossImg.color = new Color(1f, 1f, 1f, 0.01f);
            }
        }
    }

    private static Color GetDifficultyTierAccentColor(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro:
                return new Color(0.18f, 0.82f, 0.92f, 1f);
            case BattleDifficultyTier.Easy:
                return new Color(0.38f, 0.98f, 0.22f, 1f);
            case BattleDifficultyTier.Normal:
                return new Color(1f, 0.84f, 0.08f, 1f);
            case BattleDifficultyTier.Hard:
                return new Color(1f, 0.36f, 0.08f, 1f);
            case BattleDifficultyTier.Boss:
                return new Color(0.88f, 0.14f, 0.78f, 1f);
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

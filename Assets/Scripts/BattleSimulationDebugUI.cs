using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    // MASTER INDEX (merged legacy + 2026-04-15 updates)
    // A) Core debug UI lifecycle (legacy):
    //    - Start()/Update() orchestration, panel visibility, input lock, pause flow.
    // B) Card/field HUD and battle overlays (legacy):
    //    - Hand/field refresh loops, spell/attack presentation layers, result overlays.
    // C) Weather runtime dispatcher:
    //    - UpdateWeatherScreenEffects()
    // D) Weather animation loops:
    //    - AnimateFireRainFx()
    //    - AnimateHolyLightFx()
    //    - AnimateFogFx()   // Tsunami visual
    //    - AnimateGaleFx()
    // E) Weather UI panels:
    //    - OnWeatherForecastStarted()
    //    - CoShowWeatherForecastOverlay()
    //    - RefreshActiveWeatherEffectPanelText()
    // F) Weather scene layer construction:
    //    - CreateWeatherScreenFx()
    //    - CreateWeatherFxLayer()
    //    - CreateHolyLightEdge()
    //    - AddHolyLightEdgeLayer()
    //    - AddFogEdgeLayer()
    //    - AddGaleNightLayer()

    [Header("Debug panel (Play Mode)")]
    [Tooltip("When off, the keyboard chord does not toggle the floating debug window.")]
    [SerializeField] private bool debugPanelHotkeyEnabled = true;
    [Tooltip("Chord: hold both keys, or hold one and press the other. Release both before the next toggle.")]
    [SerializeField] private KeyCode debugPanelHotkeyKey1 = KeyCode.D;
    [SerializeField] private KeyCode debugPanelHotkeyKey2 = KeyCode.E;
    [Tooltip("When checked, the debug panel starts visible when entering Play Mode.")]
    [SerializeField] private bool debugPanelVisibleOnPlay;
    [Header("Hero Damage Feedback")]
    [Tooltip("When enabled, player hero damage triggers a short edge-vignette darkening effect.")]
    [SerializeField] private bool enableHeroDamageMonochromeFlash = true;
    [Range(0.08f, 0.12f)]
    [SerializeField] private float heroDamageMonochromeFlashSeconds = 0.1f;
    [Range(0.12f, 0.4f)]
    [SerializeField] private float heroDamageMonochromeRecoverSeconds = 0.22f;

    private BattleSimulationManager battleManager;
    private GameObject battleCardPrefab;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI deckText;
    private TextMeshProUGUI fieldText;
    private TextMeshProUGUI openingRollText;
    private RectTransform openingRollPanel;
    private CanvasGroup openingRollGroup;
    private TextMeshProUGUI openingRollDiceText;
    private TextMeshProUGUI openingRollFirstText;
    private TextMeshProUGUI weatherBadgeTmp;
    private RectTransform weatherForecastOverlayRt;
    private CanvasGroup weatherForecastOverlayCg;
    private TextMeshProUGUI weatherForecastTitleTmp;
    private TextMeshProUGUI weatherForecastBodyTmp;
    private Coroutine weatherForecastOverlayRoutine;
    private TextMeshProUGUI weatherHintTmp;
    private TextMeshProUGUI weatherRemainTmp;
    private Button activeWeatherEffectButton;
    private RectTransform activeWeatherEffectPanelRt;
    private TextMeshProUGUI activeWeatherEffectPanelSummaryTmp;
    private TextMeshProUGUI activeWeatherEffectPanelTextTmp;
    private RectTransform weatherScreenFxRoot;
    private RectTransform weatherFireRainFxRt;
    private RectTransform weatherHolyLightFxRt;
    private RectTransform weatherFogFxRt;
    private RectTransform weatherGaleFxRt;
    private readonly List<Image> weatherHolyLightEdgeImgs = new List<Image>();
    private readonly List<float> weatherHolyLightEdgeBaseAlphas = new List<float>();
    private Image weatherHolyLightTopEdgeImg;
    private Image weatherHolyLightBottomEdgeImg;
    private Image weatherHolyLightLeftEdgeImg;
    private Image weatherHolyLightRightEdgeImg;
    private readonly List<Image> weatherHolyLightDustImages = new List<Image>();
    private readonly List<RectTransform> weatherHolyLightDustRects = new List<RectTransform>();
    private readonly List<float> weatherHolyLightDustSpeeds = new List<float>();
    private readonly List<float> weatherHolyLightDustPhases = new List<float>();
    private readonly List<Color> weatherHolyLightDustBaseColors = new List<Color>();
    private readonly List<RectTransform> weatherFireRainStreaks = new List<RectTransform>();
    private readonly List<float> weatherFireRainStreakSpeeds = new List<float>();
    private readonly List<Image> weatherFireRainStreakImages = new List<Image>();
    private readonly List<float> weatherFireRainStreakPhases = new List<float>();
    private readonly List<RectTransform> weatherFogBands = new List<RectTransform>();
    private readonly List<Image> weatherFogBandImages = new List<Image>();
    private readonly List<float> weatherFogBandSpeeds = new List<float>();
    private readonly List<float> weatherFogBandPhases = new List<float>();
    private readonly List<Image> weatherFogEdgeImgs = new List<Image>();
    private readonly List<float> weatherFogEdgeBaseAlphas = new List<float>();
    private readonly List<RectTransform> weatherFogFoamDots = new List<RectTransform>();
    private readonly List<Image> weatherFogFoamDotImages = new List<Image>();
    private readonly List<float> weatherFogFoamDotSpeeds = new List<float>();
    private RectTransform weatherFogBoatRt;
    private Image weatherFogBoatHullImg;
    private float weatherFogBoatBaseY;
    private readonly List<Image> weatherGaleNightEdgeImgs = new List<Image>();
    private readonly List<float> weatherGaleNightEdgeBaseAlphas = new List<float>();
    private readonly List<RectTransform> weatherGaleLeafRects = new List<RectTransform>();
    private readonly List<Image> weatherGaleLeafImgs = new List<Image>();
    private readonly List<float> weatherGaleLeafSpeeds = new List<float>();
    private readonly List<float> weatherGaleLeafPhases = new List<float>();
    private readonly List<RectTransform> weatherGaleWindLineRects = new List<RectTransform>();
    private readonly List<Image> weatherGaleWindLineImgs = new List<Image>();
    private readonly List<float> weatherGaleWindLineSpeeds = new List<float>();
    private Image[] openingPlayerDicePips;
    private Image[] openingEnemyDicePips;
    private Coroutine openingRollRoutine;
    private int lastOpeningRollVersion = -1;
    private RectTransform turnBannerPanelRt;
    private CanvasGroup turnBannerCg;
    private TextMeshProUGUI turnBannerTmp;
    private Coroutine turnBannerRoutine;
    private int playerHandPressDepth;
    [Tooltip("「你的回合」浮窗：回合開始無操作／觸碰手牌後未出牌／手牌上場後未結束回合，逾時秒數（下限 10 秒）。")]
    [SerializeField] private float playerTurnIdlePromptSeconds = 10f;
    /// <summary>小於零：未啟用「回合開始完全無操作」計時。</summary>
    private float yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
    /// <summary>小於零：未啟用「手牌按下後放開且該次未出牌」計時（起點於放開之次幀判定）。</summary>
    private float yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
    /// <summary>小於零：未從「手牌上場」啟動計時；否則為最近一次成功上場後的 Unscaled 時間戳。</summary>
    private float yourTurnBannerAfterFieldPlayClockStartUnscaled = -1f;
    private bool yourTurnBannerIdlePromptShownThisWindow;
    private bool playerTurnUiIdleStandbyMode;
    private bool turnBannerPlayerFromIdleTimeout;
    private Coroutine yourTurnBannerHandTouchNoPlayArmDeferRoutine;
    private bool yourTurnBannerHandTouchSessionLedToPlay;
    private AudioSource uiAudioSource;
    private AudioClip openingRollSfx;
    private Text roundText;
    private RectTransform tooltipPanel;
    private Text tooltipText;
    private const float HandTooltipBackgroundAlpha = 0.7f;
    private const float HandTooltipPanelWidth = 640f;
    private const float HandTooltipPanelHeight = 280f;
    private const int HandTooltipFontSize = 34;
    private const float LinGazeEyeStrikeDuration = 1.18f;
    private RectTransform spellCastOverlayRoot;
    private CanvasGroup spellCastOverlayGroup;
    private TextMeshProUGUI spellCastTitleTmp;
    private TextMeshProUGUI spellCastBodyTmp;
    private Coroutine spellCastOverlayRoutine;
    private Text battleResultText;
    private CanvasGroup battleResultGroup;
    private int lastShownBattleResult = 0;
    private Coroutine battleResultFadeRoutine;
    private bool lockBattleResultAutoUpdate;
    private GameObject endBattlePanel;
    private Text endBattleTitleText;
    private CanvasGroup endBattlePanelGroup;
    private bool endBattlePanelShown;
    private GameObject battleHistoryOverlayRoot;
    private TextMeshProUGUI battleHistoryContentTmp;
    private ScrollRect battleHistoryScrollRect;
    /// <summary>結算時全螢幕凍結層（截圖 RawImage + 半透明黑），其下為停用中的戰鬥 UI。</summary>
    private GameObject settlementFreezeRoot;
    private RawImage settlementFreezeRawImage;
    private Texture2D settlementFreezeCaptureTexture;
    private Coroutine settlementFreezeRoutine;
    private readonly List<Transform> settlementRestoreTransforms = new List<Transform>();
    private readonly List<bool> settlementRestoreActive = new List<bool>();
    private bool settlementBattleUiSuppressed;
    [Tooltip("結算截圖若上下與實際畫面相反，再勾選。多數專案 ReadPixels + RawImage 預設即正確，不需翻轉。")]
    [SerializeField] private bool settlementCaptureFlipTextureY;
    /// <summary>對戰歷史彈窗與內文相對於初版尺寸的倍率。</summary>
    private const float BattleHistoryDialogScale = 1.5f;
    private RectTransform uiRoot;
    private RectTransform handArea;
    private RectTransform enemyHandArea;
    private RectTransform playerFieldArea;
    private RectTransform playerSpellFieldArea;
    private RectTransform enemyFieldArea;
    private RectTransform enemySpellFieldArea;
    private GameObject playerFieldCardObj;
    private GameObject playerSpellFieldCardObj;
    private GameObject enemyFieldCardObj;
    private GameObject enemySpellFieldCardObj;
    private bool lastPlayerFieldExists;
    private bool lastPlayerSpellFieldExists;
    private bool lastEnemyFieldExists;
    private bool lastEnemySpellFieldExists;
    private TextMeshProUGUI playerHeroHpText;
    private TextMeshProUGUI enemyHeroHpText;
    private int lastShownPlayerHeroHp = int.MinValue;
    private int lastShownEnemyHeroHp = int.MinValue;
    private float heroHudTitleSize = 24f;
    private int lastHeroHudBattleSessionId = int.MinValue;
    private float nextRefreshTime;
    private int lastHandSignature = int.MinValue;
    private int lastFieldSignature = int.MinValue;
    private const float BattleHandLayoutWidthPx = 170f;
    private const float BattleHandLayoutHeightPx = 210f;
    private Vector2 prefabCardSize = new Vector2(BattleHandLayoutWidthPx, BattleHandLayoutHeightPx);
    private Vector2 battleCardPrefabNativeSize = new Vector2(168.5f, 245.5f);
    private float lastScale = -1f;
    private float lastSpacing = -1f;
    private float lastTextScale = -1f;
    private float lastNameScale = -1f;
    private float lastBackplateScale = -1f;
    private float lastFieldScale = -1f;
    private float lastFieldSpellScale = -1f;
    private int lastFieldLayoutSignature = int.MinValue;
    private int lastFieldTextTuningSignature = int.MinValue;
    private Button quickEndTurnButton;
    public Button sceneEndTurnButton;
    private bool isPlayingCardAnimation;
    private bool isEnemyPlayingCardAnimation;
    private bool deferEnemyFieldRefresh;
    /// <summary>火球擊殺敵方場怪後，延後移除敵方場地牌直到飛行特效命中。</summary>
    private bool holdEnemyFieldCardUntilFireballHit;
    /// <summary>敵方火球擊殺我方場怪後，延後移除我方場地牌直到特效命中。</summary>
    private bool holdPlayerFieldCardUntilFireballHit;
    private TMP_FontAsset sharedUIFont;
    private RectTransform longPressRaisedCard;
    private int longPressOriginalSibling = -1;
    private Coroutine longPressScaleRoutine;
    private Vector3 longPressOriginalScale = Vector3.one;
    private ZoomUI longPressDisabledZoom;
    private bool longPressZoomWasEnabled;
    private Coroutine attackFxRoutine;
    private Coroutine lesserHealFieldFxRoutine;
    private Coroutine fireballFxRoutine;
    private Coroutine linGazeEyeStrikeRoutinePlayer;
    private Coroutine linGazeEyeStrikeRoutineEnemy;
    private Vector2 fireballHandAnchorLocal;
    private bool fireballHandAnchorValid;
    private static Sprite s_unitWhiteSprite;
    private bool deferFieldRefreshDuringAttack;
    private bool pendingFieldRefreshAfterAttack;
    private RectTransform playerDeckPileRt;
    private RectTransform enemyDeckPileRt;
    private RectTransform pausePanel;
    private bool isGamePaused;
    private float handAreaYCurrent;
    private float handAreaTargetY = float.NaN;
    private Coroutine handAreaTweenRoutine;
    private float enemyHandAreaYCurrent;
    private float enemyHandAreaTargetY = float.NaN;
    private Coroutine enemyHandAreaTweenRoutine;
    private Coroutine playerHeroDamagedFxRoutine;
    private RectTransform heroDamageMonochromeFlashRt;
    private CanvasGroup heroDamageMonochromeFlashCg;
    private Coroutine playerOpeningHandFlyRoutine;
    private int playerOpeningHandFlySessionDone = int.MinValue;
    private Coroutine enemyOpeningHandFlyRoutine;
    private int enemyOpeningHandFlySessionDone = int.MinValue;
    /// <summary>Floating debug info window; hotkey keys are set in the Inspector.</summary>
    private GameObject debugUiRoot;
    private bool debugChordLatched;
    private bool battleResultTextUsesDebugPanelLayout;
    private const float DebugUiChromeMul = 1.55f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (Object.FindFirstObjectByType<BattleSimulationManager>() == null) return;
        if (Object.FindFirstObjectByType<BattleSimulationDebugUI>() != null) return;

        GameObject host = new GameObject("BattleSimulationDebugUI");
        host.AddComponent<BattleSimulationDebugUI>();
    }

    private void Start()
    {
        battleManager = Object.FindFirstObjectByType<BattleSimulationManager>();
        if (battleManager == null)
        {
            Destroy(gameObject);
            return;
        }
        // Card prefab must be resolved first: ResolveUIFont reads CardDisplay fonts (e.g. Noto Sans TC).
        battleCardPrefab = ResolveCardPrefab();
        sharedUIFont = ResolveUIFont();
        battleManager.EnemyCardPlayed += OnEnemyCardPlayed;
        battleManager.EnemyCardDiscarded += OnEnemyCardDiscarded;
        battleManager.PlayerCardDiscarded += OnPlayerCardDiscarded;
        battleManager.AttackPerformed += OnAttackPerformed;
        battleManager.CardDrawn += OnCardDrawn;
        battleManager.SpellCastPresentationStarted += OnSpellCastPresentationStarted;
        battleManager.SpellCastAsyncPresentationFinished += OnSpellCastAsyncPresentationFinished;
        battleManager.BattleLayoutVisualRefreshRequested += OnBattleLayoutVisualRefreshRequested;
        battleManager.PlayerLesserHealVisualRequested += OnPlayerLesserHealVisualRequested;
        battleManager.EnemyLesserHealVisualRequested += OnEnemyLesserHealVisualRequested;
        battleManager.SpellCastHandAnchorCommitted += OnSpellCastHandAnchorCommitted;
        battleManager.FireballVisualRequested += OnFireballVisualRequested;
        battleManager.LinGazePeriodicStrikeVisualRequested += OnLinGazePeriodicStrikeVisualRequested;
        battleManager.WeatherForecastStarted += OnWeatherForecastStarted;
        battleManager.WeatherForecastFinished += OnWeatherForecastFinished;
        battleManager.TurnBannerRequested += OnTurnBannerRequested;
        battleManager.PlayerCommittedHandCardToFieldFromHand += OnPlayerCommittedHandCardToFieldFromHand;
        battleManager.PlayerPressedEndTurnForPromptUi += OnPlayerPressedEndTurnForPromptUi;
        battleManager.PlayerTurnActionWindowOpenedForPromptUi += OnPlayerTurnActionWindowOpenedForPromptUi;
        DisarmYourTurnBannerAllIdlePromptClocks();
        CachePrefabCardSize();

        Transform canvas2 = FindCanvas2();
        if (canvas2 == null)
        {
            Debug.LogWarning("BattleSimulationDebugUI: Canvas2 not found.");
            return;
        }

        CreateDebugPanel(canvas2);

        BattleAutoSimPlugin.Started += OnBatchWinRateSimStarted;
        BattleAutoSimPlugin.Completed += OnBatchWinRateSimCompleted;
    }

    private void OnBatchWinRateSimStarted()
    {
        deferFieldRefreshDuringAttack = false;
        pendingFieldRefreshAfterAttack = false;
        deferEnemyFieldRefresh = false;
        holdEnemyFieldCardUntilFireballHit = false;
        holdPlayerFieldCardUntilFireballHit = false;
        if (battleManager != null) battleManager.ResetPendingFireballFieldUiDefers();
        isPlayingCardAnimation = false;
        isEnemyPlayingCardAnimation = false;

        if (openingRollRoutine != null)
        {
            StopCoroutine(openingRollRoutine);
            openingRollRoutine = null;
        }
        if (attackFxRoutine != null)
        {
            StopCoroutine(attackFxRoutine);
            attackFxRoutine = null;
        }
        if (lesserHealFieldFxRoutine != null)
        {
            StopCoroutine(lesserHealFieldFxRoutine);
            lesserHealFieldFxRoutine = null;
        }
        if (fireballFxRoutine != null)
        {
            StopCoroutine(fireballFxRoutine);
            fireballFxRoutine = null;
        }
        if (linGazeEyeStrikeRoutinePlayer != null)
        {
            StopCoroutine(linGazeEyeStrikeRoutinePlayer);
            linGazeEyeStrikeRoutinePlayer = null;
        }
        if (linGazeEyeStrikeRoutineEnemy != null)
        {
            StopCoroutine(linGazeEyeStrikeRoutineEnemy);
            linGazeEyeStrikeRoutineEnemy = null;
        }
        fireballHandAnchorValid = false;
        if (handAreaTweenRoutine != null)
        {
            StopCoroutine(handAreaTweenRoutine);
            handAreaTweenRoutine = null;
        }
        if (enemyHandAreaTweenRoutine != null)
        {
            StopCoroutine(enemyHandAreaTweenRoutine);
            enemyHandAreaTweenRoutine = null;
        }
        ForceHideSpellCastOverlay();
        ForceHideTurnBanner();
        playerHandPressDepth = 0;
        DisarmYourTurnBannerAllIdlePromptClocks();
        if (longPressScaleRoutine != null)
        {
            StopCoroutine(longPressScaleRoutine);
            longPressScaleRoutine = null;
        }

        if (openingRollPanel != null) openingRollPanel.gameObject.SetActive(false);
        if (openingRollGroup != null) openingRollGroup.alpha = 0f;
    }

    private void OnBatchWinRateSimCompleted(BattleAutoSimPlugin.SimResult _)
    {
        if (battleManager == null) return;
        lastHandSignature = int.MinValue;
        lastFieldSignature = int.MinValue;
        nextRefreshTime = 0f;
        if (handArea != null) RebuildHandButtons();
        if (enemyHandArea != null) RebuildEnemyHandCards();
        RefreshFieldCards();
        SetHandButtonsInteractable();
        SyncTurnBannerWithBattleState();
    }

    private void OnWeatherForecastStarted(string weatherName, string effectText)
    {
        if (weatherForecastOverlayRt == null) return;
        if (weatherForecastOverlayRoutine != null)
        {
            StopCoroutine(weatherForecastOverlayRoutine);
            weatherForecastOverlayRoutine = null;
        }
        if (activeWeatherEffectButton != null) activeWeatherEffectButton.gameObject.SetActive(true);
        weatherForecastOverlayRoutine = StartCoroutine(CoShowWeatherForecastOverlay(weatherName, effectText));
    }

    private string SafeWeatherText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("：", ": ")
            .Replace("｜", "|")
            .Replace("，", ", ")
            .Replace("。", "")
            .Replace("（", "(")
            .Replace("）", ")")
            .Replace("＋", "+")
            .Replace("－", "-");
    }

    private void OnWeatherForecastFinished()
    {
        if (weatherForecastOverlayRoutine != null)
        {
            StopCoroutine(weatherForecastOverlayRoutine);
            weatherForecastOverlayRoutine = null;
        }
        if (weatherForecastOverlayRt == null) return;
        weatherForecastOverlayRoutine = StartCoroutine(CoHideWeatherForecastOverlay());
    }

    private IEnumerator CoShowWeatherForecastOverlay(string weatherName, string effectText)
    {
        if (weatherForecastOverlayRt == null) yield break;
        weatherForecastOverlayRt.gameObject.SetActive(true);
        weatherForecastOverlayRt.SetAsLastSibling();
        int remain = battleManager != null ? battleManager.GetCurrentWeatherRemainingRoundsForUi() : 0;
        string finalTitle = "天氣預報: " + weatherName + (remain > 0 ? " (剩餘 " + remain + " 回合)" : string.Empty);
        if (weatherForecastTitleTmp != null)
            weatherForecastTitleTmp.text = SafeWeatherText("天氣預報抽選中...");
        if (weatherForecastBodyTmp != null)
            weatherForecastBodyTmp.text = SafeWeatherText(
                "即將生效的卡牌效果\n" +
                effectText +
                "\n\n下一次預報提示: " + (battleManager != null ? battleManager.GetNextWeatherForecastHintForUi() : "-"));
        if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = 0f;

        float t = 0f;
        const float fade = 0.18f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fade);
            if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = p;
            yield return null;
        }
        if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = 1f;
        yield return StartCoroutine(CoPlayWeatherForecastRollAnimation(weatherName, finalTitle));
        weatherForecastOverlayRoutine = null;
    }

    private IEnumerator CoPlayWeatherForecastRollAnimation(string weatherName, string finalTitle)
    {
        if (weatherForecastTitleTmp == null)
            yield break;
        string[] pool = new[] { "緋焰時雨", "月華聖祈", "蒼潮夜湧", "朔風森詠" };
        int start = Random.Range(0, pool.Length);
        float elapsed = 0f;
        const float total = 0.92f;
        const float step = 0.07f;
        float tick = 0f;
        int idx = start;
        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;
            tick += Time.unscaledDeltaTime;
            if (tick >= step)
            {
                tick = 0f;
                weatherForecastTitleTmp.text = SafeWeatherText("天氣預報抽選: " + pool[idx]);
                idx = (idx + 1) % pool.Length;
            }
            yield return null;
        }
        weatherForecastTitleTmp.text = SafeWeatherText(finalTitle);
        if (!string.IsNullOrEmpty(weatherName))
            yield return new WaitForSecondsRealtime(0.08f);
    }

    private IEnumerator CoHideWeatherForecastOverlay()
    {
        if (weatherForecastOverlayRt == null) yield break;
        if (!weatherForecastOverlayRt.gameObject.activeSelf)
        {
            weatherForecastOverlayRoutine = null;
            yield break;
        }

        float t = 0f;
        const float fade = 0.18f;
        float startAlpha = weatherForecastOverlayCg != null ? weatherForecastOverlayCg.alpha : 1f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fade);
            if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = Mathf.Lerp(startAlpha, 0f, p);
            yield return null;
        }

        if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = 0f;
        weatherForecastOverlayRt.gameObject.SetActive(false);
        weatherForecastOverlayRoutine = null;
    }

    private void ToggleActiveWeatherEffectPanel()
    {
        if (activeWeatherEffectPanelRt == null) return;
        bool next = !activeWeatherEffectPanelRt.gameObject.activeSelf;
        activeWeatherEffectPanelRt.gameObject.SetActive(next);
        if (!next) return;
        activeWeatherEffectPanelRt.SetAsLastSibling();
        RefreshActiveWeatherEffectPanelText();
    }

    private void RefreshActiveWeatherEffectPanelText()
    {
        if (battleManager == null) return;
        string weather = battleManager.GetCurrentWeatherLabelForUi();
        int remain = battleManager.GetCurrentWeatherRemainingRoundsForUi();
        string status = remain > 0 ? "作用中" : "未作用";
        string queued = battleManager.GetQueuedWeatherForNextRoundLabelForUi();
        string detail = BuildAllWeatherHudText(weather);
        if (activeWeatherEffectPanelSummaryTmp != null)
        {
            activeWeatherEffectPanelSummaryTmp.text = SafeWeatherText(
                "<size=132%><b>場地效果摘要</b></size>\n\n" +
                "<size=108%>名稱</size>\n<size=118%><b>" + weather + "</b></size>\n\n" +
                "<size=108%>剩餘回合</size>\n<size=118%><b>" + remain + "</b></size>\n\n" +
                "<size=108%>狀態</size>\n<size=118%><b>" + status + "</b></size>");
        }
        if (activeWeatherEffectPanelTextTmp != null)
        {
            activeWeatherEffectPanelTextTmp.text = SafeWeatherText(
                "<size=132%><b>場地效果總覽</b></size>\n\n" +
                "<size=108%><b>下一回合將套用</b>: " + queued + "</size>\n\n" +
                "<size=100%>" + detail + "</size>");
        }
    }

    private string BuildAllWeatherHudText(string activeWeatherLabel)
    {
        return
            FormatWeatherLine("緋焰時雨", activeWeatherLabel == "緋焰時雨", "回合結束雙方場上怪獸各受 5 點傷害") + "\n\n" +
            FormatWeatherLine("月華聖祈", activeWeatherLabel == "月華聖祈", "所有治療效果增加 10") + "\n\n" +
            FormatWeatherLine("蒼潮夜湧", activeWeatherLabel == "蒼潮夜湧", "直接攻擊英雄傷害減少 50%") + "\n\n" +
            FormatWeatherLine("朔風森詠", activeWeatherLabel == "朔風森詠", "雙方首張法術效果增加 20%");
    }

    private static string FormatWeatherLine(string name, bool active, string effect)
    {
        if (active)
            return "<color=#8F5A16><b>● " + name + " (生效中)</b></color>\n<color=#3A2A1A><b>" + effect + "</b></color>";
        return "<color=#4A5A39><b>○ " + name + "</b></color>\n<color=#2F271E>" + effect + "</color>";
    }

    private void UpdateWeatherScreenEffects()
    {
        if (battleManager == null || weatherScreenFxRoot == null) return;

        string weather = battleManager.GetCurrentWeatherLabelForUi();
        bool active = battleManager.GetCurrentWeatherRemainingRoundsForUi() > 0;
        bool showFire = active && weather == "緋焰時雨";
        bool showHoly = active && weather == "月華聖祈";
        bool showFog = active && weather == "蒼潮夜湧";
        bool showGale = active && weather == "朔風森詠";

        if (weatherFireRainFxRt != null) weatherFireRainFxRt.gameObject.SetActive(showFire);
        if (weatherHolyLightFxRt != null) weatherHolyLightFxRt.gameObject.SetActive(showHoly);
        if (weatherFogFxRt != null) weatherFogFxRt.gameObject.SetActive(showFog);
        if (weatherGaleFxRt != null) weatherGaleFxRt.gameObject.SetActive(showGale);
        if (!showFire && !showHoly && !showFog && !showGale) return;

        float dt = Time.unscaledDeltaTime;
        if (showFire) AnimateFireRainFx(dt);
        if (showHoly) AnimateHolyLightFx();
        if (showFog) AnimateFogFx(dt);
        if (showGale) AnimateGaleFx(dt);
    }

    private void AnimateHolyLightFx()
    {
        float edgePulseFactor = 0.84f + Mathf.Sin(Time.unscaledTime * 0.82f) * 0.14f;
        for (int i = 0; i < weatherHolyLightEdgeImgs.Count; i++)
        {
            Image edgeImg = weatherHolyLightEdgeImgs[i];
            if (edgeImg == null) continue;
            float baseAlpha = i < weatherHolyLightEdgeBaseAlphas.Count ? weatherHolyLightEdgeBaseAlphas[i] : 0.08f;
            Color ec = edgeImg.color;
            ec.a = Mathf.Clamp01(baseAlpha * edgePulseFactor);
            edgeImg.color = ec;
        }

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < weatherHolyLightDustRects.Count; i++)
        {
            RectTransform dustRt = weatherHolyLightDustRects[i];
            if (dustRt == null) continue;
            float sp = i < weatherHolyLightDustSpeeds.Count ? weatherHolyLightDustSpeeds[i] : 13f;
            float phase = i < weatherHolyLightDustPhases.Count ? weatherHolyLightDustPhases[i] : 0f;
            Vector2 p = dustRt.anchoredPosition;
            p.y += sp * dt;
            p.x += Mathf.Sin(Time.unscaledTime * 1.15f + phase) * 14f * dt;
            if (p.y > 330f)
            {
                p.y = Random.Range(-260f, -120f);
                p.x = Random.Range(-420f, 420f);
                if (i < weatherHolyLightDustPhases.Count)
                    weatherHolyLightDustPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            dustRt.anchoredPosition = p;

            if (i < weatherHolyLightDustImages.Count)
            {
                Image dustImg = weatherHolyLightDustImages[i];
                if (dustImg != null)
                {
                    Color baseColor = i < weatherHolyLightDustBaseColors.Count
                        ? weatherHolyLightDustBaseColors[i]
                        : dustImg.color;
                    float tintPulse = 0.5f + Mathf.Sin(Time.unscaledTime * 0.95f + phase) * 0.5f;
                    Color shimmer = Color.Lerp(baseColor, new Color(0.95f, 0.96f, 1f, baseColor.a), 0.14f * tintPulse);
                    Color dc = shimmer;
                    dc.a = Mathf.Clamp01(baseColor.a + Mathf.Sin(Time.unscaledTime * 1.35f + phase) * 0.045f);
                    dustImg.color = dc;
                }
            }
            float scalePulse = 0.92f + Mathf.Sin(Time.unscaledTime * 1.05f + phase) * 0.12f;
            dustRt.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }
    }

    private void AnimateFireRainFx(float dt)
    {
        if (weatherFireRainFxRt == null || weatherFireRainStreaks.Count == 0) return;
        float h = Mathf.Max(300f, weatherFireRainFxRt.rect.height);
        float w = Mathf.Max(500f, weatherFireRainFxRt.rect.width);
        float top = h * 0.5f + 80f;
        float bottom = -h * 0.5f - 80f;
        float left = -w * 0.5f - 50f;
        float right = w * 0.5f + 50f;
        for (int i = 0; i < weatherFireRainStreaks.Count; i++)
        {
            RectTransform rt = weatherFireRainStreaks[i];
            float sp = weatherFireRainStreakSpeeds[i];
            float phase = i < weatherFireRainStreakPhases.Count ? weatherFireRainStreakPhases[i] : 0f;
            Vector2 p = rt.anchoredPosition;
            p.x -= sp * 0.42f * dt;
            p.x += Mathf.Sin(Time.unscaledTime * 3.2f + phase) * 26f * dt;
            p.y -= sp * dt;
            if (p.y < bottom || p.x < left)
            {
                p.y = top + Random.Range(0f, 120f);
                p.x = Random.Range(left + 120f, right);
                if (i < weatherFireRainStreakPhases.Count)
                    weatherFireRainStreakPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            rt.anchoredPosition = p;

            if (i < weatherFireRainStreakImages.Count)
            {
                Image img = weatherFireRainStreakImages[i];
                if (img != null)
                {
                    float pulse = 0.2f + Mathf.Sin(Time.unscaledTime * 7.5f + phase) * 0.08f;
                    Color c = img.color;
                    c.a = Mathf.Clamp01(pulse);
                    img.color = c;
                }
            }
        }
    }

    private void AnimateFogFx(float dt)
    {
        float edgePulse = 0.88f + Mathf.Sin(Time.unscaledTime * 0.72f) * 0.14f;
        for (int i = 0; i < weatherFogEdgeImgs.Count; i++)
        {
            Image edge = weatherFogEdgeImgs[i];
            if (edge == null) continue;
            float baseAlpha = i < weatherFogEdgeBaseAlphas.Count ? weatherFogEdgeBaseAlphas[i] : 0.08f;
            Color c = edge.color;
            c.a = Mathf.Clamp01(baseAlpha * edgePulse);
            edge.color = c;
        }

        if (weatherFogFxRt == null) return;
        float w = Mathf.Max(560f, weatherFogFxRt.rect.width);
        float left = -w * 0.5f - 180f;
        float right = w * 0.5f + 180f;
        for (int i = 0; i < weatherFogBands.Count; i++)
        {
            RectTransform bandRt = weatherFogBands[i];
            if (bandRt == null) continue;
            float sp = i < weatherFogBandSpeeds.Count ? weatherFogBandSpeeds[i] : 14f;
            float phase = i < weatherFogBandPhases.Count ? weatherFogBandPhases[i] : 0f;
            Vector2 p = bandRt.anchoredPosition;
            p.x -= sp * dt;
            p.y += Mathf.Sin(Time.unscaledTime * 0.95f + phase) * 8f * dt;
            if (p.x < left)
            {
                p.x = right + Random.Range(-20f, 40f);
                p.y = Random.Range(-300f, 300f);
                if (i < weatherFogBandPhases.Count)
                    weatherFogBandPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            bandRt.anchoredPosition = p;

            if (i < weatherFogBandImages.Count)
            {
                Image img = weatherFogBandImages[i];
                if (img != null)
                {
                    Color bc = img.color;
                    bc.a = Mathf.Clamp01(0.09f + Mathf.Sin(Time.unscaledTime * 1.2f + phase) * 0.04f);
                    img.color = bc;
                }
            }
        }

        for (int i = 0; i < weatherFogFoamDots.Count; i++)
        {
            RectTransform foamRt = weatherFogFoamDots[i];
            if (foamRt == null) continue;
            float sp = i < weatherFogFoamDotSpeeds.Count ? weatherFogFoamDotSpeeds[i] : 30f;
            Vector2 p = foamRt.anchoredPosition;
            p.x -= sp * dt;
            p.y += Mathf.Sin(Time.unscaledTime * 2.1f + i * 0.8f) * 10f * dt;
            if (p.x < left)
            {
                p.x = right + Random.Range(0f, 60f);
                p.y = Random.Range(-240f, 240f);
            }
            foamRt.anchoredPosition = p;
            if (i < weatherFogFoamDotImages.Count)
            {
                Image img = weatherFogFoamDotImages[i];
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Clamp01(0.10f + Mathf.Sin(Time.unscaledTime * 2.6f + i * 0.65f) * 0.05f);
                    img.color = c;
                }
            }
        }

        if (weatherFogBoatRt != null)
        {
            Vector2 bp = weatherFogBoatRt.anchoredPosition;
            bp.x -= 22f * dt;
            bp.y = weatherFogBoatBaseY + Mathf.Sin(Time.unscaledTime * 1.35f) * 7.5f;
            if (bp.x < left + 120f) bp.x = right - 140f;
            weatherFogBoatRt.anchoredPosition = bp;
            weatherFogBoatRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.8f) * 5.5f);
            if (weatherFogBoatHullImg != null)
            {
                Color hc = weatherFogBoatHullImg.color;
                hc.a = Mathf.Clamp01(0.28f + Mathf.Sin(Time.unscaledTime * 1.1f) * 0.06f);
                weatherFogBoatHullImg.color = hc;
            }
        }
    }

    private void AnimateGaleFx(float dt)
    {
        float edgePulse = 0.92f + Mathf.Sin(Time.unscaledTime * 1.25f) * 0.15f;
        for (int i = 0; i < weatherGaleNightEdgeImgs.Count; i++)
        {
            Image img = weatherGaleNightEdgeImgs[i];
            if (img == null) continue;
            float baseAlpha = i < weatherGaleNightEdgeBaseAlphas.Count ? weatherGaleNightEdgeBaseAlphas[i] : 0.08f;
            Color c = img.color;
            c.a = Mathf.Clamp01(baseAlpha * edgePulse);
            img.color = c;
        }

        if (weatherGaleFxRt == null) return;
        float w = Mathf.Max(620f, weatherGaleFxRt.rect.width);
        float h = Mathf.Max(380f, weatherGaleFxRt.rect.height);
        float left = -w * 0.5f - 180f;
        float right = w * 0.5f + 180f;
        float top = h * 0.5f + 120f;
        float bottom = -h * 0.5f - 120f;

        for (int i = 0; i < weatherGaleLeafRects.Count; i++)
        {
            RectTransform rt = weatherGaleLeafRects[i];
            if (rt == null) continue;
            float sp = i < weatherGaleLeafSpeeds.Count ? weatherGaleLeafSpeeds[i] : 90f;
            float phase = i < weatherGaleLeafPhases.Count ? weatherGaleLeafPhases[i] : 0f;
            Vector2 p = rt.anchoredPosition;
            p.x -= sp * dt;
            p.y += Mathf.Sin(Time.unscaledTime * 4.2f + phase) * 20f * dt;
            if (p.x < left || p.y < bottom || p.y > top)
            {
                p.x = right + Random.Range(20f, 120f);
                p.y = Random.Range(bottom + 60f, top - 30f);
                if (i < weatherGaleLeafPhases.Count) weatherGaleLeafPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            rt.anchoredPosition = p;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 8f + phase) * 30f);
            if (i < weatherGaleLeafImgs.Count)
            {
                Image img = weatherGaleLeafImgs[i];
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Clamp01(0.28f + Mathf.Sin(Time.unscaledTime * 3.1f + phase) * 0.12f);
                    img.color = c;
                }
            }
        }

        for (int i = 0; i < weatherGaleWindLineRects.Count; i++)
        {
            RectTransform rt = weatherGaleWindLineRects[i];
            if (rt == null) continue;
            float sp = i < weatherGaleWindLineSpeeds.Count ? weatherGaleWindLineSpeeds[i] : 140f;
            Vector2 p = rt.anchoredPosition;
            p.x -= sp * dt;
            if (p.x < left) p.x = right + Random.Range(40f, 120f);
            rt.anchoredPosition = p;
            if (i < weatherGaleWindLineImgs.Count)
            {
                Image img = weatherGaleWindLineImgs[i];
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Clamp01(0.1f + Mathf.Sin(Time.unscaledTime * 5f + i * 0.4f) * 0.05f);
                    img.color = c;
                }
            }
        }

    }

    private void ToggleDebugUiRoot()
    {
        if (debugUiRoot == null) return;
        debugUiRoot.SetActive(!debugUiRoot.activeSelf);
    }

    private void CloseDebugUiRoot()
    {
        if (debugUiRoot == null) return;
        debugUiRoot.SetActive(false);
    }

    private void Update()
    {
        if (debugPanelHotkeyEnabled && debugUiRoot != null)
        {
            KeyCode k1 = debugPanelHotkeyKey1;
            KeyCode k2 = debugPanelHotkeyKey2;
            bool k1Held = k1 != KeyCode.None && Input.GetKey(k1);
            bool k2Held = k2 != KeyCode.None && Input.GetKey(k2);
            bool k1Down = k1 != KeyCode.None && Input.GetKeyDown(k1);
            bool k2Down = k2 != KeyCode.None && Input.GetKeyDown(k2);
            bool chord =
                (k1Held && k2Held) ||
                (k1Down && k2Held) ||
                (k2Down && k1Held) ||
                (k1Down && k2Down);

            if (chord && !debugChordLatched)
            {
                ToggleDebugUiRoot();
                debugChordLatched = true;
            }

            if (!k1Held && !k2Held)
            {
                debugChordLatched = false;
            }
        }

        if (battleManager == null) return;

        RefreshHeroHpHud();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (BattleAutoSimPlugin.IsRunning)
        {
            return;
        }

        TickYourTurnBannerIdlePrompts();

        // 需在 handArea/statusText 建立前也能用；實際可否結束由 CanPlayerAct。Submit 後備給主鍵 Enter（專案 InputManager 的 return 軸），且排除 Space 以免與第二條 Submit（space 替代鍵）重複觸發。
        if (!isGamePaused)
        {
            bool spaceDown = Input.GetKeyDown(KeyCode.Space);
            bool enterDown =
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                (Input.GetButtonDown("Submit") && !spaceDown);
            if (spaceDown || enterDown)
            {
                battleManager.EndPlayerTurn();
            }
        }

        bool debugPanelVisible = debugUiRoot == null || debugUiRoot.activeSelf;
        BattleAutoSimPlugin.RefreshWinRateButtonForDebugUi(debugPanelVisible, isGamePaused);

        // Opening roll is on the full canvas; battle result drives end panel on uiRoot — keep both when debug half is hidden.
        TryPlayOpeningRollPresentation();
        UpdateBattleResultText();

        if (statusText == null || handArea == null) return;

        if (quickEndTurnButton != null)
        {
            quickEndTurnButton.interactable = !isGamePaused && battleManager.IsPlayerTurn();
        }

        if (Time.unscaledTime < nextRefreshTime) return;

        nextRefreshTime = Time.unscaledTime + 0.2f;

        if (debugPanelVisible)
        {
            statusText.text = battleManager.GetBattleStateText();
            if (deckText != null)
            {
                deckText.text =
                    "Player deck: " + battleManager.GetPlayerDeckCount() +
                    "  Enemy deck: " + battleManager.GetEnemyDeckCount() +
                    "\nPlayer discard: " + battleManager.GetPlayerDiscardCount() + "（" + battleManager.GetPlayerDiscardTopName() + "）" +
                    "  Enemy discard: " + battleManager.GetEnemyDiscardCount() + "（" + battleManager.GetEnemyDiscardTopName() + "）";
            }
            if (fieldText != null)
            {
                string toast = battleManager.GetBattleToastMessage();
                string toastLine = string.IsNullOrEmpty(toast)
                    ? string.Empty
                    : "\n<color=#AAFFCC>▶ " + toast + "</color>";
                string aiQuantLine =
                    "\n<color=#FFD580>" + battleManager.GetEnemyAiQuantifiedTextForPlayerView() + "</color>";
                fieldText.text =
                    battleManager.GetPlayerFieldText() + "\n" +
                    battleManager.GetEnemyFieldText() +
                    aiQuantLine +
                    toastLine;
            }
        }
        TickDiscardSelectionUi();
        if (roundText != null)
        {
            roundText.text = "Round " + battleManager.GetCurrentRound();
            if (debugPanelVisible) roundText.transform.SetAsLastSibling();
        }
        if (weatherBadgeTmp != null)
        {
            weatherBadgeTmp.text = "天氣：" + battleManager.GetCurrentWeatherLabelForUi();
        }
        if (weatherRemainTmp != null)
        {
            weatherRemainTmp.text = "效果剩餘回合：" + battleManager.GetCurrentWeatherRemainingRoundsForUi();
        }
        if (weatherHintTmp != null)
        {
            weatherHintTmp.text = "下一次天氣預報：" + battleManager.GetNextWeatherForecastHintForUi();
        }
        UpdateWeatherScreenEffects();
        if (activeWeatherEffectPanelRt != null && activeWeatherEffectPanelRt.gameObject.activeSelf)
        {
            RefreshActiveWeatherEffectPanelText();
        }
        int signature = ComputeHandSignature();
        int fieldSignature = ComputeFieldSignature();
        float currentScale = GetHandCardScale();
        float currentSpacing = GetHandCardSpacing();
        float currentTextScale = GetHandCardTextScale();
        float currentNameScale = GetHandCardNameScale();
        float currentBackplateScale = GetHandCardBackplateScale();
        float targetHandY = GetPlayerHandAreaYOffset();
        UpdatePlayerHandAreaYOffsetAnimated(targetHandY);
        if (enemyHandArea != null)
            UpdateEnemyHandAreaYOffsetAnimated(GetEnemyHandAreaYOffset());
        bool layoutChanged =
            !Mathf.Approximately(currentScale, lastScale) ||
            !Mathf.Approximately(currentSpacing, lastSpacing) ||
            !Mathf.Approximately(currentTextScale, lastTextScale) ||
            !Mathf.Approximately(currentNameScale, lastNameScale) ||
            !Mathf.Approximately(currentBackplateScale, lastBackplateScale);

        if (signature != lastHandSignature || layoutChanged)
        {
            lastHandSignature = signature;
            lastScale = currentScale;
            lastSpacing = currentSpacing;
            lastTextScale = currentTextScale;
            lastNameScale = currentNameScale;
            lastBackplateScale = currentBackplateScale;
            RebuildHandButtons();
            RebuildEnemyHandCards();
        }
        else
        {
            SetHandButtonsInteractable();
        }

        ApplyFieldZoneLayoutFromTuningIfChanged();

        float currentFieldScale = GetFieldMonsterScale();
        float currentFieldSpellScale = GetFieldSpellScale();
        bool fieldScaleChanged =
            !Mathf.Approximately(currentFieldScale, lastFieldScale) ||
            !Mathf.Approximately(currentFieldSpellScale, lastFieldSpellScale);
        int fieldTextSig = ComputeFieldTextTuningSignature();
        bool fieldTextChanged = fieldTextSig != lastFieldTextTuningSignature;
        if (fieldTextChanged) lastFieldTextTuningSignature = fieldTextSig;

        if (fieldSignature != lastFieldSignature)
        {
            if (deferFieldRefreshDuringAttack)
            {
                pendingFieldRefreshAfterAttack = true;
            }
            else
            {
                lastFieldSignature = fieldSignature;
                lastFieldScale = currentFieldScale;
                lastFieldSpellScale = currentFieldSpellScale;
                RefreshFieldCards();
            }
        }
        else if ((fieldScaleChanged || fieldTextChanged) && !deferFieldRefreshDuringAttack)
        {
            lastFieldScale = currentFieldScale;
            lastFieldSpellScale = currentFieldSpellScale;
            if (fieldScaleChanged) RefreshFieldCardLayoutsOnly();
            if (fieldTextChanged) RefreshFieldCardVisualTuningOnly();
        }

    }

    private void UpdatePlayerHandAreaYOffsetAnimated(float targetY)
    {
        if (handArea == null) return;
        if (float.IsNaN(handAreaTargetY))
        {
            handAreaYCurrent = targetY;
            handAreaTargetY = targetY;
            handArea.anchoredPosition = new Vector2(0f, handAreaYCurrent);
            return;
        }

        if (Mathf.Abs(targetY - handAreaTargetY) < 0.5f) return;
        handAreaTargetY = targetY;
        if (handAreaTweenRoutine != null) StopCoroutine(handAreaTweenRoutine);
        handAreaTweenRoutine = StartCoroutine(AnimateHandAreaY(
            handArea,
            handAreaTargetY,
            ApplyPlayerHandAreaY,
            ClearPlayerHandAreaTween));
    }

    private void ApplyPlayerHandAreaY(float y) => handAreaYCurrent = y;

    private void ClearPlayerHandAreaTween() => handAreaTweenRoutine = null;

    private void UpdateEnemyHandAreaYOffsetAnimated(float targetY)
    {
        if (enemyHandArea == null) return;
        if (float.IsNaN(enemyHandAreaTargetY))
        {
            enemyHandAreaYCurrent = targetY;
            enemyHandAreaTargetY = targetY;
            enemyHandArea.anchoredPosition = new Vector2(0f, enemyHandAreaYCurrent);
            return;
        }

        if (Mathf.Abs(targetY - enemyHandAreaTargetY) < 0.5f) return;
        enemyHandAreaTargetY = targetY;
        if (enemyHandAreaTweenRoutine != null) StopCoroutine(enemyHandAreaTweenRoutine);
        enemyHandAreaTweenRoutine = StartCoroutine(AnimateHandAreaY(
            enemyHandArea,
            enemyHandAreaTargetY,
            ApplyEnemyHandAreaY,
            ClearEnemyHandAreaTween));
    }

    private void ApplyEnemyHandAreaY(float y) => enemyHandAreaYCurrent = y;

    private void ClearEnemyHandAreaTween() => enemyHandAreaTweenRoutine = null;

    private IEnumerator AnimateHandAreaY(
        RectTransform area,
        float toY,
        System.Action<float> applyY,
        System.Action onComplete)
    {
        if (area == null) yield break;
        float fromY = area.anchoredPosition.y;
        float t = 0f;
        const float duration = 0.28f;
        while (t < duration && area != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            float y = Mathf.Lerp(fromY, toY, eased);
            applyY(y);
            area.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }
        if (area != null)
        {
            applyY(toY);
            area.anchoredPosition = new Vector2(0f, toY);
        }
        onComplete?.Invoke();
    }

    private Transform FindCanvas2()
    {
        // Prefer Canvas first, fallback to Canvas2/Canva2.
        GameObject targetCanvasObj = GameObject.Find("Canvas");
        if (targetCanvasObj == null) targetCanvasObj = GameObject.Find("Canvas2");
        if (targetCanvasObj == null) targetCanvasObj = GameObject.Find("Canva2");
        if (targetCanvasObj != null)
        {
            if (!targetCanvasObj.activeSelf) targetCanvasObj.SetActive(true);
            return targetCanvasObj.transform;
        }

        // Also search inactive objects and force-enable preferred canvas if found.
        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas canvas = null;
        Canvas canvas2 = null;
        Canvas canva2 = null;
        for (int i = 0; i < allCanvases.Length; i++)
        {
            Canvas c = allCanvases[i];
            if (c == null || c.gameObject == null) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            if (c.gameObject.name == "Canvas") canvas = c;
            else if (c.gameObject.name == "Canvas2") canvas2 = c;
            else if (c.gameObject.name == "Canva2") canva2 = c;
        }

        Canvas picked = canvas != null ? canvas : (canvas2 != null ? canvas2 : canva2);
        if (picked != null)
        {
            if (!picked.gameObject.activeSelf) picked.gameObject.SetActive(true);
            picked.enabled = true;
            return picked.transform;
        }

        // Last fallback: any active canvas in scene.
        Canvas anyCanvas = Object.FindFirstObjectByType<Canvas>();
        if (anyCanvas != null)
        {
            if (!anyCanvas.gameObject.activeSelf) anyCanvas.gameObject.SetActive(true);
            anyCanvas.enabled = true;
            return anyCanvas.transform;
        }

        return null;
    }

    private void CreateHeroHpHud(Transform canvasParent)
    {
        if (canvasParent == null) return;

        float margin = 28f;
        float heroHudShiftRight = 44f;
        float hpNumSize = Mathf.Clamp(Screen.height * 0.12f, 72f, 130f);
        float titleSize = Mathf.Clamp(hpNumSize * 0.26f, 20f, 36f);

        GameObject enemyHeroObj = new GameObject("EnemyHeroHpHud", typeof(RectTransform), typeof(TextMeshProUGUI));
        enemyHeroObj.transform.SetParent(canvasParent, false);
        RectTransform enemyHeroRt = enemyHeroObj.GetComponent<RectTransform>();
        enemyHeroRt.anchorMin = new Vector2(0f, 1f);
        enemyHeroRt.anchorMax = new Vector2(0f, 1f);
        enemyHeroRt.pivot = new Vector2(0f, 1f);
        enemyHeroRt.anchoredPosition = new Vector2(margin + heroHudShiftRight, -margin);
        enemyHeroRt.sizeDelta = new Vector2(560f, hpNumSize + titleSize + 40f);
        enemyHeroHpText = enemyHeroObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) enemyHeroHpText.font = sharedUIFont;
        enemyHeroHpText.fontSize = hpNumSize;
        enemyHeroHpText.alignment = TextAlignmentOptions.TopLeft;
        enemyHeroHpText.color = new Color(1f, 0.55f, 0.45f, 1f);
        enemyHeroHpText.enableWordWrapping = false;
        enemyHeroHpText.raycastTarget = false;
        enemyHeroHpText.richText = true;
        Outline eo = enemyHeroObj.AddComponent<Outline>();
        eo.effectColor = new Color(0f, 0f, 0f, 0.92f);
        eo.effectDistance = new Vector2(3f, -3f);

        GameObject playerHeroObj = new GameObject("PlayerHeroHpHud", typeof(RectTransform), typeof(TextMeshProUGUI));
        playerHeroObj.transform.SetParent(canvasParent, false);
        RectTransform playerHeroRt = playerHeroObj.GetComponent<RectTransform>();
        playerHeroRt.anchorMin = new Vector2(0f, 0f);
        playerHeroRt.anchorMax = new Vector2(0f, 0f);
        playerHeroRt.pivot = new Vector2(0f, 0f);
        playerHeroRt.anchoredPosition = new Vector2(margin + heroHudShiftRight, margin);
        playerHeroRt.sizeDelta = new Vector2(560f, hpNumSize + titleSize + 40f);
        playerHeroHpText = playerHeroObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) playerHeroHpText.font = sharedUIFont;
        playerHeroHpText.fontSize = hpNumSize;
        playerHeroHpText.alignment = TextAlignmentOptions.BottomLeft;
        playerHeroHpText.color = new Color(0.55f, 0.92f, 1f, 1f);
        playerHeroHpText.enableWordWrapping = false;
        playerHeroHpText.raycastTarget = false;
        playerHeroHpText.richText = true;
        Outline po = playerHeroObj.AddComponent<Outline>();
        po.effectColor = new Color(0f, 0f, 0f, 0.92f);
        po.effectDistance = new Vector2(3f, -3f);

        heroHudTitleSize = titleSize;

        enemyHeroObj.transform.SetAsLastSibling();
        playerHeroObj.transform.SetAsLastSibling();
    }

    private void RefreshHeroHpHud()
    {
        if (battleManager == null) return;
        int sid = battleManager.GetBattleSessionId();
        if (sid != lastHeroHudBattleSessionId)
        {
            lastHeroHudBattleSessionId = sid;
            lastShownPlayerHeroHp = int.MinValue;
            lastShownEnemyHeroHp = int.MinValue;
        }
        int p = battleManager.GetPlayerHeroHp();
        int e = battleManager.GetEnemyHeroHp();
        bool playerHeroDamaged = lastShownPlayerHeroHp != int.MinValue && p < lastShownPlayerHeroHp;
        if (p == lastShownPlayerHeroHp && e == lastShownEnemyHeroHp) return;
        lastShownPlayerHeroHp = p;
        lastShownEnemyHeroHp = e;

        int t = Mathf.RoundToInt(heroHudTitleSize);
        if (playerHeroHpText != null)
        {
            playerHeroHpText.text =
                "<size=" + t + "><color=#E8FFFFFF>我方英雄</color></size>\n<b>" + p + "</b>";
        }
        if (enemyHeroHpText != null)
        {
            enemyHeroHpText.text =
                "<size=" + t + "><color=#E8FFFFFF>敵方英雄</color></size>\n<b>" + e + "</b>";
        }

        if (playerHeroDamaged && playerHeroDamagedFxRoutine == null && !BattleAutoSimPlugin.IsRunning)
            playerHeroDamagedFxRoutine = StartCoroutine(CoPlayPlayerHeroDamagedFeedback());
    }

    private IEnumerator CoPlayPlayerHeroDamagedFeedback()
    {
        float hitDur = battleManager != null ? Mathf.Max(0.1f, battleManager.hitShakeDuration * 0.9f) : 0.22f;
        float heroShakeStrength = 26f;
        float handShakeStrength = 36f;

        RectTransform heroRt = playerHeroHpText != null ? playerHeroHpText.rectTransform : null;
        if (heroRt != null) StartCoroutine(PlayHitShake(heroRt, hitDur, heroShakeStrength));
        if (handArea != null) StartCoroutine(PlayHitShake(handArea, hitDur * 0.95f, handShakeStrength));
        if (playerHeroHpText != null) StartCoroutine(PlayDamageFlash(playerHeroHpText.gameObject, hitDur));
        if (enableHeroDamageMonochromeFlash) StartCoroutine(CoPlayHeroDamageMonochromeFlash());

        yield return new WaitForSecondsRealtime(hitDur + 0.06f);
        playerHeroDamagedFxRoutine = null;
    }

    private IEnumerator CoPlayHeroDamageMonochromeFlash()
    {
        if (heroDamageMonochromeFlashCg == null || heroDamageMonochromeFlashRt == null) yield break;

        float flash = Mathf.Clamp(heroDamageMonochromeFlashSeconds, 0.08f, 0.12f);
        float recover = Mathf.Clamp(heroDamageMonochromeRecoverSeconds, 0.12f, 0.4f);
        const float peakAlpha = 0.92f;

        heroDamageMonochromeFlashRt.SetAsLastSibling();
        heroDamageMonochromeFlashCg.alpha = peakAlpha;
        if (flash > 0f) yield return new WaitForSecondsRealtime(flash);

        float t = 0f;
        while (t < recover && heroDamageMonochromeFlashCg != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / recover);
            heroDamageMonochromeFlashCg.alpha = Mathf.Lerp(peakAlpha, 0f, p);
            yield return null;
        }
        if (heroDamageMonochromeFlashCg != null) heroDamageMonochromeFlashCg.alpha = 0f;
    }

    private void CreateBattleTurnBanner(Transform canvasParent)
    {
        if (canvasParent == null) return;

        GameObject go = new GameObject("BattleTurnBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Shadow));
        go.transform.SetParent(canvasParent, false);
        turnBannerPanelRt = go.GetComponent<RectTransform>();
        turnBannerPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        turnBannerPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        turnBannerPanelRt.pivot = new Vector2(0.5f, 0.5f);
        turnBannerPanelRt.anchoredPosition = Vector2.zero;
        turnBannerPanelRt.sizeDelta = new Vector2(540f, 112f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.34f, 0.4f, 0.3f, 0.95f);
        bg.raycastTarget = false;

        Shadow sh = go.GetComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
        sh.effectDistance = new Vector2(6f, -7f);

        turnBannerCg = go.GetComponent<CanvasGroup>();
        turnBannerCg.alpha = 0f;
        turnBannerCg.blocksRaycasts = false;
        turnBannerCg.interactable = false;
        go.SetActive(false);

        GameObject textGo = new GameObject("TurnBannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 14f);
        trt.offsetMax = new Vector2(-20f, -14f);
        turnBannerTmp = textGo.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) turnBannerTmp.font = sharedUIFont;
        turnBannerTmp.fontSize = 42f;
        turnBannerTmp.alignment = TextAlignmentOptions.Center;
        turnBannerTmp.color = new Color(0.98f, 0.96f, 0.9f, 1f);
        turnBannerTmp.raycastTarget = false;
        turnBannerTmp.enableWordWrapping = true;
        turnBannerTmp.text = string.Empty;

        Outline ol = textGo.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.78f);
        ol.effectDistance = new Vector2(2.5f, -2.5f);
    }

    /// <summary>將「你的回合／敵方操作中…」置於同 Canvas 下其他戰鬥 UI 之上，並維持在除錯面板之下（除錯為最後子物件）。</summary>
    private void ApplyBattleTurnBannerStackOrder()
    {
        if (turnBannerPanelRt == null || debugUiRoot == null) return;
        Transform bannerParent = turnBannerPanelRt.parent;
        if (bannerParent == null || debugUiRoot.transform.parent != bannerParent) return;

        turnBannerPanelRt.SetAsLastSibling();
        debugUiRoot.transform.SetAsLastSibling();
    }

    private void ForceHideTurnBanner()
    {
        if (turnBannerRoutine != null)
        {
            StopCoroutine(turnBannerRoutine);
            turnBannerRoutine = null;
        }
        if (turnBannerCg != null) turnBannerCg.alpha = 0f;
        if (turnBannerPanelRt != null) turnBannerPanelRt.gameObject.SetActive(false);
    }

    private void SyncTurnBannerWithBattleState()
    {
        if (battleManager == null) return;
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager.IsBattleOver())
        {
            ForceHideTurnBanner();
            return;
        }
        if (battleManager.IsOpeningPresentationInProgress())
        {
            ForceHideTurnBanner();
            return;
        }
        if (battleManager.IsPlayerTurn())
        {
            ForceHideTurnBanner();
            DisarmYourTurnBannerAllIdlePromptClocks();
            yourTurnBannerTurnStartIdleClockStartUnscaled = Time.unscaledTime;
            return;
        }
        OnTurnBannerRequested(BattleTurnBannerKind.EnemyTurn);
    }

    private void OnTurnBannerRequested(BattleTurnBannerKind kind)
    {
        if (turnBannerCg == null || turnBannerPanelRt == null || turnBannerTmp == null) return;

        if (kind == BattleTurnBannerKind.PlayerTurn && !turnBannerPlayerFromIdleTimeout)
            return;

        if (kind == BattleTurnBannerKind.Hidden || kind == BattleTurnBannerKind.EnemyTurn)
            DisarmYourTurnBannerAllIdlePromptClocks();

        if (turnBannerRoutine != null)
        {
            StopCoroutine(turnBannerRoutine);
            turnBannerRoutine = null;
        }

        switch (kind)
        {
            case BattleTurnBannerKind.Hidden:
                turnBannerRoutine = StartCoroutine(TurnBannerFadeOutRoutine());
                break;
            case BattleTurnBannerKind.PlayerTurn:
                turnBannerPlayerFromIdleTimeout = false;
                turnBannerTmp.text = "你的回合";
                turnBannerTmp.color = new Color(0.63f, 0.86f, 0.62f, 1f);
                turnBannerPanelRt.anchoredPosition = Vector2.zero;
                turnBannerPanelRt.gameObject.SetActive(true);
                turnBannerCg.alpha = 0f;
                ApplyBattleTurnBannerStackOrder();
                turnBannerRoutine = StartCoroutine(TurnBannerFadeInAndFloatRoutine());
                if (playerHandPressDepth > 0)
                    ForceHideTurnBanner();
                break;
            case BattleTurnBannerKind.EnemyTurn:
                turnBannerTmp.text = "敵方操作中...";
                turnBannerTmp.color = new Color(0.88f, 0.56f, 0.45f, 1f);
                turnBannerPanelRt.anchoredPosition = Vector2.zero;
                turnBannerPanelRt.gameObject.SetActive(true);
                turnBannerCg.alpha = 0f;
                ApplyBattleTurnBannerStackOrder();
                turnBannerRoutine = StartCoroutine(TurnBannerFadeInAndFloatRoutine());
                break;
        }
    }

    private IEnumerator TurnBannerFadeOutRoutine()
    {
        if (turnBannerCg == null) yield break;
        float dur = 0.2f;
        float t = 0f;
        float start = turnBannerCg.alpha;
        while (t < dur && turnBannerCg != null)
        {
            t += Time.unscaledDeltaTime;
            turnBannerCg.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / dur));
            yield return null;
        }
        if (turnBannerCg != null) turnBannerCg.alpha = 0f;
        if (turnBannerPanelRt != null) turnBannerPanelRt.gameObject.SetActive(false);
        turnBannerRoutine = null;
    }

    private IEnumerator TurnBannerFadeInAndFloatRoutine()
    {
        if (turnBannerCg == null || turnBannerPanelRt == null) yield break;
        const float fadeIn = 0.24f;
        float t = 0f;
        while (t < fadeIn && turnBannerCg != null)
        {
            t += Time.unscaledDeltaTime;
            turnBannerCg.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / fadeIn));
            yield return null;
        }
        if (turnBannerCg != null) turnBannerCg.alpha = 1f;

        while (turnBannerPanelRt != null && turnBannerPanelRt.gameObject.activeInHierarchy)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 2.55f) * 8f;
            turnBannerPanelRt.anchoredPosition = new Vector2(0f, bob);
            yield return null;
        }
        turnBannerRoutine = null;
    }

    /// <summary>其他 UI 用：已達任一「你的回合」閒置提示條件（與浮窗顯示併用）。</summary>
    public bool IsPlayerTurnUiIdleStandbyMode => playerTurnUiIdleStandbyMode;

    private void StopYourTurnBannerHandTouchNoPlayArmDeferRoutine()
    {
        if (yourTurnBannerHandTouchNoPlayArmDeferRoutine == null) return;
        StopCoroutine(yourTurnBannerHandTouchNoPlayArmDeferRoutine);
        yourTurnBannerHandTouchNoPlayArmDeferRoutine = null;
    }

    private void DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly()
    {
        yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
    }

    private void DisarmYourTurnBannerAllIdlePromptClocks()
    {
        yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
        yourTurnBannerAfterFieldPlayClockStartUnscaled = -1f;
        yourTurnBannerIdlePromptShownThisWindow = false;
        playerTurnUiIdleStandbyMode = false;
        yourTurnBannerHandTouchSessionLedToPlay = false;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
    }

    private void ClearYourTurnBannerIdlePromptClockArmsOnly()
    {
        yourTurnBannerTurnStartIdleClockStartUnscaled = -1f;
        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = -1f;
        yourTurnBannerAfterFieldPlayClockStartUnscaled = -1f;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
    }

    private void OnPlayerTurnActionWindowOpenedForPromptUi()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        DisarmYourTurnBannerAllIdlePromptClocks();
        yourTurnBannerTurnStartIdleClockStartUnscaled = Time.unscaledTime;
        ForceHideTurnBanner();
    }

    private void OnPlayerCommittedHandCardToFieldFromHand()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly();
        yourTurnBannerAfterFieldPlayClockStartUnscaled = Time.unscaledTime;
        yourTurnBannerIdlePromptShownThisWindow = false;
        playerTurnUiIdleStandbyMode = false;
        ForceHideTurnBanner();
    }

    private void OnPlayerPressedEndTurnForPromptUi()
    {
        DisarmYourTurnBannerAllIdlePromptClocks();
        ForceHideTurnBanner();
    }

    private float GetYourTurnIdlePromptThresholdSeconds()
    {
        return Mathf.Max(10f, playerTurnIdlePromptSeconds);
    }

    private IEnumerator YourTurnBannerHandTouchNoPlayMaybeArmNextFrameRoutine()
    {
        yield return null;
        yourTurnBannerHandTouchNoPlayArmDeferRoutine = null;
        if (BattleAutoSimPlugin.IsRunning) yield break;
        if (battleManager == null || !battleManager.IsPlayerTurn() || battleManager.IsBattleOver()) yield break;
        if (battleManager.IsOpeningPresentationInProgress()) yield break;
        if (battleManager.IsTurnSequenceInProgress()) yield break;
        if (battleManager.IsSpellCastPresentationActive()) yield break;
        if (isGamePaused) yield break;
        if (yourTurnBannerHandTouchSessionLedToPlay) yield break;
        if (playerHandPressDepth > 0) yield break;

        yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled = Time.unscaledTime;
        yourTurnBannerIdlePromptShownThisWindow = false;
    }

    /// <summary>回合開始無操作／觸碰手牌後未出牌／手牌上場後未結束回合，逾時顯示「你的回合」。</summary>
    private void TickYourTurnBannerIdlePrompts()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager == null) return;
        if (!battleManager.IsPlayerTurn() || battleManager.IsBattleOver()) return;
        if (battleManager.IsOpeningPresentationInProgress()) return;
        if (battleManager.IsTurnSequenceInProgress()) return;
        if (battleManager.IsSpellCastPresentationActive()) return;
        if (isGamePaused) return;

        bool anyClockArmed =
            yourTurnBannerTurnStartIdleClockStartUnscaled >= 0f ||
            yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled >= 0f ||
            yourTurnBannerAfterFieldPlayClockStartUnscaled >= 0f;
        if (!anyClockArmed) return;
        if (playerHandPressDepth > 0) return;
        if (isPlayingCardAnimation) return;

        float threshold = GetYourTurnIdlePromptThresholdSeconds();
        float deadline = float.MaxValue;
        if (yourTurnBannerTurnStartIdleClockStartUnscaled >= 0f)
            deadline = Mathf.Min(deadline, yourTurnBannerTurnStartIdleClockStartUnscaled + threshold);
        if (yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled >= 0f)
            deadline = Mathf.Min(deadline, yourTurnBannerAfterHandTouchNoPlayClockStartUnscaled + threshold);
        if (yourTurnBannerAfterFieldPlayClockStartUnscaled >= 0f)
            deadline = Mathf.Min(deadline, yourTurnBannerAfterFieldPlayClockStartUnscaled + threshold);

        if (Time.unscaledTime <= deadline) return;

        playerTurnUiIdleStandbyMode = true;
        if (yourTurnBannerIdlePromptShownThisWindow) return;
        if (IsPlayerTurnBannerVisuallyShowing()) return;

        yourTurnBannerIdlePromptShownThisWindow = true;
        ClearYourTurnBannerIdlePromptClockArmsOnly();
        turnBannerPlayerFromIdleTimeout = true;
        OnTurnBannerRequested(BattleTurnBannerKind.PlayerTurn);
    }

    private void NotifyTurnIdlePromptPlayerTookPlayOrAttackIntent()
    {
        yourTurnBannerHandTouchSessionLedToPlay = true;
        DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly();
    }

    /// <summary>我方手牌按下（由 <see cref="BattlePlayerHandCardPressNotifier"/> 呼叫）。</summary>
    public void NotifyPlayerHandCardPressBegan()
    {
        if (battleManager == null || !battleManager.IsPlayerTurn()) return;
        if (BattleAutoSimPlugin.IsRunning) return;
        playerHandPressDepth++;
        if (playerHandPressDepth == 1)
        {
            yourTurnBannerHandTouchSessionLedToPlay = false;
            ForceHideTurnBanner();
        }
    }

    /// <summary>我方手牌放開或指標離開手牌（由 <see cref="BattlePlayerHandCardPressNotifier"/> 呼叫）。</summary>
    public void NotifyPlayerHandCardPressEnded()
    {
        if (playerHandPressDepth <= 0) return;
        playerHandPressDepth--;
        if (playerHandPressDepth > 0) return;
        if (BattleAutoSimPlugin.IsRunning) return;
        if (battleManager == null || !battleManager.IsPlayerTurn()) return;
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
        yourTurnBannerHandTouchNoPlayArmDeferRoutine = StartCoroutine(YourTurnBannerHandTouchNoPlayMaybeArmNextFrameRoutine());
    }

    private bool IsPlayerTurnBannerVisuallyShowing()
    {
        return turnBannerPanelRt != null &&
               turnBannerPanelRt.gameObject.activeSelf &&
               turnBannerCg != null &&
               turnBannerCg.alpha > 0.08f &&
               turnBannerTmp != null &&
               turnBannerTmp.text == "你的回合";
    }

    private static void AttachPlayerHandPressNotifier(GameObject cardRoot, BattleSimulationDebugUI host)
    {
        if (cardRoot == null || host == null) return;
        BattlePlayerHandCardPressNotifier n = cardRoot.GetComponent<BattlePlayerHandCardPressNotifier>();
        if (n == null) n = cardRoot.AddComponent<BattlePlayerHandCardPressNotifier>();
        n.Init(host);
    }

    private void CreateDebugPanel(Transform parent)
    {
        uiRoot = parent as RectTransform;
        float m = DebugUiChromeMul;

        debugUiRoot = new GameObject("BattleDebugUIRoot");
        debugUiRoot.transform.SetParent(parent, false);
        RectTransform dbgRt = debugUiRoot.AddComponent<RectTransform>();
        dbgRt.anchorMin = new Vector2(0.5f, 0.5f);
        dbgRt.anchorMax = new Vector2(0.5f, 0.5f);
        dbgRt.pivot = new Vector2(0.5f, 0.5f);
        dbgRt.anchoredPosition = Vector2.zero;
        float winW = Mathf.Clamp(Screen.width * 0.9f, 860f, 3000f);
        float winH = Mathf.Clamp(Screen.height * 0.9f, 660f, 1800f);
        dbgRt.sizeDelta = new Vector2(winW, winH);
        Transform dbg = debugUiRoot.transform;

        GameObject panel = new GameObject("DebugBattlePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(dbg, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = new Vector2(8f, 8f);
        panelRect.offsetMax = new Vector2(-8f, -8f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.15f, 0.11f, 0.08f, 0.6f);
        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        panelShadow.effectDistance = new Vector2(4f, -4f);

        Button closeDebugBtn = CreateButton(panel.transform, "CloseDebugPanelButton", "Close", new Vector2(0f, 0f), CloseDebugUiRoot, true);
        RectTransform closeRt = closeDebugBtn.GetComponent<RectTransform>();
        if (closeRt != null)
        {
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-14f, -14f);
            closeRt.sizeDelta = new Vector2(170f, 54f);
        }

        BindSceneEndTurnButton(parent);
        CreatePauseUI(parent);

        GameObject autoSimSlotObj = new GameObject("AutoSimProgressSlot", typeof(RectTransform));
        autoSimSlotObj.transform.SetParent(panel.transform, false);
        RectTransform autoSimSlotRt = autoSimSlotObj.GetComponent<RectTransform>();
        autoSimSlotRt.anchorMin = new Vector2(0f, 0f);
        autoSimSlotRt.anchorMax = new Vector2(1f, 0f);
        autoSimSlotRt.pivot = new Vector2(0.5f, 0f);
        autoSimSlotRt.offsetMin = new Vector2(10f, 78f);
        autoSimSlotRt.offsetMax = new Vector2(-10f, 78f + 520f);
        BattleAutoSimPlugin.ProgressUiParent = autoSimSlotObj.transform;
        BattleAutoSimPlugin.PrepareEmbeddedProgressShellIfEmbedded();

        GameObject roundObj = new GameObject("RoundText", typeof(RectTransform), typeof(Text));
        roundObj.transform.SetParent(panel.transform, false);
        RectTransform roundRect = roundObj.GetComponent<RectTransform>();
        roundRect.anchorMin = new Vector2(0f, 1f);
        roundRect.anchorMax = new Vector2(1f, 1f);
        roundRect.pivot = new Vector2(0.5f, 1f);
        roundRect.offsetMin = new Vector2(10f, -58f);
        roundRect.offsetMax = new Vector2(-10f, -8f);
        roundText = roundObj.GetComponent<Text>();
        roundText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        roundText.fontSize = Mathf.RoundToInt(32 * m);
        roundText.alignment = TextAnchor.MiddleCenter;
        roundText.color = new Color(0.98f, 0.9f, 0.62f, 1f);
        roundText.raycastTarget = false;
        roundText.text = "Round 1";

        GameObject weatherBadgeObj = new GameObject("WeatherBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        weatherBadgeObj.transform.SetParent(panel.transform, false);
        RectTransform weatherBadgeRt = weatherBadgeObj.GetComponent<RectTransform>();
        weatherBadgeRt.anchorMin = new Vector2(1f, 1f);
        weatherBadgeRt.anchorMax = new Vector2(1f, 1f);
        weatherBadgeRt.pivot = new Vector2(1f, 1f);
        weatherBadgeRt.anchoredPosition = new Vector2(-22f, -20f);
        weatherBadgeRt.sizeDelta = new Vector2(320f, 52f);
        weatherBadgeTmp = weatherBadgeObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) weatherBadgeTmp.font = sharedUIFont;
        weatherBadgeTmp.fontSize = 28f;
        weatherBadgeTmp.alignment = TextAlignmentOptions.Right;
        weatherBadgeTmp.color = new Color(0.95f, 0.86f, 0.6f, 1f);
        weatherBadgeTmp.text = "天氣：無";
        weatherBadgeTmp.raycastTarget = false;

        GameObject weatherRemainObj = new GameObject("WeatherRemainText", typeof(RectTransform), typeof(TextMeshProUGUI));
        weatherRemainObj.transform.SetParent(panel.transform, false);
        RectTransform weatherRemainRt = weatherRemainObj.GetComponent<RectTransform>();
        weatherRemainRt.anchorMin = new Vector2(1f, 1f);
        weatherRemainRt.anchorMax = new Vector2(1f, 1f);
        weatherRemainRt.pivot = new Vector2(1f, 1f);
        weatherRemainRt.anchoredPosition = new Vector2(-22f, -66f);
        weatherRemainRt.sizeDelta = new Vector2(430f, 42f);
        weatherRemainTmp = weatherRemainObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) weatherRemainTmp.font = sharedUIFont;
        weatherRemainTmp.fontSize = 24f;
        weatherRemainTmp.alignment = TextAlignmentOptions.Right;
        weatherRemainTmp.color = new Color(0.78f, 0.9f, 0.78f, 1f);
        weatherRemainTmp.text = "效果剩餘回合：0";
        weatherRemainTmp.raycastTarget = false;

        GameObject weatherHintObj = new GameObject("WeatherHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        weatherHintObj.transform.SetParent(panel.transform, false);
        RectTransform weatherHintRt = weatherHintObj.GetComponent<RectTransform>();
        weatherHintRt.anchorMin = new Vector2(1f, 1f);
        weatherHintRt.anchorMax = new Vector2(1f, 1f);
        weatherHintRt.pivot = new Vector2(1f, 1f);
        weatherHintRt.anchoredPosition = new Vector2(-22f, -104f);
        weatherHintRt.sizeDelta = new Vector2(720f, 42f);
        weatherHintTmp = weatherHintObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) weatherHintTmp.font = sharedUIFont;
        weatherHintTmp.fontSize = 22f;
        weatherHintTmp.alignment = TextAlignmentOptions.Right;
        weatherHintTmp.color = new Color(0.72f, 0.86f, 0.7f, 1f);
        weatherHintTmp.text = "下一次天氣預報：初始回合不觸發";
        weatherHintTmp.raycastTarget = false;

        GameObject deckObj = new GameObject("DeckText", typeof(RectTransform), typeof(TextMeshProUGUI));
        deckObj.transform.SetParent(panel.transform, false);
        RectTransform deckRect = deckObj.GetComponent<RectTransform>();
        deckRect.anchorMin = new Vector2(0f, 1f);
        deckRect.anchorMax = new Vector2(1f, 1f);
        deckRect.pivot = new Vector2(0.5f, 1f);
        deckRect.offsetMin = new Vector2(14f, -150f);
        deckRect.offsetMax = new Vector2(-14f, -62f);
        deckText = deckObj.GetComponent<TextMeshProUGUI>();
        deckText.fontSize = 22f * m;
        deckText.color = Color.white;
        deckText.raycastTarget = false;

        GameObject openingRollPanelObj = new GameObject("OpeningRollPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        openingRollPanelObj.transform.SetParent(parent, false);
        openingRollPanel = openingRollPanelObj.GetComponent<RectTransform>();
        openingRollPanel.anchorMin = new Vector2(0.5f, 0.5f);
        openingRollPanel.anchorMax = new Vector2(0.5f, 0.5f);
        openingRollPanel.pivot = new Vector2(0.5f, 0.5f);
        openingRollPanel.anchoredPosition = Vector2.zero;
        openingRollPanel.sizeDelta = new Vector2(520f, 172f);
        Image openingRollBg = openingRollPanelObj.GetComponent<Image>();
        openingRollBg.color = new Color(0.45f, 0.36f, 0.3f, 1f);
        openingRollGroup = openingRollPanelObj.GetComponent<CanvasGroup>();
        openingRollGroup.alpha = 0f;
        openingRollPanelObj.SetActive(false);

        GameObject openingRollDiceObj = new GameObject("OpeningRollDiceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        openingRollDiceObj.transform.SetParent(openingRollPanelObj.transform, false);
        RectTransform openingRollDiceRect = openingRollDiceObj.GetComponent<RectTransform>();
        openingRollDiceRect.anchorMin = new Vector2(0f, 0.5f);
        openingRollDiceRect.anchorMax = new Vector2(1f, 0.5f);
        openingRollDiceRect.pivot = new Vector2(0.5f, 0.5f);
        openingRollDiceRect.anchoredPosition = new Vector2(0f, -8f);
        openingRollDiceRect.sizeDelta = new Vector2(-24f, 56f);
        openingRollDiceText = openingRollDiceObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) openingRollDiceText.font = sharedUIFont;
        openingRollDiceText.fontSize = 34f;
        openingRollDiceText.alignment = TextAlignmentOptions.Center;
        openingRollDiceText.color = new Color(0.98f, 0.98f, 0.98f, 1f);
        openingRollDiceText.text = string.Empty;

        openingPlayerDicePips = CreateDicePipGrid(openingRollPanelObj.transform, "OpeningPlayerDiceIcon", new Vector2(-132f, 44f));
        openingEnemyDicePips = CreateDicePipGrid(openingRollPanelObj.transform, "OpeningEnemyDiceIcon", new Vector2(132f, 44f));

        GameObject openingRollFirstObj = new GameObject("OpeningRollFirstText", typeof(RectTransform), typeof(TextMeshProUGUI));
        openingRollFirstObj.transform.SetParent(openingRollPanelObj.transform, false);
        RectTransform openingRollFirstRect = openingRollFirstObj.GetComponent<RectTransform>();
        openingRollFirstRect.anchorMin = new Vector2(0f, 0f);
        openingRollFirstRect.anchorMax = new Vector2(1f, 0f);
        openingRollFirstRect.pivot = new Vector2(0.5f, 0f);
        openingRollFirstRect.anchoredPosition = new Vector2(0f, 2f);
        openingRollFirstRect.sizeDelta = new Vector2(-24f, 58f);
        openingRollFirstText = openingRollFirstObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) openingRollFirstText.font = sharedUIFont;
        openingRollFirstText.fontSize = 28f;
        openingRollFirstText.alignment = TextAlignmentOptions.Center;
        openingRollFirstText.color = new Color(1f, 0.9f, 0.4f, 1f);
        openingRollFirstText.text = string.Empty;

        EnsureUIAudioSource(parent);
        openingRollSfx = CreateProceduralDiceRollClip();

        GameObject fieldObj = new GameObject("FieldText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fieldObj.transform.SetParent(panel.transform, false);
        RectTransform fieldRect = fieldObj.GetComponent<RectTransform>();
        fieldRect.anchorMin = new Vector2(0f, 1f);
        fieldRect.anchorMax = new Vector2(1f, 1f);
        fieldRect.pivot = new Vector2(0.5f, 1f);
        fieldRect.offsetMin = new Vector2(14f, -322f);
        fieldRect.offsetMax = new Vector2(-14f, -158f);
        fieldText = fieldObj.GetComponent<TextMeshProUGUI>();
        fieldText.fontSize = 20f * m;
        fieldText.color = Color.white;
        fieldText.raycastTarget = false;
        fieldText.richText = true;
        fieldText.enableWordWrapping = true;

        CreateBattleResultText(panel.transform, true);

        GameObject textObj = new GameObject("StateText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.offsetMin = new Vector2(12f, 612f);
        textRect.offsetMax = new Vector2(-12f, 728f);
        statusText = textObj.GetComponent<TextMeshProUGUI>();
        statusText.fontSize = 19f * m;
        statusText.enableWordWrapping = true;
        statusText.color = Color.white;
        statusText.text = battleManager.GetBattleStateText();
        statusText.raycastTarget = false;

        GameObject handObj = new GameObject("HandArea", typeof(RectTransform));
        handObj.transform.SetParent(parent, false);
        handArea = handObj.GetComponent<RectTransform>();
        handArea.anchorMin = new Vector2(0.5f, 0f);
        handArea.anchorMax = new Vector2(0.5f, 0f);
        handArea.pivot = new Vector2(0.5f, 0f);
        handArea.anchoredPosition = new Vector2(0f, GetPlayerHandAreaYOffset());
        handArea.sizeDelta = new Vector2(1100f, 230f);
        handAreaYCurrent = handArea.anchoredPosition.y;
        handAreaTargetY = handAreaYCurrent;

        GameObject enemyHandObj = new GameObject("EnemyHandArea", typeof(RectTransform));
        enemyHandObj.transform.SetParent(parent, false);
        enemyHandArea = enemyHandObj.GetComponent<RectTransform>();
        enemyHandArea.anchorMin = new Vector2(0.5f, 1f);
        enemyHandArea.anchorMax = new Vector2(0.5f, 1f);
        enemyHandArea.pivot = new Vector2(0.5f, 1f);
        enemyHandArea.anchoredPosition = new Vector2(0f, GetEnemyHandAreaYOffset());
        enemyHandArea.sizeDelta = new Vector2(1100f, 180f);
        enemyHandAreaYCurrent = enemyHandArea.anchoredPosition.y;
        enemyHandAreaTargetY = enemyHandAreaYCurrent;

        CreateDeckPileVisual(parent, false);
        CreateDeckPileVisual(parent, true);

        CreateHeroHpHud(parent);
        CreateBattleTurnBanner(parent);

        GameObject playerFieldObj = new GameObject("PlayerFieldArea", typeof(RectTransform));
        playerFieldObj.transform.SetParent(parent, false);
        playerFieldArea = playerFieldObj.GetComponent<RectTransform>();
        playerFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        playerFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        playerFieldArea.pivot = new Vector2(0.5f, 0.5f);
        playerFieldArea.anchoredPosition = Vector2.zero;
        playerFieldArea.sizeDelta = new Vector2(260f, 300f);

        GameObject playerSpellFieldObj = new GameObject("PlayerSpellFieldArea", typeof(RectTransform));
        playerSpellFieldObj.transform.SetParent(parent, false);
        playerSpellFieldArea = playerSpellFieldObj.GetComponent<RectTransform>();
        playerSpellFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        playerSpellFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        playerSpellFieldArea.pivot = new Vector2(0.5f, 0.5f);
        playerSpellFieldArea.anchoredPosition = Vector2.zero;
        playerSpellFieldArea.sizeDelta = new Vector2(150f, 300f);

        GameObject enemyFieldObj = new GameObject("EnemyFieldArea", typeof(RectTransform));
        enemyFieldObj.transform.SetParent(parent, false);
        enemyFieldArea = enemyFieldObj.GetComponent<RectTransform>();
        enemyFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        enemyFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        enemyFieldArea.pivot = new Vector2(0.5f, 0.5f);
        enemyFieldArea.anchoredPosition = Vector2.zero;
        enemyFieldArea.sizeDelta = new Vector2(260f, 300f);
        enemyFieldArea.SetAsLastSibling();

        GameObject enemySpellFieldObj = new GameObject("EnemySpellFieldArea", typeof(RectTransform));
        enemySpellFieldObj.transform.SetParent(parent, false);
        enemySpellFieldArea = enemySpellFieldObj.GetComponent<RectTransform>();
        enemySpellFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        enemySpellFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        enemySpellFieldArea.pivot = new Vector2(0.5f, 0.5f);
        enemySpellFieldArea.anchoredPosition = Vector2.zero;
        enemySpellFieldArea.sizeDelta = new Vector2(150f, 300f);
        enemySpellFieldArea.SetAsLastSibling();

        ApplyFieldZoneLayoutFromTuning(force: true);

        GameObject weatherOverlayObj = new GameObject("WeatherForecastOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        weatherOverlayObj.transform.SetParent(parent, false);
        weatherForecastOverlayRt = weatherOverlayObj.GetComponent<RectTransform>();
        weatherForecastOverlayRt.anchorMin = Vector2.zero;
        weatherForecastOverlayRt.anchorMax = Vector2.one;
        weatherForecastOverlayRt.offsetMin = Vector2.zero;
        weatherForecastOverlayRt.offsetMax = Vector2.zero;
        Image weatherOverlayBg = weatherOverlayObj.GetComponent<Image>();
        weatherOverlayBg.color = new Color(0.12f, 0.08f, 0.06f, 0.62f);
        weatherOverlayBg.raycastTarget = true;
        weatherForecastOverlayCg = weatherOverlayObj.GetComponent<CanvasGroup>();
        weatherForecastOverlayCg.alpha = 0f;
        weatherForecastOverlayCg.blocksRaycasts = true;
        weatherForecastOverlayCg.interactable = false;

        GameObject weatherCardObj = new GameObject("WeatherForecastCard", typeof(RectTransform), typeof(Image));
        weatherCardObj.transform.SetParent(weatherForecastOverlayRt, false);
        RectTransform weatherCardRt = weatherCardObj.GetComponent<RectTransform>();
        weatherCardRt.anchorMin = new Vector2(0.25f, 0.25f);
        weatherCardRt.anchorMax = new Vector2(0.75f, 0.75f);
        weatherCardRt.offsetMin = Vector2.zero;
        weatherCardRt.offsetMax = Vector2.zero;
        Image weatherCardBg = weatherCardObj.GetComponent<Image>();
        weatherCardBg.color = new Color(0.94f, 0.9f, 0.82f, 0.985f);

        GameObject weatherTitleObj = new GameObject("WeatherForecastTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        weatherTitleObj.transform.SetParent(weatherCardRt, false);
        RectTransform weatherTitleRt = weatherTitleObj.GetComponent<RectTransform>();
        weatherTitleRt.anchorMin = new Vector2(0f, 1f);
        weatherTitleRt.anchorMax = new Vector2(1f, 1f);
        weatherTitleRt.pivot = new Vector2(0.5f, 1f);
        weatherTitleRt.anchoredPosition = new Vector2(0f, -40f);
        weatherTitleRt.sizeDelta = new Vector2(-64f, 120f);
        weatherForecastTitleTmp = weatherTitleObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) weatherForecastTitleTmp.font = sharedUIFont;
        weatherForecastTitleTmp.fontSize = 62f;
        weatherForecastTitleTmp.alignment = TextAlignmentOptions.Center;
        weatherForecastTitleTmp.color = new Color(0.28f, 0.22f, 0.16f, 1f);
        weatherForecastTitleTmp.text = "天氣預報：無";
        weatherForecastTitleTmp.raycastTarget = false;

        GameObject weatherBodyObj = new GameObject("WeatherForecastBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        weatherBodyObj.transform.SetParent(weatherCardRt, false);
        RectTransform weatherBodyRt = weatherBodyObj.GetComponent<RectTransform>();
        weatherBodyRt.anchorMin = new Vector2(0f, 0f);
        weatherBodyRt.anchorMax = new Vector2(1f, 1f);
        weatherBodyRt.offsetMin = new Vector2(72f, 72f);
        weatherBodyRt.offsetMax = new Vector2(-72f, -192f);
        weatherForecastBodyTmp = weatherBodyObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) weatherForecastBodyTmp.font = sharedUIFont;
        weatherForecastBodyTmp.fontSize = 44f;
        weatherForecastBodyTmp.alignment = TextAlignmentOptions.Top;
        weatherForecastBodyTmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);
        weatherForecastBodyTmp.enableWordWrapping = true;
        weatherForecastBodyTmp.text = "本回合無額外天氣效果。";
        weatherForecastBodyTmp.raycastTarget = false;
        weatherOverlayObj.SetActive(false);

        activeWeatherEffectButton = CreateButton(parent, "ActiveWeatherEffectButton", "場地效果", new Vector2(0f, 0f), ToggleActiveWeatherEffectPanel, true);
        RectTransform activeWeatherBtnRt = activeWeatherEffectButton != null ? activeWeatherEffectButton.GetComponent<RectTransform>() : null;
        if (activeWeatherBtnRt != null)
        {
            // Place to the left of the Pause button (Pause at top-right).
            activeWeatherBtnRt.anchorMin = new Vector2(1f, 1f);
            activeWeatherBtnRt.anchorMax = new Vector2(1f, 1f);
            activeWeatherBtnRt.pivot = new Vector2(1f, 1f);
            activeWeatherBtnRt.anchoredPosition = new Vector2(-174f, -22f);
            activeWeatherBtnRt.sizeDelta = new Vector2(350f, 72f);
        }
        if (activeWeatherEffectButton != null)
        {
            TextMeshProUGUI btnLabel = activeWeatherEffectButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnLabel != null) btnLabel.fontSize = 34f;
        }
        if (activeWeatherEffectButton != null) activeWeatherEffectButton.gameObject.SetActive(false);

        GameObject activeWeatherPanelObj = new GameObject("ActiveWeatherEffectPanel", typeof(RectTransform), typeof(Image));
        activeWeatherPanelObj.transform.SetParent(parent, false);
        activeWeatherEffectPanelRt = activeWeatherPanelObj.GetComponent<RectTransform>();
        // Left half-screen information panel (about 50% viewport width), shifted right.
        activeWeatherEffectPanelRt.anchorMin = new Vector2(0.04f, 0.02f);
        activeWeatherEffectPanelRt.anchorMax = new Vector2(0.54f, 0.98f);
        activeWeatherEffectPanelRt.pivot = new Vector2(0f, 0.5f);
        activeWeatherEffectPanelRt.offsetMin = new Vector2(32f, 30f);
        activeWeatherEffectPanelRt.offsetMax = new Vector2(-22f, -30f);
        Image activeWeatherPanelBg = activeWeatherPanelObj.GetComponent<Image>();
        activeWeatherPanelBg.color = new Color(0.92f, 0.89f, 0.82f, 0.96f);
        Outline activeWeatherPanelOutline = activeWeatherPanelObj.AddComponent<Outline>();
        activeWeatherPanelOutline.effectColor = new Color(0.44f, 0.36f, 0.26f, 0.35f);
        activeWeatherPanelOutline.effectDistance = new Vector2(2f, -2f);
        activeWeatherPanelObj.SetActive(false);

        GameObject activeWeatherSummaryObj = new GameObject("ActiveWeatherSummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        activeWeatherSummaryObj.transform.SetParent(activeWeatherPanelObj.transform, false);
        RectTransform activeWeatherSummaryRt = activeWeatherSummaryObj.GetComponent<RectTransform>();
        activeWeatherSummaryRt.anchorMin = new Vector2(0f, 0f);
        activeWeatherSummaryRt.anchorMax = new Vector2(0.34f, 1f);
        activeWeatherSummaryRt.offsetMin = new Vector2(42f, 38f);
        activeWeatherSummaryRt.offsetMax = new Vector2(-24f, -38f);
        activeWeatherEffectPanelSummaryTmp = activeWeatherSummaryObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) activeWeatherEffectPanelSummaryTmp.font = sharedUIFont;
        activeWeatherEffectPanelSummaryTmp.fontSize = 29f;
        activeWeatherEffectPanelSummaryTmp.alignment = TextAlignmentOptions.TopLeft;
        activeWeatherEffectPanelSummaryTmp.color = new Color(0.27f, 0.23f, 0.18f, 1f);
        activeWeatherEffectPanelSummaryTmp.enableWordWrapping = true;
        activeWeatherEffectPanelSummaryTmp.lineSpacing = 10f;
        activeWeatherEffectPanelSummaryTmp.richText = true;
        activeWeatherEffectPanelSummaryTmp.text = "天氣摘要";
        activeWeatherEffectPanelSummaryTmp.raycastTarget = false;

        GameObject activeWeatherDividerObj = new GameObject("ActiveWeatherDivider", typeof(RectTransform), typeof(Image));
        activeWeatherDividerObj.transform.SetParent(activeWeatherPanelObj.transform, false);
        RectTransform activeWeatherDividerRt = activeWeatherDividerObj.GetComponent<RectTransform>();
        activeWeatherDividerRt.anchorMin = new Vector2(0.34f, 0f);
        activeWeatherDividerRt.anchorMax = new Vector2(0.34f, 1f);
        activeWeatherDividerRt.sizeDelta = new Vector2(2f, -76f);
        Image activeWeatherDividerImg = activeWeatherDividerObj.GetComponent<Image>();
        activeWeatherDividerImg.color = new Color(0.56f, 0.49f, 0.36f, 0.35f);
        activeWeatherDividerImg.raycastTarget = false;

        GameObject activeWeatherPanelTextObj = new GameObject("ActiveWeatherEffectPanelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        activeWeatherPanelTextObj.transform.SetParent(activeWeatherPanelObj.transform, false);
        RectTransform activeWeatherPanelTextRt = activeWeatherPanelTextObj.GetComponent<RectTransform>();
        activeWeatherPanelTextRt.anchorMin = new Vector2(0.34f, 0f);
        activeWeatherPanelTextRt.anchorMax = new Vector2(1f, 1f);
        activeWeatherPanelTextRt.offsetMin = new Vector2(34f, 38f);
        activeWeatherPanelTextRt.offsetMax = new Vector2(-42f, -38f);
        activeWeatherEffectPanelTextTmp = activeWeatherPanelTextObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) activeWeatherEffectPanelTextTmp.font = sharedUIFont;
        activeWeatherEffectPanelTextTmp.fontSize = 31f;
        activeWeatherEffectPanelTextTmp.alignment = TextAlignmentOptions.TopLeft;
        activeWeatherEffectPanelTextTmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);
        activeWeatherEffectPanelTextTmp.enableWordWrapping = true;
        activeWeatherEffectPanelTextTmp.lineSpacing = 10f;
        activeWeatherEffectPanelTextTmp.richText = true;
        activeWeatherEffectPanelTextTmp.text = "詳細效果";
        activeWeatherEffectPanelTextTmp.raycastTarget = false;

        CreateWeatherScreenFx(parent);
        EnsureHeroDamageMonochromeFlashOverlay(parent);

        RebuildHandButtons();
        RebuildEnemyHandCards();
        RefreshFieldCards();
        EnsureDiscardSelectionUi(parent);

        debugUiRoot.SetActive(debugPanelVisibleOnPlay);
        ApplyBattleTurnBannerStackOrder();
    }

    private void EnsureUIAudioSource(Transform parent)
    {
        if (uiAudioSource != null) return;
        GameObject audioObj = new GameObject("BattleUIAudio");
        audioObj.transform.SetParent(parent, false);
        uiAudioSource = audioObj.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.volume = 0.95f;
    }

    private void EnsureHeroDamageMonochromeFlashOverlay(Transform parent)
    {
        if (heroDamageMonochromeFlashRt != null || parent == null) return;

        GameObject overlayObj = new GameObject("HeroDamageVignetteFlash", typeof(RectTransform), typeof(CanvasGroup));
        overlayObj.transform.SetParent(parent, false);
        heroDamageMonochromeFlashRt = overlayObj.GetComponent<RectTransform>();
        heroDamageMonochromeFlashRt.anchorMin = Vector2.zero;
        heroDamageMonochromeFlashRt.anchorMax = Vector2.one;
        heroDamageMonochromeFlashRt.offsetMin = Vector2.zero;
        heroDamageMonochromeFlashRt.offsetMax = Vector2.zero;
        heroDamageMonochromeFlashRt.SetAsLastSibling();

        // Build a non-intrusive vignette by darkening only screen edges.
        CreateHeroDamageVignetteEdge(overlayObj.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 180f), new Color(0f, 0f, 0f, 0.85f));
        CreateHeroDamageVignetteEdge(overlayObj.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 180f), new Color(0f, 0f, 0f, 0.85f));
        CreateHeroDamageVignetteEdge(overlayObj.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(210f, 0f), new Color(0f, 0f, 0f, 0.78f));
        CreateHeroDamageVignetteEdge(overlayObj.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(210f, 0f), new Color(0f, 0f, 0f, 0.78f));

        heroDamageMonochromeFlashCg = overlayObj.GetComponent<CanvasGroup>();
        heroDamageMonochromeFlashCg.alpha = 0f;
        heroDamageMonochromeFlashCg.blocksRaycasts = false;
        heroDamageMonochromeFlashCg.interactable = false;
    }

    private static void CreateHeroDamageVignetteEdge(
        Transform parent,
        string edgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject edgeObj = new GameObject("HeroDamageVignette" + edgeName, typeof(RectTransform), typeof(Image));
        edgeObj.transform.SetParent(parent, false);
        RectTransform edgeRt = edgeObj.GetComponent<RectTransform>();
        edgeRt.anchorMin = anchorMin;
        edgeRt.anchorMax = anchorMax;
        edgeRt.pivot = pivot;
        edgeRt.anchoredPosition = anchoredPosition;
        edgeRt.sizeDelta = sizeDelta;
        Image edgeImg = edgeObj.GetComponent<Image>();
        edgeImg.color = color;
        edgeImg.raycastTarget = false;
    }

    private void PlayUIClip(AudioClip clip, float volume)
    {
        if (clip == null || uiAudioSource == null) return;
        uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }





    private void OnDestroy()
    {
        Time.timeScale = 1f;
        BattleAutoSimPlugin.ProgressUiParent = null;
        BattleAutoSimPlugin.Started -= OnBatchWinRateSimStarted;
        BattleAutoSimPlugin.Completed -= OnBatchWinRateSimCompleted;
        if (battleManager != null)
        {
            battleManager.EnemyCardPlayed -= OnEnemyCardPlayed;
            battleManager.EnemyCardDiscarded -= OnEnemyCardDiscarded;
            battleManager.PlayerCardDiscarded -= OnPlayerCardDiscarded;
            battleManager.AttackPerformed -= OnAttackPerformed;
            battleManager.CardDrawn -= OnCardDrawn;
            battleManager.SpellCastPresentationStarted -= OnSpellCastPresentationStarted;
            battleManager.SpellCastAsyncPresentationFinished -= OnSpellCastAsyncPresentationFinished;
            battleManager.BattleLayoutVisualRefreshRequested -= OnBattleLayoutVisualRefreshRequested;
            battleManager.PlayerLesserHealVisualRequested -= OnPlayerLesserHealVisualRequested;
            battleManager.EnemyLesserHealVisualRequested -= OnEnemyLesserHealVisualRequested;
            battleManager.SpellCastHandAnchorCommitted -= OnSpellCastHandAnchorCommitted;
            battleManager.FireballVisualRequested -= OnFireballVisualRequested;
            battleManager.LinGazePeriodicStrikeVisualRequested -= OnLinGazePeriodicStrikeVisualRequested;
            battleManager.WeatherForecastStarted -= OnWeatherForecastStarted;
            battleManager.WeatherForecastFinished -= OnWeatherForecastFinished;
            battleManager.TurnBannerRequested -= OnTurnBannerRequested;
            battleManager.PlayerCommittedHandCardToFieldFromHand -= OnPlayerCommittedHandCardToFieldFromHand;
            battleManager.PlayerPressedEndTurnForPromptUi -= OnPlayerPressedEndTurnForPromptUi;
            battleManager.PlayerTurnActionWindowOpenedForPromptUi -= OnPlayerTurnActionWindowOpenedForPromptUi;
        }
        StopYourTurnBannerHandTouchNoPlayArmDeferRoutine();
        if (lesserHealFieldFxRoutine != null)
        {
            StopCoroutine(lesserHealFieldFxRoutine);
            lesserHealFieldFxRoutine = null;
        }
        if (fireballFxRoutine != null)
        {
            StopCoroutine(fireballFxRoutine);
            fireballFxRoutine = null;
        }
        if (linGazeEyeStrikeRoutinePlayer != null)
        {
            StopCoroutine(linGazeEyeStrikeRoutinePlayer);
            linGazeEyeStrikeRoutinePlayer = null;
        }
        if (linGazeEyeStrikeRoutineEnemy != null)
        {
            StopCoroutine(linGazeEyeStrikeRoutineEnemy);
            linGazeEyeStrikeRoutineEnemy = null;
        }
        if (spellCastOverlayRoutine != null)
        {
            StopCoroutine(spellCastOverlayRoutine);
            spellCastOverlayRoutine = null;
        }
        if (settlementFreezeRoutine != null)
        {
            StopCoroutine(settlementFreezeRoutine);
            settlementFreezeRoutine = null;
        }
        if (playerOpeningHandFlyRoutine != null)
        {
            StopCoroutine(playerOpeningHandFlyRoutine);
            playerOpeningHandFlyRoutine = null;
        }
        if (enemyOpeningHandFlyRoutine != null)
        {
            StopCoroutine(enemyOpeningHandFlyRoutine);
            enemyOpeningHandFlyRoutine = null;
        }
        ReleaseSettlementFreezeResources();
    }

    private void CreatePauseUI(Transform parent)
    {
        Button pauseToggleBtn = CreateButton(parent, "PauseToggleButton", "Pause", new Vector2(0f, -240f), TogglePause, true);
        if (pauseToggleBtn != null)
        {
            pauseToggleBtn.onClick.RemoveAllListeners();
            pauseToggleBtn.onClick.AddListener(() => SetPaused(true));
        }
        RectTransform pauseToggleRt = pauseToggleBtn != null ? pauseToggleBtn.GetComponent<RectTransform>() : null;
        if (pauseToggleRt != null)
        {
            pauseToggleRt.anchorMin = new Vector2(1f, 1f);
            pauseToggleRt.anchorMax = new Vector2(1f, 1f);
            pauseToggleRt.pivot = new Vector2(1f, 1f);
            pauseToggleRt.anchoredPosition = new Vector2(-24f, -24f);
            pauseToggleRt.sizeDelta = new Vector2(120f, 48f);
        }

        GameObject pausePanelObj = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanelObj.transform.SetParent(parent, false);
        pausePanel = pausePanelObj.GetComponent<RectTransform>();
        pausePanel.anchorMin = Vector2.zero;
        pausePanel.anchorMax = Vector2.one;
        pausePanel.offsetMin = Vector2.zero;
        pausePanel.offsetMax = Vector2.zero;
        Image pauseBg = pausePanelObj.GetComponent<Image>();
        pauseBg.color = new Color(0.12f, 0.08f, 0.06f, 0.62f);

        GameObject pauseCardObj = new GameObject("PauseCard", typeof(RectTransform), typeof(Image));
        pauseCardObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform pauseCardRt = pauseCardObj.GetComponent<RectTransform>();
        pauseCardRt.anchorMin = new Vector2(0.5f, 0.5f);
        pauseCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        pauseCardRt.pivot = new Vector2(0.5f, 0.5f);
        pauseCardRt.anchoredPosition = Vector2.zero;
        pauseCardRt.sizeDelta = new Vector2(760f, 420f);
        Image pauseCardBg = pauseCardObj.GetComponent<Image>();
        pauseCardBg.color = new Color(0.92f, 0.88f, 0.8f, 0.96f);

        GameObject titleObj = new GameObject("PauseTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(pauseCardObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 110f);
        titleRt.sizeDelta = new Vector2(520f, 110f);
        TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) title.font = sharedUIFont;
        title.fontSize = 68f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.24f, 0.2f, 0.16f, 1f);
        title.text = "Paused";

        Button resumeBtn = CreateButton(pauseCardObj.transform, "ResumeButton", "Resume", new Vector2(0f, 0f), TogglePause, true);
        if (resumeBtn != null)
        {
            resumeBtn.onClick.RemoveAllListeners();
            resumeBtn.onClick.AddListener(() => SetPaused(false));
        }
        RectTransform resumeRt = resumeBtn != null ? resumeBtn.GetComponent<RectTransform>() : null;
        if (resumeRt != null)
        {
            resumeRt.anchorMin = new Vector2(0.5f, 0.5f);
            resumeRt.anchorMax = new Vector2(0.5f, 0.5f);
            resumeRt.pivot = new Vector2(0.5f, 0.5f);
            resumeRt.anchoredPosition = new Vector2(0f, -26f);
            resumeRt.sizeDelta = new Vector2(300f, 86f);
            Text resumeLabel = resumeBtn.GetComponentInChildren<Text>();
            if (resumeLabel != null) resumeLabel.fontSize = 34;
        }

        Button restartBtn = CreateButton(pauseCardObj.transform, "PauseRestartButton", "Restart", new Vector2(0f, 0f), OnPauseRestartClicked, true);
        RectTransform restartRt = restartBtn != null ? restartBtn.GetComponent<RectTransform>() : null;
        if (restartRt != null)
        {
            restartRt.anchorMin = new Vector2(0.5f, 0.5f);
            restartRt.anchorMax = new Vector2(0.5f, 0.5f);
            restartRt.pivot = new Vector2(0.5f, 0.5f);
            restartRt.anchoredPosition = new Vector2(-170f, -130f);
            restartRt.sizeDelta = new Vector2(220f, 66f);
            Text restartLabel = restartBtn.GetComponentInChildren<Text>();
            if (restartLabel != null) restartLabel.fontSize = 28;
        }

        Button giveUpBtn = CreateButton(pauseCardObj.transform, "PauseGiveUpButton", "Give up", new Vector2(0f, 0f), OnPauseGiveUpClicked, true);
        RectTransform giveUpRt = giveUpBtn != null ? giveUpBtn.GetComponent<RectTransform>() : null;
        if (giveUpRt != null)
        {
            giveUpRt.anchorMin = new Vector2(0.5f, 0.5f);
            giveUpRt.anchorMax = new Vector2(0.5f, 0.5f);
            giveUpRt.pivot = new Vector2(0.5f, 0.5f);
            giveUpRt.anchoredPosition = new Vector2(170f, -130f);
            giveUpRt.sizeDelta = new Vector2(220f, 66f);
            Text giveUpLabel = giveUpBtn.GetComponentInChildren<Text>();
            if (giveUpLabel != null) giveUpLabel.fontSize = 28;
        }

        pausePanelObj.SetActive(false);
    }

    private void CreateWeatherScreenFx(Transform parent)
    {
        GameObject fxRootObj = new GameObject("WeatherScreenFxRoot", typeof(RectTransform), typeof(CanvasGroup));
        fxRootObj.transform.SetParent(parent, false);
        weatherScreenFxRoot = fxRootObj.GetComponent<RectTransform>();
        weatherScreenFxRoot.anchorMin = Vector2.zero;
        weatherScreenFxRoot.anchorMax = Vector2.one;
        weatherScreenFxRoot.offsetMin = Vector2.zero;
        weatherScreenFxRoot.offsetMax = Vector2.zero;
        CanvasGroup fxCg = fxRootObj.GetComponent<CanvasGroup>();
        fxCg.blocksRaycasts = false;
        fxCg.interactable = false;

        weatherFireRainFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherFireRainFx", new Color(1f, 0.36f, 0.16f, 0.03f));
        weatherHolyLightFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherHolyLightFx", new Color(0.98f, 0.96f, 0.9f, 0.04f));
        weatherFogFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherFogFx", new Color(0.76f, 0.82f, 0.9f, 0.11f));
        weatherGaleFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherGaleFx", new Color(0.7f, 0.84f, 1f, 0.09f));

        if (weatherHolyLightFxRt != null)
        {
            weatherHolyLightEdgeImgs.Clear();
            weatherHolyLightEdgeBaseAlphas.Clear();
            weatherHolyLightDustImages.Clear();
            weatherHolyLightDustRects.Clear();
            weatherHolyLightDustSpeeds.Clear();
            weatherHolyLightDustPhases.Clear();
            weatherHolyLightDustBaseColors.Clear();
            weatherHolyLightTopEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightTopEdgeOuter", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 170f), 0.11f);
            weatherHolyLightBottomEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightBottomEdgeOuter", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 150f), 0.09f);
            weatherHolyLightLeftEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightLeftEdgeOuter", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(126f, 0f), 0.08f);
            weatherHolyLightRightEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightRightEdgeOuter", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(126f, 0f), 0.08f);
            AddHolyLightEdgeLayer(weatherHolyLightTopEdgeImg, 0.11f);
            AddHolyLightEdgeLayer(weatherHolyLightBottomEdgeImg, 0.09f);
            AddHolyLightEdgeLayer(weatherHolyLightLeftEdgeImg, 0.08f);
            AddHolyLightEdgeLayer(weatherHolyLightRightEdgeImg, 0.08f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightTopEdgeMid", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 114f), 0.06f), 0.06f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightBottomEdgeMid", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 98f), 0.05f), 0.05f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightLeftEdgeMid", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(88f, 0f), 0.043f), 0.043f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightRightEdgeMid", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(88f, 0f), 0.043f), 0.043f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightTopEdgeInner", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 66f), 0.02f), 0.02f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightBottomEdgeInner", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 56f), 0.016f), 0.016f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightLeftEdgeInner", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(54f, 0f), 0.015f), 0.015f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightRightEdgeInner", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(54f, 0f), 0.015f), 0.015f);

            for (int i = 0; i < 16; i++)
            {
                GameObject dustObj = new GameObject("HolyLightDust_" + i, typeof(RectTransform), typeof(Image));
                dustObj.transform.SetParent(weatherHolyLightFxRt, false);
                RectTransform dustRt = dustObj.GetComponent<RectTransform>();
                dustRt.anchorMin = new Vector2(0.5f, 0.5f);
                dustRt.anchorMax = new Vector2(0.5f, 0.5f);
                dustRt.pivot = new Vector2(0.5f, 0.5f);
                float size = Random.Range(4.5f, 10f);
                dustRt.sizeDelta = new Vector2(size, size);
                dustRt.anchoredPosition = new Vector2(Random.Range(-420f, 420f), Random.Range(-260f, 300f));
                Image dustImg = dustObj.GetComponent<Image>();
                dustImg.sprite = GetUnitWhiteSprite();
                bool useLavender = Random.value < 0.18f; // low ratio lavender accents
                Color baseColor = useLavender
                    ? new Color(0.92f, 0.9f, 0.98f, Random.Range(0.04f, 0.082f))
                    : new Color(0.96f, 0.98f, 0.9f, Random.Range(0.045f, 0.095f));
                dustImg.color = baseColor;
                dustImg.raycastTarget = false;
                weatherHolyLightDustRects.Add(dustRt);
                weatherHolyLightDustImages.Add(dustImg);
                weatherHolyLightDustSpeeds.Add(Random.Range(13f, 25f));
                weatherHolyLightDustPhases.Add(Random.Range(0f, Mathf.PI * 2f));
                weatherHolyLightDustBaseColors.Add(baseColor);
            }
        }

        if (weatherFogFxRt != null)
        {
            weatherFogBands.Clear();
            weatherFogBandImages.Clear();
            weatherFogBandSpeeds.Clear();
            weatherFogBandPhases.Clear();
            weatherFogEdgeImgs.Clear();
            weatherFogEdgeBaseAlphas.Clear();
            weatherFogFoamDots.Clear();
            weatherFogFoamDotImages.Clear();
            weatherFogFoamDotSpeeds.Clear();
            weatherFogBoatRt = null;
            weatherFogBoatHullImg = null;
            weatherFogBoatBaseY = -120f;

            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiTopOuter", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 150f), 0.1f), 0.1f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiBottomOuter", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 220f), 0.18f), 0.18f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiLeftOuter", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(124f, 0f), 0.11f), 0.11f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiRightOuter", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(124f, 0f), 0.11f), 0.11f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiBottomInner", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 128f), 0.11f), 0.11f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiSideInnerL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(84f, 0f), 0.075f), 0.075f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "TsunamiSideInnerR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(84f, 0f), 0.075f), 0.075f);

            for (int i = 0; i < 7; i++)
            {
                GameObject fogBandObj = new GameObject("TsunamiWaveBand_" + i, typeof(RectTransform), typeof(Image));
                fogBandObj.transform.SetParent(weatherFogFxRt, false);
                RectTransform fogBandRt = fogBandObj.GetComponent<RectTransform>();
                fogBandRt.anchorMin = new Vector2(0.5f, 0.5f);
                fogBandRt.anchorMax = new Vector2(0.5f, 0.5f);
                fogBandRt.pivot = new Vector2(0.5f, 0.5f);
                fogBandRt.sizeDelta = new Vector2(Random.Range(560f, 980f), Random.Range(70f, 140f));
                fogBandRt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(-300f, 300f));
                Image fogBandImg = fogBandObj.GetComponent<Image>();
                fogBandImg.sprite = GetUnitWhiteSprite();
                fogBandImg.color = new Color(0.38f, 0.62f, 0.8f, Random.Range(0.08f, 0.14f));
                fogBandImg.raycastTarget = false;
                weatherFogBands.Add(fogBandRt);
                weatherFogBandImages.Add(fogBandImg);
                weatherFogBandSpeeds.Add(Random.Range(30f, 56f));
                weatherFogBandPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }

            for (int i = 0; i < 18; i++)
            {
                GameObject foamDotObj = new GameObject("TsunamiFoamDot_" + i, typeof(RectTransform), typeof(Image));
                foamDotObj.transform.SetParent(weatherFogFxRt, false);
                RectTransform foamRt = foamDotObj.GetComponent<RectTransform>();
                foamRt.anchorMin = new Vector2(0.5f, 0.5f);
                foamRt.anchorMax = new Vector2(0.5f, 0.5f);
                foamRt.pivot = new Vector2(0.5f, 0.5f);
                float size = Random.Range(3.5f, 8f);
                foamRt.sizeDelta = new Vector2(size, size);
                foamRt.anchoredPosition = new Vector2(Random.Range(-560f, 560f), Random.Range(-240f, 240f));
                Image foamImg = foamDotObj.GetComponent<Image>();
                foamImg.sprite = GetUnitWhiteSprite();
                foamImg.color = new Color(0.93f, 0.98f, 1f, Random.Range(0.08f, 0.16f));
                foamImg.raycastTarget = false;
                weatherFogFoamDots.Add(foamRt);
                weatherFogFoamDotImages.Add(foamImg);
                weatherFogFoamDotSpeeds.Add(Random.Range(36f, 78f));
            }

            GameObject boatObj = new GameObject("TsunamiBoatSilhouette", typeof(RectTransform));
            boatObj.transform.SetParent(weatherFogFxRt, false);
            weatherFogBoatRt = boatObj.GetComponent<RectTransform>();
            weatherFogBoatRt.anchorMin = new Vector2(0.5f, 0.5f);
            weatherFogBoatRt.anchorMax = new Vector2(0.5f, 0.5f);
            weatherFogBoatRt.pivot = new Vector2(0.5f, 0.5f);
            weatherFogBoatRt.sizeDelta = new Vector2(96f, 72f);
            weatherFogBoatRt.anchoredPosition = new Vector2(340f, -120f);
            weatherFogBoatBaseY = -120f;

            GameObject hullObj = new GameObject("Hull", typeof(RectTransform), typeof(Image));
            hullObj.transform.SetParent(boatObj.transform, false);
            RectTransform hullRt = hullObj.GetComponent<RectTransform>();
            hullRt.anchorMin = new Vector2(0.5f, 0.5f);
            hullRt.anchorMax = new Vector2(0.5f, 0.5f);
            hullRt.pivot = new Vector2(0.5f, 0.5f);
            hullRt.sizeDelta = new Vector2(82f, 16f);
            hullRt.anchoredPosition = new Vector2(0f, -20f);
            weatherFogBoatHullImg = hullObj.GetComponent<Image>();
            weatherFogBoatHullImg.sprite = GetUnitWhiteSprite();
            weatherFogBoatHullImg.color = new Color(0.05f, 0.09f, 0.14f, 0.3f);
            weatherFogBoatHullImg.raycastTarget = false;

            GameObject mastObj = new GameObject("Mast", typeof(RectTransform), typeof(Image));
            mastObj.transform.SetParent(boatObj.transform, false);
            RectTransform mastRt = mastObj.GetComponent<RectTransform>();
            mastRt.anchorMin = new Vector2(0.5f, 0.5f);
            mastRt.anchorMax = new Vector2(0.5f, 0.5f);
            mastRt.pivot = new Vector2(0.5f, 0f);
            mastRt.sizeDelta = new Vector2(3f, 34f);
            mastRt.anchoredPosition = new Vector2(-6f, -16f);
            Image mastImg = mastObj.GetComponent<Image>();
            mastImg.sprite = GetUnitWhiteSprite();
            mastImg.color = new Color(0.05f, 0.09f, 0.14f, 0.32f);
            mastImg.raycastTarget = false;

            GameObject sailObj = new GameObject("Sail", typeof(RectTransform), typeof(Image));
            sailObj.transform.SetParent(boatObj.transform, false);
            RectTransform sailRt = sailObj.GetComponent<RectTransform>();
            sailRt.anchorMin = new Vector2(0.5f, 0.5f);
            sailRt.anchorMax = new Vector2(0.5f, 0.5f);
            sailRt.pivot = new Vector2(0f, 0.5f);
            sailRt.sizeDelta = new Vector2(26f, 22f);
            sailRt.anchoredPosition = new Vector2(-4f, -2f);
            Image sailImg = sailObj.GetComponent<Image>();
            sailImg.sprite = GetUnitWhiteSprite();
            sailImg.color = new Color(0.07f, 0.11f, 0.17f, 0.22f);
            sailImg.raycastTarget = false;
        }

        if (weatherGaleFxRt != null)
        {
            weatherGaleNightEdgeImgs.Clear();
            weatherGaleNightEdgeBaseAlphas.Clear();
            weatherGaleLeafRects.Clear();
            weatherGaleLeafImgs.Clear();
            weatherGaleLeafSpeeds.Clear();
            weatherGaleLeafPhases.Clear();
            weatherGaleWindLineRects.Clear();
            weatherGaleWindLineImgs.Clear();
            weatherGaleWindLineSpeeds.Clear();

            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f), 0.16f), 0.16f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 160f), 0.12f), 0.12f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(180f, 0f), 0.15f), 0.15f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(180f, 0f), 0.15f), 0.15f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightTopMid", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 140f), 0.1f), 0.1f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightBottomMid", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 110f), 0.08f), 0.08f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightVignetteL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(120f, 0f), 0.11f), 0.11f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightVignetteR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(120f, 0f), 0.11f), 0.11f);

            for (int i = 0; i < 16; i++)
            {
                GameObject leafObj = new GameObject("GaleLeaf_" + i, typeof(RectTransform), typeof(Image));
                leafObj.transform.SetParent(weatherGaleFxRt, false);
                RectTransform leafRt = leafObj.GetComponent<RectTransform>();
                leafRt.anchorMin = new Vector2(0.5f, 0.5f);
                leafRt.anchorMax = new Vector2(0.5f, 0.5f);
                leafRt.pivot = new Vector2(0.5f, 0.5f);
                float s = Random.Range(8f, 16f);
                leafRt.sizeDelta = new Vector2(s * 1.4f, s * 0.7f);
                leafRt.anchoredPosition = new Vector2(Random.Range(-420f, 760f), Random.Range(-240f, 300f));
                Image leafImg = leafObj.GetComponent<Image>();
                leafImg.sprite = GetUnitWhiteSprite();
                Color leafColor;
                float roll = Random.value;
                if (roll < 0.45f) leafColor = new Color(0.42f, 0.58f, 0.32f, Random.Range(0.22f, 0.42f)); // green
                else if (roll < 0.72f) leafColor = new Color(0.63f, 0.48f, 0.24f, Random.Range(0.2f, 0.38f)); // brown
                else if (roll < 0.9f) leafColor = new Color(0.74f, 0.38f, 0.2f, Random.Range(0.2f, 0.36f)); // orange
                else leafColor = new Color(0.56f, 0.2f, 0.2f, Random.Range(0.18f, 0.34f)); // red
                leafImg.color = leafColor;
                leafImg.raycastTarget = false;
                weatherGaleLeafRects.Add(leafRt);
                weatherGaleLeafImgs.Add(leafImg);
                weatherGaleLeafSpeeds.Add(Random.Range(90f, 180f));
                weatherGaleLeafPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }

            for (int i = 0; i < 11; i++)
            {
                GameObject windObj = new GameObject("GaleWindLine_" + i, typeof(RectTransform), typeof(Image));
                windObj.transform.SetParent(weatherGaleFxRt, false);
                RectTransform windRt = windObj.GetComponent<RectTransform>();
                windRt.anchorMin = new Vector2(0.5f, 0.5f);
                windRt.anchorMax = new Vector2(0.5f, 0.5f);
                windRt.pivot = new Vector2(0.5f, 0.5f);
                windRt.sizeDelta = new Vector2(Random.Range(90f, 170f), Random.Range(2.4f, 4.2f));
                windRt.anchoredPosition = new Vector2(Random.Range(-520f, 760f), Random.Range(-260f, 280f));
                windRt.rotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 6f));
                Image windImg = windObj.GetComponent<Image>();
                windImg.sprite = GetUnitWhiteSprite();
                windImg.color = new Color(0.75f, 0.86f, 0.92f, Random.Range(0.08f, 0.16f));
                windImg.raycastTarget = false;
                weatherGaleWindLineRects.Add(windRt);
                weatherGaleWindLineImgs.Add(windImg);
                weatherGaleWindLineSpeeds.Add(Random.Range(130f, 240f));
            }
        }

        if (weatherFireRainFxRt != null)
        {
            weatherFireRainStreaks.Clear();
            weatherFireRainStreakSpeeds.Clear();
            weatherFireRainStreakImages.Clear();
            weatherFireRainStreakPhases.Clear();
            for (int i = 0; i < 26; i++)
            {
                GameObject dropObj = new GameObject("FireRainDrop_" + i, typeof(RectTransform), typeof(Image));
                dropObj.transform.SetParent(weatherFireRainFxRt, false);
                RectTransform dropRt = dropObj.GetComponent<RectTransform>();
                dropRt.anchorMin = new Vector2(0.5f, 0.5f);
                dropRt.anchorMax = new Vector2(0.5f, 0.5f);
                dropRt.pivot = new Vector2(0.5f, 0.5f);
                dropRt.sizeDelta = new Vector2(Random.Range(2.2f, 4.2f), Random.Range(42f, 86f));
                dropRt.rotation = Quaternion.Euler(0f, 0f, 22f);
                dropRt.anchoredPosition = new Vector2(Random.Range(-960f, 960f), Random.Range(-560f, 560f));
                Image dropImg = dropObj.GetComponent<Image>();
                dropImg.sprite = GetUnitWhiteSprite();
                dropImg.color = new Color(1f, 0.56f, 0.26f, Random.Range(0.17f, 0.32f));
                dropImg.raycastTarget = false;
                weatherFireRainStreaks.Add(dropRt);
                weatherFireRainStreakSpeeds.Add(Random.Range(480f, 760f));
                weatherFireRainStreakImages.Add(dropImg);
                weatherFireRainStreakPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }
        }

        if (weatherFireRainFxRt != null) weatherFireRainFxRt.gameObject.SetActive(false);
        if (weatherHolyLightFxRt != null) weatherHolyLightFxRt.gameObject.SetActive(false);
        if (weatherFogFxRt != null) weatherFogFxRt.gameObject.SetActive(false);
        if (weatherGaleFxRt != null) weatherGaleFxRt.gameObject.SetActive(false);
    }

    private RectTransform CreateWeatherFxLayer(Transform parent, string name, Color tint)
    {
        GameObject layerObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        layerObj.transform.SetParent(parent, false);
        RectTransform layerRt = layerObj.GetComponent<RectTransform>();
        layerRt.anchorMin = Vector2.zero;
        layerRt.anchorMax = Vector2.one;
        layerRt.offsetMin = Vector2.zero;
        layerRt.offsetMax = Vector2.zero;
        Image layerImg = layerObj.GetComponent<Image>();
        layerImg.sprite = GetUnitWhiteSprite();
        layerImg.color = tint;
        layerImg.raycastTarget = false;
        return layerRt;
    }

    private Image CreateHolyLightEdge(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        float alpha)
    {
        GameObject edgeObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        edgeObj.transform.SetParent(parent, false);
        RectTransform edgeRt = edgeObj.GetComponent<RectTransform>();
        edgeRt.anchorMin = anchorMin;
        edgeRt.anchorMax = anchorMax;
        edgeRt.pivot = pivot;
        edgeRt.anchoredPosition = anchoredPos;
        edgeRt.sizeDelta = sizeDelta;
        Image edgeImg = edgeObj.GetComponent<Image>();
        edgeImg.sprite = GetUnitWhiteSprite();
        edgeImg.color = new Color(1f, 0.98f, 0.84f, alpha);
        edgeImg.raycastTarget = false;
        return edgeImg;
    }

    private void AddHolyLightEdgeLayer(Image img, float baseAlpha)
    {
        if (img == null) return;
        weatherHolyLightEdgeImgs.Add(img);
        weatherHolyLightEdgeBaseAlphas.Add(baseAlpha);
    }

    private void AddFogEdgeLayer(Image img, float baseAlpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.r = 0.42f;
        c.g = 0.63f;
        c.b = 0.78f;
        c.a = baseAlpha;
        img.color = c;
        weatherFogEdgeImgs.Add(img);
        weatherFogEdgeBaseAlphas.Add(baseAlpha);
    }

    private void AddGaleNightLayer(Image img, float baseAlpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.r = 0.08f;
        c.g = 0.14f;
        c.b = 0.11f;
        c.a = baseAlpha;
        img.color = c;
        weatherGaleNightEdgeImgs.Add(img);
        weatherGaleNightEdgeBaseAlphas.Add(baseAlpha);
    }


    private void TogglePause()
    {
        SetPaused(!isGamePaused);
    }

    private void SetPaused(bool paused)
    {
        isGamePaused = paused;
        Time.timeScale = isGamePaused ? 0f : 1f;
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(isGamePaused);
            if (isGamePaused) pausePanel.SetAsLastSibling();
        }
    }

    private void OnPauseRestartClicked()
    {
        RecordQuitIfBattleUnfinished();
        SetPaused(false);
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void OnPauseGiveUpClicked()
    {
        RecordQuitIfBattleUnfinished();
        SetPaused(false);
        StartCoroutine(GiveUpAfterLoseMessage());
    }

    private void RecordQuitIfBattleUnfinished()
    {
        if (battleManager == null) return;
        if (battleManager.GetBattleResult() != 0) return;
        PlayerProfileCsvService.RecordPlayerQuit();
    }

    private IEnumerator GiveUpAfterLoseMessage()
    {
        lockBattleResultAutoUpdate = true;
        if (battleResultFadeRoutine != null) StopCoroutine(battleResultFadeRoutine);
        if (battleResultText != null)
        {
            battleResultText.text = "Defeat";
            battleResultText.fontSize = battleResultTextUsesDebugPanelLayout
                ? Mathf.RoundToInt(22 * DebugUiChromeMul)
                : 56;
            battleResultText.color = new Color(1f, 0.4f, 0.4f, 1f);
        }
        if (battleResultGroup != null) battleResultGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(0.9f);
        OnClickReturnBuildbeck();
    }

    private void OnEnemyCardPlayed(Card card)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (isEnemyPlayingCardAnimation) return;
        StartCoroutine(PlayEnemyCardAnimation(card));
    }

    private void OnCardDrawn(bool isPlayer, Card card)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (uiRoot == null || card == null) return;
        // 開局 7 張改由雙方手牌區整排飛入動畫呈現，避免與牌庫幽靈重疊。
        if (battleManager != null && battleManager.IsOpeningPresentationInProgress())
            return;
        StartCoroutine(PlayDrawAnimationOnRight(isPlayer, card));
    }

    private IEnumerator PlayDrawAnimationOnRight(bool isPlayer, Card card)
    {
        if (BattleAutoSimPlugin.IsRunning) yield break;
        RectTransform parentRt = uiRoot;
        if (parentRt == null) yield break;
        RectTransform sourcePile = isPlayer ? playerDeckPileRt : enemyDeckPileRt;
        RectTransform targetHand = isPlayer ? handArea : enemyHandArea;
        if (sourcePile == null || targetHand == null) yield break;

        GameObject ghost = new GameObject(isPlayer ? "PlayerDrawGhostCard" : "EnemyDrawGhostCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        ghost.transform.SetParent(parentRt, false);
        ghost.transform.SetAsLastSibling();

        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = GetBattleCardFxDisplayedSize(0.62f);

        Image img = ghost.GetComponent<Image>();
        img.color = new Color(0.16f, 0.22f, 0.45f, 1f); // facedown card back

        GameObject frontObj = Instantiate(battleCardPrefab, ghost.transform);
        frontObj.name = "DrawFrontFace";
        RectTransform frontRt = frontObj.GetComponent<RectTransform>();
        if (frontRt == null) frontRt = frontObj.AddComponent<RectTransform>();
        frontRt.anchorMin = Vector2.zero;
        frontRt.anchorMax = Vector2.one;
        frontRt.offsetMin = Vector2.zero;
        frontRt.offsetMax = Vector2.zero;
        frontRt.localScale = Vector3.one;
        CardDisplay frontDisplay = frontObj.GetComponentInChildren<CardDisplay>();
        if (frontDisplay != null)
        {
            frontDisplay.SetCard(card);
            ApplyPrefabVisualTuning(frontDisplay);
        }
        Button fb = frontObj.GetComponent<Button>();
        if (fb != null) fb.interactable = false;
        frontObj.SetActive(false);

        Vector2 start = GetCenterInUiRoot(sourcePile);
        Vector2 target = GetCenterInUiRoot(targetHand) + (isPlayer ? new Vector2(0f, 52f) : new Vector2(0f, -52f));
        rt.anchoredPosition = start;
        rt.localScale = Vector3.one * 0.9f;

        CanvasGroup cg = ghost.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        float t = 0f;
        const float dur = 0.42f;
        bool revealed = false;
        while (t < dur && ghost != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float eased = p * p * (3f - 2f * p);
            rt.anchoredPosition = Vector2.Lerp(start, target, eased);
            float moveScale = Mathf.Lerp(0.9f, 1f, eased);
            float flipScale = 1f;
            if (p >= 0.46f && p < 0.5f)
            {
                float fp = Mathf.InverseLerp(0.46f, 0.5f, p);
                flipScale = Mathf.Lerp(1f, 0.06f, fp);
            }
            else if (p >= 0.5f && p <= 0.56f)
            {
                if (!revealed)
                {
                    revealed = true;
                    img.enabled = false;
                    frontObj.SetActive(true);
                }
                float fp = Mathf.InverseLerp(0.5f, 0.56f, p);
                flipScale = Mathf.Lerp(0.06f, 1f, fp);
            }
            rt.localScale = new Vector3(moveScale * flipScale, moveScale, 1f);
            cg.alpha = Mathf.Lerp(0f, 0.95f, eased);
            yield return null;
        }

        t = 0f;
        const float fadeDur = 0.18f;
        while (t < fadeDur && ghost != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDur);
            cg.alpha = Mathf.Lerp(0.95f, 0f, p);
            yield return null;
        }

        if (ghost != null) Destroy(ghost);
    }

    private void CreateDeckPileVisual(Transform parent, bool isPlayerPile)
    {
        GameObject root = new GameObject(isPlayerPile ? "PlayerDeckPile" : "EnemyDeckPile", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0.5f);
        rootRt.anchorMax = new Vector2(1f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = GetBattleCardFxDisplayedSize(0.62f);
        rootRt.anchoredPosition = isPlayerPile ? new Vector2(-78f, -210f) : new Vector2(-78f, 210f);

        for (int i = 0; i < 3; i++)
        {
            GameObject layer = new GameObject("PileLayer_" + i, typeof(RectTransform), typeof(Image));
            layer.transform.SetParent(root.transform, false);
            RectTransform lrt = layer.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = rootRt.sizeDelta;
            lrt.anchoredPosition = new Vector2(-i * 3f, i * 3f);
            Image li = layer.GetComponent<Image>();
            li.color = i == 2 ? new Color(0.16f, 0.22f, 0.45f, 1f) : new Color(0.08f, 0.1f, 0.2f, 0.92f);
        }

        if (isPlayerPile) playerDeckPileRt = rootRt;
        else enemyDeckPileRt = rootRt;
    }

    private Vector2 GetCenterInUiRoot(RectTransform source)
    {
        if (source == null || uiRoot == null) return Vector2.zero;
        Vector3 world = source.position;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, RectTransformUtility.WorldToScreenPoint(null, world), null, out local);
        return local;
    }

    private void OnSpellCastHandAnchorCommitted(bool isPlayer, SpellCard spell, int handIndex)
    {
        fireballHandAnchorValid = false;
        if (spell == null || spell.SpellOrdinal != 0) return;
        RectTransform area = isPlayer ? handArea : enemyHandArea;
        if (area == null || uiRoot == null) return;
        if (handIndex < 0 || handIndex >= area.childCount) return;
        RectTransform slot = area.GetChild(handIndex) as RectTransform;
        if (slot == null) return;
        fireballHandAnchorLocal = GetCenterInUiRoot(slot);
        fireballHandAnchorValid = true;
    }

    private void OnFireballVisualRequested(bool casterIsPlayer, bool aimedAtFieldMonster)
    {
        if (BattleAutoSimPlugin.IsRunning)
        {
            fireballHandAnchorValid = false;
            return;
        }
        if (fireballFxRoutine != null) StopCoroutine(fireballFxRoutine);
        fireballFxRoutine = StartCoroutine(PlayFireballProjectileRoutine(casterIsPlayer, aimedAtFieldMonster));
    }

    private Vector2 GetFallbackFireballStartLocal(bool casterIsPlayer)
    {
        RectTransform area = casterIsPlayer ? handArea : enemyHandArea;
        if (area == null || uiRoot == null) return Vector2.zero;
        return GetCenterInUiRoot(area);
    }

    private Vector2 GetHeroHpCenterLocal(bool useEnemyHero)
    {
        TextMeshProUGUI tmp = useEnemyHero ? enemyHeroHpText : playerHeroHpText;
        if (tmp == null || uiRoot == null) return Vector2.zero;
        return GetCenterInUiRoot(tmp.rectTransform);
    }

    private IEnumerator PlayFireballProjectileRoutine(bool casterIsPlayer, bool aimedAtFieldMonster)
    {
        const float travel = 0.38f;
        try
        {
            yield return null;
            if (uiRoot == null) yield break;

            Vector2 startLocal = fireballHandAnchorValid
                ? fireballHandAnchorLocal
                : GetFallbackFireballStartLocal(casterIsPlayer);
            fireballHandAnchorValid = false;

            bool toEnemySide = casterIsPlayer;
            GameObject fieldCardObj = toEnemySide ? enemyFieldCardObj : playerFieldCardObj;
            RectTransform fieldZone = toEnemySide ? enemyFieldArea : playerFieldArea;

            Vector2 endLocal;
            if (aimedAtFieldMonster && fieldCardObj != null)
            {
                RectTransform tr = fieldCardObj.GetComponent<RectTransform>();
                endLocal = GetCenterInUiRoot(tr);
            }
            else if (aimedAtFieldMonster && fieldZone != null)
                endLocal = GetCenterInUiRoot(fieldZone);
            else
                endLocal = GetHeroHpCenterLocal(toEnemySide);

            GameObject proj = new GameObject("FireballProjectile", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            proj.transform.SetParent(uiRoot, false);
            proj.transform.SetAsLastSibling();
            RectTransform prt = proj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(44f, 44f);
            prt.anchoredPosition = startLocal;
            prt.localScale = Vector3.one * 0.78f;
            prt.localRotation = Quaternion.identity;

            Image img = proj.GetComponent<Image>();
            img.sprite = GetUnitWhiteSprite();
            img.raycastTarget = false;
            img.color = new Color(1f, 0.32f, 0.04f, 1f);
            CanvasGroup pcg = proj.GetComponent<CanvasGroup>();
            pcg.alpha = 1f;

            GameObject glow = new GameObject("FireballGlow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(proj.transform, false);
            glow.transform.SetAsFirstSibling();
            RectTransform grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(62f, 62f);
            grt.anchoredPosition = Vector2.zero;
            Image gi = glow.GetComponent<Image>();
            gi.sprite = GetUnitWhiteSprite();
            gi.color = new Color(1f, 0.55f, 0.12f, 0.5f);
            gi.raycastTarget = false;

            float t = 0f;
            while (t < travel && proj != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / travel);
                float eased = p * p * (2f - p);
                prt.anchoredPosition = Vector2.Lerp(startLocal, endLocal, eased);
                prt.localRotation = Quaternion.Euler(0f, 0f, t * 620f);
                float sc = Mathf.Lerp(0.82f, 1.12f, Mathf.Sin(p * Mathf.PI));
                prt.localScale = Vector3.one * sc;
                yield return null;
            }

            if (proj != null)
                Destroy(proj);

            float hitDur = battleManager != null ? Mathf.Max(0.1f, battleManager.hitShakeDuration * 0.82f) : 0.24f;
            if (aimedAtFieldMonster && fieldCardObj != null)
                yield return StartCoroutine(PlayDamageFlash(fieldCardObj, hitDur * 0.95f));
            else if (!aimedAtFieldMonster)
            {
                GameObject heroGo = toEnemySide
                    ? (enemyHeroHpText != null ? enemyHeroHpText.gameObject : null)
                    : (playerHeroHpText != null ? playerHeroHpText.gameObject : null);
                if (heroGo != null)
                    yield return StartCoroutine(PlayDamageFlash(heroGo, 0.2f));
                else
                    yield return StartCoroutine(PlayFireballImpactBurstAt(endLocal, hitDur * 0.55f));
            }
            else
                yield return StartCoroutine(PlayFireballImpactBurstAt(endLocal, hitDur * 0.72f));
        }
        finally
        {
            bool clearedHold = false;
            if (casterIsPlayer && holdEnemyFieldCardUntilFireballHit)
            {
                holdEnemyFieldCardUntilFireballHit = false;
                clearedHold = true;
            }
            if (!casterIsPlayer && holdPlayerFieldCardUntilFireballHit)
            {
                holdPlayerFieldCardUntilFireballHit = false;
                clearedHold = true;
            }
            if (clearedHold && uiRoot != null && handArea != null && battleManager != null)
            {
                RefreshFieldCards();
                lastFieldSignature = ComputeFieldSignature();
            }
            fireballFxRoutine = null;
            fireballHandAnchorValid = false;
        }
    }

    private IEnumerator PlayFireballImpactBurstAt(Vector2 localInUiRoot, float duration)
    {
        if (uiRoot == null) yield break;
        GameObject burst = new GameObject("FireballImpactBurst", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        burst.transform.SetParent(uiRoot, false);
        burst.transform.SetAsLastSibling();
        RectTransform brt = burst.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(72f, 72f);
        brt.anchoredPosition = localInUiRoot;
        Image bi = burst.GetComponent<Image>();
        bi.sprite = GetUnitWhiteSprite();
        bi.color = new Color(1f, 0.65f, 0.2f, 0.85f);
        bi.raycastTarget = false;
        CanvasGroup bcg = burst.GetComponent<CanvasGroup>();
        bcg.alpha = 1f;
        float t = 0f;
        while (t < duration && burst != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            bcg.alpha = Mathf.Lerp(1f, 0f, p);
            brt.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.45f, p);
            yield return null;
        }
        if (burst != null) Destroy(burst);
    }

    private void OnAttackPerformed(BattleSimulationManager.AttackVisualData attackData)
    {
        if (!BattleAutoSimPlugin.IsRunning && attackData.attackerIsPlayer)
            DisarmYourTurnBannerTurnStartAndHandTouchClocksOnly();

        if (BattleAutoSimPlugin.IsRunning) return;
        if (!attackData.hasMonsterTarget)
        {
            // No field target: skip swing animation to avoid "air attack" visuals.
            return;
        }
        deferFieldRefreshDuringAttack = true;
        pendingFieldRefreshAfterAttack = true;
        if (attackFxRoutine != null) StopCoroutine(attackFxRoutine);
        attackFxRoutine = StartCoroutine(PlayAttackFx(attackData));
    }

    private IEnumerator PlayAttackFx(BattleSimulationManager.AttackVisualData attackData)
    {
        bool attackerIsPlayer = attackData.attackerIsPlayer;
        bool hasMonsterTarget = attackData.hasMonsterTarget;
        GameObject attackerObj = attackerIsPlayer ? playerFieldCardObj : enemyFieldCardObj;
        try
        {
        if (attackerObj == null) yield break;

        RectTransform attackerRt = attackerObj.GetComponent<RectTransform>();
        if (attackerRt == null) yield break;

        float dur = battleManager != null ? Mathf.Max(0.12f, battleManager.attackMotionDuration * 0.82f) : 0.32f;
        float hitFxDur = battleManager != null ? Mathf.Max(0.1f, battleManager.hitShakeDuration) : 0.28f;
        float counterGap = battleManager != null ? Mathf.Max(0f, battleManager.counterAttackGapDuration) : 0.45f;
        Vector2 start = attackerRt.anchoredPosition;
        Vector2 hitOffset = attackerIsPlayer ? new Vector2(74f, 0f) : new Vector2(-74f, 0f);
        Vector3 baseScale = attackerRt.localScale;

        float t = 0f;
        float half = dur * 0.5f;
        while (t < half && attackerRt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            float eased = 1f - (1f - p) * (1f - p);
            attackerRt.anchoredPosition = Vector2.Lerp(start, start + hitOffset, eased);
            attackerRt.localScale = Vector3.Lerp(baseScale, baseScale * 1.06f, eased);
            yield return null;
        }

        if (hasMonsterTarget)
        {
            GameObject targetObj = attackerIsPlayer ? enemyFieldCardObj : playerFieldCardObj;
            if (targetObj != null)
            {
                // First show defender getting hit, then add a pause before counter-hit feedback.
                yield return StartCoroutine(PlayDamageFlash(targetObj, hitFxDur));
                ApplyPreviewDamageToFieldCard(targetObj, attackData.attackerDamage);
                RefreshFieldCards();
                lastFieldSignature = ComputeFieldSignature();
                if (counterGap > 0f) yield return new WaitForSecondsRealtime(counterGap);
                if (attackerObj != null && attackData.counterTriggered)
                {
                    yield return StartCoroutine(PlayDamageFlash(attackerObj, hitFxDur * 0.9f));
                    ApplyPreviewDamageToFieldCard(attackerObj, attackData.counterDamage);
                    RefreshFieldCards();
                    lastFieldSignature = ComputeFieldSignature();
                }
            }
        }

        t = 0f;
        Vector2 from = attackerRt != null ? attackerRt.anchoredPosition : start + hitOffset;
        float returnDur = half * 0.78f;
        while (t < returnDur && attackerRt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / returnDur);
            float eased = p * p * (3f - 2f * p);
            attackerRt.anchoredPosition = Vector2.Lerp(from, start, eased);
            attackerRt.localScale = Vector3.Lerp(baseScale * 1.06f, baseScale, eased);
            yield return null;
        }
        if (attackerRt != null)
        {
            // Small overshoot then settle for a clear "bounce back".
            Vector2 overshoot = start - hitOffset.normalized * 8f;
            t = 0f;
            const float bounceDur = 0.08f;
            while (t < bounceDur && attackerRt != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / bounceDur);
                float eased = p * p * (3f - 2f * p);
                attackerRt.anchoredPosition = Vector2.Lerp(start, overshoot, eased);
                attackerRt.localScale = Vector3.Lerp(baseScale, baseScale * 0.98f, eased);
                yield return null;
            }

            t = 0f;
            while (t < bounceDur && attackerRt != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / bounceDur);
                float eased = 1f - Mathf.Pow(1f - p, 2f);
                attackerRt.anchoredPosition = Vector2.Lerp(overshoot, start, eased);
                attackerRt.localScale = Vector3.Lerp(baseScale * 0.98f, baseScale, eased);
                yield return null;
            }
        }
        if (attackerRt != null)
        {
            attackerRt.anchoredPosition = start;
            attackerRt.localScale = baseScale;
        }
        }
        finally
        {
            deferFieldRefreshDuringAttack = false;
            if (pendingFieldRefreshAfterAttack)
            {
                RefreshFieldCards();
                lastFieldSignature = ComputeFieldSignature();
                pendingFieldRefreshAfterAttack = false;
            }
            attackFxRoutine = null;
        }
    }

    private void ApplyPreviewDamageToFieldCard(GameObject cardObj, int damage)
    {
        if (cardObj == null || damage <= 0) return;
        CardDisplay display = cardObj.GetComponentInChildren<CardDisplay>();
        if (display == null || display.healthText == null) return;

        int currentHp;
        if (display.card is MonsterCard mc)
            currentHp = mc.healthPoint;
        else if (!int.TryParse(display.healthText.text, out currentHp))
            return;
        int nextHp = Mathf.Max(0, currentHp - damage);
        display.healthText.richText = false;
        display.healthText.text = nextHp.ToString();
        if (nextHp < currentHp)
        {
            display.healthText.color = new Color(1f, 0.28f, 0.28f, 1f);
        }
    }

    private IEnumerator PlayHitShake(RectTransform targetRt, float duration, float strength)
    {
        if (targetRt == null) yield break;
        Vector2 origin = targetRt.anchoredPosition;
        float t = 0f;
        while (t < duration && targetRt != null)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(t / duration);
            float x = Mathf.Sin(t * 90f) * strength * damper;
            targetRt.anchoredPosition = origin + new Vector2(x, 0f);
            yield return null;
        }
        if (targetRt != null) targetRt.anchoredPosition = origin;
    }

    private IEnumerator PlayDamageFlash(GameObject targetObj, float duration)
    {
        if (targetObj == null) yield break;
        RectTransform targetRt = targetObj.GetComponent<RectTransform>();
        if (targetRt == null) yield break;

        Transform old = targetObj.transform.Find("DamageFlashOverlay");
        if (old != null) Destroy(old.gameObject);

        GameObject overlayObj = new GameObject("DamageFlashOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlayObj.transform.SetParent(targetObj.transform, false);
        overlayObj.transform.SetAsLastSibling();

        RectTransform overlayRt = overlayObj.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayImg = overlayObj.GetComponent<Image>();
        overlayImg.color = new Color(1f, 0.15f, 0.15f, 0.9f);
        CanvasGroup cg = overlayObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        float half = Mathf.Max(0.05f, duration * 0.45f);
        float fade = Mathf.Max(0.05f, duration * 0.55f);
        float t = 0f;

        while (t < half && overlayObj != null)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / half) * 0.75f;
            yield return null;
        }

        t = 0f;
        float startAlpha = cg.alpha;
        while (t < fade && overlayObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fade);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, p);
            yield return null;
        }

        if (overlayObj != null) Destroy(overlayObj);
    }

    private IEnumerator PlayEnemyCardAnimation(Card card)
    {
        if (BattleAutoSimPlugin.IsRunning) yield break;
        if (battleCardPrefab == null || enemyHandArea == null) yield break;
        isEnemyPlayingCardAnimation = true;
        deferEnemyFieldRefresh = card is MonsterCard;

        GameObject ghost = Instantiate(battleCardPrefab, enemyHandArea.parent);
        ghost.name = "EnemyPlayGhostCard";
        RectTransform rect = ghost.GetComponent<RectTransform>();
        if (rect == null) rect = ghost.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = GetBattleCardFxDisplayedSize(0.75f);
        Vector2 start = enemyHandArea.anchoredPosition + new Vector2(0f, -24f);
        RectTransform sourceCard = GetEnemyPlayedSourceRect();
        if (sourceCard != null)
        {
            Vector2 projected;
            RectTransform parentRt = enemyHandArea.parent as RectTransform;
            if (parentRt != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    RectTransformUtility.WorldToScreenPoint(null, sourceCard.position),
                    null,
                    out projected))
            {
                start = projected;
            }
        }

        rect.anchoredPosition = start;
        rect.localScale = Vector3.one;

        CardDisplay display = ghost.GetComponentInChildren<CardDisplay>();
        if (display != null)
        {
            display.SetCard(card);
            ApplyPrefabVisualTuning(display, true);
        }

        Vector2 target = enemyFieldArea != null ? enemyFieldArea.anchoredPosition : new Vector2(260f, 10f);
        float t = 0f;
        const float dur = 0.24f;
        while (t < dur && ghost != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            rect.anchoredPosition = Vector2.Lerp(start, target, p);
            rect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.06f, p);
            yield return null;
        }
        if (ghost != null) Destroy(ghost);
        deferEnemyFieldRefresh = false;
        RefreshFieldCards();
        lastFieldSignature = ComputeFieldSignature();
        isEnemyPlayingCardAnimation = false;
    }

    private RectTransform GetEnemyPlayedSourceRect()
    {
        if (enemyHandArea == null || enemyHandArea.childCount <= 0) return null;

        int targetIndex = Mathf.Clamp(enemyHandArea.childCount - 1, 0, enemyHandArea.childCount - 1);
        return enemyHandArea.GetChild(targetIndex) as RectTransform;
    }




    private void ForceHideSpellCastOverlay()
    {
        if (spellCastOverlayRoutine != null)
        {
            StopCoroutine(spellCastOverlayRoutine);
            spellCastOverlayRoutine = null;
        }
        if (spellCastOverlayGroup != null) spellCastOverlayGroup.alpha = 0f;
        if (spellCastOverlayRoot != null) spellCastOverlayRoot.gameObject.SetActive(false);
    }

    private void OnSpellCastPresentationStarted(bool isPlayer, string cardName, string skillDescription)
    {
        if (uiRoot == null || battleManager == null) return;
        SetHandButtonsInteractable();
        ForceHideSpellCastOverlay();
        spellCastOverlayRoutine = StartCoroutine(SpellCastOverlayRoutine(isPlayer, cardName, skillDescription ?? string.Empty));
    }

    /// <summary>技能說明原為「我方出牌」視角；敵方出牌時交換 我方/敵方 以符合玩家人稱。</summary>
    private static string SwapFactionLabelsForEnemyPerspective(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        const string ph = "\uE000";
        string s = text.Replace("我方", ph);
        s = s.Replace("敵方", "我方");
        return s.Replace(ph, "敵方");
    }

    private void EnsureSpellCastOverlay()
    {
        if (spellCastOverlayRoot != null) return;
        if (uiRoot == null) return;

        GameObject rootObj = new GameObject("SpellCastOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        rootObj.transform.SetParent(uiRoot, false);
        spellCastOverlayRoot = rootObj.GetComponent<RectTransform>();
        spellCastOverlayRoot.anchorMin = Vector2.zero;
        spellCastOverlayRoot.anchorMax = Vector2.one;
        spellCastOverlayRoot.offsetMin = Vector2.zero;
        spellCastOverlayRoot.offsetMax = Vector2.zero;

        Image dim = rootObj.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 1f);
        dim.raycastTarget = true;

        spellCastOverlayGroup = rootObj.GetComponent<CanvasGroup>();
        spellCastOverlayGroup.blocksRaycasts = true;
        spellCastOverlayGroup.interactable = false;
        spellCastOverlayGroup.alpha = 0f;

        GameObject panelObj = new GameObject("SpellCastPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(spellCastOverlayRoot, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(1080f, 640f);
        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.93f, 0.89f, 0.82f, 0.97f);
        panelBg.raycastTarget = false;

        GameObject titleObj = new GameObject("SpellCastTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);
        titleRt.sizeDelta = new Vector2(-48f, 152f);
        spellCastTitleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) spellCastTitleTmp.font = sharedUIFont;
        spellCastTitleTmp.fontSize = 48f;
        spellCastTitleTmp.fontStyle = FontStyles.Bold;
        spellCastTitleTmp.alignment = TextAlignmentOptions.Center;
        spellCastTitleTmp.color = new Color(0.26f, 0.22f, 0.17f, 1f);
        spellCastTitleTmp.enableWordWrapping = true;
        spellCastTitleTmp.richText = true;
        spellCastTitleTmp.overflowMode = TextOverflowModes.Overflow;

        GameObject bodyObj = new GameObject("SpellCastBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(panelObj.transform, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.offsetMin = new Vector2(32f, 24f);
        bodyRt.offsetMax = new Vector2(-32f, -176f);
        spellCastBodyTmp = bodyObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) spellCastBodyTmp.font = sharedUIFont;
        spellCastBodyTmp.fontSize = 34f;
        spellCastBodyTmp.lineSpacing = 4f;
        spellCastBodyTmp.alignment = TextAlignmentOptions.TopLeft;
        spellCastBodyTmp.color = new Color(0.24f, 0.2f, 0.16f, 1f);
        spellCastBodyTmp.enableWordWrapping = true;
        spellCastBodyTmp.overflowMode = TextOverflowModes.Overflow;

        rootObj.SetActive(false);
    }

    private void ApplySpellCastOverlayTypographyFix()
    {
        if (spellCastTitleTmp != null)
        {
            spellCastTitleTmp.fontSize = 48f;
            spellCastTitleTmp.overflowMode = TextOverflowModes.Overflow;
            spellCastTitleTmp.enableWordWrapping = true;
        }
        if (spellCastBodyTmp != null)
        {
            spellCastBodyTmp.fontSize = 34f;
            spellCastBodyTmp.lineSpacing = 4f;
            spellCastBodyTmp.overflowMode = TextOverflowModes.Overflow;
            spellCastBodyTmp.enableWordWrapping = true;
        }
        if (spellCastOverlayRoot != null)
        {
            Transform panel = spellCastOverlayRoot.Find("SpellCastPanel");
            RectTransform panelRt = panel != null ? panel.GetComponent<RectTransform>() : null;
            if (panelRt != null) panelRt.sizeDelta = new Vector2(1080f, 640f);
            Transform titleT = spellCastOverlayRoot.Find("SpellCastPanel/SpellCastTitle");
            if (titleT != null)
            {
                RectTransform tr = titleT.GetComponent<RectTransform>();
                if (tr != null)
                {
                    tr.sizeDelta = new Vector2(-48f, 152f);
                    tr.anchoredPosition = new Vector2(0f, -18f);
                }
            }
            Transform bodyT = spellCastOverlayRoot.Find("SpellCastPanel/SpellCastBody");
            RectTransform br = bodyT != null ? bodyT.GetComponent<RectTransform>() : null;
            if (br != null)
            {
                br.offsetMin = new Vector2(32f, 24f);
                br.offsetMax = new Vector2(-32f, -176f);
            }
        }
    }

    private IEnumerator SpellCastOverlayRoutine(bool isPlayer, string cardName, string skillDescription)
    {
        EnsureSpellCastOverlay();
        ApplySpellCastOverlayTypographyFix();
        float total = Mathf.Max(0.05f, battleManager.GetSpellCastPresentationSeconds());
        float fadeIn = Mathf.Min(0.14f, total * 0.18f);
        float fadeOut = Mathf.Min(0.14f, total * 0.18f);
        float hold = Mathf.Max(0f, total - fadeIn - fadeOut);

        string name = string.IsNullOrEmpty(cardName) ? "—" : cardName;
        if (isPlayer)
        {
            spellCastTitleTmp.text = name;
            spellCastBodyTmp.text = string.IsNullOrEmpty(skillDescription) ? "—" : skillDescription;
        }
        else
        {
            spellCastTitleTmp.text = "敵方發動了 " + name;
            spellCastBodyTmp.text = string.IsNullOrEmpty(skillDescription)
                ? "—"
                : SwapFactionLabelsForEnemyPerspective(skillDescription);
        }

        spellCastTitleTmp.ForceMeshUpdate();
        spellCastBodyTmp.ForceMeshUpdate();

        spellCastOverlayRoot.gameObject.SetActive(true);
        spellCastOverlayRoot.SetAsLastSibling();
        spellCastOverlayGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeIn && spellCastOverlayGroup != null)
        {
            t += Time.unscaledDeltaTime;
            spellCastOverlayGroup.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        if (spellCastOverlayGroup != null) spellCastOverlayGroup.alpha = 1f;
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
        t = 0f;
        while (t < fadeOut && spellCastOverlayGroup != null)
        {
            t += Time.unscaledDeltaTime;
            spellCastOverlayGroup.alpha = Mathf.Clamp01(1f - t / fadeOut);
            yield return null;
        }
        spellCastOverlayRoutine = null;
        if (spellCastOverlayGroup != null) spellCastOverlayGroup.alpha = 0f;
        if (spellCastOverlayRoot != null) spellCastOverlayRoot.gameObject.SetActive(false);
    }

    private BattleHandLongPressTooltip BindLongPressTooltip(GameObject cardObj, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        BattleHandLongPressTooltip lp = cardObj.GetComponent<BattleHandLongPressTooltip>();
        if (lp == null) lp = cardObj.AddComponent<BattleHandLongPressTooltip>();
        lp.Setup(message, ShowTooltip, HideTooltip);
        return lp;
    }

    private void ShowTooltip(RectTransform source, string message)
    {
        EnsureTooltip();
        if (tooltipPanel == null || tooltipText == null || uiRoot == null || source == null) return;
        Image tipBg = tooltipPanel.GetComponent<Image>();
        if (tipBg != null) tipBg.color = new Color(0f, 0f, 0f, HandTooltipBackgroundAlpha);

        // Only long-press flow should raise original hand card.
        // Hover-preview flow uses an overlay ghost and should not move original card.
        BattleHandLongPressTooltip longPress = source.GetComponent<BattleHandLongPressTooltip>();
        if (source.parent == handArea && longPress != null && longPress.enabled)
        {
            RaiseLongPressedCard(source);
        }

        tooltipText.text = message;
        tooltipPanel.gameObject.SetActive(true);
        tooltipPanel.SetAsLastSibling(); // keep tooltip readable above raised card
        // Stick tooltip to the left side of the pressed card.
        Vector3 world = source.TransformPoint(new Vector3(0f, source.rect.height * 0.6f, 0f));
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, RectTransformUtility.WorldToScreenPoint(null, world), null, out local);
        Vector2 pos = local + new Vector2(0f, 6f);
        tooltipPanel.anchoredPosition = pos;
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
        RestoreRaisedCardLayer();
    }

    private void EnsureTooltip()
    {
        if (tooltipPanel != null) return;
        if (uiRoot == null) return;

        GameObject panelObj = new GameObject("HandTooltip", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(uiRoot, false);
        tooltipPanel = panelObj.GetComponent<RectTransform>();
        tooltipPanel.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipPanel.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipPanel.pivot = new Vector2(0.5f, 0.5f);
        tooltipPanel.sizeDelta = new Vector2(HandTooltipPanelWidth, HandTooltipPanelHeight);
        tooltipPanel.anchoredPosition = new Vector2(280f, 0f);

        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0.14f, 0.1f, 0.08f, HandTooltipBackgroundAlpha);

        GameObject txtObj = new GameObject("TooltipText", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(panelObj.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(16f, 14f);
        txtRect.offsetMax = new Vector2(-16f, -14f);

        tooltipText = txtObj.GetComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = HandTooltipFontSize;
        tooltipText.lineSpacing = 1.05f;
        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tooltipText.verticalOverflow = VerticalWrapMode.Truncate;
        tooltipText.color = new Color(0.98f, 0.95f, 0.9f, 1f);
        tooltipText.alignment = TextAnchor.UpperLeft;

        panelObj.SetActive(false);
    }

    private void ApplyPrefabVisualTuning(CardDisplay display, bool isFieldCard = false, bool isFieldSpellCard = false)
    {
        float textScale = GetHandCardTextScale();
        float backplateScale = GetHandCardBackplateScale();
        BattleFieldCardTuning fieldTuning = GetFieldTuning();
        float fieldStatScale = fieldTuning != null ? fieldTuning.fieldAttackHealthTextScale : 0.85f;
        float fieldSpellTextScale = fieldTuning != null ? fieldTuning.fieldSpellTextScale : 1f;

        if (isFieldCard && display.nameText != null)
        {
            display.nameText.gameObject.SetActive(false);
        }

        TextMeshProUGUI[] texts = display.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI t = texts[i];
            // Use transform scale instead of fontSize to avoid layout reflow
            // that can shift attack/defense number positions.
            float scale = textScale;
            if (display.nameText != null && t == display.nameText)
            {
                scale *= GetHandCardNameScale();
            }
            if (isFieldCard)
            {
                if ((display.attackText != null && t == display.attackText) ||
                    (display.healthText != null && t == display.healthText))
                {
                    scale *= fieldStatScale;
                }
                else if (isFieldSpellCard)
                {
                    scale *= fieldSpellTextScale;
                }
            }
            t.rectTransform.localScale = Vector3.one * scale;
        }

        if (display.backgroundImage != null)
        {
            RectTransform bgRect = display.backgroundImage.rectTransform;
            bgRect.localScale = Vector3.one * backplateScale;
        }

        // Fixed visual polish: move top Chinese card title away from the edge.
        ApplyTopInset(display.nameText, 0f, 20f);
        ApplyNamePlateInset(display.nameText, 0f, 20f);
        // Fixed visual polish: keep ATK/DEF numbers away from card edges.
        ApplyStatInset(display.attackText, 18f, 10f);
        ApplyStatInset(display.healthText, 18f, 10f);
        if (isFieldCard) ApplyVerticalNudge(display.attackText, 0f);

        // Hide translucent label plates behind name/ATK/DEF texts.
        HidePlateForText(display.nameText, display.backgroundImage);
        HidePlateForText(display.attackText, display.backgroundImage);
        HidePlateForText(display.healthText, display.backgroundImage);
    }

    private void ApplyStatInset(TextMeshProUGUI text, float xInset, float yInset)
    {
        if (text == null) return;

        RectTransform rt = text.rectTransform;
        Vector2 pos = rt.anchoredPosition;

        bool anchorLeft = rt.anchorMin.x <= 0.01f && rt.anchorMax.x <= 0.01f;
        bool anchorRight = rt.anchorMin.x >= 0.99f && rt.anchorMax.x >= 0.99f;
        bool anchorBottom = rt.anchorMin.y <= 0.01f && rt.anchorMax.y <= 0.01f;

        if (anchorLeft) pos.x = Mathf.Abs(pos.x) + xInset;
        else if (anchorRight) pos.x = -Mathf.Abs(pos.x) - xInset;

        if (anchorBottom) pos.y = Mathf.Abs(pos.y) + yInset;

        rt.anchoredPosition = pos;
    }

    private void ApplyTopInset(TextMeshProUGUI text, float xInset, float yInset)
    {
        if (text == null) return;

        RectTransform rt = text.rectTransform;
        Vector2 pos = rt.anchoredPosition;

        bool anchorTop = rt.anchorMin.y >= 0.99f && rt.anchorMax.y >= 0.99f;
        bool anchorLeft = rt.anchorMin.x <= 0.01f && rt.anchorMax.x <= 0.01f;
        bool anchorRight = rt.anchorMin.x >= 0.99f && rt.anchorMax.x >= 0.99f;

        if (anchorTop) pos.y = -Mathf.Abs(pos.y) - yInset;
        if (anchorLeft) pos.x = Mathf.Abs(pos.x) + xInset;
        else if (anchorRight) pos.x = -Mathf.Abs(pos.x) - xInset;

        rt.anchoredPosition = pos;
    }

    private void ApplyNamePlateInset(TextMeshProUGUI nameText, float xInset, float yInset)
    {
        if (nameText == null) return;
        RectTransform plate = nameText.rectTransform.parent as RectTransform;
        if (plate == null) return;

        // Only treat it as name plate when parent is an Image container.
        Image plateImage = plate.GetComponent<Image>();
        if (plateImage == null) return;

        Vector2 pos = plate.anchoredPosition;
        bool anchorTop = plate.anchorMin.y >= 0.99f && plate.anchorMax.y >= 0.99f;
        bool anchorLeft = plate.anchorMin.x <= 0.01f && plate.anchorMax.x <= 0.01f;
        bool anchorRight = plate.anchorMin.x >= 0.99f && plate.anchorMax.x >= 0.99f;

        if (anchorTop) pos.y = -Mathf.Abs(pos.y) - yInset;
        if (anchorLeft) pos.x = Mathf.Abs(pos.x) + xInset;
        else if (anchorRight) pos.x = -Mathf.Abs(pos.x) - xInset;

        plate.anchoredPosition = pos;
    }

    private void HidePlateForText(TextMeshProUGUI text, Image mainCardBackground)
    {
        if (text == null) return;
        Transform parent = text.rectTransform.parent;
        if (parent == null) return;

        Image plate = parent.GetComponent<Image>();
        if (plate == null) return;
        if (mainCardBackground != null && plate == mainCardBackground) return;

        plate.enabled = false;
    }

    private void ApplyVerticalNudge(TextMeshProUGUI text, float deltaY)
    {
        if (text == null) return;
        RectTransform rt = text.rectTransform;
        Vector2 pos = rt.anchoredPosition;
        pos.y += deltaY;
        rt.anchoredPosition = pos;
    }

    private void CachePrefabCardSize()
    {
        prefabCardSize = new Vector2(BattleHandLayoutWidthPx, BattleHandLayoutHeightPx);
        battleCardPrefabNativeSize = new Vector2(168.5f, 245.5f);

        if (battleCardPrefab == null) return;
        RectTransform prefabRect = battleCardPrefab.GetComponent<RectTransform>();
        if (prefabRect == null) return;
        if (prefabRect.sizeDelta.x > 1f && prefabRect.sizeDelta.y > 1f)
            battleCardPrefabNativeSize = prefabRect.sizeDelta;
    }

    private static Vector2 GetPrefabLogicalRectSize(RectTransform prefabRect)
    {
        if (prefabRect == null) return Vector2.zero;
        Vector3 ls = prefabRect.localScale;
        return new Vector2(
            prefabRect.sizeDelta.x * Mathf.Abs(ls.x),
            prefabRect.sizeDelta.y * Mathf.Abs(ls.y));
    }

    private float GetBattleCardPrefabLogicalWidth()
    {
        return Mathf.Max(1f, GetPrefabLogicalRectSize(
            battleCardPrefab != null ? battleCardPrefab.GetComponent<RectTransform>() : null).x);
    }

    private float GetBattleCardLayoutWidthRatio() =>
        BattleHandLayoutWidthPx / GetBattleCardPrefabLogicalWidth();

    /// <summary>手牌：含 handCardSizeMultiplier，僅用於手牌區實際卡牌。</summary>
    private float GetBattleHandUniformScale(float extraMultiplier = 1f)
    {
        float handScale = GetHandCardScale() * Mathf.Max(0.01f, extraMultiplier);
        return handScale * GetBattleCardLayoutWidthRatio();
    }

    /// <summary>抽牌／牌庫／敵方出牌幽靈等：固定基準尺寸，不受 handCardSizeMultiplier 影響。</summary>
    private float GetBattleCardFxUniformScale(float extraMultiplier = 1f)
    {
        float layoutScale = BattleSimulationManager.HandCardScaleBaseline * Mathf.Max(0.01f, extraMultiplier);
        return layoutScale * GetBattleCardLayoutWidthRatio();
    }

    private void ApplyBattleHandCardRectLayout(RectTransform rect, float extraMultiplier = 1f)
    {
        if (rect == null) return;
        rect.sizeDelta = battleCardPrefabNativeSize;
        rect.localScale = Vector3.one * GetBattleHandUniformScale(extraMultiplier);
    }

    private float GetFieldMonsterScale()
    {
        if (battleManager == null) return BattleSimulationManager.FieldCardScaleBaseline;
        return battleManager.FieldMonsterScale;
    }

    private float GetFieldSpellScale()
    {
        if (battleManager == null) return BattleFieldCardTuning.FieldSpellScaleBaseline;
        return battleManager.FieldSpellScale;
    }

    private BattleFieldCardTuning GetFieldTuning() => battleManager != null ? battleManager.CardField : null;

    private float GetBattleFieldUniformScale(bool enemy, bool isSpell)
    {
        BattleFieldCardTuning tuning = GetFieldTuning();
        float fieldScale = tuning != null
            ? tuning.GetFieldCardScaleForSide(enemy, isSpell)
            : GetFieldMonsterScale();
        return fieldScale * GetBattleCardLayoutWidthRatio();
    }

    private void ApplyBattleFieldCardRectLayout(RectTransform rect, bool enemy, bool isSpell)
    {
        if (rect == null) return;
        rect.sizeDelta = battleCardPrefabNativeSize;
        rect.localScale = Vector3.one * GetBattleFieldUniformScale(enemy, isSpell);
    }

    private int ComputeFieldLayoutSignature()
    {
        BattleFieldCardTuning t = GetFieldTuning();
        if (t == null) return 0;
        return System.HashCode.Combine(
            t.fieldAreaOffsetY,
            t.playerMonsterFieldX,
            t.enemyMonsterFieldX,
            t.monsterSpellSpacingX);
    }

    private int ComputeFieldTextTuningSignature()
    {
        BattleFieldCardTuning t = GetFieldTuning();
        if (t == null) return 0;
        return System.HashCode.Combine(t.fieldAttackHealthTextScale, t.fieldSpellTextScale);
    }

    private void ApplyFieldZoneLayoutFromTuningIfChanged()
    {
        int sig = ComputeFieldLayoutSignature();
        if (sig == lastFieldLayoutSignature) return;
        lastFieldLayoutSignature = sig;
        ApplyFieldZoneLayoutFromTuning(force: false);
    }

    private void ApplyFieldZoneLayoutFromTuning(bool force)
    {
        if (!force)
        {
            int sig = ComputeFieldLayoutSignature();
            if (sig == lastFieldLayoutSignature) return;
            lastFieldLayoutSignature = sig;
        }
        else
        {
            lastFieldLayoutSignature = ComputeFieldLayoutSignature();
        }

        BattleFieldCardTuning t = GetFieldTuning();
        if (t == null) return;

        float y = t.fieldAreaOffsetY;
        if (playerFieldArea != null)
            playerFieldArea.anchoredPosition = new Vector2(t.playerMonsterFieldX, y);
        if (playerSpellFieldArea != null)
            playerSpellFieldArea.anchoredPosition = new Vector2(t.playerMonsterFieldX - t.monsterSpellSpacingX, y);
        if (enemyFieldArea != null)
            enemyFieldArea.anchoredPosition = new Vector2(t.enemyMonsterFieldX, y);
        if (enemySpellFieldArea != null)
            enemySpellFieldArea.anchoredPosition = new Vector2(t.enemyMonsterFieldX + t.monsterSpellSpacingX, y);
    }

    private float GetBattleCardPrefabLogicalHeight()
    {
        return Mathf.Max(1f, GetPrefabLogicalRectSize(
            battleCardPrefab != null ? battleCardPrefab.GetComponent<RectTransform>() : null).y);
    }

    private float GetBattleHandDisplayedWidth(float extraMultiplier = 1f) =>
        GetBattleCardPrefabLogicalWidth() * GetBattleHandUniformScale(extraMultiplier);

    private float GetBattleHandDisplayedHeight(float extraMultiplier = 1f) =>
        GetBattleCardPrefabLogicalHeight() * GetBattleHandUniformScale(extraMultiplier);

    private Vector2 GetBattleHandDisplayedSize(float extraMultiplier = 1f) =>
        new Vector2(GetBattleHandDisplayedWidth(extraMultiplier), GetBattleHandDisplayedHeight(extraMultiplier));

    private float GetBattleCardFxDisplayedWidth(float extraMultiplier = 1f) =>
        GetBattleCardPrefabLogicalWidth() * GetBattleCardFxUniformScale(extraMultiplier);

    private float GetBattleCardFxDisplayedHeight(float extraMultiplier = 1f) =>
        GetBattleCardPrefabLogicalHeight() * GetBattleCardFxUniformScale(extraMultiplier);

    private Vector2 GetBattleCardFxDisplayedSize(float extraMultiplier = 1f) =>
        new Vector2(GetBattleCardFxDisplayedWidth(extraMultiplier), GetBattleCardFxDisplayedHeight(extraMultiplier));

    private float GetHandCardScale()
    {
        if (battleManager == null) return BattleSimulationManager.HandCardScaleBaseline;
        return battleManager.HandCardScale;
    }

    private float GetHandCardSpacing()
    {
        if (battleManager == null) return 10f;
        return Mathf.Max(0f, battleManager.handCardSpacing);
    }

    private float GetHandCardTextScale()
    {
        if (battleManager == null) return 1f;
        return Mathf.Max(0.1f, battleManager.handCardTextScale);
    }

    private float GetHandCardBackplateScale()
    {
        if (battleManager == null) return 1f;
        return Mathf.Max(0.1f, battleManager.handCardBackplateScale);
    }

    private float GetHandCardNameScale()
    {
        if (battleManager == null) return 1f;
        return Mathf.Max(0.1f, battleManager.handCardNameScale);
    }

    private float GetHandScaleLiftOffset() =>
        Mathf.Max(0f, GetHandCardScale() - BattleSimulationManager.HandCardScaleBaseline) * 120f;

    /// <summary>我方手牌區 Y：底邊錨點。</summary>
    private float GetPlayerHandAreaYOffset()
    {
        if (battleManager == null) return -25f;

        float lift = GetHandScaleLiftOffset();
        if (battleManager.CanPlayerActNow())
            return battleManager.HandAreaAnchoredYCanPlay + lift;

        return battleManager.HandAreaAnchoredYCantPlay;
    }

    /// <summary>敵方手牌區 Y：頂邊錨點。</summary>
    private float GetEnemyHandAreaYOffset()
    {
        if (battleManager == null) return -20f;

        float lift = GetHandScaleLiftOffset();
        if (battleManager.CanEnemyActNow())
            return battleManager.EnemyHandAreaAnchoredYCanPlay - lift;

        return battleManager.EnemyHandAreaAnchoredYCantPlay;
    }

    private GameObject ResolveCardPrefab()
    {
        if (battleManager != null && battleManager.battleCardPrefab != null)
        {
            return battleManager.battleCardPrefab;
        }

        OpenPackge openPack = Object.FindFirstObjectByType<OpenPackge>();
        if (openPack != null && openPack.cardPrefab != null) return openPack.cardPrefab;

        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        if (deckManager != null)
        {
            if (deckManager.deckCardPrefab != null) return deckManager.deckCardPrefab;
            if (deckManager.librarycardPrefab != null) return deckManager.librarycardPrefab;
        }

        // If BattleSimulation has no OpenPackge/DeckManager, reuse any CardDisplay prefab already in the scene.
        CardDisplay[] displays = Resources.FindObjectsOfTypeAll<CardDisplay>();
        for (int i = 0; i < displays.Length; i++)
        {
            CardDisplay d = displays[i];
            if (d == null) continue;
            if (!d.gameObject.scene.IsValid()) continue; // skip non-scene assets
            if (d.GetComponentInParent<BattleSimulationDebugUI>() != null) continue; // skip cards spawned by this UI

            RectTransform root = d.GetComponentInParent<RectTransform>();
            if (root == null) continue;

            return root.gameObject;
        }

        Debug.LogWarning("BattleSimulationDebugUI: no card prefab found, using text fallback cards. Assign BattleSimulationManager.battleCardPrefab for stable rendering.");
        return null;
    }


    private void OnLinGazePeriodicStrikeVisualRequested(bool fromPlayerLinGaze)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (fromPlayerLinGaze)
        {
            if (linGazeEyeStrikeRoutinePlayer != null) StopCoroutine(linGazeEyeStrikeRoutinePlayer);
            linGazeEyeStrikeRoutinePlayer = StartCoroutine(PlayLinGazePeriodicEyeStrikeRoutine(true));
        }
        else
        {
            if (linGazeEyeStrikeRoutineEnemy != null) StopCoroutine(linGazeEyeStrikeRoutineEnemy);
            linGazeEyeStrikeRoutineEnemy = StartCoroutine(PlayLinGazePeriodicEyeStrikeRoutine(false));
        }
    }

    /// <summary>0–1 正規化時間：瞳孔垂直開合（分段＋SmoothStep，避免高速 Sin 顫動）。</summary>
    private static float LinGazeEyeStrikePupilOpen01(float u)
    {
        u = Mathf.Clamp01(u);
        float y = 1f;
        if (u < 0.07f)
            y = Mathf.Lerp(0.42f, 1f, Mathf.SmoothStep(0f, 1f, u / 0.07f));

        float b1 = Mathf.InverseLerp(0.1f, 0.28f, u);
        if (b1 > 0f && b1 < 1f)
        {
            float k = Mathf.Sin(b1 * Mathf.PI);
            k = k * k;
            y *= Mathf.Lerp(1f, 0.2f, k);
        }

        float strike = Mathf.InverseLerp(0.32f, 0.5f, u);
        if (strike > 0f && strike < 1f)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Sin(strike * Mathf.PI));
            y *= Mathf.Lerp(1f, 0.26f, k);
        }

        float b2 = Mathf.InverseLerp(0.58f, 0.76f, u);
        if (b2 > 0f && b2 < 1f)
        {
            float k = Mathf.Sin(b2 * Mathf.PI);
            y *= Mathf.Lerp(1f, 0.58f, k * k * 0.55f);
        }

        if (u > 0.78f)
            y *= Mathf.Lerp(1f, 0.92f, Mathf.SmoothStep(0f, 1f, (u - 0.78f) / 0.22f));

        return Mathf.Max(0.07f, y);
    }

    /// <summary>凝視「傷害瞬間」權重（單峰、平滑）。</summary>
    private static float LinGazeEyeStrikeDamagePeak01(float u)
    {
        u = Mathf.Clamp01(u);
        float d = Mathf.Abs(u - 0.405f) / 0.15f;
        if (d >= 1f) return 0f;
        float x = 1f - d;
        return x * x * (3f - 2f * x);
    }

    /// <summary>林可的凝視每回合結算：在咒術區牌面上方疊一層雙眼閃動＋紫紅凝視光。</summary>
    private IEnumerator PlayLinGazePeriodicEyeStrikeRoutine(bool fromPlayerLinGaze)
    {
        const float dur = LinGazeEyeStrikeDuration;
        try
        {
            yield return null;
            yield return null;

            GameObject spellRoot = fromPlayerLinGaze ? playerSpellFieldCardObj : enemySpellFieldCardObj;
            RectTransform spellAreaRt = fromPlayerLinGaze ? playerSpellFieldArea : enemySpellFieldArea;
            Transform fxParent = spellRoot != null ? spellRoot.transform : (spellAreaRt != null ? spellAreaRt.transform : null);
            if (fxParent == null) yield break;

            Transform oldFx = fxParent.Find("LinGazeEyeStrikeFx");
            if (oldFx != null) Destroy(oldFx.gameObject);

            GameObject root = new GameObject("LinGazeEyeStrikeFx", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(fxParent, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.localScale = Vector3.one;
            root.transform.SetAsLastSibling();

            CanvasGroup rootCg = root.GetComponent<CanvasGroup>();
            rootCg.alpha = 0f;

            GameObject glare = new GameObject("GazeGlare", typeof(RectTransform), typeof(Image));
            glare.transform.SetParent(root.transform, false);
            RectTransform glareRt = glare.GetComponent<RectTransform>();
            glareRt.anchorMin = glareRt.anchorMax = new Vector2(0.5f, 0.5f);
            glareRt.pivot = new Vector2(0.5f, 0.5f);
            glareRt.sizeDelta = new Vector2(168f, 96f);
            glareRt.anchoredPosition = new Vector2(0f, 20f);
            Image glareImg = glare.GetComponent<Image>();
            glareImg.sprite = GetUnitWhiteSprite();
            glareImg.type = Image.Type.Simple;
            glareImg.raycastTarget = false;
            glareImg.color = new Color(0.62f, 0.2f, 0.9f, 0f);

            void BuildEye(string name, float xPos, out Image scleraOut, out RectTransform pupilRtOut)
            {
                GameObject eye = new GameObject(name, typeof(RectTransform));
                eye.transform.SetParent(root.transform, false);
                RectTransform eyeRt = eye.GetComponent<RectTransform>();
                eyeRt.anchorMin = eyeRt.anchorMax = new Vector2(0.5f, 0.5f);
                eyeRt.pivot = new Vector2(0.5f, 0.5f);
                eyeRt.sizeDelta = new Vector2(48f, 30f);
                eyeRt.anchoredPosition = new Vector2(xPos, 18f);

                GameObject sclera = new GameObject("Sclera", typeof(RectTransform), typeof(Image));
                sclera.transform.SetParent(eye.transform, false);
                RectTransform sRt = sclera.GetComponent<RectTransform>();
                sRt.anchorMin = Vector2.zero;
                sRt.anchorMax = Vector2.one;
                sRt.offsetMin = Vector2.zero;
                sRt.offsetMax = Vector2.zero;
                scleraOut = sclera.GetComponent<Image>();
                scleraOut.sprite = GetUnitWhiteSprite();
                scleraOut.type = Image.Type.Simple;
                scleraOut.raycastTarget = false;
                scleraOut.color = new Color(0.93f, 0.91f, 1f, 0f);

                GameObject pupil = new GameObject("Pupil", typeof(RectTransform), typeof(Image));
                pupil.transform.SetParent(eye.transform, false);
                pupilRtOut = pupil.GetComponent<RectTransform>();
                pupilRtOut.anchorMin = pupilRtOut.anchorMax = new Vector2(0.5f, 0.5f);
                pupilRtOut.pivot = new Vector2(0.5f, 0.5f);
                pupilRtOut.sizeDelta = new Vector2(15f, 15f);
                pupilRtOut.anchoredPosition = Vector2.zero;
                Image pImg = pupil.GetComponent<Image>();
                pImg.sprite = GetUnitWhiteSprite();
                pImg.type = Image.Type.Simple;
                pImg.raycastTarget = false;
                pImg.color = new Color(0.1f, 0.05f, 0.18f, 1f);
            }

            Image scleraL;
            RectTransform pupilLrt;
            BuildEye("EyeL", -36f, out scleraL, out pupilLrt);
            Image scleraR;
            RectTransform pupilRrt;
            BuildEye("EyeR", 36f, out scleraR, out pupilRrt);

            float t = 0f;
            Color scleraBase = new Color(0.93f, 0.91f, 1f, 0.96f);
            Color scleraStrike = new Color(1f, 0.38f, 0.48f, 1f);
            while (t < dur && root != null)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float intro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.08f));
                float outro = u < 0.8f ? 1f : Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((u - 0.8f) / 0.2f));
                rootCg.alpha = intro * outro;

                float pyL = LinGazeEyeStrikePupilOpen01(Mathf.Clamp01(u + 0.022f));
                float pyR = LinGazeEyeStrikePupilOpen01(Mathf.Clamp01(u - 0.022f));
                pupilLrt.localScale = new Vector3(1f, pyL, 1f);
                pupilRrt.localScale = new Vector3(1f, pyR, 1f);

                float strike = LinGazeEyeStrikeDamagePeak01(u);
                float scleraA = Mathf.Lerp(0f, scleraBase.a, intro);
                Color sclNow = Color.Lerp(scleraBase, scleraStrike, strike * 0.82f);
                sclNow.a = scleraA;
                scleraL.color = sclNow;
                scleraR.color = sclNow;

                float glareBase = 0.1f + 0.06f * Mathf.Sin(u * Mathf.PI * 2.4f);
                float glareA = Mathf.Lerp(0f, glareBase + 0.34f * strike, intro) * outro;
                glareImg.color = new Color(0.68f + 0.1f * strike, 0.19f + 0.06f * strike, 0.9f, glareA);

                float lift = 2.4f * strike * (1f - strike * 0.35f);
                glareRt.anchoredPosition = new Vector2(0f, 20f + lift);

                yield return null;
            }

            if (root != null) Destroy(root);
        }
        finally
        {
            if (fromPlayerLinGaze) linGazeEyeStrikeRoutinePlayer = null;
            else linGazeEyeStrikeRoutineEnemy = null;
        }
    }

    private void OnPlayerLesserHealVisualRequested()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (lesserHealFieldFxRoutine != null)
            StopCoroutine(lesserHealFieldFxRoutine);
        lesserHealFieldFxRoutine = StartCoroutine(PlayLesserHealFieldFxRoutine(true));
    }

    private void OnEnemyLesserHealVisualRequested()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (lesserHealFieldFxRoutine != null)
            StopCoroutine(lesserHealFieldFxRoutine);
        lesserHealFieldFxRoutine = StartCoroutine(PlayLesserHealFieldFxRoutine(false));
    }

    private static Sprite GetUnitWhiteSprite()
    {
        if (s_unitWhiteSprite != null) return s_unitWhiteSprite;
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        s_unitWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        s_unitWhiteSprite.name = "BattleSim_UnitWhite";
        return s_unitWhiteSprite;
    }

    /// <param name="onPlayerField">true=我方場上怪獸受初級治療；false=敵方場上怪獸。</param>
    private IEnumerator PlayLesserHealFieldFxRoutine(bool onPlayerField)
    {
        const float duration = 2f;
        const int healAmount = 40;
        try
        {
            yield return null;

            if (onPlayerField)
            {
                if (playerFieldCardObj == null && battleManager != null && battleManager.PlayerHasFieldMonster())
                    RefreshFieldCards();
            }
            else
            {
                if (enemyFieldCardObj == null && battleManager != null && battleManager.EnemyHasFieldMonster())
                    RefreshFieldCards();
            }

            GameObject fieldRoot = onPlayerField ? playerFieldCardObj : enemyFieldCardObj;
            if (fieldRoot == null) yield break;

            CardDisplay display = fieldRoot.GetComponentInChildren<CardDisplay>(true);
            if (display != null && battleManager != null)
            {
                Card fc = onPlayerField ? battleManager.GetPlayerFieldCard() : battleManager.GetEnemyFieldCard();
                if (fc != null)
                {
                    display.SetCard(fc);
                    ApplyFieldDamageHealthColor(display, fc);
                }
            }

            Transform fxParent = fieldRoot.transform;

            GameObject glowOuter = new GameObject("LesserHealGlowOuter", typeof(RectTransform), typeof(Image));
            glowOuter.transform.SetParent(fxParent, false);
            RectTransform goRt = glowOuter.GetComponent<RectTransform>();
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.offsetMin = new Vector2(-18f, -18f);
            goRt.offsetMax = new Vector2(18f, 18f);
            goRt.localScale = Vector3.one;
            glowOuter.transform.SetAsLastSibling();
            Image goImg = glowOuter.GetComponent<Image>();
            goImg.sprite = GetUnitWhiteSprite();
            goImg.type = Image.Type.Simple;
            goImg.raycastTarget = false;
            goImg.color = new Color(0.15f, 0.92f, 0.38f, 0.18f);
            CanvasGroup goCg = glowOuter.AddComponent<CanvasGroup>();

            GameObject glowInner = new GameObject("LesserHealGlowInner", typeof(RectTransform), typeof(Image));
            glowInner.transform.SetParent(fxParent, false);
            RectTransform giRt = glowInner.GetComponent<RectTransform>();
            giRt.anchorMin = Vector2.zero;
            giRt.anchorMax = Vector2.one;
            giRt.offsetMin = new Vector2(-10f, -10f);
            giRt.offsetMax = new Vector2(10f, 10f);
            giRt.localScale = Vector3.one;
            glowInner.transform.SetAsLastSibling();
            Image giImg = glowInner.GetComponent<Image>();
            giImg.sprite = GetUnitWhiteSprite();
            giImg.type = Image.Type.Simple;
            giImg.raycastTarget = false;
            giImg.color = new Color(0.35f, 1f, 0.55f, 0.28f);
            CanvasGroup giCg = glowInner.AddComponent<CanvasGroup>();

            GameObject floatObj = new GameObject("LesserHealFloat", typeof(RectTransform), typeof(TextMeshProUGUI));
            floatObj.transform.SetParent(fxParent, false);
            RectTransform fRt = floatObj.GetComponent<RectTransform>();
            // 錨在卡片右上角外圍（數值貼在框外，不壓在卡面中央）
            fRt.anchorMin = new Vector2(1f, 1f);
            fRt.anchorMax = new Vector2(1f, 1f);
            fRt.pivot = new Vector2(1f, 1f);
            fRt.anchoredPosition = new Vector2(14f, 10f);
            fRt.sizeDelta = new Vector2(160f, 48f);
            floatObj.transform.SetAsLastSibling();
            TextMeshProUGUI floatTmp = floatObj.GetComponent<TextMeshProUGUI>();
            floatTmp.raycastTarget = false;
            floatTmp.enableAutoSizing = false;
            floatTmp.fontSize = 34f;
            floatTmp.alignment = TextAlignmentOptions.TopRight;
            floatTmp.font = sharedUIFont != null ? sharedUIFont : TMP_Settings.defaultFontAsset;
            floatTmp.text = "+" + healAmount;
            floatTmp.color = new Color(0.45f, 1f, 0.62f, 1f);
            CanvasGroup fCg = floatObj.AddComponent<CanvasGroup>();

            Vector2 floatStart = fRt.anchoredPosition;
            float t = 0f;
            while (t < duration && fieldRoot != null)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float pulse = 0.55f + 0.38f * Mathf.Sin(t * 7.5f);
                float tail = 1f - u * u;
                goCg.alpha = Mathf.Clamp01(0.2f * pulse * tail + 0.06f);
                giCg.alpha = Mathf.Clamp01(0.36f * pulse * tail + 0.1f);
                float fade = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.4f) / 1.15f));
                floatTmp.color = new Color(0.45f, 1f, 0.62f, fade);
                fCg.alpha = fade;
                float drift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.85f));
                fRt.anchoredPosition = floatStart + new Vector2(18f, 32f) * drift;
                yield return null;
            }

            if (glowOuter != null) Destroy(glowOuter);
            if (glowInner != null) Destroy(glowInner);
            if (floatObj != null) Destroy(floatObj);
        }
        finally
        {
            lesserHealFieldFxRoutine = null;
        }
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction action, bool centerTop = false)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        if (centerTop)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(120f, 36f);

        Image image = buttonObj.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);
        image.raycastTarget = true;

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(action);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(buttonObj.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = labelObj.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        return button;
    }

    private void BindSceneEndTurnButton(Transform parent)
    {
        if (sceneEndTurnButton == null)
        {
            GameObject named = GameObject.Find("EndTurnButton");
            if (named != null) sceneEndTurnButton = named.GetComponent<Button>();
        }
        if (sceneEndTurnButton == null)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                if (b == null || b.gameObject == null) continue;
                string n = b.gameObject.name.ToLowerInvariant();
                if (n.Contains("endturn") || n.Contains("end_turn") || n.Contains("結束回合"))
                {
                    sceneEndTurnButton = b;
                    break;
                }
            }
        }

        if (sceneEndTurnButton != null)
        {
            quickEndTurnButton = sceneEndTurnButton;
            quickEndTurnButton.onClick.RemoveListener(battleManager.EndPlayerTurn);
            quickEndTurnButton.onClick.AddListener(battleManager.EndPlayerTurn);
            return;
        }

        // Fallback only when no scene button exists.
        quickEndTurnButton = CreateButton(parent, "QuickEndTurnButton", "End turn", new Vector2(0f, -190f), battleManager.EndPlayerTurn, true);
        RectTransform quickEndRect = quickEndTurnButton.GetComponent<RectTransform>();
        quickEndRect.anchorMin = new Vector2(1f, 0f);
        quickEndRect.anchorMax = new Vector2(1f, 0f);
        quickEndRect.pivot = new Vector2(1f, 0f);
        quickEndRect.anchoredPosition = new Vector2(-120f, 64f);
        quickEndRect.sizeDelta = new Vector2(220f, 66f);
        Text quickEndLabel = quickEndTurnButton.GetComponentInChildren<Text>();
        if (quickEndLabel != null) quickEndLabel.fontSize = 30;
    }

    private TMP_FontAsset ResolveUIFont()
    {
        // 1) Prefer the font already used by card prefab (CardDisplay),
        // so dynamic buttons match card Chinese rendering.
        if (battleCardPrefab != null)
        {
            CardDisplay display = battleCardPrefab.GetComponentInChildren<CardDisplay>(true);
            if (display != null)
            {
                if (display.nameText != null && display.nameText.font != null) return display.nameText.font;
                if (display.effectText != null && display.effectText.font != null) return display.effectText.font;
                if (display.attackText != null && display.attackText.font != null) return display.attackText.font;
                if (display.healthText != null && display.healthText.font != null) return display.healthText.font;
            }
        }

        // 2) Fallback: prefer a scene TMP font that likely supports CJK (card names / 對戰歷史).
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        TMP_FontAsset firstAny = null;
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].font == null) continue;
            if (firstAny == null) firstAny = texts[i].font;
            if (FontNameLikelySupportsCjk(texts[i].font.name)) return texts[i].font;
        }
        if (firstAny != null) return firstAny;

        // 3) Last fallback: TMP default font.
        return TMP_Settings.defaultFontAsset;
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

    private void RaiseLongPressedCard(RectTransform card)
    {
        if (card == null) return;
        if (longPressRaisedCard == card) return;
        RestoreRaisedCardLayer();

        // Avoid hover-zoom and long-press animation fighting each other.
        ZoomUI zoom = card.GetComponent<ZoomUI>();
        longPressDisabledZoom = zoom;
        if (zoom != null)
        {
            longPressZoomWasEnabled = zoom.enabled;
            zoom.enabled = false;
        }
        else
        {
            longPressZoomWasEnabled = false;
        }

        longPressRaisedCard = card;
        longPressOriginalSibling = card.GetSiblingIndex();
        longPressOriginalScale = card.localScale;
        card.SetAsLastSibling();
        StartLongPressScale(card, longPressOriginalScale * 1.08f, 0.12f);
    }

    private void RestoreRaisedCardLayer()
    {
        if (longPressRaisedCard == null) return;
        StartLongPressScale(longPressRaisedCard, longPressOriginalScale, 0.1f);
        if (longPressRaisedCard.parent != null &&
            longPressRaisedCard.gameObject.activeInHierarchy &&
            longPressRaisedCard.parent.gameObject.activeInHierarchy)
        {
            int max = longPressRaisedCard.parent.childCount - 1;
            int target = Mathf.Clamp(longPressOriginalSibling, 0, max);
            try
            {
                longPressRaisedCard.SetSiblingIndex(target);
            }
            catch (System.Exception)
            {
                // Parent may be in activation/deactivation transition; skip safe restore.
            }
        }

        if (longPressDisabledZoom != null)
        {
            longPressDisabledZoom.enabled = longPressZoomWasEnabled;
        }
        longPressDisabledZoom = null;
        longPressZoomWasEnabled = false;

        longPressRaisedCard = null;
        longPressOriginalSibling = -1;
    }

    private void StartLongPressScale(RectTransform target, Vector3 toScale, float duration)
    {
        if (target == null) return;
        if (longPressScaleRoutine != null) StopCoroutine(longPressScaleRoutine);
        longPressScaleRoutine = StartCoroutine(AnimateScale(target, toScale, duration));
    }

    private IEnumerator AnimateScale(RectTransform target, Vector3 toScale, float duration)
    {
        if (target == null) yield break;
        Vector3 from = target.localScale;
        float t = 0f;
        while (t < duration && target != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            target.localScale = Vector3.Lerp(from, toScale, p);
            yield return null;
        }
        if (target != null) target.localScale = toScale;
        longPressScaleRoutine = null;
    }
}

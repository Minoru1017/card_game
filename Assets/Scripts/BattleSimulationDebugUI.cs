using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    [Header("Debug panel (Play Mode)")]
    [Tooltip("When off, the keyboard chord does not toggle the floating debug window.")]
    [SerializeField] private bool debugPanelHotkeyEnabled = true;
    [Tooltip("Chord: hold both keys, or hold one and press the other. Release both before the next toggle.")]
    [SerializeField] private KeyCode debugPanelHotkeyKey1 = KeyCode.D;
    [SerializeField] private KeyCode debugPanelHotkeyKey2 = KeyCode.E;
    [Tooltip("When checked, the debug panel starts visible when entering Play Mode.")]
    [SerializeField] private bool debugPanelVisibleOnPlay;

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
    private Vector2 prefabCardSize = new Vector2(170f, 210f);
    private float lastScale = -1f;
    private float lastSpacing = -1f;
    private float lastTextScale = -1f;
    private float lastNameScale = -1f;
    private float lastBackplateScale = -1f;
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

    private void ToggleDebugUiRoot()
    {
        if (debugUiRoot == null) return;
        debugUiRoot.SetActive(!debugUiRoot.activeSelf);
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
                fieldText.text = battleManager.GetPlayerFieldText() + "\n" + battleManager.GetEnemyFieldText() + toastLine;
            }
        }
        TickDiscardSelectionUi();
        if (roundText != null)
        {
            roundText.text = "Round " + battleManager.GetCurrentRound();
            if (debugPanelVisible) roundText.transform.SetAsLastSibling();
        }
        int signature = ComputeHandSignature();
        int fieldSignature = ComputeFieldSignature();
        float currentScale = GetHandCardScale();
        float currentSpacing = GetHandCardSpacing();
        float currentTextScale = GetHandCardTextScale();
        float currentNameScale = GetHandCardNameScale();
        float currentBackplateScale = GetHandCardBackplateScale();
        float targetHandY = GetHandAreaYOffset();
        UpdateHandAreaYOffsetAnimated(targetHandY);
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

        if (fieldSignature != lastFieldSignature)
        {
            if (deferFieldRefreshDuringAttack)
            {
                pendingFieldRefreshAfterAttack = true;
            }
            else
            {
                lastFieldSignature = fieldSignature;
                RefreshFieldCards();
            }
        }

    }

    private void UpdateHandAreaYOffsetAnimated(float targetY)
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
        handAreaTweenRoutine = StartCoroutine(AnimateHandAreaY(handAreaTargetY));
    }

    private IEnumerator AnimateHandAreaY(float toY)
    {
        if (handArea == null) yield break;
        float fromY = handArea.anchoredPosition.y;
        float t = 0f;
        const float duration = 0.28f;
        while (t < duration && handArea != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            handAreaYCurrent = Mathf.Lerp(fromY, toY, eased);
            handArea.anchoredPosition = new Vector2(0f, handAreaYCurrent);
            yield return null;
        }
        if (handArea != null)
        {
            handAreaYCurrent = toY;
            handArea.anchoredPosition = new Vector2(0f, handAreaYCurrent);
        }
        handAreaTweenRoutine = null;
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
        bg.color = new Color(0.1f, 0.12f, 0.2f, 0.94f);
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
        turnBannerTmp.color = new Color(1f, 0.97f, 0.9f, 1f);
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
                turnBannerTmp.color = new Color(0.58f, 0.96f, 1f, 1f);
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
                turnBannerTmp.color = new Color(1f, 0.74f, 0.56f, 1f);
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
        dbgRt.anchorMin = new Vector2(1f, 1f);
        dbgRt.anchorMax = new Vector2(1f, 1f);
        dbgRt.pivot = new Vector2(1f, 1f);
        dbgRt.anchoredPosition = new Vector2(-14f, -14f);
        float winW = Mathf.Min(620f * m, 860f);
        float winH = Mathf.Min(940f * m, Mathf.Max(660f, Screen.height * 0.92f));
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
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        panelShadow.effectDistance = new Vector2(4f, -4f);

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
        roundRect.offsetMin = new Vector2(10f, -52f * m);
        roundRect.offsetMax = new Vector2(-10f, -6f);
        roundText = roundObj.GetComponent<Text>();
        roundText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        roundText.fontSize = Mathf.RoundToInt(32 * m);
        roundText.alignment = TextAnchor.MiddleCenter;
        roundText.color = new Color(1f, 0.95f, 0.55f, 1f);
        roundText.raycastTarget = false;
        roundText.text = "Round 1";

        GameObject deckObj = new GameObject("DeckText", typeof(RectTransform), typeof(TextMeshProUGUI));
        deckObj.transform.SetParent(panel.transform, false);
        RectTransform deckRect = deckObj.GetComponent<RectTransform>();
        deckRect.anchorMin = new Vector2(0f, 1f);
        deckRect.anchorMax = new Vector2(1f, 1f);
        deckRect.pivot = new Vector2(0.5f, 1f);
        deckRect.offsetMin = new Vector2(14f, -124f * m);
        deckRect.offsetMax = new Vector2(-14f, -58f * m);
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
        openingRollBg.color = new Color(0.35f, 0.35f, 0.35f, 1f);
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
        fieldRect.offsetMin = new Vector2(14f, -208f);
        fieldRect.offsetMax = new Vector2(-14f, -126f);
        fieldText = fieldObj.GetComponent<TextMeshProUGUI>();
        fieldText.fontSize = 22f * m;
        fieldText.color = Color.white;
        fieldText.raycastTarget = false;
        fieldText.richText = true;

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
        handArea.anchoredPosition = new Vector2(0f, GetHandAreaYOffset());
        handArea.sizeDelta = new Vector2(1100f, 230f);
        handAreaYCurrent = handArea.anchoredPosition.y;
        handAreaTargetY = handAreaYCurrent;

        GameObject enemyHandObj = new GameObject("EnemyHandArea", typeof(RectTransform));
        enemyHandObj.transform.SetParent(parent, false);
        enemyHandArea = enemyHandObj.GetComponent<RectTransform>();
        enemyHandArea.anchorMin = new Vector2(0.5f, 1f);
        enemyHandArea.anchorMax = new Vector2(0.5f, 1f);
        enemyHandArea.pivot = new Vector2(0.5f, 1f);
        enemyHandArea.anchoredPosition = new Vector2(0f, -20f);
        enemyHandArea.sizeDelta = new Vector2(1100f, 180f);

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
        playerFieldArea.anchoredPosition = new Vector2(-230f, 10f);
        playerFieldArea.sizeDelta = new Vector2(260f, 300f);

        GameObject playerSpellFieldObj = new GameObject("PlayerSpellFieldArea", typeof(RectTransform));
        playerSpellFieldObj.transform.SetParent(parent, false);
        playerSpellFieldArea = playerSpellFieldObj.GetComponent<RectTransform>();
        playerSpellFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        playerSpellFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        playerSpellFieldArea.pivot = new Vector2(0.5f, 0.5f);
        playerSpellFieldArea.anchoredPosition = new Vector2(-400f, 10f);
        playerSpellFieldArea.sizeDelta = new Vector2(150f, 300f);

        GameObject enemyFieldObj = new GameObject("EnemyFieldArea", typeof(RectTransform));
        enemyFieldObj.transform.SetParent(parent, false);
        enemyFieldArea = enemyFieldObj.GetComponent<RectTransform>();
        enemyFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        enemyFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        enemyFieldArea.pivot = new Vector2(0.5f, 0.5f);
        enemyFieldArea.anchoredPosition = new Vector2(260f, 10f);
        enemyFieldArea.sizeDelta = new Vector2(260f, 300f);
        enemyFieldArea.SetAsLastSibling();

        GameObject enemySpellFieldObj = new GameObject("EnemySpellFieldArea", typeof(RectTransform));
        enemySpellFieldObj.transform.SetParent(parent, false);
        enemySpellFieldArea = enemySpellFieldObj.GetComponent<RectTransform>();
        enemySpellFieldArea.anchorMin = new Vector2(0.5f, 0.5f);
        enemySpellFieldArea.anchorMax = new Vector2(0.5f, 0.5f);
        enemySpellFieldArea.pivot = new Vector2(0.5f, 0.5f);
        enemySpellFieldArea.anchoredPosition = new Vector2(400f, 10f);
        enemySpellFieldArea.sizeDelta = new Vector2(150f, 300f);
        enemySpellFieldArea.SetAsLastSibling();

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
        pauseBg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject pauseCardObj = new GameObject("PauseCard", typeof(RectTransform), typeof(Image));
        pauseCardObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform pauseCardRt = pauseCardObj.GetComponent<RectTransform>();
        pauseCardRt.anchorMin = new Vector2(0.5f, 0.5f);
        pauseCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        pauseCardRt.pivot = new Vector2(0.5f, 0.5f);
        pauseCardRt.anchoredPosition = Vector2.zero;
        pauseCardRt.sizeDelta = new Vector2(760f, 420f);
        Image pauseCardBg = pauseCardObj.GetComponent<Image>();
        pauseCardBg.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

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
        title.color = Color.white;
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
        SetPaused(false);
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void OnPauseGiveUpClicked()
    {
        SetPaused(false);
        StartCoroutine(GiveUpAfterLoseMessage());
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
        rt.sizeDelta = prefabCardSize * (GetHandCardScale() * 0.62f);

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
        rootRt.sizeDelta = prefabCardSize * 0.62f;
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
        rect.sizeDelta = prefabCardSize * (GetHandCardScale() * 0.75f);
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
        panelBg.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
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
        spellCastTitleTmp.color = new Color(0.98f, 0.95f, 0.88f, 1f);
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
        spellCastBodyTmp.color = new Color(0.9f, 0.92f, 0.96f, 1f);
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
        bg.color = new Color(0f, 0f, 0f, HandTooltipBackgroundAlpha);

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
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAnchor.UpperLeft;

        panelObj.SetActive(false);
    }

    private void ApplyPrefabVisualTuning(CardDisplay display, bool isFieldCard = false)
    {
        float textScale = GetHandCardTextScale();
        float backplateScale = GetHandCardBackplateScale();
        float fieldStatScale = (battleManager != null) ? battleManager.fieldMonsterStatTextScale : 0.85f;

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
        if (battleCardPrefab == null) return;

        RectTransform prefabRect = battleCardPrefab.GetComponent<RectTransform>();
        if (prefabRect == null) return;
        if (prefabRect.rect.width <= 1f || prefabRect.rect.height <= 1f) return;

        prefabCardSize = new Vector2(prefabRect.rect.width, prefabRect.rect.height);
    }

    private float GetHandCardScale()
    {
        if (battleManager == null) return 1.2f;
        return Mathf.Max(0.1f, battleManager.handCardScale);
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

    private float GetHandAreaYOffset()
    {
        float scale = GetHandCardScale();
        float baseY = battleManager == null ? 24f : battleManager.handAreaBaseYOffset;
        // Card grows upward when scaled; lift hand area to keep card bottoms visible.
        float scaleOffset = Mathf.Max(0f, scale - 1f) * 120f;
        float y = baseY + scaleOffset - 150f;

        // Only fully reveal player hand on player's turn.
        // On other turns, lower the hand so only top ~1/3 remains visible.
        if (battleManager != null && !battleManager.IsPlayerTurn())
        {
            float cardHeight = prefabCardSize.y * scale;
            y -= cardHeight * (2f / 3f);
        }

        return y;
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

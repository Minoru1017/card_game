using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>教學對戰：林可姐在戰鬥中引導出牌與結束回合（場上怪獸會在結束回合時自動攻擊；僅入門級教學戰）。</summary>
public sealed class TutorialBattleCoachUi : MonoBehaviour
{
    private const float CoachCharactersPerSecond = 9f;
    private const int HandFullWarningThreshold = 6;
    private const float ReEvaluateIntervalSeconds = 1.25f;
    private const float BorderPulseSpeed = 2.8f;

    private static readonly Vector2 CollapsedPanelSize = new Vector2(208f, 278f);
    /// <summary>與對戰英雄 HUD 左緣對齊，避免瀏海／圓角裁切林可姐面板。</summary>
    private const float PanelLeftMarginPx = 80f;
    private static readonly Vector2 CollapsedPanelPosition = new Vector2(PanelLeftMarginPx, 0f);
    private const float CollapsedPortraitSize = 152f;
    private const float CollapsedNameFontSize = 24f;
    private const float CollapsedTapHintFontSize = 17f;

    /// <summary>展開時略高於垂直中心，避開底部手牌、落在敵我英雄 HUD 之間較好閱讀。</summary>
    private static readonly Vector2 ExpandedPanelPosition = new Vector2(PanelLeftMarginPx, 108f);
    /// <summary>棄牌階段左側為棄牌區，林可姐改到右側以免遮擋。</summary>
    private static readonly Vector2 DiscardCollapsedPanelPosition = new Vector2(-PanelLeftMarginPx, 32f);
    private static readonly Vector2 DiscardExpandedPanelPosition = new Vector2(-PanelLeftMarginPx, 88f);
    private const float ExpandedPanelWidth = 520f;
    private const float ExpandedPortraitSize = 140f;
    private const float ExpandedEdgePad = 20f;
    private const float ExpandedPortraitTextGap = 16f;
    private const float ExpandedNameFontSize = 32f;
    private const float ExpandedBodyFontSize = 28f;
    private const float ExpandedNameRowHeight = 42f;
    private const float ExpandedMinPanelHeight = 220f;
    private const float ExpandedCanvasHeightMargin = 40f;

    private static readonly Color CoachBorderGold = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Color CoachBorderGlow = new Color(0.97f, 0.85f, 0.47f, 0.42f);
    private static readonly Color PortraitPlaceholderFill = new Color(0.92f, 0.86f, 0.76f, 1f);
    private static readonly Color PortraitFrameColor = new Color(0.38f, 0.28f, 0.24f, 0.9f);

    private const string CoachFontProbe =
        "林可姐點擊查看提示這是你的回合試著出一張怪獸或法術攻擊目標結束敵方在行動等他打完再輪到你好點選場上選擇若沒有其他行動可按手牌接近上限七張注意棄牌快用掉超過了先七張以下長按不要的牌拖到左側棄牌區第一回合火球還不能用治療專心出牌下再已有怪獸時只能再打初級或者教學戰完成之後背包看卡牌熟練度沒事英雄生命歸零就輸入門級再來一次吧";

    private BattleSimulationManager _manager;
    private BattleSimulationDebugUI _battleUi;
    private Transform _canvasRoot;
    private TMP_FontAsset _preferredFont;
    private PlotDialogueTypewriter _typewriter;
    private GameObject _root;
    private GameObject _backdrop;
    private RectTransform _panelRt;
    private RectTransform _borderGlowRt;
    private RectTransform _portraitFrameRt;
    private RectTransform _portraitRt;
    private RectTransform _nameRt;
    private RectTransform _tapHintRt;
    private RectTransform _bodyRt;
    private TMP_Text _bodyText;
    private TMP_Text _speakerNameText;
    private TMP_Text _tapHintText;
    private Image _portraitImage;
    private Outline _panelOutline;
    private string _lastHintMessage = string.Empty;
    private bool _uiBuilt;
    private bool _expanded;
    private bool _hasUnreadHint;
    private bool _wasPlayerTurn;
    private float _nextEvaluateUnscaled;
    private string _currentHintKey = string.Empty;

    private readonly HashSet<string> _shownThisTurnWindow = new HashSet<string>();
    private bool _eventsBound;
    private bool _discardLayoutActive;
    private bool _shownEnemyFieldMonsterHintThisBattle;

    private static Sprite placeholderPortraitSprite;

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsIntroTutorialBattle;

    public void Initialize(
        BattleSimulationManager manager,
        Transform canvasRoot,
        TMP_FontAsset uiFont = null,
        BattleSimulationDebugUI battleUi = null)
    {
        _manager = manager;
        _battleUi = battleUi;
        _canvasRoot = canvasRoot;
        _preferredFont = uiFont;
        _shownEnemyFieldMonsterHintThisBattle = false;
        ApplyCoachFontToLabels();
        if (_manager != null && !_eventsBound)
        {
            BindEvents();
            _eventsBound = true;
        }
    }

    private void OnDestroy()
    {
        ClearHandPlayHighlights();
        if (_eventsBound) UnbindEvents();
    }

    private void BindEvents()
    {
        _manager.PlayerTurnActionWindowOpenedForPromptUi += OnPlayerTurnWindowOpened;
        _manager.PlayerCommittedHandCardToFieldFromHand += OnPlayerCommittedCard;
        _manager.PlayerPressedEndTurnForPromptUi += OnPlayerPressedEndTurn;
        _manager.AttackPerformed += OnAttackPerformed;
        _manager.SpellCastAsyncPresentationFinished += OnSpellPresentationFinished;
        _manager.BattleEnded += OnBattleEnded;
    }

    private void UnbindEvents()
    {
        if (_manager == null) return;
        _manager.PlayerTurnActionWindowOpenedForPromptUi -= OnPlayerTurnWindowOpened;
        _manager.PlayerCommittedHandCardToFieldFromHand -= OnPlayerCommittedCard;
        _manager.PlayerPressedEndTurnForPromptUi -= OnPlayerPressedEndTurn;
        _manager.AttackPerformed -= OnAttackPerformed;
        _manager.SpellCastAsyncPresentationFinished -= OnSpellPresentationFinished;
        _manager.BattleEnded -= OnBattleEnded;
        _eventsBound = false;
    }

    private void Update()
    {
        if (!ShouldRun()) return;

        if (_expanded)
            _typewriter?.Tick(Time.unscaledDeltaTime);
        else
            UpdateCollapsedBorderPulse();

        if (_manager == null || _manager.IsBattleOver()) return;
        if (_manager.IsOpeningPresentationInProgress()) return;
        if (BattleAutoSimPlugin.IsRunning) return;

        bool isPlayerTurn = _manager.IsPlayerTurn();
        if (isPlayerTurn != _wasPlayerTurn)
        {
            _wasPlayerTurn = isPlayerTurn;
            if (isPlayerTurn)
            {
                _shownThisTurnWindow.Clear();
                _currentHintKey = string.Empty;
                ScheduleEvaluate(0f);
            }
            else
            {
                ClearHandPlayHighlights();
                ShowHint("enemy_turn", "敵方在行動等他打完再輪到你", oncePerTurnWindow: false);
            }
        }

        if (!isPlayerTurn) return;
        if (_manager.IsTurnSequenceInProgress() || _manager.IsSpellCastPresentationActive()) return;

        SyncDiscardPhasePanelLayout();

        if (Time.unscaledTime >= _nextEvaluateUnscaled)
            EvaluatePlayerTurnHints();
    }

    private bool IsDiscardPhaseActive()
    {
        if (_manager == null) return false;
        return _manager.IsPlayerInDiscardSelection() || _manager.GetPlayerPendingDiscardCount() > 0;
    }

    private void SyncDiscardPhasePanelLayout()
    {
        bool discard = IsDiscardPhaseActive();
        if (discard == _discardLayoutActive) return;
        _discardLayoutActive = discard;
        if (!_uiBuilt || _panelRt == null) return;

        ApplyPanelLayout();

        if (discard)
            PresentDiscardPhaseGuidance();
        else if (_currentHintKey == "discard")
        {
            ClearHandPlayHighlights();
            CollapsePanel();
            ScheduleEvaluate(0f);
        }
    }

    private void PresentDiscardPhaseGuidance()
    {
        if (!ShouldRun() || _manager == null) return;
        ShowHint("discard", BuildDiscardCoachMessage(_manager.GetPlayerPendingDiscardCount()), oncePerTurnWindow: false);
    }

    private static string BuildDiscardCoachMessage(int pendingDiscardCount)
    {
        return pendingDiscardCount <= 1
            ? "手牌超過上限了長按不要的牌拖到左側棄牌區"
            : ("手牌超過上限" + pendingDiscardCount + "張長按不要的牌拖到左側棄牌區");
    }

    private void UpdateCollapsedBorderPulse()
    {
        if (!_hasUnreadHint || _borderGlowRt == null) return;

        float pulse = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * BorderPulseSpeed));
        _borderGlowRt.localScale = Vector3.one * pulse;

        if (_panelOutline != null)
        {
            float a = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * BorderPulseSpeed));
            _panelOutline.effectColor = new Color(CoachBorderGold.r, CoachBorderGold.g, CoachBorderGold.b, a);
        }
    }

    private bool ShouldRun()
    {
        if (!IsActiveForCurrentBattle)
        {
            ClearHandPlayHighlights();
            if (_root != null) _root.SetActive(false);
            if (_backdrop != null) _backdrop.SetActive(false);
            return false;
        }

        if (_manager == null) return false;
        return true;
    }

    private void OnPlayerTurnWindowOpened()
    {
        if (!ShouldRun() || BattleAutoSimPlugin.IsRunning) return;
        _shownThisTurnWindow.Clear();
        _currentHintKey = string.Empty;
        ScheduleEvaluate(0.15f);
    }

    private void OnPlayerCommittedCard()
    {
        if (!ShouldRun() || BattleAutoSimPlugin.IsRunning) return;
        ClearHandPlayHighlights();
        if (_manager.GetPlayerFieldCard() != null)
            ShowHint("after_summon_end_turn", "很好現在按結束回合場上怪獸會自動攻擊");
        else
            ScheduleEvaluate(0.2f);
    }

    private void OnAttackPerformed(BattleSimulationManager.AttackVisualData data)
    {
        if (!ShouldRun() || BattleAutoSimPlugin.IsRunning) return;
        if (!data.attackerIsPlayer) return;
        ClearHandPlayHighlights();
    }

    private void OnSpellPresentationFinished()
    {
        if (!ShouldRun() || BattleAutoSimPlugin.IsRunning) return;
        ScheduleEvaluate(0.1f);
    }

    private void OnPlayerPressedEndTurn()
    {
        if (!ShouldRun()) return;
        ClearHandPlayHighlights();
        CollapsePanel();
        if (_root != null) _root.SetActive(false);
        if (_backdrop != null) _backdrop.SetActive(false);
    }

    private void OnBattleEnded(int result)
    {
        ClearHandPlayHighlights();
        CollapsePanel();
        if (_root != null) _root.SetActive(false);
        if (_backdrop != null) _backdrop.SetActive(false);
    }

    /// <summary>入門教學：我方場怪被擊殺且仍為玩家回合時，改回「請出牌」提示並啟用手牌高亮。</summary>
    public static void NotifyPlayerFieldEmptiedDuringPlayerTurn(BattleSimulationManager manager)
    {
        if (!IsActiveForCurrentBattle || manager == null) return;

        TutorialBattleCoachUi coach = Object.FindFirstObjectByType<TutorialBattleCoachUi>();
        if (coach == null || !coach.ShouldRun()) return;

        coach._shownThisTurnWindow.Remove("end_turn");
        coach._shownThisTurnWindow.Remove("opening_end_turn");
        coach._shownThisTurnWindow.Remove("after_summon_end_turn");
        coach._currentHintKey = string.Empty;
        coach.ScheduleEvaluate(0f);
    }

    public void HideForSettlement()
    {
        ClearHandPlayHighlights();
        CollapsePanel();
        if (_root != null) _root.SetActive(false);
        if (_backdrop != null) _backdrop.SetActive(false);
    }

    private void ScheduleEvaluate(float delaySeconds)
    {
        _nextEvaluateUnscaled = Time.unscaledTime + Mathf.Max(0f, delaySeconds);
    }

    private void EvaluatePlayerTurnHints()
    {
        _nextEvaluateUnscaled = Time.unscaledTime + ReEvaluateIntervalSeconds;
        if (!ShouldRun() || _manager == null) return;
        if (!_manager.IsPlayerTurn() || _manager.IsBattleOver()) return;
        if (_manager.IsOpeningPresentationInProgress()) return;
        if (_manager.IsTurnSequenceInProgress() || _manager.IsSpellCastPresentationActive()) return;

        if (_manager.IsPlayerInDiscardSelection() || _manager.GetPlayerPendingDiscardCount() > 0)
        {
            ShowHint("discard", BuildDiscardCoachMessage(_manager.GetPlayerPendingDiscardCount()), oncePerTurnWindow: false);
            return;
        }

        int handCount = _manager.GetPlayerHandCount();
        if (handCount >= HandFullWarningThreshold)
            ShowHint("hand_full", "手牌接近上限七張注意棄牌或快用掉");

        if (!_shownEnemyFieldMonsterHintThisBattle && _manager.GetEnemyFieldCard() != null)
        {
            _shownEnemyFieldMonsterHintThisBattle = true;
            ShowHint(
                "enemy_monster_fireball",
                StoryTextStyle.Em("火球術") + "多半拿來拆對手場上的怪 對手場上沒怪 傷害才會落到英雄身上",
                oncePerTurnWindow: false);
            return;
        }

        Card fieldCard = _manager.GetPlayerFieldCard();
        bool hasField = fieldCard != null;

        if (!hasField)
        {
            if (_manager.IsOpeningRoundFireballBlockedForPlayer() && HandContainsSpellOrdinal(0))
                ShowHint("opening_no_fireball", "第一回合火球還不能用先出怪獸吧");
            else
                ShowHint("turn_play", "這是你的回合試著出一張怪獸或法術");
            return;
        }

        if (ShouldSuggestHealBeforeEndTurn())
        {
            ShowHint("field_heal", "血量偏低可先打初級治療再按結束回合");
            return;
        }

        if (_manager.IsOpeningRoundFireballBlockedForPlayer())
        {
            ShowHint("opening_end_turn", "第一回合出完牌後按結束回合下回合怪獸才會攻擊");
            return;
        }

        ShowHint("end_turn", "場上有怪獸了按結束回合就會自動攻擊");
    }

    private bool ShouldSuggestHealBeforeEndTurn()
    {
        if (_manager == null || _manager.GetPlayerFieldCard() == null) return false;
        if (!HandContainsSpellOrdinal(1)) return false;

        int maxHp = _manager.GetHeroStartHealth();
        int playerHp = _manager.GetPlayerHeroHp();
        if (playerHp > Mathf.RoundToInt(maxHp * 0.72f)) return false;

        int handCount = _manager.GetPlayerHandCount();
        for (int i = 0; i < handCount; i++)
        {
            Card card = _manager.GetPlayerHandCard(i);
            if (card is SpellCard sp && sp.SpellOrdinal == 1)
                return true;
        }

        return false;
    }

    private bool HandContainsSpellOrdinal(int ordinal)
    {
        int count = _manager.GetPlayerHandCount();
        for (int i = 0; i < count; i++)
        {
            if (_manager.GetPlayerHandCard(i) is SpellCard sp && sp.SpellOrdinal == ordinal)
                return true;
        }

        return false;
    }

    private void ShowHint(string key, string message, bool oncePerTurnWindow = true)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (oncePerTurnWindow && _shownThisTurnWindow.Contains(key)) return;

        EnsureUi();
        if (_root == null || _bodyText == null) return;

        _root.SetActive(true);
        if (oncePerTurnWindow)
            _shownThisTurnWindow.Add(key);

        bool messageChanged = message != _lastHintMessage;
        if (key == _currentHintKey && _expanded && _typewriter != null && _typewriter.IsActive && !messageChanged)
            return;

        _currentHintKey = key;
        _lastHintMessage = message;
        _hasUnreadHint = true;

        if (key == "discard" && IsDiscardPhaseActive())
        {
            if (!_expanded)
                ExpandPanel();
            else
            {
                ApplyPanelLayout();
                if (messageChanged)
                    BeginHintTypewriter();
            }
        }
        else if (_expanded)
            BeginHintTypewriter();
        else
            SetBodyVisible(false);

        UpdateHandPlayHighlightsForHintKey(key);
    }

    private void UpdateHandPlayHighlightsForHintKey(string key)
    {
        switch (key)
        {
            case "turn_play":
            case "opening_no_fireball":
            case "field_heal":
            case "hand_full":
            case "enemy_monster_fireball":
                RequestHandPlayHighlights();
                break;
            case "discard":
                RequestHandDiscardHighlights();
                break;
            default:
                ClearHandPlayHighlights();
                break;
        }
    }

    private void RequestHandPlayHighlights()
    {
        if (_battleUi == null && _manager != null)
            _battleUi = _manager.GetComponent<BattleSimulationDebugUI>();
        _battleUi?.RequestTutorialHandPlayHighlights();
    }

    private void RequestHandDiscardHighlights()
    {
        if (_battleUi == null && _manager != null)
            _battleUi = _manager.GetComponent<BattleSimulationDebugUI>();
        _battleUi?.RequestTutorialHandDiscardHighlights();
    }

    private void ClearHandPlayHighlights()
    {
        if (_battleUi == null && _manager != null)
            _battleUi = _manager.GetComponent<BattleSimulationDebugUI>();
        _battleUi?.ClearTutorialHandPlayHighlights();
    }

    private void BeginHintTypewriter()
    {
        if (string.IsNullOrEmpty(_lastHintMessage) || _bodyText == null) return;
        SetBodyVisible(true);
        if (_expanded)
            FitExpandedPanelToContent(_lastHintMessage);
        _typewriter ??= new PlotDialogueTypewriter();
        _typewriter.Begin(_bodyText, _lastHintMessage, CoachCharactersPerSecond);
    }

    private void SetBodyVisible(bool visible)
    {
        if (_bodyText == null) return;
        _bodyText.gameObject.SetActive(visible);
        if (!visible)
            _bodyText.text = string.Empty;
    }

    private void ExpandPanel()
    {
        if (_expanded || _panelRt == null) return;
        _expanded = true;
        _hasUnreadHint = false;

        if (_backdrop != null)
        {
            _backdrop.SetActive(true);
            _backdrop.transform.SetAsLastSibling();
        }

        _root.transform.SetAsLastSibling();
        ApplyPanelLayout();
        BeginHintTypewriter();
    }

    private void CollapsePanel()
    {
        if (!_expanded || _panelRt == null) return;
        _expanded = false;
        if (_backdrop != null) _backdrop.SetActive(false);
        SetBodyVisible(false);
        ApplyPanelLayout();
    }

    private void ApplyPanelLayout()
    {
        if (_panelRt == null) return;

        bool discardPhase = IsDiscardPhaseActive();
        float portraitSize = _expanded ? ExpandedPortraitSize : CollapsedPortraitSize;
        float framePad = _expanded ? 6f : 5f;
        float portraitFrameSize = portraitSize + framePad * 2f;

        if (_expanded)
        {
            if (discardPhase)
                ApplyRightCenterPanelAnchor(DiscardExpandedPanelPosition);
            else
                ApplyLeftCenterPanelAnchor(ExpandedPanelPosition);
            LayoutExpandedHorizontal(portraitFrameSize, portraitSize);
            LayoutTapHint(false);
            FitExpandedPanelToContent(_lastHintMessage);
        }
        else
        {
            if (discardPhase)
                ApplyRightCenterPanelAnchor(DiscardCollapsedPanelPosition);
            else
                ApplyLeftCenterPanelAnchor(CollapsedPanelPosition);
            _panelRt.sizeDelta = CollapsedPanelSize;

            LayoutPortraitBlock(portraitFrameSize, portraitSize, -12f, portraitOnLeft: false);
            LayoutNameUnderPortrait(portraitFrameSize, -12f, 34f);
            LayoutTapHint(true);

            if (_bodyRt != null)
                _bodyRt.gameObject.SetActive(false);

            if (_speakerNameText != null)
            {
                _speakerNameText.fontSize = CollapsedNameFontSize;
                _speakerNameText.alignment = TextAlignmentOptions.Center;
            }
        }

        if (_borderGlowRt != null)
            _borderGlowRt.localScale = Vector3.one;
    }

    private void ApplyLeftCenterPanelAnchor(Vector2 anchoredPosition)
    {
        _panelRt.anchorMin = new Vector2(0f, 0.5f);
        _panelRt.anchorMax = new Vector2(0f, 0.5f);
        _panelRt.pivot = new Vector2(0f, 0.5f);
        _panelRt.anchoredPosition = ResolvePanelAnchoredPosition(anchoredPosition);
    }

    private void ApplyRightCenterPanelAnchor(Vector2 anchoredPosition)
    {
        _panelRt.anchorMin = new Vector2(1f, 0.5f);
        _panelRt.anchorMax = new Vector2(1f, 0.5f);
        _panelRt.pivot = new Vector2(1f, 0.5f);
        _panelRt.anchoredPosition = ResolveRightPanelAnchoredPosition(anchoredPosition);
    }

    private Vector2 ResolvePanelAnchoredPosition(Vector2 basePosition)
    {
        float x = basePosition.x + GetCanvasSafeAreaLeftInset();
        return new Vector2(x, basePosition.y);
    }

    private Vector2 ResolveRightPanelAnchoredPosition(Vector2 basePosition)
    {
        float x = basePosition.x - GetCanvasSafeAreaRightInset();
        return new Vector2(x, basePosition.y);
    }

    private float GetCanvasSafeAreaLeftInset()
    {
        Rect safe = Screen.safeArea;
        if (safe.xMin <= 0f) return 0f;

        Canvas canvas = _canvasRoot != null ? _canvasRoot.GetComponentInParent<Canvas>() : null;
        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        return safe.xMin / scale;
    }

    private float GetCanvasSafeAreaRightInset()
    {
        Rect safe = Screen.safeArea;
        float overflow = Screen.width - safe.xMax;
        if (overflow <= 0f) return 0f;

        Canvas canvas = _canvasRoot != null ? _canvasRoot.GetComponentInParent<Canvas>() : null;
        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        return overflow / scale;
    }

    private float GetExpandedContentLeftPad(float portraitFrameSize) =>
        ExpandedEdgePad + portraitFrameSize + ExpandedPortraitTextGap;

    private void LayoutExpandedHorizontal(float portraitFrameSize, float portraitSize)
    {
        float contentLeft = GetExpandedContentLeftPad(portraitFrameSize);
        const float nameBodyGap = 10f;

        LayoutPortraitBlock(portraitFrameSize, portraitSize, -ExpandedEdgePad, portraitOnLeft: true);

        if (_nameRt != null)
        {
            _nameRt.anchorMin = new Vector2(0f, 1f);
            _nameRt.anchorMax = new Vector2(1f, 1f);
            _nameRt.pivot = new Vector2(0.5f, 1f);
            _nameRt.offsetMin = new Vector2(contentLeft, 0f);
            _nameRt.offsetMax = new Vector2(-ExpandedEdgePad, 0f);
            _nameRt.sizeDelta = new Vector2(0f, ExpandedNameRowHeight);
            _nameRt.anchoredPosition = new Vector2(0f, -ExpandedEdgePad);
        }

        if (_speakerNameText != null)
        {
            _speakerNameText.fontSize = ExpandedNameFontSize;
            _speakerNameText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (_bodyRt != null)
        {
            _bodyRt.gameObject.SetActive(true);
            _bodyRt.anchorMin = new Vector2(0f, 0f);
            _bodyRt.anchorMax = new Vector2(1f, 1f);
            _bodyRt.pivot = new Vector2(0.5f, 0.5f);
            float bodyTop = ExpandedEdgePad + ExpandedNameRowHeight + nameBodyGap;
            _bodyRt.offsetMin = new Vector2(contentLeft, ExpandedEdgePad);
            _bodyRt.offsetMax = new Vector2(-ExpandedEdgePad, -bodyTop);
        }

        if (_bodyText != null)
        {
            _bodyText.fontSize = ExpandedBodyFontSize;
            _bodyText.lineSpacing = 6f;
            _bodyText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void FitExpandedPanelToContent(string message)
    {
        if (!_expanded || _panelRt == null || _bodyText == null) return;

        float portraitFrameSize = ExpandedPortraitSize + 12f;
        float contentLeft = GetExpandedContentLeftPad(portraitFrameSize);
        float bodyWidth = ExpandedPanelWidth - contentLeft - ExpandedEdgePad;
        const float nameBodyGap = 10f;

        string measureText = string.IsNullOrWhiteSpace(message) ? " " : message;
        _bodyText.fontSize = ExpandedBodyFontSize;
        Vector2 preferred = _bodyText.GetPreferredValues(measureText, bodyWidth, 0f);
        float bodyHeight = Mathf.Max(52f, preferred.y + 8f);

        float contentColumnHeight = ExpandedNameRowHeight + nameBodyGap + bodyHeight + ExpandedEdgePad;
        float panelHeight = Mathf.Max(
            ExpandedMinPanelHeight,
            Mathf.Max(portraitFrameSize + ExpandedEdgePad * 2f, contentColumnHeight + ExpandedEdgePad));

        RectTransform canvasRt = _canvasRoot as RectTransform;
        if (canvasRt != null)
        {
            float maxHeight = canvasRt.rect.height - ExpandedCanvasHeightMargin * 2f;
            if (maxHeight > ExpandedMinPanelHeight)
                panelHeight = Mathf.Min(panelHeight, maxHeight);
        }

        _panelRt.sizeDelta = new Vector2(ExpandedPanelWidth, panelHeight);
        ClampPanelWithinCanvas(IsDiscardPhaseActive());
    }

    private void ClampPanelWithinCanvas(bool discardPhase = false)
    {
        if (_panelRt == null || _canvasRoot is not RectTransform canvasRt) return;

        float canvasHalfHeight = canvasRt.rect.height * 0.5f;
        float panelHalfHeight = _panelRt.rect.height * 0.5f;
        float maxOffsetY = canvasHalfHeight - panelHalfHeight - ExpandedCanvasHeightMargin;
        if (maxOffsetY < 0f) maxOffsetY = 0f;

        Vector2 pos = _panelRt.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y, -maxOffsetY, maxOffsetY);
        _panelRt.anchoredPosition = pos;
    }

    private void LayoutPortraitBlock(float frameSize, float portraitSize, float topInset, bool portraitOnLeft)
    {
        if (portraitOnLeft)
        {
            float leftPad = _expanded ? ExpandedEdgePad : 14f;
            if (_portraitFrameRt != null)
            {
                _portraitFrameRt.anchorMin = new Vector2(0f, 1f);
                _portraitFrameRt.anchorMax = new Vector2(0f, 1f);
                _portraitFrameRt.pivot = new Vector2(0f, 1f);
                _portraitFrameRt.sizeDelta = new Vector2(frameSize, frameSize);
                _portraitFrameRt.anchoredPosition = new Vector2(leftPad, topInset);
            }

            if (_portraitRt != null)
            {
                _portraitRt.anchorMin = new Vector2(0.5f, 1f);
                _portraitRt.anchorMax = new Vector2(0.5f, 1f);
                _portraitRt.pivot = new Vector2(0.5f, 1f);
                _portraitRt.sizeDelta = new Vector2(portraitSize, portraitSize);
                _portraitRt.anchoredPosition = new Vector2(0f, -5f);
            }

            return;
        }

        if (_portraitFrameRt != null)
        {
            _portraitFrameRt.anchorMin = new Vector2(0.5f, 1f);
            _portraitFrameRt.anchorMax = new Vector2(0.5f, 1f);
            _portraitFrameRt.pivot = new Vector2(0.5f, 1f);
            _portraitFrameRt.sizeDelta = new Vector2(frameSize, frameSize);
            _portraitFrameRt.anchoredPosition = new Vector2(0f, topInset);
        }

        if (_portraitRt != null)
        {
            _portraitRt.anchorMin = new Vector2(0.5f, 1f);
            _portraitRt.anchorMax = new Vector2(0.5f, 1f);
            _portraitRt.pivot = new Vector2(0.5f, 1f);
            _portraitRt.sizeDelta = new Vector2(portraitSize, portraitSize);
            _portraitRt.anchoredPosition = new Vector2(0f, topInset - 5f);
        }
    }

    private void LayoutNameUnderPortrait(float frameSize, float topInset, float nameHeight)
    {
        if (_nameRt == null) return;
        float nameTop = topInset - frameSize - 4f;
        _nameRt.anchorMin = new Vector2(0f, 1f);
        _nameRt.anchorMax = new Vector2(1f, 1f);
        _nameRt.pivot = new Vector2(0.5f, 1f);
        _nameRt.sizeDelta = new Vector2(0f, nameHeight);
        _nameRt.anchoredPosition = new Vector2(0f, nameTop);
        if (_speakerNameText != null)
            _speakerNameText.alignment = TextAlignmentOptions.Center;
    }

    private void LayoutTapHint(bool visible)
    {
        if (_tapHintRt == null || _tapHintText == null) return;
        _tapHintText.gameObject.SetActive(visible);
        if (!visible) return;

        _tapHintRt.anchorMin = new Vector2(0f, 0f);
        _tapHintRt.anchorMax = new Vector2(1f, 0f);
        _tapHintRt.pivot = new Vector2(0.5f, 0f);
        _tapHintRt.sizeDelta = new Vector2(0f, 32f);
        _tapHintRt.anchoredPosition = new Vector2(0f, 12f);
        _tapHintText.fontSize = CollapsedTapHintFontSize;
        _tapHintText.alignment = TextAlignmentOptions.Center;
    }

    private void EnsureUi()
    {
        if (_uiBuilt) return;
        if (_canvasRoot == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas") ?? GameObject.Find("Canvas2") ?? GameObject.Find("Canva2");
            if (canvasObj != null) _canvasRoot = canvasObj.transform;
        }

        if (_canvasRoot == null) return;

        TMP_FontAsset font = ResolveCoachFont();

        _backdrop = CreateDismissBackdrop();
        _backdrop.SetActive(false);

        _root = new GameObject("TutorialBattleCoach", typeof(RectTransform));
        _root.transform.SetParent(_canvasRoot, false);
        _panelRt = _root.GetComponent<RectTransform>();

        Image panelBg = _root.AddComponent<Image>();
        panelBg.color = BattleUiColors.HallWine;
        panelBg.raycastTarget = true;

        _panelOutline = _root.AddComponent<Outline>();
        _panelOutline.effectColor = CoachBorderGold;
        _panelOutline.effectDistance = new Vector2(3f, -3f);

        Shadow panelShadow = _root.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        panelShadow.effectDistance = new Vector2(4f, -4f);

        Button panelButton = _root.AddComponent<Button>();
        panelButton.transition = Selectable.Transition.None;
        panelButton.targetGraphic = panelBg;
        panelButton.onClick.AddListener(OnPanelClicked);

        GameObject borderGlowObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
        borderGlowObj.transform.SetParent(_root.transform, false);
        borderGlowObj.transform.SetAsFirstSibling();
        _borderGlowRt = borderGlowObj.GetComponent<RectTransform>();
        StretchFull(_borderGlowRt, -8f);
        Image borderGlowImg = borderGlowObj.GetComponent<Image>();
        borderGlowImg.sprite = GetWhiteSprite();
        borderGlowImg.type = Image.Type.Sliced;
        borderGlowImg.color = CoachBorderGlow;
        borderGlowImg.raycastTarget = false;

        GameObject portraitFrameObj = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
        portraitFrameObj.transform.SetParent(_root.transform, false);
        _portraitFrameRt = portraitFrameObj.GetComponent<RectTransform>();
        Image portraitFrameImg = portraitFrameObj.GetComponent<Image>();
        portraitFrameImg.sprite = GetWhiteSprite();
        portraitFrameImg.color = PortraitFrameColor;
        portraitFrameImg.raycastTarget = false;

        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObj.transform.SetParent(portraitFrameObj.transform, false);
        _portraitRt = portraitObj.GetComponent<RectTransform>();
        StretchFull(_portraitRt, 4f);
        _portraitImage = portraitObj.GetComponent<Image>();
        _portraitImage.preserveAspect = true;
        _portraitImage.raycastTarget = false;
        ApplyPortraitSprite();

        GameObject nameObj = new GameObject("SpeakerName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(_root.transform, false);
        _nameRt = nameObj.GetComponent<RectTransform>();
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        _speakerNameText = nameTmp;
        if (font != null) nameTmp.font = font;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = BattleUiColors.TurnPlayer;
        nameTmp.text = "林可姐";
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.raycastTarget = false;

        GameObject tapHintObj = new GameObject("TapHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        tapHintObj.transform.SetParent(_root.transform, false);
        _tapHintRt = tapHintObj.GetComponent<RectTransform>();
        TextMeshProUGUI tapTmp = tapHintObj.GetComponent<TextMeshProUGUI>();
        _tapHintText = tapTmp;
        if (font != null) tapTmp.font = font;
        tapTmp.fontStyle = FontStyles.Bold;
        tapTmp.color = CoachBorderGold;
        tapTmp.text = "點擊查看提示";
        tapTmp.alignment = TextAlignmentOptions.Center;
        tapTmp.raycastTarget = false;

        GameObject bodyObj = new GameObject("CoachText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(_root.transform, false);
        _bodyRt = bodyObj.GetComponent<RectTransform>();
        _bodyRt.anchorMin = new Vector2(0f, 0f);
        _bodyRt.anchorMax = new Vector2(1f, 1f);
        _bodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        if (font != null) _bodyText.font = font;
        _bodyText.color = BattleUiColors.BtnPrimaryText;
        _bodyText.alignment = TextAlignmentOptions.TopLeft;
        _bodyText.enableWordWrapping = true;
        _bodyText.overflowMode = TextOverflowModes.Overflow;
        _bodyText.richText = true;
        _bodyText.raycastTarget = false;
        bodyObj.SetActive(false);

        _typewriter = new PlotDialogueTypewriter();
        _uiBuilt = true;
        _expanded = false;
        _discardLayoutActive = false;
        ApplyCoachFontToLabels();
        ApplyPanelLayout();
        _root.SetActive(false);
    }

    private void ApplyPortraitSprite()
    {
        if (_portraitImage == null) return;

        Sprite portrait = TutorialPlotScriptFactory.GetLinKePortraitSprite();
        if (portrait != null)
        {
            _portraitImage.sprite = portrait;
            _portraitImage.color = Color.white;
            return;
        }

        _portraitImage.sprite = GetPlaceholderPortraitSprite();
        _portraitImage.color = Color.white;
    }

    private static Sprite GetPlaceholderPortraitSprite()
    {
        if (placeholderPortraitSprite != null)
            return placeholderPortraitSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        Color32 fill = PortraitPlaceholderFill;
        Color32 frame = PortraitFrameColor;
        int border = 6;
        int innerBorder = 10;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool outer = x < border || y < border || x >= size - border || y >= size - border;
                bool inner = x < innerBorder || y < innerBorder || x >= size - innerBorder || y >= size - innerBorder;
                if (outer)
                    pixels[y * size + x] = frame;
                else if (inner)
                    pixels[y * size + x] = new Color32(210, 198, 178, 255);
                else
                    pixels[y * size + x] = fill;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        placeholderPortraitSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        return placeholderPortraitSprite;
    }

    private static Sprite whiteSprite;

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return whiteSprite;
    }

    private static void StretchFull(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private GameObject CreateDismissBackdrop()
    {
        GameObject backdrop = new GameObject("TutorialBattleCoachBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(_canvasRoot, false);
        RectTransform rt = backdrop.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = backdrop.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.22f);
        // 僅作視覺暗化；不可攔截射線，否則展開時無法直接操作手牌。
        img.raycastTarget = false;
        return backdrop;
    }

    private void OnPanelClicked()
    {
        if (_expanded)
            CollapsePanel();
        else
            ExpandPanel();
    }

    private void ApplyCoachFontToLabels()
    {
        TMP_FontAsset font = ResolveCoachFont();
        if (font == null) return;

        if (_speakerNameText != null)
        {
            _speakerNameText.font = font;
            _speakerNameText.text = "林可姐";
        }

        if (_tapHintText != null)
            _tapHintText.font = font;

        if (_bodyText != null)
            _bodyText.font = font;
    }

    private TMP_FontAsset ResolveCoachFont()
    {
        if (_preferredFont != null && BuildbeckUiFonts.FontSupportsText(_preferredFont, CoachFontProbe))
            return _preferredFont;

        TMP_FontAsset settingsFont = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settingsFont != null && BuildbeckUiFonts.FontSupportsText(settingsFont, CoachFontProbe))
            return settingsFont;

        TMP_FontAsset buildbeckFont = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeckFont != null && BuildbeckUiFonts.FontSupportsText(buildbeckFont, CoachFontProbe))
            return buildbeckFont;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset font = fonts[i];
            if (!BuildbeckUiFonts.FontSupportsText(font, CoachFontProbe)) continue;
            if (BuildbeckUiFonts.FontNameLikelySupportsCjk(font.name)) return font;
        }

        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI tmp = texts[i];
            if (tmp == null || tmp.font == null) continue;
            if (BuildbeckUiFonts.FontSupportsText(tmp.font, CoachFontProbe)) return tmp.font;
        }

        Debug.LogWarning("TutorialBattleCoachUi: 找不到支援中文的 TMP 字型，教戰提示可能無法顯示中文。");
        return _preferredFont ?? settingsFont ?? buildbeckFont;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>M-1-2 教練 UI：林可姐浮動視窗先講克制概念，再帶領實戰練習（階段 A 段考不提示）。</summary>
public sealed class M12BattleCoachUi : MonoBehaviour
{
    private const float ReEvaluateIntervalSeconds = 1.35f;

    private BattleSimulationManager _manager;
    private LinKeFloatingCoachPanel _panel;
    private Transform _canvasRoot;
    private TMP_FontAsset _preferredFont;
    private string _currentKey = string.Empty;
    private float _nextEvaluateUnscaled;
    private bool _eventsBound;
    private bool _lessonComplete;
    private bool _lessonStarted;
    private int _lessonStepIndex;
    private bool _wasPlayerTurn;
    private readonly HashSet<string> _shownThisTurnWindow = new HashSet<string>();

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsM12CoachPracticeBattle;

    public void Initialize(
        BattleSimulationManager manager,
        Transform canvasRoot,
        TMP_FontAsset uiFont = null)
    {
        _manager = manager;
        _canvasRoot = canvasRoot;
        _preferredFont = uiFont;
        _lessonComplete = false;
        _lessonStarted = false;
        _lessonStepIndex = 0;
        _wasPlayerTurn = false;
        _shownThisTurnWindow.Clear();
        EnsurePanel();
        if (_manager != null && !_eventsBound)
        {
            _manager.PlayerTurnActionWindowOpenedForPromptUi += OnPlayerTurnWindowOpened;
            _manager.PlayerCommittedHandCardToFieldFromHand += OnPlayerCommittedCard;
            _manager.PlayerPressedEndTurnForPromptUi += OnPlayerPressedEndTurn;
            _manager.BattleEnded += OnBattleEnded;
            _eventsBound = true;
        }
    }

    private void OnDestroy()
    {
        if (_panel != null)
            _panel.PanelAdvanceRequested -= OnLessonPanelAdvanceRequested;
        if (_eventsBound && _manager != null)
        {
            _manager.PlayerTurnActionWindowOpenedForPromptUi -= OnPlayerTurnWindowOpened;
            _manager.PlayerCommittedHandCardToFieldFromHand -= OnPlayerCommittedCard;
            _manager.PlayerPressedEndTurnForPromptUi -= OnPlayerPressedEndTurn;
            _manager.BattleEnded -= OnBattleEnded;
        }

        _eventsBound = false;
    }

    private void Update()
    {
        if (!ShouldRun())
            return;

        _panel?.Tick(Time.unscaledDeltaTime);

        if (_manager == null || _manager.IsBattleOver())
            return;

        if (_manager.IsOpeningPresentationInProgress() || BattleAutoSimPlugin.IsRunning)
            return;

        if (!_lessonComplete)
        {
            TryBeginLesson();
            return;
        }

        SyncDiscardLayout();
        TrackPlayerTurnChanges();

        if (!_manager.IsPlayerTurn())
            return;

        if (_manager.IsTurnSequenceInProgress() || _manager.IsSpellCastPresentationActive())
            return;

        if (Time.unscaledTime >= _nextEvaluateUnscaled)
            EvaluatePracticeHints();
    }

    private bool ShouldRun()
    {
        if (!IsActiveForCurrentBattle || _manager == null || BattleAutoSimPlugin.IsRunning)
        {
            _panel?.Hide();
            return false;
        }

        return true;
    }

    private void EnsurePanel()
    {
        if (_panel != null) return;
        _panel = GetComponent<LinKeFloatingCoachPanel>();
        if (_panel == null)
            _panel = gameObject.AddComponent<LinKeFloatingCoachPanel>();
        _panel.Initialize(_canvasRoot, _preferredFont, M12BattleCoachCatalog.SpeakerName);
        _panel.PanelAdvanceRequested += OnLessonPanelAdvanceRequested;
    }

    private void TryBeginLesson()
    {
        if (_lessonStarted || _manager == null) return;
        if (_manager.IsOpeningPresentationInProgress()) return;

        _lessonStarted = true;
        _panel.PanelClickMode = LinKeFloatingCoachPanel.ClickMode.TapToAdvance;
        ShowCurrentLessonStep();
    }

    private void ShowCurrentLessonStep()
    {
        if (!M12BattleCoachCatalog.TryGetLessonStep(_lessonStepIndex, out _, out string message))
            return;

        _panel.ShowHint(message, forceExpand: true);
    }

    private void OnLessonPanelAdvanceRequested()
    {
        if (_lessonComplete || !_lessonStarted) return;
        if (_panel.IsTypewriterActive) return;

        _lessonStepIndex++;
        if (_lessonStepIndex >= M12BattleCoachCatalog.LessonStepCount)
        {
            CompleteLesson();
            return;
        }

        ShowCurrentLessonStep();
    }

    private void CompleteLesson()
    {
        _lessonComplete = true;
        _panel.PanelClickMode = LinKeFloatingCoachPanel.ClickMode.ToggleExpand;
        _panel.CollapsePanel();
        _currentKey = string.Empty;
        _shownThisTurnWindow.Clear();
        ScheduleEvaluate(0.15f);
    }

    private void SyncDiscardLayout()
    {
        if (_panel == null || _manager == null) return;
        bool discard = _manager.IsPlayerInDiscardSelection() || _manager.GetPlayerPendingDiscardCount() > 0;
        _panel.SetDiscardPhaseActive(discard);
    }

    private void TrackPlayerTurnChanges()
    {
        bool isPlayerTurn = _manager.IsPlayerTurn();
        if (isPlayerTurn == _wasPlayerTurn) return;

        _wasPlayerTurn = isPlayerTurn;
        if (isPlayerTurn)
        {
            _shownThisTurnWindow.Clear();
            _currentKey = string.Empty;
            ScheduleEvaluate(0.12f);
        }
        else
        {
            ShowPracticeHint("enemy_turn", "敵方在行動等他打完再輪到你", oncePerTurnWindow: false);
        }
    }

    private void OnPlayerTurnWindowOpened()
    {
        _currentKey = string.Empty;
        _shownThisTurnWindow.Clear();
        ScheduleEvaluate(0.12f);
    }

    private void OnPlayerCommittedCard() => ScheduleEvaluate(0.2f);

    private void OnPlayerPressedEndTurn()
    {
        _panel?.Hide();
    }

    private void OnBattleEnded(int result)
    {
        _panel?.Hide();
    }

    public void HideForSettlement()
    {
        _panel?.Hide();
    }

    private void ScheduleEvaluate(float delay) =>
        _nextEvaluateUnscaled = Time.unscaledTime + Mathf.Max(0f, delay);

    private void EvaluatePracticeHints()
    {
        _nextEvaluateUnscaled = Time.unscaledTime + ReEvaluateIntervalSeconds;
        if (_manager == null || !_manager.IsPlayerTurn() || _manager.IsBattleOver())
            return;

        bool ok = M12BattleCoachCatalog.TryEvaluatePhaseB(_manager, out string key, out string message);
        if (!ok || string.IsNullOrWhiteSpace(message))
        {
            _panel?.Hide();
            return;
        }

        bool forceExpand = key == "discard";
        ShowPracticeHint(key, message, oncePerTurnWindow: true, forceExpand: forceExpand);
    }

    private void ShowPracticeHint(string key, string message, bool oncePerTurnWindow, bool forceExpand = false)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (oncePerTurnWindow && _shownThisTurnWindow.Contains(key)) return;

        EnsurePanel();
        if (oncePerTurnWindow)
            _shownThisTurnWindow.Add(key);

        if (key == _currentKey && _panel.IsExpanded && _panel.IsTypewriterActive)
            return;

        _currentKey = key;
        _panel.ShowHint(message, forceExpand);
    }
}

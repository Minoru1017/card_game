using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>Cross-scene handoff for Story progress ↔ Main Plot ↔ tutorial battle.</summary>
public static class StoryProgressSession
{
    public const string StoryProgressSceneName = "Story progress";
    public const string MainPlotSceneName = "Main Plot";
    public const string HallSceneName = "hall";

    private static List<MainPlotSceneController.PlotStep> pendingPlotSteps;
    private static bool launchTutorialBattleAfterPlot;
    private static bool tutorialPlotEpilogueActive;
    private static bool harborCombatClearBridgeActive;
    private static bool m12IntroPlotActive;
    private static bool m12MidPatrolPlotActive;
    private static bool m12VictoryEpilogueActive;
    private static bool launchM12PhaseABattleAfterPlot;
    private static bool launchM12PhaseBBattleAfterPlot;
    private static bool m13OpeningPlotActive;
    private static bool m13RoseTrialPlotActive;
    private static bool m13EpiloguePlotActive;
    private static bool launchM13PhaseBBattleAfterPlot;
    private static bool tutorialPlotBgmRequested;
    private static bool m12PlotBgmRequested;
    private static bool m13PlotBgmRequested;
    private static bool a1HarborPlotActive;
    private static bool a1IslandIntroPlotActive;
    private static bool a1UnsealPlotActive;
    private static bool a1ReturnPlotActive;
    private static bool a1QuestSessionActive;
    private static bool a1HarborCancelled;
    private static bool a1SkipFarm;
    private static SideQuestA1TideMarkRewardService.FarmOutcome a1PendingFarmOutcome;
    private static bool a1PendingKeptSeaPurslaneSeed;
    private static SideQuestA1TideMarkRewardService.ApplyResult a1PendingApplyResult;
    private static bool a1PendingApplyResultSet;
    private static string pendingMapFocusNodeId;
    private static bool pendingEnterMapFocusMode;
    private static bool pendingM13ContinueAfterBirdDuel;

    public const string SeawallPatrolNodeId = "M-1-2";
    public const string RiverForkNodeId = "M-1-3";
    public const string TideIslandSideNodeId = "S-A-1";

    /// <summary>教學戰勝利後的 Main Plot 結尾劇情進行中。</summary>
    public static bool IsTutorialPlotEpilogueActive => tutorialPlotEpilogueActive;

    /// <summary>港灣實戰首通後、回 Story progress 前的短銜接劇情。</summary>
    public static bool IsHarborCombatClearBridgeActive => harborCombatClearBridgeActive;

    public static bool IsM12IntroPlotActive => m12IntroPlotActive;

    public static bool IsM12MidPatrolPlotActive => m12MidPatrolPlotActive;

    public static bool IsM12VictoryEpilogueActive => m12VictoryEpilogueActive;

    public static bool IsM13OpeningPlotActive => m13OpeningPlotActive;

    public static bool IsM13RoseTrialPlotActive => m13RoseTrialPlotActive;

    public static bool IsM13EpiloguePlotActive => m13EpiloguePlotActive;

    public static bool IsA1HarborPlotActive => a1HarborPlotActive;

    public static bool IsA1IslandIntroPlotActive => a1IslandIntroPlotActive;

    public static bool IsA1UnsealPlotActive => a1UnsealPlotActive;

    public static bool IsA1ReturnPlotActive => a1ReturnPlotActive;

    public static bool IsA1QuestSessionActive => a1QuestSessionActive;

    public static bool TryConsumeLaunchM12PhaseABattleAfterPlot()
    {
        bool launch = launchM12PhaseABattleAfterPlot;
        launchM12PhaseABattleAfterPlot = false;
        return launch;
    }

    public static bool TryConsumeLaunchM12PhaseBBattleAfterPlot()
    {
        bool launch = launchM12PhaseBBattleAfterPlot;
        launchM12PhaseBBattleAfterPlot = false;
        return launch;
    }

    public static bool TryConsumeLaunchM13PhaseBBattleAfterPlot()
    {
        bool launch = launchM13PhaseBBattleAfterPlot;
        launchM13PhaseBBattleAfterPlot = false;
        return launch;
    }

    public static void QueueM13ContinueAfterBirdDuel() => pendingM13ContinueAfterBirdDuel = true;

    public static bool TryConsumeM13ContinueAfterBirdDuel()
    {
        bool pending = pendingM13ContinueAfterBirdDuel;
        pendingM13ContinueAfterBirdDuel = false;
        return pending;
    }

    /// <summary>1-1 劇情應播放 Enchanted Valley BGM（進入 Main Plot 至劇情結束）。</summary>
    public static bool TutorialPlotBgmRequested => tutorialPlotBgmRequested;

    /// <summary>M-1-2 開場／中段散策前短劇／通關終幕劇情應播放 HYPERCRUSH BGM（不含散策）。</summary>
    public static bool M12PlotBgmRequested => m12PlotBgmRequested;

    /// <summary>M-1-3 開場／玫瑰試煉／終幕劇情應播放 Bait BGM。</summary>
    public static bool M13PlotBgmRequested => m13PlotBgmRequested;

    public static void ClearTutorialPlotBgmRequest() => tutorialPlotBgmRequested = false;

    public static void ClearM12PlotBgmRequest() => m12PlotBgmRequested = false;

    public static void ClearM13PlotBgmRequest() => m13PlotBgmRequested = false;

    /// <summary>離開劇情或載入非 Main Plot 場景時呼叫，停止 BGM 並清除請求旗標。</summary>
    public static void EndPlotBgmSession()
    {
        ClearPlotBgmRequestFlags();
        PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
    }

    private enum PlotLaunchTeardownScope
    {
        StoryProgressMusicOnly,
        IncludeBattleBgms,
    }

    /// <summary>進入 Main Plot 或銜接劇情前：清對戰旗標並停止 Story／對戰 BGM。</summary>
    private static void ResetForPlotLaunch(PlotLaunchTeardownScope teardownScope)
    {
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressBackgroundMusicPlayer.StopAll();
        if (teardownScope == PlotLaunchTeardownScope.IncludeBattleBgms)
        {
            TutorialBattleBackgroundMusicPlayer.StopAll();
            FreeBattleBackgroundMusicPlayer.StopAll();
        }
    }

    private static void ClearPlotBgmRequestFlags()
    {
        tutorialPlotBgmRequested = false;
        m12PlotBgmRequested = false;
        m13PlotBgmRequested = false;
    }

    private static void SetPlotBgmRequest(bool tutorial, bool m12, bool m13)
    {
        tutorialPlotBgmRequested = tutorial;
        m12PlotBgmRequested = m12;
        m13PlotBgmRequested = m13;
    }

    private static void ResetCrossChapterBridgeFlags()
    {
        tutorialPlotEpilogueActive = false;
        harborCombatClearBridgeActive = false;
        launchTutorialBattleAfterPlot = false;
    }

    private static void ResetM12PlotSessionFlags()
    {
        m12IntroPlotActive = false;
        m12MidPatrolPlotActive = false;
        m12VictoryEpilogueActive = false;
        launchM12PhaseABattleAfterPlot = false;
        launchM12PhaseBBattleAfterPlot = false;
    }

    private static void ResetM13PlotSessionFlags()
    {
        m13OpeningPlotActive = false;
        m13RoseTrialPlotActive = false;
        m13EpiloguePlotActive = false;
        launchM13PhaseBBattleAfterPlot = false;
    }

    private static void ResetA1PlotSessionFlags()
    {
        a1HarborPlotActive = false;
        a1IslandIntroPlotActive = false;
        a1UnsealPlotActive = false;
        a1ReturnPlotActive = false;
        a1QuestSessionActive = false;
        a1HarborCancelled = false;
        a1SkipFarm = false;
        a1PendingFarmOutcome = SideQuestA1TideMarkRewardService.FarmOutcome.Skipped;
        a1PendingKeptSeaPurslaneSeed = false;
        a1PendingApplyResult = default;
        a1PendingApplyResultSet = false;
    }

    /// <summary>離開 1-1 劇情或載入非 Main Plot 場景時呼叫，停止 BGM 並清除請求旗標。</summary>
    public static void EndTutorialPlotBgmSession() => EndPlotBgmSession();

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterTutorialPlotBgmSceneGuard()
    {
        SceneManager.sceneUnloaded -= OnPlotSceneUnloaded;
        SceneManager.sceneUnloaded += OnPlotSceneUnloaded;
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private static void OnPlotSceneUnloaded(Scene scene)
    {
        if (scene.name != MainPlotSceneName)
            return;

        EndPlotBgmSession();
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MainPlotSceneName)
        {
            if (!tutorialPlotBgmRequested && !m12PlotBgmRequested && !m13PlotBgmRequested)
                PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
            return;
        }

        EndPlotBgmSession();
    }

    public static void SetPendingPlotSteps(List<MainPlotSceneController.PlotStep> steps) =>
        pendingPlotSteps = steps;

    /// <summary>劇情結束（含略過）後是否直接進入 1-1 教學對戰。</summary>
    public static bool TryConsumeLaunchTutorialBattleAfterPlot()
    {
        bool launch = launchTutorialBattleAfterPlot;
        launchTutorialBattleAfterPlot = false;
        return launch;
    }

    public static bool TryConsumePendingPlotSteps(out List<MainPlotSceneController.PlotStep> steps)
    {
        steps = pendingPlotSteps;
        pendingPlotSteps = null;
        return steps != null && steps.Count > 0;
    }

    public static void LaunchTutorialPlotScene(bool battleAfterPlot = false)
    {
        StoryProgressBackgroundMusicPlayer.StopAll();
        launchTutorialBattleAfterPlot = battleAfterPlot;
        SetPlotBgmRequest(tutorial: true, m12: false, m13: false);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildTutorialPlotSteps());
        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(MainPlotSceneName))
        {
            UnityEngine.Debug.LogError("StoryProgressSession: cannot load Main Plot — add scene to Build Settings.");
            launchTutorialBattleAfterPlot = false;
            ClearPlotBgmRequestFlags();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(MainPlotSceneName);
    }

    public static void LaunchTutorialBattleAfterPlot(bool fastCloseAnimation = false)
    {
        TutorialPlotBattleTransition.PlayFromPlotToBattle(fastCloseAnimation: fastCloseAnimation);
    }

    /// <summary>教學戰勝利結算「繼續」後：播放結尾劇情，結束後回 Story progress。</summary>
    public static void LaunchTutorialPlotEpilogueAfterVictory()
    {
        tutorialPlotEpilogueActive = true;
        launchTutorialBattleAfterPlot = false;
        NotifyTutorialBattleFinished(won: true);
        ResetForPlotLaunch(PlotLaunchTeardownScope.IncludeBattleBgms);
        SetPlotBgmRequest(tutorial: true, m12: false, m13: false);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildTutorialPlotEpilogueSteps());

        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(MainPlotSceneName))
        {
            UnityEngine.Debug.LogError(
                "StoryProgressSession: cannot load Main Plot for epilogue — add scene to Build Settings.");
            tutorialPlotEpilogueActive = false;
            ClearPlotBgmRequestFlags();
            LoadStoryProgressFallback();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(MainPlotSceneName);
    }

    public static void EndTutorialPlotEpilogueSession()
    {
        tutorialPlotEpilogueActive = false;
        EndPlotBgmSession();
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        NotifyTutorialChapterFullyCompleted();
    }

    /// <summary>港灣實戰首通結算「返回地圖」：Main Plot 短台詞 → 回 Story progress 並聚焦 M-1-2。</summary>
    public static void LaunchHarborCombatClearBridgeAfterFirstVictory()
    {
        harborCombatClearBridgeActive = true;
        ResetCrossChapterBridgeFlags();
        ClearPlotBgmRequestFlags();
        ResetForPlotLaunch(PlotLaunchTeardownScope.IncludeBattleBgms);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildHarborCombatClearBridgeSteps());

        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(MainPlotSceneName))
        {
            UnityEngine.Debug.LogError(
                "StoryProgressSession: cannot load Main Plot for harbor clear bridge — add scene to Build Settings.");
            harborCombatClearBridgeActive = false;
            QueuePostHarborClearMapFocus();
            LoadStoryProgressWithIrisTransition();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(MainPlotSceneName);
    }

    public static void EndHarborCombatClearBridgeSession()
    {
        harborCombatClearBridgeActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        QueuePostHarborClearMapFocus();
    }

    public static void LaunchM12IntroPlotScene()
    {
        m12IntroPlotActive = true;
        m12MidPatrolPlotActive = false;
        m12VictoryEpilogueActive = false;
        launchM12PhaseABattleAfterPlot = true;
        launchM12PhaseBBattleAfterPlot = false;
        ResetCrossChapterBridgeFlags();
        ResetM13PlotSessionFlags();
        SetPlotBgmRequest(tutorial: false, m12: true, m13: false);
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM12IntroPlotSteps());
        LoadMainPlotOrFallback(() =>
        {
            m12IntroPlotActive = false;
            m12PlotBgmRequested = false;
        });
    }

    public static void LaunchM12MidPatrolPlotScene()
    {
        m12MidPatrolPlotActive = true;
        m12IntroPlotActive = false;
        m12VictoryEpilogueActive = false;
        launchM12PhaseBBattleAfterPlot = true;
        launchM12PhaseABattleAfterPlot = false;
        ResetCrossChapterBridgeFlags();
        ResetM13PlotSessionFlags();
        SetPlotBgmRequest(tutorial: false, m12: true, m13: false);
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM12MidPatrolPlotSteps());
        LoadMainPlotOrFallback(() =>
        {
            m12MidPatrolPlotActive = false;
            m12PlotBgmRequested = false;
        });
    }

    public static void LaunchM12VictoryEpiloguePlotScene()
    {
        m12VictoryEpilogueActive = true;
        m12IntroPlotActive = false;
        m12MidPatrolPlotActive = false;
        launchM12PhaseABattleAfterPlot = false;
        launchM12PhaseBBattleAfterPlot = false;
        ResetCrossChapterBridgeFlags();
        ResetM13PlotSessionFlags();
        SetPlotBgmRequest(tutorial: false, m12: true, m13: false);
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM12VictoryEpilogueSteps());
        LoadMainPlotOrFallback(() =>
        {
            m12VictoryEpilogueActive = false;
            m12PlotBgmRequested = false;
        });
    }

    public static void LaunchM12PhaseABattleAfterPlot(bool fastCloseAnimation = false) =>
        M12PlotBattleTransition.PlayFromPlotToPhaseABattle(fastCloseAnimation);

    public static void LaunchM12PhaseBBattleAfterPlot(bool fastCloseAnimation = false) =>
        M12PlotBattleTransition.PlayFromPlotToPhaseBBattle(fastCloseAnimation);

    public static void LaunchM13PhaseBBattleAfterPlot(bool fastCloseAnimation = false) =>
        M13PlotBattleTransition.PlayFromPlotToPhaseBBattle(fastCloseAnimation);

    public static void EndM12IntroPlotSession()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.SetM12IntroSeen(slot, true);
        m12IntroPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndM12MidPatrolPlotSession()
    {
        m12MidPatrolPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndM12VictoryEpilogueSession()
    {
        m12VictoryEpilogueActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        pendingMapFocusNodeId = SeawallPatrolNodeId;
    }

    public static void LaunchM13OpeningPlotScene()
    {
        m13OpeningPlotActive = true;
        m13RoseTrialPlotActive = false;
        m13EpiloguePlotActive = false;
        ResetM12PlotSessionFlags();
        ResetCrossChapterBridgeFlags();
        SetPlotBgmRequest(tutorial: false, m12: false, m13: true);
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM13OpeningPlotSteps());
        LoadMainPlotOrFallback(() => m13OpeningPlotActive = false);
    }

    public static void LaunchM13RoseTrialPlotScene()
    {
        m13RoseTrialPlotActive = true;
        m13OpeningPlotActive = false;
        m13EpiloguePlotActive = false;
        ResetM12PlotSessionFlags();
        ResetCrossChapterBridgeFlags();
        launchM13PhaseBBattleAfterPlot = true;
        SetPlotBgmRequest(tutorial: false, m12: false, m13: true);
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM13RoseTrialPlotSteps());
        LoadMainPlotOrFallback(() => m13RoseTrialPlotActive = false);
    }

    public static void LaunchM13EpiloguePlotScene()
    {
        m13EpiloguePlotActive = true;
        m13OpeningPlotActive = false;
        m13RoseTrialPlotActive = false;
        ResetM12PlotSessionFlags();
        ResetCrossChapterBridgeFlags();
        launchM13PhaseBBattleAfterPlot = false;
        SetPlotBgmRequest(tutorial: false, m12: false, m13: true);
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM13EpiloguePlotSteps());
        LoadMainPlotOrFallback(() => m13EpiloguePlotActive = false);
    }

    public static void EndM13OpeningPlotSession()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M13RiverForkProgressState.MarkOpeningSeen(slot);
        m13OpeningPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndM13RoseTrialPlotSession()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M13RiverForkProgressState.MarkRoseTrialSeen(slot);
        m13RoseTrialPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndM13EpiloguePlotSession()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M13RiverForkRewardService.TryGrantRiverForkReward();
        M13RiverForkProgressState.MarkNodeCleared(slot);
        m13EpiloguePlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        pendingMapFocusNodeId = RiverForkNodeId;
    }

    public static void LaunchA1HarborPlotScene()
    {
        ResetA1PlotSessionFlags();
        ResetM12PlotSessionFlags();
        ResetM13PlotSessionFlags();
        ResetCrossChapterBridgeFlags();
        ClearPlotBgmRequestFlags();
        a1QuestSessionActive = true;
        a1HarborPlotActive = true;
        ResetForPlotLaunch(PlotLaunchTeardownScope.StoryProgressMusicOnly);
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildA1HarborPlotSteps());
        LoadMainPlotOrFallback(() =>
        {
            a1HarborPlotActive = false;
        });
    }

    public static void LaunchA1IslandIntroPlotInPlace(int slot)
    {
        a1IslandIntroPlotActive = true;
        a1HarborPlotActive = false;
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildA1IslandIntroPlotSteps(slot));
        TryBeginA1PlotInPlace();
    }

    public static void LaunchA1UnsealPlotInPlace()
    {
        a1UnsealPlotActive = true;
        a1IslandIntroPlotActive = false;
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildA1UnsealPlotSteps());
        TryBeginA1PlotInPlace();
    }

    public static void LaunchA1ReturnPlotInPlace(bool keptSeaPurslaneSeed)
    {
        a1ReturnPlotActive = true;
        a1UnsealPlotActive = false;
        a1IslandIntroPlotActive = false;
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildA1ReturnPlotSteps(keptSeaPurslaneSeed));
        TryBeginA1PlotInPlace();
    }

    public static void EndA1HarborPlotSession()
    {
        a1HarborPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndA1IslandIntroPlotSession()
    {
        a1IslandIntroPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndA1UnsealPlotSession()
    {
        a1UnsealPlotActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    public static void EndA1ReturnPlotSession()
    {
        a1ReturnPlotActive = false;
        a1QuestSessionActive = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        pendingMapFocusNodeId = TideIslandSideNodeId;
    }

    public static void CancelA1QuestSession() => ResetA1PlotSessionFlags();

    /// <summary>回港短劇結束但 session 旗標遺失時的後備判定。</summary>
    public static bool ShouldCompleteA1ReturnPlot() =>
        a1PendingApplyResultSet &&
        !a1HarborPlotActive &&
        !a1IslandIntroPlotActive &&
        !a1UnsealPlotActive;

    public static bool ConsumeA1HarborCancelled()
    {
        bool cancelled = a1HarborCancelled;
        a1HarborCancelled = false;
        return cancelled;
    }

    public static bool ConsumeA1SkipFarm()
    {
        bool skip = a1SkipFarm;
        a1SkipFarm = false;
        return skip;
    }

    public static void SetA1PendingFarmResult(
        SideQuestA1TideMarkRewardService.FarmOutcome outcome,
        bool keptSeaPurslaneSeed)
    {
        a1PendingFarmOutcome = outcome;
        a1PendingKeptSeaPurslaneSeed = keptSeaPurslaneSeed;
    }

    public static bool TryConsumeA1PendingFarmResult(
        out SideQuestA1TideMarkRewardService.FarmOutcome outcome,
        out bool keptSeaPurslaneSeed)
    {
        outcome = a1PendingFarmOutcome;
        keptSeaPurslaneSeed = a1PendingKeptSeaPurslaneSeed;
        a1PendingFarmOutcome = SideQuestA1TideMarkRewardService.FarmOutcome.Skipped;
        a1PendingKeptSeaPurslaneSeed = false;
        return true;
    }

    public static void SetA1PendingApplyResult(SideQuestA1TideMarkRewardService.ApplyResult apply)
    {
        a1PendingApplyResult = apply;
        a1PendingApplyResultSet = true;
    }

    public static bool TryConsumeA1PendingApplyResult(out SideQuestA1TideMarkRewardService.ApplyResult apply)
    {
        apply = a1PendingApplyResult;
        bool had = a1PendingApplyResultSet;
        a1PendingApplyResult = default;
        a1PendingApplyResultSet = false;
        return had;
    }

    public static void NotifyA1PlotChoice(int stepIndex, int choiceIndex)
    {
        if (stepIndex == TutorialPlotScriptFactory.A1HarborLaunchChoiceStepIndex)
        {
            if (choiceIndex == 1)
                a1HarborCancelled = true;
            return;
        }

        if (stepIndex != TutorialPlotScriptFactory.A1IslandFarmChoiceStepIndex)
            return;

        if (choiceIndex == 1)
            a1SkipFarm = true;
    }

    private static void TryBeginA1PlotInPlace()
    {
        if (!TryConsumePendingPlotSteps(out List<MainPlotSceneController.PlotStep> steps))
            return;

        MainPlotSceneController ctrl =
            UnityEngine.Object.FindFirstObjectByType<MainPlotSceneController>();
        if (ctrl == null)
        {
            UnityEngine.Debug.LogError("StoryProgressSession: Main Plot controller missing for A-1 handoff.");
            SetPendingPlotSteps(steps);
            LoadMainPlotOrFallback(null);
            return;
        }

        ctrl.ApplyRuntimeSteps(steps, true);
        ctrl.ClearTapToContinueUiRefs();
        ctrl.BeginPlot();
    }

    public static void NotifyM13PlotChoice(int stepIndex, int choiceIndex)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (stepIndex == TutorialPlotScriptFactory.M13OpeningOathChoiceStepIndex)
            return;

        if (stepIndex != TutorialPlotScriptFactory.M13RoseTrialChoiceStepIndex)
            return;

        switch (choiceIndex)
        {
            case 0:
                TutorialProgressState.SetM13RoseIntact(slot, true);
                break;
            case 1:
                TutorialProgressState.SetM13RoseBurned(slot, true);
                break;
            case 2:
                TutorialProgressState.SetM13RoseBurned(slot, true);
                TutorialProgressState.SetM13PlayerDemandedMiracle(slot, true);
                break;
        }
    }

    private static void LoadMainPlotOrFallback(System.Action onFail)
    {
        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(MainPlotSceneName))
        {
            UnityEngine.Debug.LogError("StoryProgressSession: cannot load Main Plot.");
            onFail?.Invoke();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(MainPlotSceneName);
    }

    public static bool TryConsumePendingMapFocusNodeId(out string nodeId)
    {
        nodeId = pendingMapFocusNodeId;
        pendingMapFocusNodeId = null;
        return !string.IsNullOrWhiteSpace(nodeId);
    }

    public static bool TryConsumePendingEnterMapFocusMode()
    {
        bool enter = pendingEnterMapFocusMode;
        pendingEnterMapFocusMode = false;
        return enter;
    }

    private static void QueuePostHarborClearMapFocus()
    {
        pendingMapFocusNodeId = SeawallPatrolNodeId;
        pendingEnterMapFocusMode = true;
    }

    private static void LoadStoryProgressFallback()
    {
        if (UnityEngine.Application.CanStreamedLevelBeLoaded(StoryProgressSceneName))
            TutorialPlotBattleTransition.PlayToStoryProgress();
    }

    /// <summary>1-1 教學戰結束（含結尾劇情後）回到 Story progress：光圈縮小 → 載入 → 光圈打開。</summary>
    public static void LoadStoryProgressWithIrisTransition(bool fastClose = false)
    {
        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(StoryProgressSceneName))
        {
            UnityEngine.Debug.LogError("StoryProgressSession: cannot load Story progress — add scene to Build Settings.");
            return;
        }

        TutorialPlotBattleTransition.CancelIfPlaying();
        TutorialPlotBattleTransition.PlayToStoryProgress(fastClose);
    }

    public static void NotifyTutorialPlotFinished()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.SetTutorialPlotCompleted(slot, true);
    }

    public static void NotifyTutorialBattleFinished(bool won)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (won)
            TutorialProgressState.SetTutorialBattleCompleted(slot, true);
    }

    /// <summary>結尾劇情結束或通關回 Story progress 時，確保 1-1 劇情與教學戰皆標記完成。</summary>
    public static void NotifyTutorialChapterFullyCompleted()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.SetTutorialPlotCompleted(slot, true);
        TutorialProgressState.SetTutorialBattleCompleted(slot, true);
        if (TutorialProgressState.IsIntroTrioRewardGranted(slot) ||
            TutorialProgressState.IsTutorialBattleCompleted(slot))
            TutorialProgressState.SetIntroTrioRewardGranted(slot, true);
        TutorialProgressState.PersistAcademyIntroGraduated(slot);
    }
}

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
    private static bool tutorialPlotBgmRequested;
    private static string pendingMapFocusNodeId;
    private static bool pendingEnterMapFocusMode;

    public const string SeawallPatrolNodeId = "M-1-2";

    /// <summary>教學戰勝利後的 Main Plot 結尾劇情進行中。</summary>
    public static bool IsTutorialPlotEpilogueActive => tutorialPlotEpilogueActive;

    /// <summary>港灣實戰首通後、回 Story progress 前的短銜接劇情。</summary>
    public static bool IsHarborCombatClearBridgeActive => harborCombatClearBridgeActive;

    public static bool IsM12IntroPlotActive => m12IntroPlotActive;

    public static bool IsM12MidPatrolPlotActive => m12MidPatrolPlotActive;

    public static bool IsM12VictoryEpilogueActive => m12VictoryEpilogueActive;

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

    /// <summary>1-1 劇情應播放 Enchanted Valley BGM（進入 Main Plot 至劇情結束）。</summary>
    public static bool TutorialPlotBgmRequested => tutorialPlotBgmRequested;

    public static void ClearTutorialPlotBgmRequest() => tutorialPlotBgmRequested = false;

    /// <summary>離開 1-1 劇情或載入非 Main Plot 場景時呼叫，停止 BGM 並清除請求旗標。</summary>
    public static void EndTutorialPlotBgmSession()
    {
        tutorialPlotBgmRequested = false;
        PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
    }

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

        EndTutorialPlotBgmSession();
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MainPlotSceneName)
        {
            if (!tutorialPlotBgmRequested)
                PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
            return;
        }

        EndTutorialPlotBgmSession();
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
        tutorialPlotBgmRequested = true;
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildTutorialPlotSteps());
        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(MainPlotSceneName))
        {
            UnityEngine.Debug.LogError("StoryProgressSession: cannot load Main Plot — add scene to Build Settings.");
            launchTutorialBattleAfterPlot = false;
            tutorialPlotBgmRequested = false;
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
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressBackgroundMusicPlayer.StopAll();
        TutorialBattleBackgroundMusicPlayer.StopAll();
        FreeBattleBackgroundMusicPlayer.StopAll();
        tutorialPlotBgmRequested = true;
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildTutorialPlotEpilogueSteps());

        if (!UnityEngine.Application.CanStreamedLevelBeLoaded(MainPlotSceneName))
        {
            UnityEngine.Debug.LogError(
                "StoryProgressSession: cannot load Main Plot for epilogue — add scene to Build Settings.");
            tutorialPlotEpilogueActive = false;
            tutorialPlotBgmRequested = false;
            LoadStoryProgressFallback();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(MainPlotSceneName);
    }

    public static void EndTutorialPlotEpilogueSession()
    {
        tutorialPlotEpilogueActive = false;
        EndTutorialPlotBgmSession();
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        NotifyTutorialChapterFullyCompleted();
    }

    /// <summary>港灣實戰首通結算「返回地圖」：Main Plot 短台詞 → 回 Story progress 並聚焦 M-1-2。</summary>
    public static void LaunchHarborCombatClearBridgeAfterFirstVictory()
    {
        harborCombatClearBridgeActive = true;
        tutorialPlotEpilogueActive = false;
        launchTutorialBattleAfterPlot = false;
        tutorialPlotBgmRequested = false;
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressBackgroundMusicPlayer.StopAll();
        TutorialBattleBackgroundMusicPlayer.StopAll();
        FreeBattleBackgroundMusicPlayer.StopAll();
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
        tutorialPlotEpilogueActive = false;
        harborCombatClearBridgeActive = false;
        launchTutorialBattleAfterPlot = false;
        tutorialPlotBgmRequested = false;
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressBackgroundMusicPlayer.StopAll();
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM12IntroPlotSteps());
        LoadMainPlotOrFallback(() => m12IntroPlotActive = false);
    }

    public static void LaunchM12MidPatrolPlotScene()
    {
        m12MidPatrolPlotActive = true;
        m12IntroPlotActive = false;
        m12VictoryEpilogueActive = false;
        launchM12PhaseBBattleAfterPlot = true;
        launchM12PhaseABattleAfterPlot = false;
        tutorialPlotEpilogueActive = false;
        harborCombatClearBridgeActive = false;
        launchTutorialBattleAfterPlot = false;
        tutorialPlotBgmRequested = false;
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressBackgroundMusicPlayer.StopAll();
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM12MidPatrolPlotSteps());
        LoadMainPlotOrFallback(() => m12MidPatrolPlotActive = false);
    }

    public static void LaunchM12VictoryEpiloguePlotScene()
    {
        m12VictoryEpilogueActive = true;
        m12IntroPlotActive = false;
        m12MidPatrolPlotActive = false;
        launchM12PhaseABattleAfterPlot = false;
        launchM12PhaseBBattleAfterPlot = false;
        tutorialPlotEpilogueActive = false;
        harborCombatClearBridgeActive = false;
        launchTutorialBattleAfterPlot = false;
        tutorialPlotBgmRequested = false;
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressBackgroundMusicPlayer.StopAll();
        SetPendingPlotSteps(TutorialPlotScriptFactory.BuildM12VictoryEpilogueSteps());
        LoadMainPlotOrFallback(() => m12VictoryEpilogueActive = false);
    }

    public static void LaunchM12PhaseABattleAfterPlot(bool fastCloseAnimation = false) =>
        M12PlotBattleTransition.PlayFromPlotToPhaseABattle(fastCloseAnimation);

    public static void LaunchM12PhaseBBattleAfterPlot(bool fastCloseAnimation = false) =>
        M12PlotBattleTransition.PlayFromPlotToPhaseBBattle(fastCloseAnimation);

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

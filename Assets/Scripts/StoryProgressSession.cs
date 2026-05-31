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
    private static bool tutorialPlotBgmRequested;

    /// <summary>教學戰勝利後的 Main Plot 結尾劇情進行中。</summary>
    public static bool IsTutorialPlotEpilogueActive => tutorialPlotEpilogueActive;

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

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>教學戰結束後回到 Story progress（由結算介面按鈕觸發）。</summary>
public static class StoryProgressBattleReturn
{
    public static void CompleteReturnToStoryProgress(bool won)
    {
        StoryProgressSession.NotifyTutorialBattleFinished(won);
        TutorialBattleBackgroundMusicPlayer.StopAll();
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressSession.LoadStoryProgressWithIrisTransition();
    }

    public static void RetryIntroTutorialBattle()
    {
        TutorialDeckApplicator.ApplyToActivePlayerDeck();
        SceneLoader.LaunchIntroTutorialBattleFromAnywhere();
    }

    public static void CompleteReturnFromHarborTraining(bool firstCombatClear = false)
    {
        TutorialBattleBackgroundMusicPlayer.StopAll();
        BattleLaunchContext.ClearActiveBattle();
        if (firstCombatClear)
            StoryProgressSession.LaunchHarborCombatClearBridgeAfterFirstVictory();
        else
            StoryProgressSession.LoadStoryProgressWithIrisTransition();
    }

    public static void RetryM12PhaseABattle()
    {
        M12PhaseDeckApplicator.ApplyPhaseADeck();
        SceneLoader.LaunchM12PhaseABattleDirect();
    }

    public static void RetryM12PhaseBBattle()
    {
        M12PhaseDeckApplicator.ApplyPhaseBDeck();
        SceneLoader.LaunchM12PhaseBBattleDirect();
    }
}

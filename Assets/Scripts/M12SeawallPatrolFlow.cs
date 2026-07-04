/// <summary>從 Story progress 進入 M-1-2 時解析下一個步驟。</summary>
public static class M12SeawallPatrolFlow
{
    public static bool CanEnterFromStoryProgress(int slot) =>
        M12SeawallPatrolProgressState.IsNodeAvailable(slot);

    public static void LaunchFromStoryProgress()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (!CanEnterFromStoryProgress(slot))
        {
            UnityEngine.Debug.LogWarning("M12SeawallPatrolFlow: harbor combat not cleared; M-1-2 locked.");
            return;
        }

        // 所有從 Story progress 進關的路徑都先播進關演出（暗場 → 標題／達成目標 → 光圈）。
        // 已通關的重溫從頭開始走完整流程（劇情 → 段考 A → 散策 → 加練 B），首通獎勵不重發。
        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
        {
            StoryLevelEntryTransition.PlayToIntroPlot(replay: true);
            return;
        }

        if (M12SeawallPatrolProgressState.IsMidPatrolComplete(slot))
        {
            StoryLevelEntryTransition.PlayToPhaseBBattle();
            return;
        }

        if (M12SeawallPatrolProgressState.IsPhaseAComplete(slot))
        {
            StoryLevelEntryTransition.PlayToMidPatrolPlot();
            return;
        }

        if (!TutorialProgressState.IsM12IntroSeen(slot))
        {
            StoryLevelEntryTransition.PlayToIntroPlot();
            return;
        }

        StoryLevelEntryTransition.PlayToPhaseABattle();
    }
}

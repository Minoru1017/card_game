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

        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
        {
            SceneLoader.LaunchM12PhaseBBattleDirect();
            return;
        }

        if (M12SeawallPatrolProgressState.IsMidPatrolComplete(slot))
        {
            SceneLoader.LaunchM12PhaseBBattleDirect();
            return;
        }

        if (M12SeawallPatrolProgressState.IsPhaseAComplete(slot))
        {
            StoryProgressSession.LaunchM12MidPatrolPlotScene();
            return;
        }

        if (!TutorialProgressState.IsM12IntroSeen(slot))
        {
            StoryProgressSession.LaunchM12IntroPlotScene();
            return;
        }

        SceneLoader.LaunchM12PhaseABattleDirect();
    }
}

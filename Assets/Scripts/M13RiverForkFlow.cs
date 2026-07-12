/// <summary>從 Story progress 進入 M-1-3 時解析下一個步驟。</summary>
public static class M13RiverForkFlow
{
    public static bool CanEnterFromStoryProgress(int slot) =>
        M13RiverForkProgressState.IsNodeAvailable(slot);

    public static void LaunchFromStoryProgress()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (!CanEnterFromStoryProgress(slot))
        {
            UnityEngine.Debug.LogWarning("M13RiverForkFlow: M-1-2 not cleared; M-1-3 locked.");
            return;
        }

        if (M13RiverForkProgressState.IsNodeCleared(slot))
        {
            M13RiverForkProgressState.ResetProgressFlagsForReplay(slot);
            StoryLevelEntryTransition.PlayToM13OpeningPlot(replay: true);
            return;
        }

        if (!M13RiverForkProgressState.IsOpeningSeen(slot))
        {
            StoryLevelEntryTransition.PlayToM13OpeningPlot(replay: false);
            return;
        }

        AdvanceFromCurrentProgress();
    }

    /// <summary>分波鬥鳥結束後接續：岔路散策 → 冷爐迎測 → …</summary>
    public static void ContinueAfterBirdDuel() => AdvanceFromCurrentProgress();

    private static void AdvanceFromCurrentProgress()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();

        if (!M13RiverForkProgressState.IsBirdDuelComplete(slot))
        {
            ShowBirdDuelEntry();
            return;
        }

        if (!M13RiverForkProgressState.IsForkStrollComplete(slot))
        {
            ShowForkStroll();
            return;
        }

        if (!M13RiverForkProgressState.IsPhaseAComplete(slot))
        {
            LaunchPhaseAEntry();
            return;
        }

        if (!M13RiverForkProgressState.IsRoseTrialSeen(slot))
        {
            StoryLevelEntryTransition.PlayToM13RoseTrialPlot();
            return;
        }

        StoryLevelEntryTransition.PlayToM13PhaseBBattle();
    }

    public static void ShowBirdDuelEntry()
    {
        M13BirdDuelEntryOverlay.Show(
            onPlay: SceneLoader.LaunchM13RiverForkBirdDuel,
            onSkip: SceneLoader.SkipM13RiverForkBirdDuel);
    }

    public static void ShowForkStroll()
    {
        M13RiverForkStrollOverlay.Show(path =>
        {
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            M13RiverForkProgressState.MarkForkStrollComplete(slot, path);
            LaunchPhaseAEntry();
        });
    }

    public static void LaunchPhaseAEntry()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M13RiverForkProgressState.HasBirdDuelSRank(slot) &&
            !M13RiverForkProgressState.HasOpeningWeatherPick(slot))
        {
            M13OpeningWeatherPickOverlay.Show(pick =>
            {
                M13RiverForkProgressState.SetOpeningWeatherPick(slot, pick);
                StoryLevelEntryTransition.PlayToM13PhaseABattle();
            });
            return;
        }

        StoryLevelEntryTransition.PlayToM13PhaseABattle();
    }
}

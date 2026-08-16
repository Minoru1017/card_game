using UnityEngine;

/// <summary>從 Story progress 進入 A-1 潮間島支線（劇情在 Main Plot，農事／航程為 overlay）。</summary>
public static class SideQuestA1Flow
{
    public static bool CanEnterFromStoryProgress(int slot) =>
        SideQuestA1ProgressState.IsNodeAvailable(slot);

    public static void LaunchFromStoryProgress()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (!CanEnterFromStoryProgress(slot))
        {
            string hint = SideQuestA1ProgressState.BuildLockedHint(slot);
            Debug.LogWarning("SideQuestA1Flow: locked — " + hint);
            return;
        }

        bool replay = SideQuestA1ProgressState.IsNodeCleared(slot);
        StoryLevelEntryTransition.PlayToA1TideIsland(replay);
    }

    public static void OnHarborPlotFinished()
    {
        SideQuestA1VoyageOverlay.Show(OnVoyageFinished);
    }

    public static void OnVoyageFinished()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        StoryProgressSession.LaunchA1IslandIntroPlotInPlace(slot);
    }

    public static void OnIslandIntroPlotFinished()
    {
        if (StoryProgressSession.ConsumeA1SkipFarm())
        {
            ApplySkippedAndContinueReturn();
            return;
        }

        SideQuestA1TideIslandFarmOverlay.Show(OnFarmFinished);
    }

    public static void OnFarmFinished(SideQuestA1TideIslandFarmOverlay.FarmResult farmResult)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();

        if (farmResult.outcome == SideQuestA1TideMarkRewardService.FarmOutcome.Skipped)
        {
            SideQuestA1TideMarkRewardService.ApplyResult apply =
                SideQuestA1TideMarkRewardService.ApplyFarmOutcome(
                    slot,
                    farmResult.outcome,
                    farmResult.keptSeaPurslaneSeed);
            StoryProgressSession.SetA1PendingApplyResult(apply);
            StoryProgressSession.LaunchA1ReturnPlotInPlace(apply.seaPurslaneSeedKept);
            return;
        }

        StoryProgressSession.SetA1PendingFarmResult(farmResult.outcome, farmResult.keptSeaPurslaneSeed);
        StoryProgressSession.LaunchA1UnsealPlotInPlace();
    }

    public static void OnUnsealPlotFinished()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        StoryProgressSession.TryConsumeA1PendingFarmResult(
            out SideQuestA1TideMarkRewardService.FarmOutcome outcome,
            out bool keptSeaPurslaneSeed);

        SideQuestA1TideMarkRewardService.ApplyResult apply =
            SideQuestA1TideMarkRewardService.ApplyFarmOutcome(slot, outcome, keptSeaPurslaneSeed);
        StoryProgressSession.SetA1PendingApplyResult(apply);
        StoryProgressSession.LaunchA1ReturnPlotInPlace(apply.seaPurslaneSeedKept);
    }

    public static void OnReturnPlotFinished()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (StoryProgressSession.TryConsumeA1PendingApplyResult(out SideQuestA1TideMarkRewardService.ApplyResult apply))
            FinishReturn(slot, apply);

        StoryProgressSession.EndA1ReturnPlotSession();
        StoryProgressSession.LoadStoryProgressWithIrisTransition();
    }

    private static void ApplySkippedAndContinueReturn()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        SideQuestA1TideMarkRewardService.ApplyResult apply =
            SideQuestA1TideMarkRewardService.ApplyFarmOutcome(
                slot,
                SideQuestA1TideMarkRewardService.FarmOutcome.Skipped,
                keptSeaPurslaneSeed: false);
        StoryProgressSession.SetA1PendingApplyResult(apply);
        StoryProgressSession.LaunchA1ReturnPlotInPlace(apply.seaPurslaneSeedKept);
    }

    private static void FinishReturn(int slot, SideQuestA1TideMarkRewardService.ApplyResult apply)
    {
        if (!string.IsNullOrEmpty(apply.message))
        {
            GameDevLog.Log("A-1: " + apply.message +
                           (apply.coinsGranted > 0 ? " (+" + apply.coinsGranted + " 金幣)" : string.Empty));
            if (SideQuestA1ProgressState.CanUnsealTideMarkInVault(slot))
                SceneToast.Show(apply.message, 3.2f);
        }
    }
}

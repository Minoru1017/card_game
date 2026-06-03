using UnityEngine;

/// <summary>港灣訓練場實戰進度（每角色槽存於 playerdata.csv）。</summary>
public static class HarborTrainingProgressState
{
    private const string CombatClearKey = "harbor_combat_clear";
    private const string HardRewardKey = "harbor_hard_reward";

    /// <summary>港灣實戰（簡單／普通／困難）在標準難度索引中的區間。</summary>
    private const int HarborCombatDifficultyIndexMin = 1;
    private const int HarborCombatDifficultyIndexMax = 3;

    /// <summary>任一難度首次通關（實戰 Clear，解鎖 M-1-2）。</summary>
    public static bool IsHarborCombatCleared(int slot) =>
        TutorialProgressState.ReadSlotFlag(slot, CombatClearKey);

    public static bool IsHarborCombatClearedForActivePlayer() =>
        IsHarborCombatCleared(PlayerData.GetActivePlayerSlotOrDefault());

    public static void SetHarborCombatCleared(int slot, bool cleared = true) =>
        TutorialProgressState.WriteSlotFlag(slot, CombatClearKey, cleared);

    /// <summary>困難級首通且已發放港灣畢業證 SR。</summary>
    public static bool IsHarborHardGraduationRewardGranted(int slot) =>
        TutorialProgressState.ReadSlotFlag(slot, HardRewardKey);

    public static void SetHarborHardGraduationRewardGranted(int slot, bool granted = true) =>
        TutorialProgressState.WriteSlotFlag(slot, HardRewardKey, granted);

    /// <summary>
    /// 依港灣旗標、戰績列、M-1-2 進度等修復 <c>harbor_combat_clear</c>（記錄點遺失旗標時仍顯示 Clear）。
    /// 不因商店抽到畢業證而推斷 Clear。
    /// </summary>
    public static void EnsureSlotHarborProgressConsistent(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        SanitizeInflatedHarborClear(slot);
        if (IsHarborCombatCleared(slot))
            return;

        if (TryRepairCombatClearFromHardRewardFlag(slot))
            return;
        if (TryRepairCombatClearFromBattleRecords(slot))
            return;
        TryRepairCombatClearFromDownstreamProgress(slot);
    }

    /// <summary>商店可抽到畢業證，不可僅憑收藏推斷港灣 Clear；無戰績／旗標依據時還原誤寫的 clear。</summary>
    private static void SanitizeInflatedHarborClear(int slot)
    {
        if (!IsHarborCombatCleared(slot))
            return;

        if (IsHarborHardGraduationRewardGranted(slot))
            return;
        if (PlayerProfileCsvService.SlotHasBattleVictoryInDifficultyIndexRangeOnPlayerSave(
                slot, HarborCombatDifficultyIndexMin, HarborCombatDifficultyIndexMax))
            return;
        if (TutorialProgressState.IsM12ReligiousLineRewardGranted(slot))
            return;
        if (TutorialProgressState.IsM12TrioMasteryCleared(slot))
            return;

        SetHarborCombatCleared(slot, false);
    }

    private static bool TryRepairCombatClearFromHardRewardFlag(int slot)
    {
        if (!IsHarborHardGraduationRewardGranted(slot))
            return false;
        SetHarborCombatCleared(slot, true);
        return true;
    }

    private static bool TryRepairCombatClearFromBattleRecords(int slot)
    {
        if (!PlayerProfileCsvService.SlotHasBattleVictoryInDifficultyIndexRangeOnPlayerSave(
                slot, HarborCombatDifficultyIndexMin, HarborCombatDifficultyIndexMax))
            return false;

        SetHarborCombatCleared(slot, true);
        return true;
    }

    private static bool TryRepairCombatClearFromDownstreamProgress(int slot)
    {
        if (!TutorialProgressState.IsM12ReligiousLineRewardGranted(slot) &&
            !TutorialProgressState.IsM12TrioMasteryCleared(slot))
            return false;

        SetHarborCombatCleared(slot, true);
        return true;
    }

    public static void ResetHarborTrainingForSlot(int slot)
    {
        SetHarborCombatCleared(slot, false);
        SetHarborHardGraduationRewardGranted(slot, false);
    }
}

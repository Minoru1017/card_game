using UnityEngine;

/// <summary>港灣訓練場實戰進度（每角色槽存於 playerdata.csv）。</summary>
public static class HarborTrainingProgressState
{
    private const string CombatClearKey = "harbor_combat_clear";
    private const string HardRewardKey = "harbor_hard_reward";

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

    public static void ResetHarborTrainingForSlot(int slot)
    {
        SetHarborCombatCleared(slot, false);
        SetHarborHardGraduationRewardGranted(slot, false);
    }
}

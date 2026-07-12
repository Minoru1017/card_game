/// <summary>§E 玫瑰試煉選項對階段 B 的修正（LEVEL_DESIGN_M-1-3.md §四）。</summary>
public static class M13RoseTrialOutcome
{
    public static bool HasRoseBurnedModifier(int slot) =>
        M13RiverForkProgressState.IsRoseBurned(slot);

    public static bool ShouldSilenceCoachEarlyRounds(int slot) =>
        M13RiverForkProgressState.IsPlayerDemandedMiracle(slot);

    public static void ApplyToBattleRules(int slot, out int enemyHpBonus, out bool coachSilentRounds1To3)
    {
        enemyHpBonus = HasRoseBurnedModifier(slot) ? M13PhaseBBattleRules.RoseBurnedEnemyHpBonus : 0;
        coachSilentRounds1To3 = ShouldSilenceCoachEarlyRounds(slot);
    }
}

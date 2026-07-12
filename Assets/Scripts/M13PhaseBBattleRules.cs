using UnityEngine;

/// <summary>M-1-3 階段 B：分波對決（港灣普通基底 ＋ 岔路 ＋ 玫瑰試煉修正）。</summary>
public static class M13PhaseBBattleRules
{
    public const int HarborNormalEnemyStartHealth = 15;
    public const int SteadyPathEnemyHpBonus = 1;
    public const int RoseBurnedEnemyHpBonus = 1;
    public const float HarborNormalEnemyDamageMultiplier = 0.66f;
    public const float RapidPathEnemyDamageMultiplier = 1.05f;
    public const int HotBloodReversalHpThreshold = 8;
    public const int SRankOpeningExtraDraw = 1;

    public static int ResolveEnemyStartHealth(int slot)
    {
        int hp = HarborNormalEnemyStartHealth;
        if (M13RiverForkProgressState.GetForkPath(slot) == M13RiverForkPathChoice.Steady)
            hp += SteadyPathEnemyHpBonus;
        if (M13RoseTrialOutcome.HasRoseBurnedModifier(slot))
            hp += RoseBurnedEnemyHpBonus;
        return hp;
    }

    public static EnemyAiPlayStyle ResolveEnemyAiStyle(int slot) =>
        M13RiverForkProgressState.GetForkPath(slot) == M13RiverForkPathChoice.Rapid
            ? EnemyAiPlayStyle.FastAttack
            : EnemyAiPlayStyle.Balanced;

    public static int GetEnemyDrawPerTurn(int currentRound) =>
        HarborTrainingNormalBattleRules.GetEnemyDrawPerTurn(currentRound);

    public static int ScaleEnemyDamage(int rawDamage, int slot)
    {
        if (rawDamage <= 0)
            return rawDamage;

        float mult = HarborNormalEnemyDamageMultiplier;
        if (M13RiverForkProgressState.GetForkPath(slot) == M13RiverForkPathChoice.Rapid)
            mult *= RapidPathEnemyDamageMultiplier;

        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * mult));
    }

    public static int GetFastAttackMonsterPriorityBonus(int currentRound) =>
        HarborTrainingNormalBattleRules.GetFastAttackMonsterPriorityBonus(currentRound);

    public static int GetOpeningExtraDraw(int slot) =>
        M13RiverForkProgressState.HasBirdDuelSRank(slot) ? SRankOpeningExtraDraw : 0;
}

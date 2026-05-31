using UnityEngine;

/// <summary>
/// 港灣訓練場「普通」專用平衡（企劃 KPI：入門預設牌組首通約 60%，回合數不限）。
/// 與 Buildbeck 一般 Normal 分離。簡單檔有第 10 回合必勝，普通無回合上限，故敵數值需更寬鬆。
/// KPI 請以真人試玩為準；批次自動出牌模擬勝率通常僅約 15～25%。
/// </summary>
public static class HarborTrainingNormalBattleRules
{
    public const int EnemyStartHealth = 15;

    public const int SlowDrawUntilRoundInclusive = 5;

    public const int EnemyDrawPerTurnSlow = 1;

    public const int EnemyDrawPerTurnNormal = 2;

    public const float EnemyDamageMultiplier = 0.66f;

    public const int SoftPressureRoundsInclusive = 5;

    public const int FastAttackMonsterBonusSoft = 3;

    public const int FastAttackMonsterBonusFull = 6;

    /// <summary>簡單弱牌組 + 1 主教、1 騎兵（循環至 30 張），無 SSR。</summary>
    public static readonly int[] NormalEnemyDeckCardIds =
    {
        4, 4, 4, 4,
        5, 5, 5, 5,
        22, 22, 22,
        17, 17,
        14,
        6,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(0),
        4, 5, 22, 4, 5, 22, 4, 5, 22, 14, 5, 22
    };

    public static bool IsActiveNormalBattle()
    {
        if (!BattleLaunchContext.IsHarborTrainingGroundBattle)
            return false;
        BattleDifficultyTier tier = HarborTrainingBattleCopy.TierFromLabelZh(
            BattleLaunchContext.ResolveForBattleRecord());
        return tier == BattleDifficultyTier.Normal;
    }

    public static int GetEnemyDrawPerTurn(int currentRound)
    {
        if (currentRound <= SlowDrawUntilRoundInclusive)
            return EnemyDrawPerTurnSlow;
        return EnemyDrawPerTurnNormal;
    }

    public static int ScaleEnemyDamage(int rawDamage)
    {
        if (rawDamage <= 0)
            return rawDamage;
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * EnemyDamageMultiplier));
    }

    public static int GetFastAttackMonsterPriorityBonus(int currentRound)
    {
        if (currentRound <= SoftPressureRoundsInclusive)
            return FastAttackMonsterBonusSoft;
        return FastAttackMonsterBonusFull;
    }
}

using UnityEngine;

/// <summary>
/// 港灣訓練場「困難」專用平衡（畢業門檻；與 Buildbeck 一般 Hard 分離）。
/// 高於普通：較高敵 HP／傷害倍率、較強固定牌組（仍無 SSR 四騎 8～11）。
/// </summary>
public static class HarborTrainingHardBattleRules
{
    public const int EnemyStartHealth = 18;

    public const int SlowDrawUntilRoundInclusive = 3;

    public const int EnemyDrawPerTurnSlow = 1;

    public const int EnemyDrawPerTurnNormal = 2;

    public const float EnemyDamageMultiplier = 0.74f;

    public const int SoftPressureRoundsInclusive = 3;

    public const int FastAttackMonsterBonusSoft = 5;

    public const int FastAttackMonsterBonusFull = 12;

    public const int EnemyOverLimitAllowance = 3;

    public const int MinEnemySpellsInDeck = 2;

    /// <summary>普通池加強：雙主教、雙騎兵、審判官、雙火球；無死／戰爭／瘟疫／飢荒。</summary>
    public static readonly int[] HardEnemyDeckCardIds =
    {
        4, 4, 4, 4,
        5, 5, 5, 5,
        22, 22, 22,
        17, 17,
        14, 14,
        6, 6,
        16,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(0),
        DeckCardId.SpellKeyFromOrdinal(0),
        5, 22, 6, 14, 4, 5, 22, 4
    };

    public static HarborTrainingTierConfig PendingConfig =>
        new HarborTrainingTierConfig(
            "困難",
            HardEnemyDeckCardIds,
            EnemyOverLimitAllowance,
            MinEnemySpellsInDeck);

    public static bool IsActiveHardBattle()
    {
        if (!BattleLaunchContext.IsHarborTrainingGroundBattle)
            return false;
        BattleDifficultyTier tier = HarborTrainingBattleCopy.TierFromLabelZh(
            BattleLaunchContext.ResolveForBattleRecord());
        return tier == BattleDifficultyTier.Hard;
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

    public static void GetFastAttackSpellTweak(int currentRound, int spellOrdinal, ref int spellTweak)
    {
        if (currentRound > SoftPressureRoundsInclusive)
            return;
        spellTweak = spellOrdinal == 1 ? -14 : -32;
    }
}

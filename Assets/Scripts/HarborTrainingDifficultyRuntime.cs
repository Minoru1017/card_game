using UnityEngine;

/// <summary>港灣訓練場三檔難度在戰鬥中的統一入口（避免 BattleSimulationManager 散落多段 if）。</summary>
public static class HarborTrainingDifficultyRuntime
{
    public static bool IsHarborBattleActive =>
        BattleLaunchContext.IsHarborTrainingGroundBattle;

    public static BattleDifficultyTier ResolveActiveTier()
    {
        if (!IsHarborBattleActive)
            return BattleDifficultyTier.Normal;
        return HarborTrainingBattleCopy.TierFromLabelZh(BattleLaunchContext.ResolveForBattleRecord());
    }

    public static HarborTrainingTierConfig ResolvePendingConfig(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Easy:
                return HarborTrainingEasyBattleRules.PendingConfig;
            case BattleDifficultyTier.Normal:
                return HarborTrainingNormalBattleRules.PendingConfig;
            case BattleDifficultyTier.Hard:
                return HarborTrainingHardBattleRules.PendingConfig;
            default:
                Debug.LogWarning("HarborTrainingDifficultyRuntime: unexpected tier " + tier + "; using Normal.");
                return HarborTrainingNormalBattleRules.PendingConfig;
        }
    }

    public static bool TryGetEnemyStartHealth(out int health)
    {
        health = 0;
        if (!IsHarborBattleActive)
            return false;

        switch (ResolveActiveTier())
        {
            case BattleDifficultyTier.Easy:
                health = HarborTrainingEasyBattleRules.EnemyStartHealth;
                return true;
            case BattleDifficultyTier.Normal:
                health = HarborTrainingNormalBattleRules.EnemyStartHealth;
                return true;
            case BattleDifficultyTier.Hard:
                health = HarborTrainingHardBattleRules.EnemyStartHealth;
                return true;
            default:
                return false;
        }
    }

    public static bool ShouldForcePlayerVictoryAfterRound(int currentRound)
    {
        return HarborTrainingEasyBattleRules.IsActiveEasyBattle() &&
               currentRound > HarborTrainingEasyBattleRules.MaxRoundsInclusive;
    }

    public static int GetEnemyDrawPerTurn(int currentRound)
    {
        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
            return HarborTrainingEasyBattleRules.GetEnemyDrawPerTurn(currentRound);
        if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
            return HarborTrainingNormalBattleRules.GetEnemyDrawPerTurn(currentRound);
        if (HarborTrainingHardBattleRules.IsActiveHardBattle())
            return HarborTrainingHardBattleRules.GetEnemyDrawPerTurn(currentRound);
        return 2;
    }

    public static int ScaleEnemyDamage(int rawDamage)
    {
        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
            return HarborTrainingEasyBattleRules.ScaleEnemyDamage(rawDamage);
        if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
            return HarborTrainingNormalBattleRules.ScaleEnemyDamage(rawDamage);
        if (HarborTrainingHardBattleRules.IsActiveHardBattle())
            return HarborTrainingHardBattleRules.ScaleEnemyDamage(rawDamage);
        return rawDamage;
    }

    public static int GetFastAttackMonsterPriorityBonus(int currentRound)
    {
        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
            return HarborTrainingEasyBattleRules.GetFastAttackMonsterPriorityBonus(currentRound);
        if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
            return HarborTrainingNormalBattleRules.GetFastAttackMonsterPriorityBonus(currentRound);
        if (HarborTrainingHardBattleRules.IsActiveHardBattle())
            return HarborTrainingHardBattleRules.GetFastAttackMonsterPriorityBonus(currentRound);
        return 16;
    }

    public static int GetFastAttackSpellTweak(int currentRound, int spellOrdinal, int defaultTweak)
    {
        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle() &&
            currentRound <= HarborTrainingEasyBattleRules.SoftPressureRoundsInclusive)
            return spellOrdinal == 1 ? -18 : -36;

        if (HarborTrainingNormalBattleRules.IsActiveNormalBattle() &&
            currentRound <= HarborTrainingNormalBattleRules.SoftPressureRoundsInclusive)
            return spellOrdinal == 1 ? -16 : -34;

        if (HarborTrainingHardBattleRules.IsActiveHardBattle())
        {
            int tweak = defaultTweak;
            HarborTrainingHardBattleRules.GetFastAttackSpellTweak(currentRound, spellOrdinal, ref tweak);
            return tweak;
        }

        return defaultTweak;
    }
}

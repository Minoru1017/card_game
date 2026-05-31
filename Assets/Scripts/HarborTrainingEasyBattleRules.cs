using UnityEngine;

/// <summary>
/// 港灣訓練場「簡單」專用平衡（企劃 KPI：首通約 70%、平均約 10 回合內結束）。
/// 仍為快攻主題，但前段壓力較低、敵傷害與抽牌節奏較寬鬆，第 10 回合起若未分勝負則判定玩家獲勝。
/// </summary>
public static class HarborTrainingEasyBattleRules
{
    /// <summary>與入門教學相同：完成第 10 回合後下一輪推進時結束並判定玩家獲勝（拉齊平均局長）。</summary>
    public const int MaxRoundsInclusive = 10;

    /// <summary>敵方起始 HP（略低於玩家預設 20，降低暴斃感）。</summary>
    public const int EnemyStartHealth = 17;

    /// <summary>前 N 回合敵方每回合僅抽 1 張（之後為 2），製造前段呼吸感。</summary>
    public const int SlowDrawUntilRoundInclusive = 4;

    public const int EnemyDrawPerTurnSlow = 1;

    public const int EnemyDrawPerTurnNormal = 2;

    /// <summary>敵方對玩家傷害倍率（入門 0.72、一般對戰 1.0；簡單實戰取中間）。</summary>
    public const float EnemyDamageMultiplier = 0.78f;

    /// <summary>前 N 回合 FastAttack 出牌評分加成降低（仍會出怪，但不至於連續暴壓）。</summary>
    public const int SoftPressureRoundsInclusive = 4;

    public const int FastAttackMonsterBonusSoft = 6;

    public const int FastAttackMonsterBonusFull = 16;

    /// <summary>簡單檔固定敵牌組：入門弱牌組為基礎，略增 1 張火球供「練習解法術」。</summary>
    public static readonly int[] EasyEnemyDeckCardIds =
    {
        4, 4, 4, 4,
        5, 5, 5,
        22, 22, 22,
        17, 17,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(0),
        4, 5, 22
    };

    public static bool IsActiveEasyBattle()
    {
        if (!BattleLaunchContext.IsHarborTrainingGroundBattle)
            return false;
        BattleDifficultyTier tier = HarborTrainingBattleCopy.TierFromLabelZh(
            BattleLaunchContext.ResolveForBattleRecord());
        return tier == BattleDifficultyTier.Easy;
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

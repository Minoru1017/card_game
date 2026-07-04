using UnityEngine;

/// <summary>M-1-2 階段 A 教學戰：比港灣更溫和、無天氣（綜合型 AI＋教學用出牌偏置）。</summary>
public static class M12PhaseABattleRules
{
    public const int MaxRoundsInclusive = 12;
    public const int EnemyStartHealth = 15;
    public const int EnemyDrawPerTurn = 1;

    /// <summary>第 6 回合前（currentRound &lt; 6）敵方 AI 以生存、保場為優先，避免過早被擊敗。</summary>
    public const int EnemySurvivalAiUntilRoundExclusive = 6;

    /// <summary>敵方牌表與玩家階段 A 定案 15 張完全相同（鏡像對局）；複製一份避免外部改動共用陣列。</summary>
    public static readonly int[] EnemyDeckCardIds =
        (int[])M12PhaseDeckCatalog.PhaseADeckCardIds.Clone();
}

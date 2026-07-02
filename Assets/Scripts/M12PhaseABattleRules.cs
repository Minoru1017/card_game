using UnityEngine;

/// <summary>M-1-2 階段 A 教學戰：比港灣更溫和、無天氣（綜合型 AI＋教學用出牌偏置）。</summary>
public static class M12PhaseABattleRules
{
    public const int MaxRoundsInclusive = 12;
    public const int EnemyStartHealth = 15;
    public const int EnemyDrawPerTurn = 1;
    public const float EnemyDamageMultiplier = 0.68f;

    public static readonly int[] EnemyDeckCardIds =
    {
        4, 4, 4,
        5, 5, 5,
        22, 22,
        17,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        4, 5, 22, 4, 5
    };
}

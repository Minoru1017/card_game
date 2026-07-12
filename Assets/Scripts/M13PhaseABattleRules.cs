/// <summary>M-1-3 階段 A：冷爐迎測（木樁 AI；前 3 回合無天氣，第 4 回合起預報）。</summary>
public static class M13PhaseABattleRules
{
    public const int MaxRoundsInclusive = 12;
    public const int EnemyStartHealth = 14;
    public const float EnemyDamageMultiplier = 0.80f;
    public const int EnemyDrawPerTurn = 1;

    public static readonly int[] EnemyDeckCardIds = HarborTrainingEasyBattleRules.EasyEnemyDeckCardIds;
}

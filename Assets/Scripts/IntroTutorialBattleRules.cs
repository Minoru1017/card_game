/// <summary>1-1 入門教學對戰專用平衡：回合上限、敵方牌組與 AI 調參常數。</summary>
public static class IntroTutorialBattleRules
{
    /// <summary>雙方完成第 10 回合後，下一輪推進時結束並判定玩家獲勝。</summary>
    public const int MaxRoundsInclusive = 10;

    /// <summary>教學敵方起始英雄 HP（玩家仍用場景預設 startHealth）。</summary>
    public const int EnemyStartHealth = 16;

    /// <summary>教學敵方每回合抽牌數（一般對戰為 2）。</summary>
    public const int EnemyDrawPerTurn = 1;

    /// <summary>教學敵方對玩家造成傷害倍率（含直擊英雄）。</summary>
    public const float EnemyDamageMultiplier = 0.72f;

    /// <summary>較溫和的教學敵牌組（民兵／長弓／教徒／修女＋初級治療，無火球與高費怪）。</summary>
    public static readonly int[] WeakEnemyDeckCardIds =
    {
        4, 4, 4, 4,
        5, 5, 5, 5,
        22, 22, 22,
        17, 17, 17,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        4, 5, 22
    };
}

/// <summary>敵方出牌 AI 風格（由開戰前難度選擇注入）。</summary>
public enum EnemyAiPlayStyle
{
    /// <summary>入門～普通：有牌可出則依優先度立即打出。</summary>
    Greedy = 0,
    /// <summary>困難：SR 以上高稀有卡待良好時機再出。</summary>
    SchemingHard = 1,
    /// <summary>魔王：R 以上即可能囤牌，條件更嚴才出手。</summary>
    SchemingBoss = 2
}

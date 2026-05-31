/// <summary>敵方出牌 AI 風格（由開戰前難度選擇注入）。</summary>
public enum EnemyAiPlayStyle
{
    /// <summary>普通：有牌可出則依優先度立即打出。</summary>
    Greedy = 0,
    /// <summary>困難：SR 以上高稀有卡待良好時機再出。</summary>
    SchemingHard = 1,
    /// <summary>魔王：R 以上即可能囤牌，條件更嚴才出手。</summary>
    SchemingBoss = 2,
    /// <summary>入門：Greedy 且略偏先出怪（法術評分降低）。</summary>
    IntroGreedy = 3,
    /// <summary>簡單：Greedy 且略偏法術（法術評分提高）。</summary>
    EasySpellLean = 4,
    /// <summary>快攻：Greedy 且強烈偏先出怪、壓低非直傷法術評分（港灣訓練場）。</summary>
    FastAttack = 5
}

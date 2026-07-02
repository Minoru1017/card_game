/// <summary>敵方出牌 AI 攻擊風格（由開戰前難度或關卡注入）。</summary>
public enum EnemyAiPlayStyle
{
    /// <summary>綜合型：依評分出牌，無明顯攻守偏置。</summary>
    Balanced = 0,
    /// <summary>防禦型：囤高價值牌、偏治療與待時機出手。</summary>
    Defensive = 1,
    /// <summary>快攻型：強烈偏先出怪與直傷壓迫。</summary>
    FastAttack = 2
}

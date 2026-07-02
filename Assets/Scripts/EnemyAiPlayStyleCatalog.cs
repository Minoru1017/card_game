/// <summary>敵方 AI 三風格：難度映射、顯示文案、防禦嚴格度判定。</summary>
public static class EnemyAiPlayStyleCatalog
{
    public static EnemyAiPlayStyle MapDifficultyToStyle(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro:
            case BattleDifficultyTier.Easy:
            case BattleDifficultyTier.Normal:
                return EnemyAiPlayStyle.Balanced;
            case BattleDifficultyTier.Hard:
            case BattleDifficultyTier.Boss:
                return EnemyAiPlayStyle.Defensive;
            default:
                return EnemyAiPlayStyle.Balanced;
        }
    }

    public static bool IsDefensiveStrictLabel(string difficultyLabelZh)
    {
        if (string.IsNullOrWhiteSpace(difficultyLabelZh))
            return false;

        string label = difficultyLabelZh.Trim();
        return label.StartsWith("魔王", System.StringComparison.Ordinal) ||
               string.Equals(label, "Boss", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(label, "BOSS", System.StringComparison.Ordinal);
    }

    public static bool IsDefensiveHardLabel(string difficultyLabelZh)
    {
        if (string.IsNullOrWhiteSpace(difficultyLabelZh))
            return false;

        string label = difficultyLabelZh.Trim();
        return label.StartsWith("困難", System.StringComparison.Ordinal) ||
               string.Equals(label, "Hard", System.StringComparison.OrdinalIgnoreCase);
    }

    public static string GetOneLinerZh(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.FastAttack:
                return "快攻型, 早出怪壓迫";
            case EnemyAiPlayStyle.Defensive:
                return "防禦型, 囤牌待時機";
            default:
                return "綜合型, 攻守均衡";
        }
    }

    public static string GetBriefZh(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.FastAttack:
                return "快攻 AI: 以 Greedy 為基礎, 強烈優先出場怪與直傷法術, 壓迫玩家血線.";
            case EnemyAiPlayStyle.Defensive:
                return "防禦 AI: 傾向保留高稀有卡待有利時機, 略偏治療與解法術, 不急於搶攻.";
            default:
                return "綜合 AI: 每回合在可出牌中選評分最高者立即打出, 無明顯攻守偏置.";
        }
    }
}

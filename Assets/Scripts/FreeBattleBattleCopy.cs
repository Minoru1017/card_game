/// <summary>自由對戰場景：戰前預覽文案（依所選 AI 風格）。</summary>
public static class FreeBattleBattleCopy
{
    public const string SceneName = "Free Battle";

    public static BattleDifficultyTier TierFromLabelZh(string labelZh)
    {
        if (string.IsNullOrWhiteSpace(labelZh))
            return BattleDifficultyTier.Normal;
        string label = labelZh.Trim();
        if (label.StartsWith("簡單", System.StringComparison.Ordinal))
            return BattleDifficultyTier.Easy;
        if (label.StartsWith("困難", System.StringComparison.Ordinal))
            return BattleDifficultyTier.Hard;
        return BattleDifficultyTier.Normal;
    }

    public const string PreviewHeaderRich = "<size=115%><b>自由對戰 選擇難易度</b></size>";

    public static string GetPreviewGoalRich(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.FastAttack:
                return "<color=#6C533D>對手風格 <color=#43573A><b>快攻型</b></color> 早出怪與直傷壓迫</color>";
            case EnemyAiPlayStyle.Defensive:
                return "<color=#6C533D>對手風格 <color=#43573A><b>防禦型</b></color> 囤牌待時機反擊</color>";
            default:
                return "<color=#6C533D>對手風格 <color=#43573A><b>綜合型</b></color> 攻守均衡</color>";
        }
    }

    public static string GetPreviewLeftDetailRich(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.FastAttack:
                return "<color=#43573A>簡單級節奏較緩 普通級壓力提升 困難級需善用防守與拆場法術</color>";
            case EnemyAiPlayStyle.Defensive:
                return "<color=#43573A>簡單級可熟悉囤牌節奏 普通級需把握進攻窗口 困難級對手更嚴格保留高稀有卡</color>";
            default:
                return "<color=#43573A>簡單級適合暖身 普通級節奏均衡 困難級整體壓力較高</color>";
        }
    }

    public static string GetPreviewRightDetailRich(EnemyAiPlayStyle style)
    {
        string aiLine = EnemyAiPlayStyleCatalog.GetBriefZh(style);
        return "<color=#43573A>" + aiLine + "</color>";
    }

    public static string GetEnemyHeroDisplayName(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.FastAttack:
                return "快攻陪練";
            case EnemyAiPlayStyle.Defensive:
                return "防禦陪練";
            default:
                return "綜合陪練";
        }
    }

    public static string GetAiStyleDisplayZh(EnemyAiPlayStyle style)
    {
        switch (style)
        {
            case EnemyAiPlayStyle.FastAttack:
                return "快攻型";
            case EnemyAiPlayStyle.Defensive:
                return "防禦型";
            default:
                return "綜合型";
        }
    }

    public const string PreviewLeftTitleRich = "<b>對戰提示</b>";
    public const string PreviewRightTitleRich = "<b>AI 說明</b>";

    /// <summary>自由對戰開戰前：鬥鳥暖身賽隨機事件觸發機率。</summary>
    public const float BirdDuelRandomEventChance = 0.7f;
}

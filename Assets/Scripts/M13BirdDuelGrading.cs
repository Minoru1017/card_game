/// <summary>M-1-3 分波鬥鳥評級（LEVEL_DESIGN_M-1-3.md §五 S 評）。</summary>
public static class M13BirdDuelGrading
{
    /// <summary>8 拍局滿分 15；S 評需勝利且分數 ≥12。</summary>
    public const int SRMinScore = 12;

    public static bool IsSRank(int score, BirdDuelResult result) =>
        result == BirdDuelResult.Win && score >= SRMinScore;

    public static string BuildRewardLine(bool sRank)
    {
        if (sRank)
        {
            return "S 評 · 分波手\n冷爐迎測可三選一開局天氣 · 分波對決多抽 1 張";
        }

        return "完成分波對齊 · 可繼續迎潮實測";
    }
}

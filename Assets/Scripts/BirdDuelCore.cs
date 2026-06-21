using System;

/// <summary>
/// 鬥鳥暖身賽核心規則（純邏輯，無 Unity 相依，便於測試與重用）。
/// 型別定義見 <see cref="BirdDuelTypes"/>。
/// </summary>
public static class BirdDuelCore
{
    public const int DefaultWinThreshold = 7;
    public const int DefaultDrawThreshold = 4;
    public const int MaxInsightTier = 3;
    public const int DefaultPassLimit = 2;
    public const float DefaultPerfectWindow = 0.14f;
    public const float DefaultGoodWindow = 0.30f;

    public static BirdGesture BestCounter(BirdGesture opponent)
    {
        switch (opponent)
        {
            case BirdGesture.Peck: return BirdGesture.Wing;
            case BirdGesture.Wing: return BirdGesture.Nest;
            case BirdGesture.Nest: return BirdGesture.Peck;
            default: return BirdGesture.Peck;
        }
    }

    public static int BestCounterScore(BirdGesture opponent)
    {
        switch (opponent)
        {
            case BirdGesture.Nest: return 3;
            case BirdGesture.Peck: return 2;
            case BirdGesture.Wing: return 0;
            default: return 0;
        }
    }

    public static bool BestCounterGivesInsight(BirdGesture opponent) =>
        opponent == BirdGesture.Wing;

    public static BirdBeatJudgement Judge(
        BirdGesture opponent,
        BirdGesture? input,
        float timingError,
        bool passRewardAvailable,
        float perfectWindow = DefaultPerfectWindow,
        float goodWindow = DefaultGoodWindow)
    {
        if (!input.HasValue)
            return MissJudgement(opponent);

        if (timingError > goodWindow)
            return MissJudgement(opponent);

        BirdGesture gesture = input.Value;
        if (gesture == BirdGesture.Pass)
            return PassJudgement(opponent, passRewardAvailable);

        if (gesture == BestCounter(opponent))
            return CounterJudgement(opponent, timingError, perfectWindow);

        return MissJudgement(opponent);
    }

    public static BirdDuelResult ResolveResult(
        int score,
        int winThreshold = DefaultWinThreshold,
        int drawThreshold = DefaultDrawThreshold)
    {
        if (score >= winThreshold) return BirdDuelResult.Win;
        if (score >= drawThreshold) return BirdDuelResult.Draw;
        return BirdDuelResult.Lose;
    }

    public static int ResolveIntelTier(int insight, int letNestThroughCount, int passUsed, int passLimit = DefaultPassLimit)
    {
        int penalty = Math.Max(0, letNestThroughCount) + Math.Max(0, passUsed - passLimit);
        int tier = insight - penalty;
        return Math.Clamp(tier, 0, MaxInsightTier);
    }

    public static string ShortName(BirdGesture gesture)
    {
        switch (gesture)
        {
            case BirdGesture.Peck: return "啄";
            case BirdGesture.Wing: return "翅";
            case BirdGesture.Nest: return "巢";
            case BirdGesture.Pass: return "守";
            default: return "?";
        }
    }

    public static string DisplayName(BirdGesture gesture)
    {
        switch (gesture)
        {
            case BirdGesture.Peck: return "啄擊";
            case BirdGesture.Wing: return "振翅";
            case BirdGesture.Nest: return "築巢";
            case BirdGesture.Pass: return "PASS";
            default: return "?";
        }
    }

    private static BirdBeatJudgement MissJudgement(BirdGesture opponent)
    {
        return new BirdBeatJudgement
        {
            outcome = BirdBeatOutcome.Miss,
            letNestThrough = opponent == BirdGesture.Nest
        };
    }

    private static BirdBeatJudgement PassJudgement(BirdGesture opponent, bool passRewardAvailable)
    {
        return new BirdBeatJudgement
        {
            outcome = BirdBeatOutcome.Guard,
            scoreDelta = passRewardAvailable ? 1 : 0,
            letNestThrough = opponent == BirdGesture.Nest
        };
    }

    private static BirdBeatJudgement CounterJudgement(BirdGesture opponent, float timingError, float perfectWindow)
    {
        return new BirdBeatJudgement
        {
            outcome = timingError <= perfectWindow ? BirdBeatOutcome.Perfect : BirdBeatOutcome.Good,
            scoreDelta = BestCounterScore(opponent),
            insightDelta = BestCounterGivesInsight(opponent) ? 1 : 0,
            isBestCounter = true,
            letNestThrough = false
        };
    }
}

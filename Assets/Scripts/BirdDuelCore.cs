using System;

/// <summary>
/// 鬥鳥暖身賽核心規則（純邏輯，無 Unity 相依，便於測試與重用）。
/// 規格來源：Docs/鬥鳥手勢小遊戲企劃.md（BGM 鼓點節奏反制、看破、PASS、雙進度、不設生命條）。
/// </summary>
public enum BirdGesture
{
    Peck,   // 啄擊：進攻、打斷對方築巢
    Wing,   // 振翅：防守、閃開對方啄擊
    Nest,   // 築巢：佈局、成功反制振翅取得看破
    Pass    // PASS：保守防禦，踩準鼓點時低分穩住
}

public enum BirdBeatOutcome
{
    Perfect, // 鳥勢正確且踩準鼓點中心
    Good,    // 鳥勢正確但稍早／稍晚
    Guard,   // PASS 且踩準鼓點，低分防禦
    Miss     // 鳥勢錯誤、太早、太晚或未按
}

public enum BirdDuelResult
{
    Win,
    Draw,
    Lose
}

/// <summary>單一鼓點的判定結果。</summary>
public struct BirdBeatJudgement
{
    public BirdBeatOutcome outcome;
    public int scoreDelta;
    public int insightDelta;
    public bool isBestCounter;

    /// <summary>對手築巢但未被啄擊成功打斷（含 PASS／失誤），用於降低最終情報等級。</summary>
    public bool letNestThrough;
}

public static class BirdDuelCore
{
    // 勝負門檻（6 拍）：>=7 勝、4~6 平、<=3 敗。
    public const int DefaultWinThreshold = 7;
    public const int DefaultDrawThreshold = 4;

    // 看破條與情報層級上限。
    public const int MaxInsightTier = 3;

    // PASS 每局建議次數限制；超過後 PASS 不再給分（但仍避免大失誤）。
    public const int DefaultPassLimit = 2;

    // 節奏判定窗口（秒），可由控制器覆寫。
    public const float DefaultPerfectWindow = 0.14f;
    public const float DefaultGoodWindow = 0.30f;

    /// <summary>對手鳥勢的最佳反制。</summary>
    public static BirdGesture BestCounter(BirdGesture opponent)
    {
        switch (opponent)
        {
            case BirdGesture.Peck: return BirdGesture.Wing; // 啄擊 → 振翅閃開
            case BirdGesture.Wing: return BirdGesture.Nest; // 振翅 → 築巢看破
            case BirdGesture.Nest: return BirdGesture.Peck; // 築巢 → 啄擊打斷
            default: return BirdGesture.Peck;
        }
    }

    /// <summary>最佳反制成功時的得分（啄擊破巢 +3、振翅閃擊 +2、築巢看破 +0）。</summary>
    public static int BestCounterScore(BirdGesture opponent)
    {
        switch (opponent)
        {
            case BirdGesture.Nest: return 3; // 啄擊打斷築巢，拿分最高
            case BirdGesture.Peck: return 2; // 振翅反制啄擊，穩定拿分
            case BirdGesture.Wing: return 0; // 築巢看破，不拿分換情報
            default: return 0;
        }
    }

    /// <summary>最佳反制是否提供看破（只有反制振翅的築巢給看破）。</summary>
    public static bool BestCounterGivesInsight(BirdGesture opponent)
    {
        return opponent == BirdGesture.Wing;
    }

    /// <summary>
    /// 判定一個鼓點。
    /// </summary>
    /// <param name="opponent">對手本拍鳥勢。</param>
    /// <param name="input">玩家輸入；null 表示未按。</param>
    /// <param name="timingError">玩家輸入與鼓點命中時間的絕對誤差（秒）。未按時可傳任意值。</param>
    /// <param name="passRewardAvailable">本拍 PASS 是否仍可得分（未超過 PASS 次數限制）。</param>
    /// <param name="perfectWindow">Perfect 窗口（秒）。</param>
    /// <param name="goodWindow">Good／Guard 最大窗口（秒）。</param>
    public static BirdBeatJudgement Judge(
        BirdGesture opponent,
        BirdGesture? input,
        float timingError,
        bool passRewardAvailable,
        float perfectWindow = DefaultPerfectWindow,
        float goodWindow = DefaultGoodWindow)
    {
        BirdBeatJudgement judgement = default;

        // 未按：失誤；若對手築巢則視為放過。
        if (!input.HasValue)
        {
            judgement.outcome = BirdBeatOutcome.Miss;
            judgement.letNestThrough = opponent == BirdGesture.Nest;
            return judgement;
        }

        // 節奏超出 Good 窗口：太早或太晚一律失誤。
        if (timingError > goodWindow)
        {
            judgement.outcome = BirdBeatOutcome.Miss;
            judgement.letNestThrough = opponent == BirdGesture.Nest;
            return judgement;
        }

        BirdGesture gesture = input.Value;

        // PASS：踩準鼓點為防禦（Guard）。不觸發看破、不打斷築巢。
        if (gesture == BirdGesture.Pass)
        {
            judgement.outcome = BirdBeatOutcome.Guard;
            judgement.scoreDelta = passRewardAvailable ? 1 : 0;
            judgement.insightDelta = 0;
            judgement.isBestCounter = false;
            judgement.letNestThrough = opponent == BirdGesture.Nest;
            return judgement;
        }

        // 正確反制鳥勢。
        if (gesture == BestCounter(opponent))
        {
            judgement.outcome = timingError <= perfectWindow ? BirdBeatOutcome.Perfect : BirdBeatOutcome.Good;
            judgement.scoreDelta = BestCounterScore(opponent);
            judgement.insightDelta = BestCounterGivesInsight(opponent) ? 1 : 0;
            judgement.isBestCounter = true;
            // 啄擊成功打斷築巢，因此不算放過。
            judgement.letNestThrough = false;
            return judgement;
        }

        // 錯誤鳥勢：失誤。
        judgement.outcome = BirdBeatOutcome.Miss;
        judgement.letNestThrough = opponent == BirdGesture.Nest;
        return judgement;
    }

    /// <summary>依分數條決定鬥鳥勝負。</summary>
    public static BirdDuelResult ResolveResult(
        int score,
        int winThreshold = DefaultWinThreshold,
        int drawThreshold = DefaultDrawThreshold)
    {
        if (score >= winThreshold) return BirdDuelResult.Win;
        if (score >= drawThreshold) return BirdDuelResult.Draw;
        return BirdDuelResult.Lose;
    }

    /// <summary>
    /// 依看破條與懲罰決定最終情報層級（0~3）。
    /// 懲罰來自放過對方築巢、以及超量 PASS。
    /// </summary>
    public static int ResolveIntelTier(int insight, int letNestThroughCount, int passUsed, int passLimit = DefaultPassLimit)
    {
        int penalty = Math.Max(0, letNestThroughCount) + Math.Max(0, passUsed - passLimit);
        int tier = insight - penalty;
        if (tier < 0) tier = 0;
        if (tier > MaxInsightTier) tier = MaxInsightTier;
        return tier;
    }

    /// <summary>常見展示用：鳥勢短名。</summary>
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

    /// <summary>常見展示用：鳥勢全名。</summary>
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
}

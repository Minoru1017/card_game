using System.Collections.Generic;

public static partial class BirdDuelRhythmChart
{
    public const string RiverForkWaveCdId = "river_fork_wave";
    /// <summary>段末屏息拍數（分波／匯流各一次）。</summary>
    public const double RiverForkSuspenseBeats = 2d;
    /// <summary>
    /// 步距 tuned @ 100 BPM：4 數拍 + 4 首步前奏 + 71 步間 + 4 屏息 ≈ 82 拍 ≈ 49s（目標 45–52s）。
    /// </summary>
    public const int RiverForkExpectedBeatSpan = 82;

    private static readonly BirdGesture[] RiverForkPattern =
    {
        BirdGesture.Nest, BirdGesture.Peck, BirdGesture.Wing, BirdGesture.Peck,
        BirdGesture.Nest, BirdGesture.Wing, BirdGesture.Peck, BirdGesture.Nest,
    };

    // 左汊／右汊交替：略長步距拉開記憶點，總局時落在 45–52 秒。
    private static readonly int[] RiverForkStepGaps = { 11, 9, 11, 9, 11, 9, 11 };
    private static readonly int[] RiverForkSuspenseAfterStepIndices = { 3, 6 };
    private const int RiverForkLeftBranchBeats = 4;

    public static bool IsLeftForkStep(int stepIndex) =>
        stepIndex >= 0 && stepIndex < RiverForkLeftBranchBeats;

    public static bool TryGetForkBeatCaption(int stepIndex, out string caption)
    {
        caption = null;
        if (stepIndex < 0 || stepIndex >= RiverForkPattern.Length)
            return false;

        if (IsLeftForkStep(stepIndex))
            caption = "左汊 · " + (stepIndex + 1) + "/4";
        else
            caption = "右汊 · " + (stepIndex - RiverForkLeftBranchBeats + 1) + "/4";
        return true;
    }

    public static float ResolveForkLaneOffsetX(int stepIndex) =>
        IsLeftForkStep(stepIndex) ? -150f : 150f;

    public static bool IsRiverForkWave(string cdId) =>
        string.Equals(cdId, RiverForkWaveCdId, System.StringComparison.OrdinalIgnoreCase);

    public static bool TryGetSuspenseSubtitle(string cdId, int completedStepIndex, out string subtitle)
    {
        subtitle = null;
        if (!IsRiverForkWave(cdId))
            return false;

        if (completedStepIndex == 3)
        {
            subtitle = "—— 分波 ——";
            return true;
        }

        if (completedStepIndex == 6)
        {
            subtitle = "—— 匯流 ——";
            return true;
        }

        return false;
    }

    public static double ResolveSuspenseBeats(string cdId)
    {
        if (IsMorningPrayer(cdId))
            return MorningPrayerSuspenseBeats;
        if (IsRiverForkWave(cdId))
            return RiverForkSuspenseBeats;
        return 0d;
    }
}

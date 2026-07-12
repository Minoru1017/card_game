using System;
using System.Collections.Generic;

/// <summary>
/// 鬥鳥 CD 專屬采音谱面：鳥勢序列、步距（拍）、段落屏息。
/// 無專列時退回 NPC profile 與 RhythmSync 預設步距。
/// </summary>
public static partial class BirdDuelRhythmChart
{
    public const string MorningPrayerCdId = "morning_prayer";
    public const double MorningPrayerSuspenseBeats = 2d;

    public static IReadOnlyList<BirdGesture> ResolveBeatPattern(string cdId, IReadOnlyList<BirdGesture> fallback)
    {
        if (IsMorningPrayer(cdId))
            return MorningPrayerPattern;
        if (IsRiverForkWave(cdId))
            return RiverForkPattern;
        return fallback ?? Array.Empty<BirdGesture>();
    }

    public static bool TryGetNormalStepGap(string cdId, int stepIndex, out double gapBeats)
    {
        gapBeats = 0d;
        if (IsMorningPrayer(cdId))
        {
            if (stepIndex < 0 || stepIndex >= MorningPrayerStepGaps.Length)
                return false;

            gapBeats = MorningPrayerStepGaps[stepIndex];
            return true;
        }

        if (IsRiverForkWave(cdId))
        {
            if (stepIndex < 0 || stepIndex >= RiverForkStepGaps.Length)
                return false;

            gapBeats = RiverForkStepGaps[stepIndex];
            return true;
        }

        return false;
    }

    public static bool ShouldSuspenseAfterStep(string cdId, int completedStepIndex)
    {
        if (IsMorningPrayer(cdId))
        {
            for (int i = 0; i < MorningPrayerSuspenseAfterStepIndices.Length; i++)
            {
                if (MorningPrayerSuspenseAfterStepIndices[i] == completedStepIndex)
                    return true;
            }

            return false;
        }

        if (IsRiverForkWave(cdId))
        {
            for (int i = 0; i < RiverForkSuspenseAfterStepIndices.Length; i++)
            {
                if (RiverForkSuspenseAfterStepIndices[i] == completedStepIndex)
                    return true;
            }
        }

        return false;
    }

    public static bool IsMorningPrayer(string cdId) =>
        string.Equals(cdId, MorningPrayerCdId, StringComparison.OrdinalIgnoreCase);
}

using System.Collections.Generic;

public static partial class BirdDuelRhythmChart
{
    private static readonly BirdGesture[] MorningPrayerPattern =
    {
        BirdGesture.Nest, BirdGesture.Peck, BirdGesture.Nest, BirdGesture.Peck,
        BirdGesture.Wing, BirdGesture.Peck, BirdGesture.Wing, BirdGesture.Peck,
        BirdGesture.Peck, BirdGesture.Wing, BirdGesture.Peck, BirdGesture.Nest,
    };

    private static readonly int[] MorningPrayerStepGaps = { 4, 3, 4, 2, 3, 3, 2, 3, 3, 2, 3 };
    private static readonly int[] MorningPrayerSuspenseAfterStepIndices = { 3, 7 };
}

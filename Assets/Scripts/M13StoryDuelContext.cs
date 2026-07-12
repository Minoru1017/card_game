/// <summary>M-1-3 分波鬥鳥跨場景情境（主線專用；非港灣戰前暖身）。</summary>
public static class M13StoryDuelContext
{
    public static bool IsActive { get; private set; }

    public static void Begin()
    {
        IsActive = true;
        PreBattleDuelContext.ClearActive();
        PreBattleBonusContext.Clear();
        PreBattleCdContext.SetSelectedCd(BirdDuelRhythmChart.RiverForkWaveCdId);
    }

    public static void Clear()
    {
        IsActive = false;
    }
}

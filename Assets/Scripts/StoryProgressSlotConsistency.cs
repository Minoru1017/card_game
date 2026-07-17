/// <summary>Story progress 場景：合併存檔槽一致性檢查，避免同一幀多次 EnsureSlot。</summary>
public static class StoryProgressSlotConsistency
{
    public static void EnsureAll(int slot)
    {
        TutorialProgressState.EnsureSlotIntroProgressConsistent(slot);
        HarborTrainingProgressState.EnsureSlotHarborProgressConsistent(slot);
    }
}

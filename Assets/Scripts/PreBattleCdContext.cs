/// <summary>
/// 戰前鬥鳥所選 CD 光碟（跨場景至 draft／BGM）。
/// 僅<strong>勝利</strong>時 draft 池偏向 CD 白名單；整卡不消耗。見 Docs/鬥鳥手勢小遊戲企劃.md §12。
/// </summary>
public static class PreBattleCdContext
{
    public static string SelectedCdId { get; private set; }

    public static bool HasSelection => !string.IsNullOrWhiteSpace(SelectedCdId);

    public static void SetSelectedCd(string cdId)
    {
        SelectedCdId = string.IsNullOrWhiteSpace(cdId) ? null : cdId.Trim();
    }

    public static bool ShouldUseCdDraftPool(BirdDuelResult result) =>
        result == BirdDuelResult.Win && HasSelection;

    public static void Clear()
    {
        SelectedCdId = null;
    }
}

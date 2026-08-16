/// <summary>A-1 潮間島／島嶼老人支線進度（企劃發想.md §十二）。</summary>
public static class SideQuestA1ProgressState
{
    public const string NodeId = StoryProgressSession.TideIslandSideNodeId;

    public static bool IsSealedSpellReady(int slot) =>
        TutorialProgressState.IsM12SealedSpellFound(slot);

    public static bool IsNodeAvailable(int slot) =>
        TutorialProgressState.IsM12TrioMasteryCleared(slot) &&
        IsSealedSpellReady(slot);

    public static bool IsNodeCleared(int slot) =>
        TutorialProgressState.IsA1TideIslandCleared(slot);

    public static bool IsTideMarkUnsealed(int slot) =>
        TutorialProgressState.IsA1TideMarkUnsealed(slot);

    public static bool IsSeaPurslaneSeedKept(int slot) =>
        TutorialProgressState.IsA1SeaPurslaneSeedKept(slot);

    /// <summary>A-1 通關後、尚未解封潮印時，可在貴重品庫點選封印法術解封。</summary>
    public static bool CanUnsealTideMarkInVault(int slot) =>
        IsNodeCleared(slot) && !IsTideMarkUnsealed(slot);

    public static void MarkSeaPurslaneSeedKept(int slot) =>
        TutorialProgressState.SetA1SeaPurslaneSeedKept(slot, true);

    public static void MarkNodeCleared(int slot) =>
        TutorialProgressState.SetA1TideIslandCleared(slot, true);

    public static void MarkTideMarkUnsealed(int slot) =>
        TutorialProgressState.SetA1TideMarkUnsealed(slot, true);

    public static string BuildLockedHint(int slot)
    {
        if (TutorialProgressState.IsM12TrioMasteryCleared(slot) && !IsSealedSpellReady(slot))
            return "需先在 M-1-2 海牆散策取得封印的法術。";
        if (!TutorialProgressState.IsM12TrioMasteryCleared(slot))
            return "需先通關 M-1-2 海牆巡邏。";
        return string.Empty;
    }
}

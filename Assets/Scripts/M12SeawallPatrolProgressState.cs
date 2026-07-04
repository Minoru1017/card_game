/// <summary>M-1-2 海牆巡邏段考進度（持久旗標 + A 段御三家觸發快照）。</summary>
public static class M12SeawallPatrolProgressState
{
    public static bool IsPhaseAComplete(int slot) =>
        TutorialProgressState.IsM12PhaseAComplete(slot);

    public static bool IsMidPatrolComplete(int slot) =>
        TutorialProgressState.IsM12MidPatrolComplete(slot);

    public static bool IsSealedSpellFound(int slot) =>
        TutorialProgressState.IsM12SealedSpellFound(slot);

    public static bool IsNodeCleared(int slot) =>
        TutorialProgressState.IsM12TrioMasteryCleared(slot);

    public static bool IsNodeAvailable(int slot) =>
        HarborTrainingProgressState.IsHarborCombatCleared(slot);

    public static void RecordPhaseAVictoryWithTrio(int slot)
    {
        slot = UnityEngine.Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        TutorialProgressState.SetM12PhaseAComplete(slot, true);
        TutorialProgressState.SetM12PhaseATrioMilitia(slot, M12TrioMasteryBattleTracker.QueryMilitiaTriggered());
        TutorialProgressState.SetM12PhaseATrioQueen(slot, M12TrioMasteryBattleTracker.QueryQueenTriggered());
        TutorialProgressState.SetM12PhaseATrioKing(slot, M12TrioMasteryBattleTracker.QueryKingTriggered());
    }

    public static bool QueryCombinedMilitiaTriggered(int slot)
    {
        slot = UnityEngine.Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        return TutorialProgressState.IsM12PhaseATrioMilitia(slot) ||
               M12TrioMasteryBattleTracker.QueryMilitiaTriggered();
    }

    public static bool QueryCombinedQueenTriggered(int slot)
    {
        slot = UnityEngine.Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        return TutorialProgressState.IsM12PhaseATrioQueen(slot) ||
               M12TrioMasteryBattleTracker.QueryQueenTriggered();
    }

    public static bool QueryCombinedKingTriggered(int slot)
    {
        slot = UnityEngine.Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        return TutorialProgressState.IsM12PhaseATrioKing(slot) ||
               M12TrioMasteryBattleTracker.QueryKingTriggered();
    }

    public static bool QueryCombinedTrioSatisfied(int slot)
    {
        return QueryCombinedMilitiaTriggered(slot) &&
               QueryCombinedQueenTriggered(slot) &&
               QueryCombinedKingTriggered(slot);
    }

    public static string BuildCombinedTrioMissingHint(int slot)
    {
        bool militia = QueryCombinedMilitiaTriggered(slot);
        bool queen = QueryCombinedQueenTriggered(slot);
        bool king = QueryCombinedKingTriggered(slot);

        if (!militia)
            return "加練目標：御三家合計仍缺 民兵·列陣（A+B）。";
        if (!queen)
            return "加練目標：御三家合計仍缺 王后·王室庇護（A+B）。";
        if (!king)
            return "加練目標：御三家合計仍缺 國王·庭訓號令（A+B）。";
        return string.Empty;
    }

    public static void MarkMidPatrolComplete(int slot, bool sealedSpellFound)
    {
        TutorialProgressState.SetM12MidPatrolComplete(slot, true);
        if (!sealedSpellFound)
            return;

        TutorialProgressState.SetM12SealedSpellFound(slot, true);
        if (ValuablesVaultCatalog.TrySyncSealedSpellRelicToVault(slot) &&
            ValuablesVaultState.HasPendingChanges)
        {
            PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
        }
    }

    public static void MarkNodeCleared(int slot)
    {
        TutorialProgressState.SetM12TrioMasteryCleared(slot, true);
    }
}

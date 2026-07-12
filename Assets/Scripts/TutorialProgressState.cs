using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Per player slot (1–3) tutorial flags stored in playerdata.csv with PlayerPrefs migration.</summary>
public static class TutorialProgressState
{
    private const string PlotKey = "tutorial_plot";
    private const string BattleKey = "tutorial_battle";
    private const string StarterDeckNotifyKey = "tutorial_starter_deck_notify";
    private const string IntroTrioRewardKey = "tutorial_intro_trio_reward";
    private const string M12ReligiousLineRewardKey = "m12_religious_line_reward";
    private const string M12TrioMasteryClearedKey = "m12_trio_mastery_cleared";
    private const string M12PhaseACompleteKey = "m12_phase_a_complete";
    private const string M12PhaseATrioMilitiaKey = "m12_phase_a_trio_militia";
    private const string M12PhaseATrioQueenKey = "m12_phase_a_trio_queen";
    private const string M12PhaseATrioKingKey = "m12_phase_a_trio_king";
    private const string M12MidPatrolCompleteKey = "m12_mid_patrol_complete";
    private const string M12SealedSpellFoundKey = "m12_sealed_spell_found";
    private const string M12IntroSeenKey = "m12_intro_seen";
    private const string M12PhaseADefeatCountKey = "m12_phase_a_defeat_count";
    public const int M12PhaseAExamMemoUnlockDefeatCount = 2;
    private const string M13OpeningSeenKey = "m13_opening_seen";
    private const string M13RoseTrialSeenKey = "m13_rose_trial_seen";
    private const string M13RoseIntactKey = "m13_rose_intact";
    private const string M13RoseBurnedKey = "m13_rose_burned";
    private const string M13PlayerDemandedMiracleKey = "m13_player_demanded_miracle";
    private const string M13RiverForkClearedKey = "m13_river_fork_cleared";
    private const string M13TideMarkGlimmerKey = "m13_tide_mark_glimmer";
    private const string M13BirdDuelCompleteKey = "m13_bird_duel_complete";
    private const string M13BirdDuelSkippedKey = "m13_bird_duel_skipped";
    private const string M13BirdDuelSRankKey = "m13_bird_duel_s_rank";
    private const string M13ForkStrollCompleteKey = "m13_fork_stroll_complete";
    private const string M13ForkSteadyPathKey = "m13_fork_steady_path";
    private const string M13PhaseACompleteKey = "m13_phase_a_complete";
    private const string M13OpeningWeatherPickKey = "m13_opening_weather_pick";
    private const string AcademyGraduatedKey = "academy_intro_graduated";

    private const string PlotDonePrefix = "tutorial_plot_done_v1_slot_";
    private const string BattleDonePrefix = "tutorial_battle_done_v1_slot_";

    public static bool IsTutorialPlotCompleted(int slot) => ReadCompleted(slot, PlotKey, PlotDonePrefix);

    public static bool IsTutorialBattleCompleted(int slot) => ReadCompleted(slot, BattleKey, BattleDonePrefix);

    public static bool IsTutorialFullyCompleted(int slot) =>
        IsTutorialPlotCompleted(slot) && IsTutorialBattleCompleted(slot);

    /// <summary>
    /// 學院入門已畢業：可挑戰港灣實戰（與 <c>harbor_combat_clear</c> 無關）。
    /// 港灣戰敗或未 Clear 時<strong>不</strong>回退入門流程；僅新帳／重置教學時為 false。
    /// </summary>
    public static bool IsAcademyIntroGraduated(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (ReadCompleted(slot, AcademyGraduatedKey, null))
            return true;
        if (IsTutorialFullyCompleted(slot))
        {
            PersistAcademyIntroGraduated(slot);
            return true;
        }

        if (IsIntroTrioRewardGranted(slot))
        {
            PersistAcademyIntroGraduated(slot);
            return true;
        }

        if (IsTutorialBattleCompleted(slot))
        {
            PersistAcademyIntroGraduated(slot);
            return true;
        }

        if (TryRepairAcademyIntroGraduatedFromRuntimeCards(slot) ||
            TryRepairAcademyIntroGraduatedFromSaveCards(slot))
            return true;

        return false;
    }

    /// <summary>
    /// 依旗標、港灣通關、收藏／牌組／存檔列，修復入門與地圖狀態不一致（如已領御三家但 M-1-1 仍 NEW）。
    /// </summary>
    public static void EnsureSlotIntroProgressConsistent(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        SanitizeInflatedIntroProgressIfNeverBattled(slot);
        if (TryRepairFromRecordedProgressSignals(slot))
            return;
        if (TryRepairAcademyIntroGraduatedFromRuntimeCards(slot))
            return;
        TryRepairAcademyIntroGraduatedFromSaveCards(slot);
    }

    /// <summary>從未打過任何對戰卻因商店抽到御三家而寫入的入門旗標，還原為未通關。</summary>
    private static void SanitizeInflatedIntroProgressIfNeverBattled(int slot)
    {
        if (PlayerProfileCsvService.SlotHasAnyBattleRecordOnPlayerSave(slot))
            return;

        bool inflated = ReadSlotFlag(slot, IntroTrioRewardKey) ||
                        ReadSlotFlag(slot, BattleKey) ||
                        ReadSlotFlag(slot, PlotKey) ||
                        ReadSlotFlag(slot, AcademyGraduatedKey);
        if (!inflated)
            return;

        WriteCompleted(slot, PlotKey, PlotDonePrefix, false);
        WriteCompleted(slot, BattleKey, BattleDonePrefix, false);
        WriteCompleted(slot, IntroTrioRewardKey, null, false);
        WriteCompleted(slot, StarterDeckNotifyKey, null, false);
        PersistAcademyIntroGraduated(slot, false);
    }

    /// <summary>進入 Story progress 時修復第一次入門後被存檔清掉的畢業狀態。</summary>
    public static void SyncActiveSlotGraduationFromCollection() =>
        EnsureSlotIntroProgressConsistent(PlayerData.GetActivePlayerSlotOrDefault());

    private static bool TryRepairFromRecordedProgressSignals(int slot)
    {
        if (HarborTrainingProgressState.IsHarborCombatCleared(slot))
        {
            ApplyIntroGraduationRepair(slot);
            return true;
        }

        if (IsIntroTrioRewardGranted(slot) || ReadCompleted(slot, AcademyGraduatedKey, null))
        {
            ApplyIntroGraduationRepair(slot);
            return true;
        }

        if (IsTutorialBattleCompleted(slot))
        {
            ApplyIntroGraduationRepair(slot);
            return true;
        }

        if (IsTutorialPlotCompleted(slot) && !IsTutorialBattleCompleted(slot))
            return false;

        return false;
    }

    /// <summary>作用中槽位記憶體：收藏或任一副牌組內已有御三家。</summary>
    private static bool TryRepairAcademyIntroGraduatedFromRuntimeCards(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (PlayerData.GetActivePlayerSlotOrDefault() != slot)
            return false;

        PlayerData playerData = PlayerData.ResolveCanonical();
        if (playerData == null || !HasIntroTrioOwnedOnRuntimePlayerData(playerData))
            return false;
        if (!CanInferIntroGraduationFromCollection(slot))
            return false;

        ApplyIntroGraduationRepair(slot);
        return true;
    }

    /// <summary>從 playerdata.csv 掃描該槽收藏／牌組列是否已有御三家（記錄點還原後旗標遺失時）。</summary>
    private static bool TryRepairAcademyIntroGraduatedFromSaveCards(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!TryLoadIntroTrioCountsFromSaveForSlot(slot, out Dictionary<int, int> counts))
            return false;
        if (!HasIntroTrioInCollection(counts))
            return false;
        if (!CanInferIntroGraduationFromCollection(slot))
            return false;

        ApplyIntroGraduationRepair(slot);
        return true;
    }

    private static bool CanInferIntroGraduationFromCollection(int slot)
    {
        if (PlayerProfileCsvService.SlotHasAnyBattleRecordOnPlayerSave(slot))
            return true;
        if (HarborTrainingProgressState.IsHarborCombatCleared(slot))
            return true;
        if (ReadSlotFlag(slot, IntroTrioRewardKey))
            return true;
        if (ReadSlotFlag(slot, BattleKey))
            return true;
        return false;
    }

    private static void ApplyIntroGraduationRepair(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!IsIntroTrioRewardGranted(slot))
            WriteCompleted(slot, IntroTrioRewardKey, null, true);
        if (!IsTutorialBattleCompleted(slot))
            SetTutorialBattleCompleted(slot, true);
        if (!IsTutorialPlotCompleted(slot))
            SetTutorialPlotCompleted(slot, true);
        PersistAcademyIntroGraduated(slot);
    }

    private static bool HasIntroTrioOwnedOnRuntimePlayerData(PlayerData playerData)
    {
        if (playerData == null) return false;
        if (HasIntroTrioInCollection(playerData.playerCollection))
            return true;

        int deckSlots = Mathf.Max(1, playerData.deckSlotCount);
        for (int s = 0; s < deckSlots; s++)
        {
            bool allInDeck = true;
            for (int i = 0; i < TutorialBattleRewardService.VictoryCardIds.Length; i++)
            {
                int id = TutorialBattleRewardService.VictoryCardIds[i];
                if (playerData.GetDeckCount(s, id) < 1)
                {
                    allInDeck = false;
                    break;
                }
            }

            if (allInDeck)
                return true;
        }

        return false;
    }

    private static bool TryLoadIntroTrioCountsFromSaveForSlot(int slot, out Dictionary<int, int> counts)
    {
        counts = new Dictionary<int, int>();
        if (!TryLoadSaveLines(out string[] rows))
            return false;

        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row) || row.StartsWith("#", System.StringComparison.Ordinal))
                continue;

            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int rowSlot) || rowSlot != slot) continue;

            string kind = cols[2].Trim();
            if (string.Equals(kind, "card", System.StringComparison.OrdinalIgnoreCase))
                TryAccumulateSaveCardCount(cols, 3, counts);
            else if (string.Equals(kind, "deck", System.StringComparison.OrdinalIgnoreCase))
                TryAccumulateSaveCardCount(cols, 3, counts);
            else if (string.Equals(kind, "deckslot", System.StringComparison.OrdinalIgnoreCase) && cols.Length >= 6)
                TryAccumulateSaveCardCount(cols, 4, counts);
        }

        return counts.Count > 0;
    }

    private static void TryAccumulateSaveCardCount(string[] cols, int typeIndex, Dictionary<int, int> counts)
    {
        if (cols.Length < typeIndex + 2) return;
        string typeToken = cols[typeIndex].Trim();
        if (string.Equals(typeToken, "s", System.StringComparison.OrdinalIgnoreCase))
            return;

        int cardId;
        int numIndex;
        if (string.Equals(typeToken, "m", System.StringComparison.OrdinalIgnoreCase))
        {
            if (cols.Length < typeIndex + 3) return;
            if (!int.TryParse(cols[typeIndex + 1].Trim(), out cardId)) return;
            numIndex = typeIndex + 2;
        }
        else
        {
            if (!int.TryParse(typeToken, out cardId)) return;
            numIndex = typeIndex + 1;
        }

        if (!int.TryParse(cols[numIndex].Trim(), out int num) || num <= 0) return;
        counts.TryGetValue(cardId, out int existing);
        counts[cardId] = existing + num;
    }

    /// <summary>
    /// <see cref="PlayerData.SavePlayerData"/> 重建作用中槽位時，依記憶體收藏補寫入門畢業旗標（第一次入門勝利後御三家剛入收藏、旗標列尚未在 preserve 內時）。
    /// </summary>
    public static void EnsureGraduationFlagRowsInPlayerSave(
        List<string> datas,
        int slot,
        Dictionary<int, int> playerCollection)
    {
        if (datas == null || !HasIntroTrioInCollection(playerCollection))
            return;

        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!CanInferIntroGraduationFromCollection(slot))
            return;
        UpsertSlotFlagRowInSaveList(datas, slot, IntroTrioRewardKey, true);
        UpsertSlotFlagRowInSaveList(datas, slot, BattleKey, true);
        UpsertSlotFlagRowInSaveList(datas, slot, PlotKey, true);
        UpsertSlotFlagRowInSaveList(datas, slot, AcademyGraduatedKey, true);
    }

    private static bool HasIntroTrioInCollection(Dictionary<int, int> playerCollection)
    {
        if (playerCollection == null) return false;
        for (int i = 0; i < TutorialBattleRewardService.VictoryCardIds.Length; i++)
        {
            int id = TutorialBattleRewardService.VictoryCardIds[i];
            if (!playerCollection.TryGetValue(id, out int n) || n < 1)
                return false;
        }

        return true;
    }

    private static void UpsertSlotFlagRowInSaveList(List<string> datas, int slot, string saveKey, bool completed)
    {
        string newRow = FormatSlotFlagRow(slot, saveKey, completed);
        for (int i = 0; i < datas.Count; i++)
        {
            if (!TryParseSlotFlagRow(datas[i], slot, saveKey, out _))
                continue;
            datas[i] = newRow;
            return;
        }

        datas.Add(newRow);
    }

    /// <summary>
    /// Story progress／地圖 UI 用的入門完成度。學院已畢業時 plot／battle 視為完成（不因港灣未 Clear 回退）。
    /// </summary>
    public static void GetAcademyIntroProgressForDisplay(int slot, out bool plotComplete, out bool battleComplete)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        plotComplete = IsTutorialPlotCompleted(slot);
        battleComplete = IsTutorialBattleCompleted(slot);
        if (battleComplete && !plotComplete)
        {
            SetTutorialPlotCompleted(slot, true);
            plotComplete = true;
        }

        if (!IsAcademyIntroGraduated(slot))
            return;

        if (!plotComplete)
        {
            SetTutorialPlotCompleted(slot, true);
            plotComplete = true;
        }

        if (!battleComplete)
        {
            SetTutorialBattleCompleted(slot, true);
            battleComplete = true;
        }
    }

    public static void PersistAcademyIntroGraduated(int slot, bool graduated = true) =>
        WriteCompleted(slot, AcademyGraduatedKey, null, graduated);

    public static bool IsAcademyIntroGraduatedForActivePlayer() =>
        IsAcademyIntroGraduated(PlayerData.GetActivePlayerSlotOrDefault());

    public static bool NeedsTutorialFlow(int slot) => !IsAcademyIntroGraduated(slot);

    public static bool NeedsTutorialFlowForActivePlayer() =>
        NeedsTutorialFlow(PlayerData.GetActivePlayerSlotOrDefault());

    /// <summary>是否已顯示過「獲得基礎牌組」通知（含略過劇情時的簡短提示）。</summary>
    public static bool IsStarterDeckNotifyShown(int slot)
    {
        if (ReadCompleted(slot, StarterDeckNotifyKey, null))
            return true;

        // 舊存檔：已完成入門劇情者視為已看過，避免重溫時再彈窗。
        return IsTutorialPlotCompleted(slot);
    }

    public static bool IsStarterDeckNotifyShownForActivePlayer() =>
        IsStarterDeckNotifyShown(PlayerData.GetActivePlayerSlotOrDefault());

    public static void SetStarterDeckNotifyShown(int slot, bool shown = true) =>
        WriteCompleted(slot, StarterDeckNotifyKey, null, shown);

    /// <summary>入門教學戰御三家（國王／王后／民兵）是否已發放；重溫入門不可再領。</summary>
    public static bool IsIntroTrioRewardGranted(int slot)
    {
        if (ReadCompleted(slot, IntroTrioRewardKey, null))
            return true;

        // 舊存檔：已完成教學戰者視為已領，並寫入旗標避免重溫重複發牌。
        if (IsTutorialBattleCompleted(slot))
        {
            WriteCompleted(slot, IntroTrioRewardKey, null, true);
            return true;
        }

        return false;
    }

    public static bool IsIntroTrioRewardGrantedForActivePlayer() =>
        IsIntroTrioRewardGranted(PlayerData.GetActivePlayerSlotOrDefault());

    public static void SetIntroTrioRewardGranted(int slot, bool granted = true)
    {
        WriteCompleted(slot, IntroTrioRewardKey, null, granted);
        if (granted)
            PersistAcademyIntroGraduated(slot);
    }

    /// <summary>M-1-2 段考：修女／主教／城堡是否已發放（重溫不重發）。</summary>
    public static bool IsM12ReligiousLineRewardGranted(int slot) =>
        ReadCompleted(slot, M12ReligiousLineRewardKey, null);

    public static bool IsM12ReligiousLineRewardGrantedForActivePlayer() =>
        IsM12ReligiousLineRewardGranted(PlayerData.GetActivePlayerSlotOrDefault());

    public static void SetM12ReligiousLineRewardGranted(int slot, bool granted = true) =>
        WriteCompleted(slot, M12ReligiousLineRewardKey, null, granted);

    /// <summary>M-1-2 段考通關（戰技觸發達標 + 兩階段完成）。</summary>
    public static bool IsM12TrioMasteryCleared(int slot) =>
        ReadCompleted(slot, M12TrioMasteryClearedKey, null);

    public static bool IsM12TrioMasteryClearedForActivePlayer() =>
        IsM12TrioMasteryCleared(PlayerData.GetActivePlayerSlotOrDefault());

    public static void SetM12TrioMasteryCleared(int slot, bool cleared = true) =>
        WriteCompleted(slot, M12TrioMasteryClearedKey, null, cleared);

    public static bool IsM12IntroSeen(int slot) =>
        ReadCompleted(slot, M12IntroSeenKey, null);

    public static void SetM12IntroSeen(int slot, bool seen = true) =>
        WriteCompleted(slot, M12IntroSeenKey, null, seen);

    public static int GetM12PhaseADefeatCount(int slot) =>
        ReadSlotCounter(slot, M12PhaseADefeatCountKey);

    /// <summary>段考 A 未通關落敗累計（達 <see cref="M12PhaseAExamMemoUnlockDefeatCount"/> 解鎖段考备忘）。</summary>
    public static int IncrementM12PhaseADefeatCount(int slot) =>
        IncrementSlotCounter(slot, M12PhaseADefeatCountKey);

    public static bool IsM12PhaseAExamMemoUnlocked(int slot) =>
        GetM12PhaseADefeatCount(slot) >= M12PhaseAExamMemoUnlockDefeatCount;

    public static void SetM12PhaseADefeatCount(int slot, int count) =>
        WriteSlotCounter(slot, M12PhaseADefeatCountKey, count);

    public static bool IsM12PhaseAComplete(int slot) =>
        ReadCompleted(slot, M12PhaseACompleteKey, null);

    public static void SetM12PhaseAComplete(int slot, bool complete = true) =>
        WriteCompleted(slot, M12PhaseACompleteKey, null, complete);

    public static bool IsM12PhaseATrioMilitia(int slot) =>
        ReadCompleted(slot, M12PhaseATrioMilitiaKey, null);

    public static void SetM12PhaseATrioMilitia(int slot, bool triggered) =>
        WriteCompleted(slot, M12PhaseATrioMilitiaKey, null, triggered);

    public static bool IsM12PhaseATrioQueen(int slot) =>
        ReadCompleted(slot, M12PhaseATrioQueenKey, null);

    public static void SetM12PhaseATrioQueen(int slot, bool triggered) =>
        WriteCompleted(slot, M12PhaseATrioQueenKey, null, triggered);

    public static bool IsM12PhaseATrioKing(int slot) =>
        ReadCompleted(slot, M12PhaseATrioKingKey, null);

    public static void SetM12PhaseATrioKing(int slot, bool triggered) =>
        WriteCompleted(slot, M12PhaseATrioKingKey, null, triggered);

    public static bool IsM12MidPatrolComplete(int slot) =>
        ReadCompleted(slot, M12MidPatrolCompleteKey, null);

    public static void SetM12MidPatrolComplete(int slot, bool complete = true) =>
        WriteCompleted(slot, M12MidPatrolCompleteKey, null, complete);

    public static bool IsM12SealedSpellFound(int slot) =>
        ReadCompleted(slot, M12SealedSpellFoundKey, null);

    public static void SetM12SealedSpellFound(int slot, bool found = true) =>
        WriteCompleted(slot, M12SealedSpellFoundKey, null, found);

    public static bool IsM13OpeningSeen(int slot) =>
        ReadCompleted(slot, M13OpeningSeenKey, null);

    public static void SetM13OpeningSeen(int slot, bool seen = true) =>
        WriteCompleted(slot, M13OpeningSeenKey, null, seen);

    public static bool IsM13RoseTrialSeen(int slot) =>
        ReadCompleted(slot, M13RoseTrialSeenKey, null);

    public static void SetM13RoseTrialSeen(int slot, bool seen = true) =>
        WriteCompleted(slot, M13RoseTrialSeenKey, null, seen);

    public static bool IsM13RoseIntact(int slot) =>
        ReadCompleted(slot, M13RoseIntactKey, null);

    public static void SetM13RoseIntact(int slot, bool intact = true)
    {
        WriteCompleted(slot, M13RoseIntactKey, null, intact);
        if (intact)
            WriteCompleted(slot, M13RoseBurnedKey, null, false);
    }

    public static bool IsM13RoseBurned(int slot) =>
        ReadCompleted(slot, M13RoseBurnedKey, null);

    public static void SetM13RoseBurned(int slot, bool burned = true)
    {
        WriteCompleted(slot, M13RoseBurnedKey, null, burned);
        if (burned)
            WriteCompleted(slot, M13RoseIntactKey, null, false);
    }

    public static bool IsM13PlayerDemandedMiracle(int slot) =>
        ReadCompleted(slot, M13PlayerDemandedMiracleKey, null);

    public static void SetM13PlayerDemandedMiracle(int slot, bool demanded = true) =>
        WriteCompleted(slot, M13PlayerDemandedMiracleKey, null, demanded);

    public static bool IsM13RiverForkCleared(int slot) =>
        ReadCompleted(slot, M13RiverForkClearedKey, null);

    public static void SetM13RiverForkCleared(int slot, bool cleared = true) =>
        WriteCompleted(slot, M13RiverForkClearedKey, null, cleared);

    public static bool IsM13TideMarkGlimmer(int slot) =>
        ReadCompleted(slot, M13TideMarkGlimmerKey, null);

    public static void SetM13TideMarkGlimmer(int slot, bool glimmer = true) =>
        WriteCompleted(slot, M13TideMarkGlimmerKey, null, glimmer);

    public static bool IsM13BirdDuelComplete(int slot) =>
        ReadCompleted(slot, M13BirdDuelCompleteKey, null);

    public static bool IsM13BirdDuelSkipped(int slot) =>
        ReadCompleted(slot, M13BirdDuelSkippedKey, null);

    public static bool IsM13BirdDuelSRank(int slot) =>
        ReadCompleted(slot, M13BirdDuelSRankKey, null);

    public static void SetM13BirdDuelComplete(int slot, bool skipped, bool sRank) =>
        SetM13BirdDuelProgress(slot, complete: true, skipped, sRank);

    public static void SetM13BirdDuelProgress(int slot, bool complete, bool skipped = false, bool sRank = false)
    {
        WriteCompleted(slot, M13BirdDuelCompleteKey, null, complete);
        WriteCompleted(slot, M13BirdDuelSkippedKey, null, complete && skipped);
        WriteCompleted(slot, M13BirdDuelSRankKey, null, complete && !skipped && sRank);
    }

    public static bool IsM13ForkStrollComplete(int slot) =>
        ReadCompleted(slot, M13ForkStrollCompleteKey, null);

    public static bool IsM13ForkSteadyPath(int slot) =>
        ReadCompleted(slot, M13ForkSteadyPathKey, null);

    public static bool IsM13PhaseAComplete(int slot) =>
        ReadCompleted(slot, M13PhaseACompleteKey, null);

    public static int ReadM13OpeningWeatherPick(int slot) =>
        ReadSlotCounter(slot, M13OpeningWeatherPickKey);

    public static void SetM13ForkStrollComplete(int slot, bool steadyPath) =>
        SetM13ForkStrollProgress(slot, complete: true, steadyPath);

    public static void SetM13ForkStrollProgress(int slot, bool complete, bool steadyPath = false)
    {
        WriteCompleted(slot, M13ForkStrollCompleteKey, null, complete);
        WriteCompleted(slot, M13ForkSteadyPathKey, null, complete && steadyPath);
    }

    public static void SetM13PhaseAComplete(int slot, bool complete = true) =>
        WriteCompleted(slot, M13PhaseACompleteKey, null, complete);

    public static void SetM13OpeningWeatherPick(int slot, M13OpeningWeatherPick pick) =>
        WriteSlotCounter(slot, M13OpeningWeatherPickKey, (int)pick);

    /// <summary>重溫 1-3：清中段旗標，保留通關／潮印／S 評。</summary>
    public static void ResetM13ReplayRunProgress(int slot)
    {
        WriteCompleted(slot, M13OpeningSeenKey, null, false);
        WriteCompleted(slot, M13BirdDuelCompleteKey, null, false);
        WriteCompleted(slot, M13BirdDuelSkippedKey, null, false);
        WriteCompleted(slot, M13ForkStrollCompleteKey, null, false);
        WriteCompleted(slot, M13ForkSteadyPathKey, null, false);
        WriteCompleted(slot, M13PhaseACompleteKey, null, false);
        WriteCompleted(slot, M13RoseTrialSeenKey, null, false);
        WriteCompleted(slot, M13RoseIntactKey, null, false);
        WriteCompleted(slot, M13RoseBurnedKey, null, false);
        WriteCompleted(slot, M13PlayerDemandedMiracleKey, null, false);
        WriteSlotCounter(slot, M13OpeningWeatherPickKey, 0);
    }

    public static void SetTutorialPlotCompleted(int slot, bool completed = true) =>
        WriteCompleted(slot, PlotKey, PlotDonePrefix, completed);

    public static void SetTutorialBattleCompleted(int slot, bool completed = true)
    {
        WriteCompleted(slot, BattleKey, BattleDonePrefix, completed);
        if (completed)
            PersistAcademyIntroGraduated(slot);
    }

    public static void ResetTutorialForSlot(int slot)
    {
        WriteCompleted(slot, PlotKey, PlotDonePrefix, false);
        WriteCompleted(slot, BattleKey, BattleDonePrefix, false);
        WriteCompleted(slot, StarterDeckNotifyKey, null, false);
        WriteCompleted(slot, IntroTrioRewardKey, null, false);
        WriteCompleted(slot, AcademyGraduatedKey, null, false);
        HarborTrainingProgressState.ResetHarborTrainingForSlot(slot);
    }

    /// <summary>讀取 slot 旗標列（harbor_combat_clear 等，無 PlayerPrefs 遷移）。</summary>
    public static bool ReadSlotFlag(int slot, string saveKey) =>
        ReadCompleted(slot, saveKey, null);

    /// <summary>寫入 slot 旗標列。</summary>
    public static void WriteSlotFlag(int slot, string saveKey, bool value) =>
        WriteCompleted(slot, saveKey, null, value);

    public static int ReadSlotCounter(int slot, string saveKey)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!TryLoadSaveLines(out string[] rows))
            return 0;

        for (int i = 0; i < rows.Length; i++)
        {
            if (TryParseSlotFlagRow(rows[i], slot, saveKey, out int value))
                return Mathf.Max(0, value);
        }

        return 0;
    }

    public static int IncrementSlotCounter(int slot, string saveKey)
    {
        int next = ReadSlotCounter(slot, saveKey) + 1;
        WriteSlotCounter(slot, saveKey, next);
        return next;
    }

    public static void WriteSlotCounter(int slot, string saveKey, int value)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        value = Mathf.Max(0, value);
        string newRow = FormatSlotFlagCounterRow(slot, saveKey, value);
        PlayerSaveCoordinator.UpsertSlotKeyedRow(
            slot,
            saveKey,
            newRow,
            row => TryParseSlotFlagRow(row, slot, saveKey, out _));
    }

    private static bool ReadCompleted(int slot, string saveKey, string legacyPrefsPrefix)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (TryReadFromSave(slot, saveKey, out bool saved))
            return saved;

        if (!string.IsNullOrEmpty(legacyPrefsPrefix) &&
            PlayerPrefs.GetInt(legacyPrefsPrefix + slot, 0) == 1)
        {
            WriteCompleted(slot, saveKey, legacyPrefsPrefix, true);
            return true;
        }

        return false;
    }

    private static void WriteCompleted(int slot, string saveKey, string legacyPrefsPrefix, bool completed)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        WriteToSave(slot, saveKey, completed);
        if (!string.IsNullOrEmpty(legacyPrefsPrefix))
        {
            PlayerPrefs.DeleteKey(legacyPrefsPrefix + slot);
            PlayerPrefs.Save();
        }
    }

    private static bool TryReadFromSave(int slot, string saveKey, out bool completed)
    {
        completed = false;
        if (!TryLoadSaveLines(out string[] rows))
            return false;

        for (int i = 0; i < rows.Length; i++)
        {
            if (!TryParseSlotFlagRow(rows[i], slot, saveKey, out int value))
                continue;
            completed = value == 1;
            return true;
        }

        return false;
    }

    private static void WriteToSave(int slot, string saveKey, bool completed)
    {
        string newRow = FormatSlotFlagRow(slot, saveKey, completed);
        PlayerSaveCoordinator.UpsertSlotKeyedRow(
            slot,
            saveKey,
            newRow,
            row => TryParseSlotFlagRow(row, slot, saveKey, out _));
    }

    private static bool TryLoadSaveLines(out string[] rows)
    {
        rows = System.Array.Empty<string>();
        return PlayerSaveCoordinator.TryReadPlayerDataLines(out rows, out _);
    }

    private static string FormatSlotFlagRow(int slot, string saveKey, bool completed) =>
        FormatSlotFlagCounterRow(slot, saveKey, completed ? 1 : 0);

    private static string FormatSlotFlagCounterRow(int slot, string saveKey, int value) =>
        $"slot,{slot},{saveKey},{value}";

    private static bool TryParseSlotFlagRow(string row, int slot, string saveKey, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(row)) return false;

        string[] cols = row.Split(',');
        if (cols.Length < 4) return false;
        if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (!int.TryParse(cols[1].Trim(), out int rowSlot) || rowSlot != slot) return false;
        if (!string.Equals(cols[2].Trim(), saveKey, System.StringComparison.OrdinalIgnoreCase)) return false;
        return int.TryParse(cols[3].Trim(), out value);
    }
}

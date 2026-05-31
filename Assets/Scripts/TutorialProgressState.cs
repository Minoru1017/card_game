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

        if (TryRepairAcademyIntroGraduatedFromCollection(slot))
            return true;

        return false;
    }

    /// <summary>
    /// 存檔旗標被 SavePlayerData 誤刪時，若收藏仍有御三家則還原入門畢業（不重發卡牌）。
    /// </summary>
    private static bool TryRepairAcademyIntroGraduatedFromCollection(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (PlayerData.GetActivePlayerSlotOrDefault() != slot)
            return false;

        PlayerData playerData = PlayerData.ResolveCanonical();
        if (playerData == null || !HasIntroTrioInCollection(playerData))
            return false;

        if (!IsIntroTrioRewardGranted(slot))
            WriteCompleted(slot, IntroTrioRewardKey, null, true);
        if (!IsTutorialBattleCompleted(slot))
            SetTutorialBattleCompleted(slot, true);
        if (!IsTutorialPlotCompleted(slot))
            SetTutorialPlotCompleted(slot, true);
        PersistAcademyIntroGraduated(slot);
        return true;
    }

    private static bool HasIntroTrioInCollection(PlayerData playerData) =>
        HasIntroTrioInCollection(playerData?.playerCollection);

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
        UpsertSlotFlagRowInSaveList(datas, slot, IntroTrioRewardKey, true);
        UpsertSlotFlagRowInSaveList(datas, slot, BattleKey, true);
        UpsertSlotFlagRowInSaveList(datas, slot, PlotKey, true);
        UpsertSlotFlagRowInSaveList(datas, slot, AcademyGraduatedKey, true);
    }

    /// <summary>進入 Story progress 時修復第一次入門後被存檔清掉的畢業狀態。</summary>
    public static void SyncActiveSlotGraduationFromCollection()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TryRepairAcademyIntroGraduatedFromCollection(slot);
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
        string path = PlayerData.GetPlayerSaveCsvPath();
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);

        string[] existing = PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] read, out _)
            ? read
            : System.Array.Empty<string>();

        var rows = new List<string>(existing.Length + 2);
        bool replaced = false;
        string newRow = FormatSlotFlagRow(slot, saveKey, completed);

        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (!replaced && TryParseSlotFlagRow(row, slot, saveKey, out _))
            {
                rows.Add(newRow);
                replaced = true;
                continue;
            }

            rows.Add(row);
        }

        if (!replaced)
            rows.Add(newRow);

        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(path, rows);
    }

    private static bool TryLoadSaveLines(out string[] rows)
    {
        rows = System.Array.Empty<string>();
        string path = PlayerData.GetPlayerSaveCsvPath();
        return PlayerPersistSafeIO.TryReadPlayerDataLines(path, out rows, out _);
    }

    private static string FormatSlotFlagRow(int slot, string saveKey, bool completed) =>
        $"slot,{slot},{saveKey},{(completed ? 1 : 0)}";

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

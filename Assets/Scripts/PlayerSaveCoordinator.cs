using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 唯一應寫入 <c>playerdata.csv</c> 的入口（原子寫入仍由 <see cref="PlayerPersistSafeIO"/> 執行）。
/// 新功能請勿直接呼叫 <see cref="PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups"/> 寫玩家主檔。
/// </summary>
public static class PlayerSaveCoordinator
{
    public static void WritePlayerDataCsv(IReadOnlyList<string> lines)
    {
        if (lines == null) throw new ArgumentNullException(nameof(lines));

        string path = PlayerData.GetPlayerSaveCsvPath();
        Directory.CreateDirectory(Application.persistentDataPath);
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(path, lines);
    }

    /// <summary>讀取現有主檔列（含備份 fallback），供合併／修補用。</summary>
    public static bool TryReadPlayerDataLines(out string[] lines, out string resolvedPath)
    {
        string path = PlayerData.GetPlayerSaveCsvPath();
        return PlayerPersistSafeIO.TryReadPlayerDataLines(path, out lines, out resolvedPath);
    }

    /// <summary>替換或新增一筆 <c>slot,{slot},{slotKey},...</c> 列（其餘列原樣保留）。</summary>
    public static void UpsertSlotKeyedRow(int playerSlot, string slotKey, string newRow, Func<string, bool> rowMatches)
    {
        if (rowMatches == null) throw new ArgumentNullException(nameof(rowMatches));

        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        string[] existing = TryReadPlayerDataLines(out string[] read, out _)
            ? read
            : Array.Empty<string>();

        var rows = new List<string>(existing.Length + 2);
        bool replaced = false;

        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (!replaced && rowMatches(row))
            {
                rows.Add(newRow);
                replaced = true;
                continue;
            }

            rows.Add(row);
        }

        if (!replaced)
            rows.Add(newRow);

        WritePlayerDataCsv(rows);
    }

    /// <summary>延遲存檔尚未落盤，或貴重品庫記憶體變更尚未寫入。</summary>
    public static bool HasUnpersistedPlayerChanges() =>
        PlayerSaveDebouncer.HasPendingDebouncedSave || ValuablesVaultState.HasPendingChanges;

    /// <summary>僅在有未落盤變更時寫入（App 切背景／結束用）。</summary>
    public static void FlushPendingPlayerDataIfNeeded()
    {
        if (!HasUnpersistedPlayerChanges())
            return;

        FlushDebouncedThenSavePlayerData();
    }

    /// <summary>取消延遲存檔並立即執行一次完整 <see cref="PlayerData.SavePlayerData"/>。</summary>
    public static void FlushDebouncedThenSavePlayerData()
    {
        PlayerSaveDebouncer.CancelPending();
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd != null)
            pd.SavePlayerData();
    }
}

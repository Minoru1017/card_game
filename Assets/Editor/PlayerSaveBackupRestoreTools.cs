using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor 工具：開啟存檔資料夾、整份 .bak 還原（由 <see cref="PlayerSaveRestoreWindow"/> 呼叫）。</summary>
public static class PlayerSaveBackupRestoreTools
{
    private const string MenuRoot = "Card Game/Player Save/";

    [MenuItem(MenuRoot + "Open Save Folder", false, 1)]
    public static void OpenSaveFolder()
    {
        string primary = PlayerData.GetPlayerSaveCsvPath();
        string dir = Path.GetDirectoryName(primary);
        if (string.IsNullOrEmpty(dir))
        {
            EditorUtility.DisplayDialog("Player Save", "無法解析存檔目錄。", "確定");
            return;
        }

        Directory.CreateDirectory(dir);
        EditorUtility.RevealInFinder(primary);
    }

    [MenuItem(MenuRoot + "Restore Backup Window…", false, 50)]
    private static void OpenLegacyRestoreWindow() => PlayerSaveRestoreWindow.ShowWindow();

    internal static bool TrySummarizeSave(string path, out PlayerSaveSummary summary)
    {
        summary = default;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            string[] lines = PlayerPersistSafeIO.ReadAllLines(path);
            if (!PlayerPersistSafeIO.LooksLikePlayerDataCsv(lines))
                return false;

            int activeSlot = ReadActiveSlot(lines);
            TryReadSlotCurrency(lines, activeSlot, out int coins, out int gems, out bool hasCoins, out bool hasGems);

            summary = new PlayerSaveSummary(
                path,
                Path.GetFileName(path),
                File.GetLastWriteTime(path),
                lines.Length,
                activeSlot,
                coins,
                gems,
                hasCoins,
                hasGems);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerSaveBackupRestoreTools: summarize failed -> " + path + " :: " + ex.Message);
            return false;
        }
    }

    internal static bool TryRestoreFromBackup(int backupIndex, out string message)
    {
        message = string.Empty;
        if (PlayerSaveRestoreCore.IsPlayModeBlocked(out message))
            return false;

        if (backupIndex < 1 || backupIndex > PlayerPersistSafeIO.BackupTierCount)
        {
            message = "備份編號無效。";
            return false;
        }

        string primary = PlayerData.GetPlayerSaveCsvPath();
        string source = PlayerPersistSafeIO.GetBackupPath(primary, backupIndex);
        if (!File.Exists(source))
        {
            message = "找不到備份檔：\n" + source;
            return false;
        }

        if (!TrySummarizeSave(source, out PlayerSaveSummary sourceSummary))
        {
            message = "備份檔格式無效：\n" + source;
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(primary) ?? Application.persistentDataPath);

        if (File.Exists(primary))
        {
            string quarantine = primary + ".before-restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(primary, quarantine, true);
        }

        File.Copy(source, primary, true);
        message = "已還原 " + sourceSummary.Label + "\n" +
                  "金幣 " + sourceSummary.CoinsText + " / 寶石 " + sourceSummary.GemsText + "\n" +
                  "主檔：" + primary;
        Debug.Log("[PlayerSaveRestore] " + message.Replace("\n", " | "));
        return true;
    }

    private static int ReadActiveSlot(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            string[] cols = line.Split(',');
            if (cols.Length < 2)
                continue;
            if (!string.Equals(cols[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase))
                continue;
            if (int.TryParse(cols[1].Trim(), out int slot))
                return Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        }

        return 1;
    }

    private static void TryReadSlotCurrency(
        string[] lines,
        int activeSlot,
        out int coins,
        out int gems,
        out bool hasCoins,
        out bool hasGems)
    {
        coins = 0;
        gems = 0;
        hasCoins = false;
        hasGems = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            string[] cols = line.Split(',');
            if (cols.Length < 2)
                continue;

            string key = cols[0].Trim();
            if (string.Equals(key, "coins", StringComparison.OrdinalIgnoreCase) && cols.Length >= 2)
            {
                if (int.TryParse(cols[1].Trim(), out int legacyCoins))
                {
                    coins = legacyCoins;
                    hasCoins = true;
                }

                continue;
            }

            if (string.Equals(key, "gems", StringComparison.OrdinalIgnoreCase) && cols.Length >= 2)
            {
                if (int.TryParse(cols[1].Trim(), out int legacyGems))
                {
                    gems = legacyGems;
                    hasGems = true;
                }

                continue;
            }

            if (!string.Equals(key, "slot", StringComparison.OrdinalIgnoreCase) || cols.Length < 4)
                continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot != activeSlot)
                continue;

            string slotKey = cols[2].Trim();
            if (string.Equals(slotKey, "coins", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(cols[3].Trim(), out int scopedCoins))
            {
                coins = scopedCoins;
                hasCoins = true;
            }
            else if (string.Equals(slotKey, "gems", StringComparison.OrdinalIgnoreCase)
                     && int.TryParse(cols[3].Trim(), out int scopedGems))
            {
                gems = scopedGems;
                hasGems = true;
            }
        }
    }

    internal readonly struct PlayerSaveSummary
    {
        public readonly string Path;
        public readonly string Label;
        public readonly DateTime LastWriteTime;
        public readonly int LineCount;
        public readonly int ActiveSlot;
        public readonly int Coins;
        public readonly int Gems;
        public readonly bool HasCoins;
        public readonly bool HasGems;

        public PlayerSaveSummary(
            string path,
            string label,
            DateTime lastWriteTime,
            int lineCount,
            int activeSlot,
            int coins,
            int gems,
            bool hasCoins,
            bool hasGems)
        {
            Path = path;
            Label = label;
            LastWriteTime = lastWriteTime;
            LineCount = lineCount;
            ActiveSlot = activeSlot;
            Coins = coins;
            Gems = gems;
            HasCoins = hasCoins;
            HasGems = hasGems;
        }

        public bool IsValid => !string.IsNullOrEmpty(Path);
        public string CoinsText => HasCoins ? Coins.ToString("N0") : "?";
        public string GemsText => HasGems ? Gems.ToString("N0") : "?";
        public bool LooksZeroed => HasCoins && HasGems && Coins == 0 && Gems == 0;
    }
}

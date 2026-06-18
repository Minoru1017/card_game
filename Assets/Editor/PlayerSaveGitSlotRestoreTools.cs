using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>從 Git 內的 profile mirror 還原指定角色槽（如 Minoru1017 / slot 3）。</summary>
public static class PlayerSaveGitSlotRestoreTools
{
    private const string MenuRoot = "Card Game/Player Save/";
    private const string GitMirrorPath = "Assets/PlayerDataSnapshots/playerdata.profile_mirror.csv";
    private const int MinoruSlot = 3;

    [MenuItem(MenuRoot + "Restore Minoru1017 (Slot 3) From Git HEAD…")]
    public static void RestoreMinoruFromGitHeadMenu()
    {
        PromptRestoreSlotFromGit("HEAD", MinoruSlot, "Minoru1017", setActiveSlot: true);
    }

    [MenuItem(MenuRoot + "Export Minoru1017 (Slot 3) From Git HEAD To Project")]
    public static void ExportMinoruFromGitHeadToProject()
    {
        if (!TryReadGitMirrorLines("HEAD", out string[] lines, out string error))
        {
            EditorUtility.DisplayDialog("匯出失敗", error, "確定");
            return;
        }

        if (!TryExtractSlotLines(lines, MinoruSlot, out List<string> slotLines, out string slotName))
        {
            EditorUtility.DisplayDialog("匯出失敗", "Git HEAD 中找不到 slot 3 資料。", "確定");
            return;
        }

        string exportPath = Path.Combine(
            Application.dataPath,
            "PlayerDataSnapshots",
            "playerdata.slot3.minoru1017.from-git-head.csv");

        var export = new List<string>(slotLines.Count + 2)
        {
            "active_slot," + MinoruSlot
        };
        export.AddRange(slotLines);
        PlayerPersistSafeIO.WriteAllLines(exportPath, export);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "匯出完成",
            "已匯出 slot 3（" + slotName + "）共 " + slotLines.Count + " 列至：\n" + exportPath,
            "確定");
        EditorUtility.RevealInFinder(exportPath);
    }

    internal static bool PromptRestoreSlotFromGit(
        string gitRev,
        int slot,
        string displayName,
        bool setActiveSlot)
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("還原存檔", "請先停止 Play 模式。", "確定");
            return false;
        }

        if (!TryReadGitMirrorLines(gitRev, out string[] sourceLines, out string gitError))
        {
            EditorUtility.DisplayDialog("還原失敗", gitError, "確定");
            return false;
        }

        if (!TryExtractSlotLines(sourceLines, slot, out List<string> slotLines, out string slotName))
        {
            EditorUtility.DisplayDialog(
                "還原失敗",
                gitRev + " 的 mirror 中找不到 slot " + slot + " 資料。",
                "確定");
            return false;
        }

        string primary = PlayerData.GetPlayerSaveCsvPath();
        TrySummarizeSlot(primary, slot, out int curCoins, out int curGems, out string curName);
        TrySummarizeSlotLines(slotLines, out int newCoins, out int newGems, out _);

        bool ok = EditorUtility.DisplayDialog(
            "還原 " + displayName + "（slot " + slot + "）",
            "來源：Git " + gitRev + " mirror\n" +
            "角色名：" + slotName + "\n" +
            "金幣 " + newCoins + " / 寶石 " + FormatOptional(newGems) + "\n\n" +
            "將合併至正式存檔（保留 slot 1、2 現有資料）：\n" + primary + "\n\n" +
            "目前 slot " + slot + "：" + (string.IsNullOrEmpty(curName) ? "（空）" : curName) +
            "，金幣 " + curCoins + " / 寶石 " + FormatOptional(curGems) + "\n\n" +
            (setActiveSlot ? "還原後作用中槽位會改為 slot " + slot + "。\n\n" : string.Empty) +
            "目前主檔會先另存備份。",
            "還原",
            "取消");

        if (!ok)
            return false;

        if (!TryMergeSlotIntoPrimary(primary, slotLines, slot, setActiveSlot, out string message))
        {
            EditorUtility.DisplayDialog("還原失敗", message, "確定");
            return false;
        }

        EditorUtility.DisplayDialog("還原完成", message, "確定");
        return true;
    }

    internal static bool TryReadGitMirrorLines(string gitRev, out string[] lines, out string error)
    {
        lines = null;
        error = string.Empty;
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            error = "無法解析專案根目錄。";
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "show " + gitRev + ":" + GitMirrorPath,
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using Process process = Process.Start(psi);
            if (process == null)
            {
                error = "無法啟動 git。";
                return false;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stderr)
                    ? "git show 失敗（exit " + process.ExitCode + "）。"
                    : stderr.Trim();
                return false;
            }

            lines = stdout.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            if (!PlayerPersistSafeIO.LooksLikePlayerDataCsv(lines))
            {
                error = "Git mirror 格式無效。";
                lines = null;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = "讀取 Git mirror 失敗： " + ex.Message;
            return false;
        }
    }

    internal static bool TryExtractSlotLines(string[] lines, int slot, out List<string> slotLines, out string slotName)
    {
        slotLines = new List<string>(256);
        slotName = string.Empty;
        if (lines == null || lines.Length == 0)
            return false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            if (!TryParseSlotRow(line, out int rowSlot, out _))
                continue;
            if (rowSlot != slot)
                continue;

            slotLines.Add(line);
            if (string.IsNullOrEmpty(slotName)
                && line.IndexOf(",slot_name,", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string[] cols = line.Split(',');
                if (cols.Length >= 4)
                    slotName = cols[3].Trim();
            }
        }

        return slotLines.Count > 0;
    }

    internal static bool TryMergeSlotIntoPrimary(
        string primaryPath,
        List<string> slotLines,
        int slot,
        bool setActiveSlot,
        out string message)
    {
        message = string.Empty;
        if (slotLines == null || slotLines.Count == 0)
        {
            message = "沒有可合併的 slot 資料。";
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(primaryPath) ?? Application.persistentDataPath);

        List<string> merged;
        if (File.Exists(primaryPath))
        {
            string quarantine = primaryPath + ".before-slot-restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(primaryPath, quarantine, true);
            merged = MergeLines(PlayerPersistSafeIO.ReadAllLines(primaryPath), slotLines, slot, setActiveSlot);
        }
        else
        {
            merged = MergeLines(Array.Empty<string>(), slotLines, slot, setActiveSlot);
        }

        PlayerPersistSafeIO.WriteAllLines(primaryPath, merged);
        TrySummarizeSlotLines(slotLines, out int coins, out int gems, out string slotName);
        message = "已還原 slot " + slot + "（" + slotName + "）至主檔。\n" +
                  "金幣 " + coins + " / 寶石 " + FormatOptional(gems) + "\n" +
                  primaryPath;
        Debug.Log("[PlayerSaveGitSlotRestore] " + message.Replace("\n", " | "));
        return true;
    }

    private static List<string> MergeLines(
        string[] existingLines,
        List<string> slotLines,
        int slot,
        bool setActiveSlot)
    {
        var merged = new List<string>(existingLines.Length + slotLines.Count + 4);
        bool activeWritten = false;

        for (int i = 0; i < existingLines.Length; i++)
        {
            string line = existingLines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cols = line.Split(',');
            if (cols.Length >= 2
                && string.Equals(cols[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase))
            {
                if (setActiveSlot)
                {
                    if (!activeWritten)
                    {
                        merged.Add("active_slot," + slot);
                        activeWritten = true;
                    }
                }
                else if (!activeWritten)
                {
                    merged.Add(line);
                    activeWritten = true;
                }

                continue;
            }

            if (TryParseSlotRow(line, out int rowSlot, out _) && rowSlot == slot)
                continue;

            merged.Add(line);
        }

        if (!activeWritten)
            merged.Insert(0, setActiveSlot ? "active_slot," + slot : "active_slot,1");

        merged.AddRange(slotLines);
        return merged;
    }

    private static bool TryParseSlotRow(string line, out int slot, out string slotKey)
    {
        slot = 0;
        slotKey = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        string[] cols = line.Split(',');
        if (cols.Length < 4)
            return false;
        if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!int.TryParse(cols[1].Trim(), out slot))
            return false;
        slotKey = cols[2].Trim();
        return true;
    }

    private static void TrySummarizeSlot(string path, int slot, out int coins, out int gems, out string slotName)
    {
        coins = 0;
        gems = 0;
        slotName = string.Empty;
        if (!File.Exists(path))
            return;

        string[] lines = PlayerPersistSafeIO.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!TryParseSlotRow(line, out int rowSlot, out string key) || rowSlot != slot)
                continue;

            if (string.Equals(key, "coins", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(line.Split(',')[3].Trim(), out int c))
                coins = c;
            else if (string.Equals(key, "gems", StringComparison.OrdinalIgnoreCase)
                     && int.TryParse(line.Split(',')[3].Trim(), out int g))
                gems = g;
            else if (string.Equals(key, "slot_name", StringComparison.OrdinalIgnoreCase))
                slotName = line.Split(',')[3].Trim();
        }
    }

    private static void TrySummarizeSlotLines(List<string> slotLines, out int coins, out int gems, out string slotName)
    {
        coins = 0;
        gems = 0;
        slotName = string.Empty;
        for (int i = 0; i < slotLines.Count; i++)
        {
            string line = slotLines[i];
            if (!TryParseSlotRow(line, out _, out string key))
                continue;

            string[] cols = line.Split(',');
            if (string.Equals(key, "coins", StringComparison.OrdinalIgnoreCase) && cols.Length >= 4)
                int.TryParse(cols[3].Trim(), out coins);
            else if (string.Equals(key, "gems", StringComparison.OrdinalIgnoreCase) && cols.Length >= 4)
                int.TryParse(cols[3].Trim(), out gems);
            else if (string.Equals(key, "slot_name", StringComparison.OrdinalIgnoreCase) && cols.Length >= 4)
                slotName = cols[3].Trim();
        }
    }

    private static string FormatOptional(int value) =>
        value > 0 ? value.ToString("N0") : "（未記錄）";
}

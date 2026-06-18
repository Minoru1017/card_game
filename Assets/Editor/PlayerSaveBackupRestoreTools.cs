using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor 工具：檢視 playerdata.csv 備份並還原（修復金幣／寶石被覆寫等問題）。</summary>
public static class PlayerSaveBackupRestoreTools
{
    private const string MenuRoot = "Card Game/Player Save/";

    [MenuItem(MenuRoot + "Open Save Folder")]
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

    [MenuItem(MenuRoot + "Restore Backup Window…")]
    public static void ShowRestoreWindow()
    {
        PlayerSaveBackupRestoreWindow.ShowWindow();
    }

    [MenuItem(MenuRoot + "Restore From .bak1")]
    public static void RestoreFromBak1() => PromptAndRestore(1);

    [MenuItem(MenuRoot + "Restore From .bak2")]
    public static void RestoreFromBak2() => PromptAndRestore(2);

    [MenuItem(MenuRoot + "Restore From .bak3")]
    public static void RestoreFromBak3() => PromptAndRestore(3);

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
        if (Application.isPlaying)
        {
            message = "請先停止 Play 模式再還原存檔。";
            return false;
        }

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

    private static void PromptAndRestore(int backupIndex)
    {
        string primary = PlayerData.GetPlayerSaveCsvPath();
        string source = PlayerPersistSafeIO.GetBackupPath(primary, backupIndex);
        if (!File.Exists(source))
        {
            EditorUtility.DisplayDialog(
                "還原 playerdata.csv",
                "找不到 " + Path.GetFileName(source) + "。\n\n" + source,
                "確定");
            return;
        }

        if (!TrySummarizeSave(source, out PlayerSaveSummary backupSummary))
        {
            EditorUtility.DisplayDialog("還原 playerdata.csv", "備份檔格式無效。", "確定");
            return;
        }

        TrySummarizeSave(primary, out PlayerSaveSummary currentSummary);
        string currentLine = currentSummary.IsValid
            ? "目前主檔：金幣 " + currentSummary.CoinsText + " / 寶石 " + currentSummary.GemsText
            : "目前主檔：不存在或格式無效";

        bool ok = EditorUtility.DisplayDialog(
            "還原 playerdata.csv",
            "將以 " + backupSummary.Label + " 覆寫主檔。\n\n" +
            currentLine + "\n" +
            "還原後：金幣 " + backupSummary.CoinsText + " / 寶石 " + backupSummary.GemsText + "\n\n" +
            "目前主檔會先另存為 .before-restore-時間戳 。\n" +
            "還原後請重新進入 Play。",
            "還原",
            "取消");

        if (!ok)
            return;

        if (TryRestoreFromBackup(backupIndex, out string message))
            EditorUtility.DisplayDialog("還原完成", message, "確定");
        else
            EditorUtility.DisplayDialog("還原失敗", message, "確定");
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

public sealed class PlayerSaveBackupRestoreWindow : EditorWindow
{
    private Vector2 scroll;
    private string statusLine = string.Empty;

    public static void ShowWindow()
    {
        var window = GetWindow<PlayerSaveBackupRestoreWindow>(false, "Player Save Restore", true);
        window.minSize = new Vector2(520f, 320f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("playerdata.csv 備份還原", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "若金幣／寶石被誤覆寫為 0，可從 .bak1（最新）～ .bak3 還原。\n" +
            "還原前請停止 Play；目前主檔會先另存為 .before-restore-時間戳。",
            MessageType.Info);

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 模式中無法還原，請先停止。", MessageType.Warning);
        }

        string primary = PlayerData.GetPlayerSaveCsvPath();
        EditorGUILayout.LabelField("存檔路徑", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(primary, EditorStyles.textField, GUILayout.Height(32f));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("開啟資料夾"))
                PlayerSaveBackupRestoreTools.OpenSaveFolder();
            if (GUILayout.Button("重新整理"))
                Repaint();
        }

        EditorGUILayout.Space(8f);
        DrawSummaryRow("主檔 playerdata.csv", primary, restoreBackupIndex: 0);

        for (int i = 1; i <= PlayerPersistSafeIO.BackupTierCount; i++)
        {
            string backupPath = PlayerPersistSafeIO.GetBackupPath(primary, i);
            DrawSummaryRow(".bak" + i, backupPath, i);
        }

        if (!string.IsNullOrEmpty(statusLine))
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(statusLine, MessageType.None);
        }
    }

    private void DrawSummaryRow(string title, string path, int restoreBackupIndex)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (!File.Exists(path))
        {
            EditorGUILayout.LabelField("(檔案不存在)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        if (!PlayerSaveBackupRestoreTools.TrySummarizeSave(path, out PlayerSaveBackupRestoreTools.PlayerSaveSummary summary))
        {
            EditorGUILayout.LabelField("(格式無法辨識)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField(
            "修改時間 " + summary.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") +
            "  ·  列數 " + summary.LineCount +
            "  ·  槽位 " + summary.ActiveSlot,
            EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(
                "金幣 " + summary.CoinsText + "    寶石 " + summary.GemsText,
                GUILayout.Width(220f));

            if (summary.LooksZeroed)
            {
                var warnStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.85f, 0.2f, 0.15f) } };
                EditorGUILayout.LabelField("疑似被清零", warnStyle);
            }
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying || restoreBackupIndex <= 0))
        {
            if (restoreBackupIndex > 0
                && GUILayout.Button("還原此備份到主檔", GUILayout.Height(24f)))
            {
                if (TryRestoreWithConfirm(restoreBackupIndex))
                    statusLine = "已還原 .bak" + restoreBackupIndex + " → playerdata.csv";
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static bool TryRestoreWithConfirm(int backupIndex)
    {
        string primary = PlayerData.GetPlayerSaveCsvPath();
        string source = PlayerPersistSafeIO.GetBackupPath(primary, backupIndex);
        if (!PlayerSaveBackupRestoreTools.TrySummarizeSave(source, out var backupSummary))
            return false;

        PlayerSaveBackupRestoreTools.TrySummarizeSave(primary, out var currentSummary);
        string currentLine = currentSummary.IsValid
            ? "目前主檔：金幣 " + currentSummary.CoinsText + " / 寶石 " + currentSummary.GemsText
            : "目前主檔：不存在或格式無效";

        bool ok = EditorUtility.DisplayDialog(
            "還原 playerdata.csv",
            "將以 " + backupSummary.Label + " 覆寫主檔。\n\n" +
            currentLine + "\n" +
            "還原後：金幣 " + backupSummary.CoinsText + " / 寶石 " + backupSummary.GemsText,
            "還原",
            "取消");

        if (!ok)
            return false;

        if (!PlayerSaveBackupRestoreTools.TryRestoreFromBackup(backupIndex, out string message))
        {
            EditorUtility.DisplayDialog("還原失敗", message, "確定");
            return false;
        }

        EditorUtility.DisplayDialog("還原完成", message, "確定");
        return true;
    }
}

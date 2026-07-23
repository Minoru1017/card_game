using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>統一玩家存檔還原視窗：槽位摘要、建議來源、整份／單槽還原。</summary>
public sealed class PlayerSaveRestoreWindow : EditorWindow
{
    private const string MenuPath = "Card Game/Player Save/Restore Window…";

    private Vector2 scroll;
    private string statusLine = string.Empty;

    private List<PlayerSaveRestoreCore.RestoreSource> catalog = new List<PlayerSaveRestoreCore.RestoreSource>();
    private PlayerSaveRestoreCore.SlotPreview[] currentSlots = new PlayerSaveRestoreCore.SlotPreview[PlayerData.MaxPlayerSlots];

    private int selectedSlot = 1;
    private bool setActiveSlotAfterRestore = true;
    private bool showFullFileRestore;
    private int selectedSourceIndex;

    [MenuItem(MenuPath, false, 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerSaveRestoreWindow>(false, "Player Save Restore", true);
        window.minSize = new Vector2(640f, 520f);
        window.RefreshCatalog();
        window.Show();
    }

    [MenuItem(MenuPath, true)]
    private static bool ShowWindowValidate() => !Application.isPlaying;

    private void OnEnable() => RefreshCatalog();

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("玩家存檔還原", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 先看「目前三槽」摘要\n" +
            "2. 選要救的槽位 → 從建議來源還原（只合併該槽，不動其他角色）\n" +
            "3. 若整份壞掉，再用下方「整份覆寫」\n\n" +
            "還原前請停止 Play；主檔會先另存 .before-slot-restore-時間戳。",
            MessageType.Info);

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Play 模式中無法還原，請先停止。", MessageType.Warning);

        DrawToolbar();
        EditorGUILayout.Space(8f);
        DrawCurrentSlots();
        EditorGUILayout.Space(10f);
        DrawPerSlotRestore();
        EditorGUILayout.Space(10f);
        DrawFullFileRestore();

        if (!string.IsNullOrEmpty(statusLine))
            EditorGUILayout.HelpBox(statusLine, MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("開啟存檔資料夾", GUILayout.Height(24f)))
                PlayerSaveBackupRestoreTools.OpenSaveFolder();

            if (GUILayout.Button("重新整理", GUILayout.Height(24f)))
                RefreshCatalog();

            if (GUILayout.Button("完整性監測…", GUILayout.Height(24f)))
                PlayerSaveIntegrityMonitorWindow.ShowWindow();
        }

        EditorGUILayout.LabelField("主檔路徑", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(PlayerSaveRestoreCore.PrimarySavePath, EditorStyles.textField, GUILayout.Height(28f));
    }

    private void DrawCurrentSlots()
    {
        EditorGUILayout.LabelField("目前存檔（三槽摘要）", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        for (int slot = 1; slot <= PlayerData.MaxPlayerSlots; slot++)
        {
            PlayerSaveRestoreCore.SlotPreview s = currentSlots[slot - 1];
            using (new EditorGUILayout.HorizontalScope())
            {
                string flag = s.Occupied ? "● 有資料" : "○ 空槽";
                var style = new GUIStyle(EditorStyles.label);
                if (!s.Occupied)
                    style.normal.textColor = new Color(0.45f, 0.45f, 0.45f);
                EditorGUILayout.LabelField(flag, style, GUILayout.Width(72f));
                EditorGUILayout.LabelField(s.ShortLabel);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPerSlotRestore()
    {
        EditorGUILayout.LabelField("還原單一角色（合併，保留其他槽位）", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("要還原的槽位", GUILayout.Width(100f));
            selectedSlot = EditorGUILayout.IntPopup(
                selectedSlot,
                new[] { "槽位 1", "槽位 2", "槽位 3" },
                new[] { 1, 2, 3 },
                GUILayout.Width(120f));
            setActiveSlotAfterRestore = EditorGUILayout.ToggleLeft("還原後切換到此槽位", setActiveSlotAfterRestore);
        }

        PlayerSaveRestoreCore.SlotPreview current = currentSlots[selectedSlot - 1];
        EditorGUILayout.LabelField("目前：" + current.ShortLabel, EditorStyles.miniLabel);

        List<PlayerSaveRestoreCore.RestoreSource> ranked =
            PlayerSaveRestoreCore.RankSourcesForSlot(catalog, selectedSlot, current);

        if (ranked.Count == 0)
        {
            EditorGUILayout.HelpBox("找不到比目前更好的槽位 " + selectedSlot + " 備份來源。", MessageType.Warning);
            return;
        }

        selectedSourceIndex = Mathf.Clamp(selectedSourceIndex, 0, ranked.Count - 1);
        string[] labels = new string[ranked.Count];
        for (int i = 0; i < ranked.Count; i++)
        {
            PlayerSaveRestoreCore.RestoreSource src = ranked[i];
            PlayerSaveRestoreCore.SlotPreview p = src.GetSlotPreview(selectedSlot);
            labels[i] = (i == 0 ? "★ " : string.Empty) + src.Label + " → " + p.ShortLabel;
        }

        EditorGUILayout.LabelField("建議來源（依完整度排序）", EditorStyles.miniLabel);
        selectedSourceIndex = EditorGUILayout.Popup(selectedSourceIndex, labels);

        PlayerSaveRestoreCore.RestoreSource chosen = ranked[selectedSourceIndex];
        PlayerSaveRestoreCore.SlotPreview preview = chosen.GetSlotPreview(selectedSlot);
        EditorGUILayout.LabelField("來源詳情：" + chosen.Detail, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("還原後預覽：" + preview.ShortLabel, EditorStyles.wordWrappedMiniLabel);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("還原槽位 " + selectedSlot, GUILayout.Height(32f)))
                RestoreSelectedSlot(chosen);
        }
    }

    private void DrawFullFileRestore()
    {
        showFullFileRestore = EditorGUILayout.BeginFoldoutHeaderGroup(showFullFileRestore, "進階：整份 playerdata.csv 覆寫（會替換三槽）");
        if (showFullFileRestore)
        {
            EditorGUILayout.HelpBox(
                "僅在整份主檔損壞、或確定要用某個備份完全取代時使用。\n" +
                "一般救單一角色請用上方「還原單一角色」。",
                MessageType.Warning);

            string primary = PlayerSaveRestoreCore.PrimarySavePath;
            DrawFullFileRow("目前主檔", primary, canRestore: false);

            for (int i = 1; i <= PlayerPersistSafeIO.BackupTierCount; i++)
            {
                string bak = PlayerPersistSafeIO.GetBackupPath(primary, i);
                DrawFullFileRow(".bak" + i + (i == 1 ? "（最新自動備份）" : string.Empty), bak, canRestore: true, backupIndex: i);
            }

            foreach (PlayerSaveRestoreCore.RestoreSource src in catalog)
            {
                if (src.Kind != PlayerSaveRestoreCore.RestoreSourceKind.Archive || !src.Exists)
                    continue;
                DrawFullFileRow(src.Label, src.Path, canRestore: true, fullPath: src.Path);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawFullFileRow(string title, string path, bool canRestore, int backupIndex = 0, string fullPath = null)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        if (!System.IO.File.Exists(path))
        {
            EditorGUILayout.LabelField("(檔案不存在)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        if (PlayerSaveRestoreCore.TryReadLines(path, out string[] lines))
        {
            PlayerSaveRestoreCore.SlotPreview[] slots = PlayerSaveRestoreCore.BuildSlotPreviews(lines);
            for (int i = 0; i < slots.Length; i++)
                EditorGUILayout.LabelField("  " + slots[i].ShortLabel, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "  " + System.IO.File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss") + " · " + lines.Length + " 列",
                EditorStyles.miniLabel);
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying || !canRestore))
        {
            if (canRestore && GUILayout.Button("以整份檔案覆寫主檔", GUILayout.Height(22f)))
                RestoreFullFile(title, path, backupIndex, fullPath);
        }

        EditorGUILayout.EndVertical();
    }

    private void RestoreSelectedSlot(PlayerSaveRestoreCore.RestoreSource source)
    {
        PlayerSaveRestoreCore.SlotPreview current = currentSlots[selectedSlot - 1];
        PlayerSaveRestoreCore.SlotPreview preview = source.GetSlotPreview(selectedSlot);

        bool ok = EditorUtility.DisplayDialog(
            "還原槽位 " + selectedSlot,
            "來源：" + source.Label + "\n\n" +
            "目前：" + current.ShortLabel + "\n" +
            "還原後：" + preview.ShortLabel + "\n\n" +
            "只會替換槽位 " + selectedSlot + " 的資料，其他槽位保留。\n" +
            (setActiveSlotAfterRestore ? "還原後作用中槽位會改為 " + selectedSlot + "。\n\n" : string.Empty) +
            "主檔會先自動備份。",
            "還原",
            "取消");

        if (!ok)
            return;

        if (PlayerSaveRestoreCore.TryRestoreSlotFromSource(source, selectedSlot, setActiveSlotAfterRestore, out string message))
        {
            statusLine = message;
            RefreshCatalog();
            EditorUtility.DisplayDialog("還原完成", message, "確定");
        }
        else
        {
            EditorUtility.DisplayDialog("還原失敗", message, "確定");
        }
    }

    private void RestoreFullFile(string title, string path, int backupIndex, string fullPath)
    {
        bool ok = EditorUtility.DisplayDialog(
            "整份覆寫主檔",
            "將以「" + title + "」完全取代 playerdata.csv（三槽全部替換）。\n\n" + path + "\n\n主檔會先另存備份。",
            "覆寫",
            "取消");
        if (!ok)
            return;

        bool success;
        string message;
        if (backupIndex > 0)
            success = PlayerSaveBackupRestoreTools.TryRestoreFromBackup(backupIndex, out message);
        else
            success = PlayerSaveRestoreCore.TryRestoreFullFile(fullPath ?? path, out message);

        if (success)
        {
            statusLine = message;
            RefreshCatalog();
            EditorUtility.DisplayDialog("還原完成", message, "確定");
        }
        else
        {
            EditorUtility.DisplayDialog("還原失敗", message, "確定");
        }
    }

    private void RefreshCatalog()
    {
        catalog = PlayerSaveRestoreCore.BuildSourceCatalog(includeGitHead: true);
        if (PlayerSaveRestoreCore.TryReadLines(PlayerSaveRestoreCore.PrimarySavePath, out string[] lines))
            currentSlots = PlayerSaveRestoreCore.BuildSlotPreviews(lines);
        else
            currentSlots = new PlayerSaveRestoreCore.SlotPreview[PlayerData.MaxPlayerSlots];

        selectedSourceIndex = 0;
        Repaint();
    }
}

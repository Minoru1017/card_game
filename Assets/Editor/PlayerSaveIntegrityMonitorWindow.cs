using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>存檔完整性監測：檢視快照、告警紀錄與手動掃描。</summary>
public sealed class PlayerSaveIntegrityMonitorWindow : EditorWindow
{
    private Vector2 scroll;
    private PlayerSaveIntegritySnapshot lastSnapshot;
    private List<PlayerSaveIntegrityAlert> alerts = new List<PlayerSaveIntegrityAlert>();
    private string statusLine = string.Empty;

    [MenuItem("Card Game/Player Save/Save Integrity Monitor…")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerSaveIntegrityMonitorWindow>(false, "Save Integrity", true);
        window.minSize = new Vector2(560f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        PlayerSaveIntegrityMonitor.AlertRaised += OnAlertRaised;
        RefreshView();
    }

    private void OnDisable()
    {
        PlayerSaveIntegrityMonitor.AlertRaised -= OnAlertRaised;
    }

    private void OnAlertRaised(PlayerSaveIntegrityAlert alert)
    {
        RefreshView();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("玩家存檔完整性監測", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "每次寫入 playerdata.csv 後，比對上一筆快照。\n" +
            "若金幣／寶石／背包／戰績／貴重品庫疑似被清零，會記錄告警並在 Play 中顯示 toast。",
            MessageType.Info);

        DrawSettings();
        EditorGUILayout.Space(6f);
        DrawActions();
        EditorGUILayout.Space(6f);
        DrawLastSnapshot();
        EditorGUILayout.Space(6f);
        DrawAlerts();

        if (!string.IsNullOrEmpty(statusLine))
            EditorGUILayout.HelpBox(statusLine, MessageType.None);
    }

    private static void DrawSettings()
    {
        bool enabled = EditorPrefs.GetBool(PlayerSaveIntegrityMonitor.PrefsEnabledKey, true);
        bool dialog = EditorPrefs.GetBool(PlayerSaveIntegrityMonitor.PrefsDialogKey, true);

        EditorGUI.BeginChangeCheck();
        enabled = EditorGUILayout.ToggleLeft("啟用監測（寫檔後自動比對）", enabled);
        dialog = EditorGUILayout.ToggleLeft("告警時彈出 Editor 對話框", dialog);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(PlayerSaveIntegrityMonitor.PrefsEnabledKey, enabled);
            EditorPrefs.SetBool(PlayerSaveIntegrityMonitor.PrefsDialogKey, dialog);
        }

        EditorGUILayout.LabelField("監測資料夾", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(PlayerSaveIntegrityMonitor.MonitorDirectory, EditorStyles.textField, GUILayout.Height(28f));
    }

    private void DrawActions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("重新整理"))
                RefreshView();

            if (GUILayout.Button("掃描目前存檔"))
            {
                if (PlayerSaveIntegrityMonitor.TryScanDiskSave("ManualScan", out PlayerSaveIntegrityAlert alert))
                    statusLine = alert != null ? "掃描完成：已觸發告警" : "掃描完成：未發現疑似遺失";
                else
                    statusLine = "掃描失敗：找不到 playerdata.csv";
                RefreshView();
            }

            if (GUILayout.Button("重設基準快照"))
            {
                PlayerSaveIntegrityMonitor.ResetBaselineFromDisk();
                statusLine = "已以目前磁碟存檔作為新基準";
                RefreshView();
            }

            if (GUILayout.Button("開啟監測資料夾"))
            {
                System.IO.Directory.CreateDirectory(PlayerSaveIntegrityMonitor.MonitorDirectory);
                EditorUtility.RevealInFinder(PlayerSaveIntegrityMonitor.MonitorDirectory);
            }
        }
    }

    private void DrawLastSnapshot()
    {
        EditorGUILayout.LabelField("上一筆基準快照", EditorStyles.boldLabel);
        if (lastSnapshot == null)
        {
            EditorGUILayout.LabelField("（尚無）— 下次寫檔或按「重設基準快照」後建立", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.LabelField("時間 UTC", lastSnapshot.capturedUtc, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(lastSnapshot.BuildMetricLine(), EditorStyles.wordWrappedLabel);
    }

    private void DrawAlerts()
    {
        EditorGUILayout.LabelField("告警紀錄（最近 " + alerts.Count + " 筆）", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (alerts.Count == 0)
        {
            EditorGUILayout.LabelField("（尚無告警）", EditorStyles.miniLabel);
        }
        else
        {
            for (int i = alerts.Count - 1; i >= 0; i--)
            {
                PlayerSaveIntegrityAlert alert = alerts[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(alert.summaryLine, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    (string.IsNullOrEmpty(alert.utc) ? string.Empty : alert.utc + "  ") +
                    (string.IsNullOrEmpty(alert.trigger) ? string.Empty : "· " + alert.trigger + "  ") +
                    alert.severity,
                    EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(alert.detailBody))
                    EditorGUILayout.TextArea(alert.detailBody, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshView()
    {
        PlayerSaveIntegrityMonitor.TryLoadLastSnapshot(out lastSnapshot);
        alerts = PlayerSaveIntegrityMonitor.LoadRecentAlerts(40);
    }
}

[InitializeOnLoad]
internal static class PlayerSaveIntegrityMonitorEditorHooks
{
    static PlayerSaveIntegrityMonitorEditorHooks()
    {
        PlayerSaveIntegrityMonitor.AlertRaised += HandleAlert;
    }

    private static void HandleAlert(PlayerSaveIntegrityAlert alert)
    {
        if (!PlayerSaveIntegrityMonitor.ShowEditorDialogOnAlert || alert == null)
            return;

        EditorUtility.DisplayDialog(
            "疑似存檔資料遺失",
            alert.summaryLine + "\n\n" + alert.detailBody,
            "知道了");
    }
}

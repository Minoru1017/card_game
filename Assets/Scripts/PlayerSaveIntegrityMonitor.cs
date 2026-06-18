using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 監測 playerdata.csv 寫入：若金幣／寶石／背包／戰績／貴重品庫疑似被清零，記錄並告警。
/// </summary>
public static class PlayerSaveIntegrityMonitor
{
    public const string PrefsEnabledKey = "CardGame.PlayerSaveMonitor.Enabled";
    public const string PrefsDialogKey = "CardGame.PlayerSaveMonitor.ShowDialog";

    private const string MonitorFolderName = "PlayerSaveMonitor";
    private const string LastSnapshotFileName = "last_snapshot.json";
    private const string AlertsLogFileName = "alerts.jsonl";

    public static event Action<PlayerSaveIntegrityAlert> AlertRaised;

    public static bool IsEnabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(PrefsEnabledKey, true);
#else
            return Debug.isDebugBuild;
#endif
        }
    }

#if UNITY_EDITOR
    public static bool ShowEditorDialogOnAlert =>
        UnityEditor.EditorPrefs.GetBool(PrefsDialogKey, true);
#else
    public static bool ShowEditorDialogOnAlert => false;
#endif

    public static string MonitorDirectory =>
        Path.Combine(Application.persistentDataPath, MonitorFolderName);

    public static string LastSnapshotPath => Path.Combine(MonitorDirectory, LastSnapshotFileName);
    public static string AlertsLogPath => Path.Combine(MonitorDirectory, AlertsLogFileName);

    /// <summary>在 <see cref="PlayerSaveCoordinator.WritePlayerDataCsv"/> 完成後呼叫。</summary>
    public static void NotifyPlayerDataWritten(IReadOnlyList<string> lines, string trigger)
    {
        if (!IsEnabled || lines == null || lines.Count == 0)
            return;

        try
        {
            Directory.CreateDirectory(MonitorDirectory);
            PlayerSaveIntegritySnapshot current = PlayerSaveIntegritySnapshot.CaptureFromCsvLines(lines);
            PlayerSaveIntegritySnapshot previous = TryLoadLastSnapshot();

            if (previous != null
                && TryDetectSuspectedLoss(previous, current, out PlayerSaveIntegrityAlert alert))
            {
                alert.trigger = string.IsNullOrWhiteSpace(trigger) ? "WritePlayerDataCsv" : trigger;
                RecordAlert(alert);
                RaiseAlert(alert);
            }

            SaveLastSnapshot(current);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerSaveIntegrityMonitor: check failed -> " + ex.Message);
        }
    }

    /// <summary>Editor／手動掃描目前磁碟上的 playerdata.csv。</summary>
    public static bool TryScanDiskSave(string trigger, out PlayerSaveIntegrityAlert alert)
    {
        alert = null;
        if (!PlayerSaveCoordinator.TryReadPlayerDataLines(out string[] lines, out _))
            return false;

        NotifyPlayerDataWritten(lines, trigger);
        return TryLoadLatestAlert(out alert);
    }

    public static void ResetBaselineFromDisk()
    {
        if (!PlayerSaveCoordinator.TryReadPlayerDataLines(out string[] lines, out _))
            return;

        Directory.CreateDirectory(MonitorDirectory);
        SaveLastSnapshot(PlayerSaveIntegritySnapshot.CaptureFromCsvLines(lines));
    }

    public static bool TryLoadLastSnapshot(out PlayerSaveIntegritySnapshot snapshot)
    {
        snapshot = TryLoadLastSnapshot();
        return snapshot != null;
    }

    public static List<PlayerSaveIntegrityAlert> LoadRecentAlerts(int maxCount = 30)
    {
        var list = new List<PlayerSaveIntegrityAlert>(maxCount);
        if (!File.Exists(AlertsLogPath))
            return list;

        string[] lines = File.ReadAllLines(AlertsLogPath, Encoding.UTF8);
        for (int i = Math.Max(0, lines.Length - maxCount); i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                list.Add(JsonUtility.FromJson<PlayerSaveIntegrityAlert>(line));
            }
            catch
            {
                // 略過舊格式或損毀列
            }
        }

        return list;
    }

    private static bool TryLoadLatestAlert(out PlayerSaveIntegrityAlert alert)
    {
        alert = null;
        List<PlayerSaveIntegrityAlert> recent = LoadRecentAlerts(1);
        if (recent.Count == 0)
            return false;
        alert = recent[recent.Count - 1];
        return true;
    }

    private static PlayerSaveIntegritySnapshot TryLoadLastSnapshot()
    {
        if (!File.Exists(LastSnapshotPath))
            return null;

        try
        {
            string json = File.ReadAllText(LastSnapshotPath, Encoding.UTF8);
            return JsonUtility.FromJson<PlayerSaveIntegritySnapshot>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLastSnapshot(PlayerSaveIntegritySnapshot snapshot)
    {
        string json = JsonUtility.ToJson(snapshot, true);
        File.WriteAllText(LastSnapshotPath, json, Encoding.UTF8);
    }

    private static void RecordAlert(PlayerSaveIntegrityAlert alert)
    {
        string json = JsonUtility.ToJson(alert);
        File.AppendAllText(AlertsLogPath, json + Environment.NewLine, Encoding.UTF8);
    }

    private static void RaiseAlert(PlayerSaveIntegrityAlert alert)
    {
        Debug.LogError("[PlayerSaveMonitor] " + alert.summaryLine + Environment.NewLine + alert.detailBody);
        AlertRaised?.Invoke(alert);

        if (Application.isPlaying)
            SceneToast.Show("疑似存檔資料遺失\n" + alert.summaryLine, 4.5f);
    }

    internal static bool TryDetectSuspectedLoss(
        PlayerSaveIntegritySnapshot previous,
        PlayerSaveIntegritySnapshot current,
        out PlayerSaveIntegrityAlert alert)
    {
        alert = null;
        if (previous == null || current == null)
            return false;

        if (previous.activeSlot != current.activeSlot)
            return false;

        var drops = new List<string>(8);
        CheckZeroDrop("金幣", previous.coins, current.coins, 1, drops);
        CheckZeroDrop("寶石", previous.gems, current.gems, 1, drops);
        CheckZeroDrop("背包卡牌總數", previous.cardStackTotal, current.cardStackTotal, 5, drops);
        CheckZeroDrop("背包卡牌種類", previous.cardTypeCount, current.cardTypeCount, 3, drops);
        CheckZeroDrop("戰績筆數", previous.battleRecordCount, current.battleRecordCount, 1, drops);
        CheckZeroDrop("戰績勝場", previous.profileWins, current.profileWins, 1, drops);
        CheckZeroDrop("戰績敗場", previous.profileLosses, current.profileLosses, 1, drops);
        CheckZeroDrop("貴重品庫格數", previous.valuablesCellCount, current.valuablesCellCount, 1, drops);

        if (drops.Count == 0)
            return false;

        string slotLabel = "slot " + current.activeSlot;
        if (!string.IsNullOrWhiteSpace(current.slotName))
            slotLabel += " " + current.slotName;
        else if (!string.IsNullOrWhiteSpace(previous.slotName))
            slotLabel += " " + previous.slotName;

        var detail = new StringBuilder(512);
        detail.AppendLine("比對時間：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        detail.AppendLine("角色：" + slotLabel);
        detail.AppendLine("--- 變化 ---");
        for (int i = 0; i < drops.Count; i++)
            detail.AppendLine(drops[i]);
        detail.AppendLine("--- 前次快照 ---");
        detail.AppendLine(previous.BuildMetricLine());
        detail.AppendLine("--- 本次寫入 ---");
        detail.AppendLine(current.BuildMetricLine());

        alert = new PlayerSaveIntegrityAlert
        {
            utc = DateTime.UtcNow.ToString("o"),
            activeSlot = current.activeSlot,
            slotName = string.IsNullOrWhiteSpace(current.slotName) ? previous.slotName : current.slotName,
            severity = drops.Count >= 2 ? "critical" : "warning",
            summaryLine = "疑似存檔資料遺失（" + drops.Count + " 項歸零）：" + slotLabel,
            detailBody = detail.ToString().TrimEnd(),
            previousSnapshotJson = JsonUtility.ToJson(previous),
            currentSnapshotJson = JsonUtility.ToJson(current),
        };
        return true;
    }

    private static void CheckZeroDrop(
        string label,
        int previousValue,
        int currentValue,
        int minimumPrevious,
        List<string> drops)
    {
        if (previousValue >= minimumPrevious && currentValue == 0)
            drops.Add(label + "：" + previousValue + " → 0");
    }
}

[Serializable]
public sealed class PlayerSaveIntegrityAlert
{
    public string utc;
    public string trigger;
    public int activeSlot;
    public string slotName;
    public string severity;
    public string summaryLine;
    public string detailBody;
    public string previousSnapshotJson;
    public string currentSnapshotJson;
}

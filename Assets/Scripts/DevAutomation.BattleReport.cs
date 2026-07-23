using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>DevAutomation 對戰結束戰報匯出與勝負分析。</summary>
public static partial class DevAutomation
{
#if UNITY_EDITOR
    private const string DefaultExportFolder = "Assets/SimResults";

    /// <summary>分析目前對局的勝負關鍵（文字摘要）。</summary>
    public static string GetBattleOutcomeAnalysis()
    {
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle";
        if (!battle.IsBattleOver())
            return "battle not over; result=" + battle.GetBattleResult();

        return BattleHistoryReport.BuildOutcomeAnalysis(
            battle.BattleHistoryEntries,
            battle.GetBattleResult(),
            battle.GetCurrentRound(),
            battle.GetPlayerHeroHp(),
            battle.GetEnemyHeroHp(),
            battle.GetLastBattleOutcomeReason(),
            battle.LastBattleEndedBySurrender);
    }

    /// <summary>匯出本局對戰紀錄至 <c>Assets/SimResults/</c>（或指定路徑）。</summary>
    public static string ExportBattleRecord(string filePath = null)
    {
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle";
        if (!battle.IsBattleOver())
            return "battle not over; call after battle ends";

        return WriteBattleRecordExport(battle, filePath);
    }

    internal static string TryExportBattleRecordAfterAutoPlay(BattleSimulationManager battle)
    {
        if (battle == null || !battle.IsBattleOver())
            return null;
        return WriteBattleRecordExport(battle, null);
    }

    private static string WriteBattleRecordExport(BattleSimulationManager battle, string filePath)
    {
        string text = BattleHistoryReport.BuildFullExportText(
            battle.BattleHistoryEntries,
            battle.GetBattleResult(),
            battle.GetCurrentRound(),
            battle.GetPlayerHeroHp(),
            battle.GetEnemyHeroHp(),
            battle.GetLastBattleOutcomeReason(),
            battle.LastBattleEndedBySurrender,
            SceneManager.GetActiveScene().name);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Directory.CreateDirectory(DefaultExportFolder);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int result = battle.GetBattleResult();
            string resultTag = result == 1 ? "win" : result == -1 ? "loss" : result == 2 ? "draw" : "r" + result;
            filePath = Path.Combine(DefaultExportFolder, "dev_automation_battle_" + stamp + "_" + resultTag + ".md");
        }

        string fullPath = Path.GetFullPath(filePath);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(fullPath, text, Encoding.UTF8);
        Debug.Log("DevAutomation: battle record exported -> " + fullPath);
        return "exported=" + fullPath;
    }
#endif
}

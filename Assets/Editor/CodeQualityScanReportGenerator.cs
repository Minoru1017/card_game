using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 簡化版 heuristics 報告（產出 <c>Docs/CODE_QUALITY_SCAN_REPORT.md</c> 索引）。
/// <para><b>正式掃描</b>請用 Desktop 上的
/// <c>RestartDivergentPath_Data\tools\Run-CardGameScan.ps1 -ProjectRoot …</c>，
/// 報告見 <c>CODE_QUALITY_SCAN_CARDGAME.md</c>。</para>
/// 選單：Tools/Code Quality/Generate Scan Report
/// </summary>
public static class CodeQualityScanReportGenerator
{
    private const string ScriptsFolder = "Assets/Scripts";
    private const string ReportPath = "Docs/CODE_QUALITY_SCAN_REPORT.md";

    private static readonly Regex SceneFindRegex = new Regex(
        @"(?:GameObject\.Find\s*\(|Object\.Find(?:FirstObjectByType|ObjectsByType|ObjectOfType|ObjectsOfType)\s*\()",
        RegexOptions.Compiled);

    [MenuItem("Tools/Code Quality/Generate Scan Report")]
    public static void Generate()
    {
        var groups = ScanGroups();
        string markdown = BuildMarkdown(groups);
        string fullPath = Path.Combine(Application.dataPath, "..", ReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
        File.WriteAllText(fullPath, markdown, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AssetDatabase.Refresh();
        Debug.Log("CodeQualityScanReportGenerator: 已寫入 " + ReportPath);
    }

    private static List<ScanGroup> ScanGroups()
    {
        var map = new Dictionary<string, ScanGroup>(StringComparer.Ordinal);
        string[] files = Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories);
        foreach (string abs in files)
        {
            string rel = abs.Replace('\\', '/');
            if (rel.Contains("/Editor/")) continue;

            string text = File.ReadAllText(abs);
            int lines = text.Split('\n').Length;
            int sceneFinds = SceneFindRegex.Matches(text).Count;
            string fileName = Path.GetFileNameWithoutExtension(abs);
            string baseName = fileName.Split('.')[0];

            if (!map.TryGetValue(baseName, out ScanGroup group))
            {
                group = new ScanGroup { BaseName = baseName };
                map[baseName] = group;
            }

            group.TotalLines += lines;
            group.SceneFinds += sceneFinds;
            group.Files.Add(new ScanFile { RelativePath = rel, Lines = lines, SceneFinds = sceneFinds });
        }

        foreach (ScanGroup g in map.Values)
            g.Score = Score(g.TotalLines, g.SceneFinds);

        return map.Values.OrderBy(g => g.Score).ThenByDescending(g => g.TotalLines).ToList();
    }

    private static int Score(int lines, int sceneFinds)
    {
        int s = 5;
        if (lines > 2000) s = 1;
        else if (lines > 1200) s = 2;
        else if (lines > 800) s = 3;
        else if (lines > 500) s = 4;

        if (sceneFinds > 15) s = Math.Min(s, 2);
        else if (sceneFinds > 8) s = Math.Min(s, 3);
        return s;
    }

    private static string TierLabel(int score) => score switch
    {
        5 => "优",
        4 => "良",
        3 => "尚可",
        2 => "不太好",
        _ => "差",
    };

    private static string BuildMarkdown(List<ScanGroup> groups)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Card Game 程式碼品質分級掃描報告");
        sb.AppendLine();
        sb.AppendLine("> 由 `Tools/Code Quality/Generate Scan Report` 產生。分級為啟發式參考，非編譯或測試結果。");
        sb.AppendLine("> 同一類別的 `partial` 檔案會合併計算（例如 `FightingBirdGameSceneController.*`）。");
        sb.AppendLine();
        sb.AppendLine("## 分級標準");
        sb.AppendLine();
        sb.AppendLine("| 等級 | 分數 | 主要條件 |");
        sb.AppendLine("|------|------|----------|");
        sb.AppendLine("| 优 | 5 | ≤500 行，場景 Find 少 |");
        sb.AppendLine("| 良 | 4 | ≤800 行 |");
        sb.AppendLine("| 尚可 | 3 | ≤1200 行，或結構已 partial 拆分 |");
        sb.AppendLine("| 不太好 | 2 | >1200 行，或場景 Find >15 |");
        sb.AppendLine("| 差 | 1 | >2000 行（優先分期重構） |");
        sb.AppendLine();
        sb.AppendLine("**場景 Find** 僅計 `GameObject.Find` 與 `Object.Find*ObjectByType`，不含自訂 `FindDirectChild` 等 helper。");
        sb.AppendLine();
        sb.AppendLine("## 摘要（" + DateTime.Now.ToString("yyyy-MM-dd") + "）");
        sb.AppendLine();

        AppendTierTable(sb, groups, 1, "差");
        AppendTierTable(sb, groups, 2, "不太好");
        AppendTierTable(sb, groups, 3, "尚可");
        AppendTierTable(sb, groups, 4, "良");
        AppendTierTable(sb, groups, 5, "优");

        sb.AppendLine("## 近期改善");
        sb.AppendLine();
        sb.AppendLine("- **FightingBirdGameSceneController**：由單檔 ~1669 行拆為 `partial`（主檔 ~334 行 + 7 個主題檔），對齊 `BattleSimulationDebugUI.*` 慣例。");
        sb.AppendLine("- **SettingsSceneController**：快取重複 `GameObject.Find`，降低場景查詢次數。");
        sb.AppendLine();
        sb.AppendLine("## 建議下一輪");
        sb.AppendLine();
        sb.AppendLine("1. `BackpackCardInspectPanel`、`MainPlotSceneController`：拆 partial 或抽 UI factory。");
        sb.AppendLine("2. `DeckManager` / `BattleSimulationDebugUI`：僅規劃分期，勿一次大改。");
        sb.AppendLine("3. 新功能優先寫入既有 partial 或純邏輯類（如 `BirdDuelCore`），避免主檔再膨脹。");
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendTierTable(StringBuilder sb, List<ScanGroup> groups, int score, string label)
    {
        var tier = groups.Where(g => g.Score == score).ToList();
        sb.AppendLine("### " + label + "（" + tier.Count + " 類）");
        sb.AppendLine();
        if (tier.Count == 0)
        {
            sb.AppendLine("_（無）_");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| 類別 | 總行數 | 場景 Find | 檔案數 |");
        sb.AppendLine("|------|--------|-----------|--------|");
        foreach (ScanGroup g in tier.Take(score <= 2 ? 20 : 12))
        {
            sb.AppendLine("| `" + g.BaseName + "` | " + g.TotalLines + " | " + g.SceneFinds + " | " + g.Files.Count + " |");
        }

        sb.AppendLine();
    }

    private sealed class ScanGroup
    {
        public string BaseName;
        public int TotalLines;
        public int SceneFinds;
        public int Score;
        public List<ScanFile> Files = new List<ScanFile>();
    }

    private sealed class ScanFile
    {
        public string RelativePath;
        public int Lines;
        public int SceneFinds;
    }
}

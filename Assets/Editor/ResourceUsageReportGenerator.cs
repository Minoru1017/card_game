using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 掃描專案內所有 C# 腳本，盤點「程式如何讀取資源」並產生 Markdown 報表。
/// 重點分類：美術 / UI / 音頻 / 資料設定，方便全觀檢視資源讀取來源。
///
/// 涵蓋的讀取機制：
///   - Resources.Load / Resources.LoadAll / Resources.LoadAsync（執行期主要載入）
///   - AssetDatabase.LoadAssetAtPath（Editor 後備 / 工具）
///
/// 設計目標：可重複執行、不會過期。每次重跑即反映最新程式碼狀態。
/// 選單：Tools/Resources/Generate Resource Usage Report
/// </summary>
public static class ResourceUsageReportGenerator
{
    private const string ReportRelativePath = "docs/ResourceUsageReport.md";

    // 掃描時略過的資料夾（第三方範例 / 自身，避免雜訊與誤判）。
    private static readonly string[] ExcludedPathFragments =
    {
        "/TextMesh Pro/Examples",
        "ResourceUsageReportGenerator.cs",
    };

    private sealed class LoadEntry
    {
        public string Mechanism;     // Resources / AssetDatabase
        public string Variant;       // Load / LoadAll / LoadAsync / LoadAssetAtPath
        public string TypeName;      // 泛型型別，如 Sprite / AudioClip
        public string RawArg;        // 原始引數表達式
        public string ResolvedKey;   // 解析後的 key/路徑（常數已展開；變數以 {name} 標示）
        public bool FullyResolved;   // 是否完全是字面字串
        public string Category;      // 美術 / UI / 音頻 / 資料設定 / 其他
        public string DiskLocation;  // 對應到磁碟的實際資產路徑（找不到為空）
        public bool AssetExists;
        public string SourceFile;    // 相對 Assets 的來源檔
        public int Line;
    }

    [MenuItem("Tools/Resources/Generate Resource Usage Report")]
    public static void Generate()
    {
        string assetsRoot = Application.dataPath;
        string projectRoot = Directory.GetParent(assetsRoot).FullName;

        string[] csFiles = Directory
            .GetFiles(assetsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !IsExcluded(p))
            .ToArray();

        Dictionary<string, string> constMap = BuildStringConstantMap(csFiles);
        Dictionary<string, string> resourceIndex = BuildResourceAssetIndex();

        var entries = new List<LoadEntry>();
        foreach (string file in csFiles)
        {
            string text = File.ReadAllText(file);
            string rel = MakeAssetsRelative(file, assetsRoot);
            entries.AddRange(ScanFile(text, rel, constMap));
        }

        foreach (LoadEntry e in entries)
            ResolveAssetLocation(e, resourceIndex);

        string markdown = BuildMarkdown(entries, csFiles.Length, constMap);

        string reportPath = Path.Combine(projectRoot, ReportRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, markdown, new UTF8Encoding(true));

        Debug.Log($"ResourceUsageReport: 已產生 {entries.Count} 筆讀取點 → {reportPath}");
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(reportPath);
    }

    private static bool IsExcluded(string path)
    {
        string norm = path.Replace('\\', '/');
        return ExcludedPathFragments.Any(frag => norm.Contains(frag));
    }

    private static string MakeAssetsRelative(string fullPath, string assetsRoot)
    {
        string norm = fullPath.Replace('\\', '/');
        string root = assetsRoot.Replace('\\', '/');
        int idx = norm.IndexOf(root, StringComparison.Ordinal);
        return idx >= 0 ? "Assets" + norm.Substring(idx + root.Length) : norm;
    }

    // 收集所有 `const string` 與 `static readonly string` 的字面值，key 用簡單名稱。
    private static Dictionary<string, string> BuildStringConstantMap(IEnumerable<string> files)
    {
        var map = new Dictionary<string, string>();
        var rx = new Regex(
            @"(?:const|static\s+readonly)\s+string\s+(\w+)\s*=\s*@?""([^""]*)""",
            RegexOptions.Compiled);

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            foreach (Match m in rx.Matches(text))
            {
                string name = m.Groups[1].Value;
                string value = m.Groups[2].Value;
                map[name] = value; // 後者覆蓋；同名常數罕見，可接受
            }
        }
        return map;
    }

    // 建立 Resources 資料夾下所有資產的索引：key（不含副檔名、相對 Resources）→ 資產路徑。
    private static Dictionary<string, string> BuildResourceAssetIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            int rIdx = path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
            if (rIdx < 0)
                continue;
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            string afterResources = path.Substring(rIdx + "/Resources/".Length);
            string keyNoExt = StripExtension(afterResources);
            if (!index.ContainsKey(keyNoExt))
                index[keyNoExt] = path;
        }
        return index;
    }

    private static string StripExtension(string p)
    {
        int dot = p.LastIndexOf('.');
        int slash = p.LastIndexOf('/');
        return dot > slash ? p.Substring(0, dot) : p;
    }

    private static IEnumerable<LoadEntry> ScanFile(string text, string relFile, Dictionary<string, string> constMap)
    {
        var results = new List<LoadEntry>();

        var resourcesRx = new Regex(
            @"Resources\.Load(All|Async)?\s*<\s*([\w\.]+)\s*>\s*\(\s*([^;]*?)\)",
            RegexOptions.Compiled | RegexOptions.Singleline);
        foreach (Match m in resourcesRx.Matches(text))
        {
            if (IsCommented(text, m.Index))
                continue;
            string variant = "Load" + (string.IsNullOrEmpty(m.Groups[1].Value) ? "" : m.Groups[1].Value);
            results.Add(MakeEntry("Resources", variant, m.Groups[2].Value, m.Groups[3].Value, relFile, text, m.Index, constMap));
        }

        var adbRx = new Regex(
            @"AssetDatabase\.LoadAssetAtPath\s*<\s*([\w\.]+)\s*>\s*\(\s*([^,;]+?)[,)]",
            RegexOptions.Compiled | RegexOptions.Singleline);
        foreach (Match m in adbRx.Matches(text))
        {
            if (IsCommented(text, m.Index))
                continue;
            results.Add(MakeEntry("AssetDatabase", "LoadAssetAtPath", m.Groups[1].Value, m.Groups[2].Value, relFile, text, m.Index, constMap));
        }

        return results;
    }

    private static LoadEntry MakeEntry(string mechanism, string variant, string typeName, string rawArg,
        string relFile, string text, int matchIndex, Dictionary<string, string> constMap)
    {
        rawArg = rawArg.Trim();
        bool resolved;
        string key = ResolveArgument(rawArg, constMap, out resolved);
        var entry = new LoadEntry
        {
            Mechanism = mechanism,
            Variant = variant,
            TypeName = typeName.Trim(),
            RawArg = rawArg,
            ResolvedKey = key,
            FullyResolved = resolved,
            SourceFile = relFile,
            Line = LineNumberAt(text, matchIndex),
        };
        entry.Category = Categorize(entry.TypeName, key);
        return entry;
    }

    // 解析引數：字面字串、字串相加、或常數名稱。變數無法解析者以 {name} 標示。
    private static string ResolveArgument(string arg, Dictionary<string, string> constMap, out bool fullyResolved)
    {
        fullyResolved = true;
        string[] parts = SplitTopLevelConcat(arg);
        var sb = new StringBuilder();

        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim();
            if (part.Length == 0)
                continue;

            Match lit = Regex.Match(part, @"^@?""(.*)""$", RegexOptions.Singleline);
            if (lit.Success)
            {
                sb.Append(lit.Groups[1].Value);
                continue;
            }

            string simpleName = part.Contains('.') ? part.Substring(part.LastIndexOf('.') + 1) : part;
            simpleName = Regex.Replace(simpleName, @"[^\w]", "");
            if (constMap.TryGetValue(simpleName, out string constVal))
            {
                sb.Append(constVal);
                continue;
            }

            fullyResolved = false;
            sb.Append("{").Append(part).Append("}");
        }

        return sb.ToString();
    }

    // 以最上層 '+' 分割字串相加（忽略字串字面內的 '+'）。
    private static string[] SplitTopLevelConcat(string expr)
    {
        var parts = new List<string>();
        bool inStr = false;
        int start = 0;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '"')
                inStr = !inStr;
            else if (c == '+' && !inStr)
            {
                parts.Add(expr.Substring(start, i - start));
                start = i + 1;
            }
        }
        parts.Add(expr.Substring(start));
        return parts.ToArray();
    }

    private static string Categorize(string typeName, string key)
    {
        string t = typeName.ToLowerInvariant();
        string k = (key ?? "").ToLowerInvariant();

        if (t.Contains("audioclip"))
            return "音頻";
        if (t.Contains("textasset"))
            return "資料設定";
        if (t.Contains("font") || t.Contains("material"))
            return "UI（字型/材質）";
        if (t.Contains("gameobject"))
            return "UI（Prefab）";

        if (t.Contains("sprite") || t.Contains("texture"))
        {
            bool looksUi = k.Contains("ui/") || k.Contains("rarity") || k.Contains("returnbutton")
                           || k.Contains("guide") || k.Contains("button") || k.Contains("icon");
            return looksUi ? "UI（圖像）" : "美術";
        }

        return "其他";
    }

    private static void ResolveAssetLocation(LoadEntry e, Dictionary<string, string> resourceIndex)
    {
        if (e.Mechanism == "AssetDatabase")
        {
            // 引數本身即為 Assets 路徑
            if (e.FullyResolved && !string.IsNullOrEmpty(e.ResolvedKey))
            {
                e.DiskLocation = e.ResolvedKey;
                e.AssetExists = File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, e.ResolvedKey));
            }
            return;
        }

        if (!e.FullyResolved || string.IsNullOrEmpty(e.ResolvedKey))
            return;

        if (resourceIndex.TryGetValue(e.ResolvedKey, out string assetPath))
        {
            e.DiskLocation = assetPath;
            e.AssetExists = true;
        }
        else
        {
            // LoadAll 常指向資料夾；檢查是否有任何資產位於該前綴下
            string prefix = e.ResolvedKey + "/";
            string folderHit = resourceIndex.Keys.FirstOrDefault(kk => kk.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (folderHit != null)
            {
                e.DiskLocation = "(資料夾) " + Path.GetDirectoryName(resourceIndex[folderHit]).Replace('\\', '/');
                e.AssetExists = true;
            }
            else
            {
                e.AssetExists = false;
            }
        }
    }

    private static string BuildMarkdown(List<LoadEntry> entries, int scannedFiles, Dictionary<string, string> constMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# 資源讀取清單（自動產生）");
        sb.AppendLine();
        sb.AppendLine($"> 由 `Tools/Resources/Generate Resource Usage Report` 產生於 {DateTime.Now:yyyy-MM-dd HH:mm}。");
        sb.AppendLine("> 請勿手動編輯；重跑選單即可更新。");
        sb.AppendLine();

        sb.AppendLine("## 摘要");
        sb.AppendLine();
        sb.AppendLine($"- 掃描腳本數：{scannedFiles}");
        sb.AppendLine($"- 讀取點總數：{entries.Count}");
        int missing = entries.Count(e => e.FullyResolved && !e.AssetExists);
        int dynamic = entries.Count(e => !e.FullyResolved);
        sb.AppendLine($"- 路徑可解析但磁碟找不到：{missing}（潛在錯誤，建議檢查）");
        sb.AppendLine($"- 路徑含變數無法靜態解析：{dynamic}（需看程式邏輯）");
        sb.AppendLine();

        sb.AppendLine("| 分類 | 數量 |");
        sb.AppendLine("|---|---|");
        foreach (var g in entries.GroupBy(e => e.Category).OrderBy(g => g.Key))
            sb.AppendLine($"| {g.Key} | {g.Count()} |");
        sb.AppendLine();

        string[] order = { "美術", "UI（圖像）", "UI（Prefab）", "UI（字型/材質）", "音頻", "資料設定", "其他" };
        var categories = entries.Select(e => e.Category).Distinct()
            .OrderBy(c => Array.IndexOf(order, c) is int i && i >= 0 ? i : int.MaxValue)
            .ToList();

        foreach (string cat in categories)
        {
            var rows = entries.Where(e => e.Category == cat)
                .OrderBy(e => e.ResolvedKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
            sb.AppendLine($"## {cat}（{rows.Count}）");
            sb.AppendLine();
            sb.AppendLine("| 型別 | 載入方式 | Key / 路徑 | 磁碟位置 | 狀態 | 來源 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (LoadEntry e in rows)
            {
                string status = !e.FullyResolved ? "變數" : (e.AssetExists ? "OK" : "缺檔!");
                string disk = string.IsNullOrEmpty(e.DiskLocation) ? "—" : e.DiskLocation;
                string keyCell = string.IsNullOrEmpty(e.ResolvedKey) ? "—" : e.ResolvedKey;
                sb.AppendLine($"| {e.TypeName} | {e.Mechanism}.{e.Variant} | `{keyCell}` | `{disk}` | {status} | {e.SourceFile}:{e.Line} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## 已偵測到的路徑常數（命名慣例參考）");
        sb.AppendLine();
        sb.AppendLine("| 常數名 | 值 |");
        sb.AppendLine("|---|---|");
        foreach (var kv in constMap.Where(kv => LooksLikeResourcePath(kv.Key, kv.Value))
                     .OrderBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"| {kv.Key} | `{kv.Value}` |");
        sb.AppendLine();

        return sb.ToString();
    }

    private static bool LooksLikeResourcePath(string name, string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        string n = name.ToLowerInvariant();
        return n.Contains("path") || n.Contains("folder") || n.Contains("prefix")
               || n.Contains("resource") || value.Contains('/');
    }

    private static bool IsCommented(string text, int matchIndex)
    {
        int lineStart = text.LastIndexOf('\n', Math.Max(0, matchIndex - 1)) + 1;
        string before = text.Substring(lineStart, matchIndex - lineStart);
        return before.Contains("//") || before.Contains("*");
    }

    private static int LineNumberAt(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < text.Length; i++)
            if (text[i] == '\n')
                line++;
        return line;
    }
}

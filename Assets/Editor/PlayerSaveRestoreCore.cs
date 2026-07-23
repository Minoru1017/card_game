using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>玩家存檔還原：讀取來源、摘要槽位、合併至主檔。</summary>
public static class PlayerSaveRestoreCore
{
    public const string GitMirrorAssetPath = "Assets/PlayerDataSnapshots/playerdata.profile_mirror.csv";

    public enum RestoreSourceKind
    {
        Primary,
        AutoBackup,
        Archive,
        ProjectMirror,
        ProjectCheckpoint,
        GitHead,
    }

    public readonly struct SlotPreview
    {
        public readonly int Slot;
        public readonly string Name;
        public readonly int Coins;
        public readonly int Gems;
        public readonly int Wins;
        public readonly int RowCount;
        public readonly int DeckCardCount;
        public readonly bool Occupied;

        public SlotPreview(
            int slot,
            string name,
            int coins,
            int gems,
            int wins,
            int rowCount,
            int deckCardCount,
            bool occupied)
        {
            Slot = slot;
            Name = name;
            Coins = coins;
            Gems = gems;
            Wins = wins;
            RowCount = rowCount;
            DeckCardCount = deckCardCount;
            Occupied = occupied;
        }

        public string ShortLabel =>
            "槽位 " + Slot + " · " + (string.IsNullOrWhiteSpace(Name) ? "（未命名）" : Name) +
            " · 金 " + Coins + " / 寶 " + Gems +
            (Wins > 0 ? " · " + Wins + " 勝" : string.Empty) +
            (DeckCardCount > 0 ? " · 牌組 " + DeckCardCount + " 張" : string.Empty);
    }

    public sealed class RestoreSource
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public string Path = string.Empty;
        public RestoreSourceKind Kind;
        public DateTime LastWriteTime;
        public bool Exists;
        public SlotPreview[] Slots = Array.Empty<SlotPreview>();
        public string Detail = string.Empty;
        public int RecommendedSlot;

        public SlotPreview GetSlotPreview(int slot)
        {
            if (Slots == null || slot < 1 || slot > PlayerData.MaxPlayerSlots)
                return default;
            return Slots[slot - 1];
        }
    }

    public static string PrimarySavePath => PlayerData.GetPlayerSaveCsvPath();

    public static bool IsPlayModeBlocked(out string message)
    {
        if (Application.isPlaying)
        {
            message = "請先停止 Play 模式再還原存檔。";
            return true;
        }

        message = string.Empty;
        return false;
    }

    public static List<RestoreSource> BuildSourceCatalog(bool includeGitHead = true)
    {
        var sources = new List<RestoreSource>(16);
        string primary = PrimarySavePath;
        string saveDir = Path.GetDirectoryName(primary) ?? Application.persistentDataPath;

        AddFileSource(sources, primary, RestoreSourceKind.Primary, "目前主檔 playerdata.csv", "primary");

        for (int i = 1; i <= PlayerPersistSafeIO.BackupTierCount; i++)
        {
            string bak = PlayerPersistSafeIO.GetBackupPath(primary, i);
            AddFileSource(
                sources,
                bak,
                RestoreSourceKind.AutoBackup,
                "自動備份 .bak" + i + (i == 1 ? "（最新）" : string.Empty),
                "bak" + i);
        }

        if (Directory.Exists(saveDir))
        {
            foreach (string path in Directory.GetFiles(saveDir, "playerdata.csv.before*"))
                AddFileSource(sources, path, RestoreSourceKind.Archive, "封存 " + Path.GetFileName(path), "arc:" + path);
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        string mirrorPath = Path.Combine(projectRoot, GitMirrorAssetPath.Replace('/', Path.DirectorySeparatorChar));
        AddFileSource(sources, mirrorPath, RestoreSourceKind.ProjectMirror, "專案 mirror（工作區）", "mirror");

        string snapshotsDir = Path.Combine(Application.dataPath, "PlayerDataSnapshots");
        if (Directory.Exists(snapshotsDir))
        {
            foreach (string path in Directory.GetFiles(snapshotsDir, "playerdata.slot*.csv"))
            {
                AddFileSource(
                    sources,
                    path,
                    RestoreSourceKind.ProjectCheckpoint,
                    "專案快照 " + Path.GetFileName(path),
                    "chk:" + path);
            }
        }

        if (includeGitHead && !string.IsNullOrEmpty(projectRoot))
        {
            var git = new RestoreSource
            {
                Id = "git:HEAD",
                Label = "Git HEAD mirror（版本庫最後提交）",
                Path = GitMirrorAssetPath + "@HEAD",
                Kind = RestoreSourceKind.GitHead,
                Exists = true,
                Detail = "從 git show HEAD 讀取，無需 mirror 檔案存在於工作區。",
            };
            if (TryReadGitMirrorLines("HEAD", out string[] gitLines, out _))
            {
                git.Slots = BuildSlotPreviews(gitLines);
                git.LastWriteTime = DateTime.Now;
                git.RecommendedSlot = RecommendBestSlot(git.Slots);
            }
            else
            {
                git.Exists = false;
                git.Detail = "無法讀取 Git mirror。";
            }

            sources.Add(git);
        }

        return sources;
    }

    public static List<RestoreSource> RankSourcesForSlot(List<RestoreSource> catalog, int slot, SlotPreview current)
    {
        var ranked = new List<RestoreSource>();
        for (int i = 0; i < catalog.Count; i++)
        {
            RestoreSource source = catalog[i];
            if (source.Kind == RestoreSourceKind.Primary)
                continue;

            SlotPreview preview = source.GetSlotPreview(slot);
            if (!preview.Occupied && preview.RowCount <= 3)
                continue;

            if (source.Kind != RestoreSourceKind.GitHead && !source.Exists)
                continue;

            int score = ScoreSourceForSlot(source, preview, current);
            if (score <= 0)
                continue;

            ranked.Add(source);
        }

        ranked.Sort((a, b) =>
        {
            int sa = ScoreSourceForSlot(a, a.GetSlotPreview(slot), current);
            int sb = ScoreSourceForSlot(b, b.GetSlotPreview(slot), current);
            int cmp = sb.CompareTo(sa);
            if (cmp != 0) return cmp;
            return b.LastWriteTime.CompareTo(a.LastWriteTime);
        });

        return ranked;
    }

    private static int ScoreSourceForSlot(RestoreSource source, SlotPreview preview, SlotPreview current)
    {
        if (preview.RowCount <= 0)
            return 0;

        int score = preview.RowCount;
        if (preview.DeckCardCount > 0) score += 200;
        if (preview.Wins > 0) score += preview.Wins * 8;
        if (preview.Gems != 300) score += 20;
        if (!string.Equals(preview.Name, "玩家" + preview.Slot, StringComparison.Ordinal))
            score += 15;

        if (current.Occupied)
        {
            if (preview.Wins > current.Wins) score += 40;
            if (preview.DeckCardCount > current.DeckCardCount) score += 60;
            if (preview.RowCount > current.RowCount + 5) score += 30;
        }

        switch (source.Kind)
        {
            case RestoreSourceKind.AutoBackup: score += 25; break;
            case RestoreSourceKind.Archive: score += 10; break;
            case RestoreSourceKind.GitHead: score += 5; break;
            case RestoreSourceKind.ProjectCheckpoint: score += 8; break;
        }

        return score;
    }

    public static bool TryRestoreFullFile(string sourcePath, out string message)
    {
        message = string.Empty;
        if (IsPlayModeBlocked(out message))
            return false;

        if (!File.Exists(sourcePath))
        {
            message = "找不到來源檔：\n" + sourcePath;
            return false;
        }

        if (!TryReadLines(sourcePath, out string[] lines))
        {
            message = "來源檔格式無效。";
            return false;
        }

        string primary = PrimarySavePath;
        Directory.CreateDirectory(Path.GetDirectoryName(primary) ?? Application.persistentDataPath);
        QuarantinePrimary(primary, "before-full-restore");
        PlayerPersistSafeIO.WriteAllLines(primary, lines);
        message = "已以整份檔案覆寫主檔。\n" + primary;
        Debug.Log("[PlayerSaveRestore] " + message.Replace("\n", " | "));
        return true;
    }

    public static bool TryRestoreSlotFromSource(
        RestoreSource source,
        int slot,
        bool setActiveSlot,
        out string message)
    {
        message = string.Empty;
        if (IsPlayModeBlocked(out message))
            return false;

        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!TryExtractSlotLinesFromSource(source, slot, out List<string> slotLines, out string error))
        {
            message = error;
            return false;
        }

        return TryMergeSlotLines(slotLines, slot, setActiveSlot, out message);
    }

    public static bool TryMergeSlotLines(
        List<string> slotLines,
        int slot,
        bool setActiveSlot,
        out string message)
    {
        message = string.Empty;
        if (slotLines == null || slotLines.Count == 0)
        {
            message = "沒有可合併的槽位資料。";
            return false;
        }

        string primary = PrimarySavePath;
        Directory.CreateDirectory(Path.GetDirectoryName(primary) ?? Application.persistentDataPath);

        List<string> merged;
        if (File.Exists(primary))
        {
            QuarantinePrimary(primary, "before-slot-restore");
            merged = MergeSlotLines(PlayerPersistSafeIO.ReadAllLines(primary), slotLines, slot, setActiveSlot);
        }
        else
        {
            merged = MergeSlotLines(Array.Empty<string>(), slotLines, slot, setActiveSlot);
        }

        PlayerPersistSafeIO.WriteAllLines(primary, merged);
        TrySummarizeSlotLines(slotLines, slot, out SlotPreview preview);
        message = "已還原槽位 " + slot + "（" + preview.Name + "）。\n" + preview.ShortLabel + "\n" + primary;
        Debug.Log("[PlayerSaveRestore] " + message.Replace("\n", " | "));
        return true;
    }

    public static bool TryReadLines(string path, out string[] lines)
    {
        lines = null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            lines = PlayerPersistSafeIO.ReadAllLines(path);
            return PlayerPersistSafeIO.LooksLikePlayerDataCsv(lines);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerSaveRestoreCore: read failed -> " + path + " :: " + ex.Message);
            return false;
        }
    }

    public static SlotPreview[] BuildSlotPreviews(string[] lines)
    {
        var previews = new SlotPreview[PlayerData.MaxPlayerSlots];
        if (lines == null)
            return previews;

        for (int slot = 1; slot <= PlayerData.MaxPlayerSlots; slot++)
            previews[slot - 1] = SummarizeSlot(lines, slot);
        return previews;
    }

    public static SlotPreview SummarizeSlot(string[] lines, int slot)
    {
        string name = "玩家" + slot;
        int coins = 100;
        int gems = 300;
        int wins = 0;
        int rowCount = 0;
        int deckCards = 0;
        bool occupied = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!TryParseSlotRow(line, out int rowSlot, out string key))
                continue;
            if (rowSlot != slot)
                continue;

            rowCount++;
            string val = line.Split(',')[3].Trim();

            if (key == "slot_name" && !string.IsNullOrWhiteSpace(val))
                name = val;
            else if (key == "coins" && int.TryParse(val, out int c))
                coins = c;
            else if (key == "gems" && int.TryParse(val, out int g))
                gems = g;
            else if (key == "profile_wins" && int.TryParse(val, out int w))
                wins = w;
            else if (key == "deckslot")
            {
                string[] cols = line.Split(',');
                if (cols.Length >= 7 && int.TryParse(cols[cols.Length - 1].Trim(), out int count))
                    deckCards += Mathf.Max(0, count);
            }

            if (SlotRowIndicatesOccupied(slot, key, val))
                occupied = true;
        }

        return new SlotPreview(slot, name, coins, gems, wins, rowCount, deckCards, occupied);
    }

    private static bool TryExtractSlotLinesFromSource(
        RestoreSource source,
        int slot,
        out List<string> slotLines,
        out string error)
    {
        slotLines = null;
        error = string.Empty;
        if (source == null)
        {
            error = "來源無效。";
            return false;
        }

        if (source.Kind == RestoreSourceKind.GitHead)
        {
            if (!TryReadGitMirrorLines("HEAD", out string[] gitLines, out error))
                return false;
            return TryExtractSlotLines(gitLines, slot, out slotLines, out error);
        }

        if (string.IsNullOrWhiteSpace(source.Path) || !File.Exists(source.Path))
        {
            error = "找不到來源檔。";
            return false;
        }

        if (!TryReadLines(source.Path, out string[] lines))
        {
            error = "來源檔格式無效。";
            return false;
        }

        return TryExtractSlotLines(lines, slot, out slotLines, out error);
    }

    public static bool TryExtractSlotLines(string[] lines, int slot, out List<string> slotLines, out string error)
    {
        slotLines = new List<string>(256);
        error = string.Empty;
        if (lines == null || lines.Length == 0)
        {
            error = "來源為空。";
            return false;
        }

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
        }

        if (slotLines.Count == 0)
        {
            error = "來源中找不到槽位 " + slot + " 的資料。";
            return false;
        }

        return true;
    }

    public static bool TryReadGitMirrorLines(string gitRev, out string[] lines, out string error)
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
                Arguments = "show " + gitRev + ":" + GitMirrorAssetPath,
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

    private static List<string> MergeSlotLines(
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
                if (!activeWritten)
                {
                    merged.Add(setActiveSlot ? "active_slot," + slot : line);
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

    private static void TrySummarizeSlotLines(List<string> slotLines, int slot, out SlotPreview preview)
    {
        preview = SummarizeSlot(slotLines.ToArray(), slot);
    }

    private static void AddFileSource(
        List<RestoreSource> list,
        string path,
        RestoreSourceKind kind,
        string label,
        string id)
    {
        var source = new RestoreSource
        {
            Id = id,
            Label = label,
            Path = path,
            Kind = kind,
            Exists = File.Exists(path),
        };

        if (source.Exists)
        {
            source.LastWriteTime = File.GetLastWriteTime(path);
            if (TryReadLines(path, out string[] lines))
            {
                source.Slots = BuildSlotPreviews(lines);
                source.RecommendedSlot = RecommendBestSlot(source.Slots);
                long bytes = new FileInfo(path).Length;
                source.Detail = source.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + " · " + lines.Length + " 列 · " + bytes + " bytes";
            }
        }
        else
        {
            source.Detail = "檔案不存在";
        }

        list.Add(source);
    }

    private static int RecommendBestSlot(SlotPreview[] slots)
    {
        int bestSlot = 1;
        int bestScore = int.MinValue;
        for (int i = 0; i < slots.Length; i++)
        {
            SlotPreview s = slots[i];
            int score = s.RowCount + s.DeckCardCount * 5 + s.Wins * 3 + (s.Occupied ? 20 : 0);
            if (score > bestScore)
            {
                bestScore = score;
                bestSlot = s.Slot;
            }
        }

        return bestSlot;
    }

    private static void QuarantinePrimary(string primary, string tag)
    {
        if (!File.Exists(primary))
            return;
        string quarantine = primary + "." + tag + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        File.Copy(primary, quarantine, true);
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

    /// <summary>與 <see cref="PlayerData"/> 登入槽位判定對齊（Editor 摘要用）。</summary>
    private static bool SlotRowIndicatesOccupied(int slot, string key, string val)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;
        if (key == "card" || key == "deck" || key == "deckslot")
            return true;
        if (key == "coins" && int.TryParse(val, out int coins) && coins != 100)
            return true;
        if (key == "gems" && int.TryParse(val, out int gems) && gems != 300)
            return true;
        if (key == "bird_cd" || key == "valuable" || key == "battle_record")
            return true;
        if (key == "slot_name" && !string.IsNullOrWhiteSpace(val) && val != ("玩家" + slot))
            return true;
        if ((key == "profile_wins" || key == "profile_losses" || key == "profile_draws" || key == "profile_quits")
            && int.TryParse(val, out int profileCount) && profileCount > 0)
            return true;
        if ((key == "harbor_combat_clear" || key == "academy_intro_graduated")
            && string.Equals(val, "1", StringComparison.Ordinal))
            return true;
        return false;
    }
}

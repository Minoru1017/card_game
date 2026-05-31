using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public static class PlayerProfileCsvService
{
    private const string FileName = "player_profile.csv";
    private const string CurrentSchemaVersion = "1";
    private const int MaxBattleRecords = 200;
    private const string LegacyBattleDifficultyZh = "入門";

    public static readonly string[] StandardDifficultyLabelsZh = { "入門", "簡單", "普通", "困難", "魔王" };
    private const string ExistingPlayerDataBackupFile = "playerdata_existing_backup.csv";
    private const string ExistingProfileBackupFile = "player_profile_existing_backup.csv";

    public struct BattleRecordEntry
    {
        public int result;
        public string difficultyZh;
    }

    public struct PlayerProfile
    {
        public string schemaVersion;
        public string uuid;
        public string role;
        public string decks;
        public string heroes;
        public string startDate;
        public int wins;
        public int losses;
        public int draws;
        public int quits;
        public string lastResult;
        public List<BattleRecordEntry> battleRecords;
    }

    private static string ProfilePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string PlayerDataPath => Path.Combine(Application.persistentDataPath, "playerdata.csv");
    private static string ExistingPlayerDataBackupPath => Path.Combine(Application.persistentDataPath, ExistingPlayerDataBackupFile);
    private static string ExistingProfileBackupPath => Path.Combine(Application.persistentDataPath, ExistingProfileBackupFile);
    private static string ProjectSnapshotDir
    {
        get
        {
            string projectRoot = Directory.GetParent(Application.dataPath) != null
                ? Directory.GetParent(Application.dataPath).FullName
                : Application.dataPath;
            return Path.Combine(projectRoot, "Assets", "PlayerDataSnapshots");
        }
    }

    public static PlayerProfile RefreshProfileFromRuntime()
    {
        PlayerProfile p = LoadOrCreate();
        PlayerProfile slotProfile;
        if (TryReadActiveSlotProfile(out slotProfile))
        {
            // Slot profile represents player's original creation record.
            // Prefer it over global profile cache to avoid start date drift.
            p = MergeProfileWithSlotRecord(p, slotProfile);
        }
        p.schemaVersion = string.IsNullOrWhiteSpace(p.schemaVersion) ? CurrentSchemaVersion : p.schemaVersion;
        if (string.IsNullOrWhiteSpace(p.uuid)) p.uuid = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(p.role)) p.role = "一般玩家";
        if (string.IsNullOrWhiteSpace(p.startDate)) p.startDate = DateTime.Now.ToString("yyyy-MM-dd");

        PlayerData playerData = ResolvePlayerData();
        if (playerData != null)
        {
            // 先寫入完整 playerdata.csv（含 deck_slot_name），避免 SyncProfile 僅合併 profile 時覆寫成預設名稱。
            playerData.SavePlayerData();
            p.decks = BuildDeckSummary(playerData);
            p.heroes = BuildHeroSummary(playerData);
        }

        Save(p);
        return p;
    }

    public static void SetRole(string role)
    {
        PlayerProfile p = RefreshProfileFromRuntime();
        p.role = string.IsNullOrWhiteSpace(role) ? "一般玩家" : role.Trim();
        Save(p);
    }

    public static void ResetPlayerProgressLikeBackpack(int defaultCoins = 100)
    {
        PlayerData pd = ResolvePlayerData();
        if (pd != null)
        {
            pd.playerCoins = Mathf.Max(0, defaultCoins);
            pd.totalCoins = pd.playerCoins;
            pd.ClearAllCollectionAndDecks();
            pd.SavePlayerData();
            pd.RefreshCoins();
        }
        else
        {
            OverwritePlayerDataCsvToDefaults(Mathf.Max(0, defaultCoins));
        }

        PlayerProfile p = LoadOrCreate();
        p.wins = 0;
        p.losses = 0;
        p.draws = 0;
        p.quits = 0;
        p.lastResult = "無";
        p.battleRecords = new List<BattleRecordEntry>();
        Save(p);

        // Rebuild deck/hero summary based on the reset runtime state.
        RefreshProfileFromRuntime();
    }

    public static void CreateNewPlayerDefaults(int defaultCoins = 100, string role = "一般玩家")
    {
        int coins = Mathf.Max(0, defaultCoins);
        ResetActiveSlotToDefaultsInPlayerDataCsv(coins);

        PlayerProfile p = new PlayerProfile
        {
            schemaVersion = CurrentSchemaVersion,
            uuid = Guid.NewGuid().ToString("N"),
            role = string.IsNullOrWhiteSpace(role) ? "一般玩家" : role.Trim(),
            decks = "尚無牌組資料",
            heroes = "無",
            startDate = DateTime.Now.ToString("yyyy-MM-dd"),
            wins = 0,
            losses = 0,
            draws = 0,
            quits = 0,
            lastResult = "無",
            battleRecords = new List<BattleRecordEntry>()
        };
        Save(p);
    }

    public static void BackupCurrentAsExistingPlayer()
    {
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        if (File.Exists(PlayerDataPath))
            File.Copy(PlayerDataPath, ExistingPlayerDataBackupPath, true);
        if (File.Exists(ProfilePath))
            File.Copy(ProfilePath, ExistingProfileBackupPath, true);
    }

    public static bool RestoreExistingPlayerIfBackedUp()
    {
        bool restored = false;
        if (File.Exists(ExistingPlayerDataBackupPath))
        {
            File.Copy(ExistingPlayerDataBackupPath, PlayerDataPath, true);
            restored = true;
        }
        if (File.Exists(ExistingProfileBackupPath))
        {
            File.Copy(ExistingProfileBackupPath, ProfilePath, true);
            restored = true;
        }
        return restored;
    }

    private static void OverwritePlayerDataCsvToDefaults(int defaultCoins)
    {
        // Keep behavior name for compatibility with existing call sites.
        ResetActiveSlotToDefaultsInPlayerDataCsv(defaultCoins);
    }

    private static void ResetActiveSlotToDefaultsInPlayerDataCsv(int defaultCoins)
    {
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        string[] existing = PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] read, out _)
            ? read
            : Array.Empty<string>();
        int activeSlot = ReadActiveSlotFromRows(existing);

        var merged = new List<string>(Mathf.Max(8, existing.Length + 4));
        bool activeSlotWritten = false;
        bool slotNameKept = false;

        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (string.IsNullOrWhiteSpace(row))
            {
                merged.Add(row);
                continue;
            }

            string[] cols = row.Split(',');
            if (cols.Length == 0)
            {
                merged.Add(row);
                continue;
            }

            string key0 = cols[0].Trim();
            if (string.Equals(key0, "active_slot", StringComparison.OrdinalIgnoreCase))
            {
                if (!activeSlotWritten)
                {
                    merged.Add("active_slot," + activeSlot);
                    activeSlotWritten = true;
                }
                continue;
            }

            if (string.Equals(key0, "slot", StringComparison.OrdinalIgnoreCase) &&
                cols.Length >= 3 &&
                int.TryParse(cols[1].Trim(), out int slot) &&
                slot == activeSlot)
            {
                string slotKey = cols[2].Trim();
                // Clear active slot runtime progress rows.
                if (slotKey == "coins" || slotKey == "selected_deck_slot" || slotKey == "card" || slotKey == "deck" ||
                    slotKey == "deckslot" || slotKey == "tutorial_plot" || slotKey == "tutorial_battle")
                    continue;
                if (slotKey == "slot_name") slotNameKept = true;
            }

            merged.Add(row);
        }

        if (!activeSlotWritten)
            merged.Insert(0, "active_slot," + activeSlot);

        merged.Add("slot," + activeSlot + ",coins," + defaultCoins);
        merged.Add("slot," + activeSlot + ",selected_deck_slot,0");
        if (!slotNameKept)
            merged.Add("slot," + activeSlot + ",slot_name,玩家" + activeSlot);

        EnsureAllSlotsMinimalRows(merged);
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(PlayerDataPath, merged);
    }

    private static int ReadActiveSlotFromRows(string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 2) continue;
            if (!string.Equals(cols[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot)) continue;
            return Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        }
        return 1;
    }

    private static void EnsureAllSlotsMinimalRows(List<string> rows)
    {
        bool[] hasCoin = new bool[PlayerData.MaxPlayerSlots + 1];
        bool[] hasSelect = new bool[PlayerData.MaxPlayerSlots + 1];
        bool[] hasName = new bool[PlayerData.MaxPlayerSlots + 1];
        for (int i = 0; i < rows.Count; i++)
        {
            string line = rows[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cols = line.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot < 1 || slot > PlayerData.MaxPlayerSlots) continue;
            string k = cols[2].Trim();
            if (k == "coins") hasCoin[slot] = true;
            else if (k == "selected_deck_slot") hasSelect[slot] = true;
            else if (k == "slot_name") hasName[slot] = true;
        }

        for (int slot = 1; slot <= PlayerData.MaxPlayerSlots; slot++)
        {
            if (!hasCoin[slot]) rows.Add("slot," + slot + ",coins,100");
            if (!hasSelect[slot]) rows.Add("slot," + slot + ",selected_deck_slot,0");
            if (!hasName[slot]) rows.Add("slot," + slot + ",slot_name,玩家" + slot);
        }
    }

    public static void RecordBattleResult(int result, string difficultyLabelZh = null)
    {
        PlayerProfile p = LoadOrCreate();
        EnsureBattleRecordsList(ref p);
        string difficulty = PickDifficultyLabelForNewRecord(difficultyLabelZh);
        if (result == 1)
        {
            p.wins++;
            p.lastResult = "勝利";
        }
        else if (result == -1)
        {
            p.losses++;
            p.lastResult = "戰敗";
        }
        else if (result == 2)
        {
            p.draws++;
            p.lastResult = "平手";
        }
        AppendBattleRecord(ref p, result, difficulty);
        Save(p);
    }

    public static void RecordPlayerQuit(string difficultyLabelZh = null)
    {
        PlayerProfile p = LoadOrCreate();
        EnsureBattleRecordsList(ref p);
        p.quits++;
        p.lastResult = "玩家中離";
        AppendBattleRecord(ref p, 3, PickDifficultyLabelForNewRecord(difficultyLabelZh));
        Save(p);
    }

    /// <summary>Counts per standard difficulty for one result type (1 win, -1 loss, 2 draw, 3 quit).</summary>
    public static int[] GetDifficultyCountsForResult(PlayerProfile p, int resultCode)
    {
        int length = StandardDifficultyLabelsZh.Length;
        var counts = new int[length];
        EnsureBattleRecordsList(ref p);
        if (p.battleRecords == null) return counts;

        for (int i = 0; i < p.battleRecords.Count; i++)
        {
            BattleRecordEntry entry = p.battleRecords[i];
            if (entry.result != resultCode) continue;
            int idx = GetDifficultyIndex(NormalizeDifficultyLabel(entry.difficultyZh));
            counts[idx]++;
        }
        return counts;
    }

    public static int SumCounts(int[] counts)
    {
        if (counts == null) return 0;
        int total = 0;
        for (int i = 0; i < counts.Length; i++)
            total += Mathf.Max(0, counts[i]);
        return total;
    }

    public static string ResultFilterLabel(int resultCode)
    {
        if (resultCode == 1) return "W";
        if (resultCode == -1) return "L";
        if (resultCode == 2) return "D";
        if (resultCode == 3) return "Q";
        return "?";
    }

    public static string BuildBattleRecordPanelSummary(PlayerProfile p, int resultCode)
    {
        int[] counts = GetDifficultyCountsForResult(p, resultCode);
        var parts = new List<string>(StandardDifficultyLabelsZh.Length);
        for (int i = 0; i < StandardDifficultyLabelsZh.Length; i++)
        {
            if (counts[i] <= 0) continue;
            parts.Add(StandardDifficultyLabelsZh[i] + " " + counts[i]);
        }
        string filter = ResultFilterLabel(resultCode);
        int total = SumCounts(counts);
        if (parts.Count <= 0)
            return filter + " 尚無紀錄";
        return filter + " 共 " + total + " 場：" + string.Join(" · ", parts);
    }

    public static string BuildDisplayText(PlayerProfile p)
    {
        int total = Mathf.Max(0, p.wins) + Mathf.Max(0, p.losses) + Mathf.Max(0, p.draws) + Mathf.Max(0, p.quits);
        return
            "UUID: " + SafeValue(p.uuid) + "\n" +
            "玩家身份: " + SafeValue(p.role) + "\n" +
            "持有的牌組: " + SafeValue(p.decks) + "\n" +
            "持有的英雄: " + SafeValue(p.heroes) + "\n" +
            "開始遊玩日期: " + SafeValue(p.startDate) + "\n" +
            "玩家的對戰紀錄: W " + p.wins + " / L " + p.losses + " / D " + p.draws + " / Q " + p.quits +
            " (總場次 " + total + ", 最近結果: " + SafeValue(p.lastResult) + ")\n" +
            BuildBattleRecordPanelSummary(p, 1);
    }

    public static PlayerProfile LoadOrCreate()
    {
        TryMigrateLegacyGlobalProfileIntoActiveSlotIfNeeded();
        PlayerProfile p;
        if (!TryLoadProfileFromActiveSlotRows(out p))
        {
            // Slot mirror may be missing if playerdata was saved by an older PlayerData path that
            // dropped profile_* rows; fall back to standalone player_profile.csv before minting a new UUID.
            p = Load();
            if (string.IsNullOrWhiteSpace(p.uuid))
                p = new PlayerProfile();
        }
        EnsureBattleRecordsList(ref p);
        LoadBattleRecordsIntoProfile(ref p);

        if (string.IsNullOrWhiteSpace(p.uuid))
        {
            p.schemaVersion = CurrentSchemaVersion;
            p.uuid = Guid.NewGuid().ToString("N");
            p.role = string.IsNullOrWhiteSpace(p.role) ? "一般玩家" : p.role;
            p.startDate = string.IsNullOrWhiteSpace(p.startDate) ? DateTime.Now.ToString("yyyy-MM-dd") : p.startDate;
            if (string.IsNullOrWhiteSpace(p.decks)) p.decks = "尚無牌組資料";
            if (string.IsNullOrWhiteSpace(p.heroes)) p.heroes = "無";
            if (string.IsNullOrWhiteSpace(p.lastResult)) p.lastResult = "無";
            p.quits = Mathf.Max(0, p.quits);
            TryMigrateLegacyBattleRecords(ref p);
            Save(p);
        }
        else if (TryMigrateLegacyBattleRecords(ref p))
        {
            Save(p);
        }

        return p;
    }

    private static bool TryLoadProfileFromActiveSlotRows(out PlayerProfile p)
    {
        p = new PlayerProfile();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] rows, out _))
            return false;

        int activeSlot = ReadActiveSlotFromRows(rows);
        bool foundAny = false;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot != activeSlot) continue;
            string key = cols[2].Trim();
            if (!key.StartsWith("profile_", StringComparison.OrdinalIgnoreCase)) continue;

            string value = string.Join(",", cols, 3, cols.Length - 3).Trim();
            if (key == "profile_schema_version") p.schemaVersion = value;
            else if (key == "profile_uuid") p.uuid = value;
            else if (key == "profile_role") p.role = value;
            else if (key == "profile_decks") p.decks = value;
            else if (key == "profile_heroes") p.heroes = value;
            else if (key == "profile_start_date") p.startDate = value;
            else if (key == "profile_wins") int.TryParse(value, out p.wins);
            else if (key == "profile_losses") int.TryParse(value, out p.losses);
            else if (key == "profile_draws") int.TryParse(value, out p.draws);
            else if (key == "profile_quits") int.TryParse(value, out p.quits);
            else if (key == "profile_last_result") p.lastResult = value;
            foundAny = true;
        }
        return foundAny;
    }

    private static bool HasAnySlotProfileRows(string[] rows, int slot)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int rowSlot) || rowSlot != slot) continue;
            if (cols[2].Trim().StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool HasAnySlotProfileRowsInAnySlot(string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot < 1 || slot > PlayerData.MaxPlayerSlots) continue;
            if (cols[2].Trim().StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Older saves used a single player_profile.csv plus root-level profile_* rows in playerdata.csv.
    /// Copy that into slot,N,profile_* once so switching slots does not show another slot's stats.
    /// </summary>
    private static void TryMigrateLegacyGlobalProfileIntoActiveSlotIfNeeded()
    {
        if (!PlayerPersistSafeIO.ExistsAnyWithBackups(PlayerDataPath) || !PlayerPersistSafeIO.ExistsAnyWithBackups(ProfilePath))
            return;

        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] rows, out _))
            return;
        int activeSlot = ReadActiveSlotFromRows(rows);
        if (HasAnySlotProfileRows(rows, activeSlot))
            return;
        if (HasAnySlotProfileRowsInAnySlot(rows))
            return;

        PlayerProfile legacy = Load();
        if (string.IsNullOrWhiteSpace(legacy.uuid))
            return;

        SyncProfileIntoActiveSlotRows(legacy);
    }

    private static PlayerProfile Load()
    {
        PlayerProfile p = new PlayerProfile();
        if (!PlayerPersistSafeIO.TryReadProfileLines(ProfilePath, out string[] rows, out _))
            return p;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 2) continue;
            string key = cols[0].Trim();
            string value = string.Join(",", cols, 1, cols.Length - 1).Trim();
            if (key == "schema_version") p.schemaVersion = value;
            else if (key == "uuid") p.uuid = value;
            else if (key == "role") p.role = value;
            else if (key == "decks") p.decks = value;
            else if (key == "heroes") p.heroes = value;
            else if (key == "start_date") p.startDate = value;
            else if (key == "wins") int.TryParse(value, out p.wins);
            else if (key == "losses") int.TryParse(value, out p.losses);
            else if (key == "draws") int.TryParse(value, out p.draws);
            else if (key == "quits") int.TryParse(value, out p.quits);
            else if (key == "last_result") p.lastResult = value;
        }
        return p;
    }

    private static void Save(PlayerProfile p)
    {
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        EnsureBattleRecordsList(ref p);
        var rows = new List<string>(13 + (p.battleRecords != null ? p.battleRecords.Count : 0))
        {
            "schema_version," + SafeCsv(string.IsNullOrWhiteSpace(p.schemaVersion) ? CurrentSchemaVersion : p.schemaVersion),
            "uuid," + SafeCsv(p.uuid),
            "role," + SafeCsv(p.role),
            "decks," + SafeCsv(p.decks),
            "heroes," + SafeCsv(p.heroes),
            "start_date," + SafeCsv(p.startDate),
            "wins," + p.wins,
            "losses," + p.losses,
            "draws," + p.draws,
            "quits," + p.quits,
            "last_result," + SafeCsv(p.lastResult)
        };
        AppendBattleRecordRows(rows, p.battleRecords);
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(ProfilePath, rows);
        SaveProjectSnapshot(FileName, rows);
        SyncProfileIntoActiveSlotRows(p);
    }

    private static void SyncProfileIntoActiveSlotRows(PlayerProfile p)
    {
        string[] existing = PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] read, out _)
            ? read
            : Array.Empty<string>();
        int activeSlot = ReadActiveSlotFromRows(existing);
        var merged = new List<string>(existing.Length + 10);

        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (string.IsNullOrWhiteSpace(row))
            {
                merged.Add(row);
                continue;
            }
            string[] cols = row.Split(',');
            string key0 = cols.Length > 0 ? cols[0].Trim() : string.Empty;
            // Legacy: root-level profile_* rows were shared across all slots — remove them.
            if (key0.StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(key0, "battle_record", StringComparison.OrdinalIgnoreCase))
                continue;
            if (cols.Length >= 3 &&
                string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(cols[1].Trim(), out int slot) &&
                slot == activeSlot)
            {
                string slotKey = cols[2].Trim();
                if (slotKey.StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(slotKey, "battle_record", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(slotKey, "deck_slot_name", StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            merged.Add(row);
        }

        PlayerData runtimePd = ResolvePlayerData();
        if (runtimePd != null)
        {
            int deckSlots = Mathf.Max(1, runtimePd.deckSlotCount);
            for (int s = 0; s < deckSlots; s++)
            {
                string label = runtimePd.GetDeckSlotDisplayName(s);
                merged.Add($"slot,{activeSlot},deck_slot_name,{s},{SafeCsv(label)}");
            }
        }
        else
        {
            for (int i = 0; i < existing.Length; i++)
            {
                string row = existing[i];
                if (string.IsNullOrWhiteSpace(row)) continue;
                string[] cols = row.Split(',');
                if (cols.Length < 5) continue;
                if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(cols[1].Trim(), out int slot) || slot != activeSlot) continue;
                if (!string.Equals(cols[2].Trim(), "deck_slot_name", StringComparison.OrdinalIgnoreCase)) continue;
                merged.Add(row);
            }
        }

        merged.Add($"slot,{activeSlot},profile_uuid,{SafeCsv(p.uuid)}");
        merged.Add($"slot,{activeSlot},profile_schema_version,{SafeCsv(string.IsNullOrWhiteSpace(p.schemaVersion) ? CurrentSchemaVersion : p.schemaVersion)}");
        merged.Add($"slot,{activeSlot},profile_role,{SafeCsv(p.role)}");
        merged.Add($"slot,{activeSlot},profile_decks,{SafeCsv(p.decks)}");
        merged.Add($"slot,{activeSlot},profile_heroes,{SafeCsv(p.heroes)}");
        merged.Add($"slot,{activeSlot},profile_start_date,{SafeCsv(p.startDate)}");
        merged.Add($"slot,{activeSlot},profile_wins,{p.wins}");
        merged.Add($"slot,{activeSlot},profile_losses,{p.losses}");
        merged.Add($"slot,{activeSlot},profile_draws,{p.draws}");
        merged.Add($"slot,{activeSlot},profile_quits,{p.quits}");
        merged.Add($"slot,{activeSlot},profile_last_result,{SafeCsv(p.lastResult)}");
        AppendSlotBattleRecordRows(merged, activeSlot, p.battleRecords);

        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(PlayerDataPath, merged);
        SaveProjectSnapshot("playerdata.profile_mirror.csv", merged);
    }

    private static bool TryReadActiveSlotProfile(out PlayerProfile p)
    {
        p = new PlayerProfile();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] rows, out _))
            return false;
        int activeSlot = ReadActiveSlotFromRows(rows);
        bool hasAny = false;
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot != activeSlot) continue;
            string key = cols[2].Trim();
            string value = string.Join(",", cols, 3, cols.Length - 3).Trim();
            if (!key.StartsWith("profile_", StringComparison.OrdinalIgnoreCase)) continue;
            hasAny = true;
            if (key == "profile_schema_version") p.schemaVersion = value;
            else if (key == "profile_uuid") p.uuid = value;
            else if (key == "profile_role") p.role = value;
            else if (key == "profile_decks") p.decks = value;
            else if (key == "profile_heroes") p.heroes = value;
            else if (key == "profile_start_date") p.startDate = value;
            else if (key == "profile_wins") int.TryParse(value, out p.wins);
            else if (key == "profile_losses") int.TryParse(value, out p.losses);
            else if (key == "profile_draws") int.TryParse(value, out p.draws);
            else if (key == "profile_quits") int.TryParse(value, out p.quits);
            else if (key == "profile_last_result") p.lastResult = value;
        }
        return hasAny;
    }

    private static PlayerProfile MergeProfileWithSlotRecord(PlayerProfile globalProfile, PlayerProfile slotProfile)
    {
        PlayerProfile merged = globalProfile;
        merged.battleRecords = globalProfile.battleRecords;
        if (!string.IsNullOrWhiteSpace(slotProfile.schemaVersion)) merged.schemaVersion = slotProfile.schemaVersion;
        if (!string.IsNullOrWhiteSpace(slotProfile.uuid)) merged.uuid = slotProfile.uuid;
        if (!string.IsNullOrWhiteSpace(slotProfile.role)) merged.role = slotProfile.role;
        if (!string.IsNullOrWhiteSpace(slotProfile.decks)) merged.decks = slotProfile.decks;
        if (!string.IsNullOrWhiteSpace(slotProfile.heroes)) merged.heroes = slotProfile.heroes;
        if (!string.IsNullOrWhiteSpace(slotProfile.startDate)) merged.startDate = slotProfile.startDate;
        merged.wins = Mathf.Max(0, slotProfile.wins);
        merged.losses = Mathf.Max(0, slotProfile.losses);
        merged.draws = Mathf.Max(0, slotProfile.draws);
        merged.quits = Mathf.Max(0, slotProfile.quits);
        if (!string.IsNullOrWhiteSpace(slotProfile.lastResult)) merged.lastResult = slotProfile.lastResult;
        return merged;
    }

    private static void SaveProjectSnapshot(string fileName, List<string> rows)
    {
        try
        {
            Directory.CreateDirectory(ProjectSnapshotDir);
            string outPath = Path.Combine(ProjectSnapshotDir, fileName);
            File.WriteAllLines(outPath, rows);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerProfileCsvService: snapshot write failed -> " + ex.Message);
        }
    }

    private static string BuildDeckSummary(PlayerData playerData)
    {
        if (playerData == null || playerData.deckSlotCount <= 0) return "尚無牌組資料";
        var parts = new List<string>(playerData.deckSlotCount);
        for (int slot = 0; slot < playerData.deckSlotCount; slot++)
        {
            int total = 0;
            IReadOnlyDictionary<int, int> map = playerData.GetDeckMap(slot);
            foreach (KeyValuePair<int, int> kv in map)
            {
                if (kv.Value > 0) total += kv.Value;
            }
            string deckLabel = playerData.GetDeckSlotDisplayName(slot);
            parts.Add(deckLabel + ":" + total + "張");
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "尚無牌組資料";
    }

    private static string BuildHeroSummary(PlayerData playerData)
    {
        return "無";
    }

    private static PlayerData ResolvePlayerData() => PlayerData.ResolveCanonical();

    private static void EnsureBattleRecordsList(ref PlayerProfile p)
    {
        if (p.battleRecords == null)
            p.battleRecords = new List<BattleRecordEntry>();
    }

    private static void LoadBattleRecordsIntoProfile(ref PlayerProfile p)
    {
        var parsed = new Dictionary<int, BattleRecordEntry>();
        if (PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] pdRows, out _))
        {
            int activeSlot = ReadActiveSlotFromRows(pdRows);
            CollectBattleRecordsFromRows(pdRows, activeSlot, parsed);
        }
        if (PlayerPersistSafeIO.TryReadProfileLines(ProfilePath, out string[] profileRows, out _))
            MergeBattleRecordsFromRows(profileRows, -1, parsed);

        EnsureBattleRecordsList(ref p);
        p.battleRecords.Clear();
        if (parsed.Count <= 0) return;

        var indices = new List<int>(parsed.Keys);
        indices.Sort();
        for (int i = 0; i < indices.Count; i++)
            p.battleRecords.Add(parsed[indices[i]]);
    }

    private static void MergeBattleRecordsFromRows(string[] rows, int activeSlot, Dictionary<int, BattleRecordEntry> parsed)
    {
        var incoming = new Dictionary<int, BattleRecordEntry>();
        CollectBattleRecordsFromRows(rows, activeSlot, incoming);
        foreach (var kv in incoming)
        {
            if (!parsed.TryGetValue(kv.Key, out BattleRecordEntry existing))
            {
                parsed[kv.Key] = kv.Value;
                continue;
            }
            parsed[kv.Key] = PickPreferredBattleRecordEntry(existing, kv.Value);
        }
    }

    private static BattleRecordEntry PickPreferredBattleRecordEntry(BattleRecordEntry a, BattleRecordEntry b)
    {
        int idxA = GetDifficultyIndex(NormalizeDifficultyLabel(a.difficultyZh));
        int idxB = GetDifficultyIndex(NormalizeDifficultyLabel(b.difficultyZh));
        if (idxB > idxA) return b;
        if (idxA > idxB) return a;
        return b;
    }

    private static void CollectBattleRecordsFromRows(string[] rows, int activeSlot, Dictionary<int, BattleRecordEntry> parsed)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;

            string key0 = cols[0].Trim();
            if (string.Equals(key0, "battle_record", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(cols[1].Trim(), out int index)) continue;
                if (!int.TryParse(cols[2].Trim(), out int result)) continue;
                string difficulty = cols.Length > 3 ? cols[3].Trim() : LegacyBattleDifficultyZh;
                parsed[index] = new BattleRecordEntry { result = result, difficultyZh = difficulty };
                continue;
            }

            if (activeSlot < 1) continue;
            if (!string.Equals(key0, "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot != activeSlot) continue;
            if (!string.Equals(cols[2].Trim(), "battle_record", StringComparison.OrdinalIgnoreCase)) continue;
            if (cols.Length < 6) continue;
            if (!int.TryParse(cols[3].Trim(), out int slotIndex)) continue;
            if (!int.TryParse(cols[4].Trim(), out int slotResult)) continue;
            string slotDifficulty = cols[5].Trim();
            parsed[slotIndex] = new BattleRecordEntry { result = slotResult, difficultyZh = slotDifficulty };
        }
    }

    private static bool TryMigrateLegacyBattleRecords(ref PlayerProfile p)
    {
        EnsureBattleRecordsList(ref p);
        if (p.battleRecords.Count > 0) return false;
        if (HasPersistedBattleRecordRows())
            return false;

        int total = Mathf.Max(0, p.wins) + Mathf.Max(0, p.losses) + Mathf.Max(0, p.draws) + Mathf.Max(0, p.quits);
        if (total <= 0) return false;

        for (int i = 0; i < Mathf.Max(0, p.wins); i++)
            p.battleRecords.Add(new BattleRecordEntry { result = 1, difficultyZh = LegacyBattleDifficultyZh });
        for (int i = 0; i < Mathf.Max(0, p.losses); i++)
            p.battleRecords.Add(new BattleRecordEntry { result = -1, difficultyZh = LegacyBattleDifficultyZh });
        for (int i = 0; i < Mathf.Max(0, p.draws); i++)
            p.battleRecords.Add(new BattleRecordEntry { result = 2, difficultyZh = LegacyBattleDifficultyZh });
        for (int i = 0; i < Mathf.Max(0, p.quits); i++)
            p.battleRecords.Add(new BattleRecordEntry { result = 3, difficultyZh = LegacyBattleDifficultyZh });
        return true;
    }

    private static void AppendBattleRecord(ref PlayerProfile p, int result, string difficultyZh)
    {
        EnsureBattleRecordsList(ref p);
        p.battleRecords.Add(new BattleRecordEntry { result = result, difficultyZh = difficultyZh });
        if (p.battleRecords.Count > MaxBattleRecords)
            p.battleRecords.RemoveRange(0, p.battleRecords.Count - MaxBattleRecords);
    }

    private static void AppendBattleRecordRows(List<string> rows, List<BattleRecordEntry> records)
    {
        if (records == null) return;
        for (int i = 0; i < records.Count; i++)
        {
            BattleRecordEntry entry = records[i];
            rows.Add("battle_record," + i + "," + entry.result + "," + SafeCsv(NormalizeDifficultyLabel(entry.difficultyZh)));
        }
    }

    private static void AppendSlotBattleRecordRows(List<string> rows, int activeSlot, List<BattleRecordEntry> records)
    {
        if (records == null) return;
        for (int i = 0; i < records.Count; i++)
        {
            BattleRecordEntry entry = records[i];
            rows.Add("slot," + activeSlot + ",battle_record," + i + "," + entry.result + "," +
                     SafeCsv(NormalizeDifficultyLabel(entry.difficultyZh)));
        }
    }

    private static string NormalizeDifficultyLabel(string labelZh)
    {
        if (string.IsNullOrWhiteSpace(labelZh))
            return LegacyBattleDifficultyZh;

        string trimmed = labelZh.Trim();
        if (trimmed.StartsWith("魔王", StringComparison.Ordinal) ||
            string.Equals(trimmed, "Boss", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "BOSS", StringComparison.Ordinal) ||
            trimmed == "魔王級")
            return StandardDifficultyLabelsZh[4];
        if (trimmed.StartsWith("困難", StringComparison.Ordinal) ||
            string.Equals(trimmed, "Hard", StringComparison.OrdinalIgnoreCase) ||
            trimmed == "困難級")
            return StandardDifficultyLabelsZh[3];
        if (trimmed.StartsWith("普通", StringComparison.Ordinal) ||
            string.Equals(trimmed, "Normal", StringComparison.OrdinalIgnoreCase) ||
            trimmed == "普通級")
            return StandardDifficultyLabelsZh[2];
        if (trimmed.StartsWith("簡單", StringComparison.Ordinal) ||
            string.Equals(trimmed, "Easy", StringComparison.OrdinalIgnoreCase) ||
            trimmed == "簡單級")
            return StandardDifficultyLabelsZh[1];
        if (trimmed.StartsWith("入門", StringComparison.Ordinal) ||
            string.Equals(trimmed, "Intro", StringComparison.OrdinalIgnoreCase) ||
            trimmed == "入門級")
            return StandardDifficultyLabelsZh[0];

        for (int i = 0; i < StandardDifficultyLabelsZh.Length; i++)
        {
            if (StandardDifficultyLabelsZh[i] == trimmed)
                return trimmed;
        }

        return trimmed;
    }

    private static string PickDifficultyLabelForNewRecord(string explicitLabelZh)
    {
        var candidates = new List<string>(4);
        if (!string.IsNullOrWhiteSpace(explicitLabelZh))
            candidates.Add(explicitLabelZh);
        candidates.Add(BattleLaunchContext.ResolveForBattleRecord());
        candidates.Add(BattleDifficultyRuntime.ResolveForBattleRecord());

        int bestIdx = -1;
        string bestLabel = LegacyBattleDifficultyZh;
        for (int i = 0; i < candidates.Count; i++)
        {
            string normalized = NormalizeDifficultyLabel(candidates[i]);
            int idx = GetDifficultyIndex(normalized);
            if (idx <= bestIdx) continue;
            bestIdx = idx;
            bestLabel = StandardDifficultyLabelsZh[idx];
        }
        return bestLabel;
    }

    private static bool HasPersistedBattleRecordRows()
    {
        var parsed = new Dictionary<int, BattleRecordEntry>();
        if (PlayerPersistSafeIO.TryReadPlayerDataLines(PlayerDataPath, out string[] pdRows, out _))
        {
            int activeSlot = ReadActiveSlotFromRows(pdRows);
            CollectBattleRecordsFromRows(pdRows, activeSlot, parsed);
            if (parsed.Count > 0) return true;
        }
        if (PlayerPersistSafeIO.TryReadProfileLines(ProfilePath, out string[] profileRows, out _))
        {
            parsed.Clear();
            CollectBattleRecordsFromRows(profileRows, -1, parsed);
            if (parsed.Count > 0) return true;
        }
        return false;
    }

    /// <summary>標準五階難度索引：入門 0、簡單 1、普通 2、困難 3、魔王 4；無法辨識時為 0。</summary>
    public static int GetStandardDifficultyIndex(string labelZh) => GetDifficultyIndex(labelZh);

    private static int GetDifficultyIndex(string labelZh)
    {
        string normalized = NormalizeDifficultyLabel(labelZh);
        for (int i = 0; i < StandardDifficultyLabelsZh.Length; i++)
        {
            if (StandardDifficultyLabelsZh[i] == normalized)
                return i;
        }
        if (normalized == "Boss" || normalized == "BOSS" || normalized == "魔王級")
            return 4;
        if (normalized == "Hard" || normalized == "困難級")
            return 3;
        if (normalized == "Normal" || normalized == "普通級")
            return 2;
        if (normalized == "Easy" || normalized == "簡單級")
            return 1;
        if (normalized == "Intro" || normalized == "入門級")
            return 0;
        return 0;
    }

    private static string ResultCodeToLabel(int result)
    {
        if (result == 1) return "勝利";
        if (result == -1) return "戰敗";
        if (result == 2) return "平手";
        if (result == 3) return "中離";
        return "未知";
    }

    private static string SafeCsv(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "-" : s.Replace("\n", " ").Replace("\r", " ");
    }

    private static string SafeValue(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "-" : s;
    }
}

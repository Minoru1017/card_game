using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public static class PlayerProfileCsvService
{
    private const string FileName = "player_profile.csv";
    private const string CurrentSchemaVersion = "1";
    private const string ExistingPlayerDataBackupFile = "playerdata_existing_backup.csv";
    private const string ExistingProfileBackupFile = "player_profile_existing_backup.csv";

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
        p.lastResult = "無";
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
            heroes = "尚無英雄資料",
            startDate = DateTime.Now.ToString("yyyy-MM-dd"),
            wins = 0,
            losses = 0,
            draws = 0,
            lastResult = "無"
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
        string[] existing = File.Exists(PlayerDataPath) ? File.ReadAllLines(PlayerDataPath) : Array.Empty<string>();
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
                if (slotKey == "coins" || slotKey == "selected_deck_slot" || slotKey == "card" || slotKey == "deck" || slotKey == "deckslot")
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
        File.WriteAllLines(PlayerDataPath, merged);
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

    public static void RecordBattleResult(int result)
    {
        PlayerProfile p = RefreshProfileFromRuntime();
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
        Save(p);
    }

    public static void RecordPlayerQuit()
    {
        PlayerProfile p = RefreshProfileFromRuntime();
        p.quits++;
        p.lastResult = "玩家中離";
        Save(p);
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
            " (總場次 " + total + ", 最近結果: " + SafeValue(p.lastResult) + ")";
    }

    public static PlayerProfile LoadOrCreate()
    {
        PlayerProfile p;
        if (!TryLoadProfileFromActiveSlotRows(out p))
            p = Load();
        if (string.IsNullOrWhiteSpace(p.uuid))
        {
            p.schemaVersion = CurrentSchemaVersion;
            p.uuid = Guid.NewGuid().ToString("N");
            p.role = string.IsNullOrWhiteSpace(p.role) ? "一般玩家" : p.role;
            p.startDate = string.IsNullOrWhiteSpace(p.startDate) ? DateTime.Now.ToString("yyyy-MM-dd") : p.startDate;
            if (string.IsNullOrWhiteSpace(p.decks)) p.decks = "尚無牌組資料";
            if (string.IsNullOrWhiteSpace(p.heroes)) p.heroes = "尚無英雄資料";
            if (string.IsNullOrWhiteSpace(p.lastResult)) p.lastResult = "無";
            p.quits = Mathf.Max(0, p.quits);
            Save(p);
        }
        return p;
    }

    private static bool TryLoadProfileFromActiveSlotRows(out PlayerProfile p)
    {
        p = new PlayerProfile();
        if (!File.Exists(PlayerDataPath)) return false;

        string[] rows = File.ReadAllLines(PlayerDataPath);
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

    private static PlayerProfile Load()
    {
        PlayerProfile p = new PlayerProfile();
        if (!File.Exists(ProfilePath)) return p;

        string[] rows = File.ReadAllLines(ProfilePath);
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
        var rows = new List<string>(13)
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
        File.WriteAllLines(ProfilePath, rows);
        SaveProjectSnapshot(FileName, rows);
        SyncProfileIntoPlayerDataCsv(p);
        SyncProfileIntoActiveSlotRows(p);
    }

    private static void SyncProfileIntoPlayerDataCsv(PlayerProfile p)
    {
        string[] existing = File.Exists(PlayerDataPath) ? File.ReadAllLines(PlayerDataPath) : Array.Empty<string>();
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
            string key = cols.Length > 0 ? cols[0].Trim() : string.Empty;
            if (key.StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
                continue; // replace old profile snapshot rows
            merged.Add(row);
        }

        merged.Add("profile_uuid," + SafeCsv(p.uuid));
        merged.Add("profile_schema_version," + SafeCsv(string.IsNullOrWhiteSpace(p.schemaVersion) ? CurrentSchemaVersion : p.schemaVersion));
        merged.Add("profile_role," + SafeCsv(p.role));
        merged.Add("profile_decks," + SafeCsv(p.decks));
        merged.Add("profile_heroes," + SafeCsv(p.heroes));
        merged.Add("profile_start_date," + SafeCsv(p.startDate));
        merged.Add("profile_wins," + p.wins);
        merged.Add("profile_losses," + p.losses);
        merged.Add("profile_draws," + p.draws);
        merged.Add("profile_quits," + p.quits);
        merged.Add("profile_last_result," + SafeCsv(p.lastResult));

        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        File.WriteAllLines(PlayerDataPath, merged);
        SaveProjectSnapshot("playerdata.profile_mirror.csv", merged);
    }

    private static void SyncProfileIntoActiveSlotRows(PlayerProfile p)
    {
        string[] existing = File.Exists(PlayerDataPath) ? File.ReadAllLines(PlayerDataPath) : Array.Empty<string>();
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
            if (cols.Length >= 3 &&
                string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(cols[1].Trim(), out int slot) &&
                slot == activeSlot &&
                cols[2].Trim().StartsWith("profile_", StringComparison.OrdinalIgnoreCase))
                continue;
            merged.Add(row);
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

        File.WriteAllLines(PlayerDataPath, merged);
    }

    private static bool TryReadActiveSlotProfile(out PlayerProfile p)
    {
        p = new PlayerProfile();
        if (!File.Exists(PlayerDataPath)) return false;
        string[] rows = File.ReadAllLines(PlayerDataPath);
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
            parts.Add("牌組" + (slot + 1) + ":" + total + "張");
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "尚無牌組資料";
    }

    private static string BuildHeroSummary(PlayerData playerData)
    {
        if (playerData == null) return "尚無英雄資料";
        CardStore cardStore = playerData.CardStore != null
            ? playerData.CardStore
            : UnityEngine.Object.FindFirstObjectByType<CardStore>();
        var heroNames = new HashSet<string>();
        foreach (KeyValuePair<int, int> kv in playerData.playerCollection)
        {
            if (kv.Value <= 0) continue;
            int cardId = kv.Key;
            if (cardId < 0) continue; // spell keys are negative.
            if (cardStore == null) continue;
            Card card = cardStore.GetCardById(cardId);
            MonsterCard monster = card as MonsterCard;
            if (monster == null) continue;
            if (!string.IsNullOrWhiteSpace(monster.cardName))
                heroNames.Add(monster.cardName);
        }
        if (heroNames.Count <= 0) return "尚無英雄資料";
        return string.Join("、", heroNames);
    }

    private static PlayerData ResolvePlayerData()
    {
        PlayerData pd = UnityEngine.Object.FindFirstObjectByType<PlayerData>();
        if (pd != null) return pd;
        PlayerData[] all = Resources.FindObjectsOfTypeAll<PlayerData>();
        for (int i = 0; i < all.Length; i++)
        {
            PlayerData p = all[i];
            if (p == null || p.gameObject == null) continue;
            if (!p.gameObject.scene.IsValid()) continue;
            return p;
        }
        return null;
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

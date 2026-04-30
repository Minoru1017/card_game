using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using System;

public class PlayerData : MonoBehaviour
{
    public const int MaxPlayerSlots = 3;
    public CardStore CardStore;
    public int playerCoins;
    /// <summary>Owned cards: key = runtime id (monster ≥0, spell &lt;0 via <see cref="DeckCardId"/>).</summary>
    public readonly Dictionary<int, int> playerCollection = new Dictionary<int, int>();
    public int deckSlotCount = 3;
    public int selectedDeckSlot = 0;
    public int totalCoins;
    [Range(1, MaxPlayerSlots)] public int activePlayerSlot = 1;
    public string activePlayerSlotName = "玩家1";

    private Dictionary<int, int>[] deckSlotMaps;
    private readonly List<string> cachedOtherSlotRows = new List<string>(128);

    [Header("UI")]
    public TextMeshProUGUI coinsText;

    void Awake()
    {
        if (CardStore != null) CardStore.LoadCardData();
        LoadPlayerData();
        RefreshCoins();
    }

    void Start()
    {
    }

    void Update()
    {
    }

    public void RefreshCoins()
    {
        if (coinsText != null)
            coinsText.text = GetCoinsDisplayText();
    }

    public string GetCoinsDisplayText()
    {
        return playerCoins.ToString();
    }

    private void EnsureDeckSlotMaps()
    {
        if (deckSlotCount <= 0) deckSlotCount = 3;

        if (deckSlotMaps == null || deckSlotMaps.Length != deckSlotCount)
        {
            var next = new Dictionary<int, int>[deckSlotCount];
            for (int i = 0; i < deckSlotCount; i++)
            {
                if (deckSlotMaps != null && i < deckSlotMaps.Length && deckSlotMaps[i] != null)
                    next[i] = new Dictionary<int, int>(deckSlotMaps[i]);
                else
                    next[i] = new Dictionary<int, int>();
            }
            deckSlotMaps = next;
        }
    }

    public int GetCollectionCount(int runtimeCardId)
    {
        return playerCollection.TryGetValue(runtimeCardId, out int n) ? n : 0;
    }

    public void SetCollectionCount(int runtimeCardId, int count)
    {
        if (count <= 0) playerCollection.Remove(runtimeCardId);
        else playerCollection[runtimeCardId] = count;
    }

    public void AddCollection(int runtimeCardId, int delta)
    {
        if (delta == 0) return;
        int n = GetCollectionCount(runtimeCardId) + delta;
        SetCollectionCount(runtimeCardId, n);
    }

    public int GetDeckCount(int slot, int runtimeCardId)
    {
        EnsureDeckSlotMaps();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        return deckSlotMaps[slot].TryGetValue(runtimeCardId, out int n) ? n : 0;
    }

    public int GetSelectedDeckCount(int runtimeCardId) => GetDeckCount(selectedDeckSlot, runtimeCardId);

    public void SetDeckCount(int slot, int runtimeCardId, int count)
    {
        EnsureDeckSlotMaps();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        if (count <= 0) deckSlotMaps[slot].Remove(runtimeCardId);
        else deckSlotMaps[slot][runtimeCardId] = count;
    }

    public void SetSelectedDeckCount(int runtimeCardId, int count) => SetDeckCount(selectedDeckSlot, runtimeCardId, count);

    public void AddDeckCount(int slot, int runtimeCardId, int delta)
    {
        if (delta == 0) return;
        int n = GetDeckCount(slot, runtimeCardId) + delta;
        SetDeckCount(slot, runtimeCardId, n);
    }

    public void AddSelectedDeckCount(int runtimeCardId, int delta) => AddDeckCount(selectedDeckSlot, runtimeCardId, delta);

    public IReadOnlyDictionary<int, int> GetDeckMap(int slot)
    {
        EnsureDeckSlotMaps();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        return deckSlotMaps[slot];
    }

    public void ClearAllCollectionAndDecks()
    {
        playerCollection.Clear();
        EnsureDeckSlotMaps();
        for (int i = 0; i < deckSlotMaps.Length; i++)
            deckSlotMaps[i].Clear();
    }

    public void LoadPlayerData()
    {
        EnsureDeckSlotMaps();

        playerCollection.Clear();
        for (int s = 0; s < deckSlotMaps.Length; s++)
            deckSlotMaps[s].Clear();

        string path = GetPlayerDataPath();

        if (!File.Exists(path))
        {
            playerCoins = 100;
            totalCoins = playerCoins;
            SavePlayerData();
            return;
        }

        string[] dataRow = File.ReadAllLines(path);
        Debug.Log("Load from persistent: " + path);
        activePlayerSlot = Mathf.Clamp(ReadActiveSlotFromRows(dataRow), 1, MaxPlayerSlots);
        cachedOtherSlotRows.Clear();
        bool hasDeckSlotData = false;
        bool hasSlotRows = false;

        foreach (var row in dataRow)
        {
            string[] rowArray = row.Split(',');
            if (rowArray == null || rowArray.Length == 0) continue;
            if (rowArray[0] == "#") continue;

            if (rowArray[0] == "slot")
            {
                hasSlotRows = true;
                if (rowArray.Length < 4) continue;
                if (!int.TryParse(rowArray[1].Trim(), out int slotIndex)) continue;
                if (slotIndex != activePlayerSlot)
                {
                    cachedOtherSlotRows.Add(row);
                    continue;
                }
                string[] scoped = new string[rowArray.Length - 2];
                Array.Copy(rowArray, 2, scoped, 0, scoped.Length);
                ParsePlayerRow(scoped, ref hasDeckSlotData);
                continue;
            }

            if (!hasSlotRows)
            {
                ParsePlayerRow(rowArray, ref hasDeckSlotData);
            }
        }

        Debug.Log("Loaded coins=" + playerCoins);
    }

    private void ParsePlayerRow(string[] rowArray, ref bool hasDeckSlotData)
    {
        if (rowArray == null || rowArray.Length == 0) return;
        if (rowArray[0] == "coins")
        {
            if (rowArray.Length < 2) return;
            playerCoins = int.Parse(rowArray[1].Trim());
            totalCoins = playerCoins;
        }
        else if (rowArray[0] == "card")
        {
            if (!TryParseCollectionRow(rowArray, out int key, out int num)) return;
            SetCollectionCount(key, num);
        }
        else if (rowArray[0] == "deck")
        {
            if (hasDeckSlotData) return;
            if (!TryParseDeckRow(rowArray, out int key, out int num)) return;
            SetDeckCount(0, key, num);
        }
        else if (rowArray[0] == "deckslot")
        {
            if (rowArray.Length < 4) return;
            int slot = int.Parse(rowArray[1].Trim());
            if (slot < 0 || slot >= deckSlotCount) return;

            if (rowArray.Length >= 5 && (rowArray[2] == "m" || rowArray[2] == "s"))
            {
                hasDeckSlotData = true;
                if (!TryParseTypedDeckslotRow(rowArray, out int key, out int num)) return;
                SetDeckCount(slot, key, num);
            }
            else
            {
                hasDeckSlotData = true;
                int legacyId = int.Parse(rowArray[2].Trim());
                int num = int.Parse(rowArray[3].Trim());
                int key = NormalizeLegacyUnifiedRowId(legacyId);
                SetDeckCount(slot, key, num);
            }
        }
        else if (rowArray[0] == "selected_deck_slot")
        {
            if (rowArray.Length < 2) return;
            selectedDeckSlot = Mathf.Clamp(int.Parse(rowArray[1].Trim()), 0, deckSlotCount - 1);
        }
        else if (rowArray[0] == "slot_name")
        {
            if (rowArray.Length < 2) return;
            activePlayerSlotName = string.IsNullOrWhiteSpace(rowArray[1]) ? ("玩家" + activePlayerSlot) : rowArray[1].Trim();
        }
    }

    /// <summary>Legacy 3-column rows used one int for monsters and spells. If a monster exists at <paramref name="legacyId"/>, keep it; otherwise map old spell ids to spell keys.</summary>
    private int NormalizeLegacyUnifiedRowId(int legacyId)
    {
        if (CardStore != null && CardStore.GetCardById(legacyId) is MonsterCard)
            return legacyId;
        return DeckCardId.NormalizeLegacyUnifiedId(legacyId);
    }

    private bool TryParseCollectionRow(string[] rowArray, out int key, out int num)
    {
        key = 0;
        num = 0;
        if (rowArray.Length >= 4 && rowArray[1] == "m")
        {
            key = int.Parse(rowArray[2].Trim());
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 4 && rowArray[1] == "s")
        {
            int ord = int.Parse(rowArray[2].Trim());
            key = DeckCardId.SpellKeyFromOrdinal(ord);
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 3)
        {
            int legacyId = int.Parse(rowArray[1].Trim());
            num = int.Parse(rowArray[2].Trim());
            key = NormalizeLegacyUnifiedRowId(legacyId);
            return true;
        }
        return false;
    }

    private bool TryParseDeckRow(string[] rowArray, out int key, out int num)
    {
        key = 0;
        num = 0;
        if (rowArray.Length >= 4 && rowArray[1] == "m")
        {
            key = int.Parse(rowArray[2].Trim());
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 4 && rowArray[1] == "s")
        {
            int ord = int.Parse(rowArray[2].Trim());
            key = DeckCardId.SpellKeyFromOrdinal(ord);
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 3)
        {
            int legacyId = int.Parse(rowArray[1].Trim());
            num = int.Parse(rowArray[2].Trim());
            key = NormalizeLegacyUnifiedRowId(legacyId);
            return true;
        }
        return false;
    }

    private static bool TryParseTypedDeckslotRow(string[] rowArray, out int key, out int num)
    {
        key = 0;
        num = 0;
        if (rowArray[2] == "m")
        {
            key = int.Parse(rowArray[3].Trim());
            num = int.Parse(rowArray[4].Trim());
            return true;
        }
        if (rowArray[2] == "s")
        {
            int ord = int.Parse(rowArray[3].Trim());
            key = DeckCardId.SpellKeyFromOrdinal(ord);
            num = int.Parse(rowArray[4].Trim());
            return true;
        }
        return false;
    }

    public void SavePlayerData()
    {
        string dir = Application.persistentDataPath;
        string path = GetPlayerDataPath();
        Directory.CreateDirectory(dir);

        var datas = new List<string>();
        datas.Add($"active_slot,{activePlayerSlot}");

        // Preserve all rows for non-active slots.
        for (int i = 0; i < cachedOtherSlotRows.Count; i++)
            datas.Add(cachedOtherSlotRows[i]);

        var current = new List<string>();
        current.Add($"slot,{activePlayerSlot},coins,{playerCoins}");
        current.Add($"slot,{activePlayerSlot},selected_deck_slot,{selectedDeckSlot}");
        current.Add($"slot,{activePlayerSlot},slot_name,{SanitizeSlotName(activePlayerSlotName, activePlayerSlot)}");

        foreach (var kv in playerCollection)
        {
            if (kv.Value == 0) continue;
            if (DeckCardId.IsSpellKey(kv.Key))
                current.Add($"slot,{activePlayerSlot},card,s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
            else
                current.Add($"slot,{activePlayerSlot},card,m,{kv.Key},{kv.Value}");
        }

        EnsureDeckSlotMaps();
        for (int slot = 0; slot < deckSlotMaps.Length; slot++)
        {
            foreach (var kv in deckSlotMaps[slot])
            {
                if (kv.Value == 0) continue;
                if (DeckCardId.IsSpellKey(kv.Key))
                    current.Add($"slot,{activePlayerSlot},deckslot,{slot},s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
                else
                    current.Add($"slot,{activePlayerSlot},deckslot,{slot},m,{kv.Key},{kv.Value}");
            }
        }

        foreach (var kv in deckSlotMaps[selectedDeckSlot])
        {
            if (kv.Value == 0) continue;
            if (DeckCardId.IsSpellKey(kv.Key))
                current.Add($"slot,{activePlayerSlot},deck,s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
            else
                current.Add($"slot,{activePlayerSlot},deck,m,{kv.Key},{kv.Value}");
        }

        for (int i = 0; i < current.Count; i++) datas.Add(current[i]);
        EnsureAllSlotContainers(datas);

        File.WriteAllLines(path, datas);
        Debug.Log("Save path: " + path);
    }

    private static int ReadActiveSlotFromRows(string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 2) continue;
            if (!string.Equals(c[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(c[1].Trim(), out int slot)) continue;
            return Mathf.Clamp(slot, 1, MaxPlayerSlots);
        }
        return 1;
    }

    private static void EnsureAllSlotContainers(List<string> rows)
    {
        bool[] hasSlotCoins = new bool[MaxPlayerSlots + 1];
        bool[] hasSlotSelect = new bool[MaxPlayerSlots + 1];
        bool[] hasSlotName = new bool[MaxPlayerSlots + 1];
        for (int i = 0; i < rows.Count; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 4) continue;
            if (!string.Equals(c[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(c[1].Trim(), out int slot)) continue;
            if (slot < 1 || slot > MaxPlayerSlots) continue;
            string key = c[2].Trim();
            if (key == "coins") hasSlotCoins[slot] = true;
            else if (key == "selected_deck_slot") hasSlotSelect[slot] = true;
            else if (key == "slot_name") hasSlotName[slot] = true;
        }
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
        {
            if (!hasSlotCoins[slot]) rows.Add($"slot,{slot},coins,100");
            if (!hasSlotSelect[slot]) rows.Add($"slot,{slot},selected_deck_slot,0");
            if (!hasSlotName[slot]) rows.Add($"slot,{slot},slot_name,玩家{slot}");
        }
    }

    private static string GetPlayerDataPath()
    {
        return Path.Combine(Application.persistentDataPath, "playerdata.csv");
    }

    public static bool TryGetActiveSlotCoinsFromSave(out int coins)
    {
        coins = 0;
        string path = GetPlayerDataPath();
        if (!File.Exists(path)) return false;

        string[] rows = File.ReadAllLines(path);
        int activeSlot = Mathf.Clamp(ReadActiveSlotFromRows(rows), 1, MaxPlayerSlots);

        // Preferred format: slot,<active_slot>,coins,<value>
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot != activeSlot) continue;
            if (!string.Equals(cols[2].Trim(), "coins", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[3].Trim(), out coins)) continue;
            return true;
        }

        // Legacy fallback: coins,<value>
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 2) continue;
            if (!string.Equals(cols[0].Trim(), "coins", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out coins)) continue;
            return true;
        }

        return false;
    }

    public static void SelectActivePlayerSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, MaxPlayerSlots);
        string path = GetPlayerDataPath();
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        string[] existing = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
        var rows = new List<string>(existing.Length + 2);
        bool activeWritten = false;
        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (string.IsNullOrWhiteSpace(row)) { rows.Add(row); continue; }
            string[] c = row.Split(',');
            if (c.Length > 0 && string.Equals(c[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase))
            {
                if (!activeWritten)
                {
                    rows.Add($"active_slot,{slot}");
                    activeWritten = true;
                }
                continue;
            }
            rows.Add(row);
        }
        if (!activeWritten) rows.Insert(0, $"active_slot,{slot}");
        EnsureAllSlotContainers(rows);
        File.WriteAllLines(path, rows);
    }

    public static int FindFirstEmptySlot()
    {
        string path = GetPlayerDataPath();
        if (!File.Exists(path)) return 1;
        string[] rows = File.ReadAllLines(path);
        bool[] nonDefault = new bool[MaxPlayerSlots + 1];
        for (int i = 0; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 4 || c[0].Trim() != "slot") continue;
            if (!int.TryParse(c[1].Trim(), out int slot) || slot < 1 || slot > MaxPlayerSlots) continue;
            string key = c[2].Trim();
            if (key == "card" || key == "deck" || key == "deckslot") nonDefault[slot] = true;
            if (key == "coins" && int.TryParse(c[3].Trim(), out int coins) && coins != 100) nonDefault[slot] = true;
        }
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
            if (!nonDefault[slot]) return slot;
        return 1;
    }

    public static void DeleteSlotData(int slot, int defaultCoins = 100)
    {
        slot = Mathf.Clamp(slot, 1, MaxPlayerSlots);
        string path = GetPlayerDataPath();
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        string[] existing = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
        int active = Mathf.Clamp(ReadActiveSlotFromRows(existing), 1, MaxPlayerSlots);
        if (active == slot)
        {
            int fallback = FindFirstNonDeletedSlot(existing, slot);
            active = fallback > 0 ? fallback : 1;
        }

        var rows = new List<string>(Mathf.Max(8, existing.Length + 4));
        bool activeWritten = false;

        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (string.IsNullOrWhiteSpace(row))
            {
                rows.Add(row);
                continue;
            }

            string[] c = row.Split(',');
            if (c.Length == 0)
            {
                rows.Add(row);
                continue;
            }

            if (string.Equals(c[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase))
            {
                if (!activeWritten)
                {
                    rows.Add($"active_slot,{active}");
                    activeWritten = true;
                }
                continue;
            }

            if (c.Length >= 3 &&
                string.Equals(c[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(c[1].Trim(), out int rowSlot) &&
                rowSlot == slot)
            {
                // Remove all rows for deleted slot and rebuild minimal defaults below.
                continue;
            }

            rows.Add(row);
        }

        if (!activeWritten) rows.Insert(0, $"active_slot,{active}");
        rows.Add($"slot,{slot},coins,{Mathf.Max(0, defaultCoins)}");
        rows.Add($"slot,{slot},selected_deck_slot,0");
        rows.Add($"slot,{slot},slot_name,玩家{slot}");
        EnsureAllSlotContainers(rows);
        File.WriteAllLines(path, rows);
    }

    private static int FindFirstNonDeletedSlot(string[] rows, int deletedSlot)
    {
        bool[] hasData = new bool[MaxPlayerSlots + 1];
        bool[] hasRows = new bool[MaxPlayerSlots + 1];
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] c = row.Split(',');
            if (c.Length < 4 || c[0].Trim() != "slot") continue;
            if (!int.TryParse(c[1].Trim(), out int slot) || slot < 1 || slot > MaxPlayerSlots) continue;
            if (slot == deletedSlot) continue;
            hasRows[slot] = true;
            string key = c[2].Trim();
            if (key == "card" || key == "deck" || key == "deckslot") hasData[slot] = true;
            if (key == "coins" && int.TryParse(c[3].Trim(), out int coins) && coins != 100) hasData[slot] = true;
        }
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
            if (slot != deletedSlot && hasData[slot]) return slot;
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
            if (slot != deletedSlot && hasRows[slot]) return slot;
        return 1;
    }

    public struct SlotSnapshot
    {
        public int slot;
        public bool hasData;
        public int coins;
        public string slotName;
    }

    public struct SlotDeleteSummary
    {
        public int slot;
        public string slotName;
        public string uuid;
        public string startDate;
        public string deckSummary;
        public int wins;
        public int losses;
        public int draws;
        public int quits;
    }

    public static SlotSnapshot[] GetSlotSnapshots()
    {
        bool[] hasData = new bool[MaxPlayerSlots + 1];
        int[] coins = new int[MaxPlayerSlots + 1];
        string[] names = new string[MaxPlayerSlots + 1];
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
        {
            hasData[slot] = false;
            coins[slot] = 100;
            names[slot] = "玩家" + slot;
        }

        string path = GetPlayerDataPath();
        if (!File.Exists(path))
        {
            return BuildSnapshots(hasData, coins, names);
        }

        string[] rows = File.ReadAllLines(path);
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] c = row.Split(',');
            if (c.Length < 4) continue;
            if (c[0].Trim() != "slot") continue;
            if (!int.TryParse(c[1].Trim(), out int slot) || slot < 1 || slot > MaxPlayerSlots) continue;
            string key = c[2].Trim();
            string val = c[3].Trim();

            if (key == "coins" && int.TryParse(val, out int parsedCoins))
                coins[slot] = parsedCoins;
            else if (key == "slot_name")
                names[slot] = string.IsNullOrWhiteSpace(val) ? ("玩家" + slot) : val;

            if (key == "card" || key == "deck" || key == "deckslot")
                hasData[slot] = true;
            else if (key == "coins" && coins[slot] != 100)
                hasData[slot] = true;
        }
        return BuildSnapshots(hasData, coins, names);
    }

    public static SlotDeleteSummary GetSlotDeleteSummary(int slot)
    {
        slot = Mathf.Clamp(slot, 1, MaxPlayerSlots);
        SlotDeleteSummary summary = new SlotDeleteSummary
        {
            slot = slot,
            slotName = "玩家" + slot,
            uuid = "-",
            startDate = "-",
            deckSummary = "尚無牌組資料",
            wins = 0,
            losses = 0,
            draws = 0,
            quits = 0
        };

        string path = GetPlayerDataPath();
        if (!File.Exists(path)) return summary;
        string[] rows = File.ReadAllLines(path);
        int deckRowCount = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] c = row.Split(',');
            if (c.Length < 4) continue;
            if (!string.Equals(c[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(c[1].Trim(), out int rowSlot) || rowSlot != slot) continue;
            string key = c[2].Trim();
            string val = c[3].Trim();

            if (key == "slot_name") summary.slotName = string.IsNullOrWhiteSpace(val) ? ("玩家" + slot) : val;
            else if (key == "profile_uuid") summary.uuid = string.IsNullOrWhiteSpace(val) ? "-" : val;
            else if (key == "profile_start_date") summary.startDate = string.IsNullOrWhiteSpace(val) ? "-" : val;
            else if (key == "profile_decks") summary.deckSummary = string.IsNullOrWhiteSpace(val) ? "尚無牌組資料" : val;
            else if (key == "profile_wins") int.TryParse(val, out summary.wins);
            else if (key == "profile_losses") int.TryParse(val, out summary.losses);
            else if (key == "profile_draws") int.TryParse(val, out summary.draws);
            else if (key == "profile_quits") int.TryParse(val, out summary.quits);
            else if (key == "deckslot")
            {
                if (c.Length >= 7 && int.TryParse(c[c.Length - 1].Trim(), out int count) && count > 0)
                    deckRowCount += count;
            }
        }

        if (summary.deckSummary == "尚無牌組資料" && deckRowCount > 0)
            summary.deckSummary = "已配置牌組，共 " + deckRowCount + " 張";
        return summary;
    }

    private static SlotSnapshot[] BuildSnapshots(bool[] hasData, int[] coins, string[] names)
    {
        var snapshots = new SlotSnapshot[MaxPlayerSlots];
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
        {
            snapshots[slot - 1] = new SlotSnapshot
            {
                slot = slot,
                hasData = hasData[slot],
                coins = coins[slot],
                slotName = names[slot]
            };
        }
        return snapshots;
    }

    public static string GetActivePlayerSlotName()
    {
        string path = GetPlayerDataPath();
        if (!File.Exists(path)) return "玩家1";
        int active = 1;
        string[] rows = File.ReadAllLines(path);
        active = Mathf.Clamp(ReadActiveSlotFromRows(rows), 1, MaxPlayerSlots);
        for (int i = 0; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 4 || c[0].Trim() != "slot") continue;
            if (!int.TryParse(c[1].Trim(), out int slot) || slot != active) continue;
            if (c[2].Trim() != "slot_name") continue;
            return string.IsNullOrWhiteSpace(c[3]) ? ("玩家" + active) : c[3].Trim();
        }
        return "玩家" + active;
    }

    public static void SetActivePlayerSlotName(string name)
    {
        string path = GetPlayerDataPath();
        string dir = Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        string[] existing = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
        int active = Mathf.Clamp(ReadActiveSlotFromRows(existing), 1, MaxPlayerSlots);
        string safeName = SanitizeSlotName(name, active);

        var rows = new List<string>(existing.Length + 2);
        bool wrote = false;
        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (string.IsNullOrWhiteSpace(row)) { rows.Add(row); continue; }
            string[] c = row.Split(',');
            if (c.Length >= 4 && c[0].Trim() == "slot" && int.TryParse(c[1].Trim(), out int slot) && slot == active && c[2].Trim() == "slot_name")
            {
                if (!wrote)
                {
                    rows.Add($"slot,{active},slot_name,{safeName}");
                    wrote = true;
                }
                continue;
            }
            rows.Add(row);
        }
        if (!wrote) rows.Add($"slot,{active},slot_name,{safeName}");
        EnsureAllSlotContainers(rows);
        File.WriteAllLines(path, rows);
    }

    private static string SanitizeSlotName(string name, int slot)
    {
        string n = string.IsNullOrWhiteSpace(name) ? ("玩家" + slot) : name.Trim();
        n = n.Replace("\n", " ").Replace("\r", " ").Replace(",", " ");
        if (n.Length > 24) n = n.Substring(0, 24);
        return n;
    }

    public void SetSelectedDeckSlot(int slot)
    {
        EnsureDeckSlotMaps();
        selectedDeckSlot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
    }

    public int GetSelectedDeckTotalCount()
    {
        EnsureDeckSlotMaps();
        int total = 0;
        foreach (var kv in deckSlotMaps[selectedDeckSlot])
        {
            if (kv.Value > 0) total += kv.Value;
        }
        return total;
    }
}

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using System;

public class PlayerData : MonoBehaviour
{
    public const int MaxPlayerSlots = 3;
    /// <summary>Buildbeck UI 固定 5 個牌組分頁；須與場景按鈕數一致。</summary>
    public const int MinDeckSlotCount = 5;
    public CardStore CardStore;
    public int playerCoins;
    /// <summary>Owned cards: key = runtime id (monster ≥0, spell &lt;0 via <see cref="DeckCardId"/>).</summary>
    public readonly Dictionary<int, int> playerCollection = new Dictionary<int, int>();
    public int deckSlotCount = 5;
    public int selectedDeckSlot = 0;
    public int totalCoins;
    [Range(1, MaxPlayerSlots)] public int activePlayerSlot = 1;
    public string activePlayerSlotName = "玩家1";

    private Dictionary<int, int>[] deckSlotMaps;
    /// <summary>Per deck-slot display names (Buildbeck UI). Empty entry falls back to 「牌組{n}」.</summary>
    private string[] deckSlotDisplayNames;
    /// <summary>怪物牌熟練度勝場（key = monster id）。</summary>
    private readonly Dictionary<int, CardProficiencyWins> cardProficiencyWins = new Dictionary<int, CardProficiencyWins>();
    private readonly List<string> cachedOtherSlotRows = new List<string>(128);

    [Header("UI")]
    public TextMeshProUGUI coinsText;

    /// <summary>唯一應讀寫存檔的 PlayerData（優先 DataManager 上的實例）。</summary>
    public static PlayerData ResolveCanonical()
    {
        GameObject dmGo = GameObject.Find("DataManager");
        if (dmGo != null)
        {
            PlayerData onDm = dmGo.GetComponent<PlayerData>();
            if (onDm != null) return onDm;
        }

        PlayerData[] all = UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        PlayerData withDeckManager = null;
        PlayerData any = null;
        for (int i = 0; i < all.Length; i++)
        {
            PlayerData p = all[i];
            if (p == null) continue;
            any ??= p;
            if (p.GetComponent<DeckManager>() != null)
                withDeckManager = p;
        }

        if (withDeckManager != null) return withDeckManager;
        if (any != null) return any;
        return UnityEngine.Object.FindFirstObjectByType<PlayerData>();
    }

    void Awake()
    {
        if (CardStore != null) CardStore.LoadCardData();
        if (ResolveCanonical() == this)
        {
            EnsureMinimumDeckSlotCount();
            LoadPlayerData();
            RefreshCoins();
        }
    }

    /// <summary>避免 prefab 上 deckSlotCount=3 導致第 4、5 槽名稱與牌組被 clamp 到槽位 2。</summary>
    public void EnsureMinimumDeckSlotCount()
    {
        if (deckSlotCount < MinDeckSlotCount)
            deckSlotCount = MinDeckSlotCount;
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
        EnsureMinimumDeckSlotCount();
        if (deckSlotCount <= 0) deckSlotCount = MinDeckSlotCount;

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
        cardProficiencyWins.Clear();
    }

    public CardProficiencyWins GetCardProficiencyWins(int monsterId)
    {
        return cardProficiencyWins.TryGetValue(monsterId, out CardProficiencyWins wins) ? wins : default;
    }

    public void SetCardProficiencyWins(int monsterId, float progressAny, int winsNormalDifficulty)
    {
        if (progressAny <= 0.001f && winsNormalDifficulty <= 0)
        {
            cardProficiencyWins.Remove(monsterId);
            return;
        }

        cardProficiencyWins[monsterId] = new CardProficiencyWins
        {
            progressAny = Mathf.Max(0f, progressAny),
            winsNormalDifficulty = Mathf.Max(0, winsNormalDifficulty)
        };
    }

    /// <summary>對戰結算：累加 toward-B 進度；普通難度勝利時 winsNormal +1。</summary>
    public void AddCardProficiencyProgress(int monsterId, float progressDelta, bool addNormalWin)
    {
        if (progressDelta <= 0f && !addNormalWin) return;

        CardProficiencyWins w = GetCardProficiencyWins(monsterId);
        if (progressDelta > 0f)
            w.progressAny = Mathf.Max(0f, w.progressAny + progressDelta);
        if (addNormalWin)
            w.winsNormalDifficulty++;
        cardProficiencyWins[monsterId] = w;
    }

    public void LoadPlayerData()
    {
        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
        {
            canonical.LoadPlayerData();
            return;
        }

        EnsureMinimumDeckSlotCount();
        EnsureDeckSlotMaps();

        playerCollection.Clear();
        for (int s = 0; s < deckSlotMaps.Length; s++)
            deckSlotMaps[s].Clear();
        cardProficiencyWins.Clear();
        deckSlotDisplayNames = null;

        string path = GetPlayerDataPath();

        if (!PlayerPersistSafeIO.ExistsAnyWithBackups(path))
        {
            playerCoins = 100;
            totalCoins = playerCoins;
            SavePlayerData();
            return;
        }

        foreach (string candidatePath in PlayerPersistSafeIO.EnumerateLoadCandidates(path))
        {
            if (!File.Exists(candidatePath)) continue;
            string[] dataRow;
            try
            {
                dataRow = File.ReadAllLines(candidatePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("PlayerData: could not read " + candidatePath + " -> " + ex.Message);
                continue;
            }

            if (!PlayerPersistSafeIO.LooksLikePlayerDataCsv(dataRow)) continue;

            playerCollection.Clear();
            for (int s = 0; s < deckSlotMaps.Length; s++)
                deckSlotMaps[s].Clear();
            cardProficiencyWins.Clear();
            deckSlotDisplayNames = null;

            if (!TryApplyLoadedPlayerDataRows(dataRow)) continue;

            Debug.Log("Load from persistent: " + candidatePath);
            Debug.Log("Loaded coins=" + playerCoins);
            return;
        }

        Debug.LogError("PlayerData: all save candidates failed to load; recreating defaults.");
        playerCoins = 100;
        totalCoins = playerCoins;
        SavePlayerData();
    }

    private bool TryApplyLoadedPlayerDataRows(string[] dataRow)
    {
        try
        {
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
                    ParsePlayerRow(rowArray, ref hasDeckSlotData);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerData: parse failed for candidate save -> " + ex.Message);
            return false;
        }
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
        else if (rowArray[0] == "deck_slot_name")
        {
            if (rowArray.Length < 3) return;
            if (!int.TryParse(rowArray[1].Trim(), out int deckSlotIdx)) return;
            EnsureDeckSlotMaps();
            EnsureDeckSlotNamesBuffer();
            deckSlotIdx = Mathf.Clamp(deckSlotIdx, 0, deckSlotCount - 1);
            // 名稱可含逗號：存檔以逗號 split 後需自索引 2 起整段接回。
            string rawLabel = rowArray[2];
            if (rowArray.Length > 3)
            {
                for (int ri = 3; ri < rowArray.Length; ri++)
                    rawLabel = rawLabel + "," + rowArray[ri];
            }
            deckSlotDisplayNames[deckSlotIdx] = SanitizeDeckSlotName(rawLabel);
        }
        else if (rowArray[0] == "slot_name")
        {
            if (rowArray.Length < 2) return;
            activePlayerSlotName = string.IsNullOrWhiteSpace(rowArray[1]) ? ("玩家" + activePlayerSlot) : rowArray[1].Trim();
        }
        else if (rowArray[0] == "proficiency")
        {
            if (rowArray.Length < 5) return;
            if (rowArray[1] != "m") return;
            if (!int.TryParse(rowArray[2].Trim(), out int monsterId)) return;
            if (!float.TryParse(rowArray[3].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float progressAny))
                return;
            if (!int.TryParse(rowArray[4].Trim(), out int winsNormal)) return;
            SetCardProficiencyWins(monsterId, progressAny, winsNormal);
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
        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
        {
            canonical.SavePlayerData();
            return;
        }

        EnsureMinimumDeckSlotCount();
        EnsureDeckSlotMaps();

        string dir = Application.persistentDataPath;
        string path = GetPlayerDataPath();
        Directory.CreateDirectory(dir);

        // PlayerProfileCsvService syncs W/L/D/Q and per-match battle_record rows into playerdata.
        // Those rows are not represented in Player fields; without preserving them here,
        // the next save would strip battle stats / difficulty breakdown.
        var preservedActiveProfileRows = new List<string>(64);
        if (PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] existingLines, out _))
        {
            for (int li = 0; li < existingLines.Length; li++)
            {
                string line = existingLines[li];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] cols = line.Split(',');
                if (cols.Length < 4) continue;
                if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(cols[1].Trim(), out int slotNum) || slotNum != activePlayerSlot) continue;
                string slotKey = cols[2].Trim();
                if (slotKey.StartsWith("profile_", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(slotKey, "battle_record", StringComparison.OrdinalIgnoreCase))
                    preservedActiveProfileRows.Add(line);
            }
        }

        var datas = new List<string>();
        datas.Add($"active_slot,{activePlayerSlot}");

        // Preserve all rows for non-active slots.
        for (int i = 0; i < cachedOtherSlotRows.Count; i++)
            datas.Add(cachedOtherSlotRows[i]);

        var current = new List<string>();
        current.Add($"slot,{activePlayerSlot},coins,{playerCoins}");
        current.Add($"slot,{activePlayerSlot},selected_deck_slot,{selectedDeckSlot}");
        current.Add($"slot,{activePlayerSlot},slot_name,{SanitizeSlotName(activePlayerSlotName, activePlayerSlot)}");

        EnsureDeckSlotMaps();
        EnsureDeckSlotNamesBuffer();
        for (int s = 0; s < deckSlotCount; s++)
        {
            string label = SanitizeDeckSlotName(GetDeckSlotDisplayName(s));
            current.Add($"slot,{activePlayerSlot},deck_slot_name,{s},{label}");
        }

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

        foreach (var kv in cardProficiencyWins)
        {
            if (kv.Value.progressAny <= 0.001f && kv.Value.winsNormalDifficulty <= 0) continue;
            string progressText = kv.Value.progressAny.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            current.Add($"slot,{activePlayerSlot},proficiency,m,{kv.Key},{progressText},{kv.Value.winsNormalDifficulty}");
        }

        for (int i = 0; i < current.Count; i++) datas.Add(current[i]);
        EnsureAllSlotContainers(datas);
        for (int pi = 0; pi < preservedActiveProfileRows.Count; pi++)
            datas.Add(preservedActiveProfileRows[pi]);

        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(path, datas);
        RebuildCachedOtherSlotRowsFromDisk(path);
        Debug.Log("Save path: " + path);
    }

    private void RebuildCachedOtherSlotRowsFromDisk(string path)
    {
        cachedOtherSlotRows.Clear();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row) || row.StartsWith("#", StringComparison.Ordinal)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slotIndex)) continue;
            if (slotIndex == activePlayerSlot) continue;
            cachedOtherSlotRows.Add(row);
        }
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
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return false;
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
        string[] existing = PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] read, out _)
            ? read
            : Array.Empty<string>();
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
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(path, rows);
    }

    public static int FindFirstEmptySlot()
    {
        string path = GetPlayerDataPath();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return 1;
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
        string[] existing = PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] read, out _)
            ? read
            : Array.Empty<string>();
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
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(path, rows);
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
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
        {
            return BuildSnapshots(hasData, coins, names);
        }
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
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return summary;
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
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return "玩家1";
        int active = Mathf.Clamp(ReadActiveSlotFromRows(rows), 1, MaxPlayerSlots);
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
        string[] existing = PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] read, out _)
            ? read
            : Array.Empty<string>();
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
        PlayerPersistSafeIO.WriteAllLinesWithAtomicRotateBackups(path, rows);
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
        EnsureMinimumDeckSlotCount();
        EnsureDeckSlotMaps();
        selectedDeckSlot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
    }

    private void EnsureDeckSlotNamesBuffer()
    {
        EnsureDeckSlotMaps();
        if (deckSlotDisplayNames != null && deckSlotDisplayNames.Length == deckSlotCount)
            return;
        var prev = deckSlotDisplayNames;
        deckSlotDisplayNames = new string[deckSlotCount];
        if (prev != null)
        {
            int copy = Mathf.Min(prev.Length, deckSlotCount);
            for (int i = 0; i < copy; i++)
                deckSlotDisplayNames[i] = prev[i];
        }
    }

    /// <summary>Localized deck tab label for slot index 0..deckSlotCount-1.</summary>
    public string GetDeckSlotDisplayName(int slot)
    {
        EnsureDeckSlotMaps();
        EnsureDeckSlotNamesBuffer();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        string s = deckSlotDisplayNames[slot];
        return string.IsNullOrWhiteSpace(s) ? ("牌組" + (slot + 1)) : s;
    }

    public void SetDeckSlotDisplayName(int slot, string name)
    {
        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
        {
            canonical.SetDeckSlotDisplayName(slot, name);
            return;
        }

        EnsureMinimumDeckSlotCount();
        EnsureDeckSlotMaps();
        EnsureDeckSlotNamesBuffer();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        deckSlotDisplayNames[slot] = SanitizeDeckSlotName(name);
    }

    private static string SanitizeDeckSlotName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        string n = name.Trim().Replace("\n", " ").Replace("\r", " ").Replace(",", " ");
        if (n.Length > 24) n = n.Substring(0, 24);
        return n;
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

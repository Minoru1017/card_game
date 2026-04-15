using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;

public class PlayerData : MonoBehaviour
{
    public CardStore CardStore;
    public int playerCoins;
    /// <summary>Owned cards: key = runtime id (monster ≥0, spell &lt;0 via <see cref="DeckCardId"/>).</summary>
    public readonly Dictionary<int, int> playerCollection = new Dictionary<int, int>();
    public int deckSlotCount = 3;
    public int selectedDeckSlot = 0;
    public int totalCoins;

    private Dictionary<int, int>[] deckSlotMaps;

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
            coinsText.text = playerCoins.ToString();
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

        string path = Path.Combine(Application.persistentDataPath, "playerdata.csv");

        if (!File.Exists(path))
        {
            playerCoins = 100;
            totalCoins = playerCoins;
            SavePlayerData();
            return;
        }

        string[] dataRow = File.ReadAllLines(path);
        Debug.Log("Load from persistent: " + path);
        bool hasDeckSlotData = false;

        foreach (var row in dataRow)
        {
            string[] rowArray = row.Split(',');
            if (rowArray == null || rowArray.Length == 0) continue;
            if (rowArray[0] == "#") continue;

            if (rowArray[0] == "coins")
            {
                playerCoins = int.Parse(rowArray[1].Trim());
                totalCoins = playerCoins;
            }
            else if (rowArray[0] == "card")
            {
                if (!TryParseCollectionRow(rowArray, out int key, out int num)) continue;
                SetCollectionCount(key, num);
            }
            else if (rowArray[0] == "deck")
            {
                if (hasDeckSlotData) continue;
                if (!TryParseDeckRow(rowArray, out int key, out int num)) continue;
                SetDeckCount(0, key, num);
            }
            else if (rowArray[0] == "deckslot")
            {
                if (rowArray.Length < 4) continue;
                int slot = int.Parse(rowArray[1].Trim());
                if (slot < 0 || slot >= deckSlotCount) continue;

                if (rowArray.Length >= 5 && (rowArray[2] == "m" || rowArray[2] == "s"))
                {
                    hasDeckSlotData = true;
                    if (!TryParseTypedDeckslotRow(rowArray, out int key, out int num)) continue;
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
                if (rowArray.Length < 2) continue;
                selectedDeckSlot = Mathf.Clamp(int.Parse(rowArray[1].Trim()), 0, deckSlotCount - 1);
            }
        }

        Debug.Log("Loaded coins=" + playerCoins);
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
        string path = Path.Combine(dir, "playerdata.csv");
        Directory.CreateDirectory(dir);

        var datas = new List<string>();
        datas.Add($"coins,{playerCoins}");
        datas.Add($"selected_deck_slot,{selectedDeckSlot}");

        foreach (var kv in playerCollection)
        {
            if (kv.Value == 0) continue;
            if (DeckCardId.IsSpellKey(kv.Key))
                datas.Add($"card,s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
            else
                datas.Add($"card,m,{kv.Key},{kv.Value}");
        }

        EnsureDeckSlotMaps();
        for (int slot = 0; slot < deckSlotMaps.Length; slot++)
        {
            foreach (var kv in deckSlotMaps[slot])
            {
                if (kv.Value == 0) continue;
                if (DeckCardId.IsSpellKey(kv.Key))
                    datas.Add($"deckslot,{slot},s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
                else
                    datas.Add($"deckslot,{slot},m,{kv.Key},{kv.Value}");
            }
        }

        foreach (var kv in deckSlotMaps[selectedDeckSlot])
        {
            if (kv.Value == 0) continue;
            if (DeckCardId.IsSpellKey(kv.Key))
                datas.Add($"deck,s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
            else
                datas.Add($"deck,m,{kv.Key},{kv.Value}");
        }

        File.WriteAllLines(path, datas);
        Debug.Log("Save path: " + path);
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

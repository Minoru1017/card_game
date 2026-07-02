using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using System;

public partial class PlayerData : MonoBehaviour
{
    public const int MaxPlayerSlots = 3;
    /// <summary>Buildbeck UI 固定 5 個牌組分頁；須與場景按鈕數一致。</summary>
    public const int MinDeckSlotCount = 5;
    public CardStore CardStore;
    public int playerCoins;
    /// <summary>寶石：CD 光碟抽選專用（與金幣分池）。新檔預設 300。</summary>
    public int playerGems;
    /// <summary>Owned cards: key = runtime id (monster ≥0, spell &lt;0 via <see cref="DeckCardId"/>).</summary>
    public readonly Dictionary<int, int> playerCollection = new Dictionary<int, int>();
    public int deckSlotCount = 5;
    public int selectedDeckSlot = 0;
    public int totalCoins;
    [Range(1, MaxPlayerSlots)] public int activePlayerSlot = 1;
    public string activePlayerSlotName = "玩家1";

    private Dictionary<int, int>[] deckSlotMaps;
    /// <summary>Per deck-slot custom names; empty → UI fallback 「牌組N」（見 <see cref="PlayerDeckSlotNameStorage"/>）。</summary>
    private string[] deckSlotDisplayNames;
    /// <summary>怪物牌熟練度勝場（key = monster id）。</summary>
    private readonly Dictionary<int, CardProficiencyWins> cardProficiencyWins = new Dictionary<int, CardProficiencyWins>();
    private readonly List<string> cachedOtherSlotRows = new List<string>(128);
    private bool saveHydratedFromDisk;

    [Header("UI")]
    public TextMeshProUGUI coinsText;

    /// <summary>
    /// 唯一應讀寫存檔的 <see cref="PlayerData"/>（優先 <c>DataManager</c> 物件上的實例，其次帶 <see cref="DeckManager"/> 者）。
    /// 全專案請用此方法，勿再 <c>FindFirstObjectByType&lt;PlayerData&gt;</c>。
    /// </summary>
    public static PlayerData ResolveCanonical()
    {
        PlayerData[] all = UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        PlayerData onDataManager = null;
        PlayerData withDeckManager = null;
        PlayerData any = null;
        for (int i = 0; i < all.Length; i++)
        {
            PlayerData p = all[i];
            if (p == null) continue;
            any ??= p;
            if (p.gameObject.name == "DataManager")
                onDataManager = p;
            if (p.GetComponent<DeckManager>() != null)
                withDeckManager = p;
        }

        if (onDataManager != null) return onDataManager;
        if (withDeckManager != null) return withDeckManager;
        return any;
    }

    private const string FallbackHostName = "TutorialPlotPlayerDataHost";
    private static GameObject fallbackHost;

    /// <summary>已成功自 CSV 載入金幣／寶石等欄位；未 hydrate 前不得整檔 Save。</summary>
    public bool IsSaveHydratedFromDisk => saveHydratedFromDisk;

    /// <summary>
    /// 需寫入存檔時取得可寫入的 <see cref="PlayerData"/>。
    /// 大廳／劇情等場景可能沒有 DataManager，此時建立常駐 fallback 並自 CSV 載入。
    /// </summary>
    public static PlayerData EnsureWritable()
    {
        PlayerData canonical = ResolveCanonical();
        if (canonical != null)
        {
            if (!canonical.saveHydratedFromDisk)
                canonical.LoadPlayerData();
            return canonical;
        }

        if (fallbackHost == null)
        {
            PlayerData[] all = UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.name == FallbackHostName)
                {
                    fallbackHost = all[i].gameObject;
                    break;
                }
            }
        }

        if (fallbackHost == null)
        {
            fallbackHost = new GameObject(FallbackHostName);
            UnityEngine.Object.DontDestroyOnLoad(fallbackHost);
        }

        PlayerData pd = fallbackHost.GetComponent<PlayerData>();
        if (pd == null)
            pd = fallbackHost.AddComponent<PlayerData>();

        if (!pd.saveHydratedFromDisk)
            pd.LoadPlayerData();
        return pd;
    }

    /// <summary>取消延遲存檔並寫入；不會在已 hydrate 的記憶體上重載 CSV。</summary>
    public static PlayerData ResolveForSaveWrite() => EnsureWritable();

    void Awake()
    {
        if (CardStore != null) CardStore.LoadCardData();
        if (ResolveCanonical() == this)
        {
            EnsureMinimumDeckSlotCount();
            LoadPlayerData();
            RefreshCoins();
            RefreshGems();
        }
    }

    /// <summary>避免 prefab 上 deckSlotCount=3 導致第 4、5 槽名稱與牌組被 clamp 到槽位 2。</summary>
    public void EnsureMinimumDeckSlotCount()
    {
        if (deckSlotCount < MinDeckSlotCount)
            deckSlotCount = MinDeckSlotCount;
    }

    /// <summary>手機切背景／來電、PC 失焦時：將延遲存檔與貴重品變更落盤。</summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || ResolveCanonical() != this)
            return;

        PlayerSaveCoordinator.FlushPendingPlayerDataIfNeeded();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus || ResolveCanonical() != this)
            return;

        PlayerSaveCoordinator.FlushPendingPlayerDataIfNeeded();
    }

    private void OnApplicationQuit()
    {
        if (ResolveCanonical() != this)
            return;

        PlayerSaveCoordinator.FlushPendingPlayerDataIfNeeded();
    }

    public void RefreshCoins()
    {
        if (coinsText != null)
            coinsText.text = GetCoinsDisplayText();
    }

    public void RefreshGems()
    {
        // 部分場景共用 coinsText；有獨立 gems 欄位時可再擴充 SerializeField。
    }

    public string GetGemsDisplayText() => playerGems.ToString();

    public bool TrySpendGems(int amount)
    {
        if (amount <= 0) return true;
        if (playerGems < amount) return false;
        playerGems -= amount;
        RefreshGems();
        return true;
    }

    public void AddGems(int amount)
    {
        if (amount <= 0) return;
        playerGems += amount;
        RefreshGems();
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

    public void ClearDeckSlot(int slot)
    {
        EnsureDeckSlotMaps();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        deckSlotMaps[slot].Clear();
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

    public IEnumerable<KeyValuePair<int, CardProficiencyWins>> GetAllCardProficiencyWinsSnapshot() =>
        cardProficiencyWins;

    public void RemoveCardProficiencyWins(int monsterId) => cardProficiencyWins.Remove(monsterId);

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

    /// <summary>
    /// 測試用：移除所有非御三家（國王／皇后／民兵）的熟練度存檔列；御三家維持不寫入或保留原狀，
    /// 遊戲內仍依 <see cref="CardSkillProficiencyService.IsStarterTrio"/> 視為完整解放。
    /// </summary>
    /// <returns>移除的怪物 id 筆數</returns>
    public int ResetAllCardProficiencyForTesting(bool saveAfter = true)
    {
        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
            return canonical.ResetAllCardProficiencyForTesting(saveAfter);

        CardStore store = CardStore != null ? CardStore : GetComponent<CardStore>();
        int removed = CardProficiencyDebugReset.ClearRuntimeProficiency(this, store);
        CardProficiencyDebugReset.StripAllNonStarterProficiencyRows(CardProficiencyDebugReset.GetPersistentPlayerDataCsvPath());

        if (saveAfter)
            SavePlayerData();

        return removed;
    }

    /// <summary>對戰結算：累加 toward-B 進度；普通以上難度勝利時 winsNormal +1。</summary>
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

    public static string GetPlayerSaveCsvPath() => GetPlayerDataPath();

    private static string GetPlayerDataPath()
    {
        return Path.Combine(Application.persistentDataPath, "playerdata.csv");
    }

    /// <summary>從 playerdata.csv 讀取目前作用中槽位（不依賴場景內 PlayerData 元件）。</summary>
    public static int ReadActivePlayerSlotFromSave()
    {
        string path = GetPlayerDataPath();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return 1;
        return Mathf.Clamp(ReadActiveSlotFromRows(rows), 1, MaxPlayerSlots);
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
        PlayerSaveCoordinator.EnsurePersistedBeforeDiskMerge();
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
        PlayerSaveCoordinator.WritePlayerDataCsv(rows);
        ValuablesVaultState.InvalidateAllCaches();

        PlayerData pd = ResolveCanonical();
        if (pd != null)
            pd.LoadPlayerData();
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
        PlayerSaveCoordinator.EnsurePersistedBeforeDiskMerge();
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
        PlayerSaveCoordinator.WritePlayerDataCsv(rows);
        ValuablesVaultState.InvalidateSlotCache(slot);
        TutorialProgressState.ResetTutorialForSlot(slot);
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

    public static int GetActivePlayerSlotOrDefault()
    {
        int slot = ReadActivePlayerSlotFromSave();
        PlayerData pd = ResolveCanonical();
        if (pd != null)
        {
            if (pd.IsSaveHydratedFromDisk && pd.activePlayerSlot != slot)
                pd.LoadPlayerData();
            else
                pd.activePlayerSlot = slot;
        }
        return slot;
    }

    public static string GetActivePlayerSlotName()
    {
        PlayerData pd = ResolveCanonical();
        if (pd != null && pd.IsSaveHydratedFromDisk)
        {
            int slot = Mathf.Clamp(pd.activePlayerSlot, 1, MaxPlayerSlots);
            return SanitizeSlotName(pd.activePlayerSlotName, slot);
        }

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
        int active = GetActivePlayerSlotOrDefault();
        string safeName = SanitizeSlotName(name, active);

        PlayerData pd = ResolveForSaveWrite();
        pd.activePlayerSlot = Mathf.Clamp(active, 1, MaxPlayerSlots);
        pd.activePlayerSlotName = safeName;
        pd.SavePlayerData();
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

    public int GetSelectedDeckTotalCount()
    {
        EnsureDeckSlotMaps();
        int slot = Mathf.Clamp(selectedDeckSlot, 0, deckSlotCount - 1);
        return GetDeckSlotTotalCount(slot);
    }

    public int GetDeckSlotTotalCount(int slot)
    {
        EnsureDeckSlotMaps();
        slot = Mathf.Clamp(slot, 0, deckSlotCount - 1);
        int total = 0;
        foreach (var kv in deckSlotMaps[slot])
        {
            if (kv.Value > 0) total += kv.Value;
        }
        return total;
    }

    // ── 牌組槽顯示名稱（<see cref="PlayerDeckSlotNameStorage"/>）────────────────

    internal string[] GetDeckSlotDisplayNamesInternal() => deckSlotDisplayNames;

    internal void SetDeckSlotDisplayNamesInternal(string[] names) => deckSlotDisplayNames = names;

    internal bool IsSaveHydratedInternal() => saveHydratedFromDisk;

    internal void EnsureDeckSlotMapsInternal() => EnsureDeckSlotMaps();

    private void ResetDeckSlotDisplayNamesForLoad() => PlayerDeckSlotNameStorage.ClearForLoad(this);

    public void EnsureActivePlayerSlotSyncedFromDisk() =>
        PlayerDeckSlotNameStorage.EnsureActivePlayerSlotSyncedFromDisk(this);

    public string GetDeckSlotDisplayNameRaw(int slot) =>
        PlayerDeckSlotNameStorage.GetRawName(this, slot);

    public string GetDeckSlotDisplayName(int slot) =>
        PlayerDeckSlotNameStorage.GetDisplayName(this, slot);

    public void SetDeckSlotDisplayName(int slot, string name) =>
        PlayerDeckSlotNameStorage.SetCustomName(this, slot, name);

    private void ApplyDeckSlotNameLoadRow(string[] rowArray) =>
        PlayerDeckSlotNameStorage.ApplyLoadRow(this, rowArray);

    private void AppendDeckSlotNameSaveRows(List<string> current) =>
        PlayerDeckSlotNameStorage.AppendSaveRows(this, current);
}

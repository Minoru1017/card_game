using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>每槽 CD 光碟持有、碎片與 SR 保底計數（playerdata.csv）。</summary>
public static class PlayerBirdDuelCdState
{
    private sealed class SlotState
    {
        public readonly HashSet<string> Owned = new HashSet<string>();
        public readonly Dictionary<string, int> Fragments = new Dictionary<string, int>();
        public int SrPityCounter;
    }

    private static readonly Dictionary<int, SlotState> Slots = new Dictionary<int, SlotState>();

    public static void ClearAllCaches() => Slots.Clear();

    private static SlotState GetOrCreate(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!Slots.TryGetValue(slot, out SlotState state))
        {
            state = new SlotState();
            Slots[slot] = state;
        }
        return state;
    }

    public static void ParseScopedRow(int slot, string[] rowArray)
    {
        if (rowArray == null || rowArray.Length < 2) return;
        if (!string.Equals(rowArray[0], "bird_cd", System.StringComparison.OrdinalIgnoreCase)) return;

        SlotState state = GetOrCreate(slot);
        string kind = rowArray[1].Trim();
        if (string.Equals(kind, "own", System.StringComparison.OrdinalIgnoreCase))
        {
            if (rowArray.Length < 3) return;
            string cdId = rowArray[2].Trim();
            if (!string.IsNullOrWhiteSpace(cdId))
                state.Owned.Add(cdId);
            return;
        }

        if (string.Equals(kind, "frag", System.StringComparison.OrdinalIgnoreCase))
        {
            if (rowArray.Length < 4) return;
            string cdId = rowArray[2].Trim();
            if (string.IsNullOrWhiteSpace(cdId)) return;
            if (!int.TryParse(rowArray[3].Trim(), out int count)) return;
            state.Fragments[cdId] = Mathf.Max(0, count);
            return;
        }

        if (string.Equals(kind, "pity_sr", System.StringComparison.OrdinalIgnoreCase))
        {
            if (rowArray.Length < 3) return;
            if (int.TryParse(rowArray[2].Trim(), out int counter))
                state.SrPityCounter = Mathf.Max(0, counter);
        }
    }

    public static void AppendSaveRows(int slot, List<string> rows)
    {
        if (rows == null) return;
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!Slots.TryGetValue(slot, out SlotState state))
            return;

        foreach (string cdId in state.Owned)
        {
            if (string.IsNullOrWhiteSpace(cdId)) continue;
            rows.Add($"slot,{slot},bird_cd,own,{cdId}");
        }

        foreach (KeyValuePair<string, int> pair in state.Fragments)
        {
            if (pair.Value <= 0 || string.IsNullOrWhiteSpace(pair.Key)) continue;
            rows.Add($"slot,{slot},bird_cd,frag,{pair.Key},{pair.Value}");
        }

        if (state.SrPityCounter > 0)
            rows.Add($"slot,{slot},bird_cd,pity_sr,{state.SrPityCounter}");
    }

    public static void EnsureNewPlayerDefaults(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        if (!OwnsCd(slot, BirdDuelCdCatalog.DefaultCdId))
            GrantCd(slot, BirdDuelCdCatalog.DefaultCdId);
    }

    public static bool OwnsCd(int slot, string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return false;
        return GetOrCreate(slot).Owned.Contains(cdId.Trim());
    }

    public static bool OwnsCdForActivePlayer(string cdId) =>
        OwnsCd(PlayerData.GetActivePlayerSlotOrDefault(), cdId);

    public static void GrantCd(int slot, string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return;
        cdId = cdId.Trim();
        SlotState state = GetOrCreate(slot);
        if (state.Owned.Contains(cdId))
            return;

        state.Owned.Add(cdId);
        ValuablesVaultCatalog.TryAutoPlaceOwnedCdDisc(slot, cdId);
    }

    /// <summary>丟棄貴重品庫中的整張 CD 光碟時，一併取消解鎖。</summary>
    public static bool RevokeCd(int slot, string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return false;
        return GetOrCreate(slot).Owned.Remove(cdId.Trim());
    }

    public static void AddFragments(int slot, string cdId, int amount)
    {
        if (string.IsNullOrWhiteSpace(cdId) || amount <= 0) return;
        SlotState state = GetOrCreate(slot);
        cdId = cdId.Trim();
        state.Fragments.TryGetValue(cdId, out int current);
        state.Fragments[cdId] = current + amount;
    }

    public static int GetFragments(int slot, string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return 0;
        return GetOrCreate(slot).Fragments.TryGetValue(cdId.Trim(), out int count) ? count : 0;
    }

    public static bool TrySpendFragments(int slot, string cdId, int amount)
    {
        if (string.IsNullOrWhiteSpace(cdId) || amount <= 0) return false;
        cdId = cdId.Trim();
        SlotState state = GetOrCreate(slot);
        if (!state.Fragments.TryGetValue(cdId, out int current) || current < amount)
            return false;

        int next = current - amount;
        if (next <= 0)
            state.Fragments.Remove(cdId);
        else
            state.Fragments[cdId] = next;
        return true;
    }

    public static List<string> GetWalletFragmentCdIdsSorted(int slot)
    {
        SlotState state = GetOrCreate(slot);
        var list = new List<string>();
        foreach (KeyValuePair<string, int> pair in state.Fragments)
        {
            if (pair.Value <= 0 || string.IsNullOrWhiteSpace(pair.Key)) continue;
            if (ValuablesVaultCatalog.ResolveCdFragmentDefinitionId(pair.Key) <= 0) continue;
            list.Add(pair.Key);
        }

        list.Sort(System.StringComparer.Ordinal);
        return list;
    }

    public static int GetSrPityCounter(int slot) => GetOrCreate(slot).SrPityCounter;

    public static void SetSrPityCounter(int slot, int value) =>
        GetOrCreate(slot).SrPityCounter = Mathf.Max(0, value);

    public static List<string> GetOwnedCdIdsSorted(int slot)
    {
        SlotState state = GetOrCreate(slot);
        var list = new List<string>(state.Owned);
        list.Sort(System.StringComparer.Ordinal);
        if (list.Count == 0)
            list.Add(BirdDuelCdCatalog.DefaultCdId);
        return list;
    }

    public static string BuildOwnedSummary(int slot)
    {
        List<string> ids = GetOwnedCdIdsSorted(slot);
        if (ids.Count == 0) return "（無）";
        var sb = new StringBuilder();
        for (int i = 0; i < ids.Count; i++)
        {
            if (i > 0) sb.Append('、');
            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(ids[i]);
            sb.Append(profile != null ? profile.DisplayName : ids[i]);
        }
        return sb.ToString();
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>貴重品庫：每角色槽 4×6（24 格），存於 playerdata.csv（經 <see cref="PlayerSaveCoordinator"/> 與 <see cref="PlayerData.SavePlayerData"/> 寫入）。</summary>
public static class ValuablesVaultState
{
    public const int GridWidth = 4;
    public const int GridHeight = 6;
    public const int SlotCount = GridWidth * GridHeight;

    public const string SaveKey = "valuable";

    private static readonly Dictionary<int, Dictionary<int, VaultStack>> SlotCaches =
        new Dictionary<int, Dictionary<int, VaultStack>>();

    private static readonly HashSet<int> LoadedSlots = new HashSet<int>();

    private static bool anySlotDirty;

    public static bool HasPendingChanges => anySlotDirty;

    public readonly struct VaultStack
    {
        public readonly int DefinitionId;
        public readonly int Quantity;

        public VaultStack(int definitionId, int quantity)
        {
            DefinitionId = definitionId;
            Quantity = Mathf.Max(0, quantity);
        }

        public bool IsEmpty => DefinitionId <= 0 || Quantity <= 0;
    }

    public static void InvalidateAllCaches()
    {
        if (anySlotDirty)
        {
            PlayerData pd = PlayerData.ResolveCanonical();
            if (pd != null && pd.IsSaveHydratedFromDisk)
                pd.SavePlayerData();
            else
                Debug.LogWarning(
                    "ValuablesVaultState: vault changes pending but PlayerData not hydrated; " +
                    "skipping full save to avoid overwriting coins/gems with defaults.");
        }

        SlotCaches.Clear();
        LoadedSlots.Clear();
        anySlotDirty = false;
    }

    public static void InvalidateSlotCache(int playerSlot)
    {
        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        SlotCaches.Remove(playerSlot);
        LoadedSlots.Remove(playerSlot);
    }

    public static bool IsValuableCsvRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
            return false;

        string[] cols = row.Split(',');
        if (cols.Length < 6)
            return false;
        if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase))
            return false;
        return string.Equals(cols[2].Trim(), SaveKey, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetStack(int playerSlot, int cellIndex, out VaultStack stack)
    {
        stack = default;
        if (!IsValidCellIndex(cellIndex))
            return false;

        IReadOnlyDictionary<int, VaultStack> map = LoadSlotMap(playerSlot);
        if (!map.TryGetValue(cellIndex, out VaultStack found) || found.IsEmpty)
            return false;

        stack = found;
        return true;
    }

    public static void SetStack(int playerSlot, int cellIndex, int definitionId, int quantity = 1)
    {
        if (!IsValidCellIndex(cellIndex))
            return;

        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        definitionId = Mathf.Max(0, definitionId);
        quantity = Mathf.Max(0, quantity);

        EnsureSlotLoaded(playerSlot);
        Dictionary<int, VaultStack> map = SlotCaches[playerSlot];

        if (definitionId <= 0 || quantity <= 0)
        {
            map.Remove(cellIndex);
            return;
        }

        map[cellIndex] = new VaultStack(definitionId, quantity);
        anySlotDirty = true;
    }

    public static void ClearStack(int playerSlot, int cellIndex)
    {
        SetStack(playerSlot, cellIndex, 0, 0);
    }

    public static void ClearAllForSlot(int playerSlot)
    {
        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        EnsureSlotLoaded(playerSlot);
        SlotCaches[playerSlot].Clear();
        anySlotDirty = true;
    }

    public static void MarkPersisted()
    {
        anySlotDirty = false;
    }

    public static IReadOnlyDictionary<int, VaultStack> LoadSlotMap(int playerSlot)
    {
        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        EnsureSlotLoaded(playerSlot);
        return SlotCaches[playerSlot];
    }

    /// <summary>完整存檔時寫入所有角色槽的貴重品列（避免多 writer 覆蓋）。</summary>
    public static void AppendAllSlotsSerializedRows(List<string> datas)
    {
        if (datas == null)
            return;

        for (int slot = 1; slot <= PlayerData.MaxPlayerSlots; slot++)
            AppendSerializedRowsForSlot(datas, slot);
    }

    public static void AppendSerializedRowsForSlot(List<string> datas, int playerSlot)
    {
        if (datas == null)
            return;

        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        EnsureSlotLoaded(playerSlot);

        foreach (KeyValuePair<int, VaultStack> kv in SlotCaches[playerSlot])
        {
            if (kv.Value.IsEmpty)
                continue;
            datas.Add(FormatCellRow(playerSlot, kv.Key, kv.Value.DefinitionId, kv.Value.Quantity));
        }
    }

    public static int CellIndexFromGrid(int column, int row) => row * GridWidth + column;

    public static void GridFromCellIndex(int cellIndex, out int column, out int row)
    {
        column = cellIndex % GridWidth;
        row = cellIndex / GridWidth;
    }

    public static bool IsValidCellIndex(int cellIndex) =>
        cellIndex >= 0 && cellIndex < SlotCount;

    private static void EnsureSlotLoaded(int playerSlot)
    {
        if (LoadedSlots.Contains(playerSlot))
            return;

        var map = new Dictionary<int, VaultStack>(SlotCount);
        if (TryLoadSaveLines(out string[] rows))
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (!TryParseCellRow(rows[i], playerSlot, out int cellIndex, out int definitionId, out int quantity))
                    continue;
                if (definitionId <= 0 || quantity <= 0)
                    continue;
                map[cellIndex] = new VaultStack(definitionId, quantity);
            }
        }

        SlotCaches[playerSlot] = map;
        LoadedSlots.Add(playerSlot);
    }

    public static bool TryParseCellRow(string row, int playerSlot, out int cellIndex, out int definitionId, out int quantity)
    {
        cellIndex = 0;
        definitionId = 0;
        quantity = 0;
        if (string.IsNullOrWhiteSpace(row))
            return false;

        string[] cols = row.Split(',');
        if (cols.Length < 6)
            return false;
        if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase))
            return false;
        if (!int.TryParse(cols[1].Trim(), out int rowSlot) || rowSlot != playerSlot)
            return false;
        if (!string.Equals(cols[2].Trim(), SaveKey, System.StringComparison.OrdinalIgnoreCase))
            return false;
        if (!int.TryParse(cols[3].Trim(), out cellIndex))
            return false;
        if (!int.TryParse(cols[4].Trim(), out definitionId))
            return false;
        if (!int.TryParse(cols[5].Trim(), out quantity))
            quantity = 1;
        return IsValidCellIndex(cellIndex);
    }

    private static string FormatCellRow(int playerSlot, int cellIndex, int definitionId, int quantity) =>
        $"slot,{playerSlot},{SaveKey},{cellIndex},{definitionId},{quantity}";

    private static bool TryLoadSaveLines(out string[] rows)
    {
        rows = System.Array.Empty<string>();
        return PlayerSaveCoordinator.TryReadPlayerDataLines(out rows, out _);
    }
}

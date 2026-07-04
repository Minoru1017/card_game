using System.Collections.Generic;

/// <summary>貴重品庫物品定義（非卡牌 id 的收藏品，如 CD 光碟／CD 碎片）。</summary>
public static class ValuablesVaultCatalog
{
    public const int CdFragmentDefinitionBase = 900000;
    public const int CdDiscDefinitionBase = 910000;
    public const int KeyItemDefinitionBase = 920000;

    /// <summary>M-1-2 中段拾取的封印法術（LEVEL_DESIGN_M-1-2.md §3.3.3；支線解封前效果未知）。</summary>
    public const int SealedSpellRelicDefinitionId = KeyItemDefinitionBase + 1;

    private static readonly Dictionary<string, int> CdFragmentDefinitionIds =
        new Dictionary<string, int>
        {
            { BirdDuelCdCatalog.DefaultCdId, CdFragmentDefinitionBase + 1 },
            { "court_march", CdFragmentDefinitionBase + 2 },
            { "morning_prayer", CdFragmentDefinitionBase + 3 },
            { "dawn_hymn", CdFragmentDefinitionBase + 4 },
        };

    private static readonly Dictionary<string, int> CdDiscDefinitionIds =
        new Dictionary<string, int>
        {
            { BirdDuelCdCatalog.DefaultCdId, CdDiscDefinitionBase + 1 },
            { "court_march", CdDiscDefinitionBase + 2 },
            { "morning_prayer", CdDiscDefinitionBase + 3 },
            { "dawn_hymn", CdDiscDefinitionBase + 4 },
        };

    private static readonly Dictionary<int, string> CdFragmentDefinitionToCdId =
        new Dictionary<int, string>();

    private static readonly Dictionary<int, string> CdDiscDefinitionToCdId =
        new Dictionary<int, string>();

    static ValuablesVaultCatalog()
    {
        foreach (KeyValuePair<string, int> pair in CdFragmentDefinitionIds)
            CdFragmentDefinitionToCdId[pair.Value] = pair.Key;
        foreach (KeyValuePair<string, int> pair in CdDiscDefinitionIds)
            CdDiscDefinitionToCdId[pair.Value] = pair.Key;
    }

    public static bool IsSealedSpellRelicDefinition(int definitionId) =>
        definitionId == SealedSpellRelicDefinitionId;

    /// <summary>關鍵道具（劇情伏筆）：不可丟棄、不換算寶石。</summary>
    public static bool IsKeyItemDefinition(int definitionId) =>
        IsSealedSpellRelicDefinition(definitionId);

    public static bool IsCdFragmentDefinition(int definitionId) =>
        CdFragmentDefinitionToCdId.ContainsKey(definitionId);

    public static bool IsCdDiscDefinition(int definitionId) =>
        CdDiscDefinitionToCdId.ContainsKey(definitionId);

    public static int ResolveCdFragmentDefinitionId(string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return 0;
        return CdFragmentDefinitionIds.TryGetValue(cdId.Trim(), out int id) ? id : 0;
    }

    public static int ResolveCdDiscDefinitionId(string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return 0;
        return CdDiscDefinitionIds.TryGetValue(cdId.Trim(), out int id) ? id : 0;
    }

    public static bool TryResolveCdIdFromFragmentDefinition(int definitionId, out string cdId)
    {
        cdId = null;
        if (!CdFragmentDefinitionToCdId.TryGetValue(definitionId, out cdId))
            return false;
        return !string.IsNullOrWhiteSpace(cdId);
    }

    public static bool TryResolveCdIdFromDiscDefinition(int definitionId, out string cdId)
    {
        cdId = null;
        if (!CdDiscDefinitionToCdId.TryGetValue(definitionId, out cdId))
            return false;
        return !string.IsNullOrWhiteSpace(cdId);
    }

    /// <summary>新解鎖 CD 放入貴重品庫第一個空格（已展示者跳過）。</summary>
    public static bool TryAutoPlaceOwnedCdDisc(int playerSlot, string cdId)
    {
        int definitionId = ResolveCdDiscDefinitionId(cdId);
        if (definitionId <= 0)
            return false;

        playerSlot = UnityEngine.Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        if (IsCdDiscInVault(playerSlot, definitionId))
            return false;

        if (!TryFindFirstEmptyVaultCell(playerSlot, out int cellIndex))
            return false;

        ValuablesVaultState.SetStack(playerSlot, cellIndex, definitionId, 1);
        return true;
    }

    /// <summary>開啟貴重品庫或讀檔後：補齊已解鎖但尚未入庫展示的 CD 光碟。</summary>
    public static void SyncOwnedCdsToVault(int playerSlot)
    {
        playerSlot = UnityEngine.Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        List<string> ownedIds = PlayerBirdDuelCdState.GetOwnedCdIdsSorted(playerSlot);
        for (int i = 0; i < ownedIds.Count; i++)
            TryAutoPlaceOwnedCdDisc(playerSlot, ownedIds[i]);
    }

    /// <summary>M-1-2 中段已拾取封印法術但尚未入庫時，放入第一個空格。</summary>
    public static bool TrySyncSealedSpellRelicToVault(int playerSlot)
    {
        playerSlot = UnityEngine.Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        if (!M12SeawallPatrolProgressState.IsSealedSpellFound(playerSlot))
            return false;
        if (IsDefinitionInVault(playerSlot, SealedSpellRelicDefinitionId))
            return false;
        if (!TryFindFirstEmptyVaultCell(playerSlot, out int cellIndex))
            return false;

        ValuablesVaultState.SetStack(playerSlot, cellIndex, SealedSpellRelicDefinitionId, 1);
        return true;
    }

    private static bool IsCdDiscInVault(int playerSlot, int discDefinitionId) =>
        IsDefinitionInVault(playerSlot, discDefinitionId);

    private static bool IsDefinitionInVault(int playerSlot, int definitionId)
    {
        for (int cell = 0; cell < ValuablesVaultState.SlotCount; cell++)
        {
            if (!ValuablesVaultState.TryGetStack(playerSlot, cell, out ValuablesVaultState.VaultStack stack))
                continue;
            if (stack.DefinitionId == definitionId)
                return true;
        }

        return false;
    }

    private static bool TryFindFirstEmptyVaultCell(int playerSlot, out int cellIndex)
    {
        for (int cell = 0; cell < ValuablesVaultState.SlotCount; cell++)
        {
            if (ValuablesVaultState.TryGetStack(playerSlot, cell, out ValuablesVaultState.VaultStack stack)
                && !stack.IsEmpty)
                continue;

            cellIndex = cell;
            return true;
        }

        cellIndex = -1;
        return false;
    }

    /// <summary>將錢包中的 CD 碎片放入指定格；同格已有相同碎片時堆疊數量。</summary>
    public static bool TryDepositCdFragments(int playerSlot, int cellIndex, string cdId, int quantity)
    {
        if (quantity <= 0 || !ValuablesVaultState.IsValidCellIndex(cellIndex))
            return false;

        int definitionId = ResolveCdFragmentDefinitionId(cdId);
        if (definitionId <= 0)
            return false;

        playerSlot = UnityEngine.Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        cdId = cdId.Trim();

        int walletCount = PlayerBirdDuelCdState.GetFragments(playerSlot, cdId);
        if (walletCount < quantity)
            return false;

        if (ValuablesVaultState.TryGetStack(playerSlot, cellIndex, out ValuablesVaultState.VaultStack existing)
            && !existing.IsEmpty
            && existing.DefinitionId != definitionId)
            return false;

        if (!PlayerBirdDuelCdState.TrySpendFragments(playerSlot, cdId, quantity))
            return false;

        int stacked = quantity;
        if (existing.DefinitionId == definitionId)
            stacked = existing.Quantity + quantity;

        ValuablesVaultState.SetStack(playerSlot, cellIndex, definitionId, stacked);
        return true;
    }
}

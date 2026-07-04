using UnityEngine;

/// <summary>貴重品庫丟棄：依物品類型返還寶石（CD 抽選原貨幣）後清空該格。</summary>
public static class ValuablesVaultDiscard
{
    public readonly struct Result
    {
        public readonly bool Success;
        public readonly string ToastLine;

        public Result(bool success, string toastLine)
        {
            Success = success;
            ToastLine = toastLine ?? string.Empty;
        }
    }

    /// <summary>丟棄此格物品可獲得的寶石（0 表示無法換算）。</summary>
    public static int ResolveGemRefund(int definitionId, int quantity)
    {
        if (definitionId <= 0 || quantity <= 0)
            return 0;

        if (ValuablesVaultCatalog.TryResolveCdIdFromDiscDefinition(definitionId, out string discCdId))
            return ResolveCdDiscGemRefund(discCdId);

        if (ValuablesVaultCatalog.TryResolveCdIdFromFragmentDefinition(definitionId, out string fragCdId))
            return ResolveCdFragmentGemRefund(fragCdId, quantity);

        return 0;
    }

    public static string FormatGemRefundLine(int definitionId, int quantity)
    {
        int gems = ResolveGemRefund(definitionId, quantity);
        return gems > 0 ? "丟棄返還 " + gems + " 寶石" : string.Empty;
    }

    public static Result TryDiscardCell(int playerSlot, int cellIndex)
    {
        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerData.MaxPlayerSlots);
        if (!ValuablesVaultState.IsValidCellIndex(cellIndex))
            return new Result(false, "無效的格位");

        if (!ValuablesVaultState.TryGetStack(playerSlot, cellIndex, out ValuablesVaultState.VaultStack stack)
            || stack.IsEmpty)
            return new Result(false, "此格沒有物品");

        int definitionId = stack.DefinitionId;
        int quantity = stack.Quantity;

        if (!TryApplyRefund(playerSlot, definitionId, quantity, out string toastLine, out string failureReason))
            return new Result(false, failureReason ?? "無法丟棄此物品");

        ValuablesVaultState.ClearStack(playerSlot, cellIndex);
        return new Result(true, toastLine);
    }

    private static bool TryApplyRefund(
        int playerSlot,
        int definitionId,
        int quantity,
        out string toastLine,
        out string failureReason)
    {
        toastLine = string.Empty;
        failureReason = string.Empty;
        if (quantity <= 0)
        {
            failureReason = "無法丟棄此物品";
            return false;
        }

        if (ValuablesVaultCatalog.IsKeyItemDefinition(definitionId))
        {
            failureReason = ValuablesVaultUiCopy.KeyItemCannotDiscard;
            return false;
        }

        int gemRefund = ResolveGemRefund(definitionId, quantity);

        if (ValuablesVaultCatalog.TryResolveCdIdFromDiscDefinition(definitionId, out string discCdId))
        {
            if (gemRefund <= 0)
            {
                failureReason = "無法換算此 CD 的寶石返還";
                return false;
            }

            PlayerData pd = PlayerData.EnsureWritable();

            // 貴重品庫格子與 bird_cd 解鎖可能短暫不同步；仍允許丟棄並返還寶石。
            PlayerBirdDuelCdState.RevokeCd(playerSlot, discCdId);
            pd.AddGems(gemRefund);
            pd.RefreshGems();
            CardStoreGachaLayoutUi.RefreshCurrencyLabels();

            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(discCdId);
            string name = profile != null ? profile.DisplayName : discCdId;
            toastLine = "已丟棄 · " + name + " · 獲得 " + gemRefund + " 寶石";
            return true;
        }

        if (ValuablesVaultCatalog.TryResolveCdIdFromFragmentDefinition(definitionId, out string cdId))
        {
            if (gemRefund <= 0)
            {
                failureReason = "無法換算此碎片的寶石返還";
                return false;
            }

            PlayerData pd = PlayerData.EnsureWritable();
            pd.AddGems(gemRefund);
            pd.RefreshGems();
            CardStoreGachaLayoutUi.RefreshCurrencyLabels();

            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
            string name = profile != null ? profile.DisplayName : cdId;
            string qty = quantity > 1 ? " x" + quantity : string.Empty;
            toastLine = "已丟棄 · " + name + " 碎片" + qty + " · 獲得 " + gemRefund + " 寶石";
            return true;
        }

        Card card = ResolveCard(definitionId);
        if (card != null)
        {
            PlayerData pd = PlayerData.EnsureWritable();
            pd.AddCollection(definitionId, quantity);
            string name = string.IsNullOrWhiteSpace(card.cardName) ? "卡牌" : card.cardName.Trim();
            toastLine = quantity > 1
                ? "已丟棄 · " + name + " x" + quantity + " 返還至收藏"
                : "已丟棄 · " + name + " 返還至收藏";
            return true;
        }

        if (gemRefund > 0)
        {
            failureReason = "無法丟棄此物品";
            return false;
        }

        toastLine = "已丟棄";
        return true;
    }

    private static int ResolveCdDiscGemRefund(string cdId)
    {
        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        if (profile == null)
            return BirdDuelCdGachaService.SinglePullCost;
        return ResolveGemRefundForRarity(profile.Rarity);
    }

    private static int ResolveCdFragmentGemRefund(string cdId, int quantity)
    {
        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        BirdDuelCdRarity rarity = profile != null ? profile.Rarity : BirdDuelCdRarity.N;
        int perDuplicate = ResolveGemRefundForRarity(rarity);
        int fragsPerDuplicate = BirdDuelCdCatalog.DuplicateFragmentAmount(rarity);
        if (fragsPerDuplicate <= 0)
            return 0;
        return Mathf.Max(1, perDuplicate * quantity / fragsPerDuplicate);
    }

    /// <summary>以單抽 80 寶石為 R 基準，依稀有度換算整卡價值。</summary>
    private static int ResolveGemRefundForRarity(BirdDuelCdRarity rarity)
    {
        switch (rarity)
        {
            case BirdDuelCdRarity.SR:
                return Mathf.RoundToInt(BirdDuelCdGachaService.SinglePullCost * 1.5f);
            case BirdDuelCdRarity.R:
                return BirdDuelCdGachaService.SinglePullCost;
            default:
                return Mathf.RoundToInt(BirdDuelCdGachaService.SinglePullCost * 0.6f);
        }
    }

    private static Card ResolveCard(int definitionId)
    {
        PlayerData pd = PlayerData.ResolveCanonical();
        CardStore store = pd != null ? pd.CardStore : null;
        if (store == null)
            store = Object.FindFirstObjectByType<CardStore>();
        return store?.GetCardById(definitionId);
    }
}

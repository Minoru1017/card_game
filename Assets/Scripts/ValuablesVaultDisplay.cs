/// <summary>貴重品庫格子顯示用名稱／圖示解析（definitionId 可對應卡牌 id）。</summary>
public static class ValuablesVaultDisplay
{
    public const string FontGlyphProbe =
        BattleCardTuningPresetDisplay.CjkFontProbe +
        ValuablesVaultUiCopy.FontGlyphProbeExtras;

    public static string ResolveLabel(int definitionId, int quantity)
    {
        if (definitionId <= 0)
            return string.Empty;

        string name = ResolveBaseName(definitionId);
        if (quantity > 1)
            return name + ValuablesVaultUiCopy.FormatQuantitySuffix(quantity);
        return name;
    }

    public static string ResolveCdFragmentWalletLabel(string cdId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(cdId) || quantity <= 0)
            return string.Empty;

        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        string name = profile != null ? profile.DisplayName : cdId;
        return name + " 碎片" + ValuablesVaultUiCopy.FormatQuantitySuffix(quantity);
    }

    public static string ResolveDetailLine(int definitionId, int quantity)
    {
        if (definitionId <= 0)
            return "空欄";

        return ResolveBaseName(definitionId) + "  (#" + definitionId + ")" +
               (quantity > 1 ? "  數量 " + quantity : string.Empty);
    }

    public readonly struct InfoPanelCopy
    {
        public readonly string TitleLine;
        public readonly string Body;
        public readonly string SlotLine;
        public readonly bool HasItem;

        public InfoPanelCopy(string titleLine, string body, string slotLine, bool hasItem)
        {
            TitleLine = titleLine;
            Body = body;
            SlotLine = slotLine;
            HasItem = hasItem;
        }
    }

    public static InfoPanelCopy ResolveInfoPanel(int cellIndex, int definitionId, int quantity)
    {
        if (cellIndex < 0 || !ValuablesVaultState.IsValidCellIndex(cellIndex))
        {
            return new InfoPanelCopy(
                ValuablesVaultUiCopy.NoSelectionTitle,
                ValuablesVaultUiCopy.NoSelectionBody,
                string.Empty,
                false);
        }

        ValuablesVaultState.GridFromCellIndex(cellIndex, out int col, out int row);
        string slotLine = ValuablesVaultUiCopy.FormatSlotLine(row + 1, col + 1);

        if (definitionId <= 0 || quantity <= 0)
        {
            return new InfoPanelCopy(
                ValuablesVaultUiCopy.EmptySlotTitle,
                ValuablesVaultUiCopy.EmptySlotBody,
                slotLine,
                false);
        }

        if (ValuablesVaultCatalog.IsSealedSpellRelicDefinition(definitionId))
        {
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            string sealedBody = ResolveSealedSpellRelicBody(slot);
            return new InfoPanelCopy(
                ValuablesVaultUiCopy.SealedSpellRelicName,
                sealedBody,
                slotLine,
                true);
        }

        if (ValuablesVaultCatalog.TryResolveCdIdFromDiscDefinition(definitionId, out string discCdId))
            return ResolveCdDiscInfoPanel(discCdId, slotLine, definitionId, quantity);

        if (ValuablesVaultCatalog.TryResolveCdIdFromFragmentDefinition(definitionId, out string cdId))
            return ResolveCdFragmentInfoPanel(cdId, quantity, slotLine, definitionId);

        Card card = ResolveCard(definitionId);
        string name = card != null && !string.IsNullOrWhiteSpace(card.cardName)
            ? card.cardName.Trim()
            : "貴重品 #" + definitionId;

        string body = string.Empty;
        if (card != null)
            body = card.rarity + "  稀有度";
        if (quantity > 1)
            body += (body.Length > 0 ? "\n" : string.Empty) + "數量  " + quantity;
        string gemLine = ValuablesVaultDiscard.FormatGemRefundLine(definitionId, quantity);
        if (!string.IsNullOrEmpty(gemLine))
            body += (body.Length > 0 ? "\n" : string.Empty) + gemLine;
        else
            body += (body.Length > 0 ? "\n" : string.Empty) + "丟棄返還至收藏";

        return new InfoPanelCopy(name, body, slotLine, true);
    }

    private static Card ResolveCard(int definitionId)
    {
        PlayerData pd = PlayerData.ResolveCanonical();
        CardStore store = pd != null ? pd.CardStore : null;
        if (store == null)
            store = UnityEngine.Object.FindFirstObjectByType<CardStore>();
        return store?.GetCardById(definitionId);
    }

    public static UnityEngine.Sprite ResolveIcon(int definitionId)
    {
        if (definitionId <= 0)
            return null;

        if (ValuablesVaultCatalog.TryResolveCdIdFromDiscDefinition(definitionId, out string discCdId))
            return ResolveCdDiscIcon(discCdId);

        if (ValuablesVaultCatalog.TryResolveCdIdFromFragmentDefinition(definitionId, out string cdId))
            return ResolveCdFragmentIcon(cdId);

        PlayerData pd = PlayerData.ResolveCanonical();
        CardStore store = pd != null ? pd.CardStore : null;
        if (store == null)
            store = UnityEngine.Object.FindFirstObjectByType<CardStore>();
        if (store == null)
            return null;

        Card card = store.GetCardById(definitionId);
        if (card == null)
            return null;

        UnityEngine.Sprite thumb = card.ResolveDeckThumbSprite();
        return thumb != null ? thumb : card.ResolveCardArtSprite();
    }

    private static string ResolveBaseName(int definitionId)
    {
        if (ValuablesVaultCatalog.IsSealedSpellRelicDefinition(definitionId))
            return ValuablesVaultUiCopy.SealedSpellRelicName;

        if (ValuablesVaultCatalog.TryResolveCdIdFromDiscDefinition(definitionId, out string discCdId))
        {
            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(discCdId);
            if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
                return profile.DisplayName.Trim();
            return discCdId;
        }

        if (ValuablesVaultCatalog.TryResolveCdIdFromFragmentDefinition(definitionId, out string cdId))
        {
            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
            if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
                return profile.DisplayName.Trim() + " 碎片";
            return cdId + " 碎片";
        }

        PlayerData pd = PlayerData.ResolveCanonical();
        CardStore store = pd != null ? pd.CardStore : null;
        if (store == null)
            store = UnityEngine.Object.FindFirstObjectByType<CardStore>();
        if (store != null)
        {
            Card card = store.GetCardById(definitionId);
            if (card != null && !string.IsNullOrWhiteSpace(card.cardName))
                return card.cardName.Trim();
        }

        return "貴重品 #" + definitionId;
    }

    private static string ResolveSealedSpellRelicBody(int slot)
    {
        if (SideQuestA1ProgressState.CanUnsealTideMarkInVault(slot))
            return ValuablesVaultUiCopy.SealedSpellRelicBodyReady;
        if (SideQuestA1ProgressState.IsNodeCleared(slot))
            return ValuablesVaultUiCopy.SealedSpellRelicBody;
        return ValuablesVaultUiCopy.SealedSpellRelicBodyLocked;
    }

    private static InfoPanelCopy ResolveCdDiscInfoPanel(
        string cdId,
        string slotLine,
        int definitionId,
        int quantity)
    {
        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        string name = profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName)
            ? profile.DisplayName.Trim()
            : cdId;

        string body = profile != null
            ? profile.Rarity + "  ·  " + FactionLabel(profile.Faction)
            : string.Empty;
        string gemLine = ValuablesVaultDiscard.FormatGemRefundLine(definitionId, quantity);
        if (!string.IsNullOrEmpty(gemLine))
            body += (body.Length > 0 ? "\n" : string.Empty) + gemLine;

        return new InfoPanelCopy(name, body, slotLine, true);
    }

    private static string FactionLabel(BirdDuelCdFaction faction)
    {
        switch (faction)
        {
            case BirdDuelCdFaction.King: return "國王";
            case BirdDuelCdFaction.Church: return "教會";
            default: return "通用";
        }
    }

    private static UnityEngine.Sprite ResolveCdDiscIcon(string cdId) => ResolveCdFragmentIcon(cdId);

    private static InfoPanelCopy ResolveCdFragmentInfoPanel(
        string cdId,
        int quantity,
        string slotLine,
        int definitionId)
    {
        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        string name = profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName)
            ? profile.DisplayName.Trim() + " 碎片"
            : cdId + " 碎片";

        string body = profile != null ? profile.Rarity + "  稀有度" : string.Empty;
        if (quantity > 1)
            body += (body.Length > 0 ? "\n" : string.Empty) + "數量  " + quantity;
        string gemLine = ValuablesVaultDiscard.FormatGemRefundLine(definitionId, quantity);
        if (!string.IsNullOrEmpty(gemLine))
            body += (body.Length > 0 ? "\n" : string.Empty) + gemLine;

        return new InfoPanelCopy(name, body, slotLine, true);
    }

    private static UnityEngine.Sprite ResolveCdFragmentIcon(string cdId)
    {
        UnityEngine.Sprite cover = BirdDuelCdIcons.Resolve(cdId);
        if (cover != null)
            return cover;

        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library == null || profile == null)
            return null;

        CardRarity cardRarity = profile.Rarity switch
        {
            BirdDuelCdRarity.R => CardRarity.R,
            BirdDuelCdRarity.SR => CardRarity.SR,
            _ => CardRarity.N,
        };
        return library.GetRarityFrame(cardRarity);
    }
}

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

        Card card = ResolveCard(definitionId);
        string name = card != null && !string.IsNullOrWhiteSpace(card.cardName)
            ? card.cardName.Trim()
            : "貴重品 #" + definitionId;

        string body = "編號  " + definitionId;
        if (card != null)
            body += "\n稀有度  " + card.rarity;
        if (quantity > 1)
            body += "\n數量  " + quantity;
        body += ValuablesVaultUiCopy.ReservedBodySuffix;

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
}

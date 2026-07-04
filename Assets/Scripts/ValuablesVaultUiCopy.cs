/// <summary>貴重品庫 UI 固定文案（半角標點，避免 Buildbeck 精簡 TMP 缺字）。</summary>
public static class ValuablesVaultUiCopy
{
    public const string FontGlyphProbeExtras =
        "貴重品庫儲存格空欄關閉數量物品資訊選取稀有度編號格位企劃" +
        "左側上下捲動瀏覽右側顯示尚未選取此格尚未放置點選其他格子檢視" +
        "取得方式使用效果補充說明錢包中的CD碎片放入此格丟棄返還至收藏" +
        "封印的法術未知解條件關鍵道具無法海牆巡邏拾學院歸檔支線?" +
        "x#./- NRSRSSRUR";

    public const string EmptySlotTitle = "(空欄)";
    public const string EmptySlotBody =
        "此格尚未放置貴重品.\n點選其他格子以檢視物品資訊.";
    public const string NoSelectionTitle = "尚未選取";
    public const string NoSelectionBody = "點選左側格子以檢視物品資訊.";
    public const string ReservedBodySuffix = "\n\n此欄位預留供企劃補充說明, 取得方式與使用效果.";
    public const string FooterHint = "左側選格 / 右側可放入或丟棄物品";
    public const string IconPlaceholder = "-";
    public const string SelectSlotHint = "點選左側格子以檢視物品";
    public const string DepositAllButtonLabel = "放入此格";
    public const string WalletFragmentsTitle = "錢包中的 CD 碎片";
    public const string DepositBlockedDifferentItem = "此格已有其他貴重品, 請選空欄或相同碎片格.";
    public const string DiscardButtonLabel = "丟棄";
    public const string KeyItemCannotDiscard = "關鍵道具無法丟棄";

    /// <summary>M-1-2 封印法術（支線解封前不揭示真名與效果）。</summary>
    public const string SealedSpellRelicName = "封印的法術";
    public const string SealedSpellRelicBody =
        "效果: 未知\n解封條件: ???\n\n海牆巡邏中拾得的蠟封殘卷.\n學院尚未歸檔, 關鍵道具無法丟棄.";

    public static string FormatSlotLine(int row, int col) =>
        "格位  第 " + row + " 列 / 第 " + col + " 欄";

    public static string FormatQuantitySuffix(int quantity) =>
        quantity > 1 ? " x" + quantity : string.Empty;
}

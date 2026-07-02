using UnityEngine;

/// <summary>
/// Deck Pack「查看牌組」→ Persistent：背包僅顯示該槽牌組內容，非玩家全收藏。
/// </summary>
public static class DeckPackViewSession
{
    private static bool restrictBackpackToSelectedDeck;
    private static bool preserveSelectedDeckSlotInBuildbeck;
    private static int pendingBuildbeckDeckSlot = -1;
    /// <summary>牌組頁「編輯牌組」進 Buildbeck：隱藏「準備好了／準備完成」進戰按鈕（整段停留期間有效）。</summary>
    private static bool hideReadyBattleButtonInBuildbeck;

    public static bool RestrictBackpackToSelectedDeck => restrictBackpackToSelectedDeck;

    public static bool ShouldPreserveSelectedDeckSlotInBuildbeck => preserveSelectedDeckSlotInBuildbeck;

    public static bool HideReadyBattleButtonInBuildbeck => hideReadyBattleButtonInBuildbeck;

    public static void BeginViewSelectedDeckInBackpack()
    {
        restrictBackpackToSelectedDeck = true;
    }

    public static void BeginEditSelectedDeckInBuildbeck(int deckSlotIndex)
    {
        preserveSelectedDeckSlotInBuildbeck = true;
        pendingBuildbeckDeckSlot = Mathf.Max(0, deckSlotIndex);
        hideReadyBattleButtonInBuildbeck = true;
    }

    public static bool TryGetPendingBuildbeckDeckSlot(out int deckSlotIndex)
    {
        if (preserveSelectedDeckSlotInBuildbeck && pendingBuildbeckDeckSlot >= 0)
        {
            deckSlotIndex = pendingBuildbeckDeckSlot;
            return true;
        }

        deckSlotIndex = 0;
        return false;
    }

    public static void ClearBuildbeckDeckFocus()
    {
        preserveSelectedDeckSlotInBuildbeck = false;
        pendingBuildbeckDeckSlot = -1;
    }

    public static void Clear()
    {
        restrictBackpackToSelectedDeck = false;
        hideReadyBattleButtonInBuildbeck = false;
        ClearBuildbeckDeckFocus();
    }

    public static int ResolveLibraryDisplayCount(PlayerData playerData, int cardId)
    {
        if (playerData == null) return 0;
        if (!restrictBackpackToSelectedDeck)
            return playerData.GetCollectionCount(cardId);

        int slot = Mathf.Clamp(playerData.selectedDeckSlot, 0, playerData.deckSlotCount - 1);
        return playerData.GetDeckCount(slot, cardId);
    }
}

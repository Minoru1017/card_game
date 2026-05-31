using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies the recommended 30-card tutorial deck to the active deck slot.</summary>
public static class TutorialDeckApplicator
{
    private struct DeckEntry
    {
        public int cardId;
        public int count;
        public DeckEntry(int cardId, int count)
        {
            this.cardId = cardId;
            this.count = count;
        }
    }

    private static readonly DeckEntry[] TutorialDeck =
    {
        new DeckEntry(4, 4),   // 民兵
        new DeckEntry(5, 4),   // 長弓兵
        new DeckEntry(17, 4),  // 修女
        new DeckEntry(22, 4),  // 教徒
        new DeckEntry(14, 3),  // 主教
        new DeckEntry(6, 2),   // 王國騎兵
        new DeckEntry(7, 2),   // 城堡
        new DeckEntry(2, 1),   // 護林鹿
        new DeckEntry(DeckCardId.SpellKeyFromOrdinal(0), 3), // 火球術
        new DeckEntry(DeckCardId.SpellKeyFromOrdinal(1), 3), // 初級治療
    };

    public static void ApplyToActivePlayerDeck(PlayerData playerData = null)
    {
        playerData = playerData != null ? playerData : TutorialPlotPlayerDataBridge.EnsureWritable();
        if (playerData == null)
        {
            Debug.LogWarning("TutorialDeckApplicator: PlayerData not found; deck not applied.");
            return;
        }

        playerData.LoadPlayerData();
        int slot = Mathf.Clamp(playerData.selectedDeckSlot, 0, playerData.deckSlotCount - 1);
        playerData.ClearDeckSlot(slot);

        for (int i = 0; i < TutorialDeck.Length; i++)
        {
            DeckEntry e = TutorialDeck[i];
            int owned = playerData.GetCollectionCount(e.cardId);
            int needOwned = Mathf.Max(owned, e.count);
            if (needOwned > owned)
                playerData.SetCollectionCount(e.cardId, needOwned);
            playerData.SetDeckCount(slot, e.cardId, e.count);
        }

        playerData.SavePlayerData();
        Debug.Log("TutorialDeckApplicator: applied tutorial deck to slot " + slot + ".");
    }

    /// <summary>若目前牌組為空則寫入教學 30 張；回傳是否為本次新發放。</summary>
    public static bool TryGrantIntroTutorialDeck(PlayerData playerData = null)
    {
        playerData = playerData != null ? playerData : TutorialPlotPlayerDataBridge.EnsureWritable();
        if (playerData == null)
        {
            Debug.LogWarning("TutorialDeckApplicator: PlayerData not found; deck not applied.");
            return false;
        }

        playerData.LoadPlayerData();
        if (playerData.GetSelectedDeckTotalCount() > 0)
            return false;

        ApplyToActivePlayerDeck(playerData);
        playerData.LoadPlayerData();
        bool ok = playerData.GetSelectedDeckTotalCount() > 0;
        if (!ok)
            Debug.LogError("TutorialDeckApplicator: intro tutorial deck still empty after apply.");
        return ok;
    }

    /// <summary>入門教學戰：若目前選中牌組為空則寫入推薦 30 張（須在 PlayerData 已就緒後呼叫）。</summary>
    public static bool EnsureIntroTutorialDeckReady(PlayerData playerData = null) =>
        TryGrantIntroTutorialDeck(playerData) || HasIntroTutorialDeckReady(playerData);

    private static bool HasIntroTutorialDeckReady(PlayerData playerData = null)
    {
        playerData = playerData != null ? playerData : TutorialPlotPlayerDataBridge.EnsureWritable();
        if (playerData == null) return false;
        playerData.LoadPlayerData();
        return playerData.GetSelectedDeckTotalCount() > 0;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>將 M-1-2 段考鎖定牌表寫入目前選中牌組槽（對戰前呼叫）。</summary>
public static class M12PhaseDeckApplicator
{
    public static void ApplyPhaseADeck(PlayerData playerData = null) =>
        ApplyDeck(M12PhaseDeckCatalog.PhaseADeckCardIds, playerData);

    public static void ApplyPhaseBDeck(PlayerData playerData = null) =>
        ApplyDeck(M12PhaseDeckCatalog.PhaseBDeckCardIds, playerData);

    private static void ApplyDeck(int[] cardIds, PlayerData playerData)
    {
        if (cardIds == null || cardIds.Length == 0)
            return;

        playerData = playerData != null ? playerData : PlayerData.ResolveCanonical();
        if (playerData == null)
        {
            Debug.LogWarning("M12PhaseDeckApplicator: PlayerData not found.");
            return;
        }

        playerData.LoadPlayerData();
        int slot = Mathf.Clamp(playerData.selectedDeckSlot, 0, playerData.deckSlotCount - 1);
        playerData.ClearDeckSlot(slot);

        var counts = new Dictionary<int, int>();
        for (int i = 0; i < cardIds.Length; i++)
        {
            int id = cardIds[i];
            counts.TryGetValue(id, out int c);
            counts[id] = c + 1;
        }

        foreach (var kv in counts)
        {
            int id = kv.Key;
            int need = kv.Value;
            int owned = playerData.GetCollectionCount(id);
            if (owned < need)
                playerData.SetCollectionCount(id, need);
            playerData.SetDeckCount(slot, id, need);
        }

        playerData.SavePlayerData();
    }
}

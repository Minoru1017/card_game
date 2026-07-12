using UnityEngine;

/// <summary>M-1-3 河岔分波首通：王國騎兵 ×1，熟練度 A（僅入收藏，不設 B 進度）。</summary>
public static class M13RiverForkRewardService
{
    public const int RewardCardId = 6;

    public static bool ShouldGrantForActivePlayer(int slot) =>
        !M13RiverForkProgressState.IsNodeCleared(slot);

    public static bool TryGrantRiverForkReward(PlayerData playerData = null)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M13RiverForkProgressState.IsNodeCleared(slot))
            return false;

        playerData = playerData != null ? playerData : PlayerData.ResolveCanonical();
        if (playerData == null)
        {
            Debug.LogError("M13RiverForkRewardService: PlayerData not found.");
            return false;
        }

        playerData.LoadPlayerData();
        playerData.AddCollection(RewardCardId, 1);
        playerData.SavePlayerData();
        Debug.Log("M13RiverForkRewardService: granted Kingdom Cavalry with proficiency A (first time).");
        return true;
    }

    public static string FormatRewardCardName(CardStore cardStore)
    {
        if (cardStore == null)
            return "王國騎兵";

        Card card = cardStore.GetCardById(RewardCardId);
        return card != null && !string.IsNullOrWhiteSpace(card.cardName)
            ? card.cardName.Trim()
            : "王國騎兵";
    }
}

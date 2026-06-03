using UnityEngine;

/// <summary>
/// M-1-2 海牆巡邏段考首通：修女／主教／城堡各 +1 收藏，熟練度設為 B（progressAny=2）。
/// 港灣首通不呼叫本類；見 <see cref="LEVEL_DESIGN_M-1-2.md"/>。
/// </summary>
public static class M12ReligiousLineRewardService
{
    public static readonly int[] RewardCardIds =
    {
        MonsterSkillIds.Nun,
        MonsterSkillIds.Bishop,
        MonsterSkillIds.Castle
    };

    public static bool ShouldGrantForActivePlayer() =>
        !TutorialProgressState.IsM12ReligiousLineRewardGrantedForActivePlayer();

    /// <summary>若尚未領過則發放三張並寫入 B 熟練度；回傳是否本次有發放。</summary>
    public static bool TryGrantReligiousLineReward(PlayerData playerData = null)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (TutorialProgressState.IsM12ReligiousLineRewardGranted(slot))
            return false;

        playerData = playerData != null ? playerData : PlayerData.ResolveCanonical();
        if (playerData == null)
        {
            Debug.LogError("M12ReligiousLineRewardService: PlayerData not found.");
            return false;
        }

        playerData.LoadPlayerData();
        for (int i = 0; i < RewardCardIds.Length; i++)
        {
            int id = RewardCardIds[i];
            playerData.AddCollection(id, 1);
            playerData.SetCardProficiencyWins(
                id,
                CardSkillProficiencyService.WinsRequiredForStageB,
                0);
        }

        playerData.SavePlayerData();
        TutorialProgressState.SetM12ReligiousLineRewardGranted(slot, true);
        Debug.Log("M12ReligiousLineRewardService: granted Nun, Bishop, Castle with proficiency B (first time).");
        return true;
    }

    public static string FormatRewardCardNames(CardStore cardStore)
    {
        if (cardStore == null)
            return "修女  主教  城堡";

        string nun = ResolveCardName(cardStore, MonsterSkillIds.Nun, "修女");
        string bishop = ResolveCardName(cardStore, MonsterSkillIds.Bishop, "主教");
        string castle = ResolveCardName(cardStore, MonsterSkillIds.Castle, "城堡");
        return nun + "  " + bishop + "  " + castle;
    }

    private static string ResolveCardName(CardStore store, int id, string fallback)
    {
        Card card = store.GetCardById(id);
        return card != null && !string.IsNullOrWhiteSpace(card.cardName) ? card.cardName.Trim() : fallback;
    }
}

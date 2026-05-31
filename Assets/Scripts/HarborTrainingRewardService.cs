using UnityEngine;

/// <summary>港灣訓練場通關獎勵：首通任一難度解鎖地圖；困難首通贈 SR 畢業證。</summary>
public static class HarborTrainingRewardService
{
    /// <summary>港灣畢業證 SR（聖院騎士，CardList.csv id 18）。</summary>
    public const int GraduationCardId = 18;

    public readonly struct VictoryGrantResult
    {
        public readonly bool FirstCombatClear;
        public readonly bool GrantedGraduationCard;
        public readonly string GraduationCardDisplayName;

        public VictoryGrantResult(bool firstCombatClear, bool grantedGraduationCard, string graduationCardDisplayName)
        {
            FirstCombatClear = firstCombatClear;
            GrantedGraduationCard = grantedGraduationCard;
            GraduationCardDisplayName = graduationCardDisplayName ?? string.Empty;
        }

        public bool HasNewReward => FirstCombatClear || GrantedGraduationCard;
    }

    /// <summary>勝利時呼叫；回傳本次新發放的進度／卡牌（重複通關不再發 SR）。</summary>
    public static VictoryGrantResult ProcessVictory(BattleDifficultyTier tier, PlayerData playerData = null)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        bool wasCombatCleared = HarborTrainingProgressState.IsHarborCombatCleared(slot);
        bool wasHardRewardGranted = HarborTrainingProgressState.IsHarborHardGraduationRewardGranted(slot);

        bool firstCombatClear = false;
        bool grantedCard = false;
        string cardName = string.Empty;

        if (!wasCombatCleared)
        {
            HarborTrainingProgressState.SetHarborCombatCleared(slot, true);
            firstCombatClear = true;
        }

        bool isHard = tier == BattleDifficultyTier.Hard;
        if (isHard && !wasHardRewardGranted)
        {
            playerData = playerData != null ? playerData : PlayerData.ResolveCanonical();
            if (playerData != null)
            {
                playerData.LoadPlayerData();
                playerData.AddCollection(GraduationCardId, 1);
                playerData.SavePlayerData();
                HarborTrainingProgressState.SetHarborHardGraduationRewardGranted(slot, true);
                grantedCard = true;
                cardName = ResolveCardDisplayName(playerData);
                Debug.Log("HarborTrainingRewardService: granted graduation SR card id " + GraduationCardId + ".");
            }
            else
            {
                Debug.LogWarning("HarborTrainingRewardService: PlayerData not found; graduation card not granted.");
            }
        }

        return new VictoryGrantResult(firstCombatClear, grantedCard, cardName);
    }

    public static string BuildVictorySubtitle(VictoryGrantResult result)
    {
        if (result.GrantedGraduationCard && result.FirstCombatClear)
        {
            string name = string.IsNullOrWhiteSpace(result.GraduationCardDisplayName)
                ? "港灣畢業證"
                : result.GraduationCardDisplayName;
            return "港灣實戰通關 · 海牆巡邏已解鎖\n港灣畢業證 " + name + " 已入收藏";
        }

        if (result.GrantedGraduationCard)
        {
            string name = string.IsNullOrWhiteSpace(result.GraduationCardDisplayName)
                ? "港灣畢業證"
                : result.GraduationCardDisplayName;
            return "港灣畢業證 " + name + " 已入收藏";
        }

        if (result.FirstCombatClear)
            return "港灣實戰通關 · 海牆巡邏已解鎖";

        return "熟練度與戰績已記錄";
    }

    private static string ResolveCardDisplayName(PlayerData playerData)
    {
        CardStore store = playerData != null ? playerData.CardStore : null;
        if (store == null)
            store = Object.FindFirstObjectByType<CardStore>();
        if (store != null)
        {
            Card card = store.GetCardById(GraduationCardId);
            if (card != null && !string.IsNullOrWhiteSpace(card.cardName))
                return card.cardName.Trim();
        }

        return "聖院騎士";
    }
}

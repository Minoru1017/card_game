using UnityEngine;

/// <summary>A-1 完成獎勵：金幣、節點通關旗標；潮印解封改由貴重品庫點選觸發。</summary>
public static class SideQuestA1TideMarkRewardService
{
    public const int FullRunCoinReward = 80;
    public const int SkippedRunCoinReward = 20;
    public const int FullRunAllTurnInCoinBonus = 15;

    public enum FarmOutcome
    {
        Skipped,
        Completed
    }

    public struct ApplyResult
    {
        public bool tideMarkUnsealed;
        public bool seaPurslaneSeedKept;
        public int coinsGranted;
        public string message;
    }

    public static ApplyResult ApplyFarmOutcome(int slot, FarmOutcome outcome, bool keptSeaPurslaneSeed = false)
    {
        slot = Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        var result = new ApplyResult();

        PlayerData playerData = PlayerData.ResolveCanonical();
        if (playerData == null)
        {
            result.message = "無法讀取玩家資料。";
            return result;
        }

        playerData.LoadPlayerData();

        if (outcome == FarmOutcome.Completed)
        {
            result.coinsGranted = FullRunCoinReward;
            if (!keptSeaPurslaneSeed)
                result.coinsGranted += FullRunAllTurnInCoinBonus;

            if (keptSeaPurslaneSeed)
            {
                SideQuestA1ProgressState.MarkSeaPurslaneSeedKept(slot);
                result.seaPurslaneSeedKept = true;
            }

            SideQuestA1ProgressState.MarkNodeCleared(slot);
            result.message = ValuablesVaultCatalog.CanUnsealTideMarkSpell(slot)
                ? "三畦輪作完成。請至貴重品庫點選封印的法術解封潮印。"
                : "三畦輪作完成。";
            if (result.seaPurslaneSeedKept)
                result.message += " 已留海蓬種。";
        }
        else
        {
            result.coinsGranted = SkippedRunCoinReward;
            result.message = "草奶奶代勞收成了部分作物（不解封潮印）。";
        }

        playerData.playerCoins += result.coinsGranted;
        playerData.SavePlayerData();
        return result;
    }
}

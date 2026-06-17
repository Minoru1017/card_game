using System.Collections.Generic;
using UnityEngine;

/// <summary>CD 光碟 gacha（§12.5.2–12.5.4）。</summary>
public static class BirdDuelCdGachaService
{
    public const int SinglePullCost = 80;
    public const int TenPullCost = 720;
    public const int SrHardPity = 80;

    private const float RateN = 0.58f;
    private const float RateR = 0.36f;

    public readonly struct PullOutcome
    {
        public readonly string CdId;
        public readonly BirdDuelCdRarity RolledRarity;
        public readonly bool WasDuplicate;
        public readonly int FragmentsGranted;
        public readonly bool WasNewCard;

        public PullOutcome(
            string cdId,
            BirdDuelCdRarity rolledRarity,
            bool wasDuplicate,
            int fragmentsGranted,
            bool wasNewCard)
        {
            CdId = cdId;
            RolledRarity = rolledRarity;
            WasDuplicate = wasDuplicate;
            FragmentsGranted = fragmentsGranted;
            WasNewCard = wasNewCard;
        }

        public string BuildToastLine()
        {
            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(CdId);
            string name = profile != null ? profile.DisplayName : CdId;
            if (WasNewCard)
                return "獲得 CD：" + name;
            if (FragmentsGranted > 0)
                return name + " 重複 → 碎片 +" + FragmentsGranted;
            return name;
        }
    }

    public static bool TryPullSingle(PlayerData playerData, out PullOutcome outcome)
    {
        outcome = default;
        if (playerData == null) return false;
        if (playerData.playerGems < SinglePullCost)
            return false;

        playerData.playerGems -= SinglePullCost;
        int slot = playerData.activePlayerSlot;
        outcome = RollAndApplyOne(slot);
        playerData.SavePlayerData();
        return true;
    }

    public static bool TryPullTen(PlayerData playerData, out List<PullOutcome> outcomes)
    {
        outcomes = null;
        if (playerData == null) return false;
        if (playerData.playerGems < TenPullCost)
            return false;

        playerData.playerGems -= TenPullCost;
        int slot = playerData.activePlayerSlot;

        var rarities = new List<BirdDuelCdRarity>(10);
        int pity = PlayerBirdDuelCdState.GetSrPityCounter(slot);
        for (int i = 0; i < 10; i++)
        {
            BirdDuelCdRarity rarity = ResolveRarityForPull(ref pity);
            rarities.Add(rarity);
        }

        EnsureTenPullAtLeastOneR(rarities);

        outcomes = new List<PullOutcome>(10);
        for (int i = 0; i < rarities.Count; i++)
            outcomes.Add(ApplyRarityPull(slot, rarities[i], ref pity));

        PlayerBirdDuelCdState.SetSrPityCounter(slot, pity);
        playerData.SavePlayerData();
        return true;
    }

    private static PullOutcome RollAndApplyOne(int slot)
    {
        int pity = PlayerBirdDuelCdState.GetSrPityCounter(slot);
        BirdDuelCdRarity rarity = ResolveRarityForPull(ref pity);
        PullOutcome outcome = ApplyRarityPull(slot, rarity, ref pity);
        PlayerBirdDuelCdState.SetSrPityCounter(slot, pity);
        return outcome;
    }

    private static BirdDuelCdRarity ResolveRarityForPull(ref int pityCounter)
    {
        pityCounter++;
        if (pityCounter >= SrHardPity)
        {
            pityCounter = 0;
            return BirdDuelCdRarity.SR;
        }

        BirdDuelCdRarity rarity = RollRarity();
        if (rarity == BirdDuelCdRarity.SR)
            pityCounter = 0;
        return rarity;
    }

    private static PullOutcome ApplyRarityPull(int slot, BirdDuelCdRarity rarity, ref int pityCounter)
    {
        BirdDuelCdProfile profile = PickProfileForRarity(rarity);
        if (profile == null)
            profile = BirdDuelCdCatalog.Default;

        bool owned = PlayerBirdDuelCdState.OwnsCd(slot, profile.CdId);
        if (!owned)
        {
            PlayerBirdDuelCdState.GrantCd(slot, profile.CdId);
            if (rarity == BirdDuelCdRarity.SR)
                pityCounter = 0;
            return new PullOutcome(profile.CdId, rarity, false, 0, true);
        }

        int frags = BirdDuelCdCatalog.DuplicateFragmentAmount(rarity);
        PlayerBirdDuelCdState.AddFragments(slot, profile.CdId, frags);
        if (rarity == BirdDuelCdRarity.SR)
            pityCounter = 0;
        return new PullOutcome(profile.CdId, rarity, true, frags, false);
    }

    private static void EnsureTenPullAtLeastOneR(List<BirdDuelCdRarity> rarities)
    {
        if (rarities == null || rarities.Count == 0) return;
        for (int i = 0; i < rarities.Count; i++)
        {
            if (rarities[i] >= BirdDuelCdRarity.R)
                return;
        }

        rarities[rarities.Count - 1] = BirdDuelCdRarity.R;
    }

    private static BirdDuelCdRarity RollRarity()
    {
        float roll = Random.value;
        if (roll < RateN) return BirdDuelCdRarity.N;
        if (roll < RateN + RateR) return BirdDuelCdRarity.R;
        return BirdDuelCdRarity.SR;
    }

    private static BirdDuelCdProfile PickProfileForRarity(BirdDuelCdRarity rarity)
    {
        if (rarity == BirdDuelCdRarity.N)
            return BirdDuelCdCatalog.Default;

        List<BirdDuelCdProfile> pool = BirdDuelCdCatalog.GetGachaProfilesForRarity(rarity);
        if (pool.Count == 0)
            return BirdDuelCdCatalog.Default;
        return pool[Random.Range(0, pool.Count)];
    }
}

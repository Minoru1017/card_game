using System.Collections.Generic;

/// <summary>CD 光碟查表與 gacha 池（v1）。</summary>
public static class BirdDuelCdCatalog
{
    public const string DefaultCdId = "harbor_practice_tape";

    /// <summary>封面圖檔名（Assets/UI/CD/{key}.jpg），對應 <see cref="UiSpriteLibrary.GetBirdDuelCdCover"/>。</summary>
    public const string DefaultCoverAssetKey = "CD_1";
    public const string CourtMarchCoverAssetKey = "CD_2";

    /// <summary>
    /// 庭訓進行曲勝利 draft 白名單：國王專屬命名與組合，
    /// 不含港灣練習帶的養精蓄銳／連抽／壓制／看破·全局等六項。
    /// </summary>
    private static readonly BirdDuelBonusId[] CourtMarchDraftIds =
    {
        BirdDuelBonusId.CourtDecree,
        BirdDuelBonusId.RoyalPhalanx,
        BirdDuelBonusId.VanguardRecon,
        BirdDuelBonusId.CrownGuard,
        BirdDuelBonusId.WarDrumCharge,
        BirdDuelBonusId.LastStand,
    };

    private static readonly BirdDuelBonusId[] ChurchDraftIds =
    {
        BirdDuelBonusId.Tailwind,
        BirdDuelBonusId.InsightOpening,
        BirdDuelBonusId.Regroup,
        BirdDuelBonusId.InsightFull,
        BirdDuelBonusId.Providence
    };

    private static readonly Dictionary<string, BirdDuelCdProfile> ById =
        new Dictionary<string, BirdDuelCdProfile>();

    static BirdDuelCdCatalog()
    {
        Register(BirdDuelCdProfile.Create(
            DefaultCdId, "港灣練習帶", BirdDuelCdRarity.N, BirdDuelCdFaction.General, false,
            BirdDuelBonusId.DeepRest, BirdDuelBonusId.FirstStrike, BirdDuelBonusId.DoubleDraw,
            BirdDuelBonusId.Regroup, BirdDuelBonusId.Suppress, BirdDuelBonusId.InsightFull));

        Register(BirdDuelCdProfile.Create(
            "court_march", "庭訓進行曲", BirdDuelCdRarity.R, BirdDuelCdFaction.King, true,
            CourtMarchDraftIds));

        Register(BirdDuelCdProfile.Create(
            "morning_prayer", "晨禱", BirdDuelCdRarity.R, BirdDuelCdFaction.Church, true,
            ChurchDraftIds));

        Register(BirdDuelCdProfile.Create(
            "dawn_hymn", "破曉聖詠", BirdDuelCdRarity.SR, BirdDuelCdFaction.Church, true,
            BirdDuelBonusId.Providence, BirdDuelBonusId.FullDraw, BirdDuelBonusId.InsightFull,
            BirdDuelBonusId.Regroup, BirdDuelBonusId.Tailwind));
    }

    private static void Register(BirdDuelCdProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.CdId)) return;
        ById[profile.CdId] = profile;
    }

    public static BirdDuelCdProfile Get(string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId)) return null;
        ById.TryGetValue(cdId.Trim(), out BirdDuelCdProfile profile);
        return profile;
    }

    public static BirdDuelCdProfile Default => Get(DefaultCdId);

    public static IReadOnlyList<BirdDuelCdProfile> AllProfiles
    {
        get
        {
            var list = new List<BirdDuelCdProfile>(ById.Count);
            foreach (KeyValuePair<string, BirdDuelCdProfile> pair in ById)
                list.Add(pair.Value);
            return list;
        }
    }

    public static IReadOnlyList<BirdDuelBonusId> ResolveWinDraftBonusIds(string cdId)
    {
        BirdDuelCdProfile profile = Get(cdId);
        if (profile != null && profile.WinDraftBonusIds.Count > 0)
            return profile.WinDraftBonusIds;
        return Default.WinDraftBonusIds;
    }

    public static int DuplicateFragmentAmount(BirdDuelCdRarity rarity)
    {
        switch (rarity)
        {
            case BirdDuelCdRarity.SR: return 40;
            case BirdDuelCdRarity.R: return 20;
            default: return 8;
        }
    }

    public static List<BirdDuelCdProfile> GetGachaProfilesForRarity(BirdDuelCdRarity rarity)
    {
        var list = new List<BirdDuelCdProfile>();
        foreach (KeyValuePair<string, BirdDuelCdProfile> pair in ById)
        {
            BirdDuelCdProfile p = pair.Value;
            if (!p.InGachaPool || p.Rarity != rarity) continue;
            list.Add(p);
        }
        return list;
    }
}

using System.Collections.Generic;

/// <summary>鬥鳥 CD 光碟資料（BGM、陣營、勝利 draft 白名單）。</summary>
public sealed class BirdDuelCdProfile
{
    public string CdId { get; private set; }
    public string DisplayName { get; private set; }
    public BirdDuelCdRarity Rarity { get; private set; }
    public BirdDuelCdFaction Faction { get; private set; }
    public bool InGachaPool { get; private set; }
    public IReadOnlyList<BirdDuelBonusId> WinDraftBonusIds { get; private set; }

    private BirdDuelCdProfile(
        string cdId,
        string displayName,
        BirdDuelCdRarity rarity,
        BirdDuelCdFaction faction,
        bool inGachaPool,
        IReadOnlyList<BirdDuelBonusId> winDraftBonusIds)
    {
        CdId = cdId;
        DisplayName = displayName;
        Rarity = rarity;
        Faction = faction;
        InGachaPool = inGachaPool;
        WinDraftBonusIds = winDraftBonusIds ?? System.Array.Empty<BirdDuelBonusId>();
    }

    public static BirdDuelCdProfile Create(
        string cdId,
        string displayName,
        BirdDuelCdRarity rarity,
        BirdDuelCdFaction faction,
        bool inGachaPool,
        params BirdDuelBonusId[] winDraftBonusIds)
    {
        return new BirdDuelCdProfile(cdId, displayName, rarity, faction, inGachaPool, winDraftBonusIds);
    }
}

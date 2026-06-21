/// <summary>鬥鳥戰前加成所屬的抽選池（見 Docs/鬥鳥手勢小遊戲企劃.md 第九章）。</summary>
public enum BirdDuelBonusPool
{
    Basic,
    Enhanced,
    Rare,
    EnemyBuff
}

/// <summary>鬥鳥戰前加成與敵方小強化的識別碼。</summary>
public enum BirdDuelBonusId
{
    None = 0,

    MorningPractice,
    ExtraCard,
    SteadyStance,
    Tailwind,
    InsightOpening,

    DeepRest,
    FirstStrike,
    DoubleDraw,
    Regroup,
    Suppress,
    InsightFull,

    Providence,
    FullDraw,
    LastStand,

    CourtDecree,
    RoyalPhalanx,
    VanguardRecon,
    CrownGuard,
    WarDrumCharge,

    PrayerVigil,
    VeiledSight,
    QuietRegroup,
    GalePsalm,
    SacredShield,
    HiddenPath,

    EnemyMorale,
    EnemyDraw,
    EnemyOffense
}

public struct BirdDuelBonusInfo
{
    public BirdDuelBonusId Id;
    public BirdDuelBonusPool Pool;
    public string DisplayName;
    public string Description;
}

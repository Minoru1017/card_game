/// <summary>敵方英雄查表；v1 僅港灣熱血同學，Buildbeck 陪練延後。</summary>
public static class EnemyHeroCatalog
{
    public const string HotBloodClassmateId = "harbor_hot_blood_classmate";

    private static EnemyHeroProfile hotBloodClassmate;

    public static EnemyHeroProfile HotBloodClassmate =>
        hotBloodClassmate ?? (hotBloodClassmate = EnemyHeroProfile.CreateHotBloodClassmate());

    /// <summary>1-1 港灣實戰敵方（簡／普／困難同一人）。</summary>
    public static EnemyHeroProfile ResolveForHarbor() => HotBloodClassmate;

    public static EnemyHeroProfile ResolveById(string heroId)
    {
        if (heroId == HotBloodClassmateId)
            return HotBloodClassmate;
        return null;
    }
}

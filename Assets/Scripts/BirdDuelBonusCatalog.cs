using System.Collections.Generic;
using UnityEngine;

/// <summary>加成目錄：查表、池別索引與「不重複」隨機抽選。</summary>
public static partial class BirdDuelBonusCatalog
{
    private static readonly Dictionary<BirdDuelBonusId, BirdDuelBonusInfo> ById =
        new Dictionary<BirdDuelBonusId, BirdDuelBonusInfo>();

    private static readonly Dictionary<BirdDuelBonusPool, List<BirdDuelBonusId>> ByPool =
        new Dictionary<BirdDuelBonusPool, List<BirdDuelBonusId>>();

    static BirdDuelBonusCatalog()
    {
        RegisterAllEntries();
        BuildLookups();
    }

    public static BirdDuelBonusInfo Get(BirdDuelBonusId id)
    {
        if (ById.TryGetValue(id, out BirdDuelBonusInfo info))
            return info;
        return new BirdDuelBonusInfo { Id = BirdDuelBonusId.None, DisplayName = "", Description = "" };
    }

    public static string DisplayName(BirdDuelBonusId id) => Get(id).DisplayName;
    public static string Description(BirdDuelBonusId id) => Get(id).Description;

    public static bool IsCurse(BirdDuelBonusId id) => id == BirdDuelBonusId.LastStand;

    public static List<BirdDuelBonusId> DrawDistinct(BirdDuelBonusPool pool, int count)
    {
        if (!ByPool.TryGetValue(pool, out List<BirdDuelBonusId> candidates) || candidates.Count == 0)
            return new List<BirdDuelBonusId>();

        var shuffled = new List<BirdDuelBonusId>(candidates);
        ShuffleInPlace(shuffled);
        int take = Mathf.Clamp(count, 0, shuffled.Count);
        return shuffled.GetRange(0, take);
    }

    public static List<BirdDuelBonusId> DrawDistinctFromIds(IReadOnlyList<BirdDuelBonusId> ids, int count)
    {
        var candidates = new List<BirdDuelBonusId>();
        if (ids != null)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                BirdDuelBonusId id = ids[i];
                if (id == BirdDuelBonusId.None || candidates.Contains(id)) continue;
                candidates.Add(id);
            }
        }

        ShuffleInPlace(candidates);
        int take = Mathf.Clamp(count, 0, candidates.Count);
        return candidates.GetRange(0, take);
    }

    public static BirdDuelBonusId DrawOne(BirdDuelBonusPool pool)
    {
        List<BirdDuelBonusId> one = DrawDistinct(pool, 1);
        return one.Count > 0 ? one[0] : BirdDuelBonusId.None;
    }

    private static void Register(BirdDuelBonusInfo info)
    {
        if (info.Id == BirdDuelBonusId.None) return;
        AllEntries.Add(info);
    }

    private static void BuildLookups()
    {
        ById.Clear();
        ByPool.Clear();
        for (int i = 0; i < AllEntries.Count; i++)
        {
            BirdDuelBonusInfo info = AllEntries[i];
            ById[info.Id] = info;
            if (!ByPool.TryGetValue(info.Pool, out List<BirdDuelBonusId> poolIds))
            {
                poolIds = new List<BirdDuelBonusId>();
                ByPool[info.Pool] = poolIds;
            }
            poolIds.Add(info.Id);
        }
    }

    private static void ShuffleInPlace(List<BirdDuelBonusId> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            BirdDuelBonusId tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}

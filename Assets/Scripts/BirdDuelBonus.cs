using System.Collections.Generic;
using UnityEngine;

/// <summary>鬥鳥戰前加成所屬的抽選池（見 Docs/鬥鳥手勢小遊戲企劃.md 第九章）。</summary>
public enum BirdDuelBonusPool
{
    Basic,     // 基礎池：平手／敗北保底
    Enhanced,  // 強化池：鬥鳥勝利專屬
    Rare,      // 稀有池：魔王級支線專屬
    EnemyBuff  // 敵方小強化：風險 B，敗北時施加
}

/// <summary>鬥鳥戰前加成與敵方小強化的識別碼。</summary>
public enum BirdDuelBonusId
{
    None = 0,

    // 基礎池
    MorningPractice, // 晨間練習：起始 HP +3
    ExtraCard,       // 多備一手：開局多抽 1
    SteadyStance,    // 穩固陣腳：整場敵方傷害 −15%
    Tailwind,        // 順風：開局天氣「穿堂微風」（首張法術 +20%）
    InsightOpening,  // 看破·先機：開戰揭示敵方手牌 1 張

    // 強化池
    DeepRest,        // 養精蓄銳：起始 HP +6
    FirstStrike,     // 先聲奪人：解除我方首回合禁攻
    DoubleDraw,      // 連抽：開局多抽 2
    Regroup,         // 重整旗鼓：整場我方每回合多抽 1
    Suppress,        // 壓制：整場敵方傷害 −20%
    InsightFull,     // 看破·全局：揭示敵方手牌 2 張，且整場敵方傷害 −10%

    // 稀有池（魔王級支線專屬）
    Providence,      // 天時地利：開局天氣「訓練薄霧」（英雄受擊 −50%）
    FullDraw,        // 滿弓待發：HP +6 ＋ 開局多抽 2 ＋ 解除首回合禁攻
    LastStand,       // 背水（詛咒型）：HP 設為 12 ＋ 開局多抽 2 ＋ 解除首回合禁攻 ＋ 順風

    // 敵方小強化（風險 B）
    EnemyMorale,     // 敵·士氣：敵方起始 HP +2
    EnemyDraw,       // 敵·搶手：敵方首回合多抽 1
    EnemyOffense     // 敵·攻勢：整場敵方傷害 +10%
}

public struct BirdDuelBonusInfo
{
    public BirdDuelBonusId Id;
    public BirdDuelBonusPool Pool;
    public string DisplayName;
    public string Description;
}

/// <summary>加成目錄：顯示文字與池別查詢，並提供「不重複」的純隨機抽選。</summary>
public static class BirdDuelBonusCatalog
{
    private static readonly List<BirdDuelBonusInfo> All = new List<BirdDuelBonusInfo>
    {
        // 基礎池
        Make(BirdDuelBonusId.MorningPractice, BirdDuelBonusPool.Basic, "晨間練習", "起始 HP +3"),
        Make(BirdDuelBonusId.ExtraCard, BirdDuelBonusPool.Basic, "多備一手", "開局多抽 1 張"),
        Make(BirdDuelBonusId.SteadyStance, BirdDuelBonusPool.Basic, "穩固陣腳", "整場敵方傷害 −15%"),
        Make(BirdDuelBonusId.Tailwind, BirdDuelBonusPool.Basic, "順風", "開局天氣「穿堂微風」（首張法術 +20%）"),
        Make(BirdDuelBonusId.InsightOpening, BirdDuelBonusPool.Basic, "看破·先機", "開戰揭示敵方手牌 1 張"),

        // 強化池
        Make(BirdDuelBonusId.DeepRest, BirdDuelBonusPool.Enhanced, "養精蓄銳", "起始 HP +6"),
        Make(BirdDuelBonusId.FirstStrike, BirdDuelBonusPool.Enhanced, "先聲奪人", "解除我方首回合禁攻"),
        Make(BirdDuelBonusId.DoubleDraw, BirdDuelBonusPool.Enhanced, "連抽", "開局多抽 2 張"),
        Make(BirdDuelBonusId.Regroup, BirdDuelBonusPool.Enhanced, "重整旗鼓", "整場我方每回合多抽 1 張"),
        Make(BirdDuelBonusId.Suppress, BirdDuelBonusPool.Enhanced, "壓制", "整場敵方傷害 −20%"),
        Make(BirdDuelBonusId.InsightFull, BirdDuelBonusPool.Enhanced, "看破·全局", "揭示敵方手牌 2 張，且整場敵方傷害 −10%"),

        // 稀有池
        Make(BirdDuelBonusId.Providence, BirdDuelBonusPool.Rare, "天時地利", "開局天氣「訓練薄霧」（英雄受擊 −50%）"),
        Make(BirdDuelBonusId.FullDraw, BirdDuelBonusPool.Rare, "滿弓待發", "HP +6 ＋ 開局多抽 2 ＋ 解除首回合禁攻"),
        Make(BirdDuelBonusId.LastStand, BirdDuelBonusPool.Rare, "背水", "起始 HP 降為 12，換得開局多抽 2 ＋ 解除首回合禁攻 ＋ 順風"),

        // 敵方小強化
        Make(BirdDuelBonusId.EnemyMorale, BirdDuelBonusPool.EnemyBuff, "敵·士氣", "敵方起始 HP +2"),
        Make(BirdDuelBonusId.EnemyDraw, BirdDuelBonusPool.EnemyBuff, "敵·搶手", "敵方首回合多抽 1 張"),
        Make(BirdDuelBonusId.EnemyOffense, BirdDuelBonusPool.EnemyBuff, "敵·攻勢", "整場敵方傷害 +10%"),
    };

    private static BirdDuelBonusInfo Make(BirdDuelBonusId id, BirdDuelBonusPool pool, string name, string desc)
        => new BirdDuelBonusInfo { Id = id, Pool = pool, DisplayName = name, Description = desc };

    public static BirdDuelBonusInfo Get(BirdDuelBonusId id)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Id == id) return All[i];
        return new BirdDuelBonusInfo { Id = BirdDuelBonusId.None, DisplayName = "", Description = "" };
    }

    public static string DisplayName(BirdDuelBonusId id) => Get(id).DisplayName;
    public static string Description(BirdDuelBonusId id) => Get(id).Description;

    /// <summary>背水為詛咒型加成，播報時獨立分類。</summary>
    public static bool IsCurse(BirdDuelBonusId id) => id == BirdDuelBonusId.LastStand;

    /// <summary>從指定池抽出 count 個「不重複」的加成（純隨機）。池內不足則回傳全部。</summary>
    public static List<BirdDuelBonusId> DrawDistinct(BirdDuelBonusPool pool, int count)
    {
        List<BirdDuelBonusId> candidates = new List<BirdDuelBonusId>();
        for (int i = 0; i < All.Count; i++)
            if (All[i].Pool == pool) candidates.Add(All[i].Id);

        // Fisher–Yates 洗牌後取前 count 個，確保不重複。
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            BirdDuelBonusId tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        int take = Mathf.Clamp(count, 0, candidates.Count);
        return candidates.GetRange(0, take);
    }

    /// <summary>從指定 id 清單抽出 count 個「不重複」加成（純隨機）。</summary>
    public static List<BirdDuelBonusId> DrawDistinctFromIds(IReadOnlyList<BirdDuelBonusId> ids, int count)
    {
        List<BirdDuelBonusId> candidates = new List<BirdDuelBonusId>();
        if (ids != null)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                BirdDuelBonusId id = ids[i];
                if (id == BirdDuelBonusId.None || candidates.Contains(id)) continue;
                candidates.Add(id);
            }
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            BirdDuelBonusId tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        int take = Mathf.Clamp(count, 0, candidates.Count);
        return candidates.GetRange(0, take);
    }

    /// <summary>從指定池隨機抽 1 個。</summary>
    public static BirdDuelBonusId DrawOne(BirdDuelBonusPool pool)
    {
        List<BirdDuelBonusId> one = DrawDistinct(pool, 1);
        return one.Count > 0 ? one[0] : BirdDuelBonusId.None;
    }
}

/// <summary>鬥鳥加成靜態目錄資料（由 <see cref="BirdDuelBonusCatalog"/> 建索引）。</summary>
public static partial class BirdDuelBonusCatalog
{
    private static readonly System.Collections.Generic.List<BirdDuelBonusInfo> AllEntries =
        new System.Collections.Generic.List<BirdDuelBonusInfo>();

    private static BirdDuelBonusInfo Make(BirdDuelBonusId id, BirdDuelBonusPool pool, string name, string desc)
        => new BirdDuelBonusInfo { Id = id, Pool = pool, DisplayName = name, Description = desc };

    private static void RegisterAllEntries()
    {
        AllEntries.Clear();

        Register(Make(BirdDuelBonusId.MorningPractice, BirdDuelBonusPool.Basic, "晨間練習", "起始 HP +3"));
        Register(Make(BirdDuelBonusId.ExtraCard, BirdDuelBonusPool.Basic, "多備一手", "開局多抽 1 張"));
        Register(Make(BirdDuelBonusId.SteadyStance, BirdDuelBonusPool.Basic, "穩固陣腳", "整場敵方傷害 −15%"));
        Register(Make(BirdDuelBonusId.Tailwind, BirdDuelBonusPool.Basic, "順風", "開局天氣「穿堂微風」（首張法術 +20%）"));
        Register(Make(BirdDuelBonusId.InsightOpening, BirdDuelBonusPool.Basic, "看破·先機", "開戰揭示敵方手牌 1 張"));

        Register(Make(BirdDuelBonusId.DeepRest, BirdDuelBonusPool.Enhanced, "養精蓄銳", "起始 HP +6"));
        Register(Make(BirdDuelBonusId.FirstStrike, BirdDuelBonusPool.Enhanced, "先聲奪人", "解除我方首回合禁攻"));
        Register(Make(BirdDuelBonusId.DoubleDraw, BirdDuelBonusPool.Enhanced, "連抽", "開局多抽 2 張"));
        Register(Make(BirdDuelBonusId.Regroup, BirdDuelBonusPool.Enhanced, "重整旗鼓", "整場我方每回合多抽 1 張"));
        Register(Make(BirdDuelBonusId.Suppress, BirdDuelBonusPool.Enhanced, "壓制", "整場敵方傷害 −20%"));
        Register(Make(BirdDuelBonusId.InsightFull, BirdDuelBonusPool.Enhanced, "看破·全局", "揭示敵方手牌 2 張，且整場敵方傷害 −10%"));

        Register(Make(BirdDuelBonusId.Providence, BirdDuelBonusPool.Rare, "天時地利", "開局天氣「訓練薄霧」（英雄受擊 −50%）"));
        Register(Make(BirdDuelBonusId.FullDraw, BirdDuelBonusPool.Rare, "滿弓待發", "HP +6 ＋ 開局多抽 2 ＋ 解除首回合禁攻"));
        Register(Make(BirdDuelBonusId.LastStand, BirdDuelBonusPool.Rare, "背水", "起始 HP 降為 12，換得開局多抽 2 ＋ 解除首回合禁攻 ＋ 順風"));

        Register(Make(BirdDuelBonusId.CourtDecree, BirdDuelBonusPool.Enhanced, "庭訓號令", "解除我方首回合禁攻 ＋ 開戰揭示敵方手牌 1 張"));
        Register(Make(BirdDuelBonusId.RoyalPhalanx, BirdDuelBonusPool.Enhanced, "王權方陣", "起始 HP +4 ＋ 整場敵方傷害 −12%"));
        Register(Make(BirdDuelBonusId.VanguardRecon, BirdDuelBonusPool.Enhanced, "前鋒偵察", "開局多抽 1 張 ＋ 揭示敵方手牌 2 張"));
        Register(Make(BirdDuelBonusId.CrownGuard, BirdDuelBonusPool.Enhanced, "御前護衛", "起始 HP +3 ＋ 開局天氣「訓練薄霧」（英雄受擊 −50%）"));
        Register(Make(BirdDuelBonusId.WarDrumCharge, BirdDuelBonusPool.Enhanced, "戰鼓齊進", "解除我方首回合禁攻 ＋ 開局多抽 1 張"));

        Register(Make(BirdDuelBonusId.PrayerVigil, BirdDuelBonusPool.Enhanced, "晨禱守夜", "揭示敵方手牌 2 張 ＋ 開局天氣「訓練薄霧」"));
        Register(Make(BirdDuelBonusId.VeiledSight, BirdDuelBonusPool.Enhanced, "窺視之眼", "揭示敵方手牌 3 張（看破至深）"));
        Register(Make(BirdDuelBonusId.QuietRegroup, BirdDuelBonusPool.Enhanced, "靜默重整", "起始 HP +3 ＋ 整場我方每回合多抽 1 張"));
        Register(Make(BirdDuelBonusId.GalePsalm, BirdDuelBonusPool.Enhanced, "穿堂頌詩", "開局天氣「穿堂微風」＋ 開局多抽 1 張"));
        Register(Make(BirdDuelBonusId.SacredShield, BirdDuelBonusPool.Enhanced, "聖盾禱告", "本場第一次對我方英雄造成的傷害改為 0（不論數值高低）"));
        Register(Make(BirdDuelBonusId.HiddenPath, BirdDuelBonusPool.Enhanced, "密藏引路", "較稀有的卡更容易在開局前 3 回合被抽到"));

        Register(Make(BirdDuelBonusId.EnemyMorale, BirdDuelBonusPool.EnemyBuff, "敵·士氣", "敵方起始 HP +2"));
        Register(Make(BirdDuelBonusId.EnemyDraw, BirdDuelBonusPool.EnemyBuff, "敵·搶手", "敵方首回合多抽 1 張"));
        Register(Make(BirdDuelBonusId.EnemyOffense, BirdDuelBonusPool.EnemyBuff, "敵·攻勢", "整場敵方傷害 +10%"));
    }
}

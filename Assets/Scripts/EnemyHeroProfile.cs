/// <summary>
/// 敵方英雄資料：串起鬥鳥 NPC、立繪橋接台詞與正式對戰顯示名。
/// v1 僅港灣「熱血同學」；見 Docs/鬥鳥手勢小遊戲企劃.md 第十、十一章。
/// </summary>
public sealed class EnemyHeroProfile
{
    public string HeroId { get; private set; }
    public string DisplayName { get; private set; }
    public string SpecialtyTagZh { get; private set; }

    private readonly BirdDuelNpcProfile duelNpc;

    private EnemyHeroProfile(string heroId, string displayName, string specialtyTagZh, BirdDuelNpcProfile duelNpc)
    {
        HeroId = heroId;
        DisplayName = displayName;
        SpecialtyTagZh = specialtyTagZh;
        this.duelNpc = duelNpc;
    }

    public BirdDuelNpcProfile ToBirdDuelNpcProfile() => duelNpc;

    /// <summary>立繪 A：鬥鳥前擅長說明（1～2 行）。</summary>
    public string ResolvePortraitALine(bool isRematch)
    {
        if (isRematch)
            return "又來港灣練習？我還是一樣擅長" + SpecialtyTagZh + "搶節奏——別被開場壓制喔。";
        return "我是" + DisplayName + "。擅長" + SpecialtyTagZh + "搶節奏——等下牌桌上別被開場壓制喔。";
    }

    /// <summary>立繪 B：鬥鳥後、正式對戰前迷你對話（港灣短版）。</summary>
    public string ResolvePortraitBLine(bool isRematch, BirdDuelResult duelResult)
    {
        switch (duelResult)
        {
            case BirdDuelResult.Win:
                return "剛才算你讀懂節奏，牌局可不一樣。";
            case BirdDuelResult.Draw:
                return "剛才平手，這次換牌桌見真章。";
            case BirdDuelResult.Lose:
                return "剛才讓你過關，這次可不一樣。";
            default:
                if (isRematch)
                    return "又遇見我啦，這一次我可不會手下留情喔。";
                return "第一次在牌桌上對上我？可別大意喔。";
        }
    }

    /// <summary>v1 港灣敵方：熱血同學（快攻 × 啄擊節奏）。</summary>
    public static EnemyHeroProfile CreateHotBloodClassmate()
    {
        var npc = new BirdDuelNpcProfile
        {
            displayName = "熱血同學",
            introLine = "先聽鼓點，再看我的動作。先拿十四分就算你贏！",
            winLine = "可惡，你把我的節奏全看穿了！",
            drawLine = "平手？算你跟得上我的拍子。",
            loseLine = "哈，你太急著出手啦，再來一次！",
            passLimit = BirdDuelCore.DefaultPassLimit,
            winThreshold = 14,
            drawThreshold = 8,
            beatPattern = new[]
            {
                BirdGesture.Nest,
                BirdGesture.Peck,
                BirdGesture.Wing,
                BirdGesture.Peck,
                BirdGesture.Nest,
                BirdGesture.Wing,
                BirdGesture.Peck,
                BirdGesture.Nest,
                BirdGesture.Wing,
                BirdGesture.Peck,
                BirdGesture.Nest,
                BirdGesture.Wing,
            },
        };

        return new EnemyHeroProfile(
            EnemyHeroCatalog.HotBloodClassmateId,
            npc.displayName,
            "快攻",
            npc);
    }
}

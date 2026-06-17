using System.Collections.Generic;

/// <summary>
/// 鬥鳥對手設定：固定鼓點鳥勢序列、台詞，以及依看破層級揭露的戰前情報。
/// v1 港灣敵方由 <see cref="EnemyHeroCatalog"/>／<see cref="EnemyHeroProfile"/> 組裝；本類保留鬥鳥局內欄位。
/// </summary>
public sealed class BirdDuelNpcProfile
{
    public string displayName = "熱血同學";
    public string introLine = "先聽鼓點，再看我的動作。先拿十四分就算你贏！";
    public string winLine = "可惡，你把我的節奏全看穿了！";
    public string drawLine = "平手？算你跟得上我的拍子。";
    public string loseLine = "哈，你太急著出手啦，再來一次！";

    public int passLimit = BirdDuelCore.DefaultPassLimit;
    // 12 拍版：完美得分 20、看破 4；勝 >=14、平 8~13、敗 <=7。
    public int winThreshold = 14;
    public int drawThreshold = 8;

    /// <summary>
    /// 對手鳥勢序列（12 拍，較長的暖身賽）。巢→啄(+3)、啄→翅(+2)、翅→巢(看破)循環。
    /// 完美得分 20、看破 4，足以勝利並取得進階情報。
    /// </summary>
    public IReadOnlyList<BirdGesture> beatPattern = new[]
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
    };

    /// <summary>
    /// 依最終情報層級回傳戰前情報文字。層級 0 為基本提示，3 為具體建議。
    /// </summary>
    public string ResolveIntelText(int tier)
    {
        switch (tier)
        {
            case 0:
                return "戰前情報：只看出對手節奏偏快。";
            case 1:
                return "戰前情報：對手偏好啄擊，正式戰鬥可能搶血。";
            case 2:
                return "戰前情報：對手前兩回合可能主動攻擊。";
            default:
                return "戰前情報：對手前兩回合可能主動攻擊，建議保留低費怪獸擋線。";
        }
    }

    public string ResolveResultLine(BirdDuelResult result)
    {
        switch (result)
        {
            case BirdDuelResult.Win: return winLine;
            case BirdDuelResult.Draw: return drawLine;
            default: return loseLine;
        }
    }
}

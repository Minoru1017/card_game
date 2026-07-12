/// <summary>M-1-3 分波鬥鳥對手設定（8 拍；左汊／右汊隱喻）。</summary>
public static class M13BirdDuelNpcProfile
{
    public static BirdDuelNpcProfile Create()
    {
        return new BirdDuelNpcProfile
        {
            displayName = "河岔分波",
            introLine = "聽遠处分波聲 跟鼓點對齊左汊與右汊 節奏對了 路就對了",
            winLine = "……水流與你同頻了",
            drawLine = "還差半拍 再聽一次分波",
            loseLine = "急於求成的人 聽不懂分波",
            passLimit = 1,
            winThreshold = 9,
            drawThreshold = 5,
            beatPattern = BirdDuelRhythmChart.ResolveBeatPattern(
                BirdDuelRhythmChart.RiverForkWaveCdId,
                System.Array.Empty<BirdGesture>()),
        };
    }
}

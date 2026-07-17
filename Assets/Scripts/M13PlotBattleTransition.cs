using UnityEngine;

/// <summary>M-1-3 玫瑰試煉結束後載入分波對決。</summary>
public static class M13PlotBattleTransition
{
    public static void PlayFromPlotToPhaseBBattle(bool fastCloseAnimation = false)
    {
        PlotBattleTransitionHost.PlayAsyncBattleLoad(
            SceneLoader.PrepareM13PhaseBBattleLaunch,
            nameof(M13PlotBattleTransition));
    }
}

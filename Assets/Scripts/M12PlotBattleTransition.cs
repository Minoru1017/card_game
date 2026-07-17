using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>M-1-2 劇情結束後載入段考對戰場景。</summary>
public static class M12PlotBattleTransition
{
    public static void PlayFromPlotToPhaseABattle(bool fastCloseAnimation = false)
    {
        PlotBattleTransitionHost.PlayAsyncBattleLoad(
            SceneLoader.PrepareM12PhaseABattleLaunch,
            nameof(M12PlotBattleTransition));
    }

    public static void PlayFromPlotToPhaseBBattle(bool fastCloseAnimation = false)
    {
        PlotBattleTransitionHost.PlayAsyncBattleLoad(
            SceneLoader.PrepareM12PhaseBBattleLaunch,
            nameof(M12PlotBattleTransition));
    }
}

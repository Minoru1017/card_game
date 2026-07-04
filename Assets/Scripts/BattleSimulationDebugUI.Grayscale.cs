using System.Collections;
using UnityEngine;

public partial class BattleSimulationDebugUI
{
    private Coroutine lethalGrayscaleFinalizeRoutine;

    private void BindLethalGrayscaleFx()
    {
        BattleUiGrayscaleFx.SetCoroutineHost(this);
        LethalBlowCinematicFx.SlowMotionBegan -= OnLethalSlowMotionBegan;
        LethalBlowCinematicFx.SlowMotionEnded -= OnLethalSlowMotionEnded;
        LethalBlowCinematicFx.SlowMotionBegan += OnLethalSlowMotionBegan;
        LethalBlowCinematicFx.SlowMotionEnded += OnLethalSlowMotionEnded;
    }

    private void UnbindLethalGrayscaleFx()
    {
        LethalBlowCinematicFx.SlowMotionBegan -= OnLethalSlowMotionBegan;
        LethalBlowCinematicFx.SlowMotionEnded -= OnLethalSlowMotionEnded;
        if (lethalGrayscaleFinalizeRoutine != null)
        {
            StopCoroutine(lethalGrayscaleFinalizeRoutine);
            lethalGrayscaleFinalizeRoutine = null;
        }
        BattleUiGrayscaleFx.Release();
    }

    private void OnLethalSlowMotionBegan()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        BattleUiGrayscaleFx.BeginGradualRamp();
    }

    private void OnLethalSlowMotionEnded()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (lethalGrayscaleFinalizeRoutine != null)
            StopCoroutine(lethalGrayscaleFinalizeRoutine);
        lethalGrayscaleFinalizeRoutine = StartCoroutine(CoFinalizeLethalGrayscaleAfterSlowMotion());
    }

    private IEnumerator CoFinalizeLethalGrayscaleAfterSlowMotion()
    {
        yield return null;
        yield return null;

        lethalGrayscaleFinalizeRoutine = null;
        if (battleManager != null && battleManager.IsBattleOver() && battleManager.GetBattleResult() == -1)
            BattleUiGrayscaleFx.HoldFull();
        else
            BattleUiGrayscaleFx.Release();
    }
}

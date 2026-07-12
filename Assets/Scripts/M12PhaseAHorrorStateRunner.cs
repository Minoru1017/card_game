using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>執行 M-1-2 段考恐怖狀態氛圍切換（含白閃光轉場）。</summary>
public sealed class M12PhaseAHorrorStateRunner : MonoBehaviour
{
    private Coroutine refreshRoutine;

    public static M12PhaseAHorrorStateRunner EnsureInActiveBattleScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return null;
        if (!TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(scene.name))
            return null;

        TutorialBattleBackgroundMusicPlayer bgmHost =
            TutorialBattleBackgroundMusicPlayer.EnsureInScene(scene);
        if (bgmHost == null)
            return null;

        M12PhaseAHorrorStateRunner runner = bgmHost.GetComponent<M12PhaseAHorrorStateRunner>();
        if (runner == null)
            runner = bgmHost.gameObject.AddComponent<M12PhaseAHorrorStateRunner>();
        return runner;
    }

    public void QueueAtmosphereRefresh(int currentRound, bool battleOver)
    {
        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);
        refreshRoutine = StartCoroutine(CoRefreshAtmosphere(currentRound, battleOver));
    }

    private IEnumerator CoRefreshAtmosphere(int currentRound, bool battleOver)
    {
        if (BattleAutoSimPlugin.IsRunning)
        {
            M12PhaseAHorrorStateRuntime.SyncAtmosphereStateWithoutPresentation(currentRound, battleOver);
            refreshRoutine = null;
            yield break;
        }

        if (!M12PhaseAHorrorStateRuntime.TryBeginAtmosphereTransition(currentRound, battleOver, out bool enteringHorror))
        {
            refreshRoutine = null;
            yield break;
        }

        if (enteringHorror)
        {
            yield return M12PhaseAHorrorTransitionFx.CoPlayWhiteFlash(
                M12PhaseAHorrorStateRuntime.ApplyHorrorAtmosphereImmediate);
        }
        else
        {
            M12PhaseAHorrorStateRuntime.ApplyNormalAtmosphereImmediate();
        }

        refreshRoutine = null;
    }

    private void OnDestroy()
    {
        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);
        refreshRoutine = null;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>M-1-2 劇情結束後載入段考對戰場景。</summary>
public static class M12PlotBattleTransition
{
    public static void PlayFromPlotToPhaseABattle(bool fastCloseAnimation = false)
    {
        if (TutorialPlotBattleTransition.IsPlaying)
            return;

        M12TransitionHost.Ensure().StartToPhaseA();
    }

    public static void PlayFromPlotToPhaseBBattle(bool fastCloseAnimation = false)
    {
        if (TutorialPlotBattleTransition.IsPlaying)
            return;

        M12TransitionHost.Ensure().StartToPhaseB();
    }

    private sealed class M12TransitionHost : MonoBehaviour
    {
        private static M12TransitionHost host;

        public static M12TransitionHost Ensure()
        {
            if (host != null)
                return host;

            var go = new GameObject(nameof(M12PlotBattleTransition));
            DontDestroyOnLoad(go);
            host = go.AddComponent<M12TransitionHost>();
            return host;
        }

        public void StartToPhaseA() => StartCoroutine(LoadBattle(SceneLoader.PrepareM12PhaseABattleLaunch));

        public void StartToPhaseB() => StartCoroutine(LoadBattle(SceneLoader.PrepareM12PhaseBBattleLaunch));

        private static IEnumerator LoadBattle(System.Func<string, string> prepare)
        {
            PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
            yield return null;

            string scene = prepare(null);
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("M12PlotBattleTransition: scene missing -> " + scene);
                yield break;
            }

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            if (loadOp == null)
                yield break;

            while (!loadOp.isDone)
                yield return null;
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>M-1-3 玫瑰試煉結束後載入分波對決。</summary>
public static class M13PlotBattleTransition
{
    public static void PlayFromPlotToPhaseBBattle(bool fastCloseAnimation = false)
    {
        if (TutorialPlotBattleTransition.IsPlaying)
            return;

        M13TransitionHost.Ensure().StartToPhaseB();
    }

    private sealed class M13TransitionHost : MonoBehaviour
    {
        private static M13TransitionHost host;

        public static M13TransitionHost Ensure()
        {
            if (host != null)
                return host;

            var go = new GameObject(nameof(M13PlotBattleTransition));
            DontDestroyOnLoad(go);
            host = go.AddComponent<M13TransitionHost>();
            return host;
        }

        public void StartToPhaseB() => StartCoroutine(LoadBattle(SceneLoader.PrepareM13PhaseBBattleLaunch));

        private static IEnumerator LoadBattle(System.Func<string, string> prepare)
        {
            PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
            yield return null;

            string scene = prepare(null);
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("M13PlotBattleTransition: scene missing -> " + scene);
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

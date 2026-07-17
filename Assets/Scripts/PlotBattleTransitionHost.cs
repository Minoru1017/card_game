using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>M-1-2 / M-1-3 劇情結束後載入對戰場景的共用 async host（1-1 仍用光圈過場）。</summary>
public static class PlotBattleTransitionHost
{
    private static PlotBattleTransitionRunner runner;

    public static bool IsLoading =>
        runner != null && runner.IsRunning;

    public static void PlayAsyncBattleLoad(Func<string, string> prepareLaunch, string logTag)
    {
        if (TutorialPlotBattleTransition.IsPlaying || IsLoading)
            return;

        EnsureRunner().StartLoad(prepareLaunch, logTag);
    }

    private static PlotBattleTransitionRunner EnsureRunner()
    {
        if (runner != null)
            return runner;

        var go = new GameObject(nameof(PlotBattleTransitionHost));
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<PlotBattleTransitionRunner>();
        return runner;
    }

    private sealed class PlotBattleTransitionRunner : MonoBehaviour
    {
        public bool IsRunning { get; private set; }

        public void StartLoad(Func<string, string> prepareLaunch, string logTag) =>
            StartCoroutine(LoadBattle(prepareLaunch, logTag));

        private IEnumerator LoadBattle(Func<string, string> prepareLaunch, string logTag)
        {
            IsRunning = true;
            try
            {
                PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
                yield return null;

                string scene = prepareLaunch(null);
                if (!Application.CanStreamedLevelBeLoaded(scene))
                {
                    Debug.LogError(logTag + ": scene missing -> " + scene);
                    yield break;
                }

                AsyncOperation loadOp = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
                if (loadOp == null)
                    yield break;

                while (!loadOp.isDone)
                    yield return null;
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}

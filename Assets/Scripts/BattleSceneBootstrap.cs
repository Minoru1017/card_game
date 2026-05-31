using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "BattleSimulation") return;

        // Force-fix all canvas transforms and states.
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            c.gameObject.SetActive(true);
            c.enabled = true;
            c.transform.localScale = Vector3.one;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.enabled = true;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographic = true;
        }

        BattleSimulationManager manager = Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager != null)
        {
            manager.ApplyLaunchContextDifficulty();
            manager.TryApplyLaunchDifficultyFromContext();
            manager.CaptureBattleDifficultyForRecords();
        }

        HarborTrainingBattleBackground.ApplyForActiveBattleContext();
    }
}

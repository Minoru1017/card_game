using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>清除 Main Plot 動態建立的點擊繼續 UI，避免殘留到其他場景。</summary>
public static class PlotUiOverlayCleanup
{
    /// <summary>過場開始後關閉指定場景 Camera／Canvas，避免與全螢幕遮罩雙重渲染造成卡頓。</summary>
    public static void SuppressSceneRendering(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null) continue;

            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                    cameras[i].enabled = false;
            }

            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                    canvases[i].enabled = false;
            }
        }
    }

    /// <summary>關閉除 Story progress 以外已載入場景的渲染（對戰／劇情回傳用）。</summary>
    public static void SuppressLoadedScenesExceptStoryProgress()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            if (scene.name == StoryProgressSession.StoryProgressSceneName) continue;
            SuppressSceneRendering(scene.name);
        }
    }

    public static void SuppressMainPlotSceneRendering() =>
        SuppressSceneRendering(StoryProgressSession.MainPlotSceneName);

    public static void DestroyStrayPlotTapUi()
    {
        DestroyByName("PlotTapToContinue");
        DestroyByName("PlotTapContinueHint");
        TutorialPlotStarterDeckNotify.DismissExisting();
        DestroyByName("TutorialPlotStarterDeckNotifyCanvas");

        TMP_Text[] allTmp = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < allTmp.Length; i++)
        {
            TMP_Text tmp = allTmp[i];
            if (tmp == null) continue;
            string n = tmp.gameObject.name;
            if (n == "PlotTapContinueHint" || n == "PlotTapToContinue")
                Object.Destroy(tmp.gameObject);
        }
    }

    private static void DestroyByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null)
            Object.Destroy(go);
    }
}

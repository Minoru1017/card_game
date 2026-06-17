using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>CardStore 場景：雙池版面、開包按鈕觸控修正、全局導覽。</summary>
public static class CardStoreSceneUi
{
    private const string SceneName = "CardStore";
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        ApplyNow(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyNow(scene);
    }

    public static void ApplyNow(Scene scene = default)
    {
        Scene target = scene.IsValid() ? scene : SceneManager.GetActiveScene();
        if (!target.IsValid() || target.name != SceneName)
            return;

        EnsureEventSystem();
        CardStoreGachaLayoutUi.EnsureLayout();
        DisableVideoScreenRaycastsOnly(target);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    private static void DisableVideoScreenRaycastsOnly(Scene scene)
    {
        GameObject screen = SceneSearchUtil.FindSceneObject(scene, "Screen");
        if (screen == null)
            return;

        RawImage raw = screen.GetComponent<RawImage>();
        if (raw != null)
            raw.raycastTarget = false;
    }
}

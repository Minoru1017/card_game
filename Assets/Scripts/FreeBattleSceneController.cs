using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>自由對戰場景：三種 AI 按鈕 → 帶 AI 參數進 Buildbeck 組牌。</summary>
public static class FreeBattleSceneController
{
    private const string BuildbeckSceneName = "Buildbeck";
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindFreeBattleFeatures()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        TryBindForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindForScene(scene);
    }

    private static void TryBindForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != FreeBattleBattleCopy.SceneName)
            return;

        TryBindAiButton("綜合型", EnemyAiPlayStyle.Balanced);
        TryBindAiButton("快攻型", EnemyAiPlayStyle.FastAttack);
        TryBindAiButton("防禦型", EnemyAiPlayStyle.Defensive);
        GlobalNavRuntime.RefreshActiveSceneNav();
    }

    private static void TryBindAiButton(string objName, EnemyAiPlayStyle aiStyle)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;

        Button btn = EnsureButton(go);
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => LoadBuildbeckWithAiStyle(aiStyle));
    }

    private static void LoadBuildbeckWithAiStyle(EnemyAiPlayStyle aiStyle)
    {
        FreeBattleViewSession.Begin(aiStyle);
        if (Application.CanStreamedLevelBeLoaded(BuildbeckSceneName))
            SceneManager.LoadScene(BuildbeckSceneName);
        else
            Debug.LogError("FreeBattleSceneController: Buildbeck scene not in Build Settings.");
    }

    private static Button EnsureButton(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }
}

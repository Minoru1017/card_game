using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneLoader
{
    private const string DefaultTutorialBattleScene = "BattleSimulation";

    private void ConfigureIntroTutorialBattlePending()
    {
        selectedDifficultyTier = BattleDifficultyTier.Intro;
        BattleDifficultyConfig cfg = BuildDifficultyConfig(BattleDifficultyTier.Intro);
        pendingUseFixedEnemyDeck = true;
        pendingFixedEnemyDeckCardIds = IntroTutorialBattleRules.WeakEnemyDeckCardIds;
        pendingEnemyOverLimitAllowance = 0;
        pendingMinEnemySpellsInDeck = Mathf.Min(cfg.MinSpellsInDeck, 2);
        pendingEnemyAiPlayStyle = EnemyAiPlayStyle.IntroGreedy;
        pendingDifficultyLabelZh = "入門級";
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        BattleLaunchContext.BeginIntroTutorialBattleLaunch();
        if (PlayerData.ResolveCanonical() != null)
            TutorialDeckApplicator.EnsureIntroTutorialDeckReady();
    }

    public void PushIntroTutorialConfigToManager(BattleSimulationManager manager)
    {
        ConfigureIntroTutorialBattlePending();
        if (manager == null)
            return;

        manager.QueueRuntimeDifficultyConfig(
            pendingUseFixedEnemyDeck,
            pendingFixedEnemyDeckCardIds,
            pendingEnemyOverLimitAllowance,
            pendingMinEnemySpellsInDeck,
            pendingEnemyAiPlayStyle,
            pendingDifficultyLabelZh);
    }

    public static void ApplyIntroTutorialRuntimeConfigToManager(BattleSimulationManager manager)
    {
        if (manager == null || !BattleLaunchContext.IsIntroTutorialBattle)
            return;

        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("TutorialBattleSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.PushIntroTutorialConfigToManager(manager);
    }

    /// <summary>設定 1-1 教學對戰 pending 設定並回傳目標場景名稱。</summary>
    public static string PrepareIntroTutorialBattleLaunch(string targetBattleScene = null)
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("TutorialBattleSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureIntroTutorialBattlePending();
        string scene = ResolveIntroTutorialBattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    private static string ResolveIntroTutorialBattleSceneName(SceneLoader loader, string targetBattleScene)
    {
        if (!string.IsNullOrWhiteSpace(targetBattleScene))
            return targetBattleScene;
        if (loader != null && !string.IsNullOrWhiteSpace(loader.battleSceneName))
            return loader.battleSceneName;
        return DefaultTutorialBattleScene;
    }

    /// <summary>Skip battle preview; launch intro tutorial battle (no weather, IntroGreedy AI).</summary>
    public void LaunchIntroTutorialBattleDirect(string targetBattleScene = null)
    {
        string scene = PrepareIntroTutorialBattleLaunch(targetBattleScene);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("SceneLoader: tutorial battle scene not in Build Settings -> " + scene);
            return;
        }

        StartBattleSceneLoad();
    }

    public static void LaunchIntroTutorialBattleFromAnywhere(string battleSceneName = null)
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("TutorialBattleSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }
        loader.LaunchIntroTutorialBattleDirect(battleSceneName);
    }
}

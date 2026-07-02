using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneLoader
{
    private const string DefaultM12BattleScene = "BattleSimulation";

    public static void LaunchM12PhaseABattleDirect(string targetBattleScene = null)
    {
        SceneLoader loader = ResolveSceneLoaderForActiveScene();
        loader.LaunchM12PhaseABattleInternal(targetBattleScene);
    }

    public static void LaunchM12PhaseBBattleDirect(string targetBattleScene = null)
    {
        SceneLoader loader = ResolveSceneLoaderForActiveScene();
        loader.LaunchM12PhaseBBattleInternal(targetBattleScene);
    }

    public static string PrepareM12PhaseABattleLaunch(string targetBattleScene = null)
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("M12SceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureM12PhaseABattlePending();
        string scene = ResolveM12BattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    public static string PrepareM12PhaseBBattleLaunch(string targetBattleScene = null)
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("M12SceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureM12PhaseBBattlePending();
        string scene = ResolveM12BattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    private void LaunchM12PhaseABattleInternal(string targetBattleScene = null)
    {
        string scene = PrepareM12PhaseABattleLaunch(targetBattleScene);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("SceneLoader: M-1-2 phase A scene not in Build Settings -> " + scene);
            return;
        }

        StartBattleSceneLoad();
    }

    private void LaunchM12PhaseBBattleInternal(string targetBattleScene = null)
    {
        string scene = PrepareM12PhaseBBattleLaunch(targetBattleScene);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("SceneLoader: M-1-2 phase B scene not in Build Settings -> " + scene);
            return;
        }

        StartBattleSceneLoad();
    }

    private void ConfigureM12PhaseABattlePending()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        M12PhaseDeckApplicator.ApplyPhaseADeck(playerData);

        selectedDifficultyTier = BattleDifficultyTier.Intro;
        pendingUseFixedEnemyDeck = true;
        pendingFixedEnemyDeckCardIds = M12PhaseABattleRules.EnemyDeckCardIds;
        pendingEnemyOverLimitAllowance = 0;
        pendingMinEnemySpellsInDeck = 1;
        pendingEnemyAiPlayStyle = EnemyAiPlayStyle.Balanced;
        pendingDifficultyLabelZh = "段考A";
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        BattleLaunchContext.BeginM12TrioTutorialBattleLaunch();
        EnemyHeroProfile hero = EnemyHeroCatalog.ResolveForHarbor();
        BattleLaunchContext.SetEnemyHero(hero.HeroId, hero.DisplayName);
    }

    private void ConfigureM12PhaseBBattlePending()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        M12PhaseDeckApplicator.ApplyPhaseBDeck(playerData);

        HarborTrainingTierConfig harbor = HarborTrainingDifficultyRuntime.ResolvePendingConfig(BattleDifficultyTier.Easy);
        pendingUseFixedEnemyDeck = true;
        pendingFixedEnemyDeckCardIds = harbor.FixedEnemyDeckCardIds;
        pendingEnemyOverLimitAllowance = harbor.EnemyOverLimitAllowance;
        pendingMinEnemySpellsInDeck = harbor.MinEnemySpellsInDeck;
        pendingEnemyAiPlayStyle = harbor.AiPlayStyle;
        pendingDifficultyLabelZh = harbor.LabelZh;
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        BattleLaunchContext.BeginM12CoachPracticeBattleLaunch();
        EnemyHeroProfile hero = EnemyHeroCatalog.ResolveForHarbor();
        BattleLaunchContext.SetEnemyHero(hero.HeroId, hero.DisplayName);
    }

    public static void ApplyM12RuntimeConfigToManager(BattleSimulationManager manager)
    {
        if (manager == null || !BattleLaunchContext.IsM12TrioMasteryBattle)
            return;

        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("M12SceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        if (BattleLaunchContext.IsM12TrioTutorialBattle)
        {
            loader.ConfigureM12PhaseABattlePending();
            manager.QueueRuntimeDifficultyConfig(
                true,
                M12PhaseABattleRules.EnemyDeckCardIds,
                0,
                1,
                EnemyAiPlayStyle.Balanced,
                "段考A");
            return;
        }

        loader.ConfigureM12PhaseBBattlePending();
        loader.PushHarborTrainingConfigToManager(manager);
    }

    private static string ResolveM12BattleSceneName(SceneLoader loader, string targetBattleScene)
    {
        if (!string.IsNullOrWhiteSpace(targetBattleScene))
            return targetBattleScene;
        if (loader != null && !string.IsNullOrWhiteSpace(loader.battleSceneName))
            return loader.battleSceneName;
        return DefaultM12BattleScene;
    }
}

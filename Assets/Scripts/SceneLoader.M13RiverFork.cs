using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneLoader
{
    private const string DefaultM13BattleScene = "BattleSimulation";

    /// <summary>主線 M-1-3 分波鬥鳥（強制 Maracuja BGM；無 roguelike draft）。</summary>
    public static void LaunchM13RiverForkBirdDuel()
    {
        M13StoryDuelContext.Begin();

        if (!Application.CanStreamedLevelBeLoaded(BirdDuelSceneName))
        {
            Debug.LogError("SceneLoader: bird duel scene not in Build Settings -> " + BirdDuelSceneName);
            M13StoryDuelContext.Clear();
            return;
        }

        StoryProgressBackgroundMusicPlayer.StopAll();
        SceneManager.LoadScene(BirdDuelSceneName);
    }

    public static void LaunchM13PhaseABattleDirect(string targetBattleScene = null)
    {
        SceneLoader loader = ResolveSceneLoaderForActiveScene();
        loader.LaunchM13PhaseABattleInternal(targetBattleScene);
    }

    public static void LaunchM13PhaseBBattleDirect(string targetBattleScene = null)
    {
        SceneLoader loader = ResolveSceneLoaderForActiveScene();
        loader.LaunchM13PhaseBBattleInternal(targetBattleScene);
    }

    public static string PrepareM13PhaseBBattleLaunch(string targetBattleScene = null)
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("M13SceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureM13PhaseBBattlePending();
        string scene = ResolveM13BattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    public static string PeekM13BattleSceneName()
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        return ResolveM13BattleSceneName(loader, null);
    }

    public static string PrepareM13PhaseABattleLaunch(string targetBattleScene = null)
    {
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("M13SceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureM13PhaseABattlePending();
        string scene = ResolveM13BattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    public static void ApplyM13RuntimeConfigToManager(BattleSimulationManager manager)
    {
        if (manager == null)
            return;

        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("M13SceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        if (BattleLaunchContext.IsM13WeatherTutorialBattle)
        {
            loader.ConfigureM13PhaseABattlePending();
            manager.QueueRuntimeDifficultyConfig(
                true,
                M13PhaseABattleRules.EnemyDeckCardIds,
                HarborTrainingEasyBattleRules.EnemyOverLimitAllowance,
                HarborTrainingEasyBattleRules.MinEnemySpellsInDeck,
                EnemyAiPlayStyle.Balanced,
                "冷爐迎測");
            return;
        }

        if (BattleLaunchContext.IsM13RivalDuelBattle)
        {
            loader.ConfigureM13PhaseBBattlePending();
            HarborTrainingTierConfig harbor =
                HarborTrainingDifficultyRuntime.ResolvePendingConfig(BattleDifficultyTier.Normal);
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            manager.QueueRuntimeDifficultyConfig(
                true,
                harbor.FixedEnemyDeckCardIds,
                harbor.EnemyOverLimitAllowance,
                harbor.MinEnemySpellsInDeck,
                M13PhaseBBattleRules.ResolveEnemyAiStyle(slot),
                "分波對決");
        }
    }

    /// <summary>跳過分波鬥鳥（直接迎測）；不發 S 評獎勵。</summary>
    public static void SkipM13RiverForkBirdDuel()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M13RiverForkProgressState.MarkBirdDuelSkipped(slot);
        LoadStoryProgressAfterM13BirdDuel();
    }

    /// <summary>分波鬥鳥結算後寫入進度並回大地圖。</summary>
    public static void CompleteM13RiverForkBirdDuel(int score, BirdDuelResult result)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        bool sRank = M13BirdDuelGrading.IsSRank(score, result);
        M13RiverForkProgressState.MarkBirdDuelPlayed(slot, sRank);
        LoadStoryProgressAfterM13BirdDuel();
    }

    private void LaunchM13PhaseABattleInternal(string targetBattleScene = null)
    {
        string scene = PrepareM13PhaseABattleLaunch(targetBattleScene);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("SceneLoader: M-1-3 phase A scene not in Build Settings -> " + scene);
            return;
        }

        StartBattleSceneLoad();
    }

    private void LaunchM13PhaseBBattleInternal(string targetBattleScene = null)
    {
        string scene = PrepareM13PhaseBBattleLaunch(targetBattleScene);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("SceneLoader: M-1-3 phase B scene not in Build Settings -> " + scene);
            return;
        }

        StartBattleSceneLoad();
    }

    private void ConfigureM13PhaseABattlePending()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        M13PhaseDeckApplicator.ApplyPhaseADeck(playerData);

        selectedDifficultyTier = BattleDifficultyTier.Intro;
        pendingUseFixedEnemyDeck = true;
        pendingFixedEnemyDeckCardIds = M13PhaseABattleRules.EnemyDeckCardIds;
        pendingEnemyOverLimitAllowance = HarborTrainingEasyBattleRules.EnemyOverLimitAllowance;
        pendingMinEnemySpellsInDeck = HarborTrainingEasyBattleRules.MinEnemySpellsInDeck;
        pendingEnemyAiPlayStyle = EnemyAiPlayStyle.Balanced;
        pendingDifficultyLabelZh = "冷爐迎測";
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        BattleLaunchContext.BeginM13WeatherTutorialBattleLaunch();
        BattleLaunchContext.SetEnemyHero(null, "木樁");
        ApplyM13OpeningWeatherPendingFlags();
    }

    private void ConfigureM13PhaseBBattlePending()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        M13PhaseDeckApplicator.ApplyPhaseBDeck(playerData);

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        HarborTrainingTierConfig harbor =
            HarborTrainingDifficultyRuntime.ResolvePendingConfig(BattleDifficultyTier.Normal);
        selectedDifficultyTier = BattleDifficultyTier.Normal;
        pendingUseFixedEnemyDeck = true;
        pendingFixedEnemyDeckCardIds = harbor.FixedEnemyDeckCardIds;
        pendingEnemyOverLimitAllowance = harbor.EnemyOverLimitAllowance;
        pendingMinEnemySpellsInDeck = harbor.MinEnemySpellsInDeck;
        pendingEnemyAiPlayStyle = M13PhaseBBattleRules.ResolveEnemyAiStyle(slot);
        pendingDifficultyLabelZh = "分波對決";
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        BattleLaunchContext.BeginM13RivalDuelBattleLaunch();
        BattleLaunchContext.SetEnemyHero(
            EnemyHeroCatalog.HotBloodClassmateId,
            "阿潮");
    }

    private static void ApplyM13OpeningWeatherPendingFlags()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M13OpeningWeatherPick pick = M13RiverForkProgressState.GetOpeningWeatherPick(slot);
        if (pick == M13OpeningWeatherPick.DefaultFog || pick == M13OpeningWeatherPick.Fog)
            M13WeatherLaunchRuntime.SetFirstWeatherFog();
        else if (pick == M13OpeningWeatherPick.FireRain)
            M13WeatherLaunchRuntime.SetFirstWeatherFireRain();
        else if (pick == M13OpeningWeatherPick.HolyLight)
            M13WeatherLaunchRuntime.SetFirstWeatherHolyLight();
        else
            M13WeatherLaunchRuntime.SetFirstWeatherFog();
    }

    private static string ResolveM13BattleSceneName(SceneLoader loader, string targetBattleScene)
    {
        if (!string.IsNullOrWhiteSpace(targetBattleScene))
            return targetBattleScene;
        if (loader != null && !string.IsNullOrWhiteSpace(loader.battleSceneName))
            return loader.battleSceneName;
        return DefaultM13BattleScene;
    }

    private static void LoadStoryProgressAfterM13BirdDuel()
    {
        M13StoryDuelContext.Clear();
        PreBattleCdContext.Clear();
        PreBattleBonusContext.Clear();
        StoryProgressSession.QueueM13ContinueAfterBirdDuel();

        if (Application.CanStreamedLevelBeLoaded(StoryProgressSession.StoryProgressSceneName))
            SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
        else
            Debug.LogWarning("SceneLoader: Story progress scene not in Build Settings.");
    }
}
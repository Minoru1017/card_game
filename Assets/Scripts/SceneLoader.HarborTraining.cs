using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneLoader
{
    private const string DefaultHarborBattleScene = "BattleSimulation";

    /// <summary>Story progress：開啟港灣訓練場戰前預覽（簡單／普通／困難）。</summary>
    public static void OpenHarborTrainingBattlePreviewFromStoryProgress()
    {
        ResolveSceneLoaderForActiveScene().ShowHarborTrainingBattlePreview();
    }

    private static SceneLoader ResolveSceneLoaderForActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneLoader[] loaders = UnityEngine.Object.FindObjectsByType<SceneLoader>(FindObjectsSortMode.None);
        SceneLoader bestInActive = null;
        int bestSortOrder = int.MinValue;
        for (int i = 0; i < loaders.Length; i++)
        {
            SceneLoader candidate = loaders[i];
            if (candidate == null) continue;
            if (!activeScene.IsValid() || candidate.gameObject.scene != activeScene) continue;

            Canvas canvas = candidate.GetComponentInChildren<Canvas>(true);
            int sortOrder = canvas != null ? canvas.sortingOrder : 0;
            if (bestInActive == null || sortOrder >= bestSortOrder)
            {
                bestInActive = candidate;
                bestSortOrder = sortOrder;
            }
        }

        if (bestInActive != null)
            return bestInActive;

        GameObject host = new GameObject("HarborTrainingSceneLoader");
        if (activeScene.IsValid())
            SceneManager.MoveGameObjectToScene(host, activeScene);
        return host.AddComponent<SceneLoader>();
    }

    public void ShowHarborTrainingBattlePreview()
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (playerData != null) playerData.LoadPlayerData();

        try
        {
            TutorialDeckApplicator.EnsureIntroTutorialDeckReady(playerData);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        if (playerData != null) playerData.LoadPlayerData();
        RefreshEnterBattleState(false);
        if (!HasBuiltDeck())
        {
            ShowBattlePreviewBlockedMessage(NoDeckHintMessage);
            return;
        }

        ShowNoDeckHint(false);
        battlePreviewHarborTrainingMode = true;
        battlePreviewActivePuzzleId = BattlePreviewPuzzleIndex.HarborTrainingGround;
        battlePreviewBossTierUnlocked = false;
        battlePreviewFeedbackDifficultyTier = null;
        selectedDifficultyTier = BattleDifficultyTier.Easy;

        EnsureBattlePreviewUi();
        if (battlePreviewUsesAuthoredPuzzleLayout)
        {
            ApplyAuthoredPreviewInitialVisibility();
            SyncAuthoredArchRowLayout();
        }

        RefreshBattlePreviewBodyText();
        RefreshAuthoredDifficultyAreaVisibility();
        if (battlePreviewOverlayRoot != null)
        {
            battlePreviewOverlayRoot.transform.SetAsLastSibling();
            battlePreviewOverlayRoot.SetActive(true);
            RefreshBattlePreviewTextScrollLayout();
            return;
        }

        Debug.LogError("SceneLoader: harbor training battle preview overlay failed to build.");
    }

    private void ApplyHarborTrainingPendingConfig(BattleDifficultyTier tier)
    {
        BattleDifficultyConfig cfg = BuildDifficultyConfig(tier);
        if (tier == BattleDifficultyTier.Easy)
        {
            pendingUseFixedEnemyDeck = true;
            pendingFixedEnemyDeckCardIds = HarborTrainingEasyBattleRules.EasyEnemyDeckCardIds;
            pendingEnemyOverLimitAllowance = 2;
            pendingMinEnemySpellsInDeck = 1;
            pendingEnemyAiPlayStyle = EnemyAiPlayStyle.FastAttack;
            pendingDifficultyLabelZh = "簡單";
            return;
        }

        if (tier == BattleDifficultyTier.Normal)
        {
            pendingUseFixedEnemyDeck = true;
            pendingFixedEnemyDeckCardIds = HarborTrainingNormalBattleRules.NormalEnemyDeckCardIds;
            pendingEnemyOverLimitAllowance = 2;
            pendingMinEnemySpellsInDeck = 1;
            pendingEnemyAiPlayStyle = EnemyAiPlayStyle.FastAttack;
            pendingDifficultyLabelZh = "普通";
            return;
        }

        pendingUseFixedEnemyDeck = cfg.UseFixedDeck;
        pendingFixedEnemyDeckCardIds = cfg.FixedDeckIds;
        pendingEnemyOverLimitAllowance = cfg.OverLimitAllowance;
        pendingMinEnemySpellsInDeck = cfg.MinSpellsInDeck;
        pendingEnemyAiPlayStyle = EnemyAiPlayStyle.FastAttack;
        pendingDifficultyLabelZh = cfg.LabelZh;
    }

    private void ConfigureHarborTrainingBattlePending(BattleDifficultyTier tier)
    {
        ApplyHarborTrainingPendingConfig(tier);
        BattleLaunchContext.SetPendingDifficultyLabelZh(pendingDifficultyLabelZh);
        BattleLaunchContext.BeginHarborTrainingGroundBattleLaunch();
    }

    public static string PrepareHarborTrainingBattleLaunch(BattleDifficultyTier tier, string targetBattleScene = null)
    {
        SceneLoader loader = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("HarborTrainingSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureHarborTrainingBattlePending(tier);
        string scene = ResolveHarborTrainingBattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    private static string ResolveHarborTrainingBattleSceneName(SceneLoader loader, string targetBattleScene)
    {
        if (!string.IsNullOrWhiteSpace(targetBattleScene))
            return targetBattleScene;
        if (loader != null && !string.IsNullOrWhiteSpace(loader.battleSceneName))
            return loader.battleSceneName;
        return DefaultHarborBattleScene;
    }

    public void LaunchHarborTrainingBattleDirect(BattleDifficultyTier tier, string targetBattleScene = null)
    {
        string scene = PrepareHarborTrainingBattleLaunch(tier, targetBattleScene);
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("SceneLoader: harbor training battle scene not in Build Settings -> " + scene);
            return;
        }

        StartBattleSceneLoad();
    }

    public void PushHarborTrainingConfigToManager(BattleSimulationManager manager)
    {
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

    public static void ApplyHarborTrainingRuntimeConfigToManager(BattleSimulationManager manager)
    {
        if (manager == null || !BattleLaunchContext.IsHarborTrainingGroundBattle)
            return;

        BattleDifficultyTier tier = HarborTrainingBattleCopy.TierFromLabelZh(
            BattleLaunchContext.ResolveForBattleRecord());
        SceneLoader loader = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("HarborTrainingSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureHarborTrainingBattlePending(tier);
        loader.PushHarborTrainingConfigToManager(manager);
    }
}

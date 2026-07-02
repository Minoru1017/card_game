using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneLoader
{
    private const string DefaultFreeBattleScene = "BattleSimulation";

    private bool battlePreviewFreeBattleMode;
    private EnemyAiPlayStyle battlePreviewFreeBattleAiStyle = EnemyAiPlayStyle.Balanced;

    /// <summary>自由對戰：依 AI 風格開啟難易度選擇預覽。</summary>
    public static void OpenFreeBattlePreview(EnemyAiPlayStyle aiStyle)
    {
        ResolveSceneLoaderForActiveScene().ShowFreeBattlePreview(aiStyle);
    }

    public void ShowFreeBattlePreview(EnemyAiPlayStyle aiStyle)
    {
        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (playerData != null) playerData.LoadPlayerData();

        RefreshEnterBattleState(false);
        if (!HasBuiltDeck())
        {
            ShowBattlePreviewBlockedMessage(NoDeckHintMessage);
            return;
        }

        ShowNoDeckHint(false);
        battlePreviewHarborTrainingMode = false;
        battlePreviewFreeBattleMode = true;
        battlePreviewFreeBattleAiStyle = aiStyle;
        battlePreviewActivePuzzleId = BattlePreviewPuzzleIndex.FreeBattleGround;
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

        Debug.LogError("SceneLoader: free battle preview overlay failed to build.");
    }

    private void ConfigureFreeBattleBattlePending(BattleDifficultyTier tier, EnemyAiPlayStyle aiStyle)
    {
        BattleDifficultyConfig cfg = BuildDifficultyConfig(tier);
        pendingUseFixedEnemyDeck = cfg.UseFixedDeck;
        pendingFixedEnemyDeckCardIds = cfg.FixedDeckIds;
        pendingEnemyOverLimitAllowance = cfg.OverLimitAllowance;
        pendingMinEnemySpellsInDeck = cfg.MinSpellsInDeck;
        pendingEnemyAiPlayStyle = aiStyle;
        pendingDifficultyLabelZh = cfg.LabelZh;
        BattleLaunchContext.SetPendingDifficultyLabelZh(cfg.LabelZh);
        BattleLaunchContext.BeginFreeBattleLaunch(aiStyle);
        BattleLaunchContext.SetEnemyHero(
            EnemyHeroCatalog.HotBloodClassmateId,
            FreeBattleBattleCopy.GetEnemyHeroDisplayName(aiStyle));
        FreeBattleViewSession.Clear();
    }

    public static string PrepareFreeBattleLaunch(BattleDifficultyTier tier, EnemyAiPlayStyle aiStyle, string targetBattleScene = null)
    {
        SceneLoader loader = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("FreeBattleSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureFreeBattleBattlePending(tier, aiStyle);
        string scene = ResolveFreeBattleSceneName(loader, targetBattleScene);
        loader.battleSceneName = scene;
        return scene;
    }

    private static string ResolveFreeBattleSceneName(SceneLoader loader, string targetBattleScene)
    {
        if (!string.IsNullOrWhiteSpace(targetBattleScene))
            return targetBattleScene;
        if (loader != null && !string.IsNullOrWhiteSpace(loader.battleSceneName))
            return loader.battleSceneName;
        return DefaultFreeBattleScene;
    }

    public static void ApplyFreeBattleRuntimeConfigToManager(BattleSimulationManager manager)
    {
        if (manager == null || !BattleLaunchContext.IsFreeBattle)
            return;

        BattleDifficultyTier tier = FreeBattleBattleCopy.TierFromLabelZh(
            BattleLaunchContext.ResolveForBattleRecord());
        EnemyAiPlayStyle style = BattleLaunchContext.FreeBattleAiStyle;
        SceneLoader loader = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
        if (loader == null)
        {
            GameObject host = new GameObject("FreeBattleSceneLoader");
            loader = host.AddComponent<SceneLoader>();
        }

        loader.ConfigureFreeBattleBattlePending(tier, style);
        manager.QueueRuntimeDifficultyConfig(
            loader.pendingUseFixedEnemyDeck,
            loader.pendingFixedEnemyDeckCardIds,
            loader.pendingEnemyOverLimitAllowance,
            loader.pendingMinEnemySpellsInDeck,
            loader.pendingEnemyAiPlayStyle,
            loader.pendingDifficultyLabelZh);
    }
}

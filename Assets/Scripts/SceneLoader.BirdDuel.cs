using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public partial class SceneLoader
{
    public const string BirdDuelSceneName = "Fighting bird game";

    private GameObject birdDuelEntryOverlay;
    private Coroutine freeBattleRandomEventRevealRoutine;

    /// <summary>
    /// 戰前流程：寫入 <see cref="PreBattleDuelContext"/> 並載入鬥鳥場景，
    /// 鬥鳥結束後由 <see cref="ResumeBattleAfterBirdDuel"/> 接續載入戰鬥。
    /// </summary>
    private void LaunchBirdDuelThenBattle(
        bool harborTraining,
        BattleDifficultyTier selectedTier,
        bool hasHiddenTier,
        BattleDifficultyTier hiddenTier,
        bool freeBattle = false,
        EnemyAiPlayStyle freeBattleAiStyle = EnemyAiPlayStyle.Balanced)
    {
        string battleScene = string.IsNullOrWhiteSpace(battleSceneName)
            ? DefaultHarborBattleScene
            : battleSceneName;

        string heroId = null;
        string heroName = null;
        if (harborTraining)
        {
            EnemyHeroProfile hero = EnemyHeroCatalog.ResolveForHarbor();
            heroId = hero.HeroId;
            heroName = hero.DisplayName;
        }
        else if (freeBattle)
        {
            heroId = EnemyHeroCatalog.HotBloodClassmateId;
            heroName = FreeBattleBattleCopy.GetEnemyHeroDisplayName(freeBattleAiStyle);
        }

        PreBattleDuelContext.Begin(
            battleScene,
            harborTraining,
            selectedTier,
            hasHiddenTier,
            hiddenTier,
            heroId,
            heroName,
            freeBattle,
            freeBattleAiStyle);
        PreBattleBonusContext.Clear();
        HideBattlePreviewModal();

        if (!Application.CanStreamedLevelBeLoaded(BirdDuelSceneName))
        {
            // 鬥鳥場景缺漏時退回原行為：直接打所選難度，避免卡死戰前流程。
            Debug.LogError("SceneLoader: bird duel scene not in Build Settings -> " + BirdDuelSceneName);
            PreBattleDuelContext.ClearActive();
            PreBattleBonusContext.Clear();
            PreBattleCdContext.Clear();
            ApplyPreBattleDifficultyPending(harborTraining, selectedTier, freeBattle, freeBattleAiStyle);
            StartBattleSceneLoad();
            return;
        }

        SceneManager.LoadScene(BirdDuelSceneName);
    }

    /// <summary>
    /// 鬥鳥結束回呼：玩家選擇挑戰魔王級且本戰有隱藏難度 → 打隱藏難度（魔王級）；否則打預覽所選難度。
    /// 本場加成由鬥鳥結算寫入 <see cref="PreBattleBonusContext"/>，戰鬥 StartBattle 套用。
    /// 情報帶入戰鬥（由 <see cref="OnSceneLoadedFixup"/> 以 toast 顯示）。
    /// </summary>
    public static void ResumeBattleAfterBirdDuel(bool challengeHiddenTier, string intelText)
    {
        bool harbor = PreBattleDuelContext.IsHarborTraining;
        bool freeBattle = PreBattleDuelContext.IsFreeBattle;
        EnemyAiPlayStyle freeBattleAiStyle = PreBattleDuelContext.FreeBattleAiStyle;
        bool hasHidden = PreBattleDuelContext.HasHiddenTier;
        BattleDifficultyTier finalTier = (challengeHiddenTier && hasHidden)
            ? PreBattleDuelContext.HiddenTier
            : PreBattleDuelContext.SelectedTier;
        string battleScene = PreBattleDuelContext.BattleSceneName;

        PreBattleDuelContext.SetIntelText(intelText);

        SceneLoader loader = ResolveSceneLoaderForActiveScene();
        if (!string.IsNullOrWhiteSpace(battleScene))
            loader.battleSceneName = battleScene;

        loader.ApplyPreBattleDifficultyPending(harbor, finalTier, freeBattle, freeBattleAiStyle);
        PreBattleDuelContext.ClearActive();

        if (!Application.CanStreamedLevelBeLoaded(loader.battleSceneName))
        {
            Debug.LogError("SceneLoader: battle scene not in Build Settings -> " + loader.battleSceneName);
            return;
        }

        loader.StartBattleSceneLoad();
    }

    /// <summary>套用戰鬥前難度設定（港灣、自由對戰與標準各走既有路徑），供戰前鬥鳥流程共用。</summary>
    private void ApplyPreBattleDifficultyPending(
        bool harborTraining,
        BattleDifficultyTier tier,
        bool freeBattle = false,
        EnemyAiPlayStyle freeBattleAiStyle = EnemyAiPlayStyle.Balanced)
    {
        if (harborTraining)
        {
            ConfigureHarborTrainingBattlePending(tier);
            return;
        }

        if (freeBattle)
        {
            ConfigureFreeBattleBattlePending(tier, freeBattleAiStyle);
            return;
        }

        BattleDifficultyConfig cfg = BuildDifficultyConfig(tier);
        pendingUseFixedEnemyDeck = cfg.UseFixedDeck;
        pendingFixedEnemyDeckCardIds = cfg.FixedDeckIds;
        pendingEnemyOverLimitAllowance = cfg.OverLimitAllowance;
        pendingMinEnemySpellsInDeck = cfg.MinSpellsInDeck;
        pendingEnemyAiPlayStyle = MapDifficultyToEnemyAiPlayStyle(tier);
        pendingDifficultyLabelZh = cfg.LabelZh;
        BattleLaunchContext.SetPendingDifficultyLabelZh(cfg.LabelZh);
    }

    /// <summary>自由對戰：70% 隨機事件觸發鬥鳥暖身賽。</summary>
    private void TryBeginFreeBattleRandomBirdDuelEvent(
        EnemyAiPlayStyle aiStyle,
        BattleDifficultyTier selected)
    {
        if (UnityEngine.Random.value >= FreeBattleBattleCopy.BirdDuelRandomEventChance)
        {
            EnterBattleDirectlyWithoutBirdDuel(false, true, aiStyle, selected);
            return;
        }

        ShowFreeBattleRandomBirdDuelEventOverlay(aiStyle, selected);
    }

    private void ShowFreeBattleRandomBirdDuelEventOverlay(
        EnemyAiPlayStyle aiStyle,
        BattleDifficultyTier selected)
    {
        Canvas canvas = ResolveBattlePreviewParentCanvas();
        if (canvas == null)
        {
            PreBattleCdContext.SetSelectedCd(BirdDuelCdCatalog.DefaultCdId);
            LaunchBirdDuelThenBattle(false, selected, false, BattleDifficultyTier.Boss, true, aiStyle);
            return;
        }

        CloseBirdDuelEntryChoice();

        if (freeBattleRandomEventRevealRoutine != null)
        {
            StopCoroutine(freeBattleRandomEventRevealRoutine);
            freeBattleRandomEventRevealRoutine = null;
        }

        GameObject overlay = BirdDuelOverlayUiBuild.CreateDimOverlay(
            canvas.transform, 5000, "FreeBattleRandomEventOverlay");
        GameObject panel = BuildFreeBattleRandomBirdDuelEventPanel(overlay.transform, aiStyle, selected);

        birdDuelEntryOverlay = overlay;
        overlay.transform.SetAsLastSibling();

        freeBattleRandomEventRevealRoutine = StartCoroutine(
            CoPlayFreeBattleRandomEventReveal(overlay, panel));
    }

    private IEnumerator CoPlayFreeBattleRandomEventReveal(GameObject overlay, GameObject panel)
    {
        yield return BirdDuelRandomEventRevealFx.CoPlayRevealThenShowPanel(
            overlay, panel, battlePreviewFontAsset);
        freeBattleRandomEventRevealRoutine = null;
    }

    private GameObject BuildFreeBattleRandomBirdDuelEventPanel(
        Transform overlayRoot,
        EnemyAiPlayStyle aiStyle,
        BattleDifficultyTier selected)
    {
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot);

        RectTransform headerRt = BirdDuelOverlayUiBuild.CreateHeaderBand(
            panel.transform, "✦ 隨機事件", battlePreviewFontAsset);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform, "鬥鳥暖身賽", battlePreviewFontAsset,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        string aiLabel = FreeBattleBattleCopy.GetAiStyleDisplayZh(aiStyle);
        string bodyText =
            "對手為 " + aiLabel + " 陪練。\n暖身賽勝出可獲戰前加成；落敗亦有保底，但敵方小幅強化。";
        BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            bodyText,
            battlePreviewFontAsset,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            BirdDuelOverlayUiBuild.ComputeInfoCardBottom());

        Button startBtn = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform, "StartWarmupBtn", "開始暖身賽", battlePreviewFontAsset);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(startBtn.GetComponent<RectTransform>(), 0);
        startBtn.onClick.AddListener(() =>
        {
            CloseBirdDuelEntryChoice();
            ShowBirdDuelCdSelect(false, selected, false, BattleDifficultyTier.Boss, true, aiStyle);
        });

        Button skipBtn = BirdDuelOverlayUiBuild.CreateSecondaryButton(
            panel.transform, "SkipWarmupBtn", "略過，直接對戰", battlePreviewFontAsset);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(skipBtn.GetComponent<RectTransform>(), 1);
        skipBtn.onClick.AddListener(() =>
        {
            CloseBirdDuelEntryChoice();
            EnterBattleDirectlyWithoutBirdDuel(false, true, aiStyle, selected);
        });

        Button backBtn = BirdDuelOverlayUiBuild.CreateGhostBackButton(
            headerRt, battlePreviewFontAsset);
        backBtn.onClick.AddListener(CloseBirdDuelEntryChoice);

        panel.SetActive(false);
        return panel;
    }

    // ----------------------------------------------------------------- 進關卡選擇（roguelike 分支）

    /// <summary>
    /// 進入正式關卡時的抉擇：挑戰鬥鳥（暖身賽 → 依表現拿加成）或直接進入對戰（無加成）。
    /// 見 Docs/鬥鳥手勢小遊戲企劃.md 第九章。
    /// </summary>
    private void ShowBirdDuelEntryChoice(
        bool harbor,
        BattleDifficultyTier selected,
        bool hasHidden,
        BattleDifficultyTier hiddenTier)
    {
        Canvas canvas = ResolveBattlePreviewParentCanvas();
        if (canvas == null)
        {
            PreBattleCdContext.SetSelectedCd(BirdDuelCdCatalog.DefaultCdId);
            LaunchBirdDuelThenBattle(harbor, selected, hasHidden, hiddenTier);
            return;
        }

        CloseBirdDuelEntryChoice();

        GameObject overlay = BirdDuelOverlayUiBuild.CreateDimOverlay(
            canvas.transform, 5000, "BirdDuelEntryOverlay");
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlay.transform);

        RectTransform headerRt = BirdDuelOverlayUiBuild.CreateHeaderBand(
            panel.transform, "✦ 戰前抉擇", battlePreviewFontAsset);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform, "鬥鳥暖身賽", battlePreviewFontAsset,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        string bodyText;
        if (harbor)
            bodyText = "對手是熱血同學。挑戰鬥鳥依表現得加成；敗北亦有保底，但敵方小幅強化。";
        else if (hasHidden)
            bodyText = "挑戰鬥鳥依表現得加成，勝出可挑戰魔王級；敗北亦有保底，但敵方小幅強化。";
        else
            bodyText = "挑戰鬥鳥依表現得加成；敗北亦有保底，但敵方小幅強化。";
        BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            bodyText,
            battlePreviewFontAsset,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            BirdDuelOverlayUiBuild.ComputeInfoCardBottom());

        Button challengeBtn = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform, "ChallengeBtn", "挑戰鬥鳥", battlePreviewFontAsset);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(challengeBtn.GetComponent<RectTransform>(), 0);
        challengeBtn.onClick.AddListener(() =>
        {
            CloseBirdDuelEntryChoice();
            ShowBirdDuelCdSelect(harbor, selected, hasHidden, hiddenTier);
        });

        Button directBtn = BirdDuelOverlayUiBuild.CreateSecondaryButton(
            panel.transform, "DirectBtn", "直接進入對戰", battlePreviewFontAsset);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(directBtn.GetComponent<RectTransform>(), 1);
        directBtn.onClick.AddListener(() =>
        {
            CloseBirdDuelEntryChoice();
            EnterBattleDirectlyWithoutBirdDuel(harbor, false, EnemyAiPlayStyle.Balanced, selected);
        });

        Button backBtn = BirdDuelOverlayUiBuild.CreateGhostBackButton(
            headerRt, battlePreviewFontAsset);
        backBtn.onClick.AddListener(CloseBirdDuelEntryChoice);

        birdDuelEntryOverlay = overlay;
        overlay.transform.SetAsLastSibling();
    }

    /// <summary>戰前抉擇後：選 CD 光碟，再進鬥鳥。見 Docs/鬥鳥手勢小遊戲企劃.md §12.3。</summary>
    private void ShowBirdDuelCdSelect(
        bool harbor,
        BattleDifficultyTier selected,
        bool hasHidden,
        BattleDifficultyTier hiddenTier,
        bool freeBattle = false,
        EnemyAiPlayStyle freeBattleAiStyle = EnemyAiPlayStyle.Balanced)
    {
        Canvas canvas = ResolveBattlePreviewParentCanvas();
        if (canvas == null)
        {
            PreBattleCdContext.SetSelectedCd(BirdDuelCdCatalog.DefaultCdId);
            LaunchBirdDuelThenBattle(harbor, selected, hasHidden, hiddenTier, freeBattle, freeBattleAiStyle);
            return;
        }

        BirdDuelCdSelectOverlayUi.Show(
            canvas,
            battlePreviewFontAsset,
            cdId =>
            {
                PreBattleCdContext.SetSelectedCd(
                    string.IsNullOrWhiteSpace(cdId) ? BirdDuelCdCatalog.DefaultCdId : cdId);
                LaunchBirdDuelThenBattle(harbor, selected, hasHidden, hiddenTier, freeBattle, freeBattleAiStyle);
            },
            () =>
            {
                if (freeBattle)
                    ShowFreeBattleRandomBirdDuelEventOverlay(freeBattleAiStyle, selected);
                else
                    ShowBirdDuelEntryChoice(harbor, selected, hasHidden, hiddenTier);
            });
    }

    private void CloseBirdDuelEntryChoice()
    {
        if (freeBattleRandomEventRevealRoutine != null)
        {
            StopCoroutine(freeBattleRandomEventRevealRoutine);
            freeBattleRandomEventRevealRoutine = null;
        }

        if (birdDuelEntryOverlay != null)
        {
            Destroy(birdDuelEntryOverlay);
            birdDuelEntryOverlay = null;
        }
    }

    /// <summary>「直接進入對戰」：清空加成、套用所選難度後載入戰鬥（不經鬥鳥）。</summary>
    private void EnterBattleDirectlyWithoutBirdDuel(
        bool harbor,
        bool freeBattle,
        EnemyAiPlayStyle freeBattleAiStyle,
        BattleDifficultyTier selected)
    {
        PreBattleBonusContext.Clear();
        PreBattleCdContext.Clear();
        PreBattleDuelContext.ClearActive();
        HideBattlePreviewModal();
        ApplyPreBattleDifficultyPending(harbor, selected, freeBattle, freeBattleAiStyle);

        if (!Application.CanStreamedLevelBeLoaded(battleSceneName))
        {
            Debug.LogError("SceneLoader: battle scene not in Build Settings -> " + battleSceneName);
            return;
        }
        StartBattleSceneLoad();
    }

    /// <summary>港灣實戰：立繪 A（擅長說明）→ 戰前抉擇。見企劃第十章。</summary>
    private void ShowHarborEnemyHeroPortraitA(System.Action onContinue)
    {
        Canvas canvas = ResolveBattlePreviewParentCanvas();
        if (canvas == null)
        {
            onContinue?.Invoke();
            return;
        }

        EnemyHeroProfile hero = EnemyHeroCatalog.ResolveForHarbor();
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        bool isRematch = HarborTrainingProgressState.HasMetHotBloodClassmate(slot);
        EnemyHeroPortraitBridgeUi.ShowPortraitA(
            canvas,
            hero,
            isRematch,
            battlePreviewFontAsset,
            onContinue);
    }
}

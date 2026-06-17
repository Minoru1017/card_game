using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public partial class SceneLoader
{
    public const string BirdDuelSceneName = "Fighting bird game";

    private GameObject birdDuelEntryOverlay;

    /// <summary>
    /// 戰前流程：寫入 <see cref="PreBattleDuelContext"/> 並載入鬥鳥場景，
    /// 鬥鳥結束後由 <see cref="ResumeBattleAfterBirdDuel"/> 接續載入戰鬥。
    /// </summary>
    private void LaunchBirdDuelThenBattle(
        bool harborTraining,
        BattleDifficultyTier selectedTier,
        bool hasHiddenTier,
        BattleDifficultyTier hiddenTier)
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

        PreBattleDuelContext.Begin(
            battleScene, harborTraining, selectedTier, hasHiddenTier, hiddenTier, heroId, heroName);
        PreBattleBonusContext.Clear();
        HideBattlePreviewModal();

        if (!Application.CanStreamedLevelBeLoaded(BirdDuelSceneName))
        {
            // 鬥鳥場景缺漏時退回原行為：直接打所選難度，避免卡死戰前流程。
            Debug.LogError("SceneLoader: bird duel scene not in Build Settings -> " + BirdDuelSceneName);
            PreBattleDuelContext.ClearActive();
            PreBattleBonusContext.Clear();
            PreBattleCdContext.Clear();
            ApplyPreBattleDifficultyPending(harborTraining, selectedTier);
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
        bool hasHidden = PreBattleDuelContext.HasHiddenTier;
        BattleDifficultyTier finalTier = (challengeHiddenTier && hasHidden)
            ? PreBattleDuelContext.HiddenTier
            : PreBattleDuelContext.SelectedTier;
        string battleScene = PreBattleDuelContext.BattleSceneName;

        PreBattleDuelContext.SetIntelText(intelText);

        SceneLoader loader = ResolveSceneLoaderForActiveScene();
        if (!string.IsNullOrWhiteSpace(battleScene))
            loader.battleSceneName = battleScene;

        loader.ApplyPreBattleDifficultyPending(harbor, finalTier);
        PreBattleDuelContext.ClearActive();

        if (!Application.CanStreamedLevelBeLoaded(loader.battleSceneName))
        {
            Debug.LogError("SceneLoader: battle scene not in Build Settings -> " + loader.battleSceneName);
            return;
        }

        loader.StartBattleSceneLoad();
    }

    /// <summary>套用戰鬥前難度設定（港灣與標準各走既有路徑），供戰前鬥鳥流程共用。</summary>
    private void ApplyPreBattleDifficultyPending(bool harborTraining, BattleDifficultyTier tier)
    {
        if (harborTraining)
        {
            ConfigureHarborTrainingBattlePending(tier);
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

        GameObject overlay = new GameObject(
            "BirdDuelEntryOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 5000;
        overlay.AddComponent<GraphicRaycaster>();
        RectTransform overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(940f, 480f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

        CreateBirdDuelEntryText(
            panel.transform, "Title", "戰前抉擇", 52f, FontStyles.Bold,
            new Vector2(0f, -64f), new Vector2(860f, 72f));

        string bodyText;
        if (harbor)
            bodyText = "對手是熱血同學。挑戰鬥鳥依表現得加成；\n敗北亦有保底，但敵方小幅強化。";
        else if (hasHidden)
            bodyText = "挑戰鬥鳥依表現得加成，勝出可挑戰魔王級；\n敗北亦有保底，但敵方小幅強化。";
        else
            bodyText = "挑戰鬥鳥依表現得加成；\n敗北亦有保底，但敵方小幅強化。";
        CreateBirdDuelEntryText(
            panel.transform, "Body", bodyText, 28f, FontStyles.Normal,
            new Vector2(0f, -170f), new Vector2(840f, 110f));

        Button challengeBtn = CreateModalButton(panel.transform, "ChallengeBtn", "挑戰鬥鳥");
        RectTransform cRt = challengeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0f);
        cRt.anchorMax = new Vector2(0.5f, 0f);
        cRt.pivot = new Vector2(0.5f, 0f);
        cRt.anchoredPosition = new Vector2(-190f, 44f);
        cRt.sizeDelta = new Vector2(320f, 88f);
        challengeBtn.onClick.AddListener(() =>
        {
            CloseBirdDuelEntryChoice();
            ShowBirdDuelCdSelect(harbor, selected, hasHidden, hiddenTier);
        });

        Button directBtn = CreateModalButton(panel.transform, "DirectBtn", "直接進入對戰");
        RectTransform dRt = directBtn.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.5f, 0f);
        dRt.anchorMax = new Vector2(0.5f, 0f);
        dRt.pivot = new Vector2(0.5f, 0f);
        dRt.anchoredPosition = new Vector2(190f, 44f);
        dRt.sizeDelta = new Vector2(320f, 88f);
        directBtn.onClick.AddListener(() =>
        {
            CloseBirdDuelEntryChoice();
            EnterBattleDirectlyWithoutBirdDuel(harbor, selected);
        });

        // 返回：關閉抉擇、回到戰前預覽。
        Button backBtn = CreateModalButton(panel.transform, "BackBtn", "返回");
        RectTransform bRt = backBtn.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 1f);
        bRt.anchorMax = new Vector2(1f, 1f);
        bRt.pivot = new Vector2(1f, 1f);
        bRt.anchoredPosition = new Vector2(-18f, -14f);
        bRt.sizeDelta = new Vector2(120f, 56f);
        backBtn.onClick.AddListener(CloseBirdDuelEntryChoice);

        birdDuelEntryOverlay = overlay;
        overlay.transform.SetAsLastSibling();
    }

    /// <summary>戰前抉擇後：選 CD 光碟，再進鬥鳥。見 Docs/鬥鳥手勢小遊戲企劃.md §12.3。</summary>
    private void ShowBirdDuelCdSelect(
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

        BirdDuelCdSelectOverlayUi.Show(
            canvas,
            battlePreviewFontAsset,
            cdId =>
            {
                PreBattleCdContext.SetSelectedCd(
                    string.IsNullOrWhiteSpace(cdId) ? BirdDuelCdCatalog.DefaultCdId : cdId);
                LaunchBirdDuelThenBattle(harbor, selected, hasHidden, hiddenTier);
            },
            () => ShowBirdDuelEntryChoice(harbor, selected, hasHidden, hiddenTier));
    }

    private void CloseBirdDuelEntryChoice()
    {
        if (birdDuelEntryOverlay != null)
        {
            Destroy(birdDuelEntryOverlay);
            birdDuelEntryOverlay = null;
        }
    }

    private void CreateBirdDuelEntryText(
        Transform parent, string objName, string text, float fontSize, FontStyles style,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (battlePreviewFontAsset != null) tmp.font = battlePreviewFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.enableWordWrapping = true;
        tmp.color = new Color(0.92f, 0.95f, 0.99f, 1f);
        tmp.raycastTarget = false;
    }

    /// <summary>「直接進入對戰」：清空加成、套用所選難度後載入戰鬥（不經鬥鳥）。</summary>
    private void EnterBattleDirectlyWithoutBirdDuel(bool harbor, BattleDifficultyTier selected)
    {
        PreBattleBonusContext.Clear();
        PreBattleCdContext.Clear();
        PreBattleDuelContext.ClearActive();
        HideBattlePreviewModal();
        ApplyPreBattleDifficultyPending(harbor, selected);

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

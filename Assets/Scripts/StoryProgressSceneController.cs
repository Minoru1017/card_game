using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Story progress hub: tutorial plot launch, intro battle, then hall.</summary>
public class StoryProgressSceneController : MonoBehaviour
{
    public const string EnterStageButtonObjectName = "EnterStageButton";
    public const string GoToHallButtonObjectName = "GoToHallButton";
    public const string ReplayIntroButtonObjectName = "ReplayIntroButton";
    public const string BackButtonObjectName = ReturnButtonLayout.ObjectName;

    private static readonly string[] EnterStageLabelFragments = { "進入關卡", "挑戰港灣", "港灣訓練場" };
    private static readonly string[] GoToHallLabelFragments = { "前往大廳" };
    private static readonly string[] ReplayIntroLabelFragments = { "重溫入門", "入門課" };
    private static readonly string[] BackButtonNameFragments = { "return", "Return", "返回", "返回按鈕" };

    private const string ChapterTitleDefault = StoryProgressLevelCopy.LevelTitle;

    [SerializeField] private string hallSceneName = StoryProgressSession.HallSceneName;

    private const string ChapterMapStatusObjectName = "ChapterMapStatus";
    private const string ScenarioRewardsObjectName = "ScenarioRewards";

    // Story Progress palette: medieval map tone with cozy AC-like softness.
    private static readonly Color ProgressPanelShellColor = new Color(0.20f, 0.27f, 0.22f, 0.96f);     // mossy forest shell
    private static readonly Color ProgressCardPrimaryColor = new Color(0.95f, 0.91f, 0.79f, 0.99f);    // brighter parchment story card
    private static readonly Color ProgressCardSecondaryColor = new Color(0.13f, 0.34f, 0.36f, 0.98f);  // deep teal reward card
    private static readonly Color ProgressTitleColor = new Color(0.97f, 0.94f, 0.82f, 1f);             // warm parchment on moss shell
    private static readonly Color ProgressSubtitleColor = new Color(0.49f, 0.36f, 0.24f, 1f);          // wood-brown subtitle
    private static readonly Color ProgressBodyColor = new Color(0.20f, 0.17f, 0.13f, 1f);              // ink-like body text
    private static readonly Color ProgressBulletinBodyColor = new Color(0.86f, 0.93f, 0.88f, 1f);    // soft light on bottom bar
    private static readonly Color FooterRewardBodyColor = new Color(0.95f, 0.97f, 0.94f, 1f);
    private static readonly Color FooterRewardShadowColor = new Color(0.04f, 0.10f, 0.11f, 0.55f);

    private const float DetailPanelInset = 0.11f;
    private const float DetailPanelInsetMax = 0.89f;
    private const float DetailTextPad = 36f;
    private const float DetailTitleMinY = 0.86f;
    private const float DetailTitleMaxY = 0.97f;
    private const float DetailIntroMinY = 0.35f;
    private const float DetailIntroMaxY = 0.84f;
    private const float DetailRewardsMinY = 0.08f;
    private const float DetailRewardsMaxY = 0.31f;
    private const string ScenarioPreviewPanelObjectName = "Scenario Preview";
    private const string FooterPanelObjectName = "Panel";
    private const string FooterButtonRowObjectName = "StoryProgressFooterButtonRow";
    private const float FooterButtonRowInsetRight = 96f;
    private const float FooterButtonRowInsetBottom = 24f;
    private const float FooterButtonSpacing = 14f;
    private const float FooterButtonWidth = 280f;
    private const float FooterButtonHeight = 64f;
    private const float FooterPanelDefaultHeight = 165f;
    private const float FooterPanelTallPhoneHeight = 208f;
    private const float FooterButtonWidthTallPhone = 220f;
    private const float FooterButtonHeightTallPhone = 60f;
    private const float FooterButtonSpacingTallPhone = 10f;
    private RectTransform footerButtonRowRt;
    private const float HarborBulletinPadLeft = 24f;
    private const float HarborBulletinBreathingLeft = 24f;
    private const float HarborBulletinTextMarginLeft = 18f;
    private const float HarborBulletinReserveRight = 660f;
    private const float RightDockBaseWidth = 1035.7983f;
    private const float RightDockWidthAt18By9 = 860f;
    private const float RightDockWidthAt22By9 = 940f;
    private const float RightDockBaseHeight = 1119f;
    private const float RightDockCenterYOffset = 82.5f;
    private const float RightDockAbsoluteMinScale = 0.35f;
    private const float RightDockTextScaleAt20By9 = 0.86f;

    private TMP_Text chapterMapStatusTmp;
    private TMP_Text chapterMapTitleTmp;
    private TMP_Text levelPanelTitleTmp;
    private TMP_Text chapterSummaryTmp;
    private TMP_Text scenarioPreviewTmp;
    private TMP_Text scenarioRewardsTmp;
    private GameObject scenarioOverviewPlaceholderRoot;
    private Button enterStageButton;
    private TMP_Text enterStageButtonLabel;
    private Button goToHallButton;
    private TMP_Text goToHallButtonLabel;
    private Button replayIntroButton;
    private TMP_Text replayIntroButtonLabel;
    private Button backButton;
    private bool leavingForHall;
    private Coroutine deferredBackButtonLayoutRoutine;
    private RectTransform viewLevelFlowRt;
    private RectTransform scenarioPreviewPanelRt;
    private Image scenarioPreviewPanelImage;
    private ScrollRect scenarioIntroScrollRect;
    private RectTransform scenarioIntroScrollContentRt;
    private const string ScenarioIntroScrollContentName = "ScenarioIntroScrollContent";
    private const string LegacyScenarioIntroScrollViewportName = "ScenarioIntroScrollViewport";
    private RectTransform rewardsPanelRt;
    private Image rewardsPanelImage;

    private static bool sceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallSceneHook()
    {
        if (sceneHookInstalled) return;
        sceneHookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsureController(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryEnsureController(scene);

    private static void TryEnsureController(Scene scene)
    {
        if (!scene.IsValid() || scene.name != StoryProgressSession.StoryProgressSceneName) return;
        if (Object.FindFirstObjectByType<StoryProgressSceneController>() != null) return;

        GameObject host = new GameObject("StoryProgressSceneController");
        host.AddComponent<StoryProgressSceneController>();
    }

    private void Awake()
    {
        leavingForHall = false;
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        CleanupStrayRuntimeStoryProgressUi();
        AutoBindUi();
        WireButtons();
    }

    private void Start()
    {
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
        CleanupStrayRuntimeStoryProgressUi();
        // UI 可能在同幀稍晚才就緒；再綁一次避免漏接。
        AutoBindUi();
        WireButtons();
        RefreshPresentation();
        ScheduleDeferredBackButtonLayout();
        StartCoroutine(DeferredRefreshChapterMapStatus());
        StartCoroutine(CoTryContinueM13AfterBirdDuel());
    }

    private System.Collections.IEnumerator CoTryContinueM13AfterBirdDuel()
    {
        yield return null;
        if (!StoryProgressSession.TryConsumeM13ContinueAfterBirdDuel())
            yield break;

        if (!M13RiverForkFlow.CanEnterFromStoryProgress(PlayerData.GetActivePlayerSlotOrDefault()))
            yield break;

        M13RiverForkFlow.ContinueAfterBirdDuel();
    }

    private void OnEnable()
    {
        RefreshPresentation();
        ScheduleDeferredBackButtonLayout();
    }

    private void OnDisable()
    {
        if (deferredBackButtonLayoutRoutine != null)
        {
            StopCoroutine(deferredBackButtonLayoutRoutine);
            deferredBackButtonLayoutRoutine = null;
        }
    }

    private void AutoBindUi()
    {
        if (viewLevelFlowRt == null)
        {
            GameObject viewFlow = GameObject.Find(StoryProgressLevelCopy.ViewLevelFlowPanelName);
            if (viewFlow != null)
            {
                viewFlow.SetActive(false);
                viewLevelFlowRt = viewFlow.GetComponent<RectTransform>();
            }
        }

        if (chapterMapTitleTmp == null)
            chapterMapTitleTmp = FindChapterMapTitleTmp();
        HideLegacyChapterMapLabels();

        levelPanelTitleTmp = null;

        chapterSummaryTmp = null;

        scenarioPreviewTmp = null;

        if (scenarioRewardsTmp == null)
            scenarioRewardsTmp = FindFooterRewardsTmp();
        if (scenarioRewardsTmp != null)
        {
            rewardsPanelImage = scenarioRewardsTmp.GetComponentInParent<Image>();
            rewardsPanelRt = rewardsPanelImage != null ? rewardsPanelImage.rectTransform : null;
        }

        if (scenarioOverviewPlaceholderRoot == null)
        {
            TMP_Text overviewPlaceholder = FindTmpByExactText("概述對戰資訊");
            if (overviewPlaceholder != null)
                scenarioOverviewPlaceholderRoot = overviewPlaceholder.transform.parent?.gameObject;
        }

        enterStageButton = FindButtonByLabelFragments(EnterStageButtonObjectName, EnterStageLabelFragments);
        if (enterStageButton != null)
            enterStageButtonLabel = enterStageButton.GetComponentInChildren<TMP_Text>(true);

        goToHallButton = FindButtonByLabelFragments(GoToHallButtonObjectName, GoToHallLabelFragments);
        if (goToHallButton != null)
            goToHallButtonLabel = goToHallButton.GetComponentInChildren<TMP_Text>(true);

        backButton = ResolveBackButton();
    }

    private void WireButtons()
    {
        if (enterStageButton != null)
        {
            enterStageButton.onClick.RemoveAllListeners();
            enterStageButton.onClick.AddListener(OnEnterStageClicked);
            enterStageButton.interactable = true;
        }
        else
            Debug.LogWarning("StoryProgressSceneController: enter stage button not found. Expected label: 進入關卡");

        if (goToHallButton != null)
        {
            goToHallButton.onClick.RemoveAllListeners();
            goToHallButton.onClick.AddListener(LoadHall);
        }

        WireBackButton();
    }

    private void OnEnterStageClicked()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        string selectedNode = StoryProgressWorldMapRuntime.SelectedStageNodeId;
        if (string.Equals(selectedNode, StoryProgressSession.SeawallPatrolNodeId, System.StringComparison.OrdinalIgnoreCase))
        {
            if (M12SeawallPatrolFlow.CanEnterFromStoryProgress(slot))
                M12SeawallPatrolFlow.LaunchFromStoryProgress();
            else
                Debug.LogWarning("StoryProgressSceneController: M-1-2 locked — clear harbor combat first.");
            return;
        }

        if (string.Equals(selectedNode, StoryProgressSession.RiverForkNodeId, System.StringComparison.OrdinalIgnoreCase))
        {
            if (M13RiverForkFlow.CanEnterFromStoryProgress(slot))
                M13RiverForkFlow.LaunchFromStoryProgress();
            else
                Debug.LogWarning("StoryProgressSceneController: M-1-3 locked — clear M-1-2 first.");
            return;
        }

        if (TutorialProgressState.IsAcademyIntroGraduated(slot))
        {
            // 已畢業走港灣實戰：一樣先播進關演出，黑幕後開啟難度預覽。
            StoryLevelEntryTransition.PlayToHarborTrainingPreview();
            return;
        }

        OnReplayIntroClicked();
    }

    public static void RequestRefreshPresentation()
    {
        StoryProgressSceneController ctrl =
            Object.FindFirstObjectByType<StoryProgressSceneController>();
        ctrl?.RefreshPresentation();
    }

    /// <summary>1-1 學院入門：進關演出 → 劇情 → 選項 → 教學對戰。</summary>
    private void OnReplayIntroClicked()
    {
        if (!Application.CanStreamedLevelBeLoaded(StoryProgressSession.MainPlotSceneName))
        {
            Debug.LogError("StoryProgressSceneController: Main Plot not in Build Settings.");
            return;
        }

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        bool replay = TutorialProgressState.IsAcademyIntroGraduated(slot);
        StoryLevelEntryTransition.PlayToAcademyIntroPlot(replay);
    }

    private void RefreshPresentation()
    {
        if (leavingForHall)
            return;

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.EnsureSlotIntroProgressConsistent(slot);
        HarborTrainingProgressState.EnsureSlotHarborProgressConsistent(slot);
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        TutorialProgressState.GetAcademyIntroProgressForDisplay(slot, out bool plotComplete, out bool battleComplete);
        bool introGraduated = TutorialProgressState.IsAcademyIntroGraduated(slot);

        HideLegacyChapterMapLabels();

        if (scenarioOverviewPlaceholderRoot != null)
            scenarioOverviewPlaceholderRoot.SetActive(false);

        CardStore cardStore = PlayerData.ResolveCanonical()?.CardStore;
        string selectedNode = StoryProgressWorldMapRuntime.SelectedStageNodeId;
        bool isM12Selected = string.Equals(selectedNode, StoryProgressSession.SeawallPatrolNodeId,
            System.StringComparison.OrdinalIgnoreCase);
        bool isM13Selected = string.Equals(selectedNode, StoryProgressSession.RiverForkNodeId,
            System.StringComparison.OrdinalIgnoreCase);
        bool isStoryStageSelected = isM12Selected || isM13Selected;
        bool canEnterM12 = M12SeawallPatrolFlow.CanEnterFromStoryProgress(slot);
        bool canEnterM13 = M13RiverForkFlow.CanEnterFromStoryProgress(slot);

        if (scenarioRewardsTmp == null)
            scenarioRewardsTmp = FindFooterRewardsTmp();
        string footerRewards = null;
        if (scenarioRewardsTmp != null)
        {
            if (isM12Selected)
                footerRewards = StoryProgressLevelCopyM12.BuildScenarioRewards(cardStore, slot);
            else if (isM13Selected)
                footerRewards = StoryProgressLevelCopyM13.BuildScenarioRewards(cardStore, slot);
            else
                footerRewards = StoryProgressLevelCopy.BuildScenarioRewards(cardStore);
        }

        if (enterStageButtonLabel != null)
        {
            string enterLabel;
            if (isM12Selected)
                enterLabel = StoryProgressLevelCopyM12.ResolveEnterButtonLabel(slot);
            else if (isM13Selected)
                enterLabel = StoryProgressLevelCopyM13.ResolveEnterButtonLabel(slot);
            else
                enterLabel = introGraduated ? "挑戰港灣訓練場" : "進入關卡";
            PlotUiTextUtil.ApplyButtonLabel(enterStageButtonLabel, enterLabel, ResolveUiFontSource());
        }

        if (enterStageButton != null)
        {
            if (isM12Selected)
                enterStageButton.interactable = canEnterM12;
            else if (isM13Selected)
                enterStageButton.interactable = canEnterM13;
            else
                enterStageButton.interactable = true;
        }

        if (scenarioPreviewTmp != null)
        {
            string introText;
            if (isM12Selected)
                introText = StoryProgressLevelCopyM12.BuildScenarioIntro(slot);
            else if (isM13Selected)
                introText = StoryProgressLevelCopyM13.BuildScenarioIntro(slot);
            else
                introText = StoryProgressLevelCopy.BuildScenarioIntro(introGraduated);
            ApplyScenarioIntroText(introText);
        }

        if (introGraduated)
        {
            replayIntroButton = EnsureReplayIntroButton(enterStageButton);
            if (replayIntroButton != null)
            {
                replayIntroButton.gameObject.SetActive(true);
                TMP_Text fontSource = ResolveUiFontSource();
                replayIntroButtonLabel = PlotUiTextUtil.EnsureButtonLabel(
                    replayIntroButton, replayIntroButtonLabel, fontSource);
                if (replayIntroButtonLabel != null)
                    PlotUiTextUtil.ApplyButtonLabel(replayIntroButtonLabel, "重溫入門課", fontSource);
                replayIntroButton.onClick.RemoveAllListeners();
                replayIntroButton.onClick.AddListener(OnReplayIntroClicked);
            }
        }
        else if (replayIntroButton != null)
        {
            replayIntroButton.gameObject.SetActive(false);
        }

        if (backButton == null)
            backButton = EnsureBackButton();
        if (backButton != null)
            backButton.gameObject.SetActive(true);
        WireBackButton();

        if (goToHallButton == null)
            goToHallButton = EnsureGoToHallButton(enterStageButton);
        if (goToHallButton != null)
        {
            goToHallButton.gameObject.SetActive(introGraduated);
            if (goToHallButtonLabel == null)
                goToHallButtonLabel = goToHallButton.GetComponentInChildren<TMP_Text>(true);
            if (introGraduated)
            {
                TMP_Text hallLabelFontSource = ResolveUiFontSource();
                goToHallButtonLabel = PlotUiTextUtil.EnsureButtonLabel(
                    goToHallButton, goToHallButtonLabel, hallLabelFontSource);
                if (goToHallButtonLabel != null)
                    PlotUiTextUtil.ApplyButtonLabel(goToHallButtonLabel, "前往大廳", hallLabelFontSource);
                goToHallButton.onClick.RemoveAllListeners();
                goToHallButton.onClick.AddListener(LoadHall);
            }
        }

        ApplyFooterActionButtonLayout(introGraduated, isStoryStageSelected);
        if (scenarioRewardsTmp != null && !string.IsNullOrEmpty(footerRewards))
            ApplyFooterRewardsText(scenarioRewardsTmp, footerRewards);

        ApplyResponsiveRightDockLayout();
        ApplyRightDetailPanelThemeAndLayout();
        ApplyResponsiveRightDockTextScale();

        if (levelPanelTitleTmp != null && isStoryStageSelected)
        {
            levelPanelTitleTmp.richText = true;
            levelPanelTitleTmp.text = isM13Selected
                ? StoryProgressLevelCopyM13.LevelTitle
                : StoryProgressLevelCopyM12.LevelTitle;
            levelPanelTitleTmp.ForceMeshUpdate(true, true);
        }
    }

    private void ApplyResponsiveRightDockLayout()
    {
        if (viewLevelFlowRt == null || !viewLevelFlowRt.gameObject.activeInHierarchy)
            return;

        float h = Mathf.Max(1f, Screen.height);
        float aspect = Screen.width / h;
        float t18 = Mathf.InverseLerp(16f / 9f, 18f / 9f, aspect);
        float t22 = Mathf.InverseLerp(18f / 9f, 22f / 9f, aspect);
        // 18:9→22:9：在高度受限縮小後，橫向給補償加寬，避免面板內容過度擠壓。
        float widthByAspect = aspect <= 18f / 9f
            ? Mathf.Lerp(RightDockBaseWidth, RightDockWidthAt18By9, t18)
            : Mathf.Lerp(RightDockWidthAt18By9, RightDockWidthAt22By9, t22);

        // 20:9 進一步約束：綠色底板底邊不可超過青色底板上緣。
        // 做法：在寬螢幕下再套一層等比縮放，直到 bottom == cyanTop（或更上方）。
        float fitScale = 1f;
        RectTransform footerRt = ResolveFooterPanelTransform() as RectTransform;
        RectTransform parentRt = viewLevelFlowRt.parent as RectTransform;
        if (footerRt != null && parentRt != null)
        {
            Canvas.ForceUpdateCanvases();
            Vector3[] footerCorners = new Vector3[4];
            footerRt.GetWorldCorners(footerCorners);
            float cyanTopLocalY = parentRt.InverseTransformPoint(footerCorners[1]).y;
            float maxHeight = (RightDockCenterYOffset - cyanTopLocalY) * 2f;
            if (maxHeight > 1f)
                fitScale = Mathf.Clamp(maxHeight / RightDockBaseHeight, RightDockAbsoluteMinScale, 1f);
            else
                fitScale = RightDockAbsoluteMinScale;
        }

        float width = widthByAspect * fitScale;
        float height = RightDockBaseHeight * fitScale;

        // 右側綠色底板：永遠貼齊右緣；20:9 逐步縮窄，子容器/文字會隨父層一起跟隨。
        viewLevelFlowRt.anchorMin = new Vector2(1f, 0.5f);
        viewLevelFlowRt.anchorMax = new Vector2(1f, 0.5f);
        viewLevelFlowRt.pivot = new Vector2(1f, 0.5f);
        viewLevelFlowRt.anchoredPosition = new Vector2(0f, RightDockCenterYOffset);
        viewLevelFlowRt.sizeDelta = new Vector2(width, height);
        viewLevelFlowRt.localScale = Vector3.one;
    }

    private static float ResolveRightDockTextScale()
    {
        float h = Mathf.Max(1f, Screen.height);
        float aspect = Screen.width / h;
        float t = Mathf.InverseLerp(16f / 9f, 20f / 9f, aspect);
        return Mathf.Lerp(1f, RightDockTextScaleAt20By9, t);
    }

    private void ApplyResponsiveRightDockTextScale()
    {
        if (viewLevelFlowRt == null || !viewLevelFlowRt.gameObject.activeInHierarchy)
            return;

        float scale = ResolveRightDockTextScale();

        if (levelPanelTitleTmp != null)
            levelPanelTitleTmp.fontSize = 52f * scale;

        if (scenarioPreviewTmp != null)
        {
            scenarioPreviewTmp.fontSize = 28f * scale;
            scenarioPreviewTmp.lineSpacing = 8f * scale;
            scenarioPreviewTmp.paragraphSpacing = 14f * scale;
        }

        if (scenarioRewardsTmp != null)
        {
            scenarioRewardsTmp.fontSize = 28f * scale;
            scenarioRewardsTmp.lineSpacing = 8f * scale;
            scenarioRewardsTmp.paragraphSpacing = 14f * scale;
        }
    }

    private void ApplyRightDetailPanelThemeAndLayout()
    {
        if (viewLevelFlowRt == null || !viewLevelFlowRt.gameObject.activeInHierarchy)
            return;

        if (viewLevelFlowRt != null)
        {
            Image shellImage = viewLevelFlowRt.GetComponent<Image>();
            if (shellImage != null)
                shellImage.color = ProgressPanelShellColor;
        }

        if (levelPanelTitleTmp != null)
        {
            levelPanelTitleTmp.richText = false;
            levelPanelTitleTmp.text = ChapterTitleDefault;
            levelPanelTitleTmp.color = ProgressTitleColor;
            levelPanelTitleTmp.faceColor = ProgressTitleColor;
            levelPanelTitleTmp.alignment = TextAlignmentOptions.Center;
            levelPanelTitleTmp.fontSize = 52f;
            levelPanelTitleTmp.fontStyle = FontStyles.Bold;
            levelPanelTitleTmp.enableWordWrapping = false;
            levelPanelTitleTmp.margin = Vector4.zero;
            levelPanelTitleTmp.raycastTarget = false;
            ApplyPanelRectPreset(levelPanelTitleTmp.rectTransform, DetailPanelInset, DetailTitleMinY, DetailPanelInsetMax, DetailTitleMaxY);
            levelPanelTitleTmp.ForceMeshUpdate(true, true);
        }

        if (scenarioPreviewPanelImage != null)
            scenarioPreviewPanelImage.color = ProgressCardPrimaryColor;

        if (scenarioPreviewPanelRt != null)
        {
            ApplyPanelRectPreset(scenarioPreviewPanelRt, DetailPanelInset, DetailIntroMinY, DetailPanelInsetMax, DetailIntroMaxY);
            EnsureScenarioIntroScroll();
        }

        if (rewardsPanelImage != null)
            rewardsPanelImage.color = ProgressCardSecondaryColor;
        if (rewardsPanelRt != null)
        {
            ApplyPanelRectPreset(rewardsPanelRt, DetailPanelInset, DetailRewardsMinY, DetailPanelInsetMax, DetailRewardsMaxY);
            EnsureDetailPanelMask(rewardsPanelRt);
        }

        if (scenarioPreviewTmp != null)
            ApplyDetailTextFillLayout(scenarioPreviewTmp.rectTransform, DetailTextPad);

        if (scenarioRewardsTmp != null)
            ApplyDetailTextFillLayout(scenarioRewardsTmp.rectTransform, DetailTextPad);
    }

    private static void ApplyPanelRectPreset(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void ApplyDetailTextFillLayout(RectTransform textRt, float pad)
    {
        if (textRt == null) return;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.offsetMin = new Vector2(pad, pad);
        textRt.offsetMax = new Vector2(-pad, -pad);
        textRt.anchoredPosition = Vector2.zero;
        textRt.localScale = Vector3.one;
    }

    private static void EnsureDetailPanelMask(RectTransform panelRt)
    {
        if (panelRt == null) return;
        if (panelRt.GetComponent<RectMask2D>() != null) return;
        Image panelImage = panelRt.GetComponent<Image>();
        if (panelImage == null)
            panelImage = panelRt.gameObject.AddComponent<Image>();
        panelImage.color = panelImage.color.a > 0.01f ? panelImage.color : new Color(1f, 1f, 1f, 0.01f);
        panelRt.gameObject.AddComponent<RectMask2D>();
    }

    private void ResolveScenarioPreviewPanelReference()
    {
        if (scenarioPreviewPanelRt != null &&
            string.Equals(scenarioPreviewPanelRt.name, ScenarioPreviewPanelObjectName, System.StringComparison.Ordinal))
        {
            if (scenarioPreviewPanelImage == null)
                scenarioPreviewPanelImage = scenarioPreviewPanelRt.GetComponent<Image>();
            return;
        }

        GameObject panelGo = GameObject.Find(ScenarioPreviewPanelObjectName);
        if (panelGo == null || !IsInStoryProgressScene(panelGo)) return;

        scenarioPreviewPanelRt = panelGo.GetComponent<RectTransform>();
        scenarioPreviewPanelImage = panelGo.GetComponent<Image>();
    }

    private void EnsureScenarioIntroScroll()
    {
        ResolveScenarioPreviewPanelReference();
        if (scenarioPreviewPanelRt == null || scenarioPreviewTmp == null) return;

        RemoveLegacyScenarioIntroScrollViewport();

        Image panelImage = scenarioPreviewPanelImage;
        if (panelImage == null)
            panelImage = scenarioPreviewPanelRt.GetComponent<Image>();
        if (panelImage != null)
            panelImage.color = ProgressCardPrimaryColor;

        if (scenarioPreviewPanelRt.GetComponent<RectMask2D>() == null)
        {
            if (panelImage == null)
            {
                panelImage = scenarioPreviewPanelRt.gameObject.AddComponent<Image>();
                panelImage.color = ProgressCardPrimaryColor;
                scenarioPreviewPanelImage = panelImage;
            }

            scenarioPreviewPanelRt.gameObject.AddComponent<RectMask2D>();
        }

        scenarioIntroScrollRect = scenarioPreviewPanelRt.GetComponent<ScrollRect>();
        if (scenarioIntroScrollRect == null)
            scenarioIntroScrollRect = scenarioPreviewPanelRt.gameObject.AddComponent<ScrollRect>();

        Transform contentTr = scenarioPreviewPanelRt.Find(ScenarioIntroScrollContentName);
        if (contentTr == null)
        {
            GameObject contentGo = new GameObject(ScenarioIntroScrollContentName, typeof(RectTransform));
            contentGo.transform.SetParent(scenarioPreviewPanelRt, false);
            scenarioIntroScrollContentRt = contentGo.GetComponent<RectTransform>();
            ApplyScenarioIntroScrollContentFrame(GetScenarioIntroViewportHeight());
        }
        else
        {
            scenarioIntroScrollContentRt = contentTr as RectTransform;
            ApplyScenarioIntroScrollContentFrame(GetScenarioIntroViewportHeight());
        }

        if (scenarioPreviewTmp.transform.parent != scenarioIntroScrollContentRt)
            scenarioPreviewTmp.rectTransform.SetParent(scenarioIntroScrollContentRt, false);

        scenarioIntroScrollRect.viewport = scenarioPreviewPanelRt;
        scenarioIntroScrollRect.content = scenarioIntroScrollContentRt;
        scenarioIntroScrollRect.horizontal = false;
        scenarioIntroScrollRect.vertical = true;
        scenarioIntroScrollRect.movementType = ScrollRect.MovementType.Clamped;
        scenarioIntroScrollRect.scrollSensitivity = 24f;
        scenarioIntroScrollRect.inertia = true;
        scenarioIntroScrollRect.decelerationRate = 0.14f;
    }

    private void RemoveLegacyScenarioIntroScrollViewport()
    {
        if (scenarioPreviewPanelRt == null) return;

        Transform legacyViewport = scenarioPreviewPanelRt.Find(LegacyScenarioIntroScrollViewportName);
        if (legacyViewport == null) return;

        Transform contentInLegacy = legacyViewport.Find(ScenarioIntroScrollContentName);
        if (contentInLegacy != null)
        {
            contentInLegacy.SetParent(scenarioPreviewPanelRt, false);
            scenarioIntroScrollContentRt = contentInLegacy as RectTransform;
        }
        else if (scenarioPreviewTmp != null)
        {
            scenarioPreviewTmp.rectTransform.SetParent(scenarioPreviewPanelRt, false);
        }

        Destroy(legacyViewport.gameObject);
    }

    private IEnumerator CoSyncScenarioIntroScrollAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        SyncScenarioIntroScrollContentHeight();
    }

    private float GetScenarioIntroViewportHeight()
    {
        ResolveScenarioPreviewPanelReference();
        if (scenarioPreviewPanelRt == null) return 1f;
        return Mathf.Max(1f, scenarioPreviewPanelRt.rect.height);
    }

    private float GetScenarioIntroViewportWidth()
    {
        ResolveScenarioPreviewPanelReference();
        if (scenarioPreviewPanelRt == null) return 1f;
        return Mathf.Max(1f, scenarioPreviewPanelRt.rect.width);
    }

    private void ApplyScenarioIntroScrollContentFrame(float viewportHeight)
    {
        if (scenarioIntroScrollContentRt == null) return;

        scenarioIntroScrollContentRt.anchorMin = new Vector2(0f, 1f);
        scenarioIntroScrollContentRt.anchorMax = new Vector2(1f, 1f);
        scenarioIntroScrollContentRt.pivot = new Vector2(0.5f, 1f);
        scenarioIntroScrollContentRt.anchoredPosition = Vector2.zero;
        scenarioIntroScrollContentRt.sizeDelta = new Vector2(0f, Mathf.Max(1f, viewportHeight));
    }

    private void SyncScenarioIntroScrollContentHeight()
    {
        if (scenarioPreviewTmp == null || scenarioIntroScrollContentRt == null || scenarioPreviewPanelRt == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scenarioPreviewPanelRt);

        float viewportHeight = GetScenarioIntroViewportHeight();
        float viewportWidth = GetScenarioIntroViewportWidth();
        float innerWidth = Mathf.Max(1f, viewportWidth - DetailTextPad * 2f);

        ApplyScenarioIntroScrollContentFrame(viewportHeight);

        RectTransform textRt = scenarioPreviewTmp.rectTransform;
        textRt.anchorMin = new Vector2(0f, 1f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = Vector2.zero;

        scenarioPreviewTmp.enableWordWrapping = true;
        scenarioPreviewTmp.ForceMeshUpdate(true, true);
        Vector2 preferred = scenarioPreviewTmp.GetPreferredValues(innerWidth, viewportHeight);
        float textHeight = Mathf.Max(1f, preferred.y);
        float contentHeight = Mathf.Max(viewportHeight, textHeight + DetailTextPad * 2f);

        scenarioIntroScrollContentRt.sizeDelta = new Vector2(0f, contentHeight);

        textRt.offsetMin = new Vector2(DetailTextPad, -(textHeight + DetailTextPad));
        textRt.offsetMax = new Vector2(-DetailTextPad, -DetailTextPad);

        LayoutRebuilder.ForceRebuildLayoutImmediate(scenarioIntroScrollContentRt);

        if (scenarioIntroScrollRect != null)
            scenarioIntroScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ApplyScenarioIntroText(string richText)
    {
        if (scenarioPreviewTmp == null || string.IsNullOrEmpty(richText))
            return;

        scenarioPreviewTmp.richText = true;
        scenarioPreviewTmp.text = richText;
        EnsureScenarioIntroScroll();
        SyncScenarioIntroScrollContentHeight();
    }

    private void ApplyFooterActionButtonLayout(bool tutorialComplete, bool isStoryStageSelected)
    {
        Transform footerParent = ResolveFooterPanelTransform();
        if (footerParent == null || enterStageButton == null) return;

        ApplyFooterPanelLayout(footerParent as RectTransform);
        EnsureFooterButtonRow(footerParent);
        if (footerButtonRowRt == null) return;

        TMP_Text labelFontSource = ResolveUiFontSource();
        float buttonWidth = IsTallPhoneLandscape() ? FooterButtonWidthTallPhone : FooterButtonWidth;
        float buttonHeight = IsTallPhoneLandscape() ? FooterButtonHeightTallPhone : FooterButtonHeight;
        float buttonSpacing = IsTallPhoneLandscape() ? FooterButtonSpacingTallPhone : FooterButtonSpacing;
        HorizontalLayoutGroup layout = footerButtonRowRt.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.spacing = buttonSpacing;

        if (isStoryStageSelected)
        {
            if (replayIntroButton != null)
                replayIntroButton.gameObject.SetActive(false);
            if (goToHallButton != null)
                goToHallButton.gameObject.SetActive(false);

            ReparentFooterButton(enterStageButton, 0);
            StyleFooterPrimaryButton(enterStageButton, labelFontSource, buttonWidth, buttonHeight);
            AnchorFooterButtonRow(bottomRight: true);
        }
        else if (tutorialComplete)
        {
            if (goToHallButton == null)
                goToHallButton = EnsureGoToHallButton(enterStageButton);
            if (replayIntroButton == null)
                replayIntroButton = EnsureReplayIntroButton(enterStageButton);

            if (goToHallButton != null)
                goToHallButton.gameObject.SetActive(true);
            if (replayIntroButton != null)
                replayIntroButton.gameObject.SetActive(true);

            ReparentFooterButton(replayIntroButton, 0);
            ReparentFooterButton(goToHallButton, 1);
            ReparentFooterButton(enterStageButton, 2);

            StyleFooterSecondaryButton(replayIntroButton, labelFontSource, buttonWidth, buttonHeight, lighter: true);
            StyleFooterSecondaryButton(goToHallButton, labelFontSource, buttonWidth, buttonHeight, lighter: false);
            StyleFooterPrimaryButton(enterStageButton, labelFontSource, buttonWidth, buttonHeight);

            AnchorFooterButtonRow(bottomRight: true);
        }
        else
        {
            if (replayIntroButton != null)
                replayIntroButton.gameObject.SetActive(false);
            if (goToHallButton != null)
                goToHallButton.gameObject.SetActive(false);

            ReparentFooterButton(enterStageButton, 0);
            StyleFooterPrimaryButton(enterStageButton, labelFontSource, buttonWidth, buttonHeight);
            AnchorFooterButtonRow(bottomRight: true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(footerButtonRowRt);
    }

    private void ApplyFooterPanelLayout(RectTransform footerRt)
    {
        if (footerRt == null) return;

        Vector2 size = footerRt.sizeDelta;
        size.y = IsTallPhoneLandscape() ? FooterPanelTallPhoneHeight : FooterPanelDefaultHeight;
        footerRt.sizeDelta = size;

        Image footerImage = footerRt.GetComponent<Image>();
        if (footerImage != null)
            footerImage.color = ProgressCardSecondaryColor;
    }

    private Transform ResolveFooterPanelTransform()
    {
        GameObject panel = GameObject.Find(FooterPanelObjectName);
        if (panel != null && IsInStoryProgressScene(panel))
            return panel.transform;
        if (enterStageButton != null && enterStageButton.transform.parent != null)
            return enterStageButton.transform.parent;
        return null;
    }

    private void EnsureFooterButtonRow(Transform parent)
    {
        if (parent == null) return;

        Transform existing = parent.Find(FooterButtonRowObjectName);
        if (existing != null)
        {
            footerButtonRowRt = existing as RectTransform;
            return;
        }

        GameObject rowGo = new GameObject(
            FooterButtonRowObjectName,
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(ContentSizeFitter));
        rowGo.transform.SetParent(parent, false);
        rowGo.transform.SetAsLastSibling();
        footerButtonRowRt = rowGo.GetComponent<RectTransform>();

        HorizontalLayoutGroup layout = rowGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = FooterButtonSpacing;
        layout.padding = new RectOffset(0, 0, 6, 6);
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = rowGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void AnchorFooterButtonRow(bool bottomRight)
    {
        if (footerButtonRowRt == null) return;

        if (bottomRight)
        {
            footerButtonRowRt.anchorMin = new Vector2(1f, 0f);
            footerButtonRowRt.anchorMax = new Vector2(1f, 0f);
            footerButtonRowRt.pivot = new Vector2(1f, 0f);
            footerButtonRowRt.anchoredPosition = new Vector2(-FooterButtonRowInsetRight, FooterButtonRowInsetBottom);
        }
        else
        {
            footerButtonRowRt.anchorMin = new Vector2(0.5f, 0f);
            footerButtonRowRt.anchorMax = new Vector2(0.5f, 0f);
            footerButtonRowRt.pivot = new Vector2(0.5f, 0f);
            footerButtonRowRt.anchoredPosition = new Vector2(0f, FooterButtonRowInsetBottom);
        }

        footerButtonRowRt.localScale = Vector3.one;
    }

    private void ReparentFooterButton(Button button, int siblingIndex)
    {
        if (button == null || footerButtonRowRt == null) return;

        button.transform.SetParent(footerButtonRowRt, false);
        button.transform.SetSiblingIndex(siblingIndex);
        button.gameObject.SetActive(true);
    }

    private static void ConfigureFooterLayoutElement(Button button, float width, float height)
    {
        if (button == null) return;

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private static void StyleFooterPrimaryButton(Button button, TMP_Text fontSource, float width, float height)
    {
        if (button == null) return;
        ConfigureFooterLayoutElement(button, width, height);
        BattleUiColors.ApplyHallWineButton(button);
        TMP_Text label = PlotUiTextUtil.EnsureButtonLabel(button, null, fontSource);
        ApplyFooterButtonLabelColor(label);
    }

    private static void StyleFooterSecondaryButton(
        Button button,
        TMP_Text fontSource,
        float width,
        float height,
        bool lighter)
    {
        if (button == null) return;
        ConfigureFooterLayoutElement(button, width, height);

        Image img = button.targetGraphic as Image;
        if (img == null)
            img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.white;
            ColorBlock cb = button.colors;
            if (lighter)
            {
                cb.normalColor = BattleUiColors.BtnSecondaryLight;
                cb.highlightedColor = BattleUiColors.BtnSecondaryLightH;
                cb.pressedColor = BattleUiColors.BtnSecondaryLightP;
                cb.selectedColor = BattleUiColors.BtnSecondaryLightH;
            }
            else
            {
                cb.normalColor = BattleUiColors.BtnSecondary;
                cb.highlightedColor = BattleUiColors.BtnSecondaryH;
                cb.pressedColor = BattleUiColors.BtnSecondaryP;
                cb.selectedColor = BattleUiColors.BtnSecondaryH;
            }

            button.colors = cb;
        }

        TMP_Text label = PlotUiTextUtil.EnsureButtonLabel(button, null, fontSource);
        ApplyFooterButtonLabelColor(label);
    }

    private static void ApplyFooterButtonLabelColor(TMP_Text label)
    {
        if (label == null) return;

        if (label is TextMeshProUGUI ugui)
            SettingsUiFonts.ApplyTo(ugui);

        Color textColor = BattleUiColors.BtnFooterLabelText;
        label.color = textColor;
        label.faceColor = textColor;
    }

    private static Button EnsureReplayIntroButton(Button enterStageButtonRef)
    {
        GameObject existing = GameObject.Find(ReplayIntroButtonObjectName);
        if (existing != null)
            return EnsureButton(existing);

        Transform parent = ResolveFooterPanelTransformStatic(enterStageButtonRef);
        if (parent == null) return null;

        GameObject go = new GameObject(ReplayIntroButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Button btn = EnsureButton(go);
        TMP_Text fontSource = enterStageButtonRef != null
            ? enterStageButtonRef.GetComponentInChildren<TMP_Text>(true)
            : null;
        TMP_Text label = PlotUiTextUtil.EnsureButtonLabel(btn, null, fontSource);
        PlotUiTextUtil.ApplyButtonLabel(label, "重溫入門課", fontSource);
        return btn;
    }

    private static Button EnsureGoToHallButton(Button enterStageButtonRef)
    {
        GameObject existing = GameObject.Find(GoToHallButtonObjectName);
        if (existing != null)
            return EnsureButton(existing);

        Transform parent = ResolveFooterPanelTransformStatic(enterStageButtonRef);
        if (parent == null) return null;

        GameObject go = new GameObject(GoToHallButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Button btn = EnsureButton(go);
        TMP_Text fontSource = enterStageButtonRef != null
            ? enterStageButtonRef.GetComponentInChildren<TMP_Text>(true)
            : null;
        TMP_Text label = PlotUiTextUtil.EnsureButtonLabel(btn, null, fontSource);
        PlotUiTextUtil.ApplyButtonLabel(label, "前往大廳", fontSource);
        return btn;
    }

    private static Transform ResolveFooterPanelTransformStatic(Button enterStageButtonRef)
    {
        GameObject panel = GameObject.Find(FooterPanelObjectName);
        if (panel != null)
            return panel.transform;
        if (enterStageButtonRef != null && enterStageButtonRef.transform.parent != null)
            return enterStageButtonRef.transform.parent;
        return null;
    }

    private void LoadHall()
    {
        string target = string.IsNullOrWhiteSpace(hallSceneName) ? StoryProgressSession.HallSceneName : hallSceneName;
        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError("StoryProgressSceneController: hall scene not in Build Settings -> " + target);
            return;
        }
        SceneManager.LoadScene(target);
    }

    private void ScheduleDeferredBackButtonLayout()
    {
        if (!isActiveAndEnabled || leavingForHall) return;
        if (deferredBackButtonLayoutRoutine != null)
            StopCoroutine(deferredBackButtonLayoutRoutine);
        deferredBackButtonLayoutRoutine = StartCoroutine(DeferredBackButtonLayout());
    }

    private IEnumerator DeferredBackButtonLayout()
    {
        yield return null;
        deferredBackButtonLayoutRoutine = null;
        if (leavingForHall) yield break;

        if (backButton == null)
            backButton = EnsureBackButton();
        WireBackButton();
    }

    private void WireBackButton()
    {
        if (leavingForHall || backButton == null) return;

        BringBackButtonToFront();
        Canvas canvas = backButton.GetComponentInParent<Canvas>();
        ReturnButtonLayout.ApplyTo(backButton.GetComponent<RectTransform>(), canvas);

        ReturnButtonClickFeedback fx = backButton.GetComponent<ReturnButtonClickFeedback>();
        if (fx != null)
            Destroy(fx);

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    /// <summary>Story progress 專用返回鈕：點選後立刻移除並載入 hall。</summary>
    private void OnBackButtonClicked()
    {
        if (leavingForHall) return;
        leavingForHall = true;
        DestroyAllStoryProgressBackButtons();
        LoadHall();
    }

    private void DestroyAllStoryProgressBackButtons()
    {
        backButton = null;
        Scene scene = gameObject.scene;
        if (!scene.IsValid()) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null) continue;
                if (!IsStoryProgressBackButtonName(t.gameObject.name)) continue;
                Object.Destroy(t.gameObject);
            }
        }
    }

    private static bool IsStoryProgressBackButtonName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        for (int i = 0; i < BackButtonNameFragments.Length; i++)
        {
            if (objectName.Equals(BackButtonNameFragments[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private Button ResolveBackButton()
    {
        if (leavingForHall) return null;

        Scene scene = gameObject.scene;
        if (!scene.IsValid()) return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !IsStoryProgressBackButtonName(t.gameObject.name)) continue;
                return EnsureBackButtonGraphic(t.gameObject);
            }
        }

        return null;
    }

    private Button EnsureBackButton()
    {
        if (leavingForHall) return null;

        Button existing = ResolveBackButton();
        if (existing != null)
            return existing;

        Canvas canvas = FindStoryProgressCanvas();
        if (canvas == null) return null;

        Sprite sprite = StoryProgressUiSprites.GetReturnButtonSprite();
        var go = new GameObject(BackButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        ReturnButtonLayout.ApplyTo(rt, canvas);

        Image img = go.GetComponent<Image>();
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = true;
        if (sprite != null)
            img.sprite = sprite;
        else
            Debug.LogWarning("StoryProgressSceneController: return button sprite missing. Expected Resources/UI/return.");

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        return btn;
    }

    private static Button EnsureBackButtonGraphic(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.raycastTarget = true;

        Sprite sprite = StoryProgressUiSprites.GetReturnButtonSprite();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
            img.color = new Color(1f, 1f, 1f, 0.35f);

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.gameObject.SetActive(true);
        btn.interactable = true;
        Canvas canvas = go.GetComponentInParent<Canvas>();
        ReturnButtonLayout.ApplyTo(go.GetComponent<RectTransform>(), canvas);
        return btn;
    }

    private static Canvas FindStoryProgressCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas namedCanvas = null;
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !IsInStoryProgressScene(canvas.gameObject)) continue;
            if (canvas.gameObject.name == "Canvas")
                namedCanvas = canvas;
            else if (fallback == null || canvas.sortingOrder > fallback.sortingOrder)
                fallback = canvas;
        }
        return namedCanvas != null ? namedCanvas : fallback;
    }

    private void BringBackButtonToFront()
    {
        if (leavingForHall || backButton == null) return;
        RectTransform rt = backButton.GetComponent<RectTransform>();
        if (rt == null) return;
        Canvas canvas = backButton.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        if (rt.parent != canvas.transform)
            rt.SetParent(canvas.transform, true);
        rt.SetAsLastSibling();
    }

    private static Button FindButtonByLabelFragments(string objectName, string[] labelFragments)
    {
        GameObject named = GameObject.Find(objectName);
        if (named != null && IsInStoryProgressScene(named))
            return EnsureButton(named);

        TMP_Text[] allTmp = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTmp.Length; i++)
        {
            TMP_Text tmp = allTmp[i];
            if (tmp == null || !IsInStoryProgressScene(tmp.gameObject)) continue;
            if (IsStoryProgressDetailText(tmp)) continue;
            if (!TextMatchesLabelFragments(tmp.text, labelFragments)) continue;

            Button existingBtn = tmp.GetComponentInParent<Button>();
            if (existingBtn != null)
            {
                existingBtn.gameObject.name = objectName;
                tmp.raycastTarget = false;
                return existingBtn;
            }

            if (!IsUnderNamedAncestor(tmp.transform, "Panel")) continue;

            Transform host = tmp.transform.parent;
            if (host == null) continue;

            host.gameObject.name = objectName;
            tmp.raycastTarget = false;
            return EnsureButton(host.gameObject);
        }

        GameObject panel = GameObject.Find("Panel");
        if (panel != null)
        {
            Image[] images = panel.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || !IsInStoryProgressScene(img.gameObject)) continue;
                TMP_Text label = img.GetComponentInChildren<TMP_Text>(true);
                if (label == null || !TextMatchesLabelFragments(label.text, labelFragments)) continue;

                img.gameObject.name = objectName;
                label.raycastTarget = false;
                return EnsureButton(img.gameObject);
            }
        }

        return null;
    }

    private static bool TextMatchesLabelFragments(string text, string[] fragments)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string plain = StripRichTextTags(text);
        for (int i = 0; i < fragments.Length; i++)
        {
            if (plain.Contains(fragments[i]))
                return true;
        }
        return false;
    }

    private static string StripRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", string.Empty);
    }

    private static bool IsStoryProgressDetailText(TMP_Text tmp) =>
        IsUnderNamedAncestor(tmp.transform, "Scenario Preview") ||
        IsUnderNamedAncestor(tmp.transform, "View Level Flow");

    private static bool IsUnderNamedAncestor(Transform t, string ancestorName)
    {
        while (t != null)
        {
            if (t.gameObject.name == ancestorName) return true;
            t = t.parent;
        }
        return false;
    }

    private static bool IsInStoryProgressScene(GameObject go) =>
        go != null && go.scene.IsValid() && go.scene.name == StoryProgressSession.StoryProgressSceneName;

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

    private static TMP_Text FindTmpContaining(string fragment)
    {
        TMP_Text[] all = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text t = all[i];
            if (t == null || !IsInStoryProgressScene(t.gameObject)) continue;
            if (!string.IsNullOrEmpty(t.text) && t.text.Contains(fragment))
                return t;
        }
        return null;
    }

    private static TMP_Text FindTmpByExactParentName(string parentName, string childName)
    {
        GameObject parent = GameObject.Find(parentName);
        if (parent == null) return null;
        Transform child = parent.transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text FindTmpByExactText(string exact)
    {
        if (string.IsNullOrEmpty(exact)) return null;

        TMP_Text[] all = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text t = all[i];
            if (t == null || !IsInStoryProgressScene(t.gameObject)) continue;
            if (string.Equals(t.text, exact, System.StringComparison.Ordinal))
                return t;
        }

        return null;
    }

    private static void CleanupStrayRuntimeStoryProgressUi()
    {
        GameObject strayRewards = GameObject.Find(ScenarioRewardsObjectName);
        if (strayRewards != null && IsInStoryProgressScene(strayRewards))
            Object.Destroy(strayRewards);
    }

    private static TMP_Text FindViewLevelFlowTitleTmp()
    {
        GameObject viewFlow = GameObject.Find(StoryProgressLevelCopy.ViewLevelFlowPanelName);
        if (viewFlow == null || !IsInStoryProgressScene(viewFlow)) return null;

        Transform root = viewFlow.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null) continue;
            if (child.name == "Scenario Preview" || child.name == "Image") continue;
            TMP_Text tmp = child.GetComponent<TMP_Text>();
            if (tmp != null) return tmp;
        }

        return null;
    }

    private static TMP_Text FindViewLevelFlowRewardsTmp()
    {
        GameObject viewFlow = GameObject.Find(StoryProgressLevelCopy.ViewLevelFlowPanelName);
        if (viewFlow == null || !IsInStoryProgressScene(viewFlow)) return null;

        Transform root = viewFlow.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null || child.name != "Image") continue;
            TMP_Text tmp = child.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) return tmp;
        }

        TMP_Text byPlaceholder = FindTmpByExactText(StoryProgressLevelCopy.RewardsPlaceholderText);
        if (byPlaceholder != null && IsUnderNamedAncestor(byPlaceholder.transform, StoryProgressLevelCopy.ViewLevelFlowPanelName))
            return byPlaceholder;

        return null;
    }

    private static TMP_Text FindFooterRewardsTmp()
    {
        TMP_Text byPlaceholder = FindTmpByExactText("關卡介紹一句話");
        if (byPlaceholder != null && IsUnderNamedAncestor(byPlaceholder.transform, FooterPanelObjectName))
            return byPlaceholder;

        TMP_Text byParent = FindTmpUnderParentNamed(FooterPanelObjectName);
        if (byParent != null)
            return byParent;

        return null;
    }

    private IEnumerator DeferredRefreshChapterMapStatus()
    {
        const int maxFrames = 10;
        for (int frame = 0; frame < maxFrames; frame++)
        {
            yield return null;
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            TutorialProgressState.EnsureSlotIntroProgressConsistent(slot);
            HarborTrainingProgressState.EnsureSlotHarborProgressConsistent(slot);
            StoryProgressWorldMapRuntime.RequestRefreshProgress();
            AutoBindUi();
            RefreshPresentation();
            if (UnityEngine.Object.FindFirstObjectByType<StoryProgressWorldMapRuntime>() != null)
                break;
        }
    }

    private static TMP_Text FindChapterMapTitleTmp()
    {
        TMP_Text[] all = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TMP_Text fallback = null;
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text t = all[i];
            if (t == null || !IsInStoryProgressScene(t.gameObject)) continue;
            if (IsUnderNamedAncestor(t.transform, StoryProgressLevelCopy.ViewLevelFlowPanelName)) continue;
            if (IsUnderNamedAncestor(t.transform, "Panel")) continue;

            string text = StripRichTextTags(t.text);
            if (string.IsNullOrEmpty(text)) continue;

            bool exactTitle = string.Equals(text, ChapterTitleDefault, System.StringComparison.Ordinal);
            bool containsChapter = text.Contains("1-1");
            if (!exactTitle && !containsChapter) continue;

            Transform parent = t.transform.parent;
            if (parent != null && parent.name == "Image")
                return t;

            if (fallback == null)
                fallback = t;
        }

        return fallback;
    }

    private void EnsureChapterMapStatusLabel()
    {
        chapterMapTitleTmp = FindChapterMapTitleTmp();
        Transform iconRoot = chapterMapTitleTmp != null ? chapterMapTitleTmp.transform.parent : null;
        if (iconRoot == null)
        {
            chapterMapStatusTmp = null;
            return;
        }

        Transform existing = iconRoot.Find(ChapterMapStatusObjectName);
        if (existing != null)
        {
            chapterMapStatusTmp = existing.GetComponent<TMP_Text>();
        }
        else if (chapterMapStatusTmp == null || chapterMapStatusTmp.transform.parent != iconRoot)
        {
            var go = new GameObject(ChapterMapStatusObjectName, typeof(RectTransform));
            go.transform.SetParent(iconRoot, false);
            go.transform.SetAsLastSibling();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 58f);
            rt.sizeDelta = new Vector2(220f, 40f);

            chapterMapStatusTmp = go.AddComponent<TextMeshProUGUI>();
        }

        if (chapterMapStatusTmp != null)
        {
            chapterMapStatusTmp.gameObject.SetActive(true);
            ApplyUiFont(chapterMapStatusTmp);
        }
    }

    private void ApplyChapterMapStatus(TMP_Text tmp)
    {
        if (tmp == null) return;

        ApplyUiFont(tmp);

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        string label = StoryProgressLevelCopy.ResolveMapStatusLabelForSlot(slot);
        TutorialProgressState.GetAcademyIntroProgressForDisplay(slot, out bool plotComplete, out bool battleComplete);
        bool harborCleared = HarborTrainingProgressState.IsHarborCombatCleared(slot);
        tmp.richText = false;
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        if (harborCleared)
        {
            Color clear = StoryTextStyle.HexToColor(StoryTextStyle.HighlightHex);
            tmp.color = clear;
            tmp.faceColor = clear;
        }
        else if (plotComplete && battleComplete)
        {
            Color ready = StoryTextStyle.HexToColor(StoryTextStyle.EmphasisHex);
            tmp.color = ready;
            tmp.faceColor = ready;
        }
        else if (plotComplete)
        {
            Color progress = StoryTextStyle.HexToColor(StoryTextStyle.EmphasisHex);
            tmp.color = progress;
            tmp.faceColor = progress;
        }
        else
        {
            Color fresh = StoryTextStyle.HexToColor(StoryTextStyle.MutedHex);
            tmp.color = fresh;
            tmp.faceColor = fresh;
        }

        tmp.ForceMeshUpdate(true, true);
    }

    private void HideLegacyChapterMapLabels()
    {
        if (chapterMapTitleTmp == null)
            chapterMapTitleTmp = FindChapterMapTitleTmp();

        if (chapterMapTitleTmp != null)
        {
            chapterMapTitleTmp.gameObject.SetActive(false);

            Transform iconRoot = chapterMapTitleTmp.transform.parent;
            if (iconRoot != null)
            {
                Transform legacyStatus = iconRoot.Find(ChapterMapStatusObjectName);
                if (legacyStatus != null)
                    legacyStatus.gameObject.SetActive(false);
            }
        }

        if (chapterMapStatusTmp != null)
            chapterMapStatusTmp.gameObject.SetActive(false);
    }

    private void ApplyUiFont(TMP_Text tmp)
    {
        if (tmp == null) return;
        TMP_Text source = ResolveUiFontSource();
        if (source != null && source.font != null)
        {
            tmp.font = source.font;
            tmp.fontSharedMaterial = source.fontSharedMaterial;
            return;
        }

        if (tmp is TextMeshProUGUI ugui)
            SettingsUiFonts.ApplyTo(ugui);
    }

    private TMP_Text ResolveUiFontSource()
    {
        if (enterStageButtonLabel != null && enterStageButtonLabel.font != null)
            return enterStageButtonLabel;
        if (scenarioPreviewTmp != null && scenarioPreviewTmp.font != null)
            return scenarioPreviewTmp;
        if (chapterSummaryTmp != null && chapterSummaryTmp.font != null)
            return chapterSummaryTmp;
        if (levelPanelTitleTmp != null && levelPanelTitleTmp.font != null)
            return levelPanelTitleTmp;
        return chapterMapTitleTmp;
    }

    private static TMP_Text FindTmpUnderParentNamed(string parentObjectName)
    {
        GameObject parent = GameObject.Find(parentObjectName);
        if (parent == null || !IsInStoryProgressScene(parent)) return null;
        return parent.GetComponentInChildren<TMP_Text>(true);
    }

    private static void ApplyHarborBulletinText(TMP_Text tmp, string richText)
    {
        if (tmp == null) return;

        tmp.gameObject.SetActive(true);
        ApplyHarborBulletinLayout(tmp.rectTransform);
        ApplyHarborBulletinFont(tmp);

        tmp.richText = true;
        tmp.text = richText ?? string.Empty;
        tmp.color = ProgressBulletinBodyColor;
        tmp.faceColor = ProgressBulletinBodyColor;
        tmp.fontSize = 30f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.lineSpacing = 0f;
        tmp.paragraphSpacing = 0f;
        tmp.characterSpacing = 0.5f;
        tmp.margin = new Vector4(HarborBulletinTextMarginLeft, 8f, 16f, 8f);
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.ForceMeshUpdate(true, true);
    }

    private void ApplyFooterRewardsText(TMP_Text tmp, string richText)
    {
        if (tmp == null) return;

        tmp.gameObject.SetActive(true);
        ApplyFooterRewardsLayout(tmp.rectTransform);
        ApplyStoryProgressDetailFont(tmp);

        tmp.richText = true;
        tmp.text = NormalizeFooterRewardsRichText(richText);
        tmp.color = FooterRewardBodyColor;
        tmp.faceColor = FooterRewardBodyColor;
        tmp.fontSize = IsTallPhoneLandscape() ? 24f : 26f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.lineSpacing = IsTallPhoneLandscape() ? 8f : 6f;
        tmp.paragraphSpacing = IsTallPhoneLandscape() ? 12f : 10f;
        tmp.characterSpacing = 0.2f;
        tmp.margin = new Vector4(18f, 10f, 18f, 10f);
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20f;
        tmp.fontSizeMax = IsTallPhoneLandscape() ? 25f : 27f;
        tmp.outlineWidth = 0.12f;
        tmp.outlineColor = FooterRewardShadowColor;
        tmp.ForceMeshUpdate(true, true);
    }

    private void ApplyFooterRewardsLayout(RectTransform rt)
    {
        if (rt == null) return;

        float reserveRight = ResolveFooterRewardsReserveRight();
        float topPad = IsTallPhoneLandscape() ? 14f : 12f;
        float bottomPad = IsTallPhoneLandscape() ? 18f : 14f;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = new Vector2(HarborBulletinPadLeft + HarborBulletinBreathingLeft, bottomPad);
        rt.offsetMax = new Vector2(-reserveRight, -topPad);
        rt.localScale = Vector3.one;
        rt.SetAsFirstSibling();
    }

    private float ResolveFooterRewardsReserveRight()
    {
        float reserve = IsTallPhoneLandscape() ? 340f : 420f;
        if (footerButtonRowRt == null)
            return reserve;

        Canvas.ForceUpdateCanvases();
        reserve = Mathf.Max(
            reserve,
            footerButtonRowRt.rect.width + (IsTallPhoneLandscape() ? 84f : 112f));
        return reserve;
    }

    private static string NormalizeFooterRewardsRichText(string richText)
    {
        if (string.IsNullOrEmpty(richText))
            return string.Empty;

        return richText
            .Replace("#8F6A36", "#F4C768")
            .Replace("#2C6F8F", "#86F0D5")
            .Replace("#7A4A92", "#D9A8FF")
            .Replace("#4F5B62", "#D6E2E6");
    }

    private static bool IsTallPhoneLandscape()
    {
        float h = Mathf.Max(1f, Screen.height);
        return (Screen.width / h) >= 1.95f;
    }

    private static void ApplyHarborBulletinFont(TMP_Text tmp)
    {
        ApplyStoryProgressDetailFont(tmp);
    }

    /// <summary>關卡說明／通關獎勵等需完整中文的 TMP（Noto Sans TC）。</summary>
    private static void ApplyStoryProgressDetailFont(TMP_Text tmp)
    {
        if (tmp == null) return;

        TMP_FontAsset font = ResolveStoryProgressDetailFont();
        if (font != null)
            tmp.font = font;
    }

    private static TMP_FontAsset ResolveStoryProgressDetailFont()
    {
        TMP_FontAsset font = SettingsUiFonts.ResolveParameterDetailsFont();
        if (font != null && BuildbeckUiFonts.FontSupportsText(font, StoryProgressLevelCopy.RewardsFontGlyphProbe))
            return font;

        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null && BuildbeckUiFonts.FontSupportsText(buildbeck, StoryProgressLevelCopy.RewardsFontGlyphProbe))
            return buildbeck;

        return font;
    }

    private static void ApplyHarborBulletinLayout(RectTransform rt)
    {
        if (rt == null) return;

        float leftInset = HarborBulletinPadLeft + HarborBulletinBreathingLeft;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(leftInset, 12f);
        rt.offsetMax = new Vector2(-HarborBulletinReserveRight, -12f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void ApplyStoryProgressPlainText(TMP_Text tmp, string text, float fontSize)
    {
        if (tmp == null) return;
        tmp.richText = false;
        tmp.text = text ?? string.Empty;
        tmp.color = Color.white;
        tmp.faceColor = Color.white;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.ForceMeshUpdate(true, true);
    }

    private static void ApplyStoryProgressBodyText(TMP_Text tmp, string richText)
    {
        if (tmp == null) return;
        ApplyDetailTextFillLayout(tmp.rectTransform, DetailTextPad);
        ApplyStoryProgressDetailFont(tmp);
        tmp.richText = true;
        tmp.text = richText ?? string.Empty;
        Color body = ProgressBodyColor;
        tmp.color = body;
        tmp.faceColor = body;
        tmp.fontSize = 28f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.wordWrappingRatios = 0.35f;
        tmp.lineSpacing = 8f;
        tmp.paragraphSpacing = 14f;
        tmp.characterSpacing = 0.4f;
        tmp.margin = Vector4.zero;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = true;
        tmp.ForceMeshUpdate(true, true);
    }

    private static void ApplyStoryProgressRewardsText(TMP_Text tmp, string richText)
    {
        if (tmp == null) return;
        ApplyDetailTextFillLayout(tmp.rectTransform, DetailTextPad);
        ApplyStoryProgressDetailFont(tmp);
        tmp.richText = true;
        tmp.text = richText ?? string.Empty;
        Color body = ProgressBodyColor;
        tmp.color = body;
        tmp.faceColor = body;
        tmp.fontSize = 28f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.wordWrappingRatios = 0.35f;
        tmp.lineSpacing = 8f;
        tmp.paragraphSpacing = 14f;
        tmp.characterSpacing = 0.5f;
        tmp.margin = Vector4.zero;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.ForceMeshUpdate(true, true);
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainPlotSceneController : MonoBehaviour
{
    public enum PlotAdvanceKind
    {
        /// <summary>點擊畫面空白／任意處前進（由 tapNextStepIndex 指定下一步）。</summary>
        TapToContinue = 0,
        /// <summary>顯示 1～3 個選項按鈕，由玩家選擇分支。</summary>
        PlayerChoice = 1
    }

    [System.Serializable]
    public class PlotStep
    {
        [Header("Story Content")]
        public string speakerName;
        [TextArea(2, 6)] public string dialogueText;
        public Sprite backgroundSprite;
        public Sprite characterASprite;
        public Sprite characterBSprite;
        public Sprite characterCSprite;

        [Header("Advance")]
        public PlotAdvanceKind advanceKind = PlotAdvanceKind.PlayerChoice;
        [Tooltip("TapToContinue：點擊後跳轉的步驟索引；-1 且 tapEndsPlot 時結束劇情。")]
        public int tapNextStepIndex = -1;
        public bool tapEndsPlot;

        [Header("Player Choices (PlayerChoice only)")]
        public string choice1Text = "選擇一";
        public string choice2Text = "選擇二";
        public string choice3Text = "選擇三";

        [Header("Choice Next Step Index (-1 = hide / terminal)")]
        public int choice1Next = -1;
        public int choice2Next = -1;
        public int choice3Next = -1;
    }

    private const float ChoiceButtonWidth = 366.94f;
    private const float ChoiceButtonHeight = 64.24f;
    private const float ChoiceButtonX = 912f;
    private static readonly float[] ChoiceButtonY = { -311.07f, -413f, -516.5f };
    private static readonly string[] SceneChoiceButtonNames = { "玩家選擇按鈕1", "玩家選擇按鈕2", "玩家選擇按鈕3" };
    private static readonly string[] LegacyRuntimeChoiceNames = { "PlotRuntimeChoice1", "PlotRuntimeChoice2", "PlotRuntimeChoice3" };

    [Header("Scene UI Refs (auto-bind by name if empty)")]
    [SerializeField] private TMP_Text dialogueTextTmp;
    [SerializeField] private TMP_Text speakerNameTmp;
    [SerializeField] private Image dialoguePanelImage;
    [SerializeField] private Image plotBackgroundImage;
    [SerializeField] private Image characterAImage;
    [SerializeField] private Image characterBImage;
    [SerializeField] private Image characterCImage;
    [SerializeField] private Button choice1Button;
    [SerializeField] private Button choice2Button;
    [SerializeField] private Button choice3Button;
    [SerializeField] private TMP_Text choice1TextTmp;
    [SerializeField] private TMP_Text choice2TextTmp;
    [SerializeField] private TMP_Text choice3TextTmp;

    [Header("Script Data")]
    [SerializeField] private List<PlotStep> steps = new List<PlotStep>();
    [SerializeField] private int startStepIndex;

    [Header("Dialogue Typewriter")]
    [SerializeField] private float dialogueCharactersPerSecond = 9f;

    private int currentStepIndex = -1;
    private bool launchedFromStoryProgress;
    private bool plotBegun;
    private Button tapToContinueButton;
    private TMP_Text tapContinueHintTmp;
    private Button skipPlotButton;
    private PlotDialogueTypewriter dialogueTypewriter;
    private PlotDialogueTypewriterSfx plotTypewriterSfx;
    private PlotMenuClickSfx plotMenuClickSfx;
    private PlotAdvanceKind currentStepAdvanceKind;
    private PlotBackgroundMusicPlayer plotBgm;

    private static bool mainPlotSceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallMainPlotSceneHook()
    {
        if (mainPlotSceneHookInstalled) return;
        mainPlotSceneHookInstalled = true;
        SceneManager.sceneLoaded += OnMainPlotSceneLoaded;
        TryEnsureMainPlotController(SceneManager.GetActiveScene());
    }

    private static void OnMainPlotSceneLoaded(Scene scene, LoadSceneMode mode) => TryEnsureMainPlotController(scene);

    private static void TryEnsureMainPlotController(Scene scene)
    {
        if (!scene.IsValid() || scene.name != StoryProgressSession.MainPlotSceneName) return;

        MainPlotSceneController ctrl = Object.FindFirstObjectByType<MainPlotSceneController>();
        if (ctrl == null)
        {
            GameObject host = new GameObject("MainPlotSceneController");
            SceneManager.MoveGameObjectToScene(host, scene);
            ctrl = host.AddComponent<MainPlotSceneController>();
        }

        if (StoryProgressSession.TryConsumePendingPlotSteps(out List<PlotStep> injected))
            ctrl.ApplyRuntimeSteps(injected, true);
    }

    private void Awake()
    {
        if (StoryProgressSession.TryConsumePendingPlotSteps(out List<PlotStep> injected))
            ApplyRuntimeSteps(injected, true);

        PreparePlotSceneUi();
        EnsurePlotTypewriterSfx();
        EnsurePlotMenuClickSfx();
    }

    private void Start()
    {
        if (!plotBegun && steps != null && steps.Count > 0)
            BeginPlot();

        if (plotBegun && ShouldPlayTutorialPlotBgm())
            TryPlayTutorialPlotBgm();
    }

    private void Update()
    {
        if (dialogueTypewriter == null || !dialogueTypewriter.IsActive)
            return;

        bool wasComplete = dialogueTypewriter.IsComplete;
        dialogueTypewriter.Tick(Time.deltaTime);
        if (!wasComplete && dialogueTypewriter.IsComplete)
            OnDialogueTypewriterFinished();
    }

    public void ApplyRuntimeSteps(List<PlotStep> newSteps, bool fromStoryProgress)
    {
        steps = newSteps;
        launchedFromStoryProgress = fromStoryProgress;
        startStepIndex = 0;
        plotBegun = false;
    }

    public void BeginPlot()
    {
        if (steps == null || steps.Count == 0)
        {
            bool fromStoryProgress = launchedFromStoryProgress || StoryProgressSession.TutorialPlotBgmRequested;
            ApplyRuntimeSteps(TutorialPlotScriptFactory.BuildTutorialPlotSteps(), fromStoryProgress);
        }

        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("MainPlotSceneController: no steps to show.");
            return;
        }

        plotBegun = true;
        PreparePlotSceneUi();
        ShowStep(Mathf.Clamp(startStepIndex, 0, steps.Count - 1));
    }

    /// <summary>僅 1-1 從遊戲進度進入的教學劇情播放專屬 BGM。</summary>
    private bool ShouldPlayTutorialPlotBgm() => launchedFromStoryProgress;

    private void TryPlayTutorialPlotBgm()
    {
        if (!ShouldPlayTutorialPlotBgm()) return;

        PlotBackgroundMusicPlayer bgm = PlotBackgroundMusicPlayer.FindInMainPlotScene();
        if (bgm == null)
        {
            GameDevLog.LogWarning("MainPlotSceneController: PlotBackgroundMusicPlayer not found on Main Plot Main Camera.");
            return;
        }

        plotBgm = bgm;
        plotBgm.PlayTutorialPlotBgm();
    }

    private void StopTutorialPlotBgm() => StoryProgressSession.EndTutorialPlotBgmSession();

    private void OnDestroy()
    {
        StopTutorialPlotBgm();
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();
    }

    private void PreparePlotSceneUi()
    {
        AutoBindUiIfMissing();
        FixCanvasRootScale();
        WireSkipButton();
        EnsureTapToContinueUi();
        EnsureDialoguePanelVisible();
        DisableDialogueChromeRaycasts();
    }

    private void DisableDialogueChromeRaycasts()
    {
        if (dialoguePanelImage != null)
            dialoguePanelImage.raycastTarget = false;

        if (plotBackgroundImage != null)
            plotBackgroundImage.raycastTarget = false;

        if (dialogueTextTmp != null)
            dialogueTextTmp.raycastTarget = false;
        if (speakerNameTmp != null)
            speakerNameTmp.raycastTarget = false;
    }

    private void AutoBindUiIfMissing()
    {
        DestroyStalePlotChoiceButtonsRoot();

        if (dialogueTextTmp == null) dialogueTextTmp = FindTmpInPlotScene("劇本文字");
        if (speakerNameTmp == null) speakerNameTmp = FindTmpInPlotScene("角色名稱");
        if (dialoguePanelImage == null) dialoguePanelImage = FindImageInPlotScene("文字背板");
        if (plotBackgroundImage == null) plotBackgroundImage = FindImageInPlotScene("劇情背景");
        if (characterAImage == null) characterAImage = FindImageInPlotScene("角色A");
        if (characterBImage == null) characterBImage = FindImageInPlotScene("角色B");
        if (characterCImage == null) characterCImage = FindImageInPlotScene("角色C");

        DestroyLegacyRuntimeChoiceButtons();
        BindSceneChoiceButtons();
        WireChoiceButtons();
    }

    private void BindSceneChoiceButtons()
    {
        choice1Button = BindOrCreateChoiceButton(0);
        choice2Button = BindOrCreateChoiceButton(1);
        choice3Button = BindOrCreateChoiceButton(2);

        choice1TextTmp = PlotUiTextUtil.EnsureButtonLabel(choice1Button, choice1TextTmp, dialogueTextTmp);
        choice2TextTmp = PlotUiTextUtil.EnsureButtonLabel(choice2Button, choice2TextTmp, dialogueTextTmp);
        choice3TextTmp = PlotUiTextUtil.EnsureButtonLabel(choice3Button, choice3TextTmp, dialogueTextTmp);

        Button[] buttons = { choice1Button, choice2Button, choice3Button };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            LayoutOneChoiceButton(buttons[i], i);
            buttons[i].gameObject.SetActive(false);
            buttons[i].interactable = false;
            SetButtonRaycastTargets(buttons[i], false);
        }
    }

    private Button BindOrCreateChoiceButton(int index)
    {
        if (index < 0 || index >= SceneChoiceButtonNames.Length) return null;

        Button button = FindButtonInPlotScene(SceneChoiceButtonNames[index]);
        if (button != null) return button;

        Canvas canvas = FindPlotCanvas();
        if (canvas == null) return null;

        var go = new GameObject(SceneChoiceButtonNames[index], typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);
        button = EnsureButton(go);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        labelGo.AddComponent<TextMeshProUGUI>();

        LayoutOneChoiceButton(button, index);
        return button;
    }

    private void EnsureChoiceButtonsReady()
    {
        if (choice1Button != null && choice2Button != null && choice3Button != null)
            return;

        BindSceneChoiceButtons();
        WireChoiceButtons();
    }

    private static Canvas FindPlotCanvas()
    {
        Scene plotScene = ResolvePlotScene();
        if (plotScene.IsValid())
        {
            GameObject[] roots = plotScene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Canvas canvas = roots[r].GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas;
            }
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.scene.name == StoryProgressSession.MainPlotSceneName)
                return canvas;
        }

        return null;
    }

    private static void DestroyLegacyRuntimeChoiceButtons()
    {
        for (int i = 0; i < LegacyRuntimeChoiceNames.Length; i++)
        {
            GameObject go = FindInPlotScene(LegacyRuntimeChoiceNames[i]);
            if (go != null)
                Object.Destroy(go);
        }
    }

    /// <summary>Tap 模式：關閉場景預設占位選項，避免與點擊繼續搶焦。</summary>
    private void SuppressPlaceholderChoiceButtons()
    {
        Scene plotScene = ResolvePlotScene();
        if (!plotScene.IsValid()) return;

        GameObject[] roots = plotScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Button[] buttons = roots[r].GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null) continue;
                if (IsBoundPlotChoiceButton(button)) continue;
                if (!IsStrayPlotChoiceButton(button)) continue;

                button.gameObject.SetActive(false);
                button.interactable = false;
                SetButtonRaycastTargets(button, false);
            }
        }
    }

    private bool IsBoundPlotChoiceButton(Button button)
    {
        return button != null
               && (button == choice1Button || button == choice2Button || button == choice3Button);
    }

    private static bool IsStrayPlotChoiceButton(Button button)
    {
        if (button == null) return false;

        string objectName = button.gameObject.name;
        if (objectName.StartsWith("PlotRuntimeChoice"))
            return true;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null || string.IsNullOrWhiteSpace(label.text)) return false;
        return IsDefaultPlaceholderChoice(label.text.Trim());
    }

    private static void DestroyStalePlotChoiceButtonsRoot()
    {
        GameObject rootGo = GameObject.Find("PlotChoiceButtonsRoot");
        if (rootGo == null) return;

        Canvas canvas = FindPlotCanvas();
        Transform canvasTransform = canvas != null ? canvas.transform : null;

        Transform root = rootGo.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (canvasTransform != null)
                child.SetParent(canvasTransform, true);
        }

        Object.Destroy(rootGo);
    }

    private static void FixCanvasRootScale()
    {
        Canvas canvas = FindPlotCanvas();
        if (canvas == null) return;
        RectTransform rt = canvas.GetComponent<RectTransform>();
        if (rt != null && rt.localScale.sqrMagnitude < 0.001f)
            rt.localScale = Vector3.one;
    }

    private void LayoutOneChoiceButton(Button button, int layoutIndex)
    {
        if (button == null || layoutIndex < 0 || layoutIndex >= ChoiceButtonY.Length) return;

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(ChoiceButtonX, ChoiceButtonY[layoutIndex]);
        rt.sizeDelta = new Vector2(ChoiceButtonWidth, ChoiceButtonHeight);
        rt.localScale = Vector3.one;

        BattleUiColors.ApplyHallWineButton(button);
        button.interactable = true;
        SetButtonRaycastTargets(button, true);
    }

    private void BringVisibleChoiceButtonsToFront()
    {
        Button[] buttons = { choice1Button, choice2Button, choice3Button };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.gameObject.activeInHierarchy)
                button.transform.SetAsLastSibling();
        }
    }

    private static void SetButtonRaycastTargets(Button button, bool raycast)
    {
        if (button == null) return;
        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
            images[i].raycastTarget = raycast;
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

    private void WireChoiceButtons()
    {
        WireOneChoiceButton(choice1Button, 0);
        WireOneChoiceButton(choice2Button, 1);
        WireOneChoiceButton(choice3Button, 2);
    }

    private void WireOneChoiceButton(Button button, int choiceIndex)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        int captured = choiceIndex;
        button.onClick.AddListener(() =>
        {
            PlayPlotMenuClickSound();
            OnChoiceClicked(captured);
        });
    }

    private void WireSkipButton()
    {
        skipPlotButton = FindButtonInPlotScene("略過本段劇情");
        if (skipPlotButton == null) return;
        LayoutSkipButtonTopRight();
        BattleUiColors.ApplyHallWineButton(skipPlotButton);
        TMP_Text skipLabel = PlotUiTextUtil.EnsureButtonLabel(skipPlotButton, null, dialogueTextTmp);
        PlotUiTextUtil.ApplyButtonLabel(skipLabel, "略過本段劇情", dialogueTextTmp);
        skipPlotButton.onClick.RemoveAllListeners();
        skipPlotButton.onClick.AddListener(() =>
        {
            PlayPlotMenuClickSound();
            OnSkipPlotClicked();
        });
    }

    private void BringSkipButtonToFront()
    {
        if (skipPlotButton != null)
            skipPlotButton.transform.SetAsLastSibling();
    }

    private void LayoutSkipButtonTopRight()
    {
        if (skipPlotButton == null) return;
        RectTransform rt = skipPlotButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(220f, 52f);
    }

    private void LayoutSingleChoiceButton(Button button, float y)
    {
        if (button == null) return;
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(ChoiceButtonWidth, ChoiceButtonHeight);
        button.transform.SetAsLastSibling();
    }

    private void OnSkipPlotClicked()
    {
        dialogueTypewriter?.Complete();
        HideAllChoiceButtons();
        ApplyTapToContinueUi(false);
        FinishPlotAndReturn(skippedPlot: true);
    }

    private void EnsureTapToContinueUi()
    {
        if (tapToContinueButton != null) return;

        Canvas canvas = FindPlotCanvas();
        if (canvas == null) return;

        var overlayGo = new GameObject("PlotTapToContinue", typeof(RectTransform), typeof(Image), typeof(Button));
        overlayGo.transform.SetParent(canvas.transform, false);
        overlayGo.SetActive(false);

        RectTransform rt = overlayGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = overlayGo.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        tapToContinueButton = overlayGo.GetComponent<Button>();
        tapToContinueButton.transition = Selectable.Transition.None;
        tapToContinueButton.targetGraphic = img;
        tapToContinueButton.onClick.RemoveAllListeners();
        tapToContinueButton.onClick.AddListener(() =>
        {
            PlayPlotMenuClickSound();
            OnTapToContinue();
        });
        overlayGo.SetActive(false);
        img.raycastTarget = false;

        var hintGo = new GameObject("PlotTapContinueHint", typeof(RectTransform));
        hintGo.transform.SetParent(canvas.transform, false);
        RectTransform hintRt = hintGo.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 48f);
        hintRt.sizeDelta = new Vector2(480f, 36f);

        tapContinueHintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        tapContinueHintTmp.raycastTarget = false;
        tapContinueHintTmp.fontSize = 20f;
        tapContinueHintTmp.alignment = TextAlignmentOptions.Center;
        tapContinueHintTmp.text = StoryTextStyle.Mu("點擊空白處繼續");
        if (dialogueTextTmp != null && dialogueTextTmp.font != null)
        {
            tapContinueHintTmp.font = dialogueTextTmp.font;
            tapContinueHintTmp.fontSharedMaterial = dialogueTextTmp.fontSharedMaterial;
        }

        hintGo.SetActive(false);
    }

    private void OnTapToContinue()
    {
        if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        if (dialogueTypewriter != null && dialogueTypewriter.IsActive)
        {
            dialogueTypewriter.Complete();
            RefreshTapContinueHint();
            OnDialogueTypewriterFinished();
            return;
        }

        PlotStep step = steps[currentStepIndex];
        if (ResolveAdvanceKind(step) != PlotAdvanceKind.TapToContinue) return;

        if (step.tapEndsPlot)
        {
            FinishPlotAndReturn();
            return;
        }

        int next = step.tapNextStepIndex;
        if (next >= 0 && next < steps.Count)
        {
            if (TryGrantStarterDeckAndNotifyBeforeStep(next))
                return;
            ShowStep(next);
        }
    }

    private bool IsOpeningTutorialPlotSession()
    {
        if (StoryProgressSession.IsTutorialPlotEpilogueActive)
            return false;
        return launchedFromStoryProgress || StoryProgressSession.TutorialPlotBgmRequested;
    }

    private bool IsLeavingStarterDeckGrantStep()
    {
        if (!IsOpeningTutorialPlotSession())
            return false;
        if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return false;

        PlotStep step = steps[currentStepIndex];
        if (TutorialPlotScriptFactory.IsStarterDeckGrantPlotStep(step))
            return true;

        return currentStepIndex == TutorialPlotScriptFactory.IntroStarterDeckGrantStepIndex;
    }

    private bool ShouldShowStarterDeckNotifyWhenLeavingCurrentStep() =>
        IsLeavingStarterDeckGrantStep() &&
        !TutorialProgressState.IsStarterDeckNotifyShownForActivePlayer();

    /// <summary>離開「基礎牌組」台詞後發牌組並顯示獲得通知，再進入下一步。</summary>
    private void TryLaunchTutorialBattleAfterPlot(bool skippedPlot)
    {
        if (skippedPlot && IsOpeningTutorialPlotSession())
        {
            try
            {
                TutorialDeckApplicator.EnsureIntroTutorialDeckReady();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            if (TutorialProgressState.IsStarterDeckNotifyShownForActivePlayer())
            {
                StoryProgressSession.LaunchTutorialBattleAfterPlot(fastCloseAnimation: true);
                return;
            }

            Canvas canvas = FindPlotCanvas();
            if (canvas != null)
            {
                int slot = PlayerData.GetActivePlayerSlotOrDefault();
                TutorialProgressState.SetStarterDeckNotifyShown(slot);
                TutorialPlotStarterDeckNotify.ShowSkipReadyBrief(canvas, dialogueTextTmp, () =>
                    StoryProgressSession.LaunchTutorialBattleAfterPlot(fastCloseAnimation: true));
                return;
            }
        }

        StoryProgressSession.LaunchTutorialBattleAfterPlot(fastCloseAnimation: skippedPlot);
    }

    private bool TryGrantStarterDeckAndNotifyBeforeStep(int nextStepIndex)
    {
        if (!IsLeavingStarterDeckGrantStep())
            return false;

        try
        {
            TutorialDeckApplicator.EnsureIntroTutorialDeckReady();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        if (!ShouldShowStarterDeckNotifyWhenLeavingCurrentStep())
            return false;

        ApplyTapToContinueUi(false);
        HideAllChoiceButtons();

        Canvas canvas = FindPlotCanvas();
        if (canvas == null)
        {
            GameDevLog.LogWarning("MainPlotSceneController: plot canvas not found; skipping starter deck notify.");
            ShowStep(nextStepIndex);
            return true;
        }

        int activeSlot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.SetStarterDeckNotifyShown(activeSlot);

        try
        {
            TutorialPlotStarterDeckNotify.Show(canvas, dialogueTextTmp, () => ShowStep(nextStepIndex));
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            TutorialPlotStarterDeckNotify.DismissExisting();
            ShowStep(nextStepIndex);
        }

        return true;
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count) return;

        PlotStep step = steps[currentStepIndex];
        int nextIndex = -1;
        switch (choiceIndex)
        {
            case 0: nextIndex = step.choice1Next; break;
            case 1: nextIndex = step.choice2Next; break;
            case 2: nextIndex = step.choice3Next; break;
        }

        if (nextIndex < 0 || nextIndex >= steps.Count)
        {
            if (launchedFromStoryProgress && IsTerminalPlotChoice(step, choiceIndex))
                FinishPlotAndReturn();
            else
                HideAllChoiceButtons();
            return;
        }

        ShowStep(nextIndex);
    }

    private static bool IsTerminalPlotChoice(PlotStep step, int choiceIndex)
    {
        if (choiceIndex == 0)
            return step.choice1Next < 0 && !string.IsNullOrWhiteSpace(step.choice1Text);
        if (choiceIndex == 1)
            return step.choice2Next < 0 && !string.IsNullOrWhiteSpace(step.choice2Text);
        return step.choice3Next < 0 && !string.IsNullOrWhiteSpace(step.choice3Text);
    }

    private void FinishPlotAndReturn(bool skippedPlot = false)
    {
        StopTutorialPlotBgm();
        PlotUiOverlayCleanup.DestroyStrayPlotTapUi();

        if (StoryProgressSession.IsTutorialPlotEpilogueActive)
        {
            StoryProgressSession.LoadStoryProgressWithIrisTransition();
            return;
        }

        if (launchedFromStoryProgress)
        {
            StoryProgressSession.NotifyTutorialPlotFinished();
            if (StoryProgressSession.TryConsumeLaunchTutorialBattleAfterPlot())
            {
                TryLaunchTutorialBattleAfterPlot(skippedPlot);
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(StoryProgressSession.StoryProgressSceneName))
            {
                SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
                return;
            }
        }

        HideAllChoiceButtons();
    }

    public void ShowStep(int stepIndex)
    {
        if (steps == null || stepIndex < 0 || stepIndex >= steps.Count) return;

        currentStepIndex = stepIndex;
        PlotStep step = steps[stepIndex];

        if (speakerNameTmp != null)
        {
            speakerNameTmp.richText = true;
            speakerNameTmp.text = step.speakerName ?? string.Empty;
        }
        EnsureDialoguePanelVisible();
        ApplyOptionalStepSprite(plotBackgroundImage, step.backgroundSprite);
        ApplyImageSprite(characterAImage, step.characterASprite);
        ApplyImageSprite(characterBImage, step.characterBSprite);
        ApplyImageSprite(characterCImage, step.characterCSprite);

        HideAllChoiceButtons();

        currentStepAdvanceKind = ResolveAdvanceKind(step);
        ApplyTapToContinueUi(true);
        SuppressPlaceholderChoiceButtons();
        BringSkipButtonToFront();
        BeginDialogueTypewriter(step.dialogueText);
    }

    private void EnsurePlotTypewriterSfx()
    {
        if (plotTypewriterSfx != null)
            return;

        plotTypewriterSfx = PlotDialogueTypewriterSfx.FindInMainPlotScene()
                            ?? PlotDialogueTypewriterSfx.EnsureOnMainCamera();
    }

    private void EnsurePlotMenuClickSfx()
    {
        if (plotMenuClickSfx != null)
            return;

        plotMenuClickSfx = PlotMenuClickSfx.FindInMainPlotScene()
                           ?? PlotMenuClickSfx.EnsureOnMainCamera();
    }

    private void PlayPlotMenuClickSound()
    {
        if (!launchedFromStoryProgress)
            return;

        EnsurePlotMenuClickSfx();
        plotMenuClickSfx?.PlayMenuClick();
    }

    private void BeginDialogueTypewriter(string dialogueText)
    {
        if (dialogueTypewriter == null)
            dialogueTypewriter = new PlotDialogueTypewriter();

        EnsurePlotTypewriterSfx();
        System.Action onTypingStarted = null;
        System.Action onTypingEnded = null;
        if (launchedFromStoryProgress)
        {
            onTypingStarted = BeginPlotTypingSound;
            onTypingEnded = StopPlotTypingSound;
        }

        dialogueTypewriter.Begin(
            dialogueTextTmp,
            dialogueText,
            dialogueCharactersPerSecond,
            onTypingStarted,
            onTypingEnded);
        RefreshTapContinueHint();

        if (dialogueTypewriter.IsComplete)
            OnDialogueTypewriterFinished();
    }

    private void BeginPlotTypingSound() => plotTypewriterSfx?.BeginTypingSound();

    private void StopPlotTypingSound() => plotTypewriterSfx?.StopTypingSound();

    private void OnDialogueTypewriterFinished()
    {
        if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        PlotStep step = steps[currentStepIndex];
        if (currentStepAdvanceKind == PlotAdvanceKind.PlayerChoice)
        {
            ApplyTapToContinueUi(false);
            EnsureChoiceButtonsReady();
            WireChoiceButtons();
            SetupChoiceButton(choice1Button, ref choice1TextTmp, step.choice1Text, step.choice1Next, 0);
            SetupChoiceButton(choice2Button, ref choice2TextTmp, step.choice2Text, step.choice2Next, 1);
            SetupChoiceButton(choice3Button, ref choice3TextTmp, step.choice3Text, step.choice3Next, 2);
            BringVisibleChoiceButtonsToFront();
            BringSkipButtonToFront();
            return;
        }

        RefreshTapContinueHint();
        BringSkipButtonToFront();
    }

    private void RefreshTapContinueHint()
    {
        if (tapContinueHintTmp == null)
            return;

        bool revealing = dialogueTypewriter != null && dialogueTypewriter.IsActive;
        tapContinueHintTmp.text = revealing
            ? StoryTextStyle.Mu("點擊顯示全文")
            : StoryTextStyle.Mu("點擊空白處繼續");
    }

    private static PlotAdvanceKind ResolveAdvanceKind(PlotStep step)
    {
        if (step.advanceKind == PlotAdvanceKind.PlayerChoice && HasAnyActiveChoice(step))
            return PlotAdvanceKind.PlayerChoice;
        if (step.advanceKind == PlotAdvanceKind.TapToContinue
            || step.tapNextStepIndex >= 0
            || step.tapEndsPlot)
            return PlotAdvanceKind.TapToContinue;
        if (HasAnyActiveChoice(step))
            return PlotAdvanceKind.PlayerChoice;
        return PlotAdvanceKind.TapToContinue;
    }

    private static bool HasAnyActiveChoice(PlotStep step)
    {
        return IsChoiceSlotActive(step.choice1Text, step.choice1Next)
               || IsChoiceSlotActive(step.choice2Text, step.choice2Next)
               || IsChoiceSlotActive(step.choice3Text, step.choice3Next);
    }

    private static bool IsChoiceSlotActive(string label, int nextIndex)
    {
        if (string.IsNullOrWhiteSpace(label)) return false;
        if (nextIndex >= 0) return true;
        if (nextIndex < 0 && IsDefaultPlaceholderChoice(label)) return false;
        return true;
    }

    private static bool IsDefaultPlaceholderChoice(string label)
    {
        return label == "選擇一" || label == "選擇二" || label == "選擇三"
               || label == "選項一" || label == "選項二" || label == "選項三";
    }

    private void ApplyTapToContinueUi(bool active)
    {
        EnsureTapToContinueUi();
        if (tapToContinueButton != null)
        {
            Image overlayImg = tapToContinueButton.targetGraphic as Image;
            tapToContinueButton.gameObject.SetActive(active);
            if (overlayImg != null)
                overlayImg.raycastTarget = active;
            if (active)
                tapToContinueButton.transform.SetAsLastSibling();
        }

        if (tapContinueHintTmp != null)
        {
            tapContinueHintTmp.gameObject.SetActive(active);
            if (active && tapToContinueButton != null)
                tapContinueHintTmp.transform.SetSiblingIndex(tapToContinueButton.transform.GetSiblingIndex() + 1);
        }
    }

    private void EnsureDialoguePanelVisible()
    {
        if (dialoguePanelImage == null)
            dialoguePanelImage = FindImageInPlotScene("文字背板");
        if (dialoguePanelImage == null) return;

        dialoguePanelImage.gameObject.SetActive(true);
        dialoguePanelImage.enabled = true;

        if (dialogueTextTmp != null)
        {
            int textIndex = dialogueTextTmp.transform.GetSiblingIndex();
            dialoguePanelImage.transform.SetSiblingIndex(Mathf.Max(0, textIndex - 1));
        }
    }

    private static void ApplyOptionalStepSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        if (sprite == null) return;

        image.sprite = sprite;
        image.enabled = true;
        image.gameObject.SetActive(true);
    }

    private static void ApplyImageSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        if (sprite == null)
        {
            image.enabled = false;
            return;
        }

        image.sprite = sprite;
        image.enabled = true;
        image.gameObject.SetActive(true);
        image.preserveAspect = true;
    }

    private void SetupChoiceButton(
        Button button,
        ref TMP_Text textTmp,
        string label,
        int nextIndex,
        int layoutIndex)
    {
        if (button == null) return;

        bool visible = IsChoiceSlotActive(label, nextIndex);

        if (!visible)
        {
            button.gameObject.SetActive(false);
            button.interactable = false;
            SetButtonRaycastTargets(button, false);
            return;
        }

        button.gameObject.SetActive(true);
        LayoutOneChoiceButton(button, layoutIndex);
        BattleUiColors.ApplyHallWineButton(button);
        button.interactable = true;
        SetButtonRaycastTargets(button, true);

        textTmp = PlotUiTextUtil.EnsureButtonLabel(button, textTmp, dialogueTextTmp);
        PlotUiTextUtil.ApplyButtonLabel(textTmp, label, dialogueTextTmp);
        button.transform.SetAsLastSibling();
    }

    private void HideAllChoiceButtons()
    {
        Button[] buttons = { choice1Button, choice2Button, choice3Button };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;
            button.gameObject.SetActive(false);
            button.interactable = false;
            SetButtonRaycastTargets(button, false);
            button.transform.SetAsFirstSibling();
        }
    }

    private static Scene ResolvePlotScene()
    {
        Scene plotScene = SceneManager.GetSceneByName(StoryProgressSession.MainPlotSceneName);
        if (plotScene.IsValid()) return plotScene;

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.name == StoryProgressSession.MainPlotSceneName)
            return active;

        return default;
    }

    private static GameObject FindInPlotScene(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        Scene plotScene = ResolvePlotScene();
        if (plotScene.IsValid())
        {
            GameObject[] roots = plotScene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform t = transforms[i];
                    if (t.name == objectName)
                        return t.gameObject;
                }
            }
        }

        GameObject found = GameObject.Find(objectName);
        if (found != null && found.scene.name == StoryProgressSession.MainPlotSceneName)
            return found;

        return null;
    }

    private static TMP_Text FindTmpInPlotScene(string objectName)
    {
        GameObject go = FindInPlotScene(objectName);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImageInPlotScene(string objectName)
    {
        GameObject go = FindInPlotScene(objectName);
        return go != null ? go.GetComponent<Image>() : null;
    }

    private static Button FindButtonInPlotScene(string objectName)
    {
        GameObject go = FindInPlotScene(objectName);
        if (go == null) return null;
        return go.GetComponent<Button>() ?? EnsureButton(go);
    }

    /// <summary>螢幕擷取失敗時，用目前劇情背景／立繪組成過場快照。</summary>
    public static Texture2D TryBuildTransitionSnapshotTexture()
    {
        MainPlotSceneController ctrl = Object.FindFirstObjectByType<MainPlotSceneController>();
        return ctrl != null ? ctrl.BuildTransitionSnapshotTexture() : null;
    }

    private Texture2D BuildTransitionSnapshotTexture()
    {
        Sprite bg = plotBackgroundImage != null ? plotBackgroundImage.sprite : null;
        Sprite a = characterAImage != null ? characterAImage.sprite : null;
        Sprite b = characterBImage != null ? characterBImage.sprite : null;
        Sprite c = characterCImage != null ? characterCImage.sprite : null;
        Texture2D shot = TutorialPlotIrisMaskUtil.BuildSnapshotFromSprites(bg, a, b, c);
        if (shot != null && !TutorialPlotIrisMaskUtil.IsTextureMostlyBlack(shot))
            return shot;

        if (shot != null)
            Destroy(shot);
        return bg != null ? TutorialPlotIrisMaskUtil.BuildSnapshotFromSprites(bg) : null;
    }
}

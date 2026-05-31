using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Batch win-rate simulation for battles. Trigger via <see cref="BattleAutoSimPlugin.Run"/> or the debug UI button.
/// Fires <see cref="Completed"/> when finished (summary is also printed to the Console).
/// </summary>
public static class BattleAutoSimPlugin
{
    public readonly struct SimResult
    {
        public readonly int Wins;
        public readonly int Losses;
        public readonly int Draws;
        public readonly int RoundsFinished;
        public readonly bool AbortedByStepLimit;

        public SimResult(int wins, int losses, int draws, int roundsFinished, bool abortedByStepLimit)
        {
            Wins = wins;
            Losses = losses;
            Draws = draws;
            RoundsFinished = roundsFinished;
            AbortedByStepLimit = abortedByStepLimit;
        }

        public float WinRate => RoundsFinished > 0 ? (float)Wins / RoundsFinished : 0f;
    }

    /// <summary>Optional: fired when a batch run starts.</summary>
    public static event Action Started;

    /// <summary>Fired when the batch finishes or aborts.</summary>
    public static event Action<SimResult> Completed;

    public static int DefaultRounds { get; set; } = 10;
    public static float DefaultTimeScaleWhileRunning { get; set; } = 80f;
    /// <summary>批次每局開場 Realtime 秒數；0 為最快（仍受 BattleSimulationManager 在 IsRunning 時略過 Realtime 的影響）。</summary>
    public static float DefaultBatchOpeningSeconds { get; set; } = 0f;
    public static int MaxStepsPerBattle { get; set; } = 50000;

    /// <summary>批次內層 while 每幀最多推進幾次（僅在無開場／無回合序列／無法術演出等待時連續執行），可大幅降低 yield 次數。</summary>
    public static int BatchSimMaxPumpsPerFrame { get; set; } = 64;

    /// <summary>True while a batch win-rate run is in progress. Battle UI should skip card/field animations and rely on progress UI only.</summary>
    public static bool IsRunning { get; private set; }

    /// <summary>港灣勝率批次等無 UI 宿主時，強制標記批次模擬中（開場不等待 Realtime）。</summary>
    public static void ForceBatchRunning(bool on) => IsRunning = on;

    /// <summary>
    /// When assigned (e.g. a slot inside the battle debug floating window), batch progress UI is built under this
    /// transform instead of a separate full-screen overlay canvas.
    /// </summary>
    public static Transform ProgressUiParent { get; set; }

    /// <summary>Start using <see cref="DefaultRounds"/>.</summary>
    public static void Run()
    {
        Run(DefaultRounds, DefaultTimeScaleWhileRunning, DefaultBatchOpeningSeconds);
    }

    /// <summary>Start with a fixed number of games; other options use static defaults.</summary>
    public static void Run(int rounds)
    {
        Run(rounds, DefaultTimeScaleWhileRunning, DefaultBatchOpeningSeconds);
    }

    /// <summary>Start with explicit game count and speed parameters.</summary>
    public static void Run(int rounds, float timeScaleWhileRunning, float batchOpeningPresentationSeconds)
    {
        if (IsRunning)
        {
            Debug.LogWarning("BattleAutoSimPlugin: a simulation is already running; request ignored.");
            return;
        }

        BattleSimulationManager mgr = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (mgr == null)
        {
            Debug.LogError("BattleAutoSimPlugin: BattleSimulationManager not found in the scene.");
            return;
        }

        rounds = Mathf.Max(1, rounds);
        Transform slot = ProgressUiParent;
        if (slot != null)
        {
            Transform x = slot;
            while (x != null)
            {
                if (!x.gameObject.activeSelf) x.gameObject.SetActive(true);
                x = x.parent;
            }
        }

        EnsureHost();
        IsRunning = true;
        Started?.Invoke();
        Host.StartCoroutine(Host.RunBatchCoroutine(mgr, rounds, timeScaleWhileRunning, batchOpeningPresentationSeconds));
    }

    private static BattleAutoSimPluginHost Host { get; set; }

    private static void EnsureHost()
    {
        if (Host != null) return;
        GameObject go = new GameObject("BattleAutoSimPluginHost");
        Host = go.AddComponent<BattleAutoSimPluginHost>();
    }

    internal static void FinishRun(SimResult result)
    {
        IsRunning = false;
        Completed?.Invoke(result);
    }

    /// <summary>
    /// When batch progress is embedded in the debug UI slot, builds the panel once and shows the idle shell with Win-rate sim.
    /// </summary>
    public static void PrepareEmbeddedProgressShellIfEmbedded()
    {
        if (ProgressUiParent == null) return;
        EnsureHost();
        Host.EnsureProgressUi();
        Host.ShowEmbeddedIdleShellAfterCreate();
    }

    /// <summary>Sync Win-rate sim button with debug panel visibility / pause (embedded shell only).</summary>
    public static void RefreshWinRateButtonForDebugUi(bool debugPanelVisible, bool gamePaused)
    {
        if (Host == null) return;
        Host.ApplyWinRateInteractableForDebug(debugPanelVisible && !gamePaused && !IsRunning);
    }

    private sealed class BattleAutoSimPluginHost : MonoBehaviour
    {
        private GameObject progressOverlayHost;
        private GameObject progressRoot;
        private CanvasGroup progressCanvasGroup;
        private TextMeshProUGUI progressTitleTmp;
        private TextMeshProUGUI progressStatusTmp;
        private TextMeshProUGUI progressSummaryTmp;
        private Image progressFillImage;
        private Button progressCloseButton;
        private Button progressWinRateButton;
        private TMP_FontAsset cachedUiFont;
        private static Sprite _uiWhiteSprite;

        private static Sprite GetUiWhiteSprite()
        {
            if (_uiWhiteSprite != null) return _uiWhiteSprite;

            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            _uiWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return _uiWhiteSprite;
        }

        private static void AssignUiSprite(Image img)
        {
            if (img == null) return;
            img.sprite = GetUiWhiteSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }

        internal IEnumerator RunBatchCoroutine(
            BattleSimulationManager battle,
            int rounds,
            float timeScaleWhileRunning,
            float batchOpeningPresentationSeconds)
        {
            float savedTimeScale = Time.timeScale;
            float savedOpeningSeconds = battle.GetOpeningPresentationSeconds();
            int wins = 0;
            int losses = 0;
            int draws = 0;
            bool aborted = false;
            SimResult outcome = new SimResult(0, 0, 0, 0, true);

            try
            {
                battle.SetOpeningPresentationSeconds(Mathf.Clamp(batchOpeningPresentationSeconds, 0f, 15f));
                Time.timeScale = Mathf.Max(1f, timeScaleWhileRunning);

                try
                {
                    EnsureProgressUi();
                }
                catch (Exception e)
                {
                    Debug.LogError("BattleAutoSimPlugin: failed to build progress overlay — " + e.Message);
                }
                if (progressOverlayHost == null)
                {
                    Debug.LogError("BattleAutoSimPlugin: progress overlay missing; progress logs appear in the Console only.");
                }

                SetProgressPanelVisible(true);
                SetProgressBarFill(0f);
                SetProgressTexts("Win-rate batch", "Total " + RichInt(rounds) + " games", "Starting…", string.Empty);
                if (progressCloseButton != null) progressCloseButton.gameObject.SetActive(false);
                if (progressWinRateButton != null) progressWinRateButton.gameObject.SetActive(false);

                for (int b = 0; b < rounds; b++)
                {
                    SetProgressTexts(
                        "Win-rate batch",
                        "Game " + RichInt(b + 1) + " / " + RichInt(rounds),
                        b == 0 ? "Battle in progress…" : "Next game started…",
                        BuildRunningSummaryLine(wins, losses, draws));
                    SetProgressBarFill(b / (float)rounds);

                    if (b > 0)
                    {
                        battle.StartBattle();
                    }

                    yield return new WaitUntil(() => !battle.IsOpeningPresentationInProgress());

                    int steps = 0;
                    while (!battle.IsBattleOver() && steps < MaxStepsPerBattle)
                    {
                        steps++;
                        if (battle.IsOpeningPresentationInProgress())
                        {
                            yield return null;
                            continue;
                        }

                        if (battle.IsTurnSequenceInProgress() || battle.IsSpellCastPresentationActive())
                        {
                            yield return null;
                            continue;
                        }

                        int pumps = Mathf.Max(1, BatchSimMaxPumpsPerFrame);
                        for (int p = 0; p < pumps && !battle.IsBattleOver(); p++)
                        {
                            if (battle.IsOpeningPresentationInProgress() ||
                                battle.IsTurnSequenceInProgress() ||
                                battle.IsSpellCastPresentationActive())
                            {
                                break;
                            }

                            if (!battle.IsPlayerTurn())
                                break;

                            TryAutoPlayOneCard(battle);
                            if (battle.IsPlayerTurn() && !battle.IsBattleOver() &&
                                !battle.IsTurnSequenceInProgress() && !battle.IsSpellCastPresentationActive())
                            {
                                battle.EndPlayerTurn();
                            }

                            if (battle.IsTurnSequenceInProgress() || battle.IsSpellCastPresentationActive())
                                break;
                        }

                        yield return null;
                    }

                    if (steps >= MaxStepsPerBattle)
                    {
                        Debug.LogWarning("BattleAutoSimPlugin: game " + (b + 1) + " exceeded step limit; batch aborted.");
                        aborted = true;
                        SetProgressTexts(
                            "Win-rate batch",
                            "Aborted",
                            "Game " + RichInt(b + 1) + " exceeded the step limit.",
                            BuildRunningSummaryLine(wins, losses, draws));
                        break;
                    }

                    int r = battle.GetBattleResult();
                    if (r == 1) wins++;
                    else if (r == -1) losses++;
                    else if (r == 2) draws++;

                    int completed = wins + losses + draws;
                    SetProgressBarFill(completed / (float)rounds);
                    SetProgressTexts(
                        "Win-rate batch",
                        "Finished " + RichInt(completed) + " / " + RichInt(rounds) + " games",
                        "Game " + RichInt(b + 1) + ": " + BattleResultLabel(r),
                        BuildRunningSummaryLine(wins, losses, draws));
                }

                int finished = wins + losses + draws;
                float winRate = finished > 0 ? (float)wins / finished : 0f;
                string rateLine = finished > 0
                    ? "Win rate " + RichPercent(winRate) + " (" + RichInt(wins) + " W / " + RichInt(losses) + " L / " + RichInt(draws) + " D)"
                    : "No completed games; win rate undefined.";

                Debug.Log("BattleAutoSimPlugin: done " + finished + " games | W=" + wins + " L=" + losses + " D=" + draws + " | win rate=" + (winRate * 100f).ToString("F1") + "%");

                SetProgressBarFill(1f);
                SetProgressTexts(
                    "Batch summary",
                    "Completed " + RichInt(finished) + " games",
                    rateLine,
                    "Totals: W " + RichInt(wins) + "  L " + RichInt(losses) + "  D " + RichInt(draws));
                if (progressCloseButton != null) progressCloseButton.gameObject.SetActive(true);
                if (progressWinRateButton != null) progressWinRateButton.gameObject.SetActive(true);
                UpdateProgressRaycastBlocking();
                if (progressOverlayHost != null) progressOverlayHost.transform.SetAsLastSibling();

                outcome = new SimResult(wins, losses, draws, finished, aborted);
            }
            finally
            {
                if (battle != null)
                {
                    try
                    {
                        battle.SetOpeningPresentationSeconds(savedOpeningSeconds);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                Time.timeScale = savedTimeScale;
                FinishRun(outcome);
            }
        }

        private static string BattleResultLabel(int code)
        {
            if (code == 1) return "Win";
            if (code == -1) return "Loss";
            if (code == 2) return "Draw";
            return "Unknown";
        }

        private static string BuildRunningSummaryLine(int wins, int losses, int draws)
        {
            return "Running totals: W " + RichInt(wins) + "  L " + RichInt(losses) + "  D " + RichInt(draws);
        }

        /// <summary>Highlight numeric tokens in batch summary (TMP rich text).</summary>
        private static string RichInt(int v)
        {
            return "<color=#FFE566><b>" + v + "</b></color>";
        }

        private static string RichPercent(float value0to1)
        {
            return "<color=#FFE566><b>" + (value0to1 * 100f).ToString("F1") + "%</b></color>";
        }

        internal void EnsureProgressUi()
        {
            if (progressOverlayHost != null) return;

            EnsureEventSystemExists();
            cachedUiFont = ResolveUiFont();

            Transform embedParent = ProgressUiParent;
            bool embedded = embedParent != null && embedParent.gameObject != null;

            if (embedded)
            {
                progressOverlayHost = new GameObject("BattleAutoSimProgressHost", typeof(RectTransform));
                progressOverlayHost.layer = embedParent.gameObject.layer;
                progressOverlayHost.transform.SetParent(embedParent, false);
                RectTransform hostRt = progressOverlayHost.GetComponent<RectTransform>();
                hostRt.anchorMin = Vector2.zero;
                hostRt.anchorMax = Vector2.one;
                hostRt.offsetMin = Vector2.zero;
                hostRt.offsetMax = Vector2.zero;
            }
            else
            {
                progressOverlayHost = new GameObject("BattleAutoSimOverlay");
                progressOverlayHost.layer = 5;
                Canvas overlayCanvas = progressOverlayHost.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.overrideSorting = true;
                overlayCanvas.sortingOrder = 60000;
                CanvasScaler scaler = progressOverlayHost.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                // Batch summary overlay authored for 3000×1600 design space (see rootRt.sizeDelta).
                scaler.referenceResolution = new Vector2(3000f, 1600f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                progressOverlayHost.AddComponent<GraphicRaycaster>();

                RectTransform canvasRt = progressOverlayHost.GetComponent<RectTransform>();
                canvasRt.anchorMin = Vector2.zero;
                canvasRt.anchorMax = Vector2.one;
                canvasRt.pivot = new Vector2(0.5f, 0.5f);
                canvasRt.anchoredPosition = Vector2.zero;
                canvasRt.sizeDelta = Vector2.zero;
            }

            progressRoot = new GameObject("AutoSimProgressPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            progressRoot.layer = progressOverlayHost.layer;
            progressRoot.transform.SetParent(progressOverlayHost.transform, false);

            RectTransform rootRt = progressRoot.GetComponent<RectTransform>();
            if (embedded)
            {
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.offsetMin = new Vector2(2f, 2f);
                rootRt.offsetMax = new Vector2(-2f, -2f);
            }
            else
            {
                rootRt.anchorMin = new Vector2(1f, 1f);
                rootRt.anchorMax = new Vector2(1f, 1f);
                rootRt.pivot = new Vector2(1f, 1f);
                rootRt.anchoredPosition = new Vector2(-20f, -36f);
                float pw = Mathf.Min(3000f, Mathf.Max(1040f, Screen.width - 36f));
                float ph = Mathf.Min(1600f, Mathf.Max(720f, Screen.height - 36f));
                rootRt.sizeDelta = new Vector2(pw, ph);
            }

            Image rootBg = progressRoot.GetComponent<Image>();
            AssignUiSprite(rootBg);
            rootBg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
            rootBg.raycastTarget = true;

            progressCanvasGroup = progressRoot.GetComponent<CanvasGroup>();
            progressCanvasGroup.blocksRaycasts = false;
            progressCanvasGroup.interactable = true;

            float panelH = embedded ? 520f : rootRt.sizeDelta.y;
            float btnRowH = embedded ? 80f : 112f;
            float trackH = embedded ? 28f : 44f;
            float trackY = btnRowH + 20f;
            float titleFs = embedded ? 44f : 68f;
            float statusFs = embedded ? 36f : 52f;
            float summaryFs = embedded ? 32f : 44f;
            Vector2 titleSize = embedded ? new Vector2(-28f, 64f) : new Vector2(-40f, 116f);
            Vector2 statusSize = embedded ? new Vector2(-28f, 56f) : new Vector2(-40f, 100f);
            float summaryTop = embedded ? 172f : 272f;
            float summaryH = Mathf.Clamp(panelH - summaryTop - trackY - trackH - 36f, 144f, embedded ? 236f : 1240f);
            Vector2 summarySize = new Vector2(embedded ? -28f : -40f, summaryH);

            progressTitleTmp = CreateTmp("Title", progressRoot.transform, new Vector2(embedded ? 14f : 20f, -28f), titleSize, titleFs, FontStyles.Bold, TextAlignmentOptions.Left, cachedUiFont);
            progressStatusTmp = CreateTmp("Status", progressRoot.transform, new Vector2(embedded ? 14f : 20f, embedded ? -100f : -156f), statusSize, statusFs, FontStyles.Bold, TextAlignmentOptions.Left, cachedUiFont);
            progressSummaryTmp = CreateTmp("Summary", progressRoot.transform, new Vector2(embedded ? 14f : 20f, -summaryTop), summarySize, summaryFs, FontStyles.Normal, TextAlignmentOptions.TopLeft, cachedUiFont);

            GameObject track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(progressRoot.transform, false);
            RectTransform trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(1f, 0f);
            trackRt.pivot = new Vector2(0.5f, 0f);
            trackRt.anchoredPosition = new Vector2(0f, trackY);
            trackRt.sizeDelta = new Vector2(embedded ? -28f : -40f, trackH);
            Image trackImg = track.GetComponent<Image>();
            AssignUiSprite(trackImg);
            trackImg.color = new Color(0.2f, 0.22f, 0.26f, 1f);
            trackImg.raycastTarget = false;

            GameObject fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            progressFillImage = fill.GetComponent<Image>();
            AssignUiSprite(progressFillImage);
            progressFillImage.type = Image.Type.Filled;
            progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressFillImage.fillOrigin = 0;
            progressFillImage.fillAmount = 0f;
            progressFillImage.color = new Color(0.28f, 0.72f, 0.48f, 1f);
            progressFillImage.raycastTarget = false;

            GameObject wrObj = new GameObject("WinRateSimButton", typeof(RectTransform), typeof(Image), typeof(Button));
            wrObj.transform.SetParent(progressRoot.transform, false);
            RectTransform wrRt = wrObj.GetComponent<RectTransform>();
            if (embedded)
            {
                // Stretch with debug slot width; leave room for Close (overlay keeps fixed layout — unchanged).
                wrRt.anchorMin = new Vector2(0f, 0f);
                wrRt.anchorMax = new Vector2(1f, 0f);
                wrRt.pivot = new Vector2(0.5f, 0f);
                wrRt.anchoredPosition = Vector2.zero;
                wrRt.offsetMin = new Vector2(12f, 20f);
                wrRt.offsetMax = new Vector2(-228f, 20f + 76f);
            }
            else
            {
                wrRt.anchorMin = new Vector2(0f, 0f);
                wrRt.anchorMax = new Vector2(0f, 0f);
                wrRt.pivot = new Vector2(0f, 0f);
                wrRt.anchoredPosition = new Vector2(20f, 20f);
                wrRt.sizeDelta = new Vector2(1120f, btnRowH);
            }
            Image wrBg = wrObj.GetComponent<Image>();
            AssignUiSprite(wrBg);
            wrBg.color = new Color(0.22f, 0.55f, 0.38f, 1f);
            progressWinRateButton = wrObj.GetComponent<Button>();
            progressWinRateButton.onClick.AddListener(() => BattleAutoSimPlugin.Run());

            GameObject wrLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            wrLabel.transform.SetParent(wrObj.transform, false);
            RectTransform wrLblRt = wrLabel.GetComponent<RectTransform>();
            wrLblRt.anchorMin = Vector2.zero;
            wrLblRt.anchorMax = Vector2.one;
            wrLblRt.offsetMin = new Vector2(8f, 2f);
            wrLblRt.offsetMax = new Vector2(-8f, -2f);
            TextMeshProUGUI wrTmp = wrLabel.GetComponent<TextMeshProUGUI>();
            wrTmp.font = cachedUiFont != null ? cachedUiFont : TMP_Settings.defaultFontAsset;
            wrTmp.fontSize = embedded ? 34f : 44f;
            wrTmp.fontStyle = FontStyles.Bold;
            wrTmp.alignment = TextAlignmentOptions.Center;
            wrTmp.color = Color.white;
            wrTmp.text = "Win-rate sim (" + DefaultRounds + " games)";
            wrTmp.raycastTarget = false;

            GameObject btnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(progressRoot.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(1f, 0f);
            btnRt.anchoredPosition = new Vector2(embedded ? -12f : -20f, 20f);
            btnRt.sizeDelta = embedded ? new Vector2(200f, 68f) : new Vector2(260f, btnRowH);
            Image btnBg = btnObj.GetComponent<Image>();
            AssignUiSprite(btnBg);
            btnBg.color = new Color(0.32f, 0.36f, 0.42f, 1f);
            progressCloseButton = btnObj.GetComponent<Button>();
            progressCloseButton.onClick.AddListener(() => SetProgressPanelVisible(false));

            GameObject btnLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLabel.transform.SetParent(btnObj.transform, false);
            RectTransform lblRt = btnLabel.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;
            TextMeshProUGUI lbl = btnLabel.GetComponent<TextMeshProUGUI>();
            lbl.font = cachedUiFont != null ? cachedUiFont : TMP_Settings.defaultFontAsset;
            lbl.fontSize = embedded ? 30f : 38f;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white;
            lbl.text = "Close";

            if (embedded)
            {
                progressOverlayHost.SetActive(true);
                ApplyEmbeddedIdlePresentation();
                Debug.Log("BattleAutoSimPlugin: created embedded progress panel on debug UI.");
            }
            else
            {
                progressOverlayHost.SetActive(false);
                Debug.Log("BattleAutoSimPlugin: created overlay progress window.");
            }
        }

        internal void ShowEmbeddedIdleShellAfterCreate()
        {
            if (ProgressUiParent == null) return;
            ApplyEmbeddedIdlePresentation();
        }

        private void ApplyEmbeddedIdlePresentation()
        {
            if (progressOverlayHost == null || ProgressUiParent == null) return;
            progressOverlayHost.SetActive(true);
            SetProgressTexts(
                "Batch summary",
                "Ready",
                "Tap Win-rate sim to run " + RichInt(DefaultRounds) + " games.",
                string.Empty);
            SetProgressBarFill(0f);
            if (progressCloseButton != null) progressCloseButton.gameObject.SetActive(false);
            if (progressWinRateButton != null) progressWinRateButton.gameObject.SetActive(true);
            UpdateProgressRaycastBlocking();
        }

        internal void ApplyWinRateInteractableForDebug(bool allow)
        {
            if (progressWinRateButton == null) return;
            progressWinRateButton.interactable = allow;
            UpdateProgressRaycastBlocking();
        }

        private void UpdateProgressRaycastBlocking()
        {
            if (progressCanvasGroup == null || progressOverlayHost == null || !progressOverlayHost.activeSelf)
                return;
            bool closeOn = progressCloseButton != null && progressCloseButton.gameObject.activeInHierarchy;
            bool winShown = progressWinRateButton != null && progressWinRateButton.gameObject.activeInHierarchy;
            progressCanvasGroup.blocksRaycasts = closeOn || winShown;
        }

        private static void EnsureEventSystemExists()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Debug.LogWarning("BattleAutoSimPlugin: no EventSystem in scene; created one automatically.");
        }

        private void SetProgressPanelVisible(bool visible)
        {
            if (progressOverlayHost == null) return;
            progressOverlayHost.SetActive(visible);
            if (progressCanvasGroup != null)
            {
                if (visible)
                    UpdateProgressRaycastBlocking();
                else
                    progressCanvasGroup.blocksRaycasts = false;
            }
            if (visible)
            {
                progressOverlayHost.transform.SetAsLastSibling();
                Canvas.ForceUpdateCanvases();
            }
        }

        private void SetProgressBarFill(float t)
        {
            if (progressFillImage == null) return;
            progressFillImage.fillAmount = Mathf.Clamp01(t);
        }

        private void SetProgressTexts(string title, string status, string detail, string footer)
        {
            if (progressTitleTmp != null) progressTitleTmp.text = title;
            if (progressStatusTmp != null) progressStatusTmp.text = status;
            if (progressSummaryTmp == null) return;
            if (string.IsNullOrEmpty(footer))
            {
                progressSummaryTmp.text = detail;
            }
            else
            {
                progressSummaryTmp.text = detail + "\n" + footer;
            }
        }

        private static TextMeshProUGUI CreateTmp(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions align,
            TMP_FontAsset font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.richText = true;
            tmp.enableWordWrapping = true;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            tmp.raycastTarget = false;
            return tmp;
        }

        private static TMP_FontAsset ResolveUiFont()
        {
            TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].font != null && texts[i].gameObject.scene.IsValid())
                {
                    return texts[i].font;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        private static bool IsPlayerSpellUnplayableNow(BattleSimulationManager b, SpellCard sp)
        {
            if (sp == null) return true;
            if (b.PlayerHasFieldMonster())
            {
                if (sp.SpellOrdinal == 1) return false;
                return true;
            }
            if (sp.SpellOrdinal == 1) return true;
            if (sp.SpellOrdinal == 2 && !b.CanPlayerCastLinGazeNow()) return true;
            return false;
        }

        private static void TryAutoPlayOneCard(BattleSimulationManager b)
        {
            if (b == null || !b.IsPlayerTurn()) return;
            if (b.GetPlayerPendingDiscardCount() > 0)
            {
                b.AutoDiscardOneForPlayer();
                return;
            }

            if (b.PlayerHasFieldMonster())
            {
                for (int i = 0; i < b.GetPlayerHandCount(); i++)
                {
                    if (b.GetPlayerHandCard(i) is SpellCard sp && !IsPlayerSpellUnplayableNow(b, sp))
                    {
                        b.PlayerPlayCardFromHand(i);
                        return;
                    }
                }
                return;
            }

            bool spellFirst = IsRunning && Random.value < b.AutoSimPlayerSpellFirstChance;
            if (spellFirst)
            {
                for (int i = 0; i < b.GetPlayerHandCount(); i++)
                {
                    if (b.GetPlayerHandCard(i) is SpellCard sp && !IsPlayerSpellUnplayableNow(b, sp))
                    {
                        b.PlayerPlayCardFromHand(i);
                        return;
                    }
                }
                for (int i = 0; i < b.GetPlayerHandCount(); i++)
                {
                    if (b.GetPlayerHandCard(i) is MonsterCard)
                    {
                        b.PlayerPlayCardFromHand(i);
                        return;
                    }
                }
            }
            else
            {
                for (int i = 0; i < b.GetPlayerHandCount(); i++)
                {
                    if (b.GetPlayerHandCard(i) is MonsterCard)
                    {
                        b.PlayerPlayCardFromHand(i);
                        return;
                    }
                }
                for (int i = 0; i < b.GetPlayerHandCount(); i++)
                {
                    if (b.GetPlayerHandCard(i) is SpellCard sp2 && !IsPlayerSpellUnplayableNow(b, sp2))
                    {
                        b.PlayerPlayCardFromHand(i);
                        return;
                    }
                }
            }
        }
    }
}

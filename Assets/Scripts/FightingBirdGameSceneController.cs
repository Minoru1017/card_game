using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 「Fighting bird game」場景控制器：鬥鳥暖身賽。
/// 跟著鼓點看對手鳥勢、按出正確反制；分數條決定勝負，看破條決定戰前情報。
/// 場景為空，UI 全部以程式建構（對齊本專案既有 scaffold 慣例）。
/// 規格：Docs/鬥鳥手勢小遊戲企劃.md
/// </summary>
public sealed class FightingBirdGameSceneController : MonoBehaviour
{
    private const string SceneName = "Fighting bird game";
    private const string HallSceneName = "hall";

#if UNITY_EDITOR
    // BGM 實體檔在 Assets/Music/（不放 Resources）；AudioLibraryPopulator 依此路徑填表。
    public const string ComeAgainAssetPath = "Assets/Music/feinsmecker - Come Again.mp3";
    public const string StampedeAssetPath = "Assets/Music/Risian - Stampede.mp3";
#endif
    public const string HitSfxAssetPath = BirdDuelHitSfxBank.SourceAssetPath;

    private const float BgmVolume = 0.7f;

    // 節奏對齊 BGM：以 AudioSettings.dspTime 為時鐘、等距節拍格點，BGM 以 PlayScheduled 同步排程，零飄移。
    // BPM 與第一下拍偏移由 BirdDuelRhythmSync（Tools/Audio/Analyze Bird Duel BGM Tempo 量測）提供，找不到則用預設。
    private const int CountInBeats = 4;          // 開場數拍（落在 BGM 拍點）
    private const int BeatsPerBar = 4;           // 節拍器小節長度（下拍加重用）
    private const int NormalBeatsPerStep = 4;    // 一般每步 4 拍（命中落在小節下拍）

    // 玩家快要贏時，每步間隔在 2~8 拍之間隨機，打亂節奏、增添臨門一腳的緊張感。
    private const int CloseToWinScoreMargin = 4;  // 距勝門檻 <= 此值視為「快要贏」
    private const int CloseToWinMinBeats = 2;     // 快要贏時每步最短拍數
    private const int CloseToWinMaxBeats = 8;     // 快要贏時每步最長拍數
    private const int TripletsPerBeat = 3;        // 決勝拍 3 連音（每拍 3 格）
    private const int TripletsPerBar = 12;        // 4/4 一小節 12 個三連音格
    private const int DecisiveMinTriplets = 6;    // 決勝最短 ≈ 2 拍（6 連音）
    private const int DecisiveMaxTriplets = 24;   // 決勝最長 ≈ 8 拍（24 連音）
    private const int FakeScareMinGapTriplets = 10;       // 休息 ≥ 此格數才考慮假 scare（≈3.3 拍）
    private const float FakeScareLeadBeats = 2.8f;        // 假 scare 外框收束比平常更長、更明顯
    private const float FakeScareMinScale = 0.62f;        // 收束終點（約鼓面大小）
    private const float FakeScareFrameSize = 256f;        // 方形外框基準邊長（配合程式生成 sprite）
    private const int FakeScareFrameThicknessPx = 11;
    private const int CourtMarchFakeScaresPerMatch = 1;   // 《庭訓進行曲》每局至少嚇 1 次
    private const float PostGrace = 0.16f;       // 命中後仍接受正確輸入的寬限（秒）
    private const float BasePerfectWindow = 0.11f;   // 命中容差（秒）
    private const float BaseGoodWindow = 0.24f;
    private const float BaseTelegraphLeadBeats = 2f;
    private const double ScheduleLeadSeconds = 0.25; // BGM 排程提前量，讓硬體有時間備妥
    private const float DraftButtonBottomOffset = 230f; // 加成／魔王級選項卡距面板底部的偏移（避開下方手勢列）
    private static readonly Vector2 BeatPadAnchor = new Vector2(0f, -60f);
    private const float PerfectBeatShakeDuration = 0.22f;
    private const float PerfectBeatShakeStrength = 13f;

    private float bpm = BirdDuelRhythmSync.DefaultBpm;
    private float firstDownbeatOffset = BirdDuelRhythmSync.DefaultFirstDownbeatOffset;
    private BirdDuelRhythmSync.GridMode rhythmGrid = BirdDuelRhythmSync.GridMode.QuarterBeat;
    private BirdDuelRhythmSync.Profile rhythmProfile = BirdDuelRhythmSync.Profile.Default;
    private float perfectWindow = BasePerfectWindow;
    private float goodWindow = BaseGoodWindow;
    private float telegraphLeadBeats = BaseTelegraphLeadBeats;
    private string activeCdId = BirdDuelCdCatalog.DefaultCdId;
    private float bgmLoopStartSeconds;
    private float bgmLoopLengthSeconds;
    private int activeCloseToWinScoreMargin = BirdDuelRhythmSync.HarborCloseToWinScoreMargin;
    private int activeNormalBeatsPerStep = BirdDuelRhythmSync.HarborNormalBeatsPerStep;
    private int activeDecisiveMinTriplets = BirdDuelRhythmSync.HarborDecisiveMinTriplets;
    private int activeDecisiveMaxTriplets = BirdDuelRhythmSync.HarborDecisiveMaxTriplets;
    private float SecondsPerBeat => 60f / Mathf.Max(1f, bpm);

    private static readonly Color ColorBg = new Color(0.10f, 0.12f, 0.16f, 1f);
    private static readonly Color ColorPanel = new Color(0.16f, 0.19f, 0.25f, 0.96f);
    private static readonly Color ColorPeck = new Color(0.86f, 0.34f, 0.26f, 1f);
    private static readonly Color ColorWing = new Color(0.27f, 0.55f, 0.86f, 1f);
    private static readonly Color ColorNest = new Color(0.92f, 0.74f, 0.24f, 1f);
    private static readonly Color ColorPass = new Color(0.62f, 0.66f, 0.70f, 1f);
    private static readonly Color ColorScoreFill = new Color(0.30f, 0.78f, 0.45f, 1f);
    private static readonly Color ColorInsightFill = new Color(0.92f, 0.74f, 0.24f, 1f);
    private static readonly Color ColorIdle = new Color(0.30f, 0.34f, 0.42f, 1f);
    private static readonly Color ColorSubtitle = new Color(0.78f, 0.82f, 0.88f, 1f);
    private static readonly Color ColorBeatPadIdle = new Color(0.85f, 0.88f, 0.95f, 0.85f);
    private static readonly Color ColorShrinkIdle = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ColorDecisive = new Color(0.98f, 0.55f, 0.20f, 1f); // 決勝拍：熱橙色提示
    private static readonly Color ColorFakeScareRing = new Color(1f, 1f, 1f, 1f);     // 假 scare：白色方形外框

    private static bool subscribed;

    private BirdDuelNpcProfile npc;

    // UI references.
    private TextMeshProUGUI subtitleText;
    private Image opponentPad;
    private TextMeshProUGUI opponentGlyph;
    private TextMeshProUGUI opponentName;
    private RectTransform shrinkIndicator;
    private Image shrinkIndicatorImage;
    private RectTransform fakeScareRing;
    private Image fakeScareRingImage;
    private Image beatPad;
    private RectTransform scoreFill;
    private RectTransform insightFill;
    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI insightLabel;
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI peekHintText;
    private readonly Dictionary<BirdGesture, Image> buttonImages = new Dictionary<BirdGesture, Image>();

    private GameObject resultPanel;
    private TextMeshProUGUI resultTitle;
    private TextMeshProUGUI resultLine;
    private TextMeshProUGUI resultIntel;
    private TextMeshProUGUI leaveButtonLabel;

    private bool decisiveMode; // 玩家快要贏 → 已顯示決勝拍視覺提示
    private bool shrinkIdle = true; // 收束框是否已在閒置狀態（避免每幀重寫 transform）
    private int courtMarchFakeScaresRemaining;
    private bool fakeScareActive;
    private double fakeScareHitDsp;
    private float fakeScareLeadSeconds;
    private float fakeScareEdgeScale = 14f;
    private bool fakeScareImpactFired;
    private static Sprite fakeScareRingSprite;

    // 戰前模式：由戰前預覽進入時為 true，結束後接續戰鬥（而非返回 hall）。
    private bool preBattleMode;
    private string lastIntelText = "";
    private BirdDuelResult lastResult = BirdDuelResult.Lose;

    // 加成抽選（roguelike 分支）。
    private Transform uiRoot;
    private Canvas gameCanvas;
    private GameObject draftPanel;
    private readonly System.Collections.Generic.List<BirdDuelBonusId> chosenBonuses =
        new System.Collections.Generic.List<BirdDuelBonusId>();
    private BirdDuelBonusId selectedEnhancedBonus = BirdDuelBonusId.None;
    private BirdDuelBonusId pendingEnemyBuff = BirdDuelBonusId.None;

    // Match state.
    private int score;
    private int insight;
    private int passUsed;
    private int letNestThroughCount;
    private int scoreBarMax = 1;
    private int insightBarMax = 1;

    private bool beatWindowOpen;
    private bool inputWindowOpen;
    private bool inputLocked;
    private BirdGesture pendingInput;
    private double pendingInputDsp;
    private double currentBeatDsp;
    private bool insightPeekActive;

    // 節拍時鐘（dsp 秒）。
    private double songStartDsp;
    private bool clockRunning;
    private int matchFirstBeat;
    private int lastTickBeat = -1;
    private int lastTickSubdivision = -1;

    private Coroutine matchRoutine;
    private Coroutine beatPadShakeRoutine;
    private AudioSource audioSource;
    private AudioSource hitSfxSource;
    private BirdDuelHitSfxBank hitSfxBank;
    private AudioClip tickClip;
    private AudioClip downbeatClip;

    private AudioSource bgmSource;
    private AudioClip bgmClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        TryBindScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindScene(scene);
    }

    private static void TryBindScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName) return;
        if (Object.FindFirstObjectByType<FightingBirdGameSceneController>() != null) return;

        GameObject host = new GameObject("FightingBirdGameSceneController");
        host.AddComponent<FightingBirdGameSceneController>();
    }

    private void Awake()
    {
        preBattleMode = PreBattleDuelContext.IsActive;
        ConfigurePerformance();
        npc = ResolveNpcProfile();
        ComputeBarMaxes();
        EnsureEventSystem();
        BuildUi();
        SetupAudio();
        StartMatch();
    }

    private BirdDuelNpcProfile ResolveNpcProfile()
    {
        if (PreBattleDuelContext.IsHarborTraining)
            return EnemyHeroCatalog.ResolveForHarbor().ToBirdDuelNpcProfile();

        if (!string.IsNullOrWhiteSpace(PreBattleDuelContext.HeroId))
        {
            EnemyHeroProfile hero = EnemyHeroCatalog.ResolveById(PreBattleDuelContext.HeroId);
            if (hero != null)
                return hero.ToBirdDuelNpcProfile();
        }

        return new BirdDuelNpcProfile();
    }

    /// <summary>節奏遊戲需要穩定 60fps：行動裝置常被預設鎖在 30fps，這裡明確解鎖。</summary>
    private void ConfigurePerformance()
    {
        // vSync 會覆蓋 targetFrameRate；關閉以讓 60fps 生效（行動裝置會忽略 vSync，桌面測試也一致）。
        if (QualitySettings.vSyncCount != 0)
            QualitySettings.vSyncCount = 0;
        if (Application.targetFrameRate != 60)
            Application.targetFrameRate = 60;
    }

    private void Update()
    {
        UpdateMetronome();
        MaintainBgmLoopRegion();
        UpdateBeatVisual();
        HandleKeyboard();
    }

    /// <summary>節拍器：在每個音樂拍（或 8／12 分音格）播放 tick 並脈動拍點。</summary>
    private void UpdateMetronome()
    {
        if (!clockRunning) return;

        if (rhythmGrid == BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth)
        {
            UpdateMetronomeSubdivisionGrid();
            return;
        }

        double elapsed = AudioSettings.dspTime - (songStartDsp + firstDownbeatOffset);
        if (elapsed < 0d) return;

        int beat = (int)System.Math.Floor(elapsed / SecondsPerBeat);
        if (beat < matchFirstBeat || beat == lastTickBeat) return;

        lastTickBeat = beat;
        bool downbeat = ((beat - matchFirstBeat) % BeatsPerBar) == 0;
        PlayTick(downbeat);
        PulseBeatPad();
    }

    /// <summary>8 分／12 分音小節交替；決勝拍改為全程 3 連音（12 格／小節）。</summary>
    private void UpdateMetronomeSubdivisionGrid()
    {
        double elapsedBeats = (AudioSettings.dspTime - (songStartDsp + firstDownbeatOffset)) / SecondsPerBeat;
        if (elapsedBeats < matchFirstBeat) return;

        elapsedBeats -= matchFirstBeat;
        int bar = (int)System.Math.Floor(elapsedBeats / BeatsPerBar);
        double beatInBar = elapsedBeats - bar * BeatsPerBar;
        int subs = UsesDecisiveTripletGrid() ? TripletsPerBar : SubdivisionsInBar(bar);
        double subDuration = BeatsPerBar / subs;
        int sub = (int)System.Math.Floor(beatInBar / subDuration);
        sub = Mathf.Clamp(sub, 0, subs - 1);

        int linearId = bar * 16 + sub;
        if (linearId == lastTickSubdivision) return;

        lastTickSubdivision = linearId;
        bool accent = sub == 0 || sub % TripletsPerBeat == 0;
        PlayTick(accent);
        PulseBeatPad();
    }

    private bool UsesDecisiveTripletGrid() =>
        decisiveMode && rhythmGrid == BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth;

    private double TripletUnitBeats => BeatsPerBar / (double)TripletsPerBar;

    private double SnapToTripletGrid(double beatFraction)
    {
        double unit = TripletUnitBeats;
        return System.Math.Ceiling(beatFraction / unit - 1e-9) * unit;
    }

    private static int SubdivisionsInBar(int barIndex) =>
        (barIndex & 1) == 0 ? 8 : 12;

    private double BeatFractionDsp(double beatFraction) =>
        songStartDsp + firstDownbeatOffset + beatFraction * SecondsPerBeat;

    private void ComputeBarMaxes()
    {
        int maxScore = 0;
        int wingCount = 0;
        for (int i = 0; i < npc.beatPattern.Count; i++)
        {
            BirdGesture opp = npc.beatPattern[i];
            maxScore += BirdDuelCore.BestCounterScore(opp);
            if (opp == BirdGesture.Wing) wingCount++;
        }
        scoreBarMax = Mathf.Max(1, maxScore);
        insightBarMax = Mathf.Max(1, wingCount);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    // ----------------------------------------------------------------- UI build

    private void BuildUi()
    {
        Canvas canvas = CreateCanvas();
        gameCanvas = canvas;
        Transform root = canvas.transform;
        uiRoot = root;

        // 全螢幕背景。
        CreateImage("BG", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, ColorBg, false);

        // 標題與說明。
        CreateText("Title", root, "鬥鳥暖身賽", 64f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 84f), Color.white);
        subtitleText = CreateText("Subtitle", root,
            DefaultSubtitle(), 30f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1400f, 44f),
            ColorSubtitle);

        BuildOpponentArea(root);
        BuildBeatArea(root);
        BuildBars(root);
        BuildFeedback(root);
        BuildButtons(root);
        BuildResultPanel(root);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("FightingBirdGameCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void BuildOpponentArea(Transform root)
    {
        opponentName = CreateText("OpponentName", root, npc.displayName, 34f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(600f, 44f),
            new Color(0.95f, 0.86f, 0.62f, 1f));

        opponentPad = CreateImage("OpponentPad", root,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, ColorIdle, false);
        RectTransform padRt = opponentPad.rectTransform;
        padRt.sizeDelta = new Vector2(220f, 220f);
        padRt.anchoredPosition = new Vector2(0f, -370f);

        opponentGlyph = CreateText("OpponentGlyph", opponentPad.transform, "?", 120f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        opponentGlyph.rectTransform.offsetMin = Vector2.zero;
        opponentGlyph.rectTransform.offsetMax = Vector2.zero;

        peekHintText = CreateText("PeekHint", root, "", 30f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(900f, 40f),
            ColorNest);
    }

    private void BuildBeatArea(Transform root)
    {
        // 收束指示框（由大縮小到命中尺寸）。
        Image shrink = CreateImage("ShrinkIndicator", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            ColorShrinkIdle, false);
        shrink.rectTransform.sizeDelta = new Vector2(160f, 160f);
        shrink.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        shrinkIndicator = shrink.rectTransform;
        shrinkIndicatorImage = shrink;

        // 鼓點中心（命中時脈動）。
        beatPad = CreateImage("BeatPad", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            ColorBeatPadIdle, false);
        beatPad.rectTransform.sizeDelta = new Vector2(150f, 150f);
        beatPad.rectTransform.anchoredPosition = BeatPadAnchor;

        BuildFakeScareRing(root);
    }

    /// <summary>庭訓假 scare：白色方形外框，自螢幕邊緣收束至鼓面（呼應 ShrinkIndicator 方形收束）。</summary>
    private void BuildFakeScareRing(Transform root)
    {
        Image ring = CreateImage("FakeScareFrame", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            ColorFakeScareRing, false);
        ring.sprite = GetFakeScareFrameSprite();
        ring.type = Image.Type.Simple;
        ring.preserveAspect = true;
        ring.rectTransform.sizeDelta = new Vector2(FakeScareFrameSize, FakeScareFrameSize);
        ring.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        fakeScareRing = ring.rectTransform;
        fakeScareRingImage = ring;
        ring.gameObject.SetActive(false);
    }

    private void BuildBars(Transform root)
    {
        // 分數條（左）。
        scoreLabel = CreateText("ScoreLabel", root, "分數 0", 30f, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -250f), new Vector2(360f, 40f), Color.white);
        scoreLabel.rectTransform.pivot = new Vector2(0f, 1f);
        scoreFill = CreateBar(root, "ScoreBar", new Vector2(60f, -300f), new Vector2(360f, 34f), ColorScoreFill);

        // 看破條（右）。
        insightLabel = CreateText("InsightLabel", root, "看破 0", 30f, TextAlignmentOptions.Right,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -250f), new Vector2(360f, 40f), ColorNest);
        insightLabel.rectTransform.pivot = new Vector2(1f, 1f);
        insightFill = CreateBar(root, "InsightBar", new Vector2(-420f, -300f), new Vector2(360f, 34f), ColorInsightFill);
        // 右側條：以右上為錨。
        RectTransform insightBg = insightFill.parent as RectTransform;
        insightBg.anchorMin = new Vector2(1f, 1f);
        insightBg.anchorMax = new Vector2(1f, 1f);
        insightBg.pivot = new Vector2(1f, 1f);
        insightBg.anchoredPosition = new Vector2(-60f, -300f);
    }

    private void BuildFeedback(Transform root)
    {
        feedbackText = CreateText("Feedback", root, "", 46f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(1000f, 60f),
            Color.white);
    }

    private void BuildButtons(Transform root)
    {
        buttonImages.Clear();
        BirdGesture[] order = { BirdGesture.Peck, BirdGesture.Wing, BirdGesture.Nest, BirdGesture.Pass };
        Color[] colors = { ColorPeck, ColorWing, ColorNest, ColorPass };
        string[] hints = { "啄擊\n進攻 +3", "振翅\n防守 +2", "築巢\n看破", "PASS\n防禦 +1" };

        const float btnW = 280f;
        const float btnH = 150f;
        const float gap = 36f;
        float totalW = order.Length * btnW + (order.Length - 1) * gap;
        float startX = -totalW * 0.5f + btnW * 0.5f;

        for (int i = 0; i < order.Length; i++)
        {
            BirdGesture g = order[i];
            float x = startX + i * (btnW + gap);
            Image img = CreateImage("Btn_" + g, root,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, colors[i], true);
            RectTransform rt = img.rectTransform;
            rt.sizeDelta = new Vector2(btnW, btnH);
            rt.anchoredPosition = new Vector2(x, 130f);

            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            BirdGesture captured = g;
            btn.onClick.AddListener(() => RegisterInput(captured));

            TextMeshProUGUI label = CreateText("Label", img.transform, hints[i], 36f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.raycastTarget = false;

            buttonImages[g] = img;
        }
    }

    private void BuildResultPanel(Transform root)
    {
        resultPanel = CreateImage("ResultPanel", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorPanel, true).gameObject;
        RectTransform panelRt = resultPanel.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(1100f, 620f);

        resultTitle = CreateText("ResultTitle", resultPanel.transform, "", 64f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1000f, 84f), Color.white);
        resultLine = CreateText("ResultLine", resultPanel.transform, "", 34f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(980f, 120f),
            new Color(0.86f, 0.9f, 0.96f, 1f));
        resultLine.enableWordWrapping = true;
        resultIntel = CreateText("ResultIntel", resultPanel.transform, "", 36f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(980f, 140f),
            ColorNest);
        resultIntel.enableWordWrapping = true;

        // 再練一次。
        Image replay = CreateImage("ReplayBtn", resultPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, ColorWing, true);
        replay.rectTransform.sizeDelta = new Vector2(320f, 96f);
        replay.rectTransform.anchoredPosition = new Vector2(-190f, 80f);
        Button replayBtn = replay.gameObject.AddComponent<Button>();
        replayBtn.targetGraphic = replay;
        replayBtn.onClick.AddListener(StartMatch);
        TextMeshProUGUI replayLabel = CreateText("Label", replay.transform, "再練一次", 38f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        replayLabel.raycastTarget = false;

        // 進入對戰／返回 hall。
        Image leave = CreateImage("LeaveBtn", resultPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, ColorScoreFill, true);
        leave.rectTransform.sizeDelta = new Vector2(320f, 96f);
        leave.rectTransform.anchoredPosition = new Vector2(190f, 80f);
        Button leaveBtn = leave.gameObject.AddComponent<Button>();
        leaveBtn.targetGraphic = leave;
        leaveBtn.onClick.AddListener(OnLeavePressed);
        leaveButtonLabel = CreateText("Label", leave.transform, preBattleMode ? "進入對戰" : "返回大廳", 38f,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        leaveButtonLabel.raycastTarget = false;

        resultPanel.SetActive(false);
    }

    // ----------------------------------------------------------------- match flow

    private void StartMatch()
    {
        if (matchRoutine != null) StopCoroutine(matchRoutine);
        matchRoutine = StartCoroutine(RunMatch());
    }

    private IEnumerator RunMatch()
    {
        ResetState();
        if (resultPanel != null) resultPanel.SetActive(false);
        SetButtonsInteractable(true);

        // 歌曲從頭重播並重設節拍時鐘（「再練一次」也會重播）。
        RestartSongAndClock();

        // 錨定到 anchor 之後的下一個整拍（beat 0 = BGM 第一下拍點）。
        double anchor = songStartDsp + firstDownbeatOffset;
        double aheadBeats = (AudioSettings.dspTime - anchor) / SecondsPerBeat;
        matchFirstBeat = Mathf.Max(0, Mathf.CeilToInt((float)aheadBeats));
        lastTickBeat = matchFirstBeat - 1;
        clockRunning = true;

        // 數拍預備（count-in），落在 BGM 拍點上；tick/脈動由 UpdateMetronome 處理。
        for (int c = 0; c < CountInBeats; c++)
        {
            if (subtitleText != null)
                subtitleText.text = c < CountInBeats - 1 ? "預備…" : "開始！";
            double beatDsp = BeatDsp(matchFirstBeat + c);
            while (AudioSettings.dspTime < beatDsp)
                yield return null;
        }
        if (subtitleText != null)
            subtitleText.text = DefaultSubtitle();

        IReadOnlyList<BirdGesture> pattern = npc.beatPattern;
        // 首步判定需与 count-in「開始！」拉开整段 NormalBeatsPerStep，否则玩家跟 GO 抢按会固定 Miss。
        // count-in 结束在 beat (matchFirstBeat + CountInBeats - 1)；首步命中在其后 NormalBeatsPerStep 拍。
        double beatCursor = matchFirstBeat + CountInBeats - 1 + activeNormalBeatsPerStep;
        for (int step = 0; step < pattern.Count; step++)
        {
            if (UsesDecisiveTripletGrid())
                beatCursor = SnapToTripletGrid(beatCursor);

            BirdGesture opp = pattern[step];
            double hitDsp = BeatFractionDsp(beatCursor);
            currentBeatDsp = hitDsp;

            // 提前 TelegraphLeadBeats 拍揭露對手鳥勢。
            double telegraphDsp = hitDsp - telegraphLeadBeats * SecondsPerBeat;
            while (AudioSettings.dspTime < telegraphDsp)
                yield return null;

            bool peek = insightPeekActive;
            insightPeekActive = false;
            ShowTelegraph(opp, peek);

            pendingInput = default;
            inputLocked = false;
            inputWindowOpen = false;
            beatWindowOpen = true;

            // 預告期間只顯示收束提示，不接受輸入（避免一看到鳥勢就搶按 → 固定 Miss）。
            double inputOpenDsp = hitDsp - goodWindow;
            while (AudioSettings.dspTime < inputOpenDsp)
                yield return null;

            inputWindowOpen = true;

            double inputCloseDsp = hitDsp + goodWindow;
            while (AudioSettings.dspTime < inputCloseDsp && !inputLocked)
                yield return null;

            beatWindowOpen = false;
            inputWindowOpen = false;

            float timingError = inputLocked ? Mathf.Abs((float)(pendingInputDsp - hitDsp)) : 999f;
            bool passRewardAvailable = passUsed < npc.passLimit;
            BirdGesture? input = inputLocked ? pendingInput : (BirdGesture?)null;

            BirdBeatJudgement judgement = BirdDuelCore.Judge(
                opp, input, timingError, passRewardAvailable, perfectWindow, goodWindow);

            ApplyJudgement(opp, input, judgement);
            ShowFeedback(judgement);
            ClearTelegraph();

            // 玩家快要贏：首次跨過門檻時給視覺提示，並開始拉長／隨機化步距。
            if (!decisiveMode && step < pattern.Count - 1 && score >= npc.winThreshold - activeCloseToWinScoreMargin)
                EnterDecisiveMode();

            // 依目前分數決定到下一步的間隔（快要贏 → 2~8 拍或 8／12 分格隨機）。
            if (step < pattern.Count - 1)
            {
                double nextGapBeats = ResolveNextStepBeatDelta();
                beatCursor += nextGapBeats;
                double nextHitDsp = BeatFractionDsp(beatCursor);

                if (TryPickCourtMarchFakeScareHit(
                        hitDsp, nextHitDsp, nextGapBeats, pattern.Count - 1 - step, out double fakeHitDsp))
                {
                    yield return RunCourtMarchFakeScare(fakeHitDsp);
                }
            }
        }

        clockRunning = false;
        ShowResult();
        matchRoutine = null;
    }

    /// <summary>進入決勝拍：視覺提示並收緊判定窗口／預告時間。</summary>
    private void EnterDecisiveMode()
    {
        decisiveMode = true;
        ApplyDecisiveDifficulty();

        if (subtitleText != null)
        {
            subtitleText.text = UsesDecisiveTripletGrid() || rhythmGrid == BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth
                ? "決勝拍！3 連音——抓準節奏！"
                : "決勝拍！節奏開始變化——抓準鼓點！";
            subtitleText.color = ColorDecisive;
        }
        if (beatPad != null)
            beatPad.color = ColorDecisive;
        if (shrinkIndicatorImage != null)
        {
            Color c = ColorDecisive;
            c.a = shrinkIndicatorImage.color.a; // 透明度仍由 UpdateBeatVisual 控制
            shrinkIndicatorImage.color = c;
        }
        PulseBeatPad();
        lastTickSubdivision = -1;
    }

    private void ApplyDecisiveDifficulty()
    {
        perfectWindow = BasePerfectWindow * rhythmProfile.BasePerfectWindowMul * rhythmProfile.DecisivePerfectWindowMul;
        goodWindow = BaseGoodWindow * rhythmProfile.BaseGoodWindowMul * rhythmProfile.DecisiveGoodWindowMul;
        telegraphLeadBeats = BaseTelegraphLeadBeats * rhythmProfile.BaseTelegraphLeadMul * rhythmProfile.DecisiveTelegraphLeadMul;
    }

    private void ResetJudgementWindows()
    {
        perfectWindow = BasePerfectWindow * rhythmProfile.BasePerfectWindowMul;
        goodWindow = BaseGoodWindow * rhythmProfile.BaseGoodWindowMul;
        telegraphLeadBeats = BaseTelegraphLeadBeats * rhythmProfile.BaseTelegraphLeadMul;
    }

    /// <summary>到下一判定步的音樂拍長。庭訓決勝段以 3 連音格隨機 6~24 格（≈2~8 拍）。</summary>
    private double ResolveNextStepBeatDelta()
    {
        bool closeToWin = score >= npc.winThreshold - activeCloseToWinScoreMargin;
        if (!closeToWin)
            return activeNormalBeatsPerStep;

        if (rhythmGrid == BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth)
        {
            int triplets = UnityEngine.Random.Range(activeDecisiveMinTriplets, activeDecisiveMaxTriplets + 1);
            return triplets * TripletUnitBeats;
        }

        return UnityEngine.Random.Range(CloseToWinMinBeats, CloseToWinMaxBeats + 1);
    }

    /// <summary>庭訓決勝 3 連音長休息：假 scare 大光圈收束（無判定），每局至少 1 次。</summary>
    private bool TryPickCourtMarchFakeScareHit(
        double lastHitDsp,
        double nextHitDsp,
        double gapBeats,
        int stepsUntilLast,
        out double fakeHitDsp)
    {
        fakeHitDsp = 0d;
        if (!UsesDecisiveTripletGrid() || !IsCourtMarchCd())
            return false;

        float leadSec = FakeScareLeadBeats * SecondsPerBeat;
        double restStart = lastHitDsp + goodWindow;
        double restEnd = nextHitDsp - telegraphLeadBeats * SecondsPerBeat;
        if (restEnd - restStart < leadSec + 0.12d)
            return false;

        double gapTriplets = gapBeats / TripletUnitBeats;
        bool mustPlay = courtMarchFakeScaresRemaining > 0;
        bool longRest = gapTriplets >= FakeScareMinGapTriplets;
        bool lastChance = mustPlay && stepsUntilLast <= 2;
        if (!longRest && !lastChance)
            return false;
        if (!mustPlay && UnityEngine.Random.value > 0.28f)
            return false;

        double earliest = restStart + leadSec;
        double latest = restEnd - 0.06d;
        if (latest <= earliest)
            fakeHitDsp = (restStart + restEnd) * 0.5d;
        else
            fakeHitDsp = restStart + (restEnd - restStart) * UnityEngine.Random.Range(0.34f, 0.56f);

        fakeHitDsp = System.Math.Max(earliest, System.Math.Min(latest, fakeHitDsp));
        if (mustPlay)
            courtMarchFakeScaresRemaining--;
        return true;
    }

    private IEnumerator RunCourtMarchFakeScare(double fakeHitDsp)
    {
        fakeScareLeadSeconds = FakeScareLeadBeats * SecondsPerBeat;
        double startDsp = fakeHitDsp - fakeScareLeadSeconds;

        while (AudioSettings.dspTime < startDsp)
            yield return null;

        fakeScareActive = true;
        fakeScareHitDsp = fakeHitDsp;
        fakeScareImpactFired = false;
        fakeScareEdgeScale = ResolveFakeScareEdgeScale();
        if (fakeScareRing != null)
        {
            fakeScareRing.gameObject.SetActive(true);
            fakeScareRing.localScale = Vector3.one * fakeScareEdgeScale;
        }
        if (fakeScareRingImage != null)
        {
            Color c = ColorFakeScareRing;
            c.a = 0.92f;
            fakeScareRingImage.color = c;
        }

        while (AudioSettings.dspTime < fakeHitDsp + 0.14d)
            yield return null;

        fakeScareActive = false;
        fakeScareHitDsp = 0d;
        HideFakeScareRing();
        if (beatPad != null && decisiveMode)
            beatPad.color = ColorDecisive;
    }

    private void HideFakeScareRing()
    {
        if (fakeScareRing == null) return;
        fakeScareRing.gameObject.SetActive(false);
        fakeScareRing.localScale = Vector3.one;
        if (fakeScareRingImage != null)
            fakeScareRingImage.color = ColorFakeScareRing;
    }

    private float ResolveFakeScareEdgeScale()
    {
        if (gameCanvas == null || beatPad == null)
            return 14f;

        RectTransform canvasRt = gameCanvas.transform as RectTransform;
        Vector2 beatPos = beatPad.rectTransform.anchoredPosition;
        float halfW = canvasRt.rect.width * 0.5f;
        float halfH = canvasRt.rect.height * 0.5f;
        Vector2[] corners =
        {
            new Vector2(-halfW, -halfH),
            new Vector2(-halfW, halfH),
            new Vector2(halfW, halfH),
            new Vector2(halfW, -halfH),
        };

        float maxHalfSide = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            float dx = Mathf.Abs(beatPos.x - corners[i].x);
            float dy = Mathf.Abs(beatPos.y - corners[i].y);
            maxHalfSide = Mathf.Max(maxHalfSide, Mathf.Max(dx, dy));
        }

        return maxHalfSide * 2.08f / (FakeScareFrameSize * 0.5f);
    }

    private static Sprite GetFakeScareFrameSprite()
    {
        if (fakeScareRingSprite != null)
            return fakeScareRingSprite;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float outerHalf = center - 1.5f;
        float innerHalf = outerHalf - FakeScareFrameThicknessPx;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float ax = Mathf.Abs(x + 0.5f - center);
                float ay = Mathf.Abs(y + 0.5f - center);
                bool inOuter = ax <= outerHalf && ay <= outerHalf;
                bool inInner = ax <= innerHalf && ay <= innerHalf;
                tex.SetPixel(x, y, inOuter && !inInner ? Color.white : Color.clear);
            }
        }

        tex.Apply(false, true);
        fakeScareRingSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        return fakeScareRingSprite;
    }

    private bool IsCourtMarchCd() =>
        string.Equals(activeCdId, "court_march", System.StringComparison.OrdinalIgnoreCase);

    private void ResetState()
    {
        score = 0;
        insight = 0;
        passUsed = 0;
        letNestThroughCount = 0;
        insightPeekActive = false;
        beatWindowOpen = false;
        inputWindowOpen = false;
        inputLocked = false;
        clockRunning = false;
        currentBeatDsp = 0d;
        lastTickBeat = -1;
        lastTickSubdivision = -1;
        decisiveMode = false;
        ResetJudgementWindows();
        courtMarchFakeScaresRemaining = IsCourtMarchCd() ? CourtMarchFakeScaresPerMatch : 0;
        fakeScareActive = false;
        fakeScareHitDsp = 0d;
        fakeScareImpactFired = false;
        HideFakeScareRing();
        StopBeatPadShake();
        shrinkIdle = false; // 強制下一幀重設閒置 transform
        if (subtitleText != null) subtitleText.color = ColorSubtitle;
        if (beatPad != null) beatPad.color = ColorBeatPadIdle;
        if (shrinkIndicatorImage != null) shrinkIndicatorImage.color = ColorShrinkIdle;
        if (feedbackText != null) feedbackText.text = "";
        if (peekHintText != null) peekHintText.text = "";
        UpdateBars();
        ClearTelegraph();
    }

    private void ApplyJudgement(BirdGesture opp, BirdGesture? input, BirdBeatJudgement judgement)
    {
        score = Mathf.Max(0, score + judgement.scoreDelta);
        insight += judgement.insightDelta;
        if (judgement.letNestThrough) letNestThroughCount++;
        if (input.HasValue && input.Value == BirdGesture.Pass) passUsed++;

        // 成功築巢（反制振翅）→ 取得看破，下一拍提早揭露對手鳥勢。
        if (judgement.isBestCounter && opp == BirdGesture.Wing)
            insightPeekActive = true;

        if (judgement.outcome == BirdBeatOutcome.Perfect)
            PlayPerfectBeatShake();

        PlayHitOutcomeSfx(judgement.outcome);

        UpdateBars();
    }

    private void ShowResult()
    {
        clockRunning = false;
        SetButtonsInteractable(false);
        ClearTelegraph();
        if (feedbackText != null) feedbackText.text = "";

        BirdDuelResult result = BirdDuelCore.ResolveResult(score, npc.winThreshold, npc.drawThreshold);
        int tier = BirdDuelCore.ResolveIntelTier(insight, letNestThroughCount, passUsed, npc.passLimit);
        lastResult = result;
        lastIntelText = npc.ResolveIntelText(tier);

        if (resultTitle != null)
        {
            switch (result)
            {
                case BirdDuelResult.Win: resultTitle.text = "鬥鳥勝利"; resultTitle.color = ColorScoreFill; break;
                case BirdDuelResult.Draw: resultTitle.text = "平手"; resultTitle.color = ColorNest; break;
                default: resultTitle.text = "再練一次"; resultTitle.color = ColorPass; break;
            }
        }

        if (resultLine != null)
            resultLine.text = $"{npc.ResolveResultLine(result)}\n分數 {score} / {scoreBarMax}　看破 {insight}";
        if (resultIntel != null)
            resultIntel.text = lastIntelText;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }
    }

    private string DefaultSubtitle()
    {
        const string baseLine = "看對手鳥勢，在鼓點按正確反制：啄擊←巢　振翅←啄　築巢←翅";
        if (preBattleMode && PreBattleDuelContext.HasHiddenTier)
            return baseLine + "　｜　勝出鬥鳥可挑戰魔王級";
        return baseLine;
    }

    private void OnLeavePressed()
    {
        if (preBattleMode)
        {
            BeginBonusDraft();
            return;
        }
        ReturnToHall();
    }

    // ----------------------------------------------------------------- 加成抽選（roguelike 分支）

    private void BeginBonusDraft()
    {
        chosenBonuses.Clear();
        selectedEnhancedBonus = BirdDuelBonusId.None;
        pendingEnemyBuff = BirdDuelBonusId.None;

        switch (lastResult)
        {
            case BirdDuelResult.Win:
            {
                System.Collections.Generic.List<BirdDuelBonusId> opts;
                if (PreBattleCdContext.ShouldUseCdDraftPool(lastResult))
                {
                    var whitelist = BirdDuelCdCatalog.ResolveWinDraftBonusIds(PreBattleCdContext.SelectedCdId);
                    opts = BirdDuelBonusCatalog.DrawDistinctFromIds(whitelist, 3);
                    if (opts.Count == 0)
                        opts = BirdDuelBonusCatalog.DrawDistinct(BirdDuelBonusPool.Enhanced, 3);
                }
                else
                    opts = BirdDuelBonusCatalog.DrawDistinct(BirdDuelBonusPool.Enhanced, 3);

                string cdName = BirdDuelCdCatalog.Get(PreBattleCdContext.SelectedCdId)?.DisplayName;
                string subtitle = PreBattleCdContext.ShouldUseCdDraftPool(lastResult) && !string.IsNullOrWhiteSpace(cdName)
                    ? "CD「" + cdName + "」偏向加成池"
                    : "選擇一項強化加成";
                ShowDraftChoices("鬥鳥勝利", subtitle, opts, pick =>
                {
                    selectedEnhancedBonus = pick;
                    chosenBonuses.Add(pick);
                    if (PreBattleDuelContext.HasHiddenTier)
                        ShowBossChoice();
                    else
                        FinishDraft(false);
                });
                break;
            }
            case BirdDuelResult.Draw:
            {
                var opts = BirdDuelBonusCatalog.DrawDistinct(BirdDuelBonusPool.Basic, 3);
                ShowDraftChoices("平手", "選擇一項基礎加成", opts, pick =>
                {
                    chosenBonuses.Add(pick);
                    FinishDraft(false);
                });
                break;
            }
            default:
            {
                // 敗北：保底 1 個基礎加成 ＋ 敵方小強化（風險模型 B）。
                BirdDuelBonusId basic = BirdDuelBonusCatalog.DrawOne(BirdDuelBonusPool.Basic);
                pendingEnemyBuff = BirdDuelBonusCatalog.DrawOne(BirdDuelBonusPool.EnemyBuff);
                if (basic != BirdDuelBonusId.None) chosenBonuses.Add(basic);
                ShowLossConfirm(basic, pendingEnemyBuff);
                break;
            }
        }
    }

    private void ShowBossChoice()
    {
        GameObject panel = BuildDraftPanel("鬥鳥全勝", "是否挑戰魔王級？高風險高報酬，額外獲得 1 個稀有加成。");

        Button bossBtn = CreateDraftButton(panel.transform, "BossBtn",
            "挑戰魔王級", "額外稀有加成＋難度升為魔王級", ColorPeck, new Vector2(-260f, DraftButtonBottomOffset));
        bossBtn.onClick.AddListener(() =>
        {
            BirdDuelBonusId rare = BirdDuelBonusCatalog.DrawOne(BirdDuelBonusPool.Rare);
            if (rare != BirdDuelBonusId.None) chosenBonuses.Add(rare);
            FinishDraft(true);
        });

        Button safeBtn = CreateDraftButton(panel.transform, "SafeBtn",
            "打所選難度", "帶著加成穩穩開打", ColorScoreFill, new Vector2(260f, DraftButtonBottomOffset));
        safeBtn.onClick.AddListener(() => FinishDraft(false));
    }

    private void ShowLossConfirm(BirdDuelBonusId basic, BirdDuelBonusId enemyBuff)
    {
        string body = "保底加成：" + DescribeBonus(basic) + "\n敵方強化：" + DescribeBonus(enemyBuff);
        GameObject panel = BuildDraftPanel("再接再厲", body);

        Button goBtn = CreateDraftButton(panel.transform, "GoBtn",
            "進入對戰", "帶著保底加成開打", ColorScoreFill, new Vector2(0f, DraftButtonBottomOffset));
        goBtn.onClick.AddListener(() => FinishDraft(false));
    }

    private void ShowDraftChoices(
        string title, string subtitle,
        System.Collections.Generic.List<BirdDuelBonusId> options,
        System.Action<BirdDuelBonusId> onPick)
    {
        GameObject panel = BuildDraftPanel(title, subtitle);

        int n = options.Count;
        const float cardW = 320f;
        const float gap = 36f;
        float totalW = n * cardW + (n - 1) * gap;
        float startX = -totalW * 0.5f + cardW * 0.5f;
        Color[] tints = { ColorWing, ColorNest, ColorScoreFill };

        for (int i = 0; i < n; i++)
        {
            BirdDuelBonusId id = options[i];
            BirdDuelBonusInfo info = BirdDuelBonusCatalog.Get(id);
            float x = startX + i * (cardW + gap);
            Button btn = CreateDraftButton(panel.transform, "Opt_" + id,
                info.DisplayName, info.Description, tints[i % tints.Length], new Vector2(x, DraftButtonBottomOffset));
            BirdDuelBonusId captured = id;
            btn.onClick.AddListener(() => onPick(captured));
        }
    }

    private void FinishDraft(bool challengeHiddenTier)
    {
        CloseDraftPanel();
        if (selectedEnhancedBonus != BirdDuelBonusId.None &&
            !chosenBonuses.Contains(selectedEnhancedBonus))
        {
            chosenBonuses.Add(selectedEnhancedBonus);
        }

        PreBattleBonusContext.Begin(new List<BirdDuelBonusId>(chosenBonuses), pendingEnemyBuff);
        ProceedToBattleAfterBirdDuel(challengeHiddenTier);
    }

    private void ProceedToBattleAfterBirdDuel(bool challengeHiddenTier)
    {
        if (preBattleMode && PreBattleDuelContext.IsHarborTraining && gameCanvas != null)
        {
            EnemyHeroProfile hero = EnemyHeroCatalog.ResolveForHarbor();
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            bool isRematch = HarborTrainingProgressState.HasMetHotBloodClassmate(slot);
            EnemyHeroPortraitBridgeUi.ShowPortraitB(
                gameCanvas,
                hero,
                isRematch,
                lastResult,
                null,
                () =>
                {
                    if (!isRematch)
                        HarborTrainingProgressState.MarkHotBloodClassmateMet(slot);
                    SceneLoader.ResumeBattleAfterBirdDuel(challengeHiddenTier, lastIntelText);
                });
            return;
        }

        SceneLoader.ResumeBattleAfterBirdDuel(challengeHiddenTier, lastIntelText);
    }

    private GameObject BuildDraftPanel(string title, string subtitle)
    {
        CloseDraftPanel();
        if (uiRoot == null) uiRoot = transform;

        GameObject overlay = CreateImage("BonusDraftOverlay", uiRoot,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f), true).gameObject;
        overlay.transform.SetAsLastSibling();

        Image panelImg = CreateImage("Panel", overlay.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorPanel, true);
        panelImg.rectTransform.sizeDelta = new Vector2(1180f, 560f);

        CreateText("DraftTitle", panelImg.transform, title, 58f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1100f, 76f), Color.white);
        TextMeshProUGUI sub = CreateText("DraftSubtitle", panelImg.transform, subtitle, 32f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1080f, 120f), ColorSubtitle);
        sub.enableWordWrapping = true;

        draftPanel = overlay;
        return panelImg.gameObject;
    }

    /// <summary>建立一張加成選項卡（名稱＋描述），錨定於面板底部中央偏移處。</summary>
    private Button CreateDraftButton(Transform panel, string name, string title, string desc, Color tint, Vector2 anchoredPos)
    {
        Image card = CreateImage(name, panel,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, tint, true);
        card.rectTransform.sizeDelta = new Vector2(320f, 200f);
        card.rectTransform.anchoredPosition = anchoredPos;

        Button btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card;

        TextMeshProUGUI titleText = CreateText("Title", card.transform, title, 38f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(300f, 60f), Color.white);
        titleText.raycastTarget = false;
        TextMeshProUGUI descText = CreateText("Desc", card.transform, desc, 26f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(296f, 110f),
            new Color(0.97f, 0.98f, 1f, 0.95f));
        descText.enableWordWrapping = true;
        descText.raycastTarget = false;
        return btn;
    }

    private string DescribeBonus(BirdDuelBonusId id)
    {
        if (id == BirdDuelBonusId.None) return "無";
        BirdDuelBonusInfo info = BirdDuelBonusCatalog.Get(id);
        return info.DisplayName + "（" + info.Description + "）";
    }

    private void CloseDraftPanel()
    {
        if (draftPanel != null)
        {
            Destroy(draftPanel);
            draftPanel = null;
        }
    }

    private void ReturnToHall()
    {
        if (Application.CanStreamedLevelBeLoaded(HallSceneName))
            SceneManager.LoadScene(HallSceneName);
        else
            Debug.LogWarning("FightingBirdGameSceneController: hall 場景不在 Build Settings，無法返回。");
    }

    // ----------------------------------------------------------------- input

    private void RegisterInput(BirdGesture gesture)
    {
        if (!inputWindowOpen || inputLocked || currentBeatDsp <= 0d) return;

        float timingErrorNow = Mathf.Abs((float)(AudioSettings.dspTime - currentBeatDsp));
        if (timingErrorNow > goodWindow) return;

        pendingInput = gesture;
        pendingInputDsp = AudioSettings.dspTime;
        inputLocked = true;
        FlashButton(gesture);
    }

    private void HandleKeyboard()
    {
        if (!inputWindowOpen || inputLocked) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) RegisterInput(BirdGesture.Peck);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) RegisterInput(BirdGesture.Wing);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) RegisterInput(BirdGesture.Nest);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Space))
            RegisterInput(BirdGesture.Pass);
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (KeyValuePair<BirdGesture, Image> pair in buttonImages)
        {
            if (pair.Value == null) continue;
            Button btn = pair.Value.GetComponent<Button>();
            if (btn != null) btn.interactable = value;
            Color c = pair.Value.color;
            c.a = value ? 1f : 0.45f;
            pair.Value.color = c;
        }
    }

    // ----------------------------------------------------------------- visuals

    private void ShowTelegraph(BirdGesture opp, bool peek)
    {
        if (opponentPad != null) opponentPad.color = GestureColor(opp);
        if (opponentGlyph != null) opponentGlyph.text = BirdDuelCore.ShortName(opp);

        if (peekHintText == null) return;
        if (peek)
            peekHintText.text = $"看破：對手準備 {BirdDuelCore.DisplayName(opp)}";
        else
            peekHintText.text = "";
    }

    private void ClearTelegraph()
    {
        if (opponentPad != null) opponentPad.color = ColorIdle;
        if (opponentGlyph != null) opponentGlyph.text = "?";
        if (peekHintText != null) peekHintText.text = "";
    }

    private void ShowFeedback(BirdBeatJudgement judgement)
    {
        if (feedbackText == null) return;

        string main;
        Color color;
        switch (judgement.outcome)
        {
            case BirdBeatOutcome.Perfect: main = "Perfect"; color = ColorScoreFill; break;
            case BirdBeatOutcome.Good: main = "Good"; color = new Color(0.55f, 0.85f, 0.6f, 1f); break;
            case BirdBeatOutcome.Guard: main = "Guard"; color = ColorPass; break;
            default: main = "Miss"; color = ColorPeck; break;
        }

        if (judgement.scoreDelta > 0) main += $" +{judgement.scoreDelta}";
        if (judgement.insightDelta > 0) main += "　看破 +1";

        feedbackText.text = main;
        feedbackText.color = color;
    }

    private void UpdateBeatVisual()
    {
        if (fakeScareActive && fakeScareHitDsp > 0d && fakeScareRing != null)
        {
            float lead = Mathf.Max(0.0001f, fakeScareLeadSeconds);
            float remaining = (float)(fakeScareHitDsp - AudioSettings.dspTime);
            float linearT = Mathf.Clamp01(remaining / lead);
            float easedT = linearT * linearT * linearT; // 後段加速收束，營造「猛然」感
            float scale = Mathf.Lerp(FakeScareMinScale, fakeScareEdgeScale, easedT);
            fakeScareRing.localScale = new Vector3(scale, scale, 1f);

            if (fakeScareRingImage != null)
            {
                float alpha = Mathf.Lerp(0.18f, 0.98f, Mathf.Pow(linearT, 0.55f));
                Color c = ColorFakeScareRing;
                c.a = alpha;
                fakeScareRingImage.color = c;
            }

            if (remaining <= 0f && !fakeScareImpactFired)
            {
                fakeScareImpactFired = true;
                PulseBeatPad();
                PlayTick(true);
                if (beatPad != null)
                    beatPad.color = Color.white;
            }

            return;
        }

        if (shrinkIndicator == null) return;

        if (beatWindowOpen && currentBeatDsp > 0d)
        {
            float lead = telegraphLeadBeats * SecondsPerBeat;
            float remaining = (float)(currentBeatDsp - AudioSettings.dspTime);
            float t = Mathf.Clamp01(remaining / Mathf.Max(0.0001f, lead)); // 1 → 0
            float scale = Mathf.Lerp(1f, 2.4f, t);
            shrinkIndicator.localScale = new Vector3(scale, scale, 1f);
            shrinkIdle = false;
            if (shrinkIndicatorImage != null)
            {
                float alpha = Mathf.Lerp(0.55f, 0.12f, t);
                Color c = shrinkIndicatorImage.color; c.a = alpha; shrinkIndicatorImage.color = c;
            }
        }
        else if (!shrinkIdle)
        {
            // 只在進入閒置時寫一次，避免每幀變更 transform 而反覆觸發 Canvas 重新批次。
            shrinkIndicator.localScale = Vector3.one * 2.4f;
            shrinkIdle = true;
            if (shrinkIndicatorImage != null && decisiveMode)
            {
                Color c = ColorDecisive;
                c.a = 0.20f;
                shrinkIndicatorImage.color = c;
            }
            else if (shrinkIndicatorImage != null)
            {
                shrinkIndicatorImage.color = ColorShrinkIdle;
            }
            if (beatPad != null && decisiveMode)
                beatPad.color = ColorDecisive;
        }
    }

    private void PulseBeatPad()
    {
        if (beatPad != null) StartCoroutine(PulseRoutine(beatPad.rectTransform));
    }

    private void PlayPerfectBeatShake()
    {
        if (beatPad == null) return;
        if (beatPadShakeRoutine != null)
            StopCoroutine(beatPadShakeRoutine);
        beatPadShakeRoutine = StartCoroutine(PerfectBeatShakeRoutine(beatPad.rectTransform));
    }

    private void StopBeatPadShake()
    {
        if (beatPadShakeRoutine != null)
        {
            StopCoroutine(beatPadShakeRoutine);
            beatPadShakeRoutine = null;
        }

        if (beatPad != null)
            beatPad.rectTransform.anchoredPosition = BeatPadAnchor;
    }

    private IEnumerator PerfectBeatShakeRoutine(RectTransform rt)
    {
        if (rt == null) yield break;

        Vector2 origin = BeatPadAnchor;
        rt.anchoredPosition = origin;
        float t = 0f;
        while (t < PerfectBeatShakeDuration && rt != null)
        {
            t += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(t / PerfectBeatShakeDuration);
            float phase = t * 52f;
            float x = Mathf.Sin(phase) * PerfectBeatShakeStrength * damper;
            float y = Mathf.Cos(phase * 1.15f) * PerfectBeatShakeStrength * 0.38f * damper;
            rt.anchoredPosition = origin + new Vector2(x, y);
            yield return null;
        }

        if (rt != null)
            rt.anchoredPosition = origin;
        beatPadShakeRoutine = null;
    }

    private IEnumerator PulseRoutine(RectTransform rt)
    {
        if (rt == null) yield break;
        float duration = 0.18f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f + 0.25f * (1f - t / duration);
            rt.localScale = new Vector3(k, k, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private void FlashButton(BirdGesture gesture)
    {
        if (buttonImages.TryGetValue(gesture, out Image img) && img != null)
            StartCoroutine(FlashRoutine(img));
    }

    private IEnumerator FlashRoutine(Image img)
    {
        if (img == null) yield break;
        Color baseColor = img.color;
        img.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (img != null) img.color = baseColor;
    }

    private void UpdateBars()
    {
        if (scoreFill != null)
            scoreFill.anchorMax = new Vector2(Mathf.Clamp01((float)score / scoreBarMax), 1f);
        if (insightFill != null)
            insightFill.anchorMax = new Vector2(Mathf.Clamp01((float)insight / insightBarMax), 1f);
        if (scoreLabel != null) scoreLabel.text = $"分數 {score}";
        if (insightLabel != null) insightLabel.text = $"看破 {insight}";
    }

    private static Color GestureColor(BirdGesture gesture)
    {
        switch (gesture)
        {
            case BirdGesture.Peck: return ColorPeck;
            case BirdGesture.Wing: return ColorWing;
            case BirdGesture.Nest: return ColorNest;
            default: return ColorPass;
        }
    }

    // ----------------------------------------------------------------- audio

    private void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        tickClip = BuildClickClip(900f, 0.05f);
        downbeatClip = BuildClickClip(500f, 0.07f);

        hitSfxSource = gameObject.AddComponent<AudioSource>();
        hitSfxSource.playOnAwake = false;
        hitSfxSource.spatialBlend = 0f;
        hitSfxSource.bypassListenerEffects = true;
        hitSfxSource.ignoreListenerPause = true;
        hitSfxBank = BirdDuelHitSfxBank.TryCreate(ResolveHitSfxSourceClip());

        SetupBgm();
    }

    private AudioClip ResolveHitSfxSourceClip()
    {
        AudioLibrary library = AudioLibrary.Instance;
        if (library != null && library.BirdDuelHitSfxSource != null)
            return library.BirdDuelHitSfxSource;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(HitSfxAssetPath);
#else
        return null;
#endif
    }

    private void PlayHitOutcomeSfx(BirdBeatOutcome outcome)
    {
        if (hitSfxSource == null || hitSfxBank == null || !hitSfxBank.IsReady)
            return;

        AudioClip clip = hitSfxBank.ResolveClip(outcome);
        if (clip == null)
            return;

        hitSfxSource.PlayOneShot(clip, BirdDuelHitSfxBank.ResolveVolume(outcome));
    }

    /// <summary>鬥鳥預設曲：feinsmecker - Come Again。建立並備妥音源；實際排程於每場開始時 <see cref="RestartSongAndClock"/>。</summary>
    private void SetupBgm()
    {
        LoadRhythmSync();
        ResolveBgmClipIfMissing();

        if (bgmClip == null)
        {
            Debug.LogWarning(
                "FightingBirdGameSceneController: 找不到鬥鳥 BGM（cd=" + activeCdId + "），節拍將靜音進行。");
            return;
        }

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = !rhythmProfile.UsesCustomBgmLoop;
        bgmSource.spatialBlend = 0f;
        bgmSource.bypassListenerEffects = true;
        bgmSource.ignoreListenerPause = true;
        bgmSource.volume = BgmVolume;
        bgmSource.clip = bgmClip;

        if (bgmClip.loadState != AudioDataLoadState.Loaded)
            bgmClip.LoadAudioData();
    }

    /// <summary>每場開始（含「再練一次」）：歌曲從頭重播，並把節拍時鐘錨點重設到新的排程起點。</summary>
    private void RestartSongAndClock()
    {
        // 節拍時鐘錨點：BGM 從此 dsp 時間開始，第一個下拍 = songStartDsp + firstDownbeatOffset。
        // 即使缺 BGM 也設定，讓節拍格點（靜音）仍可運作。
        songStartDsp = AudioSettings.dspTime + ScheduleLeadSeconds;

        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.time = rhythmProfile.UsesCustomBgmLoop ? bgmLoopStartSeconds : 0f;
        bgmSource.PlayScheduled(songStartDsp);
    }

    /// <summary>自訂循環區：每圈長度對齊港灣練習帶，避免長曲前奏過短的有效演奏感。</summary>
    private void MaintainBgmLoopRegion()
    {
        if (bgmSource == null || !bgmSource.isPlaying || bgmLoopLengthSeconds <= 0.01f)
            return;

        if (bgmSource.time >= bgmLoopStartSeconds + bgmLoopLengthSeconds)
            bgmSource.time = bgmLoopStartSeconds;
    }

    private void LoadRhythmSync()
    {
        activeCdId = ResolveActiveCdId();
        rhythmProfile = BirdDuelRhythmSync.ResolveForCd(activeCdId);
        bpm = rhythmProfile.Bpm;
        firstDownbeatOffset = rhythmProfile.FirstDownbeatOffset;
        rhythmGrid = rhythmProfile.Grid;
        bgmLoopStartSeconds = rhythmProfile.BgmLoopStartSeconds;
        bgmLoopLengthSeconds = rhythmProfile.BgmLoopLengthSeconds;
        activeCloseToWinScoreMargin = rhythmProfile.CloseToWinScoreMargin;
        activeNormalBeatsPerStep = rhythmProfile.NormalBeatsPerStep;
        activeDecisiveMinTriplets = rhythmProfile.DecisiveMinTriplets;
        activeDecisiveMaxTriplets = rhythmProfile.DecisiveMaxTriplets;
        ResetJudgementWindows();
    }

    private static string ResolveActiveCdId()
    {
        if (PreBattleCdContext.HasSelection)
            return PreBattleCdContext.SelectedCdId;
        return BirdDuelCdCatalog.DefaultCdId;
    }

    /// <summary>第 beatIndex 個整拍的 dsp 命中時間（count-in 用）。</summary>
    private double BeatDsp(int beatIndex) =>
        songStartDsp + firstDownbeatOffset + beatIndex * SecondsPerBeat;

    private void ResolveBgmClipIfMissing()
    {
        if (bgmClip != null)
            return;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null)
            bgmClip = library.GetBirdDuelCdBgm(activeCdId);

#if UNITY_EDITOR
        if (bgmClip == null)
        {
            string path = string.Equals(activeCdId, "court_march", System.StringComparison.OrdinalIgnoreCase)
                ? StampedeAssetPath
                : ComeAgainAssetPath;
            bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
#endif
    }

    private void OnDestroy()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
        hitSfxBank?.Release();
        hitSfxBank = null;
    }

    private void PlayTick(bool downbeat)
    {
        if (audioSource == null) return;
        AudioClip clip = downbeat ? downbeatClip : tickClip;
        if (clip != null) audioSource.PlayOneShot(clip, downbeat ? 0.9f : 0.7f);
    }

    private static AudioClip BuildClickClip(float frequency, float duration)
    {
        const int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        AudioClip clip = AudioClip.Create("birdTick", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 28f); // 快速衰減的鼓點感
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.6f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    // ----------------------------------------------------------------- ui helpers

    private static Image CreateImage(
        string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        Color color, bool raycastTarget)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = raycastTarget;
        return img;
    }

    private static TextMeshProUGUI CreateText(
        string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        UiFontResolver.ApplyTo(tmp, text);
        return tmp;
    }

    /// <summary>建立水平進度條，回傳「填充」RectTransform（以 anchorMax.x 表示進度）。</summary>
    private static RectTransform CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        Image bg = CreateImage(name + "Bg", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero,
            new Color(0.08f, 0.09f, 0.12f, 0.9f), false);
        RectTransform bgRt = bg.rectTransform;
        bgRt.pivot = new Vector2(0f, 1f);
        bgRt.sizeDelta = size;
        bgRt.anchoredPosition = anchoredPos;

        Image fill = CreateImage(name + "Fill", bg.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, fillColor, false);
        RectTransform fillRt = fill.rectTransform;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        return fillRt;
    }
}

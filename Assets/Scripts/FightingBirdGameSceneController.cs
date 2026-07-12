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
public sealed partial class FightingBirdGameSceneController : MonoBehaviour
{
    private const string SceneName = "Fighting bird game";
    private const string HallSceneName = "hall";

#if UNITY_EDITOR
    // BGM 實體檔在 Assets/Music/（不放 Resources）；AudioLibraryPopulator 依此路徑填表。
    public const string ComeAgainAssetPath = "Assets/Music/feinsmecker - Come Again.mp3";
    public const string StampedeAssetPath = "Assets/Music/Risian - Stampede.mp3";
    public const string MorningPrayerAssetPath = "Assets/Music/Naama Zafran - Who Are You Hiding From.mp3";
    public const string RiverForkWaveAssetPath = "Assets/Music/ES_Maracuja - Kristoffer Adamah.mp3";
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

    private static Color ColorBg => BirdDuelUiColors.SceneBg;
    private static Color ColorPanel => BirdDuelUiColors.ScenePanel;
    private static Color ColorPeck => BirdDuelUiColors.GesturePeck;
    private static Color ColorWing => BirdDuelUiColors.GestureWing;
    private static Color ColorNest => BirdDuelUiColors.GestureNest;
    private static Color ColorPass => BirdDuelUiColors.GesturePass;
    private static Color ColorScoreFill => BirdDuelUiColors.ScoreFill;
    private static Color ColorInsightFill => BirdDuelUiColors.InsightFill;
    private static Color ColorIdle => BirdDuelUiColors.OpponentIdle;
    private static Color ColorSubtitle => BirdDuelUiColors.Subtitle;
    private static Color ColorBeatPadIdle => BirdDuelUiColors.BeatPadIdle;
    private static Color ColorShrinkIdle => BirdDuelUiColors.ShrinkIdle;
    private static Color ColorDecisive => BirdDuelUiColors.Decisive;
    private static Color ColorFakeScareRing => BirdDuelUiColors.FakeScareRing;

    private static bool subscribed;

    private BirdDuelNpcProfile npc;

    // UI references.
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI m13ForkLeftBadge;
    private TextMeshProUGUI m13ForkRightBadge;
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
    private bool m13StoryMode;
    private string lastIntelText = "";
    private BirdDuelResult lastResult = BirdDuelResult.Lose;

    // 加成抽選（roguelike 分支）。
    private Transform uiRoot;
    private Canvas gameCanvas;
    private Canvas beatFxCanvas;
    private Canvas overlayCanvas;
    private Transform overlayRoot;
    private GameObject resultOverlayRoot;
    private GameObject replayButtonRoot;
    // 收束／假 scare 動畫快取：僅在數值變化時寫 transform，減少 Canvas 網格重建。
    private float shrinkAnimScale = -1f;
    private float shrinkAnimAlpha = -1f;
    private float fakeScareAnimScale = -1f;
    private float fakeScareAnimAlpha = -1f;
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
        m13StoryMode = M13StoryDuelContext.IsActive;
        preBattleMode = PreBattleDuelContext.IsActive && !m13StoryMode;
        ConfigurePerformance();
        npc = ResolveNpcProfile();
        EnsureEventSystem();
        BuildUi();
        SetupAudio();
        ComputeBarMaxes();
        if (m13StoryMode)
            ConfigureM13StoryUi();
        StartMatch();
    }

    private BirdDuelNpcProfile ResolveNpcProfile()
    {
        if (m13StoryMode)
            return M13BirdDuelNpcProfile.Create();

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
        // 降低排程延遲，避免收束動畫「一頓一頓」。
        if (QualitySettings.maxQueuedFrames != 1)
            QualitySettings.maxQueuedFrames = 1;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void Update()
    {
        UpdateMetronome();
        MaintainBgmLoopRegion();
        HandleKeyboard();
    }

    private void LateUpdate()
    {
        UpdateBeatVisual();
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
        IReadOnlyList<BirdGesture> pattern = BirdDuelRhythmChart.ResolveBeatPattern(activeCdId, npc.beatPattern);
        for (int i = 0; i < pattern.Count; i++)
        {
            BirdGesture opp = pattern[i];
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
}

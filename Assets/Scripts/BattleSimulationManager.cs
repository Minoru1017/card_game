using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>中央回合提示 UI：隱藏、我方回合、敵方回合。</summary>
public enum BattleTurnBannerKind
{
    Hidden,
    PlayerTurn,
    EnemyTurn
}

public class BattleSimulationManager : MonoBehaviour
{
    // MASTER INDEX (merged legacy + 2026-04-15 updates)
    // 1) Battle core (legacy):
    //    - Deck build/shuffle, opening presentation, turn sequencing, win/lose checks.
    //    - Player/enemy action gates and spell/attack resolve flow.
    // 2) Difficulty/runtime config (legacy):
    //    - Runtime difficulty queue/apply and enemy deck constraints.
    // 3) Weather naming + rotation:
    //    - GetWeatherLabel()
    //    - GetRotatingWeatherBySerial()
    //    - GetFirstWeatherOverrideIfAny()
    // 4) Weather phase flow:
    //    - CoPresentWeatherForecastForTurn()
    //    - TryEnterWeatherPhaseForCurrentRound()
    // 5) Weather rule effects:
    //    - ApplyWeatherSpellPowerBonus()
    //    - ApplyFogDirectDamageReductionIfNeeded()
    //    - ApplyHolyLightHealBonusIfNeeded()
    //    - ApplyFireRainEndTurnEffect()
    // 6) Weather UI text export:
    //    - GetCurrentWeatherForecastDetailsText()
    //    - GetWeatherPseudoCardText()
    //    - GetCurrentWeatherLabelForUi()
    //    - GetCurrentWeatherRemainingRoundsForUi()
    //    - GetNextWeatherForecastHintForUi()
    //    - GetActiveWeatherBoardEffectTextForUi()

    private enum BattleWeatherType
    {
        None,
        FireRain,
        HolyLight,
        Fog,
        Gale
    }

    public struct AttackVisualData
    {
        public bool attackerIsPlayer;
        public bool hasMonsterTarget;
        public int attackerDamage;
        public bool counterTriggered;
        public int counterDamage;
    }

    public event System.Action<Card> EnemyCardPlayed;
    public event System.Action<Card> EnemyCardDiscarded;
    public event System.Action<Card> PlayerCardDiscarded;
    public event System.Action<AttackVisualData> AttackPerformed;
    public event System.Action<bool, Card> CardDrawn; // (isPlayer, card)
    [Header("Refs")]
    [Tooltip("可拖入 DataManager 的 Prefab 或場景實例。若為 Prefab 資產，進入 Play 時會自動 Instantiate 到本物件底下。")]
    public GameObject dataManager;
    public PlayerData playerData;
    public CardStore cardStore;
    public EnemyAI enemyAI;
    public GameObject battleCardPrefab;
    public GameObject fieldMonsterPrefab;

    public const float HandCardScaleBaseline = BattleCardLayoutTuning.HandCardScaleBaseline;
    public const float HandCardSizeMultiplierMax = BattleCardLayoutTuning.HandCardSizeMultiplierMax;
    public const float FieldCardScaleBaseline = BattleFieldCardTuning.FieldMonsterScaleBaseline;
    public const float FieldCardSizeMultiplierMax = BattleFieldCardTuning.FieldCardSizeMultiplierMax;

    [Header("手牌版面（大小／間距／手牌區位置）")]
    [Tooltip("僅手牌。詳見 BattleCardLayoutTuning.cs")]
    [SerializeField] private BattleCardLayoutTuning cardLayout = new BattleCardLayoutTuning();

    [Header("手牌文字（字級縮放）")]
    [Tooltip("僅手牌卡面文字。詳見 BattleCardTextTuning.cs")]
    [SerializeField] private BattleCardTextTuning cardText = new BattleCardTextTuning();

    [Header("場上卡牌（怪獸／法術／間距／攻血字）")]
    [Tooltip("場上怪獸與法術牌、槽位、ATK／HP。詳見 BattleFieldCardTuning.cs")]
    [SerializeField] private BattleFieldCardTuning cardField = new BattleFieldCardTuning();

    public BattleCardLayoutTuning CardLayout => cardLayout;
    public BattleCardTextTuning CardText => cardText;
    public BattleFieldCardTuning CardField => cardField;

    /// <summary>將調校快照寫入執行中的 cardLayout／cardText／cardField。</summary>
    public void ApplyCardTuning(
        BattleCardLayoutTuning layout,
        BattleCardTextTuning text,
        BattleFieldCardTuning field)
    {
        if (layout != null) BattleCardTuningCopy.Copy(cardLayout, layout);
        if (text != null) BattleCardTuningCopy.Copy(cardText, text);
        if (field != null) BattleCardTuningCopy.Copy(cardField, field);
    }

    /// <summary>從 Resources 預設（如 <see cref="BattleCardTuningPresetLibrary.Preset1Id"/>）載入並套用。</summary>
    public bool TryApplyCardTuningPreset(string presetId) =>
        BattleCardTuningPresetLibrary.TryApplyPreset(this, presetId);

    public float HandCardScale => cardLayout.HandCardScale;
    public float FieldMonsterScale => cardField.FieldMonsterScale;
    public float FieldSpellScale => cardField.FieldSpellScale;
    public float HandAreaAnchoredYCanPlay => cardLayout.handAreaAnchoredYCanPlay;
    public float EnemyHandAreaAnchoredYCanPlay => cardLayout.enemyHandAreaAnchoredYCanPlay;
    public float HandAreaAnchoredYCantPlay => cardLayout.handAreaAnchoredYCantPlay;
    public float EnemyHandAreaAnchoredYCantPlay => cardLayout.enemyHandAreaAnchoredYCantPlay;

    public float handCardSpacing
    {
        get => cardLayout.handCardSpacing;
        set => cardLayout.handCardSpacing = value;
    }

    public float handCardTextScale
    {
        get => cardText.handCardTextScale;
        set => cardText.handCardTextScale = value;
    }

    public float handCardNameScale
    {
        get => cardText.handCardNameScale;
        set => cardText.handCardNameScale = value;
    }

    public float handCardBackplateScale
    {
        get => cardText.handCardBackplateScale;
        set => cardText.handCardBackplateScale = value;
    }

    public float fieldMonsterStatTextScale
    {
        get => cardField.fieldAttackHealthTextScale;
        set => cardField.fieldAttackHealthTextScale = value;
    }

    [Header("Battle Settings")]
    [Tooltip("我方／敵方英雄起始生命")]
    public int startHealth = 20;
    public int startHandCount = 7;
    public int maxHandSize = 7;
    public int enemyDeckSize = 20;
    public bool useFixedEnemyDeck = true;
    public int[] fixedEnemyDeckCardIds = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 19, 22, -1, -2, -3 };
    private static readonly int[] BossFixedEnemyDeckCardIds =
    {
        13, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 19, 22, -1, -2, -3
    };
    public bool forceEnemyStartWithMonster = false;

    [Header("Enemy deck spell mix")]
    [Tooltip("When building a random enemy deck (non-fixed pool), ensure at least this many spell cards if any exist in CardStore.")]
    [Range(0, 20)]
    [SerializeField] private int minEnemySpellsInDeck = 2;

    [Header("Win-rate balance (50–70% target)")]
    [Tooltip("All battles: enemy may keep this many monsters above your deck max ATK/HP before replacement. Higher = stronger enemy (tune ~8–16).")]
    [Range(0, 30)]
    [FormerlySerializedAs("batchSimEnemyOverLimitAllowance")]
    [SerializeField] private int enemyOverLimitAllowance = 6;
    [Tooltip("All battles: if true, skip injecting weak 0 ATK or 0 HP filler monsters when the deck rule would add them.")]
    [FormerlySerializedAs("batchSimSkipZeroStatEnemyFiller")]
    [SerializeField] private bool skipZeroStatEnemyFiller = true;
    [Tooltip("Batch sim auto-player only: chance to play a spell before summoning when field is empty. Tune with enemy allowance for ~50–70% win rate.")]
    [Range(0f, 0.5f)]
    [FormerlySerializedAs("batchSimPlayerSpellFirstChance")]
    [SerializeField] private float autoSimPlayerSpellFirstChance = 0.22f;

    private bool runtimeDifficultyConfigPending;
    private bool runtimeUseFixedEnemyDeck;
    private int[] runtimeFixedEnemyDeckCardIds;
    private int runtimeEnemyOverLimitAllowance;
    private int runtimeMinEnemySpellsInDeck;
    private EnemyAiPlayStyle runtimeEnemyAiPlayStyle = EnemyAiPlayStyle.Greedy;
    private string runtimeDifficultyLabelZh;
    private bool runtimeDifficultyLabelExplicit;
    private bool lastBattleEndedBySurrender;

    public bool LastBattleEndedBySurrender => lastBattleEndedBySurrender;

    public string CurrentBattleDifficultyLabelZh => GetBattleDifficultyLabelForRecord();

    public string GetBattleDifficultyLabelForRecord()
    {
        string active = BattleLaunchContext.GetActiveBattleDifficultyLabelZh();
        if (!string.IsNullOrWhiteSpace(active))
            return active.Trim();
        return ResolveDifficultyLabelForCapture();
    }

    public void CaptureBattleDifficultyForRecords()
    {
        ApplyLaunchContextDifficulty();
        string label = ResolveDifficultyLabelForCapture();
        runtimeDifficultyLabelZh = label;
        runtimeDifficultyLabelExplicit = true;
        BattleDifficultyRuntime.SetCurrentLabelZh(label);
        BattleLaunchContext.ConfirmActiveBattleDifficulty(label);
    }

    private string ResolveDifficultyLabelForCapture()
    {
        if (runtimeDifficultyLabelExplicit && !string.IsNullOrWhiteSpace(runtimeDifficultyLabelZh))
            return runtimeDifficultyLabelZh.Trim();
        if (runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss)
            return "魔王";
        if (runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingHard)
            return "困難";
        string pending = BattleLaunchContext.PeekDifficultyLabelZh();
        if (!string.IsNullOrWhiteSpace(pending))
            return pending.Trim();
        return BattleDifficultyRuntime.CurrentLabelZh;
    }

    public void ApplyLaunchContextDifficulty()
    {
        if (runtimeDifficultyLabelExplicit)
            return;

        string pending = BattleLaunchContext.PeekDifficultyLabelZh();
        if (string.IsNullOrWhiteSpace(pending))
            return;
        runtimeDifficultyLabelZh = pending.Trim();
        runtimeDifficultyLabelExplicit = true;
        BattleDifficultyRuntime.SetCurrentLabelZh(runtimeDifficultyLabelZh);
    }

    /// <summary>When battle scene loads without SceneLoader fixup, still apply boss/hard AI from launch context.</summary>
    public void TryApplyLaunchDifficultyFromContext()
    {
        if (runtimeDifficultyConfigPending)
            return;

        if (BattleLaunchContext.IsIntroTutorialBattle)
        {
            SceneLoader.ApplyIntroTutorialRuntimeConfigToManager(this);
            return;
        }

        if (BattleLaunchContext.IsHarborTrainingGroundBattle)
        {
            SceneLoader.ApplyHarborTrainingRuntimeConfigToManager(this);
            return;
        }

        string label = BattleLaunchContext.ResolveForBattleRecord();
        if (string.IsNullOrWhiteSpace(label))
            label = BattleDifficultyRuntime.CurrentLabelZh;
        label = string.IsNullOrWhiteSpace(label) ? BattleDifficultyRuntime.DefaultLabelZh : label.Trim();

        EnemyAiPlayStyle style = EnemyAiPlayStyle.Greedy;
        if (label.StartsWith("魔王", System.StringComparison.Ordinal) ||
            string.Equals(label, "Boss", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label, "BOSS", System.StringComparison.Ordinal))
            style = EnemyAiPlayStyle.SchemingBoss;
        else if (label.StartsWith("困難", System.StringComparison.Ordinal) ||
                 string.Equals(label, "Hard", System.StringComparison.OrdinalIgnoreCase))
            style = EnemyAiPlayStyle.SchemingHard;

        if (style == EnemyAiPlayStyle.Greedy && runtimeEnemyAiPlayStyle != EnemyAiPlayStyle.Greedy)
            return;

        if (style == EnemyAiPlayStyle.SchemingBoss)
        {
            QueueRuntimeDifficultyConfig(
                true,
                BossFixedEnemyDeckCardIds,
                enemyOverLimitAllowance > 0 ? enemyOverLimitAllowance : 12,
                minEnemySpellsInDeck > 0 ? minEnemySpellsInDeck : 4,
                EnemyAiPlayStyle.SchemingBoss,
                "魔王");
            return;
        }

        if (style == EnemyAiPlayStyle.SchemingHard)
        {
            QueueRuntimeDifficultyConfig(
                useFixedEnemyDeck,
                fixedEnemyDeckCardIds,
                enemyOverLimitAllowance > 0 ? enemyOverLimitAllowance : 8,
                minEnemySpellsInDeck > 0 ? minEnemySpellsInDeck : 3,
                EnemyAiPlayStyle.SchemingHard,
                "困難");
        }
    }

    private int enemySchemingHoldStreak;
    private BattleWeatherType currentWeather = BattleWeatherType.None;
    private int weatherTurnSerial;
    private bool playerFirstSpellBoostAvailable;
    private bool enemyFirstSpellBoostAvailable;
    private bool weatherForecastInProgress;
    [SerializeField] private float weatherForecastPresentationSeconds = 5f;
    [SerializeField] private int weatherActiveDurationRounds = 3;
    [SerializeField] private int weatherForecastCooldownRounds = 3;
    [Header("Weather First Trigger Override")]
    [Tooltip("勾選後，第一輪天氣觸發將優先套用「爐心飛燼」。若同時勾選多個，依序取第一個：爐心飛燼 > 暖燈浮塵 > 訓練薄霧 > 穿堂微風。")]
    [SerializeField] private bool firstWeatherPreferFireRain;
    [Tooltip("勾選後，第一輪天氣觸發將優先套用「暖燈浮塵」。")]
    [SerializeField] private bool firstWeatherPreferHolyLight;
    [Tooltip("勾選後，第一輪天氣觸發將優先套用「訓練薄霧」。")]
    [SerializeField] private bool firstWeatherPreferFog;
    [Tooltip("勾選後，第一輪天氣觸發將優先套用「穿堂微風」。")]
    [SerializeField] private bool firstWeatherPreferGale;
    private bool firstWeatherOverrideConsumed;
    private int weatherActiveRoundsRemaining;
    private int weatherCooldownRoundsRemaining;
    private int weatherRemainingRoundsForUi;
    private BattleWeatherType queuedWeatherForNextRound = BattleWeatherType.None;
    private BattleWeatherType forecastPreviewWeatherForUi = BattleWeatherType.None;
    private readonly List<BattleWeatherType> weatherRandomCycleBag = new List<BattleWeatherType>(4);
    private int weatherRandomCycleCursor;

    /// <summary>入門級教學戰不啟用天氣（預報、持續效果、全屏演出）。</summary>
    public bool IsWeatherSystemEnabledForBattle()
    {
        if (BattleLaunchContext.IsIntroTutorialBattle)
            return false;

        return runtimeEnemyAiPlayStyle != EnemyAiPlayStyle.IntroGreedy;
    }

    private void ResetWeatherStateToInactive()
    {
        currentWeather = BattleWeatherType.None;
        forecastPreviewWeatherForUi = BattleWeatherType.None;
        queuedWeatherForNextRound = BattleWeatherType.None;
        weatherActiveRoundsRemaining = 0;
        weatherCooldownRoundsRemaining = 0;
        weatherRemainingRoundsForUi = 0;
        playerFirstSpellBoostAvailable = false;
        enemyFirstSpellBoostAvailable = false;
    }

    /// <summary>Batch auto-play only: spell-before-monster chance when field is empty.</summary>
    public float AutoSimPlayerSpellFirstChance => autoSimPlayerSpellFirstChance;

    [Header("Debug")]
    public bool autoStartOnPlay = true;
    [Header("Combat Animation")]
    public float attackMotionDuration = 0.4f;
    public float hitShakeDuration = 0.28f;
    public float hitShakeStrength = 12f;
    public float counterAttackGapDuration = 0.45f;
    public float attackDelayAfterEndTurn = 1.0f;
    [Tooltip("我方／敵方打出法術牌時，全屏技能介紹演出秒數（結束後才結算法術效果）。")]
    [SerializeField] private float spellCastPresentationSeconds = 5f;

    /// <summary>全屏法術介紹 UI：bool 為是否我方。</summary>
    public event System.Action<bool, string, string> SpellCastPresentationStarted;
    /// <summary>我方法術異步演出結束（供手牌出牌動畫銜接）。</summary>
    public event System.Action SpellCastAsyncPresentationFinished;
    /// <summary>法術結算或需重刷場上／手牌 UI 時（避免簽名溢位等漏更新）。</summary>
    public event System.Action BattleLayoutVisualRefreshRequested;
    /// <summary>初級治療成功結算後，供場上怪獸卡播放綠色回復特效（UI）。</summary>
    public event System.Action PlayerLesserHealVisualRequested;
    /// <summary>敵方初級治療成功結算後，供敵方場上怪獸卡播放綠色回復特效（UI）。</summary>
    public event System.Action EnemyLesserHealVisualRequested;
    /// <summary>打出法術並從手牌移除前：供 UI 記錄該手牌槽位（如火球起點）。isPlayer、手牌索引。</summary>
    public event System.Action<bool, SpellCard, int> SpellCastHandAnchorCommitted;
    /// <summary>火球術結算後：施法者是否為我方；當次是否為攻擊對方場上怪（否則為直擊英雄）。</summary>
    public event System.Action<bool, bool> FireballVisualRequested;
    /// <summary>林可的凝視每回合全體 -5 觸發時；true=我方場上凝視牌，false=敵方場上凝視牌（供眼睛攻擊特效）。</summary>
    public event System.Action<bool> LinGazePeriodicStrikeVisualRequested;
    /// <summary>天氣全域預報開始：天氣名稱、效果敘述。</summary>
    public event System.Action<string, string> WeatherForecastStarted;
    /// <summary>天氣全域預報結束（回合恢復）。</summary>
    public event System.Action WeatherForecastFinished;
    /// <summary>畫面中央回合浮動提示（略過批次勝率模擬時的顯示，結束戰鬥時仍會隱藏）。</summary>
    public event System.Action<BattleTurnBannerKind> TurnBannerRequested;
    /// <summary>玩家將手牌成功放到場上（怪獸上場，或林可的凝視結算後置於場上）。</summary>
    public event System.Action PlayerCommittedHandCardToFieldFromHand;
    /// <summary>玩家按下結束回合（取消「你的回合」浮窗計時）。</summary>
    public event System.Action PlayerPressedEndTurnForPromptUi;
    /// <summary>我方回合可操作視窗開始（開場骰子結束後先手，或敵方回合結束並抽牌後）；供「你的回合」閒置計時起點。</summary>
    public event System.Action PlayerTurnActionWindowOpenedForPromptUi;
    /// <summary>對戰結束（勝利 1、戰敗 -1、平手 2）；供結算 UI 事件驅動更新。</summary>
    public event System.Action<int> BattleEnded;
    /// <summary>規則阻斷訊息變更（非空時結算區顯示提示而非結算面板）。</summary>
    public event System.Action<string> BattleRuleMessageChanged;

    private bool deferEnemyFieldUiClearAfterPlayerFireballKill;
    private bool deferPlayerFieldUiClearAfterEnemyFireballKill;

    public bool PeekDeferEnemyFieldUiClearAfterPlayerFireballKill() => deferEnemyFieldUiClearAfterPlayerFireballKill;

    public void ClearDeferEnemyFieldUiClearAfterPlayerFireballKill()
    {
        deferEnemyFieldUiClearAfterPlayerFireballKill = false;
    }

    public bool PeekDeferPlayerFieldUiClearAfterEnemyFireballKill() => deferPlayerFieldUiClearAfterEnemyFireballKill;

    public void ClearDeferPlayerFieldUiClearAfterEnemyFireballKill()
    {
        deferPlayerFieldUiClearAfterEnemyFireballKill = false;
    }

    public void ResetPendingFireballFieldUiDefers()
    {
        deferEnemyFieldUiClearAfterPlayerFireballKill = false;
        deferPlayerFieldUiClearAfterEnemyFireballKill = false;
    }

    private void NotifyTurnBanner(BattleTurnBannerKind kind)
    {
        if (kind != BattleTurnBannerKind.Hidden && BattleAutoSimPlugin.IsRunning)
            return;
        TurnBannerRequested?.Invoke(kind);
    }

    private void NotifyPlayerCommittedHandCardToFieldFromHandForUi()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        PlayerCommittedHandCardToFieldFromHand?.Invoke();
    }

    private void NotifyPlayerPressedEndTurnForPromptUi()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        PlayerPressedEndTurnForPromptUi?.Invoke();
    }

    private void NotifyPlayerTurnActionWindowOpenedForPromptUi()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        PlayerTurnActionWindowOpenedForPromptUi?.Invoke();
    }

    private string GetWeatherLabel(BattleWeatherType wt)
    {
        switch (wt)
        {
            case BattleWeatherType.FireRain: return BattleWeatherLabels.EmberHearth;
            case BattleWeatherType.HolyLight: return BattleWeatherLabels.WarmLamplight;
            case BattleWeatherType.Fog: return BattleWeatherLabels.TrainingMist;
            case BattleWeatherType.Gale: return BattleWeatherLabels.HallDraft;
            default: return "無";
        }
    }

    private BattleWeatherType GetRotatingWeatherBySerial(int serial)
    {
        if (serial <= 0) return BattleWeatherType.None;
        int r = (serial - 1) % 4;
        switch (r)
        {
            case 0: return BattleWeatherType.FireRain;
            case 1: return BattleWeatherType.HolyLight;
            case 2: return BattleWeatherType.Fog;
            default: return BattleWeatherType.Gale;
        }
    }

    private BattleWeatherType GetFirstWeatherOverrideIfAny()
    {
        if (firstWeatherPreferFireRain) return BattleWeatherType.FireRain;
        if (firstWeatherPreferHolyLight) return BattleWeatherType.HolyLight;
        if (firstWeatherPreferFog) return BattleWeatherType.Fog;
        if (firstWeatherPreferGale) return BattleWeatherType.Gale;
        return BattleWeatherType.None;
    }

    private void RefillWeatherRandomCycleBag(BattleWeatherType exclude = BattleWeatherType.None)
    {
        weatherRandomCycleBag.Clear();
        weatherRandomCycleCursor = 0;
        if (exclude != BattleWeatherType.FireRain) weatherRandomCycleBag.Add(BattleWeatherType.FireRain);
        if (exclude != BattleWeatherType.HolyLight) weatherRandomCycleBag.Add(BattleWeatherType.HolyLight);
        if (exclude != BattleWeatherType.Fog) weatherRandomCycleBag.Add(BattleWeatherType.Fog);
        if (exclude != BattleWeatherType.Gale) weatherRandomCycleBag.Add(BattleWeatherType.Gale);
        for (int i = weatherRandomCycleBag.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            BattleWeatherType t = weatherRandomCycleBag[i];
            weatherRandomCycleBag[i] = weatherRandomCycleBag[j];
            weatherRandomCycleBag[j] = t;
        }
    }

    private BattleWeatherType DrawNextWeatherRandomNoRepeat()
    {
        if (weatherRandomCycleCursor >= weatherRandomCycleBag.Count)
            RefillWeatherRandomCycleBag();
        if (weatherRandomCycleBag.Count <= 0) return BattleWeatherType.FireRain;
        BattleWeatherType picked = weatherRandomCycleBag[Mathf.Clamp(weatherRandomCycleCursor, 0, weatherRandomCycleBag.Count - 1)];
        weatherRandomCycleCursor++;
        return picked;
    }

    private BattleWeatherType PickForecastWeatherForNextRound()
    {
        BattleWeatherType overridden = BattleWeatherType.None;
        if (!firstWeatherOverrideConsumed && weatherTurnSerial == 0)
        {
            overridden = GetFirstWeatherOverrideIfAny();
            firstWeatherOverrideConsumed = true;
        }
        if (overridden != BattleWeatherType.None)
        {
            // First-trigger override counts as one draw in the current cycle.
            RefillWeatherRandomCycleBag(overridden);
            return overridden;
        }
        return DrawNextWeatherRandomNoRepeat();
    }

    private string GetWeatherForecastDetailsText(BattleWeatherType wt)
    {
        string ComposeForecastDetail(string benefit, string drawback, string impacted)
        {
            bool hasBenefit = !string.IsNullOrEmpty(benefit);
            bool hasDrawback = !string.IsNullOrEmpty(drawback);
            if (hasBenefit && hasDrawback)
                return "增益: " + benefit + "\n減益: " + drawback + "\n受影響卡牌: " + impacted;
            if (hasBenefit)
                return "增益: " + benefit + "\n受影響卡牌: " + impacted;
            if (hasDrawback)
                return "減益: " + drawback + "\n受影響卡牌: " + impacted;
            return "受影響卡牌: " + impacted;
        }

        switch (wt)
        {
            case BattleWeatherType.FireRain:
                return ComposeForecastDetail(
                    string.Empty,
                    "雙方場上怪獸於回合結束各受 5 點傷害",
                    "高血量怪獸較有優勢");
            case BattleWeatherType.HolyLight:
                return ComposeForecastDetail(
                    "所有治療效果增加 10",
                    string.Empty,
                    "治療類法術與續航型怪獸更有利");
            case BattleWeatherType.Fog:
                return ComposeForecastDetail(
                    "防守方生存壓力下降",
                    "直接攻擊英雄傷害減少 50%",
                    "直傷與快攻終結效率下降");
            case BattleWeatherType.Gale:
                return ComposeForecastDetail(
                    "雙方本回合首張法術效果增加 20%",
                    string.Empty,
                    "爆發型與功能型法術價值提升");
            default:
                return "本回合無天氣加成與減益";
        }
    }

    private void PrepareWeatherEffectFlagsForCurrentRound()
    {
        playerFirstSpellBoostAvailable = false;
        enemyFirstSpellBoostAvailable = false;
        if (currentWeather == BattleWeatherType.Gale)
        {
            // 狂風：本回合雙方第一張法術都可享受 20% 強化。
            playerFirstSpellBoostAvailable = true;
            enemyFirstSpellBoostAvailable = true;
        }

    }

    private string GetCurrentWeatherEffectText()
    {
        switch (currentWeather)
        {
            case BattleWeatherType.FireRain: return "回合結束：雙方場上怪獸各受 5 點傷害。";
            case BattleWeatherType.HolyLight: return "本回合所有治療 +10。";
            case BattleWeatherType.Fog: return "本回合直接攻擊英雄傷害 -50%。";
            case BattleWeatherType.Gale: return "本回合首張法術效果 +20%。";
            default: return "本回合無額外天氣效果。";
        }
    }

    private IEnumerator CoPresentWeatherForecastForTurn(bool isPlayerTurnNow)
    {
        bool forecastTriggeredThisRound = TryEnterWeatherPhaseForCurrentRound();
        BattleLayoutVisualRefreshRequested?.Invoke();
        if (!forecastTriggeredThisRound) yield break;

        BattleWeatherType forecastType = forecastPreviewWeatherForUi != BattleWeatherType.None ? forecastPreviewWeatherForUi : currentWeather;
        string weatherName = GetWeatherLabel(forecastType);
        string effectText = GetWeatherForecastDetailsText(forecastType);
        WeatherForecastStarted?.Invoke(weatherName, effectText);
        weatherForecastInProgress = true;

        string owner = isPlayerTurnNow ? "我方" : "敵方";
        ShowBattleToast("天氣預報: " + weatherName + " | " + owner + "回合", 2.4f);
        LogBattleHistory("天氣預報: " + weatherName + " | 效果: " + effectText);

        if (!BattleAutoSimPlugin.IsRunning)
        {
            float wait = Mathf.Clamp(weatherForecastPresentationSeconds, 0.2f, 8f);
            yield return new WaitForSecondsRealtime(wait);
        }

        weatherForecastInProgress = false;
        WeatherForecastFinished?.Invoke();
    }

    private bool TryEnterWeatherPhaseForCurrentRound()
    {
        weatherRemainingRoundsForUi = 0;

        if (!IsWeatherSystemEnabledForBattle())
        {
            ResetWeatherStateToInactive();
            PrepareWeatherEffectFlagsForCurrentRound();
            return false;
        }

        // 初始回合（無論先後手）不觸發天氣預報。
        if (currentRound <= 1)
        {
            currentWeather = BattleWeatherType.None;
            forecastPreviewWeatherForUi = BattleWeatherType.None;
            PrepareWeatherEffectFlagsForCurrentRound();
            return false;
        }

        // 天氣作用中：維持效果，但只在啟動當回合播報視窗。
        if (weatherActiveRoundsRemaining > 0)
        {
            BattleWeatherType activeWeather = currentWeather;
            forecastPreviewWeatherForUi = BattleWeatherType.None;
            weatherRemainingRoundsForUi = weatherActiveRoundsRemaining;
            PrepareWeatherEffectFlagsForCurrentRound();
            weatherActiveRoundsRemaining--;
            if (weatherActiveRoundsRemaining <= 0)
            {
                weatherCooldownRoundsRemaining = Mathf.Max(0, weatherForecastCooldownRounds);
                string weatherName = GetWeatherLabel(activeWeather);
                if (!string.IsNullOrEmpty(weatherName) && weatherName != "無")
                    LogBattleHistory("天氣結算: " + weatherName + " 效果結束");
            }
            return false;
        }

        // Resolve the queued weather chosen by forecast on previous no-weather round.
        if (queuedWeatherForNextRound != BattleWeatherType.None)
        {
            currentWeather = queuedWeatherForNextRound;
            queuedWeatherForNextRound = BattleWeatherType.None;
            forecastPreviewWeatherForUi = BattleWeatherType.None;
            weatherTurnSerial++;
            weatherActiveRoundsRemaining = Mathf.Max(1, weatherActiveDurationRounds);
            weatherRemainingRoundsForUi = weatherActiveRoundsRemaining;
            PrepareWeatherEffectFlagsForCurrentRound();
            weatherActiveRoundsRemaining--;
            if (weatherActiveRoundsRemaining <= 0)
                weatherCooldownRoundsRemaining = Mathf.Max(0, weatherForecastCooldownRounds);
            return false;
        }

        // No active weather now; this is the forecast round that picks next round's weather.
        currentWeather = BattleWeatherType.None;
        PrepareWeatherEffectFlagsForCurrentRound();
        if (weatherCooldownRoundsRemaining > 1)
        {
            weatherCooldownRoundsRemaining--;
            forecastPreviewWeatherForUi = BattleWeatherType.None;
            return false;
        }
        if (weatherCooldownRoundsRemaining == 1)
            weatherCooldownRoundsRemaining = 0;

        BattleWeatherType picked = PickForecastWeatherForNextRound();
        queuedWeatherForNextRound = picked;
        forecastPreviewWeatherForUi = picked;
        PrepareWeatherEffectFlagsForCurrentRound();
        return queuedWeatherForNextRound != BattleWeatherType.None;
    }

    private string GetCurrentWeatherForecastDetailsText()
    {
        return GetWeatherForecastDetailsText(currentWeather);
    }

    private string GetWeatherPseudoCardText(bool forPlayerSide)
    {
        switch (currentWeather)
        {
            case BattleWeatherType.FireRain:
                return "【場地偽卡】" + BattleWeatherLabels.EmberHearth + "：回合結束時，場上怪獸 -5 HP";
            case BattleWeatherType.HolyLight:
                return "【場地偽卡】" + BattleWeatherLabels.WarmLamplight + "：本回合治療 +10";
            case BattleWeatherType.Fog:
                return "【場地偽卡】" + BattleWeatherLabels.TrainingMist + "：本回合直擊英雄傷害 -50%";
            case BattleWeatherType.Gale:
                bool hasBuff = forPlayerSide ? playerFirstSpellBoostAvailable : enemyFirstSpellBoostAvailable;
                return hasBuff
                    ? "【場地偽卡】" + BattleWeatherLabels.HallDraft + "：本回合首張法術效果 +20%（未觸發）"
                    : "【場地偽卡】" + BattleWeatherLabels.HallDraft + "：本回合首張法術效果 +20%（已觸發）";
            default:
                return "【天氣偽卡】無";
        }
    }

    private int ApplyWeatherSpellPowerBonus(int baseValue, bool isPlayerCaster)
    {
        if (!IsWeatherSystemEnabledForBattle()) return baseValue;
        int v = baseValue;
        if (currentWeather == BattleWeatherType.Gale)
        {
            bool consume = isPlayerCaster ? playerFirstSpellBoostAvailable : enemyFirstSpellBoostAvailable;
            if (consume)
            {
                v = Mathf.CeilToInt(baseValue * 1.2f);
                if (isPlayerCaster) playerFirstSpellBoostAvailable = false;
                else enemyFirstSpellBoostAvailable = false;
            }
        }
        return v;
    }

    private int ApplyFogDirectDamageReductionIfNeeded(int directDamage)
    {
        if (!IsWeatherSystemEnabledForBattle()) return directDamage;
        if (currentWeather != BattleWeatherType.Fog) return directDamage;
        return Mathf.Max(0, Mathf.CeilToInt(directDamage * 0.5f));
    }

    private void ResetMonsterSkillBattleState()
    {
        playerKingTrainingCharges = 3;
        enemyKingTrainingCharges = 3;
        playerQueenShelterUsed = false;
        enemyQueenShelterUsed = false;
        playerMilitiaFormationUsed = false;
        enemyMilitiaFormationUsed = false;
        playerKingWasOnFieldThisBattle = false;
        enemyKingWasOnFieldThisBattle = false;
    }

    private void ApplySummonMonsterSkills(BattleMonster field, bool isPlayer)
    {
        NoteKingSummonedThisBattle(field, isPlayer);
        if (field == null || field.id != MonsterSkillIds.Militia) return;
        if (isPlayer)
        {
            if (playerMilitiaFormationUsed) return;
            playerMilitiaFormationUsed = true;
        }
        else
        {
            if (enemyMilitiaFormationUsed) return;
            enemyMilitiaFormationUsed = true;
        }
        field.attack += 5;
        string side = isPlayer ? "我方" : "敵方";
        LogBattleHistory(side + " 列陣：這次攻擊力多 5 點 本局僅1次");
        ShowBattleToast(side + "民兵·列陣：攻擊力多 5 點", 2.2f);
    }

    private void NoteKingSummonedThisBattle(BattleMonster field, bool isPlayer)
    {
        if (field == null || field.id != MonsterSkillIds.King) return;
        if (isPlayer) playerKingWasOnFieldThisBattle = true;
        else enemyKingWasOnFieldThisBattle = true;
    }

    private int ModifyDamageToPlayerMonster(int rawDamage)
    {
        if (playerField == null || rawDamage <= 0) return rawDamage;
        int dmg = rawDamage;
        if (playerField.id == MonsterSkillIds.Queen)
            dmg = MonsterSkillRegistry.ApplyQueenShelter(ref playerQueenShelterUsed, dmg, LogBattleHistory);
        if (playerField.id == MonsterSkillIds.King)
            dmg = MonsterSkillRegistry.ApplyTrainingCourtDecree(ref playerKingTrainingCharges, dmg, LogBattleHistory);
        return dmg;
    }

    private int ModifyDamageToEnemyMonster(int rawDamage)
    {
        if (enemyField == null || rawDamage <= 0) return rawDamage;
        int dmg = rawDamage;
        if (enemyField.id == MonsterSkillIds.Queen)
            dmg = MonsterSkillRegistry.ApplyQueenShelter(ref enemyQueenShelterUsed, dmg, LogBattleHistory);
        if (enemyField.id == MonsterSkillIds.King)
            dmg = MonsterSkillRegistry.ApplyTrainingCourtDecree(ref enemyKingTrainingCharges, dmg, LogBattleHistory);
        return dmg;
    }

    private int ModifyDirectDamageToPlayerHero(int rawDamage)
    {
        int dmg = ApplyFogDirectDamageReductionIfNeeded(rawDamage);
        if (playerKingWasOnFieldThisBattle && playerKingTrainingCharges > 0 &&
            (playerField == null || playerField.id == MonsterSkillIds.King))
            dmg = MonsterSkillRegistry.ApplyTrainingCourtDecree(ref playerKingTrainingCharges, dmg, LogBattleHistory);
        return dmg;
    }

    private int ModifyDirectDamageToEnemyHero(int rawDamage)
    {
        int dmg = ApplyFogDirectDamageReductionIfNeeded(rawDamage);
        if (enemyKingWasOnFieldThisBattle && enemyKingTrainingCharges > 0 &&
            (enemyField == null || enemyField.id == MonsterSkillIds.King))
            dmg = MonsterSkillRegistry.ApplyTrainingCourtDecree(ref enemyKingTrainingCharges, dmg, LogBattleHistory);
        return dmg;
    }

    private int ApplyHolyLightHealBonusIfNeeded(int baseHeal)
    {
        if (!IsWeatherSystemEnabledForBattle()) return baseHeal;
        if (currentWeather != BattleWeatherType.HolyLight) return baseHeal;
        return baseHeal + 10;
    }

    private void ApplyFireRainEndTurnEffect()
    {
        if (!IsWeatherSystemEnabledForBattle()) return;
        if (currentWeather != BattleWeatherType.FireRain) return;
        const int dot = 5;
        bool any = false;
        bool hitPlayerMonster = false;
        bool hitEnemyMonster = false;
        if (playerField != null)
        {
            hitPlayerMonster = true;
            int playerDot = ModifyDamageToPlayerMonster(dot);
            playerField.currentHp -= playerDot;
            if (playerField.currentHp <= 0) playerField = null;
            any = true;
        }
        if (enemyField != null)
        {
            hitEnemyMonster = true;
            int enemyDot = ModifyDamageToEnemyMonster(dot);
            enemyField.currentHp -= enemyDot;
            if (enemyField.currentHp <= 0) enemyField = null;
            any = true;
        }
        if (!any) return;

        string txt = BattleWeatherLabels.EmberHearth + "：回合結束，雙方場上怪獸各受 5 點傷害。";
        ShowBattleToast(txt, 2.4f);
        LogBattleHistory(txt);
        if (hitPlayerMonster && hitEnemyMonster)
            LogBattleHistory("天氣結算：我方與敵方場上怪獸各 -5 HP（英雄不受此效果影響）。");
        else if (hitPlayerMonster)
            LogBattleHistory("天氣結算：我方場上怪獸 -5 HP（英雄不受此效果影響）。");
        else if (hitEnemyMonster)
            LogBattleHistory("天氣結算：敵方場上怪獸 -5 HP（英雄不受此效果影響）。");
        BattleLayoutVisualRefreshRequested?.Invoke();
    }

    private readonly List<string> battleHistoryLines = new List<string>(128);
    private readonly List<BattleHistoryEntry> battleHistoryEntries = new List<BattleHistoryEntry>(128);
    private int battleHistorySequence;
    private readonly List<string> pendingDiscardHistoryLines = new List<string>(16);
    private Coroutine discardHistoryFlushRoutine;
    private const float DiscardHistoryFlushDelaySeconds = 0.14f;
    private bool playerHeroDeathLoggedThisBattle;

    /// <summary>新增一則歷史時通知 UI（即時戰報側欄等）。</summary>
    public event Action BattleHistoryChanged;

    public IReadOnlyList<BattleHistoryEntry> BattleHistoryEntries => battleHistoryEntries;

    /// <summary>本局累積的對戰歷史（每行一則，時間序）；批次模擬不寫入。</summary>
    public string GetBattleHistoryFullText()
    {
        if (battleHistoryEntries.Count == 0)
            return "（本局尚無對戰歷史紀錄）";
        var sb = new StringBuilder();
        for (int i = 0; i < battleHistoryEntries.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(battleHistoryEntries[i].Text);
        }
        return sb.ToString();
    }

    /// <summary>由新到舊的歷史事件（供完整戰報 UI）。</summary>
    public List<BattleHistoryEntry> GetBattleHistoryEntriesNewestFirst()
    {
        var list = new List<BattleHistoryEntry>(battleHistoryEntries.Count);
        for (int i = battleHistoryEntries.Count - 1; i >= 0; i--)
            list.Add(battleHistoryEntries[i]);
        return list;
    }

    /// <summary>最近 N 則事件（由新到舊）。</summary>
    public List<BattleHistoryEntry> GetRecentBattleHistoryEntries(int maxCount)
    {
        int take = Mathf.Clamp(maxCount, 1, 32);
        var list = new List<BattleHistoryEntry>(take);
        for (int i = battleHistoryEntries.Count - 1; i >= 0 && list.Count < take; i--)
            list.Add(battleHistoryEntries[i]);
        return list;
    }

    /// <summary>結算戰報摘要（3～5 行）。</summary>
    public string GetBattleHistorySummaryText(int maxLines = 5)
    {
        return BattleHistoryReport.BuildSummary(
            battleHistoryEntries,
            battleResult,
            currentRound,
            maxLines);
    }

    /// <summary>對戰歷史：寫入清單並印 Console；批次勝率模擬時略過以免洗版。</summary>
    private void LogBattleHistory(string message)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (string.IsNullOrEmpty(message)) return;

        if (message.IndexOf('\n') >= 0)
        {
            string[] parts = message.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
                AddBattleHistoryEntry(parts[i]);
            return;
        }

        AddBattleHistoryEntry(message);
    }

    private void AddBattleHistoryEntry(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        string line = message.Trim();
        if (line.Length == 0) return;

        battleHistorySequence++;
        var entry = new BattleHistoryEntry(
            battleHistorySequence,
            currentRound,
            BattleHistoryReport.InferKind(line),
            line,
            BattleHistoryReport.InferIsPlayerPerspective(line));
        battleHistoryEntries.Add(entry);
        battleHistoryLines.Add(line);
        BattleVerbose(line);
        BattleHistoryChanged?.Invoke();
    }

    private void QueueDiscardHistory(string message)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (string.IsNullOrEmpty(message)) return;
        pendingDiscardHistoryLines.Add(message);
        if (discardHistoryFlushRoutine == null)
            discardHistoryFlushRoutine = StartCoroutine(CoFlushDiscardHistoryDelayed());
    }

    private IEnumerator CoFlushDiscardHistoryDelayed()
    {
        yield return new WaitForSecondsRealtime(DiscardHistoryFlushDelaySeconds);
        FlushPendingDiscardHistory();
        discardHistoryFlushRoutine = null;
    }

    private void FlushPendingDiscardHistory()
    {
        if (pendingDiscardHistoryLines.Count <= 0) return;
        LogBattleHistory(string.Join("\n", pendingDiscardHistoryLines));
        pendingDiscardHistoryLines.Clear();
    }

    private void RecordBattleOutcomeHistory()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        switch (battleResult)
        {
            case 1:
                LogBattleHistory("對戰結束");
                LogBattleHistory("我方勝利");
                break;
            case -1:
                LogBattleHistory("對戰結束");
                LogBattleHistory("我方戰敗");
                break;
            case 2:
                LogBattleHistory("對戰結束");
                LogBattleHistory("平手");
                break;
        }
    }

    private void RecordPlayerHeroHpLossHistory(int hpBefore, int hpAfter, int damage, string source)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        int actualDamage = Mathf.Max(0, damage);
        LogBattleHistory(source + "：我方英雄受到 " + actualDamage + " 點傷害");
        LogBattleHistory("我方英雄生命: " + hpBefore + " 降至 " + hpAfter + "（-" + actualDamage + "）");
        if (!playerHeroDeathLoggedThisBattle && hpBefore > 0 && hpAfter <= 0)
        {
            playerHeroDeathLoggedThisBattle = true;
            LogBattleHistory("我方英雄死亡");
        }
    }

    private class BattleMonster
    {
        public int id;
        public string cardName;
        public string cardNameEnglish;
        public int attack;
        public int currentHp;
        public int maxHp;

        public BattleMonster(MonsterCard source)
        {
            id = source.id;
            cardName = source.cardName;
            cardNameEnglish = source.cardNameEnglish ?? string.Empty;
            attack = source.attack;
            maxHp = source.healthPointMax;
            currentHp = source.healthPointMax;
        }
    }

    private static string DebugFieldMonsterName(BattleMonster m)
    {
        if (m == null) return "None";
        return string.IsNullOrWhiteSpace(m.cardNameEnglish) ? m.cardName : m.cardNameEnglish;
    }

    private int playerHp;
    private int enemyHp;
    private bool playerTurn;
    private bool battleOver;
    private int battleResult; // 0=ongoing, 1=player win, -1=player lose, 2=draw
    private string battleRuleMessage = string.Empty;
    [Header("Opening")]
    [Tooltip("Opening dice presentation duration (realtime seconds). Player input is locked. BattleAutoSimPlugin may shorten this during batch sim.")]
    [SerializeField] private float openingPresentationSeconds = 3f;
    private string openingRollMessage = string.Empty;
    private float openingRollMessageUntil;
    private int openingPlayerDice;
    private int openingEnemyDice;
    private bool openingPlayerFirst;
    private int openingRollVersion;
    private int currentRound = 1;
    private int battleSessionId;

    private readonly List<Card> playerDeck = new List<Card>();
    private readonly List<Card> enemyDeck = new List<Card>();
    private readonly List<Card> playerHand = new List<Card>();
    private readonly List<Card> enemyHand = new List<Card>();
    private readonly List<Card> playerDiscardPile = new List<Card>();
    private readonly List<Card> enemyDiscardPile = new List<Card>();

    private BattleMonster playerField;
    private BattleMonster enemyField;
    private bool playerHasAttackedThisTurn;
    private bool enemyOpeningTurnInProgress;
    private bool openingPresentationInProgress;
    private bool playerPlacedCardThisRound;
    private bool enemyPlacedCardThisRound;
    private int playerPendingDiscardCount;
    private bool playerPlayedHandCardThisTurn;
    private bool enemyPlayedHandCardThisTurn;
    private bool playerPassedTurnThisTurn;
    private bool enemyPassedTurnThisTurn;
    private bool playerEndedTurnThisRound;
    private bool enemyEndedTurnThisRound;
    private bool pendingPlayerDirectAttackUnlock;
    private bool playerCanDirectAttackThisTurn;
    private bool pendingEnemyDirectAttackUnlock;
    private bool enemyCanDirectAttackThisTurn;
    private bool playerCounterUsedThisRound;
    private bool enemyCounterUsedThisRound;
    private bool turnSequenceInProgress;
    private int activeSpellCastPresentationCount;
    private int enemySpellPresentationDepth;
    private float enemyDiscardPopupLockUntilUnscaled;
    private const float EnemyDiscardPopupLockSeconds = 2.85f;

    private SpellCard playerLinGazeSource;
    private int playerLinGazeRoundsRemaining;
    private SpellCard enemyLinGazeSource;
    private int enemyLinGazeRoundsRemaining;
    private bool linGazeEnemyAttackNoticeSentThisEnemyTurn;
    private bool linGazePlayerAttackNoticeSentThisPlayerTurn;

    private int playerKingTrainingCharges = 3;
    private int enemyKingTrainingCharges = 3;
    private bool playerQueenShelterUsed;
    private bool enemyQueenShelterUsed;
    private bool playerMilitiaFormationUsed;
    private bool enemyMilitiaFormationUsed;
    private bool playerKingWasOnFieldThisBattle;
    private bool enemyKingWasOnFieldThisBattle;

    private string battleToastMessage = string.Empty;
    private float battleToastUntilUnscaled;

    [Header("Debugging")]
    [Tooltip("開啟時才將流程說明與對戰歷史寫入 Console；關閉時僅保留 LogWarning / LogError。")]
    [SerializeField] private bool verboseBattleConsoleLog;

    private void BattleVerbose(string message)
    {
        if (!verboseBattleConsoleLog) return;
        GameDevLog.Log(message);
    }

    void Awake()
    {
        ApplyLaunchContextDifficulty();
    }

    void Start()
    {
        ApplyLaunchContextDifficulty();
        EnsureSceneVisualsReady();
        EnsureBattleUIExists();
        ResolveRefs();
        BattleCardTuningUserSettings.TryApplySelectedPreset(this);
        if (autoStartOnPlay) StartBattle();
    }

    private void EnsureSceneVisualsReady()
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas canvas2 = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null) continue;
            if (c.gameObject.name == "Canvas2" || c.gameObject.name == "Canva2")
            {
                canvas2 = c;
            }
        }

        if (canvas2 != null)
        {
            canvas2.gameObject.SetActive(true);
            canvas2.enabled = true;
            canvas2.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogWarning("BattleSimulation: Canvas2/Canva2 not found. UI may not display.");
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.enabled = true;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographic = true;
        }
    }

    private void EnsureBattleUIExists()
    {
        BattleSimulationDebugUI ui = UnityEngine.Object.FindFirstObjectByType<BattleSimulationDebugUI>();
        if (ui != null) return;

        GameObject uiHost = new GameObject("BattleSimulationDebugUI");
        uiHost.AddComponent<BattleSimulationDebugUI>();
        BattleVerbose("BattleSimulationManager: auto-created BattleSimulationDebugUI.");
    }

    private void ResolveRefs()
    {
        // dataManager 若指向 Project 裡的 Prefab（不屬於任何 Scene），執行期必須先 Instantiate，否則 GetComponent 取不到 PlayerData / CardStore。
        if (dataManager != null && !dataManager.scene.IsValid())
        {
            dataManager = Instantiate(dataManager, transform);
            dataManager.name = "DataManager";
        }

        if (dataManager != null)
        {
            if (playerData == null) playerData = dataManager.GetComponent<PlayerData>();
            if (cardStore == null) cardStore = dataManager.GetComponent<CardStore>();
        }

        if (playerData == null) playerData = PlayerData.ResolveCanonical();
        if (cardStore == null) cardStore = UnityEngine.Object.FindFirstObjectByType<CardStore>();
        if (cardStore == null && playerData != null && playerData.CardStore != null)
            cardStore = playerData.CardStore;
        if (enemyAI == null) enemyAI = UnityEngine.Object.FindFirstObjectByType<EnemyAI>();
        if (enemyAI == null)
        {
            enemyAI = GetComponent<EnemyAI>();
            if (enemyAI == null) enemyAI = gameObject.AddComponent<EnemyAI>();
            BattleVerbose("BattleSimulationManager: auto attached EnemyAI");
        }
    }

    public void StartBattle()
    {
        ResolveRefs();
        if (playerData == null || cardStore == null)
        {
            Debug.LogError("Battle start failed: missing PlayerData/CardStore");
            return;
        }
        if (cardStore.cardList == null || cardStore.cardList.Count == 0)
        {
            cardStore.LoadCardData();
        }
        if (cardStore.cardList == null || cardStore.cardList.Count == 0)
        {
            Debug.LogError("Battle start failed: card list is empty");
            return;
        }
        playerData.LoadPlayerData(); // ensure battle uses latest saved deck data
        if (BattleLaunchContext.IsIntroTutorialBattle)
            TutorialDeckApplicator.EnsureIntroTutorialDeckReady(playerData);

        battleHistoryLines.Clear();
        battleHistoryEntries.Clear();
        battleHistorySequence = 0;
        pendingDiscardHistoryLines.Clear();
        playerHeroDeathLoggedThisBattle = false;
        if (discardHistoryFlushRoutine != null)
        {
            StopCoroutine(discardHistoryFlushRoutine);
            discardHistoryFlushRoutine = null;
        }
        battleSessionId++;
        playerDeck.Clear();
        enemyDeck.Clear();
        playerHand.Clear();
        enemyHand.Clear();
        playerDiscardPile.Clear();
        enemyDiscardPile.Clear();
        playerField = null;
        enemyField = null;
        playerHasAttackedThisTurn = false;
        enemyOpeningTurnInProgress = false;
        openingPresentationInProgress = true;
        playerPlacedCardThisRound = false;
        enemyPlacedCardThisRound = false;
        playerPendingDiscardCount = 0;
        playerPlayedHandCardThisTurn = false;
        enemyPlayedHandCardThisTurn = false;
        playerPassedTurnThisTurn = false;
        enemyPassedTurnThisTurn = false;
        playerEndedTurnThisRound = false;
        enemyEndedTurnThisRound = false;
        pendingPlayerDirectAttackUnlock = false;
        playerCanDirectAttackThisTurn = false;
        pendingEnemyDirectAttackUnlock = false;
        enemyCanDirectAttackThisTurn = false;
        playerCounterUsedThisRound = false;
        enemyCounterUsedThisRound = false;
        turnSequenceInProgress = false;
        activeSpellCastPresentationCount = 0;
        enemySpellPresentationDepth = 0;
        enemyDiscardPopupLockUntilUnscaled = 0f;
        deferEnemyFieldUiClearAfterPlayerFireballKill = false;
        deferPlayerFieldUiClearAfterEnemyFireballKill = false;
        battleOver = false;
        battleResult = 0;
        lastBattleEndedBySurrender = false;
        battleRuleMessage = string.Empty;
        NotifyTurnBanner(BattleTurnBannerKind.Hidden);
        ClearPlayerLinGaze();
        ClearEnemyLinGaze();
        ResetMonsterSkillBattleState();
        enemySchemingHoldStreak = 0;
        battleToastMessage = string.Empty;
        battleToastUntilUnscaled = 0f;
        openingRollMessage = string.Empty;
        openingRollMessageUntil = 0f;
        openingPlayerDice = 0;
        openingEnemyDice = 0;
        openingPlayerFirst = true;
        currentRound = 1;
        currentWeather = BattleWeatherType.None;
        weatherTurnSerial = 0;
        playerFirstSpellBoostAvailable = false;
        enemyFirstSpellBoostAvailable = false;
        weatherForecastInProgress = false;
        firstWeatherOverrideConsumed = false;
        weatherActiveRoundsRemaining = 0;
        weatherCooldownRoundsRemaining = 0;
        weatherRemainingRoundsForUi = 0;
        queuedWeatherForNextRound = BattleWeatherType.None;
        forecastPreviewWeatherForUi = BattleWeatherType.None;
        weatherRandomCycleBag.Clear();
        weatherRandomCycleCursor = 0;

        BuildPlayerDeck();
        BattleVerbose("BattleSimulation: loaded player deck count = " + playerDeck.Count);
        TryApplyLaunchDifficultyFromContext();
        BuildEnemyDeck();
        CaptureBattleDifficultyForRecords();
        Shuffle(playerDeck);
        Shuffle(enemyDeck);

        playerHp = startHealth;
        if (BattleLaunchContext.IsIntroTutorialBattle)
            enemyHp = IntroTutorialBattleRules.EnemyStartHealth;
        else if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
            enemyHp = HarborTrainingEasyBattleRules.EnemyStartHealth;
        else if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
            enemyHp = HarborTrainingNormalBattleRules.EnemyStartHealth;
        else
            enemyHp = startHealth;
        int playerDice = UnityEngine.Random.Range(1, 7);
        int enemyDice = UnityEngine.Random.Range(1, 7);
        while (playerDice == enemyDice)
        {
            playerDice = UnityEngine.Random.Range(1, 7);
            enemyDice = UnityEngine.Random.Range(1, 7);
        }
        playerTurn = playerDice > enemyDice;
        openingPlayerDice = playerDice;
        openingEnemyDice = enemyDice;
        openingPlayerFirst = playerTurn;
        openingRollVersion++;
        openingRollMessage =
            "First roll  Player dice: " + playerDice +
            "  Enemy dice: " + enemyDice +
            "  → " + (playerTurn ? "Player first" : "Enemy first");
        openingRollMessageUntil = Time.unscaledTime + openingPresentationSeconds;
        BattleVerbose("Battle start dice: Player=" + playerDice + " Enemy=" + enemyDice + " | first=" + (playerTurn ? "Player" : "Enemy"));

        int openingHandCount = Mathf.Clamp(5, 0, maxHandSize);
        for (int i = 0; i < openingHandCount; i++)
        {
            DrawCard(playerDeck, playerHand, "Player");
            DrawCard(enemyDeck, enemyHand, "Enemy");
        }

        if (BattleLaunchContext.IsIntroTutorialBattle && playerHand.Count == 0)
            Debug.LogError("Intro tutorial battle started with an empty player hand. Check deck save and CardStore ids.");

        EnsurePlayerHandDiscardRequirement();
        ResolveEnemyHandOverflowDiscards();

        BattleVerbose("Battle started");
        LogBattleHistory("對戰開始");
        LogBattleHistory("我方骰" + playerDice + "敵方骰" + enemyDice);
        LogBattleHistory(playerTurn ? "我方先手" : "敵方先手");
        PrintBattleState();
        StartCoroutine(BeginBattleAfterOpeningPresentation());
    }

    private IEnumerator BeginBattleAfterOpeningPresentation()
    {
        // 批次模擬：開場不等待實時秒數，避免每局固定卡 Realtime。
        float openWait = BattleAutoSimPlugin.IsRunning ? 0f : openingPresentationSeconds;
        yield return new WaitForSecondsRealtime(openWait);
        openingPresentationInProgress = false;
        if (!playerTurn)
        {
            StartCoroutine(RunEnemyOpeningTurn());
        }
        else
        {
            yield return StartCoroutine(CoPresentWeatherForecastForTurn(true));
            NotifyPlayerTurnActionWindowOpenedForPromptUi();
        }
    }

    private IEnumerator RunEnemyOpeningTurn()
    {
        enemyOpeningTurnInProgress = true;
        yield return StartCoroutine(RunEnemyTurn());
    }

    /// <returns>若為法術且已啟動異步演出則 true（出牌動畫須等 SpellCastAsyncPresentationFinished 再清 busy）。</returns>
    public bool PlayerPlayCardFromHand(int handIndex)
    {
        if (!CanPlayerAct()) return false;
        if (playerPendingDiscardCount > 0) return false;
        if (handIndex < 0 || handIndex >= playerHand.Count)
        {
            Debug.LogWarning("Invalid hand index");
            return false;
        }

        Card selected = playerHand[handIndex];
        if (playerField != null)
        {
            bool lesserHealExempt = selected is SpellCard spPre && spPre.SpellOrdinal == 1;
            if (!lesserHealExempt)
            {
                ShowBattleToast("我方場上已有怪獸時，僅能打初級治療；其他手牌無法打出。", 2.5f);
                return false;
            }
        }

        if (selected is MonsterCard monster)
        {
            playerHand.RemoveAt(handIndex);
            playerField = new BattleMonster(monster);
            ApplySummonMonsterSkills(playerField, true);
            playerPlacedCardThisRound = true;
            playerPlayedHandCardThisTurn = true;
            BattleVerbose("Player summoned: " + playerField.cardName);
            NotifyPlayerCommittedHandCardToFieldFromHandForUi();
        }
        else if (selected is SpellCard spell)
        {
            SpellCastHandAnchorCommitted?.Invoke(true, spell, handIndex);
            playerHand.RemoveAt(handIndex);
            StartCoroutine(PlayerResolveSpellAfterPresentation(spell, handIndex));
            return true;
        }

        CheckBattleResult();
        PrintBattleState();
        return false;
    }

    private IEnumerator PlayerResolveSpellAfterPresentation(SpellCard spell, int insertIndexIfFail)
    {
        activeSpellCastPresentationCount++;
        try
        {
            bool skipPresent = BattleAutoSimPlugin.IsRunning;
            if (!skipPresent)
            {
                SpellCastPresentationStarted?.Invoke(true, spell.cardName, GetSpellEffectTextForPresentation(spell));
            }
            yield return new WaitForSecondsRealtime(skipPresent ? 0f : spellCastPresentationSeconds);
            bool hadEnemyMonsterForFireball = spell.SpellOrdinal == 0 && enemyField != null;
            bool resolved = TryResolvePlayerSpell(spell);
            if (!resolved)
            {
                playerHand.Insert(insertIndexIfFail, spell);
            }
            else
            {
                if (spell.SpellOrdinal != 0 && spell.SpellOrdinal != 1)
                    BattleVerbose("Player cast spell: " + spell.cardName);
                playerPlacedCardThisRound = true;
                playerPlayedHandCardThisTurn = true;
            }
            CheckBattleResult();
            PrintBattleState();
            deferEnemyFieldUiClearAfterPlayerFireballKill =
                !BattleAutoSimPlugin.IsRunning &&
                resolved &&
                spell.SpellOrdinal == 0 &&
                hadEnemyMonsterForFireball &&
                enemyField == null;
            BattleLayoutVisualRefreshRequested?.Invoke();
            if (resolved && spell.SpellOrdinal == 1)
                PlayerLesserHealVisualRequested?.Invoke();
            if (resolved && spell.SpellOrdinal == 0)
                FireballVisualRequested?.Invoke(true, hadEnemyMonsterForFireball);
            if (resolved && spell.SpellOrdinal == 2)
                NotifyPlayerCommittedHandCardToFieldFromHandForUi();
        }
        finally
        {
            activeSpellCastPresentationCount--;
            SpellCastAsyncPresentationFinished?.Invoke();
        }
    }

    public void PlayerAttack()
    {
        if (!CanPlayerAct()) return;
        if (IsOpeningRoundAttackBlocked())
        {
            BattleVerbose("Opening round: attacks are not allowed; play cards only.");
            return;
        }
        if (playerHasAttackedThisTurn)
        {
            BattleVerbose("Already attacked this turn");
            return;
        }
        if (playerField == null)
        {
            BattleVerbose("No monster on player field");
            return;
        }

        if (EnemyLinGazeActive())
        {
            if (!linGazePlayerAttackNoticeSentThisPlayerTurn)
            {
                linGazePlayerAttackNoticeSentThisPlayerTurn = true;
                ShowBattleToast("林可的凝視：敵方場上凝視生效中，我方無法發動攻擊。", 3.2f);
            }
            BattleVerbose("Player attack blocked by enemy Lin's Gaze.");
            return;
        }

        if (enemyField != null)
        {
            string defenderName = enemyField.cardName;
            string attackerName = playerField.cardName;
            int playerAtkDmg = ModifyDamageToEnemyMonster(playerField.attack);
            enemyField.currentHp -= playerAtkDmg;
            if (enemyField.currentHp <= 0) enemyField = null;
            LogBattleHistory("我方場地上 怪物牌 " + attackerName + " 對敵方造成" + playerAtkDmg + " 點傷害");
            bool counterTriggered = false;
            int counterDamage = 0;
            if (enemyField != null && playerField != null && playerField.currentHp > 0 && !enemyCounterUsedThisRound)
            {
                counterDamage = ModifyDamageToPlayerMonster(enemyField.attack);
                playerField.currentHp -= counterDamage;
                enemyCounterUsedThisRound = true;
                counterTriggered = true;
                LogBattleHistory("敵方場地上 怪物牌 " + enemyField.cardName + " 反擊了我方場地上 怪物牌 " + playerField.cardName + " 1次");
            }
            AttackPerformed?.Invoke(new AttackVisualData
            {
                attackerIsPlayer = true,
                hasMonsterTarget = true,
                attackerDamage = playerAtkDmg,
                counterTriggered = counterTriggered,
                counterDamage = counterDamage
            });
            if (playerField.currentHp <= 0) playerField = null;
        }
        else
        {
            if (!playerCanDirectAttackThisTurn)
            {
                BattleVerbose("Enemy field is empty: direct attack is allowed on your next player turn after you end this turn.");
                return;
            }
            int directDmg = ModifyDirectDamageToEnemyHero(playerField.attack);
            enemyHp -= directDmg;
            LogBattleHistory("我方場地上 怪物牌 " + playerField.cardName + " 對敵方英雄造成" + directDmg + " 點傷害");
            AttackPerformed?.Invoke(new AttackVisualData
            {
                attackerIsPlayer = true,
                hasMonsterTarget = false,
                attackerDamage = directDmg,
                counterTriggered = false,
                counterDamage = 0
            });
        }

        playerHasAttackedThisTurn = true;
        CheckBattleResult();
        PrintBattleState();
    }

    public void EndPlayerTurn()
    {
        if (!CanPlayerAct()) return;
        if (playerPendingDiscardCount > 0)
        {
            ShowBattleToast("手牌超過上限，請先棄牌。", 1.8f);
            return;
        }
        NotifyPlayerPressedEndTurnForPromptUi();
        playerPassedTurnThisTurn = !playerPlayedHandCardThisTurn && HasPlayerPersistentBoardOrSpellActive();
        if (playerPassedTurnThisTurn)
            LogBattleHistory("我方本回合 PASS");
        // Ending the turn without playing counts as a pass so the exchange clock (and field effects like Lin's Gaze) can advance.
        if (!playerPlacedCardThisRound)
            playerPlacedCardThisRound = true;
        playerEndedTurnThisRound = true;
        pendingPlayerDirectAttackUnlock = (enemyField == null);
        BattleVerbose("Player turn ended");
        StartCoroutine(RunPlayerEndTurnAttackThenEnemyTurn());
    }

    private IEnumerator RunPlayerEndTurnAttackThenEnemyTurn()
    {
        turnSequenceInProgress = true;
        if (!BattleAutoSimPlugin.IsRunning && attackDelayAfterEndTurn > 0f)
            yield return new WaitForSeconds(attackDelayAfterEndTurn);
        if (!battleOver && !playerHasAttackedThisTurn)
        {
            PlayerAttack();
            yield return WaitForBattleAttackFxIfAny();
        }
        if (!battleOver)
        {
            ApplyFireRainEndTurnEffect();
            CheckBattleResult();
        }
        if (battleOver)
        {
            turnSequenceInProgress = false;
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            yield break;
        }

        playerTurn = false;
        yield return StartCoroutine(RunEnemyTurn());
    }

    private IEnumerator RunEnemyTurn()
    {
        // 每個敵方回合開始時重置。若換回合條件未滿導致 TryAdvanceRound 未執行到重置處，上回合殘留的 true 會讓本回合「被攻擊方反擊」永遠被擋下。
        playerCounterUsedThisRound = false;
        enemyCounterUsedThisRound = false;
        enemyPlayedHandCardThisTurn = false;
        enemyPassedTurnThisTurn = false;

        NotifyTurnBanner(BattleTurnBannerKind.EnemyTurn);
        turnSequenceInProgress = true;
        if (!BattleAutoSimPlugin.IsRunning)
            yield return new WaitForSeconds(0.4f);
        if (battleOver)
        {
            turnSequenceInProgress = false;
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            yield break;
        }
        enemyCanDirectAttackThisTurn = pendingEnemyDirectAttackUnlock;
        pendingEnemyDirectAttackUnlock = false;
        BattleVerbose("Enemy turn: begin");
        linGazeEnemyAttackNoticeSentThisEnemyTurn = false;

        EnemyDrawCards(GetEnemyDrawCountPerTurn());
        ResolveEnemyHandOverflowDiscards();
        yield return WaitForEnemyDiscardPopupLockRelease();
        if (!BattleAutoSimPlugin.IsRunning)
            yield return new WaitForSeconds(0.2f);

        if (enemyAI != null) enemyAI.ExecutePlay(this);
        else EnemyPlayCardIfPossible();
        while (enemySpellPresentationDepth > 0)
        {
            yield return null;
        }
        BattleVerbose("Enemy turn: after play | hand=" + enemyHand.Count + " field=" + (enemyField == null ? "None" : enemyField.cardName));
        if (!BattleAutoSimPlugin.IsRunning && attackDelayAfterEndTurn > 0f)
            yield return new WaitForSeconds(attackDelayAfterEndTurn);

        if (enemyAI != null) enemyAI.ExecuteAttack(this);
        else EnemyAttackIfPossible();
        yield return WaitForBattleAttackFxIfAny();
        BattleVerbose("Enemy turn: after attack");
        if (!BattleAutoSimPlugin.IsRunning)
            yield return new WaitForSeconds(0.2f);
        if (!battleOver)
        {
            ApplyFireRainEndTurnEffect();
            CheckBattleResult();
        }

        CheckBattleResult();
        if (battleOver)
        {
            turnSequenceInProgress = false;
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            yield break;
        }

        playerTurn = true;
        playerHasAttackedThisTurn = false;
        linGazePlayerAttackNoticeSentThisPlayerTurn = false;
        playerCanDirectAttackThisTurn = pendingPlayerDirectAttackUnlock;
        pendingPlayerDirectAttackUnlock = false;
        pendingEnemyDirectAttackUnlock = (playerField == null);
        enemyEndedTurnThisRound = true;
        playerPlayedHandCardThisTurn = false;
        playerPassedTurnThisTurn = false;
        enemyPassedTurnThisTurn = !enemyPlayedHandCardThisTurn && HasEnemyPersistentBoardOrSpellActive();
        if (enemyPassedTurnThisTurn)
            LogBattleHistory("敵方本回合 PASS");
        // Enemy did not play a card this turn (e.g. empty hand or failed summon): treat as pass so rounds / gaze timers advance.
        if (!enemyPlacedCardThisRound)
            enemyPlacedCardThisRound = true;
        if (enemyOpeningTurnInProgress)
        {
            enemyOpeningTurnInProgress = false;
        }
        else
        {
            TryAdvanceRound();
        }
        CheckBattleResult();
        if (battleOver)
        {
            turnSequenceInProgress = false;
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            yield break;
        }
        DrawPlayerCards(2);
        EnsurePlayerHandDiscardRequirement();
        BattleVerbose("Player turn started | Round " + currentRound);
        PrintBattleState();
        yield return StartCoroutine(CoPresentWeatherForecastForTurn(true));
        turnSequenceInProgress = false;
        NotifyPlayerTurnActionWindowOpenedForPromptUi();
    }

    private void EnemyPlayCardIfPossible()
    {
        int chosen = ChooseEnemyHandCardToPlayIndex();
        if (chosen < 0) return;
        EnemyPlayCardFromHand(chosen);
    }

    private void EnemyAttackIfPossible()
    {
        if (IsOpeningRoundAttackBlocked())
        {
            BattleVerbose("Enemy attack skipped: opening round cannot attack.");
            return;
        }
        if (enemyField == null) return;

        if (PlayerLinGazeActive())
        {
            if (!linGazeEnemyAttackNoticeSentThisEnemyTurn)
            {
                linGazeEnemyAttackNoticeSentThisEnemyTurn = true;
                ShowBattleToast("林可的凝視：敵方無法發動攻擊。", 3.2f);
            }
            BattleVerbose("Enemy attack blocked by Lin's Gaze.");
            return;
        }

        if (playerField != null)
        {
            int attackerDamage = enemyField.attack;
            string attackerName = enemyField.cardName;
            int enemyAtkDmg = ScaleContextualEnemyDamage(
                ModifyDamageToPlayerMonster(enemyField.attack));
            playerField.currentHp -= enemyAtkDmg;
            if (playerField.currentHp <= 0) playerField = null;
            LogBattleHistory("敵方場地上 怪物牌 " + attackerName + " 對我方造成" + enemyAtkDmg + " 點傷害");
            bool counterTriggered = false;
            int counterDamage = 0;
            if (playerField != null && enemyField != null && enemyField.currentHp > 0 && !playerCounterUsedThisRound)
            {
                counterDamage = ModifyDamageToEnemyMonster(playerField.attack);
                enemyField.currentHp -= counterDamage;
                playerCounterUsedThisRound = true;
                counterTriggered = true;
                LogBattleHistory("我方場地上 怪物牌 " + playerField.cardName + " 反擊了敵方場地上 怪物牌 " + enemyField.cardName + " 1次");
            }
            AttackPerformed?.Invoke(new AttackVisualData
            {
                attackerIsPlayer = false,
                hasMonsterTarget = true,
                attackerDamage = enemyAtkDmg,
                counterTriggered = counterTriggered,
                counterDamage = counterDamage
            });
            if (enemyField.currentHp <= 0) enemyField = null;
        }
        else
        {
            if (!enemyCanDirectAttackThisTurn)
            {
                BattleVerbose("Enemy direct attack blocked: can attack only on next enemy turn after seeing empty player field.");
                return;
            }
            int directDmg = ScaleContextualEnemyDamage(
                ModifyDirectDamageToPlayerHero(enemyField.attack));
            int hpBefore = playerHp;
            playerHp -= directDmg;
            LogBattleHistory("敵方場地上 怪物牌 " + enemyField.cardName + " 對我方英雄造成" + directDmg + " 點傷害");
            RecordPlayerHeroHpLossHistory(hpBefore, playerHp, directDmg, "敵方場地怪獸直擊");
            AttackPerformed?.Invoke(new AttackVisualData
            {
                attackerIsPlayer = false,
                hasMonsterTarget = false,
                attackerDamage = directDmg,
                counterTriggered = false,
                counterDamage = 0
            });
        }
    }

    private bool IsOpeningRoundAttackBlocked()
    {
        return currentRound <= 1;
    }

    private bool IsOpeningRoundFireballBlocked()
    {
        return currentRound <= 1;
    }

    private void TryAdvanceRound()
    {
        if (!playerEndedTurnThisRound || !enemyEndedTurnThisRound) return;
        if (playerHand.Count == 0)
            playerPlacedCardThisRound = true;
        if (enemyHand.Count == 0)
            enemyPlacedCardThisRound = true;
        if (!playerPlacedCardThisRound || !enemyPlacedCardThisRound) return;

        currentRound++;
        if (BattleLaunchContext.IsIntroTutorialBattle &&
            currentRound > IntroTutorialBattleRules.MaxRoundsInclusive)
        {
            ForceIntroTutorialRoundCapVictory();
            return;
        }

        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle() &&
            currentRound > HarborTrainingEasyBattleRules.MaxRoundsInclusive)
        {
            ForceHarborEasyRoundCapVictory();
            return;
        }

        TickPlayerLinGazeEndOfRound();
        TickEnemyLinGazeEndOfRound();
        playerPlacedCardThisRound = false;
        enemyPlacedCardThisRound = false;
        playerEndedTurnThisRound = false;
        enemyEndedTurnThisRound = false;
        playerCounterUsedThisRound = false;
        enemyCounterUsedThisRound = false;
    }

    public bool PlayerLinGazeActive()
    {
        return playerLinGazeSource != null && playerLinGazeRoundsRemaining > 0;
    }

    /// <summary>林可的凝視：我方場上須無怪獸且未已有凝視效果。</summary>
    public bool CanPlayerCastLinGazeNow()
    {
        return playerField == null && !PlayerLinGazeActive();
    }

    /// <summary>林可的凝視（敵方手牌／AI）：敵方怪獸區須為空且場上尚無敵方凝視。</summary>
    public bool CanEnemyCastLinGazeNow()
    {
        return enemyField == null && !EnemyLinGazeActive();
    }

    public bool EnemyLinGazeActive()
    {
        return enemyLinGazeSource != null && enemyLinGazeRoundsRemaining > 0;
    }

    public int GetEnemyLinGazeRoundsRemaining()
    {
        return enemyLinGazeRoundsRemaining;
    }

    public int GetPlayerLinGazeRoundsRemaining()
    {
        return playerLinGazeRoundsRemaining;
    }

    private void ClearPlayerLinGaze()
    {
        bool hadGaze = playerLinGazeSource != null;
        playerLinGazeSource = null;
        playerLinGazeRoundsRemaining = 0;
        if (hadGaze)
            BattleLayoutVisualRefreshRequested?.Invoke();
    }

    private void ClearEnemyLinGaze()
    {
        bool hadGaze = enemyLinGazeSource != null;
        enemyLinGazeSource = null;
        enemyLinGazeRoundsRemaining = 0;
        if (hadGaze)
            BattleLayoutVisualRefreshRequested?.Invoke();
    }

    private void TickPlayerLinGazeEndOfRound()
    {
        if (!PlayerLinGazeActive()) return;

        ApplyLinGazePeriodicDamage(5);
        LinGazePeriodicStrikeVisualRequested?.Invoke(true);
        playerLinGazeRoundsRemaining--;
        if (playerLinGazeRoundsRemaining <= 0)
        {
            ClearPlayerLinGaze();
            ShowBattleToast("林可的凝視：最後一次全體 -5 HP，效果結束。", 3f);
        }
        else
        {
            ShowBattleToast("林可的凝視：全體 -5 HP（效果尚餘 " + playerLinGazeRoundsRemaining + " 回合）", 2.8f);
        }
    }

    private void TickEnemyLinGazeEndOfRound()
    {
        if (!EnemyLinGazeActive()) return;

        ApplyLinGazePeriodicDamage(5);
        LinGazePeriodicStrikeVisualRequested?.Invoke(false);
        enemyLinGazeRoundsRemaining--;
        if (enemyLinGazeRoundsRemaining <= 0)
        {
            ClearEnemyLinGaze();
            ShowBattleToast("林可的凝視（敵）：最後一次全體 -5 HP，效果結束。", 3f);
        }
        else
        {
            ShowBattleToast("林可的凝視（敵）：全體 -5 HP（效果尚餘 " + enemyLinGazeRoundsRemaining + " 回合）", 2.8f);
        }
    }

    private void ApplyLinGazePeriodicDamage(int amount)
    {
        int playerHpBefore = playerHp;
        playerHp = Mathf.Max(0, playerHp - amount);
        enemyHp = Mathf.Max(0, enemyHp - amount);
        int playerHeroDamage = Mathf.Max(0, playerHpBefore - playerHp);
        if (playerHeroDamage > 0)
            RecordPlayerHeroHpLossHistory(playerHpBefore, playerHp, playerHeroDamage, "林可的凝視");
        if (playerField != null)
        {
            int gazeDmgPlayer = ModifyDamageToPlayerMonster(amount);
            playerField.currentHp -= gazeDmgPlayer;
            if (playerField.currentHp <= 0) playerField = null;
        }
        if (enemyField != null)
        {
            int gazeDmgEnemy = ModifyDamageToEnemyMonster(amount);
            enemyField.currentHp -= gazeDmgEnemy;
            if (enemyField.currentHp <= 0) enemyField = null;
        }
    }

    /// <summary>Ongoing Lin's Gaze spell shown on player field (not a monster).</summary>
    public Card GetPlayerFieldSpellCard()
    {
        return PlayerLinGazeActive() ? playerLinGazeSource : null;
    }

    /// <summary>敵方林可的凝視置於敵方咒術區（非怪獸）。</summary>
    public Card GetEnemyFieldSpellCard()
    {
        return EnemyLinGazeActive() ? enemyLinGazeSource : null;
    }

    public string GetBattleToastMessage()
    {
        return Time.unscaledTime <= battleToastUntilUnscaled ? battleToastMessage : string.Empty;
    }

    public void ShowBattleToast(string message, float seconds = 2.5f)
    {
        if (string.IsNullOrEmpty(message)) return;
        battleToastMessage = message;
        battleToastUntilUnscaled = Time.unscaledTime + Mathf.Max(0.35f, seconds);
    }

    private bool TryResolvePlayerSpell(SpellCard spell)
    {
        switch (spell.SpellOrdinal)
        {
            case 0:
                if (IsOpeningRoundFireballBlocked())
                {
                    ShowBattleToast("首回合禁用火球術。", 1.8f);
                    return false;
                }
                ApplyPlayerSpellFireball(spell);
                return true;
            case 1:
                return ApplyPlayerSpellLesserHeal(spell);
            case 2:
                return ApplyPlayerSpellLinGaze(spell);
            default:
                Debug.LogWarning("Unknown player spell ordinal: " + spell.SpellOrdinal);
                return true;
        }
    }

    private void ApplyPlayerSpellFireball(SpellCard spell)
    {
        int dmg = ApplyWeatherSpellPowerBonus(20, true);
        int deal;
        if (enemyField != null)
        {
            int toMonster = ModifyDamageToEnemyMonster(dmg);
            deal = Mathf.Min(toMonster, Mathf.Max(0, enemyField.currentHp));
            enemyField.currentHp -= deal;
            if (enemyField.currentHp <= 0) enemyField = null;
        }
        else
        {
            int toHero = ModifyDirectDamageToEnemyHero(dmg);
            int before = enemyHp;
            enemyHp = Mathf.Max(0, enemyHp - toHero);
            deal = before - enemyHp;
        }
        LogBattleHistory("我方咒術區 法術牌 " + spell.cardName + " 對敵方造成" + deal + "點傷害");
    }

    private bool ApplyPlayerSpellLesserHeal(SpellCard spell)
    {
        if (playerField == null)
        {
            ShowBattleToast("初級治療：我方場上需要怪物才能回復。", 2.2f);
            return false;
        }
        int healAmount = ApplyHolyLightHealBonusIfNeeded(ApplyWeatherSpellPowerBonus(40, true));
        playerField.currentHp += healAmount;
        LogBattleHistory("我方使用了 法術牌 " + spell.cardName + " 對我方回復" + healAmount + "點生命值");
        ShowBattleToast("初級治療：我方場上怪獸 +" + healAmount + " HP（目前 " + playerField.currentHp + "／上限 " + playerField.maxHp + "，可溢出）。", 2.8f);
        return true;
    }

    private bool ApplyPlayerSpellLinGaze(SpellCard spell)
    {
        if (playerField != null)
        {
            ShowBattleToast("林可的凝視：我方場上有怪獸時無法發動。", 2.2f);
            return false;
        }
        if (PlayerLinGazeActive())
        {
            ShowBattleToast("林可的凝視：場上已有此效果。", 2f);
            return false;
        }
        playerLinGazeSource = spell;
        playerLinGazeRoundsRemaining = 3;
        ShowBattleToast("林可的凝視：已置於場上。3 回合內每回合全體 -5 HP，且敵方無法攻擊。", 3.5f);
        return true;
    }

    private bool TryApplyEnemySpell(SpellCard spell)
    {
        switch (spell.SpellOrdinal)
        {
            case 0:
                if (IsOpeningRoundFireballBlocked())
                    return false;
                ApplyEnemySpellFireball(spell);
                return true;
            case 1:
                return ApplyEnemySpellLesserHeal(spell);
            case 2:
                return ApplyEnemySpellLinGaze(spell);
            default:
                int hpBefore = playerHp;
                playerHp -= 2;
                RecordPlayerHeroHpLossHistory(hpBefore, playerHp, 2, "敵方法術效果");
                return true;
        }
    }

    private bool ApplyEnemySpellLesserHeal(SpellCard spell)
    {
        if (enemyField == null)
        {
            ShowBattleToast("初級治療：敵方場上需要怪物才能回復。", 2.2f);
            return false;
        }
        int healAmount = ApplyHolyLightHealBonusIfNeeded(ApplyWeatherSpellPowerBonus(40, false));
        enemyField.currentHp += healAmount;
        LogBattleHistory("敵方使用了 法術牌 " + spell.cardName + " 對敵方回復" + healAmount + "點生命值");
        ShowBattleToast("初級治療：敵方場上怪獸 +" + healAmount + " HP（目前 " + enemyField.currentHp + "／上限 " + enemyField.maxHp + "，可溢出）。", 2.8f);
        return true;
    }

    private bool ApplyEnemySpellLinGaze(SpellCard spell)
    {
        if (enemyField != null)
        {
            ShowBattleToast("林可的凝視：敵方場上有怪獸時無法發動。", 2.2f);
            return false;
        }
        if (EnemyLinGazeActive())
        {
            ShowBattleToast("林可的凝視：敵方場上已有此效果。", 2f);
            return false;
        }
        enemyLinGazeSource = spell;
        enemyLinGazeRoundsRemaining = 3;
        ShowBattleToast("林可的凝視（敵）：已置於場上。3 回合內每回合全體 -5 HP，且我方無法攻擊。", 3.5f);
        return true;
    }

    private void ApplyEnemySpellFireball(SpellCard spell)
    {
        int dmg = ScaleContextualEnemyDamage(ApplyWeatherSpellPowerBonus(20, false));
        int deal;
        if (playerField != null)
        {
            int toMonster = ModifyDamageToPlayerMonster(dmg);
            deal = Mathf.Min(toMonster, Mathf.Max(0, playerField.currentHp));
            playerField.currentHp -= deal;
            if (playerField.currentHp <= 0) playerField = null;
        }
        else
        {
            int toHero = ModifyDirectDamageToPlayerHero(dmg);
            int before = playerHp;
            playerHp = Mathf.Max(0, playerHp - toHero);
            deal = before - playerHp;
            if (deal > 0)
                RecordPlayerHeroHpLossHistory(before, playerHp, deal, "敵方法術 " + spell.cardName);
        }
        LogBattleHistory("敵方咒術區 法術牌 " + spell.cardName + " 對我方造成" + deal + "點傷害");
    }

    private void DrawCard(List<Card> fromDeck, List<Card> toHand, string owner)
    {
        if (fromDeck.Count == 0) return;

        Card c = fromDeck[0];
        fromDeck.RemoveAt(0);
        toHand.Add(c);
        CardDrawn?.Invoke(owner == "Player", c);
        BattleVerbose(owner + " draws: " + c.cardName);
    }

    private void DrawPlayerCards(int count)
    {
        for (int i = 0; i < count; i++)
            DrawCard(playerDeck, playerHand, "Player");
    }

    private void EnemyDrawCards(int count)
    {
        for (int i = 0; i < count; i++)
            DrawCard(enemyDeck, enemyHand, "Enemy");
    }

    private void BuildPlayerDeck()
    {
        PopulatePlayerDeckFromSave();

        if (playerDeck.Count == 0 && BattleLaunchContext.IsIntroTutorialBattle)
        {
            if (TutorialDeckApplicator.EnsureIntroTutorialDeckReady(playerData))
            {
                playerData.LoadPlayerData();
                PopulatePlayerDeckFromSave();
            }
        }

        if (playerDeck.Count == 0)
            Debug.LogWarning("Player deck is empty.");
        else if (BattleLaunchContext.IsIntroTutorialBattle && playerDeck.Count < 5)
            Debug.LogWarning("Intro tutorial player deck has fewer than 5 cards: " + playerDeck.Count);
    }

    private void PopulatePlayerDeckFromSave()
    {
        playerDeck.Clear();
        var saved = playerData.GetDeckMap(playerData.selectedDeckSlot);
        foreach (var kv in saved)
        {
            int id = kv.Key;
            int count = kv.Value;
            if (count <= 0) continue;

            Card card = cardStore.GetCardById(id);
            if (card == null)
            {
                Debug.LogWarning("BuildPlayerDeck: card id " + id + " not found in CardStore.");
                continue;
            }

            for (int i = 0; i < count; i++)
                playerDeck.Add(card);
        }
    }

    private void BuildEnemyDeck()
    {
        if (runtimeDifficultyConfigPending)
        {
            useFixedEnemyDeck = runtimeUseFixedEnemyDeck;
            if (runtimeFixedEnemyDeckCardIds != null && runtimeFixedEnemyDeckCardIds.Length > 0)
                fixedEnemyDeckCardIds = runtimeFixedEnemyDeckCardIds;
            enemyOverLimitAllowance = Mathf.Clamp(runtimeEnemyOverLimitAllowance, 0, 30);
            minEnemySpellsInDeck = Mathf.Clamp(runtimeMinEnemySpellsInDeck, 0, 20);
            runtimeDifficultyConfigPending = false;
        }
        int targetEnemyDeckSize = GetTargetEnemyDeckSizeToMatchPlayer();
        int playerMaxAttack;
        int playerMaxHealth;
        GetPlayerSavedDeckMaxStats(out playerMaxAttack, out playerMaxHealth);
        if (useFixedEnemyDeck && fixedEnemyDeckCardIds != null && fixedEnemyDeckCardIds.Length > 0)
        {
            int[] deckIdsForBuild = runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss
                ? BossFixedEnemyDeckCardIds
                : fixedEnemyDeckCardIds;
            List<Card> fixedPool = new List<Card>();
            for (int i = 0; i < deckIdsForBuild.Length; i++)
            {
                int id = deckIdsForBuild[i];
                Card fixedCard = cardStore.GetCardById(id);
                if (fixedCard != null) fixedPool.Add(fixedCard);
            }
            if (fixedPool.Count > 0)
            {
                for (int i = 0; i < targetEnemyDeckSize; i++)
                {
                    enemyDeck.Add(fixedPool[i % fixedPool.Count]);
                }
                ApplyEnemyDeckPowerBalancing(playerMaxAttack, playerMaxHealth);
                return;
            }
        }

        List<Card> monsters = new List<Card>();
        List<Card> spells = new List<Card>();
        for (int i = 0; i < cardStore.cardList.Count; i++)
        {
            Card c = cardStore.cardList[i];
            if (c is MonsterCard) monsters.Add(c);
            else if (c is SpellCard) spells.Add(c);
        }

        int guaranteedMonsters = Mathf.Min(targetEnemyDeckSize, Mathf.Max(8, targetEnemyDeckSize / 2));

        for (int i = 0; i < guaranteedMonsters; i++)
        {
            if (monsters.Count == 0) break;
            enemyDeck.Add(monsters[UnityEngine.Random.Range(0, monsters.Count)]);
        }

        while (enemyDeck.Count < targetEnemyDeckSize)
        {
            Card random = cardStore.RandomCard();
            if (random == null) break;
            enemyDeck.Add(random);
        }

        // Absolute fallback: if still no monster in deck, force one in.
        bool hasMonster = false;
        for (int i = 0; i < enemyDeck.Count; i++)
        {
            if (enemyDeck[i] is MonsterCard)
            {
                hasMonster = true;
                break;
            }
        }
        if (!hasMonster && monsters.Count > 0)
        {
            if (enemyDeck.Count == 0) enemyDeck.Add(monsters[0]);
            else enemyDeck[0] = monsters[UnityEngine.Random.Range(0, monsters.Count)];
        }

        while (enemyDeck.Count < targetEnemyDeckSize)
        {
            Card pick = cardStore.RandomCard();
            if (pick == null) break;
            enemyDeck.Add(pick);
        }

        EnsureMinSpellsInEnemyDeck(spells, targetEnemyDeckSize);
        ApplyEnemyDeckPowerBalancing(playerMaxAttack, playerMaxHealth);
    }

    /// <summary>Apply runtime enemy-difficulty config before the next battle deck build.</summary>
    public void QueueRuntimeDifficultyConfig(
        bool useFixed,
        int[] fixedDeckIds,
        int overLimitAllowance,
        int minSpells,
        EnemyAiPlayStyle aiPlayStyle = EnemyAiPlayStyle.Greedy,
        string difficultyLabelZh = null)
    {
        runtimeUseFixedEnemyDeck = useFixed;
        runtimeFixedEnemyDeckCardIds = fixedDeckIds != null ? (int[])fixedDeckIds.Clone() : null;
        runtimeEnemyOverLimitAllowance = overLimitAllowance;
        runtimeMinEnemySpellsInDeck = minSpells;
        runtimeEnemyAiPlayStyle = aiPlayStyle;
        runtimeDifficultyLabelZh = ResolveDifficultyLabelZh(difficultyLabelZh, aiPlayStyle);
        runtimeDifficultyLabelExplicit = true;
        BattleDifficultyRuntime.SetCurrentLabelZh(runtimeDifficultyLabelZh);
        BattleLaunchContext.SetPendingDifficultyLabelZh(runtimeDifficultyLabelZh);
        BattleLaunchContext.ConfirmActiveBattleDifficulty(runtimeDifficultyLabelZh);
        runtimeDifficultyConfigPending = true;
    }

    private static string ResolveDifficultyLabelZh(string difficultyLabelZh, EnemyAiPlayStyle aiPlayStyle)
    {
        if (!string.IsNullOrWhiteSpace(difficultyLabelZh))
            return difficultyLabelZh.Trim();
        switch (aiPlayStyle)
        {
            case EnemyAiPlayStyle.SchemingBoss: return "魔王";
            case EnemyAiPlayStyle.SchemingHard: return "困難";
            default: return BattleDifficultyRuntime.CurrentLabelZh;
        }
    }

    /// <summary>
    /// Enemy deck size should follow the player's actual battle deck count first.
    /// Fallback to saved deck metadata, then inspector default if needed.
    /// </summary>
    private int GetTargetEnemyDeckSizeToMatchPlayer()
    {
        if (playerDeck != null && playerDeck.Count > 0)
            return Mathf.Max(1, playerDeck.Count);
        return Mathf.Max(1, GetPlayerSavedDeckCardCount());
    }

    private void EnsureMinSpellsInEnemyDeck(List<Card> spells, int targetEnemyDeckSize)
    {
        if (spells == null || spells.Count == 0 || minEnemySpellsInDeck <= 0) return;
        int want = Mathf.Min(minEnemySpellsInDeck, spells.Count, Mathf.Max(0, targetEnemyDeckSize));
        int have = enemyDeck.Count(c => c is SpellCard);
        for (int n = have; n < want; n++)
        {
            List<int> monsterIdx = new List<int>();
            for (int i = 0; i < enemyDeck.Count; i++)
            {
                if (enemyDeck[i] is MonsterCard) monsterIdx.Add(i);
            }
            if (monsterIdx.Count == 0) break;
            int ri = monsterIdx[UnityEngine.Random.Range(0, monsterIdx.Count)];
            enemyDeck[ri] = spells[UnityEngine.Random.Range(0, spells.Count)];
        }
    }

    private void ApplyEnemyDeckPowerBalancing(int playerMaxAttack, int playerMaxHealth)
    {
        int overAllow = Mathf.Max(1, enemyOverLimitAllowance);
        EnforceEnemyDeckPowerCap(playerMaxAttack, playerMaxHealth, overAllow);
        if (!skipZeroStatEnemyFiller)
            EnsureEnemyDeckZeroStatRequirementIfOverLimit(playerMaxAttack, playerMaxHealth, 2);
    }

    private int GetPlayerSavedDeckCardCount()
    {
        if (playerData == null) return enemyDeckSize;
        int total = playerData.GetSelectedDeckTotalCount();
        return total > 0 ? total : enemyDeckSize;
    }

    private void GetPlayerSavedDeckMaxStats(out int maxAttack, out int maxHealth)
    {
        maxAttack = 0;
        maxHealth = 0;
        if (playerData == null || cardStore == null) return;

        var saved = playerData.GetDeckMap(playerData.selectedDeckSlot);
        foreach (var kv in saved)
        {
            if (kv.Value <= 0) continue;
            MonsterCard m = cardStore.GetCardById(kv.Key) as MonsterCard;
            if (m == null) continue;
            if (m.attack > maxAttack) maxAttack = m.attack;
            if (m.healthPointMax > maxHealth) maxHealth = m.healthPointMax;
        }
    }

    private void EnforceEnemyDeckPowerCap(int playerMaxAttack, int playerMaxHealth, int allowedOverLimitCount)
    {
        if (enemyDeck == null || enemyDeck.Count == 0) return;

        List<MonsterCard> legalMonsters = new List<MonsterCard>();
        if (cardStore != null && cardStore.cardList != null)
        {
            for (int i = 0; i < cardStore.cardList.Count; i++)
            {
                MonsterCard m = cardStore.cardList[i] as MonsterCard;
                if (m == null) continue;
                bool overLimit = m.attack > playerMaxAttack || m.healthPointMax > playerMaxHealth;
                if (!overLimit) legalMonsters.Add(m);
            }
        }

        int overLimitCount = 0;
        for (int i = 0; i < enemyDeck.Count; i++)
        {
            MonsterCard m = enemyDeck[i] as MonsterCard;
            if (m == null) continue;

            bool overLimit = m.attack > playerMaxAttack || m.healthPointMax > playerMaxHealth;
            if (!overLimit) continue;

            overLimitCount++;
            if (overLimitCount <= allowedOverLimitCount) continue;

            if (legalMonsters.Count > 0)
            {
                enemyDeck[i] = legalMonsters[UnityEngine.Random.Range(0, legalMonsters.Count)];
            }
        }
    }

    private void EnsureEnemyDeckZeroStatRequirementIfOverLimit(int playerMaxAttack, int playerMaxHealth, int minZeroStatCards)
    {
        if (enemyDeck == null || enemyDeck.Count == 0) return;

        bool hasOverLimit = false;
        int zeroStatCount = 0;
        for (int i = 0; i < enemyDeck.Count; i++)
        {
            MonsterCard m = enemyDeck[i] as MonsterCard;
            if (m == null) continue;
            if (m.attack > playerMaxAttack || m.healthPointMax > playerMaxHealth) hasOverLimit = true;
            if (m.attack == 0 || m.healthPointMax == 0) zeroStatCount++;
        }
        if (!hasOverLimit || zeroStatCount >= minZeroStatCards) return;

        List<MonsterCard> zeroStatPool = new List<MonsterCard>();
        if (cardStore != null && cardStore.cardList != null)
        {
            for (int i = 0; i < cardStore.cardList.Count; i++)
            {
                MonsterCard m = cardStore.cardList[i] as MonsterCard;
                if (m == null) continue;
                if (m.attack == 0 || m.healthPointMax == 0) zeroStatPool.Add(m);
            }
        }
        if (zeroStatPool.Count == 0)
        {
            Debug.LogWarning("Enemy deck rule: no zero-stat monsters available to satisfy requirement.");
            return;
        }

        int needed = minZeroStatCards - zeroStatCount;
        for (int i = 0; i < enemyDeck.Count && needed > 0; i++)
        {
            MonsterCard m = enemyDeck[i] as MonsterCard;
            if (m == null) continue;
            if (m.attack == 0 || m.healthPointMax == 0) continue;
            enemyDeck[i] = zeroStatPool[UnityEngine.Random.Range(0, zeroStatPool.Count)];
            needed--;
        }
    }

    private void Shuffle(List<Card> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Card temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private bool CanPlayerAct()
    {
        if (battleOver) return false;
        if (IsEnemyDiscardPopupLockActive()) return false;
        if (openingPresentationInProgress) return false;
        if (weatherForecastInProgress) return false;
        if (activeSpellCastPresentationCount > 0) return false;
        if (!playerTurn)
            return false;
        return true;
    }

    private bool PlayerHasNoHandAndNoFieldCards()
    {
        return playerHand.Count == 0 && playerField == null && !PlayerLinGazeActive();
    }

    private bool EnemyHasNoHandAndNoFieldCards()
    {
        return enemyHand.Count == 0 && enemyField == null && !EnemyLinGazeActive();
    }

    /// <summary>手牌、牌庫皆空且場上無怪／無凝視（我方）—無牌可打。</summary>
    private bool PlayerHasNoCardsToPlay()
    {
        return playerHand.Count == 0 && playerDeck.Count == 0 && playerField == null && !PlayerLinGazeActive();
    }

    private bool EnemyHasNoCardsToPlay()
    {
        return enemyHand.Count == 0 && enemyDeck.Count == 0 && enemyField == null && !EnemyLinGazeActive();
    }

    private bool HasPlayerPersistentBoardOrSpellActive()
    {
        return playerField != null || PlayerLinGazeActive();
    }

    private bool HasEnemyPersistentBoardOrSpellActive()
    {
        return enemyField != null || EnemyLinGazeActive();
    }

    private bool IsEnemyDiscardPopupLockActive()
    {
        if (BattleAutoSimPlugin.IsRunning) return false;
        return Time.unscaledTime < enemyDiscardPopupLockUntilUnscaled;
    }

    private IEnumerator WaitForBattleAttackFxIfAny()
    {
        if (BattleAutoSimPlugin.IsRunning) yield break;
        BattleSimulationDebugUI ui = UnityEngine.Object.FindFirstObjectByType<BattleSimulationDebugUI>();
        if (ui == null) yield break;
        yield return ui.WaitForAttackFxRoutine();
    }

    private IEnumerator WaitForEnemyDiscardPopupLockRelease()
    {
        while (IsEnemyDiscardPopupLockActive())
            yield return null;
    }

    private void EnsurePlayerHandDiscardRequirement()
    {
        playerPendingDiscardCount = Mathf.Max(0, playerHand.Count - maxHandSize);
    }

    private void DiscardOneFromPlayerHand(int handIndex)
    {
        if (handIndex < 0 || handIndex >= playerHand.Count) return;
        Card card = playerHand[handIndex];
        playerHand.RemoveAt(handIndex);
        playerDiscardPile.Add(card);
        PlayerCardDiscarded?.Invoke(card);
        QueueDiscardHistory("我方棄牌 " + (card != null ? card.DebugDisplayName : "Unknown"));
        EnsurePlayerHandDiscardRequirement();
    }

    private void DiscardOneFromEnemyHand(int handIndex)
    {
        if (handIndex < 0 || handIndex >= enemyHand.Count) return;
        Card card = enemyHand[handIndex];
        enemyHand.RemoveAt(handIndex);
        enemyDiscardPile.Add(card);
        if (!BattleAutoSimPlugin.IsRunning)
            enemyDiscardPopupLockUntilUnscaled = Mathf.Max(enemyDiscardPopupLockUntilUnscaled, Time.unscaledTime + EnemyDiscardPopupLockSeconds);
        EnemyCardDiscarded?.Invoke(card);
        QueueDiscardHistory("敵方棄牌 " + (card != null ? card.DebugDisplayName : "Unknown"));
    }

    private bool IsEnemyCardUnplayableNow(Card card)
    {
        if (card == null) return true;
        if (card is MonsterCard) return enemyField != null;
        if (card is SpellCard sp)
        {
            if (enemyField != null && sp.SpellOrdinal != 1) return true;
            if (sp.SpellOrdinal == 1 && enemyField == null) return true;
            if (sp.SpellOrdinal == 2 && !CanEnemyCastLinGazeNow()) return true;
            return false;
        }
        return true;
    }

    private int EvaluateEnemyCardKeepValue(Card card)
    {
        return EvaluateEnemyCardPlayPriority(card);
    }

    /// <summary>敵方本回合出牌優先度（含稀有度；數值愈高愈優先打出）。</summary>
    private int EvaluateEnemyCardPlayPriority(Card card)
    {
        if (card == null) return int.MinValue;
        int rarityBonus = GetEnemyPlayRarityBonus(card.rarity);
        if (card is MonsterCard m)
            return ApplyIntroEasyPriorityTweak(m.attack * 2 + m.healthPointMax + rarityBonus, card);
        if (card is SpellCard sp)
        {
            int spellValue;
            if (sp.SpellOrdinal == 1) spellValue = enemyField != null ? 90 : 8;
            else if (sp.SpellOrdinal == 0) spellValue = enemyField != null ? 55 : 75;
            else if (sp.SpellOrdinal == 2) spellValue = CanEnemyCastLinGazeNow() ? 62 : 10;
            else spellValue = 20;
            if (sp.SpellOrdinal == 0 && IsOpeningRoundFireballBlocked()) spellValue = int.MinValue / 4;
            return ApplyIntroEasyPriorityTweak(spellValue + rarityBonus, card);
        }
        return ApplyIntroEasyPriorityTweak(rarityBonus, card);
    }

    private int ApplyIntroEasyPriorityTweak(int basePriority, Card card)
    {
        if (card == null) return basePriority;
        if (runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.IntroGreedy)
        {
            if (card is SpellCard)
                return basePriority - 26;
            if (card is MonsterCard)
                return basePriority - 12;
        }

        if (runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.FastAttack)
        {
            if (card is SpellCard sp)
            {
                int spellTweak = sp.SpellOrdinal == 1 ? -12 : -30;
                if (HarborTrainingEasyBattleRules.IsActiveEasyBattle() && currentRound <= HarborTrainingEasyBattleRules.SoftPressureRoundsInclusive)
                    spellTweak = sp.SpellOrdinal == 1 ? -18 : -36;
                else if (HarborTrainingNormalBattleRules.IsActiveNormalBattle() &&
                         currentRound <= HarborTrainingNormalBattleRules.SoftPressureRoundsInclusive)
                    spellTweak = sp.SpellOrdinal == 1 ? -16 : -34;
                return basePriority + spellTweak;
            }

            if (card is MonsterCard)
            {
                int monBonus = 16;
                if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
                    monBonus = HarborTrainingEasyBattleRules.GetFastAttackMonsterPriorityBonus(currentRound);
                else if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
                    monBonus = HarborTrainingNormalBattleRules.GetFastAttackMonsterPriorityBonus(currentRound);
                return basePriority + monBonus;
            }
        }

        if (runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.EasySpellLean && card is SpellCard)
            return basePriority + 12;
        return basePriority;
    }

    private int GetEnemyDrawCountPerTurn()
    {
        if (BattleLaunchContext.IsIntroTutorialBattle)
            return IntroTutorialBattleRules.EnemyDrawPerTurn;
        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
            return HarborTrainingEasyBattleRules.GetEnemyDrawPerTurn(currentRound);
        if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
            return HarborTrainingNormalBattleRules.GetEnemyDrawPerTurn(currentRound);
        return 2;
    }

    private static int ScaleContextualEnemyDamage(int rawDamage)
    {
        if (rawDamage <= 0)
            return rawDamage;
        if (BattleLaunchContext.IsIntroTutorialBattle)
        {
            return Mathf.Max(
                1,
                Mathf.RoundToInt(rawDamage * IntroTutorialBattleRules.EnemyDamageMultiplier));
        }

        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle())
            return HarborTrainingEasyBattleRules.ScaleEnemyDamage(rawDamage);
        if (HarborTrainingNormalBattleRules.IsActiveNormalBattle())
            return HarborTrainingNormalBattleRules.ScaleEnemyDamage(rawDamage);
        return rawDamage;
    }

    private void ForceIntroTutorialRoundCapVictory()
    {
        if (battleOver) return;

        ShowBattleToast(
            "教學戰第 " + IntroTutorialBattleRules.MaxRoundsInclusive + " 回合結束，判定獲勝",
            3.2f);
        LogBattleHistory(
            "教學戰限時：第 " + IntroTutorialBattleRules.MaxRoundsInclusive + " 回合結束，判定我方獲勝。");
        CompleteBattle(1, "教學戰限時：第 " + IntroTutorialBattleRules.MaxRoundsInclusive + " 回合結束，判定我方獲勝。");
    }

    private void ForceHarborEasyRoundCapVictory()
    {
        if (battleOver) return;

        string msg = "港灣訓練（簡單）第 " + HarborTrainingEasyBattleRules.MaxRoundsInclusive +
                       " 回合結束，判定獲勝";
        ShowBattleToast(msg, 3.2f);
        LogBattleHistory(msg + "。");
        CompleteBattle(1, msg + "。");
    }

    private bool UsesSchemingEnemyAi =>
        runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingHard ||
        runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss;

    private int GetEnemyPlayRarityBonus(CardRarity rarity)
    {
        int bonus = CardRarityUtility.GetPlayAndKeepBonus(rarity);
        if (runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss)
            bonus = Mathf.RoundToInt(bonus * 1.35f);
        return bonus;
    }

    private int GetSchemingMaxHoldStreak() =>
        runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss ? 4 : 3;

    private int GetSchemingMinRarityRank() =>
        runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss
            ? (int)CardRarity.R
            : (int)CardRarity.SR;

    private bool IsSchemingHighRarityCard(Card card) =>
        card != null && CardRarityUtility.GetRank(card.rarity) >= GetSchemingMinRarityRank();

    private bool ShouldDeferSchemingCard(Card card)
    {
        if (!UsesSchemingEnemyAi || !IsSchemingHighRarityCard(card)) return false;
        if (enemySchemingHoldStreak >= GetSchemingMaxHoldStreak()) return false;
        return !IsSchemingPremiumTimingReady(card);
    }

    private bool IsSchemingPremiumTimingReady(Card card)
    {
        if (card == null) return true;
        bool strict = runtimeEnemyAiPlayStyle == EnemyAiPlayStyle.SchemingBoss;
        if (card is MonsterCard) return IsSchemingMonsterSummonReady(strict);
        if (card is SpellCard sp) return IsSchemingSpellReady(sp, strict);
        return true;
    }

    private bool IsSchemingMonsterSummonReady(bool strict)
    {
        if (enemyField != null) return true;
        MonsterCard playerMonster = GetPlayerFieldCard() as MonsterCard;
        if (playerMonster != null) return true;

        int hpGatePlayer = strict ? 58 : 65;
        int hpGateEnemyLow = strict ? 42 : 35;
        if (playerHp <= Mathf.CeilToInt(startHealth * hpGatePlayer / 100f)) return true;
        if (enemyHp <= Mathf.CeilToInt(startHealth * hpGateEnemyLow / 100f)) return true;
        if (enemyHand.Count >= 7) return true;

        if (playerField == null && playerHp > Mathf.CeilToInt(startHealth * (strict ? 0.68f : 0.7f)) &&
            enemyHp > Mathf.CeilToInt(startHealth * (strict ? 0.68f : 0.65f)))
            return false;

        return true;
    }

    private bool IsSchemingSpellReady(SpellCard sp, bool strict)
    {
        if (sp == null) return true;
        switch (sp.SpellOrdinal)
        {
            case 0:
                if (IsOpeningRoundFireballBlocked()) return false;
                if (enemyField != null) return true;
                if (playerField != null) return true;
                if (playerHp <= Mathf.CeilToInt(startHealth * (strict ? 0.52f : 0.5f))) return true;
                if (playerField == null && playerHp > Mathf.CeilToInt(startHealth * (strict ? 0.58f : 0.72f)))
                    return false;
                return true;
            case 1:
                if (enemyField == null) return false;
                float hurtRatio = strict ? 0.82f : 0.78f;
                return enemyField.currentHp < Mathf.CeilToInt(enemyField.maxHp * hurtRatio);
            case 2:
                if (!CanEnemyCastLinGazeNow()) return false;
                if (playerHp <= Mathf.CeilToInt(startHealth * (strict ? 0.48f : 0.45f))) return true;
                if (playerField == null && playerHp > Mathf.CeilToInt(startHealth * (strict ? 0.52f : 0.55f)))
                    return false;
                return true;
            default:
                return true;
        }
    }

    private int PickBestEnemyHandIndex(System.Predicate<Card> includeCard)
    {
        int chosen = -1;
        int bestPriority = int.MinValue;
        for (int i = 0; i < enemyHand.Count; i++)
        {
            Card c = enemyHand[i];
            if (IsEnemyCardUnplayableNow(c)) continue;
            if (includeCard != null && !includeCard(c)) continue;
            int priority = EvaluateEnemyCardPlayPriority(c);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                chosen = i;
            }
        }
        return chosen;
    }

    private int TryPickLethalMonsterIndex()
    {
        if (BattleLaunchContext.IsIntroTutorialBattle)
            return -1;

        MonsterCard playerMonster = GetPlayerFieldCard() as MonsterCard;
        if (enemyField != null || playerMonster == null) return -1;
        int glassCannonHp = playerMonster.healthPoint;
        if (playerMonster.attack <= glassCannonHp) return -1;

        int lethalIndex = -1;
        int bestRarityRank = -1;
        int bestAttack = -1;
        for (int i = 0; i < enemyHand.Count; i++)
        {
            if (!(enemyHand[i] is MonsterCard enemyMonster)) continue;
            if (enemyMonster.attack < glassCannonHp) continue;
            int rank = CardRarityUtility.GetRank(enemyMonster.rarity);
            if (rank > bestRarityRank || (rank == bestRarityRank && enemyMonster.attack > bestAttack))
            {
                bestRarityRank = rank;
                bestAttack = enemyMonster.attack;
                lethalIndex = i;
            }
        }
        return lethalIndex;
    }

    private void NoteEnemySchemingCardPlayed(Card played)
    {
        if (!UsesSchemingEnemyAi) return;
        if (played != null && IsSchemingHighRarityCard(played))
        {
            enemySchemingHoldStreak = 0;
            return;
        }
        for (int i = 0; i < enemyHand.Count; i++)
        {
            if (ShouldDeferSchemingCard(enemyHand[i]))
            {
                enemySchemingHoldStreak++;
                return;
            }
        }
        enemySchemingHoldStreak = 0;
    }

    /// <summary>敵方 AI：戰術斬殺 &gt;（困難／魔王）囤高稀有待機 &gt; 優先度出牌。</summary>
    public int ChooseEnemyHandCardToPlayIndex()
    {
        if (enemyHand.Count == 0) return -1;

        int lethal = TryPickLethalMonsterIndex();
        if (lethal >= 0) return lethal;

        System.Predicate<Card> notDeferred = c => !ShouldDeferSchemingCard(c);
        System.Predicate<Card> isMonster = c => c is MonsterCard;

        if (enemyField == null)
        {
            int monster = PickBestEnemyHandIndex(c => isMonster(c) && notDeferred(c));
            if (monster < 0) monster = PickBestEnemyHandIndex(isMonster);
            if (monster >= 0) return monster;
        }

        int chosen = PickBestEnemyHandIndex(notDeferred);
        if (chosen < 0) chosen = PickBestEnemyHandIndex(null);
        return chosen;
    }

    private bool IsPlayerCardUnplayableNow(Card card)
    {
        if (card == null) return true;
        if (playerField != null)
        {
            if (card is SpellCard spOnField) return spOnField.SpellOrdinal != 1;
            return true;
        }
        if (card is SpellCard sp)
        {
            if (sp.SpellOrdinal == 1) return true;
            if (sp.SpellOrdinal == 2 && !CanPlayerCastLinGazeNow()) return true;
        }
        return false;
    }

    private int EvaluatePlayerCardKeepValue(Card card)
    {
        if (card is MonsterCard m)
            return m.attack * 2 + m.healthPointMax;
        if (card is SpellCard sp)
        {
            if (sp.SpellOrdinal == 1) return playerField != null ? 90 : 8;
            if (sp.SpellOrdinal == 0) return playerField != null ? 55 : 75;
            if (sp.SpellOrdinal == 2) return CanPlayerCastLinGazeNow() ? 62 : 10;
            return 20;
        }
        return 0;
    }

    private int ChoosePlayerDiscardIndex()
    {
        if (playerHand.Count <= 0) return -1;
        for (int i = 0; i < playerHand.Count; i++)
        {
            if (IsPlayerCardUnplayableNow(playerHand[i])) return i;
        }

        int dupBestIndex = -1;
        int dupBestValue = int.MaxValue;
        for (int i = 0; i < playerHand.Count; i++)
        {
            Card c = playerHand[i];
            int dupCount = 0;
            for (int j = 0; j < playerHand.Count; j++)
            {
                if (i == j) continue;
                Card other = playerHand[j];
                if (other != null && c != null && other.id == c.id) dupCount++;
            }
            if (dupCount <= 0) continue;
            int value = EvaluatePlayerCardKeepValue(c);
            if (value < dupBestValue)
            {
                dupBestValue = value;
                dupBestIndex = i;
            }
        }
        if (dupBestIndex >= 0) return dupBestIndex;

        int best = 0;
        int bestValue = EvaluatePlayerCardKeepValue(playerHand[0]);
        for (int i = 1; i < playerHand.Count; i++)
        {
            int value = EvaluatePlayerCardKeepValue(playerHand[i]);
            if (value < bestValue)
            {
                bestValue = value;
                best = i;
            }
        }
        return best;
    }

    private int ChooseEnemyDiscardIndex()
    {
        if (enemyHand.Count <= 0) return -1;

        // 1) Cards not playable now.
        for (int i = 0; i < enemyHand.Count; i++)
        {
            if (IsEnemyCardUnplayableNow(enemyHand[i])) return i;
        }

        // 2) Duplicate + low value first.
        int dupBestIndex = -1;
        int dupBestValue = int.MaxValue;
        for (int i = 0; i < enemyHand.Count; i++)
        {
            Card c = enemyHand[i];
            int dupCount = 0;
            for (int j = 0; j < enemyHand.Count; j++)
            {
                if (i == j) continue;
                Card other = enemyHand[j];
                if (other != null && c != null && other.id == c.id) dupCount++;
            }
            if (dupCount <= 0) continue;
            int value = EvaluateEnemyCardKeepValue(c);
            if (value < dupBestValue)
            {
                dupBestValue = value;
                dupBestIndex = i;
            }
        }
        if (dupBestIndex >= 0) return dupBestIndex;

        // 3) Lowest current turn value.
        int best = 0;
        int bestValue = EvaluateEnemyCardKeepValue(enemyHand[0]);
        for (int i = 1; i < enemyHand.Count; i++)
        {
            int value = EvaluateEnemyCardKeepValue(enemyHand[i]);
            if (value < bestValue)
            {
                bestValue = value;
                best = i;
            }
        }
        return best;
    }

    private void ResolveEnemyHandOverflowDiscards()
    {
        while (enemyHand.Count > maxHandSize)
        {
            int idx = ChooseEnemyDiscardIndex();
            if (idx < 0) break;
            DiscardOneFromEnemyHand(idx);
        }
    }

    private void CheckBattleResult()
    {
        if (battleOver) return;

        if (BattleLaunchContext.IsIntroTutorialBattle &&
            currentRound > IntroTutorialBattleRules.MaxRoundsInclusive)
        {
            ForceIntroTutorialRoundCapVictory();
            return;
        }

        if (HarborTrainingEasyBattleRules.IsActiveEasyBattle() &&
            currentRound > HarborTrainingEasyBattleRules.MaxRoundsInclusive)
        {
            ForceHarborEasyRoundCapVictory();
            return;
        }

        // 平手：①雙方英雄 HP≥1 且皆無牌可打 ②雙方英雄於同一次結算中皆 HP≤0
        if (playerHp >= 1 && enemyHp >= 1 && PlayerHasNoCardsToPlay() && EnemyHasNoCardsToPlay())
        {
            CompleteBattle(2, "平手：雙方英雄 HP≥1，且皆無牌可打（手牌、牌庫與場上皆無可用牌）。");
            return;
        }
        if (playerHp <= 0 && enemyHp <= 0)
        {
            CompleteBattle(2, "平手：雙方英雄於同一次結算中生命皆≤0。");
            return;
        }

        // 戰敗：①英雄 HP≤0 ②手牌與場上皆無牌（牌庫可有牌，仍可能因無法上場而戰敗）
        bool playerDefeated = playerHp <= 0 || PlayerHasNoHandAndNoFieldCards();
        bool enemyDefeated = enemyHp <= 0 || EnemyHasNoHandAndNoFieldCards();

        if (playerDefeated && enemyDefeated)
        {
            CompleteBattle(2, "平手：雙方皆符合戰敗條件（英雄 HP≤0 或手牌＋場上無牌）。");
            return;
        }
        if (playerDefeated)
        {
            CompleteBattle(-1, "我方戰敗：英雄 HP≤0，或手牌與場上無牌。", logPlayerHeroDeath: true);
            return;
        }
        if (enemyDefeated)
        {
            CompleteBattle(1, "我方勝利：敵方符合戰敗條件（英雄 HP≤0 或手牌＋場上無牌）。");
        }
    }

    /// <summary>暫停選單「放棄對戰」：結束本局並記為戰敗（非中離）。</summary>
    public void ForcePlayerSurrender()
    {
        if (battleOver) return;
        lastBattleEndedBySurrender = true;
        CompleteBattle(-1, "我方戰敗：玩家放棄對戰。");
    }

    private void CompleteBattle(int result, string verboseMessage, bool logPlayerHeroDeath = false)
    {
        if (battleOver) return;
        battleOver = true;
        battleResult = result;
        if (logPlayerHeroDeath && playerHp <= 0 && !playerHeroDeathLoggedThisBattle)
        {
            playerHeroDeathLoggedThisBattle = true;
            LogBattleHistory("我方英雄死亡");
        }
        if (!string.IsNullOrEmpty(verboseMessage))
            BattleVerbose(verboseMessage);
        RecordBattleOutcomeHistory();
        string difficultyLabel = GetBattleDifficultyLabelForRecord();
        RecordCardProficiencyAfterBattle(difficultyLabel);
        PlayerProfileCsvService.RecordBattleResult(battleResult, difficultyLabel);
        NotifyTurnBanner(BattleTurnBannerKind.Hidden);
        BattleEnded?.Invoke(battleResult);
    }

    private void SetBattleRuleMessage(string message)
    {
        string next = message ?? string.Empty;
        if (battleRuleMessage == next) return;
        battleRuleMessage = next;
        BattleRuleMessageChanged?.Invoke(battleRuleMessage);
    }

    private void RecordCardProficiencyAfterBattle(string difficultyLabelZh)
    {
        if (playerData == null) return;
        IReadOnlyDictionary<int, int> deck = GetMonsterDeckMapForProficiencyRecord();
        CardSkillProficiencyService.RecordBattleOutcome(playerData, deck, battleResult, difficultyLabelZh);
        playerData.SavePlayerData();
    }

    /// <summary>結算熟練度：優先使用已存牌組；若槽位為空則從本局實際牌堆彙總怪物 id。</summary>
    public IReadOnlyDictionary<int, int> GetMonsterDeckMapForProficiencyRecord()
    {
        IReadOnlyDictionary<int, int> saved = playerData.GetDeckMap(playerData.selectedDeckSlot);
        if (saved != null && saved.Count > 0)
            return saved;
        return BuildRuntimeMonsterDeckMapForProficiency();
    }

    private Dictionary<int, int> BuildRuntimeMonsterDeckMapForProficiency()
    {
        var map = new Dictionary<int, int>();
        AccumulateMonsterIds(map, playerDeck);
        AccumulateMonsterIds(map, playerHand);
        AccumulateMonsterIds(map, playerDiscardPile);
        if (playerField != null)
            AddMonsterId(map, playerField.id);
        return map;
    }

    private static void AccumulateMonsterIds(Dictionary<int, int> map, List<Card> pile)
    {
        if (pile == null) return;
        for (int i = 0; i < pile.Count; i++)
        {
            if (pile[i] is MonsterCard monster)
                AddMonsterId(map, monster.id);
        }
    }

    private static void AddMonsterId(Dictionary<int, int> map, int monsterId)
    {
        if (monsterId <= 0) return;
        if (map.TryGetValue(monsterId, out int count))
            map[monsterId] = count + 1;
        else
            map[monsterId] = 1;
    }

    public void PrintBattleState()
    {
        BattleVerbose(GetBattleStateText());
    }

    public string GetBattleStateText()
    {
        string pf = playerField == null ? "None" : DebugFieldMonsterName(playerField) + " (" + playerField.currentHp + "/" + playerField.maxHp + ")";
        string ef = enemyField == null ? "None" : DebugFieldMonsterName(enemyField) + " (" + enemyField.currentHp + "/" + enemyField.maxHp + ")";
        string gaze = PlayerLinGazeActive() ? " [凝視×" + playerLinGazeRoundsRemaining + "]" : string.Empty;
        return
            "State | PlayerHP=" + playerHp +
            " EnemyHP=" + enemyHp +
            " Round=" + currentRound +
            " Weather=" + GetWeatherLabel(currentWeather) +
            (weatherRemainingRoundsForUi > 0 ? "(剩餘" + weatherRemainingRoundsForUi + "回合)" : string.Empty) +
            " PlayerHand=" + playerHand.Count +
            " EnemyHand=" + enemyHand.Count +
            " PlayerDiscard=" + playerDiscardPile.Count +
            " EnemyDiscard=" + enemyDiscardPile.Count +
            (playerPendingDiscardCount > 0 ? "  [請棄牌 " + playerPendingDiscardCount + "]" : string.Empty) +
            " PlayerField=" + pf + gaze +
            " EnemyField=" + ef;
    }

    public bool IsWeatherForecastInProgress()
    {
        return weatherForecastInProgress;
    }

    public string GetCurrentWeatherLabelForUi()
    {
        return GetWeatherLabel(currentWeather);
    }

    public int GetCurrentWeatherKindForUi()
    {
        return (int)currentWeather;
    }

    public bool IsCurrentWeatherFxActive(int weatherKind)
    {
        return weatherRemainingRoundsForUi > 0 && (int)currentWeather == weatherKind;
    }

    public string GetCurrentWeatherEffectForUi()
    {
        return GetCurrentWeatherEffectText();
    }

    public int GetCurrentWeatherRemainingRoundsForUi()
    {
        return weatherRemainingRoundsForUi;
    }

    public string GetNextWeatherForecastHintForUi()
    {
        if (currentRound <= 1)
            return "初始回合不觸發天氣預報";
        if (weatherRemainingRoundsForUi > 0)
            return "天氣作用中";
        if (queuedWeatherForNextRound != BattleWeatherType.None)
            return "下一回合將套用: " + GetWeatherLabel(queuedWeatherForNextRound);
        if (weatherCooldownRoundsRemaining <= 0)
            return "下一回合將觸發天氣預報";
        if (weatherCooldownRoundsRemaining == 1)
            return "再 1 回合後觸發天氣預報";
        return "再 " + weatherCooldownRoundsRemaining + " 回合後觸發天氣預報";
    }

    public string GetActiveWeatherBoardEffectTextForUi()
    {
        if (weatherRemainingRoundsForUi <= 0 || currentWeather == BattleWeatherType.None)
            return "目前無作用中的場上效果。";
        return GetCurrentWeatherForecastDetailsText();
    }

    public string GetQueuedWeatherForNextRoundLabelForUi()
    {
        return queuedWeatherForNextRound == BattleWeatherType.None
            ? "（未決定）"
            : GetWeatherLabel(queuedWeatherForNextRound);
    }

    public int GetPlayerHandCount() { return playerHand.Count; }
    public int GetPlayerPendingDiscardCount() { return playerPendingDiscardCount; }
    public bool IsPlayerInDiscardSelection() { return playerPendingDiscardCount > 0; }
    public int GetPlayerDiscardCount() { return playerDiscardPile.Count; }
    public int GetEnemyDiscardCount() { return enemyDiscardPile.Count; }
    public string GetPlayerDiscardTopName()
    {
        if (playerDiscardPile.Count <= 0) return "(無)";
        Card c = playerDiscardPile[playerDiscardPile.Count - 1];
        return c != null ? c.DebugDisplayName : "(未知)";
    }
    public string GetEnemyDiscardTopName()
    {
        if (enemyDiscardPile.Count <= 0) return "(無)";
        Card c = enemyDiscardPile[enemyDiscardPile.Count - 1];
        return c != null ? c.DebugDisplayName : "(未知)";
    }
    public bool PlayerDiscardCardFromHand(int handIndex)
    {
        if (!CanPlayerAct()) return false;
        EnsurePlayerHandDiscardRequirement();
        if (playerPendingDiscardCount <= 0) return false;
        if (handIndex < 0 || handIndex >= playerHand.Count) return false;
        DiscardOneFromPlayerHand(handIndex);
        return true;
    }
    public bool AutoDiscardOneForPlayer()
    {
        if (!CanPlayerAct()) return false;
        EnsurePlayerHandDiscardRequirement();
        if (playerPendingDiscardCount <= 0) return false;
        int index = ChoosePlayerDiscardIndex();
        if (index < 0) return false;
        DiscardOneFromPlayerHand(index);
        return true;
    }

    /// <summary>下一張建議棄掉的手牌索引（與 <see cref="AutoDiscardOneForPlayer"/> 相同邏輯）。</summary>
    public int GetRecommendedPlayerDiscardHandIndex() => ChoosePlayerDiscardIndex();
    public Card GetPlayerHandCard(int index)
    {
        if (index < 0 || index >= playerHand.Count) return null;
        return playerHand[index];
    }
    public int GetPlayerHandCardIndex(Card card)
    {
        if (card == null) return -1;
        return playerHand.IndexOf(card);
    }
    public int GetPlayerDeckCount() { return playerDeck.Count; }
    public int GetEnemyDeckCount() { return enemyDeck.Count; }
    public int GetEnemyHandCount() { return enemyHand.Count; }
    public Card GetEnemyHandCard(int index)
    {
        if (index < 0 || index >= enemyHand.Count) return null;
        return enemyHand[index];
    }
    public bool IsPlayerTurn() { return playerTurn && !battleOver; }

    /// <summary>玩家是否可操作手牌（我方回合且無開場／棄牌／法術演出鎖定）。</summary>
    public bool CanPlayerActNow() => CanPlayerAct();

    /// <summary>敵方是否可操作手牌（敵方回合且無開場／法術演出鎖定）。</summary>
    public bool CanEnemyActNow() => CanEnemyAct();

    private bool CanEnemyAct()
    {
        if (battleOver) return false;
        if (IsEnemyDiscardPopupLockActive()) return false;
        if (openingPresentationInProgress) return false;
        if (weatherForecastInProgress) return false;
        if (activeSpellCastPresentationCount > 0) return false;
        if (playerTurn) return false;
        return true;
    }
    public bool DidPlayerPassTurnThisTurn() { return playerPassedTurnThisTurn; }
    public bool DidEnemyPassTurnThisTurn() { return enemyPassedTurnThisTurn; }

    /// <summary>與 <see cref="PlayerAttack"/> 成功發動條件一致；為 false 時目前無法以場上怪物攻擊（開局、已攻擊、無場怪、敵方凝視、敵場空且尚未解鎖直擊、或非可操作視窗等）。</summary>
    public bool HasPlayerAttackedThisTurn() => playerHasAttackedThisTurn;

    public bool CanPlayerMonsterAttackNow()
    {
        if (!CanPlayerAct()) return false;
        if (IsOpeningRoundAttackBlocked()) return false;
        if (playerHasAttackedThisTurn) return false;
        if (playerField == null) return false;
        if (EnemyLinGazeActive()) return false;
        if (enemyField == null && !playerCanDirectAttackThisTurn) return false;
        return true;
    }

    public bool IsOpeningRoundFireballBlockedForPlayer()
    {
        return IsOpeningRoundFireballBlocked();
    }

    public bool IsOpeningPresentationInProgress() { return openingPresentationInProgress; }
    public bool IsTurnSequenceInProgress() { return turnSequenceInProgress || weatherForecastInProgress; }
    public bool IsSpellCastPresentationActive() { return activeSpellCastPresentationCount > 0 || enemySpellPresentationDepth > 0; }
    public float GetSpellCastPresentationSeconds() { return spellCastPresentationSeconds; }
    public bool IsBattleOver() { return battleOver; }
    public int GetBattleResult() { return battleResult; }
    public string GetBattleRuleMessage() { return battleRuleMessage; }
    public string GetOpeningRollMessage()
    {
        if (Time.unscaledTime > openingRollMessageUntil) return string.Empty;
        return openingRollMessage;
    }
    public int GetOpeningPlayerDice() { return openingPlayerDice; }
    public int GetOpeningEnemyDice() { return openingEnemyDice; }
    public bool IsOpeningPlayerFirst() { return openingPlayerFirst; }
    public int GetOpeningRollVersion() { return openingRollVersion; }
    public float GetOpeningPresentationSeconds() { return openingPresentationSeconds; }
    public void SetOpeningPresentationSeconds(float seconds)
    {
        openingPresentationSeconds = Mathf.Clamp(seconds, 0f, 15f);
    }
    public int GetCurrentRound() { return currentRound; }

    /// <summary>
    /// 本回合誰先結算「結束回合」後的場上怪獸攻擊（甲案：依現行回合順序推導，不改規則）。
    /// 第 1 回合若開場敵方先手則敵方先攻擊；其餘回合為我方先攻擊。
    /// </summary>
    public bool DoesPlayerStrikeFirstThisRound() => currentRound > 1 || openingPlayerFirst;

    public bool DoesEnemyStrikeFirstThisRound() => !DoesPlayerStrikeFirstThisRound();

    public int GetPlayerHeroHp() { return playerHp; }

    #region Harbor combat coach (read-only damage estimates)

    public bool PeekPendingEnemyDirectAttackUnlockForCoach() => pendingEnemyDirectAttackUnlock;

    public int EstimateHarborCoachEnemyFireballRawDamage() =>
        ScaleContextualEnemyDamage(ApplyWeatherSpellPowerBonus(20, false));

    public int EstimateHarborCoachDamageToPlayerMonsterFromRaw(int rawDamage) =>
        Mathf.Max(0, ModifyDamageToPlayerMonster(rawDamage));

    public int EstimateHarborCoachDirectDamageToPlayerHeroFromRaw(int rawDamage) =>
        Mathf.Max(0, ModifyDirectDamageToPlayerHero(rawDamage));

    public int EstimateHarborCoachScaledEnemyAttackToPlayerHero(int attack) =>
        Mathf.Max(0, ScaleContextualEnemyDamage(ModifyDirectDamageToPlayerHero(attack)));

    #endregion

    public int GetEnemyHeroHp() { return enemyHp; }

    public int GetHeroStartHealth() { return startHealth; }

    public int GetBattleSessionId() { return battleSessionId; }

    /// <summary>
    /// 玩家視角的 AI 量化指標（0~100）。
    /// Threat: 當前壓制度（我方是否正在被壓著打）
    /// Burst:  下一回合爆發風險（我方被一波帶走的危險度）
    /// Tempo:  敵方續戰節奏（中長期優勢）
    /// </summary>
    public string GetEnemyAiQuantifiedTextForPlayerView()
    {
        int threat = GetEnemyThreatScoreForPlayerView();
        int burst = GetEnemyBurstRiskScoreForPlayerView();
        int tempo = GetEnemyTempoScoreForPlayerView();
        string tier = GetEnemyPressureTierLabel(threat, burst, tempo);
        return
            "Enemy pressure (" + tier + ")\n" +
            "Threat " + threat + "/100  |  Burst " + burst + "/100  |  Tempo " + tempo + "/100";
    }

    public int GetEnemyThreatScoreForPlayerView()
    {
        float score = 16f;
        if (enemyField != null)
        {
            score += enemyField.attack * 0.72f;
            score += Mathf.Clamp(enemyField.currentHp, 0, 120) * 0.24f;
            if (enemyCanDirectAttackThisTurn) score += 16f;
        }
        if (playerField == null) score += 10f;
        if (EnemyLinGazeActive()) score += 12f;
        if (PlayerLinGazeActive()) score -= 18f;

        int hpLost = Mathf.Max(0, startHealth - playerHp);
        score += hpLost * 1.35f;
        score += Mathf.Clamp(enemyHand.Count, 0, 10) * 2.1f;

        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }

    public int GetEnemyBurstRiskScoreForPlayerView()
    {
        float score = 8f;
        if (enemyField != null && playerField == null)
        {
            score += 22f;
            score += enemyField.attack * 0.55f;
            if (enemyCanDirectAttackThisTurn) score += 18f;
        }

        if (playerHp <= 12) score += (12 - playerHp) * 3.2f;
        score += Mathf.Clamp(enemyHand.Count, 0, 10) * 2.8f;

        // 玩家視角只看「可能性」：手牌越多、我方越空場，越容易被法術補刀。
        if (playerField == null) score += 8f;

        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }

    public int GetEnemyTempoScoreForPlayerView()
    {
        float score = 28f;
        score += Mathf.Clamp(enemyHp, 0, 60) * 1.1f;
        score -= Mathf.Clamp(playerHp, 0, 60) * 0.75f;
        score += Mathf.Clamp(enemyDeck.Count - playerDeck.Count, -10, 10) * 1.8f;
        score += Mathf.Clamp(enemyHand.Count - playerHand.Count, -5, 5) * 3.5f;
        if (enemyField != null) score += 8f;
        if (playerField == null) score += 6f;

        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }

    private static string GetEnemyPressureTierLabel(int threat, int burst, int tempo)
    {
        float weighted = threat * 0.5f + burst * 0.3f + tempo * 0.2f;
        if (weighted >= 80f) return "Severe";
        if (weighted >= 62f) return "High";
        if (weighted >= 40f) return "Medium";
        return "Low";
    }

    public string GetPlayerFieldText()
    {
        string m = playerField == null
            ? "(無怪獸)"
            : DebugFieldMonsterName(playerField) + " ATK " + playerField.attack + " HP " + playerField.currentHp + "/" + playerField.maxHp;
        string g = PlayerLinGazeActive()
            ? "  |  林可的凝視 剩" + playerLinGazeRoundsRemaining + "回合"
            : string.Empty;
        string w = "  |  " + GetWeatherPseudoCardText(true);
        return "Player field: " + m + g + w;
    }

    public string GetEnemyFieldText()
    {
        string baseLine = enemyField == null
            ? "Enemy field: (empty)"
            : "Enemy field: " + DebugFieldMonsterName(enemyField) + " ATK " + enemyField.attack + " HP " + enemyField.currentHp + "/" + enemyField.maxHp;
        return baseLine + "  |  " + GetWeatherPseudoCardText(false);
    }

    public string GetCardPreviewText(Card card)
    {
        if (card == null) return "Empty";
        string dn = card.DebugDisplayName;
        if (card is MonsterCard monster)
            return "[m" + card.id + "] " + dn + "\nMonster  ATK:" + monster.attack + "  HP:" + monster.healthPointMax;
        if (card is SpellCard spell)
            return "[s" + spell.SpellOrdinal + "] " + dn + "\nSpell  " + spell.effect;
        return "[" + card.id + "] " + dn;
    }

    /// <summary>手牌區顯示用：法術不含技能／效果內文（改由長按浮窗顯示）。</summary>
    public string GetCardHandPreviewText(Card card)
    {
        if (card == null) return "Empty";
        string dn = card.DebugDisplayName;
        if (card is MonsterCard monster)
            return "[m" + card.id + "] " + dn + "\nMonster  ATK:" + monster.attack + "  HP:" + monster.healthPointMax;
        if (card is SpellCard spell)
            return "[s" + spell.SpellOrdinal + "] " + dn + "\nSpell";
        return GetCardPreviewText(card);
    }

    /// <summary>法術全屏介紹：與 CardList.csv「效果」欄（<see cref="SpellCard.effect"/>）相同。</summary>
    public string GetSpellEffectTextForPresentation(SpellCard spell)
    {
        if (spell == null) return string.Empty;
        string t = spell.effect != null ? spell.effect.Trim() : string.Empty;
        return string.IsNullOrEmpty(t) ? GetCardSkillDescription(spell) : t;
    }

    public string GetCardSkillDescription(Card card)
    {
        if (card == null) return string.Empty;
        if (card is SpellCard spell)
        {
            switch (spell.SpellOrdinal)
            {
                case 0:
                    return "置於場上：立刻對敵方場上卡牌造成 20 傷害（不超過其剩餘 HP）；無場上卡則對敵方英雄 20。消耗。";
                case 1:
                    return "在手牌點擊：我方場上怪獸回復 40 HP（可溢出，溢出以綠字顯示）。消耗。場上已有怪獸時，僅此法術可從手牌打出。";
                case 2:
                    return "僅在我方場上無怪獸時可發動。置於場上：3 回合每回合全體 -5 HP；期間敵方無法攻擊。3 回合後消失。";
                default:
                    return spell.effect;
            }
        }
        if (card is MonsterCard monster)
        {
            if (MonsterSkillRegistry.TryGetSkillLineB(monster.id, out string lineB))
                return lineB;
            return string.Empty;
        }
        return "No skill description for this card.";
    }

    /// <summary>對戰手牌長按浮窗：完整戰技／效果說明（不含 B 階一行摘要）。</summary>
    public string GetCardHandLongPressTooltipText(Card card)
    {
        return TryGetCardHandLongPressTooltipModel(card, out HandLongPressTooltipModel model)
            ? model.heading + "\n" + model.subtitleRich + "\n" + model.bodyRich
            : string.Empty;
    }

    public bool TryGetCardHandLongPressTooltipModel(Card card, out HandLongPressTooltipModel model)
    {
        model = default;
        if (card == null) return false;
        if (card is MonsterCard monster)
            return MonsterSkillRegistry.TryGetBattleHandLongPressModel(monster.id, out model);
        if (card is SpellCard spell)
        {
            string body = GetSpellEffectTextForPresentation(spell);
            if (string.IsNullOrWhiteSpace(body))
                body = GetCardSkillDescription(spell);
            if (string.IsNullOrWhiteSpace(body)) return false;
            string name = string.IsNullOrWhiteSpace(spell.cardName) ? "法術" : spell.cardName.Trim();
            model = new HandLongPressTooltipModel
            {
                heading = "效果說明",
                subtitleRich = MonsterSkillRegistry.FormatSkillNameRich(name),
                bodyRich = body.Trim()
            };
            return true;
        }
        return false;
    }

    public Card GetPlayerFieldCard()
    {
        return BuildDisplayCardFromField(playerField);
    }

    public Card GetEnemyFieldCard()
    {
        return BuildDisplayCardFromField(enemyField);
    }

    private Card BuildDisplayCardFromField(BattleMonster field)
    {
        if (field == null) return null;

        Card source = cardStore != null ? cardStore.GetCardById(field.id) : null;
        if (source is MonsterCard m)
        {
            MonsterCard display = new MonsterCard(m.id, m.cardName, field.attack, field.maxHp);
            display.healthPoint = field.currentHp;
            display.cardNameEnglish = m.cardNameEnglish;
            display.rarity = m.rarity;
            display.SetArtwork(m.artworkResourcePath, m.artworkSprite);
            display.SetDeckThumb(m.deckThumbResourcePath, m.deckThumbSprite);
            return display;
        }

        MonsterCard fallback = new MonsterCard(field.id, field.cardName, field.attack, Mathf.Max(1, field.maxHp > 0 ? field.maxHp : field.currentHp));
        fallback.healthPoint = field.currentHp;
        if (source != null)
        {
            fallback.cardNameEnglish = source.cardNameEnglish;
            fallback.rarity = source.rarity;
            fallback.SetArtwork(source.artworkResourcePath, source.artworkSprite);
            if (source is MonsterCard mm)
                fallback.SetDeckThumb(mm.deckThumbResourcePath, mm.deckThumbSprite);
        }
        return fallback;
    }

    /// <summary>場上怪獸卡中央狀態徽章（不可攻擊／不可反擊＋副標）。</summary>
    public struct FieldMonsterStatusBadge
    {
        public bool HasValue;
        public string Primary;
        public string Secondary;

        public static FieldMonsterStatusBadge None => default;
    }

    /// <summary>我方場上怪獸：敵方凝視／首回合／本回合已攻擊／本回合已反擊等。</summary>
    public FieldMonsterStatusBadge GetPlayerFieldMonsterStatusBadge()
    {
        if (playerField == null) return FieldMonsterStatusBadge.None;
        if (EnemyLinGazeActive())
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotAttack,
                Secondary = FieldCardStatusIndex.BadgeSecondaryRoundsRemaining(GetEnemyLinGazeRoundsRemaining())
            };
        }
        if (IsOpeningRoundAttackBlocked())
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotAttack,
                Secondary = FieldCardStatusIndex.BadgeSecondaryOpeningRound
            };
        }
        if (playerHasAttackedThisTurn && playerTurn)
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotAttack,
                Secondary = FieldCardStatusIndex.BadgeSecondaryThisTurn
            };
        }
        if (playerCounterUsedThisRound)
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotCounter,
                Secondary = FieldCardStatusIndex.BadgeSecondaryThisTurn
            };
        }
        return FieldMonsterStatusBadge.None;
    }

    /// <summary>敵方場上怪獸：我方凝視／首回合／本回合已反擊等。</summary>
    public FieldMonsterStatusBadge GetEnemyFieldMonsterStatusBadge()
    {
        if (enemyField == null) return FieldMonsterStatusBadge.None;
        if (PlayerLinGazeActive())
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotAttack,
                Secondary = FieldCardStatusIndex.BadgeSecondaryRoundsRemaining(GetPlayerLinGazeRoundsRemaining())
            };
        }
        if (IsOpeningRoundAttackBlocked())
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotAttack,
                Secondary = FieldCardStatusIndex.BadgeSecondaryOpeningRound
            };
        }
        if (enemyCounterUsedThisRound)
        {
            return new FieldMonsterStatusBadge
            {
                HasValue = true,
                Primary = FieldCardStatusIndex.BadgeCannotCounter,
                Secondary = FieldCardStatusIndex.BadgeSecondaryThisTurn
            };
        }
        return FieldMonsterStatusBadge.None;
    }

    // -------- Enemy AI public hooks --------
    public void EnemyDrawOneCard()
    {
        DrawCard(enemyDeck, enemyHand, "Enemy");
    }

    public bool EnemyHasFieldMonster()
    {
        return enemyField != null;
    }

    public bool PlayerHasFieldMonster()
    {
        return playerField != null;
    }

    public void EnemyPlayCardFromHand(int handIndex)
    {
        if (handIndex < 0 || handIndex >= enemyHand.Count) return;

        Card selected = enemyHand[handIndex];
        if (enemyField != null)
        {
            bool lesserHealExempt = selected is SpellCard spField && spField.SpellOrdinal == 1;
            if (!lesserHealExempt)
                return;
        }
        if (selected is SpellCard spellPre && spellPre.SpellOrdinal == 2 && !CanEnemyCastLinGazeNow())
            return;
        if (selected is SpellCard spellHeal && spellHeal.SpellOrdinal == 1 && enemyField == null)
            return;
        if (selected is SpellCard spellFireball && spellFireball.SpellOrdinal == 0 && IsOpeningRoundFireballBlocked())
            return;

        if (selected is SpellCard spCommit)
            SpellCastHandAnchorCommitted?.Invoke(false, spCommit, handIndex);

        enemyHand.RemoveAt(handIndex);

        if (selected is MonsterCard monster)
        {
            if (enemyField != null)
            {
                enemyHand.Insert(handIndex, selected);
                return;
            }
            enemyField = new BattleMonster(monster);
            ApplySummonMonsterSkills(enemyField, false);
            enemyPlacedCardThisRound = true;
            enemyPlayedHandCardThisTurn = true;
            BattleVerbose("Enemy summoned: " + enemyField.cardName);
            NoteEnemySchemingCardPlayed(selected);
            EnemyCardPlayed?.Invoke(selected);
        }
        else if (selected is SpellCard spell)
        {
            StartCoroutine(EnemyResolveSpellAfterPresentation(spell, selected, handIndex));
        }
    }

    private IEnumerator EnemyResolveSpellAfterPresentation(SpellCard spell, Card eventCard, int insertIndexIfFail)
    {
        enemySpellPresentationDepth++;
        try
        {
            bool skipPresent = BattleAutoSimPlugin.IsRunning;
            if (!skipPresent)
            {
                SpellCastPresentationStarted?.Invoke(false, spell.cardName, GetSpellEffectTextForPresentation(spell));
            }
            yield return new WaitForSecondsRealtime(skipPresent ? 0f : spellCastPresentationSeconds);
            bool hadPlayerMonsterForFireball = spell.SpellOrdinal == 0 && playerField != null;
            bool resolved = TryApplyEnemySpell(spell);
            if (!resolved)
            {
                enemyHand.Insert(insertIndexIfFail, spell);
            }
            else
            {
                enemyPlacedCardThisRound = true;
                enemyPlayedHandCardThisTurn = true;
                if (spell.SpellOrdinal != 0 && spell.SpellOrdinal != 1)
                    BattleVerbose("Enemy cast spell: " + spell.cardName);
                NoteEnemySchemingCardPlayed(eventCard);
                EnemyCardPlayed?.Invoke(eventCard);
            }
            deferPlayerFieldUiClearAfterEnemyFireballKill =
                !BattleAutoSimPlugin.IsRunning &&
                resolved &&
                spell.SpellOrdinal == 0 &&
                hadPlayerMonsterForFireball &&
                playerField == null;
            BattleLayoutVisualRefreshRequested?.Invoke();
            if (resolved && spell.SpellOrdinal == 0)
                FireballVisualRequested?.Invoke(false, hadPlayerMonsterForFireball);
            if (resolved && spell.SpellOrdinal == 1)
                EnemyLesserHealVisualRequested?.Invoke();
        }
        finally
        {
            enemySpellPresentationDepth--;
        }
    }

    public void EnemyAttack()
    {
        EnemyAttackIfPossible();
    }

    private void ForceEnemySummonAtStart()
    {
        if (enemyField != null) return;

        for (int i = 0; i < enemyHand.Count; i++)
        {
            if (enemyHand[i] is MonsterCard handMonster)
            {
                enemyField = new BattleMonster(handMonster);
                enemyHand.RemoveAt(i);
                BattleVerbose("Enemy start summon (hand): " + enemyField.cardName);
                return;
            }
        }

        for (int i = 0; i < enemyDeck.Count; i++)
        {
            if (enemyDeck[i] is MonsterCard deckMonster)
            {
                enemyField = new BattleMonster(deckMonster);
                ApplySummonMonsterSkills(enemyField, false);
                enemyDeck.RemoveAt(i);
                BattleVerbose("Enemy start summon (deck): " + enemyField.cardName);
                return;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cardLayout != null) cardLayout.OnValidateInEditor();
        if (cardText != null) cardText.OnValidateInEditor();
        if (cardField != null) cardField.OnValidateInEditor();
    }
#endif
}

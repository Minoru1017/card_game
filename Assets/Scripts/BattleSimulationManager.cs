using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [Range(0.5f, 2.0f)] public float fieldMonsterScale = 1.2f;
    [Range(0.5f, 2.0f)] public float fieldMonsterStatTextScale = 0.85f;

    [Header("Battle Settings")]
    [Tooltip("我方／敵方英雄起始生命")]
    public int startHealth = 20;
    public int startHandCount = 7;
    public int maxHandSize = 7;
    public int enemyDeckSize = 20;
    public bool useFixedEnemyDeck = true;
    public int[] fixedEnemyDeckCardIds = new int[] { 0, 1, -1, 2, -2, 3, -3, 4, 5, 6 };
    public bool forceEnemyStartWithMonster = false;

    [Header("Enemy deck spell mix")]
    [Tooltip("When building a random enemy deck (non-fixed pool), ensure at least this many spell cards if any exist in CardStore.")]
    [Range(0, 20)]
    [SerializeField] private int minEnemySpellsInDeck = 3;

    [Header("Win-rate balance (50–70% target)")]
    [Tooltip("All battles: enemy may keep this many monsters above your deck max ATK/HP before replacement. Higher = stronger enemy (tune ~8–16).")]
    [Range(0, 30)]
    [FormerlySerializedAs("batchSimEnemyOverLimitAllowance")]
    [SerializeField] private int enemyOverLimitAllowance = 12;
    [Tooltip("All battles: if true, skip injecting weak 0 ATK or 0 HP filler monsters when the deck rule would add them.")]
    [FormerlySerializedAs("batchSimSkipZeroStatEnemyFiller")]
    [SerializeField] private bool skipZeroStatEnemyFiller = true;
    [Tooltip("Batch sim auto-player only: chance to play a spell before summoning when field is empty. Tune with enemy allowance for ~50–70% win rate.")]
    [Range(0f, 0.5f)]
    [FormerlySerializedAs("batchSimPlayerSpellFirstChance")]
    [SerializeField] private float autoSimPlayerSpellFirstChance = 0.22f;

    /// <summary>Batch auto-play only: spell-before-monster chance when field is empty.</summary>
    public float AutoSimPlayerSpellFirstChance => autoSimPlayerSpellFirstChance;

    [Header("Debug")]
    public bool autoStartOnPlay = true;
    [Range(0.5f, 2.0f)] public float handCardScale = 1.5f;
    [Range(0f, 80f)] public float handCardSpacing = 10f;
    [Range(0f, 220f)] public float handAreaBaseYOffset = 24f;
    [Range(0.5f, 2.5f)] public float handCardTextScale = 1.0f;
    [Range(0.5f, 2.5f)] public float handCardNameScale = 1.0f;
    [Range(0.5f, 2.0f)] public float handCardBackplateScale = 1.0f;
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
    /// <summary>畫面中央回合浮動提示（略過批次勝率模擬時的顯示，結束戰鬥時仍會隱藏）。</summary>
    public event System.Action<BattleTurnBannerKind> TurnBannerRequested;
    /// <summary>玩家將手牌成功放到場上（怪獸上場，或林可的凝視結算後置於場上）。</summary>
    public event System.Action PlayerCommittedHandCardToFieldFromHand;
    /// <summary>玩家按下結束回合（取消「你的回合」浮窗計時）。</summary>
    public event System.Action PlayerPressedEndTurnForPromptUi;
    /// <summary>我方回合可操作視窗開始（開場骰子結束後先手，或敵方回合結束並抽牌後）；供「你的回合」閒置計時起點。</summary>
    public event System.Action PlayerTurnActionWindowOpenedForPromptUi;

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

    private readonly List<string> battleHistoryLines = new List<string>(128);
    private readonly List<string> pendingDiscardHistoryLines = new List<string>(16);
    private Coroutine discardHistoryFlushRoutine;
    private const float DiscardHistoryFlushDelaySeconds = 0.14f;

    /// <summary>本局累積的對戰歷史（每行一則）；批次模擬不寫入。</summary>
    public string GetBattleHistoryFullText()
    {
        if (battleHistoryLines.Count == 0)
            return "（本局尚無對戰歷史紀錄）";
        return string.Join("\n", battleHistoryLines);
    }

    /// <summary>對戰歷史：寫入清單並印 Console；批次勝率模擬時略過以免洗版。</summary>
    private void LogBattleHistory(string message)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        battleHistoryLines.Add(message);
        BattleVerbose(message);
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

    private string battleToastMessage = string.Empty;
    private float battleToastUntilUnscaled;

    [Header("Debugging")]
    [Tooltip("開啟時才將流程說明與對戰歷史寫入 Console；關閉時僅保留 LogWarning / LogError。")]
    [SerializeField] private bool verboseBattleConsoleLog;

    private void BattleVerbose(string message)
    {
        if (!verboseBattleConsoleLog) return;
        Debug.Log(message);
    }

    void Start()
    {
        EnsureSceneVisualsReady();
        EnsureBattleUIExists();
        ResolveRefs();
        if (autoStartOnPlay) StartBattle();
    }

    private void EnsureSceneVisualsReady()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
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
        BattleSimulationDebugUI ui = Object.FindFirstObjectByType<BattleSimulationDebugUI>();
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

        if (playerData == null) playerData = Object.FindFirstObjectByType<PlayerData>();
        if (cardStore == null) cardStore = Object.FindFirstObjectByType<CardStore>();
        if (cardStore == null && playerData != null && playerData.CardStore != null)
            cardStore = playerData.CardStore;
        if (enemyAI == null) enemyAI = Object.FindFirstObjectByType<EnemyAI>();
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

        battleHistoryLines.Clear();
        pendingDiscardHistoryLines.Clear();
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
        battleRuleMessage = string.Empty;
        NotifyTurnBanner(BattleTurnBannerKind.Hidden);
        ClearPlayerLinGaze();
        ClearEnemyLinGaze();
        battleToastMessage = string.Empty;
        battleToastUntilUnscaled = 0f;
        openingRollMessage = string.Empty;
        openingRollMessageUntil = 0f;
        openingPlayerDice = 0;
        openingEnemyDice = 0;
        openingPlayerFirst = true;
        currentRound = 1;

        BuildPlayerDeck();
        BattleVerbose("BattleSimulation: loaded player deck count = " + playerDeck.Count);
        BuildEnemyDeck();
        Shuffle(playerDeck);
        Shuffle(enemyDeck);

        playerHp = startHealth;
        enemyHp = startHealth;
        int playerDice = Random.Range(1, 7);
        int enemyDice = Random.Range(1, 7);
        while (playerDice == enemyDice)
        {
            playerDice = Random.Range(1, 7);
            enemyDice = Random.Range(1, 7);
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
            int attackerDamage = playerField.attack;
            string attackerName = playerField.cardName;
            enemyField.currentHp -= playerField.attack;
            if (enemyField.currentHp <= 0) enemyField = null;
            LogBattleHistory("我方場地上 怪物牌 " + attackerName + " 對敵方造成" + attackerDamage + " 點傷害");
            bool counterTriggered = false;
            int counterDamage = 0;
            if (enemyField != null && playerField != null && playerField.currentHp > 0 && !enemyCounterUsedThisRound)
            {
                counterDamage = enemyField.attack;
                playerField.currentHp -= counterDamage;
                enemyCounterUsedThisRound = true;
                counterTriggered = true;
                LogBattleHistory("敵方場地上 怪物牌 " + enemyField.cardName + " 反擊了我方場地上 怪物牌 " + playerField.cardName + " 1次");
            }
            AttackPerformed?.Invoke(new AttackVisualData
            {
                attackerIsPlayer = true,
                hasMonsterTarget = true,
                attackerDamage = attackerDamage,
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
            int directDmg = playerField.attack;
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

        EnemyDrawCards(2);
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
        BattleVerbose("Enemy turn: after attack");
        if (!BattleAutoSimPlugin.IsRunning)
            yield return new WaitForSeconds(0.2f);

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
        turnSequenceInProgress = false;
        NotifyPlayerTurnActionWindowOpenedForPromptUi();
    }

    private void EnemyPlayCardIfPossible()
    {
        if (enemyHand.Count == 0) return;

        int chosen = -1;

        // Priority: if enemy field is empty, summon a monster first.
        if (enemyField == null)
        {
            for (int i = 0; i < enemyHand.Count; i++)
            {
                if (enemyHand[i] is MonsterCard)
                {
                    chosen = i;
                    break;
                }
            }
        }

        // Fallback: play first spell.
        if (chosen < 0)
        {
            for (int i = 0; i < enemyHand.Count; i++)
            {
                if (enemyField != null && enemyHand[i] is SpellCard spWhenField && spWhenField.SpellOrdinal != 1)
                    continue;
                if (enemyHand[i] is SpellCard spFireballOpen && spFireballOpen.SpellOrdinal == 0 && IsOpeningRoundFireballBlocked())
                    continue;
                if (enemyHand[i] is SpellCard spTry && spTry.SpellOrdinal == 2 && !CanEnemyCastLinGazeNow())
                    continue;
                if (enemyHand[i] is SpellCard spHeal && spHeal.SpellOrdinal == 1 && enemyField == null)
                    continue;
                if (enemyHand[i] is SpellCard)
                {
                    chosen = i;
                    break;
                }
            }
        }

        if (chosen < 0) return;

        Card selected = enemyHand[chosen];
        enemyHand.RemoveAt(chosen);

        if (selected is MonsterCard monster)
        {
            enemyField = new BattleMonster(monster);
            enemyPlacedCardThisRound = true;
            enemyPlayedHandCardThisTurn = true;
            BattleVerbose("Enemy summoned: " + enemyField.cardName);
        }
        else if (selected is SpellCard spell)
        {
            if (!TryApplyEnemySpell(spell))
            {
                enemyHand.Insert(chosen, selected);
                return;
            }
            enemyPlayedHandCardThisTurn = true;
            BattleVerbose("Enemy cast spell: " + spell.cardName);
        }
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
            playerField.currentHp -= enemyField.attack;
            if (playerField.currentHp <= 0) playerField = null;
            LogBattleHistory("敵方場地上 怪物牌 " + attackerName + " 對我方造成" + attackerDamage + " 點傷害");
            bool counterTriggered = false;
            int counterDamage = 0;
            if (playerField != null && enemyField != null && enemyField.currentHp > 0 && !playerCounterUsedThisRound)
            {
                counterDamage = playerField.attack;
                enemyField.currentHp -= counterDamage;
                playerCounterUsedThisRound = true;
                counterTriggered = true;
                LogBattleHistory("我方場地上 怪物牌 " + playerField.cardName + " 反擊了敵方場地上 怪物牌 " + enemyField.cardName + " 1次");
            }
            AttackPerformed?.Invoke(new AttackVisualData
            {
                attackerIsPlayer = false,
                hasMonsterTarget = true,
                attackerDamage = attackerDamage,
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
            int directDmg = enemyField.attack;
            playerHp -= directDmg;
            LogBattleHistory("敵方場地上 怪物牌 " + enemyField.cardName + " 對我方英雄造成" + directDmg + " 點傷害");
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
        playerHp = Mathf.Max(0, playerHp - amount);
        enemyHp = Mathf.Max(0, enemyHp - amount);
        if (playerField != null)
        {
            playerField.currentHp -= amount;
            if (playerField.currentHp <= 0) playerField = null;
        }
        if (enemyField != null)
        {
            enemyField.currentHp -= amount;
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
        const int dmg = 20;
        int deal;
        if (enemyField != null)
        {
            deal = Mathf.Min(dmg, Mathf.Max(0, enemyField.currentHp));
            enemyField.currentHp -= deal;
            if (enemyField.currentHp <= 0) enemyField = null;
        }
        else
        {
            int before = enemyHp;
            enemyHp = Mathf.Max(0, enemyHp - dmg);
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
        const int healAmount = 40;
        playerField.currentHp += healAmount;
        LogBattleHistory("我方使用了 法術牌 " + spell.cardName + " 對我方回復" + healAmount + "點生命值");
        ShowBattleToast("初級治療：我方場上怪獸 +" + 40 + " HP（目前 " + playerField.currentHp + "／上限 " + playerField.maxHp + "，可溢出）。", 2.8f);
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
                playerHp -= 2;
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
        const int healAmount = 40;
        enemyField.currentHp += healAmount;
        LogBattleHistory("敵方使用了 法術牌 " + spell.cardName + " 對敵方回復" + healAmount + "點生命值");
        ShowBattleToast("初級治療：敵方場上怪獸 +" + 40 + " HP（目前 " + enemyField.currentHp + "／上限 " + enemyField.maxHp + "，可溢出）。", 2.8f);
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
        const int dmg = 20;
        int deal;
        if (playerField != null)
        {
            deal = Mathf.Min(dmg, Mathf.Max(0, playerField.currentHp));
            playerField.currentHp -= deal;
            if (playerField.currentHp <= 0) playerField = null;
        }
        else
        {
            int before = playerHp;
            playerHp = Mathf.Max(0, playerHp - dmg);
            deal = before - playerHp;
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
        var saved = playerData.GetDeckMap(playerData.selectedDeckSlot);
        foreach (var kv in saved)
        {
            int id = kv.Key;
            int count = kv.Value;
            if (count <= 0) continue;

            Card card = cardStore.GetCardById(id);
            if (card == null) continue;
            for (int i = 0; i < count; i++) playerDeck.Add(card);
        }

        if (playerDeck.Count == 0)
        {
            Debug.LogWarning("Player deck is empty.");
        }
    }

    private void BuildEnemyDeck()
    {
        int targetEnemyDeckSize = Mathf.Max(1, GetPlayerSavedDeckCardCount());
        int playerMaxAttack;
        int playerMaxHealth;
        GetPlayerSavedDeckMaxStats(out playerMaxAttack, out playerMaxHealth);
        if (useFixedEnemyDeck && fixedEnemyDeckCardIds != null && fixedEnemyDeckCardIds.Length > 0)
        {
            List<Card> fixedPool = new List<Card>();
            for (int i = 0; i < fixedEnemyDeckCardIds.Length; i++)
            {
                int id = fixedEnemyDeckCardIds[i];
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
            enemyDeck.Add(monsters[Random.Range(0, monsters.Count)]);
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
            else enemyDeck[0] = monsters[Random.Range(0, monsters.Count)];
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
            int ri = monsterIdx[Random.Range(0, monsterIdx.Count)];
            enemyDeck[ri] = spells[Random.Range(0, spells.Count)];
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
                enemyDeck[i] = legalMonsters[Random.Range(0, legalMonsters.Count)];
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
            enemyDeck[i] = zeroStatPool[Random.Range(0, zeroStatPool.Count)];
            needed--;
        }
    }

    private void Shuffle(List<Card> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
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
        if (card is MonsterCard m)
            return m.attack * 2 + m.healthPointMax;
        if (card is SpellCard sp)
        {
            if (sp.SpellOrdinal == 1) return enemyField != null ? 90 : 8;
            if (sp.SpellOrdinal == 0) return enemyField != null ? 55 : 75;
            if (sp.SpellOrdinal == 2) return CanEnemyCastLinGazeNow() ? 62 : 10;
            return 20;
        }
        return 0;
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

        // 平手：①雙方英雄 HP≥1 且皆無牌可打 ②雙方英雄於同一次結算中皆 HP≤0
        if (playerHp >= 1 && enemyHp >= 1 && PlayerHasNoCardsToPlay() && EnemyHasNoCardsToPlay())
        {
            battleOver = true;
            battleResult = 2;
            BattleVerbose("平手：雙方英雄 HP≥1，且皆無牌可打（手牌、牌庫與場上皆無可用牌）。");
            RecordBattleOutcomeHistory();
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            return;
        }
        if (playerHp <= 0 && enemyHp <= 0)
        {
            battleOver = true;
            battleResult = 2;
            BattleVerbose("平手：雙方英雄於同一次結算中生命皆≤0。");
            RecordBattleOutcomeHistory();
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            return;
        }

        // 戰敗：①英雄 HP≤0 ②手牌與場上皆無牌（牌庫可有牌，仍可能因無法上場而戰敗）
        bool playerDefeated = playerHp <= 0 || PlayerHasNoHandAndNoFieldCards();
        bool enemyDefeated = enemyHp <= 0 || EnemyHasNoHandAndNoFieldCards();

        if (playerDefeated && enemyDefeated)
        {
            battleOver = true;
            battleResult = 2;
            BattleVerbose("平手：雙方皆符合戰敗條件（英雄 HP≤0 或手牌＋場上無牌）。");
            RecordBattleOutcomeHistory();
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            return;
        }
        if (playerDefeated)
        {
            battleOver = true;
            battleResult = -1;
            BattleVerbose("我方戰敗：英雄 HP≤0，或手牌與場上無牌。");
            RecordBattleOutcomeHistory();
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
            return;
        }
        if (enemyDefeated)
        {
            battleOver = true;
            battleResult = 1;
            BattleVerbose("我方勝利：敵方符合戰敗條件（英雄 HP≤0 或手牌＋場上無牌）。");
            RecordBattleOutcomeHistory();
            NotifyTurnBanner(BattleTurnBannerKind.Hidden);
        }
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
            " PlayerHand=" + playerHand.Count +
            " EnemyHand=" + enemyHand.Count +
            " PlayerDiscard=" + playerDiscardPile.Count +
            " EnemyDiscard=" + enemyDiscardPile.Count +
            (playerPendingDiscardCount > 0 ? "  [請棄牌 " + playerPendingDiscardCount + "]" : string.Empty) +
            " PlayerField=" + pf + gaze +
            " EnemyField=" + ef;
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
    public bool DidPlayerPassTurnThisTurn() { return playerPassedTurnThisTurn; }
    public bool DidEnemyPassTurnThisTurn() { return enemyPassedTurnThisTurn; }

    /// <summary>與 <see cref="PlayerAttack"/> 成功發動條件一致；為 false 時目前無法以場上怪物攻擊（開局、已攻擊、無場怪、敵方凝視、敵場空且尚未解鎖直擊、或非可操作視窗等）。</summary>
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
    public bool IsTurnSequenceInProgress() { return turnSequenceInProgress; }
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

    public int GetPlayerHeroHp() { return playerHp; }

    public int GetEnemyHeroHp() { return enemyHp; }

    public int GetHeroStartHealth() { return startHealth; }

    public int GetBattleSessionId() { return battleSessionId; }

    public string GetPlayerFieldText()
    {
        string m = playerField == null
            ? "(無怪獸)"
            : DebugFieldMonsterName(playerField) + " ATK " + playerField.attack + " HP " + playerField.currentHp + "/" + playerField.maxHp;
        string g = PlayerLinGazeActive()
            ? "  |  林可的凝視 剩" + playerLinGazeRoundsRemaining + "回合"
            : string.Empty;
        return "Player field: " + m + g;
    }

    public string GetEnemyFieldText()
    {
        if (enemyField == null) return "Enemy field: (empty)";
        return "Enemy field: " + DebugFieldMonsterName(enemyField) + " ATK " + enemyField.attack + " HP " + enemyField.currentHp + "/" + enemyField.maxHp;
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
        if (card is MonsterCard) return "No active skill description for this card.";
        return "No skill description for this card.";
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
            return display;
        }

        MonsterCard fallback = new MonsterCard(field.id, field.cardName, field.attack, Mathf.Max(1, field.maxHp > 0 ? field.maxHp : field.currentHp));
        fallback.healthPoint = field.currentHp;
        return fallback;
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
            enemyPlacedCardThisRound = true;
            enemyPlayedHandCardThisTurn = true;
            BattleVerbose("Enemy summoned: " + enemyField.cardName);
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
                enemyDeck.RemoveAt(i);
                BattleVerbose("Enemy start summon (deck): " + enemyField.cardName);
                return;
            }
        }
    }
}

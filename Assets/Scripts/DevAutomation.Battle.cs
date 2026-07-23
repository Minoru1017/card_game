using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// MCP / Play Mode 對戰自動化（僅 Unity Editor）：直接呼叫 <see cref="BattleSimulationManager"/> API，
/// 不依賴 Game 視窗滑鼠。法術目標、場怪攻擊目標由規則引擎自動判定；祝聖綁定可程式選擇。
/// </summary>
public static partial class DevAutomation
{
#if UNITY_EDITOR
    public const int DefaultBattleAutoPlayMaxPumps = 8000;

    /// <summary>對戰狀態摘要（含手牌索引，供 MCP 選牌）。</summary>
    public static string GetBattleStatus()
    {
        EnsurePlaying();
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle; " + GetStatus();

        var sb = new StringBuilder(768);
        sb.Append("scene=").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        sb.Append(" over=").Append(battle.IsBattleOver());
        sb.Append(" result=").Append(battle.GetBattleResult());
        sb.Append(" round=").Append(battle.GetCurrentRound());
        sb.Append(" playerTurn=").Append(battle.IsPlayerTurn());
        sb.Append(" canAct=").Append(battle.CanPlayerActNow());
        sb.Append(" opening=").Append(battle.IsOpeningPresentationInProgress());
        sb.Append(" turnSeq=").Append(battle.IsTurnSequenceInProgress());
        sb.Append(" spellFx=").Append(battle.IsSpellCastPresentationActive());
        sb.Append(" discardPending=").Append(battle.GetPlayerPendingDiscardCount());
        sb.Append(" inDiscardSelection=").Append(battle.IsPlayerInDiscardSelection());
        if (battle.GetPlayerPendingDiscardCount() > 0)
        {
            int discardIdx = battle.GetRecommendedPlayerDiscardHandIndex();
            Card discardCard = discardIdx >= 0 ? battle.GetPlayerHandCard(discardIdx) : null;
            sb.Append(" nextDiscardIdx=").Append(discardIdx);
            if (discardCard != null)
                sb.Append(" nextDiscard=").Append(discardCard.DebugDisplayName);
        }
        sb.Append(" consecrationChoice=").Append(battle.IsPlayerAwaitingConsecrationBindChoice());
        sb.Append(" attacked=").Append(battle.HasPlayerAttackedThisTurn());
        sb.Append(" canAttack=").Append(battle.CanPlayerMonsterAttackNow());
        int playIdx = battle.GetRecommendedPlayerPlayHandIndex();
        sb.Append(" recommendedPlayIdx=").Append(playIdx);
        if (playIdx >= 0)
        {
            Card playCard = battle.GetPlayerHandCard(playIdx);
            if (playCard != null)
                sb.Append(" recommendedPlay=").Append(playCard.DebugDisplayName);
        }
        sb.Append(" | playerHeroHp=").Append(battle.GetPlayerHeroHp());
        sb.Append(" enemyHeroHp=").Append(battle.GetEnemyHeroHp());
        sb.Append(" | ").Append(battle.GetPlayerFieldText());
        sb.Append(" | ").Append(battle.GetEnemyFieldText());
        sb.Append(" | hand=").Append(DescribePlayerHand(battle));
        return sb.ToString();
    }

    /// <summary>列出玩家手牌索引與可否打出（0-based）。</summary>
    public static string DescribePlayerHand()
    {
        EnsurePlaying();
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle";
        return DescribePlayerHand(battle);
    }

    /// <summary>從手牌打出指定索引（0-based）。</summary>
    public static string PlayHandCard(int handIndex)
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.IsBattleOver())
            return "battle over; result=" + battle.GetBattleResult();
        if (!battle.IsPlayerTurn())
            return "not player turn";
        if (battle.IsPlayerAwaitingConsecrationBindChoice())
            return "awaiting consecration choice — call ResolveConsecrationChoice first";
        if (battle.GetPlayerPendingDiscardCount() > 0)
            return "discard pending — call AutoDiscardOnce first";
        if (handIndex < 0 || handIndex >= battle.GetPlayerHandCount())
            return "invalid handIndex=" + handIndex + "; count=" + battle.GetPlayerHandCount();
        if (!battle.TryValidatePlayerAutoSimHandPlay(handIndex, out string validationError))
            return validationError;
        if (!battle.IsPlayerHandCardPlayableNow(handIndex))
            return "handIndex " + handIndex + " not playable now";

        Card card = battle.GetPlayerHandCard(handIndex);
        string name = card != null ? card.DebugDisplayName : "?";
        bool asyncSpell = battle.PlayerPlayCardFromHand(handIndex);
        return asyncSpell
            ? "cast spell (async): " + name + " idx=" + handIndex
            : "played: " + name + " idx=" + handIndex;
    }

    /// <summary>查入門級 Greedy 建議打出的手牌索引（−1 表示無牌可出）。</summary>
    public static string GetRecommendedPlayHandIndex()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        int index = battle.GetRecommendedPlayerPlayHandIndex();
        if (index < 0)
            return "no playable card";
        Card card = battle.GetPlayerHandCard(index);
        string name = card != null ? card.DebugDisplayName : "?";
        return "recommendedPlayIdx=" + index + " " + name;
    }

    /// <summary>依入門級 Greedy 自動選一張可出的牌並打出；場上已有牌則改為結束回合。</summary>
    public static string PlayBestHandCard()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.ShouldPlayerAutoSimSkipHandPlayBecauseFieldOccupied())
            return EndBattleTurnIfFieldOccupied();
        if (!BattleDevAutoPlay.TryFindBestHandIndex(battle, out int index, out string reason))
            return "no playable card" + (string.IsNullOrEmpty(reason) ? string.Empty : ": " + reason);
        return PlayHandCard(index);
    }

    /// <summary>場上怪獸攻擊（有敵場怪打場怪，否則在允許時直擊英雄）。</summary>
    public static string PlayerAttackNow()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.IsBattleOver())
            return "battle over";
        if (!battle.IsPlayerTurn())
            return "not player turn";
        if (!battle.CanPlayerMonsterAttackNow())
            return "cannot attack now (no field monster, already attacked, lin gaze, or opening round)";
        if (battle.HasPlayerAttackedThisTurn())
            return "already attacked this turn";

        battle.PlayerAttack();
        return "attacked";
    }

    /// <summary>結束我方回合（會先自動棄完超限手牌，再攻擊／換回合）。</summary>
    public static string EndBattleTurn()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.IsBattleOver())
            return "battle over; result=" + battle.GetBattleResult();
        if (!battle.IsPlayerTurn())
            return "not player turn";

        if (battle.GetPlayerPendingDiscardCount() > 0)
        {
            string discard = AutoDiscardAllPending();
            if (battle.GetPlayerPendingDiscardCount() > 0)
                return discard + " | still pending, cannot end turn";
            if (!battle.CanPlayerActNow())
                return discard + " | cannot act after discard (animation or consecration)";

            battle.EndPlayerTurn();
            return discard + " | ended player turn";
        }
        else if (!battle.CanPlayerActNow())
        {
            return "cannot act now (animation, opening, consecration, or discard pending)";
        }

        battle.EndPlayerTurn();
        return "ended player turn";
    }

    /// <summary>場上已有怪獸則結束回合；空場時回傳提示（不自動出牌）。</summary>
    public static string EndBattleTurnIfFieldOccupied()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.IsBattleOver())
            return "battle over; result=" + battle.GetBattleResult();
        if (!battle.IsPlayerTurn())
            return "not player turn";
        if (!battle.ShouldPlayerAutoSimSkipHandPlayBecauseFieldOccupied())
            return "field empty; play a card first or call EndBattleTurn()";

        if (battle.GetPlayerPendingDiscardCount() > 0)
        {
            string discard = AutoDiscardAllPending();
            if (battle.GetPlayerPendingDiscardCount() > 0)
                return "field occupied | " + discard + " | still pending, cannot end turn";
            if (!battle.CanPlayerActNow())
                return "field occupied | " + discard + " | cannot act after discard";
        }
        else if (!battle.CanPlayerActNow())
        {
            return "field occupied | cannot act now (animation, opening, or consecration)";
        }

        battle.EndPlayerTurn();
        return "field occupied | ended player turn";
    }

    /// <summary>下一張建議棄牌的手牌索引（與引擎 <see cref="BattleSimulationManager.GetRecommendedPlayerDiscardHandIndex"/> 相同）。</summary>
    public static string GetRecommendedDiscardHandIndex()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.GetPlayerPendingDiscardCount() <= 0)
            return "no discard pending";
        int index = battle.GetRecommendedPlayerDiscardHandIndex();
        if (index < 0 || index >= battle.GetPlayerHandCount())
            return "no valid discard index";
        Card card = battle.GetPlayerHandCard(index);
        return "index=" + index + " " + (card != null ? card.DebugDisplayName : "?");
    }

    /// <summary>棄掉指定手牌（須處於棄牌階段）。</summary>
    public static string DiscardHandCard(int handIndex)
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.GetPlayerPendingDiscardCount() <= 0)
            return "no discard pending";
        if (handIndex < 0 || handIndex >= battle.GetPlayerHandCount())
            return "invalid handIndex=" + handIndex;

        Card card = battle.GetPlayerHandCard(handIndex);
        string name = card != null ? card.DebugDisplayName : "?";
        if (!battle.PlayerDiscardCardFromHand(handIndex))
            return "discard failed: " + name + " idx=" + handIndex;
        return "discarded: " + name + " idx=" + handIndex + "; remaining=" + battle.GetPlayerPendingDiscardCount();
    }

    /// <summary>棄掉一張建議手牌（手牌超過上限時）。</summary>
    public static string AutoDiscardOnce()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (battle.GetPlayerPendingDiscardCount() <= 0)
            return "no discard pending";

        int index = battle.GetRecommendedPlayerDiscardHandIndex();
        Card card = index >= 0 ? battle.GetPlayerHandCard(index) : null;
        string name = card != null ? card.DebugDisplayName : "?";
        if (!battle.AutoDiscardOneForPlayer())
            return "auto discard failed";
        return "discarded: " + name + " idx=" + index + "; remaining=" + battle.GetPlayerPendingDiscardCount();
    }

    /// <summary>自動棄完所有待棄手牌（上限 8 張防呆）。</summary>
    public static string AutoDiscardAllPending()
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        int pendingStart = battle.GetPlayerPendingDiscardCount();
        if (pendingStart <= 0)
            return "no discard pending";

        var log = new StringBuilder(192);
        int guard = 0;
        int discarded = 0;
        while (battle.GetPlayerPendingDiscardCount() > 0 && guard++ < 8)
        {
            string step = AutoDiscardOnce();
            if (!step.StartsWith("discarded:"))
                return log.Length > 0 ? log + " | " + step : step;

            if (log.Length > 0)
                log.Append(" | ");
            log.Append(step);
            discarded++;
        }

        int remaining = battle.GetPlayerPendingDiscardCount();
        if (remaining > 0)
            return log + " | discard incomplete; remaining=" + remaining;

        return "discarded " + discarded + "/" + pendingStart + " | " + log;
    }

    /// <summary>推進棄牌一步：<paramref name="discardAll"/> 為 true 時一次棄完。</summary>
    public static string TryBattleDiscardStep(bool discardAll = false)
    {
        EnsurePlaying();
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle; " + GetStatus();
        if (battle.IsBattleOver())
            return FormatBattleOver(battle);
        if (!battle.IsPlayerTurn())
            return "not player turn";
        if (battle.GetPlayerPendingDiscardCount() <= 0)
            return "no discard pending";

        return discardAll ? AutoDiscardAllPending() : AutoDiscardOnce();
    }

    /// <summary>祝聖綁定選目標：true=綁定場上主教，false=綁定下一張場怪。</summary>
    public static string ResolveConsecrationChoice(bool bindToCurrentBishop = true)
    {
        EnsurePlaying();
        BattleSimulationManager battle = RequireBattleManager();
        if (!battle.IsPlayerAwaitingConsecrationBindChoice())
            return "not awaiting consecration choice";

        if (bindToCurrentBishop)
            battle.PlayerChooseConsecrationBindToCurrentBishop();
        else
            battle.PlayerChooseConsecrationBindToNextMonster();

        return bindToCurrentBishop ? "consecration: bind current bishop" : "consecration: bind next monster";
    }

    /// <summary>
    /// 推進對戰一步：祝聖 → 棄牌 → 出一張牌。
    /// 若 <paramref name="endTurnIfNoPlay"/> 且無牌可出，則結束回合。
    /// </summary>
    public static string TryBattleStep(int? handIndex = null, bool endTurnIfNoPlay = false)
    {
        EnsurePlaying();
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle; " + GetStatus();
        if (battle.IsBattleOver())
            return FormatBattleOver(battle);

        if (BattleDevAutoPlay.TryGetWaitReason(battle, out string waitReason))
            return waitReason;

        if (!battle.IsPlayerTurn())
            return "waiting: enemy turn";

        if (battle.IsPlayerAwaitingConsecrationBindChoice())
            return ResolveConsecrationChoice(bindToCurrentBishop: true);

        if (battle.GetPlayerPendingDiscardCount() > 0)
            return AutoDiscardOnce();

        if (handIndex.HasValue)
            return PlayHandCard(handIndex.Value);

        string pump = BattleDevAutoPlay.PumpPlayerTurnOnce(battle, endTurnIfIdle: endTurnIfNoPlay);
        if (pump.StartsWith("idle:") && endTurnIfNoPlay)
            return EndBattleTurn();
        return pump;
    }

    /// <summary>
    /// 推進一個完整我方回合：祝聖 → 棄光 → 出一張牌 → 結束回合（與 Win-rate 批次相同節奏，不作弊）。
    /// </summary>
    public static string TryBattlePumpOnce()
    {
        EnsurePlaying();
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            return "no battle; " + GetStatus();
        if (battle.IsBattleOver())
            return FormatBattleOver(battle);
        if (BattleDevAutoPlay.TryGetWaitReason(battle, out string waitReason))
            return waitReason;
        if (!battle.IsPlayerTurn())
            return "waiting: enemy turn";

        return BattleDevAutoPlay.PumpPlayerTurnOnce(battle, endTurnIfIdle: true);
    }

    /// <summary>本回合盡可能出牌後結束回合（等同 <see cref="TryBattlePumpOnce"/>）。</summary>
    public static string TryBattleCompleteTurn(int? preferredHandIndex = null)
    {
        if (preferredHandIndex.HasValue)
        {
            string play = TryBattleStep(preferredHandIndex, endTurnIfNoPlay: false);
            if (!play.StartsWith("played") && !play.StartsWith("cast spell"))
                return play;
            return TryBattlePumpOnce();
        }

        return TryBattlePumpOnce();
    }

    /// <summary>背景協程依規則自動打完本局（不使用 ForceBattleWin）；結束後自動匯出戰報。</summary>
    public static string PlayBattleToEnd(int maxPumps = DefaultBattleAutoPlayMaxPumps)
    {
        EnsurePlaying();
        return DevAutomationBattleHost.Ensure().StartAutoPlay(maxPumps);
    }

    /// <summary>背景協程自動對戰至結束或步數上限（<see cref="PlayBattleToEnd"/> 別名）。</summary>
    public static string StartBattleAutoPlayRoutine(int maxPumps = DefaultBattleAutoPlayMaxPumps) =>
        PlayBattleToEnd(maxPumps);

    public static string GetBattleAutoPlayRoutineStatus()
    {
        if (!Application.isPlaying)
            return "not in play mode";
        DevAutomationBattleHost host = DevAutomationBattleHost.TryGetInstance();
        if (host == null)
            return "idle: auto-play not started | " + GetBattleStatus();
        return host.GetStatusText();
    }

    public static bool IsBattleAutoPlayRoutineRunning()
    {
        if (!Application.isPlaying)
            return false;
        DevAutomationBattleHost host = DevAutomationBattleHost.TryGetInstance();
        return host != null && host.IsRunning;
    }

    private static string FormatBattleOver(BattleSimulationManager battle)
    {
        int result = battle.GetBattleResult();
        string label = result == 1 ? "win" : result == -1 ? "loss" : result == 2 ? "draw" : "unknown";
        return "battle over; result=" + result + " (" + label + ")";
    }

    private static BattleSimulationManager FindBattleManager() =>
        UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();

    private static BattleSimulationManager RequireBattleManager()
    {
        BattleSimulationManager battle = FindBattleManager();
        if (battle == null)
            throw new System.InvalidOperationException("DevAutomation: BattleSimulationManager not found.");
        return battle;
    }

    private static string DescribePlayerHand(BattleSimulationManager battle)
    {
        int count = battle.GetPlayerHandCount();
        if (count <= 0)
            return "(empty)";

        var sb = new StringBuilder(count * 24);
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                sb.Append("; ");
            Card card = battle.GetPlayerHandCard(i);
            sb.Append('[').Append(i).Append("] ");
            sb.Append(card != null ? card.DebugDisplayName : "?");
            sb.Append(battle.IsPlayerHandCardPlayableNow(i) ? "*" : "-");
        }

        return sb.ToString();
    }

    internal static class BattleDevAutoPlay
    {
        internal static bool TryGetWaitReason(BattleSimulationManager battle, out string reason)
        {
            reason = null;
            if (battle == null)
                return false;
            if (battle.IsOpeningPresentationInProgress())
            {
                reason = "waiting: opening presentation";
                return true;
            }

            if (battle.IsTurnSequenceInProgress())
            {
                reason = "waiting: turn sequence";
                return true;
            }

            if (battle.IsSpellCastPresentationActive())
            {
                reason = "waiting: spell presentation";
                return true;
            }

            return false;
        }

        /// <summary>祝聖選擇 → 棄完待棄牌。若已處理則回傳訊息；否則 prepAction=null。</summary>
        internal static string TryPreparePlayerTurn(BattleSimulationManager battle, out string prepAction)
        {
            prepAction = null;
            if (battle == null)
                return "no battle";

            if (battle.IsPlayerAwaitingConsecrationBindChoice())
            {
                battle.PlayerChooseConsecrationBindToCurrentBishop();
                prepAction = "consecration: bind current bishop";
                return prepAction;
            }

            if (battle.GetPlayerPendingDiscardCount() > 0)
            {
                prepAction = AutoDiscardAllPending();
                return prepAction;
            }

            return null;
        }

        /// <summary>入門級 Greedy：場上已有牌則跳過出牌；否則最多出一張後結束回合。</summary>
        internal static string PumpPlayerTurnOnce(BattleSimulationManager battle, bool endTurnIfIdle)
        {
            if (battle.IsBattleOver())
                return FormatBattleOver(battle);

            string prep = TryPreparePlayerTurn(battle, out string prepAction);
            if (prep != null)
                return prepAction;

            if (battle.ShouldPlayerAutoSimSkipHandPlayBecauseFieldOccupied())
                return TryEndPlayerTurnWhenFieldOccupied(battle);

            if (battle.HasPlayerPlayedHandCardThisTurn())
                return TryEndPlayerTurnWhenFieldOccupied(battle);

            bool played = TryPlayOneCard(battle, out string playDetail);

            if (TryGetWaitReason(battle, out string waitReason))
                return played ? playDetail + " | " + waitReason : waitReason;

            if (battle.IsPlayerTurn() && !battle.IsBattleOver() && battle.CanPlayerActNow())
            {
                if (played || endTurnIfIdle)
                {
                    battle.EndPlayerTurn();
                    return played ? playDetail + " | ended player turn" : "pass | ended player turn";
                }
            }

            if (played)
                return playDetail;

            return "idle: no playable card; " + DescribePlayerHand(battle);
        }

        internal static string TryEndPlayerTurnWhenFieldOccupied(BattleSimulationManager battle)
        {
            if (TryGetWaitReason(battle, out string waitReason))
                return "field occupied | " + waitReason;

            if (battle.IsPlayerTurn() && !battle.IsBattleOver() && battle.CanPlayerActNow())
            {
                battle.EndPlayerTurn();
                return "field occupied | ended player turn";
            }

            return "field occupied | cannot act yet";
        }

        internal static bool TryPlayOneCard(BattleSimulationManager battle, out string detail)
        {
            detail = string.Empty;
            if (battle.HasPlayerPlayedHandCardThisTurn())
            {
                detail = "already played hand card this turn";
                return false;
            }

            if (!TryFindBestHandIndex(battle, out int index, out string reason))
            {
                detail = reason;
                return false;
            }

            if (!battle.TryValidatePlayerAutoSimHandPlay(index, out string validationError))
            {
                detail = "play blocked: " + validationError;
                return false;
            }

            Card card = battle.GetPlayerHandCard(index);
            string name = card != null ? card.DebugDisplayName : "?";
            bool asyncSpell = battle.PlayerPlayCardFromHand(index);
            detail = asyncSpell
                ? "cast spell (async): " + name + " idx=" + index
                : "played: " + name + " idx=" + index;
            return true;
        }

        internal static bool TryFindBestHandIndex(
            BattleSimulationManager b,
            out int handIndex,
            out string reason)
        {
            handIndex = -1;
            reason = string.Empty;
            if (b == null || !b.IsPlayerTurn())
            {
                reason = "not player turn";
                return false;
            }

            if (b.GetPlayerPendingDiscardCount() > 0)
            {
                reason = "discard pending";
                return false;
            }

            handIndex = b.GetRecommendedPlayerPlayHandIndex();
            if (handIndex >= 0)
                return true;

            reason = "no legal card";
            return false;
        }
    }

    internal sealed class DevAutomationBattleHost : MonoBehaviour
    {
        private static DevAutomationBattleHost instance;
        private string statusText = "idle";
        private bool isRunning;
        private int pumpsCompleted;

        public bool IsRunning => isRunning;

        public static DevAutomationBattleHost Ensure()
        {
            if (instance != null)
                return instance;
            if (!Application.isPlaying)
                throw new System.InvalidOperationException("DevAutomation: enter Play Mode first.");

            var go = new GameObject("__DevAutomationBattleHost");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DevAutomationBattleHost>();
            return instance;
        }

        public static DevAutomationBattleHost TryGetInstance() => instance;

        public string StartAutoPlay(int maxPumps)
        {
            if (isRunning)
                return "battle auto-play already running; " + statusText;

            maxPumps = Mathf.Max(1, maxPumps);
            pumpsCompleted = 0;
            StartCoroutine(CoAutoPlay(maxPumps));
            return "battle auto-play started; maxPumps=" + maxPumps;
        }

        public string GetStatusText()
        {
            string prefix = isRunning ? "running" : "done";
            return prefix + ": " + statusText + " pumps=" + pumpsCompleted + " | " + GetBattleStatus();
        }

        private IEnumerator CoAutoPlay(int maxPumps)
        {
            isRunning = true;
            BattleSimulationManager battle = null;
            try
            {
                float deadline = Time.realtimeSinceStartup + 1800f;

                while (pumpsCompleted < maxPumps && Time.realtimeSinceStartup < deadline)
                {
                    battle = FindBattleManager();
                    if (battle == null)
                    {
                        statusText = "no battle manager";
                        yield break;
                    }

                    if (battle.IsBattleOver())
                    {
                        statusText = FormatBattleOver(battle);
                        string exported = TryExportBattleRecordAfterAutoPlay(battle);
                        if (!string.IsNullOrEmpty(exported))
                            statusText += " | " + exported;
                        yield break;
                    }

                    if (BattleDevAutoPlay.TryGetWaitReason(battle, out string waitReason))
                    {
                        statusText = waitReason;
                        yield return null;
                        continue;
                    }

                    if (!battle.IsPlayerTurn())
                    {
                        statusText = "waiting: enemy turn";
                        yield return null;
                        continue;
                    }

                    string prep = BattleDevAutoPlay.TryPreparePlayerTurn(battle, out string prepAction);
                    if (prep != null)
                    {
                        statusText = prepAction;
                        pumpsCompleted++;
                        yield return new WaitForSecondsRealtime(0.05f);
                        continue;
                    }

                    statusText = BattleDevAutoPlay.PumpPlayerTurnOnce(battle, endTurnIfIdle: true);
                    pumpsCompleted++;
                    yield return new WaitForSecondsRealtime(0.05f);
                }

                if (battle != null && battle.IsBattleOver())
                {
                    statusText = FormatBattleOver(battle);
                    string exported = TryExportBattleRecordAfterAutoPlay(battle);
                    if (!string.IsNullOrEmpty(exported))
                        statusText += " | " + exported;
                }
                else if (pumpsCompleted >= maxPumps)
                    statusText = "pump limit reached (battle continues); " + GetBattleStatus();
                else
                    statusText = "deadline reached; " + GetBattleStatus();
            }
            finally
            {
                isRunning = false;
            }
        }
    }
#endif
}

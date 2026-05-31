using System.Collections.Generic;
using UnityEngine;

public sealed class HarborCombatCoachHint
{
    public string Key;
    public string RichMessage;
    public HarborCombatHandHighlightMode HighlightMode = HarborCombatHandHighlightMode.None;
}

/// <summary>港灣實戰戰術教練觸發評估與冷卻（HARBOR_COMBAT_COACH_GDD §四）。</summary>
public sealed class HarborCombatCoachAdvisorSession
{
    private readonly Dictionary<string, int> _perBattleCounts = new Dictionary<string, int>();
    private readonly HashSet<string> _perTurnWindow = new HashSet<string>();
    private readonly HashSet<string> _weatherShownThisBattle = new HashSet<string>();
    private readonly Dictionary<string, int> _lastRoundByKey = new Dictionary<string, int>();
    private int _battleStartPlayerHp = -1;

    public void ResetForNewBattle(int playerStartHp)
    {
        _perBattleCounts.Clear();
        _perTurnWindow.Clear();
        _weatherShownThisBattle.Clear();
        _lastRoundByKey.Clear();
        _battleStartPlayerHp = playerStartHp;
    }

    public void ClearTurnWindow() => _perTurnWindow.Clear();

    public void RemoveTurnWindowKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _perTurnWindow.Remove(key);
    }

    public bool TryConsumeHint(string key, int currentRound, HarborCombatCoachHintCooldown cooldown)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        switch (cooldown.Scope)
        {
            case HarborCombatCoachCooldownScope.PerTurnWindow:
                if (_perTurnWindow.Contains(key)) return false;
                _perTurnWindow.Add(key);
                return true;
            case HarborCombatCoachCooldownScope.PerBattle:
                _perBattleCounts.TryGetValue(key, out int count);
                if (count >= cooldown.MaxPerBattle) return false;
                _perBattleCounts[key] = count + 1;
                return true;
            case HarborCombatCoachCooldownScope.PerBattleOnce:
                if (_weatherShownThisBattle.Contains(key)) return false;
                _weatherShownThisBattle.Add(key);
                return true;
            case HarborCombatCoachCooldownScope.EveryNRounds:
                if (_lastRoundByKey.TryGetValue(key, out int lastRound) &&
                    currentRound - lastRound < cooldown.MinRoundGap)
                    return false;
                _lastRoundByKey[key] = currentRound;
                return true;
            default:
                return true;
        }
    }

    public int BattleStartPlayerHp => _battleStartPlayerHp;
}

public enum HarborCombatCoachCooldownScope
{
    PerTurnWindow,
    PerBattle,
    PerBattleOnce,
    EveryNRounds
}

public struct HarborCombatCoachHintCooldown
{
    public HarborCombatCoachCooldownScope Scope;
    public int MaxPerBattle;
    public int MinRoundGap;
}

public static class HarborCombatCoachAdvisor
{
    private const int HandNearCapThreshold = 6;
    private const float HealHpRatio = 0.72f;
    private const float HarborPressureHpDropRatio = 0.35f;

    public static bool TryEvaluate(
        BattleSimulationManager manager,
        HarborCombatCoachAdvisorSession session,
        out HarborCombatCoachHint hint)
    {
        hint = null;
        if (manager == null || session == null) return false;
        if (!HarborCombatCoachPrefs.AreTacticalHintsEnabled()) return false;
        if (!manager.IsPlayerTurn() || manager.IsBattleOver()) return false;
        if (manager.IsOpeningPresentationInProgress()) return false;
        if (manager.IsTurnSequenceInProgress() || manager.IsSpellCastPresentationActive()) return false;

        if (IsDiscardPhase(manager))
            return TryEvaluateDiscardExclusive(manager, out hint);

        BattleDifficultyTier tier = ResolveHarborTier();
        int round = manager.GetCurrentRound();

        var candidates = new List<(int priority, HarborCombatCoachHint candidate, HarborCombatCoachHintCooldown cooldown)>();

        if (TryBuildLethal(manager, session, tier, round, out HarborCombatCoachHint lethal))
        {
            candidates.Add((1, lethal, new HarborCombatCoachHintCooldown
            {
                Scope = HarborCombatCoachCooldownScope.PerBattle,
                MaxPerBattle = 2
            }));
        }

        if (TryBuildDiscard(manager, out HarborCombatCoachHint discard))
        {
            candidates.Add((2, discard, new HarborCombatCoachHintCooldown
            {
                Scope = HarborCombatCoachCooldownScope.PerTurnWindow
            }));
        }

        if (TryBuildWeather(manager, session, out HarborCombatCoachHint weather))
        {
            candidates.Add((3, weather, new HarborCombatCoachHintCooldown
            {
                Scope = HarborCombatCoachCooldownScope.PerBattleOnce
            }));
        }

        if (TryBuildHandNearCap(manager, out HarborCombatCoachHint handCap))
        {
            candidates.Add((4, handCap, new HarborCombatCoachHintCooldown
            {
                Scope = HarborCombatCoachCooldownScope.PerBattle,
                MaxPerBattle = 1
            }));
        }

        if (tier != BattleDifficultyTier.Hard)
            AddP1Candidates(manager, session, tier, round, candidates);

        candidates.Sort((a, b) => a.priority.CompareTo(b.priority));
        for (int i = 0; i < candidates.Count; i++)
        {
            var entry = candidates[i];
            if (!session.TryConsumeHint(entry.candidate.Key, round, entry.cooldown)) continue;
            hint = entry.candidate;
            return true;
        }

        return false;
    }

    private static void AddP1Candidates(
        BattleSimulationManager manager,
        HarborCombatCoachAdvisorSession session,
        BattleDifficultyTier tier,
        int round,
        List<(int priority, HarborCombatCoachHint candidate, HarborCombatCoachHintCooldown cooldown)> candidates)
    {
        if (tier == BattleDifficultyTier.Easy)
        {
            if (TryBuildHarborPressure(manager, session, round, out HarborCombatCoachHint pressure))
            {
                candidates.Add((8, pressure, new HarborCombatCoachHintCooldown
                {
                    Scope = HarborCombatCoachCooldownScope.PerBattle,
                    MaxPerBattle = 1
                }));
            }

            if (TryBuildHeal(manager, out HarborCombatCoachHint heal))
            {
                candidates.Add((7, heal, new HarborCombatCoachHintCooldown
                {
                    Scope = HarborCombatCoachCooldownScope.PerBattle,
                    MaxPerBattle = 1
                }));
            }
        }
        else if (tier == BattleDifficultyTier.Normal)
        {
            if (TryBuildThreatField(manager, out HarborCombatCoachHint threat))
            {
                candidates.Add((5, threat, new HarborCombatCoachHintCooldown
                {
                    Scope = HarborCombatCoachCooldownScope.PerBattle,
                    MaxPerBattle = 1
                }));
            }

            if (TryBuildNoField(manager, round, out HarborCombatCoachHint noField))
            {
                candidates.Add((6, noField, new HarborCombatCoachHintCooldown
                {
                    Scope = HarborCombatCoachCooldownScope.EveryNRounds,
                    MinRoundGap = 2
                }));
            }

            if (TryBuildHeal(manager, out HarborCombatCoachHint heal))
            {
                candidates.Add((7, heal, new HarborCombatCoachHintCooldown
                {
                    Scope = HarborCombatCoachCooldownScope.PerBattle,
                    MaxPerBattle = 1
                }));
            }

            if (TryBuildHarborPressure(manager, session, round, out HarborCombatCoachHint pressure))
            {
                candidates.Add((8, pressure, new HarborCombatCoachHintCooldown
                {
                    Scope = HarborCombatCoachCooldownScope.PerBattle,
                    MaxPerBattle = 1
                }));
            }
        }
    }

    private static bool IsDiscardPhase(BattleSimulationManager manager) =>
        manager != null &&
        (manager.IsPlayerInDiscardSelection() || manager.GetPlayerPendingDiscardCount() > 0);

    /// <summary>棄牌階段獨佔教練：不與致死／出牌等提示並存。</summary>
    private static bool TryEvaluateDiscardExclusive(
        BattleSimulationManager manager,
        out HarborCombatCoachHint hint)
    {
        hint = null;
        if (!TryBuildDiscard(manager, out HarborCombatCoachHint discard)) return false;
        hint = discard;
        return true;
    }

    private static bool TryBuildLethal(
        BattleSimulationManager manager,
        HarborCombatCoachAdvisorSession session,
        BattleDifficultyTier tier,
        int round,
        out HarborCombatCoachHint hint)
    {
        hint = null;
        HarborCombatLethalThreatEstimator.Result estimate =
            HarborCombatLethalThreatEstimator.Evaluate(manager, tier);
        if (!estimate.ShouldWarn) return false;

        int dmg = Mathf.Max(1, estimate.ThreatDamageMax);
        string extra = estimate.PrimarySpellDirect ? "注意直傷" : "解場或出怪";
        hint = new HarborCombatCoachHint
        {
            Key = "lethal_next_turn",
            RichMessage =
                "下回合敵方可能造成約" + StoryTextStyle.Em(dmg.ToString()) + "點傷害 優先解場治療或出怪擋 " + extra,
            HighlightMode = HarborCombatHandHighlightMode.LethalResponse
        };
        return true;
    }

    private static bool TryBuildDiscard(BattleSimulationManager manager, out HarborCombatCoachHint hint)
    {
        hint = null;
        int pending = manager.GetPlayerPendingDiscardCount();
        if (pending <= 0 && !manager.IsPlayerInDiscardSelection()) return false;

        if (pending <= 0) pending = 1;
        string message = pending <= 1
            ? "手牌超過上限 長按不要的牌拖到左側棄牌區"
            : "手牌超過上限" + pending + "張 長按不要的牌拖到左側棄牌區";

        hint = new HarborCombatCoachHint
        {
            Key = "discard_required",
            RichMessage = message,
            HighlightMode = HarborCombatHandHighlightMode.DiscardRecommend
        };
        return true;
    }

    private static bool TryBuildWeather(
        BattleSimulationManager manager,
        HarborCombatCoachAdvisorSession session,
        out HarborCombatCoachHint hint)
    {
        hint = null;
        if (!manager.IsWeatherSystemEnabledForBattle()) return false;

        int kind = manager.GetCurrentWeatherKindForUi();
        if (kind <= 0) return false;

        string key = kind switch
        {
            1 => "weather_fire_rain",
            2 => "weather_holy_light",
            3 => "weather_fog",
            4 => "weather_gale",
            _ => null
        };

        if (string.IsNullOrEmpty(key)) return false;

        string message = key switch
        {
            "weather_fire_rain" => "緋焰時雨生效 回合結束場上怪各受" + StoryTextStyle.Em("5") + "傷 先想清楚要不要解場",
            "weather_holy_light" => "月華聖祈生效 本回合治療加" + StoryTextStyle.Em("10") + " 有治療或場怪時值得現在打",
            "weather_fog" => "蒼潮夜湧生效 直傷英雄減半 有時打英雄比硬解怪划算",
            "weather_gale" => "朔風森詠生效 本回合第一張法術加" + StoryTextStyle.Em("20") + " 可現在打或囤給關鍵法術",
            _ => null
        };

        if (message == null) return false;

        hint = new HarborCombatCoachHint
        {
            Key = key,
            RichMessage = message,
            HighlightMode = key == "weather_holy_light" || key == "weather_gale"
                ? HarborCombatHandHighlightMode.PlayRecommend
                : HarborCombatHandHighlightMode.None
        };
        return true;
    }

    private static bool TryBuildHandNearCap(BattleSimulationManager manager, out HarborCombatCoachHint hint)
    {
        hint = null;
        if (manager.IsPlayerInDiscardSelection() || manager.GetPlayerPendingDiscardCount() > 0) return false;
        if (manager.GetPlayerHandCount() < HandNearCapThreshold) return false;

        hint = new HarborCombatCoachHint
        {
            Key = "hand_near_cap",
            RichMessage = "手牌接近上限 先打掉低價值牌 避免被迫棄掉關鍵牌",
            HighlightMode = HarborCombatHandHighlightMode.None
        };
        return true;
    }

    private static bool TryBuildThreatField(BattleSimulationManager manager, out HarborCombatCoachHint hint)
    {
        hint = null;
        MonsterCard enemy = manager.GetEnemyFieldCard() as MonsterCard;
        if (enemy == null) return false;
        if (!HandHasPlayableFireball(manager)) return false;

        MonsterCard player = manager.GetPlayerFieldCard() as MonsterCard;
        int enemyThreat = enemy.attack + enemy.healthPoint;
        int playerThreat = player != null ? player.attack + player.healthPoint : 0;
        if (player != null && enemyThreat < playerThreat + 8) return false;

        hint = new HarborCombatCoachHint
        {
            Key = "threat_field",
            RichMessage = "敵方場上怪壓力大 用" + StoryTextStyle.Em("火球術") + "拆場或先出怪換血",
            HighlightMode = HarborCombatHandHighlightMode.PlayRecommend
        };
        return true;
    }

    private static bool TryBuildNoField(BattleSimulationManager manager, int round, out HarborCombatCoachHint hint)
    {
        hint = null;
        if (round <= 1 || manager.GetPlayerFieldCard() != null) return false;
        if (!HandHasPlayableMonster(manager)) return false;

        hint = new HarborCombatCoachHint
        {
            Key = "no_field_before_end",
            RichMessage = "場上還沒怪 先出一隻再結束回合 下回合才能穩定攻擊",
            HighlightMode = HarborCombatHandHighlightMode.PlayRecommend
        };
        return true;
    }

    private static bool TryBuildHeal(BattleSimulationManager manager, out HarborCombatCoachHint hint)
    {
        hint = null;
        if (manager.GetPlayerFieldCard() == null) return false;

        int maxHp = manager.GetHeroStartHealth();
        if (manager.GetPlayerHeroHp() > Mathf.RoundToInt(maxHp * HealHpRatio)) return false;
        if (!HandHasPlayableHeal(manager)) return false;

        hint = new HarborCombatCoachHint
        {
            Key = "heal_before_end",
            RichMessage = "血量偏低 場上有怪時可先打初級治療",
            HighlightMode = HarborCombatHandHighlightMode.PlayRecommend
        };
        return true;
    }

    private static bool TryBuildHarborPressure(
        BattleSimulationManager manager,
        HarborCombatCoachAdvisorSession session,
        int round,
        out HarborCombatCoachHint hint)
    {
        hint = null;
        if (round < 3 || round > 6) return false;
        if (session.BattleStartPlayerHp <= 0) return false;

        int maxHp = manager.GetHeroStartHealth();
        int drop = session.BattleStartPlayerHp - manager.GetPlayerHeroHp();
        if (drop < Mathf.RoundToInt(maxHp * HarborPressureHpDropRatio)) return false;

        hint = new HarborCombatCoachHint
        {
            Key = "harbor_pressure",
            RichMessage = "港灣訓練壓力上來了 多用防守牌和法術別只換血",
            HighlightMode = HarborCombatHandHighlightMode.PlayRecommend
        };
        return true;
    }

    private static bool HandHasPlayableFireball(BattleSimulationManager manager)
    {
        int count = manager.GetPlayerHandCount();
        for (int i = 0; i < count; i++)
        {
            Card card = manager.GetPlayerHandCard(i);
            if (card is SpellCard sp && sp.SpellOrdinal == 0 &&
                !manager.IsOpeningRoundFireballBlockedForPlayer() &&
                (manager.GetPlayerFieldCard() == null || manager.GetEnemyFieldCard() != null))
                return true;
        }

        return false;
    }

    private static bool HandHasPlayableHeal(BattleSimulationManager manager)
    {
        if (manager.GetPlayerFieldCard() == null) return false;
        int count = manager.GetPlayerHandCount();
        for (int i = 0; i < count; i++)
        {
            if (manager.GetPlayerHandCard(i) is SpellCard sp && sp.SpellOrdinal == 1)
                return true;
        }

        return false;
    }

    private static bool HandHasPlayableMonster(BattleSimulationManager manager)
    {
        if (manager.GetPlayerFieldCard() != null) return false;
        int count = manager.GetPlayerHandCount();
        for (int i = 0; i < count; i++)
        {
            if (manager.GetPlayerHandCard(i) is MonsterCard)
                return true;
        }

        return false;
    }

    public static BattleDifficultyTier ResolveHarborTier() =>
        HarborTrainingBattleCopy.TierFromLabelZh(BattleLaunchContext.ResolveForBattleRecord());
}

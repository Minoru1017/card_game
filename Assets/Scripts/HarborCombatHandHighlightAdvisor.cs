using System.Collections.Generic;
using UnityEngine;

public enum HarborCombatHandHighlightMode
{
    None,
    PlayRecommend,
    DiscardRecommend,
    LethalResponse
}

/// <summary>港灣實戰：依教練 hintKey 推薦手牌高亮索引。</summary>
public static class HarborCombatHandHighlightAdvisor
{
    public static bool TryGetHighlightedHandIndices(
        BattleSimulationManager manager,
        string hintKey,
        List<int> output)
    {
        output.Clear();
        if (manager == null || string.IsNullOrWhiteSpace(hintKey)) return false;
        if (!HarborCombatCoachUi.ShouldAllowHandHighlight()) return false;

        switch (hintKey)
        {
            case "discard_required":
                return TryDiscard(manager, output);
            case "lethal_next_turn":
                return TryLethalResponse(manager, output);
            case "threat_field":
                return TryFireball(manager, output);
            case "no_field_before_end":
                return TrySummonMonster(manager, output);
            case "heal_before_end":
                return TryHeal(manager, output);
            case "harbor_pressure":
                return TryDefensive(manager, output);
            case "weather_holy_light":
                return TryHeal(manager, output) || TrySummonMonster(manager, output);
            case "weather_gale":
                return TrySpellInHand(manager, output);
            default:
                return false;
        }
    }

    private static bool TryDiscard(BattleSimulationManager manager, List<int> output)
    {
        int index = manager.GetRecommendedPlayerDiscardHandIndex();
        if (index < 0 || index >= manager.GetPlayerHandCount()) return false;
        output.Add(index);
        return true;
    }

    private static bool TryLethalResponse(BattleSimulationManager manager, List<int> output)
    {
        if (TryHeal(manager, output)) return true;
        if (TryFireball(manager, output)) return true;
        return TrySummonMonster(manager, output);
    }

    private static bool TryFireball(BattleSimulationManager manager, List<int> output)
    {
        if (manager.GetEnemyFieldCard() == null) return false;
        return TryHandIndexWithSpellOrdinal(manager, 0, output);
    }

    private static bool TryHeal(BattleSimulationManager manager, List<int> output)
    {
        if (manager.GetPlayerFieldCard() == null) return false;
        return TryHandIndexWithSpellOrdinal(manager, 1, output);
    }

    private static bool TrySummonMonster(BattleSimulationManager manager, List<int> output)
    {
        if (manager.GetPlayerFieldCard() != null) return false;

        int best = -1;
        int bestScore = int.MinValue;
        int handCount = manager.GetPlayerHandCount();
        for (int i = 0; i < handCount; i++)
        {
            Card card = manager.GetPlayerHandCard(i);
            if (card is not MonsterCard monster) continue;
            if (!CanPlayMonsterNow(manager)) continue;
            int score = monster.attack * 2 + monster.healthPointMax;
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        if (best < 0) return false;
        output.Add(best);
        return true;
    }

    private static bool TryDefensive(BattleSimulationManager manager, List<int> output)
    {
        if (TryHeal(manager, output)) return true;
        if (TryFireball(manager, output)) return true;
        return TrySpellInHand(manager, output);
    }

    private static bool TrySpellInHand(BattleSimulationManager manager, List<int> output)
    {
        int handCount = manager.GetPlayerHandCount();
        for (int i = 0; i < handCount; i++)
        {
            if (manager.GetPlayerHandCard(i) is SpellCard)
            {
                output.Add(i);
                return true;
            }
        }

        return false;
    }

    private static bool TryHandIndexWithSpellOrdinal(
        BattleSimulationManager manager,
        int ordinal,
        List<int> output)
    {
        int handCount = manager.GetPlayerHandCount();
        for (int i = 0; i < handCount; i++)
        {
            Card card = manager.GetPlayerHandCard(i);
            if (card is SpellCard sp && sp.SpellOrdinal == ordinal && !IsUnplayable(manager, card))
            {
                output.Add(i);
                return true;
            }
        }

        return false;
    }

    private static bool CanPlayMonsterNow(BattleSimulationManager manager) =>
        manager.GetPlayerFieldCard() == null;

    private static bool IsUnplayable(BattleSimulationManager manager, Card card)
    {
        if (card == null) return true;
        if (manager.IsOpeningRoundFireballBlockedForPlayer() &&
            card is SpellCard fireball &&
            fireball.SpellOrdinal == 0)
            return true;

        if (manager.PlayerHasFieldMonster())
        {
            if (card is SpellCard heal && heal.SpellOrdinal == 1) return false;
            return card is not SpellCard;
        }

        if (card is SpellCard sp)
        {
            if (sp.SpellOrdinal == 1) return true;
            if (sp.SpellOrdinal == 2 && !manager.CanPlayerCastLinGazeNow()) return true;
        }

        return false;
    }
}

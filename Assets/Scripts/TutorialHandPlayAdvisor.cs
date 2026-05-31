using System.Collections.Generic;
using UnityEngine;

/// <summary>1-1 教學戰：依生存與限時獲勝目標，推薦當回合應打出的手牌索引。</summary>
public static class TutorialHandPlayAdvisor
{
    private const float RecommendScoreRatio = 0.88f;

    public static bool TryGetRecommendedHandIndices(BattleSimulationManager manager, List<int> output)
    {
        output.Clear();
        if (manager == null || !TutorialBattleCoachUi.IsActiveForCurrentBattle) return false;
        if (!manager.IsPlayerTurn() || manager.IsBattleOver()) return false;
        if (manager.IsOpeningPresentationInProgress()) return false;
        if (manager.IsTurnSequenceInProgress() || manager.IsSpellCastPresentationActive()) return false;
        if (manager.IsPlayerInDiscardSelection() || manager.GetPlayerPendingDiscardCount() > 0) return false;

        Card playerField = manager.GetPlayerFieldCard();
        if (playerField != null)
            return TryRecommendWithFieldMonster(manager, output);

        return TryRecommendEmptyField(manager, output);
    }

    private static bool TryRecommendWithFieldMonster(BattleSimulationManager manager, List<int> output)
    {
        return TryRecommendHealOnly(manager, output, urgentOnly: false);
    }

    private static bool TryRecommendEmptyField(BattleSimulationManager manager, List<int> output)
    {
        int bestScore = int.MinValue;
        var scores = new List<(int index, int score)>();

        int handCount = manager.GetPlayerHandCount();
        for (int i = 0; i < handCount; i++)
        {
            Card card = manager.GetPlayerHandCard(i);
            if (IsHandCardUnplayableNow(manager, card)) continue;

            int score = ScoreCardForEmptyFieldPlay(manager, card);
            if (score < 0) continue;

            scores.Add((i, score));
            if (score > bestScore) bestScore = score;
        }

        if (bestScore == int.MinValue) return false;

        int threshold = Mathf.RoundToInt(bestScore * RecommendScoreRatio);
        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].score >= threshold)
                output.Add(scores[i].index);
        }

        return output.Count > 0;
    }

    private static bool TryRecommendHealOnly(BattleSimulationManager manager, List<int> output, bool urgentOnly)
    {
        int maxHp = manager.GetHeroStartHealth();
        int playerHp = manager.GetPlayerHeroHp();
        bool wantHeal = !urgentOnly || playerHp <= Mathf.RoundToInt(maxHp * 0.72f);

        int bestHealIndex = -1;
        int bestHealScore = int.MinValue;
        int handCount = manager.GetPlayerHandCount();
        for (int i = 0; i < handCount; i++)
        {
            Card card = manager.GetPlayerHandCard(i);
            if (card is not SpellCard sp || sp.SpellOrdinal != 1) continue;
            if (IsHandCardUnplayableNow(manager, card)) continue;

            int score = ScoreHeal(manager);
            if (score > bestHealScore)
            {
                bestHealScore = score;
                bestHealIndex = i;
            }
        }

        if (bestHealIndex < 0) return false;
        if (!wantHeal && bestHealScore < 120) return false;

        output.Add(bestHealIndex);
        return true;
    }

    private static int ScoreCardForEmptyFieldPlay(BattleSimulationManager manager, Card card)
    {
        int maxHp = manager.GetHeroStartHealth();
        int playerHp = manager.GetPlayerHeroHp();
        int enemyHp = manager.GetEnemyHeroHp();
        Card enemyField = manager.GetEnemyFieldCard();
        int round = manager.GetCurrentRound();
        bool fireballBlocked = manager.IsOpeningRoundFireballBlockedForPlayer();

        int handPressureBonus = manager.GetPlayerHandCount() >= 6 ? 18 : 0;

        if (card is MonsterCard monster)
        {
            int score = monster.attack * 3 + monster.healthPointMax * 2 + CardRarityUtility.GetPlayAndKeepBonus(monster.rarity) + handPressureBonus;
            if (enemyField == null) score += 24;
            if (enemyHp <= maxHp / 2) score += 18;
            if (round >= IntroTutorialBattleRules.MaxRoundsInclusive - 3) score += 22;
            if (playerHp <= Mathf.RoundToInt(maxHp * 0.45f)) score += 8;
            return score;
        }

        if (card is SpellCard spell)
        {
            switch (spell.SpellOrdinal)
            {
                case 0:
                    if (fireballBlocked) return -1;
                    if (enemyField is MonsterCard em)
                        return 130 + em.attack * 2 + em.healthPointMax + handPressureBonus;
                    return 95 + Mathf.Max(0, maxHp - enemyHp) + handPressureBonus;
                case 1:
                    return -1;
                default:
                    return -1;
            }
        }

        return -1;
    }

    private static int ScoreHeal(BattleSimulationManager manager)
    {
        int maxHp = manager.GetHeroStartHealth();
        int playerHp = manager.GetPlayerHeroHp();
        int missing = Mathf.Max(0, maxHp - playerHp);
        if (missing <= 0) return 40;
        return 80 + missing * 6;
    }

    private static bool IsHandCardUnplayableNow(BattleSimulationManager manager, Card card)
    {
        if (card == null) return true;

        if (manager.IsOpeningRoundFireballBlockedForPlayer() &&
            card is SpellCard fireball &&
            fireball.SpellOrdinal == 0)
            return true;

        if (manager.PlayerHasFieldMonster())
        {
            if (card is SpellCard heal && heal.SpellOrdinal == 1) return false;
            return true;
        }

        if (card is SpellCard sp)
        {
            if (sp.SpellOrdinal == 1) return true;
            if (sp.SpellOrdinal == 2 && !manager.CanPlayerCastLinGazeNow()) return true;
        }

        return false;
    }

}

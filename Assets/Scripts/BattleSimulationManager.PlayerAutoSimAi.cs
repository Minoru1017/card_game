/// <summary>
/// 我方自動化／批次模擬出牌 AI（對標入門檔敵方 Greedy + IntroGreedy 法術 −14）。
/// 供 <see cref="BattleAutoSimPlugin"/> 與 <see cref="DevAutomation"/> 共用。
/// </summary>
using UnityEngine;

public partial class BattleSimulationManager
{
    /// <summary>對標入門檔 IntroGreedy：法術評分略降，偏先出怪（DIFFICULTY_AND_AI_DESIGN.md §2.4）。</summary>
    private const int PlayerAutoSimIntroSpellPriorityTweak = -14;

    /// <summary>下一張建議打出的手牌索引（入門級 Greedy；無牌可出時 −1）。</summary>
    public int GetRecommendedPlayerPlayHandIndex()
    {
        if (ShouldPlayerAutoSimSkipHandPlayBecauseFieldOccupied())
            return -1;
        if (HasPlayerPlayedHandCardThisTurn())
            return -1;
        return ChoosePlayerHandCardToPlayIndexForAutoSim();
    }

    /// <summary>自動化出牌前驗證：索引合法、牌仍在手牌且與 UI 規則一致。</summary>
    public bool TryValidatePlayerAutoSimHandPlay(int handIndex, out string reason)
    {
        reason = null;
        if (handIndex < 0 || handIndex >= playerHand.Count)
        {
            reason = "invalid handIndex=" + handIndex;
            return false;
        }

        if (HasPlayerPlayedHandCardThisTurn())
        {
            reason = "already played a hand card this turn";
            return false;
        }

        Card card = playerHand[handIndex];
        if (card == null)
        {
            reason = "hand card is null at index " + handIndex;
            return false;
        }

        if (!IsPlayerHandCardPlayableNow(handIndex))
        {
            reason = "handIndex " + handIndex + " not playable now (" + card.DebugDisplayName + ")";
            return false;
        }

        return true;
    }

    /// <summary>我方自動化 AI：戰術換場斬殺 &gt; 空場先怪 &gt; 祝聖綁定 &gt; 評分最高。</summary>
    public int ChoosePlayerHandCardToPlayIndexForAutoSim()
    {
        if (playerHand.Count == 0) return -1;

        int lethal = TryPickPlayerFieldTradeMonsterIndex();
        if (lethal >= 0) return lethal;

        System.Predicate<Card> isMonster = c => c is MonsterCard;

        if (playerField == null)
        {
            int monster = PickBestPlayerHandIndexForAutoSim(c => isMonster(c));
            if (monster >= 0) return monster;
        }
        else if (CanReplaceFieldMonsterForConsecration(true))
        {
            int bind = PickBestPlayerConsecrationBindHandIndex();
            if (bind >= 0) return bind;
        }

        return PickBestPlayerHandIndexForAutoSim(null);
    }

    private int PickBestPlayerHandIndexForAutoSim(System.Predicate<Card> includeCard)
    {
        int chosen = -1;
        int bestPriority = int.MinValue;
        for (int i = 0; i < playerHand.Count; i++)
        {
            if (!IsPlayerHandCardPlayableNow(i)) continue;
            Card c = playerHand[i];
            if (includeCard != null && !includeCard(c)) continue;
            int priority = EvaluatePlayerCardPlayPriorityForAutoSim(c);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                chosen = i;
            }
        }
        return chosen;
    }

    private int TryPickPlayerFieldTradeMonsterIndex()
    {
        if (BattleLaunchContext.IsIntroTutorialBattle)
            return -1;

        MonsterCard enemyMonster = GetEnemyFieldCard() as MonsterCard;
        if (playerField != null || enemyMonster == null) return -1;

        int targetHp = enemyMonster.healthPoint;
        int lethalIndex = -1;
        int bestRarityRank = -1;
        int bestAttack = -1;
        for (int i = 0; i < playerHand.Count; i++)
        {
            if (!(playerHand[i] is MonsterCard playerMonster)) continue;
            if (!IsPlayerHandCardPlayableNow(i)) continue;
            if (playerMonster.attack < targetHp) continue;
            int rank = CardRarityUtility.GetRank(playerMonster.rarity);
            if (rank > bestRarityRank || (rank == bestRarityRank && playerMonster.attack > bestAttack))
            {
                bestRarityRank = rank;
                bestAttack = playerMonster.attack;
                lethalIndex = i;
            }
        }
        return lethalIndex;
    }

    private int PickBestPlayerConsecrationBindHandIndex()
    {
        int chosen = -1;
        int best = int.MinValue;
        for (int i = 0; i < playerHand.Count; i++)
        {
            if (!(playerHand[i] is MonsterCard m)) continue;
            if (!IsPlayerHandCardPlayableNow(i)) continue;
            int priority = EvaluateEnemyConsecrationBindMonsterBonus(m) + GetPlayerAutoSimPlayRarityBonus(m.rarity);
            if (priority > best)
            {
                best = priority;
                chosen = i;
            }
        }
        return chosen;
    }

    private bool PlayerHandHasLesserHeal()
    {
        for (int i = 0; i < playerHand.Count; i++)
        {
            if (playerHand[i] is SpellCard sp && sp.SpellOrdinal == 1)
                return true;
        }
        return false;
    }

    private static int GetPlayerAutoSimPlayRarityBonus(CardRarity rarity) =>
        CardRarityUtility.GetPlayAndKeepBonus(rarity);

    /// <summary>我方入門級自動出牌優先度（鏡像敵方 Greedy，含戰技加權）。</summary>
    private int EvaluatePlayerCardPlayPriorityForAutoSim(Card card)
    {
        if (card == null) return int.MinValue;
        int rarityBonus = GetPlayerAutoSimPlayRarityBonus(card.rarity);
        if (card is MonsterCard m)
        {
            int baseMonster = m.attack * 2 + m.healthPointMax + rarityBonus;
            int withSkills = ApplyPlayerReligiousLineSkillPlayBonusForAutoSim(baseMonster, card);
            withSkills = ApplyPlayerStarterTrioSkillPlayBonusForAutoSim(withSkills, card);
            return ApplyPlayerAutoSimIntroStylePriorityTweak(withSkills, card);
        }
        if (card is SpellCard sp)
        {
            int spellValue;
            if (sp.SpellOrdinal == 1) spellValue = playerField != null ? 90 : 8;
            else if (sp.SpellOrdinal == 0) spellValue = playerField != null ? 55 : 75;
            else if (sp.SpellOrdinal == 2) spellValue = CanPlayerCastLinGazeNow() ? 62 : 10;
            else spellValue = 20;
            if (sp.SpellOrdinal == 0 && IsOpeningRoundFireballBlocked()) spellValue = int.MinValue / 4;
            int withSkills = ApplyPlayerReligiousLineSkillPlayBonusForAutoSim(spellValue + rarityBonus, card);
            withSkills = ApplyPlayerStarterTrioSkillPlayBonusForAutoSim(withSkills, card);
            return ApplyPlayerAutoSimIntroStylePriorityTweak(withSkills, card);
        }
        return ApplyPlayerAutoSimIntroStylePriorityTweak(rarityBonus, card);
    }

    private int ApplyPlayerAutoSimIntroStylePriorityTweak(int basePriority, Card card)
    {
        if (card == null) return basePriority;

        if (BattleLaunchContext.IsIntroTutorialBattle || BattleLaunchContext.IsM12TrioTutorialBattle)
        {
            if (card is SpellCard) return basePriority - 26;
            if (card is MonsterCard) return basePriority - 12;
        }

        if (card is SpellCard) return basePriority + PlayerAutoSimIntroSpellPriorityTweak;
        return basePriority;
    }

    private int ApplyPlayerStarterTrioSkillPlayBonusForAutoSim(int priority, Card card)
    {
        if (card == null || priority <= int.MinValue / 8) return priority;
        if (card is MonsterCard m)
            return priority + EvaluatePlayerStarterTrioMonsterPlayBonusForAutoSim(m);
        if (card is SpellCard sp)
            return priority + EvaluatePlayerStarterTrioSpellPlayBonusForAutoSim(sp);
        return priority;
    }

    private int EvaluatePlayerStarterTrioMonsterPlayBonusForAutoSim(MonsterCard monster)
    {
        if (monster == null || !CardSkillProficiencyService.IsStarterTrio(monster.id)) return 0;

        int bonus = 0;
        bool emptyField = playerField == null;
        bool enemyHasField = enemyField != null;
        bool enemyCanDirect = enemyField == null && !EnemyLinGazeActive();

        int id = monster.id;
        if (id == MonsterSkillIds.Militia && !playerMilitiaFormationUsed && emptyField)
        {
            bonus += 34;
            if (enemyHasField) bonus += 18;
            if (enemyCanDirect) bonus += 14;
        }

        if (id == MonsterSkillIds.Queen && !playerQueenShelterUsed)
        {
            if (emptyField)
            {
                if (enemyHasField) bonus += 44;
                else bonus += 12;
            }
            else if (playerField.id == MonsterSkillIds.Queen)
                bonus += 8;
        }

        if (id == MonsterSkillIds.King && playerKingTrainingCharges > 0)
        {
            if (emptyField)
            {
                if (enemyCanDirect) bonus += 38;
                else if (enemyHasField) bonus += 22;
                else bonus += 14;
                if (playerHp <= Mathf.CeilToInt(startHealth * 0.62f)) bonus += 12;
            }
            else if (playerField.id == MonsterSkillIds.King)
                bonus += 10;
            else if (playerKingWasOnFieldThisBattle && enemyCanDirect)
                bonus += 6;
        }

        return bonus;
    }

    private int EvaluatePlayerStarterTrioSpellPlayBonusForAutoSim(SpellCard spell)
    {
        if (spell == null || playerField == null) return 0;
        if (spell.SpellOrdinal != 1) return 0;

        int bonus = 0;
        if (playerField.id == MonsterSkillIds.Queen && !playerQueenShelterUsed)
            bonus += 14;
        if (playerField.id == MonsterSkillIds.King && playerKingTrainingCharges > 0)
            bonus += 8;
        if (playerField.id == MonsterSkillIds.Militia && !playerMilitiaFormationUsed)
            bonus += 6;
        return bonus;
    }

    private int ApplyPlayerReligiousLineSkillPlayBonusForAutoSim(int priority, Card card)
    {
        if (card == null || priority <= int.MinValue / 8) return priority;
        if (card is MonsterCard m)
            return priority + EvaluatePlayerReligiousMonsterPlayBonusForAutoSim(m);
        if (card is SpellCard sp)
            return priority + EvaluatePlayerReligiousSpellPlayBonusForAutoSim(sp);
        return priority;
    }

    private int EvaluatePlayerReligiousMonsterPlayBonusForAutoSim(MonsterCard monster)
    {
        if (monster == null) return 0;
        int bonus = 0;
        int id = monster.id;

        if (id == MonsterSkillIds.Bishop && IsPlayerBishopSkillActive())
        {
            if (playerField == null && !playerConsecration.reserveGrantedThisBattle)
                bonus += 52;
            else if (playerField != null && playerField.id == MonsterSkillIds.Bishop &&
                     playerConsecration.awaitingNextSummon)
                bonus -= 18;
        }

        if (id == MonsterSkillIds.Castle && IsPlayerCastleSkillActive())
        {
            if (playerField == null)
            {
                if (enemyField != null)
                    bonus += 44;
                else if (playerHp <= Mathf.CeilToInt(startHealth * 0.55f))
                    bonus += 22;
            }
            else if (playerField.id == MonsterSkillIds.Castle && !playerCastleFortressUsed)
                bonus += 12;
        }

        if (id == MonsterSkillIds.Nun)
        {
            if (playerConsecration.awaitingNextSummon && IsPlayerBishopSkillActive())
                bonus += 58;
            else if (playerField == null && PlayerHandHasLesserHeal())
                bonus += 14;
        }

        if (id == MonsterSkillIds.SanctumKnight && IsPlayerSanctumKnightSkillActive() &&
            !playerSanctumHolyGuardUsed && CanReplaceFieldMonsterForConsecration(true) &&
            playerField != null && playerField.attack == 0)
            bonus += 66;

        if (playerConsecration.awaitingNextSummon && IsPlayerBishopSkillActive() &&
            MonsterSkillReligion.IsReligiousMonsterId(id) && id != MonsterSkillIds.Bishop)
            bonus += 30;

        if (CanReplaceFieldMonsterForConsecration(true))
            bonus += EvaluateEnemyConsecrationBindMonsterBonus(monster);

        return bonus;
    }

    private int EvaluatePlayerReligiousSpellPlayBonusForAutoSim(SpellCard spell)
    {
        if (spell == null || spell.SpellOrdinal != 1 || playerField == null) return 0;
        int bonus = 0;
        float hurtRatio = playerField.currentHp < Mathf.CeilToInt(playerField.maxHp * 0.88f) ? 1f : 0.55f;
        bonus += Mathf.RoundToInt(12f * hurtRatio);

        if (playerField.id == MonsterSkillIds.Nun)
        {
            bonus += 28;
            if (!playerNunHolyResonanceUsed) bonus += 18;
            if (playerConsecration.holyTherapyLinkOnNun) bonus += 22;
        }
        else if (playerField.id == MonsterSkillIds.Castle && !playerCastleFortressUsed)
            bonus += 10;
        else if (playerField.id == MonsterSkillIds.Bishop && playerConsecration.awaitingNextSummon)
            bonus -= 8;

        return bonus;
    }

    /// <summary>場上已有怪獸時，自動化不再從手牌出牌（直接結束回合）。</summary>
    public bool ShouldPlayerAutoSimSkipHandPlayBecauseFieldOccupied() => PlayerHasFieldMonster();
}

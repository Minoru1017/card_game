/// <summary>M-1-2 段考 A：第 6 回合前敵方出牌偏生存（保場、御三家防護、治療優先）。</summary>
public partial class BattleSimulationManager
{
    private bool IsM12PhaseAEarlySurvivalAiActive() =>
        BattleLaunchContext.IsM12TrioTutorialBattle &&
        currentRound < M12PhaseABattleRules.EnemySurvivalAiUntilRoundExclusive;

    /// <summary>段考 A 前期：強制保場／治療／防禦型上場順序。</summary>
    private int TryChooseM12PhaseAEarlySurvivalHandIndex()
    {
        if (!IsM12PhaseAEarlySurvivalAiActive() || enemyHand.Count == 0)
            return -1;

        if (enemyField == null)
            return PickM12PhaseADefensiveMonsterHandIndex();

        int heal = TryPickM12PhaseAPriorityLesserHealIndex();
        if (heal >= 0)
            return heal;

        if (enemyField.currentHp < enemyField.maxHp)
            return -1;

        return -1;
    }

    private int PickM12PhaseADefensiveMonsterHandIndex()
    {
        int best = -1;
        int bestScore = int.MinValue;
        for (int i = 0; i < enemyHand.Count; i++)
        {
            if (!(enemyHand[i] is MonsterCard monster))
                continue;
            int score = ScoreM12PhaseADefensiveMonster(monster);
            if (score <= bestScore)
                continue;
            bestScore = score;
            best = i;
        }
        return best;
    }

    private int ScoreM12PhaseADefensiveMonster(MonsterCard monster)
    {
        if (monster == null)
            return int.MinValue;

        return ScoreM12PhaseADefensiveField(
            monster.id,
            monster.healthPointMax,
            monster.attack,
            !enemyQueenShelterUsed,
            enemyKingTrainingCharges > 0);
    }

    private int ScoreM12PhaseADefensiveField(
        int monsterId,
        int maxHp,
        int attack,
        bool queenShelterAvailable,
        bool kingTrainingAvailable)
    {
        int score = maxHp * 4 + attack;
        bool playerCanDirect = playerField == null && !PlayerLinGazeActive();

        if (monsterId == MonsterSkillIds.Queen && queenShelterAvailable)
        {
            score += 220;
            if (playerField != null)
                score += 40;
        }

        if (monsterId == MonsterSkillIds.King && kingTrainingAvailable)
        {
            score += 200;
            if (playerCanDirect)
                score += 50;
            else if (playerField != null)
                score += 28;
        }

        if (monsterId == 22)
            score += 90;

        if (monsterId == 5)
            score += 55;

        if (monsterId == MonsterSkillIds.Militia)
            score += 25;

        return score;
    }

    private int TryPickM12PhaseAPriorityLesserHealIndex()
    {
        if (enemyField == null || enemyField.currentHp >= enemyField.maxHp)
            return -1;

        for (int i = 0; i < enemyHand.Count; i++)
        {
            if (enemyHand[i] is SpellCard sp && sp.SpellOrdinal == 1 && !IsEnemyCardUnplayableNow(sp))
                return i;
        }
        return -1;
    }

    private int ApplyM12PhaseAEarlySurvivalPlayPriorityTweak(int priority, Card card)
    {
        if (!IsM12PhaseAEarlySurvivalAiActive() || card == null || priority <= int.MinValue / 8)
            return priority;

        if (card is SpellCard sp)
        {
            if (enemyField == null)
                return int.MinValue / 4;

            if (sp.SpellOrdinal == 1)
            {
                if (enemyField.currentHp < enemyField.maxHp)
                    return priority + 140;
                return priority - 40;
            }

            if (sp.SpellOrdinal == 0)
            {
                if (playerField != null)
                    return priority - 18;
                return priority - 80;
            }

            return priority - 24;
        }

        if (card is MonsterCard monster)
        {
            if (enemyField == null)
                return priority + ScoreM12PhaseADefensiveMonster(monster) / 3;

            if (enemyField.currentHp < enemyField.maxHp)
                return priority - 35;

            return priority + EvaluateM12PhaseAFieldReplacementBonus(monster);
        }

        return priority;
    }

    private int EvaluateM12PhaseAFieldReplacementBonus(MonsterCard candidate)
    {
        if (candidate == null || enemyField == null || !CanReplaceFieldMonsterForConsecration(false))
            return 0;

        int current = ScoreM12PhaseADefensiveField(
            enemyField.id,
            enemyField.maxHp,
            enemyField.attack,
            !enemyQueenShelterUsed,
            enemyKingTrainingCharges > 0);
        int next = ScoreM12PhaseADefensiveMonster(candidate);
        if (next <= current + 25)
            return -120;
        return next - current;
    }
}

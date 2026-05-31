using UnityEngine;

/// <summary>港灣實戰致死預警：唯讀模擬敵方下回合對英雄最大合理傷害（HARBOR_COMBAT_COACH_GDD §4.4）。</summary>
public static class HarborCombatLethalThreatEstimator
{
    public struct Result
    {
        public int ThreatDamageMax;
        public bool ShouldWarn;
        public bool PrimarySpellDirect;
    }

    public static Result Evaluate(BattleSimulationManager manager, BattleDifficultyTier tier)
    {
        Result result = new Result();
        if (manager == null || manager.IsBattleOver()) return result;

        float safetyMargin = tier switch
        {
            BattleDifficultyTier.Easy => 1.20f,
            BattleDifficultyTier.Hard => 1.10f,
            _ => 1.15f
        };

        float fireballDirectWeight = tier switch
        {
            BattleDifficultyTier.Easy => 0.35f,
            BattleDifficultyTier.Hard => 0.65f,
            _ => 0.50f
        };

        if (manager.PlayerLinGazeActive())
            return result;

        int threatMax = 0;
        bool primarySpell = false;
        bool playerFieldRemains = manager.GetPlayerFieldCard() != null;

        int playIndex = manager.ChooseEnemyHandCardToPlayIndex();
        if (playIndex >= 0)
        {
            Card chosen = manager.GetEnemyHandCard(playIndex);
            if (chosen is SpellCard spell && spell.SpellOrdinal == 0)
            {
                int raw = manager.EstimateHarborCoachEnemyFireballRawDamage();
                MonsterCard playerField = manager.GetPlayerFieldCard() as MonsterCard;
                if (playerField == null)
                {
                    int heroDmg = manager.EstimateHarborCoachDirectDamageToPlayerHeroFromRaw(raw);
                    threatMax = Mathf.Max(threatMax, heroDmg);
                    primarySpell = true;
                }
                else
                {
                    int toMonster = manager.EstimateHarborCoachDamageToPlayerMonsterFromRaw(raw);
                    if (toMonster >= playerField.healthPoint)
                        playerFieldRemains = false;
                    else
                    {
                        MonsterCard enemyField = manager.GetEnemyFieldCard() as MonsterCard;
                        if (enemyField != null && playerField.healthPoint <= enemyField.attack)
                        {
                            int weighted = Mathf.RoundToInt(
                                manager.EstimateHarborCoachDirectDamageToPlayerHeroFromRaw(raw) * fireballDirectWeight);
                            threatMax = Mathf.Max(threatMax, weighted);
                        }
                    }
                }
            }
        }

        MonsterCard enemyMonster = manager.GetEnemyFieldCard() as MonsterCard;
        if (enemyMonster != null)
        {
            if (!playerFieldRemains)
            {
                bool canDirect = manager.PeekPendingEnemyDirectAttackUnlockForCoach() || !playerFieldRemains;
                if (canDirect)
                {
                    int heroDmg = manager.EstimateHarborCoachScaledEnemyAttackToPlayerHero(enemyMonster.attack);
                    threatMax = Mathf.Max(threatMax, heroDmg);
                }
            }
        }

        result.ThreatDamageMax = threatMax;
        result.PrimarySpellDirect = primarySpell;
        int hp = manager.GetPlayerHeroHp();
        result.ShouldWarn = threatMax > 0 && hp <= Mathf.CeilToInt(threatMax * safetyMargin);
        return result;
    }
}

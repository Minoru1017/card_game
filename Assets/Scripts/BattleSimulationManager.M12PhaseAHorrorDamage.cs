using UnityEngine;

/// <summary>M-1-2 段考 A 恐怖狀態：凍結所有傷害結算（回合仍照常推進）。</summary>
public partial class BattleSimulationManager
{
    private bool IsM12HorrorDamageFrozen =>
        BattleLaunchContext.IsM12TrioTutorialBattle &&
        M12PhaseAHorrorStateRuntime.IsHorrorActive(currentRound, battleOver);

    private void ApplyDamageToEnemyFieldMonster(int damage)
    {
        if (damage <= 0 || enemyField == null || IsM12HorrorDamageFrozen)
            return;

        enemyField.currentHp -= damage;
        if (enemyField.currentHp <= 0)
            enemyField = null;
    }

    private void ApplyDamageToPlayerFieldMonster(int damage)
    {
        if (damage <= 0 || playerField == null || IsM12HorrorDamageFrozen)
            return;

        playerField.currentHp -= damage;
        if (playerField.currentHp <= 0)
            playerField = null;
    }

    private int ApplyCappedDamageToEnemyFieldMonster(int rawDamage)
    {
        if (rawDamage <= 0 || enemyField == null)
            return 0;

        int deal = Mathf.Min(rawDamage, Mathf.Max(0, enemyField.currentHp));
        if (IsM12HorrorDamageFrozen)
            return deal;

        enemyField.currentHp -= deal;
        if (enemyField.currentHp <= 0)
            enemyField = null;
        return deal;
    }

    private int ApplyCappedDamageToPlayerFieldMonster(int rawDamage)
    {
        if (rawDamage <= 0 || playerField == null)
            return 0;

        int deal = Mathf.Min(rawDamage, Mathf.Max(0, playerField.currentHp));
        if (IsM12HorrorDamageFrozen)
            return deal;

        playerField.currentHp -= deal;
        if (playerField.currentHp <= 0)
            playerField = null;
        return deal;
    }

    private int ApplyDamageToEnemyHero(int damage)
    {
        if (damage <= 0)
            return 0;
        if (IsM12HorrorDamageFrozen)
            return damage;

        int before = enemyHp;
        enemyHp = Mathf.Max(0, enemyHp - damage);
        int dealt = before - enemyHp;

        if (BattleLaunchContext.IsM13RivalDuelBattle && playerTurn && dealt > 0)
            M13RivalDuelBattleTracker.NotifyPlayerTurnHeroDamage(dealt);

        if (BattleLaunchContext.IsM13RivalDuelBattle &&
            !m13HotBloodReversalConsumed &&
            enemyHp > 0 &&
            enemyHp <= M13PhaseBBattleRules.HotBloodReversalHpThreshold)
        {
            m13HotBloodReversalConsumed = true;
            M13RivalDuelBattleTracker.NotifyHotBloodReversal();
            ShowBattleToast("阿潮：現在呢 還信灰燼嗎", 3.2f);
            LogBattleHistory("熱血逆轉：阿潮 HP≤8，敵方多抽 1 張");
            EnemyDrawCards(1);
        }

        return dealt;
    }
}

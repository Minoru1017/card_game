/// <summary>戰位克制傷害修正（<see cref="CombatRoleBattleRules"/>）。</summary>
public partial class BattleSimulationManager
{
    private int ScaleOutgoingMonsterDamageVsPlayerField(int rawAttack)
    {
        if (enemyField == null || playerField == null || rawAttack <= 0)
            return rawAttack;
        return CombatRoleBattleRules.ScaleMonsterVsMonster(
            rawAttack,
            enemyField.combatRole,
            playerField.combatRole,
            playerField.currentHp,
            playerField.maxHp);
    }

    private int ScaleOutgoingMonsterDamageVsEnemyField(int rawAttack)
    {
        if (playerField == null || enemyField == null || rawAttack <= 0)
            return rawAttack;
        return CombatRoleBattleRules.ScaleMonsterVsMonster(
            rawAttack,
            playerField.combatRole,
            enemyField.combatRole,
            enemyField.currentHp,
            enemyField.maxHp);
    }

    private int ScaleOutgoingDirectDamageToEnemyHero(int rawAttack)
    {
        if (playerField == null || rawAttack <= 0)
            return rawAttack;
        return CombatRoleBattleRules.ScaleDirectHeroDamage(rawAttack, playerField.combatRole, enemyHp);
    }

    private int ScaleOutgoingDirectDamageToPlayerHero(int rawAttack)
    {
        if (enemyField == null || rawAttack <= 0)
            return rawAttack;
        return CombatRoleBattleRules.ScaleDirectHeroDamage(rawAttack, enemyField.combatRole, playerHp);
    }

    private CombatRoleMatchup ResolveMonsterHitMatchup(bool attackerIsPlayer)
    {
        if (!CombatRoleBattleRules.IsMechanicsEnabled())
            return CombatRoleMatchup.Neutral;

        BattleMonster attacker = attackerIsPlayer ? playerField : enemyField;
        BattleMonster defender = attackerIsPlayer ? enemyField : playerField;
        if (attacker == null || defender == null)
            return CombatRoleMatchup.Neutral;

        return CombatRoleBattleRules.GetTriangleMatchup(attacker.combatRole, defender.combatRole);
    }
}

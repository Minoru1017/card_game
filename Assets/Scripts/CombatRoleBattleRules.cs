using UnityEngine;

/// <summary>卡牌戰位克制（<see cref="CARD_ATTRIBUTES_GDD.md"/> §五）。</summary>
public enum CombatRoleMatchup
{
    Neutral = 0,
    Advantage = 1,
    Disadvantage = 2
}

/// <summary>戰位三角克制與定式加成；入門／M-1-2 段考 A 關閉。</summary>
public static class CombatRoleBattleRules
{
    public const float AdvantageMultiplier = 1.15f;
    public const float DisadvantageMultiplier = 0.90f;
    public const float FinisherBonusMultiplier = 1.10f;
    public const int FinisherHeroHpThreshold = 8;

    public static bool IsMechanicsEnabled()
    {
        if (BattleLaunchContext.IsIntroTutorialBattle)
            return false;
        if (BattleLaunchContext.IsM12TrioTutorialBattle)
            return false;
        return true;
    }

    /// <summary>港灣簡單／M-1-2 B：場上怪顯示戰位角標。</summary>
    public static bool ShouldShowRoleOnFieldMonster()
    {
        if (!IsMechanicsEnabled())
            return false;
        return BattleLaunchContext.IsHarborTrainingGroundBattle ||
               BattleLaunchContext.IsM12CoachPracticeBattle ||
               BattleLaunchContext.IsFreeBattle ||
               BattleLaunchContext.IsM13WeatherTutorialBattle ||
               BattleLaunchContext.IsM13RivalDuelBattle;
    }

    /// <summary>港灣普通／困難／M-1-2 B：手牌怪顯示戰位角標。</summary>
    public static bool ShouldShowRoleOnHandMonster()
    {
        if (!IsMechanicsEnabled())
            return false;
        if (BattleLaunchContext.IsM12CoachPracticeBattle)
            return true;
        if (BattleLaunchContext.IsFreeBattle)
            return true;
        if (!BattleLaunchContext.IsHarborTrainingGroundBattle)
            return false;
        BattleDifficultyTier tier = HarborTrainingDifficultyRuntime.ResolveActiveTier();
        return tier == BattleDifficultyTier.Normal || tier == BattleDifficultyTier.Hard;
    }

    public static CombatRoleMatchup GetTriangleMatchup(CombatRole attacker, CombatRole defender)
    {
        if (attacker == CombatRole.Finisher || defender == CombatRole.Finisher)
            return CombatRoleMatchup.Neutral;

        if (attacker == CombatRole.Strike && defender == CombatRole.Support)
            return CombatRoleMatchup.Advantage;
        if (attacker == CombatRole.Support && defender == CombatRole.Tank)
            return CombatRoleMatchup.Advantage;
        if (attacker == CombatRole.Tank && defender == CombatRole.Strike)
            return CombatRoleMatchup.Advantage;

        if (attacker == CombatRole.Support && defender == CombatRole.Strike)
            return CombatRoleMatchup.Disadvantage;
        if (attacker == CombatRole.Tank && defender == CombatRole.Support)
            return CombatRoleMatchup.Disadvantage;
        if (attacker == CombatRole.Strike && defender == CombatRole.Tank)
            return CombatRoleMatchup.Disadvantage;

        return CombatRoleMatchup.Neutral;
    }

    public static int ScaleMonsterVsMonster(
        int rawDamage,
        CombatRole attackerRole,
        CombatRole defenderRole,
        int defenderCurrentHp,
        int defenderMaxHp)
    {
        if (!IsMechanicsEnabled() || rawDamage <= 0)
            return rawDamage;

        float mult = GetTriangleMultiplier(GetTriangleMatchup(attackerRole, defenderRole));
        if (attackerRole == CombatRole.Finisher &&
            defenderMaxHp > 0 &&
            defenderCurrentHp * 2 <= defenderMaxHp)
        {
            mult *= FinisherBonusMultiplier;
        }

        return ScaleRounded(rawDamage, mult);
    }

    public static int ScaleDirectHeroDamage(int rawDamage, CombatRole attackerRole, int targetHeroHp)
    {
        if (!IsMechanicsEnabled() || rawDamage <= 0)
            return rawDamage;

        float mult = 1f;
        if (attackerRole == CombatRole.Finisher && targetHeroHp <= FinisherHeroHpThreshold)
            mult *= FinisherBonusMultiplier;

        return ScaleRounded(rawDamage, mult);
    }

    public static CombatRole ResolveRole(MonsterCard monster) =>
        monster != null ? monster.combatRole : CombatRole.Strike;

    private static float GetTriangleMultiplier(CombatRoleMatchup matchup) => matchup switch
    {
        CombatRoleMatchup.Advantage => AdvantageMultiplier,
        CombatRoleMatchup.Disadvantage => DisadvantageMultiplier,
        _ => 1f
    };

    private static int ScaleRounded(int rawDamage, float mult) =>
        Mathf.Max(0, Mathf.RoundToInt(rawDamage * mult));
}

using UnityEngine;

/// <summary>組裝對戰長按浮窗的卡牌屬性標籤（<see cref="CARD_ATTRIBUTES_GDD.md"/> §八）。</summary>
public static class HandLongPressTooltipAttributes
{
    public static HandLongPressAttributeTag[] BuildForCard(Card card)
    {
        if (card is MonsterCard monster)
            return BuildForMonster(monster);
        if (card is SpellCard spell)
            return BuildForSpell(spell);
        return System.Array.Empty<HandLongPressAttributeTag>();
    }

    private static HandLongPressAttributeTag[] BuildForMonster(MonsterCard monster)
    {
        if (monster == null || !CombatRoleBattleRules.IsMechanicsEnabled())
            return System.Array.Empty<HandLongPressAttributeTag>();

        return new[]
        {
            new HandLongPressAttributeTag(
                "戰位",
                CombatRoleUtility.GetDisplayName(monster.combatRole),
                ResolveRoleAccent(monster.combatRole))
        };
    }

    private static HandLongPressAttributeTag[] BuildForSpell(SpellCard spell)
    {
        if (spell == null)
            return System.Array.Empty<HandLongPressAttributeTag>();

        return new[]
        {
            new HandLongPressAttributeTag("類型", "法術", BattleUiColors.Hex("#7EB8E8"))
        };
    }

    private static Color ResolveRoleAccent(CombatRole role) => role switch
    {
        CombatRole.Strike => BattleUiColors.Hex("#E8BB6A"),
        CombatRole.Tank => BattleUiColors.Hex("#7AAED4"),
        CombatRole.Support => BattleUiColors.Hex("#7EC8A8"),
        CombatRole.Finisher => BattleUiColors.Hex("#B898D8"),
        _ => BattleUiColors.Hex("#C8D0D8")
    };
}

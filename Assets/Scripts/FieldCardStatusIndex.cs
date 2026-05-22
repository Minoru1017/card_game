/// <summary>
/// 場地牌狀態索引常數 — 對照 <c>FIELD_CARD_STATUS_INDEX.md</c>。
/// </summary>
public static class FieldCardStatusIndex
{
    // §1 場地分區
    public const string ZonePlayerMonster = "P_MON";
    public const string ZoneEnemyMonster = "E_MON";
    public const string ZonePlayerSpell = "P_SPL";
    public const string ZoneEnemySpell = "E_SPL";

    // §2 狀態 ID
    public const string S01_PlayerMonster_BlockedByEnemyLinGaze = "S01";
    public const string S02_EnemyMonster_BlockedByPlayerLinGaze = "S02";
    public const string S03_OpeningRoundNoAttack = "S03";
    public const string S04_PlayerMonster_AttackedThisTurn = "S04";
    public const string S05_PlayerMonster_CounterUsedThisRound = "S05";
    public const string S06_EnemyMonster_CounterUsedThisRound = "S06";
    public const string S07_FloatingAttackDamage = "S07";
    public const string S08_FloatingCounterDamage = "S08";
    public const string S09_CounterAttackLabel = "S09";
    public const string S10_FieldSelectHalo = "S10";
    public const string S11_LinGazeShield = "S11";
    public const string S12_FieldHpHurtColor = "S12";

    // §3.1 持續徽章文案
    public const string BadgeCannotAttack = "不可攻擊";
    public const string BadgeCannotCounter = "不可反擊";
    public const string BadgeSecondaryOpeningRound = "首回合";
    public const string BadgeSecondaryThisTurn = "本回合";

    public static string BadgeSecondaryRoundsRemaining(int rounds) => "剩" + rounds + "回合";

    // UI 子物件名稱（與 Hierarchy 一致）
    public const string UiFieldRestrictionBadge = "FieldRestrictionBadge";
    public const string UiFloatingDamageLabel = "FloatingDamageLabel";
    public const string UiCounterAttackLabel = "CounterAttackLabel";
    public const string UiFieldSelectHaloRoot = "FieldSelectHaloRoot";
    public const string UiLinGazeShieldRoot = "LinGazeShieldRoot";
}

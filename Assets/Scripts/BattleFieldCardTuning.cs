using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 對戰「場上」卡牌調整：怪獸／法術顯示大小、槽位間距與位置、ATK／HP 與法術文字。
/// 掛在 <see cref="BattleSimulationManager.cardField"/>。
/// </summary>
[System.Serializable]
public class BattleFieldCardTuning
{
    public const float FieldMonsterScaleBaseline = 1f;
    public const float FieldSpellScaleBaseline = 1f;
    public const float FieldCardSizeMultiplierMax = 5f;

    [Header("場上怪獸卡 — 顯示大小")]
    [Tooltip("怪獸牌大小倍率：1＝基準最小。Play 中調整會即時更新場上怪獸卡。")]
    [Range(1f, FieldCardSizeMultiplierMax)]
    [FormerlySerializedAs("fieldCardSizeMultiplier")]
    public float fieldMonsterCardSizeMultiplier = 1f;

    [Header("場上法術卡 — 顯示大小")]
    [Tooltip("持續法術場上牌（如林可的凝視）大小倍率：1＝基準最小。")]
    [Range(1f, FieldCardSizeMultiplierMax)]
    public float fieldSpellCardSizeMultiplier = 1f;

    [Tooltip("敵方場上卡再乘此倍率（怪獸與法術皆適用）。")]
    [Range(0.5f, 1.5f)]
    public float enemyFieldSizeMultiplier = 0.95f;

    [Header("場上槽位 — 位置與間距")]
    [Tooltip("怪獸／法術場上區域的垂直位置（anchoredPosition.y）。")]
    [Range(-200f, 200f)]
    public float fieldAreaOffsetY = 10f;

    [Tooltip("我方怪獸場上區中心 X。")]
    [Range(-600f, 0f)]
    public float playerMonsterFieldX = -230f;

    [Tooltip("敵方怪獸場上區中心 X。")]
    [Range(0f, 600f)]
    public float enemyMonsterFieldX = 260f;

    [Tooltip("怪獸槽與法術槽中心的水平間距（我方：法術在怪獸左側；敵方：法術在怪獸右側）。")]
    [Range(0f, 400f)]
    public float monsterSpellSpacingX = 170f;

    [Header("場上怪獸 — ATK／HP 文字")]
    [Tooltip("僅場上怪獸的攻擊、血量數字縮放（乘在手牌文字倍率之上）。")]
    [Range(0.5f, 2.5f)]
    [FormerlySerializedAs("fieldMonsterStatTextScale")]
    public float fieldAttackHealthTextScale = 0.85f;

    [Header("場上法術卡 — 卡面文字")]
    [Tooltip("場上持續法術牌的文字縮放（乘在手牌文字倍率之上）。")]
    [Range(0.5f, 2.5f)]
    public float fieldSpellTextScale = 1f;

    public float FieldMonsterScale => FieldMonsterScaleBaseline * fieldMonsterCardSizeMultiplier;
    public float FieldSpellScale => FieldSpellScaleBaseline * fieldSpellCardSizeMultiplier;

    public float GetFieldCardScale(bool isSpell) => isSpell ? FieldSpellScale : FieldMonsterScale;

    public float GetFieldCardScaleForSide(bool enemy, bool isSpell)
    {
        float scale = GetFieldCardScale(isSpell);
        if (enemy) scale *= enemyFieldSizeMultiplier;
        return scale;
    }

#if UNITY_EDITOR
    public void OnValidateInEditor()
    {
        fieldMonsterCardSizeMultiplier = MigrateLegacyCardSizeSlider(
            fieldMonsterCardSizeMultiplier, FieldMonsterScaleBaseline, FieldCardSizeMultiplierMax);
        fieldSpellCardSizeMultiplier = MigrateLegacyCardSizeSlider(
            fieldSpellCardSizeMultiplier, FieldSpellScaleBaseline, FieldCardSizeMultiplierMax);
        enemyFieldSizeMultiplier = Mathf.Clamp(enemyFieldSizeMultiplier, 0.5f, 1.5f);
        fieldAreaOffsetY = Mathf.Clamp(fieldAreaOffsetY, -200f, 200f);
        playerMonsterFieldX = Mathf.Clamp(playerMonsterFieldX, -600f, 0f);
        enemyMonsterFieldX = Mathf.Clamp(enemyMonsterFieldX, 0f, 600f);
        monsterSpellSpacingX = Mathf.Clamp(monsterSpellSpacingX, 0f, 400f);
        fieldAttackHealthTextScale = Mathf.Clamp(fieldAttackHealthTextScale, 0.5f, 2.5f);
        fieldSpellTextScale = Mathf.Clamp(fieldSpellTextScale, 0.5f, 2.5f);
    }

    private static float MigrateLegacyCardSizeSlider(float value, float baseline, float multiplierMax)
    {
        if (value > multiplierMax + 0.001f)
            value /= baseline;
        return Mathf.Clamp(value, 1f, multiplierMax);
    }
#endif
}

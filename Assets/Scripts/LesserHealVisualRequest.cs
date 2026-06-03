using UnityEngine;

/// <summary>初級治療 UI 特效請求（場上怪獸回復；修女聖療共鳴時含英雄轉移量）。</summary>
public readonly struct LesserHealVisualRequest
{
    /// <summary>true=我方場上怪＋我方英雄；false=敵方場上怪＋敵方英雄。</summary>
    public readonly bool onPlayerSide;
    public readonly int fieldHealAmount;
    /// <summary>聖療共鳴轉補英雄的量；0 表示本局未觸發或無戰技。</summary>
    public readonly int heroResonanceBonus;
    /// <summary>主教聖療連攜額外治療量；0 表示未觸發。</summary>
    public readonly int holyTherapyBonus;

    public bool HasHolyResonance => heroResonanceBonus > 0;
    public bool HasHolyTherapyLink => holyTherapyBonus > 0;

    public LesserHealVisualRequest(bool onPlayerSide, int fieldHealAmount, int heroResonanceBonus, int holyTherapyBonus = 0)
    {
        this.onPlayerSide = onPlayerSide;
        this.fieldHealAmount = fieldHealAmount > 0 ? fieldHealAmount : 40;
        this.heroResonanceBonus = Mathf.Max(0, heroResonanceBonus);
        this.holyTherapyBonus = Mathf.Max(0, holyTherapyBonus);
    }
}

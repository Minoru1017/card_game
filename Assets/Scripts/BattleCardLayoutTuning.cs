using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 對戰卡牌「版面」調整：手牌／場上卡大小、手牌間距、手牌區 Y 位置。
/// 掛在 <see cref="BattleSimulationManager.cardLayout"/>，與文字設定分開方便技術美術辨識。
/// </summary>
[System.Serializable]
public class BattleCardLayoutTuning
{
    public const float HandCardScaleBaseline = 1.75f;
    public const float HandCardSizeMultiplierMax = 3f;

    [Header("手牌 — 顯示大小")]
    [Tooltip("手牌大小倍率：1＝基準最小，最大 3。Play 中調整會即時重建手牌。")]
    [Range(1f, HandCardSizeMultiplierMax)]
    [FormerlySerializedAs("handCardScale")]
    public float handCardSizeMultiplier = 1f;

    [Header("手牌 — 橫向間距")]
    [Tooltip("手牌區相鄰卡牌的水平間距（像素）。")]
    [Range(0f, 80f)]
    public float handCardSpacing = 10f;

    [Header("手牌區 Y 位置 — 我方（底邊錨點）")]
    [Tooltip("能出牌時 anchoredPosition.y（負值＝往下）。")]
    [Range(-200f, 200f)]
    [FormerlySerializedAs("handAreaAnchoredY")]
    [FormerlySerializedAs("handAreaBaseYOffset")]
    public float handAreaAnchoredYCanPlay = -25f;

    [Tooltip("不能出牌時 anchoredPosition.y（負值＝往下收合）。")]
    [Range(-200f, 200f)]
    [FormerlySerializedAs("handAreaCantPlayPeekOffsetPlayer")]
    public float handAreaAnchoredYCantPlay = -85f;

    [Header("手牌區 Y 位置 — 敵方（頂邊錨點）")]
    [Tooltip("能出牌時 anchoredPosition.y（負值＝往畫面內）。")]
    [Range(-200f, 200f)]
    public float enemyHandAreaAnchoredYCanPlay = -20f;

    [Tooltip("不能出牌時 anchoredPosition.y（正值＝往上收合）。")]
    [Range(-200f, 200f)]
    [FormerlySerializedAs("handAreaCantPlayPeekOffsetEnemy")]
    public float enemyHandAreaAnchoredYCantPlay = 85f;

    public float HandCardScale => HandCardScaleBaseline * handCardSizeMultiplier;

#if UNITY_EDITOR
    public void OnValidateInEditor()
    {
        handCardSizeMultiplier = MigrateLegacyCardSizeSlider(
            handCardSizeMultiplier, HandCardScaleBaseline, HandCardSizeMultiplierMax);
        handAreaAnchoredYCanPlay = Mathf.Clamp(handAreaAnchoredYCanPlay, -200f, 200f);
        enemyHandAreaAnchoredYCanPlay = Mathf.Clamp(enemyHandAreaAnchoredYCanPlay, -200f, 200f);
        handAreaAnchoredYCantPlay = Mathf.Clamp(handAreaAnchoredYCantPlay, -200f, 200f);
        enemyHandAreaAnchoredYCantPlay = Mathf.Clamp(enemyHandAreaAnchoredYCantPlay, -200f, 200f);
        handCardSpacing = Mathf.Max(0f, handCardSpacing);
    }

    private static float MigrateLegacyCardSizeSlider(float value, float baseline, float multiplierMax)
    {
        if (value > multiplierMax + 0.001f)
            value /= baseline;
        return Mathf.Clamp(value, 1f, multiplierMax);
    }
#endif
}

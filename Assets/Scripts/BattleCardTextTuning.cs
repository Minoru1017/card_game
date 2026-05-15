using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 對戰卡牌「文字」調整：卡名、攻擊／血量、效果字與場上數字縮放。
/// 掛在 <see cref="BattleSimulationManager.cardText"/>，與版面設定分開方便技術美術辨識。
/// </summary>
[System.Serializable]
public class BattleCardTextTuning
{
    [Header("手牌 — 文字整體")]
    [Tooltip("手牌上所有 TMP 的 uniform 縮放（攻擊、血量、效果等）。")]
    [Range(0.5f, 2.5f)]
    public float handCardTextScale = 1f;

    [Tooltip("手牌卡名額外倍率（乘在 handCardTextScale 之上）。")]
    [Range(0.5f, 2.5f)]
    public float handCardNameScale = 1f;

    [Header("手牌 — 卡底圖")]
    [Tooltip("手牌卡底／背景 Image 的 localScale（非文字）。")]
    [Range(0.5f, 2f)]
    public float handCardBackplateScale = 1f;

#if UNITY_EDITOR
    public void OnValidateInEditor()
    {
        handCardTextScale = Mathf.Clamp(handCardTextScale, 0.5f, 2.5f);
        handCardNameScale = Mathf.Clamp(handCardNameScale, 0.5f, 2.5f);
        handCardBackplateScale = Mathf.Clamp(handCardBackplateScale, 0.5f, 2f);
    }
#endif
}

using UnityEngine;

/// <summary>
/// 卡牌本體立繪顯示基準（與 <c>Assets/UI/CardArt/Card preset images</c> Sprite 裁切 337×491 一致）。
/// </summary>
public static class CardArtLayoutSpec
{
    public const float PresetArtWidthPx = 337f;
    public const float PresetArtHeightPx = 491f;

    /// <summary>對戰／開包等 prefab 根節點 scale=2 時的 sizeDelta。</summary>
    public const float PrefabRootWidthPx = PresetArtWidthPx * 0.5f;
    public const float PrefabRootHeightPx = PresetArtHeightPx * 0.5f;

    public static Vector2 PresetArtSize => new Vector2(PresetArtWidthPx, PresetArtHeightPx);

    /// <summary>在 uniform scale 下，使 RectTransform 實際顯示為 <see cref="PresetArtWidthPx"/>×<see cref="PresetArtHeightPx"/> 的 sizeDelta。</summary>
    public static Vector2 GetRectSizeDeltaForUniformScale(float uniformScale)
    {
        float s = Mathf.Max(0.001f, uniformScale);
        return new Vector2(PresetArtWidthPx / s, PresetArtHeightPx / s);
    }
}

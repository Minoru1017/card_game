using UnityEngine;

/// <summary>對戰手牌長按浮窗：屬性標籤（戰位／攻擊／生命／稀有度等）。</summary>
public readonly struct HandLongPressAttributeTag
{
    public readonly string label;
    public readonly string value;
    public readonly Color accentColor;

    public HandLongPressAttributeTag(string label, string value, Color accentColor = default)
    {
        this.label = label ?? string.Empty;
        this.value = value ?? string.Empty;
        this.accentColor = accentColor;
    }

    public bool HasAccent => accentColor.a > 0.01f;
}

/// <summary>對戰手牌長按浮窗：大標／副標／屬性標籤／內文（TMP Rich Text）。</summary>
public struct HandLongPressTooltipModel
{
    public string heading;
    public string subtitleRich;
    public string bodyRich;
    public HandLongPressAttributeTag[] attributeTags;

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(heading) ||
        !string.IsNullOrWhiteSpace(subtitleRich) ||
        !string.IsNullOrWhiteSpace(bodyRich);

    public bool HasAttributeTags => attributeTags != null && attributeTags.Length > 0;
}

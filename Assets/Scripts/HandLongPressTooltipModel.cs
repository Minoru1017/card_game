/// <summary>對戰手牌長按浮窗：大標／副標／內文（TMP Rich Text）。</summary>
public struct HandLongPressTooltipModel
{
    public string heading;
    public string subtitleRich;
    public string bodyRich;

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(heading) ||
        !string.IsNullOrWhiteSpace(subtitleRich) ||
        !string.IsNullOrWhiteSpace(bodyRich);
}

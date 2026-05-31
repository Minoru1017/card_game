using UnityEngine;

/// <summary>
/// Return button layout from <c>Buildbeck.unity</c> <c>return</c> (148×94 @ 1920×1080).
/// Maps to top-left of the host canvas without changing <see cref="Canvas"/> scaler.
/// </summary>
public static class ReturnButtonLayout
{
    public const string ObjectName = "return";

    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public static readonly Vector2 Size = new Vector2(148f, 94f);
    public static readonly Vector2 CenterAnchoredPosition = new Vector2(-1001f, 424f);

    /// <summary>Visible top inset from canvas top on the reference layout (69px @ 1080).</summary>
    public static readonly float ReferenceTopInsetPx = 69f;

    public static bool ApplyTo(RectTransform rt, Canvas hostCanvas = null)
    {
        if (rt == null) return false;

        Canvas canvas = hostCanvas ?? rt.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        RectTransform canvasRt = canvas.transform as RectTransform;
        if (canvasRt != null && rt.parent != canvasRt)
            rt.SetParent(canvasRt, false);

        Canvas.ForceUpdateCanvases();

        Vector2 parentSize = canvasRt != null ? canvasRt.rect.size : Vector2.zero;
        if (parentSize.x < 1f || parentSize.y < 1f)
            return false;

        float refCenterX = ReferenceResolution.x * 0.5f;
        float refCenterY = ReferenceResolution.y * 0.5f;
        float refLeft = refCenterX + CenterAnchoredPosition.x - Size.x * 0.5f;

        float x = Mathf.Max(0f, refLeft * (parentSize.x / ReferenceResolution.x));
        float y = -ReferenceTopInsetPx * (parentSize.y / ReferenceResolution.y);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = Size;
        rt.anchoredPosition = new Vector2(x, y);
        rt.localScale = Vector3.one;
        rt.gameObject.SetActive(true);
        return true;
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleHandLongPressTooltip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float holdSeconds = 0.35f;

    private bool pressing;
    private bool shown;
    private float downTime;
    private int suppressClickUntilFrame = -1;
    private string message;
    private Action<RectTransform, string> onShow;
    private Action onHide;

    public void Setup(string tooltipMessage, Action<RectTransform, string> showCallback, Action hideCallback)
    {
        message = tooltipMessage;
        onShow = showCallback;
        onHide = hideCallback;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        pressing = true;
        shown = false;
        downTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
        if (shown)
        {
            onHide?.Invoke();
            // Suppress click once when long-press was used.
            suppressClickUntilFrame = Time.frameCount + 1;
        }
        shown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pressing = false;
        if (shown) onHide?.Invoke();
        shown = false;
    }

    private void Update()
    {
        if (!pressing || shown) return;
        if (Time.unscaledTime - downTime < holdSeconds) return;

        RectTransform rt = transform as RectTransform;
        onShow?.Invoke(rt, message);
        shown = true;
    }

    public bool ShouldSuppressClick()
    {
        return Time.frameCount <= suppressClickUntilFrame;
    }
}

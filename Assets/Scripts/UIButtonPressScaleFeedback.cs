using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIButtonPressScaleFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.96f;

    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
        transform.localScale = baseScale * Mathf.Clamp(pressedScale, 0.85f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = baseScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = baseScale;
    }
}

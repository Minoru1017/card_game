using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonYellowOutlineFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Vector2 outlineDistance = new Vector2(4f, -4f);

    private Outline outline;

    private void Awake()
    {
        EnsureOutline();
        SetOutlineVisible(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
        EnsureOutline();
        SetOutlineVisible(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetOutlineVisible(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetOutlineVisible(false);
    }

    private void EnsureOutline()
    {
        if (outline == null)
        {
            outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
        }
        outline.effectColor = outlineColor;
        outline.effectDistance = outlineDistance;
    }

    private void SetOutlineVisible(bool visible)
    {
        if (outline != null) outline.enabled = visible;
    }
}

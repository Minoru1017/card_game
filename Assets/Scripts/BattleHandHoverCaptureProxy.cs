using UnityEngine.EventSystems;
using UnityEngine;

public class BattleHandHoverCaptureProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BattleHandHoverPreview owner;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.NotifyGhostPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.NotifyGhostPointerExit(eventData);
    }
}

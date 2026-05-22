using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 長按卡牌開啟 <see cref="BackpackCardInspectPanel"/>（商店開包、背包等）。
/// </summary>
public class CardInspectLongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float holdSeconds = 0.4f;

    private bool pressing;
    private bool shown;
    private float downTime;
    private Card boundCard;
    private CardDisplay boundDisplay;
    private ICardInspectPanelHost inspectHost;

    public void Setup(Card card, CardDisplay display, ICardInspectPanelHost host, float hold = 0.4f)
    {
        boundCard = card;
        boundDisplay = display;
        inspectHost = host;
        holdSeconds = hold;
        EnsurePointerTarget();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (boundCard == null || inspectHost == null) return;
        pressing = true;
        shown = false;
        downTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
        shown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pressing = false;
        shown = false;
    }

    private void Update()
    {
        if (!pressing || shown) return;
        if (boundCard == null || inspectHost == null) return;
        if (Time.unscaledTime - downTime < holdSeconds) return;

        inspectHost.ShowCardInspect(boundCard, boundDisplay);
        shown = true;
        pressing = false;
    }

    private void EnsurePointerTarget()
    {
        if (GetComponent<Graphic>() != null) return;
        Image img = gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.004f);
        img.raycastTarget = true;
    }
}

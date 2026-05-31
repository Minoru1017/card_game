using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleHandDiscardDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float holdSeconds = 1f;
    public float minDragDistance = 8f;

    private RectTransform cardRect;
    private CanvasGroup canvasGroup;
    private Outline glowOutlineYellow;
    private Outline glowOutlineBlack;
    private Image holdProgressFill;
    private bool glowActive;
    private bool pressing;
    private bool dragging;
    private float downTimeUnscaled;
    private int pointerId = int.MinValue;
    private Vector2 downScreenPos;
    private Vector2 startAnchoredPos;
    private Vector3 restHandLocalScale = Vector3.one;
    private Vector3 dragRootNeutralLocalScale = Vector3.one;
    private int startSibling;
    private RectTransform startParentRect;
    private RectTransform parentRect;
    private RectTransform dragRootRect;
    private Camera eventCamera;
    private const float GlowPulseMinAlpha = 0.5f;
    private const float GlowPulseMaxAlpha = 1f;
    private const float GlowPulseSpeed = 9f;

    private Func<bool> canDrag;
    private Func<Vector2, Camera, bool> isOverDropZone;
    private Func<Vector2, Camera, bool> tryDropAt;
    private Action<bool> onDropZoneHoverChanged;
    private Action onDropCommitted;
    private bool hoveringDropZone;
    private static Sprite s_whiteSprite;
    private bool settlingAfterDrop;

    public void Setup(
        Func<bool> canDragPredicate,
        Func<Vector2, Camera, bool> isOverDropZoneScreenPoint,
        Func<Vector2, Camera, bool> tryDropAtScreenPoint,
        Action<bool> onDropZoneHoverChangedCallback = null,
        Action onDropCommittedCallback = null)
    {
        canDrag = canDragPredicate;
        isOverDropZone = isOverDropZoneScreenPoint;
        tryDropAt = tryDropAtScreenPoint;
        onDropZoneHoverChanged = onDropZoneHoverChangedCallback;
        onDropCommitted = onDropCommittedCallback;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (canDrag == null || !canDrag()) return;
        cardRect = transform as RectTransform;
        if (cardRect == null) return;
        parentRect = cardRect.parent as RectTransform;
        if (parentRect == null) return;
        startParentRect = parentRect;
        Canvas rootCanvas = cardRect.GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            dragRootRect = rootCanvas.rootCanvas.transform as RectTransform;
        else
            dragRootRect = null;

        pressing = true;
        dragging = false;
        downTimeUnscaled = Time.unscaledTime;
        pointerId = eventData.pointerId;
        downScreenPos = eventData.position;
        eventCamera = eventData.pressEventCamera;
        startAnchoredPos = cardRect.anchoredPosition;
        restHandLocalScale = cardRect.localScale;
        startSibling = cardRect.GetSiblingIndex();

        if (canvasGroup == null) canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        EnsureHoldProgressFill();
        SetHoldProgressActive(true);
        SetHoldProgress(0f);
        SetGlowActive(false);
        PromoteCardToTopLayer();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pressing || settlingAfterDrop || eventData == null || eventData.pointerId != pointerId) return;
        if (canDrag == null || !canDrag()) return;

        if (!dragging)
        {
            float held = Time.unscaledTime - downTimeUnscaled;
            float moved = Vector2.Distance(downScreenPos, eventData.position);
            if (held < holdSeconds || moved < minDragDistance) return;
            dragging = true;
            canvasGroup.blocksRaycasts = false;
            SetHoldProgressActive(false);
            PromoteCardToTopLayer();
            dragRootNeutralLocalScale = cardRect.localScale;
        }

        Vector2 localPoint;
        Vector2 baseAnchored = cardRect.anchoredPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out localPoint))
        {
            // Align card center to pointer/touch center regardless of card pivot.
            Vector2 cardSize = Vector2.Scale(cardRect.rect.size, cardRect.localScale);
            Vector2 centerOffsetFromPivot = new Vector2(
                (0.5f - cardRect.pivot.x) * cardSize.x,
                (0.5f - cardRect.pivot.y) * cardSize.y);
            // localPoint is in parent local-space (origin at parent pivot),
            // while anchoredPosition is relative to child anchors.
            // Hand cards are anchored at (0,0), so convert local-space -> anchored-space explicitly.
            Vector2 parentPivotOffset = new Vector2(
                parentRect.rect.width * parentRect.pivot.x,
                parentRect.rect.height * parentRect.pivot.y);
            baseAnchored = localPoint - centerOffsetFromPivot + parentPivotOffset;
            cardRect.anchoredPosition = baseAnchored;
        }
        bool nowHoveringDropZone = isOverDropZone != null && isOverDropZone(eventData.position, eventCamera);
        if (nowHoveringDropZone != hoveringDropZone)
        {
            hoveringDropZone = nowHoveringDropZone;
            onDropZoneHoverChanged?.Invoke(hoveringDropZone);
        }
        if (hoveringDropZone)
        {
            cardRect.localScale = Vector3.Lerp(cardRect.localScale, dragRootNeutralLocalScale * 0.94f, 0.4f);
            cardRect.anchoredPosition = Vector2.Lerp(cardRect.anchoredPosition, baseAnchored + new Vector2(-10f, 0f), 0.45f);
        }
        else
        {
            cardRect.localScale = Vector3.Lerp(cardRect.localScale, dragRootNeutralLocalScale, 0.25f);
            cardRect.anchoredPosition = Vector2.Lerp(cardRect.anchoredPosition, baseAnchored, 0.35f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        bool dropped = false;
        if (dragging && eventData != null && tryDropAt != null)
        {
            dropped = tryDropAt(eventData.position, eventCamera);
        }

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        if (!dropped && cardRect != null)
        {
            if (startParentRect != null)
            {
                cardRect.SetParent(startParentRect, false);
                cardRect.anchoredPosition = startAnchoredPos;
                int max = startParentRect.childCount - 1;
                cardRect.SetSiblingIndex(Mathf.Clamp(startSibling, 0, max));
            }
        }
        else if (dropped && cardRect != null)
        {
            StartCoroutine(CoDropSettleAndFinalize());
        }

        pressing = false;
        dragging = false;
        pointerId = int.MinValue;
        if (!dropped && cardRect != null) cardRect.localScale = restHandLocalScale;
        if (hoveringDropZone)
        {
            hoveringDropZone = false;
            onDropZoneHoverChanged?.Invoke(false);
        }
        SetHoldProgressActive(false);
        SetGlowActive(false);
    }

    private System.Collections.IEnumerator CoDropSettleAndFinalize()
    {
        GameObject droppedCardObj = cardRect != null ? cardRect.gameObject : null;
        if (cardRect == null)
        {
            onDropCommitted?.Invoke();
            yield break;
        }

        settlingAfterDrop = true;
        float dur = 0.14f;
        float t = 0f;
        Vector2 fromPos = cardRect.anchoredPosition;
        Vector2 toPos = fromPos + new Vector2(-70f, 0f);
        Vector3 fromScale = cardRect.localScale;
        Vector3 toScale = dragRootNeutralLocalScale * 0.82f;
        if (canvasGroup == null) canvasGroup = gameObject.GetComponent<CanvasGroup>();
        float fromAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        while (t < dur && cardRect != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            p = 1f - Mathf.Pow(1f - p, 3f);
            cardRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, p);
            cardRect.localScale = Vector3.Lerp(fromScale, toScale, p);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(fromAlpha, 0f, p);
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        settlingAfterDrop = false;
        if (droppedCardObj != null)
            Destroy(droppedCardObj);
        onDropCommitted?.Invoke();
    }

    private void Update()
    {
        if ((pressing || dragging) && cardRect != null)
            PromoteCardToTopLayer();

        if (pressing && !dragging && holdProgressFill != null)
        {
            float p = holdSeconds > 0f ? Mathf.Clamp01((Time.unscaledTime - downTimeUnscaled) / holdSeconds) : 1f;
            SetHoldProgress(p);
            if (!glowActive && p >= 1f)
                SetGlowActive(true);
        }

        if (!glowActive) return;
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * GlowPulseSpeed);
        float yellowAlpha = Mathf.Lerp(GlowPulseMinAlpha, GlowPulseMaxAlpha, t);
        float blackAlpha = Mathf.Lerp(GlowPulseMinAlpha * 0.9f, GlowPulseMaxAlpha * 0.95f, 1f - t);

        if (glowOutlineYellow != null)
        {
            Color yc = glowOutlineYellow.effectColor;
            yc.a = yellowAlpha;
            glowOutlineYellow.effectColor = yc;
        }
        if (glowOutlineBlack != null)
        {
            Color bc = glowOutlineBlack.effectColor;
            bc.a = blackAlpha;
            glowOutlineBlack.effectColor = bc;
        }
    }

    private void SetGlowActive(bool active)
    {
        glowActive = active;
        if (active)
        {
            EnsureGlowOutlines();
            if (glowOutlineYellow != null)
            {
                glowOutlineYellow.effectDistance = new Vector2(11f, 11f);
                glowOutlineYellow.useGraphicAlpha = false;
                // Fluorescent warning yellow.
                glowOutlineYellow.effectColor = new Color(0.95f, 1f, 0.1f, GlowPulseMaxAlpha);
            }
            if (glowOutlineBlack != null)
            {
                glowOutlineBlack.effectDistance = new Vector2(15f, 15f);
                glowOutlineBlack.useGraphicAlpha = false;
                glowOutlineBlack.effectColor = new Color(0f, 0f, 0f, GlowPulseMaxAlpha * 0.85f);
            }
        }
        else
        {
            if (glowOutlineYellow != null)
                glowOutlineYellow.effectColor = new Color(0.95f, 1f, 0.1f, 0f);
            if (glowOutlineBlack != null)
                glowOutlineBlack.effectColor = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void EnsureGlowOutlines()
    {
        Outline[] outlines = gameObject.GetComponents<Outline>();
        if (outlines != null && outlines.Length > 0)
        {
            glowOutlineYellow = outlines[0];
            if (outlines.Length > 1) glowOutlineBlack = outlines[1];
        }
        if (glowOutlineYellow == null) glowOutlineYellow = gameObject.AddComponent<Outline>();
        if (glowOutlineBlack == null) glowOutlineBlack = gameObject.AddComponent<Outline>();
    }

    private void PromoteCardToTopLayer()
    {
        if (cardRect == null) return;
        if (dragRootRect != null && cardRect.parent != dragRootRect)
        {
            // Keep world pose when promoting to root canvas top layer.
            cardRect.SetParent(dragRootRect, true);
            parentRect = dragRootRect;
        }
        cardRect.SetAsLastSibling();
    }

    private void EnsureHoldProgressFill()
    {
        if (holdProgressFill != null) return;
        Transform t = transform.Find("DiscardHoldProgressFill");
        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
            holdProgressFill = go.GetComponent<Image>();
        }
        else
        {
            go = new GameObject("DiscardHoldProgressFill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            holdProgressFill = go.GetComponent<Image>();
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        holdProgressFill.raycastTarget = false;
        holdProgressFill.sprite = GetWhiteSprite();
        holdProgressFill.type = Image.Type.Filled;
        holdProgressFill.fillMethod = Image.FillMethod.Vertical;
        holdProgressFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        holdProgressFill.fillAmount = 0f;
        holdProgressFill.color = new Color(1f, 0.15f, 0.15f, 0.42f);
        go.SetActive(false);
    }

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite != null) return s_whiteSprite;
        Texture2D tex = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return s_whiteSprite;
    }

    private void SetHoldProgressActive(bool active)
    {
        if (holdProgressFill != null) holdProgressFill.gameObject.SetActive(active);
    }

    private void SetHoldProgress(float p)
    {
        if (holdProgressFill == null) return;
        holdProgressFill.fillAmount = Mathf.Clamp01(p);
    }
}

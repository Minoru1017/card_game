using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A-1 耕田：工具拖曳（犁、鐮、鹽網等）。</summary>
public sealed class SideQuestA1FarmUiDrag : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform dragBounds;
    public float boundsPadding = 14f;
    public float dragScaleMultiplier = 1.38f;
    public float pressScaleMultiplier = 1.16f;
    public float maxTiltDegrees = 18f;

    public Action<Vector2, Camera> onDragScreen;
    public Action onDragEnded;
    public Action<bool> onDragHighlightChanged;
    public Func<bool> canDrag;

    private static Sprite s_whiteSprite;

    private RectTransform rect;
    private Image image;
    private CanvasGroup canvasGroup;
    private GameObject glowHaloGo;
    private Image glowHaloImage;
    private GameObject pointerRingGo;
    private Image pointerRingImage;
    private Outline glowOutlineYellow;
    private Outline glowOutlineBlack;

    private Transform homeParent;
    private Vector2 homeAnchoredPos;
    private Vector2 homeAnchorMin;
    private Vector2 homeAnchorMax;
    private Vector2 homePivot;
    private Vector3 homeScale;
    private Color homeColor;

    private Vector2 lastDragAnchoredPos;
    private Vector3 scaleBeforeDragBoost;
    private Camera eventCamera;
    private bool dragging;
    private bool pressing;
    private bool glowPulseActive;

    private const float GlowPulseMinAlpha = 0.55f;
    private const float GlowPulseMaxAlpha = 1f;
    private const float GlowPulseSpeed = 10f;

    private void Awake()
    {
        rect = transform as RectTransform;
        image = GetComponent<Image>();
        ApplyWhiteSprite(image);
        EnsureGlowHalo();
        EnsurePointerRing();
        EnsureGlowOutlines();
        EnsureCanvasGroup();
        CaptureHome();
    }

    public static void ApplyWhiteSprite(Image target)
    {
        if (target == null)
            return;

        target.sprite = GetWhiteSprite();
        target.type = Image.Type.Simple;
    }

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite != null)
            return s_whiteSprite;

        Texture2D tex = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));
        return s_whiteSprite;
    }

    public void CaptureHome()
    {
        if (rect == null)
            return;

        homeParent = rect.parent;
        homeAnchoredPos = rect.anchoredPosition;
        homeAnchorMin = rect.anchorMin;
        homeAnchorMax = rect.anchorMax;
        homePivot = rect.pivot;
        homeScale = rect.localScale;
        if (image != null)
            homeColor = image.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAcceptPointer(eventData))
            return;

        pressing = true;
        eventCamera = eventData.pressEventCamera;
        ApplyPressVisuals(true);
        onDragHighlightChanged?.Invoke(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
        if (!dragging)
        {
            ApplyPressVisuals(false);
            onDragHighlightChanged?.Invoke(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanAcceptPointer(eventData))
            return;

        dragging = true;
        eventCamera = eventData.pressEventCamera;

        RectTransform dragLayer = dragBounds != null ? dragBounds : rect.parent as RectTransform;
        if (dragLayer != null)
        {
            Vector3 worldPos = rect.position;
            rect.SetParent(dragLayer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.position = worldPos;
        }

        rect.SetAsLastSibling();
        CanvasGroup group = EnsureCanvasGroup();
        if (group != null)
            group.blocksRaycasts = false;

        scaleBeforeDragBoost = rect.localScale;
        ApplyDragVisuals(true);
        onDragHighlightChanged?.Invoke(true);
        MoveToScreen(eventData.position, eventData.pressEventCamera, true);
        onDragScreen?.Invoke(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || eventData == null)
            return;

        MoveToScreen(eventData.position, eventData.pressEventCamera, false);
        onDragScreen?.Invoke(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        pressing = false;
        glowPulseActive = false;
        ApplyDragVisuals(false);
        onDragHighlightChanged?.Invoke(false);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (rect != null && homeParent != null)
        {
            rect.SetParent(homeParent, false);
            rect.anchorMin = homeAnchorMin;
            rect.anchorMax = homeAnchorMax;
            rect.pivot = homePivot;
            rect.anchoredPosition = homeAnchoredPos;
            rect.localScale = homeScale;
            rect.localRotation = Quaternion.identity;
        }

        onDragEnded?.Invoke();
    }

    private void Update()
    {
        if (!glowPulseActive)
            return;

        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * GlowPulseSpeed);
        float yellowAlpha = Mathf.Lerp(GlowPulseMinAlpha, GlowPulseMaxAlpha, t);
        float blackAlpha = Mathf.Lerp(GlowPulseMinAlpha * 0.9f, GlowPulseMaxAlpha * 0.95f, 1f - t);
        SetOutlineAlpha(yellowAlpha, blackAlpha);

        if (pointerRingImage != null)
        {
            Color ring = pointerRingImage.color;
            ring.a = Mathf.Lerp(0.35f, 0.72f, t);
            pointerRingImage.color = ring;
        }
    }

    private bool CanAcceptPointer(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return false;
        if (canDrag != null && !canDrag())
            return false;
        return rect != null;
    }

    private CanvasGroup EnsureCanvasGroup()
    {
        if (canvasGroup != null && canvasGroup.gameObject == gameObject)
            return canvasGroup;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    private void ApplyPressVisuals(bool active)
    {
        if (active)
        {
            rect.localScale = homeScale * pressScaleMultiplier;
            SetGlowHaloVisible(true, 0.55f);
            SetPointerRingVisible(true, 0.42f);
            glowPulseActive = true;
            SetOutlineAlpha(0.72f, 0.62f);
            if (image != null)
                image.color = Color.Lerp(homeColor, Color.white, 0.22f);
            return;
        }

        if (dragging)
            return;

        glowPulseActive = false;
        rect.localScale = homeScale;
        rect.localRotation = Quaternion.identity;
        SetGlowHaloVisible(false, 0f);
        SetPointerRingVisible(false, 0f);
        SetOutlineAlpha(0f, 0f);
        if (image != null)
            image.color = homeColor;
    }

    private void ApplyDragVisuals(bool active)
    {
        if (active)
        {
            rect.localScale = scaleBeforeDragBoost * dragScaleMultiplier;
            SetGlowHaloVisible(true, 0.88f);
            SetPointerRingVisible(true, 0.62f);
            glowPulseActive = true;
            SetOutlineAlpha(1f, 0.92f);
            if (image != null)
                image.color = Color.Lerp(homeColor, new Color(1f, 0.98f, 0.78f, 1f), 0.55f);
            return;
        }

        glowPulseActive = false;
        SetGlowHaloVisible(false, 0f);
        SetPointerRingVisible(false, 0f);
        SetOutlineAlpha(0f, 0f);
        if (image != null)
            image.color = homeColor;
    }

    private void EnsureGlowHalo()
    {
        if (glowHaloGo != null)
            return;

        glowHaloGo = new GameObject("DragGlow", typeof(RectTransform), typeof(Image));
        glowHaloGo.transform.SetParent(transform, false);
        glowHaloGo.transform.SetAsFirstSibling();

        RectTransform haloRt = glowHaloGo.GetComponent<RectTransform>();
        haloRt.anchorMin = Vector2.zero;
        haloRt.anchorMax = Vector2.one;
        haloRt.offsetMin = new Vector2(-34f, -34f);
        haloRt.offsetMax = new Vector2(34f, 34f);

        glowHaloImage = glowHaloGo.GetComponent<Image>();
        ApplyWhiteSprite(glowHaloImage);
        glowHaloImage.color = new Color(1f, 0.84f, 0.12f, 0f);
        glowHaloImage.raycastTarget = false;
        SetGlowHaloVisible(false, 0f);
    }

    private void EnsurePointerRing()
    {
        if (pointerRingGo != null)
            return;

        pointerRingGo = new GameObject("PointerRing", typeof(RectTransform), typeof(Image));
        pointerRingGo.transform.SetParent(transform, false);
        pointerRingGo.transform.SetAsFirstSibling();

        RectTransform ringRt = pointerRingGo.GetComponent<RectTransform>();
        ringRt.anchorMin = new Vector2(0.5f, 0.5f);
        ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot = new Vector2(0.5f, 0.5f);
        ringRt.sizeDelta = new Vector2(148f, 148f);
        ringRt.anchoredPosition = Vector2.zero;

        pointerRingImage = pointerRingGo.GetComponent<Image>();
        ApplyWhiteSprite(pointerRingImage);
        pointerRingImage.color = new Color(1f, 0.92f, 0.18f, 0f);
        pointerRingImage.raycastTarget = false;
        SetPointerRingVisible(false, 0f);
    }

    private void SetGlowHaloVisible(bool visible, float alpha)
    {
        if (glowHaloImage == null)
            return;

        Color c = glowHaloImage.color;
        c.a = visible ? alpha : 0f;
        glowHaloImage.color = c;
    }

    private void SetPointerRingVisible(bool visible, float alpha)
    {
        if (pointerRingImage == null)
            return;

        Color c = pointerRingImage.color;
        c.a = visible ? alpha : 0f;
        pointerRingImage.color = c;
    }

    private void EnsureGlowOutlines()
    {
        Outline[] outlines = GetComponents<Outline>();
        if (outlines.Length > 0)
            glowOutlineYellow = outlines[0];
        if (outlines.Length > 1)
            glowOutlineBlack = outlines[1];

        if (glowOutlineYellow == null)
            glowOutlineYellow = gameObject.AddComponent<Outline>();
        if (glowOutlineBlack == null)
            glowOutlineBlack = gameObject.AddComponent<Outline>();

        glowOutlineYellow.effectDistance = new Vector2(12f, 12f);
        glowOutlineYellow.useGraphicAlpha = false;
        glowOutlineYellow.effectColor = new Color(1f, 0.95f, 0.12f, 0f);

        glowOutlineBlack.effectDistance = new Vector2(16f, 16f);
        glowOutlineBlack.useGraphicAlpha = false;
        glowOutlineBlack.effectColor = new Color(0.02f, 0.08f, 0.05f, 0f);
    }

    private void SetOutlineAlpha(float yellowAlpha, float blackAlpha)
    {
        if (glowOutlineYellow != null)
        {
            Color y = glowOutlineYellow.effectColor;
            y.a = yellowAlpha;
            glowOutlineYellow.effectColor = y;
        }

        if (glowOutlineBlack != null)
        {
            Color b = glowOutlineBlack.effectColor;
            b.a = blackAlpha;
            glowOutlineBlack.effectColor = b;
        }
    }

    public Vector2 GetCenterScreenPoint()
    {
        if (rect == null)
            return Vector2.zero;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.WorldToScreenPoint(cam, rect.position);
    }

    private void MoveToScreen(Vector2 screenPos, Camera cam, bool isFirstMove)
    {
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, cam, out Vector2 localPoint))
            return;

        localPoint = ClampToParentRect(localPoint, parent);

        if (!isFirstMove)
        {
            float deltaX = localPoint.x - lastDragAnchoredPos.x;
            float tilt = Mathf.Clamp(deltaX * 0.08f, -maxTiltDegrees, maxTiltDegrees);
            rect.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        lastDragAnchoredPos = localPoint;
        rect.anchoredPosition = localPoint;
    }

    private Vector2 GetVisualHalfSize()
    {
        float scale = Mathf.Max(rect.localScale.x, rect.localScale.y);
        Vector2 half = Vector2.Scale(rect.rect.size, rect.localScale) * 0.5f;
        float glowPad = 34f * scale;
        return half + new Vector2(glowPad, glowPad);
    }

    private Vector2 ClampToParentRect(Vector2 localPoint, RectTransform parent)
    {
        Rect bounds = parent.rect;
        Vector2 half = GetVisualHalfSize();
        float pad = boundsPadding;

        float minX = bounds.xMin + half.x + pad;
        float maxX = bounds.xMax - half.x - pad;
        float minY = bounds.yMin + half.y + pad;
        float maxY = bounds.yMax - half.y - pad;

        if (minX > maxX)
            localPoint.x = (bounds.xMin + bounds.xMax) * 0.5f;
        else
            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);

        if (minY > maxY)
            localPoint.y = (bounds.yMin + bounds.yMax) * 0.5f;
        else
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        return localPoint;
    }
}

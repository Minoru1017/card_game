using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BattleHandHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform overlayRoot;
    public float previewScale = 1.08f;
    public float tooltipHoldSeconds = 2f;
    public float hoverLeaveGraceSeconds = 0.06f;
    public float hoverAnimDuration = 0.16f;

    private RectTransform sourceRect;
    private GameObject previewGhost;
    private RectTransform previewRect;
    private bool hovering;
    private bool pointerOverCard;
    private bool pointerOverGhost;
    private Camera pointerEventCamera;
    private float hoverEnterTime;
    private bool tooltipShown;
    private string tooltipMessage;
    private System.Action<RectTransform, string> onShowTooltip;
    private System.Action onHideTooltip;
    private Coroutine delayedStopRoutine;
    private Coroutine hoverAnimRoutine;
    private CanvasGroup previewCanvasGroup;
    private static BattleHandHoverPreview activePreview;
    private float currentScaleFactor = 1f;

    /// <summary>若回傳 true，不建立幽靈預覽與縮放動畫（與手牌根節點透明度調低時關閉 hover 並用）。</summary>
    public Func<bool> shouldSuppressHeavyHoverEffects;

    public void Setup(
        RectTransform source,
        RectTransform overlay,
        float scale,
        float holdSeconds,
        string message,
        System.Action<RectTransform, string> showTooltip,
        System.Action hideTooltip)
    {
        sourceRect = source;
        overlayRoot = overlay;
        previewScale = Mathf.Max(1f, scale);
        tooltipHoldSeconds = Mathf.Max(0.1f, holdSeconds);
        tooltipMessage = message;
        onShowTooltip = showTooltip;
        onHideTooltip = hideTooltip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shouldSuppressHeavyHoverEffects != null && shouldSuppressHeavyHoverEffects()) return;
        pointerEventCamera = eventData != null ? eventData.pressEventCamera : null;
        pointerOverCard = true;
        if (IsPointerInActiveHoverZone(sourceRect))
        {
            StartHoverVisuals();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shouldSuppressHeavyHoverEffects != null && shouldSuppressHeavyHoverEffects()) return;
        pointerEventCamera = eventData != null ? eventData.pressEventCamera : pointerEventCamera;
        pointerOverCard = false;
        if (!pointerOverGhost) ScheduleStopHoverVisuals();
    }

    private void LateUpdate()
    {
        if (shouldSuppressHeavyHoverEffects != null && shouldSuppressHeavyHoverEffects())
        {
            if (hovering) StopHoverVisuals();
            return;
        }

        // Immediate cancel rule:
        // if player clicks anywhere outside this card/its preview ghost, close hover effects.
        if (hovering && Input.GetMouseButtonDown(0))
        {
            bool clickOnSource = IsPointerInsideRect(sourceRect);
            bool clickOnGhost = previewRect != null && IsPointerInsideRect(previewRect);
            if (!clickOnSource && !clickOnGhost)
            {
                StopHoverVisuals();
                return;
            }
        }

        // Probe pointer position directly every frame; do not rely only on enter/exit flags.
        // This improves reliability for edge cards in fan layout.
        bool inCardZone = IsPointerInActiveHoverZone(sourceRect);
        bool inGhostZone = previewRect != null && IsPointerInActiveHoverZone(previewRect);
        bool inActiveZone = inCardZone || inGhostZone;
        if (inActiveZone)
        {
            CancelScheduledStop();
            if (!hovering) StartHoverVisuals();
        }
        else if (hovering)
        {
            // Use grace close instead of immediate hide to avoid flicker.
            ScheduleStopHoverVisuals();
        }

        if (!hovering) return;
        if (previewGhost != null) SyncPreviewTransform();
        if (!tooltipShown &&
            !string.IsNullOrWhiteSpace(tooltipMessage) &&
            Time.unscaledTime - hoverEnterTime >= tooltipHoldSeconds)
        {
            onShowTooltip?.Invoke(sourceRect, tooltipMessage);
            tooltipShown = true;
        }
    }

    private void OnDisable()
    {
        pointerOverCard = false;
        StopHoverVisuals();
    }

    private void OnDestroy()
    {
        DestroyPreviewGhost();
    }

    private void CreatePreviewGhost()
    {
        if (previewGhost != null || sourceRect == null || overlayRoot == null) return;

        previewGhost = Instantiate(sourceRect.gameObject, overlayRoot);
        previewGhost.name = sourceRect.gameObject.name + "_HoverPreview";
        previewRect = previewGhost.GetComponent<RectTransform>();
        if (previewRect == null) previewRect = previewGhost.AddComponent<RectTransform>();
        previewRect.SetAsLastSibling();
        previewCanvasGroup = previewGhost.GetComponent<CanvasGroup>();
        if (previewCanvasGroup == null) previewCanvasGroup = previewGhost.AddComponent<CanvasGroup>();
        previewCanvasGroup.alpha = 1f;

        DisablePreviewInteractions(previewGhost);
        AttachGhostHoverCapture(previewGhost);
    }

    private void DisablePreviewInteractions(GameObject root)
    {
        if (root == null) return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++) buttons[i].enabled = false;

        BattleHandLongPressTooltip[] longPresses = root.GetComponentsInChildren<BattleHandLongPressTooltip>(true);
        for (int i = 0; i < longPresses.Length; i++) longPresses[i].enabled = false;

        ClickCard[] clickCards = root.GetComponentsInChildren<ClickCard>(true);
        for (int i = 0; i < clickCards.Length; i++) clickCards[i].enabled = false;

        ZoomUI[] zooms = root.GetComponentsInChildren<ZoomUI>(true);
        for (int i = 0; i < zooms.Length; i++) zooms[i].enabled = false;

        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg == null) cg = root.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++) images[i].raycastTarget = false;

        Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyTexts.Length; i++) legacyTexts[i].raycastTarget = false;

        TextMeshProUGUI[] tmpTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmpTexts.Length; i++) tmpTexts[i].raycastTarget = false;
    }

    private void SyncPreviewTransform()
    {
        if (previewRect == null || sourceRect == null || overlayRoot == null) return;
        // Match source card world transform first, then apply preview scale.
        previewRect.position = sourceRect.position;
        previewRect.rotation = sourceRect.rotation;
        previewRect.anchorMin = sourceRect.anchorMin;
        previewRect.anchorMax = sourceRect.anchorMax;
        previewRect.pivot = sourceRect.pivot;
        previewRect.sizeDelta = sourceRect.sizeDelta;
        previewRect.localScale = GetLocalScaleFromWorld(sourceRect, overlayRoot, previewScale * currentScaleFactor);
    }

    private Vector3 GetLocalScaleFromWorld(RectTransform source, RectTransform targetParent, float scaleMul)
    {
        Vector3 sourceWorld = source.lossyScale * scaleMul;
        Vector3 parentWorld = targetParent != null ? targetParent.lossyScale : Vector3.one;
        return new Vector3(
            parentWorld.x != 0f ? sourceWorld.x / parentWorld.x : sourceWorld.x,
            parentWorld.y != 0f ? sourceWorld.y / parentWorld.y : sourceWorld.y,
            parentWorld.z != 0f ? sourceWorld.z / parentWorld.z : sourceWorld.z
        );
    }

    private void DestroyPreviewGhost()
    {
        if (hoverAnimRoutine != null)
        {
            StopCoroutine(hoverAnimRoutine);
            hoverAnimRoutine = null;
        }
        if (previewGhost != null)
        {
            Destroy(previewGhost);
            previewGhost = null;
            previewRect = null;
        }
        previewCanvasGroup = null;
        pointerOverGhost = false;
    }

    private void StartHoverVisuals()
    {
        CancelScheduledStop();
        if (activePreview != null && activePreview != this)
        {
            activePreview.ForceStopNow();
        }
        activePreview = this;
        hovering = true;
        hoverEnterTime = Time.unscaledTime;
        tooltipShown = false;
        currentScaleFactor = 0.94f;
        CreatePreviewGhost();
        SyncPreviewTransform();
        StartHoverAnim(show: true);
    }

    private void StopHoverVisuals()
    {
        CancelScheduledStop();
        hovering = false;
        currentScaleFactor = 1f;
        if (tooltipShown)
        {
            onHideTooltip?.Invoke();
            tooltipShown = false;
        }
        StartHoverAnim(show: false);
        if (activePreview == this) activePreview = null;
    }

    private bool IsPointerInActiveHoverZone(RectTransform targetRect)
    {
        if (targetRect == null) return false;
        Camera cam = pointerEventCamera;
        if (cam == null)
        {
            Canvas c = targetRect.GetComponentInParent<Canvas>();
            if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) cam = c.worldCamera;
        }

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, Input.mousePosition, cam, out localPoint))
            return false;

        Rect r = targetRect.rect;
        if (!r.Contains(localPoint)) return false;

        // Active area: from card top edge to the point 1/3 above bottom edge.
        // (Bottom 1/3 is inactive.)
        float inactiveBottomBoundaryY = r.yMin + r.height * (1f / 3f);
        return localPoint.y >= inactiveBottomBoundaryY;
    }

    private bool IsPointerInsideRect(RectTransform targetRect)
    {
        if (targetRect == null) return false;
        Camera cam = pointerEventCamera;
        if (cam == null)
        {
            Canvas c = targetRect.GetComponentInParent<Canvas>();
            if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) cam = c.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(targetRect, Input.mousePosition, cam);
    }

    private void AttachGhostHoverCapture(GameObject ghostRoot)
    {
        if (ghostRoot == null) return;
        GameObject capture = new GameObject("HoverCapture", typeof(RectTransform), typeof(Image), typeof(BattleHandHoverCaptureProxy));
        capture.transform.SetParent(ghostRoot.transform, false);
        RectTransform rt = capture.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = capture.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.001f);
        img.raycastTarget = true;
        BattleHandHoverCaptureProxy proxy = capture.GetComponent<BattleHandHoverCaptureProxy>();
        proxy.owner = this;
    }

    public void NotifyGhostPointerEnter(PointerEventData eventData)
    {
        if (shouldSuppressHeavyHoverEffects != null && shouldSuppressHeavyHoverEffects()) return;
        pointerEventCamera = eventData != null ? eventData.pressEventCamera : pointerEventCamera;
        pointerOverGhost = true;
        CancelScheduledStop();
        if (!hovering) StartHoverVisuals();
    }

    public void NotifyGhostPointerExit(PointerEventData eventData)
    {
        if (shouldSuppressHeavyHoverEffects != null && shouldSuppressHeavyHoverEffects()) return;
        pointerEventCamera = eventData != null ? eventData.pressEventCamera : pointerEventCamera;
        pointerOverGhost = false;
        if (!pointerOverCard) ScheduleStopHoverVisuals();
    }

    private void ScheduleStopHoverVisuals()
    {
        if (delayedStopRoutine != null) return;
        if (!gameObject.activeInHierarchy) return;
        delayedStopRoutine = StartCoroutine(DelayedStopHoverVisuals());
    }

    private IEnumerator DelayedStopHoverVisuals()
    {
        float wait = Mathf.Max(0f, hoverLeaveGraceSeconds);
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        bool stillInCardZone = IsPointerInActiveHoverZone(sourceRect);
        bool stillInGhostZone = previewRect != null && IsPointerInActiveHoverZone(previewRect);
        if (!stillInCardZone && !stillInGhostZone)
        {
            StopHoverVisuals();
        }
        delayedStopRoutine = null;
    }

    private void CancelScheduledStop()
    {
        if (delayedStopRoutine != null)
        {
            StopCoroutine(delayedStopRoutine);
            delayedStopRoutine = null;
        }
    }

    private void ForceStopNow()
    {
        pointerOverCard = false;
        pointerOverGhost = false;
        StopHoverVisuals();
    }

    private void StartHoverAnim(bool show)
    {
        if (previewGhost == null || previewRect == null) return;
        if (hoverAnimRoutine != null)
        {
            StopCoroutine(hoverAnimRoutine);
            hoverAnimRoutine = null;
        }
        // OnDisable / hand rebuild deactivates this object; cannot StartCoroutine then.
        if (!gameObject.activeInHierarchy)
        {
            if (!show) DestroyPreviewGhost();
            return;
        }
        hoverAnimRoutine = StartCoroutine(HoverAnimRoutine(show));
    }

    private IEnumerator HoverAnimRoutine(bool show)
    {
        if (previewRect == null) yield break;
        float dur = Mathf.Max(0.01f, hoverAnimDuration);
        float t = 0f;

        if (show)
        {
            SyncPreviewTransform();
            float fromFactor = currentScaleFactor;
            float toFactor = 1f;

            while (t < dur && previewRect != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                float eased = EaseOutBack(p);
                currentScaleFactor = Mathf.Lerp(fromFactor, toFactor, eased);
                SyncPreviewTransform();
                yield return null;
            }

            currentScaleFactor = 1f;
            SyncPreviewTransform();
        }
        else
        {
            float fromFactor = currentScaleFactor <= 0f ? 1f : currentScaleFactor;
            float toFactor = 0.96f;

            while (t < dur && previewRect != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                float eased = EaseInBack(p);
                currentScaleFactor = Mathf.Lerp(fromFactor, toFactor, eased);
                SyncPreviewTransform();
                yield return null;
            }
        }

        hoverAnimRoutine = null;
        if (!hovering)
        {
            DestroyPreviewGhost();
        }
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseInBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * x * x * x - c1 * x * x;
    }
}

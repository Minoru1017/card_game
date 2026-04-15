using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ZoomUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Zoom")]
    public float zoomSize = 1.2f;
    public float zoomDuration = 0.12f;
    public bool clickToToggle = true;
    public bool clickPulseOnly = false;
    public bool hoverToPreview = false;
    public bool raiseToFrontWhenZoomed = true;

    private static ZoomUI activeZoom;

    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;
    private int baseSiblingIndex = -1;
    private bool isZoomed;
    private Coroutine zoomRoutine;

    /// <summary>若回傳 true，略過縮放／點擊脈衝（手牌根節點透明度調低時關閉以省效能）。</summary>
    public Func<bool> shouldSuppressScaleEffects;

    void Awake()
    {
        rect = transform as RectTransform;
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (rect == null) rect = transform as RectTransform;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (shouldSuppressScaleEffects == null || !shouldSuppressScaleEffects()) return;
        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }
        if (activeZoom == this) activeZoom = null;
        isZoomed = false;
        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shouldSuppressScaleEffects != null && shouldSuppressScaleEffects()) return;
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (clickPulseOnly)
        {
            StartZoomTween(new Vector3(zoomSize, zoomSize, 1f), true);
            return;
        }
        if (!clickToToggle) return;
        if (activeZoom != null && activeZoom != this)
        {
            activeZoom.SetZoomed(false, true);
        }

        bool next = !isZoomed;
        SetZoomed(next, true);
        activeZoom = isZoomed ? this : null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shouldSuppressScaleEffects != null && shouldSuppressScaleEffects()) return;
        if (!hoverToPreview || clickToToggle || isZoomed) return;
        SetZoomed(true, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shouldSuppressScaleEffects != null && shouldSuppressScaleEffects()) return;
        if (!hoverToPreview || clickToToggle || !isZoomed) return;
        SetZoomed(false, false);
    }

    private void SetZoomed(bool zoomed, bool rememberSelection)
    {
        if (isZoomed == zoomed) return;
        isZoomed = zoomed;

        if (zoomed)
        {
            baseScale = transform.localScale;
            if (raiseToFrontWhenZoomed && transform.parent != null)
            {
                baseSiblingIndex = transform.GetSiblingIndex();
                transform.SetAsLastSibling();
            }
        }
        else
        {
            if (raiseToFrontWhenZoomed && transform.parent != null && baseSiblingIndex >= 0)
            {
                int max = transform.parent.childCount - 1;
                transform.SetSiblingIndex(Mathf.Clamp(baseSiblingIndex, 0, max));
            }
            baseSiblingIndex = -1;
        }

        Vector3 target = zoomed ? new Vector3(zoomSize, zoomSize, 1f) : Vector3.one;
        StartZoomTween(target);

        if (rememberSelection)
        {
            if (zoomed) activeZoom = this;
            else if (activeZoom == this) activeZoom = null;
        }
    }

    private void StartZoomTween(Vector3 targetScale, bool bounceBack = false)
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomTweenRoutine(targetScale, bounceBack));
    }

    private IEnumerator ZoomTweenRoutine(Vector3 targetScale, bool bounceBack)
    {
        Vector3 from = transform.localScale;
        float t = 0f;
        float dur = Mathf.Max(0.01f, zoomDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            // smoothstep easing
            float eased = p * p * (3f - 2f * p);
            transform.localScale = Vector3.Lerp(from, targetScale, eased);
            yield return null;
        }
        transform.localScale = targetScale;
        if (bounceBack)
        {
            t = 0f;
            Vector3 backFrom = transform.localScale;
            Vector3 backTo = Vector3.one;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                float eased = p * p * (3f - 2f * p);
                transform.localScale = Vector3.Lerp(backFrom, backTo, eased);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
        zoomRoutine = null;
    }

    void OnDisable()
    {
        if (activeZoom == this) activeZoom = null;
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = null;
        transform.localScale = Vector3.one;
    }
}

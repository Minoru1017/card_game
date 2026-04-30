using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UISelectionFrameEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private sealed class SelectionRingOwnerTag : MonoBehaviour
    {
        public UISelectionFrameEffect owner;
    }

    [SerializeField] private Color frameColor = new Color(0.82f, 1f, 0.18f, 1f);
    [SerializeField] private float pulseAmplitude = 0.22f;
    [SerializeField] private float pulseSpeed = 4.8f;
    [SerializeField] private float outerScale = 1.12f;
    [SerializeField] private float alphaMul = 1f;
    [SerializeField] private float fadeDuration = 0.14f;
    [SerializeField] private float scaleSmoothTime = 0.08f;

    private Image targetImage;
    private RectTransform targetRt;
    private Image ringImage;
    private RectTransform ringRt;
    private bool highlighted;
    private float visibleWeight;
    private float scaleVelocity;

    private void Awake()
    {
        ringImage = null;
        ringRt = null;
        EnsureRing();
        SetVisible(false, true);
    }

    private void OnEnable()
    {
        ringImage = null;
        ringRt = null;
        EnsureRing();
        SetVisible(false, true);
        highlighted = false;
    }

    private void OnDestroy()
    {
        if (ringRt != null)
        {
            Destroy(ringRt.gameObject);
            ringRt = null;
            ringImage = null;
        }
    }

    private void Update()
    {
        if (ringRt != null && targetRt != null)
        {
            ringRt.anchorMin = targetRt.anchorMin;
            ringRt.anchorMax = targetRt.anchorMax;
            ringRt.pivot = targetRt.pivot;
            ringRt.anchoredPosition = targetRt.anchoredPosition;
            ringRt.sizeDelta = targetRt.sizeDelta;
        }

        if (ringImage == null || ringRt == null) return;
        float targetWeight = highlighted ? 1f : 0f;
        float fadeDur = Mathf.Max(0.01f, fadeDuration);
        visibleWeight = Mathf.MoveTowards(visibleWeight, targetWeight, Time.unscaledDeltaTime / fadeDur);

        float pulse = 1f - pulseAmplitude + Mathf.Sin(Time.unscaledTime * pulseSpeed) * (pulseAmplitude * 0.5f) + (pulseAmplitude * 0.5f);
        float minScale = Mathf.Clamp(outerScale - pulseAmplitude * 0.3f, 1.01f, 1.35f);
        float maxScale = Mathf.Clamp(outerScale + pulseAmplitude * 0.3f, 1.01f, 1.35f);
        float targetScale = Mathf.Lerp(minScale, maxScale, pulse) * Mathf.Lerp(0.98f, 1f, visibleWeight);
        float currentScale = ringRt.localScale.x;
        float smooth = Mathf.Max(0.01f, scaleSmoothTime);
        float nextScale = Mathf.SmoothDamp(currentScale, targetScale, ref scaleVelocity, smooth, Mathf.Infinity, Time.unscaledDeltaTime);
        ringRt.localScale = Vector3.one * nextScale;

        Color c = frameColor;
        c.a *= pulse * Mathf.Clamp01(alphaMul) * visibleWeight;
        ringImage.color = c;
        ringImage.enabled = c.a > 0.01f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        highlighted = true;
        SetVisible(true, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        highlighted = false;
        SetVisible(false, false);
    }

    private void EnsureRing()
    {
        if (targetRt == null) targetRt = transform as RectTransform;
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetRt == null || targetImage == null) return;

        Transform parent = transform.parent;
        if (parent == null) return;

        // Instantiated objects may inherit cached references from template instance.
        // Validate cached ring ownership; otherwise clear and rebuild.
        bool cachedValid = ringImage != null &&
                           ringRt != null &&
                           ringRt.transform.parent == parent;
        if (cachedValid)
        {
            SelectionRingOwnerTag cachedTag = ringImage.GetComponent<SelectionRingOwnerTag>();
            cachedValid = cachedTag != null && cachedTag.owner == this;
        }
        if (!cachedValid)
        {
            ringImage = null;
            ringRt = null;
        }

        if (ringImage != null && ringRt != null) return;

        SelectionRingOwnerTag[] tags = parent.GetComponentsInChildren<SelectionRingOwnerTag>(true);
        for (int i = 0; i < tags.Length; i++)
        {
            SelectionRingOwnerTag tag = tags[i];
            if (tag == null || tag.owner != this) continue;
            ringRt = tag.transform as RectTransform;
            ringImage = tag.GetComponent<Image>();
            if (ringRt != null && ringImage != null)
            {
                ringImage.raycastTarget = false;
                ringImage.sprite = targetImage.sprite;
                ringImage.type = targetImage.type;
                ringImage.preserveAspect = targetImage.preserveAspect;
                ringImage.color = frameColor;
                return;
            }
        }

        GameObject ringObj = new GameObject("SelectionRing_" + GetInstanceID(), typeof(RectTransform), typeof(Image), typeof(SelectionRingOwnerTag));
        ringObj.transform.SetParent(parent, false);
        ringRt = ringObj.GetComponent<RectTransform>();
        ringImage = ringObj.GetComponent<Image>();
        SelectionRingOwnerTag ownerTag = ringObj.GetComponent<SelectionRingOwnerTag>();
        ownerTag.owner = this;
        ringImage.raycastTarget = false;
        ringImage.sprite = targetImage.sprite;
        ringImage.type = targetImage.type;
        ringImage.preserveAspect = targetImage.preserveAspect;
        ringImage.color = frameColor;

        int idx = transform.GetSiblingIndex();
        ringObj.transform.SetSiblingIndex(Mathf.Max(0, idx));
        transform.SetSiblingIndex(Mathf.Min(parent.childCount - 1, idx + 1));
    }

    private void SetVisible(bool visible)
    {
        SetVisible(visible, visible);
    }

    private void SetVisible(bool visible, bool instant)
    {
        if (ringImage == null || ringRt == null) return;
        highlighted = visible;
        if (instant)
        {
            visibleWeight = visible ? 1f : 0f;
            scaleVelocity = 0f;
            float baseScale = Mathf.Clamp(outerScale, 1.01f, 1.3f);
            ringRt.localScale = Vector3.one * baseScale;
        }
        if (visible)
        {
            ringImage.enabled = true;
        }
    }
}

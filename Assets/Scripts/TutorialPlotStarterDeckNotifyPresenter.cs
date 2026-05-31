using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>「獲得基礎牌組」通知：遮罩淡入 + 面板自下而上彈出（unscaled）。</summary>
public sealed class TutorialPlotStarterDeckNotifyPresenter : MonoBehaviour
{
    private const float DimFadeSeconds = 0.3f;
    private const float PanelPopSeconds = 0.42f;
    private const float PanelStartScale = 0.82f;
    private const float PanelPeakScale = 1.05f;
    private const float PanelSlideUpPx = 48f;
    private const float TitleDelaySeconds = 0.12f;
    private const float DismissSeconds = 0.18f;

    private Image dimImage;
    private Color dimColorFull;
    private RectTransform panelRt;
    private Vector2 panelRestAnchoredPos;
    private CanvasGroup panelCg;
    private CanvasGroup titleCg;
    private CanvasGroup bodyCg;
    private CanvasGroup hintCg;
    private Button dimButton;
    private Action onDismissed;

    private bool entranceComplete;
    private bool dismissing;

    private const float SkipBriefHoldSeconds = 1.1f;
    private const float SkipBriefFadeSeconds = 0.22f;

    public void InitializeSkipBrief(Image dim, RectTransform panel, CanvasGroup panelGroup, Button dimButton, Action dismissed)
    {
        dimImage = dim;
        dimColorFull = dim != null ? dim.color : new Color(0.08f, 0.06f, 0.05f, 0.45f);
        panelRt = panel;
        panelCg = panelGroup;
        this.dimButton = dimButton;
        onDismissed = dismissed;
        titleCg = null;
        bodyCg = null;
        hintCg = null;

        if (dimImage != null)
            dimImage.color = WithAlpha(dimColorFull, 0f);
        if (panelCg != null)
            panelCg.alpha = 0f;
        if (panelRt != null)
            panelRt.localScale = Vector3.one * 0.94f;

        if (this.dimButton != null)
        {
            this.dimButton.onClick.RemoveAllListeners();
            this.dimButton.onClick.AddListener(OnDismissClicked);
            this.dimButton.interactable = false;
        }

        StopAllCoroutines();
        StartCoroutine(CoPlaySkipBrief());
    }

    public void Initialize(
        Image dim,
        RectTransform panel,
        CanvasGroup panelGroup,
        TMP_Text title,
        TMP_Text body,
        TMP_Text hint,
        Action dismissed)
    {
        dimImage = dim;
        dimColorFull = dim != null ? dim.color : new Color(0.08f, 0.06f, 0.05f, 0.55f);
        panelRt = panel;
        panelCg = panelGroup;
        onDismissed = dismissed;

        if (panelRt != null)
            panelRestAnchoredPos = panelRt.anchoredPosition;

        titleCg = EnsureChildCanvasGroup(title);
        bodyCg = EnsureChildCanvasGroup(body);
        hintCg = EnsureChildCanvasGroup(hint);

        dimButton = dim != null ? dim.GetComponent<Button>() : null;
        if (dimButton != null)
        {
            dimButton.onClick.RemoveAllListeners();
            dimButton.onClick.AddListener(OnDismissClicked);
            dimButton.interactable = false;
        }

        if (dimImage != null)
            dimImage.color = WithAlpha(dimColorFull, 0f);
        if (panelCg != null)
            panelCg.alpha = 0f;
        if (panelRt != null)
        {
            panelRt.localScale = Vector3.one * PanelStartScale;
            panelRt.anchoredPosition = panelRestAnchoredPos + new Vector2(0f, -PanelSlideUpPx);
        }

        SetTextGroupsAlpha(0f);

        StopAllCoroutines();
        StartCoroutine(CoPlayEntrance());
    }

    private static CanvasGroup EnsureChildCanvasGroup(TMP_Text text)
    {
        if (text == null) return null;
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = text.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        return cg;
    }

    private void SetTextGroupsAlpha(float alpha)
    {
        if (titleCg != null) titleCg.alpha = alpha;
        if (bodyCg != null) bodyCg.alpha = alpha;
        if (hintCg != null) hintCg.alpha = alpha;
    }

    private void OnDismissClicked()
    {
        if (!entranceComplete || dismissing)
            return;
        StartCoroutine(CoPlayDismiss());
    }

    private IEnumerator CoPlaySkipBrief()
    {
        float fadeIn = SkipBriefFadeSeconds;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = SmoothStep(Mathf.Clamp01(elapsed / fadeIn));
            if (dimImage != null)
                dimImage.color = WithAlpha(dimColorFull, dimColorFull.a * t);
            if (panelCg != null)
                panelCg.alpha = t;
            if (panelRt != null)
                panelRt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, t);
            yield return null;
        }

        if (dimImage != null)
            dimImage.color = dimColorFull;
        if (panelCg != null)
            panelCg.alpha = 1f;
        if (panelRt != null)
            panelRt.localScale = Vector3.one;

        entranceComplete = true;
        if (dimButton != null)
            dimButton.interactable = true;

        yield return new WaitForSecondsRealtime(SkipBriefHoldSeconds);

        if (!dismissing)
            StartCoroutine(CoPlayDismiss());
    }

    private IEnumerator CoPlayEntrance()
    {
        float elapsed = 0f;
        float total = PanelPopSeconds + 0.08f;

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;

            if (dimImage != null)
            {
                float dimT = Mathf.Clamp01(elapsed / DimFadeSeconds);
                dimImage.color = WithAlpha(dimColorFull, dimColorFull.a * SmoothStep(dimT));
            }

            if (panelRt != null && panelCg != null)
            {
                float panelT = Mathf.Clamp01(elapsed / PanelPopSeconds);
                float popEased = EaseOutBack(panelT);

                float scale = panelT < 0.7f
                    ? Mathf.LerpUnclamped(PanelStartScale, PanelPeakScale, popEased / 0.7f)
                    : Mathf.Lerp(PanelPeakScale, 1f, (panelT - 0.7f) / 0.3f);
                panelRt.localScale = Vector3.one * scale;

                float slideT = SmoothStep(panelT);
                panelRt.anchoredPosition = Vector2.LerpUnclamped(
                    panelRestAnchoredPos + new Vector2(0f, -PanelSlideUpPx),
                    panelRestAnchoredPos,
                    slideT);

                panelCg.alpha = 1f - Mathf.Pow(1f - panelT, 2.4f);
            }

            float textElapsed = Mathf.Max(0f, elapsed - TitleDelaySeconds);
            float titleA = SmoothStep(Mathf.Clamp01(textElapsed / 0.22f));
            float bodyA = SmoothStep(Mathf.Clamp01((textElapsed - 0.06f) / 0.24f));
            float hintA = SmoothStep(Mathf.Clamp01((textElapsed - 0.14f) / 0.26f));
            if (titleCg != null) titleCg.alpha = titleA;
            if (bodyCg != null) bodyCg.alpha = bodyA;
            if (hintCg != null) hintCg.alpha = hintA;

            yield return null;
        }

        if (dimImage != null)
            dimImage.color = dimColorFull;
        if (panelRt != null)
        {
            panelRt.localScale = Vector3.one;
            panelRt.anchoredPosition = panelRestAnchoredPos;
        }
        if (panelCg != null)
            panelCg.alpha = 1f;
        SetTextGroupsAlpha(1f);

        entranceComplete = true;
        if (dimButton != null)
            dimButton.interactable = true;
    }

    private IEnumerator CoPlayDismiss()
    {
        dismissing = true;
        if (dimButton != null)
            dimButton.interactable = false;

        float startDimA = dimImage != null ? dimImage.color.a : 0f;
        float startPanelA = panelCg != null ? panelCg.alpha : 1f;
        Vector3 startScale = panelRt != null ? panelRt.localScale : Vector3.one;
        Vector2 startPos = panelRt != null ? panelRt.anchoredPosition : panelRestAnchoredPos;

        float elapsed = 0f;
        while (elapsed < DismissSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = SmoothStep(Mathf.Clamp01(elapsed / DismissSeconds));

            if (dimImage != null)
                dimImage.color = WithAlpha(dimColorFull, Mathf.Lerp(startDimA, 0f, t));
            if (panelCg != null)
                panelCg.alpha = Mathf.Lerp(startPanelA, 0f, t);
            if (panelRt != null)
            {
                panelRt.localScale = Vector3.Lerp(startScale, Vector3.one * 0.9f, t);
                panelRt.anchoredPosition = Vector2.Lerp(startPos, startPos + new Vector2(0f, -24f), t);
            }

            SetTextGroupsAlpha(Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        onDismissed?.Invoke();
        Destroy(gameObject);
    }

    private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private static float SmoothStep(float t) => t * t * (3f - 2f * t);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「You will die」致死預警演出：偵測到敵方下一步會打死我方英雄時，於該步執行前觸發。
/// 流程：畫面忽然轉暗 → 「You will die」圖示出現 2 秒 → 淡出 → 致死一擊以慢動作播放。
/// 慢動作實作：本專案戰鬥演出皆走 unscaled time，故不用 Time.timeScale，
/// 改由 <see cref="FxDurationMultiplier"/> 讓窗口內開始的攻擊／火球演出時長放大（速度變慢）。
/// </summary>
public static class LethalBlowCinematicFx
{
    /// <summary>圖示完整顯示的持續秒數（不含淡入淡出）。</summary>
    public const float IconHoldSeconds = 2f;

    /// <summary>高於全域導覽（6000）／敵方頭像橋接（6000），低於場景轉場門檻（60000）。</summary>
    private const int OverlaySortOrder = 32000;

    private const float DimFadeInSeconds = 0.22f;
    private const float IconFadeInSeconds = 0.3f;
    private const float FadeOutSeconds = 0.32f;
    private const float DimTargetAlpha = 0.97f;
    private const float IconSize = 460f;

    /// <summary>慢動作倍率：窗口內開始的戰鬥演出時長乘上此值。</summary>
    private const float SlowMotionDurationMultiplier = 3.5f;

    private static float slowMotionWindowUntilRealtime;

    /// <summary>致死慢動作窗口開啟（You will die 警示播畢後、致死一擊開始前）。</summary>
    public static event System.Action SlowMotionBegan;

    /// <summary>致死慢動作窗口關閉（致死一擊演出播畢）。</summary>
    public static event System.Action SlowMotionEnded;

    /// <summary>目前戰鬥演出應使用的時長倍率（非慢動作窗口內為 1）。</summary>
    public static float FxDurationMultiplier =>
        Time.realtimeSinceStartup < slowMotionWindowUntilRealtime ? SlowMotionDurationMultiplier : 1f;

    /// <summary>將基礎演出秒數依慢動作窗口放大。</summary>
    public static float ScaleDuration(float seconds) => seconds * FxDurationMultiplier;

    /// <summary>開啟慢動作窗口：窗口內「開始」的攻擊／火球演出會以慢動作播放；到時自動失效。</summary>
    public static void BeginSlowMotionWindow(float maxRealSeconds)
    {
        slowMotionWindowUntilRealtime = Time.realtimeSinceStartup + Mathf.Max(0f, maxRealSeconds);
        SlowMotionBegan?.Invoke();
    }

    /// <summary>致死一擊演出播畢後提前關閉窗口，避免影響後續演出。</summary>
    public static void EndSlowMotionWindow()
    {
        slowMotionWindowUntilRealtime = 0f;
        SlowMotionEnded?.Invoke();
    }

    /// <summary>播放全屏警示（忽然轉暗 + 圖示 2 秒 + 整體淡出）；期間阻擋點擊。</summary>
    public static IEnumerator CoPlayWarning()
    {
        var root = new GameObject("YouWillDieCinematicOverlay", typeof(RectTransform), typeof(CanvasGroup));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        CanvasGroup rootCg = root.GetComponent<CanvasGroup>();
        rootCg.alpha = 1f;
        rootCg.blocksRaycasts = true;
        rootCg.interactable = false;

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimGo.GetComponent<Image>();
        Color dimColor = new Color(0f, 0f, 0f, 0f);
        dimImg.color = dimColor;
        dimImg.raycastTarget = true;

        RectTransform iconRt = CreateIcon(root.transform, out CanvasGroup iconCg);

        // 1) 畫面忽然轉暗
        float t = 0f;
        while (t < DimFadeInSeconds && dimImg != null)
        {
            t += Time.unscaledDeltaTime;
            dimColor.a = Mathf.Lerp(0f, DimTargetAlpha, EaseOutCubic(Mathf.Clamp01(t / DimFadeInSeconds)));
            dimImg.color = dimColor;
            yield return null;
        }
        if (dimImg != null)
        {
            dimColor.a = DimTargetAlpha;
            dimImg.color = dimColor;
        }

        // 2) 圖示現身（縮放收攏 + 淡入）
        YouWillDieWarningSfx.Play();
        t = 0f;
        while (t < IconFadeInSeconds && iconRt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(Mathf.Clamp01(t / IconFadeInSeconds));
            iconCg.alpha = p;
            float s = Mathf.Lerp(1.32f, 1f, p);
            iconRt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (iconRt != null)
        {
            iconCg.alpha = 1f;
            iconRt.localScale = Vector3.one;
        }

        // 3) 停留 2 秒（帶微幅脈動）
        t = 0f;
        while (t < IconHoldSeconds && iconRt != null)
        {
            t += Time.unscaledDeltaTime;
            float pulse = 1f + 0.045f * Mathf.Sin(t * Mathf.PI * 2.4f);
            iconRt.localScale = new Vector3(pulse, pulse, 1f);
            yield return null;
        }

        // 4) 整體淡出，讓出畫面給慢動作致死一擊
        t = 0f;
        while (t < FadeOutSeconds && rootCg != null)
        {
            t += Time.unscaledDeltaTime;
            rootCg.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / FadeOutSeconds));
            yield return null;
        }

        if (root != null)
            UnityEngine.Object.Destroy(root);
    }

    private static RectTransform CreateIcon(Transform parent, out CanvasGroup iconCg)
    {
        var iconGo = new GameObject("YouWillDieIcon", typeof(RectTransform), typeof(CanvasGroup));
        iconGo.transform.SetParent(parent, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(IconSize, IconSize);
        iconCg = iconGo.GetComponent<CanvasGroup>();
        iconCg.alpha = 0f;
        iconCg.blocksRaycasts = false;

        Sprite sprite = UiSpriteLibrary.Instance != null ? UiSpriteLibrary.Instance.YouWillDieIcon : null;
        if (sprite != null)
        {
            var img = iconGo.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            float width = 820f;
            iconRt.sizeDelta = new Vector2(width, width / aspect);
        }
        else
        {
            // 圖示缺漏時的保底：純文字警示（英文不受 CJK 字型限制）。
            var tmp = iconGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "YOU WILL DIE";
            tmp.fontSize = 84f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.86f, 0.12f, 0.1f, 1f);
            tmp.raycastTarget = false;
        }

        return iconRt;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
}

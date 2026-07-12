using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-2 段考恐怖狀態進入：全屏白閃光轉場。</summary>
public static class M12PhaseAHorrorTransitionFx
{
    private const int OverlaySortOrder = 31000;
    private const float FadeInSeconds = 0.1f;
    private const float HoldAtPeakSeconds = 0.04f;
    private const float FadeOutSeconds = 0.28f;

    private static Sprite s_whiteSprite;

    public static IEnumerator CoPlayWhiteFlash(Action onPeak = null)
    {
        if (BattleAutoSimPlugin.IsRunning)
        {
            onPeak?.Invoke();
            yield break;
        }

        var root = new GameObject("M12HorrorWhiteFlashOverlay", typeof(RectTransform), typeof(CanvasGroup));
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

        var flashGo = new GameObject("WhiteFlash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(root.transform, false);
        RectTransform flashRt = flashGo.GetComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero;
        flashRt.offsetMax = Vector2.zero;
        Image flashImg = flashGo.GetComponent<Image>();
        flashImg.sprite = GetWhiteSprite();
        flashImg.color = Color.white;
        flashImg.raycastTarget = true;

        Color flashColor = Color.white;
        flashColor.a = 0f;
        flashImg.color = flashColor;

        float t = 0f;
        while (t < FadeInSeconds && flashImg != null)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(Mathf.Clamp01(t / FadeInSeconds));
            flashColor.a = p;
            flashImg.color = flashColor;
            yield return null;
        }

        if (flashImg != null)
        {
            flashColor.a = 1f;
            flashImg.color = flashColor;
        }

        onPeak?.Invoke();

        if (HoldAtPeakSeconds > 0f)
            yield return new WaitForSecondsRealtime(HoldAtPeakSeconds);

        t = 0f;
        while (t < FadeOutSeconds && flashImg != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / FadeOutSeconds);
            flashColor.a = 1f - EaseInCubic(p);
            flashImg.color = flashColor;
            yield return null;
        }

        if (root != null)
            UnityEngine.Object.Destroy(root);
    }

    private static Sprite GetWhiteSprite()
    {
        if (s_whiteSprite != null)
            return s_whiteSprite;

        Texture2D tex = Texture2D.whiteTexture;
        s_whiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return s_whiteSprite;
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private static float EaseInCubic(float t) => t * t * t;
}

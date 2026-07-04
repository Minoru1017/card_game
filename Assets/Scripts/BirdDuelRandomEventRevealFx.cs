using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>自由對戰等「隨機事件」觸發鬥鳥前的全屏演出（變暗 + 標語 + 音效）。</summary>
public static class BirdDuelRandomEventRevealFx
{
    public const string AnnounceText = "隨機事件發生！";

    private const float DimFadeInDuration = 0.32f;
    private const float TextFadeInDuration = 0.28f;
    private const float TextHoldDuration = 0.95f;
    private const float TextFadeOutDuration = 0.22f;
    private const float PanelFadeInDuration = 0.26f;

    private const float AnnounceFontSize = 58f;
    private const float AnnounceStartScale = 1.22f;

    /// <summary>在 overlay 根節點上播放開場，結束後顯示 <paramref name="panel"/>。</summary>
    public static IEnumerator CoPlayRevealThenShowPanel(
        GameObject overlayRoot,
        GameObject panel,
        TMP_FontAsset font)
    {
        if (overlayRoot == null)
            yield break;

        Image rootDim = overlayRoot.GetComponent<Image>();
        if (rootDim != null)
            rootDim.color = new Color(BirdDuelUiColors.Dim.r, BirdDuelUiColors.Dim.g, BirdDuelUiColors.Dim.b, 0f);

        GameObject revealLayer = new GameObject(
            "RandomEventRevealLayer", typeof(RectTransform), typeof(CanvasGroup));
        revealLayer.transform.SetParent(overlayRoot.transform, false);
        RectTransform revealRt = revealLayer.GetComponent<RectTransform>();
        revealRt.anchorMin = Vector2.zero;
        revealRt.anchorMax = Vector2.one;
        revealRt.offsetMin = Vector2.zero;
        revealRt.offsetMax = Vector2.zero;
        CanvasGroup revealCg = revealLayer.GetComponent<CanvasGroup>();
        revealCg.alpha = 1f;
        revealCg.blocksRaycasts = true;
        revealCg.interactable = false;

        GameObject textObj = new GameObject("AnnounceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(revealLayer.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(920f, 120f);

        TextMeshProUGUI announceTmp = textObj.GetComponent<TextMeshProUGUI>();
        BirdDuelOverlayUiBuild.ApplyFont(announceTmp, font);
        announceTmp.text = AnnounceText;
        announceTmp.fontSize = AnnounceFontSize;
        announceTmp.fontStyle = FontStyles.Bold;
        announceTmp.alignment = TextAlignmentOptions.Center;
        announceTmp.color = new Color(BirdDuelUiColors.WonderBadge.r, BirdDuelUiColors.WonderBadge.g, BirdDuelUiColors.WonderBadge.b, 0f);
        announceTmp.raycastTarget = false;
        announceTmp.enableWordWrapping = false;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(BirdDuelUiColors.HeaderBand.r, BirdDuelUiColors.HeaderBand.g, BirdDuelUiColors.HeaderBand.b, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        if (panel != null)
        {
            panel.SetActive(false);
            CanvasGroup panelCg = panel.GetComponent<CanvasGroup>();
            if (panelCg == null)
                panelCg = panel.AddComponent<CanvasGroup>();
            panelCg.alpha = 0f;
            panelCg.interactable = false;
            panelCg.blocksRaycasts = false;
        }

        // 1) 畫面忽然變暗
        float t = 0f;
        Color dimTarget = BirdDuelUiColors.Dim;
        while (t < DimFadeInDuration && overlayRoot != null)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(Mathf.Clamp01(t / DimFadeInDuration));
            if (rootDim != null)
            {
                Color c = dimTarget;
                c.a = Mathf.Lerp(0f, dimTarget.a, p);
                rootDim.color = c;
            }

            yield return null;
        }

        if (rootDim != null)
            rootDim.color = dimTarget;

        // 2) 標語出現 + 音效
        PlayRevealSfx();
        textRt.localScale = Vector3.one * AnnounceStartScale;
        t = 0f;
        while (t < TextFadeInDuration && textObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutBack(Mathf.Clamp01(t / TextFadeInDuration));
            float scale = Mathf.Lerp(AnnounceStartScale, 1f, p);
            textRt.localScale = new Vector3(scale, scale, 1f);
            Color tc = BirdDuelUiColors.WonderBadge;
            tc.a = Mathf.Lerp(0f, 1f, p);
            announceTmp.color = tc;
            yield return null;
        }

        if (announceTmp != null)
            announceTmp.color = BirdDuelUiColors.WonderBadge;
        if (textRt != null)
            textRt.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(TextHoldDuration);

        // 3) 標語淡出
        t = 0f;
        while (t < TextFadeOutDuration && announceTmp != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / TextFadeOutDuration);
            Color tc = BirdDuelUiColors.WonderBadge;
            tc.a = Mathf.Lerp(1f, 0f, p);
            announceTmp.color = tc;
            yield return null;
        }

        if (revealLayer != null)
            Object.Destroy(revealLayer);

        // 4) 選單面板淡入
        if (panel == null)
            yield break;

        panel.SetActive(true);
        CanvasGroup panelCgFinal = panel.GetComponent<CanvasGroup>();
        if (panelCgFinal == null)
            panelCgFinal = panel.AddComponent<CanvasGroup>();

        t = 0f;
        while (t < PanelFadeInDuration && panel != null)
        {
            t += Time.unscaledDeltaTime;
            float p = EaseOutCubic(Mathf.Clamp01(t / PanelFadeInDuration));
            panelCgFinal.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        if (panelCgFinal != null)
        {
            panelCgFinal.alpha = 1f;
            panelCgFinal.interactable = true;
            panelCgFinal.blocksRaycasts = true;
        }
    }

    private static void PlayRevealSfx()
    {
        if (!GameAudioUserSettings.IsMasterEnabled())
            return;

        AudioLibrary library = AudioLibrary.Instance;
        AudioClip primary = library != null ? library.MenuClickSfx : null;
        if (primary == null)
            return;

        GameObject host = new GameObject("BirdDuelRandomEventRevealSfx");
        AudioSource src = host.AddComponent<AudioSource>();
        src.clip = primary;
        src.volume = GameAudioUserSettings.ScaleButtonSfx(1f);
        src.pitch = 0.88f;
        src.spatialBlend = 0f;
        src.playOnAwake = false;
        src.Play();
        Object.Destroy(host, primary.length / Mathf.Max(0.01f, src.pitch) + 0.15f);

        AudioClip accent = library != null ? library.BirdDuelHitSfxSource : null;
        if (accent == null || accent.length < 0.05f)
            return;

        GameObject accentHost = new GameObject("BirdDuelRandomEventRevealAccentSfx");
        AudioSource accentSrc = accentHost.AddComponent<AudioSource>();
        accentSrc.clip = accent;
        accentSrc.time = Mathf.Clamp(accent.length * 0.12f, 0f, accent.length - 0.01f);
        accentSrc.volume = GameAudioUserSettings.ScaleBattleSfx(0.22f);
        accentSrc.pitch = 1.35f;
        accentSrc.spatialBlend = 0f;
        accentSrc.playOnAwake = false;
        accentSrc.Play();
        Object.Destroy(accentHost, 0.35f);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

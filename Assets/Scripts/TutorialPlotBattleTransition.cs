using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>1-1 劇情 ↔ 教學對戰／Story progress：共用圓形遮罩過場（快照縮小 → 全黑標題 → shader 光圈打開）。</summary>
public static class TutorialPlotBattleTransition
{
    public const string TitleText = "1-1 對戰模擬開始";
    public const string ReturnToStoryProgressTitleText = "返回關卡進度";

    private static TransitionHost host;
    private static Sprite whiteSprite;

    public static bool IsPlotToBattlePlaying => host != null && host.IsRunning;

    public static bool IsPlaying => IsPlotToBattlePlaying;

    public static void PlayFromPlotToBattle(string battleSceneOverride = null, bool fastCloseAnimation = false)
    {
        if (IsPlaying)
            return;

        EnsureHost().StartPlotToBattle(battleSceneOverride, fastCloseAnimation);
    }

    /// <summary>教學戰結束（勝／敗）或結尾劇情後回到 Story progress（與劇情→對戰同一套過場）。</summary>
    public static void PlayToStoryProgress(bool fastCloseAnimation = false)
    {
        if (IsPlaying)
            return;

        EnsureHost().StartToStoryProgress(fastCloseAnimation);
    }

    private static TransitionHost EnsureHost()
    {
        if (host != null)
            return host;

        var go = new GameObject(nameof(TutorialPlotBattleTransition));
        Object.DontDestroyOnLoad(go);
        host = go.AddComponent<TransitionHost>();
        return host;
    }

    private static float ComputeCoverRadius(float aspect)
    {
        float hx = 0.5f * aspect;
        float hy = 0.5f;
        return Mathf.Sqrt(hx * hx + hy * hy) + TutorialIrisTransitionTiming.RadiusMargin;
    }

    private static float CurrentAspect()
    {
        float h = Mathf.Max(1f, Screen.height);
        return Screen.width / h;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return whiteSprite;
    }

    private static float EaseInOutSine(float t)
    {
        t = Mathf.Clamp01(t);
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }

    private static float EaseOutQuart(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u * u;
    }

    private sealed class TransitionHost : MonoBehaviour
    {
        private Canvas rootCanvas;
        private IrisBlackoutGraphic irisGraphic;
        private Image solidBlackImage;
        private RectTransform snapshotPortal;
        private TextMeshProUGUI titleTmp;
        private CanvasGroup titleGroup;
        private float cachedAspect;
        private float cachedMaxRadius;
        private Texture2D plotSnapshot;
        private bool running;

        public bool IsRunning => running;

        public void StartPlotToBattle(string battleSceneOverride, bool fastClose)
        {
            StopAllCoroutines();
            StartCoroutine(RunPlotToBattle(battleSceneOverride, fastClose));
        }

        public void StartToStoryProgress(bool fastClose)
        {
            StopAllCoroutines();
            StartCoroutine(RunToStoryProgress(fastClose));
        }

        private IEnumerator RunPlotToBattle(string battleSceneOverride, bool fastClose)
        {
            yield return RunSharedIrisTransition(
                fastClose,
                titleText: TitleText,
                usePlotSnapshotFallback: false,
                suppressSourceScenes: PlotUiOverlayCleanup.SuppressMainPlotSceneRendering,
                resolveTargetScene: () => SceneLoader.PrepareIntroTutorialBattleLaunch(battleSceneOverride),
                onBeforeSceneLoad: null,
                onTargetSceneMissing: () => SceneLoader.LaunchIntroTutorialBattleFromAnywhere(battleSceneOverride));
        }

        private IEnumerator RunToStoryProgress(bool fastClose)
        {
            yield return RunSharedIrisTransition(
                fastClose,
                titleText: ReturnToStoryProgressTitleText,
                usePlotSnapshotFallback: true,
                suppressSourceScenes: PlotUiOverlayCleanup.SuppressLoadedScenesExceptStoryProgress,
                resolveTargetScene: () => StoryProgressSession.StoryProgressSceneName,
                onBeforeSceneLoad: () =>
                {
                    if (StoryProgressSession.IsTutorialPlotEpilogueActive)
                        StoryProgressSession.EndTutorialPlotEpilogueSession();
                },
                onTargetSceneMissing: null);
        }

        /// <summary>快照圓形縮小 → 全黑標題 → 載入 → IrisBlackout shader 光圈打開。</summary>
        private IEnumerator RunSharedIrisTransition(
            bool fastClose,
            string titleText,
            bool usePlotSnapshotFallback,
            System.Action suppressSourceScenes,
            System.Func<string> resolveTargetScene,
            System.Action onBeforeSceneLoad,
            System.Action onTargetSceneMissing)
        {
            running = true;
            CacheScreenMetrics();

            float closeDuration = fastClose
                ? TutorialIrisTransitionTiming.SkippedCloseDuration
                : TutorialIrisTransitionTiming.CloseDuration;
            float holdAtCenter = fastClose
                ? TutorialIrisTransitionTiming.SkippedHoldAtCenterDuration
                : TutorialIrisTransitionTiming.HoldAtCenterDuration;
            float titleFadeIn = fastClose
                ? TutorialIrisTransitionTiming.SkippedTitleFadeInDuration
                : TutorialIrisTransitionTiming.TitleFadeInDuration;
            float titleHold = fastClose
                ? TutorialIrisTransitionTiming.SkippedTitleHoldDuration
                : TutorialIrisTransitionTiming.TitleHoldDuration;
            float initBehindBlack = fastClose
                ? TutorialIrisTransitionTiming.SkippedInitBehindBlackSeconds
                : TutorialIrisTransitionTiming.InitBehindBlackSeconds;

            yield return PlaySnapshotCloseSequence(closeDuration, holdAtCenter, usePlotSnapshotFallback);
            suppressSourceScenes?.Invoke();

            yield return null;
            string targetScene = resolveTargetScene != null ? resolveTargetScene() : string.Empty;
            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError("TutorialPlotBattleTransition: target scene not in Build Settings -> " + targetScene);
                CleanupOverlay();
                running = false;
                onTargetSceneMissing?.Invoke();
                yield break;
            }

            onBeforeSceneLoad?.Invoke();
            yield return null;

            titleTmp.text = titleText;
            titleTmp.gameObject.SetActive(true);
            titleTmp.transform.SetAsLastSibling();
            solidBlackImage.gameObject.SetActive(true);
            solidBlackImage.color = Color.black;
            solidBlackImage.transform.SetAsLastSibling();
            titleTmp.transform.SetAsLastSibling();
            yield return FadeTitleTimed(0f, 1f, titleFadeIn);

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
            if (loadOp == null)
            {
                Debug.LogError("TutorialPlotBattleTransition: LoadSceneAsync failed -> " + targetScene);
                CleanupOverlay();
                running = false;
                yield break;
            }

            loadOp.allowSceneActivation = false;
            float titleElapsed = 0f;
            while (loadOp.progress < 0.9f || titleElapsed < titleHold)
            {
                titleElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            loadOp.allowSceneActivation = true;
            while (!loadOp.isDone)
                yield return null;

            yield return WaitSceneSettled();
            float initUntil = Time.unscaledTime + initBehindBlack;
            while (Time.unscaledTime < initUntil)
                yield return null;

            titleTmp.gameObject.SetActive(false);
            titleGroup.alpha = 0f;

            TutorialPlotIrisMaskUtil.EnsureSceneRenderingEnabled(targetScene);
            yield return null;
            yield return new WaitForEndOfFrame();

            if (irisGraphic != null)
                yield return PlayIrisOpen(TutorialIrisTransitionTiming.OpenDuration, usePortalOpenReveal: usePlotSnapshotFallback);
            else
                solidBlackImage.gameObject.SetActive(false);

            CleanupOverlay();
            running = false;
        }

        private void ResetSolidBlackColor()
        {
            if (solidBlackImage == null)
                return;

            solidBlackImage.color = Color.black;
        }

        private IEnumerator PlaySnapshotCloseSequence(float closeDuration, float holdAtCenter, bool usePlotSnapshotFallback)
        {
            plotSnapshot = null;
            if (usePlotSnapshotFallback)
                plotSnapshot = MainPlotSceneController.TryBuildTransitionSnapshotTexture();

            if (plotSnapshot == null)
            {
                yield return TutorialPlotIrisMaskUtil.CaptureScreenAfterFrameEnd(
                    tex => plotSnapshot = tex,
                    usePlotSnapshotFallback: usePlotSnapshotFallback);
            }

            EnsureOverlayUi();
            ResetSolidBlackColor();
            HideAllTransitionLayers();

            if (plotSnapshot == null)
                GameDevLog.LogWarning("TutorialPlotBattleTransition: screenshot failed; close uses fade to black.");
            else if (!TutorialPlotIrisMaskUtil.IsTextureMostlyBlack(plotSnapshot))
                EnsureSnapshotPortalContent();

            yield return PlaySnapshotClose(closeDuration, usePlotSnapshotFallback);
            yield return new WaitForSecondsRealtime(holdAtCenter);

            HideSnapshotPortal();
            ReleasePlotSnapshot();
            solidBlackImage.gameObject.SetActive(true);
            solidBlackImage.transform.SetAsLastSibling();
        }

        private IEnumerator WaitSceneSettled()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            float settleUntil = Time.unscaledTime + TutorialIrisTransitionTiming.PostLoadSettleSeconds;
            while (Time.unscaledTime < settleUntil)
                yield return null;
        }

        private IEnumerator PlaySnapshotClose(float duration, bool preferIrisSnapshotClose)
        {
            solidBlackImage.gameObject.SetActive(true);
            solidBlackImage.color = Color.black;
            solidBlackImage.transform.SetAsFirstSibling();

            bool hasVisibleSnapshot = plotSnapshot != null
                && !TutorialPlotIrisMaskUtil.IsTextureMostlyBlack(plotSnapshot);

            if (preferIrisSnapshotClose)
            {
                if (hasVisibleSnapshot && snapshotPortal != null)
                    yield return PlayPortalScaleClose(duration);
                else
                    yield return PlayIrisLiveClose(duration);
                yield break;
            }

            if (!hasVisibleSnapshot || snapshotPortal == null)
            {
                yield return FadeSolidBlack(0f, 1f, Mathf.Max(0.2f, duration * 0.5f));
                yield break;
            }

            yield return PlayPortalScaleClose(duration);
        }

        /// <summary>圓形快照 portal 縮小（黑底上可見劇情合成圖）。</summary>
        private IEnumerator PlayPortalScaleClose(float duration)
        {
            snapshotPortal.gameObject.SetActive(true);
            snapshotPortal.SetAsLastSibling();
            snapshotPortal.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();
            yield return null;

            if (duration <= 0f)
            {
                snapshotPortal.localScale = Vector3.one * TutorialIrisTransitionTiming.MinSnapshotPortalScale;
                yield break;
            }

            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            while (Time.unscaledTime < endTime)
            {
                float t = EaseInOutSine(Mathf.InverseLerp(startTime, endTime, Time.unscaledTime));
                float scale = Mathf.Lerp(1f, TutorialIrisTransitionTiming.MinSnapshotPortalScale, t);
                snapshotPortal.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            snapshotPortal.localScale = Vector3.one * TutorialIrisTransitionTiming.MinSnapshotPortalScale;
        }

        /// <summary>光圈縮小，洞內透出底下仍渲染中的場景（不依賴截圖）。</summary>
        private IEnumerator PlayIrisLiveClose(float duration)
        {
            HideSnapshotPortal();
            irisGraphic.gameObject.SetActive(true);
            irisGraphic.transform.SetAsLastSibling();
            irisGraphic.ClearSnapshot();
            irisGraphic.Aspect = cachedAspect;
            irisGraphic.EdgeSoftness = TutorialIrisTransitionTiming.IrisEdgeSoftness;
            irisGraphic.Radius = cachedMaxRadius;
            irisGraphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            yield return null;

            yield return AnimateIrisRadiusTimed(cachedMaxRadius, 0f, duration, EaseInOutSine);

            irisGraphic.gameObject.SetActive(false);
        }

        private IEnumerator PlayIrisOpen(float duration, bool usePortalOpenReveal)
        {
            if (usePortalOpenReveal)
            {
                yield return PlayPortalScaleOpen(duration);
                yield break;
            }

            solidBlackImage.gameObject.SetActive(false);
            irisGraphic.gameObject.SetActive(true);
            irisGraphic.transform.SetAsLastSibling();
            irisGraphic.ClearSnapshot();
            irisGraphic.Aspect = cachedAspect;
            irisGraphic.EdgeSoftness = TutorialIrisTransitionTiming.IrisEdgeSoftness;
            irisGraphic.Radius = 0f;
            irisGraphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            yield return null;

            yield return AnimateIrisRadiusTimed(0f, cachedMaxRadius, duration, EaseOutQuart);
        }

        /// <summary>載入後以圓形快照放大作為 shader 光圈備援。</summary>
        private IEnumerator PlayPortalScaleOpen(float duration)
        {
            Texture2D openSnapshot = null;
            yield return CaptureLoadedSceneSnapshot(tex => openSnapshot = tex);

            if (openSnapshot == null || TutorialPlotIrisMaskUtil.IsTextureMostlyBlack(openSnapshot))
            {
                if (openSnapshot != null)
                    Destroy(openSnapshot);
                yield break;
            }

            solidBlackImage.gameObject.SetActive(true);
            solidBlackImage.color = Color.black;
            solidBlackImage.transform.SetAsFirstSibling();
            EnsureSnapshotPortalContent(openSnapshot);
            snapshotPortal.gameObject.SetActive(true);
            snapshotPortal.SetAsLastSibling();
            snapshotPortal.localScale = Vector3.one * TutorialIrisTransitionTiming.MinSnapshotPortalScale;
            Canvas.ForceUpdateCanvases();
            yield return null;

            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            while (Time.unscaledTime < endTime)
            {
                float t = EaseOutQuart(Mathf.InverseLerp(startTime, endTime, Time.unscaledTime));
                float scale = Mathf.Lerp(
                    TutorialIrisTransitionTiming.MinSnapshotPortalScale,
                    1f,
                    t);
                snapshotPortal.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            snapshotPortal.localScale = Vector3.one;
            yield return null;
            HideSnapshotPortal();
            Destroy(openSnapshot);
            solidBlackImage.gameObject.SetActive(false);
        }

        private IEnumerator CaptureLoadedSceneSnapshot(System.Action<Texture2D> onCaptured)
        {
            Canvas[] disabled = TutorialPlotIrisMaskUtil.SetTransitionOverlayCanvasesEnabled(false);
            yield return null;
            yield return new WaitForEndOfFrame();
            onCaptured?.Invoke(TutorialPlotIrisMaskUtil.CaptureScreenToTexture());
            TutorialPlotIrisMaskUtil.SetTransitionOverlayCanvasesEnabled(disabled, true);
        }

        private IEnumerator FadeSolidBlack(float from, float to, float duration)
        {
            Color c = solidBlackImage.color;
            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            while (Time.unscaledTime < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.unscaledTime);
                c.a = Mathf.Lerp(from, to, t);
                solidBlackImage.color = c;
                yield return null;
            }

            c.a = to;
            solidBlackImage.color = c;
        }

        private void HideAllTransitionLayers()
        {
            if (titleTmp != null)
                titleTmp.gameObject.SetActive(false);
            if (titleGroup != null)
                titleGroup.alpha = 0f;
            if (solidBlackImage != null)
                solidBlackImage.gameObject.SetActive(false);
            if (irisGraphic != null)
                irisGraphic.gameObject.SetActive(false);
            HideSnapshotPortal();
        }

        private void HideSnapshotPortal()
        {
            if (snapshotPortal != null)
                snapshotPortal.gameObject.SetActive(false);
        }

        private void CacheScreenMetrics()
        {
            cachedAspect = CurrentAspect();
            cachedMaxRadius = ComputeCoverRadius(cachedAspect);
        }

        private void ReleasePlotSnapshot()
        {
            if (irisGraphic != null)
                irisGraphic.ClearSnapshot();

            if (plotSnapshot != null)
            {
                Destroy(plotSnapshot);
                plotSnapshot = null;
            }
        }

        private IEnumerator AnimateIrisRadiusTimed(
            float from,
            float to,
            float duration,
            System.Func<float, float> ease)
        {
            if (irisGraphic == null)
                yield break;

            if (duration <= 0f)
            {
                irisGraphic.Radius = to;
                yield break;
            }

            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            while (Time.unscaledTime < endTime)
            {
                float t = ease(Mathf.InverseLerp(startTime, endTime, Time.unscaledTime));
                irisGraphic.Radius = Mathf.Lerp(from, to, t);
                irisGraphic.SetAllDirty();
                yield return null;
            }

            irisGraphic.Radius = to;
            irisGraphic.SetAllDirty();
        }

        private IEnumerator FadeTitleTimed(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                titleGroup.alpha = to;
                yield break;
            }

            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            while (Time.unscaledTime < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.unscaledTime);
                t = 1f - (1f - t) * (1f - t);
                titleGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            titleGroup.alpha = to;
        }

        private void EnsureOverlayUi()
        {
            if (rootCanvas != null)
                return;

            var root = new GameObject("TutorialPlotBattleTransitionOverlay", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            rootCanvas = root.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = (int)TutorialIrisTransitionTiming.OverlaySortOrder;
            rootCanvas.pixelPerfect = false;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>().enabled = false;

            solidBlackImage = CreateFullscreenImage(root.transform, "SolidBlack", Color.black);

            var irisGo = new GameObject("IrisOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(IrisBlackoutGraphic));
            irisGo.transform.SetParent(root.transform, false);
            TutorialPlotIrisMaskUtil.StretchFull(irisGo.GetComponent<RectTransform>());
            irisGraphic = irisGo.GetComponent<IrisBlackoutGraphic>();
            irisGraphic.color = Color.white;
            irisGraphic.gameObject.SetActive(false);

            snapshotPortal = new GameObject("PlotSnapshotPortal", typeof(RectTransform)).GetComponent<RectTransform>();
            snapshotPortal.SetParent(root.transform, false);
            TutorialPlotIrisMaskUtil.StretchFull(snapshotPortal);
            snapshotPortal.gameObject.SetActive(false);

            var titleGo = new GameObject("TransitionTitle", typeof(RectTransform));
            titleGo.transform.SetParent(root.transform, false);
            TutorialPlotIrisMaskUtil.StretchFull(titleGo.GetComponent<RectTransform>());
            titleGroup = titleGo.AddComponent<CanvasGroup>();
            titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            SettingsUiFonts.ApplyTo(titleTmp);
            titleTmp.text = TitleText;
            titleTmp.fontSize = 56f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleTmp.enableWordWrapping = false;
            titleTmp.raycastTarget = false;
        }

        private void EnsureSnapshotPortalContent()
        {
            EnsureSnapshotPortalContent(plotSnapshot);
        }

        private void EnsureSnapshotPortalContent(Texture2D snapshot)
        {
            if (snapshotPortal == null || snapshot == null)
                return;

            for (int i = snapshotPortal.childCount - 1; i >= 0; i--)
                Destroy(snapshotPortal.GetChild(i).gameObject);

            TutorialPlotIrisMaskUtil.BuildShrinkingSnapshotPortal(snapshotPortal, snapshot);
        }

        private void CleanupOverlay()
        {
            ReleasePlotSnapshot();

            if (rootCanvas != null)
                Destroy(rootCanvas.gameObject);

            rootCanvas = null;
            irisGraphic = null;
            solidBlackImage = null;
            snapshotPortal = null;
            titleTmp = null;
            titleGroup = null;
        }

        private static Image CreateFullscreenImage(Transform parent, string objectName, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            TutorialPlotIrisMaskUtil.StretchFull(rt);
            Image img = go.GetComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}

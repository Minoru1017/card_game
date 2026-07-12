using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 關卡進場演出（1-1／1-2 共用）：畫面漸暗 → 關卡標題＋達成目標卡 → 背後載入目標場景 →
/// <see cref="IrisBlackoutGraphic"/>（UI/IrisBlackout shader）光圈打開接入劇情／戰鬥。
/// </summary>
public static class StoryLevelEntryTransition
{
    private const float DarkenDuration = 0.45f;
    private const float TitleFadeInDuration = 0.35f;
    private const float TitleHoldDuration = 1.7f;
    private const float TitleFadeOutDuration = 0.22f;
    /// <summary>整段演出（暗場 → 標題 → 光圈開完）保證的最短總時長。</summary>
    private const float MinPresentationSeconds = 5f;

    private const string ObjectiveHeaderHex = "#9ED4DE";
    private const float SceneLoadTimeoutSeconds = 6f;

    private static readonly Color TitleColor = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Color ObjectiveBodyColor = new Color(0.93f, 0.94f, 0.90f, 1f);
    private static readonly Color DividerColor = new Color(0.97f, 0.85f, 0.47f, 0.55f);

    private static Host host;

    public static bool IsPlaying => host != null && host.IsRunning;

    // ── 1-1 學院入門 ──────────────────────────────────────────────

    /// <summary>1-1 進關／重溫入門課：標題卡 → 入門劇情（劇情後接教學對戰）。</summary>
    public static void PlayToAcademyIntroPlot(bool replay)
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopy.LevelTitle,
            replay ? BuildAcademyReplayObjectivesText() : BuildAcademyIntroObjectivesText(),
            () => StoryProgressSession.LaunchTutorialPlotScene(battleAfterPlot: true),
            StoryProgressSession.MainPlotSceneName);
    }

    /// <summary>1-1 已畢業：標題卡 → 港灣訓練場戰前預覽（同場景 overlay，黑幕後開啟）。</summary>
    public static void PlayToHarborTrainingPreview()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopy.LevelTitle,
            BuildHarborChallengeObjectivesText(),
            SceneLoader.OpenHarborTrainingBattlePreviewFromStoryProgress,
            StoryProgressSession.StoryProgressSceneName);
    }

    // ── 1-2 海牆巡邏 ──────────────────────────────────────────────

    /// <summary>首次進關或整關重溫：標題卡 → 開場劇情（段考說明）。</summary>
    public static void PlayToIntroPlot(bool replay = false)
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM12.LevelTitle,
            replay ? BuildReplayObjectivesText() : BuildFullObjectivesText(),
            StoryProgressSession.LaunchM12IntroPlotScene,
            StoryProgressSession.MainPlotSceneName);
    }

    /// <summary>A 段通過後再進關：標題卡 → 中段散策劇情。</summary>
    public static void PlayToMidPatrolPlot()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM12.LevelTitle,
            BuildMidPatrolObjectivesText(),
            StoryProgressSession.LaunchM12MidPatrolPlotScene,
            StoryProgressSession.MainPlotSceneName);
    }

    /// <summary>開場已看過、A 段未過：標題卡 → 直進階段 A 戰鬥。</summary>
    public static void PlayToPhaseABattle()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM12.LevelTitle,
            BuildPhaseAObjectivesText(),
            () => SceneLoader.LaunchM12PhaseABattleDirect(),
            SceneLoader.PeekM12BattleSceneName());
    }

    /// <summary>散策已完成：標題卡 → 直進階段 B 戰鬥。</summary>
    public static void PlayToPhaseBBattle()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM12.LevelTitle,
            BuildPhaseBObjectivesText(),
            () => SceneLoader.LaunchM12PhaseBBattleDirect(),
            SceneLoader.PeekM12BattleSceneName());
    }

    // ── 目標文案 ──────────────────────────────────────────────────

    private static string BuildAcademyIntroObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "學院入門 - 跟林可姐完成教學對戰 學會出牌與戰技\n" +
        "通關獎勵 - 國王 王后 民兵 各 1 張";

    private static string BuildAcademyReplayObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>重溫入門課</b></color>\n" +
        "教學對戰再練一次 首通獎勵不重發";

    private static string BuildHarborChallengeObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "港灣實戰 - 選擇難度 擊敗快攻型敵人\n" +
        "用防守牌與法術應對 通過即解鎖 1-2";

    private static string BuildFullObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "階段 A 御三家應用 - 勝利 且 本局三戰技各觸發 1 次\n" +
        "階段 B 戰位克制教學 - 勝利 且 A+B 戰技合計達標";

    private static string BuildMidPatrolObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "海牆散策 - 巡視海牆 拾回封印殘卷\n" +
        "階段 B 戰位克制教學 - 勝利 且 A+B 戰技合計達標";

    private static string BuildPhaseAObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "階段 A 御三家應用 - 勝利 且 本局三戰技各觸發 1 次";

    private static string BuildPhaseBObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "階段 B 戰位克制教學 - 勝利 且 A+B 戰技合計達標";

    private static string BuildReplayObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>重溫關卡</b></color>\n" +
        "從頭重走 劇情 - 段考 A - 海牆散策 - 加練 B\n" +
        "首通獎勵不重發";

    /// <summary>已通關重溫或首次：標題卡 → 邊燈夜話開場劇情。</summary>
    public static void PlayToM13OpeningPlot(bool replay = false)
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM13.LevelTitle,
            replay ? BuildM13ReplayObjectivesText() : BuildM13OpeningObjectivesText(),
            StoryProgressSession.LaunchM13OpeningPlotScene,
            StoryProgressSession.MainPlotSceneName);
    }

    /// <summary>開場已看過：標題卡 → 玫瑰試煉劇情。</summary>
    public static void PlayToM13RoseTrialPlot()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM13.LevelTitle,
            BuildM13RoseTrialObjectivesText(),
            StoryProgressSession.LaunchM13RoseTrialPlotScene,
            StoryProgressSession.MainPlotSceneName);
    }

    /// <summary>岔路散策後：標題卡 → 冷爐迎測 Phase A。</summary>
    public static void PlayToM13PhaseABattle()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM13.LevelTitle,
            BuildM13PhaseAObjectivesText(),
            () => SceneLoader.LaunchM13PhaseABattleDirect(),
            SceneLoader.PeekM13BattleSceneName());
    }

    /// <summary>玫瑰試煉後：標題卡 → 分波對決 Phase B。</summary>
    public static void PlayToM13PhaseBBattle()
    {
        if (IsPlaying)
            return;

        EnsureHost().Begin(
            StoryProgressLevelCopyM13.LevelTitle,
            BuildM13PhaseBObjectivesText(),
            () => SceneLoader.LaunchM13PhaseBBattleDirect(),
            SceneLoader.PeekM13BattleSceneName());
    }

    private static string BuildM13PhaseBObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "分波對決 - 對決阿潮\n" +
        "須勝利 · 單回合直擊 ≥8 · 祝聖→修女→初級治療";

    private static string BuildM13PhaseAObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "冷爐迎測 - 前 3 回合無天氣 第 4 回合起預報\n" +
        "須勝利（任務欄細項待實裝）";

    private static string BuildM13OpeningObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "邊燈夜話 - 向燈守·賽爾立誓\n" +
        "分波鬥鳥 · 冷爐迎測 · 玫瑰試煉 · 對決阿潮 戰鬥段陸續接入";

    private static string BuildM13RoseTrialObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>達成目標</b></color>\n" +
        "玫瑰試煉 - 面對阿潮的當場證明\n" +
        "選項影響分波對決氛圍 不影響通關";

    private static string BuildM13ReplayObjectivesText() =>
        "<color=" + ObjectiveHeaderHex + "><b>重溫關卡</b></color>\n" +
        "從邊燈夜話重走 首通獎勵不重發";

    private static Host EnsureHost()
    {
        if (host != null)
            return host;

        var go = new GameObject(nameof(StoryLevelEntryTransition));
        Object.DontDestroyOnLoad(go);
        host = go.AddComponent<Host>();
        return host;
    }

    private static float ComputeCoverRadius(float aspect)
    {
        float hx = 0.5f * aspect;
        float hy = 0.5f;
        return Mathf.Sqrt(hx * hx + hy * hy) + TutorialIrisTransitionTiming.RadiusMargin;
    }

    private static float EaseOutQuart(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u * u;
    }

    private sealed class Host : MonoBehaviour
    {
        private Canvas rootCanvas;
        private Image solidBlackImage;
        private IrisBlackoutGraphic irisGraphic;
        private CanvasGroup titleGroup;
        private TextMeshProUGUI titleTmp;
        private TextMeshProUGUI objectivesTmp;
        private Image dividerImage;
        private bool running;

        public bool IsRunning => running;

        public void Begin(string title, string objectives, System.Action performLaunch, string targetSceneName)
        {
            StopAllCoroutines();
            StartCoroutine(Run(title, objectives, performLaunch, targetSceneName));
        }

        private IEnumerator Run(string title, string objectives, System.Action performLaunch, string targetSceneName)
        {
            running = true;
            float runStartTime = Time.unscaledTime;
            EnsureOverlayUi();

            titleTmp.text = title;
            objectivesTmp.text = objectives;
            titleGroup.alpha = 0f;
            titleGroup.gameObject.SetActive(true);
            solidBlackImage.gameObject.SetActive(true);
            irisGraphic.gameObject.SetActive(false);

            // 1) 畫面漸暗。
            yield return FadeBlack(0f, 1f, DarkenDuration);

            // 2) 關卡標題＋達成目標。
            yield return FadeGroup(titleGroup, 0f, 1f, TitleFadeInDuration);

            // 3) 黑幕背後載入目標場景（標題停留期間同步載入）。
            performLaunch?.Invoke();

            float holdUntil = Time.unscaledTime + TitleHoldDuration;
            while (Time.unscaledTime < holdUntil)
                yield return null;

            // 等目標場景載入完成；逾時（場景缺失等）直接收掉黑幕避免卡死。
            float loadTimeoutAt = Time.unscaledTime + SceneLoadTimeoutSeconds;
            Scene target = SceneManager.GetSceneByName(targetSceneName);
            while (!target.IsValid() || !target.isLoaded)
            {
                if (Time.unscaledTime >= loadTimeoutAt)
                {
                    Debug.LogError("StoryLevelEntryTransition: target scene not loaded -> " + targetSceneName);
                    CleanupOverlay();
                    running = false;
                    yield break;
                }

                yield return null;
                target = SceneManager.GetSceneByName(targetSceneName);
            }

            float settleUntil = Time.unscaledTime + TutorialIrisTransitionTiming.InitBehindBlackSeconds;
            while (Time.unscaledTime < settleUntil)
                yield return null;

            // 保證整段演出至少 MinPresentationSeconds：不足的時間讓標題卡多停留，
            // 扣掉還沒播的標題淡出與光圈張開時間。
            float minTitleVisibleUntil = runStartTime + MinPresentationSeconds
                - TitleFadeOutDuration - TutorialIrisTransitionTiming.OpenDuration;
            while (Time.unscaledTime < minTitleVisibleUntil)
                yield return null;

            yield return FadeGroup(titleGroup, 1f, 0f, TitleFadeOutDuration);
            titleGroup.gameObject.SetActive(false);

            // 4) shader 光圈打開接入目標場景。
            TutorialPlotIrisMaskUtil.EnsureSceneRenderingEnabled(targetSceneName);
            yield return null;
            yield return new WaitForEndOfFrame();

            float aspect = Screen.width / Mathf.Max(1f, Screen.height);
            float coverRadius = ComputeCoverRadius(aspect);
            solidBlackImage.gameObject.SetActive(false);
            irisGraphic.gameObject.SetActive(true);
            irisGraphic.transform.SetAsLastSibling();
            irisGraphic.ClearSnapshot();
            irisGraphic.color = Color.black;
            irisGraphic.Aspect = aspect;
            irisGraphic.EdgeSoftness = TutorialIrisTransitionTiming.IrisEdgeSoftness;
            irisGraphic.Radius = 0f;
            irisGraphic.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            yield return null;

            float openDuration = TutorialIrisTransitionTiming.OpenDuration;
            float startTime = Time.unscaledTime;
            float endTime = startTime + openDuration;
            while (Time.unscaledTime < endTime)
            {
                float t = EaseOutQuart(Mathf.InverseLerp(startTime, endTime, Time.unscaledTime));
                irisGraphic.Radius = Mathf.Lerp(0f, coverRadius, t);
                irisGraphic.SetAllDirty();
                yield return null;
            }

            irisGraphic.Radius = coverRadius;
            CleanupOverlay();
            running = false;
        }

        private IEnumerator FadeBlack(float from, float to, float duration)
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

        private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float startTime = Time.unscaledTime;
            float endTime = startTime + duration;
            while (Time.unscaledTime < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.unscaledTime);
                t = 1f - (1f - t) * (1f - t);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
        }

        private void EnsureOverlayUi()
        {
            if (rootCanvas != null)
                return;

            var root = new GameObject("StoryLevelEntryOverlay", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            rootCanvas = root.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = (int)TutorialIrisTransitionTiming.OverlaySortOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var blackGo = new GameObject("SolidBlack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blackGo.transform.SetParent(root.transform, false);
            TutorialPlotIrisMaskUtil.StretchFull(blackGo.GetComponent<RectTransform>());
            solidBlackImage = blackGo.GetComponent<Image>();
            solidBlackImage.color = new Color(0f, 0f, 0f, 0f);
            // 演出期間擋住底下場景的點擊。
            solidBlackImage.raycastTarget = true;

            var irisGo = new GameObject("IrisOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(IrisBlackoutGraphic));
            irisGo.transform.SetParent(root.transform, false);
            TutorialPlotIrisMaskUtil.StretchFull(irisGo.GetComponent<RectTransform>());
            irisGraphic = irisGo.GetComponent<IrisBlackoutGraphic>();
            irisGraphic.color = Color.black;
            irisGraphic.gameObject.SetActive(false);

            var titleRootGo = new GameObject("TitleCard", typeof(RectTransform), typeof(CanvasGroup));
            titleRootGo.transform.SetParent(root.transform, false);
            TutorialPlotIrisMaskUtil.StretchFull(titleRootGo.GetComponent<RectTransform>());
            titleGroup = titleRootGo.GetComponent<CanvasGroup>();
            titleGroup.blocksRaycasts = false;

            titleTmp = CreateText(titleRootGo.transform, "Title", 62f, FontStyles.Bold, TitleColor);
            RectTransform titleRt = titleTmp.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 96f);
            titleRt.sizeDelta = new Vector2(1400f, 90f);

            var dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dividerGo.transform.SetParent(titleRootGo.transform, false);
            RectTransform dividerRt = dividerGo.GetComponent<RectTransform>();
            dividerRt.anchorMin = new Vector2(0.5f, 0.5f);
            dividerRt.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRt.anchoredPosition = new Vector2(0f, 38f);
            dividerRt.sizeDelta = new Vector2(620f, 3f);
            dividerImage = dividerGo.GetComponent<Image>();
            dividerImage.color = DividerColor;
            dividerImage.raycastTarget = false;

            objectivesTmp = CreateText(titleRootGo.transform, "Objectives", 30f, FontStyles.Normal, ObjectiveBodyColor);
            RectTransform objRt = objectivesTmp.rectTransform;
            objRt.anchorMin = new Vector2(0.5f, 0.5f);
            objRt.anchorMax = new Vector2(0.5f, 0.5f);
            objRt.anchoredPosition = new Vector2(0f, -78f);
            objRt.sizeDelta = new Vector2(1400f, 200f);
            objectivesTmp.lineSpacing = 22f;

            titleGroup.gameObject.SetActive(false);
        }

        private TextMeshProUGUI CreateText(
            Transform parent, string name, float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            SettingsUiFonts.ApplyTo(tmp);
            // ApplyTo 會關閉 richText（設定頁保護行為）；目標文字需要 <color>/<b> 標籤，重新開啟。
            tmp.richText = true;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void CleanupOverlay()
        {
            if (rootCanvas != null)
                Destroy(rootCanvas.gameObject);
            rootCanvas = null;
            solidBlackImage = null;
            irisGraphic = null;
            titleGroup = null;
            titleTmp = null;
            objectivesTmp = null;
            dividerImage = null;
        }
    }
}

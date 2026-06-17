using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 輕量的畫面提示（toast）：在最上層顯示一段訊息，淡入→停留→淡出。
/// 用法：SceneToast.Show("金幣不足");
/// 特性：
///  - 自帶獨立 Canvas（極高 sortingOrder），跨場景常駐（DontDestroyOnLoad）。
///  - 不擋點擊（blocksRaycasts/raycastTarget 皆關），不會干擾底下 UI。
///  - 使用未縮放時間，遊戲暫停時仍能顯示。
///  - 文字採用支援中文的字型，避免缺字。
/// </summary>
public sealed class SceneToast : MonoBehaviour
{
    private const int OverlaySortingOrder = 32760;
    private const float FadeInSeconds = 0.15f;
    private const float FadeOutSeconds = 0.35f;
    private const float DefaultHoldSeconds = 1.6f;

    private static SceneToast instance;

    private CanvasGroup canvasGroup;
    private RectTransform plateRt;
    private Image plateImage;
    private TextMeshProUGUI label;
    private Coroutine activeRoutine;

    public static void Show(string message, float holdSeconds = DefaultHoldSeconds)
    {
        if (string.IsNullOrEmpty(message))
            return;

        EnsureInstance();
        instance.DisplayCompact(message, holdSeconds);
    }

    /// <summary>全寬黑底播報條（貫穿左右），播完才返回。供開場加成播報等需阻塞流程的場合。</summary>
    public static IEnumerator ShowFullWidthBannerAndWait(string message, float holdSeconds = 5f)
    {
        if (string.IsNullOrEmpty(message))
            yield break;

        EnsureInstance();
        yield return instance.RunFullWidthBanner(message, holdSeconds);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject("SceneToast");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneToast>();
        instance.Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;
        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        plateGo.transform.SetParent(transform, false);
        plateRt = plateGo.GetComponent<RectTransform>();
        plateRt.anchorMin = new Vector2(0.5f, 0.12f);
        plateRt.anchorMax = new Vector2(0.5f, 0.12f);
        plateRt.pivot = new Vector2(0.5f, 0.5f);
        plateRt.sizeDelta = new Vector2(680f, 96f);
        plateImage = plateGo.GetComponent<Image>();
        plateImage.color = new Color(0f, 0f, 0f, 0.78f);
        plateImage.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(plateGo.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(24f, 12f);
        labelRt.offsetMax = new Vector2(-24f, -12f);

        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 40f;
        label.color = Color.white;
        label.raycastTarget = false;
        TMP_FontAsset font = ValuablesVaultFonts.ResolveUIFont();
        if (font != null) label.font = font;
    }

    private void DisplayCompact(string message, float holdSeconds)
    {
        label.text = message;

        int lineCount = CountLines(message);
        bool multiLine = lineCount > 1 || message.Length > 28;
        if (multiLine)
        {
            plateRt.anchorMin = new Vector2(0.5f, 0.52f);
            plateRt.anchorMax = new Vector2(0.5f, 0.52f);
            plateRt.pivot = new Vector2(0.5f, 0.5f);
            plateRt.anchoredPosition = Vector2.zero;
            plateRt.sizeDelta = new Vector2(
                Mathf.Min(920f, 680f + message.Length * 0.35f),
                Mathf.Clamp(88f + lineCount * 38f, 96f, 520f));
            plateImage.color = new Color(0f, 0f, 0f, 0.78f);
            label.fontSizeMin = 20f;
            label.fontSizeMax = 32f;
            label.alignment = TextAlignmentOptions.Top;
        }
        else
        {
            plateRt.anchorMin = new Vector2(0.5f, 0.12f);
            plateRt.anchorMax = new Vector2(0.5f, 0.12f);
            plateRt.pivot = new Vector2(0.5f, 0.5f);
            plateRt.anchoredPosition = Vector2.zero;
            plateRt.sizeDelta = new Vector2(680f, 96f);
            plateImage.color = new Color(0f, 0f, 0f, 0.78f);
            label.fontSizeMin = 22f;
            label.fontSizeMax = 40f;
            label.alignment = TextAlignmentOptions.Center;
        }

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(FadeRoutine(Mathf.Max(0.1f, holdSeconds)));
    }

    private IEnumerator RunFullWidthBanner(string message, float holdSeconds)
    {
        label.text = message;

        int lineCount = CountLines(message);
        float bannerHeight = Mathf.Clamp(100f + lineCount * 40f, 120f, 420f);

        // 貫穿左右邊的黑底播報條。
        plateRt.anchorMin = new Vector2(0f, 0.5f);
        plateRt.anchorMax = new Vector2(1f, 0.5f);
        plateRt.pivot = new Vector2(0.5f, 0.5f);
        plateRt.anchoredPosition = Vector2.zero;
        plateRt.sizeDelta = new Vector2(0f, bannerHeight);
        plateImage.color = new Color(0f, 0f, 0f, 0.92f);

        label.enableAutoSizing = false;
        label.fontSize = lineCount > 4 ? 26f : 30f;
        label.alignment = TextAlignmentOptions.Center;
        label.lineSpacing = 4f;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        yield return FadeRoutine(Mathf.Max(0.5f, holdSeconds));
    }

    private static int CountLines(string message)
    {
        int lineCount = 1;
        for (int i = 0; i < message.Length; i++)
            if (message[i] == '\n') lineCount++;
        return lineCount;
    }

    private IEnumerator FadeRoutine(float holdSeconds)
    {
        yield return Fade(canvasGroup.alpha, 1f, FadeInSeconds);
        yield return new WaitForSecondsRealtime(holdSeconds);
        yield return Fade(1f, 0f, FadeOutSeconds);
        activeRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}

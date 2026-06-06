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
    private TextMeshProUGUI label;
    private Coroutine activeRoutine;

    public static void Show(string message, float holdSeconds = DefaultHoldSeconds)
    {
        if (string.IsNullOrEmpty(message))
            return;

        EnsureInstance();
        instance.Display(message, holdSeconds);
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

        // 半透明背板
        var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        plateGo.transform.SetParent(transform, false);
        var plateRt = plateGo.GetComponent<RectTransform>();
        plateRt.anchorMin = new Vector2(0.5f, 0.12f);
        plateRt.anchorMax = new Vector2(0.5f, 0.12f);
        plateRt.pivot = new Vector2(0.5f, 0.5f);
        plateRt.sizeDelta = new Vector2(680f, 96f);
        var plateImg = plateGo.GetComponent<Image>();
        plateImg.color = new Color(0f, 0f, 0f, 0.78f);
        plateImg.raycastTarget = false;

        // 文字
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(plateGo.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(24f, 12f);
        labelRt.offsetMax = new Vector2(-24f, -12f);

        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 22f;
        label.fontSizeMax = 40f;
        label.color = Color.white;
        label.raycastTarget = false;
        TMP_FontAsset font = ValuablesVaultFonts.ResolveUIFont();
        if (font != null) label.font = font;
    }

    private void Display(string message, float holdSeconds)
    {
        label.text = message;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(FadeRoutine(Mathf.Max(0.1f, holdSeconds)));
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

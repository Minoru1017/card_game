using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A-1 搭船過場（企劃 §A-1.3 幕 1→2，可點擊略過）。</summary>
public sealed class SideQuestA1VoyageOverlay : MonoBehaviour
{
    private const float DurationSeconds = 15f;

    private Action onFinished;
    private float elapsed;
    private TextMeshProUGUI captionTmp;
    private TextMeshProUGUI hintTmp;
    private TMP_FontAsset font;

    public static void Show(Action onFinished)
    {
        M13StoryOverlayHost.EnsureEventSystem();
        GameObject root = M13StoryOverlayHost.CreateDimOverlay("SideQuestA1VoyageOverlay");
        SideQuestA1VoyageOverlay overlay = root.AddComponent<SideQuestA1VoyageOverlay>();
        overlay.onFinished = onFinished;
        overlay.BuildUi(root.transform);
    }

    private void BuildUi(Transform overlayRoot)
    {
        font = ResolveFont();
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");
        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "退潮", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform,
            "舢板",
            font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        captionTmp = BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            SideQuestA1PlotCopy.VoyageCaption,
            font,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + 96f);

        hintTmp = CreateHint(panel.transform, "輕觸略過");
        Button skip = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform,
            "SkipBtn",
            "登島",
            font);
        skip.onClick.AddListener(Finish);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (hintTmp != null)
            hintTmp.text = "輕觸略過 · " + Mathf.CeilToInt(Mathf.Max(0f, DurationSeconds - elapsed)) + "s";
        if (elapsed >= DurationSeconds)
            Finish();
    }

    private void Finish()
    {
        Action cb = onFinished;
        onFinished = null;
        SideQuestA1OverlayVoice.Stop();
        Destroy(gameObject);
        cb?.Invoke();
    }

    private static TextMeshProUGUI CreateHint(Transform parent, string text)
    {
        GameObject go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + 104f);
        rt.sizeDelta = new Vector2(720f, 36f);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22f;
        tmp.color = new Color(0.82f, 0.88f, 0.86f, 0.9f);
        tmp.alignment = TextAlignmentOptions.Center;
        BirdDuelOverlayUiBuild.ApplyFont(tmp, UiFontResolver.ResolveUiFont());
        return tmp;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = UiFontResolver.ResolveUiFont();
        return font != null
            ? font
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }
}

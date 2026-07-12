using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-2 段考备忘全屏靜態 overlay（第 2 次 A 段落敗解鎖；非戰中教練）。</summary>
public sealed class M12PhaseAExamMemoOverlay : MonoBehaviour
{
    private const int OverlaySortOrder = 780;
    private const float PanelWidth = 760f;
    private const float PanelHeight = 560f;

    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.62f);
    private static readonly Color PanelColor = new Color(0.12f, 0.14f, 0.16f, 0.98f);
    private static readonly Color TitleColor = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Color BodyColor = new Color(0.94f, 0.92f, 0.86f, 1f);

    private System.Action onDismiss;
    private TMP_FontAsset font;

    public static M12PhaseAExamMemoOverlay Show(bool firstUnlockReveal, System.Action onDismiss)
    {
        GameObject host = new GameObject("M12PhaseAExamMemoOverlay");
        M12PhaseAExamMemoOverlay overlay = host.AddComponent<M12PhaseAExamMemoOverlay>();
        overlay.onDismiss = onDismiss;
        overlay.BuildUi(firstUnlockReveal);
        return overlay;
    }

    private void BuildUi(bool firstUnlockReveal)
    {
        font = ResolveFont();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(transform, false);
        Stretch(dimGo.GetComponent<RectTransform>());
        Image dim = dimGo.GetComponent<Image>();
        dim.color = DimColor;
        dim.raycastTarget = true;

        GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image panelBg = panelGo.GetComponent<Image>();
        panelBg.color = PanelColor;

        TextMeshProUGUI titleTmp = CreateText(panelGo.transform, "Title", 34f, FontStyles.Bold, TitleColor);
        RectTransform titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(-48f, 48f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.text = M12PhaseAExamMemoCopy.PanelTitle;

        TextMeshProUGUI speakerTmp = CreateText(panelGo.transform, "Speaker", 22f, FontStyles.Bold, TitleColor);
        RectTransform speakerRt = speakerTmp.rectTransform;
        speakerRt.anchorMin = new Vector2(0f, 1f);
        speakerRt.anchorMax = new Vector2(1f, 1f);
        speakerRt.pivot = new Vector2(0.5f, 1f);
        speakerRt.anchoredPosition = new Vector2(0f, -68f);
        speakerRt.sizeDelta = new Vector2(-48f, 32f);
        speakerTmp.alignment = TextAlignmentOptions.Center;
        speakerTmp.text = M12PhaseAExamMemoCopy.SpeakerName + " 整理";

        GameObject bodyScrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        bodyScrollGo.transform.SetParent(panelGo.transform, false);
        RectTransform scrollRt = bodyScrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0.16f);
        scrollRt.anchorMax = new Vector2(1f, 0.78f);
        scrollRt.offsetMin = new Vector2(32f, 0f);
        scrollRt.offsetMax = new Vector2(-32f, 0f);
        bodyScrollGo.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.11f, 0.55f);

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(bodyScrollGo.transform, false);
        RectTransform contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI bodyTmp = CreateText(contentGo.transform, "Body", 24f, FontStyles.Normal, BodyColor);
        RectTransform bodyRt = bodyTmp.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.anchoredPosition = Vector2.zero;
        bodyRt.sizeDelta = new Vector2(-24f, 0f);
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.richText = true;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.text = M12PhaseAExamMemoCopy.BuildBodyRichText(firstUnlockReveal);
        bodyTmp.ForceMeshUpdate(true, true);
        float textHeight = Mathf.Max(220f, bodyTmp.preferredHeight + 24f);
        contentRt.sizeDelta = new Vector2(0f, textHeight);
        bodyRt.sizeDelta = new Vector2(-24f, textHeight);

        ScrollRect scroll = bodyScrollGo.GetComponent<ScrollRect>();
        scroll.viewport = scrollRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject btnGo = new GameObject("Dismiss", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panelGo.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 24f);
        btnRt.sizeDelta = new Vector2(240f, 56f);
        Button btn = btnGo.GetComponent<Button>();
        BattleUiColors.ApplyButtonStyle(btn, "EndTurnButton");
        btn.onClick.AddListener(Dismiss);

        TextMeshProUGUI btnLabel = CreateText(btnGo.transform, "Label", 24f, FontStyles.Bold, Color.white);
        Stretch(btnLabel.rectTransform);
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.text = M12PhaseAExamMemoCopy.ResolveDismissLabel(firstUnlockReveal);
    }

    private void Dismiss()
    {
        System.Action callback = onDismiss;
        onDismiss = null;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
        return tmp;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset settings = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settings != null)
            return settings;
        return BuildbeckUiFonts.ResolveBuildbeckButtonFont();
    }
}

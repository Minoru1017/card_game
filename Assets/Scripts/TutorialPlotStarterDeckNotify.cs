using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>1-1 劇情：離開「基礎牌組」步驟後的獲得通知（獨立高層 Canvas，避免被劇情 UI 遮住）。</summary>
public static class TutorialPlotStarterDeckNotify
{
    public const string OverlayRootName = "TutorialPlotStarterDeckNotify";
    private const string OverlayCanvasName = "TutorialPlotStarterDeckNotifyCanvas";
    private const int OverlaySortOrder = 800;

    public static void Show(Canvas plotCanvas, TMP_Text fontSource, Action onDismissed)
    {
        if (plotCanvas == null)
        {
            onDismissed?.Invoke();
            return;
        }

        DismissExisting();

        Canvas overlayCanvas = EnsureOverlayCanvas(plotCanvas);
        if (overlayCanvas == null)
        {
            onDismissed?.Invoke();
            return;
        }

        var rootGo = new GameObject(OverlayRootName, typeof(RectTransform));
        rootGo.transform.SetParent(overlayCanvas.transform, false);
        rootGo.transform.SetAsLastSibling();

        RectTransform rootRt = rootGo.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dimGo.transform.SetParent(rootGo.transform, false);
        RectTransform dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimGo.GetComponent<Image>();
        dimImg.color = new Color(0.08f, 0.06f, 0.05f, 0.55f);
        dimImg.raycastTarget = true;

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelGo.transform.SetParent(rootGo.transform, false);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(720f, 280f);
        Image panelImg = panelGo.GetComponent<Image>();
        panelImg.color = BattleUiColors.PanelCream96;
        panelImg.raycastTarget = false;

        TMP_Text titleTmp = CreateLabel(panelGo.transform, "Title", 36f, TextAlignmentOptions.Center);
        titleTmp.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleTmp.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleTmp.rectTransform.offsetMin = new Vector2(32f, -88f);
        titleTmp.rectTransform.offsetMax = new Vector2(-32f, -24f);
        titleTmp.text = StoryTextStyle.Em("獲得基礎牌組");
        titleTmp.color = BattleUiColors.Ink;

        TMP_Text bodyTmp = CreateLabel(panelGo.transform, "Body", 26f, TextAlignmentOptions.Center);
        bodyTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
        bodyTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
        bodyTmp.rectTransform.offsetMin = new Vector2(40f, 56f);
        bodyTmp.rectTransform.offsetMax = new Vector2(-40f, -96f);
        bodyTmp.text =
            "已裝入目前牌組 共 " + StoryTextStyle.Em("30") + " 張 " +
            StoryTextStyle.Mu("含民兵 長弓 治療 火球等");
        bodyTmp.color = BattleUiColors.Ink;

        TMP_Text hintTmp = CreateLabel(panelGo.transform, "Hint", 20f, TextAlignmentOptions.Center);
        hintTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
        hintTmp.rectTransform.anchorMax = new Vector2(1f, 0f);
        hintTmp.rectTransform.pivot = new Vector2(0.5f, 0f);
        hintTmp.rectTransform.offsetMin = new Vector2(24f, 20f);
        hintTmp.rectTransform.offsetMax = new Vector2(-24f, 52f);
        hintTmp.text = StoryTextStyle.Mu("點擊任意處繼續");
        hintTmp.color = BattleUiColors.InkSoft;

        PlotUiTextUtil.ApplyFontForRichText(titleTmp, fontSource);
        PlotUiTextUtil.ApplyFontForRichText(bodyTmp, fontSource);
        PlotUiTextUtil.ApplyFontForRichText(hintTmp, fontSource);

        titleTmp.ForceMeshUpdate();
        bodyTmp.ForceMeshUpdate();
        hintTmp.ForceMeshUpdate();

        Button dimBtn = dimGo.GetComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.targetGraphic = dimImg;

        CanvasGroup panelCg = panelGo.GetComponent<CanvasGroup>();
        panelCg.blocksRaycasts = true;
        panelCg.interactable = false;

        var presenter = rootGo.AddComponent<TutorialPlotStarterDeckNotifyPresenter>();
        presenter.Initialize(dimImg, panelRt, panelCg, titleTmp, bodyTmp, hintTmp, () =>
        {
            if (overlayCanvas != null)
                overlayCanvas.gameObject.SetActive(false);
            onDismissed?.Invoke();
        });

        rootGo.SetActive(true);
        overlayCanvas.gameObject.SetActive(true);
    }

    /// <summary>略過劇情時：一行字卡提示牌組已就緒，再進教學戰。</summary>
    public static void ShowSkipReadyBrief(Canvas plotCanvas, TMP_Text fontSource, Action onDismissed)
    {
        if (plotCanvas == null)
        {
            onDismissed?.Invoke();
            return;
        }

        DismissExisting();

        Canvas overlayCanvas = EnsureOverlayCanvas(plotCanvas);
        if (overlayCanvas == null)
        {
            onDismissed?.Invoke();
            return;
        }

        var rootGo = new GameObject(OverlayRootName, typeof(RectTransform));
        rootGo.transform.SetParent(overlayCanvas.transform, false);

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dimGo.transform.SetParent(rootGo.transform, false);
        RectTransform dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimGo.GetComponent<Image>();
        dimImg.color = new Color(0.08f, 0.06f, 0.05f, 0.45f);

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelGo.transform.SetParent(rootGo.transform, false);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 88f);
        Image panelImg = panelGo.GetComponent<Image>();
        panelImg.color = BattleUiColors.PanelCream96;
        panelImg.raycastTarget = false;

        TMP_Text lineTmp = CreateLabel(panelGo.transform, "Line", 30f, TextAlignmentOptions.Center);
        lineTmp.rectTransform.anchorMin = Vector2.zero;
        lineTmp.rectTransform.anchorMax = Vector2.one;
        lineTmp.rectTransform.offsetMin = new Vector2(24f, 12f);
        lineTmp.rectTransform.offsetMax = new Vector2(-24f, -12f);
        lineTmp.text = StoryTextStyle.Em("基礎牌組已就緒");
        lineTmp.color = BattleUiColors.Ink;
        PlotUiTextUtil.ApplyFontForRichText(lineTmp, fontSource);
        lineTmp.ForceMeshUpdate();

        CanvasGroup panelCg = panelGo.GetComponent<CanvasGroup>();
        var presenter = rootGo.AddComponent<TutorialPlotStarterDeckNotifyPresenter>();
        presenter.InitializeSkipBrief(dimImg, panelRt, panelCg, dimGo.GetComponent<Button>(), onDismissed);

        rootGo.SetActive(true);
        overlayCanvas.gameObject.SetActive(true);
    }

    public static void DismissExisting()
    {
        GameObject existingRoot = GameObject.Find(OverlayRootName);
        if (existingRoot != null)
            UnityEngine.Object.Destroy(existingRoot);

        GameObject existingCanvas = GameObject.Find(OverlayCanvasName);
        if (existingCanvas != null)
            UnityEngine.Object.Destroy(existingCanvas);
    }

    private static Canvas EnsureOverlayCanvas(Canvas reference)
    {
        if (reference == null) return null;

        Transform parent = reference.transform.root;
        Transform existing = parent.Find(OverlayCanvasName);
        if (existing != null)
        {
            Canvas cached = existing.GetComponent<Canvas>();
            if (cached != null)
                return cached;
        }

        var go = new GameObject(OverlayCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = reference.renderMode;
        canvas.worldCamera = reference.worldCamera;
        canvas.planeDistance = reference.planeDistance;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        CanvasScaler refScaler = reference.GetComponent<CanvasScaler>();
        if (refScaler != null)
        {
            scaler.uiScaleMode = refScaler.uiScaleMode;
            scaler.referenceResolution = refScaler.referenceResolution;
            scaler.screenMatchMode = refScaler.screenMatchMode;
            scaler.matchWidthOrHeight = refScaler.matchWidthOrHeight;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        return canvas;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.richText = true;
        tmp.enableWordWrapping = true;
        return tmp;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- UI build

    private void BuildUi()
    {
        Canvas canvas = CreateCanvas();
        gameCanvas = canvas;
        Transform root = canvas.transform;
        uiRoot = root;

        // 全螢幕背景。
        CreateImage("BG", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, ColorBg, false);

        // 標題與說明。
        titleText = CreateText("Title", root, "鬥鳥暖身賽", 64f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 84f),
            BirdDuelUiColors.WonderBadge);
        subtitleText = CreateText("Subtitle", root,
            DefaultSubtitle(), 30f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1400f, 44f),
            ColorSubtitle);

        BuildOpponentArea(root);
        beatFxCanvas = CreateBeatFxCanvas();
        BuildBeatArea(beatFxCanvas.transform);
        BuildBars(root);
        BuildFeedback(root);
        BuildButtons(root);
        overlayCanvas = CreateOverlayCanvas();
        overlayRoot = overlayCanvas.transform;
        BuildResultPanel(overlayRoot);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("FightingBirdGameCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>
    /// 收束框／鼓面／假 scare 獨立 Canvas：每幀縮放不會拖動含 TMP 按鈕的主 UI 重新批次。
    /// </summary>
    private Canvas CreateBeatFxCanvas()
    {
        GameObject canvasObj = new GameObject("FightingBirdBeatFxCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 51;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>結果／加成抽選等模態 UI，排序高於 Beat FX Canvas。</summary>
    private Canvas CreateOverlayCanvas()
    {
        GameObject canvasObj = new GameObject("FightingBirdOverlayCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 52;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void BuildOpponentArea(Transform root)
    {
        opponentName = CreateText("OpponentName", root, npc.displayName, 34f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(600f, 44f),
            BirdDuelUiColors.OpponentName);

        opponentPad = CreateImage("OpponentPad", root,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, ColorIdle, false);
        RectTransform padRt = opponentPad.rectTransform;
        padRt.sizeDelta = new Vector2(220f, 220f);
        padRt.anchoredPosition = new Vector2(0f, -370f);

        opponentGlyph = CreateText("OpponentGlyph", opponentPad.transform, "?", 120f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        opponentGlyph.rectTransform.offsetMin = Vector2.zero;
        opponentGlyph.rectTransform.offsetMax = Vector2.zero;

        peekHintText = CreateText("PeekHint", root, "", 30f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(900f, 40f),
            ColorNest);
    }

    private void BuildBeatArea(Transform root)
    {
        // 收束指示框（由大縮小到命中尺寸）。
        Image shrink = CreateImage("ShrinkIndicator", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            ColorShrinkIdle, false);
        shrink.rectTransform.sizeDelta = new Vector2(160f, 160f);
        shrink.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        shrinkIndicator = shrink.rectTransform;
        shrinkIndicatorImage = shrink;

        // 鼓點中心（命中時脈動）。
        beatPad = CreateImage("BeatPad", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            ColorBeatPadIdle, false);
        beatPad.rectTransform.sizeDelta = new Vector2(150f, 150f);
        beatPad.rectTransform.anchoredPosition = BeatPadAnchor;

        BuildFakeScareRing(root);
    }

    /// <summary>庭訓假 scare：白色方形外框，自螢幕邊緣收束至鼓面（呼應 ShrinkIndicator 方形收束）。</summary>
    private void BuildFakeScareRing(Transform root)
    {
        Image ring = CreateImage("FakeScareFrame", root,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            ColorFakeScareRing, false);
        ring.sprite = GetFakeScareFrameSprite();
        ring.type = Image.Type.Simple;
        ring.preserveAspect = true;
        ring.rectTransform.sizeDelta = new Vector2(FakeScareFrameSize, FakeScareFrameSize);
        ring.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        fakeScareRing = ring.rectTransform;
        fakeScareRingImage = ring;
        ring.gameObject.SetActive(false);
    }

    private void BuildBars(Transform root)
    {
        // 分數條（左）。
        scoreLabel = CreateText("ScoreLabel", root, "分數 0", 30f, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -250f), new Vector2(360f, 40f), Color.white);
        scoreLabel.rectTransform.pivot = new Vector2(0f, 1f);
        scoreFill = CreateBar(root, "ScoreBar", new Vector2(60f, -300f), new Vector2(360f, 34f), ColorScoreFill);

        // 看破條（右）。
        insightLabel = CreateText("InsightLabel", root, "看破 0", 30f, TextAlignmentOptions.Right,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -250f), new Vector2(360f, 40f), ColorNest);
        insightLabel.rectTransform.pivot = new Vector2(1f, 1f);
        insightFill = CreateBar(root, "InsightBar", new Vector2(-420f, -300f), new Vector2(360f, 34f), ColorInsightFill);
        // 右側條：以右上為錨。
        RectTransform insightBg = insightFill.parent as RectTransform;
        insightBg.anchorMin = new Vector2(1f, 1f);
        insightBg.anchorMax = new Vector2(1f, 1f);
        insightBg.pivot = new Vector2(1f, 1f);
        insightBg.anchoredPosition = new Vector2(-60f, -300f);
    }

    private void BuildFeedback(Transform root)
    {
        feedbackText = CreateText("Feedback", root, "", 46f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(1000f, 60f),
            Color.white);
    }

    private void BuildButtons(Transform root)
    {
        buttonImages.Clear();
        BirdGesture[] order = { BirdGesture.Peck, BirdGesture.Wing, BirdGesture.Nest, BirdGesture.Pass };
        Color[] colors = { ColorPeck, ColorWing, ColorNest, ColorPass };
        string[] hints = { "啄擊\n進攻 +3", "振翅\n防守 +2", "築巢\n看破", "PASS\n防禦 +1" };

        const float btnW = 280f;
        const float btnH = 150f;
        const float gap = 36f;
        float totalW = order.Length * btnW + (order.Length - 1) * gap;
        float startX = -totalW * 0.5f + btnW * 0.5f;

        for (int i = 0; i < order.Length; i++)
        {
            BirdGesture g = order[i];
            float x = startX + i * (btnW + gap);
            Image img = CreateImage("Btn_" + g, root,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, colors[i], true);
            RectTransform rt = img.rectTransform;
            rt.sizeDelta = new Vector2(btnW, btnH);
            rt.anchoredPosition = new Vector2(x, 130f);

            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            BirdGesture captured = g;
            btn.onClick.AddListener(() => RegisterInput(captured));

            TextMeshProUGUI label = CreateText("Label", img.transform, hints[i], 36f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.raycastTarget = false;

            buttonImages[g] = img;
        }
    }

    private void BuildResultPanel(Transform overlayRoot)
    {
        resultOverlayRoot = CreateImage("ResultOverlay", overlayRoot,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, BirdDuelUiColors.Dim, true).gameObject;

        resultPanel = CreateImage("ResultPanel", resultOverlayRoot.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorPanel, true).gameObject;
        RectTransform panelRt = resultPanel.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(1100f, 620f);

        resultTitle = CreateText("ResultTitle", resultPanel.transform, "", 64f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1000f, 84f),
            BirdDuelUiColors.WonderBadge);
        resultLine = CreateText("ResultLine", resultPanel.transform, "", 34f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(980f, 120f),
            BirdDuelUiColors.ResultLine);
        resultLine.enableWordWrapping = true;
        resultIntel = CreateText("ResultIntel", resultPanel.transform, "", 36f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(980f, 140f),
            ColorNest);
        resultIntel.enableWordWrapping = true;

        // 再練一次。
        Image replay = CreateImage("ReplayBtn", resultPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, ColorWing, true);
        replay.rectTransform.sizeDelta = new Vector2(320f, 112f);
        replay.rectTransform.anchoredPosition = new Vector2(-190f, 80f);
        replayButtonRoot = replay.gameObject;
        Button replayBtn = replay.gameObject.AddComponent<Button>();
        replayBtn.targetGraphic = replay;
        replayBtn.onClick.AddListener(StartMatch);
        TextMeshProUGUI replayLabel = CreateText("Label", replay.transform, "再練一次", 38f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        replayLabel.raycastTarget = false;

        // 進入對戰／返回 hall。
        Image leave = CreateImage("LeaveBtn", resultPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, ColorScoreFill, true);
        leave.rectTransform.sizeDelta = new Vector2(320f, 112f);
        leave.rectTransform.anchoredPosition = new Vector2(190f, 80f);
        Button leaveBtn = leave.gameObject.AddComponent<Button>();
        leaveBtn.targetGraphic = leave;
        leaveBtn.onClick.AddListener(OnLeavePressed);
        leaveButtonLabel = CreateText("Label", leave.transform, ResolveLeaveButtonLabel(), 38f,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        leaveButtonLabel.raycastTarget = false;

        resultOverlayRoot.SetActive(false);
    }

    private string ResolveLeaveButtonLabel()
    {
        if (m13StoryMode)
            return "繼續迎測";
        if (preBattleMode)
            return "進入對戰";
        return "返回大廳";
    }
}

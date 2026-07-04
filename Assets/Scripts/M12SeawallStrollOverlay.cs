using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M-1-2 中段海牆散策（LEVEL_DESIGN_M-1-2.md §3.3.2）：
/// 單屏海牆場景 + 4 個固定熱區（號燈／系船柱／木樁假人／蠟封殘卷），
/// 點過至少 1 處即可「前往加練」；無戰鬥，封印法術首通預設拾取。
/// </summary>
public sealed class M12SeawallStrollOverlay : MonoBehaviour
{
    private const int OverlaySortOrder = 620;
    private const float HotspotPulsePeriod = 1.6f;

    private static readonly Color SkyColor = new Color(0.20f, 0.31f, 0.44f, 1f);
    private static readonly Color SeaColor = new Color(0.13f, 0.34f, 0.40f, 1f);
    private static readonly Color WallColor = new Color(0.44f, 0.41f, 0.36f, 1f);
    private static readonly Color ParapetColor = new Color(0.35f, 0.32f, 0.28f, 1f);
    private static readonly Color WalkwayColor = new Color(0.27f, 0.25f, 0.22f, 1f);
    private static readonly Color TitleColor = new Color(0.97f, 0.90f, 0.66f, 1f);
    private static readonly Color HintColor = new Color(0.88f, 0.92f, 0.90f, 0.92f);
    private static readonly Color CaptionPanelColor = new Color(0.09f, 0.11f, 0.13f, 0.92f);
    private static readonly Color SpeakerColor = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Color CaptionBodyColor = new Color(0.95f, 0.96f, 0.93f, 1f);
    private static readonly Color HotspotRingColor = new Color(0.98f, 0.86f, 0.44f, 1f);
    private static readonly Color HotspotLabelColor = new Color(0.96f, 0.95f, 0.90f, 1f);
    private static readonly Color VisitedTint = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color ContinueEnabledBg = new Color(0.17f, 0.45f, 0.58f, 1f);
    private static readonly Color ContinueDisabledBg = new Color(0.30f, 0.33f, 0.34f, 0.9f);

    private System.Action onContinue;
    private TMP_FontAsset font;
    private TextMeshProUGUI captionSpeakerTmp;
    private TextMeshProUGUI captionBodyTmp;
    private TextMeshProUGUI progressTmp;
    private Button continueButton;
    private Image continueButtonBg;
    private TextMeshProUGUI continueLabelTmp;
    private readonly HotspotUi[] hotspots = new HotspotUi[4];
    private int visitedCount;
    private bool finished;
    private Coroutine pickupToastRoutine;
    private GameObject pickupToastRoot;

    private sealed class HotspotUi
    {
        public string label;
        public string caption;
        public bool visited;
        public Outline ring;
        public Image[] shapeImages;
        public Color[] shapeBaseColors;
        public TextMeshProUGUI labelTmp;
        public bool isSealedScroll;
    }

    /// <summary>建立並顯示海牆散策全屏覆蓋；點「前往加練」時呼叫 onContinue。</summary>
    public static M12SeawallStrollOverlay Show(System.Action onContinue)
    {
        GameObject host = new GameObject("M12SeawallStrollOverlay");
        M12SeawallStrollOverlay overlay = host.AddComponent<M12SeawallStrollOverlay>();
        overlay.onContinue = onContinue;
        overlay.BuildUi();
        return overlay;
    }

    private void Update()
    {
        // 未巡視熱區的金色外框呼吸提示。
        float pulse = 0.45f + 0.55f * Mathf.PingPong(Time.unscaledTime / (HotspotPulsePeriod * 0.5f), 1f);
        for (int i = 0; i < hotspots.Length; i++)
        {
            HotspotUi h = hotspots[i];
            if (h?.ring == null || h.visited)
                continue;
            Color c = HotspotRingColor;
            c.a = pulse;
            h.ring.effectColor = c;
        }
    }

    private void BuildUi()
    {
        font = ResolveFont();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        BuildBackdrop();
        BuildHeader();
        BuildHotspots();
        BuildCaptionPanel();
        BuildContinueButton();
        RefreshProgress();
        SetCaption("林可姐", "這裡就是海牆 想看什麼就點什麼 巡過一處 我們就去加練");
    }

    private void BuildBackdrop()
    {
        // 全屏擋點擊，避免空白處點擊穿透到底下的劇情 UI。
        GameObject blocker = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image));
        blocker.transform.SetParent(transform, false);
        RectTransform blockerRt = blocker.GetComponent<RectTransform>();
        blockerRt.anchorMin = Vector2.zero;
        blockerRt.anchorMax = Vector2.one;
        blockerRt.offsetMin = Vector2.zero;
        blockerRt.offsetMax = Vector2.zero;
        Image blockerImg = blocker.GetComponent<Image>();
        blockerImg.color = new Color(0f, 0f, 0f, 0.001f);
        blockerImg.raycastTarget = true;

        CreateBand("Sky", 0.52f, 1f, SkyColor);
        CreateBand("Sea", 0.34f, 0.52f, SeaColor);
        CreateBand("Wall", 0.14f, 0.34f, WallColor);
        CreateBand("Parapet", 0.32f, 0.345f, ParapetColor);
        CreateBand("Walkway", 0f, 0.14f, WalkwayColor);
    }

    private void CreateBand(string name, float yMin, float yMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void BuildHeader()
    {
        TextMeshProUGUI title = CreateText(transform, "Title", "海牆散策", 44f, FontStyles.Bold, TitleColor);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(48f, -36f);
        titleRt.sizeDelta = new Vector2(560f, 60f);
        title.alignment = TextAlignmentOptions.TopLeft;

        TextMeshProUGUI hint = CreateText(
            transform, "Hint", "點一點海牆上的東西 · 巡過至少 1 處即可前往加練", 24f, FontStyles.Normal, HintColor);
        RectTransform hintRt = hint.rectTransform;
        hintRt.anchorMin = new Vector2(0f, 1f);
        hintRt.anchorMax = new Vector2(0f, 1f);
        hintRt.pivot = new Vector2(0f, 1f);
        hintRt.anchoredPosition = new Vector2(50f, -96f);
        hintRt.sizeDelta = new Vector2(760f, 40f);
        hint.alignment = TextAlignmentOptions.TopLeft;

        progressTmp = CreateText(transform, "Progress", string.Empty, 28f, FontStyles.Bold, HintColor);
        RectTransform progressRt = progressTmp.rectTransform;
        progressRt.anchorMin = new Vector2(1f, 1f);
        progressRt.anchorMax = new Vector2(1f, 1f);
        progressRt.pivot = new Vector2(1f, 1f);
        progressRt.anchoredPosition = new Vector2(-48f, -40f);
        progressRt.sizeDelta = new Vector2(320f, 44f);
        progressTmp.alignment = TextAlignmentOptions.TopRight;
    }

    private void BuildHotspots()
    {
        // 熱區 1：海牆號燈（燈塔）。
        hotspots[0] = CreateHotspot(
            index: 0,
            label: "號燈",
            caption: "那是海牆號燈 巡邏隊每晚要對一次燈號 從這裡望得到港灣訓練場",
            anchor: new Vector2(0.14f, 0.56f),
            size: new Vector2(190f, 330f),
            buildShapes: BuildLighthouseShapes);

        // 熱區 2：纜繩／系船柱。
        hotspots[1] = CreateHotspot(
            index: 1,
            label: "系船柱",
            caption: "繩結歸繩結 卡牌歸卡牌 別把繫船結當成牌打出去",
            anchor: new Vector2(0.40f, 0.20f),
            size: new Vector2(190f, 170f),
            buildShapes: BuildBollardShapes);

        // 熱區 3：木樁／假人。
        hotspots[2] = CreateHotspot(
            index: 2,
            label: "木樁假人",
            caption: "巡邏隊拿它練御三家戰技 你剛剛段考打得比它們像樣 加練完還有教會線的畢業禮",
            anchor: new Vector2(0.76f, 0.30f),
            size: new Vector2(190f, 250f),
            buildShapes: BuildDummyShapes);

        // 熱區 4：封印法術殘卷（§3.3.3 伏筆；獨立熱區）。
        hotspots[3] = CreateHotspot(
            index: 3,
            label: "蠟封殘卷",
            caption: "……封印的法術 學院還沒歸檔 別亂拆 先收進貴重品庫 以後再說",
            anchor: new Vector2(0.575f, 0.255f),
            size: new Vector2(130f, 130f),
            buildShapes: BuildSealedScrollShapes);
        hotspots[3].isSealedScroll = true;
    }

    private HotspotUi CreateHotspot(
        int index,
        string label,
        string caption,
        Vector2 anchor,
        Vector2 size,
        System.Func<Transform, (Image[] images, Color[] colors)> buildShapes)
    {
        GameObject root = new GameObject("Hotspot_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(transform, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Image hitArea = root.GetComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.02f);

        Outline ring = root.AddComponent<Outline>();
        ring.effectColor = HotspotRingColor;
        ring.effectDistance = new Vector2(3f, -3f);

        (Image[] images, Color[] colors) = buildShapes(root.transform);

        TextMeshProUGUI labelTmp = CreateText(
            root.transform, "Label", "○ " + label, 26f, FontStyles.Bold, HotspotLabelColor);
        RectTransform labelRt = labelTmp.rectTransform;
        labelRt.anchorMin = new Vector2(0.5f, 0f);
        labelRt.anchorMax = new Vector2(0.5f, 0f);
        labelRt.pivot = new Vector2(0.5f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, -8f);
        labelRt.sizeDelta = new Vector2(260f, 40f);
        labelTmp.alignment = TextAlignmentOptions.Top;

        HotspotUi ui = new HotspotUi
        {
            label = label,
            caption = caption,
            ring = ring,
            shapeImages = images,
            shapeBaseColors = colors,
            labelTmp = labelTmp
        };

        Button button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => OnHotspotClicked(index));
        return ui;
    }

    private (Image[], Color[]) BuildLighthouseShapes(Transform parent)
    {
        Image tower = CreateShape(parent, "Tower", new Vector2(0.5f, 0f), new Vector2(0f, 30f),
            new Vector2(84f, 230f), new Color(0.82f, 0.79f, 0.72f, 1f));
        Image stripe = CreateShape(parent, "Stripe", new Vector2(0.5f, 0f), new Vector2(0f, 118f),
            new Vector2(84f, 34f), new Color(0.62f, 0.28f, 0.24f, 1f));
        Image lamp = CreateShape(parent, "Lamp", new Vector2(0.5f, 0f), new Vector2(0f, 268f),
            new Vector2(56f, 52f), new Color(0.99f, 0.85f, 0.42f, 1f));
        return (new[] { tower, stripe, lamp },
            new[] { tower.color, stripe.color, lamp.color });
    }

    private (Image[], Color[]) BuildBollardShapes(Transform parent)
    {
        Image post = CreateShape(parent, "Post", new Vector2(0.5f, 0f), new Vector2(-24f, 26f),
            new Vector2(64f, 96f), new Color(0.24f, 0.23f, 0.22f, 1f));
        Image cap = CreateShape(parent, "Cap", new Vector2(0.5f, 0f), new Vector2(-24f, 120f),
            new Vector2(84f, 26f), new Color(0.31f, 0.30f, 0.28f, 1f));
        Image rope = CreateShape(parent, "Rope", new Vector2(0.5f, 0f), new Vector2(48f, 92f),
            new Vector2(120f, 16f), new Color(0.72f, 0.60f, 0.40f, 1f));
        rope.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        return (new[] { post, cap, rope },
            new[] { post.color, cap.color, rope.color });
    }

    private (Image[], Color[]) BuildDummyShapes(Transform parent)
    {
        Image post = CreateShape(parent, "Post", new Vector2(0.5f, 0f), new Vector2(0f, 24f),
            new Vector2(46f, 190f), new Color(0.48f, 0.36f, 0.24f, 1f));
        Image arms = CreateShape(parent, "Arms", new Vector2(0.5f, 0f), new Vector2(0f, 158f),
            new Vector2(160f, 30f), new Color(0.54f, 0.41f, 0.28f, 1f));
        Image head = CreateShape(parent, "Head", new Vector2(0.5f, 0f), new Vector2(0f, 208f),
            new Vector2(58f, 52f), new Color(0.76f, 0.66f, 0.50f, 1f));
        return (new[] { post, arms, head },
            new[] { post.color, arms.color, head.color });
    }

    private (Image[], Color[]) BuildSealedScrollShapes(Transform parent)
    {
        Image page = CreateShape(parent, "Page", new Vector2(0.5f, 0.5f), new Vector2(0f, 6f),
            new Vector2(78f, 92f), new Color(0.87f, 0.79f, 0.60f, 1f));
        page.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        Image seal = CreateShape(parent, "WaxSeal", new Vector2(0.5f, 0.5f), new Vector2(12f, -14f),
            new Vector2(30f, 30f), new Color(0.63f, 0.17f, 0.16f, 1f));
        return (new[] { page, seal },
            new[] { page.color, seal.color });
    }

    private Image CreateShape(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0f);
        if (Mathf.Approximately(anchor.y, 0.5f))
            rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void BuildCaptionPanel()
    {
        // 放在上方天空區（標題列下方置中），避免遮住牆面／步道上的可點熱區。
        GameObject panel = new GameObject("CaptionPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -150f);
        rt.sizeDelta = new Vector2(1140f, 168f);
        Image bg = panel.GetComponent<Image>();
        bg.color = CaptionPanelColor;
        bg.raycastTarget = false;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.72f, 0.78f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        captionSpeakerTmp = CreateText(panel.transform, "Speaker", string.Empty, 26f, FontStyles.Bold, SpeakerColor);
        RectTransform speakerRt = captionSpeakerTmp.rectTransform;
        speakerRt.anchorMin = new Vector2(0f, 1f);
        speakerRt.anchorMax = new Vector2(1f, 1f);
        speakerRt.pivot = new Vector2(0f, 1f);
        speakerRt.anchoredPosition = new Vector2(24f, -14f);
        speakerRt.sizeDelta = new Vector2(-48f, 36f);
        captionSpeakerTmp.alignment = TextAlignmentOptions.TopLeft;

        captionBodyTmp = CreateText(panel.transform, "Body", string.Empty, 27f, FontStyles.Normal, CaptionBodyColor);
        RectTransform bodyRt = captionBodyTmp.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(24f, 16f);
        bodyRt.offsetMax = new Vector2(-24f, -54f);
        captionBodyTmp.alignment = TextAlignmentOptions.TopLeft;
        captionBodyTmp.enableWordWrapping = true;
    }

    private void BuildContinueButton()
    {
        GameObject btnGo = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(transform, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-48f, 54f);
        rt.sizeDelta = new Vector2(320f, 96f);

        continueButtonBg = btnGo.GetComponent<Image>();
        continueButton = btnGo.GetComponent<Button>();
        continueButton.onClick.AddListener(OnContinueClicked);

        continueLabelTmp = CreateText(btnGo.transform, "Label", "前往加練", 32f, FontStyles.Bold, Color.white);
        RectTransform labelRt = continueLabelTmp.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        continueLabelTmp.alignment = TextAlignmentOptions.Center;

        RefreshContinueButton();
    }

    private void OnHotspotClicked(int index)
    {
        if (finished || index < 0 || index >= hotspots.Length)
            return;

        HotspotUi h = hotspots[index];
        if (h == null)
            return;

        SetCaption("林可姐", h.caption);
        if (h.isSealedScroll && !h.visited)
            ShowSealedSpellPickupToast();

        if (!h.visited)
        {
            h.visited = true;
            visitedCount++;
            if (h.labelTmp != null)
                h.labelTmp.text = "● " + h.label + " · 已巡視";
            if (h.ring != null)
                h.ring.effectColor = new Color(0.55f, 0.92f, 0.62f, 0.85f);
            if (h.shapeImages != null)
            {
                for (int i = 0; i < h.shapeImages.Length; i++)
                {
                    if (h.shapeImages[i] != null)
                        h.shapeImages[i].color = h.shapeBaseColors[i] * VisitedTint;
                }
            }

            RefreshProgress();
            RefreshContinueButton();
        }
    }

    private void ShowSealedSpellPickupToast()
    {
        if (pickupToastRoutine != null)
            StopCoroutine(pickupToastRoutine);
        if (pickupToastRoot != null)
            Destroy(pickupToastRoot);

        pickupToastRoot = new GameObject("SealedSpellToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        pickupToastRoot.transform.SetParent(transform, false);
        RectTransform rt = pickupToastRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.62f);
        rt.anchorMax = new Vector2(0.5f, 0.62f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(620f, 150f);
        Image bg = pickupToastRoot.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.10f, 0.08f, 0.96f);
        bg.raycastTarget = false;
        Outline outline = pickupToastRoot.AddComponent<Outline>();
        outline.effectColor = HotspotRingColor;
        outline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI title = CreateText(
            pickupToastRoot.transform, "Title", "獲得 " + ValuablesVaultUiCopy.SealedSpellRelicName,
            30f, FontStyles.Bold, TitleColor);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);
        titleRt.sizeDelta = new Vector2(-48f, 44f);
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI body = CreateText(
            pickupToastRoot.transform, "Body", "效果: 未知 · 解封條件: ???\n已收入貴重品庫",
            24f, FontStyles.Normal, CaptionBodyColor);
        RectTransform bodyRt = body.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(24f, 14f);
        bodyRt.offsetMax = new Vector2(-24f, -62f);
        body.alignment = TextAlignmentOptions.Center;

        pickupToastRoutine = StartCoroutine(CoFadeOutPickupToast());
    }

    private IEnumerator CoFadeOutPickupToast()
    {
        yield return new WaitForSecondsRealtime(2.6f);
        CanvasGroup cg = pickupToastRoot != null ? pickupToastRoot.GetComponent<CanvasGroup>() : null;
        float t = 0f;
        const float fadeDuration = 0.45f;
        while (t < fadeDuration && cg != null)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        if (pickupToastRoot != null)
            Destroy(pickupToastRoot);
        pickupToastRoot = null;
        pickupToastRoutine = null;
    }

    private void OnContinueClicked()
    {
        if (finished || visitedCount < 1)
            return;

        // 保留畫面直到戰鬥場景載入完成（場景切換時本物件隨舊場景卸載）。
        finished = true;
        if (continueButton != null)
            continueButton.interactable = false;
        if (continueLabelTmp != null)
            continueLabelTmp.text = "前往加練中…";
        System.Action callback = onContinue;
        onContinue = null;
        callback?.Invoke();
    }

    private void RefreshProgress()
    {
        if (progressTmp != null)
            progressTmp.text = "已巡視 " + visitedCount + " / " + hotspots.Length;
    }

    private void RefreshContinueButton()
    {
        bool ready = visitedCount >= 1;
        if (continueButtonBg != null)
            continueButtonBg.color = ready ? ContinueEnabledBg : ContinueDisabledBg;
        if (continueButton != null)
            continueButton.interactable = ready;
        if (continueLabelTmp != null)
        {
            continueLabelTmp.text = ready ? "前往加練" : "先巡視 1 處";
            continueLabelTmp.color = ready ? Color.white : new Color(0.82f, 0.84f, 0.83f, 0.85f);
        }
    }

    private void SetCaption(string speaker, string body)
    {
        if (captionSpeakerTmp != null)
            captionSpeakerTmp.text = speaker;
        if (captionBodyTmp != null)
            captionBodyTmp.text = body;
    }

    private TextMeshProUGUI CreateText(
        Transform parent, string name, string text, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
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
        if (settings != null) return settings;
        return BuildbeckUiFonts.ResolveBuildbeckButtonFont();
    }
}

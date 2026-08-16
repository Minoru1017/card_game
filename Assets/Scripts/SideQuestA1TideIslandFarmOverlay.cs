using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A-1 潮間島三畦耕田：拖曳／節拍／點選（企劃 §A-1.1／§A-1.2）。</summary>
public sealed class SideQuestA1TideIslandFarmOverlay : MonoBehaviour
{
    public struct FarmResult
    {
        public SideQuestA1TideMarkRewardService.FarmOutcome outcome;
        public bool keptSeaPurslaneSeed;
    }

    private enum Crop
    {
        Rye = 0,
        Fallow = 1,
        Bean = 2
    }

    private enum StepKind
    {
        PlowDrag,
        ClickPlot,
        RhythmCompact,
        WaitAnimation,
        ScytheSwipe,
        ClickSoilBlocks,
        NetDrag,
        ClickHarvestPlants,
        PurslaneChoice,
        HoldSoak,
        ClickGridCells,
        WaterChannelDrag,
        ClickWeeds,
        ClickPods
    }

    private struct StepDef
    {
        public StepKind kind;
        public string instruction;
        public string toolLabel;
    }

    private static readonly StepDef[] RyeSteps =
    {
        new StepDef { kind = StepKind.PlowDrag, instruction = "拖曳簡犁橫向犁溝（覆蓋 ≥80%）。", toolLabel = "簡犁" },
        new StepDef { kind = StepKind.ClickPlot, instruction = "點擊畦面播種。", toolLabel = "種袋" },
        new StepDef { kind = StepKind.RhythmCompact, instruction = "依節拍點按「壓土」兩次。", toolLabel = "壓土" },
        new StepDef { kind = StepKind.WaitAnimation, instruction = "過一夜……", toolLabel = string.Empty },
        new StepDef { kind = StepKind.ScytheSwipe, instruction = "拖曳鐮刀左右各劃一次收割。", toolLabel = "鐮刀" }
    };

    private static readonly StepDef[] FallowSteps =
    {
        new StepDef { kind = StepKind.ClickSoilBlocks, instruction = "點擊土塊松土（3/3）。", toolLabel = "鋤頭" },
        new StepDef { kind = StepKind.NetDrag, instruction = "拖曳鹽網蓋住畦面（≥70%）。", toolLabel = "鹽網" },
        new StepDef { kind = StepKind.WaitAnimation, instruction = "過一季……", toolLabel = string.Empty },
        new StepDef { kind = StepKind.ClickHarvestPlants, instruction = "採收海蓬（5 叢）。", toolLabel = "手摘" },
        new StepDef { kind = StepKind.PurslaneChoice, instruction = "留一叢種子，或全部交出？", toolLabel = string.Empty }
    };

    private static readonly StepDef[] BeanSteps =
    {
        new StepDef { kind = StepKind.HoldSoak, instruction = "連續點按泡種盆直到冒泡（3 次）。", toolLabel = "泡種" },
        new StepDef { kind = StepKind.ClickGridCells, instruction = "逐格點種（6 格）。", toolLabel = "豆種" },
        new StepDef { kind = StepKind.WaterChannelDrag, instruction = "拖引潮渠連過每格（L 形水路）。", toolLabel = "水渠" },
        new StepDef { kind = StepKind.ClickWeeds, instruction = "點除藤草（2 次）。", toolLabel = "除藤" },
        new StepDef { kind = StepKind.ClickPods, instruction = "點收成熟豆莢（3 次）。", toolLabel = "收莢" }
    };

    private Action<FarmResult> onFinished;
    private TMP_FontAsset font;
    private TextMeshProUGUI infoSeasonTmp;
    private TextMeshProUGUI infoInstructionTmp;
    private TextMeshProUGUI toolLabelTmp;
    private TextMeshProUGUI grandmaTmp;
    private RectTransform plotAreaRt;
    private RectTransform toolHomeRt;
    private RectTransform workAreaRt;
    private RectTransform grandmaBarRt;
    private Image plotFillImage;
    private Image[] plotCellImages;
    private Outline plotDragOutline;
    private Button actionButton;
    private Button skipButton;
    private Image actionFillImage;

    private const float InfoStripTop = BirdDuelMobileOverlayLayout.HeaderHeight + 56f;
    private const float InfoStripHeight = 96f;
    private const float GrandmaBarHeight = 60f;
    private const float FooterBottomPad = 8f;

    private Crop crop = Crop.Rye;
    private int stepIndex;
    private bool keptSeaPurslaneSeed;
    private bool waitRoutineRunning;

    private readonly bool[] plowCells = new bool[20];
    private readonly bool[] netCells = new bool[20];
    private readonly bool[] gridCells = new bool[6];
    private readonly HashSet<int> waterCells = new HashSet<int>();
    private int soilHits;
    private int plantHits;
    private int weedHits;
    private int podHits;
    private bool scytheLeft;
    private bool scytheRight;
    private int rhythmHits;
    private float nextBeatTime;
    private float rhythmStartTime;
    private bool rhythmActive;
    private int soakStirHits;
    private SideQuestA1FarmUiDrag activeDrag;
    private GameObject dragToolGo;
    private GameObject staticToolGo;
    private GameObject purslaneChoiceRowGo;
    private readonly bool[] harvestPlantsPicked = new bool[5];
    private readonly List<GameObject> harvestPlantMarkers = new List<GameObject>();
    private Coroutine harvestPickRoutine;
    private readonly List<GameObject> clickTargets = new List<GameObject>();
    private readonly bool[] soilBlocksDone = new bool[3];
    private readonly bool[] weedsRemoved = new bool[2];
    private readonly bool[] podsPicked = new bool[3];
    private bool ryePlotPlanted;
    private readonly List<Button> hotspotButtons = new List<Button>();

    private static readonly Vector2[] DefaultSoilBlockAnchors =
    {
        new Vector2(0.22f, 0.58f),
        new Vector2(0.50f, 0.52f),
        new Vector2(0.78f, 0.58f),
    };

    private static readonly Vector2[] DefaultWeedAnchors =
    {
        new Vector2(0.28f, 0.38f),
        new Vector2(0.72f, 0.28f),
    };

    private static readonly Vector2[] DefaultPodAnchors =
    {
        new Vector2(0.22f, 0.18f),
        new Vector2(0.50f, 0.14f),
        new Vector2(0.78f, 0.18f),
    };

    private static readonly Vector2[] DefaultHarvestPlantAnchors =
    {
        new Vector2(0.18f, 0.58f),
        new Vector2(0.50f, 0.62f),
        new Vector2(0.82f, 0.58f),
        new Vector2(0.28f, 0.42f),
        new Vector2(0.68f, 0.42f),
    };

    private static readonly Vector2[] DefaultRyePlantAnchors =
    {
        new Vector2(0.38f, 0.84f),
        new Vector2(0.50f, 0.82f),
        new Vector2(0.62f, 0.84f),
    };

    private static readonly Vector2[] DefaultSoakBasinAnchors =
    {
        new Vector2(0.38f, 0.16f),
        new Vector2(0.50f, 0.16f),
        new Vector2(0.62f, 0.16f),
    };

    private Vector2[] soilBlockAnchors;
    private Vector2[] weedAnchors;
    private Vector2[] podAnchors;
    private Vector2[] harvestPlantAnchors;
    private Vector2[] gridCellAnchors;
    private Vector2 ryePlantAnchor;
    private Vector2 soakBasinAnchor;

    private const int PlotCols = 5;
    private const int PlotRows = 4;
    private const float RhythmBpm = 90f;
    private const float RhythmGoodWindow = 0.35f;

    public static void Show(Action<FarmResult> onFinished)
    {
        M13StoryOverlayHost.EnsureEventSystem();
        GameObject root = M13StoryOverlayHost.CreateDimOverlay("SideQuestA1TideIslandFarmOverlay");
        SideQuestA1TideIslandFarmOverlay overlay = root.AddComponent<SideQuestA1TideIslandFarmOverlay>();
        overlay.onFinished = onFinished;
        overlay.RandomizeClickAnchors();
        overlay.BuildUi(root.transform);
        overlay.EnterStep();
    }

    /// <summary>每次進入 overlay 打亂點選／採收熱區位置（作物順序不變）。</summary>
    private void RandomizeClickAnchors()
    {
        soilBlockAnchors = ShuffledCopy(DefaultSoilBlockAnchors);
        weedAnchors = ShuffledCopy(DefaultWeedAnchors);
        podAnchors = ShuffledCopy(DefaultPodAnchors);
        harvestPlantAnchors = ShuffledCopy(DefaultHarvestPlantAnchors);

        var defaultGrid = new Vector2[6];
        for (int i = 0; i < defaultGrid.Length; i++)
            defaultGrid[i] = ComputeDefaultGridCellAnchor(i);
        gridCellAnchors = ShuffledCopy(defaultGrid);

        ryePlantAnchor = PickRandomAnchor(DefaultRyePlantAnchors);
        soakBasinAnchor = PickRandomAnchor(DefaultSoakBasinAnchors);
    }

    private static Vector2[] ShuffledCopy(Vector2[] source)
    {
        if (source == null || source.Length == 0)
            return System.Array.Empty<Vector2>();

        var copy = (Vector2[])source.Clone();
        for (int i = copy.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector2 tmp = copy[i];
            copy[i] = copy[j];
            copy[j] = tmp;
        }

        return copy;
    }

    private static Vector2 PickRandomAnchor(Vector2[] pool)
    {
        if (pool == null || pool.Length == 0)
            return new Vector2(0.5f, 0.5f);
        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    private void BuildUi(Transform overlayRoot)
    {
        font = ResolveFont();
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");
        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "潮間島", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform,
            "三畦輪作",
            font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        float workTop = InfoStripTop + InfoStripHeight + BirdDuelMobileOverlayLayout.SectionGap;
        float workBottom = ComputeWorkBottom(actionVisible: false);

        BuildInfoStrip(panel.transform, workTop);
        BuildWorkArea(panel.transform, workTop, workBottom);
        BuildGrandmaBar(panel.transform, workBottom);

        actionButton = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform,
            "ActionBtn",
            "壓土",
            font);
        actionButton.gameObject.SetActive(false);
        actionButton.onClick.AddListener(OnActionButtonClicked);
        LayoutFooterButton(actionButton.GetComponent<RectTransform>(), 1);

        GameObject fillBar = new GameObject("HoldFill", typeof(RectTransform), typeof(Image));
        fillBar.transform.SetParent(actionButton.transform, false);
        RectTransform fillBarRt = fillBar.GetComponent<RectTransform>();
        fillBarRt.anchorMin = Vector2.zero;
        fillBarRt.anchorMax = Vector2.one;
        fillBarRt.offsetMin = new Vector2(6f, 6f);
        fillBarRt.offsetMax = new Vector2(-6f, -6f);
        actionFillImage = fillBar.GetComponent<Image>();
        actionFillImage.color = new Color(0.3f, 0.65f, 0.45f, 0.45f);
        actionFillImage.type = Image.Type.Filled;
        actionFillImage.fillMethod = Image.FillMethod.Horizontal;
        actionFillImage.fillAmount = 0f;
        actionFillImage.raycastTarget = false;

        skipButton = BirdDuelOverlayUiBuild.CreateSecondaryButton(
            panel.transform,
            "SkipBtn",
            "草奶奶代勞",
            font);
        skipButton.onClick.AddListener(() => Finish(new FarmResult
        {
            outcome = SideQuestA1TideMarkRewardService.FarmOutcome.Skipped
        }));
        LayoutFooterButton(skipButton.GetComponent<RectTransform>(), 0);
    }

    private static float ComputeWorkBottom(bool actionVisible)
    {
        float footerHeight = FooterBottomPad + BirdDuelMobileOverlayLayout.ButtonHeightSecondary;
        if (actionVisible)
        {
            footerHeight += BirdDuelMobileOverlayLayout.ButtonGap +
                            BirdDuelMobileOverlayLayout.ButtonHeightPrimary;
        }

        return footerHeight + GrandmaBarHeight + BirdDuelMobileOverlayLayout.SectionGap;
    }

    private void RefreshFooterLayout(bool actionVisible)
    {
        LayoutFooterButton(skipButton.GetComponent<RectTransform>(), 0);
        LayoutFooterButton(actionButton.GetComponent<RectTransform>(), 1);
        actionButton.gameObject.SetActive(actionVisible);

        float workBottom = ComputeWorkBottom(actionVisible);
        if (workAreaRt != null)
        {
            Vector2 min = workAreaRt.offsetMin;
            min.y = workBottom;
            workAreaRt.offsetMin = min;
        }
        if (grandmaBarRt != null)
            grandmaBarRt.anchoredPosition = new Vector2(0f, workBottom);
    }

    private static void LayoutFooterButton(RectTransform rt, int indexFromBottom)
    {
        float y = FooterBottomPad;
        if (indexFromBottom > 0)
        {
            y += BirdDuelMobileOverlayLayout.ButtonHeightSecondary + BirdDuelMobileOverlayLayout.ButtonGap;
        }

        float height = indexFromBottom == 0
            ? BirdDuelMobileOverlayLayout.ButtonHeightSecondary
            : BirdDuelMobileOverlayLayout.ButtonHeightPrimary;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-BirdDuelMobileOverlayLayout.ContentPadH * 2f, height);
    }

    private void BuildInfoStrip(Transform panel, float workTop)
    {
        GameObject infoGo = new GameObject("InfoStrip", typeof(RectTransform), typeof(Image));
        infoGo.transform.SetParent(panel, false);
        RectTransform infoRt = infoGo.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0f, 1f);
        infoRt.anchorMax = new Vector2(1f, 1f);
        infoRt.pivot = new Vector2(0.5f, 1f);
        infoRt.anchoredPosition = new Vector2(0f, -InfoStripTop);
        infoRt.sizeDelta = new Vector2(
            -BirdDuelMobileOverlayLayout.ContentPadH * 2f,
            InfoStripHeight);
        infoGo.GetComponent<Image>().color = BirdDuelUiColors.InfoCard;

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(infoGo.transform, false);
        RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(16f, 12f);
        bodyRt.offsetMax = new Vector2(-16f, -12f);

        GameObject seasonGo = new GameObject("Season", typeof(RectTransform), typeof(TextMeshProUGUI));
        seasonGo.transform.SetParent(bodyGo.transform, false);
        RectTransform seasonRt = seasonGo.GetComponent<RectTransform>();
        seasonRt.anchorMin = new Vector2(0f, 1f);
        seasonRt.anchorMax = new Vector2(1f, 1f);
        seasonRt.pivot = new Vector2(0.5f, 1f);
        seasonRt.sizeDelta = new Vector2(0f, 34f);
        seasonRt.anchoredPosition = Vector2.zero;
        infoSeasonTmp = seasonGo.GetComponent<TextMeshProUGUI>();
        infoSeasonTmp.fontSize = BirdDuelMobileOverlayLayout.BodyFontSize - 2f;
        infoSeasonTmp.fontStyle = FontStyles.Bold;
        infoSeasonTmp.alignment = TextAlignmentOptions.TopLeft;
        infoSeasonTmp.color = new Color(0.78f, 0.90f, 0.84f, 1f);
        infoSeasonTmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(infoSeasonTmp, font);

        GameObject instructionGo = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
        instructionGo.transform.SetParent(bodyGo.transform, false);
        RectTransform instructionRt = instructionGo.GetComponent<RectTransform>();
        instructionRt.anchorMin = new Vector2(0f, 0f);
        instructionRt.anchorMax = new Vector2(1f, 1f);
        instructionRt.offsetMin = Vector2.zero;
        instructionRt.offsetMax = new Vector2(0f, -38f);
        infoInstructionTmp = instructionGo.GetComponent<TextMeshProUGUI>();
        infoInstructionTmp.fontSize = BirdDuelMobileOverlayLayout.BodyFontSize - 4f;
        infoInstructionTmp.alignment = TextAlignmentOptions.TopLeft;
        infoInstructionTmp.enableWordWrapping = true;
        infoInstructionTmp.lineSpacing = 2f;
        infoInstructionTmp.color = BirdDuelUiColors.InkSoft;
        infoInstructionTmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(infoInstructionTmp, font);
    }

    private void BuildWorkArea(Transform panel, float workTop, float workBottom)
    {
        GameObject workGo = new GameObject("WorkArea", typeof(RectTransform));
        workGo.transform.SetParent(panel, false);
        workAreaRt = workGo.GetComponent<RectTransform>();
        BirdDuelMobileOverlayLayout.StretchHorizontal(workAreaRt, workTop, workBottom);

        GameObject toolCol = new GameObject("ToolCol", typeof(RectTransform), typeof(Image));
        toolCol.transform.SetParent(workGo.transform, false);
        RectTransform toolColRt = toolCol.GetComponent<RectTransform>();
        toolColRt.anchorMin = new Vector2(0f, 0f);
        toolColRt.anchorMax = new Vector2(0.28f, 1f);
        toolColRt.offsetMin = Vector2.zero;
        toolColRt.offsetMax = Vector2.zero;
        Image toolColImage = toolCol.GetComponent<Image>();
        toolColImage.color = new Color(0.62f, 0.56f, 0.44f, 0.35f);
        toolColImage.raycastTarget = false;

        GameObject toolHome = new GameObject("ToolHome", typeof(RectTransform), typeof(Image));
        toolHome.transform.SetParent(toolCol.transform, false);
        toolHomeRt = toolHome.GetComponent<RectTransform>();
        toolHomeRt.anchorMin = new Vector2(0.5f, 0.5f);
        toolHomeRt.anchorMax = new Vector2(0.5f, 0.5f);
        toolHomeRt.pivot = new Vector2(0.5f, 0.5f);
        toolHomeRt.sizeDelta = new Vector2(108f, 108f);
        toolHomeRt.anchoredPosition = Vector2.zero;
        Image toolHomeImage = toolHome.GetComponent<Image>();
        toolHomeImage.color = new Color(0.72f, 0.62f, 0.40f, 0.98f);
        toolHomeImage.raycastTarget = false;

        GameObject toolCaptionGo = new GameObject("ToolCaption", typeof(RectTransform), typeof(TextMeshProUGUI));
        toolCaptionGo.transform.SetParent(toolCol.transform, false);
        RectTransform toolCaptionRt = toolCaptionGo.GetComponent<RectTransform>();
        toolCaptionRt.anchorMin = new Vector2(0f, 0f);
        toolCaptionRt.anchorMax = new Vector2(1f, 0f);
        toolCaptionRt.pivot = new Vector2(0.5f, 0f);
        toolCaptionRt.anchoredPosition = new Vector2(0f, 12f);
        toolCaptionRt.sizeDelta = new Vector2(-8f, 36f);
        toolLabelTmp = toolCaptionGo.GetComponent<TextMeshProUGUI>();
        toolLabelTmp.fontSize = 22f;
        toolLabelTmp.alignment = TextAlignmentOptions.Center;
        toolLabelTmp.color = BirdDuelUiColors.InkSoft;
        toolLabelTmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(toolLabelTmp, font);

        GameObject plotHost = new GameObject("PlotHost", typeof(RectTransform), typeof(Image), typeof(Outline));
        plotHost.transform.SetParent(workGo.transform, false);
        plotAreaRt = plotHost.GetComponent<RectTransform>();
        plotAreaRt.anchorMin = new Vector2(0.30f, 0f);
        plotAreaRt.anchorMax = new Vector2(1f, 1f);
        plotAreaRt.offsetMin = new Vector2(8f, 8f);
        plotAreaRt.offsetMax = new Vector2(-8f, -8f);
        Image plotBg = plotHost.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(plotBg);
        plotBg.color = new Color(0.52f, 0.44f, 0.32f, 1f);
        plotDragOutline = plotHost.GetComponent<Outline>();
        plotDragOutline.effectColor = new Color(0.24f, 0.30f, 0.22f, 0.9f);
        plotDragOutline.effectDistance = new Vector2(2f, -2f);
        plotDragOutline.useGraphicAlpha = false;

        GameObject fillGo = new GameObject("PlotFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(plotHost.transform, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(10f, 10f);
        fillRt.offsetMax = new Vector2(-10f, -10f);
        plotFillImage = fillGo.GetComponent<Image>();
        plotFillImage.color = new Color(0.62f, 0.50f, 0.36f, 0.55f);
        plotFillImage.raycastTarget = false;

        BuildPlotGrid(plotHost.transform);
        BuildBedLabels(plotHost.transform);
    }

    private void BuildPlotGrid(Transform plotHost)
    {
        plotCellImages = new Image[PlotCols * PlotRows];
        for (int row = 0; row < PlotRows; row++)
        {
            for (int col = 0; col < PlotCols; col++)
            {
                int index = row * PlotCols + col;
                GameObject cellGo = new GameObject("Cell_" + index, typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(plotHost, false);
                RectTransform cellRt = cellGo.GetComponent<RectTransform>();
                float w = 1f / PlotCols;
                float h = 1f / PlotRows;
                cellRt.anchorMin = new Vector2(col * w, 1f - (row + 1) * h);
                cellRt.anchorMax = new Vector2((col + 1) * w, 1f - row * h);
                cellRt.offsetMin = new Vector2(2f, 2f);
                cellRt.offsetMax = new Vector2(-2f, -2f);
                Image cellImg = cellGo.GetComponent<Image>();
                cellImg.color = new Color(0.58f, 0.46f, 0.32f, 0.35f);
                cellImg.raycastTarget = false;
                plotCellImages[index] = cellImg;
            }
        }
    }

    private void BuildBedLabels(Transform plotHost)
    {
        CreateBedLabel(plotHost, "上畦·黑麥", new Vector2(0.5f, 0.92f));
        CreateBedLabel(plotHost, "中畦·休耕", new Vector2(0.5f, 0.52f));
        CreateBedLabel(plotHost, "下畦·豆", new Vector2(0.5f, 0.12f));
    }

    private void CreateBedLabel(Transform parent, string text, Vector2 anchor)
    {
        GameObject go = new GameObject("BedLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(160f, 28f);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.92f, 0.82f, 0.85f);
        tmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(tmp, font);
    }

    private void BuildGrandmaBar(Transform panel, float workBottom)
    {
        GameObject barGo = new GameObject("GrandmaBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(panel, false);
        grandmaBarRt = barGo.GetComponent<RectTransform>();
        grandmaBarRt.anchorMin = new Vector2(0f, 0f);
        grandmaBarRt.anchorMax = new Vector2(1f, 0f);
        grandmaBarRt.pivot = new Vector2(0.5f, 0f);
        grandmaBarRt.anchoredPosition = new Vector2(0f, workBottom);
        grandmaBarRt.sizeDelta = new Vector2(
            -BirdDuelMobileOverlayLayout.ContentPadH * 2f,
            GrandmaBarHeight);
        barGo.GetComponent<Image>().color = new Color(0.36f, 0.30f, 0.22f, 0.55f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(barGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 8f);
        textRt.offsetMax = new Vector2(-12f, -8f);
        grandmaTmp = textGo.GetComponent<TextMeshProUGUI>();
        grandmaTmp.fontSize = 22f;
        grandmaTmp.alignment = TextAlignmentOptions.Center;
        grandmaTmp.color = new Color(0.97f, 0.85f, 0.47f, 1f);
        grandmaTmp.enableWordWrapping = true;
        grandmaTmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(grandmaTmp, font);
        grandmaTmp.text = string.Empty;
    }

    private void RefreshInfoText(string instruction)
    {
        if (infoSeasonTmp != null)
            infoSeasonTmp.text = ResolveSeasonLabel();
        if (infoInstructionTmp != null)
            infoInstructionTmp.text = instruction;
    }

    private void Update()
    {
        if (rhythmActive)
            UpdateRhythm();
    }

    private void UpdateRhythm()
    {
        if (Time.unscaledTime >= nextBeatTime)
        {
            nextBeatTime += 60f / RhythmBpm;
            PulsePlot();
        }
    }

    private void OnActionButtonClicked()
    {
        StepDef step = GetCurrentStep();
        if (step.kind == StepKind.RhythmCompact)
        {
            float error = Mathf.Abs(Time.unscaledTime - nextBeatTime);
            if (error > RhythmGoodWindow && error < 60f / RhythmBpm - RhythmGoodWindow)
            {
                grandmaTmp.text = SideQuestA1PlotCopy.FarmInterject.RyeCompactFail;
                rhythmStartTime = Time.unscaledTime;
                nextBeatTime = rhythmStartTime + 60f / RhythmBpm;
                return;
            }

            rhythmHits++;
            BounceStaticTool();
            PulsePlot();
            RefreshProgressInstruction("依節拍點按「壓土」（{0}/2）。", rhythmHits, 2);
            if (rhythmHits >= 2)
            {
                rhythmActive = false;
                AdvanceStep();
            }
            else
            {
                nextBeatTime += 60f / RhythmBpm;
            }

            return;
        }
    }

    private void EnterStep()
    {
        ClearHotspots();
        DestroyDragTool();
        DestroyStaticTool();
        ResetStepCounters();
        waitRoutineRunning = false;
        rhythmActive = false;
        soakStirHits = 0;
        actionFillImage.fillAmount = 0f;

        StepDef step = GetCurrentStep();
        bool showActionButton = step.kind == StepKind.RhythmCompact;
        RefreshFooterLayout(showActionButton);
        RefreshInfoText(step.instruction);
        toolLabelTmp.text = string.IsNullOrEmpty(step.toolLabel) ? "—" : step.toolLabel;
        grandmaTmp.text = string.Empty;
        RefreshPlotVisual();

        switch (step.kind)
        {
            case StepKind.PlowDrag:
                CreateDragTool("犁", OnPlowDrag, null);
                break;
            case StepKind.ClickPlot:
                CreateStaticTool("種", new Color(0.78f, 0.62f, 0.32f, 1f));
                CreateClickTarget(
                    ryePlantAnchor,
                    new Vector2(240f, 96f),
                    "播種",
                    new Color(0.50f, 0.38f, 0.26f, 0.90f),
                    new Color(0.98f, 0.86f, 0.40f, 1f),
                    OnRyePlotPlantClick);
                break;
            case StepKind.RhythmCompact:
                CreateStaticTool("壓", new Color(0.68f, 0.54f, 0.36f, 1f));
                actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "壓土";
                rhythmHits = 0;
                rhythmActive = true;
                rhythmStartTime = Time.unscaledTime;
                nextBeatTime = rhythmStartTime + 60f / RhythmBpm;
                RefreshProgressInstruction("依節拍點按「壓土」（{0}/2）。", rhythmHits, 2);
                break;
            case StepKind.WaitAnimation:
                if (!waitRoutineRunning)
                    StartCoroutine(WaitStepRoutine());
                break;
            case StepKind.ScytheSwipe:
                CreateDragTool("鐮", OnScytheDrag, OnScytheDragEnd);
                break;
            case StepKind.ClickSoilBlocks:
                CreateStaticTool("鋤", new Color(0.72f, 0.58f, 0.38f, 1f));
                for (int i = 0; i < soilBlockAnchors.Length; i++)
                {
                    int captured = i;
                    CreateClickTarget(
                        soilBlockAnchors[captured],
                        new Vector2(84f, 84f),
                        "土塊",
                        new Color(0.55f, 0.42f, 0.30f, 0.95f),
                        new Color(0.82f, 0.68f, 0.42f, 1f),
                        go => OnSoilBlockClick(captured, go));
                }
                RefreshProgressInstruction("點擊土塊松土（{0}/3）。", soilHits, 3);
                break;
            case StepKind.NetDrag:
                Array.Clear(netCells, 0, netCells.Length);
                CreateDragTool("網", OnNetDrag, null);
                break;
            case StepKind.ClickHarvestPlants:
                CreateStaticTool("手摘", new Color(0.88f, 0.72f, 0.46f, 1f));
                BuildHarvestPlants();
                RefreshHarvestInstruction();
                break;
            case StepKind.PurslaneChoice:
                ShowPurslaneChoice();
                break;
            case StepKind.HoldSoak:
                CreateStaticTool("泡", new Color(0.42f, 0.62f, 0.78f, 1f));
                CreateClickTarget(
                    soakBasinAnchor,
                    new Vector2(200f, 88f),
                    "泡種盆",
                    new Color(0.34f, 0.58f, 0.76f, 0.88f),
                    new Color(0.52f, 0.78f, 0.92f, 1f),
                    OnSoakBasinClick);
                RefreshProgressInstruction("連續點按泡種盆直到冒泡（{0}/3）。", soakStirHits, 3);
                break;
            case StepKind.ClickGridCells:
                CreateStaticTool("豆", new Color(0.58f, 0.72f, 0.38f, 1f));
                for (int i = 0; i < 6; i++)
                {
                    int captured = i;
                    CreateClickTarget(
                        gridCellAnchors[captured],
                        new Vector2(76f, 76f),
                        "種",
                        new Color(0.46f, 0.62f, 0.32f, 0.92f),
                        new Color(0.78f, 0.92f, 0.48f, 1f),
                        go => OnGridCellClick(captured, go));
                }
                RefreshProgressInstruction("逐格點種（{0}/6）。", CountTrue(gridCells), 6);
                break;
            case StepKind.WaterChannelDrag:
                waterCells.Clear();
                CreateDragTool("水", OnWaterDrag, () =>
                {
                    if (waterCells.Count >= 6)
                        AdvanceAfterLine(SideQuestA1PlotCopy.FarmInterject.BeanWaterDone);
                    else
                        RefreshInfoText("水路還沒連到每一格，再試一次。");
                });
                break;
            case StepKind.ClickWeeds:
                CreateStaticTool("除", new Color(0.52f, 0.68f, 0.40f, 1f));
                for (int i = 0; i < weedAnchors.Length; i++)
                {
                    int captured = i;
                    CreateClickTarget(
                        weedAnchors[captured],
                        new Vector2(92f, 72f),
                        "藤草",
                        new Color(0.22f, 0.52f, 0.28f, 0.94f),
                        new Color(0.48f, 0.82f, 0.38f, 1f),
                        go => OnWeedClick(captured, go));
                }
                RefreshProgressInstruction("點除藤草（{0}/2）。", weedHits, 2);
                break;
            case StepKind.ClickPods:
                CreateStaticTool("莢", new Color(0.82f, 0.68f, 0.32f, 1f));
                for (int i = 0; i < podAnchors.Length; i++)
                {
                    int captured = i;
                    CreateClickTarget(
                        podAnchors[captured],
                        new Vector2(72f, 92f),
                        "豆莢",
                        new Color(0.62f, 0.48f, 0.22f, 0.95f),
                        new Color(0.98f, 0.84f, 0.32f, 1f),
                        go => OnPodClick(captured, go));
                }
                RefreshProgressInstruction("點收成熟豆莢（{0}/3）。", podHits, 3);
                break;
        }
    }

    private void OnRyePlotPlantClick(GameObject target)
    {
        PlayClickFeedback(target, true, () =>
        {
            ryePlotPlanted = true;
            RefreshPlotVisual();
            AdvanceStep();
        });
    }

    private void OnSoilBlockClick(int index, GameObject target)
    {
        if (index < 0 || index >= soilBlocksDone.Length || soilBlocksDone[index])
            return;

        soilBlocksDone[index] = true;
        PlayClickFeedback(target, true, () =>
        {
            soilHits++;
            RefreshProgressInstruction("點擊土塊松土（{0}/3）。", soilHits, 3);
            RefreshPlotVisual();
            if (soilHits >= 3)
                AdvanceStep();
        });
    }

    private void OnGridCellClick(int index, GameObject target)
    {
        if (index < 0 || index >= gridCells.Length || gridCells[index])
            return;

        gridCells[index] = true;
        PlayClickFeedback(target, true, () =>
        {
            RefreshProgressInstruction("逐格點種（{0}/6）。", CountTrue(gridCells), 6);
            RefreshPlotVisual();
            if (CountTrue(gridCells) >= 6)
                AdvanceStep();
        });
    }

    private void OnWeedClick(int index, GameObject target)
    {
        if (index < 0 || index >= weedsRemoved.Length || weedsRemoved[index])
            return;

        weedsRemoved[index] = true;
        PlayClickFeedback(target, true, () =>
        {
            weedHits++;
            RefreshProgressInstruction("點除藤草（{0}/2）。", weedHits, 2);
            RefreshPlotVisual();
            if (weedHits >= 2)
                AdvanceStep();
        });
    }

    private void OnPodClick(int index, GameObject target)
    {
        if (index < 0 || index >= podsPicked.Length || podsPicked[index])
            return;

        podsPicked[index] = true;
        PlayClickFeedback(target, true, () =>
        {
            podHits++;
            RefreshProgressInstruction("點收成熟豆莢（{0}/3）。", podHits, 3);
            RefreshPlotVisual();
            if (podHits >= 3)
                AdvanceAfterLine(SideQuestA1PlotCopy.FarmInterject.BeanHarvestDone);
        });
    }

    private void OnSoakBasinClick(GameObject target)
    {
        if (GetCurrentStep().kind != StepKind.HoldSoak || soakStirHits >= 3)
            return;

        PlayClickFeedback(target, false, () =>
        {
            soakStirHits++;
            RefreshProgressInstruction("連續點按泡種盆直到冒泡（{0}/3）。", soakStirHits, 3);
            RefreshPlotVisual();
            if (soakStirHits >= 3)
                AdvanceStep();
        });
    }

    private IEnumerator WaitStepRoutine()
    {
        waitRoutineRunning = true;
        yield return new WaitForSecondsRealtime(1.6f);
        waitRoutineRunning = false;
        AdvanceStep();
    }

    private void ShowPurslaneChoice()
    {
        RefreshInfoText("留一叢種子，或全部交出？");
        DestroyPurslaneChoiceRow();

        purslaneChoiceRowGo = new GameObject("PurslaneChoiceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        purslaneChoiceRowGo.transform.SetParent(plotAreaRt, false);
        RectTransform rowRt = purslaneChoiceRowGo.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.52f);
        rowRt.anchorMax = new Vector2(0.5f, 0.52f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(360f, 76f);
        rowRt.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = purslaneChoiceRowGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 32f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateChoiceButton(purslaneChoiceRowGo.transform, "留種", () =>
        {
            keptSeaPurslaneSeed = true;
            CompleteCropCycle();
        });
        CreateChoiceButton(purslaneChoiceRowGo.transform, "全交", CompleteCropCycle);
    }

    private void CreateChoiceButton(Transform parent, string label, Action onClick)
    {
        GameObject go = new GameObject("Choice_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = 148f;
        layout.preferredHeight = 72f;

        Image img = go.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(img);
        img.color = new Color(0.42f, 0.52f, 0.44f, 0.92f);

        Button btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.55f, 0.68f, 0.56f, 1f);
        colors.pressedColor = new Color(0.34f, 0.44f, 0.36f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());
        CreateCenteredLabel(go.transform, label, 22f);
    }

    private void DestroyPurslaneChoiceRow()
    {
        if (purslaneChoiceRowGo != null)
        {
            Destroy(purslaneChoiceRowGo);
            purslaneChoiceRowGo = null;
        }
    }

    private void CompleteCropCycle()
    {
        ClearHotspots();
        if (crop == Crop.Rye)
        {
            grandmaTmp.text = SideQuestA1PlotCopy.FarmInterject.RyeHarvestDone;
            crop = Crop.Fallow;
            stepIndex = 0;
            EnterStep();
            return;
        }

        if (crop == Crop.Fallow)
        {
            crop = Crop.Bean;
            stepIndex = 0;
            EnterStep();
            return;
        }

        Finish(new FarmResult
        {
            outcome = SideQuestA1TideMarkRewardService.FarmOutcome.Completed,
            keptSeaPurslaneSeed = keptSeaPurslaneSeed
        });
    }

    private void AdvanceAfterLine(string line)
    {
        grandmaTmp.text = line;
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        stepIndex++;
        StepDef[] steps = GetStepsForCrop(crop);
        if (stepIndex >= steps.Length)
        {
            CompleteCropCycle();
            return;
        }

        EnterStep();
    }

    private void OnPlowDrag(Vector2 screenPos, Camera cam)
    {
        Vector2 sample = ResolveDragSamplePoint(screenPos, cam);
        MarkCellsFromTool(sample, cam, plowCells);
        HighlightHoverCell(sample, cam);
        RefreshPlotVisual();
        if (CountTrue(plowCells) >= 16)
        {
            DestroyDragTool();
            AdvanceStep();
        }
    }

    private void OnNetDrag(Vector2 screenPos, Camera cam)
    {
        Vector2 sample = ResolveDragSamplePoint(screenPos, cam);
        MarkCellsFromTool(sample, cam, netCells);
        HighlightHoverCell(sample, cam);
        RefreshPlotVisual();
        if (CountTrue(netCells) >= 14)
        {
            DestroyDragTool();
            AdvanceAfterLine(SideQuestA1PlotCopy.FarmInterject.FallowNetDone);
        }
    }

    private void OnWaterDrag(Vector2 screenPos, Camera cam)
    {
        Vector2 sample = ResolveDragSamplePoint(screenPos, cam);
        HighlightHoverCell(sample, cam);
        if (!TryGetPlotCell(sample, cam, out int cell))
            return;
        waterCells.Add(cell);
        RefreshPlotVisual();
    }

    private Vector2 lastScythePos;
    private bool hasScythePos;

    private void OnScytheDrag(Vector2 screenPos, Camera cam)
    {
        if (hasScythePos)
        {
            float dx = screenPos.x - lastScythePos.x;
            if (dx < -80f)
                scytheLeft = true;
            if (dx > 80f)
                scytheRight = true;
        }

        lastScythePos = screenPos;
        hasScythePos = true;
    }

    private void OnScytheDragEnd()
    {
        hasScythePos = false;
        if (scytheLeft && scytheRight)
        {
            DestroyDragTool();
            AdvanceAfterLine(SideQuestA1PlotCopy.FarmInterject.RyeHarvestDone);
        }
        else
        {
            RefreshInfoText("左右各劃一次，才能收穫黑麥。");
        }
    }

    private int hoverCellIndex = -1;

    private Vector2 ResolveDragSamplePoint(Vector2 pointerScreenPos, Camera cam)
    {
        if (activeDrag != null)
            return activeDrag.GetCenterScreenPoint();
        return pointerScreenPos;
    }

    private void HighlightHoverCell(Vector2 screenPos, Camera cam)
    {
        hoverCellIndex = TryGetPlotCell(screenPos, cam, out int cell) ? cell : -1;
    }

    private void MarkCellsFromTool(Vector2 screenPos, Camera cam, bool[] cells)
    {
        if (!TryGetPlotCell(screenPos, cam, out int center))
            return;

        cells[center] = true;
        if (center % PlotCols > 0) cells[center - 1] = true;
        if (center % PlotCols < PlotCols - 1) cells[center + 1] = true;
        if (center >= PlotCols) cells[center - PlotCols] = true;
        if (center < PlotCols * (PlotRows - 1)) cells[center + PlotCols] = true;
    }

    private bool TryGetPlotCell(Vector2 screenPos, Camera cam, out int cellIndex)
    {
        cellIndex = -1;
        if (plotAreaRt == null)
            return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(plotAreaRt, screenPos, cam, out Vector2 local))
            return false;

        Rect rect = plotAreaRt.rect;
        float nx = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float ny = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
            return false;

        int col = Mathf.Clamp(Mathf.FloorToInt(nx * PlotCols), 0, PlotCols - 1);
        // Grid row 0 is anchored at the top; local Y grows upward.
        int row = Mathf.Clamp(Mathf.FloorToInt((1f - ny) * PlotRows), 0, PlotRows - 1);
        cellIndex = row * PlotCols + col;
        return true;
    }

    private void RefreshPlotVisual()
    {
        StepDef step = GetCurrentStep();
        Color baseSoil = new Color(0.58f, 0.46f, 0.32f, 0.35f);
        Color workedSoil = new Color(0.28f, 0.34f, 0.24f, 0.82f);
        Color planted = new Color(0.24f, 0.48f, 0.28f, 0.82f);
        Color watered = new Color(0.22f, 0.42f, 0.58f, 0.82f);

        if (plotCellImages == null)
            return;

        for (int i = 0; i < plotCellImages.Length; i++)
        {
            if (plotCellImages[i] == null)
                continue;

            Color cellColor = baseSoil;
            if (i == hoverCellIndex && (step.kind == StepKind.PlowDrag || step.kind == StepKind.NetDrag))
                cellColor = new Color(1f, 0.88f, 0.22f, 0.92f);
            else if (step.kind == StepKind.PlowDrag && plowCells[i])
                cellColor = workedSoil;
            else if (step.kind == StepKind.NetDrag && netCells[i])
                cellColor = workedSoil;
            else if (step.kind == StepKind.ClickGridCells && i < gridCells.Length && gridCells[i])
                cellColor = planted;
            else if (step.kind == StepKind.WaterChannelDrag && waterCells.Contains(i))
                cellColor = watered;
            else if (i == hoverCellIndex && step.kind == StepKind.WaterChannelDrag)
                cellColor = new Color(0.42f, 0.72f, 0.92f, 0.92f);
            else if (step.kind == StepKind.ClickHarvestPlants)
            {
                int row = i / PlotCols;
                if (row == 1 || row == 2)
                    cellColor = new Color(0.28f, 0.54f, 0.34f, 0.78f);
            }
            else if (step.kind == StepKind.ClickPlot)
            {
                int row = i / PlotCols;
                if (row == 0)
                    cellColor = ryePlotPlanted
                        ? planted
                        : new Color(0.48f, 0.40f, 0.28f, 0.72f);
            }
            else if (step.kind == StepKind.ClickSoilBlocks)
            {
                int row = i / PlotCols;
                if (row == 1 || row == 2)
                {
                    cellColor = Color.Lerp(
                        new Color(0.50f, 0.40f, 0.28f, 0.70f),
                        workedSoil,
                        soilHits / 3f);
                }
            }
            else if (step.kind == StepKind.ClickWeeds)
            {
                int row = i / PlotCols;
                if (row == 2 || row == 3)
                    cellColor = new Color(0.30f, 0.50f, 0.32f, 0.72f);
            }
            else if (step.kind == StepKind.ClickPods)
            {
                int row = i / PlotCols;
                if (row == 3)
                    cellColor = new Color(0.28f, 0.50f, 0.30f, 0.80f);
            }

            plotCellImages[i].color = cellColor;
        }

        if (step.kind == StepKind.PlowDrag || step.kind == StepKind.NetDrag)
        {
            float ratio = CountTrue(step.kind == StepKind.PlowDrag ? plowCells : netCells) /
                          (float)plotCellImages.Length;
            plotFillImage.color = Color.Lerp(
                new Color(0.62f, 0.50f, 0.36f, 0.45f),
                new Color(0.34f, 0.40f, 0.30f, 0.65f),
                ratio);
        }
        else if (step.kind == StepKind.WaterChannelDrag)
        {
            plotFillImage.color = waterCells.Count >= 6
                ? new Color(0.24f, 0.40f, 0.52f, 0.55f)
                : new Color(0.62f, 0.50f, 0.36f, 0.45f);
        }
        else if (step.kind == StepKind.ClickHarvestPlants)
        {
            float ratio = plantHits / 5f;
            plotFillImage.color = Color.Lerp(
                new Color(0.30f, 0.52f, 0.34f, 0.58f),
                new Color(0.50f, 0.44f, 0.30f, 0.48f),
                ratio);
        }
        else if (step.kind == StepKind.ClickSoilBlocks)
        {
            plotFillImage.color = Color.Lerp(
                new Color(0.54f, 0.44f, 0.30f, 0.52f),
                new Color(0.36f, 0.40f, 0.28f, 0.62f),
                soilHits / 3f);
        }
        else if (step.kind == StepKind.ClickGridCells)
        {
            float ratio = CountTrue(gridCells) / 6f;
            plotFillImage.color = Color.Lerp(
                new Color(0.58f, 0.48f, 0.32f, 0.48f),
                new Color(0.28f, 0.46f, 0.30f, 0.58f),
                ratio);
        }
        else if (step.kind == StepKind.ClickPods)
        {
            plotFillImage.color = Color.Lerp(
                new Color(0.30f, 0.48f, 0.30f, 0.55f),
                new Color(0.52f, 0.44f, 0.28f, 0.48f),
                podHits / 3f);
        }
        else if (step.kind == StepKind.HoldSoak)
        {
            plotFillImage.color = Color.Lerp(
                new Color(0.38f, 0.56f, 0.72f, 0.45f),
                new Color(0.28f, 0.62f, 0.82f, 0.62f),
                soakStirHits / 3f);
        }
        else
        {
            plotFillImage.color = new Color(0.62f, 0.50f, 0.36f, 0.45f);
        }
    }

    private void PulsePlot()
    {
        if (plotFillImage == null)
            return;
        plotFillImage.color = new Color(0.45f, 0.36f, 0.24f, 0.95f);
    }

    private void CreateDragTool(string label, Action<Vector2, Camera> onDrag, Action onEnd)
    {
        DestroyDragTool();
        dragToolGo = new GameObject("DragTool", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SideQuestA1FarmUiDrag));
        dragToolGo.transform.SetParent(toolHomeRt, false);
        RectTransform rt = dragToolGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image toolImage = dragToolGo.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(toolImage);
        toolImage.color = new Color(0.92f, 0.78f, 0.38f, 1f);
        toolImage.raycastTarget = true;

        CreateCenteredLabel(dragToolGo.transform, label, 28f);

        activeDrag = dragToolGo.GetComponent<SideQuestA1FarmUiDrag>();
        activeDrag.dragBounds = workAreaRt != null ? workAreaRt : plotAreaRt;
        activeDrag.boundsPadding = 8f;
        activeDrag.dragScaleMultiplier = 1.34f;
        activeDrag.pressScaleMultiplier = 1.14f;
        activeDrag.onDragHighlightChanged = SetPlotDragHighlight;
        activeDrag.CaptureHome();
        activeDrag.canDrag = () => true;
        activeDrag.onDragScreen = onDrag;
        activeDrag.onDragEnded = onEnd;
    }

    private void SetPlotDragHighlight(bool active)
    {
        if (plotDragOutline == null)
            return;

        plotDragOutline.effectColor = active
            ? new Color(1f, 0.88f, 0.12f, 1f)
            : new Color(0.24f, 0.30f, 0.22f, 0.9f);
        plotDragOutline.effectDistance = active
            ? new Vector2(8f, -8f)
            : new Vector2(2f, -2f);

        if (plotFillImage != null)
        {
            Color baseFill = new Color(0.62f, 0.50f, 0.36f, active ? 0.72f : 0.45f);
            plotFillImage.color = active
                ? Color.Lerp(baseFill, new Color(0.92f, 0.82f, 0.28f, 0.55f), 0.45f)
                : baseFill;
        }
    }

    private void DestroyDragTool()
    {
        hoverCellIndex = -1;
        SetPlotDragHighlight(false);
        if (dragToolGo != null)
            Destroy(dragToolGo);
        dragToolGo = null;
        activeDrag = null;
    }

    private void CreateStaticTool(string label, Color color)
    {
        DestroyStaticTool();
        staticToolGo = new GameObject("StaticTool", typeof(RectTransform), typeof(Image), typeof(Outline));
        staticToolGo.transform.SetParent(toolHomeRt, false);
        RectTransform rt = staticToolGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image toolImage = staticToolGo.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(toolImage);
        toolImage.color = color;
        toolImage.raycastTarget = false;

        Outline outline = staticToolGo.GetComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.88f, 0.35f, 0.85f);
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = false;

        CreateCenteredLabel(staticToolGo.transform, label, 28f);
    }

    private void DestroyStaticTool()
    {
        if (staticToolGo != null)
            Destroy(staticToolGo);
        staticToolGo = null;
    }

    private void BuildHarvestPlants()
    {
        ClearHarvestPlants();
        for (int i = 0; i < harvestPlantAnchors.Length; i++)
        {
            int captured = i;
            GameObject plantGo = CreateHarvestPlantMarker(harvestPlantAnchors[i], captured);
            harvestPlantMarkers.Add(plantGo);
        }
    }

    private GameObject CreateHarvestPlantMarker(Vector2 anchor, int index)
    {
        GameObject root = new GameObject("HarvestPlant_" + index, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        root.transform.SetParent(plotAreaRt, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(84f, 84f);
        rt.anchoredPosition = Vector2.zero;

        Image bush = root.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(bush);
        bush.color = new Color(0.24f, 0.58f, 0.32f, 0.96f);
        bush.raycastTarget = true;

        Outline outline = root.GetComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.92f, 0.48f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = false;

        GameObject crown = new GameObject("Crown", typeof(RectTransform), typeof(Image));
        crown.transform.SetParent(root.transform, false);
        RectTransform crownRt = crown.GetComponent<RectTransform>();
        crownRt.anchorMin = new Vector2(0.15f, 0.35f);
        crownRt.anchorMax = new Vector2(0.85f, 0.95f);
        crownRt.offsetMin = Vector2.zero;
        crownRt.offsetMax = Vector2.zero;
        Image crownImg = crown.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(crownImg);
        crownImg.color = new Color(0.34f, 0.72f, 0.38f, 0.92f);
        crownImg.raycastTarget = false;

        GameObject tagGo = new GameObject("Tag", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagGo.transform.SetParent(root.transform, false);
        RectTransform tagRt = tagGo.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0f, 0f);
        tagRt.anchorMax = new Vector2(1f, 0f);
        tagRt.pivot = new Vector2(0.5f, 1f);
        tagRt.sizeDelta = new Vector2(0f, 22f);
        tagRt.anchoredPosition = new Vector2(0f, -6f);
        TextMeshProUGUI tagTmp = tagGo.GetComponent<TextMeshProUGUI>();
        tagTmp.text = "海蓬";
        tagTmp.fontSize = 18f;
        tagTmp.alignment = TextAlignmentOptions.Center;
        tagTmp.color = new Color(0.92f, 0.98f, 0.86f, 1f);
        tagTmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(tagTmp, font);

        Button btn = root.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.98f, 0.82f, 1f);
        colors.pressedColor = new Color(1f, 0.90f, 0.45f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;
        btn.targetGraphic = bush;
        btn.onClick.AddListener(() => OnPickHarvestPlant(index, root));

        return root;
    }

    private void OnPickHarvestPlant(int index, GameObject plantGo)
    {
        if (index < 0 || index >= harvestPlantsPicked.Length || harvestPlantsPicked[index])
            return;

        harvestPlantsPicked[index] = true;
        Button btn = plantGo.GetComponent<Button>();
        if (btn != null)
            btn.interactable = false;

        if (harvestPickRoutine != null)
            StopCoroutine(harvestPickRoutine);
        harvestPickRoutine = StartCoroutine(CoPickPlantFeedback(index, plantGo));
    }

    private IEnumerator CoPickPlantFeedback(int index, GameObject plantGo)
    {
        RectTransform rt = plantGo != null ? plantGo.transform as RectTransform : null;
        Image bush = plantGo != null ? plantGo.GetComponent<Image>() : null;
        Outline outline = plantGo != null ? plantGo.GetComponent<Outline>() : null;
        Graphic[] graphics = plantGo != null ? plantGo.GetComponentsInChildren<Graphic>(true) : null;
        float[] startAlphas = null;
        if (graphics != null)
        {
            startAlphas = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                startAlphas[i] = graphics[i] != null ? graphics[i].color.a : 1f;
        }

        Vector3 startScale = rt != null ? rt.localScale : Vector3.one;
        Color startOutline = outline != null ? outline.effectColor : Color.white;

        PulsePlot();
        BounceStaticTool();

        const float popDur = 0.12f;
        const float fadeDur = 0.22f;
        float t = 0f;
        while (t < popDur && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDur);
            rt.localScale = Vector3.Lerp(startScale, startScale * 1.22f, p);
            if (outline != null)
            {
                outline.effectColor = Color.Lerp(
                    startOutline,
                    new Color(1f, 0.92f, 0.18f, 1f),
                    p);
            }
            yield return null;
        }

        t = 0f;
        while (t < fadeDur && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDur);
            rt.localScale = Vector3.Lerp(startScale * 1.22f, startScale * 0.72f, p);
            if (graphics != null && startAlphas != null)
            {
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] == null)
                        continue;
                    Color c = graphics[i].color;
                    c.a = startAlphas[i] * (1f - p);
                    graphics[i].color = c;
                }
            }
            yield return null;
        }

        plantHits++;
        RefreshHarvestInstruction();
        RefreshPlotVisual();

        if (plantGo != null)
            Destroy(plantGo);
        if (index >= 0 && index < harvestPlantMarkers.Count)
            harvestPlantMarkers[index] = null;
        harvestPickRoutine = null;

        if (plantHits >= 5)
            AdvanceAfterLine(SideQuestA1PlotCopy.FarmInterject.PurslanePick);
    }

    private void BounceStaticTool()
    {
        if (staticToolGo == null)
            return;
        StartCoroutine(CoBounceStaticTool(staticToolGo.transform as RectTransform));
    }

    private static IEnumerator CoBounceStaticTool(RectTransform rt)
    {
        if (rt == null)
            yield break;

        Vector3 baseScale = rt.localScale;
        const float dur = 0.16f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float bump = 1f + 0.14f * Mathf.Sin(p * Mathf.PI);
            rt.localScale = baseScale * bump;
            yield return null;
        }

        rt.localScale = baseScale;
    }

    private void RefreshHarvestInstruction()
    {
        RefreshInfoText($"採收海蓬（{plantHits}/5）。");
    }

    private void RefreshProgressInstruction(string template, int current, int total)
    {
        RefreshInfoText(string.Format(template, current, total));
    }

    private GameObject CreateClickTarget(
        Vector2 anchor,
        Vector2 size,
        string tag,
        Color bodyColor,
        Color outlineColor,
        Action<GameObject> onClick)
    {
        GameObject root = new GameObject("ClickTarget_" + tag, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        root.transform.SetParent(plotAreaRt, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image body = root.GetComponent<Image>();
        SideQuestA1FarmUiDrag.ApplyWhiteSprite(body);
        body.color = bodyColor;
        body.raycastTarget = true;

        Outline outline = root.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = false;

        GameObject tagGo = new GameObject("Tag", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagGo.transform.SetParent(root.transform, false);
        RectTransform tagRt = tagGo.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0f, 0f);
        tagRt.anchorMax = new Vector2(1f, 0f);
        tagRt.pivot = new Vector2(0.5f, 1f);
        tagRt.sizeDelta = new Vector2(0f, 22f);
        tagRt.anchoredPosition = new Vector2(0f, -4f);
        TextMeshProUGUI tagTmp = tagGo.GetComponent<TextMeshProUGUI>();
        tagTmp.text = tag;
        tagTmp.fontSize = 18f;
        tagTmp.alignment = TextAlignmentOptions.Center;
        tagTmp.color = new Color(0.96f, 0.94f, 0.86f, 0.95f);
        tagTmp.raycastTarget = false;
        BirdDuelOverlayUiBuild.ApplyFont(tagTmp, font);

        Button btn = root.GetComponent<Button>();
        ApplyInteractiveButtonColors(btn);
        btn.targetGraphic = body;
        btn.onClick.AddListener(() => onClick?.Invoke(root));

        clickTargets.Add(root);
        return root;
    }

    private static void ApplyInteractiveButtonColors(Button btn)
    {
        if (btn == null)
            return;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.98f, 0.82f, 1f);
        colors.pressedColor = new Color(1f, 0.90f, 0.42f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;
    }

    private void PlayClickFeedback(GameObject target, bool destroyAfter, Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        Button btn = target.GetComponent<Button>();
        if (btn != null)
            btn.interactable = false;

        PulsePlot();
        BounceStaticTool();
        StartCoroutine(CoClickPopFeedback(target, destroyAfter, onComplete));
    }

    private IEnumerator CoClickPopFeedback(GameObject target, bool destroyAfter, Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        RectTransform rt = target.transform as RectTransform;
        Outline outline = target.GetComponent<Outline>();
        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        float[] startAlphas = new float[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
            startAlphas[i] = graphics[i] != null ? graphics[i].color.a : 1f;

        Vector3 startScale = rt != null ? rt.localScale : Vector3.one;
        Color startOutline = outline != null ? outline.effectColor : Color.white;

        const float popDur = 0.10f;
        float t = 0f;
        while (t < popDur && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDur);
            rt.localScale = Vector3.Lerp(startScale, startScale * 1.18f, p);
            if (outline != null)
            {
                outline.effectColor = Color.Lerp(startOutline, new Color(1f, 0.92f, 0.18f, 1f), p);
            }
            yield return null;
        }

        if (!destroyAfter)
        {
            if (rt != null)
                rt.localScale = startScale;
            if (outline != null)
                outline.effectColor = startOutline;
            Button btn = target.GetComponent<Button>();
            if (btn != null)
                btn.interactable = true;
            onComplete?.Invoke();
            yield break;
        }

        const float fadeDur = 0.18f;
        t = 0f;
        while (t < fadeDur && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDur);
            rt.localScale = Vector3.Lerp(startScale * 1.18f, startScale * 0.76f, p);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                    continue;
                Color c = graphics[i].color;
                c.a = startAlphas[i] * (1f - p);
                graphics[i].color = c;
            }
            yield return null;
        }

        clickTargets.Remove(target);
        Destroy(target);
        onComplete?.Invoke();
    }

    private void ClearClickTargets()
    {
        for (int i = 0; i < clickTargets.Count; i++)
        {
            if (clickTargets[i] != null)
                Destroy(clickTargets[i]);
        }
        clickTargets.Clear();
    }

    private void ClearHarvestPlants()
    {
        if (harvestPickRoutine != null)
        {
            StopCoroutine(harvestPickRoutine);
            harvestPickRoutine = null;
        }

        for (int i = 0; i < harvestPlantMarkers.Count; i++)
        {
            if (harvestPlantMarkers[i] != null)
                Destroy(harvestPlantMarkers[i]);
        }
        harvestPlantMarkers.Clear();
        Array.Clear(harvestPlantsPicked, 0, harvestPlantsPicked.Length);
    }

    private void ClearHotspots()
    {
        ClearHarvestPlants();
        ClearClickTargets();
        DestroyPurslaneChoiceRow();
        for (int i = 0; i < hotspotButtons.Count; i++)
        {
            if (hotspotButtons[i] != null)
                Destroy(hotspotButtons[i].gameObject);
        }
        hotspotButtons.Clear();
    }

    private void ResetStepCounters()
    {
        Array.Clear(plowCells, 0, plowCells.Length);
        Array.Clear(netCells, 0, netCells.Length);
        Array.Clear(gridCells, 0, gridCells.Length);
        waterCells.Clear();
        soilHits = 0;
        plantHits = 0;
        Array.Clear(harvestPlantsPicked, 0, harvestPlantsPicked.Length);
        Array.Clear(soilBlocksDone, 0, soilBlocksDone.Length);
        Array.Clear(weedsRemoved, 0, weedsRemoved.Length);
        Array.Clear(podsPicked, 0, podsPicked.Length);
        ryePlotPlanted = false;
        weedHits = 0;
        podHits = 0;
        scytheLeft = false;
        scytheRight = false;
    }

    private StepDef GetCurrentStep() => GetStepsForCrop(crop)[stepIndex];

    private static StepDef[] GetStepsForCrop(Crop c)
    {
        switch (c)
        {
            case Crop.Fallow: return FallowSteps;
            case Crop.Bean: return BeanSteps;
            default: return RyeSteps;
        }
    }

    private string ResolveSeasonLabel()
    {
        switch (crop)
        {
            case Crop.Rye: return "本旬：秋播 · 海風黑麥（上畦）";
            case Crop.Fallow: return "本旬：休耕 · 燈芯海蓬（中畦）";
            default: return "本旬：春播 · 潮根豆（下畦）";
        }
    }

    private static Vector2 ComputeDefaultGridCellAnchor(int index)
    {
        int col = index % 3;
        int row = index / 3;
        return new Vector2(0.18f + col * 0.32f, 0.08f + row * 0.12f);
    }

    private static int CountTrue(bool[] values)
    {
        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i])
                count++;
        }

        return count;
    }

    private void Finish(FarmResult result)
    {
        Action<FarmResult> cb = onFinished;
        onFinished = null;
        SideQuestA1OverlayVoice.Stop();
        Destroy(gameObject);
        cb?.Invoke(result);
    }

    private static TextMeshProUGUI CreateCenteredLabel(Transform parent, string text, float size)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
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

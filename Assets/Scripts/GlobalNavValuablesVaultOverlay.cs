using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>全局導航：貴重品庫全螢幕視窗（4 欄 × 6 列，右側物品資訊）。</summary>
public sealed class GlobalNavValuablesVaultOverlay
{
    private const float HeaderHeight = 76f;
    private const float FooterHeight = 48f;
    private const float PanelPad = 28f;
    private const float GridPad = 12f;
    private const float BodyColumnGap = 16f;
    private const float DetailPanelWidth = 300f;
    private const float DetailPanelInnerPad = 20f;
    private const float DetailIconSize = 168f;
    private const float CellSpacing = 10f;
    private const float ScrollContentPadH = 12f;
    private const float ScrollContentPadV = 8f;
    private const int OverlayLayoutVersion = 8;

    private static readonly Color DimColor = new Color(0.1f, 0.08f, 0.06f, 0.72f);
    private static readonly Color PanelBg = new Color(0.94f, 0.9f, 0.84f, 0.99f);
    private static readonly Color HeaderBg = new Color(0.85f, 0.79f, 0.68f, 0.9f);
    private static readonly Color FooterBg = new Color(0.88f, 0.82f, 0.74f, 0.92f);
    private static readonly Color DetailPanelBg = new Color(0.91f, 0.87f, 0.8f, 0.98f);
    private static readonly Color DetailDividerColor = new Color(0.72f, 0.64f, 0.54f, 0.55f);
    private static readonly Color GridAreaBg = new Color(0.96f, 0.93f, 0.88f, 0.35f);
    private static readonly Color CellEmptyBg = new Color(0.9f, 0.86f, 0.8f, 0.95f);
    private static readonly Color CellFilledBg = new Color(0.98f, 0.96f, 0.92f, 0.98f);
    private static readonly Color CellBorder = new Color(0.55f, 0.48f, 0.4f, 0.85f);
    private static readonly Color CellSelectedBorder = new Color(0.32f, 0.52f, 0.78f, 0.98f);
    private static readonly Color DetailTitleColor = new Color(0.38f, 0.32f, 0.26f, 1f);
    private static readonly Color DetailBodyColor = new Color(0.28f, 0.24f, 0.2f, 1f);
    private static readonly Color DetailMutedColor = new Color(0.48f, 0.42f, 0.36f, 1f);

    private readonly Action<TextMeshProUGUI> applyFont;
    private readonly Func<Transform, GameObject> createCloseButton;

    private GameObject root;
    private TextMeshProUGUI footerHintTmp;
    private TextMeshProUGUI detailNameTmp;
    private TextMeshProUGUI detailBodyTmp;
    private TextMeshProUGUI detailSlotTmp;
    private Image detailIconImage;
    private GameObject detailIconPlaceholder;
    private RectTransform detailDepositRoot;
    private ScrollRect vaultScrollRect;
    private int builtLayoutVersion;
    private int selectedCellIndex = -1;
    private readonly CellUi[] cells = new CellUi[ValuablesVaultState.SlotCount];

    private sealed class CellUi
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI label;
        public Button button;
        public Outline outline;
        public int cellIndex;
    }

    public GlobalNavValuablesVaultOverlay(Action<TextMeshProUGUI> applyFont, Func<Transform, GameObject> createCloseButton)
    {
        this.applyFont = applyFont;
        this.createCloseButton = createCloseButton;
    }

    public bool IsOpen => root != null && root.activeSelf;

    public void EnsureBuilt(Transform canvasTransform)
    {
        if (root != null)
        {
            if (builtLayoutVersion == OverlayLayoutVersion)
                return;
            Destroy();
        }

        root = new GameObject("GlobalValuablesVaultOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasTransform, false);
        StretchFull(root.GetComponent<RectTransform>());
        Image dim = root.GetComponent<Image>();
        dim.color = DimColor;
        dim.raycastTarget = true;

        float panelWidth = Mathf.Min(Screen.width * 0.88f, 1080f);
        float panelHeight = Mathf.Min(Screen.height * 0.86f, 820f);

        GameObject panel = new GameObject("VaultPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
        panel.GetComponent<Image>().color = PanelBg;

        BuildHeader(panel.transform, panelWidth);
        BuildFooter(panel.transform);
        BuildBody(panel.transform, panelWidth, panelHeight);

        root.SetActive(false);
        builtLayoutVersion = OverlayLayoutVersion;
    }

    public void Open()
    {
        if (root == null)
            return;
        selectedCellIndex = -1;
        ValuablesVaultCatalog.SyncOwnedCdsToVault(PlayerData.GetActivePlayerSlotOrDefault());
        if (ValuablesVaultState.HasPendingChanges)
            PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
        RefreshAllCells();
        RefreshDetailPanel(-1);
        RefreshCellSelectionVisuals();
        ResetScrollToTop();
        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Destroy()
    {
        if (root != null)
            UnityEngine.Object.Destroy(root);
        root = null;
        footerHintTmp = null;
        detailNameTmp = null;
        detailBodyTmp = null;
        detailSlotTmp = null;
        detailIconImage = null;
        detailIconPlaceholder = null;
        detailDepositRoot = null;
        vaultScrollRect = null;
        builtLayoutVersion = 0;
        selectedCellIndex = -1;
        for (int i = 0; i < cells.Length; i++)
            cells[i] = null;
    }

    private void ResetScrollToTop()
    {
        if (vaultScrollRect == null)
            return;
        vaultScrollRect.StopMovement();
        vaultScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildHeader(Transform panel, float panelWidth)
    {
        GameObject header = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.offsetMin = new Vector2(0f, -HeaderHeight);
        headerRt.offsetMax = Vector2.zero;
        header.GetComponent<Image>().color = HeaderBg;

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(32f, -18f);
        titleRt.sizeDelta = new Vector2(panelWidth - 200f, 48f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(titleTmp);
        titleTmp.fontSize = 40f;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = new Color(0.25f, 0.2f, 0.15f, 1f);
        titleTmp.text = "貴重品庫";
        titleTmp.raycastTarget = false;

        GameObject closeBtnObj = createCloseButton?.Invoke(header.transform);
        if (closeBtnObj != null)
        {
            Button closeBtn = closeBtnObj.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }
        }
    }

    private void BuildFooter(Transform panel)
    {
        GameObject footer = new GameObject("FooterBar", typeof(RectTransform), typeof(Image));
        footer.transform.SetParent(panel, false);
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = Vector2.zero;
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.offsetMin = Vector2.zero;
        footerRt.offsetMax = new Vector2(0f, FooterHeight);
        footer.GetComponent<Image>().color = FooterBg;

        GameObject hintObj = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(footer.transform, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = Vector2.zero;
        hintRt.anchorMax = Vector2.one;
        hintRt.offsetMin = new Vector2(PanelPad, 6f);
        hintRt.offsetMax = new Vector2(-PanelPad, -6f);
        footerHintTmp = hintObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(footerHintTmp);
        footerHintTmp.fontSize = 20f;
        footerHintTmp.alignment = TextAlignmentOptions.MidlineLeft;
        footerHintTmp.color = DetailMutedColor;
        footerHintTmp.text = ValuablesVaultUiCopy.FooterHint;
        footerHintTmp.raycastTarget = false;
    }

    private void BuildBody(Transform panel, float panelWidth, float panelHeight)
    {
        float bodyLeft = PanelPad;
        float bodyRight = PanelPad;
        float bodyBottom = FooterHeight + GridPad;
        float bodyTop = HeaderHeight + GridPad;
        float innerWidth = panelWidth - bodyLeft - bodyRight;
        float gridColumnWidth = innerWidth - DetailPanelWidth - BodyColumnGap;

        GameObject bodyRow = new GameObject("VaultBodyRow", typeof(RectTransform));
        bodyRow.transform.SetParent(panel, false);
        RectTransform bodyRowRt = bodyRow.GetComponent<RectTransform>();
        bodyRowRt.anchorMin = Vector2.zero;
        bodyRowRt.anchorMax = Vector2.one;
        bodyRowRt.offsetMin = new Vector2(bodyLeft, bodyBottom);
        bodyRowRt.offsetMax = new Vector2(-bodyRight, -bodyTop);

        BuildGridColumn(bodyRow.transform, gridColumnWidth);
        BuildDetailPanel(bodyRow.transform, DetailPanelWidth);
    }

    private void BuildGridColumn(Transform bodyRow, float columnWidth)
    {
        GameObject gridColumn = new GameObject("GridColumn", typeof(RectTransform));
        gridColumn.transform.SetParent(bodyRow, false);
        RectTransform gridColumnRt = gridColumn.GetComponent<RectTransform>();
        gridColumnRt.anchorMin = new Vector2(0f, 0f);
        gridColumnRt.anchorMax = new Vector2(0f, 1f);
        gridColumnRt.pivot = new Vector2(0f, 0.5f);
        gridColumnRt.sizeDelta = new Vector2(columnWidth, 0f);
        gridColumnRt.anchoredPosition = Vector2.zero;

        GameObject viewportGo = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(RectMask2D),
            typeof(ScrollRect));
        viewportGo.transform.SetParent(gridColumn.transform, false);
        RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
        StretchFull(viewportRt);
        viewportGo.GetComponent<Image>().color = GridAreaBg;

        Vector2 cellSize = ResolveViewportSquareCellSize(columnWidth);

        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        RectTransform contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GridLayoutGroup gridLayout = contentGo.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = ValuablesVaultState.GridWidth;
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(CellSpacing, CellSpacing);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.padding = new RectOffset(
            Mathf.RoundToInt(ScrollContentPadH),
            Mathf.RoundToInt(ScrollContentPadH),
            Mathf.RoundToInt(ScrollContentPadV),
            Mathf.RoundToInt(ScrollContentPadV));

        for (int row = 0; row < ValuablesVaultState.GridHeight; row++)
        {
            for (int col = 0; col < ValuablesVaultState.GridWidth; col++)
            {
                int cellIndex = ValuablesVaultState.CellIndexFromGrid(col, row);
                cells[cellIndex] = CreateCell(contentGo.transform, cellIndex, cellSize);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

        ScrollRect scroll = viewportGo.GetComponent<ScrollRect>();
        scroll.content = contentRt;
        scroll.viewport = viewportRt;
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        vaultScrollRect = scroll;
    }

    private void BuildDetailPanel(Transform bodyRow, float panelWidth)
    {
        GameObject detailPanel = new GameObject("ItemInfoPanel", typeof(RectTransform), typeof(Image));
        detailPanel.transform.SetParent(bodyRow, false);
        RectTransform detailRt = detailPanel.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(1f, 0f);
        detailRt.anchorMax = new Vector2(1f, 1f);
        detailRt.pivot = new Vector2(1f, 0.5f);
        detailRt.sizeDelta = new Vector2(panelWidth, 0f);
        detailRt.anchoredPosition = Vector2.zero;
        detailPanel.GetComponent<Image>().color = DetailPanelBg;

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(detailPanel.transform, false);
        RectTransform dividerRt = divider.GetComponent<RectTransform>();
        dividerRt.anchorMin = new Vector2(0f, 0f);
        dividerRt.anchorMax = new Vector2(0f, 1f);
        dividerRt.pivot = new Vector2(0f, 0.5f);
        dividerRt.sizeDelta = new Vector2(2f, 0f);
        dividerRt.anchoredPosition = Vector2.zero;
        divider.GetComponent<Image>().color = DetailDividerColor;

        float pad = DetailPanelInnerPad;

        GameObject sectionTitleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        sectionTitleObj.transform.SetParent(detailPanel.transform, false);
        RectTransform sectionTitleRt = sectionTitleObj.GetComponent<RectTransform>();
        sectionTitleRt.anchorMin = new Vector2(0f, 1f);
        sectionTitleRt.anchorMax = new Vector2(1f, 1f);
        sectionTitleRt.pivot = new Vector2(0.5f, 1f);
        sectionTitleRt.anchoredPosition = new Vector2(0f, -pad);
        sectionTitleRt.sizeDelta = new Vector2(-pad * 2f, 32f);
        TextMeshProUGUI sectionTitleTmp = sectionTitleObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(sectionTitleTmp);
        sectionTitleTmp.fontSize = 26f;
        sectionTitleTmp.fontStyle = FontStyles.Bold;
        sectionTitleTmp.alignment = TextAlignmentOptions.Center;
        sectionTitleTmp.color = DetailTitleColor;
        sectionTitleTmp.text = "物品資訊";
        sectionTitleTmp.raycastTarget = false;

        GameObject iconFrame = new GameObject("IconFrame", typeof(RectTransform), typeof(Image));
        iconFrame.transform.SetParent(detailPanel.transform, false);
        RectTransform iconFrameRt = iconFrame.GetComponent<RectTransform>();
        iconFrameRt.anchorMin = new Vector2(0.5f, 1f);
        iconFrameRt.anchorMax = new Vector2(0.5f, 1f);
        iconFrameRt.pivot = new Vector2(0.5f, 1f);
        iconFrameRt.anchoredPosition = new Vector2(0f, -(pad + 40f));
        iconFrameRt.sizeDelta = new Vector2(DetailIconSize + 16f, DetailIconSize + 16f);
        iconFrame.GetComponent<Image>().color = new Color(0.86f, 0.82f, 0.76f, 0.95f);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(iconFrame.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(DetailIconSize, DetailIconSize);
        detailIconImage = iconObj.GetComponent<Image>();
        detailIconImage.preserveAspect = true;
        detailIconImage.raycastTarget = false;
        detailIconImage.enabled = false;

        GameObject placeholderObj = new GameObject("IconPlaceholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(iconFrame.transform, false);
        RectTransform placeholderRt = placeholderObj.GetComponent<RectTransform>();
        StretchFull(placeholderRt);
        TextMeshProUGUI placeholderTmp = placeholderObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(placeholderTmp);
        placeholderTmp.fontSize = 22f;
        placeholderTmp.alignment = TextAlignmentOptions.Center;
        placeholderTmp.color = DetailMutedColor;
        placeholderTmp.text = ValuablesVaultUiCopy.IconPlaceholder;
        placeholderTmp.raycastTarget = false;
        detailIconPlaceholder = placeholderObj;

        float textTop = pad + 40f + DetailIconSize + 28f;

        GameObject nameObj = new GameObject("ItemName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(detailPanel.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -textTop);
        nameRt.sizeDelta = new Vector2(-pad * 2f, 40f);
        detailNameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(detailNameTmp);
        detailNameTmp.fontSize = 28f;
        detailNameTmp.fontStyle = FontStyles.Bold;
        detailNameTmp.alignment = TextAlignmentOptions.Center;
        detailNameTmp.enableWordWrapping = true;
        detailNameTmp.color = DetailBodyColor;

        GameObject bodyObj = new GameObject("ItemBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(detailPanel.transform, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(pad, pad + 212f);
        bodyRt.offsetMax = new Vector2(-pad, -(textTop + 48f));
        detailBodyTmp = bodyObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(detailBodyTmp);
        detailBodyTmp.fontSize = 20f;
        detailBodyTmp.alignment = TextAlignmentOptions.TopLeft;
        detailBodyTmp.enableWordWrapping = true;
        detailBodyTmp.lineSpacing = 4f;
        detailBodyTmp.color = DetailBodyColor;

        GameObject depositRootObj = new GameObject("DepositActions", typeof(RectTransform));
        depositRootObj.transform.SetParent(detailPanel.transform, false);
        detailDepositRoot = depositRootObj.GetComponent<RectTransform>();
        detailDepositRoot.anchorMin = new Vector2(0f, 0f);
        detailDepositRoot.anchorMax = new Vector2(1f, 0f);
        detailDepositRoot.pivot = new Vector2(0.5f, 0f);
        detailDepositRoot.anchoredPosition = new Vector2(0f, pad + 36f);
        detailDepositRoot.sizeDelta = new Vector2(-pad * 2f, 168f);

        GameObject slotObj = new GameObject("SlotLine", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotObj.transform.SetParent(detailPanel.transform, false);
        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        slotRt.anchorMin = new Vector2(0f, 0f);
        slotRt.anchorMax = new Vector2(1f, 0f);
        slotRt.pivot = new Vector2(0.5f, 0f);
        slotRt.anchoredPosition = new Vector2(0f, pad);
        slotRt.sizeDelta = new Vector2(-pad * 2f, 28f);
        detailSlotTmp = slotObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(detailSlotTmp);
        detailSlotTmp.fontSize = 18f;
        detailSlotTmp.alignment = TextAlignmentOptions.Center;
        detailSlotTmp.color = DetailMutedColor;
    }

    private static Vector2 ResolveViewportSquareCellSize(float viewportWidth)
    {
        int cols = ValuablesVaultState.GridWidth;
        float side = (viewportWidth - ScrollContentPadH * 2f - CellSpacing * (cols - 1)) / cols;
        side = Mathf.Max(64f, side);
        return new Vector2(side, side);
    }

    private CellUi CreateCell(Transform parent, int cellIndex, Vector2 cellSize)
    {
        GameObject go = new GameObject("Cell_" + cellIndex, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = cellSize;

        Image bg = go.GetComponent<Image>();
        bg.color = CellEmptyBg;
        bg.raycastTarget = true;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = CellBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.55f);
        iconRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        float iconSide = cellSize.x * 0.68f;
        iconRt.sizeDelta = new Vector2(iconSide, iconSide);
        Image icon = iconObj.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(go.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.05f, 0f);
        labelRt.anchorMax = new Vector2(0.95f, 0.42f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(label);
        label.fontSize = Mathf.Clamp(cellSize.y * 0.16f, 14f, 20f);
        label.alignment = TextAlignmentOptions.Bottom;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.color = new Color(0.22f, 0.18f, 0.14f, 1f);
        label.raycastTarget = false;

        Button button = go.GetComponent<Button>();
        int capturedIndex = cellIndex;
        button.onClick.AddListener(() => OnCellClicked(capturedIndex));

        return new CellUi
        {
            background = bg,
            icon = icon,
            label = label,
            button = button,
            outline = outline,
            cellIndex = cellIndex
        };
    }

    private void OnCellClicked(int cellIndex)
    {
        selectedCellIndex = cellIndex;
        RefreshCellSelectionVisuals();
        RefreshDetailPanel(cellIndex);
    }

    private void RefreshDetailPanel(int cellIndex)
    {
        if (detailNameTmp == null || detailBodyTmp == null || detailSlotTmp == null)
            return;

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        int definitionId = 0;
        int quantity = 0;
        if (cellIndex >= 0 && ValuablesVaultState.TryGetStack(slot, cellIndex, out ValuablesVaultState.VaultStack stack))
        {
            definitionId = stack.DefinitionId;
            quantity = stack.Quantity;
        }

        ValuablesVaultDisplay.InfoPanelCopy copy =
            ValuablesVaultDisplay.ResolveInfoPanel(cellIndex, definitionId, quantity);

        detailNameTmp.text = copy.TitleLine;
        detailBodyTmp.text = copy.Body;
        detailSlotTmp.text = cellIndex >= 0 ? copy.SlotLine : ValuablesVaultUiCopy.SelectSlotHint;

        Sprite sprite = copy.HasItem ? ValuablesVaultDisplay.ResolveIcon(definitionId) : null;
        if (detailIconImage != null)
        {
            detailIconImage.sprite = sprite;
            detailIconImage.enabled = sprite != null;
        }

        if (detailIconPlaceholder != null)
            detailIconPlaceholder.SetActive(sprite == null);

        RefreshDetailActions(cellIndex, definitionId, quantity, copy.HasItem);
    }

    private void RefreshDetailActions(int cellIndex, int cellDefinitionId, int cellQuantity, bool hasItem)
    {
        if (detailDepositRoot == null)
            return;

        for (int i = detailDepositRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(detailDepositRoot.GetChild(i).gameObject);

        if (cellIndex < 0)
            return;

        float y = 0f;
        if (hasItem && cellDefinitionId > 0 && cellQuantity > 0)
        {
            CreateDiscardButton(ValuablesVaultUiCopy.DiscardButtonLabel, y, cellIndex);
            y -= 50f;
        }

        RefreshDepositActions(cellIndex, cellDefinitionId, y);
    }

    private void RefreshDepositActions(int cellIndex, int cellDefinitionId, float startY)
    {
        if (detailDepositRoot == null)
            return;

        if (cellIndex < 0)
            return;

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (ValuablesVaultState.TryGetStack(slot, cellIndex, out ValuablesVaultState.VaultStack existing)
            && !existing.IsEmpty
            && !ValuablesVaultCatalog.IsCdFragmentDefinition(existing.DefinitionId))
            return;

        if (ValuablesVaultState.TryGetStack(slot, cellIndex, out existing)
            && !existing.IsEmpty
            && ValuablesVaultCatalog.IsCdFragmentDefinition(existing.DefinitionId))
        {
            cellDefinitionId = existing.DefinitionId;
        }

        List<string> walletIds = PlayerBirdDuelCdState.GetWalletFragmentCdIdsSorted(slot);
        if (walletIds.Count == 0)
            return;

        CreateDepositHintLabel(ValuablesVaultUiCopy.WalletFragmentsTitle, startY);

        float y = startY - 32f;
        for (int i = 0; i < walletIds.Count; i++)
        {
            string cdId = walletIds[i];
            int walletCount = PlayerBirdDuelCdState.GetFragments(slot, cdId);
            if (walletCount <= 0) continue;

            int fragmentDefId = ValuablesVaultCatalog.ResolveCdFragmentDefinitionId(cdId);
            if (fragmentDefId <= 0) continue;

            if (cellDefinitionId > 0 && cellDefinitionId != fragmentDefId)
                continue;

            string label = ValuablesVaultDisplay.ResolveCdFragmentWalletLabel(cdId, walletCount)
                + " · " + ValuablesVaultUiCopy.DepositAllButtonLabel;
            CreateDepositButton(label, y, cdId, cellIndex, walletCount);
            y -= 44f;
        }
    }

    private void CreateDepositHintLabel(string text, float y)
    {
        GameObject hintObj = new GameObject("DepositHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(detailDepositRoot, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 1f);
        hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f);
        hintRt.anchoredPosition = new Vector2(0f, y);
        hintRt.sizeDelta = new Vector2(0f, 28f);
        TextMeshProUGUI hintTmp = hintObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(hintTmp);
        hintTmp.fontSize = 18f;
        hintTmp.alignment = TextAlignmentOptions.MidlineLeft;
        hintTmp.color = DetailMutedColor;
        hintTmp.text = text;
        hintTmp.raycastTarget = false;
    }

    private void CreateDepositButton(string label, float y, string cdId, int cellIndex, int walletCount)
    {
        GameObject btnObj = new GameObject("Deposit_" + cdId, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(detailDepositRoot, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 1f);
        btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot = new Vector2(0.5f, 1f);
        btnRt.anchoredPosition = new Vector2(0f, y);
        btnRt.sizeDelta = new Vector2(0f, 38f);

        Image btnBg = btnObj.GetComponent<Image>();
        btnBg.color = new Color(0.78f, 0.86f, 0.72f, 0.98f);
        btnBg.raycastTarget = true;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        StretchFull(textRt);
        TextMeshProUGUI textTmp = textObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(textTmp);
        textTmp.fontSize = 17f;
        textTmp.alignment = TextAlignmentOptions.Center;
        textTmp.color = new Color(0.18f, 0.24f, 0.16f, 1f);
        textTmp.text = label;
        textTmp.raycastTarget = false;

        Button button = btnObj.GetComponent<Button>();
        button.targetGraphic = btnBg;
        string capturedCdId = cdId;
        int capturedCell = cellIndex;
        int capturedCount = walletCount;
        button.onClick.AddListener(() => OnDepositCdFragmentsClicked(capturedCell, capturedCdId, capturedCount));
    }

    private void CreateDiscardButton(string label, float y, int cellIndex)
    {
        GameObject btnObj = new GameObject("DiscardButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(detailDepositRoot, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 1f);
        btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot = new Vector2(0.5f, 1f);
        btnRt.anchoredPosition = new Vector2(0f, y);
        btnRt.sizeDelta = new Vector2(0f, 38f);

        Image btnBg = btnObj.GetComponent<Image>();
        btnBg.color = new Color(0.94f, 0.62f, 0.58f, 1f);
        btnBg.raycastTarget = true;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        StretchFull(textRt);
        TextMeshProUGUI textTmp = textObj.GetComponent<TextMeshProUGUI>();
        applyFont?.Invoke(textTmp);
        textTmp.fontSize = 17f;
        textTmp.fontStyle = FontStyles.Bold;
        textTmp.alignment = TextAlignmentOptions.Center;
        textTmp.enableWordWrapping = false;
        textTmp.color = new Color(0.34f, 0.14f, 0.12f, 1f);
        textTmp.text = label;
        textTmp.raycastTarget = false;

        Button button = btnObj.GetComponent<Button>();
        button.targetGraphic = btnBg;
        int capturedCell = cellIndex;
        button.onClick.AddListener(() => OnDiscardCellClicked(capturedCell));
    }

    private void OnDiscardCellClicked(int cellIndex)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        ValuablesVaultDiscard.Result result = ValuablesVaultDiscard.TryDiscardCell(slot, cellIndex);
        if (!result.Success)
        {
            SceneToast.Show(string.IsNullOrEmpty(result.ToastLine) ? "無法丟棄" : result.ToastLine);
            return;
        }

        PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
        RefreshAllCells();
        RefreshDetailPanel(cellIndex);
        RefreshCellSelectionVisuals();
        SceneToast.Show(result.ToastLine);
    }

    private void OnDepositCdFragmentsClicked(int cellIndex, string cdId, int quantity)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (!ValuablesVaultCatalog.TryDepositCdFragments(slot, cellIndex, cdId, quantity))
        {
            SceneToast.Show("無法放入此格");
            return;
        }

        RefreshAllCells();
        RefreshDetailPanel(cellIndex);
        RefreshCellSelectionVisuals();

        BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(cdId);
        string name = profile != null ? profile.DisplayName : cdId;
        SceneToast.Show(name + " 碎片已放入貴重品庫");
        PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
    }

    private void RefreshCellSelectionVisuals()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            CellUi cell = cells[i];
            if (cell == null || cell.outline == null)
                continue;

            bool selected = i == selectedCellIndex;
            cell.outline.effectColor = selected ? CellSelectedBorder : CellBorder;
            cell.outline.effectDistance = selected
                ? new Vector2(2f, -2f)
                : new Vector2(1.5f, -1.5f);
        }
    }

    private void RefreshAllCells()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        var map = ValuablesVaultState.LoadSlotMap(slot);

        for (int i = 0; i < cells.Length; i++)
        {
            CellUi cell = cells[i];
            if (cell == null)
                continue;

            if (!map.TryGetValue(i, out ValuablesVaultState.VaultStack stack) || stack.IsEmpty)
            {
                cell.background.color = CellEmptyBg;
                cell.icon.enabled = false;
                cell.icon.sprite = null;
                cell.label.text = string.Empty;
                continue;
            }

            cell.background.color = CellFilledBg;
            cell.label.text = ValuablesVaultDisplay.ResolveLabel(stack.DefinitionId, stack.Quantity);
            Sprite sprite = ValuablesVaultDisplay.ResolveIcon(stack.DefinitionId);
            if (sprite != null)
            {
                cell.icon.sprite = sprite;
                cell.icon.enabled = true;
            }
            else
            {
                cell.icon.enabled = false;
                cell.icon.sprite = null;
            }
        }
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

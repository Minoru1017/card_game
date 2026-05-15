using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>在 Parameter details 捲動區建立四欄 UGUI 對照表。</summary>
public static class SettingsParameterComparisonTable
{
    private const string TableRootName = "comparison table";
    private const string RowName = "row";

    private const float RowMinHeight = 52f;
    private const float HeaderFontSize = 24f;
    private const float BodyFontSize = 22f;
    private const float HPad = 8f;
    private const float VPad = 6f;

    private static readonly float[] ColumnFlex = { 0.22f, 0.32f, 0.23f, 0.23f };

    private static readonly Color HeaderRowBg = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color SelectedColumnBg = new Color(0.18f, 0.42f, 0.82f, 0.42f);
    private static readonly Color SelectedHeaderColumnBg = new Color(0.22f, 0.48f, 0.9f, 0.55f);

    public static int ResolveHighlightColumn(string presetId)
    {
        if (string.Equals(presetId, BattleCardTuningPresetLibrary.ControlGroupId, System.StringComparison.OrdinalIgnoreCase))
            return 3;
        return 2;
    }

    public static void Refresh(
        RectTransform viewport,
        IReadOnlyList<ParameterComparisonRow> rows,
        string selectedPresetId = null)
    {
        if (viewport == null) return;

        int highlightColumn = string.IsNullOrWhiteSpace(selectedPresetId)
            ? -1
            : ResolveHighlightColumn(selectedPresetId);

        RectTransform tableRoot = FindOrCreateTableRoot(viewport);
        ClearChildren(tableRoot);

        if (rows == null || rows.Count == 0)
        {
            CreateRow(tableRoot, new ParameterComparisonRow("", "無預設資料", "", "", false), false, highlightColumn);
        }
        else
        {
            for (int i = 0; i < rows.Count; i++)
                CreateRow(tableRoot, rows[i], rows[i].isHeader, highlightColumn);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tableRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
    }

    public static RectTransform FindTableRoot(Transform viewport)
    {
        if (viewport == null) return null;
        Transform t = viewport.Find(TableRootName);
        return t as RectTransform;
    }

    private static RectTransform FindOrCreateTableRoot(RectTransform viewport)
    {
        RectTransform existing = FindTableRoot(viewport);
        if (existing != null) return existing;

        GameObject rootGo = new GameObject(TableRootName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        rootGo.transform.SetParent(viewport, false);

        RectTransform rt = rootGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = rootGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(4, 4, 8, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = rootGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rt;
    }

    private static void CreateRow(
        RectTransform tableRoot,
        ParameterComparisonRow row,
        bool isHeader,
        int highlightColumn)
    {
        GameObject rowGo = new GameObject(RowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGo.transform.SetParent(tableRoot, false);

        LayoutElement rowLayout = rowGo.GetComponent<LayoutElement>();
        rowLayout.minHeight = RowMinHeight;
        rowLayout.preferredHeight = RowMinHeight;

        HorizontalLayoutGroup hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        if (isHeader)
        {
            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.color = HeaderRowBg;
            rowBg.raycastTarget = false;
        }

        string[] texts = { row.category, row.parameter, row.preset1, row.control };
        for (int col = 0; col < 4; col++)
        {
            bool isValueCol = col >= 2;
            TextAlignmentOptions align = isHeader || isValueCol
                ? TextAlignmentOptions.Center
                : TextAlignmentOptions.MidlineLeft;
            bool highlight = col == highlightColumn;
            CreateCell(rowGo.transform, texts[col], ColumnFlex[col], isHeader, align, highlight);
        }
    }

    private static void CreateCell(
        Transform rowParent,
        string text,
        float flexWeight,
        bool isHeader,
        TextAlignmentOptions alignment,
        bool highlight)
    {
        GameObject cellGo = new GameObject("cell", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        cellGo.transform.SetParent(rowParent, false);

        Image cellBg = cellGo.GetComponent<Image>();
        cellBg.raycastTarget = false;
        if (highlight)
            cellBg.color = isHeader ? SelectedHeaderColumnBg : SelectedColumnBg;
        else
            cellBg.color = Color.clear;

        LayoutElement le = cellGo.GetComponent<LayoutElement>();
        le.flexibleWidth = Mathf.Max(0.01f, flexWeight);
        le.minWidth = 48f;

        GameObject labelGo = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(cellGo.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(HPad, VPad);
        labelRt.offsetMax = new Vector2(-HPad, -VPad);

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = text ?? "";
        tmp.fontSize = isHeader ? HeaderFontSize : BodyFontSize;
        tmp.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = highlight
            ? new Color(1f, 1f, 1f, 1f)
            : (isHeader ? new Color(0.95f, 0.97f, 1f) : new Color(0.88f, 0.9f, 0.94f));
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(tmp);
    }

    private static void ClearChildren(RectTransform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
                Object.Destroy(child.gameObject);
        }
    }
}

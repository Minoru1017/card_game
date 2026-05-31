using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private const int BattleLiveHistoryMaxLines = 7;
    private const float BattleLiveHistoryToggleWidth = 300f;
    private const float BattleLiveHistoryExpandedWidth = 440f;
    private const float BattleLiveHistoryToggleHeight = 36f;
    private const float BattleLiveHistoryLineHeight = 38f;
    private const float BattleLiveHistoryLineSpacing = 6f;
    private const float BattleLiveHistoryBodyPaddingV = 12f;
    private const float BattleLiveHistoryBodyPaddingH = 14f;
    private const int BattleLiveHistoryTrimChars = 40;

    private GameObject battleLiveHistoryRoot;
    private GameObject battleLiveHistoryBody;
    private Button battleLiveHistoryToggleButton;
    private TextMeshProUGUI battleLiveHistoryToggleLabel;
    private RectTransform battleLiveHistoryContentRt;
    private bool battleLiveHistoryExpanded;
    private bool battleHistoryUiBound;

    private void BindBattleHistoryUi()
    {
        if (battleHistoryUiBound || battleManager == null) return;
        battleManager.BattleHistoryChanged += OnBattleHistoryChanged;
        battleHistoryUiBound = true;
        RefreshBattleLiveHistoryFeed();
    }

    private void UnbindBattleHistoryUi()
    {
        if (!battleHistoryUiBound || battleManager == null) return;
        battleManager.BattleHistoryChanged -= OnBattleHistoryChanged;
        battleHistoryUiBound = false;
    }

    private void OnBattleHistoryChanged() => RefreshBattleLiveHistoryFeed();

    private void OnBattleLiveHistoryToggleClicked()
    {
        battleLiveHistoryExpanded = !battleLiveHistoryExpanded;
        ApplyBattleLiveHistoryPanelLayout();
        if (battleLiveHistoryExpanded)
            RebuildBattleLiveHistoryLineRows();
        UpdateBattleLiveHistoryToggleLabel();
    }

    private void EnsureBattleLiveHistoryFeed()
    {
        if (battleLiveHistoryRoot != null || uiRoot == null) return;

        battleLiveHistoryRoot = new GameObject("BattleLiveHistoryFeed", typeof(RectTransform));
        battleLiveHistoryRoot.transform.SetParent(uiRoot, false);
        RectTransform rootRt = battleLiveHistoryRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0.5f);
        rootRt.anchorMax = new Vector2(1f, 0.5f);
        rootRt.pivot = new Vector2(1f, 0.5f);
        rootRt.anchoredPosition = new Vector2(-14f, 40f);
        rootRt.sizeDelta = new Vector2(BattleLiveHistoryToggleWidth, BattleLiveHistoryToggleHeight);

        GameObject toggleObj = new GameObject("ToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
        toggleObj.transform.SetParent(battleLiveHistoryRoot.transform, false);
        RectTransform toggleRt = toggleObj.GetComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(0f, 1f);
        toggleRt.anchorMax = new Vector2(1f, 1f);
        toggleRt.pivot = new Vector2(0.5f, 1f);
        toggleRt.anchoredPosition = Vector2.zero;
        toggleRt.sizeDelta = new Vector2(0f, BattleLiveHistoryToggleHeight);
        Image toggleBg = toggleObj.GetComponent<Image>();
        toggleBg.sprite = GetUnitWhiteSprite();
        toggleBg.type = Image.Type.Simple;
        toggleBg.color = BattleUiColors.PanelCream96;
        battleLiveHistoryToggleButton = toggleObj.GetComponent<Button>();
        BattleUiColors.ApplyHallWineButton(battleLiveHistoryToggleButton);
        battleLiveHistoryToggleButton.onClick.AddListener(OnBattleLiveHistoryToggleClicked);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(toggleObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(10f, 0f);
        labelRt.offsetMax = new Vector2(-10f, 0f);
        battleLiveHistoryToggleLabel = labelObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) battleLiveHistoryToggleLabel.font = sharedUIFont;
        battleLiveHistoryToggleLabel.fontSize = 18f;
        battleLiveHistoryToggleLabel.fontStyle = FontStyles.Bold;
        battleLiveHistoryToggleLabel.alignment = TextAlignmentOptions.Center;
        battleLiveHistoryToggleLabel.color = BattleUiColors.BtnPrimaryText;
        battleLiveHistoryToggleLabel.raycastTarget = false;

        battleLiveHistoryBody = new GameObject("Body", typeof(RectTransform), typeof(Image), typeof(Outline));
        battleLiveHistoryBody.transform.SetParent(battleLiveHistoryRoot.transform, false);
        RectTransform bodyRt = battleLiveHistoryBody.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.anchoredPosition = new Vector2(0f, -BattleLiveHistoryToggleHeight);
        bodyRt.sizeDelta = new Vector2(0f, GetBattleLiveHistoryBodyHeight());
        Image bodyBg = battleLiveHistoryBody.GetComponent<Image>();
        bodyBg.sprite = GetUnitWhiteSprite();
        bodyBg.type = Image.Type.Simple;
        bodyBg.color = BattleUiColors.WithAlpha(BattleUiColors.PanelCream96, 0.92f);
        bodyBg.raycastTarget = false;
        Outline bodyOutline = battleLiveHistoryBody.GetComponent<Outline>();
        bodyOutline.effectColor = BattleUiColors.PanelEdge35;
        bodyOutline.effectDistance = new Vector2(2f, -2f);

        GameObject contentObj = new GameObject("Lines", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(battleLiveHistoryBody.transform, false);
        battleLiveHistoryContentRt = contentObj.GetComponent<RectTransform>();
        battleLiveHistoryContentRt.anchorMin = Vector2.zero;
        battleLiveHistoryContentRt.anchorMax = Vector2.one;
        battleLiveHistoryContentRt.offsetMin = new Vector2(BattleLiveHistoryBodyPaddingH, BattleLiveHistoryBodyPaddingV);
        battleLiveHistoryContentRt.offsetMax = new Vector2(-BattleLiveHistoryBodyPaddingH, -BattleLiveHistoryBodyPaddingV);
        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = BattleLiveHistoryLineSpacing;

        battleLiveHistoryExpanded = false;
        battleLiveHistoryBody.SetActive(false);
        UpdateBattleLiveHistoryToggleLabel();
        ApplyBattleLiveHistoryPanelLayout();
        battleLiveHistoryRoot.SetActive(false);
    }

    private float GetBattleLiveHistoryBodyHeight() =>
        BattleLiveHistoryBodyPaddingV * 2f
        + BattleLiveHistoryMaxLines * BattleLiveHistoryLineHeight
        + Mathf.Max(0, BattleLiveHistoryMaxLines - 1) * BattleLiveHistoryLineSpacing;

    private void ApplyBattleLiveHistoryPanelLayout()
    {
        if (battleLiveHistoryRoot == null) return;

        RectTransform rootRt = battleLiveHistoryRoot.GetComponent<RectTransform>();
        float rootH = BattleLiveHistoryToggleHeight;
        if (battleLiveHistoryExpanded && battleLiveHistoryBody != null)
            rootH += GetBattleLiveHistoryBodyHeight();
        float panelWidth = battleLiveHistoryExpanded ? BattleLiveHistoryExpandedWidth : BattleLiveHistoryToggleWidth;
        rootRt.sizeDelta = new Vector2(panelWidth, rootH);

        if (battleLiveHistoryBody != null)
        {
            battleLiveHistoryBody.SetActive(battleLiveHistoryExpanded);
            if (battleLiveHistoryExpanded)
            {
                RectTransform bodyRt = battleLiveHistoryBody.GetComponent<RectTransform>();
                bodyRt.anchoredPosition = new Vector2(0f, -BattleLiveHistoryToggleHeight);
                bodyRt.sizeDelta = new Vector2(0f, GetBattleLiveHistoryBodyHeight());
            }
        }
    }

    private void UpdateBattleLiveHistoryToggleLabel()
    {
        if (battleLiveHistoryToggleLabel == null) return;
        int count = battleManager != null ? battleManager.BattleHistoryEntries.Count : 0;
        string arrow = battleLiveHistoryExpanded ? "▲" : "▼";
        battleLiveHistoryToggleLabel.text = count > 0
            ? "最近戰況 " + arrow + " (" + count + ")"
            : "最近戰況 " + arrow;
    }

    private void RefreshBattleLiveHistoryFeed()
    {
        EnsureBattleLiveHistoryFeed();
        if (battleLiveHistoryRoot == null || battleManager == null) return;

        bool show = !BattleAutoSimPlugin.IsRunning
            && !battleManager.IsBattleOver()
            && battleManager.BattleHistoryEntries.Count > 0;
        battleLiveHistoryRoot.SetActive(show);
        if (!show) return;

        UpdateBattleLiveHistoryToggleLabel();
        ApplyBattleLiveHistoryPanelLayout();
        if (battleLiveHistoryExpanded)
            RebuildBattleLiveHistoryLineRows();
    }

    private void RebuildBattleLiveHistoryLineRows()
    {
        if (battleLiveHistoryContentRt == null || battleManager == null) return;

        for (int i = battleLiveHistoryContentRt.childCount - 1; i >= 0; i--)
            Destroy(battleLiveHistoryContentRt.GetChild(i).gameObject);

        List<BattleHistoryEntry> recent = battleManager.GetRecentBattleHistoryEntries(BattleLiveHistoryMaxLines);
        for (int i = 0; i < recent.Count; i++)
            CreateBattleLiveHistoryLineRow(recent[i], i == 0);
    }

    private void CreateBattleLiveHistoryLineRow(BattleHistoryEntry entry, bool isNewest)
    {
        GameObject rowObj = new GameObject("LiveLine", typeof(RectTransform), typeof(LayoutElement));
        rowObj.transform.SetParent(battleLiveHistoryContentRt, false);
        LayoutElement le = rowObj.GetComponent<LayoutElement>();
        le.preferredHeight = BattleLiveHistoryLineHeight;
        le.minHeight = BattleLiveHistoryLineHeight;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(rowObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(isNewest ? 6f : 0f, 0f);
        txtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) tmp.font = sharedUIFont;
        tmp.fontSize = isNewest ? 20f : 18f;
        tmp.fontStyle = isNewest ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = isNewest ? BattleUiColors.Ink : BattleUiColors.InkSoft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.maxVisibleLines = 1;
        tmp.richText = true;
        tmp.text = FormatBattleHistoryRichText(TrimLiveHistoryLine(entry.Text));
        tmp.raycastTarget = false;
    }

    private static string TrimLiveHistoryLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string t = text.Trim();
        if (t.Length <= BattleLiveHistoryTrimChars) return t;
        return t.Substring(0, BattleLiveHistoryTrimChars - 1) + "…";
    }
}

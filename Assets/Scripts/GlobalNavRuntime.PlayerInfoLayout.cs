using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GlobalNavRuntime : MonoBehaviour
{
    private void EnsurePlayerInfoOverlay()
    {
        if (playerInfoOverlayRoot != null && builtPlayerInfoLayoutVersion == PlayerInfoOverlayLayoutVersion)
            return;

        DestroyPlayerInfoOverlayIfAny();
        if (view == null || view.rootCanvas == null) return;

        GameObject root = new GameObject("GlobalPlayerInfoOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(view.rootCanvas.transform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        Image dim = root.GetComponent<Image>();
        dim.color = new Color(0.1f, 0.08f, 0.06f, 0.72f);
        dim.raycastTarget = true;

        float panelWidth = Mathf.Min(Screen.width * 0.8f, 1160f);
        float panelHeight = Mathf.Min(Screen.height * 0.82f, 780f);

        GameObject panel = new GameObject("ProfilePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.94f, 0.9f, 0.84f, 0.99f);
        panelBg.raycastTarget = true;

        GameObject header = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.offsetMin = new Vector2(0f, -PlayerInfoHeaderHeight);
        headerRt.offsetMax = new Vector2(0f, 0f);
        Image headerBg = header.GetComponent<Image>();
        headerBg.color = new Color(0.85f, 0.79f, 0.68f, 0.9f);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(header.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(32f, -18f);
        titleRt.sizeDelta = new Vector2(420f, 48f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(titleTmp);
        titleTmp.fontSize = 40f;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = new Color(0.25f, 0.2f, 0.15f, 1f);
        titleTmp.text = "玩家資訊";

        GameObject closeBtnObj = CreatePlayerInfoStyleCloseButton(header.transform);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(() =>
        {
            if (playerInfoOverlayRoot != null) playerInfoOverlayRoot.SetActive(false);
        });

        GameObject footer = new GameObject("FooterBar", typeof(RectTransform), typeof(Image));
        footer.transform.SetParent(panel.transform, false);
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = Vector2.zero;
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.offsetMin = Vector2.zero;
        footerRt.offsetMax = new Vector2(0f, PlayerInfoFooterHeight);
        footer.GetComponent<Image>().color = new Color(0.88f, 0.82f, 0.74f, 0.92f);

        GameObject resetBtnObj = CreateButton(
            footer.transform,
            "ResetButton",
            "重置資料",
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-PlayerInfoPadH, 0f),
            new Vector2(148f, 44f),
            new Color(0.6f, 0.24f, 0.22f, 0.96f),
            22f);
        Button resetBtn = resetBtnObj.GetComponent<Button>();
        resetBtn.onClick.RemoveAllListeners();
        resetBtn.onClick.AddListener(() =>
        {
            PlayerProfileCsvService.ResetPlayerProgressLikeBackpack();
            PlayerProfileCsvService.SetRole("遊戲測試員");
            RefreshPlayerInfoOverlayContent();
        });

        Transform scrollContent = CreatePlayerInfoScrollArea(panel.transform, panelWidth, PlayerInfoHeaderHeight, PlayerInfoFooterHeight);
        playerInfoContentWidth = panelWidth - PlayerInfoPadH * 2f;
        playerInfoLayoutY = -8f;

        const float profileTwoLineRowH = 58f;

        Transform basicBody = CreatePlayerInfoSection(scrollContent, "基本資料", 328f);
        float rowY = -14f;
        CreatePlayerInfoSlotNameRow(basicBody, ref rowY);
        playerInfoUuidText = PlaceProfileField(basicBody, "UUID", "UuidText", ref rowY, profileTwoLineRowH, 19f);
        playerInfoRoleText = PlaceProfileField(basicBody, "玩家身份", "RoleText", ref rowY, profileTwoLineRowH, 19f);
        playerInfoStartDateText = PlaceProfileField(basicBody, "開始遊玩", "StartDateText", ref rowY, profileTwoLineRowH, 19f);

        const int progressLineCount = 7;
        const float progressLineHeight = 28f;
        float progressBlockRowH = 22f + 4f + progressLineCount * progressLineHeight;
        Transform progressBody = CreatePlayerInfoSection(scrollContent, PlayerInfoProgressCopy.SectionTitle, 52f + progressBlockRowH + 16f);
        rowY = -14f;
        playerInfoProgressText = PlaceProfileField(
            progressBody,
            "主線章節",
            "StoryProgressText",
            ref rowY,
            progressBlockRowH,
            19f,
            wrapValue: true,
            valueLineSpacing: 4f);

        int deckLineCount = 5;
        PlayerData layoutPlayerData = PlayerData.ResolveCanonical();
        if (layoutPlayerData != null && layoutPlayerData.deckSlotCount > 0)
            deckLineCount = layoutPlayerData.deckSlotCount;
        const float profileDeckLineHeight = 30f;
        float profileDeckBlockRowH = 22f + 4f + deckLineCount * profileDeckLineHeight;
        float profileAssetSectionH = 52f + profileTwoLineRowH + PlayerInfoLineGap + profileDeckBlockRowH +
                                     PlayerInfoLineGap + profileTwoLineRowH + 24f;

        Transform assetBody = CreatePlayerInfoSection(scrollContent, "資產與收藏", profileAssetSectionH);
        rowY = -14f;
        playerInfoCoinsText = PlaceProfileField(assetBody, "金幣", "CoinsText", ref rowY, profileTwoLineRowH, 21f);
        playerInfoDeckSummaryText = PlaceProfileField(assetBody, "牌組", "DeckSummaryText", ref rowY, profileDeckBlockRowH, 19f, wrapValue: true, valueLineSpacing: 6f);
        playerInfoHeroSummaryText = PlaceProfileField(assetBody, "英雄", "HeroSummaryText", ref rowY, profileTwoLineRowH, 19f);

        Transform recordBody = CreatePlayerInfoSection(scrollContent, "對戰紀錄", 340f);
        rowY = -12f;
        playerInfoLastResultText = PlaceProfileField(recordBody, "最近結果", "LastResultText", ref rowY, profileTwoLineRowH, 19f);
        BuildPlayerInfoRecordPanel(recordBody, ref rowY);

        FinalizePlayerInfoScrollContent();

        playerInfoOverlayRoot = root;
        playerInfoOverlayRoot.SetActive(false);
        builtPlayerInfoLayoutVersion = PlayerInfoOverlayLayoutVersion;
    }

    private void DestroyPlayerInfoOverlayIfAny()
    {
        if (playerInfoOverlayRoot != null)
            Destroy(playerInfoOverlayRoot);

        playerInfoOverlayRoot = null;
        playerInfoUuidText = null;
        playerInfoRoleText = null;
        playerInfoStartDateText = null;
        playerInfoCoinsText = null;
        playerInfoDeckSummaryText = null;
        playerInfoHeroSummaryText = null;
        playerInfoLastResultText = null;
        playerInfoProgressText = null;
        playerInfoRecordTotalText = null;
        playerInfoScrollContentRt = null;
        playerSlotNameInput = null;
        playerInfoRecordColumns = null;
        builtPlayerInfoLayoutVersion = 0;
    }

    private Transform CreatePlayerInfoScrollArea(Transform panel, float panelWidth, float headerHeight, float footerHeight)
    {
        GameObject scrollRoot = new GameObject("ProfileScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollRoot.transform.SetParent(panel, false);
        RectTransform scrollRt = scrollRoot.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(PlayerInfoPadH, footerHeight + 8f);
        scrollRt.offsetMax = new Vector2(-PlayerInfoPadH, -headerHeight - 4f);
        scrollRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollRoot.transform, false);
        RectTransform viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        playerInfoScrollContentRt = content.GetComponent<RectTransform>();
        playerInfoScrollContentRt.anchorMin = new Vector2(0f, 1f);
        playerInfoScrollContentRt.anchorMax = new Vector2(1f, 1f);
        playerInfoScrollContentRt.pivot = new Vector2(0.5f, 1f);
        playerInfoScrollContentRt.anchoredPosition = Vector2.zero;
        playerInfoScrollContentRt.sizeDelta = new Vector2(0f, 900f);

        ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = playerInfoScrollContentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        return content.transform;
    }

    private void FinalizePlayerInfoScrollContent()
    {
        if (playerInfoScrollContentRt == null) return;
        float totalHeight = Mathf.Max(480f, -playerInfoLayoutY + 40f);
        playerInfoScrollContentRt.sizeDelta = new Vector2(0f, totalHeight);
    }

    private Transform CreatePlayerInfoSection(Transform contentRoot, string title, float sectionHeight)
    {
        float sectionWidth = playerInfoContentWidth;
        GameObject section = new GameObject(title + "Section", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(contentRoot, false);
        RectTransform sectionRt = section.GetComponent<RectTransform>();
        sectionRt.anchorMin = new Vector2(0f, 1f);
        sectionRt.anchorMax = new Vector2(1f, 1f);
        sectionRt.pivot = new Vector2(0.5f, 1f);
        sectionRt.anchoredPosition = new Vector2(0f, playerInfoLayoutY);
        sectionRt.sizeDelta = new Vector2(0f, sectionHeight);
        section.GetComponent<Image>().color = PlayerInfoSectionBg;

        GameObject titleObj = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(section.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(16f, -10f);
        titleRt.sizeDelta = new Vector2(sectionWidth - 32f, 30f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(titleTmp);
        titleTmp.text = title;
        titleTmp.fontSize = 22f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = PlayerInfoSectionTitle;
        titleTmp.alignment = TextAlignmentOptions.Left;

        GameObject body = new GameObject("Body", typeof(RectTransform), typeof(RectMask2D));
        body.transform.SetParent(section.transform, false);
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(16f, 12f);
        bodyRt.offsetMax = new Vector2(-16f, -40f);

        playerInfoLayoutY -= sectionHeight + PlayerInfoSectionGap;
        return body.transform;
    }

    private TextMeshProUGUI PlaceProfileField(
        Transform parent,
        string label,
        string valueObjectName,
        ref float rowY,
        float rowHeight,
        float valueFontSize,
        bool wrapValue = false,
        float valueLineSpacing = 2f)
    {
        const float labelHeight = 22f;
        float valueHeight = Mathf.Max(26f, rowHeight - labelHeight - 4f);

        CreateProfileTextLine(parent, valueObjectName + "_Label", ref rowY, labelHeight, 17f, PlayerInfoTextMuted, label, false, false, 0f);
        TextMeshProUGUI valueTmp = CreateProfileTextLine(
            parent,
            valueObjectName,
            ref rowY,
            valueHeight,
            valueFontSize,
            PlayerInfoTextPrimary,
            string.Empty,
            wrapValue,
            false,
            valueLineSpacing);
        rowY -= PlayerInfoLineGap;
        return valueTmp;
    }

    private TextMeshProUGUI CreateProfileTextLine(
        Transform parent,
        string name,
        ref float rowY,
        float lineHeight,
        float fontSize,
        Color color,
        string text,
        bool wrap,
        bool richText,
        float lineSpacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, rowY);
        rt.sizeDelta = new Vector2(0f, lineHeight);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(tmp);
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.richText = richText;
        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.lineSpacing = lineSpacing;
        tmp.paragraphSpacing = wrap ? 4f : 0f;
        tmp.text = text;
        rowY -= lineHeight;
        return tmp;
    }

    private void CreatePlayerInfoSlotNameRow(Transform parent, ref float rowY)
    {
        float rowHeight = 48f;
        GameObject row = new GameObject("SlotNameRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, rowY);
        rowRt.sizeDelta = new Vector2(0f, rowHeight);

        GameObject slotNameLabel = new GameObject("SlotNameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotNameLabel.transform.SetParent(row.transform, false);
        RectTransform slotNameLabelRt = slotNameLabel.GetComponent<RectTransform>();
        slotNameLabelRt.anchorMin = new Vector2(0f, 1f);
        slotNameLabelRt.anchorMax = new Vector2(0f, 1f);
        slotNameLabelRt.pivot = new Vector2(0f, 1f);
        slotNameLabelRt.anchoredPosition = Vector2.zero;
        slotNameLabelRt.sizeDelta = new Vector2(88f, rowHeight);
        TextMeshProUGUI slotNameLabelTmp = slotNameLabel.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(slotNameLabelTmp);
        slotNameLabelTmp.fontSize = 17f;
        slotNameLabelTmp.alignment = TextAlignmentOptions.Left;
        slotNameLabelTmp.color = PlayerInfoTextMuted;
        slotNameLabelTmp.text = "槽位名稱";

        GameObject inputBgObj = new GameObject("SlotNameInputBg", typeof(RectTransform), typeof(Image));
        inputBgObj.transform.SetParent(row.transform, false);
        RectTransform inputBgRt = inputBgObj.GetComponent<RectTransform>();
        inputBgRt.anchorMin = new Vector2(0f, 1f);
        inputBgRt.anchorMax = new Vector2(1f, 1f);
        inputBgRt.pivot = new Vector2(0f, 1f);
        inputBgRt.anchoredPosition = new Vector2(96f, 0f);
        inputBgRt.sizeDelta = new Vector2(-280f, rowHeight);
        Image inputBg = inputBgObj.GetComponent<Image>();
        inputBg.color = Color.white;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(inputBgObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(10f, 6f);
        viewportRt.offsetMax = new Vector2(-10f, -6f);

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(viewportObj.transform, false);
        RectTransform phRt = placeholderObj.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(placeholder);
        placeholder.fontSize = 20f;
        placeholder.color = new Color(0.55f, 0.5f, 0.45f, 0.8f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.richText = false;
        placeholder.text = "輸入名稱";

        GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inputTextObj.transform.SetParent(viewportObj.transform, false);
        RectTransform inputTextRt = inputTextObj.GetComponent<RectTransform>();
        inputTextRt.anchorMin = Vector2.zero;
        inputTextRt.anchorMax = Vector2.one;
        inputTextRt.offsetMin = Vector2.zero;
        inputTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI inputText = inputTextObj.GetComponent<TextMeshProUGUI>();
        ApplyPlayerInfoFont(inputText);
        inputText.fontSize = 20f;
        inputText.color = PlayerInfoTextPrimary;
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.richText = false;
        inputText.overflowMode = TextOverflowModes.Overflow;
        inputText.enableWordWrapping = false;

        playerSlotNameInput = inputBgObj.AddComponent<TmpInputFieldImeRedraw>();
        playerSlotNameInput.textViewport = viewportRt;
        playerSlotNameInput.textComponent = inputText;
        playerSlotNameInput.placeholder = placeholder;
        playerSlotNameInput.characterLimit = 24;
        playerSlotNameInput.characterValidation = TMP_InputField.CharacterValidation.None;
        playerSlotNameInput.richText = false;

        GameObject saveNameBtnObj = CreateButton(
            row.transform,
            "SaveSlotNameButton",
            "儲存",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(120f, rowHeight),
            new Color(0.24f, 0.47f, 0.32f, 0.96f),
            20f);
        Button saveNameBtn = saveNameBtnObj.GetComponent<Button>();
        saveNameBtn.onClick.RemoveAllListeners();
        saveNameBtn.onClick.AddListener(() =>
        {
            string newName = playerSlotNameInput != null ? playerSlotNameInput.text : string.Empty;
            PlayerData.SetActivePlayerSlotName(newName);
            RefreshPlayerInfoOverlayContent();
            SceneToast.Show("玩家名稱已儲存");
        });

        rowY -= rowHeight + PlayerInfoLineGap;
    }
}

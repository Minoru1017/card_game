using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>戰前 CD 光碟選擇（程式化 overlay，手機優先版面 + 驚奇感色票）。</summary>
public static class BirdDuelCdSelectOverlayUi
{
    private const float PickerBarHeight = 88f;
    private const float HeaderHeight = 56f;
    private static float FooterHeight => BirdDuelMobileOverlayLayout.FooterBarHeight;
    private static float SwitchRowHeight => BirdDuelMobileOverlayLayout.SwitchRowHeight;
    private const float ContentPadH = 20f;

    private static GameObject activeOverlay;

    public static void Show(
        Canvas parentCanvas,
        TMP_FontAsset font,
        Action<string> onConfirm,
        Action onCancel = null)
    {
        Close();
        if (parentCanvas == null)
        {
            onConfirm?.Invoke(BirdDuelCdCatalog.DefaultCdId);
            return;
        }

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        List<string> ownedIds = PlayerBirdDuelCdState.GetOwnedCdIdsSorted(slot);
        string initialId = ownedIds.Count > 0 ? ownedIds[0] : BirdDuelCdCatalog.DefaultCdId;

        var ui = new OverlayUi { Font = font, SelectedCdId = initialId };

        GameObject overlay = new GameObject(
            "BirdDuelCdSelectOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(parentCanvas.transform, false);
        StretchFull(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = BirdDuelUiColors.Dim;

        Canvas overlayCanvas = overlay.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 5100;
        overlay.AddComponent<GraphicRaycaster>();

        GameObject panel = CreatePanel(overlay.transform);

        ui.PickerBar = BuildPickerBar(panel.transform, font, ownedIds, ui);
        BuildHeader(panel.transform, font, ui);

        ui.BodyRt = BuildMainContent(panel.transform, font, ui);
        ui.SwitchRow = BuildSwitchRow(panel.transform, font, ui);
        ui.SetPickerVisible(false);

        BuildFooter(panel.transform, font, () =>
        {
            Close();
            onCancel?.Invoke();
        }, () =>
        {
            Close();
            onConfirm?.Invoke(ui.SelectedCdId ?? BirdDuelCdCatalog.DefaultCdId);
        });

        if (ui.SwitchRow != null)
            ui.SwitchRow.transform.SetAsLastSibling();

        ui.Refresh();
        activeOverlay = overlay;
        overlay.transform.SetAsLastSibling();
    }

    public static void Close()
    {
        if (activeOverlay != null)
        {
            UnityEngine.Object.Destroy(activeOverlay);
            activeOverlay = null;
        }
    }

    private sealed class OverlayUi
    {
        public TMP_FontAsset Font;
        public string SelectedCdId;
        public GameObject PickerBar;
        public GameObject SwitchRow;
        public RectTransform HeaderRt;
        public RectTransform BodyRt;
        public Image HeroIcon;
        public TextMeshProUGUI NameTmp;
        public TextMeshProUGUI MetaTmp;
        public TextMeshProUGUI DescTmp;
        public readonly List<string> SlotCdIds = new List<string>();
        public readonly List<Image> SlotBackgrounds = new List<Image>();

        public void Select(string cdId)
        {
            if (string.IsNullOrWhiteSpace(cdId)) return;
            SelectedCdId = cdId.Trim();
            Refresh();
        }

        public void SetPickerVisible(bool visible)
        {
            if (PickerBar != null)
                PickerBar.SetActive(visible);
            ApplyBodyLayout(visible);
        }

        private void ApplyBodyLayout(bool pickerVisible)
        {
            float pickerH = pickerVisible ? PickerBarHeight : 0f;
            if (HeaderRt != null)
                HeaderRt.anchoredPosition = new Vector2(0f, -pickerH);

            if (BodyRt == null) return;
            float switchH = SwitchRow != null && SwitchRow.activeSelf ? SwitchRowHeight + 8f : 0f;
            float top = pickerH + HeaderHeight + 12f;
            float bottom = FooterHeight + switchH + 16f;
            BodyRt.offsetMin = new Vector2(ContentPadH, bottom);
            BodyRt.offsetMax = new Vector2(-ContentPadH, -top);
        }

        public void Refresh()
        {
            BirdDuelCdProfile profile = BirdDuelCdCatalog.Get(SelectedCdId);
            string name = profile != null ? profile.DisplayName : SelectedCdId;
            if (NameTmp != null) NameTmp.text = name;
            if (MetaTmp != null)
            {
                MetaTmp.text = profile != null
                    ? RarityLabel(profile.Rarity) + " · " + FactionLabel(profile.Faction)
                    : string.Empty;
            }

            if (DescTmp != null)
            {
                DescTmp.text = BuildDescription(profile);
            }

            if (HeroIcon != null)
            {
                Sprite cover = BirdDuelCdIcons.Resolve(SelectedCdId);
                HeroIcon.sprite = cover;
                HeroIcon.enabled = cover != null;
            }

            for (int i = 0; i < SlotCdIds.Count; i++)
            {
                if (SlotBackgrounds[i] == null) continue;
                bool on = string.Equals(SlotCdIds[i], SelectedCdId, StringComparison.Ordinal);
                SlotBackgrounds[i].color = on ? BirdDuelUiColors.CdSlotSelected : BirdDuelUiColors.CdSlotIdle;
            }
        }
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        BirdDuelMobileOverlayLayout.ApplyMobilePanel(rt);
        Image panelImg = panel.GetComponent<Image>();
        panelImg.color = BirdDuelUiColors.CdPanelBg;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = BirdDuelUiColors.PanelEdge;
        outline.effectDistance = new Vector2(0f, -3f);
        return panel;
    }

    private static GameObject BuildPickerBar(
        Transform panel,
        TMP_FontAsset font,
        List<string> ownedIds,
        OverlayUi ui)
    {
        GameObject bar = new GameObject("PickerBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(panel, false);
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0f, PickerBarHeight);
        bar.GetComponent<Image>().color = BirdDuelUiColors.CdPanelAccent;

        GameObject scrollRoot = new GameObject("Slots", typeof(RectTransform));
        scrollRoot.transform.SetParent(bar.transform, false);
        RectTransform slotsRt = scrollRoot.GetComponent<RectTransform>();
        slotsRt.anchorMin = new Vector2(0f, 0f);
        slotsRt.anchorMax = new Vector2(1f, 1f);
        slotsRt.offsetMin = new Vector2(20f, 10f);
        slotsRt.offsetMax = new Vector2(-72f, -10f);

        HorizontalLayoutGroup layout = scrollRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < ownedIds.Count; i++)
        {
            string cdId = ownedIds[i];
            ui.SlotCdIds.Add(cdId);
            CreatePickerSlot(slotsRt, font, cdId, ui);
        }

        Button collapseBtn = CreateSquareButton(bar.transform, font, "CollapsePicker", "◀", 52f);
        RectTransform collapseRt = collapseBtn.GetComponent<RectTransform>();
        collapseRt.anchorMin = new Vector2(1f, 0.5f);
        collapseRt.anchorMax = new Vector2(1f, 0.5f);
        collapseRt.pivot = new Vector2(1f, 0.5f);
        collapseRt.anchoredPosition = new Vector2(-12f, 0f);
        collapseBtn.onClick.AddListener(() => ui.SetPickerVisible(false));

        return bar;
    }

    private static void CreatePickerSlot(Transform parent, TMP_FontAsset font, string cdId, OverlayUi ui)
    {
        GameObject slot = new GameObject("Slot_" + cdId, typeof(RectTransform), typeof(Image), typeof(Button));
        slot.transform.SetParent(parent, false);
        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(72f, 72f);
        Image bg = slot.GetComponent<Image>();
        bg.color = BirdDuelUiColors.CdSlotIdle;
        ui.SlotBackgrounds.Add(bg);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(slot.transform, false);
        StretchFull(iconObj.GetComponent<RectTransform>(), 8f);
        Image icon = iconObj.GetComponent<Image>();
        icon.sprite = BirdDuelCdIcons.Resolve(cdId);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = icon.sprite != null;

        Button btn = slot.GetComponent<Button>();
        string captured = cdId;
        btn.onClick.AddListener(() => ui.Select(captured));
    }

    private static void BuildHeader(Transform panel, TMP_FontAsset font, OverlayUi ui)
    {
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel, false);
        RectTransform rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, HeaderHeight);
        header.GetComponent<Image>().color = BirdDuelUiColors.CdPanelAccent;
        ui.HeaderRt = rt;

        CreateAnchoredText(header.transform, font, "Title", "✦ 選擇 CD 光碟",
            30f, FontStyles.Bold, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center, BirdDuelUiColors.WonderBadge);
    }

    private static GameObject BuildSwitchRow(
        Transform panel,
        TMP_FontAsset font,
        OverlayUi ui)
    {
        GameObject row = new GameObject("SwitchRow", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(panel, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.offsetMin = new Vector2(ContentPadH, FooterHeight + 8f);
        rowRt.offsetMax = new Vector2(-ContentPadH, FooterHeight + 8f + SwitchRowHeight);
        row.GetComponent<Image>().color = BirdDuelUiColors.CdPanelAccent;

        Button otherBtn = CreateModalButton(row.transform, font, "OtherCdButton", "＋  其他 CD 光碟");
        StretchFull(otherBtn.GetComponent<RectTransform>(), 0f);
        ApplyButtonColors(otherBtn, true);
        TextMeshProUGUI label = otherBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.fontSize = 28f;

        otherBtn.onClick.AddListener(() => ui.SetPickerVisible(true));
        row.transform.SetAsLastSibling();
        return row;
    }

    private static void BuildFooter(
        Transform panel,
        TMP_FontAsset font,
        Action onBack,
        Action onConfirm)
    {
        GameObject footer = new GameObject("Footer", typeof(RectTransform));
        footer.transform.SetParent(panel, false);
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.offsetMin = Vector2.zero;
        footerRt.offsetMax = new Vector2(0f, FooterHeight);

        Button backBtn = CreateModalButton(footer.transform, font, "BackBtn", "返回");
        RectTransform backRt = backBtn.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0f, 0f);
        backRt.anchorMax = new Vector2(0.5f, 1f);
        backRt.offsetMin = new Vector2(ContentPadH, FooterButtonPadV);
        backRt.offsetMax = new Vector2(-10f, -FooterButtonPadV);
        ApplyButtonColors(backBtn, false);
        backBtn.onClick.AddListener(() => onBack?.Invoke());

        Button confirmBtn = CreateModalButton(footer.transform, font, "ConfirmBtn", "確認交付");
        RectTransform confirmRt = confirmBtn.GetComponent<RectTransform>();
        confirmRt.anchorMin = new Vector2(0.5f, 0f);
        confirmRt.anchorMax = new Vector2(1f, 1f);
        confirmRt.offsetMin = new Vector2(10f, FooterButtonPadV);
        confirmRt.offsetMax = new Vector2(-ContentPadH, -FooterButtonPadV);
        ApplyButtonColors(confirmBtn, true);
        confirmBtn.onClick.AddListener(() => onConfirm?.Invoke());
    }

    private static float FooterButtonPadV => BirdDuelMobileOverlayLayout.FooterButtonPadV;

    private static RectTransform BuildMainContent(Transform panel, TMP_FontAsset font, OverlayUi ui)
    {
        bool portraitStack = BirdDuelMobileOverlayLayout.PreferPortraitStack();
        float coverSize = portraitStack ? 140f : 160f;
        float coverColWidth = portraitStack ? 0f : 176f;

        GameObject body = new GameObject("Body", typeof(RectTransform));
        body.transform.SetParent(panel, false);
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(ContentPadH, FooterHeight + SwitchRowHeight + 16f);
        bodyRt.offsetMax = new Vector2(-ContentPadH, -(HeaderHeight + 12f));

        GameObject frame = new GameObject("CoverFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(body.transform, false);
        RectTransform frameRt = frame.GetComponent<RectTransform>();
        if (portraitStack)
        {
            frameRt.anchorMin = new Vector2(0.5f, 1f);
            frameRt.anchorMax = new Vector2(0.5f, 1f);
            frameRt.pivot = new Vector2(0.5f, 1f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(coverSize, coverSize);
        }
        else
        {
            frameRt.anchorMin = new Vector2(0f, 1f);
            frameRt.anchorMax = new Vector2(0f, 1f);
            frameRt.pivot = new Vector2(0f, 1f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(coverSize, coverSize);
        }
        frame.GetComponent<Image>().color = BirdDuelUiColors.CdFrameBg;

        GameObject iconObj = new GameObject("Cover", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(frame.transform, false);
        StretchFull(iconObj.GetComponent<RectTransform>(), 10f);
        ui.HeroIcon = iconObj.GetComponent<Image>();
        ui.HeroIcon.preserveAspect = true;
        ui.HeroIcon.raycastTarget = false;

        GameObject textCol = new GameObject("TextColumn", typeof(RectTransform));
        textCol.transform.SetParent(body.transform, false);
        RectTransform textRt = textCol.GetComponent<RectTransform>();
        if (portraitStack)
        {
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(0f, 0f);
            textRt.offsetMax = new Vector2(0f, -(coverSize + 16f));
        }
        else
        {
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(coverColWidth, 0f);
            textRt.offsetMax = Vector2.zero;
        }

        ui.NameTmp = CreateAnchoredText(textCol.transform, font, "CdName", string.Empty,
            30f, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 44f),
            TextAlignmentOptions.Left, BirdDuelUiColors.CdTextMain);

        ui.MetaTmp = CreateAnchoredText(textCol.transform, font, "Meta", string.Empty,
            24f, FontStyles.Normal, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(0f, 30f),
            TextAlignmentOptions.Left, BirdDuelUiColors.WonderGlow);

        GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(textCol.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(0f, 0f);
        descRt.offsetMax = new Vector2(0f, -88f);
        ui.DescTmp = descObj.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.DescTmp.font = font;
        ui.DescTmp.text = string.Empty;
        ui.DescTmp.fontSize = portraitStack ? 28f : 26f;
        ui.DescTmp.alignment = TextAlignmentOptions.TopLeft;
        ui.DescTmp.enableWordWrapping = true;
        ui.DescTmp.lineSpacing = 6f;
        ui.DescTmp.paragraphSpacing = 8f;
        ui.DescTmp.color = BirdDuelUiColors.CdTextMuted;
        ui.DescTmp.raycastTarget = false;

        return bodyRt;
    }

    private static Button CreateSquareButton(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string label,
        float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        Image img = go.GetComponent<Image>();
        img.color = Color.white;
        ApplyButtonColors(go.GetComponent<Button>(), true);

        CreateAnchoredText(go.transform, font, "Label", label, 24f, FontStyles.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 8f, size - 8f),
            TextAlignmentOptions.Center, BirdDuelUiColors.BtnPrimaryText);

        return go.GetComponent<Button>();
    }

    private static string BuildDescription(BirdDuelCdProfile profile)
    {
        const string rules =
            "整卡不消耗。僅鬥鳥勝利時，draft 偏向此碟陣營加成；平手／敗北無偏向。";
        if (profile == null)
            return rules;

        string factionHint;
        if (string.Equals(profile.CdId, "court_march", StringComparison.OrdinalIgnoreCase))
        {
            factionHint = "勝利 draft：庭訓號令／王權方陣／前鋒偵察／御前護衛／戰鼓齊進／背水（與港灣池不同）。";
        }
        else if (BirdDuelRhythmChart.IsMorningPrayer(profile.CdId))
        {
            factionHint =
                "節奏：屏息谱面。勝利 draft：晨禱守夜／窺視之眼／靜默重整／穿堂頌詩／聖盾禱告／密藏引路（情報＋庇護，無先攻）。";
        }
        else
        {
            factionHint = profile.Faction switch
            {
                BirdDuelCdFaction.King => "勝利 draft 國王專屬（庭訓號令、王權方陣…，與港灣池不同）。",
                BirdDuelCdFaction.Church => "勝利 draft 偏向教會陣營加成。",
                _ => "勝利 draft 使用通用強化池。",
            };
        }

        return rules + "\n\n" + factionHint;
    }

    private static string RarityLabel(BirdDuelCdRarity rarity)
    {
        switch (rarity)
        {
            case BirdDuelCdRarity.SR: return "SR";
            case BirdDuelCdRarity.R: return "R";
            default: return "N";
        }
    }

    private static string FactionLabel(BirdDuelCdFaction faction)
    {
        switch (faction)
        {
            case BirdDuelCdFaction.King: return "國王";
            case BirdDuelCdFaction.Church: return "教會";
            default: return "通用";
        }
    }

    private static TextMeshProUGUI CreateAnchoredText(
        Transform parent,
        TMP_FontAsset font,
        string objName,
        string text,
        float fontSize,
        FontStyles style,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        TextAlignmentOptions align,
        Color color)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        SettingsUiFonts.ApplyTo(tmp);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateModalButton(Transform parent, TMP_FontAsset font, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Color.white;
        Button btn = go.GetComponent<Button>();
        ApplyButtonColors(btn, true);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        SettingsUiFonts.ApplyTo(tmp);
        tmp.text = label;
        tmp.fontSize = BirdDuelMobileOverlayLayout.ButtonFontSecondary;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = BirdDuelUiColors.BtnPrimaryText;
        tmp.raycastTarget = false;
        return btn;
    }

    private static void ApplyButtonColors(Button btn, bool primary)
    {
        if (btn == null) return;
        Image img = btn.targetGraphic as Image;
        if (img == null) img = btn.GetComponent<Image>();
        if (img != null) img.color = Color.white;

        var cb = btn.colors;
        if (primary)
        {
            cb.normalColor = BirdDuelUiColors.BtnPrimary;
            cb.highlightedColor = BirdDuelUiColors.BtnPrimaryH;
            cb.pressedColor = BirdDuelUiColors.BtnPrimaryP;
        }
        else
        {
            cb.normalColor = BirdDuelUiColors.BtnSecondary;
            cb.highlightedColor = BirdDuelUiColors.BtnSecondaryH;
            cb.pressedColor = BirdDuelUiColors.BtnSecondaryP;
        }

        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = BirdDuelUiColors.BtnDisabledBg;
        btn.colors = cb;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.color = primary ? BirdDuelUiColors.BtnPrimaryText : BirdDuelUiColors.BtnSecondaryText;
    }

    private static void StretchFull(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}

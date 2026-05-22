using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>背包卡牌詳情：戰技三階段區塊（遊戲風格 UI）。</summary>
public sealed class BackpackInspectSkillUi
{
    public RectTransform root;
    public TextMeshProUGUI sectionTitleTmp;
    public RectTransform stepperRoot;
    public readonly ProficiencyStepNodeUi[] stepNodes = new ProficiencyStepNodeUi[3];
    public Image skillBannerBg;
    public TextMeshProUGUI skillNameTmp;
    public readonly BackpackInspectSkillStageRowUi[] stageRows = new BackpackInspectSkillStageRowUi[3];
}

public sealed class ProficiencyStepNodeUi
{
    public GameObject root;
    public Image connectorLeft;
    public Image connectorRight;
    public Image nodeBg;
    public Image nodeRing;
    public TextMeshProUGUI glyphTmp;
    public TextMeshProUGUI labelTmp;
}

public sealed class BackpackInspectSkillStageRowUi
{
    public GameObject root;
    public Image panelBg;
    public Image accentStrip;
    public Image stageBadgeBg;
    public TextMeshProUGUI stageBadgeGlyphTmp;
    public TextMeshProUGUI headerTmp;
    public TextMeshProUGUI statusTmp;
    public TextMeshProUGUI bodyTmp;
    public Image lockVeil;
    public TextMeshProUGUI lockGlyphTmp;
    public CardSkillRevealStage stage;
}

public static class BackpackInspectSkillUiBuilder
{
    private const int StageBodySize = 20;
    private const float StepNodeSize = 44f;

    public static BackpackInspectSkillUi AttachToScrollContent(RectTransform scrollContent, TMP_FontAsset font)
        => AttachFlat(scrollContent, font);

    /// <summary>扁平區塊（無巢狀 ContentSizeFitter 框），捲動區高度較穩定。</summary>
    public static BackpackInspectSkillUi AttachFlat(RectTransform scrollContent, TMP_FontAsset font)
    {
        var ui = new BackpackInspectSkillUi();

        GameObject section = new GameObject("SkillBlock", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        section.transform.SetParent(scrollContent, false);
        ui.root = section.GetComponent<RectTransform>();
        Image sectionBg = section.GetComponent<Image>();
        sectionBg.color = new Color(0.1f, 0.14f, 0.22f, 0.94f);
        sectionBg.raycastTarget = false;

        LayoutElement sectionLe = section.GetComponent<LayoutElement>();
        sectionLe.flexibleWidth = 1f;
        sectionLe.minHeight = 120f;

        VerticalLayoutGroup sectionVlg = section.GetComponent<VerticalLayoutGroup>();
        sectionVlg.spacing = 12f;
        sectionVlg.padding = new RectOffset(14, 14, 14, 14);
        sectionVlg.childAlignment = TextAnchor.UpperLeft;
        sectionVlg.childControlWidth = true;
        sectionVlg.childControlHeight = true;
        sectionVlg.childForceExpandWidth = true;
        sectionVlg.childForceExpandHeight = false;

        Image topAccent = BackpackInspectVisualStyle.CreateSolid(section.transform, "TopAccent", BackpackInspectVisualStyle.FrameHighlight);
        topAccent.rectTransform.sizeDelta = new Vector2(0f, 3f);
        LayoutElement accentLe = topAccent.gameObject.AddComponent<LayoutElement>();
        accentLe.preferredHeight = 3f;
        accentLe.flexibleWidth = 1f;
        topAccent.transform.SetAsFirstSibling();

        ui.sectionTitleTmp = CreateText(section.transform, font, "SkillSectionTitle", 24, FontStyles.Bold,
            BackpackInspectVisualStyle.TitleGold, TextAlignmentOptions.Center);
        ui.sectionTitleTmp.text = "【 戰技熟練度 】";

        ui.stepperRoot = BuildProficiencyStepper(section.transform, font, ui.stepNodes);
        ui.skillBannerBg = BuildSkillNameBanner(section.transform, font, out ui.skillNameTmp);

        CardSkillRevealStage[] stages =
        {
            CardSkillRevealStage.LockedA,
            CardSkillRevealStage.BasicB,
            CardSkillRevealStage.FullC
        };
        for (int i = 0; i < stages.Length; i++)
            ui.stageRows[i] = CreateStageRow(section.transform, font, stages[i]);

        ui.root.gameObject.SetActive(false);
        return ui;
    }

    public static void RefreshFonts(BackpackInspectSkillUi ui, TMP_FontAsset font)
    {
        if (ui == null) return;
        TMP_FontAsset resolved = ResolveSkillFont(font);
        ApplyFont(ui.sectionTitleTmp, resolved);
        ApplyFont(ui.skillNameTmp, resolved);
        for (int i = 0; i < ui.stepNodes.Length; i++)
        {
            if (ui.stepNodes[i] == null) continue;
            ApplyFont(ui.stepNodes[i].glyphTmp, resolved);
            ApplyFont(ui.stepNodes[i].labelTmp, resolved);
        }
        for (int i = 0; i < ui.stageRows.Length; i++)
        {
            BackpackInspectSkillStageRowUi row = ui.stageRows[i];
            if (row == null) continue;
            ApplyFont(row.stageBadgeGlyphTmp, resolved);
            ApplyFont(row.headerTmp, resolved);
            ApplyFont(row.statusTmp, resolved);
            ApplyFont(row.bodyTmp, resolved);
            ApplyFont(row.lockGlyphTmp, resolved);
        }
    }

    public static void Apply(Card card, BackpackInspectSkillUi ui)
    {
        if (ui == null || ui.root == null) return;
        if (card == null)
        {
            ui.root.gameObject.SetActive(false);
            return;
        }

        if (card is SpellCard spell)
        {
            ApplySpell(spell, ui);
            return;
        }

        if (card is MonsterCard monster)
        {
            ApplyMonster(monster, ui);
            return;
        }

        ui.root.gameObject.SetActive(false);
    }

    private static void ApplySpell(SpellCard spell, BackpackInspectSkillUi ui)
    {
        ui.root.gameObject.SetActive(true);
        ui.sectionTitleTmp.text = "【 法術效果 】";
        ui.stepperRoot.gameObject.SetActive(false);
        ui.skillNameTmp.text = MonsterSkillRegistry.FormatSkillNameRich(
            string.IsNullOrWhiteSpace(spell.cardName) ? "法術" : spell.cardName.Trim());

        string body = string.IsNullOrWhiteSpace(spell.effect) ? "此法術暫無效果描述。" : spell.effect.Trim();
        ApplyStageRowVisual(ui.stageRows[0], CardSkillRevealStage.FullC, true, body, false);
        ui.stageRows[0].headerTmp.text = "法術效果";
        ui.stageRows[0].stageBadgeGlyphTmp.text = "✦";
        ui.stageRows[0].accentStrip.color = new Color(0.75f, 0.55f, 0.95f, 1f);
        ui.stageRows[1].root.SetActive(false);
        ui.stageRows[2].root.SetActive(false);
    }

    private static void ApplyMonster(MonsterCard monster, BackpackInspectSkillUi ui)
    {
        if (!MonsterSkillRegistry.HasSkillTrack(monster.id))
        {
            ui.root.gameObject.SetActive(false);
            return;
        }

        ui.root.gameObject.SetActive(true);
        ui.sectionTitleTmp.text = "【 戰技熟練度 】";
        ui.stepperRoot.gameObject.SetActive(true);

        CardSkillRevealStage unlocked = CardSkillProficiencyService.GetUnlockedStage(monster.id);
        ApplyProficiencyStepper(ui.stepNodes, unlocked);

        if (!MonsterSkillRegistry.TryGetSkillName(monster.id, out string skillName))
            skillName = "戰技";
        ui.skillNameTmp.text = MonsterSkillRegistry.FormatSkillNameRich(skillName);

        ApplyMonsterStageRow(ui.stageRows[0], monster.id, CardSkillRevealStage.LockedA, unlocked);
        ApplyMonsterStageRow(ui.stageRows[1], monster.id, CardSkillRevealStage.BasicB, unlocked);
        ApplyMonsterStageRow(ui.stageRows[2], monster.id, CardSkillRevealStage.FullC, unlocked);
    }

    private static void ApplyMonsterStageRow(
        BackpackInspectSkillStageRowUi row,
        int monsterId,
        CardSkillRevealStage stage,
        CardSkillRevealStage unlocked)
    {
        row.root.SetActive(true);
        bool stageUnlocked = (int)unlocked >= (int)stage;
        string body;
        if (!stageUnlocked)
            body = MonsterSkillRegistry.GetLockedStagePlaceholder(stage);
        else if (!MonsterSkillRegistry.TryGetSkillStageBodyRich(monsterId, stage, out body))
            body = "（尚無此階段文案）";

        ApplyStageRowVisual(row, stage, stageUnlocked, body, true);
    }

    private static void ApplyStageRowVisual(
        BackpackInspectSkillStageRowUi row,
        CardSkillRevealStage stage,
        bool unlocked,
        string body,
        bool richBody)
    {
        Color accent = BackpackInspectVisualStyle.StageAccent(stage);
        row.headerTmp.text = BackpackInspectVisualStyle.StageTitleZh(stage);
        row.statusTmp.text = unlocked ? "已解放" : "未解放";
        row.bodyTmp.richText = richBody;
        row.bodyTmp.text = body ?? string.Empty;

        row.accentStrip.color = unlocked ? accent : BackpackInspectVisualStyle.InkDim;
        row.stageBadgeBg.color = unlocked ? WithAlpha(accent, 0.35f) : new Color(0.15f, 0.17f, 0.22f, 0.9f);
        row.stageBadgeGlyphTmp.color = unlocked ? accent : BackpackInspectVisualStyle.InkDim;
        row.panelBg.color = unlocked
            ? new Color(0.12f, 0.16f, 0.24f, 0.95f)
            : new Color(0.08f, 0.1f, 0.14f, 0.88f);
        row.headerTmp.color = unlocked ? BackpackInspectVisualStyle.InkBright : BackpackInspectVisualStyle.InkMuted;
        row.statusTmp.color = unlocked ? accent : new Color(0.75f, 0.45f, 0.48f, 1f);
        row.bodyTmp.color = unlocked ? new Color(0.9f, 0.93f, 0.98f, 1f) : BackpackInspectVisualStyle.InkDim;
        row.lockVeil.gameObject.SetActive(!unlocked);
        row.lockGlyphTmp.gameObject.SetActive(!unlocked);
        row.bodyTmp.alpha = unlocked ? 1f : 0.55f;
    }

    private static void ApplyProficiencyStepper(ProficiencyStepNodeUi[] nodes, CardSkillRevealStage unlocked)
    {
        CardSkillRevealStage[] stages =
        {
            CardSkillRevealStage.LockedA,
            CardSkillRevealStage.BasicB,
            CardSkillRevealStage.FullC
        };
        string[] labels = { "覺察", "基礎", "完整" };

        for (int i = 0; i < nodes.Length; i++)
        {
            ProficiencyStepNodeUi node = nodes[i];
            CardSkillRevealStage stage = stages[i];
            bool done = (int)unlocked > (int)stage;
            bool current = unlocked == stage;
            Color accent = BackpackInspectVisualStyle.StageAccent(stage);

            node.glyphTmp.text = BackpackInspectVisualStyle.StageGlyph(stage);
            node.labelTmp.text = labels[i];

            if (done)
            {
                node.nodeBg.color = WithAlpha(accent, 0.55f);
                node.nodeRing.color = accent;
                node.glyphTmp.color = BackpackInspectVisualStyle.InkBright;
                node.labelTmp.color = accent;
            }
            else if (current)
            {
                node.nodeBg.color = WithAlpha(accent, 0.35f);
                node.nodeRing.color = BackpackInspectVisualStyle.TitleGold;
                node.glyphTmp.color = BackpackInspectVisualStyle.TitleGold;
                node.labelTmp.color = BackpackInspectVisualStyle.TitleGold;
            }
            else
            {
                node.nodeBg.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
                node.nodeRing.color = BackpackInspectVisualStyle.InkDim;
                node.glyphTmp.color = BackpackInspectVisualStyle.InkDim;
                node.labelTmp.color = BackpackInspectVisualStyle.InkMuted;
            }

            Color conn = done || current ? WithAlpha(accent, 0.7f) : BackpackInspectVisualStyle.InkDim;
            if (node.connectorLeft != null) node.connectorLeft.color = conn;
            if (node.connectorRight != null) node.connectorRight.color = conn;

            node.labelTmp.fontStyle = current ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private static RectTransform BuildProficiencyStepper(Transform parent, TMP_FontAsset font, ProficiencyStepNodeUi[] nodes)
    {
        GameObject row = new GameObject("ProficiencyStepper", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 78f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0f;
        hlg.padding = new RectOffset(4, 4, 4, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        CardSkillRevealStage[] stages =
        {
            CardSkillRevealStage.LockedA,
            CardSkillRevealStage.BasicB,
            CardSkillRevealStage.FullC
        };
        string[] labels = { "覺察", "基礎", "完整" };

        for (int i = 0; i < 3; i++)
        {
            nodes[i] = CreateStepNode(row.transform, font, stages[i], labels[i], i == 0, i == 2);
            LayoutElement nodeLe = nodes[i].root.GetComponent<LayoutElement>();
            nodeLe.flexibleWidth = 1f;
            nodeLe.preferredHeight = 72f;
        }

        return row.GetComponent<RectTransform>();
    }

    private static ProficiencyStepNodeUi CreateStepNode(
        Transform parent,
        TMP_FontAsset font,
        CardSkillRevealStage stage,
        string label,
        bool hideLeftConnector,
        bool hideRightConnector)
    {
        var node = new ProficiencyStepNodeUi();
        GameObject root = new GameObject("Step_" + stage, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        node.root = root;

        GameObject leftConn = new GameObject("ConnL", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        leftConn.transform.SetParent(root.transform, false);
        node.connectorLeft = leftConn.GetComponent<Image>();
        node.connectorLeft.color = BackpackInspectVisualStyle.InkDim;
        LayoutElement leftLe = leftConn.GetComponent<LayoutElement>();
        leftLe.flexibleWidth = 1f;
        leftLe.preferredHeight = 4f;
        leftLe.minHeight = 4f;
        if (hideLeftConnector) leftConn.SetActive(false);

        GameObject badge = new GameObject("Badge", typeof(RectTransform), typeof(LayoutElement));
        badge.transform.SetParent(root.transform, false);
        LayoutElement badgeLe = badge.GetComponent<LayoutElement>();
        badgeLe.preferredWidth = StepNodeSize;
        badgeLe.preferredHeight = StepNodeSize + 22f;
        badgeLe.flexibleWidth = 0f;

        VerticalLayoutGroup badgeVlg = badge.AddComponent<VerticalLayoutGroup>();
        badgeVlg.spacing = 4f;
        badgeVlg.childAlignment = TextAnchor.MiddleCenter;
        badgeVlg.childControlWidth = true;
        badgeVlg.childControlHeight = true;
        badgeVlg.childForceExpandWidth = true;
        badgeVlg.childForceExpandHeight = false;

        GameObject ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
        ring.transform.SetParent(badge.transform, false);
        RectTransform ringRt = ring.GetComponent<RectTransform>();
        ringRt.sizeDelta = new Vector2(StepNodeSize, StepNodeSize);
        node.nodeRing = ring.GetComponent<Image>();
        node.nodeRing.color = BackpackInspectVisualStyle.InkDim;

        GameObject nodeBg = new GameObject("NodeBg", typeof(RectTransform), typeof(Image));
        nodeBg.transform.SetParent(ring.transform, false);
        RectTransform nodeBgRt = nodeBg.GetComponent<RectTransform>();
        BackpackInspectVisualStyle.Stretch(nodeBgRt, 5f, 5f, 5f, 5f);
        node.nodeBg = nodeBg.GetComponent<Image>();
        node.nodeBg.color = new Color(0.12f, 0.14f, 0.2f, 1f);

        node.glyphTmp = CreateText(ring.transform, font, "Glyph", 22, FontStyles.Bold,
            BackpackInspectVisualStyle.InkMuted, TextAlignmentOptions.Center);
        RectTransform glyphRt = node.glyphTmp.rectTransform;
        BackpackInspectVisualStyle.Stretch(glyphRt, 0f, 0f, 0f, 0f);

        node.labelTmp = CreateText(badge.transform, font, "Label", 16, FontStyles.Normal,
            BackpackInspectVisualStyle.InkMuted, TextAlignmentOptions.Center);
        node.labelTmp.text = label;

        GameObject rightConn = new GameObject("ConnR", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rightConn.transform.SetParent(root.transform, false);
        node.connectorRight = rightConn.GetComponent<Image>();
        node.connectorRight.color = BackpackInspectVisualStyle.InkDim;
        LayoutElement rightLe = rightConn.GetComponent<LayoutElement>();
        rightLe.flexibleWidth = 1f;
        rightLe.preferredHeight = 4f;
        if (hideRightConnector) rightConn.SetActive(false);

        return node;
    }

    private static Image BuildSkillNameBanner(Transform parent, TMP_FontAsset font, out TextMeshProUGUI skillName)
    {
        GameObject banner = new GameObject("SkillNameBanner", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        banner.transform.SetParent(parent, false);
        banner.GetComponent<LayoutElement>().preferredHeight = 52f;

        Image bannerBg = banner.GetComponent<Image>();
        bannerBg.color = new Color(0.08f, 0.11f, 0.18f, 1f);

        Image leftStripe = BackpackInspectVisualStyle.CreateSolid(banner.transform, "Stripe", BackpackInspectVisualStyle.FrameHighlight);
        RectTransform stripeRt = leftStripe.rectTransform;
        stripeRt.anchorMin = new Vector2(0f, 0f);
        stripeRt.anchorMax = new Vector2(0f, 1f);
        stripeRt.pivot = new Vector2(0f, 0.5f);
        stripeRt.sizeDelta = new Vector2(5f, 0f);
        stripeRt.anchoredPosition = Vector2.zero;

        Image innerGlow = BackpackInspectVisualStyle.CreateSolid(banner.transform, "Glow", WithAlpha(BackpackInspectVisualStyle.StageB, 0.12f));
        RectTransform glowRt = innerGlow.rectTransform;
        BackpackInspectVisualStyle.Stretch(glowRt, 8f, 4f, 8f, 4f);

        skillName = CreateText(banner.transform, font, "SkillName", 26, FontStyles.Bold,
            BackpackInspectVisualStyle.InkBright, TextAlignmentOptions.MidlineLeft);
        RectTransform nameRt = skillName.rectTransform;
        BackpackInspectVisualStyle.Stretch(nameRt, 18f, 0f, 12f, 0f);

        return bannerBg;
    }

    private static BackpackInspectSkillStageRowUi CreateStageRow(
        Transform parent,
        TMP_FontAsset font,
        CardSkillRevealStage stage)
    {
        var row = new BackpackInspectSkillStageRowUi { stage = stage };
        Color accent = BackpackInspectVisualStyle.StageAccent(stage);

        GameObject rowObj = new GameObject("Stage_" + stage, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObj.transform.SetParent(parent, false);
        row.root = rowObj;
        row.panelBg = rowObj.GetComponent<Image>();
        row.panelBg.color = new Color(0.1f, 0.13f, 0.2f, 0.92f);

        LayoutElement le = rowObj.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = 88f;

        row.accentStrip = BackpackInspectVisualStyle.CreateSolid(rowObj.transform, "Accent", accent);
        RectTransform stripRt = row.accentStrip.rectTransform;
        stripRt.anchorMin = new Vector2(0f, 0f);
        stripRt.anchorMax = new Vector2(0f, 1f);
        stripRt.pivot = new Vector2(0f, 0.5f);
        stripRt.sizeDelta = new Vector2(5f, 0f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        content.transform.SetParent(rowObj.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        BackpackInspectVisualStyle.Stretch(contentRt, 12f, 8f, 10f, 8f);
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(4, 4, 2, 2);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject headerRow = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerRow.transform.SetParent(content.transform, false);
        HorizontalLayoutGroup hlg = headerRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        GameObject badge = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        badge.transform.SetParent(headerRow.transform, false);
        row.stageBadgeBg = badge.GetComponent<Image>();
        row.stageBadgeBg.color = WithAlpha(accent, 0.3f);
        LayoutElement badgeLe = badge.GetComponent<LayoutElement>();
        badgeLe.preferredWidth = 36f;
        badgeLe.preferredHeight = 36f;

        row.stageBadgeGlyphTmp = CreateText(badge.transform, font, "Glyph", 20, FontStyles.Bold, accent,
            TextAlignmentOptions.Center);
        BackpackInspectVisualStyle.Stretch(row.stageBadgeGlyphTmp.rectTransform, 0f, 0f, 0f, 0f);
        row.stageBadgeGlyphTmp.text = BackpackInspectVisualStyle.StageGlyph(stage);

        row.headerTmp = CreateText(headerRow.transform, font, "Title", 21, FontStyles.Bold,
            BackpackInspectVisualStyle.InkBright, TextAlignmentOptions.MidlineLeft);
        row.headerTmp.text = BackpackInspectVisualStyle.StageTitleZh(stage);
        LayoutElement titleLe = row.headerTmp.gameObject.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;

        GameObject statusPill = new GameObject("StatusPill", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        statusPill.transform.SetParent(headerRow.transform, false);
        Image statusBg = statusPill.GetComponent<Image>();
        statusBg.color = WithAlpha(accent, 0.22f);
        LayoutElement statusLe = statusPill.GetComponent<LayoutElement>();
        statusLe.minWidth = 76f;
        statusLe.preferredHeight = 28f;

        row.statusTmp = CreateText(statusPill.transform, font, "Status", 15, FontStyles.Bold, accent,
            TextAlignmentOptions.Center);
        BackpackInspectVisualStyle.Stretch(row.statusTmp.rectTransform, 6f, 2f, 6f, 2f);

        row.bodyTmp = CreateText(content.transform, font, "Body", StageBodySize, FontStyles.Normal,
            BackpackInspectVisualStyle.InkBright, TextAlignmentOptions.TopLeft);
        row.bodyTmp.lineSpacing = 6f;
        row.bodyTmp.richText = true;
        LayoutElement bodyLe = row.bodyTmp.gameObject.GetComponent<LayoutElement>();
        if (bodyLe == null) bodyLe = row.bodyTmp.gameObject.AddComponent<LayoutElement>();
        bodyLe.flexibleWidth = 1f;
        bodyLe.minHeight = 48f;

        row.lockVeil = BackpackInspectVisualStyle.CreateSolid(rowObj.transform, "LockVeil", BackpackInspectVisualStyle.LockVeil);
        RectTransform veilRt = row.lockVeil.rectTransform;
        BackpackInspectVisualStyle.Stretch(veilRt, 0f, 0f, 0f, 0f);
        row.lockVeil.gameObject.SetActive(false);

        row.lockGlyphTmp = CreateText(rowObj.transform, font, "Lock", 36, FontStyles.Bold,
            new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.Center);
        RectTransform lockRt = row.lockGlyphTmp.rectTransform;
        BackpackInspectVisualStyle.Stretch(lockRt, 0f, 0f, 0f, 0f);
        row.lockGlyphTmp.text = "🔒";
        row.lockGlyphTmp.raycastTarget = false;
        row.lockGlyphTmp.gameObject.SetActive(false);

        return row;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        TMP_FontAsset font,
        string name,
        int fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = Mathf.Max(fontSize + 8f, 24f);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset resolved = ResolveSkillFont(font);
        if (resolved != null) tmp.font = resolved;
        tmp.outlineWidth = 0f;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        return tmp;
    }

    private static TMP_FontAsset ResolveSkillFont(TMP_FontAsset font)
    {
        if (font != null && BuildbeckUiFonts.FontSupportsText(font, "戰技"))
            return font;

        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null) return buildbeck;

        return TMP_Settings.defaultFontAsset;
    }

    private static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (tmp == null || font == null) return;
        tmp.font = font;
        tmp.outlineWidth = 0f;
        tmp.ForceMeshUpdate();
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}

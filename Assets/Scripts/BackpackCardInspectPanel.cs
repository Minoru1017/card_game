using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>背包卡牌詳情：例圖式版面（立繪／標題／階段分頁／數值列／熟練度／戰技單欄）。</summary>
[DisallowMultipleComponent]
public class BackpackCardInspectPanel : MonoBehaviour
{
    private const int UiBuildGeneration = 19;

    private const float ArtAnchorMax = 0.58f;
    private const float HeaderLeftAnchorMax = 0.54f;
    private const float HeaderRightAnchorMin = 0.56f;
    private const float DeckBarHeight = 54f;
    private const float MasteryBarHeight = BackpackInspectMasteryLayout.BarHeight;
    private const float MasteryInset = BackpackInspectMasteryLayout.Inset;
    private const float MasteryHeaderHeight = BackpackInspectMasteryLayout.HeaderHeight;
    private const float MasteryHelpButtonSizePx = BackpackInspectMasteryLayout.HelpButtonSizePx;
    private const float MasteryStatusRightReservePx = BackpackInspectMasteryLayout.StatusRightReservePx;
    private const float MasteryTrackHeight = BackpackInspectMasteryLayout.TrackHeight;
    private const float StageTabRowHeight = 94f;
    private const float StatStripHeight = 84f;
    private const float StatChipSpacing = 8f;
    private const float StatStripPadH = 10f;
    private const float StatStripPadV = 10f;

    private ICardInspectPanelHost host;
    private Canvas uiCanvas;
    private int uiBuildGeneration;

    private GameObject root;
    private RectTransform panelRt;
    private Image artImage;

    private RectTransform infoContentRt;
    private ScrollRect infoScroll;
    private RectTransform headerLeftRt;
    private RectTransform headerRightRt;
    private RectTransform deckBarRt;
    private RectTransform masteryBarRt;
    private RectTransform statStripRt;
    private RectTransform skillSectionRt;

    private TextMeshProUGUI titleTmp;
    private TextMeshProUGUI subtitleTmp;
    private TextMeshProUGUI typeTmp;
    private TextMeshProUGUI deckBarTmp;
    private TextMeshProUGUI masteryLabelTmp;
    private TextMeshProUGUI masteryStatusTmp;
    private RectTransform masteryFillRt;
    private readonly TextMeshProUGUI[] statChipTmps = new TextMeshProUGUI[4];
    private TextMeshProUGUI skillTmp;
    private readonly TextMeshProUGUI[] stageTabLabelTmps = new TextMeshProUGUI[3];
    private readonly Image[] stageTabBgImages = new Image[3];

    private TextMeshProUGUI pageTmp;
    private TextMeshProUGUI hintTmp;

    private readonly List<int> cardIds = new List<int>();
    private int currentIndex = -1;
    private Card currentCard;
    private CardDisplay sourceDisplay;
    private CardSkillRevealStage previewStage = CardSkillRevealStage.LockedA;
    private float ignoreSwipeUntil;

    public void BindHost(ICardInspectPanelHost panelHost) => host = panelHost;

    public bool IsOpen => root != null && root.activeSelf;

    private void OnDestroy() => DestroyUi();

    public void Show(Card card, CardDisplay displaySource = null)
    {
        if (card == null || host == null) return;
        host.EnsureCoreRefsForInspect();

        Canvas canvas = host.BackpackInspectResolveCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[BackpackCardInspect] 找不到 UI Canvas。");
            return;
        }

        EnsureUi(canvas);
        if (root == null) return;

        sourceDisplay = displaySource != null && displaySource.card != null && displaySource.card.id == card.id
            ? displaySource
            : null;

        RebuildCardList();
        currentIndex = cardIds.IndexOf(card.id);
        if (currentIndex < 0)
        {
            cardIds.Add(card.id);
            currentIndex = cardIds.Count - 1;
        }

        currentCard = card;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        ignoreSwipeUntil = Time.unscaledTime + 0.15f;

        ApplyCard(card);
        RefreshPageHint();
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        cardIds.Clear();
        currentIndex = -1;
        currentCard = null;
        sourceDisplay = null;
    }

    public void TickSwipeInput()
    {
        if (!IsOpen || cardIds.Count <= 1) return;
        if (Time.unscaledTime < ignoreSwipeUntil) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            OnSwipe(60f);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            OnSwipe(-60f);
    }

    public void OnSwipe(float dragDeltaX)
    {
        if (root == null || !root.activeSelf || cardIds.Count <= 1) return;
        if (Time.unscaledTime < ignoreSwipeUntil) return;

        const float threshold = 48f;
        if (Mathf.Abs(dragDeltaX) < threshold) return;

        ignoreSwipeUntil = Time.unscaledTime + 0.22f;

        currentIndex += dragDeltaX < 0f ? 1 : -1;
        if (currentIndex < 0) currentIndex = cardIds.Count - 1;
        else if (currentIndex >= cardIds.Count) currentIndex = 0;

        Card card = ResolveCard(cardIds[currentIndex]);
        if (card == null) return;

        if (sourceDisplay != null && sourceDisplay.card != null && sourceDisplay.card.id != card.id)
            sourceDisplay = null;

        currentCard = card;
        ApplyCard(card);
        RefreshPageHint();
    }

    public void SelectPreviewStage(CardSkillRevealStage stage)
    {
        previewStage = stage;
        if (currentCard != null && skillTmp != null)
        {
            skillTmp.text = BuildSkillRich(currentCard);
            StartCoroutine(CoRefreshInfoScrollLayout());
        }
        RefreshStageTabVisuals();
    }

    private void ApplyCard(Card card)
    {
        if (card == null) return;

        TMP_FontAsset font = host.BackpackInspectResolveFont();
        if (font == null) font = TMP_Settings.defaultFontAsset;
        ApplyTypographyFonts(font);
        ApplyTypographySpacing();

        string title = string.IsNullOrWhiteSpace(card.cardName) ? "未命名卡牌" : card.cardName.Trim();
        if (titleTmp != null) titleTmp.text = title;

        if (subtitleTmp != null)
        {
            string en = card.cardNameEnglish != null ? card.cardNameEnglish.Trim() : string.Empty;
            subtitleTmp.text = string.IsNullOrEmpty(en) ? string.Empty : en;
            subtitleTmp.gameObject.SetActive(!string.IsNullOrEmpty(en));
        }

        if (typeTmp != null)
            typeTmp.text = BuildTypeRarityLine(card);

        if (deckBarTmp != null)
            deckBarTmp.text = host.BackpackInspectDeckInclusionText(card.id);

        ApplyMasteryBar(card);

        if (card is MonsterCard monster)
            previewStage = CardSkillProficiencyService.GetUnlockedStage(monster.id);
        else
            previewStage = CardSkillRevealStage.FullC;

        ApplyStatChips(card);
        if (skillTmp != null) skillTmp.text = BuildSkillRich(card);

        RefreshStageTabVisuals();
        ApplyArt(card);
        StartCoroutine(CoRefreshInfoScrollLayout());
    }

    private void ApplyMasteryBar(Card card)
    {
        ProficiencyBarViewModel bar = CardSkillProficiencyService.GetProficiencyBarForCard(card);
        bool show = bar.show && masteryBarRt != null;

        if (masteryBarRt != null)
            masteryBarRt.gameObject.SetActive(show);
        if (!show) return;

        if (masteryLabelTmp != null)
        {
            masteryLabelTmp.text = bar.label;
            masteryLabelTmp.ForceMeshUpdate();
        }

        if (masteryStatusTmp != null)
        {
            masteryStatusTmp.text = bar.statusText;
            masteryStatusTmp.ForceMeshUpdate();
        }

        CardProficiencyDebugReset.ApplyBackpackMasteryFill(masteryFillRt, bar.fill01);
    }

    /// <summary>Debug 清空熟練度後刷新目前詳情列。</summary>
    public void RefreshMasteryBarIfOpen()
    {
        if (!IsOpen || currentCard == null) return;
        ApplyMasteryBar(currentCard);
        RefreshStageTabVisuals();
        if (skillTmp != null)
            skillTmp.text = BuildSkillRich(currentCard);
    }

    private void RefreshStageTabVisuals()
    {
        CardSkillRevealStage[] stages =
        {
            CardSkillRevealStage.LockedA,
            CardSkillRevealStage.BasicB,
            CardSkillRevealStage.FullC
        };
        string[] labels = { "A 階段", "B 階段", "C 階段" };

        for (int i = 0; i < stageTabBgImages.Length; i++)
        {
            if (stageTabBgImages[i] == null) continue;
            bool selected = previewStage == stages[i];
            stageTabBgImages[i].color = selected
                ? BackpackInspectUiColors.TabSelectedBg
                : BackpackInspectUiColors.TabIdleBg;
            if (stageTabLabelTmps[i] != null)
            {
                stageTabLabelTmps[i].text = labels[i];
                stageTabLabelTmps[i].color = selected
                    ? BackpackInspectUiColors.TabSelectedText
                    : BackpackInspectUiColors.TabIdleText;
            }
        }
    }

    private IEnumerator CoRefreshInfoScrollLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (infoContentRt == null) yield break;

        float padH = BackpackInspectVisualStyle.Typography.InfoPaddingH;
        float gap = BackpackInspectVisualStyle.Typography.BlockGap;
        float padTop = BackpackInspectVisualStyle.Typography.InfoPaddingTop;
        float padBottom = 28f;
        float fullW = infoContentRt.rect.width > 8f ? infoContentRt.rect.width : 520f;

        float y = padTop;

        float headerLeftW = fullW * HeaderLeftAnchorMax;
        float headerLeftH = LayoutInfoColumn(0f, padH, gap, headerLeftW, titleTmp, subtitleTmp, typeTmp);
        headerLeftH = Mathf.Max(headerLeftH, StageTabRowHeight);
        PlaceColumnBand(headerLeftRt, y, headerLeftH);
        PlaceColumnBand(headerRightRt, y, StageTabRowHeight);

        float headerH = Mathf.Max(headerLeftH, StageTabRowHeight);
        y += headerH + gap;

        PlaceBand(deckBarRt, y, DeckBarHeight);
        y += DeckBarHeight + gap;

        PlaceBand(statStripRt, y, StatStripHeight);
        y += StatStripHeight + gap;

        if (masteryBarRt != null && masteryBarRt.gameObject.activeSelf)
        {
            PlaceBand(masteryBarRt, y, MasteryBarHeight);
            y += MasteryBarHeight + gap;
        }

        float skillH = LayoutInfoColumn(0f, padH, gap, fullW, skillTmp);
        PlaceBand(skillSectionRt, y, skillH);
        y += skillH + padBottom;
        infoContentRt.sizeDelta = new Vector2(0f, y);

        if (infoScroll != null)
            infoScroll.verticalNormalizedPosition = 1f;
    }

    private static void PlaceColumnBand(RectTransform col, float yTop, float height)
    {
        if (col == null) return;
        col.anchoredPosition = new Vector2(0f, -yTop);
        col.sizeDelta = new Vector2(0f, height);
    }

    private static void PlaceBand(RectTransform band, float yTop, float height)
    {
        if (band == null) return;
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = new Vector2(1f, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.anchoredPosition = new Vector2(0f, -yTop);
        band.sizeDelta = new Vector2(0f, height);
    }

    private static float LayoutInfoColumn(
        float yStart,
        float padH,
        float gap,
        float columnWidth,
        params TextMeshProUGUI[] blocks)
    {
        float y = yStart;
        if (blocks == null) return y;

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null || !blocks[i].gameObject.activeInHierarchy) continue;
            y = PlaceTextBlockInColumn(blocks[i], y, columnWidth, padH) + gap;
        }

        return y > yStart ? y - gap : yStart;
    }

    private static float PlaceTextBlockInColumn(TextMeshProUGUI tmp, float yTop, float columnWidth, float padH)
    {
        if (tmp == null) return yTop;
        float innerW = Mathf.Max(80f, columnWidth - padH * 2f);
        float height = Mathf.Max(34f, tmp.GetPreferredValues(tmp.text, innerW, 0f).y + 6f);
        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yTop);
        rt.sizeDelta = new Vector2(-padH * 2f, height);
        return yTop + height;
    }

    private void ApplyArt(Card card)
    {
        if (artImage == null) return;

        Sprite sprite = ResolveSprite(card, sourceDisplay);
        bool has = sprite != null;
        artImage.sprite = sprite;
        artImage.enabled = has;
        artImage.color = has ? Color.white : BackpackInspectVisualStyle.FrameInner;
        CardDisplay.SyncCardArtRarityOverlay(artImage, has ? card : null);
    }

    private static string BuildTypeRarityLine(Card card)
    {
        string type = card is SpellCard ? "法術牌" : (card is MonsterCard ? "怪物牌" : "卡牌");
        return $"{type}  {card.rarity}";
    }

    private void ApplyStatChips(Card card)
    {
        if (statChipTmps[0] == null) return;

        string atk;
        string hp;
        if (card is MonsterCard m)
        {
            atk = m.attack.ToString();
            hp = m.healthPointMax.ToString();
        }
        else
        {
            atk = "無";
            hp = "無";
        }

        int owned = host.BackpackInspectCollectionCount(card.id);
        string rarity = card.rarity.ToString();
        string[] labels = { "攻擊力", "生命值", "持有數", "稀有度" };
        string[] values = { atk, hp, owned.ToString(), rarity };

        for (int i = 0; i < statChipTmps.Length; i++)
        {
            if (statChipTmps[i] == null) continue;
            statChipTmps[i].text = labels[i] + " " + values[i];
        }
    }

    private void ApplyTypographySpacing()
    {
        if (subtitleTmp != null)
            subtitleTmp.lineSpacing = BackpackInspectVisualStyle.Typography.SubtitleLineSpacing;
        if (skillTmp != null)
        {
            skillTmp.lineSpacing = BackpackInspectVisualStyle.Typography.BodyLineSpacing;
            skillTmp.paragraphSpacing = BackpackInspectVisualStyle.Typography.BodyParagraphSpacing;
        }
    }

    private string BuildSkillRich(Card card)
    {
        var sb = new StringBuilder(512);

        if (card is SpellCard spell)
        {
            sb.Append(BackpackInspectVisualStyle.WrapSubtitleRich("法術效果"));
            string effect = string.IsNullOrWhiteSpace(spell.effect) ? "此法術暫無效果描述。" : spell.effect.Trim();
            sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectVisualStyle.Typography.BodyOnSkill));
            sb.Append(effect);
            sb.Append("</color>");
            return sb.ToString();
        }

        if (card is MonsterCard monster)
        {
            if (!MonsterSkillRegistry.HasSkillTrack(monster.id))
            {
                sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectVisualStyle.Typography.BodyOnSkill));
                sb.Append("此卡尚無戰技說明");
                sb.Append("</color>");
                return sb.ToString();
            }

            if (MonsterSkillRegistry.TryGetSkillName(monster.id, out string skillName))
            {
                sb.Append(BackpackInspectVisualStyle.WrapSubtitleRich(skillName));
                sb.Append('\n');
            }

            CardSkillRevealStage unlocked = CardSkillProficiencyService.GetUnlockedStage(monster.id);
            AppendStageRich(sb, monster.id, previewStage, unlocked);
            return sb.ToString().TrimEnd();
        }

        return string.Empty;
    }

    private static void AppendStageRich(
        StringBuilder sb,
        int monsterId,
        CardSkillRevealStage stage,
        CardSkillRevealStage unlocked)
    {
        string title = BackpackInspectVisualStyle.StageTitleZh(stage);
        bool open = (int)unlocked >= (int)stage;
        Color accent = BackpackInspectVisualStyle.StageAccent(stage);

        sb.Append(BackpackInspectVisualStyle.ColorTag(open ? accent : BackpackInspectVisualStyle.InkDim));
        sb.Append(title);
        sb.Append(open ? "  已解放" : "  未解放");
        sb.Append("</color>\n");

        if (!open)
        {
            sb.Append(MonsterSkillRegistry.GetLockedStagePlaceholder(stage));
        }
        else if (MonsterSkillRegistry.TryGetSkillStageBodyRich(monsterId, stage, out string rich))
        {
            sb.Append(rich);
        }
        else
        {
            sb.Append(BackpackInspectVisualStyle.ColorTag(BackpackInspectVisualStyle.Typography.BodyMuted));
            sb.Append("尚無此階段文案");
            sb.Append("</color>");
        }
    }

    private static Sprite ResolveSprite(Card card, CardDisplay display)
    {
        if (card == null) return null;

        if (display != null)
        {
            if (display.backgroundImage != null && display.backgroundImage.sprite != null)
                return display.backgroundImage.sprite;
            Sprite s = display.card?.ResolveCardArtSprite();
            if (s != null) return s;
            s = display.card?.ResolveDeckThumbSprite();
            if (s != null) return s;
        }

        Sprite art = card.ResolveCardArtSprite();
        if (art != null) return art;
        art = card.ResolveDeckThumbSprite();
        if (art != null) return art;

        if (!string.IsNullOrWhiteSpace(card.artworkResourcePath))
        {
            art = Resources.Load<Sprite>(card.artworkResourcePath.Trim());
            if (art != null) return art;
        }
        if (!string.IsNullOrWhiteSpace(card.deckThumbResourcePath))
        {
            art = Resources.Load<Sprite>(card.deckThumbResourcePath.Trim());
            if (art != null) return art;
        }

        return Resources.Load<Sprite>($"CardArt/{card.id}");
    }

    private void RefreshPageHint()
    {
        bool many = cardIds.Count > 1;
        if (pageTmp != null)
            pageTmp.text = many && currentIndex >= 0 ? $"{currentIndex + 1} / {cardIds.Count}" : string.Empty;
        if (hintTmp != null)
            hintTmp.text = "上下滑動閱讀詳情";
    }

    private void RebuildCardList()
    {
        cardIds.Clear();
        host.BackpackInspectFillCollectionIds(cardIds);
        cardIds.Sort();
    }

    private Card ResolveCard(int id)
    {
        Card c = host.BackpackInspectGetCard(id);
        if (c != null) return c;
        if (sourceDisplay != null && sourceDisplay.card != null && sourceDisplay.card.id == id)
            return sourceDisplay.card;
        return currentCard != null && currentCard.id == id ? currentCard : null;
    }

    private void ApplyTypographyFonts(TMP_FontAsset font)
    {
        ApplyFont(titleTmp, font);
        ApplyFont(subtitleTmp, font);
        ApplyFont(typeTmp, font);
        ApplyFont(deckBarTmp, font);
        ApplyFont(masteryLabelTmp, font);
        ApplyFont(masteryStatusTmp, font);
        for (int i = 0; i < statChipTmps.Length; i++)
            ApplyFont(statChipTmps[i], font);
        ApplyFont(skillTmp, font);
        ApplyFont(pageTmp, font);
        ApplyFont(hintTmp, font);
        for (int i = 0; i < stageTabLabelTmps.Length; i++)
            ApplyFont(stageTabLabelTmps[i], font);
    }

    private static void ApplyFont(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (tmp == null || font == null) return;
        tmp.font = font;
        tmp.outlineWidth = 0f;
    }

    private void EnsureUi(Canvas canvas)
    {
        if (root != null && uiCanvas == canvas && uiBuildGeneration == UiBuildGeneration) return;
        DestroyUi();
        BuildUi(canvas);
    }

    private void DestroyUi()
    {
        if (root != null) Destroy(root);
        root = null;
        uiCanvas = null;
        panelRt = null;
        artImage = null;
        titleTmp = subtitleTmp = typeTmp = deckBarTmp = null;
        masteryLabelTmp = masteryStatusTmp = null;
        masteryFillRt = null;
        masteryBarRt = null;
        for (int i = 0; i < statChipTmps.Length; i++)
            statChipTmps[i] = null;
        skillTmp = null;
        infoScroll = null;
        infoContentRt = null;
        headerLeftRt = headerRightRt = deckBarRt = null;
        statStripRt = skillSectionRt = null;
        pageTmp = hintTmp = null;
        uiBuildGeneration = 0;
    }

    private void BuildUi(Canvas canvas)
    {
        uiCanvas = canvas;
        uiBuildGeneration = UiBuildGeneration;
        TMP_FontAsset font = host.BackpackInspectResolveFont();
        if (font == null) font = TMP_Settings.defaultFontAsset;

        PurgeLegacyRoots();

        root = new GameObject("BackpackCardInspectRoot", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(canvas.transform, false);
        Stretch(root.GetComponent<RectTransform>(), 0, 0, 0, 0);

        Canvas overlay = root.GetComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = canvas.sortingOrder + 250;
        overlay.renderMode = canvas.renderMode;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            overlay.worldCamera = canvas.worldCamera;

        Image dim = CreateChild(root.transform, "Dim", true);
        Stretch(dim.rectTransform, 0, 0, 0, 0);
        dim.color = BackpackInspectUiColors.Dim;
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(Hide);
        AttachSwipeRelay(dim.gameObject);

        Image panelImg = CreateChild(root.transform, "Panel", true);
        panelRt = panelImg.rectTransform;
        panelRt.anchorMin = new Vector2(0.03f, 0.05f);
        panelRt.anchorMax = new Vector2(0.97f, 0.95f);
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
        panelImg.color = BackpackInspectUiColors.PagePaper;

        BuildArtColumn(panelRt, font);
        BuildInfoColumn(panelRt, font);
        BuildFooterHints(panelRt, font);

        root.SetActive(false);
    }

    private void BuildArtColumn(RectTransform panel, TMP_FontAsset font)
    {
        RectTransform artRegion = CreateRect(panel, "ArtRegion");
        artRegion.anchorMin = Vector2.zero;
        artRegion.anchorMax = new Vector2(ArtAnchorMax, 1f);
        artRegion.offsetMin = new Vector2(20f, 24f);
        artRegion.offsetMax = new Vector2(-8f, -24f);

        Image swipeCapture = CreateChild(artRegion, "SwipeCapture", true);
        Stretch(swipeCapture.rectTransform, 0, 0, 0, 0);
        swipeCapture.color = new Color(1f, 1f, 1f, 0.001f);
        AttachSwipeRelay(swipeCapture.gameObject);
        swipeCapture.transform.SetAsFirstSibling();

        Image artWell = CreateChild(artRegion, "ArtWell", false);
        Stretch(artWell.rectTransform, 0, 0, 0, 0);
        artWell.color = BackpackInspectUiColors.ArtWellWash;
        artWell.transform.SetAsFirstSibling();

        Image backBtnImg = CreateChild(artRegion, "BackButton", true);
        RectTransform backRt = backBtnImg.rectTransform;
        backRt.anchorMin = backRt.anchorMax = new Vector2(0f, 1f);
        backRt.pivot = new Vector2(0f, 1f);
        backRt.anchoredPosition = new Vector2(8f, -8f);
        backRt.sizeDelta = new Vector2(96f, 44f);
        backBtnImg.color = BackpackInspectUiColors.BtnBack;
        Button backBtn = backBtnImg.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = backBtnImg;
        backBtn.onClick.AddListener(Hide);
        TextMeshProUGUI backLabel = CreateText(backRt, "返回", font, BackpackInspectVisualStyle.Typography.HintSize,
            FontStyles.Bold, BackpackInspectUiColors.BtnBackText, TextAlignmentOptions.Center);
        Stretch(backLabel.rectTransform, 0, 0, 0, 0);

        Image artFrame = CreateChild(artRegion, "ArtFrame", false);
        Stretch(artFrame.rectTransform, 0, 0, 0, 0);
        artFrame.color = BackpackInspectUiColors.ArtFrame;

        artImage = CreateChild(artFrame.rectTransform, "Art", false);
        Stretch(artImage.rectTransform, 10, 10, 10, 10);
        artImage.preserveAspect = true;
        artImage.raycastTarget = false;
    }

    private void BuildInfoColumn(RectTransform panel, TMP_FontAsset font)
    {
        Image infoBg = CreateChild(panel, "InfoRegion", false);
        RectTransform infoRt = infoBg.rectTransform;
        infoRt.anchorMin = new Vector2(ArtAnchorMax, 0f);
        infoRt.anchorMax = Vector2.one;
        infoRt.offsetMin = new Vector2(12f, 24f);
        infoRt.offsetMax = new Vector2(-20f, -24f);
        infoBg.color = BackpackInspectUiColors.PagePaper;

        GameObject scrollGo = new GameObject("InfoScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(infoRt, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        Stretch(scrollRt, 0, 0, 0, 0);

        infoScroll = scrollGo.GetComponent<ScrollRect>();
        infoScroll.horizontal = false;
        infoScroll.vertical = true;
        infoScroll.movementType = ScrollRect.MovementType.Clamped;
        infoScroll.scrollSensitivity = 28f;

        RectTransform viewport = CreateRect(scrollRt, "Viewport");
        Stretch(viewport, 0, 0, 0, 0);
        Image viewportHit = viewport.gameObject.GetComponent<Image>();
        if (viewportHit == null) viewportHit = viewport.gameObject.AddComponent<Image>();
        viewportHit.color = new Color(0f, 0f, 0f, 0.001f);
        viewportHit.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        infoScroll.viewport = viewport;
        AttachSwipeRelay(viewport.gameObject, infoScroll);

        infoContentRt = CreateRect(viewport, "Content");
        infoContentRt.anchorMin = new Vector2(0f, 1f);
        infoContentRt.anchorMax = new Vector2(1f, 1f);
        infoContentRt.pivot = new Vector2(0.5f, 1f);
        infoContentRt.sizeDelta = new Vector2(0f, 1200f);
        infoScroll.content = infoContentRt;
        AttachSwipeRelay(infoContentRt.gameObject, infoScroll);

        headerLeftRt = CreateContentBand(infoContentRt, "HeaderLeft", 0f, HeaderLeftAnchorMax);
        headerRightRt = CreateContentBand(infoContentRt, "HeaderRight", HeaderRightAnchorMin, 1f);
        deckBarRt = CreateContentBand(infoContentRt, "DeckBar", 0f, 1f);
        statStripRt = CreateContentBand(infoContentRt, "StatStrip", 0f, 1f);
        masteryBarRt = CreateContentBand(infoContentRt, "MasteryBar", 0f, 1f);
        skillSectionRt = CreateContentBand(infoContentRt, "SkillSection", 0f, 1f);

        titleTmp = CreateText(headerLeftRt, string.Empty, font, BackpackInspectVisualStyle.Typography.MainTitleSize,
            FontStyles.Bold, BackpackInspectUiColors.MainTitle, TextAlignmentOptions.TopLeft);
        BackpackInspectVisualStyle.AddTmpShadow(titleTmp, BackpackInspectUiColors.WithAlpha(BackpackInspectUiColors.Ink, 0.35f),
            new Vector2(1.5f, -1.5f));

        subtitleTmp = CreateText(headerLeftRt, string.Empty, font, BackpackInspectVisualStyle.Typography.SubtitleSize,
            FontStyles.Normal, BackpackInspectUiColors.InkSoft, TextAlignmentOptions.TopLeft);

        typeTmp = CreateText(headerLeftRt, string.Empty, font, BackpackInspectVisualStyle.Typography.BodySize,
            FontStyles.Normal, BackpackInspectUiColors.InkMuted, TextAlignmentOptions.TopLeft);

        deckBarTmp = CreateText(deckBarRt, string.Empty, font, BackpackInspectVisualStyle.Typography.BodySize,
            FontStyles.Normal, BackpackInspectUiColors.Ink, TextAlignmentOptions.MidlineLeft);
        Stretch(deckBarTmp.rectTransform, 16, 0, 16, 0);

        BuildStatChips(statStripRt, font);
        BuildMasteryBar(masteryBarRt, font);
        BuildStageTabs(headerRightRt, font);

        Image skillBg = CreateChild(skillSectionRt, "SkillBg", false);
        Stretch(skillBg.rectTransform, 0, 0, 0, 0);
        skillBg.color = BackpackInspectUiColors.PanelSkill;

        skillTmp = CreateText(skillSectionRt, string.Empty, font, BackpackInspectVisualStyle.Typography.BodySize,
            FontStyles.Normal, BackpackInspectUiColors.InkOnSkill, TextAlignmentOptions.TopLeft);
        skillTmp.richText = true;
    }

    private void BuildStatChips(RectTransform parent, TMP_FontAsset font)
    {
        Image stripBg = CreateChild(parent, "StatsStripBg", false);
        Stretch(stripBg.rectTransform, 0, 0, 0, 0);
        stripBg.color = BackpackInspectUiColors.StatStripBg;

        GameObject rowGo = new GameObject("StatChipsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(parent, false);
        RectTransform rowRt = rowGo.GetComponent<RectTransform>();
        Stretch(rowRt, StatStripPadH, StatStripPadV, StatStripPadH, StatStripPadV);

        HorizontalLayoutGroup hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = StatChipSpacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        string[] labels = { "攻擊力", "生命值", "持有數", "稀有度" };
        for (int i = 0; i < labels.Length; i++)
        {
            GameObject chipGo = new GameObject($"StatChip{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            chipGo.transform.SetParent(rowRt, false);
            LayoutElement chipLe = chipGo.GetComponent<LayoutElement>();
            chipLe.flexibleWidth = 1f;
            chipLe.minHeight = StatStripHeight - StatStripPadV * 2f;

            Image chipBg = chipGo.GetComponent<Image>();
            chipBg.color = BackpackInspectUiColors.StatChipBg;
            chipBg.raycastTarget = false;

            statChipTmps[i] = CreateText(chipGo.transform, labels[i], font, BackpackInspectVisualStyle.Typography.BodySize,
                FontStyles.Normal, BackpackInspectUiColors.Ink, TextAlignmentOptions.Center);
            Stretch(statChipTmps[i].rectTransform, 4, 4, 4, 4);
            statChipTmps[i].richText = false;
        }
    }

    private static RectTransform CreateContentBand(RectTransform parent, string name, float anchorMinX, float anchorMaxX)
    {
        RectTransform band = CreateRect(parent, name);
        band.anchorMin = new Vector2(anchorMinX, 1f);
        band.anchorMax = new Vector2(anchorMaxX, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.anchoredPosition = Vector2.zero;
        band.sizeDelta = new Vector2(0f, 200f);
        return band;
    }

    private void BuildMasteryBar(RectTransform parent, TMP_FontAsset font)
    {
        Image barBg = CreateChild(parent, "BarBg", false);
        Stretch(barBg.rectTransform, 0, 0, 0, 0);
        barBg.color = BackpackInspectUiColors.ProficiencyBg;

        float headerBottom = MasteryInset + MasteryHeaderHeight;

        Image track = CreateChild(barBg.rectTransform, "Track", false);
        RectTransform trackRt = track.rectTransform;
        trackRt.anchorMin = new Vector2(0f, 0f);
        trackRt.anchorMax = new Vector2(1f, 0f);
        trackRt.pivot = new Vector2(0.5f, 0f);
        trackRt.offsetMin = new Vector2(MasteryInset, MasteryInset);
        trackRt.offsetMax = new Vector2(-MasteryInset, MasteryInset + MasteryTrackHeight);
        track.color = BackpackInspectUiColors.ProficiencyTrack;

        Image fill = CreateChild(track.rectTransform, "Fill", false);
        masteryFillRt = fill.rectTransform;
        masteryFillRt.anchorMin = Vector2.zero;
        masteryFillRt.anchorMax = new Vector2(0.7f, 1f);
        masteryFillRt.offsetMin = masteryFillRt.offsetMax = Vector2.zero;
        fill.color = BackpackInspectUiColors.ProficiencyFill;

        masteryLabelTmp = CreateText(barBg.rectTransform, "怪物牌 熟練度", font, BackpackInspectVisualStyle.Typography.BodySize,
            FontStyles.Bold, BackpackInspectUiColors.ProficiencyLabel, TextAlignmentOptions.TopLeft);
        PlaceMasteryHeaderText(masteryLabelTmp.rectTransform, true, headerBottom, rightReservePx: 0f);

        masteryStatusTmp = CreateText(barBg.rectTransform, string.Empty, font, BackpackInspectVisualStyle.Typography.BodySize,
            FontStyles.Bold, BackpackInspectUiColors.ProficiencyStatus, TextAlignmentOptions.TopRight);
        PlaceMasteryHeaderText(masteryStatusTmp.rectTransform, false, headerBottom, rightReservePx: MasteryStatusRightReservePx);

        Image helpImg = CreateChild(barBg.rectTransform, "ProficiencyHelpButton", true);
        RectTransform helpRt = helpImg.rectTransform;
        helpRt.anchorMin = helpRt.anchorMax = new Vector2(1f, 1f);
        helpRt.pivot = new Vector2(1f, 1f);
        helpRt.anchoredPosition = new Vector2(-MasteryInset, -MasteryInset);
        helpRt.sizeDelta = new Vector2(MasteryHelpButtonSizePx, MasteryHelpButtonSizePx);
        helpImg.color = BackpackInspectUiColors.ProficiencyFill;
        Outline helpOutline = helpImg.gameObject.AddComponent<Outline>();
        helpOutline.effectColor = BackpackInspectUiColors.Ink;
        helpOutline.effectDistance = new Vector2(1.5f, -1.5f);
        Button helpBtn = helpImg.gameObject.AddComponent<Button>();
        helpBtn.targetGraphic = helpImg;
        helpBtn.onClick.AddListener(ShowProficiencyHelp);
        TextMeshProUGUI helpLbl = CreateText(helpRt, "?", font, 30, FontStyles.Bold,
            BackpackInspectUiColors.Ink, TextAlignmentOptions.Center);
        Stretch(helpLbl.rectTransform, 0, 0, 0, 0);

        masteryLabelTmp.transform.SetAsLastSibling();
        masteryStatusTmp.transform.SetAsLastSibling();
        helpImg.transform.SetAsLastSibling();
    }

    private void ShowProficiencyHelp()
    {
        if (host == null) return;
        host.ShowBackpackProficiencyHelp();
    }

    private static void PlaceMasteryHeaderText(RectTransform rt, bool alignLeft, float headerBottom, float rightReservePx)
    {
        if (rt == null) return;

        TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        float rightPad = MasteryInset + (alignLeft ? 0f : rightReservePx);
        const float masteryHeaderSplit = 0.36f;
        rt.anchorMin = new Vector2(alignLeft ? 0f : masteryHeaderSplit, 1f);
        rt.anchorMax = new Vector2(alignLeft ? masteryHeaderSplit : 1f, 1f);
        rt.pivot = new Vector2(alignLeft ? 0f : 1f, 1f);
        rt.offsetMin = new Vector2(alignLeft ? MasteryInset : 0f, -headerBottom);
        rt.offsetMax = new Vector2(alignLeft ? 0f : -rightPad, -MasteryInset);
    }

    private void BuildStageTabs(RectTransform parent, TMP_FontAsset font)
    {
        CardSkillRevealStage[] stages =
        {
            CardSkillRevealStage.LockedA,
            CardSkillRevealStage.BasicB,
            CardSkillRevealStage.FullC
        };
        string[] labels = { "A 階段", "B 階段", "C 階段" };
        float[] anchors = { 0.02f, 0.35f, 0.68f };

        for (int i = 0; i < 3; i++)
        {
            Image tabBg = CreateChild(parent, $"StageTab{i}", true);
            RectTransform tabRt = tabBg.rectTransform;
            tabRt.anchorMin = new Vector2(anchors[i], 0.5f);
            tabRt.anchorMax = new Vector2(anchors[i] + 0.28f, 0.5f);
            tabRt.pivot = new Vector2(0f, 0.5f);
            tabRt.sizeDelta = new Vector2(0f, 76f);
            tabBg.color = BackpackInspectUiColors.TabIdleBg;
            stageTabBgImages[i] = tabBg;

            Button btn = tabBg.gameObject.AddComponent<Button>();
            btn.targetGraphic = tabBg;
            CardSkillRevealStage captured = stages[i];
            btn.onClick.AddListener(() => SelectPreviewStage(captured));

            stageTabLabelTmps[i] = CreateText(tabRt, labels[i], font, BackpackInspectVisualStyle.Typography.BodySize,
                FontStyles.Bold, BackpackInspectUiColors.TabIdleText, TextAlignmentOptions.Center);
            Stretch(stageTabLabelTmps[i].rectTransform, 4, 4, 4, 4);
        }
    }

    private void BuildFooterHints(RectTransform panel, TMP_FontAsset font)
    {
        pageTmp = CreateText(panel, string.Empty, font, BackpackInspectVisualStyle.Typography.HintSize,
            FontStyles.Normal, BackpackInspectVisualStyle.Typography.Hint, TextAlignmentOptions.Center);
        RectTransform pageRt = pageTmp.rectTransform;
        pageRt.anchorMin = pageRt.anchorMax = new Vector2(0.5f, 0f);
        pageRt.pivot = new Vector2(0.5f, 0f);
        pageRt.anchoredPosition = new Vector2(0f, 10f);
        pageRt.sizeDelta = new Vector2(220f, 28f);

        hintTmp = CreateText(panel, string.Empty, font, BackpackInspectVisualStyle.Typography.HintSize,
            FontStyles.Italic, BackpackInspectVisualStyle.Typography.Hint, TextAlignmentOptions.Center);
        RectTransform hintRt = hintTmp.rectTransform;
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 42f);
        hintRt.sizeDelta = new Vector2(440f, 34f);
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string text,
        TMP_FontAsset font,
        int size,
        FontStyles style,
        Color color,
        TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        ApplyFont(tmp, font);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        return tmp;
    }

    private static void PurgeLegacyRoots()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && (t.name == "BackpackCardInspectRoot" || t.name == "BackpackInspectFloatingPanel"))
                Object.Destroy(t.gameObject);
        }
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image CreateChild(Transform parent, string name, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = raycast;
        return img;
    }

    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private void AttachSwipeRelay(GameObject go, ScrollRect scrollToSuspend = null)
    {
        if (go == null) return;
        SwipeRelay relay = go.GetComponent<SwipeRelay>();
        if (relay == null) relay = go.AddComponent<SwipeRelay>();
        relay.panel = this;
        relay.scrollToSuspend = scrollToSuspend;
    }

    /// <summary>水平滑動切換收藏中的上一張／下一張卡牌。</summary>
    private sealed class SwipeRelay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public BackpackCardInspectPanel panel;
        public ScrollRect scrollToSuspend;

        private Vector2 _start;
        private bool _horizontalNav;
        private bool _scrollWasEnabled = true;

        private const float HorizontalIntentPx = 14f;
        private const float HorizontalDominanceRatio = 1.2f;

        public void OnBeginDrag(PointerEventData e)
        {
            _start = e != null ? e.position : Vector2.zero;
            _horizontalNav = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (panel == null || e == null || _horizontalNav) return;

            Vector2 delta = e.position - _start;
            if (Mathf.Abs(delta.x) < HorizontalIntentPx) return;
            if (Mathf.Abs(delta.x) <= Mathf.Abs(delta.y) * HorizontalDominanceRatio) return;

            _horizontalNav = true;
            if (scrollToSuspend != null)
            {
                _scrollWasEnabled = scrollToSuspend.enabled;
                scrollToSuspend.enabled = false;
            }
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (scrollToSuspend != null)
                scrollToSuspend.enabled = _scrollWasEnabled;

            if (panel == null || e == null) return;

            float dragDeltaX = e.position.x - _start.x;
            if (scrollToSuspend == null)
            {
                panel.OnSwipe(dragDeltaX);
                return;
            }

            if (_horizontalNav)
                panel.OnSwipe(dragDeltaX);

            _horizontalNav = false;
        }
    }
}

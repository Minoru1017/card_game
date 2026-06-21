using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>背包卡牌詳情：例圖式版面（立繪／標題／階段分頁／數值列／熟練度／戰技單欄）。</summary>
[DisallowMultipleComponent]
public partial class BackpackCardInspectPanel : MonoBehaviour
{
    // MASTER INDEX
    // Main (this file): fields, Show/Hide, swipe navigation, stage tab API.
    // Layout: ApplyCard, stat/mastery/skill binding, scroll layout coroutines, typography.
    // UiBuild: EnsureUi/BuildUi, column builders, Create* helpers, SwipeRelay.

    private const int UiBuildGeneration = 24;

    private struct SkillLayoutCache
    {
        public int cardId;
        public CardSkillRevealStage stage;
        public float columnWidth;
        public float preferredH;
        public float contentH;
        public float sectionH;
        public bool scrollActive;
        public bool valid;
    }

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
    private const float SkillScrollPadV = 12f;
    private const float SkillScrollMinMaxHeight = 120f;
    private const float SkillScrollVisibleMax = 240f;
    private const float SkillScrollMaxHeightCap = 320f;

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
    private ScrollRect skillScroll;
    private RectTransform skillScrollContentRt;
    private bool skillScrollActive;
    private SkillLayoutCache skillLayoutCache;
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
            InvalidateSkillLayoutCache();
            StartCoroutine(CoRefreshInfoScrollLayout());
        }
        RefreshStageTabVisuals();
    }
}

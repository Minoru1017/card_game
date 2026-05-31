using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>教學對戰專用勝利／失敗結算介面。</summary>
public sealed class TutorialBattleSettlementUi : MonoBehaviour
{
    private const float VictoryPanelWidth = 720f;
    private const float VictoryPanelHeight = 560f;
    private const float DefeatPanelWidth = 560f;
    private const float DefeatPanelHeight = 340f;
    private const float VictoryCardCellWidth = 168f;
    private const float VictoryCardCellHeight = 246f;
    private const float CardRevealStaggerSeconds = 0.14f;
    private const float CardRevealDurationSeconds = 0.42f;
    private const float PanelPopDurationSeconds = 0.38f;

    private sealed class VictoryRewardCardSlot
    {
        public RectTransform Root;
        public CanvasGroup CanvasGroup;
    }

    private BattleSimulationManager _manager;
    private Transform _canvasRoot;
    private TMP_FontAsset _font;
    private GameObject _cardPrefab;
    private GameObject _overlayRoot;
    private GameObject _panelRoot;
    private RectTransform _panelRt;
    private CanvasGroup _panelCanvasGroup;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _bodyText;
    private RectTransform _bodyRt;
    private GameObject _cardRewardSection;
    private RectTransform _cardRowRt;
    private RectTransform _buttonRowRt;
    private readonly List<VictoryRewardCardSlot> _victoryCardSlots = new List<VictoryRewardCardSlot>();
    private readonly List<TutorialBattleRewardService.VictoryCardPreview> _victoryPreviews =
        new List<TutorialBattleRewardService.VictoryCardPreview>();
    private bool _uiBuilt;
    private bool _showing;
    private bool _grantIntroTrioOnContinue;
    private bool _eventsBound;
    private Coroutine _showRoutine;
    private Coroutine _victoryAnimRoutine;

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsIntroTutorialBattle;

    public void Initialize(
        BattleSimulationManager manager,
        Transform canvasRoot,
        TMP_FontAsset font = null,
        GameObject cardPrefab = null)
    {
        _manager = manager;
        _canvasRoot = canvasRoot;
        _font = font;
        _cardPrefab = cardPrefab;
        if (_manager != null && !_eventsBound)
        {
            _manager.BattleEnded += OnBattleEnded;
            _eventsBound = true;
        }
    }

    private void OnDestroy()
    {
        if (_eventsBound && _manager != null)
            _manager.BattleEnded -= OnBattleEnded;
        _eventsBound = false;
    }

    private void OnBattleEnded(int result)
    {
        if (!IsActiveForCurrentBattle || BattleAutoSimPlugin.IsRunning) return;
        if (_showing) return;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(CoShowSettlement(result));
    }

    private IEnumerator CoShowSettlement(int result)
    {
        _showing = true;
        yield return null;
        yield return new WaitForEndOfFrame();

        if (result == -1)
            yield return new WaitForSecondsRealtime(0.45f);

        EnsureUi();
        if (_overlayRoot == null || _panelRoot == null)
        {
            _showing = false;
            _showRoutine = null;
            yield break;
        }

        HideTutorialCoach();
        bool won = result == 1;
        ApplyContent(won);
        _overlayRoot.SetActive(true);
        _overlayRoot.transform.SetAsLastSibling();

        if (won)
        {
            TutorialBattleVictorySfx.PlayIfIntroTutorialBattle();
            if (_victoryAnimRoutine != null)
                StopCoroutine(_victoryAnimRoutine);
            _victoryAnimRoutine = StartCoroutine(CoPlayVictoryPresentation());
        }
        else
        {
            TutorialBattleDefeatSfx.PlayIfIntroTutorialBattle();
            ResetPanelForInstantShow();
        }

        _showRoutine = null;
    }

    private static void HideTutorialCoach()
    {
        TutorialBattleCoachUi coach = Object.FindFirstObjectByType<TutorialBattleCoachUi>();
        coach?.HideForSettlement();
    }

    private void ApplyContent(bool won)
    {
        ClearButtons();
        ClearVictoryCards();
        if (_titleText == null || _bodyText == null) return;

        CardStore store = _manager != null ? _manager.cardStore : null;
        if (store == null)
            store = Object.FindFirstObjectByType<CardStore>();

        _grantIntroTrioOnContinue = won && TutorialBattleRewardService.ShouldGrantIntroTrioForActivePlayer();

        if (_cardRewardSection != null)
            _cardRewardSection.SetActive(won && _grantIntroTrioOnContinue);

        if (won)
        {
            _titleText.text = "教學對戰勝利";
            _bodyText.gameObject.SetActive(true);
            CreateFooterButton("繼續", true, OnClickVictoryContinue);

            if (_grantIntroTrioOnContinue)
            {
                ApplyVictoryPanelLayout();
                ApplyVictoryBodyLayout();
                _bodyText.text = "獲得卡牌";
                TutorialBattleRewardService.FillVictoryCardPreviews(store, _victoryPreviews);
                BuildVictoryCardSlots();
                SetFooterButtonsVisible(false);
            }
            else
            {
                ApplyReplayVictoryPanelLayout();
                _bodyText.text = "入門試煉通過\n\n御三家已於首次通關時發入收藏";
                ClearVictoryCards();
                SetFooterButtonsVisible(false);
            }
        }
        else
        {
            ApplyDefeatPanelLayout();
            ApplyDefeatBodyLayout();
            _titleText.text = "教學對戰失敗";
            _bodyText.text = "英雄生命歸零就輸了\n\n再試一次或返回劇情";
            CreateFooterButton("再試一次", true, OnClickRetry);
            CreateFooterButton("返回劇情", false, OnClickReturnStory);
            SetFooterButtonsVisible(true);
        }
    }

    private void ApplyVictoryPanelLayout()
    {
        if (_panelRt == null) return;
        _panelRt.sizeDelta = new Vector2(VictoryPanelWidth, VictoryPanelHeight);
    }

    private void ApplyDefeatPanelLayout()
    {
        if (_panelRt == null) return;
        _panelRt.sizeDelta = new Vector2(DefeatPanelWidth, DefeatPanelHeight);
    }

    private void ApplyReplayVictoryPanelLayout()
    {
        ApplyDefeatPanelLayout();
        ApplyDefeatBodyLayout();
    }

    private void ApplyVictoryBodyLayout()
    {
        if (_bodyRt == null || _bodyText == null) return;
        _bodyRt.anchorMin = new Vector2(0f, 0f);
        _bodyRt.anchorMax = new Vector2(1f, 0f);
        _bodyRt.pivot = new Vector2(0.5f, 0f);
        _bodyRt.sizeDelta = new Vector2(0f, 44f);
        _bodyRt.anchoredPosition = new Vector2(0f, 92f);
        _bodyText.fontSize = 24f;
        _bodyText.lineSpacing = 8f;
        _bodyText.alignment = TextAlignmentOptions.Center;
        _bodyText.enableWordWrapping = false;
    }

    private void ApplyDefeatBodyLayout()
    {
        if (_bodyRt == null || _bodyText == null) return;
        _bodyRt.anchorMin = new Vector2(0f, 0f);
        _bodyRt.anchorMax = new Vector2(1f, 1f);
        _bodyRt.pivot = new Vector2(0.5f, 0.5f);
        _bodyRt.anchoredPosition = Vector2.zero;
        _bodyRt.sizeDelta = Vector2.zero;
        _bodyRt.offsetMin = new Vector2(28f, 96f);
        _bodyRt.offsetMax = new Vector2(-28f, -108f);
        _bodyText.fontSize = 26f;
        _bodyText.lineSpacing = 12f;
        _bodyText.alignment = TextAlignmentOptions.Center;
        _bodyText.enableWordWrapping = true;
    }

    private IEnumerator CoPlayVictoryPresentation()
    {
        ResetPanelForPopIn();
        PrepareVictoryCardsHidden();

        float t = 0f;
        while (t < PanelPopDurationSeconds)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / PanelPopDurationSeconds);
            float eased = EaseOutBack(p);
            if (_panelCanvasGroup != null)
                _panelCanvasGroup.alpha = eased;
            if (_panelRt != null)
                _panelRt.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, eased);
            yield return null;
        }

        if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f;
        if (_panelRt != null) _panelRt.localScale = Vector3.one;

        if (_victoryCardSlots.Count > 0)
        {
            for (int i = 0; i < _victoryCardSlots.Count; i++)
            {
                yield return AnimateVictoryCardReveal(_victoryCardSlots[i]);
                if (i < _victoryCardSlots.Count - 1)
                    yield return new WaitForSecondsRealtime(CardRevealStaggerSeconds);
            }

            yield return new WaitForSecondsRealtime(0.2f);
            if (_bodyText != null)
                _bodyText.text = "已加入背包";
        }

        SetFooterButtonsVisible(true);
        AnimateFooterButtonsIn();
        _victoryAnimRoutine = null;
    }

    private IEnumerator AnimateVictoryCardReveal(VictoryRewardCardSlot slot)
    {
        if (slot?.Root == null) yield break;

        float t = 0f;
        while (t < CardRevealDurationSeconds)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / CardRevealDurationSeconds);
            float eased = EaseOutBack(p);
            if (slot.CanvasGroup != null)
                slot.CanvasGroup.alpha = eased;
            slot.Root.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, eased);
            float lift = Mathf.Lerp(-28f, 0f, eased);
            slot.Root.anchoredPosition = new Vector2(slot.Root.anchoredPosition.x, lift);
            yield return null;
        }

        if (slot.CanvasGroup != null) slot.CanvasGroup.alpha = 1f;
        slot.Root.localScale = Vector3.one;
        slot.Root.anchoredPosition = new Vector2(slot.Root.anchoredPosition.x, 0f);
    }

    private void PrepareVictoryCardsHidden()
    {
        for (int i = 0; i < _victoryCardSlots.Count; i++)
        {
            VictoryRewardCardSlot slot = _victoryCardSlots[i];
            if (slot?.Root == null) continue;
            if (slot.CanvasGroup != null) slot.CanvasGroup.alpha = 0f;
            slot.Root.localScale = Vector3.one * 0.5f;
            slot.Root.anchoredPosition = new Vector2(slot.Root.anchoredPosition.x, -28f);
        }
    }

    private void ResetPanelForPopIn()
    {
        if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 0f;
        if (_panelRt != null) _panelRt.localScale = Vector3.one * 0.88f;
        if (_bodyText != null && _grantIntroTrioOnContinue)
            _bodyText.text = "獲得卡牌";
        SetFooterButtonsVisible(false);
    }

    private void ResetPanelForInstantShow()
    {
        if (_panelCanvasGroup != null) _panelCanvasGroup.alpha = 1f;
        if (_panelRt != null) _panelRt.localScale = Vector3.one;
    }

    private void SetFooterButtonsVisible(bool visible)
    {
        if (_buttonRowRt == null) return;
        CanvasGroup cg = _buttonRowRt.GetComponent<CanvasGroup>();
        if (cg == null) cg = _buttonRowRt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }

    private void AnimateFooterButtonsIn()
    {
        if (_buttonRowRt == null) return;
        CanvasGroup cg = _buttonRowRt.GetComponent<CanvasGroup>();
        if (cg == null) return;
        cg.alpha = 1f;
        _buttonRowRt.localScale = Vector3.one;
    }

    private void BuildVictoryCardSlots()
    {
        if (_cardRowRt == null) return;

        for (int i = 0; i < _victoryPreviews.Count; i++)
            CreateVictoryCardSlot(_victoryPreviews[i]);
    }

    private void CreateVictoryCardSlot(TutorialBattleRewardService.VictoryCardPreview preview)
    {
        GameObject slotObj = new GameObject(
            "RewardCard_" + preview.CardId,
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(LayoutElement));
        slotObj.transform.SetParent(_cardRowRt, false);

        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        LayoutElement le = slotObj.GetComponent<LayoutElement>();
        le.preferredWidth = VictoryCardCellWidth;
        le.preferredHeight = VictoryCardCellHeight;
        le.minWidth = VictoryCardCellWidth;
        le.minHeight = VictoryCardCellHeight;

        CanvasGroup slotCg = slotObj.GetComponent<CanvasGroup>();

        if (preview.Card != null && _cardPrefab != null)
        {
            GameObject cardGo = Object.Instantiate(_cardPrefab, slotObj.transform);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = Vector2.zero;
                cardRt.localScale = Vector3.one;
                cardRt.sizeDelta = new Vector2(VictoryCardCellWidth, VictoryCardCellHeight);
            }

            CardDisplay display = cardGo.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.SetCard(preview.Card);
                HideCombatStatsForRewardPreview(display);
            }
        }
        else
        {
            BuildFallbackCardVisual(slotObj.transform, preview);
        }

        _victoryCardSlots.Add(new VictoryRewardCardSlot { Root = slotRt, CanvasGroup = slotCg });
    }

    private void BuildFallbackCardVisual(Transform parent, TutorialBattleRewardService.VictoryCardPreview preview)
    {
        GameObject frameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(parent, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = Vector2.zero;
        frameRt.offsetMax = Vector2.zero;
        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.color = BattleUiColors.PanelScroll;

        GameObject artObj = new GameObject("Art", typeof(RectTransform), typeof(Image));
        artObj.transform.SetParent(frameObj.transform, false);
        RectTransform artRt = artObj.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0.08f, 0.14f);
        artRt.anchorMax = new Vector2(0.92f, 0.88f);
        artRt.offsetMin = Vector2.zero;
        artRt.offsetMax = Vector2.zero;
        Image artImg = artObj.GetComponent<Image>();
        artImg.preserveAspect = true;
        artImg.sprite = preview.ArtSprite;
        artImg.color = preview.ArtSprite != null ? Color.white : BattleUiColors.PanelEdge35;

        GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(parent, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0f);
        nameRt.anchorMax = new Vector2(1f, 0f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.sizeDelta = new Vector2(0f, 32f);
        nameRt.anchoredPosition = new Vector2(0f, -36f);
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(nameTmp);
        nameTmp.text = preview.DisplayName;
        nameTmp.fontSize = 20f;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = BattleUiColors.Ink;
    }

    private static void HideCombatStatsForRewardPreview(CardDisplay display)
    {
        if (display == null) return;
        if (display.attackText != null) display.attackText.gameObject.SetActive(false);
        if (display.healthText != null) display.healthText.gameObject.SetActive(false);
        if (display.effectText != null) display.effectText.gameObject.SetActive(false);
    }

    private void OnClickVictoryContinue()
    {
        if (_grantIntroTrioOnContinue)
            TutorialBattleRewardService.TryGrantIntroTrioReward();
        HideOverlay();
        StoryProgressSession.LaunchTutorialPlotEpilogueAfterVictory();
    }

    private void OnClickRetry()
    {
        HideOverlay();
        StoryProgressBattleReturn.RetryIntroTutorialBattle();
    }

    private void OnClickReturnStory()
    {
        HideOverlay();
        StoryProgressBattleReturn.CompleteReturnToStoryProgress(won: false);
    }

    private void HideOverlay()
    {
        _showing = false;
        if (_victoryAnimRoutine != null)
        {
            StopCoroutine(_victoryAnimRoutine);
            _victoryAnimRoutine = null;
        }
        if (_overlayRoot != null) _overlayRoot.SetActive(false);
    }

    private void EnsureUi()
    {
        if (_uiBuilt) return;
        if (_canvasRoot == null) return;

        _overlayRoot = new GameObject("TutorialBattleSettlementOverlay", typeof(RectTransform));
        _overlayRoot.transform.SetParent(_canvasRoot, false);
        RectTransform overlayRt = _overlayRoot.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        GameObject dimObj = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimObj.transform.SetParent(_overlayRoot.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = BattleUiColors.DimHeavy;
        dimImg.raycastTarget = true;

        _panelRoot = new GameObject(
            "TutorialSettlementPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup));
        _panelRoot.transform.SetParent(_overlayRoot.transform, false);
        _panelRt = _panelRoot.GetComponent<RectTransform>();
        _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRt.pivot = new Vector2(0.5f, 0.5f);
        _panelRt.sizeDelta = new Vector2(VictoryPanelWidth, VictoryPanelHeight);
        _panelRt.anchoredPosition = Vector2.zero;
        _panelCanvasGroup = _panelRoot.GetComponent<CanvasGroup>();

        Image panelBg = _panelRoot.GetComponent<Image>();
        panelBg.color = BattleUiColors.PanelCream96;
        panelBg.raycastTarget = true;
        Outline outline = _panelRoot.GetComponent<Outline>();
        outline.effectColor = BattleUiColors.PanelEdge35;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject stripObj = new GameObject("HeaderStrip", typeof(RectTransform), typeof(Image));
        stripObj.transform.SetParent(_panelRoot.transform, false);
        RectTransform stripRt = stripObj.GetComponent<RectTransform>();
        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.sizeDelta = new Vector2(0f, 96f);
        stripRt.anchoredPosition = Vector2.zero;
        Image stripImg = stripObj.GetComponent<Image>();
        stripImg.color = BattleUiColors.WithAlpha(BattleUiColors.HallWine, 0.35f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(stripObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = new Vector2(20f, 8f);
        titleRt.offsetMax = new Vector2(-20f, -8f);
        _titleText = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(_titleText);
        _titleText.fontSize = 40f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.color = BattleUiColors.BtnPrimaryText;

        _cardRewardSection = new GameObject("CardRewardSection", typeof(RectTransform));
        _cardRewardSection.transform.SetParent(_panelRoot.transform, false);
        RectTransform sectionRt = _cardRewardSection.GetComponent<RectTransform>();
        sectionRt.anchorMin = new Vector2(0f, 0f);
        sectionRt.anchorMax = new Vector2(1f, 1f);
        sectionRt.offsetMin = new Vector2(20f, 108f);
        sectionRt.offsetMax = new Vector2(-20f, -188f);

        GameObject rowObj = new GameObject("CardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObj.transform.SetParent(_cardRewardSection.transform, false);
        _cardRowRt = rowObj.GetComponent<RectTransform>();
        _cardRowRt.anchorMin = new Vector2(0f, 0.22f);
        _cardRowRt.anchorMax = new Vector2(1f, 1f);
        _cardRowRt.offsetMin = Vector2.zero;
        _cardRowRt.offsetMax = Vector2.zero;
        HorizontalLayoutGroup cardHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
        cardHlg.childAlignment = TextAnchor.MiddleCenter;
        cardHlg.spacing = 18f;
        cardHlg.padding = new RectOffset(8, 8, 4, 4);
        cardHlg.childControlWidth = false;
        cardHlg.childControlHeight = false;
        cardHlg.childForceExpandWidth = false;
        cardHlg.childForceExpandHeight = false;

        GameObject bodyObj = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(_panelRoot.transform, false);
        _bodyRt = bodyObj.GetComponent<RectTransform>();
        _bodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(_bodyText);
        _bodyText.fontSize = 24f;
        _bodyText.alignment = TextAlignmentOptions.Center;
        _bodyText.color = BattleUiColors.Ink;

        GameObject footerRowObj = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(CanvasGroup));
        footerRowObj.transform.SetParent(_panelRoot.transform, false);
        _buttonRowRt = footerRowObj.GetComponent<RectTransform>();
        _buttonRowRt.anchorMin = new Vector2(0f, 0f);
        _buttonRowRt.anchorMax = new Vector2(1f, 0f);
        _buttonRowRt.pivot = new Vector2(0.5f, 0f);
        _buttonRowRt.sizeDelta = new Vector2(0f, 72f);
        _buttonRowRt.anchoredPosition = new Vector2(0f, 18f);
        HorizontalLayoutGroup hlg = footerRowObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 16f;
        hlg.padding = new RectOffset(24, 24, 0, 0);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        _cardRewardSection.SetActive(false);
        _uiBuilt = true;
        _overlayRoot.SetActive(false);
    }

    private void ClearVictoryCards()
    {
        _victoryCardSlots.Clear();
        if (_cardRowRt == null) return;
        for (int i = _cardRowRt.childCount - 1; i >= 0; i--)
            Destroy(_cardRowRt.GetChild(i).gameObject);
    }

    private void ClearButtons()
    {
        if (_buttonRowRt == null) return;
        for (int i = _buttonRowRt.childCount - 1; i >= 0; i--)
            Destroy(_buttonRowRt.GetChild(i).gameObject);
    }

    private void CreateFooterButton(string label, bool primary, UnityEngine.Events.UnityAction onClick)
    {
        if (_buttonRowRt == null) return;

        GameObject btnObj = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(_buttonRowRt, false);
        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        le.minHeight = 56f;
        le.preferredHeight = 56f;

        Image img = btnObj.GetComponent<Image>();
        img.color = primary ? BattleUiColors.BtnPrimary : BattleUiColors.BtnSecondary;
        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        BattleUiColors.ApplyButtonStyle(btn, btnObj.name);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = primary ? BattleUiColors.BtnPrimaryText : BattleUiColors.BtnSecondaryText;
        tmp.raycastTarget = false;
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        TMP_FontAsset font = _font ?? ResolveFont();
        if (font != null) tmp.font = font;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset settings = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settings != null) return settings;
        return BuildbeckUiFonts.ResolveBuildbeckButtonFont();
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

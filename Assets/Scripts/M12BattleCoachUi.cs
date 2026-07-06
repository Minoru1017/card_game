using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-2 教練 UI（讀取 <see cref="M12BattleCoachCatalog"/>）；僅階段 B 加練，階段 A 段考不提示。</summary>
public sealed class M12BattleCoachUi : MonoBehaviour
{
    private const float ReEvaluateIntervalSeconds = 1.35f;
    private const float PanelLeftMarginPx = 80f;

    private BattleSimulationManager _manager;
    private Transform _canvasRoot;
    private TMP_FontAsset _preferredFont;
    private GameObject _root;
    private TMP_Text _bodyText;
    private TMP_Text _speakerText;
    private Image _portraitImage;
    private string _currentKey = string.Empty;
    private float _nextEvaluateUnscaled;
    private bool _uiBuilt;
    private bool _eventsBound;

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsM12CoachPracticeBattle;

    public void Initialize(
        BattleSimulationManager manager,
        Transform canvasRoot,
        TMP_FontAsset uiFont = null)
    {
        _manager = manager;
        _canvasRoot = canvasRoot;
        _preferredFont = uiFont;
        if (_manager != null && !_eventsBound)
        {
            _manager.PlayerTurnActionWindowOpenedForPromptUi += OnPlayerTurnWindowOpened;
            _manager.PlayerCommittedHandCardToFieldFromHand += OnPlayerCommittedCard;
            _manager.PlayerPressedEndTurnForPromptUi += OnPlayerPressedEndTurn;
            _manager.BattleEnded += OnBattleEnded;
            _eventsBound = true;
        }
    }

    private void OnDestroy()
    {
        if (_eventsBound && _manager != null)
        {
            _manager.PlayerTurnActionWindowOpenedForPromptUi -= OnPlayerTurnWindowOpened;
            _manager.PlayerCommittedHandCardToFieldFromHand -= OnPlayerCommittedCard;
            _manager.PlayerPressedEndTurnForPromptUi -= OnPlayerPressedEndTurn;
            _manager.BattleEnded -= OnBattleEnded;
        }

        _eventsBound = false;
    }

    private void Update()
    {
        if (!IsActiveForCurrentBattle || _manager == null || BattleAutoSimPlugin.IsRunning)
        {
            if (_root != null) _root.SetActive(false);
            return;
        }

        if (Time.unscaledTime >= _nextEvaluateUnscaled)
            EvaluateHints();
    }

    private void OnPlayerTurnWindowOpened()
    {
        _currentKey = string.Empty;
        ScheduleEvaluate(0.12f);
    }

    private void OnPlayerCommittedCard() => ScheduleEvaluate(0.2f);

    private void OnPlayerPressedEndTurn()
    {
        if (_root != null) _root.SetActive(false);
    }

    private void OnBattleEnded(int result)
    {
        if (_root != null) _root.SetActive(false);
    }

    public void HideForSettlement()
    {
        if (_root != null) _root.SetActive(false);
    }

    private void ScheduleEvaluate(float delay)
    {
        _nextEvaluateUnscaled = Time.unscaledTime + Mathf.Max(0f, delay);
    }

    private void EvaluateHints()
    {
        _nextEvaluateUnscaled = Time.unscaledTime + ReEvaluateIntervalSeconds;
        if (_manager == null || !_manager.IsPlayerTurn() || _manager.IsBattleOver())
            return;

        bool ok = M12BattleCoachCatalog.TryEvaluatePhaseB(_manager, out string key, out string message);

        if (!ok || string.IsNullOrWhiteSpace(message))
        {
            if (_root != null) _root.SetActive(false);
            return;
        }

        if (key == _currentKey && _root != null && _root.activeSelf)
            return;

        _currentKey = key;
        EnsureUi();
        if (_root == null) return;
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
        if (_speakerText != null)
            _speakerText.text = M12BattleCoachCatalog.SpeakerName;
        if (_bodyText != null)
            _bodyText.text = message;
    }

    private void EnsureUi()
    {
        if (_uiBuilt || _canvasRoot == null)
            return;

        _uiBuilt = true;
        _root = new GameObject("M12BattleCoach", typeof(RectTransform));
        _root.transform.SetParent(_canvasRoot, false);
        RectTransform panelRt = _root.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0.5f);
        panelRt.anchorMax = new Vector2(0f, 0.5f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.anchoredPosition = new Vector2(PanelLeftMarginPx, 96f);
        panelRt.sizeDelta = new Vector2(500f, 220f);

        Image bg = _root.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.10f, 0.08f, 0.92f);
        Outline outline = _root.AddComponent<Outline>();
        outline.effectColor = new Color(0.97f, 0.85f, 0.47f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject portraitFrameObj = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image), typeof(Outline));
        portraitFrameObj.transform.SetParent(_root.transform, false);
        RectTransform portraitFrameRt = portraitFrameObj.GetComponent<RectTransform>();
        portraitFrameRt.anchorMin = new Vector2(0f, 0.5f);
        portraitFrameRt.anchorMax = new Vector2(0f, 0.5f);
        portraitFrameRt.pivot = new Vector2(0f, 0.5f);
        portraitFrameRt.anchoredPosition = new Vector2(16f, 0f);
        portraitFrameRt.sizeDelta = new Vector2(150f, 150f);
        Image portraitFrameImg = portraitFrameObj.GetComponent<Image>();
        portraitFrameImg.color = BattleUiColors.CoachPortraitMat;
        portraitFrameImg.raycastTarget = false;
        Outline portraitFrameOutline = portraitFrameObj.GetComponent<Outline>();
        portraitFrameOutline.effectColor = BattleUiColors.CoachPortraitFrame;
        portraitFrameOutline.effectDistance = new Vector2(2f, -2f);

        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObj.transform.SetParent(portraitFrameObj.transform, false);
        RectTransform portraitRt = portraitObj.GetComponent<RectTransform>();
        portraitRt.anchorMin = Vector2.zero;
        portraitRt.anchorMax = Vector2.one;
        portraitRt.offsetMin = new Vector2(4f, 4f);
        portraitRt.offsetMax = new Vector2(-4f, -4f);
        _portraitImage = portraitObj.GetComponent<Image>();
        _portraitImage.color = Color.white;
        Sprite portrait = HarborCombatCoachExpressionCatalog.ResolveNeutralOrFallback();
        if (portrait != null)
        {
            _portraitImage.sprite = portrait;
            _portraitImage.preserveAspect = true;
        }

        GameObject textCol = new GameObject("TextColumn", typeof(RectTransform));
        textCol.transform.SetParent(_root.transform, false);
        RectTransform textRt = textCol.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(180f, 16f);
        textRt.offsetMax = new Vector2(-16f, -16f);

        GameObject speakerGo = new GameObject("Speaker", typeof(RectTransform), typeof(TextMeshProUGUI));
        speakerGo.transform.SetParent(textCol.transform, false);
        RectTransform speakerRt = speakerGo.GetComponent<RectTransform>();
        speakerRt.anchorMin = new Vector2(0f, 1f);
        speakerRt.anchorMax = new Vector2(1f, 1f);
        speakerRt.pivot = new Vector2(0f, 1f);
        speakerRt.anchoredPosition = Vector2.zero;
        speakerRt.sizeDelta = new Vector2(0f, 36f);
        _speakerText = speakerGo.GetComponent<TextMeshProUGUI>();
        _speakerText.fontSize = 26f;
        _speakerText.fontStyle = FontStyles.Bold;
        _speakerText.color = new Color(0.97f, 0.85f, 0.47f, 1f);
        ApplyFont(_speakerText);

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(textCol.transform, false);
        RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(0f, 0f);
        bodyRt.offsetMax = new Vector2(0f, -40f);
        _bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
        _bodyText.fontSize = 24f;
        _bodyText.color = new Color(0.95f, 0.93f, 0.88f, 1f);
        _bodyText.enableWordWrapping = true;
        ApplyFont(_bodyText);
    }

    private void ApplyFont(TMP_Text tmp)
    {
        if (tmp == null) return;
        TMP_FontAsset font = _preferredFont ?? ResolveFont();
        if (font != null)
            tmp.font = font;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset settings = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settings != null) return settings;
        return BuildbeckUiFonts.ResolveBuildbeckButtonFont();
    }
}

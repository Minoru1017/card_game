using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>林可姐對戰浮動教學面板：收合頭像＋點擊展開打字提示（入門／M-1-2 等共用）。</summary>
public sealed class LinKeFloatingCoachPanel : MonoBehaviour
{
    public enum ClickMode
    {
        ToggleExpand,
        TapToAdvance
    }

    private const float CoachCharactersPerSecond = 9f;
    private const float BorderPulseSpeed = 2.8f;
    private const float CollapsedPulseScaleMin = 0.84f;
    private const float CollapsedPulseScaleRange = 0.24f;
    private const float PortraitOutlineDist = 2f;
    private const float CollapsedPulseOutlineDistMax = 8f;
    private const float PanelLeftMarginPx = 80f;

    private static readonly Vector2 CollapsedPanelPosition = new Vector2(PanelLeftMarginPx, 0f);
    private static readonly Vector2 ExpandedPanelPosition = new Vector2(PanelLeftMarginPx, 108f);
    private static readonly Vector2 DiscardCollapsedPanelPosition = new Vector2(-PanelLeftMarginPx, 32f);
    private static readonly Vector2 DiscardExpandedPanelPosition = new Vector2(-PanelLeftMarginPx, 88f);
    private const float CollapsedPortraitSize = 152f;
    private const float ExpandedPanelWidth = 560f;
    private const float ExpandedPortraitSize = 180f;
    private const float ExpandedEdgePad = 20f;
    private const float ExpandedPortraitTextGap = 16f;
    private const float ExpandedTextPanelMinWidth = 220f;
    private const float ExpandedTextPanelMaxWidth = 340f;
    private const float ExpandedTextPanelPadH = 18f;
    private const float ExpandedTextPanelPadV = 16f;
    private const float ExpandedBodyFontSize = 28f;
    private const float ExpandedMinPanelHeight = 220f;
    private const float ExpandedCanvasHeightMargin = 40f;

    private const string CoachFontProbe =
        "林可姐點擊查看提示戰位克制先鋒守陣策應定式三角加成被克";

    private Transform _canvasRoot;
    private TMP_FontAsset _preferredFont;
    private string _speakerName = TutorialPlotScriptFactory.LinKeSpeaker;
    private PlotDialogueTypewriter _typewriter;
    private GameObject _root;
    private GameObject _backdrop;
    private RectTransform _panelRt;
    private RectTransform _borderGlowRt;
    private Image _borderGlowImage;
    private RectTransform _portraitFrameRt;
    private RectTransform _portraitRt;
    private RectTransform _nameRt;
    private RectTransform _tapHintRt;
    private RectTransform _bodyRt;
    private RectTransform _bodyPanelRt;
    private GameObject _bodyPanelObj;
    private TMP_Text _bodyText;
    private TMP_Text _speakerNameText;
    private TMP_Text _tapHintText;
    private GameObject _panelChromeObj;
    private Image _portraitFrameImage;
    private Outline _portraitFrameOutline;
    private Image _portraitImage;
    private Button _panelButton;
    private string _lastHintMessage = string.Empty;
    private bool _uiBuilt;
    private bool _expanded;
    private bool _hasUnreadHint;
    private bool _discardLayoutActive;
    private bool _portraitOnRight;
    private static Sprite placeholderPortraitSprite;
    private static Sprite whiteSprite;

    public ClickMode PanelClickMode { get; set; } = ClickMode.ToggleExpand;
    public bool IsExpanded => _expanded;
    public bool IsTypewriterActive => _typewriter != null && _typewriter.IsActive;
    public bool HasUnreadHint => _hasUnreadHint;
    public event Action PanelAdvanceRequested;

    public void Initialize(Transform canvasRoot, TMP_FontAsset uiFont = null, string speakerName = null)
    {
        _canvasRoot = canvasRoot;
        if (uiFont != null) _preferredFont = uiFont;
        if (!string.IsNullOrWhiteSpace(speakerName)) _speakerName = speakerName;
        ApplyCoachFontToLabels();
    }

    public void SetDiscardPhaseActive(bool active)
    {
        if (_discardLayoutActive == active) return;
        _discardLayoutActive = active;
        if (_uiBuilt) ApplyPanelLayout();
    }

    public void ShowHint(string message, bool forceExpand = false)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        EnsureUi();
        if (_root == null || _bodyText == null) return;

        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
        bool messageChanged = message != _lastHintMessage;
        _lastHintMessage = message;
        _hasUnreadHint = true;

        if (forceExpand && !_expanded)
            ExpandPanel();
        else if (forceExpand)
            ApplyPanelLayout();

        if (_expanded)
        {
            if (messageChanged || !_typewriter.IsActive)
                BeginHintTypewriter();
        }
        else
            SetBodyVisible(false);
    }

    public void Tick(float unscaledDeltaTime)
    {
        if (!_uiBuilt || _root == null || !_root.activeSelf) return;
        if (_expanded)
            _typewriter?.Tick(unscaledDeltaTime);
        else if (_hasUnreadHint)
            UpdateCollapsedBorderPulse();
        else
            ResetBorderGlowPresentation();
    }

    public void Hide()
    {
        CollapsePanel();
        if (_root != null) _root.SetActive(false);
        if (_backdrop != null) _backdrop.SetActive(false);
    }

    public void ApplyPortraitExpression(string hintKey) =>
        HarborCombatCoachExpressionCatalog.ApplyToPortrait(_portraitImage, hintKey);

    public void CollapsePanel()
    {
        if (!_expanded || _panelRt == null) return;
        _expanded = false;
        if (_backdrop != null) _backdrop.SetActive(false);
        SetBodyVisible(false);
        ApplyPanelLayout();
    }

    private void ExpandPanel()
    {
        if (_expanded || _panelRt == null) return;
        _expanded = true;
        _hasUnreadHint = false;
        if (_backdrop != null)
        {
            _backdrop.SetActive(true);
            _backdrop.transform.SetAsLastSibling();
        }

        _root.transform.SetAsLastSibling();
        ApplyPanelLayout();
        BeginHintTypewriter();
    }

    private void OnPanelClicked()
    {
        if (PanelClickMode == ClickMode.TapToAdvance)
        {
            PanelAdvanceRequested?.Invoke();
            return;
        }

        if (_expanded) CollapsePanel();
        else ExpandPanel();
    }

    private void BeginHintTypewriter()
    {
        if (string.IsNullOrEmpty(_lastHintMessage) || _bodyText == null) return;
        SetBodyVisible(true);
        if (_expanded)
            FitExpandedPanelToContent(_lastHintMessage);
        _typewriter ??= new PlotDialogueTypewriter();
        _typewriter.Begin(_bodyText, _lastHintMessage, CoachCharactersPerSecond);
    }

    private void SetBodyVisible(bool visible)
    {
        if (_bodyPanelObj != null)
            _bodyPanelObj.SetActive(visible);
        if (_bodyRt != null)
            _bodyRt.gameObject.SetActive(visible);
        if (_bodyText == null) return;
        if (!visible)
            _bodyText.text = string.Empty;
        else if (_bodyPanelObj != null)
            _bodyPanelObj.transform.SetAsLastSibling();
    }

    private void UpdateCollapsedBorderPulse()
    {
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * BorderPulseSpeed);
        Color pulseColor = Color.Lerp(
            BattleUiColors.CoachUnreadPulseGlowDim,
            BattleUiColors.CoachUnreadPulseGlow,
            t);
        float pulseScale = CollapsedPulseScaleMin + CollapsedPulseScaleRange * t;

        if (_portraitFrameImage != null)
            _portraitFrameImage.color = BattleUiColors.CoachPortraitMat;
        if (_portraitFrameOutline != null)
        {
            _portraitFrameOutline.effectColor = Color.Lerp(
                BattleUiColors.CoachUnreadPulseGlowDim,
                BattleUiColors.CoachUnreadPulseGlow,
                t);
            float outlineDist = Mathf.Lerp(PortraitOutlineDist, CollapsedPulseOutlineDistMax, t);
            _portraitFrameOutline.effectDistance = new Vector2(outlineDist, -outlineDist);
        }

        if (_portraitFrameRt != null)
            _portraitFrameRt.localScale = Vector3.one * pulseScale;
        if (_borderGlowImage != null)
            _borderGlowImage.color = pulseColor;
    }

    private void ResetBorderGlowPresentation()
    {
        if (_borderGlowImage != null)
            _borderGlowImage.color = BattleUiColors.CoachBorderGlowFill;
        if (_portraitFrameImage != null)
            _portraitFrameImage.color = BattleUiColors.CoachPortraitMat;
        if (_portraitFrameOutline != null)
        {
            _portraitFrameOutline.effectColor = BattleUiColors.CoachPortraitFrame;
            _portraitFrameOutline.effectDistance = new Vector2(PortraitOutlineDist, -PortraitOutlineDist);
        }

        if (_portraitFrameRt != null)
            _portraitFrameRt.localScale = Vector3.one;
    }

    private void ApplyPanelLayout()
    {
        if (_panelRt == null) return;

        _portraitOnRight = _discardLayoutActive;
        float portraitSize = _expanded ? ExpandedPortraitSize : CollapsedPortraitSize;
        float framePad = _expanded ? 6f : 5f;
        float portraitFrameSize = portraitSize + framePad * 2f;

        if (_expanded)
        {
            if (_portraitOnRight)
                ApplyRightCenterPanelAnchor(DiscardExpandedPanelPosition);
            else
                ApplyLeftCenterPanelAnchor(ExpandedPanelPosition);
            LayoutExpandedSplit(portraitFrameSize, portraitSize, _portraitOnRight);
            if (!string.IsNullOrEmpty(_lastHintMessage))
                SetBodyVisible(true);
            FitExpandedPanelToContent(_lastHintMessage);
        }
        else
        {
            if (_portraitOnRight)
                ApplyRightCenterPanelAnchor(DiscardCollapsedPanelPosition);
            else
                ApplyLeftCenterPanelAnchor(CollapsedPanelPosition);
            LayoutCollapsedPortraitOnly(portraitFrameSize, portraitSize);
        }

        ResetBorderGlowPresentation();
    }

    private void ApplyLeftCenterPanelAnchor(Vector2 anchoredPosition)
    {
        _panelRt.anchorMin = new Vector2(0f, 0.5f);
        _panelRt.anchorMax = new Vector2(0f, 0.5f);
        _panelRt.pivot = new Vector2(0f, 0.5f);
        _panelRt.anchoredPosition = ResolvePanelAnchoredPosition(anchoredPosition);
    }

    private void ApplyRightCenterPanelAnchor(Vector2 anchoredPosition)
    {
        _panelRt.anchorMin = new Vector2(1f, 0.5f);
        _panelRt.anchorMax = new Vector2(1f, 0.5f);
        _panelRt.pivot = new Vector2(1f, 0.5f);
        _panelRt.anchoredPosition = ResolveRightPanelAnchoredPosition(anchoredPosition);
    }

    private Vector2 ResolvePanelAnchoredPosition(Vector2 basePosition)
    {
        float x = basePosition.x + GetCanvasSafeAreaLeftInset();
        return new Vector2(x, basePosition.y);
    }

    private Vector2 ResolveRightPanelAnchoredPosition(Vector2 basePosition)
    {
        float x = basePosition.x - GetCanvasSafeAreaRightInset();
        return new Vector2(x, basePosition.y);
    }

    private float GetCanvasSafeAreaLeftInset()
    {
        Rect safe = Screen.safeArea;
        if (safe.xMin <= 0f) return 0f;
        Canvas canvas = _canvasRoot != null ? _canvasRoot.GetComponentInParent<Canvas>() : null;
        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        return safe.xMin / scale;
    }

    private float GetCanvasSafeAreaRightInset()
    {
        Rect safe = Screen.safeArea;
        float overflow = Screen.width - safe.xMax;
        if (overflow <= 0f) return 0f;
        Canvas canvas = _canvasRoot != null ? _canvasRoot.GetComponentInParent<Canvas>() : null;
        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        return overflow / scale;
    }

    private void HideOuterChrome()
    {
        if (_panelChromeObj != null) _panelChromeObj.SetActive(false);
        if (_borderGlowRt != null) _borderGlowRt.gameObject.SetActive(false);
        if (_nameRt != null) _nameRt.gameObject.SetActive(false);
        if (_tapHintRt != null) _tapHintRt.gameObject.SetActive(false);
    }

    private void LayoutCollapsedPortraitOnly(float portraitFrameSize, float portraitSize)
    {
        HideOuterChrome();
        if (_panelRt != null)
            _panelRt.sizeDelta = new Vector2(portraitFrameSize, portraitFrameSize);

        if (_portraitFrameRt != null)
        {
            _portraitFrameRt.anchorMin = new Vector2(0.5f, 0.5f);
            _portraitFrameRt.anchorMax = new Vector2(0.5f, 0.5f);
            _portraitFrameRt.pivot = new Vector2(0.5f, 0.5f);
            _portraitFrameRt.sizeDelta = new Vector2(portraitFrameSize, portraitFrameSize);
            _portraitFrameRt.anchoredPosition = Vector2.zero;
        }

        if (_portraitRt != null)
        {
            _portraitRt.anchorMin = new Vector2(0.5f, 0.5f);
            _portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
            _portraitRt.pivot = new Vector2(0.5f, 0.5f);
            _portraitRt.sizeDelta = new Vector2(portraitSize, portraitSize);
            _portraitRt.anchoredPosition = Vector2.zero;
        }

        if (_panelButton != null && _portraitFrameImage != null)
        {
            _panelButton.targetGraphic = _portraitFrameImage;
            _portraitFrameImage.raycastTarget = true;
        }
    }

    private void LayoutExpandedSplit(float portraitFrameSize, float portraitSize, bool portraitOnRight)
    {
        HideOuterChrome();
        float portraitAnchorX = portraitOnRight ? 1f : 0f;
        if (_portraitFrameRt != null)
        {
            _portraitFrameRt.anchorMin = new Vector2(portraitAnchorX, 0.5f);
            _portraitFrameRt.anchorMax = new Vector2(portraitAnchorX, 0.5f);
            _portraitFrameRt.pivot = new Vector2(portraitAnchorX, 0.5f);
            _portraitFrameRt.sizeDelta = new Vector2(portraitFrameSize, portraitFrameSize);
            _portraitFrameRt.anchoredPosition = Vector2.zero;
        }

        if (_portraitRt != null)
            StretchFull(_portraitRt, 4f);

        if (_panelButton != null && _portraitFrameImage != null)
        {
            _panelButton.targetGraphic = _portraitFrameImage;
            _portraitFrameImage.raycastTarget = true;
        }

        if (_bodyText != null)
        {
            _bodyText.fontSize = ExpandedBodyFontSize;
            _bodyText.lineSpacing = 6f;
            _bodyText.overflowMode = TextOverflowModes.Overflow;
            _bodyText.color = BattleUiColors.CoachHintText;
            _bodyText.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private void LayoutExpandedTextPanel(float portraitFrameSize, float panelWidth, float panelHeight, bool portraitOnRight)
    {
        if (_bodyPanelRt == null) return;
        float gap = ExpandedPortraitTextGap;
        float anchorX = portraitOnRight ? 1f : 0f;
        float panelOffsetX = portraitOnRight ? -(portraitFrameSize + gap) : portraitFrameSize + gap;
        _bodyPanelRt.anchorMin = new Vector2(anchorX, 0.5f);
        _bodyPanelRt.anchorMax = new Vector2(anchorX, 0.5f);
        _bodyPanelRt.pivot = new Vector2(anchorX, 0.5f);
        _bodyPanelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
        _bodyPanelRt.anchoredPosition = new Vector2(panelOffsetX, 0f);
        if (_bodyRt != null)
        {
            _bodyRt.anchorMin = Vector2.zero;
            _bodyRt.anchorMax = Vector2.one;
            _bodyRt.offsetMin = new Vector2(ExpandedTextPanelPadH, ExpandedTextPanelPadV);
            _bodyRt.offsetMax = new Vector2(-ExpandedTextPanelPadH, -ExpandedTextPanelPadV);
        }
    }

    private void FitExpandedPanelToContent(string message)
    {
        if (!_expanded || _panelRt == null || _bodyText == null) return;
        float portraitFrameSize = ExpandedPortraitSize + 12f;
        float innerMaxWidth = ExpandedTextPanelMaxWidth - ExpandedTextPanelPadH * 2f;
        string measureText = string.IsNullOrWhiteSpace(message) ? " " : message;
        _bodyText.fontSize = ExpandedBodyFontSize;
        Vector2 preferred = _bodyText.GetPreferredValues(measureText, innerMaxWidth, 0f);
        float textWidth = Mathf.Clamp(
            preferred.x + 4f,
            ExpandedTextPanelMinWidth - ExpandedTextPanelPadH * 2f,
            innerMaxWidth);
        float textHeight = Mathf.Max(40f, preferred.y + 4f);
        float panelWidth = textWidth + ExpandedTextPanelPadH * 2f;
        float panelHeight = textHeight + ExpandedTextPanelPadV * 2f;
        float rootWidth = portraitFrameSize + ExpandedPortraitTextGap + panelWidth;
        float rootHeight = Mathf.Max(portraitFrameSize, panelHeight, ExpandedMinPanelHeight);
        if (_canvasRoot is RectTransform canvasRt)
        {
            float maxHeight = canvasRt.rect.height - ExpandedCanvasHeightMargin * 2f;
            if (maxHeight > ExpandedMinPanelHeight)
                rootHeight = Mathf.Min(rootHeight, maxHeight);
        }

        _panelRt.sizeDelta = new Vector2(rootWidth, rootHeight);
        LayoutExpandedTextPanel(portraitFrameSize, panelWidth, panelHeight, _portraitOnRight);
    }

    private void EnsureUi()
    {
        if (_uiBuilt) return;
        if (_canvasRoot == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas") ?? GameObject.Find("Canvas2") ?? GameObject.Find("Canva2");
            if (canvasObj != null) _canvasRoot = canvasObj.transform;
        }

        if (_canvasRoot == null) return;
        TMP_FontAsset font = ResolveCoachFont();
        _backdrop = CreateDismissBackdrop();
        _backdrop.SetActive(false);

        _root = new GameObject("LinKeFloatingCoachPanel", typeof(RectTransform));
        _root.transform.SetParent(_canvasRoot, false);
        _panelRt = _root.GetComponent<RectTransform>();
        _panelButton = _root.AddComponent<Button>();
        _panelButton.transition = Selectable.Transition.None;
        _panelButton.onClick.AddListener(OnPanelClicked);

        GameObject borderGlowObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
        borderGlowObj.transform.SetParent(_root.transform, false);
        _borderGlowRt = borderGlowObj.GetComponent<RectTransform>();
        StretchFull(_borderGlowRt, -8f);
        _borderGlowImage = borderGlowObj.GetComponent<Image>();
        _borderGlowImage.sprite = GetWhiteSprite();
        _borderGlowImage.color = BattleUiColors.CoachBorderGlowFill;
        _borderGlowImage.raycastTarget = false;

        _panelChromeObj = new GameObject("PanelChrome", typeof(RectTransform), typeof(Image));
        _panelChromeObj.transform.SetParent(_root.transform, false);
        StretchFull(_panelChromeObj.GetComponent<RectTransform>(), 0f);
        Image panelBg = _panelChromeObj.GetComponent<Image>();
        panelBg.color = BattleUiColors.CoachPanelBg;
        panelBg.raycastTarget = true;

        GameObject portraitFrameObj = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
        portraitFrameObj.transform.SetParent(_root.transform, false);
        _portraitFrameRt = portraitFrameObj.GetComponent<RectTransform>();
        _portraitFrameImage = portraitFrameObj.GetComponent<Image>();
        _portraitFrameImage.sprite = GetWhiteSprite();
        _portraitFrameImage.color = BattleUiColors.CoachPortraitMat;
        _portraitFrameOutline = portraitFrameObj.AddComponent<Outline>();
        _portraitFrameOutline.effectColor = BattleUiColors.CoachPortraitFrame;
        _portraitFrameOutline.effectDistance = new Vector2(PortraitOutlineDist, -PortraitOutlineDist);

        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObj.transform.SetParent(portraitFrameObj.transform, false);
        _portraitRt = portraitObj.GetComponent<RectTransform>();
        StretchFull(_portraitRt, 4f);
        _portraitImage = portraitObj.GetComponent<Image>();
        _portraitImage.preserveAspect = true;
        ApplyPortraitSprite();

        GameObject nameObj = new GameObject("SpeakerName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(_root.transform, false);
        _nameRt = nameObj.GetComponent<RectTransform>();
        _speakerNameText = nameObj.GetComponent<TextMeshProUGUI>();
        if (font != null) _speakerNameText.font = font;
        _speakerNameText.fontStyle = FontStyles.Bold;
        _speakerNameText.color = BattleUiColors.TurnPlayer;
        _speakerNameText.text = _speakerName;
        _speakerNameText.raycastTarget = false;

        GameObject tapHintObj = new GameObject("TapHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        tapHintObj.transform.SetParent(_root.transform, false);
        _tapHintRt = tapHintObj.GetComponent<RectTransform>();
        _tapHintText = tapHintObj.GetComponent<TextMeshProUGUI>();
        if (font != null) _tapHintText.font = font;
        _tapHintText.text = "點擊查看提示";
        _tapHintText.raycastTarget = false;

        _bodyPanelObj = new GameObject("HintTextPanel", typeof(RectTransform), typeof(Image));
        _bodyPanelObj.transform.SetParent(_root.transform, false);
        _bodyPanelRt = _bodyPanelObj.GetComponent<RectTransform>();
        Image bodyPanelImg = _bodyPanelObj.GetComponent<Image>();
        bodyPanelImg.sprite = GetWhiteSprite();
        bodyPanelImg.color = BattleUiColors.CoachHintTextPanelBg;
        _bodyPanelObj.SetActive(false);

        GameObject bodyObj = new GameObject("CoachText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(_bodyPanelObj.transform, false);
        _bodyRt = bodyObj.GetComponent<RectTransform>();
        _bodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        if (font != null) _bodyText.font = font;
        _bodyText.enableWordWrapping = true;
        _bodyText.richText = true;
        _bodyText.raycastTarget = false;

        _typewriter = new PlotDialogueTypewriter();
        _uiBuilt = true;
        ApplyCoachFontToLabels();
        ApplyPanelLayout();
        _root.SetActive(false);
    }

    private void ApplyPortraitSprite()
    {
        if (_portraitImage == null) return;
        Sprite portrait = HarborCombatCoachExpressionCatalog.ResolveNeutralOrFallback();
        _portraitImage.sprite = portrait != null ? portrait : GetPlaceholderPortraitSprite();
        _portraitImage.color = Color.white;
    }

    private GameObject CreateDismissBackdrop()
    {
        GameObject backdrop = new GameObject("LinKeCoachBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(_canvasRoot, false);
        StretchFull(backdrop.GetComponent<RectTransform>(), 0f);
        Image img = backdrop.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.22f);
        img.raycastTarget = false;
        return backdrop;
    }

    private void ApplyCoachFontToLabels()
    {
        TMP_FontAsset font = ResolveCoachFont();
        if (font == null) return;
        if (_speakerNameText != null) _speakerNameText.font = font;
        if (_tapHintText != null) _tapHintText.font = font;
        if (_bodyText != null) _bodyText.font = font;
    }

    private TMP_FontAsset ResolveCoachFont()
    {
        if (_preferredFont != null && BuildbeckUiFonts.FontSupportsText(_preferredFont, CoachFontProbe))
            return _preferredFont;
        TMP_FontAsset settingsFont = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settingsFont != null && BuildbeckUiFonts.FontSupportsText(settingsFont, CoachFontProbe))
            return settingsFont;
        TMP_FontAsset buildbeckFont = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeckFont != null && BuildbeckUiFonts.FontSupportsText(buildbeckFont, CoachFontProbe))
            return buildbeckFont;
        return _preferredFont ?? settingsFont ?? buildbeckFont;
    }

    private static void StretchFull(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return whiteSprite;
    }

    private static Sprite GetPlaceholderPortraitSprite()
    {
        if (placeholderPortraitSprite != null) return placeholderPortraitSprite;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        Color32 fill = BattleUiColors.CoachPortraitMat;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        placeholderPortraitSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return placeholderPortraitSprite;
    }
}

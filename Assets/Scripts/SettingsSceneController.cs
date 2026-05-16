using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Settings 場景：巢狀選單（顯示比例／畫面品質／關於）、離開回 hall、戰鬥預設一。
/// </summary>
public class SettingsSceneController : MonoBehaviour
{
    private const string SceneName = "Settings";
    private const string HallSceneName = "hall";

    private const string DisplayRatioName = "display ratio";
    private const string ImageQualityName = "Image quality settings";
    private const string LeaveButtonName = "leave";
    private const string PresetSetName = "preset set";
    private const string PresetNestedPanelName = "Preset nested panel";
    private const string QualityButtonRowName = "QualityButtonRow";
    private const string ParameterDetailsName = "Parameter details";
    private const string AboutRootName = "about";
    private const string AboutBgChildName = "BG";
    private const string AboutSaveDetailsName = "About save details";
    private static readonly string[] ParameterTitleNames = { "title", "大標" };
    private static readonly string[] ParameterSubtitleNames = { "Sub-label", "副標" };
    private static readonly string[] ParameterBodyNames = { "content", "內文" };
    private const string ApplyFeedbackToastName = "Apply feedback toast";
    private const int PresetNestedCanvasSortOrder = 80;

    private const string AboutSaveInfoTitle = "玩家存檔與資料安全";
    private const string AboutSaveInfoSubtitle = "以下說明本遊戲如何盡量保護您的本機存檔，以及無法保證的部分。";
    private const string AboutSaveInfoBody =
        "我們做了什麼：存檔會先寫暫存檔再換正式檔，減少當機時寫到一半壞檔；並保留最近幾份備份，必要時可自動改用備份讀取。\n\n" +
        "我們沒做／做不到什麼：資料在您的電腦／裝置上，沒有像銀行那樣的線上驗證；若有人手動改檔、重灌系統、刪資料夾，仍可能遺失或變更（若沒有自己備份整個存檔資料夾）。";

    private static bool subscribed;

    private GameObject displayRatioRoot;
    private GameObject displayRatioDetailBg;
    private GameObject presetSetButton;
    private GameObject battleSceneLabel;
    private GameObject imageQualityDetailBg;
    private GameObject presetNestedPanel;
    private GameObject aboutRoot;
    private GameObject aboutDetailBg;
    private readonly List<(Button button, string presetId)> presetChoiceButtons = new List<(Button, string)>();
    private readonly List<Button> qualityButtons = new List<Button>();
    private TMP_FontAsset settingsUiFont;
    private GameObject parameterDetailsPanel;
    private TextMeshProUGUI parameterTitleTmp;
    private TextMeshProUGUI parameterSubtitleTmp;
    private TextMeshProUGUI parameterBodyTmp;
    private GameObject aboutSaveDetailsPanel;
    private TextMeshProUGUI aboutTitleTmp;
    private TextMeshProUGUI aboutSubtitleTmp;
    private TextMeshProUGUI aboutBodyTmp;
    private GameObject applyFeedbackToast;
    private TextMeshProUGUI applyFeedbackToastText;
    private Coroutine applyFeedbackRoutine;

    private bool displayRatioTierOpen;
    private bool imageQualityTierOpen;
    private bool aboutTierOpen;
    private bool presetNestedOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        TryBindScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindScene(scene);
    }

    private static void TryBindScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName) return;

        SettingsSceneController existing = Object.FindFirstObjectByType<SettingsSceneController>();
        if (existing != null) return;

        GameObject host = new GameObject("SettingsSceneController");
        host.AddComponent<SettingsSceneController>();
    }

    private void Awake()
    {
        FixCanvasScale();
        FixSettingsCanvasNonInteractiveRaycasts();
        CacheUiRefs();
        settingsUiFont = ResolveSettingsUiFont();
        EnsureParameterFeedbackUi();
        BuildPresetNestedUi();
        BuildQualityButtons();
        WireNavigationButtons();
        ApplyTierVisibility();
        BattleCardTuningUserSettings.ApplySavedQualityLevel();
        EnsureAboutSaveInfoUi();
        DisableTmpRaycastsUnderPanelSelection();
    }

    private void FixCanvasScale()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;
        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        if (canvasRt != null && canvasRt.localScale.sqrMagnitude < 0.001f)
            canvasRt.localScale = Vector3.one;
    }

    /// <summary>
    /// Decorative full-screen or oversized Images with raycastTarget steal clicks from other controls.
    /// </summary>
    private void FixSettingsCanvasNonInteractiveRaycasts()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            Transform rootBg = canvasGo.transform.Find("BG");
            if (rootBg != null)
            {
                Image img = rootBg.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
        }

        GameObject aboutRoot = GameObject.Find(AboutRootName);
        if (aboutRoot != null)
        {
            Transform aboutBg = aboutRoot.transform.Find(AboutBgChildName);
            if (aboutBg != null)
            {
                Image outerImg = aboutBg.GetComponent<Image>();
                if (outerImg != null) outerImg.raycastTarget = false;
            }
        }
    }

    private static void DisableTmpRaycastsUnderPanelSelection()
    {
        GameObject panelSel = GameObject.Find("Panel selection");
        if (panelSel == null) return;
        TextMeshProUGUI[] tmps = panelSel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] != null) tmps[i].raycastTarget = false;
        }
    }

    private void CacheUiRefs()
    {
        displayRatioRoot = GameObject.Find(DisplayRatioName);
        GameObject imageQualityRoot = GameObject.Find(ImageQualityName);

        if (displayRatioRoot != null)
        {
            displayRatioDetailBg = FindDirectChild(displayRatioRoot.transform, "BG");
            presetSetButton = FindDirectChild(displayRatioRoot.transform, PresetSetName);
            battleSceneLabel = FindDirectChild(displayRatioRoot.transform, "battle scene");
        }

        if (imageQualityRoot != null)
            imageQualityDetailBg = FindDirectChild(imageQualityRoot.transform, "BG");

        aboutRoot = GameObject.Find(AboutRootName);
        if (aboutRoot != null)
            aboutDetailBg = FindDirectChild(aboutRoot.transform, AboutBgChildName);
    }

    private void WireNavigationButtons()
    {
        BindButton(GameObject.Find(LeaveButtonName), OnLeaveClicked);

        if (displayRatioRoot != null)
            BindButton(ResolveNavHitArea(displayRatioRoot), OnDisplayRatioNavClicked);

        GameObject imageQualityRoot = GameObject.Find(ImageQualityName);
        if (imageQualityRoot != null)
            BindButton(ResolveNavHitArea(imageQualityRoot), OnImageQualityNavClicked);

        if (aboutRoot != null)
            BindButton(ResolveNavHitArea(aboutRoot), OnAboutNavClicked);

        if (presetSetButton != null)
            BindButton(presetSetButton, OnPresetSetClicked);
    }

    private void BuildPresetNestedUi()
    {
        if (presetSetButton == null) return;

        RectTransform presetSetRt = presetSetButton.GetComponent<RectTransform>();
        if (presetSetRt == null) return;
        Vector2 presetButtonSize = presetSetRt.sizeDelta;

        Transform parent = presetSetButton.transform;
        presetNestedPanel = FindDirectChild(parent, PresetNestedPanelName);
        if (presetNestedPanel == null)
        {
            presetNestedPanel = new GameObject(PresetNestedPanelName, typeof(RectTransform), typeof(Image));
            presetNestedPanel.transform.SetParent(parent, false);
            Image panelBg = presetNestedPanel.GetComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
            panelBg.raycastTarget = true;
        }

        RectTransform nestedPanelRt = presetNestedPanel.GetComponent<RectTransform>();
        if (nestedPanelRt != null)
        {
            CopyRectLayout(presetSetRt, nestedPanelRt);
            nestedPanelRt.pivot = new Vector2(0.5f, 1f);
            nestedPanelRt.anchoredPosition = new Vector2(0f, -presetButtonSize.y * 0.5f);
            nestedPanelRt.sizeDelta = presetButtonSize;
        }

        ClearPresetChoiceButtons();
        presetChoiceButtons.Clear();

        BattleCardTuningPresetCatalog catalog = BattleCardTuningPresetLibrary.LoadCatalog();
        BattleCardTuningPresetEntry[] presets = catalog?.presets;
        if (presets == null || presets.Length == 0) return;

        const float buttonGap = 8f;
        int count = 0;
        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i] != null && !string.IsNullOrWhiteSpace(presets[i].presetId)) count++;
        }

        if (count <= 0) return;

        float panelHeight = count * presetButtonSize.y + (count - 1) * buttonGap;
        if (nestedPanelRt != null)
            nestedPanelRt.sizeDelta = new Vector2(presetButtonSize.x, panelHeight);

        int stacked = 0;
        for (int i = 0; i < presets.Length; i++)
        {
            BattleCardTuningPresetEntry entry = presets[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.presetId)) continue;
            float y = -stacked * (presetButtonSize.y + buttonGap);
            string label = string.IsNullOrWhiteSpace(entry.displayName) ? entry.presetId : entry.displayName;
            CreatePresetChoiceButton(label, entry.presetId, presetSetRt, y);
            stacked++;
        }

        EnsurePresetNestedTopLayer();
        if (presetNestedPanel != null && presetNestedPanel.activeSelf)
            BringPresetNestedToFront();
    }

    private void ClearPresetChoiceButtons()
    {
        if (presetNestedPanel == null) return;
        Transform panelTransform = presetNestedPanel.transform;
        for (int i = panelTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = panelTransform.GetChild(i);
            if (child != null && child.name.StartsWith("Preset_", System.StringComparison.Ordinal))
                Destroy(child.gameObject);
        }
    }

    private void CreatePresetChoiceButton(string label, string presetId, RectTransform presetSetRt, float yOffset)
    {
        if (presetNestedPanel == null || presetSetRt == null) return;

        Vector2 presetButtonSize = presetSetRt.sizeDelta;

        GameObject btnGo = new GameObject("Preset_" + presetId, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(presetNestedPanel.transform, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        CopyRectLayout(presetSetRt, rt);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta = presetButtonSize;

        Image img = btnGo.GetComponent<Image>();
        img.color = new Color(0.35f, 0.55f, 0.95f, 1f);
        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        ApplySettingsLabelFont(tmp);

        string capturedId = presetId;
        btn.onClick.AddListener(() => OnPresetChosen(capturedId));
        presetChoiceButtons.Add((btn, capturedId));
        btnGo.transform.SetAsLastSibling();
        RefreshPresetButtonHighlights();
    }

    private void BuildQualityButtons()
    {
        if (imageQualityDetailBg == null) return;

        Transform row = imageQualityDetailBg.transform.Find(QualityButtonRowName);
        if (row == null)
        {
            GameObject rowGo = new GameObject(QualityButtonRowName, typeof(RectTransform));
            rowGo.transform.SetParent(imageQualityDetailBg.transform, false);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, 0f);
            rowRt.sizeDelta = new Vector2(900f, 520f);
            row = rowGo.transform;
        }

        qualityButtons.Clear();
        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0) return;

        float buttonHeight = 72f;
        float gap = 12f;
        float totalHeight = names.Length * buttonHeight + (names.Length - 1) * gap;
        float startY = totalHeight * 0.5f - buttonHeight * 0.5f;

        for (int i = 0; i < names.Length; i++)
        {
            int level = i;
            float y = startY - i * (buttonHeight + gap);
            Button btn = CreateQualityButton(row, names[i], y, buttonHeight, settingsUiFont);
            btn.onClick.AddListener(() => OnQualityChosen(level));
            qualityButtons.Add(btn);
        }

        RefreshQualityButtonHighlights();
    }

    private static Button CreateQualityButton(
        Transform parent, string label, float anchoredY, float height, TMP_FontAsset font)
    {
        GameObject btnGo = new GameObject("Quality_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta = new Vector2(520f, height);

        Image img = btnGo.GetComponent<Image>();
        img.color = new Color(0.28f, 0.62f, 0.88f, 1f);
        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (font != null)
            tmp.font = font;
        else if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        return btn;
    }

    private void ApplySettingsLabelFont(TextMeshProUGUI tmp)
    {
        SettingsUiFonts.ApplyTo(tmp);
    }

    private static TMP_FontAsset ResolveSettingsUiFont()
    {
        TMP_FontAsset parameterFont = SettingsUiFonts.ResolveParameterDetailsFont();
        if (parameterFont != null) return parameterFont;

        const string cjkProbe = BattleCardTuningPresetDisplay.CjkFontProbe;

        GameObject displayRatioRoot = GameObject.Find(DisplayRatioName);
        if (displayRatioRoot != null)
        {
            TextMeshProUGUI[] labels = displayRatioRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_FontAsset font = labels[i] != null ? labels[i].font : null;
                if (font != null && BuildbeckUiFonts.FontSupportsText(font, cjkProbe))
                    return font;
            }
        }

        GameObject imageQualityRoot = GameObject.Find(ImageQualityName);
        if (imageQualityRoot != null)
        {
            TextMeshProUGUI[] labels = imageQualityRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_FontAsset font = labels[i] != null ? labels[i].font : null;
                if (font != null && BuildbeckUiFonts.FontSupportsText(font, cjkProbe))
                    return font;
            }
        }

        TMP_FontAsset resolved = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (resolved != null && BuildbeckUiFonts.FontSupportsText(resolved, cjkProbe))
            return resolved;

        return TMP_Settings.defaultFontAsset;
    }

    private void OnLeaveClicked()
    {
        if (Application.CanStreamedLevelBeLoaded(HallSceneName))
            SceneManager.LoadScene(HallSceneName);
        else
            Debug.LogError("SettingsSceneController: hall scene not in Build Settings.");
    }

    private void OnDisplayRatioNavClicked()
    {
        displayRatioTierOpen = !displayRatioTierOpen;
        if (!displayRatioTierOpen)
            presetNestedOpen = false;
        ApplyTierVisibility();
    }

    private void OnImageQualityNavClicked()
    {
        imageQualityTierOpen = !imageQualityTierOpen;
        ApplyTierVisibility();
    }

    private void OnAboutNavClicked()
    {
        aboutTierOpen = !aboutTierOpen;
        ApplyAboutTierVisibility();
    }

    private void OnPresetSetClicked()
    {
        if (!displayRatioTierOpen)
            displayRatioTierOpen = true;
        presetNestedOpen = !presetNestedOpen;
        ApplyTierVisibility();
    }

    private void OnPresetChosen(string presetId)
    {
        if (!BattleCardTuningPresetLibrary.TryGetPreset(presetId, out BattleCardTuningPresetEntry entry))
        {
            Debug.LogWarning("SettingsSceneController: 找不到預設 → " + presetId);
            return;
        }

        BattleCardTuningUserSettings.SetSelectedPresetId(presetId);
        RefreshPresetButtonHighlights();

        BattleSimulationManager battleManager = Object.FindFirstObjectByType<BattleSimulationManager>();
        if (battleManager != null)
            BattleCardTuningUserSettings.TryApplySelectedPreset(battleManager);

        ShowApplyFeedbackToast(entry);
        ShowParameterDetails(entry);
    }

    private void OnQualityChosen(int level)
    {
        BattleCardTuningUserSettings.SetQualityLevel(level);
        RefreshQualityButtonHighlights();
    }

    private void ApplyTierVisibility()
    {
        SetActive(displayRatioDetailBg, displayRatioTierOpen);
        SetActive(presetSetButton, displayRatioTierOpen);
        SetActive(battleSceneLabel, displayRatioTierOpen);
        SetActive(presetNestedPanel, displayRatioTierOpen && presetNestedOpen);
        SetActive(imageQualityDetailBg, imageQualityTierOpen);
        ApplyParameterDetailsPanelState();

        if (displayRatioTierOpen && presetNestedOpen)
            BringPresetNestedToFront();

        if (parameterDetailsPanel != null)
            parameterDetailsPanel.transform.SetAsLastSibling();

        ApplyAboutTierVisibility();
    }

    private void ApplyAboutTierVisibility()
    {
        SetActive(aboutDetailBg, aboutTierOpen);
        if (aboutDetailBg != null && aboutTierOpen)
            aboutDetailBg.transform.SetAsLastSibling();
    }

    private void ApplyParameterDetailsPanelState()
    {
        if (parameterDetailsPanel == null) return;

        if (!displayRatioTierOpen)
        {
            SetActive(parameterDetailsPanel, false);
            return;
        }

        SetActive(parameterDetailsPanel, true);

        RectTransform panelRt = parameterDetailsPanel.GetComponent<RectTransform>();
        if (panelRt != null)
            SettingsParameterDetailsLayout.Apply(panelRt, parameterTitleTmp, parameterSubtitleTmp, parameterBodyTmp);

        if (parameterSubtitleTmp != null)
            parameterSubtitleTmp.gameObject.SetActive(true);

        ApplyParameterDetailsText(null);
    }

    private static GameObject ResolveNavHitArea(GameObject navRoot)
    {
        if (navRoot == null) return null;
        if (navRoot.GetComponent<Graphic>() != null)
            return navRoot;

        Transform label = navRoot.transform.Find("Text (TMP)");
        if (label != null && label.GetComponent<Graphic>() != null)
            return label.gameObject;

        return navRoot;
    }

    private void EnsurePresetNestedTopLayer()
    {
        if (presetNestedPanel == null) return;

        Canvas canvas = presetNestedPanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = presetNestedPanel.AddComponent<Canvas>();
            if (presetNestedPanel.GetComponent<GraphicRaycaster>() == null)
                presetNestedPanel.AddComponent<GraphicRaycaster>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = PresetNestedCanvasSortOrder;
    }

    private void BringPresetNestedToFront()
    {
        if (displayRatioRoot != null && presetSetButton != null)
            presetSetButton.transform.SetAsLastSibling();

        if (presetNestedPanel == null) return;

        EnsurePresetNestedTopLayer();
        presetNestedPanel.transform.SetAsLastSibling();
    }

    private void EnsureParameterFeedbackUi()
    {
        if (displayRatioRoot == null) return;

        parameterDetailsPanel = FindDeepChild(displayRatioRoot.transform, ParameterDetailsName);
        if (parameterDetailsPanel == null && displayRatioDetailBg != null)
            parameterDetailsPanel = CreateParameterDetailsPanel(displayRatioDetailBg.transform);

        if (parameterDetailsPanel != null)
        {
            BindParameterTextFields();
            SetActive(parameterDetailsPanel, false);
        }

        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;

        applyFeedbackToast = FindDeepChild(canvasGo.transform, ApplyFeedbackToastName);
        if (applyFeedbackToast == null)
            applyFeedbackToast = CreateApplyFeedbackToast(canvasGo.transform);

        applyFeedbackToastText = applyFeedbackToast != null
            ? applyFeedbackToast.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (applyFeedbackToastText != null)
            ApplySettingsLabelFont(applyFeedbackToastText);
        SetActive(applyFeedbackToast, false);
    }

    private void EnsureAboutSaveInfoUi()
    {
        if (aboutRoot == null) aboutRoot = GameObject.Find(AboutRootName);
        if (aboutRoot == null) return;

        if (aboutDetailBg == null)
            aboutDetailBg = FindDirectChild(aboutRoot.transform, AboutBgChildName);
        if (aboutDetailBg == null) return;

        aboutSaveDetailsPanel = FindDeepChild(aboutRoot.transform, AboutSaveDetailsName);
        if (aboutSaveDetailsPanel == null)
            aboutSaveDetailsPanel = CreateAboutSaveDetailsPanel(aboutDetailBg.transform);

        if (aboutSaveDetailsPanel == null) return;

        BindAboutSaveTextFieldsIfNeeded();
        RectTransform panelRt = aboutSaveDetailsPanel.GetComponent<RectTransform>();
        if (panelRt != null)
            SettingsParameterDetailsLayout.Apply(panelRt, aboutTitleTmp, aboutSubtitleTmp, aboutBodyTmp);
        ApplyAboutSaveInfoText();
        aboutSaveDetailsPanel.transform.SetAsLastSibling();
        SetActive(aboutSaveDetailsPanel, true);
    }

    private void BindAboutSaveTextFieldsIfNeeded()
    {
        if (aboutSaveDetailsPanel == null) return;

        if (aboutTitleTmp == null) aboutTitleTmp = ResolveAboutTmp(ParameterTitleNames);
        if (aboutSubtitleTmp == null) aboutSubtitleTmp = ResolveAboutTmp(ParameterSubtitleNames);
        if (aboutBodyTmp == null) aboutBodyTmp = ResolveAboutTmp(ParameterBodyNames);

        if (aboutTitleTmp != null) ApplySettingsLabelFont(aboutTitleTmp);
        if (aboutSubtitleTmp != null) ApplySettingsLabelFont(aboutSubtitleTmp);
        if (aboutBodyTmp != null) ApplySettingsLabelFont(aboutBodyTmp);
    }

    private TextMeshProUGUI ResolveAboutTmp(params string[] objectNames)
    {
        if (aboutSaveDetailsPanel == null || objectNames == null) return null;
        for (int i = 0; i < objectNames.Length; i++)
        {
            string objectName = objectNames[i];
            if (string.IsNullOrWhiteSpace(objectName)) continue;
            TextMeshProUGUI tmp = FindTmpOnChild(aboutSaveDetailsPanel, objectName);
            if (tmp != null) return tmp;
        }

        return null;
    }

    private GameObject CreateAboutSaveDetailsPanel(Transform parent)
    {
        GameObject panel = new GameObject(AboutSaveDetailsName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(24f, 24f);
        panelRt.offsetMax = new Vector2(-24f, -24f);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
        panelBg.raycastTarget = false;

        aboutTitleTmp = CreateParameterText(panel.transform, ParameterTitleNames[0], 48f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(1200f, 72f), TextAlignmentOptions.Center);
        aboutSubtitleTmp = CreateParameterText(panel.transform, ParameterSubtitleNames[0], 32f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(1200f, 48f), TextAlignmentOptions.Center);
        aboutBodyTmp = CreateParameterText(panel.transform, ParameterBodyNames[0], 26f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 400f), TextAlignmentOptions.TopLeft);

        SettingsParameterDetailsLayout.Apply(panelRt, aboutTitleTmp, aboutSubtitleTmp, aboutBodyTmp);
        return panel;
    }

    private void ApplyAboutSaveInfoText()
    {
        if (aboutTitleTmp != null)
        {
            aboutTitleTmp.text = AboutSaveInfoTitle;
            aboutTitleTmp.ForceMeshUpdate();
        }

        if (aboutSubtitleTmp != null)
        {
            aboutSubtitleTmp.text = AboutSaveInfoSubtitle;
            aboutSubtitleTmp.ForceMeshUpdate();
        }

        if (aboutBodyTmp != null)
        {
            aboutBodyTmp.gameObject.SetActive(true);
            aboutBodyTmp.text = AboutSaveInfoBody;
            SettingsParameterDetailsLayout.RefreshAfterTextChanged(aboutBodyTmp);
        }
    }

    private void BindParameterTextFields()
    {
        parameterTitleTmp = ResolveParameterTmp(ParameterTitleNames);
        parameterSubtitleTmp = ResolveParameterTmp(ParameterSubtitleNames);
        parameterBodyTmp = ResolveParameterTmp(ParameterBodyNames);

        if (parameterTitleTmp != null) ApplySettingsLabelFont(parameterTitleTmp);
        if (parameterSubtitleTmp != null) ApplySettingsLabelFont(parameterSubtitleTmp);
        if (parameterBodyTmp != null) ApplySettingsLabelFont(parameterBodyTmp);

        RectTransform panelRt = parameterDetailsPanel != null
            ? parameterDetailsPanel.GetComponent<RectTransform>()
            : null;
        if (panelRt != null)
            SettingsParameterDetailsLayout.Apply(panelRt, parameterTitleTmp, parameterSubtitleTmp, parameterBodyTmp);
    }

    private TextMeshProUGUI ResolveParameterTmp(params string[] objectNames)
    {
        if (objectNames == null) return null;
        for (int i = 0; i < objectNames.Length; i++)
        {
            string objectName = objectNames[i];
            if (string.IsNullOrWhiteSpace(objectName)) continue;

            TextMeshProUGUI tmp = FindTmpOnChild(parameterDetailsPanel, objectName);
            if (tmp != null) return tmp;
            if (displayRatioRoot != null)
            {
                tmp = FindTmpOnChild(displayRatioRoot, objectName);
                if (tmp != null) return tmp;
            }
        }

        return null;
    }

    private GameObject CreateParameterDetailsPanel(Transform parent)
    {
        GameObject panel = new GameObject(ParameterDetailsName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(24f, 24f);
        panelRt.offsetMax = new Vector2(-24f, -24f);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.08f, 0.1f, 0.14f, 0.88f);
        panelBg.raycastTarget = false;

        parameterTitleTmp = CreateParameterText(panel.transform, ParameterTitleNames[0], 48f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(1200f, 72f), TextAlignmentOptions.Center);
        parameterSubtitleTmp = CreateParameterText(panel.transform, ParameterSubtitleNames[0], 32f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(1200f, 48f), TextAlignmentOptions.Center);
        parameterBodyTmp = CreateParameterText(panel.transform, ParameterBodyNames[0], 26f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 400f), TextAlignmentOptions.TopLeft);

        SettingsParameterDetailsLayout.Apply(panelRt, parameterTitleTmp, parameterSubtitleTmp, parameterBodyTmp);
        return panel;
    }

    private TextMeshProUGUI CreateParameterText(
        Transform parent,
        string objectName,
        float fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        ApplySettingsLabelFont(tmp);
        return tmp;
    }

    private static GameObject CreateApplyFeedbackToast(Transform canvasTransform)
    {
        GameObject toast = new GameObject(ApplyFeedbackToastName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        toast.transform.SetParent(canvasTransform, false);
        RectTransform rt = toast.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(720f, 120f);

        Image bg = toast.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.18f, 0.94f);
        bg.raycastTarget = false;

        CanvasGroup cg = toast.GetComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(toast.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 40f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return toast;
    }

    private void ShowParameterDetails(BattleCardTuningPresetEntry entry)
    {
        if (parameterDetailsPanel == null) return;

        if (parameterTitleTmp == null || parameterSubtitleTmp == null || parameterBodyTmp == null)
            BindParameterTextFields();

        RectTransform panelRt = parameterDetailsPanel.GetComponent<RectTransform>();
        if (panelRt != null)
            SettingsParameterDetailsLayout.Apply(panelRt, parameterTitleTmp, parameterSubtitleTmp, parameterBodyTmp);

        ApplyParameterDetailsText(entry);

        SetActive(parameterDetailsPanel, true);
        parameterDetailsPanel.transform.SetAsLastSibling();

        if (presetNestedPanel != null && presetNestedPanel.activeSelf)
            BringPresetNestedToFront();
    }

    private void ApplyParameterDetailsText(BattleCardTuningPresetEntry entry)
    {
        if (parameterTitleTmp != null)
        {
            parameterTitleTmp.text = BattleCardTuningPresetDisplay.BuildComparisonTitle();
            parameterTitleTmp.ForceMeshUpdate();
        }

        if (parameterSubtitleTmp != null)
        {
            parameterSubtitleTmp.text = BattleCardTuningPresetDisplay.BuildComparisonSubtitle();
            parameterSubtitleTmp.ForceMeshUpdate();
        }

        if (parameterBodyTmp != null)
        {
            RectTransform viewport = parameterBodyTmp.rectTransform.parent as RectTransform;
            parameterBodyTmp.gameObject.SetActive(false);
            parameterBodyTmp.text = "";

            string selectedPresetId = entry != null ? entry.presetId : BattleCardTuningUserSettings.GetSelectedPresetId();
            SettingsParameterComparisonTable.Refresh(
                viewport,
                BattleCardTuningPresetDisplay.GetComparisonRows(),
                selectedPresetId);
            SettingsParameterDetailsLayout.RefreshAfterTableChanged(viewport);
        }
    }

    private void ShowApplyFeedbackToast(BattleCardTuningPresetEntry entry)
    {
        if (applyFeedbackToast == null) return;

        if (applyFeedbackToastText == null)
            applyFeedbackToastText = applyFeedbackToast.GetComponentInChildren<TextMeshProUGUI>(true);

        if (applyFeedbackToastText != null)
        {
            applyFeedbackToastText.text = BattleCardTuningPresetDisplay.BuildAppliedToastMessage(entry);
            ApplySettingsLabelFont(applyFeedbackToastText);
        }

        SetActive(applyFeedbackToast, true);
        applyFeedbackToast.transform.SetAsLastSibling();

        if (applyFeedbackRoutine != null)
            StopCoroutine(applyFeedbackRoutine);
        applyFeedbackRoutine = StartCoroutine(HideApplyFeedbackToastAfterDelay(2.2f));
    }

    private IEnumerator HideApplyFeedbackToastAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetActive(applyFeedbackToast, false);
        applyFeedbackRoutine = null;
    }

    private static TextMeshProUGUI FindTmpOnChild(GameObject parent, string childName)
    {
        if (parent == null) return null;
        Transform child = FindDeepChildTransform(parent.transform, childName);
        if (child == null) return null;
        TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp : child.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private static GameObject FindDeepChild(Transform root, string childName)
    {
        Transform found = FindDeepChildTransform(root, childName);
        return found != null ? found.gameObject : null;
    }

    private static Transform FindDeepChildTransform(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChildTransform(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    private void RefreshPresetButtonHighlights()
    {
        string selected = BattleCardTuningUserSettings.GetSelectedPresetId();
        for (int i = 0; i < presetChoiceButtons.Count; i++)
        {
            (Button btn, string presetId) entry = presetChoiceButtons[i];
            if (entry.btn == null) continue;
            Image img = entry.btn.targetGraphic as Image;
            if (img == null) continue;
            bool isSelected = string.Equals(entry.presetId, selected, System.StringComparison.OrdinalIgnoreCase);
            img.color = isSelected
                ? new Color(0.2f, 0.75f, 0.45f, 1f)
                : new Color(0.35f, 0.55f, 0.95f, 1f);
        }
    }

    private void RefreshQualityButtonHighlights()
    {
        int selected = BattleCardTuningUserSettings.GetSavedQualityLevel();
        for (int i = 0; i < qualityButtons.Count; i++)
        {
            Button btn = qualityButtons[i];
            if (btn == null) continue;
            Image img = btn.targetGraphic as Image;
            if (img == null) continue;
            img.color = i == selected
                ? new Color(0.2f, 0.75f, 0.45f, 1f)
                : new Color(0.28f, 0.62f, 0.88f, 1f);
        }
    }

    private static void BindButton(GameObject go, UnityEngine.Events.UnityAction onClick)
    {
        if (go == null || onClick == null) return;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        if (btn == null) return;

        Graphic graphic = go.GetComponent<Graphic>();
        if (graphic == null)
        {
            Image img = go.AddComponent<Image>();
            graphic = img;
        }

        if (graphic != null)
        {
            graphic.raycastTarget = true;
            btn.targetGraphic = graphic;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
    }

    private static GameObject FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private static void CopyRectLayout(RectTransform source, RectTransform destination)
    {
        if (source == null || destination == null) return;
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.sizeDelta = source.sizeDelta;
    }
}

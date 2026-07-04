using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GlobalNavRuntime : MonoBehaviour
{
    private void BuildUiRuntime()
    {
        PlayerProfileCsvService.SetRole("遊戲測試員");

        GameObject uiRoot = new GameObject("GlobalNavUI");
        uiRoot.transform.SetParent(transform, false);

        GameObject canvasObj = new GameObject("GlobalNavCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(uiRoot.transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject trigger = CreateButton(
            canvasObj.transform,
            "TriggerButton",
            "≡",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(28f, -28f),
            new Vector2(128f, 128f),
            new Color(0.53f, 0.36f, 0.78f, 0.95f),
            40f);

        GameObject panel = new GameObject("TabPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.94f, 0.9f, 0.82f, 0.98f);
        panelBg.type = Image.Type.Sliced;

        GameObject homeBtnObj = CreateNavTileButton(
            panel.transform,
            "HomeButton",
            "回首頁",
            new Vector2(0.5f, 0.68f),
            new Vector2(0.5f, 0.68f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f),
            30f);
        GameObject playerInfoBtnObj = CreateNavTileButton(
            panel.transform,
            "PlayerInfoButton",
            "玩家資訊",
            new Vector2(0.5f, 0.48f),
            new Vector2(0.5f, 0.48f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.35f, 0.56f, 0.34f, 0.98f),
            30f);
        GameObject valuablesVaultBtnObj = CreateNavTileButton(
            panel.transform,
            "ValuablesVaultButton",
            "貴重品庫",
            new Vector2(0.5f, 0.38f),
            new Vector2(0.5f, 0.38f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.62f, 0.48f, 0.28f, 0.98f),
            30f);
        GameObject backpackBtnObj = CreateNavTileButton(
            panel.transform,
            "BackpackButton",
            "背包",
            new Vector2(0.5f, 0.28f),
            new Vector2(0.5f, 0.28f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.24f, 0.47f, 0.32f, 0.98f),
            30f);
        GameObject settingsBtnObj = CreateNavTileButton(
            panel.transform,
            "SettingsButton",
            "遊戲設定",
            new Vector2(0.5f, 0.20f),
            new Vector2(0.5f, 0.20f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.38f, 0.32f, 0.58f, 0.98f),
            30f);
        GameObject goLoginBtnObj = CreateNavTileButton(
            panel.transform,
            "GoLoginButton",
            "回到登入頁面",
            new Vector2(0.5f, 0.14f),
            new Vector2(0.5f, 0.14f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(160f, 160f),
            new Color(0.42f, 0.37f, 0.22f, 0.98f),
            30f);
        view = uiRoot.AddComponent<GlobalNavView>();
        view.rootCanvas = canvas;
        view.triggerButtonObject = trigger;
        view.tabPanelObject = panel;
        view.triggerButton = trigger.GetComponent<Button>();
        view.homeButton = homeBtnObj.GetComponent<Button>();
        view.playerInfoButton = playerInfoBtnObj.GetComponent<Button>();
        view.closeButton = null;
        backpackButton = backpackBtnObj.GetComponent<Button>();
        valuablesVaultButton = valuablesVaultBtnObj.GetComponent<Button>();
        settingsButton = settingsBtnObj.GetComponent<Button>();
        goLoginButton = goLoginBtnObj.GetComponent<Button>();

        valuablesVaultOverlay = new GlobalNavValuablesVaultOverlay(
            ValuablesVaultFonts.ApplyTo,
            CreatePlayerInfoStyleCloseButton);
        if (view.rootCanvas != null)
            valuablesVaultOverlay.EnsureBuilt(view.rootCanvas.transform);

        if (view.rootCanvas != null) view.rootCanvas.sortingOrder = 6000;

        if (view.triggerButtonObject != null)
        {
            RectTransform rt = view.triggerButtonObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                float m = Mathf.Max(0f, config.triggerTopRightMargin);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(m, -m);
                float s = Mathf.Max(64f, config.triggerSize);
                rt.sizeDelta = new Vector2(s, s);
            }
        }

        if (view.triggerButton != null)
        {
            view.triggerButton.onClick.RemoveAllListeners();
            view.triggerButton.onClick.AddListener(() =>
            {
                bool willOpen = view.tabPanelObject != null && !view.tabPanelObject.activeSelf;
                SetTabPanelOpen(willOpen);
            });
        }

        ApplyTabPanelLayout();

        if (view.homeButton != null)
        {
            view.homeButton.onClick.RemoveAllListeners();
            view.homeButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadHomeScene();
            });
        }

        if (view.playerInfoButton != null)
        {
            view.playerInfoButton.onClick.RemoveAllListeners();
            view.playerInfoButton.onClick.AddListener(() =>
            {
                TogglePlayerInfoPanel();
                SetTabPanelOpen(false);
            });
        }

        if (valuablesVaultButton != null)
        {
            valuablesVaultButton.onClick.RemoveAllListeners();
            valuablesVaultButton.onClick.AddListener(() =>
            {
                ToggleValuablesVaultPanel();
                SetTabPanelOpen(false);
            });
        }

        if (backpackButton != null)
        {
            backpackButton.onClick.RemoveAllListeners();
            backpackButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadBackpackScene();
            });
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadSettingsScene();
            });
        }

        if (goLoginButton != null)
        {
            goLoginButton.onClick.RemoveAllListeners();
            goLoginButton.onClick.AddListener(() =>
            {
                SetTabPanelOpen(false);
                TryLoadLoginScene();
            });
        }

        RefreshNavFontAndApplyToAllTexts();
        SetTabPanelOpen(false);
    }

    private static GameObject CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = go.GetComponent<Image>();
        bg.color = bgColor;
        bg.type = Image.Type.Sliced;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        EnsureNavLabelFont();
        if (navLabelFont != null) tmp.font = navLabelFont;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return go;
    }

    private static GameObject CreateNavTileButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image bg = go.GetComponent<Image>();
        bg.color = bgColor;
        bg.type = Image.Type.Sliced;

        GameObject iconSlotObj = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        iconSlotObj.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconSlotObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.62f);
        iconRt.anchorMax = new Vector2(0.5f, 0.62f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(96f, 96f);
        Image iconImg = iconSlotObj.GetComponent<Image>();
        iconImg.color = new Color(1f, 1f, 1f, 0.2f);
        iconImg.type = Image.Type.Sliced;
        iconImg.raycastTarget = false;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.5f, 0f);
        tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -8f);
        tr.sizeDelta = new Vector2(size.x + 28f, 40f);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        EnsureNavLabelFont();
        if (navLabelFont != null) tmp.font = navLabelFont;
        tmp.fontSize = fontSize - 2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return go;
    }

    private static void ApplyPlayerInfoFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        EnsureNavLabelFont();
        if (navLabelFont != null)
            tmp.font = navLabelFont;
        SettingsUiFonts.ApplyTo(tmp);
    }

    private static void EnsureNavLabelFont()
    {
        if (navLabelFont != null && FontSupportsRequiredGlyphs(navLabelFont)) return;

        TMP_FontAsset font = SettingsUiFonts.ResolveParameterDetailsFont();
        if (font != null && FontSupportsRequiredGlyphs(font))
        {
            navLabelFont = font;
            return;
        }

        font = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (font != null && FontSupportsRequiredGlyphs(font))
        {
            navLabelFont = font;
            return;
        }

        navLabelFont = UiFontResolver.ResolveUiFont();
    }

    private static bool FontSupportsRequiredGlyphs(TMP_FontAsset font) =>
        BuildbeckUiFonts.FontSupportsText(font, PlayerInfoProgressCopy.FontGlyphProbe) &&
        BuildbeckUiFonts.FontSupportsText(font, ValuablesVaultDisplay.FontGlyphProbe);

    private static bool FontNameLikelySupportsCjk(string fontAssetName)
    {
        if (string.IsNullOrEmpty(fontAssetName)) return false;
        string n = fontAssetName.ToLowerInvariant();
        return n.Contains("noto") ||
               n.Contains("cjk") ||
               n.Contains("sourcehansans") ||
               n.Contains("source han") ||
               n.Contains("jhenghei") ||
               n.Contains("yahei") ||
               n.Contains("pingfang") ||
               n.Contains("applesdgothic") ||
               n.Contains("nanum") ||
               n.Contains("mplus") ||
               (n.Contains("han") && (n.Contains("sans") || n.Contains("serif")));
    }

    private void ApplyTabPanelLayout()
    {
        if (view == null || view.tabPanelObject == null) return;
        RectTransform panelRt = view.tabPanelObject.GetComponent<RectTransform>();
        if (panelRt == null) return;

        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.offsetMin = new Vector2(TabPanelLeftMargin, TabPanelBottomMargin);
        panelRt.offsetMax = new Vector2(-TabPanelRightMargin, -TabPanelTopMargin);

        ResizeTabButton(view.homeButton, 0.10f, 0.5f);
        ResizeTabButton(view.playerInfoButton, 0.26f, 0.5f);
        ResizeTabButton(valuablesVaultButton, 0.42f, 0.5f);
        ResizeTabButton(backpackButton, 0.58f, 0.5f);
        ResizeTabButton(settingsButton, 0.74f, 0.5f);
        ResizeTabButton(goLoginButton, 0.90f, 0.5f);
    }

    private static void ResizeTabButton(Button button, float anchorX, float anchorY)
    {
        if (button == null) return;
        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 200f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.fontSize = 28f;
    }

    private void SetTabPanelOpen(bool open)
    {
        if (view == null || view.tabPanelObject == null) return;
        view.tabPanelObject.SetActive(open);

        Image panelImage = view.tabPanelObject.GetComponent<Image>();
        if (panelImage != null) panelImage.raycastTarget = open;

        if (view.homeButton != null) view.homeButton.interactable = open;
        if (view.playerInfoButton != null) view.playerInfoButton.interactable = open;
        if (valuablesVaultButton != null) valuablesVaultButton.interactable = open;
        if (backpackButton != null) backpackButton.interactable = open;
        if (settingsButton != null) settingsButton.interactable = open;
        if (goLoginButton != null) goLoginButton.interactable = open;
        if (view.closeButton != null) view.closeButton.interactable = open;
    }

    private static GameObject CreatePlayerInfoStyleCloseButton(Transform headerParent)
    {
        return CreateButton(
            headerParent,
            "CloseButton",
            "關閉",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -18f),
            new Vector2(124f, 54f),
            new Color(0.45f, 0.29f, 0.24f, 0.96f),
            28f);
    }
}

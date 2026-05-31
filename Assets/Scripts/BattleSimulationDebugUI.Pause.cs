using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private const float PauseCardWidth = 500f;
    private const float PauseCardHeight = 500f;

    private TextMeshProUGUI pauseDifficultyCaptionText;
    private TextMeshProUGUI pauseDifficultyValueText;

    private void CreatePauseUI(Transform parent)
    {
        Button pauseToggleBtn = CreateButton(parent, "PauseToggleButton", "暫停", new Vector2(0f, -240f), TogglePause, true);
        if (pauseToggleBtn != null)
        {
            pauseToggleBtn.onClick.RemoveAllListeners();
            pauseToggleBtn.onClick.AddListener(() => SetPaused(true));
        }

        RectTransform pauseToggleRt = pauseToggleBtn != null ? pauseToggleBtn.GetComponent<RectTransform>() : null;
        if (pauseToggleRt != null)
        {
            pauseToggleRt.anchorMin = new Vector2(1f, 1f);
            pauseToggleRt.anchorMax = new Vector2(1f, 1f);
            pauseToggleRt.pivot = new Vector2(1f, 1f);
            pauseToggleRt.anchoredPosition = new Vector2(-24f, -24f);
            pauseToggleRt.sizeDelta = new Vector2(112f, 48f);
            Text toggleLabel = pauseToggleBtn.GetComponentInChildren<Text>();
            if (toggleLabel != null) toggleLabel.fontSize = 26;
            BattleUiColors.ApplyButtonStyle(pauseToggleBtn, pauseToggleBtn.gameObject.name);
        }

        GameObject pausePanelObj = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanelObj.transform.SetParent(parent, false);
        pausePanel = pausePanelObj.GetComponent<RectTransform>();
        pausePanel.anchorMin = Vector2.zero;
        pausePanel.anchorMax = Vector2.one;
        pausePanel.offsetMin = Vector2.zero;
        pausePanel.offsetMax = Vector2.zero;
        Image pauseBg = pausePanelObj.GetComponent<Image>();
        pauseBg.color = BattleUiColors.DimHeavy;
        pauseBg.raycastTarget = true;

        GameObject pauseCardObj = new GameObject("PauseCard", typeof(RectTransform), typeof(Image), typeof(Outline));
        pauseCardObj.transform.SetParent(pausePanelObj.transform, false);
        RectTransform pauseCardRt = pauseCardObj.GetComponent<RectTransform>();
        pauseCardRt.anchorMin = new Vector2(0.5f, 0.5f);
        pauseCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        pauseCardRt.pivot = new Vector2(0.5f, 0.5f);
        pauseCardRt.anchoredPosition = Vector2.zero;
        pauseCardRt.sizeDelta = new Vector2(PauseCardWidth, PauseCardHeight);
        Image pauseCardBg = pauseCardObj.GetComponent<Image>();
        pauseCardBg.color = BattleUiColors.PanelMilk985;
        Outline cardOutline = pauseCardObj.GetComponent<Outline>();
        cardOutline.effectColor = BattleUiColors.PanelEdge35;
        cardOutline.effectDistance = new Vector2(2f, -2f);

        CreatePauseTmpBlock(
            pauseCardObj.transform,
            "PauseTitle",
            new Vector2(0f, 168f),
            new Vector2(440f, 72f),
            52f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            BattleUiColors.Ink,
            "暫停");

        CreatePauseTmpBlock(
            pauseCardObj.transform,
            "PauseSubtitle",
            new Vector2(0f, 118f),
            new Vector2(400f, 40f),
            24f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            BattleUiColors.InkSoft,
            "對戰已暫停");

        GameObject dividerObj = new GameObject("PauseDivider", typeof(RectTransform), typeof(Image));
        dividerObj.transform.SetParent(pauseCardObj.transform, false);
        RectTransform dividerRt = dividerObj.GetComponent<RectTransform>();
        dividerRt.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRt.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRt.pivot = new Vector2(0.5f, 0.5f);
        dividerRt.anchoredPosition = new Vector2(0f, 88f);
        dividerRt.sizeDelta = new Vector2(360f, 2f);
        dividerObj.GetComponent<Image>().color = BattleUiColors.PanelEdge35;

        GameObject diffPanelObj = new GameObject("PauseDifficultyPanel", typeof(RectTransform), typeof(Image));
        diffPanelObj.transform.SetParent(pauseCardObj.transform, false);
        RectTransform diffPanelRt = diffPanelObj.GetComponent<RectTransform>();
        diffPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        diffPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        diffPanelRt.pivot = new Vector2(0.5f, 0.5f);
        diffPanelRt.anchoredPosition = new Vector2(0f, 28f);
        diffPanelRt.sizeDelta = new Vector2(400f, 108f);
        diffPanelObj.GetComponent<Image>().color = BattleUiColors.WithAlpha(BattleUiColors.PanelScroll, 0.55f);

        pauseDifficultyCaptionText = CreatePauseTmpBlock(
            diffPanelObj.transform,
            "DifficultyCaption",
            new Vector2(0f, 22f),
            new Vector2(360f, 32f),
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            BattleUiColors.InkSoft,
            "目前關卡難易度");

        pauseDifficultyValueText = CreatePauseTmpBlock(
            diffPanelObj.transform,
            "DifficultyValue",
            new Vector2(0f, -18f),
            new Vector2(360f, 52f),
            40f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            BattleUiColors.Ink,
            FormatPauseDifficultyTierLabel(BattleDifficultyRuntime.DefaultLabelZh));

        Button resumeBtn = CreatePauseMenuButton(
            pauseCardObj.transform,
            "ResumeButton",
            "繼續對戰",
            new Vector2(0f, -82f),
            new Vector2(340f, 72f),
            () => SetPaused(false),
            primary: true);
        if (resumeBtn != null)
            BattleUiColors.ApplyButtonStyle(resumeBtn, "ResumeButton");

        Button restartBtn = CreatePauseMenuButton(
            pauseCardObj.transform,
            "PauseRestartButton",
            "重新開始",
            new Vector2(-98f, -168f),
            new Vector2(200f, 60f),
            OnPauseRestartClicked,
            primary: false);
        if (restartBtn != null)
            BattleUiColors.ApplyButtonStyle(restartBtn, restartBtn.gameObject.name);

        Button giveUpBtn = CreatePauseMenuButton(
            pauseCardObj.transform,
            "PauseGiveUpButton",
            "放棄對戰",
            new Vector2(98f, -168f),
            new Vector2(200f, 60f),
            OnPauseGiveUpClicked,
            primary: false);
        if (giveUpBtn != null)
            BattleUiColors.ApplyButtonStyle(giveUpBtn, giveUpBtn.gameObject.name);

        pausePanelObj.SetActive(false);
    }

    private void RefreshPausePanelPresentation()
    {
        string labelZh = ResolvePauseDifficultyLabelZh();
        if (pauseDifficultyValueText == null) return;

        pauseDifficultyValueText.text = FormatPauseDifficultyTierLabel(labelZh);
        pauseDifficultyValueText.color = GetPauseDifficultyAccentColor(labelZh);
    }

    private string ResolvePauseDifficultyLabelZh()
    {
        if (battleManager != null)
        {
            string fromManager = battleManager.GetBattleDifficultyLabelForRecord();
            if (!string.IsNullOrWhiteSpace(fromManager))
                return fromManager.Trim();
        }

        string resolved = BattleDifficultyRuntime.ResolveForBattleRecord();
        if (!string.IsNullOrWhiteSpace(resolved))
            return resolved.Trim();

        return BattleDifficultyRuntime.DefaultLabelZh;
    }

    private static string FormatPauseDifficultyTierLabel(string labelZh)
    {
        if (string.IsNullOrWhiteSpace(labelZh))
            return BattleDifficultyRuntime.DefaultLabelZh + "級";

        string trimmed = labelZh.Trim();
        if (trimmed.EndsWith("級", System.StringComparison.Ordinal))
            return trimmed;

        return trimmed + "級";
    }

    private static Color GetPauseDifficultyAccentColor(string labelZh)
    {
        if (string.IsNullOrWhiteSpace(labelZh))
            return BattleUiColors.Ink;

        string n = labelZh.Trim();
        if (n.Contains("魔王")) return new Color(0.88f, 0.14f, 0.78f, 1f);
        if (n.Contains("困難") || n.Contains("困难")) return new Color(1f, 0.36f, 0.08f, 1f);
        if (n.Contains("普通")) return new Color(1f, 0.84f, 0.08f, 1f);
        if (n.Contains("簡單") || n.Contains("简单")) return new Color(0.38f, 0.98f, 0.22f, 1f);
        if (n.Contains("入門") || n.Contains("入门")) return new Color(0.18f, 0.82f, 0.92f, 1f);
        return BattleUiColors.Ink;
    }

    private TextMeshProUGUI CreatePauseTmpBlock(
        Transform parent,
        string objName,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color,
        string text)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) tmp.font = sharedUIFont;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.text = text;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreatePauseMenuButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        UnityEngine.Events.UnityAction onClick,
        bool primary)
    {
        Button btn = CreateButton(parent, name, label, Vector2.zero, onClick, true);
        if (btn == null) return null;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Text legacyLabel = btn.GetComponentInChildren<Text>();
        if (legacyLabel != null)
            legacyLabel.fontSize = primary ? 32 : 26;

        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = primary ? BattleUiColors.BtnPrimary : BattleUiColors.BtnSecondary;

        return btn;
    }

    private IEnumerator CoShowCenterBattleMessagePrompt(string title, string subtitle, float holdSeconds)
    {
        if (uiRoot == null) yield break;

        GameObject overlayObj = new GameObject("BattleCenterPrompt", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlayObj.transform.SetParent(uiRoot, false);
        overlayObj.transform.SetAsLastSibling();
        RectTransform overlayRt = overlayObj.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        Image overlayImg = overlayObj.GetComponent<Image>();
        overlayImg.color = BattleUiColors.DimHeavy;
        overlayImg.raycastTarget = true;
        CanvasGroup overlayCg = overlayObj.GetComponent<CanvasGroup>();
        overlayCg.alpha = 0f;

        GameObject cardObj = new GameObject("PromptCard", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardObj.transform.SetParent(overlayObj.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(520f, 280f);
        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = BattleUiColors.PanelMilk985;
        Outline cardOutline = cardObj.GetComponent<Outline>();
        cardOutline.effectColor = BattleUiColors.WithAlpha(BattleUiColors.FoeHp, 0.55f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        CreatePauseTmpBlock(
            cardObj.transform,
            "PromptTitle",
            new Vector2(0f, 36f),
            new Vector2(460f, 88f),
            64f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            BattleUiColors.FoeHp,
            title);

        CreatePauseTmpBlock(
            cardObj.transform,
            "PromptSubtitle",
            new Vector2(0f, -28f),
            new Vector2(440f, 48f),
            28f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            BattleUiColors.InkSoft,
            subtitle);

        const float fadeIn = 0.22f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            overlayCg.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }

        overlayCg.alpha = 1f;
        if (holdSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdSeconds);

        const float fadeOut = 0.18f;
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            overlayCg.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }

        if (overlayObj != null)
            Destroy(overlayObj);
    }
}

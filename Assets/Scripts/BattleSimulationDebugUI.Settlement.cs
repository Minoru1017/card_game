using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private void CreateBattleResultText(Transform parent, bool useDebugPanelLayout = false)
    {
        battleResultTextUsesDebugPanelLayout = useDebugPanelLayout;
        GameObject txtObj = new GameObject("BattleResultText", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow), typeof(CanvasGroup));
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        if (useDebugPanelLayout)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(10f, -282f);
            rt.offsetMax = new Vector2(-10f, -222f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(980f, 180f);
        }

        battleResultText = txtObj.GetComponent<Text>();
        battleResultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        battleResultText.fontSize = useDebugPanelLayout ? Mathf.RoundToInt(18 * DebugUiChromeMul) : 56;
        battleResultText.alignment = TextAnchor.MiddleCenter;
        battleResultText.horizontalOverflow = HorizontalWrapMode.Wrap;
        battleResultText.verticalOverflow = VerticalWrapMode.Overflow;
        battleResultText.color = Color.white;
        battleResultText.text = string.Empty;
        battleResultText.raycastTarget = false;

        Outline outline = txtObj.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = useDebugPanelLayout ? new Vector2(1f, -1f) : new Vector2(2f, -2f);

        Shadow shadow = txtObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = useDebugPanelLayout ? new Vector2(2f, -2f) : new Vector2(4f, -4f);

        battleResultGroup = txtObj.GetComponent<CanvasGroup>();
        battleResultGroup.alpha = 0f;
        battleResultGroup.blocksRaycasts = false;
    }

    private void UpdateBattleResultText()
    {
        if (lockBattleResultAutoUpdate) return;
        if (battleResultText == null || battleManager == null) return;
        string ruleMsg = battleManager.GetBattleRuleMessage();
        if (!string.IsNullOrEmpty(ruleMsg))
        {
            battleResultText.text = ruleMsg;
            battleResultText.fontSize = battleResultTextUsesDebugPanelLayout
                ? Mathf.RoundToInt(20 * DebugUiChromeMul)
                : 40;
            battleResultText.color = new Color(1f, 0.82f, 0.3f, 1f);
            if (battleResultGroup != null) battleResultGroup.alpha = 1f;
            HideEndBattlePanel();
            return;
        }

        if (!battleManager.IsBattleOver())
        {
            battleResultText.text = string.Empty;
            lastShownBattleResult = 0;
            if (battleResultGroup != null) battleResultGroup.alpha = 0f;
            endBattlePanelShown = false;
            HideEndBattlePanel();
            return;
        }

        int result = battleManager.GetBattleResult();
        if (result == lastShownBattleResult) return;
        battleResultText.text = string.Empty;
        if (battleResultGroup != null) battleResultGroup.alpha = 0f;

        lastShownBattleResult = result;
        ShowEndBattlePanel();
    }


    private IEnumerator FadeInBattleResultText()
    {
        if (battleResultGroup == null) yield break;
        battleResultGroup.alpha = 0f;
        float t = 0f;
        const float duration = 0.45f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            battleResultGroup.alpha = p;
            yield return null;
        }
        battleResultGroup.alpha = 1f;
        battleResultFadeRoutine = null;
    }

    private void ShowEndBattlePanel()
    {
        if (endBattlePanelShown) return;
        EnsureEndBattlePanel();
        if (endBattlePanel == null) return;
        if (settlementFreezeRoutine != null)
        {
            StopCoroutine(settlementFreezeRoutine);
            settlementFreezeRoutine = null;
        }
        settlementFreezeRoutine = StartCoroutine(CoShowEndBattlePanelWithFreeze());
    }

    private void HideEndBattlePanel()
    {
        HideBattleHistoryOverlay();
        if (settlementFreezeRoutine != null)
        {
            StopCoroutine(settlementFreezeRoutine);
            settlementFreezeRoutine = null;
            if (endBattlePanel == null || !endBattlePanel.activeSelf)
                endBattlePanelShown = false;
        }
        RestoreSettlementBattleUiIfNeeded();
        if (endBattlePanel != null && endBattlePanelGroup != null)
        {
            endBattlePanelGroup.alpha = 1f;
            endBattlePanel.transform.localScale = Vector3.one;
        }
        if (endBattlePanel != null) endBattlePanel.SetActive(false);
    }

    private void ReleaseSettlementFreezeResources()
    {
        // OnDestroy 時整棵 UI 可能已在銷毀流程，不可對其他 GameObject 呼叫 SetActive。
        RestoreSettlementBattleUiIfNeeded(skipChildRestoreBecauseDestroy: true);
    }

    private void EnsureSettlementFreezeRoot()
    {
        if (settlementFreezeRoot != null || uiRoot == null) return;

        settlementFreezeRoot = new GameObject("BattleSettlementFreezeRoot", typeof(RectTransform), typeof(CanvasGroup));
        settlementFreezeRoot.transform.SetParent(uiRoot, false);
        RectTransform rootRt = settlementFreezeRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.localScale = Vector3.one;
        CanvasGroup rootCg = settlementFreezeRoot.GetComponent<CanvasGroup>();
        rootCg.alpha = 1f;
        rootCg.blocksRaycasts = true;
        rootCg.interactable = false;

        GameObject shotGo = new GameObject("SettlementScreenshot", typeof(RectTransform), typeof(RawImage));
        shotGo.transform.SetParent(settlementFreezeRoot.transform, false);
        RectTransform shotRt = shotGo.GetComponent<RectTransform>();
        shotRt.anchorMin = Vector2.zero;
        shotRt.anchorMax = Vector2.one;
        shotRt.offsetMin = Vector2.zero;
        shotRt.offsetMax = Vector2.zero;
        shotRt.localScale = Vector3.one;
        settlementFreezeRawImage = shotGo.GetComponent<RawImage>();
        settlementFreezeRawImage.raycastTarget = false;
        settlementFreezeRawImage.color = Color.white;

        GameObject dimGo = new GameObject("SettlementFreezeDim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(settlementFreezeRoot.transform, false);
        RectTransform dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        dimRt.localScale = Vector3.one;
        Image dimImg = dimGo.GetComponent<Image>();
        dimImg.sprite = GetUnitWhiteSprite();
        dimImg.type = Image.Type.Simple;
        dimImg.color = new Color(0f, 0f, 0f, 0.7f);
        dimImg.raycastTarget = true;

        settlementFreezeRoot.SetActive(false);
    }

    private void RestoreSettlementBattleUiIfNeeded(bool skipChildRestoreBecauseDestroy = false)
    {
        if (settlementBattleUiSuppressed)
        {
            if (!skipChildRestoreBecauseDestroy)
            {
                for (int i = 0; i < settlementRestoreTransforms.Count; i++)
                {
                    Transform t = settlementRestoreTransforms[i];
                    if (t == null) continue;
                    GameObject go = t.gameObject;
                    if (!go) continue;
                    if (!go.scene.IsValid()) continue;
                    bool want = i < settlementRestoreActive.Count && settlementRestoreActive[i];
                    go.SetActive(want);
                }
            }
            settlementRestoreTransforms.Clear();
            settlementRestoreActive.Clear();
            settlementBattleUiSuppressed = false;
        }

        if (settlementFreezeCaptureTexture != null)
        {
            Destroy(settlementFreezeCaptureTexture);
            settlementFreezeCaptureTexture = null;
        }
        if (settlementFreezeRawImage != null)
            settlementFreezeRawImage.texture = null;
        if (settlementFreezeRoot != null && !skipChildRestoreBecauseDestroy)
            settlementFreezeRoot.SetActive(false);
    }

    private IEnumerator CoShowEndBattlePanelWithFreeze()
    {
        endBattlePanelShown = true;
        if (endBattleTitleText != null && battleManager != null)
        {
            int result = battleManager.GetBattleResult();
            if (result == 1) endBattleTitleText.text = "Victory";
            else if (result == -1) endBattleTitleText.text = "Defeat";
            else endBattleTitleText.text = "Draw";
        }

        if (endBattlePanel != null)
            endBattlePanel.SetActive(false);

        EnsureSettlementFreezeRoot();

        Canvas.ForceUpdateCanvases();
        yield return null;
        yield return new WaitForEndOfFrame();

        bool captured = false;
        if (!BattleAutoSimPlugin.IsRunning && uiRoot != null && settlementFreezeRawImage != null)
        {
            int w = Screen.width;
            int h = Screen.height;
            if (w > 0 && h > 0)
            {
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                try
                {
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    if (settlementCaptureFlipTextureY)
                        FlipTextureVerticallyForScreenCapture(tex);
                    tex.Apply(false);
                    captured = true;
                    if (settlementFreezeCaptureTexture != null)
                        Destroy(settlementFreezeCaptureTexture);
                    settlementFreezeCaptureTexture = tex;

                    settlementFreezeRawImage.texture = settlementFreezeCaptureTexture;
                }
                catch (Exception)
                {
                    Destroy(tex);
                }
            }
        }

        if (captured && uiRoot != null)
        {
            settlementRestoreTransforms.Clear();
            settlementRestoreActive.Clear();
            for (int i = 0; i < uiRoot.childCount; i++)
            {
                Transform child = uiRoot.GetChild(i);
                if (child == null) continue;
                GameObject go = child.gameObject;
                if (go == settlementFreezeRoot || go == endBattlePanel)
                    continue;
                settlementRestoreTransforms.Add(child);
                settlementRestoreActive.Add(go.activeSelf);
                go.SetActive(false);
            }
            settlementBattleUiSuppressed = true;
            settlementFreezeRoot.SetActive(true);
            settlementFreezeRoot.transform.SetAsLastSibling();
        }

        if (endBattlePanel != null)
        {
            endBattlePanel.SetActive(true);
            endBattlePanel.transform.SetAsLastSibling();
            RectTransform endPanelRt = endBattlePanel.GetComponent<RectTransform>();
            if (endBattlePanelGroup != null && endPanelRt != null)
            {
                endBattlePanelGroup.alpha = 0f;
                const float startScale = 0.92f;
                endPanelRt.localScale = Vector3.one * startScale;
                const float dur = 0.36f;
                float t = 0f;
                while (t < dur && endBattlePanel != null && endBattlePanel.activeInHierarchy)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / dur);
                    float eased = p * p * (3f - 2f * p);
                    endBattlePanelGroup.alpha = eased;
                    endPanelRt.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, eased);
                    yield return null;
                }
                if (endBattlePanelGroup != null) endBattlePanelGroup.alpha = 1f;
                if (endPanelRt != null) endPanelRt.localScale = Vector3.one;
            }
        }

        settlementFreezeRoutine = null;
    }

    private void EnsureEndBattlePanel()
    {
        if (endBattlePanel != null || uiRoot == null) return;

        endBattlePanel = new GameObject("EndBattlePanel", typeof(RectTransform), typeof(Image));
        endBattlePanel.transform.SetParent(uiRoot, false);
        RectTransform panelRect = endBattlePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(960f, 520f);
        panelRect.localScale = Vector3.one;
        Image bg = endBattlePanel.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

        GameObject titleObj = new GameObject("EndBattleTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(endBattlePanel.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 118f);
        titleRect.sizeDelta = new Vector2(560f, 96f);
        endBattleTitleText = titleObj.GetComponent<Text>();
        endBattleTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        endBattleTitleText.fontSize = 64;
        endBattleTitleText.alignment = TextAnchor.MiddleCenter;
        endBattleTitleText.color = new Color(1f, 0.93f, 0.25f, 1f);
        endBattleTitleText.text = "Victory";

        CreateEndBattleButton(endBattlePanel.transform, "BattleHistoryButton", "對戰歷史", new Vector2(-240f, -128f), OnClickBattleHistory);
        CreateEndBattleButton(endBattlePanel.transform, "RestartBattleButton", "Rematch", new Vector2(0f, -128f), OnClickRestartBattle);
        CreateEndBattleButton(endBattlePanel.transform, "ReturnBuildbeckButton", "Deck builder", new Vector2(240f, -128f), OnClickReturnBuildbeck);
        endBattlePanelGroup = endBattlePanel.AddComponent<CanvasGroup>();
        endBattlePanelGroup.blocksRaycasts = true;
        endBattlePanelGroup.interactable = true;
        endBattlePanelGroup.alpha = 1f;
        endBattlePanel.SetActive(false);
    }

    private void OnClickBattleHistory()
    {
        if (battleManager == null) return;
        EnsureBattleHistoryOverlay();
        if (battleHistoryOverlayRoot == null || battleHistoryContentTmp == null) return;

        battleHistoryContentTmp.text = FormatBattleHistoryRichText(battleManager.GetBattleHistoryFullText());
        battleHistoryOverlayRoot.SetActive(true);
        battleHistoryOverlayRoot.transform.SetAsLastSibling();

        RectTransform contentRt = battleHistoryContentTmp.rectTransform;
        Canvas.ForceUpdateCanvases();
        float wrapWidth = 640f * BattleHistoryDialogScale;
        if (battleHistoryScrollRect != null && battleHistoryScrollRect.viewport != null)
        {
            float pad = 28f * BattleHistoryDialogScale;
            wrapWidth = Mathf.Max(120f * BattleHistoryDialogScale, battleHistoryScrollRect.viewport.rect.width - pad);
        }
        Vector2 preferred = battleHistoryContentTmp.GetPreferredValues(battleHistoryContentTmp.text, wrapWidth, 0f);
        float minBodyH = 80f * BattleHistoryDialogScale;
        float bodyPad = 16f * BattleHistoryDialogScale;
        contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, Mathf.Max(minBodyH, preferred.y + bodyPad));
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        if (battleHistoryScrollRect != null)
            battleHistoryScrollRect.verticalNormalizedPosition = 1f;
    }

    private void HideBattleHistoryOverlay()
    {
        if (battleHistoryOverlayRoot != null)
            battleHistoryOverlayRoot.SetActive(false);
    }

    /// <summary>對戰歷史內文：怪物牌＋卡名（高明度橘）、法術牌＋卡名（高明度紫）、數字與「點傷害」間可有空白時仍套用高明度紅。</summary>
    private static string FormatBattleHistoryRichText(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;

        const string monsterOpen = "<color=#FFAA55>";
        const string spellOpen = "<color=#DD88FF>";
        const string damageOpen = "<color=#FF5555>";
        const string healOpen = "<color=#66FF7A>";
        const string outcomeOpen = "<color=#FFE45C>";
        const string colorClose = "</color>";

        string[] lines = plain.Split(new[] { '\n' }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;

            string trimmed = line.Trim();
            bool battleStartLine =
                trimmed == "對戰開始" ||
                Regex.IsMatch(trimmed, @"^我方骰\d+敵方骰\d+$") ||
                trimmed == "我方先手" ||
                trimmed == "敵方先手";
            bool battleEndTitleLine = trimmed == "對戰結束";
            bool battleOutcomeLine = trimmed == "我方戰敗" || trimmed == "我方勝利";

            string s = line.Replace("<", "＜").Replace(">", "＞");

            s = Regex.Replace(
                s,
                @"怪物牌\s+.+?(?=\s+對|\s+反擊)",
                m => monsterOpen + m.Value + colorClose);

            s = Regex.Replace(
                s,
                @"法術牌\s+.+?(?=\s+對)",
                m => spellOpen + m.Value + colorClose);

            s = Regex.Replace(
                s,
                @"\d+\s*點傷害",
                m => damageOpen + m.Value + colorClose);

            s = Regex.Replace(
                s,
                @"\d+\s*點生命值",
                m => healOpen + m.Value + colorClose);

            if (battleStartLine)
                s = "<b>" + s + "</b>";
            if (battleEndTitleLine)
                s = "<b>" + s + "</b>";
            if (battleOutcomeLine)
                s = "<b>" + outcomeOpen + s + colorClose + "</b>";

            lines[i] = s;
        }

        return string.Join("\n", lines);
    }

    private void EnsureBattleHistoryOverlay()
    {
        if (battleHistoryOverlayRoot != null || uiRoot == null) return;

        float s = BattleHistoryDialogScale;

        GameObject root = new GameObject("BattleHistoryOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(uiRoot, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        Image rootDim = root.GetComponent<Image>();
        rootDim.color = new Color(0f, 0f, 0f, 0.74f);
        rootDim.raycastTarget = true;
        Button dimClose = root.GetComponent<Button>();
        dimClose.targetGraphic = rootDim;
        dimClose.onClick.AddListener(HideBattleHistoryOverlay);

        GameObject dlg = new GameObject("BattleHistoryDialog", typeof(RectTransform), typeof(Image));
        dlg.transform.SetParent(root.transform, false);
        RectTransform dlgRt = dlg.GetComponent<RectTransform>();
        dlgRt.anchorMin = new Vector2(0.5f, 0.5f);
        dlgRt.anchorMax = new Vector2(0.5f, 0.5f);
        dlgRt.pivot = new Vector2(0.5f, 0.5f);
        dlgRt.anchoredPosition = Vector2.zero;
        dlgRt.sizeDelta = new Vector2(
            Mathf.Min(1020f, Screen.width - 32f) * s,
            Mathf.Min(780f, Screen.height - 48f) * s);
        Image dlgBg = dlg.GetComponent<Image>();
        dlgBg.color = new Color(0.12f, 0.13f, 0.16f, 1f);
        dlgBg.raycastTarget = true;

        GameObject titleGo = new GameObject("BattleHistoryTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(dlg.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -10f * s);
        titleRt.sizeDelta = new Vector2(-140f * s, 44f * s);
        TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) titleTmp.font = sharedUIFont;
        titleTmp.fontSize = 34f * s;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.text = "本局對戰歷史紀錄";
        titleTmp.raycastTarget = false;

        GameObject closeBtnObj = new GameObject("CloseBattleHistoryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(dlg.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-12f * s, -8f * s);
        closeRt.sizeDelta = new Vector2(118f * s, 44f * s);
        closeBtnObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(HideBattleHistoryOverlay);
        GameObject closeLbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
        closeLbl.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeLblRt = closeLbl.GetComponent<RectTransform>();
        closeLblRt.anchorMin = Vector2.zero;
        closeLblRt.anchorMax = Vector2.one;
        closeLblRt.offsetMin = Vector2.zero;
        closeLblRt.offsetMax = Vector2.zero;
        Text closeTxt = closeLbl.GetComponent<Text>();
        closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeTxt.text = "關閉";
        closeTxt.fontSize = Mathf.RoundToInt(22f * s);
        closeTxt.alignment = TextAnchor.MiddleCenter;
        closeTxt.color = Color.black;

        GameObject scrollGo = new GameObject("BattleHistoryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(dlg.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(14f * s, 16f * s);
        scrollRt.offsetMax = new Vector2(-14f * s, -58f * s);
        scrollGo.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.09f, 1f);
        ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 28f * s;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(6f * s, 6f * s);
        vpRt.offsetMax = new Vector2(-6f * s, -6f * s);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.GetComponent<Image>().raycastTarget = true;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(-16f * s, 400f * s);

        battleHistoryContentTmp = content.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) battleHistoryContentTmp.font = sharedUIFont;
        battleHistoryContentTmp.fontSize = 22f * s;
        battleHistoryContentTmp.alignment = TextAlignmentOptions.TopLeft;
        battleHistoryContentTmp.color = new Color(0.94f, 0.94f, 0.96f, 1f);
        battleHistoryContentTmp.enableWordWrapping = true;
        battleHistoryContentTmp.richText = true;
        battleHistoryContentTmp.text = string.Empty;
        battleHistoryContentTmp.raycastTarget = true;

        sr.content = cRt;
        sr.viewport = vpRt;
        battleHistoryScrollRect = sr;

        battleHistoryOverlayRoot = root;
        root.SetActive(false);
    }

    private void CreateEndBattleButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(220f, 66f);

        Image img = buttonObj.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.94f);
        Button btn = buttonObj.GetComponent<Button>();
        btn.onClick.AddListener(action);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text t = textObj.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = 28;
        t.color = Color.black;
    }

    private void OnClickRestartBattle()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void OnClickReturnBuildbeck()
    {
        string[] candidateNames = new string[] { "Buildbeck", "buildbeck", "BuildBeck" };
        for (int i = 0; i < candidateNames.Length; i++)
        {
            string sceneName = candidateNames[i];
            if (!Application.CanStreamedLevelBeLoaded(sceneName)) continue;
            Debug.Log("BattleSimulationDebugUI: returning to scene -> " + sceneName);
            SceneManager.LoadScene(sceneName);
            return;
        }

        Debug.LogError("BattleSimulationDebugUI: return scene not found in Build Settings. Expected Buildbeck/buildbeck.");
    }

    /// <summary>選用：settlementCaptureFlipTextureY 為 true 時將貼圖像素上下對調（少數管線與預設 RawImage 組合才需要）。</summary>
    private static void FlipTextureVerticallyForScreenCapture(Texture2D tex)
    {
        if (tex == null) return;
        int w = tex.width;
        int h = tex.height;
        if (w <= 0 || h <= 0) return;
        Color[] p = tex.GetPixels();
        int half = h / 2;
        for (int y = 0; y < half; y++)
        {
            int y2 = h - 1 - y;
            int row = y * w;
            int row2 = y2 * w;
            for (int x = 0; x < w; x++)
            {
                int i = row + x;
                int j = row2 + x;
                Color t = p[i];
                p[i] = p[j];
                p[j] = t;
            }
        }
        tex.SetPixels(p);
    }

}

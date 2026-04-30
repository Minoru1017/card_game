using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private RectTransform battleHistoryContentRt;
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
        dimImg.color = new Color(0.12f, 0.08f, 0.06f, 0.66f);
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
        int result = battleManager != null ? battleManager.GetBattleResult() : 0;
        if (endBattleTitleText != null && battleManager != null)
        {
            if (result == 1) endBattleTitleText.text = "Victory";
            else if (result == -1) endBattleTitleText.text = "Defeat";
            else endBattleTitleText.text = "Draw";
        }

        if (endBattlePanel != null)
            endBattlePanel.SetActive(false);

        // Defeat flow: play a short hero-death feedback first, then freeze/capture.
        if (result == -1 && !BattleAutoSimPlugin.IsRunning)
        {
            yield return StartCoroutine(CoPlayPlayerHeroDefeatBeforeSettlement());
            // Lock capture timing to the finishing frame of defeat presentation.
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
        }

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

    private IEnumerator CoPlayPlayerHeroDefeatBeforeSettlement()
    {
        float heavyHitDur = battleManager != null ? Mathf.Max(0.32f, battleManager.hitShakeDuration * 1.7f) : 0.62f;
        const float pauseDur = 0.42f;
        const float finishDur = 0.42f;
        RectTransform heroRt = playerHeroHpText != null ? playerHeroHpText.rectTransform : null;
        // Make sure HUD has settled to latest HP (including 0) before defeat presentation starts.
        RefreshHeroHpHud();
        yield return null;

        // Stage 1: heavy hit.
        if (heroRt != null) StartCoroutine(PlayHitShake(heroRt, heavyHitDur, 48f));
        if (handArea != null) StartCoroutine(PlayHitShake(handArea, heavyHitDur * 0.95f, 56f));
        if (playerHeroHpText != null) StartCoroutine(PlayDamageFlash(playerHeroHpText.gameObject, heavyHitDur));
        if (enableHeroDamageMonochromeFlash) StartCoroutine(CoPlayHeroDamageMonochromeFlash());

        float t = 0f;
        float pulseDur = Mathf.Max(0.4f, heavyHitDur * 0.9f);
        Color baseColor = playerHeroHpText != null ? playerHeroHpText.color : Color.white;
        Color hurtColor = new Color(1f, 0.26f, 0.26f, 1f);
        while (t < pulseDur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / pulseDur);
            float pulse = Mathf.Sin(p * Mathf.PI);
            if (playerHeroHpText != null) playerHeroHpText.color = Color.Lerp(baseColor, hurtColor, pulse * 0.92f);
            yield return null;
        }

        // Stage 2: brief pause to hold impact.
        if (playerHeroHpText != null) playerHeroHpText.color = hurtColor;
        yield return new WaitForSecondsRealtime(pauseDur);

        // Stage 3: finish and recover to baseline before freeze capture.
        t = 0f;
        while (t < finishDur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / finishDur);
            if (playerHeroHpText != null) playerHeroHpText.color = Color.Lerp(hurtColor, baseColor, p);
            yield return null;
        }
        if (playerHeroHpText != null) playerHeroHpText.color = baseColor;
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
        panelRect.sizeDelta = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        panelRect.localScale = Vector3.one;
        Image bg = endBattlePanel.GetComponent<Image>();
        bg.color = new Color(0.93f, 0.89f, 0.82f, 0.96f);

        GameObject titleObj = new GameObject("EndBattleTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(endBattlePanel.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 128f);
        titleRect.sizeDelta = new Vector2(680f, 120f);
        endBattleTitleText = titleObj.GetComponent<Text>();
        endBattleTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        endBattleTitleText.fontSize = 76;
        endBattleTitleText.alignment = TextAnchor.MiddleCenter;
        endBattleTitleText.color = new Color(0.29f, 0.23f, 0.17f, 1f);
        endBattleTitleText.text = "Victory";

        CreateEndBattleButton(endBattlePanel.transform, "BattleHistoryButton", "對戰歷史", new Vector2(-300f, -138f), OnClickBattleHistory);
        CreateEndBattleButton(endBattlePanel.transform, "RestartBattleButton", "Rematch", new Vector2(0f, -138f), OnClickRestartBattle);
        CreateEndBattleButton(endBattlePanel.transform, "ReturnBuildbeckButton", "Deck builder", new Vector2(300f, -138f), OnClickReturnBuildbeck);
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
        if (battleHistoryOverlayRoot == null || battleHistoryContentRt == null) return;

        BuildBattleHistoryRows(battleManager.GetBattleHistoryFullText());
        battleHistoryOverlayRoot.SetActive(true);
        battleHistoryOverlayRoot.transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(battleHistoryContentRt);
        if (battleHistoryScrollRect != null)
            battleHistoryScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildBattleHistoryRows(string plain)
    {
        if (battleHistoryContentRt == null) return;

        for (int i = battleHistoryContentRt.childCount - 1; i >= 0; i--)
            Destroy(battleHistoryContentRt.GetChild(i).gameObject);

        string[] lines = string.IsNullOrEmpty(plain)
            ? new[] { "（本局尚無對戰歷史紀錄）" }
            : plain.Split(new[] { '\n' }, StringSplitOptions.None);
        float s = BattleHistoryDialogScale;
        float wrapWidth = 640f * s;
        if (battleHistoryScrollRect != null && battleHistoryScrollRect.viewport != null)
        {
            float pad = 62f * s;
            wrapWidth = Mathf.Max(120f * s, battleHistoryScrollRect.viewport.rect.width - pad);
        }

        bool weatherBlockActive = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string rawLine = lines[i] ?? string.Empty;
            string trimmed = rawLine.Trim();
            if (trimmed.StartsWith("天氣預報:", StringComparison.Ordinal))
                weatherBlockActive = true;
            bool isWeatherLine = weatherBlockActive;

            string richLine = FormatBattleHistoryRichText(rawLine);
            GameObject rowObj = new GameObject("HistoryLine_" + i, typeof(RectTransform), typeof(LayoutElement));
            rowObj.transform.SetParent(battleHistoryContentRt, false);
            GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(rowObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image rowBg = bgObj.GetComponent<Image>();
            // Project palette match: warm brown tone (same family as battle buttons/panels).
            rowBg.color = isWeatherLine ? new Color(0.443f, 0.282f, 0.247f, 0.28f) : new Color(0f, 0f, 0f, 0f);
            rowBg.raycastTarget = false;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(rowObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(12f * s, 5f * s);
            txtRt.offsetMax = new Vector2(-12f * s, -5f * s);
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            if (sharedUIFont != null) tmp.font = sharedUIFont;
            tmp.fontSize = 22f * s;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.text = richLine;
            tmp.raycastTarget = false;
            bgObj.transform.SetAsFirstSibling();
            txtObj.transform.SetAsLastSibling();

            float preferredH = tmp.GetPreferredValues(richLine, wrapWidth, 0f).y;
            LayoutElement le = rowObj.GetComponent<LayoutElement>();
            le.preferredHeight = Mathf.Max(34f * s, preferredH + 14f * s);
            le.minHeight = le.preferredHeight;

            if (trimmed.StartsWith("天氣結算:", StringComparison.Ordinal))
                weatherBlockActive = false;
        }
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

        // Project theme colors: warm + readable (avoid overly light tones).
        const string monsterOpen = "<color=#A85F22>";
        const string spellOpen = "<color=#7A4EA3>";
        const string damageOpen = "<color=#B33A2C>";
        const string healOpen = "<color=#2D7A43>";
        const string outcomeOpen = "<color=#8A651E>";
        const string colorClose = "</color>";

        string line = plain;
        if (string.IsNullOrEmpty(line)) return line;

        string trimmed = line.Trim();
        bool battleStartLine =
            trimmed == "對戰開始" ||
            Regex.IsMatch(trimmed, @"^我方骰\d+敵方骰\d+$") ||
            trimmed == "我方先手" ||
            trimmed == "敵方先手";
        bool battleEndTitleLine = trimmed == "對戰結束";
        bool battleOutcomeLine = trimmed == "我方戰敗" || trimmed == "我方勝利";

        string s = line
            .Replace("<", "＜")
            .Replace(">", "＞")
            // Normalize symbols/punctuation to reduce missing glyphs across CJK fonts.
            .Replace('：', ':')
            .Replace('，', ',')
            .Replace('。', '.')
            .Replace('（', '(')
            .Replace('）', ')')
            .Replace('？', '?')
            .Replace('！', '!')
            .Replace('「', '"')
            .Replace('」', '"')
            .Replace('、', ',')
            .Replace('—', '-')
            .Replace('…', '.');

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

        return s;
    }

    private void EnsureBattleHistoryOverlay()
    {
        if (battleHistoryOverlayRoot != null || uiRoot == null) return;

        float s = BattleHistoryDialogScale;

        GameObject root = new GameObject("BattleHistoryOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(uiRoot, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        Image rootDim = root.GetComponent<Image>();
        rootDim.color = new Color(0.12f, 0.08f, 0.06f, 0.68f);
        rootDim.raycastTarget = true;

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
        dlgBg.color = new Color(0.94f, 0.9f, 0.84f, 1f);
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
        titleTmp.color = new Color(0.25f, 0.2f, 0.15f, 1f);
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
        closeBtnObj.GetComponent<Image>().color = new Color(0.4431373f, 0.28235295f, 0.24705884f, 0.96f);
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
        closeTxt.color = Color.white;

        GameObject scrollGo = new GameObject("BattleHistoryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(dlg.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(14f * s, 16f * s);
        scrollRt.offsetMax = new Vector2(-14f * s, -58f * s);
        scrollGo.GetComponent<Image>().color = new Color(0.88f, 0.83f, 0.76f, 1f);
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

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(-16f * s, 400f * s);
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0f;
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        battleHistoryContentRt = cRt;
        battleHistoryContentTmp = null;

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
        rect.sizeDelta = new Vector2(280f, 84f);

        Image img = buttonObj.GetComponent<Image>();
        img.color = new Color(0.4431373f, 0.28235295f, 0.24705884f, 0.96f);
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
        t.fontSize = 34;
        t.color = Color.white;
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

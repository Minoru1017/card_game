using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    private sealed class SettlementProficiencyRowUi
    {
        public RectTransform fillRt;
        public Image fillImg;
        public TextMeshProUGUI statusText;
        public float fillFrom;
        public float fillTo;
    }

    private const float SettlementProficiencyFillEpsilon = 0.001f;
    private HarborTrainingRewardService.VictoryGrantResult lastHarborVictoryReward;

    /// <summary>
    /// 結算面板整棵 UI 樹版本。已凍結為 6：僅在必須整批重建結算面板時才 +1；
    /// 日常調字級／間距請改 <see cref="EndBattleProficiencyColumnCount"/> 等常數，避免玩家結算閃爍。
    /// </summary>
    private const int EndBattlePanelLayoutVersion = 8;
    private const int EndBattleProficiencyColumnCount = 5;
    private const float EndBattleProficiencyGridPaddingH = 24f;
    private const float EndBattleProficiencyGridSpacingX = 10f;
    private const float EndBattleProficiencyGridSpacingY = 12f;
    private const float EndBattleHeaderHeightPx = 252f;
    private const int EndBattleHeaderStatsMaxLines = 1;
    private const float EndBattleSubtitleFontSize = 30f;
    private const float EndBattleHeaderStatsFontSize = 26f;
    private const float EndBattleProficiencyBannerHeightPx = 76f;
    private const float EndBattleFooterHeightPx = 108f;
    private const float EndBattlePanelWidthMarginPx = 28f;
    private const float EndBattlePanelMinWidthPx = 720f;
    private const float EndBattlePanelHeightRatio = 0.82f;
    private const float EndBattlePanelMinHeightPx = 640f;
    private const float EndBattleProficiencyBarAnimDuration = 0.85f;
    private const float EndBattleProficiencyBarStagger = 0.1f;
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

    private void OnBattleEndedForSettlement(int _)
    {
        RefreshBattleLiveHistoryFeed();
        UpdateBattleResultText();
    }

    private void OnBattleRuleMessageChangedForSettlement(string _) => UpdateBattleResultText();

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
        if (BattleLaunchContext.IsIntroTutorialBattle)
            return;
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
        if (endBattleProficiencyAnimRoutine != null)
        {
            StopCoroutine(endBattleProficiencyAnimRoutine);
            endBattleProficiencyAnimRoutine = null;
        }
        ClearEndBattleProficiencyRows();
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
        dimImg.color = BattleUiColors.DimHeavy;
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
        lastHarborVictoryReward = default;
        if (result == 1 && BattleLaunchContext.IsHarborTrainingGroundBattle)
        {
            BattleDifficultyTier tier = HarborTrainingBattleCopy.TierFromLabelZh(
                BattleLaunchContext.ResolveForBattleRecord());
            lastHarborVictoryReward = HarborTrainingRewardService.ProcessVictory(tier);
        }

        if (battleManager != null)
            ApplyEndBattleResultHeader(result);

        if (endBattlePanel != null)
            endBattlePanel.SetActive(false);

        // Defeat flow: play a short hero-death feedback first, then freeze/capture.
        if (result == -1 && !BattleAutoSimPlugin.IsRunning
            && (battleManager == null || !battleManager.LastBattleEndedBySurrender))
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

            PopulateEndBattleProficiencyRows();
            UpdateEndBattleProficiencyUpdateBanner();
            if (endBattleProficiencyAnimRoutine != null)
            {
                StopCoroutine(endBattleProficiencyAnimRoutine);
                endBattleProficiencyAnimRoutine = null;
            }
            if (endBattleProficiencyRows.Count > 0)
                endBattleProficiencyAnimRoutine = StartCoroutine(CoAnimateEndBattleProficiencyBars());
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
        Color hurtColor = BattleFxColors.HurtFlash;
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

    private static void ResolveEndBattlePanelSize(out float panelW, out float panelH)
    {
        panelW = Mathf.Max(EndBattlePanelMinWidthPx, Screen.width - EndBattlePanelWidthMarginPx);
        panelH = Mathf.Min(
            Mathf.Max(EndBattlePanelMinHeightPx, Screen.height * EndBattlePanelHeightRatio),
            Screen.height - EndBattlePanelWidthMarginPx);
    }

    private void DestroyEndBattlePanelUi()
    {
        if (endBattlePanel == null) return;
        Destroy(endBattlePanel);
        endBattlePanel = null;
        endBattleTitleText = null;
        endBattleSubtitleText = null;
        endBattleHeaderStatsText = null;
        endBattleHeaderStripImage = null;
        endBattleProficiencyUpdateBanner = null;
        endBattleFooterBar = null;
        endBattlePanelGroup = null;
        endBattleProficiencySection = null;
        endBattleProficiencyContentRt = null;
        endBattleProficiencyScroll = null;
        ClearEndBattleProficiencyRows();
    }

    private void EnsureEndBattlePanel()
    {
        if (endBattlePanel != null &&
            (endBattleProficiencySection == null || endBattlePanelLayoutBuilt != EndBattlePanelLayoutVersion))
            DestroyEndBattlePanelUi();
        if (endBattlePanel != null || uiRoot == null) return;

        endBattlePanel = new GameObject("EndBattlePanel", typeof(RectTransform), typeof(Image), typeof(Outline));
        endBattlePanel.transform.SetParent(uiRoot, false);
        RectTransform panelRect = endBattlePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        ResolveEndBattlePanelSize(out float panelW, out float panelH);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelRect.localScale = Vector3.one;
        Image bg = endBattlePanel.GetComponent<Image>();
        bg.sprite = GetUnitWhiteSprite();
        bg.type = Image.Type.Simple;
        bg.color = BattleUiColors.PanelCream96;
        Outline panelOutline = endBattlePanel.GetComponent<Outline>();
        panelOutline.effectColor = BattleUiColors.PanelEdge35;
        panelOutline.effectDistance = new Vector2(3f, -3f);

        CreateEndBattleHeaderStrip(endBattlePanel.transform, panelW);
        CreateEndBattleProficiencyUpdateBanner(endBattlePanel.transform, panelW);
        BuildEndBattleProficiencySection(endBattlePanel.transform);
        endBattleFooterBar = CreateEndBattleFooterBar(endBattlePanel.transform, panelW);
        CreateEndBattleFooterButton(endBattleFooterBar.transform, "BattleHistoryButton", "對戰歷史", OnClickBattleHistory, false);
        CreateEndBattleFooterButton(endBattleFooterBar.transform, "RestartBattleButton", "再戰一局", OnClickRestartBattle, true);
        string returnLabel = BattleLaunchContext.IsHarborTrainingGroundBattle ? "返回地圖" : "牌組編輯";
        UnityEngine.Events.UnityAction returnAction = BattleLaunchContext.IsHarborTrainingGroundBattle
            ? OnClickReturnStoryProgress
            : OnClickReturnBuildbeck;
        CreateEndBattleFooterButton(
            endBattleFooterBar.transform,
            "ReturnBuildbeckButton",
            returnLabel,
            returnAction,
            false);
        ConfigureEndBattleProficiencySectionLayout(showBanner: false);
        endBattlePanelGroup = endBattlePanel.AddComponent<CanvasGroup>();
        endBattlePanelGroup.blocksRaycasts = true;
        endBattlePanelGroup.interactable = true;
        endBattlePanelGroup.alpha = 1f;
        endBattlePanel.SetActive(false);
        endBattlePanelLayoutBuilt = EndBattlePanelLayoutVersion;
    }

    private void OnClickBattleHistory()
    {
        if (battleManager == null) return;
        EnsureBattleHistoryOverlay();
        if (battleHistoryOverlayRoot == null || battleHistoryContentRt == null) return;

        BuildBattleHistoryRowsFromEntries(battleManager.GetBattleHistoryEntriesNewestFirst());
        battleHistoryOverlayRoot.SetActive(true);
        battleHistoryOverlayRoot.transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(battleHistoryContentRt);
        if (battleHistoryScrollRect != null)
            battleHistoryScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildBattleHistoryRowsFromEntries(List<BattleHistoryEntry> entriesNewestFirst)
    {
        if (battleHistoryContentRt == null) return;

        for (int i = battleHistoryContentRt.childCount - 1; i >= 0; i--)
            Destroy(battleHistoryContentRt.GetChild(i).gameObject);

        float s = BattleHistoryDialogScale;
        float wrapWidth = 640f * s;
        if (battleHistoryScrollRect != null && battleHistoryScrollRect.viewport != null)
        {
            float pad = 62f * s;
            wrapWidth = Mathf.Max(120f * s, battleHistoryScrollRect.viewport.rect.width - pad);
        }

        if (entriesNewestFirst == null || entriesNewestFirst.Count == 0)
        {
            CreateBattleHistoryEventRow("（本局尚無對戰歷史紀錄）", false, false, wrapWidth, s, 0);
            return;
        }

        int lastRound = int.MinValue;
        int rowIndex = 0;
        for (int i = 0; i < entriesNewestFirst.Count; i++)
        {
            BattleHistoryEntry entry = entriesNewestFirst[i];
            if (entry.Round != lastRound)
            {
                lastRound = entry.Round;
                string header = entry.Round <= 0
                    ? "── 開局 ──"
                    : "── 第 " + entry.Round + " 回合 ──";
                CreateBattleHistoryRoundHeaderRow(header, wrapWidth, s, rowIndex++);
            }

            bool isWeather = entry.Kind == BattleHistoryKind.Weather;
            CreateBattleHistoryEventRow(entry.Text, isWeather, true, wrapWidth, s, rowIndex++);
        }
    }

    private void CreateBattleHistoryRoundHeaderRow(string headerText, float wrapWidth, float s, int rowIndex)
    {
        GameObject rowObj = new GameObject("HistoryRound_" + rowIndex, typeof(RectTransform), typeof(LayoutElement));
        rowObj.transform.SetParent(battleHistoryContentRt, false);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(rowObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(8f * s, 4f * s);
        txtRt.offsetMax = new Vector2(-8f * s, -4f * s);
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        if (sharedUIFont != null) tmp.font = sharedUIFont;
        tmp.fontSize = 20f * s;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = BattleUiColors.Ink;
        tmp.enableWordWrapping = false;
        tmp.richText = false;
        tmp.text = headerText;
        tmp.raycastTarget = false;

        LayoutElement le = rowObj.GetComponent<LayoutElement>();
        le.preferredHeight = 36f * s;
        le.minHeight = le.preferredHeight;
    }

    private void CreateBattleHistoryEventRow(
        string rawLine,
        bool isWeatherLine,
        bool useRichText,
        float wrapWidth,
        float s,
        int rowIndex)
    {
        string richLine = useRichText ? FormatBattleHistoryRichText(rawLine) : rawLine;
        GameObject rowObj = new GameObject("HistoryLine_" + rowIndex, typeof(RectTransform), typeof(LayoutElement));
        rowObj.transform.SetParent(battleHistoryContentRt, false);
        GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(rowObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image rowBg = bgObj.GetComponent<Image>();
        rowBg.color = isWeatherLine ? BattleUiColors.HallWine28 : new Color(0f, 0f, 0f, 0f);
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
        tmp.color = BattleUiColors.InkSoft;
        tmp.enableWordWrapping = true;
        tmp.richText = useRichText;
        tmp.text = richLine;
        tmp.raycastTarget = false;
        bgObj.transform.SetAsFirstSibling();
        txtObj.transform.SetAsLastSibling();

        float preferredH = tmp.GetPreferredValues(richLine, wrapWidth, 0f).y;
        LayoutElement le = rowObj.GetComponent<LayoutElement>();
        le.preferredHeight = Mathf.Max(34f * s, preferredH + 14f * s);
        le.minHeight = le.preferredHeight;
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
        rootDim.color = BattleUiColors.DimHeavy;
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
        dlgBg.color = BattleUiColors.PanelMilk;
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
        titleTmp.color = BattleUiColors.Ink;
        titleTmp.text = "本局對戰歷史（最新在上 · 依回合）";
        titleTmp.raycastTarget = false;

        GameObject closeBtnObj = new GameObject("CloseBattleHistoryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(dlg.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-12f * s, -8f * s);
        closeRt.sizeDelta = new Vector2(118f * s, 44f * s);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(HideBattleHistoryOverlay);
        BattleUiColors.ApplyHallWineButton(closeBtn);
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
        closeTxt.color = BattleUiColors.BtnPrimaryText;

        GameObject scrollGo = new GameObject("BattleHistoryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(dlg.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(14f * s, 16f * s);
        scrollRt.offsetMax = new Vector2(-14f * s, -58f * s);
        scrollGo.GetComponent<Image>().color = BattleUiColors.PanelScroll;
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

    private void BuildEndBattleProficiencySection(Transform panel)
    {
        endBattleProficiencySection = new GameObject("ProficiencySection", typeof(RectTransform));
        endBattleProficiencySection.transform.SetParent(panel, false);
        ConfigureEndBattleProficiencySectionLayout(showBanner: false);

        GameObject scrollGo = new GameObject("ProficiencyScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(endBattleProficiencySection.transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        Image scrollBg = scrollGo.GetComponent<Image>();
        scrollBg.sprite = GetUnitWhiteSprite();
        scrollBg.type = Image.Type.Simple;
        scrollBg.color = BattleUiColors.PanelScroll;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(10f, 10f);
        vpRt.offsetMax = new Vector2(-10f, -10f);
        viewport.GetComponent<Image>().color = BattleUiColors.PanelMilk985;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        endBattleProficiencyContentRt = content.GetComponent<RectTransform>();
        endBattleProficiencyContentRt.anchorMin = new Vector2(0f, 1f);
        endBattleProficiencyContentRt.anchorMax = new Vector2(1f, 1f);
        endBattleProficiencyContentRt.pivot = new Vector2(0.5f, 1f);
        endBattleProficiencyContentRt.anchoredPosition = Vector2.zero;
        endBattleProficiencyContentRt.sizeDelta = new Vector2(0f, 400f);
        GridLayoutGroup glg = content.GetComponent<GridLayoutGroup>();
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = EndBattleProficiencyColumnCount;
        glg.spacing = new Vector2(EndBattleProficiencyGridSpacingX, EndBattleProficiencyGridSpacingY);
        glg.padding = new RectOffset(12, 12, 12, 12);
        glg.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        endBattleProficiencyScroll = scrollGo.GetComponent<ScrollRect>();
        endBattleProficiencyScroll.horizontal = false;
        endBattleProficiencyScroll.vertical = true;
        endBattleProficiencyScroll.movementType = ScrollRect.MovementType.Clamped;
        endBattleProficiencyScroll.content = endBattleProficiencyContentRt;
        endBattleProficiencyScroll.viewport = vpRt;
    }

    private void ResolveEndBattleProficiencyRowMetrics(
        out float rowWidth,
        out float rowHeight,
        out float cardScaleMul,
        out float nameBottomY,
        out float barBottomY,
        out int nameFontSize,
        out int statusFontSize,
        out float barHeight)
    {
        float viewportW = EndBattlePanelMinWidthPx - 80f;
        float viewportH = 260f;
        if (endBattleProficiencyScroll != null && endBattleProficiencyScroll.viewport != null)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform vpRt = endBattleProficiencyScroll.viewport;
            LayoutRebuilder.ForceRebuildLayoutImmediate(vpRt);
            float w = vpRt.rect.width;
            float h = vpRt.rect.height;
            if (w > 48f) viewportW = w;
            if (h > 48f) viewportH = h;
        }
        else if (endBattlePanel != null)
        {
            ResolveEndBattlePanelSize(out float panelW, out float panelH);
            viewportW = Mathf.Max(480f, panelW - 80f);
            viewportH = Mathf.Max(200f, panelH - 230f);
        }

        float spacingTotal = (EndBattleProficiencyColumnCount - 1) * EndBattleProficiencyGridSpacingX;
        rowWidth = Mathf.Floor(
            Mathf.Max(96f, (viewportW - EndBattleProficiencyGridPaddingH - spacingTotal) / EndBattleProficiencyColumnCount));

        const float footerBlock = 88f;
        float handH = GetBattleHandDisplayedHeight(1f);
        float handW = GetBattleHandDisplayedWidth(1f);
        if (handH < 1f) handH = 210f;
        if (handW < 1f) handW = 140f;

        float innerCardW = Mathf.Max(64f, rowWidth - 6f);
        float scaleFromWidth = innerCardW / handW;
        float targetCardH = Mathf.Max(88f, (viewportH - footerBlock - 12f) * 0.88f);
        float scaleFromHeight = targetCardH / handH;
        cardScaleMul = Mathf.Clamp(Mathf.Min(scaleFromWidth, scaleFromHeight), 0.38f, 0.92f);
        float scaleT = Mathf.InverseLerp(0.38f, 0.92f, cardScaleMul);

        Vector2 cardSize = GetBattleHandDisplayedSize(cardScaleMul);
        rowHeight = Mathf.Ceil(cardSize.y + footerBlock);

        barHeight = Mathf.Round(Mathf.Lerp(34f, 46f, scaleT));
        nameBottomY = cardSize.y + 8f;
        barBottomY = Mathf.Round(Mathf.Lerp(38f, 50f, scaleT));
        nameFontSize = Mathf.RoundToInt(Mathf.Lerp(24f, 32f, scaleT));
        statusFontSize = Mathf.RoundToInt(Mathf.Lerp(20f, 28f, scaleT));
    }

    private void ApplySettlementProficiencyCardVisualTuning(CardDisplay display, float cardScaleMul)
    {
        ApplyPrefabVisualTuning(display);
        float scaleT = Mathf.InverseLerp(0.38f, 0.92f, cardScaleMul);
        float textBoost = Mathf.Lerp(1.12f, 1.35f, scaleT);
        TextMeshProUGUI[] texts = display.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI t = texts[i];
            if (t == null) continue;
            t.rectTransform.localScale *= textBoost;
        }

        if (display.backgroundImage != null)
            display.backgroundImage.rectTransform.localScale *= Mathf.Lerp(1.06f, 1.14f, scaleT);
    }

    private void PopulateEndBattleProficiencyRows()
    {
        ClearEndBattleProficiencyRows();
        if (endBattleProficiencyContentRt == null) return;

        IReadOnlyList<BattleProficiencySettlementEntry> entries = CardSkillProficiencyService.LastSettlementEntries;
        bool hasRows = entries != null && entries.Count > 0;
        if (endBattleProficiencySection != null)
            endBattleProficiencySection.SetActive(true);
        if (!hasRows)
        {
            CreateEndBattleProficiencyEmptyPlaceholder();
            return;
        }

        ResolveEndBattleProficiencyRowMetrics(
            out float rowWidth,
            out float rowHeight,
            out float cardScaleMul,
            out float nameBottomY,
            out float barBottomY,
            out int nameFontSize,
            out int statusFontSize,
            out float barHeight);

        GridLayoutGroup grid = endBattleProficiencyContentRt != null
            ? endBattleProficiencyContentRt.GetComponent<GridLayoutGroup>()
            : null;
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = EndBattleProficiencyColumnCount;
            grid.cellSize = new Vector2(rowWidth, rowHeight);
        }

        PlayerData pd = battleManager != null ? battleManager.playerData : null;
        if (pd == null) pd = PlayerData.ResolveCanonical();
        GameObject cardPrefab = ResolveCardPrefab();
        Vector2 cardDisplaySize = GetBattleHandDisplayedSize(cardScaleMul);

        for (int i = 0; i < entries.Count; i++)
        {
            BattleProficiencySettlementEntry entry = entries[i];
            GameObject row = new GameObject("ProfRow_" + entry.monsterId, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(endBattleProficiencyContentRt, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(rowWidth, rowHeight);
            Image rowBg = row.GetComponent<Image>();
            rowBg.sprite = GetUnitWhiteSprite();
            rowBg.type = Image.Type.Simple;
            rowBg.color = BattleUiColors.PanelMilk985;

            GameObject cardSlot = new GameObject("CardSlot", typeof(RectTransform));
            cardSlot.transform.SetParent(row.transform, false);
            RectTransform cardSlotRt = cardSlot.GetComponent<RectTransform>();
            cardSlotRt.anchorMin = new Vector2(0.5f, 1f);
            cardSlotRt.anchorMax = new Vector2(0.5f, 1f);
            cardSlotRt.pivot = new Vector2(0.5f, 1f);
            cardSlotRt.anchoredPosition = Vector2.zero;
            cardSlotRt.sizeDelta = cardDisplaySize;

            if (cardPrefab != null && pd != null && pd.CardStore != null)
            {
                Card card = pd.CardStore.GetCardById(entry.monsterId);
                if (card != null)
                {
                    GameObject cardObj = Instantiate(cardPrefab, cardSlot.transform);
                    RectTransform cardRt = cardObj.GetComponent<RectTransform>();
                    if (cardRt != null)
                    {
                        cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                        cardRt.pivot = new Vector2(0.5f, 0.5f);
                        cardRt.anchoredPosition = Vector2.zero;
                        ApplyBattleHandCardRectLayout(cardRt, cardScaleMul);
                    }
                    CardDisplay display = cardObj.GetComponentInChildren<CardDisplay>();
                    if (display != null)
                    {
                        display.SetCard(card);
                        ApplySettlementProficiencyCardVisualTuning(display, cardScaleMul);
                    }
                    Button b = cardObj.GetComponent<Button>();
                    if (b != null) b.interactable = false;
                }
            }

            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(row.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, nameBottomY);
            nameRt.sizeDelta = new Vector2(0f, Mathf.Round(nameFontSize * 1.15f));
            Text nameTxt = nameObj.GetComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize = nameFontSize;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = BattleUiColors.Ink;
            nameTxt.text = entry.cardName;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameTxt.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject trackObj = new GameObject("BarTrack", typeof(RectTransform), typeof(Image));
            trackObj.transform.SetParent(row.transform, false);
            RectTransform trackRt = trackObj.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(1f, 0f);
            trackRt.pivot = new Vector2(0.5f, 0f);
            trackRt.anchoredPosition = new Vector2(0f, barBottomY);
            trackRt.sizeDelta = new Vector2(-4f, barHeight);
            Image trackImg = trackObj.GetComponent<Image>();
            trackImg.sprite = GetUnitWhiteSprite();
            trackImg.type = Image.Type.Simple;
            trackImg.color = new Color(0.18f, 0.16f, 0.14f, 0.55f);

            GameObject fillObj = new GameObject("BarFill", typeof(RectTransform), typeof(Image), typeof(Outline));
            fillObj.transform.SetParent(trackObj.transform, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = new Vector2(1f, 1f);
            fillRt.offsetMax = new Vector2(-1f, -1f);
            float startFill = Mathf.Clamp01(entry.fillBefore01);
            Image fillImg = fillObj.GetComponent<Image>();
            ApplySettlementProficiencyFill(fillRt, fillImg, startFill);
            fillImg.sprite = GetUnitWhiteSprite();
            fillImg.type = Image.Type.Simple;
            fillImg.color = BattleUiColors.AllyHp;
            Outline fillOutline = fillObj.GetComponent<Outline>();
            fillOutline.effectColor = BattleUiColors.WithAlpha(BattleUiColors.Ink, 0.45f);
            fillOutline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject statusObj = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(row.transform, false);
            RectTransform statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 0f);
            statusRt.anchorMax = new Vector2(1f, 0f);
            statusRt.pivot = new Vector2(0.5f, 0f);
            statusRt.anchoredPosition = Vector2.zero;
            statusRt.sizeDelta = new Vector2(0f, Mathf.Max(statusFontSize + 8f, barBottomY - 2f));
            TextMeshProUGUI statusTxt = statusObj.GetComponent<TextMeshProUGUI>();
            if (sharedUIFont != null) statusTxt.font = sharedUIFont;
            statusTxt.fontSize = statusFontSize;
            statusTxt.fontStyle = FontStyles.Bold;
            statusTxt.alignment = TextAlignmentOptions.Center;
            statusTxt.color = BattleUiColors.InkSoft;
            statusTxt.enableWordWrapping = true;
            statusTxt.overflowMode = TextOverflowModes.Ellipsis;
            CardProficiencyWins wins = pd != null ? pd.GetCardProficiencyWins(entry.monsterId) : default;
            string deltaSuffix = CardSkillProficiencyService.FormatProgressDelta(entry.progressDelta);
            if (entry.stageAfter != entry.stageBefore)
            {
                statusTxt.text = "已解鎖 " + CardSkillProficiencyService.GetStageShortLabel(entry.stageAfter);
                statusTxt.color = BattleUiColors.AllyHp;
            }
            else
            {
                string progressLabel = entry.stageAfter == CardSkillRevealStage.BasicB
                    ? CardSkillProficiencyService.FormatProgressTowardC(wins.winsNormalDifficulty)
                    : CardSkillProficiencyService.FormatProgressTowardB(wins.progressAny);
                statusTxt.text = CardSkillProficiencyService.GetStageShortLabel(entry.stageAfter) + "  " +
                                   progressLabel +
                                   (string.IsNullOrEmpty(deltaSuffix) ? "" : "  " + deltaSuffix);
                statusTxt.color = string.IsNullOrEmpty(deltaSuffix)
                    ? BattleUiColors.InkSoft
                    : BattleUiColors.AllyHp;
            }

            float endFill = Mathf.Clamp01(entry.fillAfter01);
            endBattleProficiencyRows.Add(new SettlementProficiencyRowUi
            {
                fillRt = fillRt,
                fillImg = fillImg,
                statusText = statusTxt,
                fillFrom = startFill,
                fillTo = endFill
            });
        }

        Canvas.ForceUpdateCanvases();
        if (endBattleProficiencyContentRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(endBattleProficiencyContentRt);
        if (endBattleProficiencyScroll != null)
            endBattleProficiencyScroll.verticalNormalizedPosition = 1f;
    }

    private void ClearEndBattleProficiencyRows()
    {
        endBattleProficiencyRows.Clear();
        if (endBattleProficiencyContentRt == null) return;
        for (int i = endBattleProficiencyContentRt.childCount - 1; i >= 0; i--)
            Destroy(endBattleProficiencyContentRt.GetChild(i).gameObject);
    }

    private void CreateEndBattleProficiencyEmptyPlaceholder()
    {
        if (endBattleProficiencyContentRt == null) return;
        GameObject placeholder = new GameObject("ProficiencyEmptyHint", typeof(RectTransform), typeof(Text));
        placeholder.transform.SetParent(endBattleProficiencyContentRt, false);
        RectTransform rt = placeholder.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 120f);
        Text txt = placeholder.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 28;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = BattleUiColors.InkSoft;
        txt.text = "本局牌組沒有可顯示的怪物牌熟練度";
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        if (endBattleProficiencyContentRt != null)
            endBattleProficiencyContentRt.sizeDelta = new Vector2(0f, 140f);
    }

    private static void ApplySettlementProficiencyFill(RectTransform fillRt, Image fillImg, float fill01)
    {
        float fill = Mathf.Clamp01(fill01);
        bool visible = fill > SettlementProficiencyFillEpsilon;
        if (fillImg != null)
            fillImg.enabled = visible;
        Outline outline = fillImg != null ? fillImg.GetComponent<Outline>() : null;
        if (outline != null)
            outline.enabled = visible;
        if (fillRt == null) return;
        fillRt.anchorMax = new Vector2(visible ? fill : 0f, 1f);
    }

    private IEnumerator CoAnimateEndBattleProficiencyBars()
    {
        if (endBattleProficiencyRows.Count == 0) yield break;

        for (int i = 0; i < endBattleProficiencyRows.Count; i++)
        {
            SettlementProficiencyRowUi row = endBattleProficiencyRows[i];
            ApplySettlementProficiencyFill(row.fillRt, row.fillImg, row.fillFrom);
        }

        float maxEnd = 0f;
        for (int i = 0; i < endBattleProficiencyRows.Count; i++)
        {
            SettlementProficiencyRowUi row = endBattleProficiencyRows[i];
            float startDelay = i * EndBattleProficiencyBarStagger;
            float endTime = startDelay + EndBattleProficiencyBarAnimDuration;
            if (endTime > maxEnd) maxEnd = endTime;
        }

        float elapsed = 0f;
        while (elapsed < maxEnd)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < endBattleProficiencyRows.Count; i++)
            {
                SettlementProficiencyRowUi row = endBattleProficiencyRows[i];
                float localT = elapsed - i * EndBattleProficiencyBarStagger;
                float p = localT <= 0f ? 0f : Mathf.Clamp01(localT / EndBattleProficiencyBarAnimDuration);
                float eased = p * p * (3f - 2f * p);
                float fill = Mathf.Lerp(row.fillFrom, row.fillTo, eased);
                ApplySettlementProficiencyFill(row.fillRt, row.fillImg, fill);
            }
            yield return null;
        }

        for (int i = 0; i < endBattleProficiencyRows.Count; i++)
        {
            SettlementProficiencyRowUi row = endBattleProficiencyRows[i];
            ApplySettlementProficiencyFill(row.fillRt, row.fillImg, row.fillTo);
        }

        endBattleProficiencyAnimRoutine = null;
    }

    private void ApplyEndBattleResultHeader(int result)
    {
        if (endBattleTitleText == null) return;

        if (endBattleHeaderStatsText != null && battleManager != null)
        {
            endBattleHeaderStatsText.text = battleManager.GetBattleHistorySummaryText(EndBattleHeaderStatsMaxLines);
            endBattleHeaderStatsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(endBattleHeaderStatsText.text));
        }

        if (result == 1)
        {
            endBattleTitleText.text = "勝利";
            if (endBattleSubtitleText != null)
            {
                endBattleSubtitleText.text = BattleLaunchContext.IsHarborTrainingGroundBattle &&
                                             lastHarborVictoryReward.HasNewReward
                    ? HarborTrainingRewardService.BuildVictorySubtitle(lastHarborVictoryReward)
                    : "熟練度與戰績已記錄";
                endBattleSubtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(endBattleSubtitleText.text));
            }

            if (endBattleHeaderStripImage != null)
                endBattleHeaderStripImage.color = BattleUiColors.WithAlpha(BattleUiColors.AllyHp, 0.38f);
            endBattleTitleText.color = BattleUiColors.Ink;
        }
        else if (result == -1)
        {
            endBattleTitleText.text = "戰敗";
            if (endBattleSubtitleText != null)
            {
                endBattleSubtitleText.text = battleManager != null && battleManager.LastBattleEndedBySurrender
                    ? "您已放棄本局對戰"
                    : "仍可累積部分熟練度進度";
                endBattleSubtitleText.gameObject.SetActive(true);
            }
            if (endBattleHeaderStripImage != null)
                endBattleHeaderStripImage.color = BattleUiColors.WithAlpha(BattleUiColors.FoeHp, 0.42f);
            endBattleTitleText.color = BattleUiColors.Ink;
        }
        else
        {
            endBattleTitleText.text = "平手";
            if (endBattleSubtitleText != null)
            {
                endBattleSubtitleText.text = "本局戰績已記錄";
                endBattleSubtitleText.gameObject.SetActive(true);
            }
            if (endBattleHeaderStripImage != null)
                endBattleHeaderStripImage.color = BattleUiColors.WithAlpha(BattleUiColors.PanelScroll, 0.95f);
            endBattleTitleText.color = BattleUiColors.InkSoft;
        }
    }

    private void UpdateEndBattleProficiencyUpdateBanner()
    {
        bool showBanner = false;
        IReadOnlyList<BattleProficiencySettlementEntry> entries = CardSkillProficiencyService.LastSettlementEntries;
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].HadProgressChange)
                {
                    showBanner = true;
                    break;
                }
            }
        }

        if (endBattleProficiencyUpdateBanner != null)
            endBattleProficiencyUpdateBanner.SetActive(showBanner);
        ConfigureEndBattleProficiencySectionLayout(showBanner);
    }

    private void ConfigureEndBattleProficiencySectionLayout(bool showBanner)
    {
        if (endBattleProficiencySection == null) return;
        RectTransform sectionRt = endBattleProficiencySection.GetComponent<RectTransform>();
        sectionRt.anchorMin = Vector2.zero;
        sectionRt.anchorMax = Vector2.one;
        sectionRt.pivot = new Vector2(0.5f, 0.5f);
        sectionRt.anchoredPosition = Vector2.zero;
        sectionRt.sizeDelta = Vector2.zero;
        float topInset = EndBattleHeaderHeightPx + (showBanner ? EndBattleProficiencyBannerHeightPx + 10f : 8f);
        sectionRt.offsetMin = new Vector2(24f, EndBattleFooterHeightPx + 14f);
        sectionRt.offsetMax = new Vector2(-24f, -topInset);
    }

    private void CreateEndBattleHeaderStrip(Transform panel, float panelW)
    {
        GameObject strip = new GameObject("HeaderStrip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(panel, false);
        RectTransform stripRt = strip.GetComponent<RectTransform>();
        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = Vector2.zero;
        stripRt.sizeDelta = new Vector2(0f, EndBattleHeaderHeightPx);
        endBattleHeaderStripImage = strip.GetComponent<Image>();
        endBattleHeaderStripImage.sprite = GetUnitWhiteSprite();
        endBattleHeaderStripImage.type = Image.Type.Simple;
        endBattleHeaderStripImage.color = BattleUiColors.WithAlpha(BattleUiColors.AllyHp, 0.38f);

        GameObject titleObj = new GameObject("EndBattleTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(strip.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.76f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(24f, 0f);
        titleRect.offsetMax = new Vector2(-24f, -4f);
        endBattleTitleText = titleObj.GetComponent<Text>();
        endBattleTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        endBattleTitleText.fontSize = 64;
        endBattleTitleText.fontStyle = FontStyle.Bold;
        endBattleTitleText.alignment = TextAnchor.MiddleCenter;
        endBattleTitleText.color = BattleUiColors.Ink;
        endBattleTitleText.text = "勝利";

        GameObject subObj = new GameObject("EndBattleSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(strip.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.44f);
        subRect.anchorMax = new Vector2(1f, 0.76f);
        subRect.offsetMin = new Vector2(28f, 0f);
        subRect.offsetMax = new Vector2(-28f, 0f);
        endBattleSubtitleText = subObj.GetComponent<TextMeshProUGUI>();
        ApplyEndBattleHeaderTmp(
            endBattleSubtitleText,
            EndBattleSubtitleFontSize,
            FontStyles.Normal,
            BattleUiColors.Ink,
            TextAlignmentOptions.Center);
        endBattleSubtitleText.lineSpacing = -4f;
        endBattleSubtitleText.text = "熟練度與戰績已記錄";

        GameObject statsObj = new GameObject("EndBattleHeaderStats", typeof(RectTransform), typeof(TextMeshProUGUI));
        statsObj.transform.SetParent(strip.transform, false);
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 0.08f);
        statsRect.anchorMax = new Vector2(1f, 0.42f);
        statsRect.offsetMin = new Vector2(28f, 0f);
        statsRect.offsetMax = new Vector2(-28f, 0f);
        endBattleHeaderStatsText = statsObj.GetComponent<TextMeshProUGUI>();
        ApplyEndBattleHeaderTmp(
            endBattleHeaderStatsText,
            EndBattleHeaderStatsFontSize,
            FontStyles.Bold,
            BattleUiColors.InkSoft,
            TextAlignmentOptions.Center);
        endBattleHeaderStatsText.text = string.Empty;
    }

    private void ApplyEndBattleHeaderTmp(
        TextMeshProUGUI tmp,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        if (tmp == null) return;

        TMP_FontAsset font = sharedUIFont;
        if (font == null)
            font = SettingsUiFonts.ResolveParameterDetailsFont();
        if (font != null)
            tmp.font = font;

        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
    }

    private void CreateEndBattleProficiencyUpdateBanner(Transform panel, float panelW)
    {
        float y = -(EndBattleHeaderHeightPx + 4f);
        endBattleProficiencyUpdateBanner = new GameObject("ProficiencyUpdatedBanner", typeof(RectTransform), typeof(Image));
        endBattleProficiencyUpdateBanner.transform.SetParent(panel, false);
        RectTransform bannerRt = endBattleProficiencyUpdateBanner.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0f, 1f);
        bannerRt.anchorMax = new Vector2(1f, 1f);
        bannerRt.pivot = new Vector2(0.5f, 1f);
        bannerRt.anchoredPosition = new Vector2(0f, y);
        bannerRt.sizeDelta = new Vector2(-48f, EndBattleProficiencyBannerHeightPx);
        Image bannerBg = endBattleProficiencyUpdateBanner.GetComponent<Image>();
        bannerBg.sprite = GetUnitWhiteSprite();
        bannerBg.type = Image.Type.Simple;
        bannerBg.color = BattleUiColors.PanelMilk985;

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(endBattleProficiencyUpdateBanner.transform, false);
        RectTransform accentRt = accent.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.sizeDelta = new Vector2(6f, 0f);
        accentRt.offsetMin = new Vector2(0f, 6f);
        accentRt.offsetMax = new Vector2(6f, -6f);
        accent.GetComponent<Image>().sprite = GetUnitWhiteSprite();
        accent.GetComponent<Image>().color = BattleUiColors.AllyHp;

        GameObject titleObj = new GameObject("BannerTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(endBattleProficiencyUpdateBanner.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(18f, 0f);
        titleRt.offsetMax = new Vector2(-12f, -4f);
        Text titleTxt = titleObj.GetComponent<Text>();
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 36;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleLeft;
        titleTxt.color = BattleUiColors.Ink;
        titleTxt.text = "卡牌熟練度已更新";

        GameObject hintObj = new GameObject("BannerHint", typeof(RectTransform), typeof(Text));
        hintObj.transform.SetParent(endBattleProficiencyUpdateBanner.transform, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0.5f);
        hintRt.offsetMin = new Vector2(18f, 4f);
        hintRt.offsetMax = new Vector2(-12f, 0f);
        Text hintTxt = hintObj.GetComponent<Text>();
        hintTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize = 30;
        hintTxt.fontStyle = FontStyle.Bold;
        hintTxt.alignment = TextAnchor.MiddleLeft;
        hintTxt.color = BattleUiColors.Ink;
        hintTxt.text = "牌組內怪物進度已同步至存檔";

        endBattleProficiencyUpdateBanner.SetActive(false);
    }

    private GameObject CreateEndBattleFooterBar(Transform panel, float panelW)
    {
        GameObject footer = new GameObject("FooterBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footer.transform.SetParent(panel, false);
        RectTransform footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = new Vector2(0f, 14f);
        footerRt.sizeDelta = new Vector2(-48f, EndBattleFooterHeightPx);
        HorizontalLayoutGroup hlg = footer.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14f;
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        return footer;
    }

    private void CreateEndBattleFooterButton(
        Transform parent,
        string name,
        string label,
        UnityEngine.Events.UnityAction action,
        bool primary)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObj.transform.SetParent(parent, false);
        LayoutElement le = buttonObj.GetComponent<LayoutElement>();
        le.minHeight = 76f;
        le.preferredHeight = 76f;

        Button btn = buttonObj.GetComponent<Button>();
        btn.onClick.AddListener(action);
        if (primary)
            BattleUiColors.ApplyButtonStyle(btn, "EndTurnButton");
        else
            BattleUiColors.ApplyHallWineButton(btn);

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
        t.fontSize = 30;
        t.color = primary ? BattleUiColors.BtnPrimaryText : BattleUiColors.BtnPrimaryText;
    }

    private void OnClickRestartBattle()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void OnClickReturnStoryProgress()
    {
        StoryProgressBattleReturn.CompleteReturnFromHarborTraining();
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

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>戰位克制／被克攻擊特效（<see cref="CombatRoleBattleRules"/>）。</summary>
public partial class BattleSimulationDebugUI
{
    private const float CombatRoleLabelTotalDuration = 1.02f;
    private const string CombatRoleLabelObjectName = "CombatRoleMatchupLabel";
    private const string CombatRoleFxLayerObjectName = "CombatRoleFxLayer";
    private const float CombatRoleLabelBelowCardPx = 36f;

    private RectTransform combatRoleFxLayer;

    private void TryStartCombatRoleMatchupFx(GameObject anchorObj, CombatRoleMatchup matchup)
    {
        if (matchup == CombatRoleMatchup.Neutral || anchorObj == null || BattleAutoSimPlugin.IsRunning)
            return;
        StartCoroutine(PlayCombatRoleMatchupFx(anchorObj, matchup));
    }

    private IEnumerator PlayCombatRoleMatchupFx(GameObject anchorObj, CombatRoleMatchup matchup)
    {
        if (anchorObj == null || matchup == CombatRoleMatchup.Neutral || uiRoot == null)
            yield break;

        if (!TryResolveCombatRoleLabelAnchorInUiRoot(anchorObj, out Vector2 bottomCenter))
            yield break;

        RectTransform fxLayer = EnsureCombatRoleFxLayer();
        if (fxLayer == null)
            yield break;

        bool advantage = matchup == CombatRoleMatchup.Advantage;
        Color core = advantage ? BattleFxColors.CombatRoleAdvantageCore : BattleFxColors.CombatRoleDisadvantageCore;
        Color glow = advantage ? BattleFxColors.CombatRoleAdvantageGlow : BattleFxColors.CombatRoleDisadvantageGlow;
        Color ring = advantage ? BattleFxColors.CombatRoleAdvantageRing : BattleFxColors.CombatRoleDisadvantageRing;
        Color labelBg = advantage ? BattleFxColors.CombatRoleAdvantageLabelBg : BattleFxColors.CombatRoleDisadvantageLabelBg;
        Color labelBorder = advantage ? BattleFxColors.CombatRoleAdvantageLabelBorder : BattleFxColors.CombatRoleDisadvantageLabelBorder;
        Color labelText = advantage ? BattleFxColors.CombatRoleAdvantageLabelText : BattleFxColors.CombatRoleDisadvantageLabelText;
        Color flashPeak = advantage ? BattleFxColors.CombatRoleAdvantageFlash : BattleFxColors.CombatRoleDisadvantageFlash;
        string label = advantage ? "克制" : "被克";

        Vector2 ringCenter = bottomCenter + new Vector2(0f, -8f);
        Vector2 labelCenter = bottomCenter + new Vector2(0f, -CombatRoleLabelBelowCardPx);
        StartCoroutine(PlayCombatRoleImpactRing(fxLayer, ringCenter, ring, core, advantage));

        Transform old = fxLayer.Find(CombatRoleLabelObjectName);
        if (old != null)
            Destroy(old.gameObject);

        GameObject labelObj = new GameObject(CombatRoleLabelObjectName, typeof(RectTransform), typeof(CanvasGroup));
        labelObj.transform.SetParent(fxLayer, false);
        labelObj.transform.SetAsLastSibling();
        fxLayer.SetAsLastSibling();

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = labelCenter;
        labelRt.sizeDelta = new Vector2(132f, 54f);

        CreateCombatRoleLabelLayer(labelObj.transform, "Shadow", Vector2.zero, Vector2.one,
            new Vector2(5f, -5f), new Vector2(9f, -1f), BattleFxColors.CounterLabelShadow);
        RectTransform glowRt = CreateCombatRoleLabelLayer(labelObj.transform, "Glow", Vector2.zero, Vector2.one,
            new Vector2(-12f, -10f), new Vector2(12f, 10f), glow);
        CreateCombatRoleLabelLayer(labelObj.transform, "Badge", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, labelBg);
        CreateCombatRoleLabelLayer(labelObj.transform, "Border", Vector2.zero, Vector2.one,
            new Vector2(-4f, -4f), new Vector2(4f, 4f), labelBorder);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(labelObj.transform, false);
        textObj.transform.SetAsLastSibling();
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveUIFont();
        if (font != null)
            tmp.font = font;
        tmp.text = label;
        tmp.fontSize = advantage ? 36f : 34f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = labelText;
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color32(0, 0, 0, 220);
        tmp.raycastTarget = false;

        CanvasGroup cg = labelObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        labelRt.localScale = Vector3.one * (advantage ? 0.58f : 0.62f);
        labelRt.localRotation = Quaternion.Euler(0f, 0f, advantage ? -4f : 3f);

        StartCoroutine(PlayColorFlashOnCard(anchorObj, advantage ? 0.16f : 0.14f, flashPeak));

        const float popDur = 0.13f;
        const float holdDur = 0.62f;
        const float fadeDur = CombatRoleLabelTotalDuration - popDur - holdDur;
        float t = 0f;

        while (t < popDur && labelObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDur);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            cg.alpha = eased;
            float peakScale = advantage ? 1.12f : 1.02f;
            labelRt.localScale = Vector3.one * Mathf.Lerp(advantage ? 0.58f : 0.62f, peakScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < holdDur && labelObj != null)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f;
            float pulse = 1f + Mathf.Sin(t * (advantage ? 10f : 8f)) * (advantage ? 0.04f : 0.02f);
            labelRt.localScale = Vector3.one * ((advantage ? 1.12f : 1.02f) * pulse);
            if (glowRt != null)
            {
                float glowPulse = 1f + Mathf.Sin(t * (advantage ? 10f : 8f)) * (advantage ? 0.10f : 0.05f);
                glowRt.localScale = Vector3.one * glowPulse;
            }
            yield return null;
        }

        t = 0f;
        while (t < fadeDur && labelObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDur);
            cg.alpha = 1f - p;
            labelRt.localScale = Vector3.one * Mathf.Lerp(advantage ? 1.12f : 1.02f, 0.86f, p);
            yield return null;
        }

        if (labelObj != null)
            Destroy(labelObj);
    }

    private RectTransform EnsureCombatRoleFxLayer()
    {
        if (uiRoot == null)
            return null;

        if (combatRoleFxLayer != null)
            return combatRoleFxLayer;

        Transform existing = uiRoot.Find(CombatRoleFxLayerObjectName);
        if (existing != null)
        {
            combatRoleFxLayer = existing as RectTransform;
            return combatRoleFxLayer;
        }

        GameObject layerObj = new GameObject(CombatRoleFxLayerObjectName, typeof(RectTransform));
        layerObj.transform.SetParent(uiRoot, false);
        combatRoleFxLayer = layerObj.GetComponent<RectTransform>();
        combatRoleFxLayer.anchorMin = Vector2.zero;
        combatRoleFxLayer.anchorMax = Vector2.one;
        combatRoleFxLayer.offsetMin = Vector2.zero;
        combatRoleFxLayer.offsetMax = Vector2.zero;
        combatRoleFxLayer.pivot = new Vector2(0.5f, 0.5f);
        return combatRoleFxLayer;
    }

    private RectTransform CreateCombatRoleLabelLayer(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        GameObject layerObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        layerObj.transform.SetParent(parent, false);
        RectTransform rt = layerObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Image img = layerObj.GetComponent<Image>();
        img.sprite = GetUnitWhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private IEnumerator PlayCombatRoleImpactRing(
        RectTransform fxLayer,
        Vector2 centerLocal,
        Color ringColor,
        Color coreColor,
        bool advantage)
    {
        if (fxLayer == null)
            yield break;

        GameObject ringObj = new GameObject("CombatRoleImpactRing", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        ringObj.transform.SetParent(fxLayer, false);
        ringObj.transform.SetAsLastSibling();
        fxLayer.SetAsLastSibling();

        RectTransform ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot = new Vector2(0.5f, 0.5f);
        ringRt.anchoredPosition = centerLocal;
        ringRt.sizeDelta = new Vector2(advantage ? 118f : 104f, advantage ? 118f : 104f);
        Image ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = GetUnitWhiteSprite();
        ringImg.color = ringColor;
        ringImg.raycastTarget = false;
        CanvasGroup cg = ringObj.GetComponent<CanvasGroup>();

        GameObject coreObj = new GameObject("Core", typeof(RectTransform), typeof(Image));
        coreObj.transform.SetParent(ringObj.transform, false);
        RectTransform coreRt = coreObj.GetComponent<RectTransform>();
        coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
        coreRt.pivot = new Vector2(0.5f, 0.5f);
        coreRt.sizeDelta = new Vector2(advantage ? 36f : 28f, advantage ? 36f : 28f);
        Image coreImg = coreObj.GetComponent<Image>();
        coreImg.sprite = GetUnitWhiteSprite();
        coreImg.color = BattleFxColors.WithAlpha(coreColor, advantage ? 0.92f : 0.72f);
        coreImg.raycastTarget = false;

        cg.alpha = advantage ? 0.92f : 0.78f;
        ringRt.localScale = Vector3.one * 0.42f;

        float expandDur = advantage ? 0.22f : 0.18f;
        float t = 0f;
        while (t < expandDur && ringObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / expandDur);
            float eased = advantage ? 1f - Mathf.Pow(1f - p, 2f) : p * p;
            ringRt.localScale = Vector3.one * Mathf.Lerp(0.42f, advantage ? 1.28f : 1.08f, eased);
            cg.alpha = Mathf.Lerp(advantage ? 0.92f : 0.78f, 0f, p);
            yield return null;
        }

        if (ringObj != null)
            Destroy(ringObj);
    }

    /// <summary>場上卡底邊中心（uiRoot 本地座標）。</summary>
    private bool TryResolveCombatRoleLabelAnchorInUiRoot(GameObject cardObj, out Vector2 localInUiRoot)
    {
        if (TryGetFieldCardBottomCenterInUiRoot(cardObj, out localInUiRoot))
            return true;
        if (TryGetFieldCardBottomCenterViaCenterOffset(cardObj, out localInUiRoot))
            return true;
        return TryGetFieldSideBottomCenterInUiRoot(cardObj, out localInUiRoot);
    }

    private bool TryGetFieldCardBottomCenterInUiRoot(GameObject cardObj, out Vector2 localInUiRoot)
    {
        localInUiRoot = Vector2.zero;
        if (cardObj == null || uiRoot == null)
            return false;

        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        if (cardRt == null)
            return false;

        Vector3[] corners = new Vector3[4];
        cardRt.GetWorldCorners(corners);
        Vector3 worldBottom = (corners[0] + corners[3]) * 0.5f;
        return TryProjectWorldPointToUiRoot(worldBottom, cardRt, out localInUiRoot);
    }

    private bool TryGetFieldCardBottomCenterViaCenterOffset(GameObject cardObj, out Vector2 localInUiRoot)
    {
        localInUiRoot = Vector2.zero;
        if (cardObj == null || uiRoot == null)
            return false;

        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        if (cardRt == null)
            return false;

        float halfHeight = Mathf.Max(8f, cardRt.rect.height * 0.5f);
        Vector3 worldBottom = cardRt.TransformPoint(new Vector3(0f, -halfHeight, 0f));
        return TryProjectWorldPointToUiRoot(worldBottom, cardRt, out localInUiRoot);
    }

    private bool TryGetFieldSideBottomCenterInUiRoot(GameObject cardObj, out Vector2 localInUiRoot)
    {
        localInUiRoot = Vector2.zero;
        if (cardObj == null || uiRoot == null)
            return false;

        RectTransform area = ResolveFieldAreaForVisual(cardObj);
        if (area == null)
            return false;

        float halfHeight = GetBattleCardFxDisplayedHeight() * 0.5f;
        Vector3 worldBottom = area.TransformPoint(new Vector3(0f, -halfHeight, 0f));
        return TryProjectWorldPointToUiRoot(worldBottom, area, out localInUiRoot);
    }

    private RectTransform ResolveFieldAreaForVisual(GameObject cardObj)
    {
        if (cardObj == null)
            return null;
        if (playerFieldArea != null && cardObj.transform.IsChildOf(playerFieldArea))
            return playerFieldArea;
        if (enemyFieldArea != null && cardObj.transform.IsChildOf(enemyFieldArea))
            return enemyFieldArea;
        if (cardObj == playerFieldCardObj)
            return playerFieldArea;
        if (cardObj == enemyFieldCardObj)
            return enemyFieldArea;
        return null;
    }

    private bool TryProjectWorldPointToUiRoot(Vector3 worldPoint, RectTransform sourceRt, out Vector2 localInUiRoot)
    {
        localInUiRoot = Vector2.zero;
        if (uiRoot == null || sourceRt == null)
            return false;

        Camera sourceCam = ResolveUiEventCamera(sourceRt);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCam, worldPoint);

        Camera uiCam = ResolveUiEventCamera(uiRoot);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, uiCam, out localInUiRoot))
            return true;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out localInUiRoot);
    }

    private static Camera ResolveUiEventCamera(RectTransform rt)
    {
        if (rt == null)
            return null;

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (canvas.worldCamera != null)
            return canvas.worldCamera;

        return Camera.main;
    }
}

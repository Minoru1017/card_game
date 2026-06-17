using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private readonly Queue<BishopConsecrationVisualRequest> pendingBishopConsecrationVisuals =
        new Queue<BishopConsecrationVisualRequest>();

    private Coroutine bishopConsecrationFxRoutine;

    private void OnBishopConsecrationVisualRequested(BishopConsecrationVisualRequest request)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        pendingBishopConsecrationVisuals.Enqueue(request);
        if (bishopConsecrationFxRoutine == null)
            bishopConsecrationFxRoutine = StartCoroutine(FlushPendingBishopConsecrationVisualsRoutine());
    }

    private void ClearPendingBishopConsecrationVisuals()
    {
        pendingBishopConsecrationVisuals.Clear();
        if (bishopConsecrationFxRoutine != null)
        {
            StopCoroutine(bishopConsecrationFxRoutine);
            bishopConsecrationFxRoutine = null;
        }
    }

    private IEnumerator FlushPendingBishopConsecrationVisualsRoutine()
    {
        try
        {
            while (pendingBishopConsecrationVisuals.Count > 0)
            {
                BishopConsecrationVisualRequest request = pendingBishopConsecrationVisuals.Dequeue();
                yield return PlayBishopConsecrationVisualRoutine(request);
            }
        }
        finally
        {
            bishopConsecrationFxRoutine = null;
        }
    }

    private IEnumerator PlayBishopConsecrationVisualRoutine(BishopConsecrationVisualRequest request)
    {
        if (battleManager == null) yield break;

        const int maxWaitFrames = 24;
        for (int i = 0; i < maxWaitFrames; i++)
        {
            bool needsMonster = request.onPlayerSide
                ? battleManager.PlayerHasFieldMonster()
                : battleManager.EnemyHasFieldMonster();
            if (needsMonster)
            {
                bool prevDeferEnemy = deferEnemyFieldRefresh;
                bool prevDeferAttack = deferFieldRefreshDuringAttack;
                deferEnemyFieldRefresh = false;
                deferFieldRefreshDuringAttack = false;
                RefreshFieldCards();
                lastFieldSignature = ComputeFieldSignature();
                deferEnemyFieldRefresh = prevDeferEnemy;
                deferFieldRefreshDuringAttack = prevDeferAttack;
            }

            if (TryResolveConsecrationFxParent(request.onPlayerSide, out RectTransform fxParent))
            {
                yield return null;
                yield return null;
                if (TryResolveConsecrationFxParent(request.onPlayerSide, out fxParent))
                {
                    switch (request.kind)
                    {
                        case BishopConsecrationVisualKind.ReserveGranted:
                            yield return PlayConsecrationReserveRoutine(fxParent);
                            break;
                        case BishopConsecrationVisualKind.BoundToField:
                            yield return PlayConsecrationBoundRoutine(fxParent, request.holyTherapyLinkOnNun);
                            break;
                        case BishopConsecrationVisualKind.FirstHitReduced:
                            yield return PlayConsecrationFirstHitRoutine(fxParent, request);
                            break;
                    }
                    yield break;
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// 掛在場區而非場上卡：RefreshFieldCards 會 Destroy 重建卡片，掛卡上特效會被清掉。
    /// </summary>
    private bool TryResolveConsecrationFxParent(bool onPlayerSide, out RectTransform fxParent)
    {
        fxParent = onPlayerSide ? playerFieldArea : enemyFieldArea;
        if (fxParent == null)
            return false;
        GameObject cardObj = onPlayerSide ? playerFieldCardObj : enemyFieldCardObj;
        return cardObj != null;
    }

    private IEnumerator PlayConsecrationReserveRoutine(RectTransform fxParent)
    {
        const float duration = 1.85f;
        Transform parent = fxParent;

        GameObject pillar = new GameObject("ConsecrationReservePillar", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        pillar.transform.SetParent(parent, false);
        RectTransform pillarRt = pillar.GetComponent<RectTransform>();
        pillarRt.anchorMin = new Vector2(0.5f, 0f);
        pillarRt.anchorMax = new Vector2(0.5f, 0f);
        pillarRt.pivot = new Vector2(0.5f, 0f);
        pillarRt.anchoredPosition = new Vector2(0f, -8f);
        pillarRt.sizeDelta = new Vector2(96f, 140f);
        pillar.transform.SetAsLastSibling();
        Image pillarImg = pillar.GetComponent<Image>();
        pillarImg.sprite = GetUnitWhiteSprite();
        pillarImg.raycastTarget = false;
        pillarImg.color = BattleFxColors.ConsecrationReserveOuter;
        CanvasGroup pillarCg = pillar.GetComponent<CanvasGroup>();

        GameObject core = new GameObject("ConsecrationReserveCore", typeof(RectTransform), typeof(Image));
        core.transform.SetParent(pillar.transform, false);
        RectTransform coreRt = core.GetComponent<RectTransform>();
        coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
        coreRt.pivot = new Vector2(0.5f, 0.5f);
        coreRt.sizeDelta = new Vector2(56f, 56f);
        Image coreImg = core.GetComponent<Image>();
        coreImg.sprite = GetUnitWhiteSprite();
        coreImg.raycastTarget = false;
        coreImg.color = BattleFxColors.ConsecrationReserveCore;

        GameObject tag = CreateConsecrationFloatingTag(parent, "祝聖預留", new Vector2(0f, 28f), 24f, ResolveConsecrationFxFont());
        CanvasGroup tagCg = tag.GetComponent<CanvasGroup>();
        tagCg.alpha = 0f;

        float t = 0f;
        while (t < duration && pillar != null)
        {
            t += Time.unscaledDeltaTime;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.55f));
            float fade = 1f - Mathf.Clamp01((t - 1.1f) / 0.75f);
            pillarCg.alpha = Mathf.Clamp01(0.55f * fade * (0.7f + 0.3f * Mathf.Sin(t * 6f)));
            pillarRt.localScale = new Vector3(1f, 0.35f + rise * 0.95f, 1f);
            coreRt.localScale = Vector3.one * (0.85f + 0.2f * Mathf.Sin(t * 8f));
            float tagIn = Mathf.Clamp01((t - 0.25f) / 0.35f);
            float tagOut = 1f - Mathf.Clamp01((t - 1.25f) / 0.45f);
            tagCg.alpha = tagIn * tagOut;
            yield return null;
        }

        if (pillar != null) Destroy(pillar);
        if (tag != null) Destroy(tag);
    }

    private IEnumerator PlayConsecrationBoundRoutine(RectTransform fxParent, bool holyTherapyLinkOnNun)
    {
        const float duration = 1.45f;
        Transform parent = fxParent;

        GameObject ringOuter = new GameObject("ConsecrationBoundRingOuter", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        ringOuter.transform.SetParent(parent, false);
        RectTransform outerRt = ringOuter.GetComponent<RectTransform>();
        outerRt.anchorMin = outerRt.anchorMax = new Vector2(0.5f, 0.5f);
        outerRt.pivot = new Vector2(0.5f, 0.5f);
        outerRt.sizeDelta = new Vector2(148f, 148f);
        ringOuter.transform.SetAsLastSibling();
        Image outerImg = ringOuter.GetComponent<Image>();
        outerImg.sprite = GetUnitWhiteSprite();
        outerImg.raycastTarget = false;
        outerImg.color = BattleFxColors.ConsecrationBoundRing;
        CanvasGroup outerCg = ringOuter.GetComponent<CanvasGroup>();

        GameObject ring = new GameObject("ConsecrationBoundRing", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        ring.transform.SetParent(parent, false);
        RectTransform ringRt = ring.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot = new Vector2(0.5f, 0.5f);
        ringRt.sizeDelta = new Vector2(118f, 118f);
        ring.transform.SetAsLastSibling();
        Image ringImg = ring.GetComponent<Image>();
        ringImg.sprite = GetUnitWhiteSprite();
        ringImg.raycastTarget = false;
        ringImg.color = BattleFxColors.ConsecrationBoundCore;
        CanvasGroup ringCg = ring.GetComponent<CanvasGroup>();

        GameObject core = new GameObject("ConsecrationBoundCore", typeof(RectTransform), typeof(Image));
        core.transform.SetParent(ring.transform, false);
        RectTransform coreRt = core.GetComponent<RectTransform>();
        coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
        coreRt.pivot = new Vector2(0.5f, 0.5f);
        coreRt.sizeDelta = new Vector2(40f, 40f);
        Image coreImg = core.GetComponent<Image>();
        coreImg.sprite = GetUnitWhiteSprite();
        coreImg.raycastTarget = false;
        coreImg.color = BattleFxColors.ConsecrationReserveCore;

        string tagText = holyTherapyLinkOnNun ? "祝聖 · 聖療連攜" : "祝聖";
        GameObject tag = CreateConsecrationFloatingTag(parent, tagText, new Vector2(0f, 24f), 22f, ResolveConsecrationFxFont());
        CanvasGroup tagCg = tag.GetComponent<CanvasGroup>();
        tagCg.alpha = 0f;

        float t = 0f;
        while (t < duration && ring != null)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float pulse = 0.55f + 0.45f * Mathf.Sin(t * 8f);
            float expand = 0.65f + u * 0.55f;
            outerCg.alpha = Mathf.Clamp01((1f - u * 0.85f) * 0.5f * pulse);
            outerRt.localScale = Vector3.one * expand;
            ringCg.alpha = Mathf.Clamp01((1f - u * u) * 0.9f * pulse);
            ringRt.localScale = Vector3.one * (0.78f + u * 0.38f);
            coreRt.localRotation = Quaternion.Euler(0f, 0f, t * 140f);
            float tagIn = Mathf.Clamp01((t - 0.1f) / 0.25f);
            float tagOut = 1f - Mathf.Clamp01((t - 1.05f) / 0.35f);
            tagCg.alpha = tagIn * tagOut;
            yield return null;
        }

        if (ringOuter != null) Destroy(ringOuter);
        if (ring != null) Destroy(ring);
        if (tag != null) Destroy(tag);
    }

    private IEnumerator PlayConsecrationFirstHitRoutine(RectTransform fxParent, BishopConsecrationVisualRequest request)
    {
        const float duration = 1.05f;
        Transform parent = fxParent;

        GameObject shield = new GameObject("ConsecrationShieldFlash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        shield.transform.SetParent(parent, false);
        RectTransform shieldRt = shield.GetComponent<RectTransform>();
        shieldRt.anchorMin = shieldRt.anchorMax = new Vector2(0.5f, 0.5f);
        shieldRt.pivot = new Vector2(0.5f, 0.5f);
        shieldRt.sizeDelta = new Vector2(200f, 280f);
        shield.transform.SetAsLastSibling();
        Image shieldImg = shield.GetComponent<Image>();
        shieldImg.sprite = GetUnitWhiteSprite();
        shieldImg.raycastTarget = false;
        shieldImg.color = BattleFxColors.ConsecrationShieldPeak;
        CanvasGroup shieldCg = shield.GetComponent<CanvasGroup>();

        string label = request.religiousSynergy ? "宗教連攜" : "祝聖";
        GameObject badge = CreateConsecrationReductionBadge(parent, request.reductionAmount, label, ResolveConsecrationFxFont());
        CanvasGroup badgeCg = badge.GetComponent<CanvasGroup>();
        badgeCg.alpha = 0f;

        float t = 0f;
        while (t < duration && shield != null)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float flash = u < 0.35f ? u / 0.35f : 1f - (u - 0.35f) / 0.65f;
            shieldCg.alpha = Mathf.Clamp01(flash * 0.78f);
            shieldRt.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * 14f));
            float badgeIn = Mathf.Clamp01((t - 0.08f) / 0.22f);
            float badgeOut = 1f - Mathf.Clamp01((t - 0.72f) / 0.3f);
            badgeCg.alpha = badgeIn * badgeOut;
            RectTransform badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(12f, 18f + 14f * Mathf.SmoothStep(0f, 1f, badgeIn));
            yield return null;
        }

        if (shield != null) Destroy(shield);
        if (badge != null) Destroy(badge);
    }

    private static GameObject CreateConsecrationFloatingTag(
        Transform parent, string text, Vector2 anchoredPos, float fontSize, TMP_FontAsset font)
    {
        GameObject tag = new GameObject("ConsecrationTag", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        tag.transform.SetParent(parent, false);
        RectTransform tagRt = tag.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0.5f, 1f);
        tagRt.pivot = new Vector2(0.5f, 0f);
        tagRt.anchoredPosition = anchoredPos;
        tagRt.sizeDelta = new Vector2(240f, 40f);
        tag.transform.SetAsLastSibling();
        TextMeshProUGUI tmp = tag.GetComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.font = font != null ? font : UiFontResolver.ResolveUiFont();
        tmp.text = text;
        tmp.color = BattleFxColors.ConsecrationLabelText;
        tag.AddComponent<CanvasGroup>();
        return tag;
    }

    private static GameObject CreateConsecrationReductionBadge(
        Transform parent, int reduction, string label, TMP_FontAsset font)
    {
        GameObject root = new GameObject("ConsecrationReduceBadge", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 0f);
        rootRt.anchoredPosition = new Vector2(12f, 18f);
        rootRt.sizeDelta = new Vector2(118f, 54f);
        root.transform.SetAsLastSibling();

        GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(root.transform, false);
        RectTransform glowRt = glow.GetComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.offsetMin = new Vector2(-6f, -4f);
        glowRt.offsetMax = new Vector2(6f, 4f);
        Image glowImg = glow.GetComponent<Image>();
        glowImg.sprite = GetUnitWhiteSprite();
        glowImg.color = BattleFxColors.ConsecrationReduceLabelGlow;
        glowImg.raycastTarget = false;

        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.sprite = GetUnitWhiteSprite();
        bgImg.color = BattleFxColors.ConsecrationReduceLabelBg;
        bgImg.raycastTarget = false;

        GameObject amountObj = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        amountObj.transform.SetParent(root.transform, false);
        RectTransform amountRt = amountObj.GetComponent<RectTransform>();
        amountRt.anchorMin = new Vector2(0f, 0.5f);
        amountRt.anchorMax = new Vector2(0.42f, 0.5f);
        amountRt.offsetMin = new Vector2(6f, -18f);
        amountRt.offsetMax = new Vector2(-2f, 18f);
        TextMeshProUGUI amountTmp = amountObj.GetComponent<TextMeshProUGUI>();
        amountTmp.raycastTarget = false;
        amountTmp.fontSize = 28f;
        amountTmp.alignment = TextAlignmentOptions.MidlineRight;
        amountTmp.font = font != null ? font : UiFontResolver.ResolveUiFont();
        amountTmp.text = "-" + reduction;
        amountTmp.color = BattleFxColors.ConsecrationReduceLabelText;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(root.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.4f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(0f, 4f);
        labelRt.offsetMax = new Vector2(-6f, -4f);
        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        labelTmp.raycastTarget = false;
        labelTmp.fontSize = 18f;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.font = font;
        labelTmp.text = label;
        labelTmp.color = BattleFxColors.ConsecrationLabelText;

        root.AddComponent<CanvasGroup>();
        return root;
    }

    private TMP_FontAsset ResolveConsecrationFxFont() =>
        sharedUIFont != null ? sharedUIFont : UiFontResolver.ResolveUiFont();
}

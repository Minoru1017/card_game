using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private readonly Queue<CastleFortressStandVisualRequest> pendingCastleFortressVisuals =
        new Queue<CastleFortressStandVisualRequest>();

    private Coroutine castleFortressFxRoutine;

    private void OnCastleFortressStandVisualRequested(CastleFortressStandVisualRequest request)
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        pendingCastleFortressVisuals.Enqueue(request);
        if (castleFortressFxRoutine == null)
            castleFortressFxRoutine = StartCoroutine(FlushPendingCastleFortressVisualsRoutine());
    }

    private void ClearPendingCastleFortressVisuals()
    {
        pendingCastleFortressVisuals.Clear();
        if (castleFortressFxRoutine != null)
        {
            StopCoroutine(castleFortressFxRoutine);
            castleFortressFxRoutine = null;
        }
    }

    private IEnumerator FlushPendingCastleFortressVisualsRoutine()
    {
        try
        {
            while (pendingCastleFortressVisuals.Count > 0)
            {
                CastleFortressStandVisualRequest request = pendingCastleFortressVisuals.Dequeue();
                yield return PlayCastleFortressStandVisualRoutine(request);
            }
        }
        finally
        {
            castleFortressFxRoutine = null;
        }
    }

    private IEnumerator PlayCastleFortressStandVisualRoutine(CastleFortressStandVisualRequest request)
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

            if (TryResolveCastleFortressFxParent(request.onPlayerSide, out RectTransform fxParent))
            {
                yield return null;
                yield return null;
                if (TryResolveCastleFortressFxParent(request.onPlayerSide, out fxParent))
                {
                    yield return PlayCastleFortressStandHitRoutine(fxParent, request);
                    yield break;
                }
            }

            yield return null;
        }
    }

    private bool TryResolveCastleFortressFxParent(bool onPlayerSide, out RectTransform fxParent)
    {
        fxParent = onPlayerSide ? playerFieldArea : enemyFieldArea;
        if (fxParent == null)
            return false;
        GameObject cardObj = onPlayerSide ? playerFieldCardObj : enemyFieldCardObj;
        return cardObj != null;
    }

    private IEnumerator PlayCastleFortressStandHitRoutine(
        RectTransform fxParent,
        CastleFortressStandVisualRequest request)
    {
        const float duration = 1.15f;
        Transform parent = fxParent;
        TMP_FontAsset font = ResolveCastleFortressFxFont();

        GameObject wallBase = new GameObject("CastleFortressWallBase", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        wallBase.transform.SetParent(parent, false);
        RectTransform baseRt = wallBase.GetComponent<RectTransform>();
        baseRt.anchorMin = new Vector2(0.5f, 0f);
        baseRt.anchorMax = new Vector2(0.5f, 0f);
        baseRt.pivot = new Vector2(0.5f, 0f);
        baseRt.anchoredPosition = new Vector2(0f, -6f);
        baseRt.sizeDelta = new Vector2(220f, 52f);
        wallBase.transform.SetAsLastSibling();
        Image baseImg = wallBase.GetComponent<Image>();
        baseImg.sprite = GetUnitWhiteSprite();
        baseImg.raycastTarget = false;
        baseImg.color = BattleFxColors.CastleFortressWallOuter;
        CanvasGroup baseCg = wallBase.GetComponent<CanvasGroup>();

        GameObject wallMid = new GameObject("CastleFortressWallMid", typeof(RectTransform), typeof(Image));
        wallMid.transform.SetParent(wallBase.transform, false);
        RectTransform midRt = wallMid.GetComponent<RectTransform>();
        midRt.anchorMin = midRt.anchorMax = new Vector2(0.5f, 0.5f);
        midRt.pivot = new Vector2(0.5f, 0.5f);
        midRt.sizeDelta = new Vector2(188f, 34f);
        Image midImg = wallMid.GetComponent<Image>();
        midImg.sprite = GetUnitWhiteSprite();
        midImg.raycastTarget = false;
        midImg.color = BattleFxColors.CastleFortressWallMid;

        GameObject wallTop = new GameObject("CastleFortressWallTop", typeof(RectTransform), typeof(Image));
        wallTop.transform.SetParent(wallBase.transform, false);
        RectTransform topRt = wallTop.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0.5f, 1f);
        topRt.anchorMax = new Vector2(0.5f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.anchoredPosition = new Vector2(0f, 2f);
        topRt.sizeDelta = new Vector2(156f, 14f);
        Image topImg = wallTop.GetComponent<Image>();
        topImg.sprite = GetUnitWhiteSprite();
        topImg.raycastTarget = false;
        topImg.color = BattleFxColors.CastleFortressWallHighlight;

        GameObject flash = new GameObject("CastleFortressFlash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        flash.transform.SetParent(parent, false);
        RectTransform flashRt = flash.GetComponent<RectTransform>();
        flashRt.anchorMin = flashRt.anchorMax = new Vector2(0.5f, 0.5f);
        flashRt.pivot = new Vector2(0.5f, 0.5f);
        flashRt.sizeDelta = new Vector2(210f, 260f);
        flash.transform.SetAsLastSibling();
        Image flashImg = flash.GetComponent<Image>();
        flashImg.sprite = GetUnitWhiteSprite();
        flashImg.raycastTarget = false;
        flashImg.color = BattleFxColors.CastleFortressFlashPeak;
        CanvasGroup flashCg = flash.GetComponent<CanvasGroup>();

        var shards = new List<GameObject>(4);
        for (int s = 0; s < 4; s++)
        {
            GameObject shard = new GameObject("CastleFortressShard" + s, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            shard.transform.SetParent(parent, false);
            RectTransform shardRt = shard.GetComponent<RectTransform>();
            shardRt.anchorMin = shardRt.anchorMax = new Vector2(0.5f, 0.35f);
            shardRt.pivot = new Vector2(0.5f, 0.5f);
            float w = 18f + s * 4f;
            float h = 14f + (s % 2) * 6f;
            shardRt.sizeDelta = new Vector2(w, h);
            shardRt.anchoredPosition = new Vector2(-52f + s * 34f, 8f + (s % 2) * 6f);
            shardRt.localRotation = Quaternion.Euler(0f, 0f, -18f + s * 12f);
            shard.transform.SetAsLastSibling();
            Image shardImg = shard.GetComponent<Image>();
            shardImg.sprite = GetUnitWhiteSprite();
            shardImg.raycastTarget = false;
            shardImg.color = BattleFxColors.CastleFortressShard;
            shard.AddComponent<CanvasGroup>();
            shards.Add(shard);
        }

        GameObject tag = CreateCastleFortressFloatingTag(parent, "堅城駐守", new Vector2(0f, 30f), 22f, font);
        CanvasGroup tagCg = tag.GetComponent<CanvasGroup>();
        tagCg.alpha = 0f;

        GameObject badge = CreateCastleFortressReductionBadge(parent, request.reductionAmount, font);
        CanvasGroup badgeCg = badge.GetComponent<CanvasGroup>();
        badgeCg.alpha = 0f;

        float t = 0f;
        while (t < duration && wallBase != null)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.42f));
            float fade = 1f - Mathf.Clamp01((t - 0.85f) / 0.3f);
            baseCg.alpha = Mathf.Clamp01(0.85f * fade * (0.75f + 0.25f * Mathf.Sin(t * 7f)));
            baseRt.localScale = new Vector3(0.55f + rise * 0.5f, 0.25f + rise * 0.95f, 1f);
            midRt.localScale = Vector3.one * (0.9f + 0.08f * Mathf.Sin(t * 9f));

            float flashPeak = u < 0.28f ? u / 0.28f : 1f - (u - 0.28f) / 0.72f;
            flashCg.alpha = Mathf.Clamp01(flashPeak * 0.72f);
            flashRt.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 12f));

            for (int s = 0; s < shards.Count; s++)
            {
                if (shards[s] == null) continue;
                CanvasGroup scg = shards[s].GetComponent<CanvasGroup>();
                RectTransform srt = shards[s].GetComponent<RectTransform>();
                float shardIn = Mathf.Clamp01((t - 0.05f * s) / 0.3f);
                float shardOut = 1f - Mathf.Clamp01((t - 0.7f) / 0.35f);
                scg.alpha = shardIn * shardOut * 0.9f;
                Vector2 start = new Vector2(-52f + s * 34f, 8f + (s % 2) * 6f);
                srt.anchoredPosition = start + new Vector2(0f, 22f * shardIn);
                srt.localRotation = Quaternion.Euler(0f, 0f, -18f + s * 12f + t * 40f);
            }

            float tagIn = Mathf.Clamp01((t - 0.12f) / 0.24f);
            float tagOut = 1f - Mathf.Clamp01((t - 0.88f) / 0.25f);
            tagCg.alpha = tagIn * tagOut;

            float badgeIn = Mathf.Clamp01((t - 0.1f) / 0.22f);
            float badgeOut = 1f - Mathf.Clamp01((t - 0.78f) / 0.3f);
            badgeCg.alpha = badgeIn * badgeOut;
            RectTransform badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(12f, 16f + 12f * Mathf.SmoothStep(0f, 1f, badgeIn));

            yield return null;
        }

        if (wallBase != null) Destroy(wallBase);
        if (flash != null) Destroy(flash);
        if (tag != null) Destroy(tag);
        if (badge != null) Destroy(badge);
        for (int s = 0; s < shards.Count; s++)
        {
            if (shards[s] != null) Destroy(shards[s]);
        }
    }

    private static GameObject CreateCastleFortressFloatingTag(
        Transform parent, string text, Vector2 anchoredPos, float fontSize, TMP_FontAsset font)
    {
        GameObject tag = new GameObject("CastleFortressTag", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
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
        tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.color = BattleFxColors.CastleFortressLabelText;
        tag.AddComponent<CanvasGroup>();
        return tag;
    }

    private static GameObject CreateCastleFortressReductionBadge(
        Transform parent, int reduction, TMP_FontAsset font)
    {
        GameObject root = new GameObject("CastleFortressReduceBadge", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 0f);
        rootRt.anchoredPosition = new Vector2(12f, 16f);
        rootRt.sizeDelta = new Vector2(128f, 54f);
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
        glowImg.color = BattleFxColors.CastleFortressReduceLabelGlow;
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
        bgImg.color = BattleFxColors.CastleFortressReduceLabelBg;
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
        amountTmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
        amountTmp.text = "-" + reduction;
        amountTmp.color = BattleFxColors.CastleFortressReduceLabelText;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(root.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.4f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(0f, 4f);
        labelRt.offsetMax = new Vector2(-6f, -4f);
        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        labelTmp.raycastTarget = false;
        labelTmp.fontSize = 17f;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.font = font;
        labelTmp.text = "堅城";
        labelTmp.color = BattleFxColors.CastleFortressLabelText;

        root.AddComponent<CanvasGroup>();
        return root;
    }

    private TMP_FontAsset ResolveCastleFortressFxFont() =>
        ResolveBattleRichTextUIFont();
}

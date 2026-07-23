using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private IEnumerator PlayLongbowPierceFxRoutine(bool attackerIsPlayer, int pierceDamage, float hitFxDur)
    {
        if (BattleAutoSimPlugin.IsRunning || uiRoot == null || pierceDamage <= 0)
            yield break;

        yield return null;

        GameObject attackerObj = ResolveFieldMonsterVisualForFx(attackerIsPlayer);
        bool targetIsPlayerHero = !attackerIsPlayer;
        GameObject heroHudObj = GetHeroHudObjectForDirectAttack(targetIsPlayerHero);
        if (attackerObj == null || heroHudObj == null)
            yield break;

        RectTransform attackerRt = attackerObj.GetComponent<RectTransform>();
        if (attackerRt == null)
            yield break;

        Vector2 startLocal = GetCenterInUiRoot(attackerRt);
        Vector2 endLocal = GetHeroHpCenterLocal(!attackerIsPlayer);
        Vector2 delta = endLocal - startLocal;
        float travelDist = delta.magnitude;
        if (travelDist < 24f)
            yield break;

        float angleZ = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        TMP_FontAsset font = ResolveBattleRichTextUIFont();

        var trailStreaks = new List<GameObject>(3);
        for (int i = 0; i < 3; i++)
        {
            GameObject streak = CreateLongbowPierceStreak(uiRoot, startLocal, angleZ, 96f + i * 18f, 8f - i * 1.5f);
            trailStreaks.Add(streak);
        }

        GameObject arrow = new GameObject("LongbowPierceArrow", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        arrow.transform.SetParent(uiRoot, false);
        arrow.transform.SetAsLastSibling();
        RectTransform arrowRt = arrow.GetComponent<RectTransform>();
        arrowRt.anchorMin = arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRt.pivot = new Vector2(0.15f, 0.5f);
        arrowRt.sizeDelta = new Vector2(72f, 16f);
        arrowRt.anchoredPosition = startLocal;
        arrowRt.localRotation = Quaternion.Euler(0f, 0f, angleZ);
        Image arrowImg = arrow.GetComponent<Image>();
        arrowImg.sprite = GetUnitWhiteSprite();
        arrowImg.raycastTarget = false;
        arrowImg.color = BattleFxColors.LongbowPierceTrailCore;
        CanvasGroup arrowCg = arrow.GetComponent<CanvasGroup>();

        GameObject head = new GameObject("Head", typeof(RectTransform), typeof(Image));
        head.transform.SetParent(arrow.transform, false);
        RectTransform headRt = head.GetComponent<RectTransform>();
        headRt.anchorMin = headRt.anchorMax = new Vector2(1f, 0.5f);
        headRt.pivot = new Vector2(0.5f, 0.5f);
        headRt.anchoredPosition = Vector2.zero;
        headRt.sizeDelta = new Vector2(18f, 18f);
        Image headImg = head.GetComponent<Image>();
        headImg.sprite = GetUnitWhiteSprite();
        headImg.raycastTarget = false;
        headImg.color = BattleFxColors.LongbowPierceArrowHead;

        GameObject tag = CreateLongbowPierceTag(attackerRt, "穿矢", font);

        float travelDur = LethalBlowCinematicFx.ScaleDuration(0.42f);
        float t = 0f;
        while (t < travelDur && arrow != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / travelDur);
            float eased = 1f - (1f - p) * (1f - p);
            Vector2 pos = Vector2.Lerp(startLocal, endLocal, eased);
            arrowRt.anchoredPosition = pos;
            arrowCg.alpha = 0.55f + 0.45f * Mathf.Clamp01(p / 0.2f);
            arrowRt.localScale = Vector3.one * (0.85f + 0.25f * eased);

            for (int i = 0; i < trailStreaks.Count; i++)
            {
                if (trailStreaks[i] == null) continue;
                RectTransform streakRt = trailStreaks[i].GetComponent<RectTransform>();
                CanvasGroup streakCg = trailStreaks[i].GetComponent<CanvasGroup>();
                float lag = Mathf.Clamp01((p - 0.06f * i) / 0.88f);
                streakRt.anchoredPosition = Vector2.Lerp(startLocal, endLocal, lag);
                streakCg.alpha = (1f - lag) * (0.35f + 0.25f * (1f - i * 0.2f));
                streakRt.localScale = new Vector3(0.5f + lag * 0.8f, 1f, 1f);
            }

            yield return null;
        }

        GameObject hitFlash = new GameObject("LongbowPierceHitFlash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        hitFlash.transform.SetParent(uiRoot, false);
        hitFlash.transform.SetAsLastSibling();
        RectTransform hitRt = hitFlash.GetComponent<RectTransform>();
        hitRt.anchorMin = hitRt.anchorMax = new Vector2(0.5f, 0.5f);
        hitRt.pivot = new Vector2(0.5f, 0.5f);
        hitRt.anchoredPosition = endLocal;
        hitRt.sizeDelta = new Vector2(96f, 96f);
        Image hitImg = hitFlash.GetComponent<Image>();
        hitImg.sprite = GetUnitWhiteSprite();
        hitImg.raycastTarget = false;
        hitImg.color = BattleFxColors.LongbowPierceFlashPeak;
        CanvasGroup hitCg = hitFlash.GetComponent<CanvasGroup>();

        float flashDur = LethalBlowCinematicFx.ScaleDuration(Mathf.Max(0.12f, hitFxDur * 0.75f));
        t = 0f;
        while (t < flashDur && hitFlash != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / flashDur);
            float peak = p < 0.35f ? p / 0.35f : 1f - (p - 0.35f) / 0.65f;
            hitCg.alpha = peak * 0.78f;
            hitRt.localScale = Vector3.one * (0.75f + peak * 0.55f);
            yield return null;
        }

        if (heroHudObj != null)
        {
            yield return StartCoroutine(PlayDamageFlash(heroHudObj, hitFxDur));
            if (pierceDamage > 0)
                StartCoroutine(PlayFloatingDamageNumber(heroHudObj, pierceDamage, FloatingDamageKind.Attack));
            if (targetIsPlayerHero)
            {
                deferPlayerHeroDamagedFxForDirectAttack = false;
                if (playerHeroDamagedFxRoutine == null)
                    playerHeroDamagedFxRoutine = StartCoroutine(CoPlayPlayerHeroDamagedFeedback(enhancedDirectHit: true));
            }
            else
            {
                yield return StartCoroutine(CoPlayEnemyHeroDamagedFeedback(enhancedDirectHit: true));
            }
        }

        if (arrow != null) Destroy(arrow);
        if (hitFlash != null) Destroy(hitFlash);
        if (tag != null) Destroy(tag);
        for (int i = 0; i < trailStreaks.Count; i++)
        {
            if (trailStreaks[i] != null) Destroy(trailStreaks[i]);
        }
    }

    private static GameObject CreateLongbowPierceStreak(
        Transform parent,
        Vector2 anchoredPos,
        float angleZ,
        float length,
        float height)
    {
        GameObject streak = new GameObject("LongbowPierceStreak", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        streak.transform.SetParent(parent, false);
        RectTransform streakRt = streak.GetComponent<RectTransform>();
        streakRt.anchorMin = streakRt.anchorMax = new Vector2(0.5f, 0.5f);
        streakRt.pivot = new Vector2(0f, 0.5f);
        streakRt.sizeDelta = new Vector2(length, height);
        streakRt.anchoredPosition = anchoredPos;
        streakRt.localRotation = Quaternion.Euler(0f, 0f, angleZ);
        Image streakImg = streak.GetComponent<Image>();
        streakImg.sprite = GetUnitWhiteSprite();
        streakImg.raycastTarget = false;
        streakImg.color = BattleFxColors.LongbowPierceTrailGlow;
        streak.GetComponent<CanvasGroup>().alpha = 0f;
        return streak;
    }

    private static GameObject CreateLongbowPierceTag(Transform parent, string text, TMP_FontAsset font)
    {
        GameObject root = new GameObject("LongbowPierceTag", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 1f);
        rootRt.anchorMax = new Vector2(0.5f, 1f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, 36f);
        rootRt.sizeDelta = new Vector2(160f, 54f);
        root.transform.SetAsLastSibling();
        CanvasGroup rootCg = root.GetComponent<CanvasGroup>();
        rootCg.alpha = 0f;

        GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(root.transform, false);
        RectTransform glowRt = glow.GetComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.offsetMin = new Vector2(-8f, -4f);
        glowRt.offsetMax = new Vector2(8f, 4f);
        Image glowImg = glow.GetComponent<Image>();
        glowImg.sprite = GetUnitWhiteSprite();
        glowImg.raycastTarget = false;
        glowImg.color = BattleFxColors.LongbowPierceLabelGlow;

        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.sprite = GetUnitWhiteSprite();
        bgImg.raycastTarget = false;
        bgImg.color = BattleFxColors.LongbowPierceLabelBg;

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(root.transform, false);
        RectTransform borderRt = border.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-2f, -2f);
        borderRt.offsetMax = new Vector2(2f, 2f);
        Image borderImg = border.GetComponent<Image>();
        borderImg.sprite = GetUnitWhiteSprite();
        borderImg.raycastTarget = false;
        borderImg.color = BattleFxColors.LongbowPierceLabelBorder;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(root.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(6f, 4f);
        labelRt.offsetMax = new Vector2(-6f, -4f);
        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        labelTmp.raycastTarget = false;
        labelTmp.fontSize = 22f;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.font = font != null ? font : UiFontResolver.ResolveUiFont();
        labelTmp.text = text;
        labelTmp.color = BattleFxColors.LongbowPierceLabelText;

        rootCg.alpha = 1f;
        return root;
    }
}

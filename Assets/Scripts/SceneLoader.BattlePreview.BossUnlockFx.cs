using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class SceneLoader
{
    private const int BossUnlockFxRayCount = 14;
    private const int BossUnlockFxSparkCount = 10;
    private static Sprite _bossUnlockFxUiSprite;

    private void StopBossUnlockRevealFx()
    {
        battlePreviewBossUnlockAnimating = false;
        if (battlePreviewBossUnlockFxRoutine != null)
        {
            StopCoroutine(battlePreviewBossUnlockFxRoutine);
            battlePreviewBossUnlockFxRoutine = null;
        }

        if (battlePreviewArchRowRoot != null)
        {
            CanvasGroup archCg = battlePreviewArchRowRoot.GetComponent<CanvasGroup>();
            if (archCg != null)
                archCg.alpha = 1f;
            battlePreviewArchRowRoot.transform.localScale = Vector3.one;
        }

        if (battlePreviewBossTierButton != null)
            battlePreviewBossTierButton.transform.localScale = Vector3.one;
    }

    private IEnumerator CoUnlockBossTierRevealFx()
    {
        battlePreviewBossUnlockAnimating = true;
        StopAllAuthoredDifficultyFeedbackAnims();

        RectTransform panelRt = ResolveBattlePreviewPanelRect();
        if (panelRt == null || battlePreviewBossRevealRoot == null || battlePreviewBossTierButton == null)
        {
            CompleteBossTierUnlockState();
            battlePreviewBossUnlockAnimating = false;
            battlePreviewBossUnlockFxRoutine = null;
            yield break;
        }

        BattleDifficultyTier revealTier = battlePreviewAuthoredRevealTier;
        Color bossAccent = GetDifficultyTierAccentColor(revealTier);
        Color goldAccent = new Color(1f, 0.82f, 0.28f, 1f);
        Color flashTint = Color.Lerp(bossAccent, goldAccent, 0.42f);

        GameObject fxRoot = CreateBossUnlockFxRoot(panelRt);
        Transform burstRoot = fxRoot.transform.Find("BurstRoot");
        Image flashImg = fxRoot.transform.Find("ScreenFlash")?.GetComponent<Image>();
        List<Image> ringImgs = new List<Image>(3);
        List<RectTransform> rayRts = new List<RectTransform>(BossUnlockFxRayCount);
        List<Image> sparkImgs = new List<Image>(BossUnlockFxSparkCount);

        if (burstRoot != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform ringTr = burstRoot.Find("Ring" + i);
                if (ringTr != null)
                {
                    Image ringImg = ringTr.GetComponent<Image>();
                    if (ringImg != null)
                        ringImgs.Add(ringImg);
                }
            }

            for (int i = 0; i < BossUnlockFxRayCount; i++)
            {
                Transform rayTr = burstRoot.Find("Ray" + i);
                if (rayTr != null)
                    rayRts.Add(rayTr.GetComponent<RectTransform>());
            }

            for (int i = 0; i < BossUnlockFxSparkCount; i++)
            {
                Transform sparkTr = burstRoot.Find("Spark" + i);
                if (sparkTr != null)
                {
                    Image sparkImg = sparkTr.GetComponent<Image>();
                    if (sparkImg != null)
                        sparkImgs.Add(sparkImg);
                }
            }
        }

        yield return CoBossUnlockScreenFlash(flashImg, flashTint);
        yield return CoBossUnlockCollapseArchRow(0.34f);
        yield return CoBossUnlockBurstRingsAndRays(ringImgs, rayRts, sparkImgs, bossAccent, goldAccent);

        SetAuthoredArchButtonsVisible(false);
        battlePreviewBossRevealRoot.SetActive(true);
        yield return CoBossUnlockRevealBossButton(bossAccent, goldAccent);

        CompleteBossTierUnlockState();
        yield return CoBossUnlockPuzzleTextPunch();

        if (fxRoot != null)
            Destroy(fxRoot);

        battlePreviewBossUnlockAnimating = false;
        battlePreviewBossUnlockFxRoutine = null;
    }

    private IEnumerator CoUnlockPz02HardFourthArchFx()
    {
        battlePreviewBossUnlockAnimating = true;
        StopAllAuthoredDifficultyFeedbackAnims();

        RectTransform panelRt = ResolveBattlePreviewPanelRect();
        int slotIndex = BattlePreviewPuzzleIndex.Pz02HardUnlockArchSlotIndex;
        Button fourthBtn = slotIndex >= 0 && slotIndex < battlePreviewDifficultyButtons.Count
            ? battlePreviewDifficultyButtons[slotIndex]
            : null;

        Color hardAccent = GetDifficultyTierAccentColor(BattleDifficultyTier.Hard);
        Color goldAccent = new Color(1f, 0.82f, 0.28f, 1f);
        Color flashTint = Color.Lerp(hardAccent, goldAccent, 0.35f);

        if (panelRt != null)
        {
            GameObject fxRoot = new GameObject("Pz02HardUnlockFx", typeof(RectTransform));
            fxRoot.transform.SetParent(panelRt, false);
            fxRoot.transform.SetAsLastSibling();
            StretchRect(fxRoot.GetComponent<RectTransform>());
            GameObject flashObj = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
            flashObj.transform.SetParent(fxRoot.transform, false);
            StretchRect(flashObj.GetComponent<RectTransform>());
            Image flashImg = flashObj.GetComponent<Image>();
            SetupBossUnlockFxImage(flashImg, Color.white);
            flashImg.color = new Color(1f, 1f, 1f, 0f);
            flashImg.raycastTarget = false;
            yield return CoBossUnlockScreenFlash(flashImg, flashTint);
            Destroy(fxRoot);
        }

        if (fourthBtn != null)
        {
            Transform fourthTr = fourthBtn.transform;
            Image fourthImg = fourthBtn.GetComponent<Image>();
            Vector3 baseScale = fourthTr.localScale;
            const float morphDuration = 0.48f;
            float elapsed = 0f;
            while (elapsed < morphDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / morphDuration);
                float scale = Mathf.Lerp(1f, 1.18f, EaseOutSoftBack(p));
                if (p > 0.7f)
                    scale = Mathf.Lerp(1.18f, 1f, EaseInOutSmooth((p - 0.7f) / 0.3f));
                fourthTr.localScale = baseScale * scale;
                if (fourthImg != null)
                {
                    Color c = Color.Lerp(Color.white, hardAccent, Mathf.Sin(p * Mathf.PI) * 0.4f);
                    c.a = 1f;
                    fourthImg.color = c;
                }

                yield return null;
            }

            fourthTr.localScale = baseScale;
            if (fourthImg != null)
                fourthImg.color = Color.white;
        }

        CompleteBossTierUnlockState();
        yield return CoBossUnlockPuzzleTextPunch();

        battlePreviewBossUnlockAnimating = false;
        battlePreviewBossUnlockFxRoutine = null;
    }

    private RectTransform ResolveBattlePreviewPanelRect()
    {
        if (battlePreviewPanelRt != null)
            return battlePreviewPanelRt;
        if (battlePreviewOverlayRoot == null)
            return null;
        Transform panelTr = battlePreviewOverlayRoot.transform.Find("BattlePreviewPanel");
        return panelTr != null ? panelTr.GetComponent<RectTransform>() : null;
    }

    private GameObject CreateBossUnlockFxRoot(RectTransform panelRt)
    {
        GameObject fxRoot = new GameObject("BossUnlockFx", typeof(RectTransform));
        fxRoot.transform.SetParent(panelRt, false);
        fxRoot.transform.SetAsLastSibling();
        RectTransform fxRt = fxRoot.GetComponent<RectTransform>();
        fxRt.anchorMin = Vector2.zero;
        fxRt.anchorMax = Vector2.one;
        fxRt.offsetMin = Vector2.zero;
        fxRt.offsetMax = Vector2.zero;

        GameObject flashObj = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
        flashObj.transform.SetParent(fxRoot.transform, false);
        StretchRect(flashObj.GetComponent<RectTransform>());
        Image flashImg = flashObj.GetComponent<Image>();
        SetupBossUnlockFxImage(flashImg, Color.white);
        flashImg.color = new Color(1f, 1f, 1f, 0f);
        flashImg.raycastTarget = false;

        GameObject burstObj = new GameObject("BurstRoot", typeof(RectTransform));
        burstObj.transform.SetParent(fxRoot.transform, false);
        RectTransform burstRt = burstObj.GetComponent<RectTransform>();
        burstRt.anchorMin = new Vector2(AuthoredBossRevealAnchorXMin, AuthoredBossRevealAnchorYMin);
        burstRt.anchorMax = new Vector2(AuthoredBossRevealAnchorXMax, AuthoredBossRevealAnchorYMax);
        burstRt.offsetMin = Vector2.zero;
        burstRt.offsetMax = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            Image ring = CreateBossUnlockFxImage(burstObj.transform, "Ring" + i, Color.white);
            RectTransform ringRt = ring.rectTransform;
            ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.pivot = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(40f, 40f);
            ring.color = new Color(1f, 1f, 1f, 0f);
        }

        for (int i = 0; i < BossUnlockFxRayCount; i++)
        {
            Image ray = CreateBossUnlockFxImage(burstObj.transform, "Ray" + i, Color.white);
            RectTransform rayRt = ray.rectTransform;
            rayRt.anchorMin = rayRt.anchorMax = new Vector2(0.5f, 0.5f);
            rayRt.pivot = new Vector2(0.5f, 0f);
            rayRt.sizeDelta = new Vector2(10f, 140f);
            float angle = (360f / BossUnlockFxRayCount) * i;
            rayRt.localRotation = Quaternion.Euler(0f, 0f, angle);
            ray.color = new Color(1f, 1f, 1f, 0f);
        }

        for (int i = 0; i < BossUnlockFxSparkCount; i++)
        {
            Image spark = CreateBossUnlockFxImage(burstObj.transform, "Spark" + i, Color.white);
            RectTransform sparkRt = spark.rectTransform;
            sparkRt.anchorMin = sparkRt.anchorMax = new Vector2(0.5f, 0.5f);
            sparkRt.pivot = new Vector2(0.5f, 0.5f);
            sparkRt.sizeDelta = new Vector2(14f, 14f);
            float angle = (360f / BossUnlockFxSparkCount) * i + 18f;
            sparkRt.anchoredPosition = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) * 36f, Mathf.Sin(angle * Mathf.Deg2Rad) * 36f);
            spark.color = new Color(1f, 1f, 1f, 0f);
        }

        return fxRoot;
    }

    private IEnumerator CoBossUnlockScreenFlash(Image flashImg, Color flashTint)
    {
        if (flashImg == null)
            yield break;

        const float rise = 0.14f;
        const float fall = 0.28f;
        float elapsed = 0f;
        while (elapsed < rise)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / rise);
            Color c = flashTint;
            c.a = Mathf.Lerp(0f, 0.52f, EaseInOutSmooth(p));
            flashImg.color = c;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fall)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / fall);
            Color c = flashTint;
            c.a = Mathf.Lerp(0.52f, 0f, EaseInOutSmooth(p));
            flashImg.color = c;
            yield return null;
        }

        flashImg.color = new Color(flashTint.r, flashTint.g, flashTint.b, 0f);
    }

    private IEnumerator CoBossUnlockCollapseArchRow(float duration)
    {
        if (battlePreviewArchRowRoot == null)
            yield break;

        CanvasGroup archCg = battlePreviewArchRowRoot.GetComponent<CanvasGroup>();
        if (archCg == null)
            archCg = battlePreviewArchRowRoot.AddComponent<CanvasGroup>();

        Transform archTr = battlePreviewArchRowRoot.transform;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInOutSmooth(p);
            archCg.alpha = Mathf.Lerp(1f, 0f, eased);
            float scale = Mathf.Lerp(1f, 0.82f, eased);
            archTr.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        archCg.alpha = 0f;
        archTr.localScale = Vector3.one * 0.82f;
    }

    private IEnumerator CoBossUnlockBurstRingsAndRays(
        List<Image> ringImgs,
        List<RectTransform> rayRts,
        List<Image> sparkImgs,
        Color bossAccent,
        Color goldAccent)
    {
        const float duration = 0.62f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < ringImgs.Count; i++)
            {
                Image ring = ringImgs[i];
                if (ring == null) continue;
                float local = Mathf.Clamp01((p - i * 0.12f) / 0.72f);
                float scale = Mathf.Lerp(0.35f, 2.6f + i * 0.35f, EaseOutSoftBack(local));
                ring.rectTransform.localScale = new Vector3(scale, scale, 1f);
                Color ringColor = Color.Lerp(goldAccent, bossAccent, i * 0.35f);
                ringColor.a = Mathf.Lerp(0.65f, 0f, local) * (1f - p * 0.35f);
                ring.color = ringColor;
            }

            for (int i = 0; i < rayRts.Count; i++)
            {
                RectTransform rayRt = rayRts[i];
                if (rayRt == null) continue;
                Image rayImg = rayRt.GetComponent<Image>();
                if (rayImg == null) continue;
                float rayPhase = Mathf.Repeat(p * 1.35f + i * 0.04f, 1f);
                float alpha = Mathf.Sin(rayPhase * Mathf.PI) * 0.55f;
                Color rayColor = Color.Lerp(goldAccent, bossAccent, 0.25f + 0.5f * Mathf.PingPong(p + i * 0.07f, 1f));
                rayColor.a = alpha * (1f - p * 0.4f);
                rayImg.color = rayColor;
                float spin = (360f / BossUnlockFxRayCount) * i + p * 120f;
                rayRt.localRotation = Quaternion.Euler(0f, 0f, spin);
                float len = Mathf.Lerp(80f, 200f, EaseOutSoftBack(Mathf.Clamp01(p * 1.2f)));
                rayRt.sizeDelta = new Vector2(10f, len);
            }

            for (int i = 0; i < sparkImgs.Count; i++)
            {
                Image spark = sparkImgs[i];
                if (spark == null) continue;
                float sparkP = Mathf.Clamp01((p - 0.08f) / 0.8f);
                float orbit = 48f + sparkP * 120f;
                float angle = (360f / BossUnlockFxSparkCount) * i + p * 220f;
                spark.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * orbit,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * orbit);
                Color sparkColor = Color.Lerp(goldAccent, bossAccent, 0.5f);
                sparkColor.a = Mathf.Sin(sparkP * Mathf.PI) * 0.9f;
                float sparkScale = Mathf.Lerp(0.4f, 1.4f, sparkP);
                spark.rectTransform.localScale = new Vector3(sparkScale, sparkScale, 1f);
                spark.color = sparkColor;
            }

            yield return null;
        }
    }

    private IEnumerator CoBossUnlockRevealBossButton(Color bossAccent, Color goldAccent)
    {
        if (battlePreviewBossTierButton == null)
            yield break;

        Transform bossTr = battlePreviewBossTierButton.transform;
        Image bossImg = battlePreviewBossTierButton.GetComponent<Image>();
        CanvasGroup bossCg = battlePreviewBossTierButton.GetComponent<CanvasGroup>();
        if (bossCg == null)
            bossCg = battlePreviewBossTierButton.gameObject.AddComponent<CanvasGroup>();

        bossTr.localScale = Vector3.zero;
        bossCg.alpha = 0f;
        Color imgFrom = bossImg != null ? bossImg.color : Color.white;

        const float popDuration = 0.55f;
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / popDuration);
            float scale = Mathf.Lerp(0f, 1.14f, EaseOutSoftBack(p));
            if (p > 0.72f)
                scale = Mathf.Lerp(1.14f, 1f, EaseInOutSmooth((p - 0.72f) / 0.28f));
            bossTr.localScale = new Vector3(scale, scale, 1f);
            bossCg.alpha = Mathf.Clamp01(p * 1.25f);

            if (bossImg != null)
            {
                Color tint = Color.Lerp(imgFrom, Color.Lerp(bossAccent, Color.white, 0.35f), EaseInOutSmooth(p));
                tint.a = 1f;
                bossImg.color = tint;
            }

            yield return null;
        }

        bossTr.localScale = Vector3.one;
        bossCg.alpha = 1f;
        if (bossImg != null)
            bossImg.color = Color.white;

        const float glowDuration = 0.28f;
        elapsed = 0f;
        while (elapsed < glowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / glowDuration);
            float pulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.06f;
            bossTr.localScale = new Vector3(pulse, pulse, 1f);
            if (bossImg != null)
            {
                Color glow = Color.Lerp(Color.white, goldAccent, Mathf.Sin(p * Mathf.PI) * 0.35f);
                glow.a = 1f;
                bossImg.color = glow;
            }

            yield return null;
        }

        bossTr.localScale = Vector3.one;
        if (bossImg != null)
            bossImg.color = Color.white;
    }

    private IEnumerator CoBossUnlockPuzzleTextPunch()
    {
        if (battlePreviewAuthoredPuzzleTitleText == null)
            yield break;

        RectTransform titleRt = battlePreviewAuthoredPuzzleTitleText.rectTransform;
        Vector3 baseScale = titleRt.localScale;
        Color baseColor = battlePreviewAuthoredPuzzleTitleText.color;
        Color bossAccent = GetDifficultyTierAccentColor(battlePreviewAuthoredRevealTier);

        const float duration = 0.42f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(0.82f, 1.08f, EaseOutSoftBack(p));
            if (p > 0.65f)
                scale = Mathf.Lerp(1.08f, 1f, EaseInOutSmooth((p - 0.65f) / 0.35f));
            titleRt.localScale = baseScale * scale;
            battlePreviewAuthoredPuzzleTitleText.color = Color.Lerp(baseColor, bossAccent, Mathf.Sin(p * Mathf.PI) * 0.45f);
            yield return null;
        }

        titleRt.localScale = baseScale;
        battlePreviewAuthoredPuzzleTitleText.color = baseColor;

        if (battlePreviewAuthoredPuzzleHintText != null)
        {
            RectTransform hintRt = battlePreviewAuthoredPuzzleHintText.rectTransform;
            CanvasGroup hintCg = battlePreviewAuthoredPuzzleHintText.GetComponent<CanvasGroup>();
            if (hintCg == null)
                hintCg = battlePreviewAuthoredPuzzleHintText.gameObject.AddComponent<CanvasGroup>();
            hintCg.alpha = 0f;
            elapsed = 0f;
            const float hintFade = 0.24f;
            while (elapsed < hintFade)
            {
                elapsed += Time.unscaledDeltaTime;
                hintCg.alpha = EaseInOutSmooth(Mathf.Clamp01(elapsed / hintFade));
                yield return null;
            }

            hintCg.alpha = 1f;
            hintRt.localScale = Vector3.one;
        }
    }

    private Sprite ResolveBossUnlockFxUiSprite()
    {
        if (runtimeRoundedUiSprite != null)
            return runtimeRoundedUiSprite;
        if (_bossUnlockFxUiSprite != null)
            return _bossUnlockFxUiSprite;
        Texture2D tex = Texture2D.whiteTexture;
        _bossUnlockFxUiSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        return _bossUnlockFxUiSprite;
    }

    private Image CreateBossUnlockFxImage(Transform parent, string name, Color baseColor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image img = obj.GetComponent<Image>();
        SetupBossUnlockFxImage(img, baseColor);
        img.raycastTarget = false;
        return img;
    }

    private void SetupBossUnlockFxImage(Image img, Color baseColor)
    {
        Sprite sprite = ResolveBossUnlockFxUiSprite();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = sprite == runtimeRoundedUiSprite ? Image.Type.Sliced : Image.Type.Simple;
        }

        img.color = baseColor;
    }

    private static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

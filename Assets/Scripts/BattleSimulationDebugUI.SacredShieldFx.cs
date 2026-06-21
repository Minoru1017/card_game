using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private void BuildPlayerHeroSacredShieldVisual(Transform heroHudRoot, float hpNumSize, float titleSize)
    {
        if (heroHudRoot == null) return;

        Transform existing = heroHudRoot.Find("SacredShieldHeroRoot");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject root = new GameObject("SacredShieldHeroRoot", typeof(RectTransform));
        root.transform.SetParent(heroHudRoot, false);
        root.transform.SetAsFirstSibling();

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.zero;
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        float shieldSize = hpNumSize * 1.42f;
        rootRt.sizeDelta = new Vector2(shieldSize, shieldSize);
        rootRt.anchoredPosition = new Vector2(hpNumSize * 0.34f, hpNumSize * 0.38f + titleSize * 0.08f);

        Image outerRing = CreateShieldRingImage(root.transform, "ShieldOuter", 1f, BattleFxColors.SacredShieldHeroOuter);
        Image innerRing = CreateShieldRingImage(root.transform, "ShieldInner", 0.78f, BattleFxColors.SacredShieldHeroInner);
        Image coreGlow = CreateShieldRingImage(root.transform, "ShieldCore", 0.52f, BattleFxColors.SacredShieldHeroCore);

        CanvasGroup outerCg = outerRing.gameObject.AddComponent<CanvasGroup>();
        CanvasGroup innerCg = innerRing.gameObject.AddComponent<CanvasGroup>();
        CanvasGroup coreCg = coreGlow.gameObject.AddComponent<CanvasGroup>();

        playerHeroSacredShieldVisual = root.AddComponent<SacredShieldHeroHudVisual>();
        playerHeroSacredShieldVisual.Initialize(rootRt, outerRing.rectTransform, outerCg, innerCg, coreCg, coreGlow);
        playerHeroSacredShieldVisual.SyncActive(false);
    }

    private static Image CreateShieldRingImage(Transform parent, string name, float scale, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220f, 220f);
        rt.localScale = Vector3.one * scale;
        Image img = go.GetComponent<Image>();
        img.sprite = CreateShieldRingSprite();
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        img.color = color;
        return img;
    }

    private static Sprite shieldRingSprite;

    private static Sprite CreateShieldRingSprite()
    {
        if (shieldRingSprite != null) return shieldRingSprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) * 0.5f;
        float outerHalf = size * 0.46f;
        float innerHalf = size * 0.34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float ax = Mathf.Abs(x - center);
                float ay = Mathf.Abs(y - center);
                bool inOuter = ax <= outerHalf && ay <= outerHalf;
                bool inInner = ax <= innerHalf && ay <= innerHalf;
                tex.SetPixel(x, y, inOuter && !inInner ? Color.white : Color.clear);
            }
        }

        tex.Apply(false, true);
        shieldRingSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return shieldRingSprite;
    }

    private void RefreshPlayerHeroSacredShieldFx()
    {
        if (battleManager == null || playerHeroSacredShieldVisual == null) return;

        int sid = battleManager.GetBattleSessionId();
        if (sid != lastSacredShieldBattleSessionId)
        {
            lastSacredShieldBattleSessionId = sid;
            playerHeroSacredShieldVisual.ResetVisual();
        }

        if (BattleAutoSimPlugin.IsRunning)
        {
            playerHeroSacredShieldVisual.SyncActive(battleManager.HasPlayerHeroShieldRemaining);
            return;
        }

        playerHeroSacredShieldVisual.SyncActive(battleManager.HasPlayerHeroShieldRemaining);
    }

    private void OnPlayerHeroShieldConsumed()
    {
        if (BattleAutoSimPlugin.IsRunning) return;
        if (playerHeroSacredShieldVisual != null)
            playerHeroSacredShieldVisual.PlayBreak();
    }

    private sealed class SacredShieldHeroHudVisual : MonoBehaviour
    {
        private RectTransform rootRt;
        private RectTransform rotateRt;
        private CanvasGroup outerCg;
        private CanvasGroup innerCg;
        private CanvasGroup coreCg;
        private Image coreImg;
        private bool visible;
        private bool breaking;
        private float enabledUnscaledTime;
        private Coroutine breakRoutine;

        public void Initialize(
            RectTransform root,
            RectTransform rotateOuter,
            CanvasGroup outer,
            CanvasGroup inner,
            CanvasGroup core,
            Image coreImage)
        {
            rootRt = root;
            rotateRt = rotateOuter;
            outerCg = outer;
            innerCg = inner;
            coreCg = core;
            coreImg = coreImage;
        }

        public void ResetVisual()
        {
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }

            breaking = false;
            visible = false;
            if (rootRt != null)
                rootRt.gameObject.SetActive(false);
        }

        public void SyncActive(bool active)
        {
            if (breaking) return;
            if (active == visible) return;

            visible = active;
            if (rootRt != null)
                rootRt.gameObject.SetActive(active);
            if (active)
                enabledUnscaledTime = Time.unscaledTime;
        }

        public void PlayBreak()
        {
            if (!visible || breaking) return;
            breaking = true;
            visible = false;
            if (breakRoutine != null)
                StopCoroutine(breakRoutine);
            breakRoutine = StartCoroutine(CoBreak());
        }

        private void Update()
        {
            if (!visible || breaking || rootRt == null || !rootRt.gameObject.activeSelf) return;

            float t = Time.unscaledTime;
            float fadeIn = Mathf.Clamp01((t - enabledUnscaledTime) / 0.35f);

            if (rotateRt != null)
                rotateRt.Rotate(0f, 0f, -14f * Time.unscaledDeltaTime);

            float phase = Mathf.Sin(t * 2.2f) * 0.5f + 0.5f;
            if (outerCg != null)
                outerCg.alpha = Mathf.Lerp(0.22f, 0.58f, phase) * fadeIn;
            if (innerCg != null)
                innerCg.alpha = Mathf.Lerp(0.18f, 0.48f, 1f - phase) * fadeIn;
            if (coreCg != null)
                coreCg.alpha = (0.12f + 0.1f * Mathf.Sin(t * 3.6f)) * fadeIn;

            float pulse = 1f + 0.028f * Mathf.Sin(t * 3.1f);
            rootRt.localScale = new Vector3(pulse, pulse, 1f);
        }

        private IEnumerator CoBreak()
        {
            const float duration = 0.52f;
            float start = Time.unscaledTime;
            Vector3 startScale = rootRt != null ? rootRt.localScale : Vector3.one;

            while (Time.unscaledTime - start < duration)
            {
                float p = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                float flash = p < 0.18f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.18f) / 0.82f);
                if (outerCg != null)
                    outerCg.alpha = Mathf.Lerp(0.55f, 0f, p);
                if (innerCg != null)
                    innerCg.alpha = Mathf.Lerp(0.42f, 0f, p);
                if (coreCg != null)
                    coreCg.alpha = Mathf.Lerp(0.35f, 0f, p);
                if (coreImg != null)
                {
                    coreImg.color = Color.Lerp(
                        BattleFxColors.SacredShieldHeroCore,
                        BattleFxColors.SacredShieldHeroBreakFlash,
                        flash);
                }
                if (rootRt != null)
                    rootRt.localScale = startScale * (1f + 0.12f * Mathf.Sin(p * Mathf.PI));
                yield return null;
            }

            ResetVisual();
            breakRoutine = null;
        }
    }
}

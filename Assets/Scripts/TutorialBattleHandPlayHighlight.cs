using UnityEngine;
using UnityEngine.UI;

/// <summary>教學戰手牌「建議打出」醒目外框（脈動金色光暈）。</summary>
[DisallowMultipleComponent]
public sealed class TutorialBattleHandPlayHighlight : MonoBehaviour
{
    private const float PulseSpeed = 5.5f;
    private const float ScalePulseAmount = 0.06f;
    private static readonly Color GlowGold = new Color(0.98f, 0.86f, 0.42f, 0.95f);
    private static readonly Color GlowGoldDim = new Color(0.98f, 0.86f, 0.42f, 0.45f);
    private static readonly Color RimDark = new Color(0.12f, 0.08f, 0.06f, 0.85f);

    private static Sprite whiteSprite;

    private RectTransform cardRect;
    private RectTransform glowRt;
    private Image glowImage;
    private Outline outlineGold;
    private Outline outlineDark;
    private Vector3 restScale = Vector3.one;
    private float restY;
    private bool highlighted;

    public void SetHighlighted(bool on)
    {
        if (on == highlighted) return;
        highlighted = on;
        EnsureVisuals();
        if (glowRt != null) glowRt.gameObject.SetActive(on);
        enabled = on;

        if (cardRect == null) cardRect = transform as RectTransform;
        if (cardRect == null) return;

        if (on)
        {
            restScale = cardRect.localScale;
            restY = cardRect.anchoredPosition.y;
            cardRect.SetAsLastSibling();
            cardRect.anchoredPosition = new Vector2(cardRect.anchoredPosition.x, restY + 14f);
        }
        else
        {
            cardRect.localScale = restScale;
            cardRect.anchoredPosition = new Vector2(cardRect.anchoredPosition.x, restY);
        }
    }

    private void Update()
    {
        if (!highlighted || cardRect == null) return;

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
        float scaleMul = 1f + ScalePulseAmount * wave;
        cardRect.localScale = restScale * scaleMul;

        if (outlineGold != null)
            outlineGold.effectColor = Color.Lerp(GlowGoldDim, GlowGold, wave);

        if (glowImage != null)
            glowImage.color = new Color(GlowGold.r, GlowGold.g, GlowGold.b, 0.22f + 0.2f * wave);
    }

    private void EnsureVisuals()
    {
        if (glowRt != null) return;

        cardRect = transform as RectTransform;
        GameObject glowObj = new GameObject("TutorialPlayHintGlow", typeof(RectTransform), typeof(Image));
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.SetAsFirstSibling();
        glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.offsetMin = new Vector2(-14f, -18f);
        glowRt.offsetMax = new Vector2(14f, 22f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);

        glowImage = glowObj.GetComponent<Image>();
        glowImage.sprite = GetWhiteSprite();
        glowImage.type = Image.Type.Sliced;
        glowImage.color = new Color(GlowGold.r, GlowGold.g, GlowGold.b, 0.32f);
        glowImage.raycastTarget = false;

        outlineDark = glowObj.AddComponent<Outline>();
        outlineDark.effectColor = RimDark;
        outlineDark.effectDistance = new Vector2(3f, -3f);

        outlineGold = glowObj.AddComponent<Outline>();
        outlineGold.effectColor = GlowGold;
        outlineGold.effectDistance = new Vector2(5f, -5f);
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}

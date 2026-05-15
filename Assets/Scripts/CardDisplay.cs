using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI effectText;

    public Image backgroundImage;
    public Card card;

    void Start()
    {
        
    }

    public void SetCard(Card c)
    {
        card = c;
        if (card == null)
        {
            Debug.LogError("CardDisplay.SetCard: card is null");
            return;
        }
        ShowCard();
    }


    public void ShowCard()
    {
        if (card == null) return;
        if (nameText != null) nameText.text = card.cardName;

        Sprite portrait = ResolvePortraitSpriteForCurrentContext();
        if (portrait != null)
            ApplyArtworkSprite(portrait);

        // ?????????}?]??????~???^
        if (attackText != null) attackText.gameObject.SetActive(true);
        if (healthText != null) healthText.gameObject.SetActive(true);
        if (effectText != null) effectText.gameObject.SetActive(true);

        if (card is MonsterCard monster)
        {
            if (attackText != null) attackText.text = monster.attack.ToString();
            if (healthText != null)
            {
                if (monster.healthPoint > monster.healthPointMax)
                {
                    int ov = monster.healthPoint - monster.healthPointMax;
                    healthText.overflowMode = TMPro.TextOverflowModes.Overflow;
                    healthText.richText = true;
                    healthText.text =
                        "<color=#FFFFFF>" + monster.healthPointMax + "</color> <color=#66FF99>+" + ov + "</color>";
                    healthText.color = Color.white;
                }
                else
                {
                    healthText.richText = false;
                    healthText.text = monster.healthPoint.ToString();
                }
            }
            if (effectText != null) effectText.gameObject.SetActive(false);
        }
        else if (card is SpellCard spell)
        {
            if (effectText != null) effectText.text = spell.effect;
            if (attackText != null) attackText.gameObject.SetActive(false);
            if (healthText != null) healthText.gameObject.SetActive(false);
        }
    }

    private static ClickCard ResolveClickCardInContext(MonoBehaviour host)
    {
        if (host == null) return null;
        ClickCard onSelf = host.GetComponent<ClickCard>();
        if (onSelf != null) return onSelf;
        return host.GetComponentInParent<ClickCard>();
    }

    private Sprite ResolvePortraitSpriteForCurrentContext()
    {
        if (card == null) return null;
        ClickCard clickCard = ResolveClickCardInContext(this);
        bool isDeckCard = clickCard != null && clickCard.state == CardState.Deck;
        if (isDeckCard)
            return card.ResolveDeckThumbSprite();

        bool isLibraryCard = clickCard != null && clickCard.state == CardState.Library;
        // Buildbeck「Library Grid Viewport」館藏格：與牌組區一致使用 DeckThumb；其餘場景館藏仍用本體立繪。
        if (isLibraryCard && IsBuildbeckLibraryGridContext())
        {
            Sprite thumb = card.ResolveDeckThumbSprite();
            if (thumb != null) return thumb;
        }

        return card.ResolveCardArtSprite();
    }

    private static bool IsBuildbeckLibraryGridContext()
    {
        Scene s = SceneManager.GetActiveScene();
        return s.IsValid() && s.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase);
    }

    private Image ResolveArtworkTargetImage()
    {
        if (backgroundImage != null) return backgroundImage;

        // Prefer common art node names used by current card prefabs.
        string[] preferredNames = { "Role", "BGImage", "Artwork", "Art", "Card Art" };
        for (int i = 0; i < preferredNames.Length; i++)
        {
            Transform t = FindDeepChildByName(transform, preferredNames[i]);
            if (t == null) continue;
            Image namedImg = t.GetComponent<Image>();
            if (namedImg != null)
            {
                backgroundImage = namedImg;
                return backgroundImage;
            }
        }

        // Fallback: first child Image in this card object hierarchy.
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            if (images[i].transform == transform) continue;
            backgroundImage = images[i];
            return backgroundImage;
        }

        return null;
    }

    private void ApplyArtworkSprite(Sprite sprite)
    {
        if (sprite == null) return;
        ClickCard clickCard = ResolveClickCardInContext(this);
        bool isLibraryCard = clickCard != null && clickCard.state == CardState.Library;
        bool isDeckCard = clickCard != null && clickCard.state == CardState.Deck;

        // 組建牌組區：僅 DeckThumb，套在 DeckCardInfoStrip mt 的 Art 上。
        if (isDeckCard)
        {
            Image dfArt = FindDeckStripShellArtImage(DeckStripMtDfName);
            Image oiArt = FindDeckStripShellArtImage(DeckStripMtOiName);

            if (dfArt != null || oiArt != null)
            {
                ApplySpriteToDeckStripArtTarget(dfArt, sprite);
                ApplySpriteToDeckStripArtTarget(oiArt, sprite);
                backgroundImage = dfArt != null ? dfArt : oiArt;
                return;
            }

            Image stripArt = FindNamedImage("Art");
            if (stripArt != null && IsUnderDeckCardInfoStrip(stripArt.transform))
            {
                ApplySpriteToDeckStripArtTarget(stripArt, sprite);
                backgroundImage = stripArt;
            }

            return;
        }

        // 館藏：Buildbeck Grid 上為 DeckThumb（與 ResolvePortrait 一致）；DeckGen 橢圓槽位 + 可選 Card Art 層。
        if (isLibraryCard)
        {
            Image libraryCardArt = FindNamedImage("Card Art");
            if (libraryCardArt != null)
            {
                ApplyCardPresetArtSprite(libraryCardArt, sprite);
                backgroundImage = libraryCardArt;
            }

            Image dfArt = FindDeckGenLibraryShellArtImage("DeckGen_Library_df");
            Image oiArt = FindDeckGenLibraryShellArtImage("DeckGen_Library_oi");
            if (oiArt == null) oiArt = FindDeckGenLibraryShellArtImage("DeckGen_Library_ol");

            if (dfArt != null || oiArt != null)
            {
                ApplySpriteToLibraryArtTarget(dfArt, sprite);
                ApplySpriteToLibraryArtTarget(oiArt, sprite);
                if (backgroundImage == null)
                    backgroundImage = dfArt != null ? dfArt : oiArt;
                return;
            }

            if (backgroundImage != null) return;

            Image libraryTarget = FindNamedImage("BGImage");
            if (libraryTarget == null)
            {
                Transform libDf = FindDeepChildByName(transform, "DeckGen_Library_df");
                if (libDf != null) libraryTarget = libDf.GetComponent<Image>();
            }

            if (libraryTarget != null)
            {
                ApplyCardPresetArtSprite(libraryTarget, sprite);
                backgroundImage = libraryTarget;
                return;
            }
        }

        // Try commonly used art layers first.
        Image nbBg = FindNamedImage("NB_BG");
        Image role = FindNamedImage("Role");
        Image bgImage = FindNamedImage("BGImage");
        Image art = FindNamedImage("Art");
        Image artwork = FindNamedImage("Artwork");
        Image cardArt = FindNamedImage("Card Art");

        // Prefer an already-visible layer to avoid layout side effects.
        Image target = PickFirstVisible(nbBg, role, bgImage, art, artwork, cardArt);
        if (target == null) target = PickFirstNonNull(nbBg, role, bgImage, art, artwork, cardArt, ResolveArtworkTargetImage());
        if (target == null) return;

        if (IsBattleSimulationPortraitContext())
        {
            ApplyBattlePortraitSprite(target, sprite);
            backgroundImage = target;
            return;
        }

        ApplyCardPresetArtSprite(target, sprite);
        backgroundImage = target;
    }

    private static bool IsBattleSimulationPortraitContext()
    {
        return SceneManager.GetActiveScene().name == "BattleSimulation";
    }

    /// <summary>對戰手牌／場上：只換圖、等比縮放，不改卡框比例。</summary>
    private static void ApplyBattlePortraitSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = true;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }

    /// <summary>本體立繪：背包／詳情等用 337×491。</summary>
    private void ApplyCardPresetArtSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;

        RectTransform rt = target.rectTransform;
        if (rt != null && IsCardPresetArtLayer(target.transform))
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = CardArtLayoutSpec.PresetArtSize;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = false;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }

    private static bool IsCardPresetArtLayer(Transform t)
    {
        if (t == null) return false;
        string n = t.name;
        return n == "Role" || n == "Card Art" || n == "Artwork";
    }

    private static Image PickFirstVisible(params Image[] images)
    {
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null) continue;
            if (!img.gameObject.activeSelf) continue;
            if (img.color.a <= 0.001f) continue;
            return img;
        }
        return null;
    }

    private static Image PickFirstNonNull(params Image[] images)
    {
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null) return images[i];
        }
        return null;
    }

    private Image FindNamedImage(string name)
    {
        Transform t = FindDeepChildByName(transform, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private const string LibraryDeckGenArtChildName = "Art";
    private const string DeckStripMtDfName = "DeckCardInfoStrip_mt_df";
    private const string DeckStripMtOiName = "DeckCardInfoStrip_mt_oi";

    /// <summary>在 <c>DeckCardInfoStrip_mt_df</c>／<c>mt_oi</c> 子階層的 <c>Art</c> 上解析縮圖用 <see cref="Image"/>。</summary>
    private Image FindDeckStripShellArtImage(string shellExactName)
    {
        Transform shell = FindDeepChildByName(transform, shellExactName);
        if (shell == null) return null;
        Transform art = shell.Find(LibraryDeckGenArtChildName);
        if (art == null) art = FindDeepChildByName(shell, LibraryDeckGenArtChildName);
        if (art == null) return null;
        Image onArt = art.GetComponent<Image>();
        if (onArt != null) return onArt;
        return art.GetComponentInChildren<Image>(true);
    }

    private static bool IsUnderDeckCardInfoStrip(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            if (p.name == "DeckCardInfoStrip") return true;
        }

        return false;
    }

    private static void ApplySpriteToDeckStripArtTarget(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        for (Transform t = target.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            string n = t.name;
            if (n == DeckStripMtDfName || n == DeckStripMtOiName || n == "DeckCardInfoStrip")
                break;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = false;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
    }

    /// <summary>在 <c>DeckGen_Library_df</c>／<c>DeckGen_Library_oi</c> 子階層的 <c>Art</c> 上解析立繪用 <see cref="Image"/>（Art 本體或子物件皆可）。</summary>
    private Image FindDeckGenLibraryShellArtImage(string shellExactName)
    {
        Transform shell = FindDeepChildByName(transform, shellExactName);
        if (shell == null) return null;
        Transform art = shell.Find(LibraryDeckGenArtChildName);
        if (art == null) art = FindDeepChildByName(shell, LibraryDeckGenArtChildName);
        if (art == null) return null;
        Image onArt = art.GetComponent<Image>();
        if (onArt != null) return onArt;
        return art.GetComponentInChildren<Image>(true);
    }

    private static void ApplySpriteToLibraryArtTarget(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        for (Transform t = target.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            string n = t.name;
            if (n == "DeckGen_Library_df" || n == "DeckGen_Library_oi" || n == "DeckGen_Library_ol")
                break;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.preserveAspect = false;
    }

    private static Transform FindDeepChildByName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName)) return null;
        if (root.name == exactName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChildByName(root.GetChild(i), exactName);
            if (found != null) return found;
        }
        return null;
    }
}

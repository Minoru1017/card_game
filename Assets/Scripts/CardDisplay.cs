using UnityEngine;
using UnityEngine.UI;
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

        if (card.artworkSprite != null)
            ApplyArtworkSprite(card.artworkSprite);

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
        ClickCard clickCard = GetComponentInParent<ClickCard>();
        bool isLibraryCard = clickCard != null && clickCard.state == CardState.Library;
        bool isDeckCard = clickCard != null && clickCard.state == CardState.Deck;

        // User-requested behavior: Deck area keeps original style (no per-card artwork).
        if (isDeckCard) return;

        // User-requested behavior: Library thumbnail should use the green oval layer.
        if (isLibraryCard)
        {
            // Backpack card prefab uses "Card Art"; legacy library card uses "BGImage".
            Image libraryTarget = FindNamedImage("Card Art");
            if (libraryTarget == null) libraryTarget = FindNamedImage("BGImage");
            if (libraryTarget != null)
            {
                libraryTarget.sprite = sprite;
                libraryTarget.color = Color.white;
                if (!libraryTarget.gameObject.activeSelf) libraryTarget.gameObject.SetActive(true);
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

        target.sprite = sprite;
        target.color = Color.white;
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
        backgroundImage = target;
    }

    private Image FindNamedImage(string name)
    {
        Transform t = FindDeepChildByName(transform, name);
        return t != null ? t.GetComponent<Image>() : null;
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

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
}

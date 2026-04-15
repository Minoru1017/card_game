using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStore : MonoBehaviour
{
    public TextAsset cardData;
    public List<Card> cardList = new List<Card>();
    public Card GetCardById(int id) => cardList.Find(c => c.id == id);

    void Start()
    {
        LoadCardData();
    }

    void Update()
    {
    }

    public void LoadCardData()
    {
        cardList.Clear();
        if (cardData == null)
        {
            Debug.LogError("CardStore.LoadCardData: cardData is not assigned.");
            return;
        }

        string[] dataRow = cardData.text.Split('\n');
        foreach (var row in dataRow)
        {
            string[] rowArray = row.Split(',');
            if (rowArray.Length == 0 || string.IsNullOrWhiteSpace(rowArray[0])) continue;
            if (rowArray[0] == "#")
            {
                continue;
            }
            else if (rowArray[0] == "monster")
            {
                // monster,id,nameZh,nameEn,atk,hp  (legacy: monster,id,nameZh,atk,hp)
                if (rowArray.Length < 5) continue;
                int id = int.Parse(rowArray[1].Trim());
                string name = rowArray[2].Trim();
                string nameEn = string.Empty;
                int atk;
                int health;
                if (rowArray.Length >= 6 && int.TryParse(rowArray[4].Trim(), out atk) && int.TryParse(rowArray[5].Trim(), out health))
                {
                    nameEn = rowArray[3].Trim();
                }
                else
                {
                    atk = int.Parse(rowArray[3].Trim());
                    health = int.Parse(rowArray[4].Trim());
                }

                MonsterCard monsterCard = new MonsterCard(id, name, atk, health);
                monsterCard.cardNameEnglish = nameEn;
                cardList.Add(monsterCard);
            }
            else if (rowArray[0] == "spell")
            {
                // spell,id,nameZh,nameEn,effect  (legacy: spell,id,nameZh,effect) — id is ordinal (000,001,…)
                if (rowArray.Length < 4) continue;
                int spellOrdinal = int.Parse(rowArray[1].Trim());
                string name = rowArray[2].Trim();
                string nameEn = string.Empty;
                string effect;
                if (rowArray.Length >= 5)
                {
                    nameEn = rowArray[3].Trim();
                    effect = rowArray[4].Trim();
                }
                else
                {
                    effect = rowArray[3].Trim();
                }

                SpellCard spellCard = new SpellCard(spellOrdinal, name, effect);
                spellCard.cardNameEnglish = nameEn;
                cardList.Add(spellCard);
            }
        }
    }

    public void TestLoad()
    {
        foreach (var item in cardList)
        {
            Debug.Log("card:" + item.id.ToString() + item.cardName);
        }
    }

    public Card RandomCard()
    {
        if (cardList == null || cardList.Count == 0)
        {
            Debug.LogError("CardStore.RandomCard: cardList is empty.");
            return null;
        }
        int index = Random.Range(0, cardList.Count);
        return cardList[index];
    }
}

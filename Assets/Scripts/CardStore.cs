using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardStore : MonoBehaviour
{
    [System.Serializable]
    public class CardArtworkOverride
    {
        public int id;
        [Tooltip("Optional resource path under Assets/Resources, e.g. CardArt/monster_001")]
        public string artworkResourcePath;
        public Sprite artworkSprite;
    }

    public TextAsset cardData;
    [Header("Optional per-card artwork overrides")]
    public List<CardArtworkOverride> artworkOverrides = new List<CardArtworkOverride>();
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
                // monster,id,nameZh,nameEn,atk,hp,artResourcePath  (legacy: monster,id,nameZh,atk,hp)
                if (rowArray.Length < 5) continue;
                int id = int.Parse(rowArray[1].Trim());
                string name = rowArray[2].Trim();
                string nameEn = string.Empty;
                int atk;
                int health;
                string artPath = string.Empty;
                if (rowArray.Length >= 6 && int.TryParse(rowArray[4].Trim(), out atk) && int.TryParse(rowArray[5].Trim(), out health))
                {
                    nameEn = rowArray[3].Trim();
                    if (rowArray.Length >= 7) artPath = rowArray[6].Trim();
                }
                else
                {
                    atk = int.Parse(rowArray[3].Trim());
                    health = int.Parse(rowArray[4].Trim());
                    if (rowArray.Length >= 6) artPath = rowArray[5].Trim();
                }

                MonsterCard monsterCard = new MonsterCard(id, name, atk, health);
                monsterCard.cardNameEnglish = nameEn;
                ApplyCardArtwork(monsterCard, artPath);
                cardList.Add(monsterCard);
            }
            else if (rowArray[0] == "spell")
            {
                // spell,id,nameZh,nameEn,effect,artResourcePath  (legacy: spell,id,nameZh,effect) — id is ordinal (000,001,…)
                if (rowArray.Length < 4) continue;
                int spellOrdinal = int.Parse(rowArray[1].Trim());
                string name = rowArray[2].Trim();
                string nameEn = string.Empty;
                string effect;
                string artPath = string.Empty;
                if (rowArray.Length >= 5)
                {
                    nameEn = rowArray[3].Trim();
                    effect = rowArray[4].Trim();
                    if (rowArray.Length >= 6) artPath = rowArray[5].Trim();
                }
                else
                {
                    effect = rowArray[3].Trim();
                }

                SpellCard spellCard = new SpellCard(spellOrdinal, name, effect);
                spellCard.cardNameEnglish = nameEn;
                ApplyCardArtwork(spellCard, artPath);
                cardList.Add(spellCard);
            }
        }
    }

    private void ApplyCardArtwork(Card card, string csvArtPath)
    {
        if (card == null) return;
        string path = string.IsNullOrWhiteSpace(csvArtPath) ? string.Empty : csvArtPath.Trim();
        Sprite sprite = LoadSpriteByPath(path);

        // Inspector override has higher priority and can replace CSV setting.
        for (int i = 0; i < artworkOverrides.Count; i++)
        {
            CardArtworkOverride entry = artworkOverrides[i];
            if (entry == null || entry.id != card.id) continue;
            if (!string.IsNullOrWhiteSpace(entry.artworkResourcePath))
            {
                path = entry.artworkResourcePath.Trim();
                sprite = LoadSpriteByPath(path);
            }
            if (entry.artworkSprite != null)
            {
                sprite = entry.artworkSprite;
            }
            break;
        }

        card.SetArtwork(path, sprite);
    }

    private static Sprite LoadSpriteByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Resources.Load<Sprite>(path.Trim());
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

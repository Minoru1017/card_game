public class Card
{
    public int id;
    public string cardName;
    /// <summary>Optional English name from CardList.csv; gameplay UI still uses <see cref="cardName"/>.</summary>
    public string cardNameEnglish = string.Empty;
    /// <summary>Optional per-card artwork resource path (under Assets/Resources).</summary>
    public string artworkResourcePath = string.Empty;
    /// <summary>Optional resolved card artwork sprite.</summary>
    public UnityEngine.Sprite artworkSprite;

    /// <summary>Name shown in battle simulation debug readouts (English when available).</summary>
    public string DebugDisplayName =>
        string.IsNullOrWhiteSpace(cardNameEnglish) ? cardName : cardNameEnglish;

    public Card(int _id, string _cardName) //?c?y???
    {
        this.id = _id;
        this.cardName = _cardName;
    }

    public void SetArtwork(string resourcePath, UnityEngine.Sprite sprite)
    {
        artworkResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath.Trim();
        artworkSprite = sprite;
    }
}

public class MonsterCard : Card
{
    public int attack;
    public int healthPoint; //???e????q
    public int healthPointMax;

    //?i?H?s?W????B???
    public MonsterCard(int _id, string _cardName, int _attack, int _healthPointMax) : base(_id, _cardName)
    {
        this.attack = _attack;
        this.healthPoint = _healthPointMax;
        this.healthPointMax = _healthPointMax;
    }
}

public class SpellCard: Card
{
    public string effect;

    /// <summary>CSV ordinal (000 �� 0). Distinct from monster ids.</summary>
    public int SpellOrdinal => DeckCardId.SpellOrdinalFromKey(id);

    public SpellCard(int spellOrdinal, string _cardName, string _effect)
        : base(DeckCardId.SpellKeyFromOrdinal(spellOrdinal), _cardName)
    {
        this.effect = _effect;
    }
}
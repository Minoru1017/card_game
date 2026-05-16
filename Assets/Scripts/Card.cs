/// <summary>CardList.csv 使用之稀有度（<c>N / R / SR / SSR / UR</c>）。</summary>
public enum CardRarity
{
    N = 0,
    R = 1,
    SR = 2,
    SSR = 3,
    UR = 4
}

public class Card
{
    public int id;
    public string cardName;
    /// <summary>CSV「稀有度」欄；舊表未標示時載入為 <see cref="CardRarity.N"/>。</summary>
    public CardRarity rarity = CardRarity.N;
    /// <summary>Optional English name from CardList.csv; gameplay UI still uses <see cref="cardName"/>.</summary>
    public string cardNameEnglish = string.Empty;
    /// <summary>卡牌本體立繪（對戰、背包詳情等）。Resources 路徑，可留空。</summary>
    public string artworkResourcePath = string.Empty;
    /// <summary>卡牌本體立繪 Sprite。</summary>
    public UnityEngine.Sprite artworkSprite;

    /// <summary>組建牌組／館藏縮圖（Buildbeck Library、DeckGen df/oi 的 Art 等）。</summary>
    public string deckThumbResourcePath = string.Empty;
    /// <summary>組建牌組／館藏縮圖 Sprite。</summary>
    public UnityEngine.Sprite deckThumbSprite;

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

    public void SetDeckThumb(string resourcePath, UnityEngine.Sprite sprite)
    {
        deckThumbResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath.Trim();
        deckThumbSprite = sprite;
    }

    /// <summary>組建牌組區用圖（<c>Assets/UI/DeckThumb/</c>）；未綁定時為 null。</summary>
    public UnityEngine.Sprite ResolveDeckThumbSprite() => deckThumbSprite;

    /// <summary>對戰／詳情用圖。</summary>
    public UnityEngine.Sprite ResolveCardArtSprite() => artworkSprite;
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
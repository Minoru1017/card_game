/// <summary>CardList.csv 使用之稀有度（<c>N / R / SR / SSR / UR</c>）。</summary>
public enum CardRarity
{
    N = 0,
    R = 1,
    SR = 2,
    SSR = 3,
    UR = 4
}

/// <summary>卡牌戰位（<c>CardList.csv</c> <c>combat_role</c> 欄；顯示名見 <see cref="CombatRoleUtility"/>）。</summary>
public enum CombatRole
{
    Strike = 0,
    Tank = 1,
    Support = 2,
    Finisher = 3
}

/// <summary>戰位 CSV 解析與 UI 顯示名（先鋒／守陣／策應／定式）。</summary>
public static class CombatRoleUtility
{
    public static bool TryParse(string token, out CombatRole role)
    {
        role = CombatRole.Strike;
        if (string.IsNullOrWhiteSpace(token)) return false;
        string u = token.Trim();
        if (u.Equals("Strike", System.StringComparison.OrdinalIgnoreCase)) { role = CombatRole.Strike; return true; }
        if (u.Equals("Tank", System.StringComparison.OrdinalIgnoreCase)) { role = CombatRole.Tank; return true; }
        if (u.Equals("Support", System.StringComparison.OrdinalIgnoreCase)) { role = CombatRole.Support; return true; }
        if (u.Equals("Finisher", System.StringComparison.OrdinalIgnoreCase)) { role = CombatRole.Finisher; return true; }
        return false;
    }

    public static string GetDisplayName(CombatRole role) => role switch
    {
        CombatRole.Strike => "先鋒",
        CombatRole.Tank => "守陣",
        CombatRole.Support => "策應",
        CombatRole.Finisher => "定式",
        _ => "先鋒"
    };
}

/// <summary>稀有度排序與 AI 加權（數值越大越優先保留／打出）。</summary>
public static class CardRarityUtility
{
    public static int GetRank(CardRarity rarity) => (int)rarity;

    /// <summary>出牌／留牌加權：UR 明顯高於低稀有同名級卡。</summary>
    public static int GetPlayAndKeepBonus(CardRarity rarity) => GetRank(rarity) * 25;
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
    /// <summary>戰位；CSV 未填時默認 <see cref="CombatRole.Strike"/>。</summary>
    public CombatRole combatRole = CombatRole.Strike;

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
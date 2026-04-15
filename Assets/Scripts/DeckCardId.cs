/// <summary>
/// Runtime deck/collection keys: monster uses CardList monster id (>= 0). Spell uses negative ids derived from CSV ordinal (000→0, …) so it never collides with any monster id.
/// </summary>
public static class DeckCardId
{
    public static bool IsSpellKey(int key) => key < 0;

    public static int SpellKeyFromOrdinal(int ordinal) => -1 - ordinal;

    public static int SpellOrdinalFromKey(int key)
    {
        if (key >= 0) return -1;
        return -key - 1;
    }

    /// <summary>Legacy unified int ids from older saves / CSV encodings.</summary>
    public static int NormalizeLegacyUnifiedId(int legacyId)
    {
        switch (legacyId)
        {
            case 100:
            case 23:
            case 30:
            case 4:
                return SpellKeyFromOrdinal(0);
            case 101:
            case 24:
            case 31:
            case 5:
                return SpellKeyFromOrdinal(1);
            case 102:
            case 25:
            case 32:
            case 6:
                return SpellKeyFromOrdinal(2);
            default:
                return legacyId;
        }
    }
}

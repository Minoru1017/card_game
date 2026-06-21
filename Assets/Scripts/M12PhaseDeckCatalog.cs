/// <summary>M-1-2 段考鎖定牌表（定案見 LEVEL_DESIGN_M-1-2.md §3.1／§3.2）。</summary>
public static class M12PhaseDeckCatalog
{
    /// <summary>階段 A：御三家應用 · 15 張。</summary>
    public static readonly int[] PhaseADeckCardIds =
    {
        13, 13,
        12, 12,
        4, 4, 4, 4,
        5, 5,
        22, 22,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(0)
    };

    /// <summary>階段 B：教會三張搭配 · 20 張 · 港灣簡單。</summary>
    public static readonly int[] PhaseBDeckCardIds =
    {
        17, 17,
        14, 14,
        7,
        22, 22, 22,
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        DeckCardId.SpellKeyFromOrdinal(1),
        13,
        12,
        4, 4,
        5, 5,
        6,
        DeckCardId.SpellKeyFromOrdinal(0)
    };
}

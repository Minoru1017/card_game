/// <summary>鬥鳥加成可指定的開局天氣（避免外部依賴 BattleSimulationManager 的私有列舉）。</summary>
public enum BirdDuelOpeningWeather
{
    None,
    Gale,
    Fog
}

/// <summary>鬥鳥加成聚合後的開局效果，由 <c>BattleSimulationManager.StartBattle</c> 快照並套用。</summary>
public struct BirdDuelBonusEffects
{
    public int PlayerHpDelta;
    public int PlayerHpAbsolute;
    public int OpeningExtraDraw;
    public int PlayerExtraDrawPerTurn;
    public int EnemyHpDelta;
    public int EnemyExtraOpeningDraw;
    public float EnemyDamageMultiplier;
    public bool UnlockPlayerOpeningAttack;
    public int RevealEnemyHandCount;
    public BirdDuelOpeningWeather OpeningWeather;
    public bool PlayerHeroShieldActive;
    public int PlayerRarityDrawMaxRound;
}

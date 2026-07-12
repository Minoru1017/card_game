/// <summary>M-1-3 鎖定牌組：A 段教會線、B 段入門 30。</summary>
public static class M13PhaseDeckApplicator
{
    public static void ApplyPhaseADeck(PlayerData playerData = null) =>
        M12PhaseDeckApplicator.ApplyPhaseBDeck(playerData);

    public static void ApplyPhaseBDeck(PlayerData playerData = null) =>
        TutorialDeckApplicator.ApplyToActivePlayerDeck(playerData);
}

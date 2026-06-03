/// <summary>單方主教·祝聖預留／宗教連攜／聖療連攜 對局狀態。</summary>
public struct BishopConsecrationBattleState
{
    public bool reserveGrantedThisBattle;
    /// <summary>玩家：已祝聖預留，尚未選擇綁主教或下一張場怪。</summary>
    public bool awaitingPlayerBindChoice;
    public bool awaitingNextSummon;
    public bool awaitingFirstHit;
    public bool religiousSynergy;
    public bool holyTherapyLinkOnNun;
    public bool holyTherapyHealBonusUsed;

    public void Reset()
    {
        reserveGrantedThisBattle = false;
        awaitingPlayerBindChoice = false;
        awaitingNextSummon = false;
        awaitingFirstHit = false;
        religiousSynergy = false;
        holyTherapyLinkOnNun = false;
        holyTherapyHealBonusUsed = false;
    }
}

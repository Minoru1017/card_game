/// <summary>
/// M-1-2 段考：記錄本局是否已觸發御三家戰技（對局開始呼叫 Reset，結束呼叫 QuerySatisfied）。
/// 階段 A 關卡目標僅計我方場上怪獸／牌組觸發，不含敵方。
/// </summary>
public static class M12TrioMasteryBattleTracker
{
    private static bool militiaFormationTriggered;
    private static bool queenShelterTriggered;
    private static bool kingDecreeTriggered;

    public static void ResetForNewBattle()
    {
        militiaFormationTriggered = false;
        queenShelterTriggered = false;
        kingDecreeTriggered = false;
    }

    public static void NotifyMilitiaFormationTriggered(bool isPlayerSide)
    {
        if (ShouldCountSkillTrigger(isPlayerSide))
            militiaFormationTriggered = true;
    }

    public static void NotifyQueenShelterTriggered(bool isPlayerSide)
    {
        if (ShouldCountSkillTrigger(isPlayerSide))
            queenShelterTriggered = true;
    }

    public static void NotifyKingDecreeTriggered(bool isPlayerSide)
    {
        if (ShouldCountSkillTrigger(isPlayerSide))
            kingDecreeTriggered = true;
    }

    private static bool ShouldCountSkillTrigger(bool isPlayerSide)
    {
        if (!BattleLaunchContext.IsM12TrioMasteryBattle)
            return false;
        if (BattleLaunchContext.IsM12TrioTutorialBattle)
            return isPlayerSide;
        return true;
    }

    public static bool QueryMilitiaTriggered() => militiaFormationTriggered;

    public static bool QueryQueenTriggered() => queenShelterTriggered;

    public static bool QueryKingTriggered() => kingDecreeTriggered;

    public static bool QueryAllTrioSkillsTriggered() =>
        militiaFormationTriggered && queenShelterTriggered && kingDecreeTriggered;

    public static string BuildMissingSkillHint()
    {
        if (QueryAllTrioSkillsTriggered())
            return string.Empty;
        if (!militiaFormationTriggered)
            return "段考目標：請讓民兵·列陣觸發至少 1 次。";
        if (!queenShelterTriggered)
            return "段考目標：請讓王后·王室庇護觸發至少 1 次。";
        if (!kingDecreeTriggered)
            return "段考目標：請讓國王·庭訓號令觸發至少 1 次。";
        return "段考目標：御三家戰技尚需觸發。";
    }
}

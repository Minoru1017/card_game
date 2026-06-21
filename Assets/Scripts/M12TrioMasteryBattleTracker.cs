/// <summary>
/// M-1-2 段考：記錄本局是否已觸發御三家戰技（對局開始呼叫 Reset，結束呼叫 QuerySatisfied）。
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

    public static void NotifyMilitiaFormationTriggered() => militiaFormationTriggered = true;

    public static void NotifyQueenShelterTriggered() => queenShelterTriggered = true;

    public static void NotifyKingDecreeTriggered() => kingDecreeTriggered = true;

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

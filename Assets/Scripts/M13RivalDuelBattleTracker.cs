/// <summary>M-1-3 分波對決任務追蹤：單回合直擊 ≥8、祝聖→修女→初級治療。</summary>
public static class M13RivalDuelBattleTracker
{
    private static int currentPlayerTurnHeroDamage;
    private static bool singleTurnHeroDamageAtLeastEight;
    private static bool consecrationBoundToNun;
    private static bool holyTherapyChainComplete;
    private static bool hotBloodReversalTriggered;

    public static void ResetForNewBattle()
    {
        currentPlayerTurnHeroDamage = 0;
        singleTurnHeroDamageAtLeastEight = false;
        consecrationBoundToNun = false;
        holyTherapyChainComplete = false;
        hotBloodReversalTriggered = false;
    }

    public static void NotifyPlayerTurnHeroDamage(int damage)
    {
        if (damage <= 0)
            return;

        currentPlayerTurnHeroDamage += damage;
        if (currentPlayerTurnHeroDamage >= 8)
            singleTurnHeroDamageAtLeastEight = true;
    }

    public static void NotifyPlayerTurnEnded() => currentPlayerTurnHeroDamage = 0;

    public static void NotifyConsecrationBoundToNun()
    {
        consecrationBoundToNun = true;
    }

    public static void NotifyLesserHealOnConsecratedNun()
    {
        if (consecrationBoundToNun)
            holyTherapyChainComplete = true;
    }

    public static void NotifyHotBloodReversal() => hotBloodReversalTriggered = true;

    public static bool QuerySingleTurnHeroDamageAtLeastEight() => singleTurnHeroDamageAtLeastEight;

    public static bool QueryHolyTherapyChainComplete() => holyTherapyChainComplete;

    public static bool QueryHotBloodReversalTriggered() => hotBloodReversalTriggered;

    public static bool QueryAllMissionGoalsMet() =>
        QuerySingleTurnHeroDamageAtLeastEight() && QueryHolyTherapyChainComplete();

    public static string BuildMissingMissionHint()
    {
        var lines = new System.Collections.Generic.List<string>(2);
        if (!QuerySingleTurnHeroDamageAtLeastEight())
            lines.Add("○ 單回合對敵英雄傷害 ≥8");
        if (!QueryHolyTherapyChainComplete())
            lines.Add("○ 祝聖→修女→初級治療 完整連攬");
        return string.Join("\n", lines);
    }
}

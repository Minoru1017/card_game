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

    public static int QueryTriggeredSkillCount()
    {
        int count = 0;
        if (militiaFormationTriggered) count++;
        if (queenShelterTriggered) count++;
        if (kingDecreeTriggered) count++;
        return count;
    }

    /// <summary>階段 A 未通關結算：任務進度表 + 近失敗文案（不改通關條件）。</summary>
    public static string BuildPhaseAIncompleteSettlementBody(bool won)
    {
        const string doneHex = "#8CEB9E";
        const string pendingHex = "#C8CCC8";
        const string headerHex = "#F5D978";

        string Mark(bool done) => done
            ? "<color=" + doneHex + ">●</color>"
            : "<color=" + pendingHex + ">○</color>";

        bool militia = militiaFormationTriggered;
        bool queen = queenShelterTriggered;
        bool king = kingDecreeTriggered;

        var lines = new System.Text.StringBuilder(320);
        lines.Append("<color=").Append(headerHex).Append("><b>本局段考進度</b></color>\n");
        lines.Append(Mark(won)).Append(" 取得勝利\n");
        lines.Append(Mark(militia)).Append(" 民兵·列陣（本局）\n");
        lines.Append(Mark(queen)).Append(" 王后·王室庇護（本局）\n");
        lines.Append(Mark(king)).Append(" 國王·庭訓號令（本局）");

        string nearMiss = BuildPhaseANearMissMessage(won);
        if (!string.IsNullOrEmpty(nearMiss))
        {
            lines.Append("\n\n");
            lines.Append(nearMiss);
        }

        return lines.ToString();
    }

    public static string BuildPhaseANearMissMessage(bool won)
    {
        int triggered = QueryTriggeredSkillCount();
        bool allSkills = QueryAllTrioSkillsTriggered();

        if (won && !allSkills)
        {
            if (triggered == 2)
                return "這局差 " + FormatEmphasis(ResolveFirstMissingSkillLabel()) + " 就過段考";
            if (triggered == 1)
                return "本局已觸發 1 項戰技 再補 2 項即可過段考";
            return "牌局贏了 段考還沒簽名 三項戰技都要在本局觸發";
        }

        if (!won)
        {
            if (allSkills)
                return "三戰技都觸發了 下一局把" + FormatEmphasis("勝利") + "收回來就過段考";
            if (triggered >= 2)
                return "本局已觸發 " + triggered + " 項戰技 再穩住勝負並補齊剩餘戰技";
            if (triggered == 1)
                return "已摸到 1 項戰技 下一局照任務欄把剩下兩項補上";
            return "先看任務欄四項 逐項達成 段考本來就可以重考";
        }

        return string.Empty;
    }

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

    private static string ResolveFirstMissingSkillLabel()
    {
        if (!militiaFormationTriggered)
            return "民兵·列陣";
        if (!queenShelterTriggered)
            return "王后·王室庇護";
        if (!kingDecreeTriggered)
            return "國王·庭訓號令";
        return "御三家戰技";
    }

    private static string FormatEmphasis(string text) =>
        StoryTextStyle.Em(text);
}

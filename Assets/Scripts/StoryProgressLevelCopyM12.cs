/// <summary>Story progress 1-2 關卡面板文案。</summary>
public static class StoryProgressLevelCopyM12
{
    public const string LevelTitle = "1-2 海牆巡邏";
    public const string EstimatedPlayMinutesLabel = "8～12分鐘";

    public static string BuildScenarioIntro(int slot)
    {
        bool cleared = M12SeawallPatrolProgressState.IsNodeCleared(slot);
        bool phaseA = M12SeawallPatrolProgressState.IsPhaseAComplete(slot);
        bool mid = M12SeawallPatrolProgressState.IsMidPatrolComplete(slot);

        if (cleared)
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "你已通過" + FormatEmphasis("海牆巡邏段考") + " 可重溫加練 首通獎不重發" +
                   "</line-height>";
        }

        if (mid)
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "散策已完成 下一步" + FormatEmphasis("教會三張搭配") + " 實戰加練 須勝利" +
                   "</line-height>";
        }

        if (phaseA)
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   FormatEmphasis("御三家應用") + " 已通過 沿海牆散策後進入階段 B" +
                   "</line-height>";
        }

        return FormatSectionTag("關卡說明") + "\n" +
               "<line-height=128%>" +
               "段考分兩階段 先" + FormatEmphasis("御三家戰技") + " 再" +
               FormatEmphasis("修女／主教／城堡") + " 搭配 鎖定牌組不可換" +
               "</line-height>\n\n" +
               FormatSectionTag("關卡流程") + "\n" +
               "<line-height=128%>" +
               "階段 A 須勝利且本局三戰技各觸發 ≥1 中段海牆散策 階段 B 港灣簡單 須勝利" +
               " 預計約" + FormatTagHighlight(EstimatedPlayMinutesLabel) +
               "</line-height>";
    }

    public static string BuildScenarioRewards(CardStore cardStore, int slot)
    {
        string names = M12ReligiousLineRewardService.FormatRewardCardNames(cardStore);
        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
        {
            return FormatSectionTag("通關獎勵") + "\n" +
                   "<line-height=125%>" + names + "  " + FormatEmphasis("B 熟練度") + " · 已取得" +
                   "</line-height>";
        }

        return FormatSectionTag("通關獎勵") + "\n" +
               "<line-height=125%>" + names + "  " + FormatEmphasis("首通各 +1") + " 熟練度 " +
               FormatEmphasis("B") + "</line-height>";
    }

    public static string BuildBulletin(int slot)
    {
        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
        {
            return FormatBulletinTag("海牆佈告") + " " +
                   FormatBulletinEmphasis("海牆巡邏") +
                   FormatBulletinBody(" 段考已通過 按") +
                   FormatBulletinEmphasis("重溫段考");
        }

        return FormatBulletinTag("海牆佈告") + " " +
               FormatBulletinEmphasis("海牆巡邏") +
               FormatBulletinBody(" 御三家段考 按") +
               FormatBulletinEmphasis("進入海牆巡邏");
    }

    public static string ResolveEnterButtonLabel(int slot)
    {
        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
            return "重溫段考";
        if (M12SeawallPatrolProgressState.IsMidPatrolComplete(slot))
            return "進入加練";
        if (M12SeawallPatrolProgressState.IsPhaseAComplete(slot))
            return "繼續散策";
        return "進入海牆巡邏";
    }

    private const string SectionTagColor = "#8F6A36";
    private const string EmphasisColor = "#2C6F8F";
    private const string BulletinTagColor = "#F5E3A8";
    private const string BulletinBodyColor = "#DCE8DF";
    private const string BulletinEmphasisColor = "#B8F0D0";

    private static string FormatSectionTag(string label) =>
        "<size=32><color=" + SectionTagColor + "><b>" + label + "</b></color></size>";

    private static string FormatEmphasis(string text) =>
        "<color=" + EmphasisColor + "><b>" + text + "</b></color>";

    private static string FormatTagHighlight(string text) =>
        "<color=" + SectionTagColor + "><b>" + text + "</b></color>";

    private static string FormatBulletinTag(string label) =>
        "<size=38><color=" + BulletinTagColor + "><b>" + label + "</b></color></size>";

    private static string FormatBulletinBody(string text) =>
        "<color=" + BulletinBodyColor + ">" + text + "</color>";

    private static string FormatBulletinEmphasis(string text) =>
        "<color=" + BulletinEmphasisColor + "><b>" + text + "</b></color>";
}

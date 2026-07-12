/// <summary>Story progress 1-3 關卡面板文案。</summary>
public static class StoryProgressLevelCopyM13
{
    public const string LevelTitle = "1-3 河岔分波";
    public const string LevelSubtitle = "迎潮實測 · 燈下之詢";
    public const string EstimatedPlayMinutesLabel = "14～20分鐘";

    public static string BuildScenarioIntro(int slot)
    {
        if (M13RiverForkProgressState.IsNodeCleared(slot))
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "你已通關" + FormatEmphasis("河岔分波") + " 可重溫燈塔夜話與玫瑰試煉 首通獎不重發" +
                   "</line-height>";
        }

        if (M13RiverForkProgressState.IsRoseTrialSeen(slot))
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "玫瑰試煉已過 下一步" + FormatEmphasis("分波對決") + " 對決阿潮" +
                   "</line-height>";
        }

        if (M13RiverForkProgressState.IsPhaseAComplete(slot))
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "冷爐迎測已過 下一步" + FormatEmphasis("玫瑰試煉") +
                   "</line-height>";
        }

        if (M13RiverForkProgressState.IsForkStrollComplete(slot))
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "岔路已選 下一步" + FormatEmphasis("冷爐迎測") +
                   (M13RiverForkProgressState.GetForkPath(slot) == M13RiverForkPathChoice.Steady
                       ? "（穩流道）"
                       : "（急流道）") +
                   "</line-height>";
        }

        if (M13RiverForkProgressState.IsBirdDuelComplete(slot))
        {
            string sRankLine = M13RiverForkProgressState.HasBirdDuelSRank(slot)
                ? FormatEmphasis("S 評") + " 已解鎖開局天氣三選一 · 分波對決多抽 1"
                : "分波鬥鳥已完成";
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   sRankLine + " 下一步" + FormatEmphasis("岔路散策") + " 與" +
                   FormatEmphasis("冷爐迎測") +
                   "</line-height>";
        }

        if (M13RiverForkProgressState.IsOpeningSeen(slot))
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "邊燈夜話已讀 下一步" + FormatEmphasis("分波鬥鳥") + " 讓節奏與水流對齊" +
                   "</line-height>";
        }

        return FormatSectionTag("關卡說明") + "\n" +
               "<line-height=128%>" +
               FormatEmphasis("燈守·賽爾") + " 在河岔邊燈塔等候 問的是誓約不是金幣 " +
               "變天實測與" + FormatEmphasis("迎潮玫瑰") + " 試煉改寫自學院舊檔" +
               "</line-height>\n\n" +
               FormatSectionTag("關卡流程") + "\n" +
               "<line-height=128%>" +
               "邊燈夜話 → 分波鬥鳥 → 冷爐天氣戰 → 玫瑰試煉 → 對決阿潮 預計約" +
               FormatTagHighlight(EstimatedPlayMinutesLabel) +
               "</line-height>";
    }

    public static string BuildScenarioRewards(CardStore cardStore, int slot)
    {
        string cavalryName = FormatCardName(cardStore, 6, "王國騎兵");
        if (M13RiverForkProgressState.IsNodeCleared(slot))
        {
            return FormatSectionTag("通關獎勵") + "\n" +
                   "<line-height=125%>" + cavalryName + "  " + FormatEmphasis("A 熟練度") + " · 已取得" +
                   "</line-height>";
        }

        return FormatSectionTag("通關獎勵") + "\n" +
               "<line-height=125%>" + cavalryName + "  " + FormatEmphasis("首通 +1") + " 熟練度 " +
               FormatEmphasis("A") + "</line-height>";
    }

    public static string BuildBulletin(int slot)
    {
        if (M13RiverForkProgressState.IsNodeCleared(slot))
        {
            return FormatBulletinTag("河岔佈告") + " " +
                   FormatBulletinEmphasis("河岔分波") +
                   FormatBulletinBody(" 已通關 按") +
                   FormatBulletinEmphasis("重溫關卡");
        }

        return FormatBulletinTag("河岔佈告") + " " +
               FormatBulletinEmphasis("迎潮實測") +
               FormatBulletinBody(" 邊燈在等 按") +
               FormatBulletinEmphasis("進入河岔分波");
    }

    public static string ResolveEnterButtonLabel(int slot)
    {
        if (M13RiverForkProgressState.IsNodeCleared(slot))
            return "重溫關卡";
        if (M13RiverForkProgressState.IsRoseTrialSeen(slot))
            return "分波對決";
        if (M13RiverForkProgressState.IsPhaseAComplete(slot))
            return "玫瑰試煉";
        if (M13RiverForkProgressState.IsForkStrollComplete(slot))
            return "冷爐迎測";
        if (M13RiverForkProgressState.IsBirdDuelComplete(slot))
            return "岔路散策";
        if (M13RiverForkProgressState.IsOpeningSeen(slot))
            return "分波鬥鳥";
        return "進入河岔分波";
    }

    private static string FormatCardName(CardStore cardStore, int cardId, string fallback)
    {
        if (cardStore == null)
            return FormatEmphasis(fallback);

        Card card = cardStore.GetCardById(cardId);
        string name = card?.cardName;
        return FormatEmphasis(string.IsNullOrWhiteSpace(name) ? fallback : name);
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

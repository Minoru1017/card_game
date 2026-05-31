/// <summary>
/// Story progress 1-1 關卡介紹文案與地圖狀態標籤。
/// 世界觀定案見專案根目錄 STORY_PROGRESS_WORLDVIEW.md。
/// </summary>
public static class StoryProgressLevelCopy
{
    /// <summary>大地圖節點／課程註冊名（目標戰區，非當前對戰座標）。</summary>
    public const string LevelTitle = "1-1 港灣訓練場";

    public const string EstimatedPlayMinutesLabel = "5～8分鐘";

    public const string ViewLevelFlowPanelName = "View Level Flow";
    public const string LevelPanelTitlePlaceholder = "關卡的名稱";
    public const string ScenarioIntroPlaceholder = "描述劇本內容";
    public const string RewardsPlaceholderText = "呈現對戰通過後的獎勵";

    /// <summary>通關獎勵區 TMP 字型探測（與 <see cref="BuildScenarioRewards"/> 文案同步）。</summary>
    public const string RewardsFontGlyphProbe =
        BattleCardTuningPresetDisplay.CjkFontProbe +
        "通關獎勵入門試煉首通已取得港灣畢業證困難海牆巡邏解鎖任一難度國王后民兵";

    private const string SectionTagColor = "#8F6A36";
    private const string EmphasisColor = "#2C6F8F";
    private const string RareSrColor = "#7A4A92";
    private const string RareNColor = "#4F5B62";
    private const string BulletinTagColor = "#F5E3A8";
    private const string BulletinBodyColor = "#DCE8DF";
    private const string BulletinEmphasisColor = "#B8F0D0";
    private const string IntroLocationTagColor = "#B8F0D0";

    public static string ResolveMapStatusLabel(bool plotCompleted, bool battleCompleted, bool harborCombatCleared)
    {
        if (harborCombatCleared)
            return "Clear";
        if (plotCompleted && battleCompleted)
            return "實戰區";
        if (plotCompleted)
            return "進行中";
        return "NEW";
    }

    public static string BuildScenarioIntro(bool academyIntroGraduated)
    {
        if (academyIntroGraduated)
        {
            return FormatSectionTag("關卡說明") + "\n" +
                   "<line-height=128%>" +
                   "你已通過" + FormatEmphasis("學院入門試煉") + " " +
                   FormatTagHighlight("港灣訓練場") + "已解鎖 " +
                   "可選" + FormatEmphasis("簡單") + " " + FormatEmphasis("普通") + " " + FormatEmphasis("困難") + " " +
                   "敵方" + FormatEmphasis("快攻型") + " " +
                   "請用" + FormatEmphasis("防守牌") + "與" + FormatEmphasis("法術") + "應對" +
                   "</line-height>\n\n" +
                   FormatSectionTag("操作") + "\n" +
                   "<line-height=128%>" +
                   FormatEmphasis("挑戰港灣訓練場") + "選難度開戰 " +
                   FormatEmphasis("前往大廳") + "繼續旅程 " +
                   FormatEmphasis("重溫入門課") + "複習劇情教學戰" +
                   "</line-height>";
        }

        return FormatScenarioIntroSectionHeader(showIntroLocationTag: true) + "\n" +
               "<line-height=130%>" +
               "大地圖上的" + FormatTagHighlight("港灣訓練場") + "是你學會規則後要自己前往的實戰區 " +
               "今日仍由林可姐在" + FormatEmphasis("學院舊校舍對戰館") + "帶你完成入門試煉 " +
               "館內會先發放基礎牌組 之後再到" + FormatEmphasis("Buildbeck") + "自行調換" +
               "</line-height>\n\n" +
               FormatSectionTag("關卡流程") + "\n" +
               "<line-height=130%>" +
               "此關卡含" + FormatEmphasis("劇情選擇") + "與" + FormatEmphasis("對戰") +
               " 均在學院內進行 預計約" + FormatTagHighlight(EstimatedPlayMinutesLabel) +
               " 通過後才算有資格面對地圖上的港灣挑戰" +
               "</line-height>";
    }

    public static string BuildScenarioRewards(CardStore cardStore)
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        bool academyIntroGraduated = TutorialProgressState.IsAcademyIntroGraduated(slot);
        string gradName = ResolveCardName(cardStore, HarborTrainingRewardService.GraduationCardId, "聖院騎士");

        if (HarborTrainingProgressState.IsHarborHardGraduationRewardGranted(slot))
        {
            return FormatSectionTag("通關獎勵") + "\n" +
                   "<line-height=125%>" +
                   "<color=" + RareSrColor + "><b>SR" + gradName + "</b></color>  " +
                   FormatEmphasis("港灣畢業證") + " · 困難首通 · 已取得" +
                   "</line-height>";
        }

        if (academyIntroGraduated)
        {
            if (HarborTrainingProgressState.IsHarborCombatCleared(slot))
            {
                return FormatSectionTag("通關獎勵") + "\n" +
                       "<line-height=125%>" +
                       FormatTagHighlight("海牆巡邏") + FormatEmphasis("已解鎖") + "\n" +
                       "<color=" + RareSrColor + "><b>SR" + gradName + "</b></color>  " +
                       FormatEmphasis("港灣畢業證") + " · 困難首通可領" +
                       "</line-height>";
            }

            return FormatSectionTag("通關獎勵") + "\n" +
                   "<line-height=125%>" +
                   FormatEmphasis("首通任一難度") + " 解鎖" + FormatTagHighlight("海牆巡邏") + "\n" +
                   "<color=" + RareSrColor + "><b>SR" + gradName + "</b></color>  " +
                   FormatEmphasis("港灣畢業證") + " · 困難首通可領" +
                   "</line-height>";
        }

        if (TutorialProgressState.IsIntroTrioRewardGranted(slot))
        {
            return FormatSectionTag("通關獎勵") + "\n" +
                   "<line-height=125%>" +
                   FormatEmphasis("UR國王") + "  " +
                   "<color=" + RareSrColor + "><b>SR王后</b></color>  " +
                   "<color=" + RareNColor + "><b>N民兵</b></color>  " +
                   FormatEmphasis("入門試煉 · 已取得") +
                   "</line-height>";
        }

        return FormatSectionTag("通關獎勵") + "\n" +
               "<line-height=125%>" +
               FormatEmphasis("UR國王") + "  " +
               "<color=" + RareSrColor + "><b>SR王后</b></color>  " +
               "<color=" + RareNColor + "><b>N民兵</b></color>  " +
               FormatEmphasis("入門試煉首通") +
               "</line-height>";
    }

    private static string ResolveCardName(CardStore cardStore, int cardId, string fallback)
    {
        if (cardStore == null) return fallback;
        Card card = cardStore.GetCardById(cardId);
        return card != null && !string.IsNullOrWhiteSpace(card.cardName) ? card.cardName.Trim() : fallback;
    }

    public static string BuildHarborBulletin(bool academyIntroGraduated)
    {
        if (academyIntroGraduated)
        {
            return FormatBulletinTag("港灣佈告") + " " +
                   FormatBulletinEmphasis("港灣訓練場") +
                   FormatBulletinBody("已解鎖 ") +
                   FormatBulletinEmphasis("簡單 普通 困難") +
                   FormatBulletinBody(" 按") +
                   FormatBulletinEmphasis("挑戰港灣訓練場");
        }

        return FormatBulletinTag("港灣佈告") + " " +
               FormatBulletinEmphasis("港灣訓練場") +
               FormatBulletinBody("為地圖日後實戰區 今日") +
               FormatBulletinEmphasis("學院入門") +
               FormatBulletinBody(" 按") +
               FormatBulletinEmphasis("進入關卡");
    }

    private static string FormatBulletinTag(string label) =>
        "<size=38><color=" + BulletinTagColor + "><b>" + label + "</b></color></size>";

    private static string FormatBulletinBody(string text) =>
        "<color=" + BulletinBodyColor + ">" + text + "</color>";

    private static string FormatBulletinEmphasis(string text) =>
        "<color=" + BulletinEmphasisColor + "><b>" + text + "</b></color>";

    private static string FormatSectionTag(string label) =>
        "<size=32><color=" + SectionTagColor + "><b>" + label + "</b></color></size>";

    /// <summary>關卡說明標題列；入門中於右側顯示「入門課 · 學院內」（與地圖節點副標一致）。</summary>
    private static string FormatScenarioIntroSectionHeader(bool showIntroLocationTag)
    {
        if (!showIntroLocationTag)
            return FormatSectionTag("關卡說明");

        return FormatSectionTag("關卡說明") +
               "<pos=20%><size=26><color=" + IntroLocationTagColor + "><b>入門課 · 學院內</b></color></size>";
    }

    private static string FormatEmphasis(string text) =>
        "<color=" + EmphasisColor + "><b>" + text + "</b></color>";

    private static string FormatTagHighlight(string text) =>
        "<color=" + SectionTagColor + "><b>" + text + "</b></color>";
}

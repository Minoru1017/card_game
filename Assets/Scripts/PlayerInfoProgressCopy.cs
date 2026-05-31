/// <summary>玩家資訊面板用之主線進度摘要（對照 LEVEL_DESIGN_GDD / Story progress 旗標）。</summary>
public static class PlayerInfoProgressCopy
{
    public const string SectionTitle = "遊戲進度";

    /// <summary>玩家資訊 TMP 應能顯示的字元探測（含常用標點，避免缺字方塊）。</summary>
    public const string FontGlyphProbe =
        BattleCardTuningPresetDisplay.CjkFontProbe +
        "遊戲進度玩家資訊槽位入門劇情對戰港灣實戰區通關解鎖畢業證已取得未開始進行中可挑戰海牆巡邏章節地圖狀態張 ·";

    public static string BuildSummary(int slot)
    {
        slot = UnityEngine.Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        TutorialProgressState.GetAcademyIntroProgressForDisplay(slot, out bool plot, out bool battle);
        bool introGraduated = TutorialProgressState.IsAcademyIntroGraduated(slot);
        bool harbor = HarborTrainingProgressState.IsHarborCombatCleared(slot);
        bool hardGrad = HarborTrainingProgressState.IsHarborHardGraduationRewardGranted(slot);
        string mapStatus = StoryProgressLevelCopy.ResolveMapStatusLabel(plot, battle, harbor);

        return "章節 " + StoryProgressLevelCopy.LevelTitle + "\n" +
               "地圖狀態 " + mapStatus + "\n" +
               "入門劇情 " + (plot ? "已完成" : "未完成") + "\n" +
               "入門對戰 " + FormatIntroBattleStatus(plot, battle) + "\n" +
               "港灣實戰 " + (harbor ? "已通關" : FormatHarborCombatStatus(introGraduated)) + "\n" +
               "港灣畢業證 " + (hardGrad ? "已取得" : FormatGraduationStatus(harbor)) + "\n" +
               "M-1-2 海牆巡邏 " + (harbor ? "已解鎖" : "未解鎖");
    }

    public static string FormatRoleWithSlot(string role, int slot)
    {
        string r = string.IsNullOrWhiteSpace(role) ? "-" : role.Trim();
        return r + " · 槽位 " + slot;
    }

    private static string FormatIntroBattleStatus(bool plot, bool battle)
    {
        if (battle) return "已完成";
        if (plot) return "待進行";
        return "未開始";
    }

    private static string FormatHarborCombatStatus(bool introGraduated)
    {
        if (introGraduated) return "可挑戰";
        return "未解鎖";
    }

    private static string FormatGraduationStatus(bool harborCleared)
    {
        if (harborCleared) return "困難首通可領";
        return "未取得";
    }
}

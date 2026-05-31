/// <summary>Persists selected battle difficulty across scene load and through battle end.</summary>
public static class BattleLaunchContext
{
    public static string PendingDifficultyLabelZh { get; private set; }
    public static string ActiveBattleDifficultyLabelZh { get; private set; }
    /// <summary>對戰結束後回到 Story progress 場景（入門教學戰或港灣訓練場）。</summary>
    public static bool ReturnToStoryProgressAfterBattle { get; private set; }

    /// <summary>1-1 學院入門教學對戰（含落敗重試），此期間不得啟用天氣。</summary>
    public static bool IsIntroTutorialBattle { get; private set; }

    /// <summary>1-1 港灣訓練場實戰（入門通關後解鎖，簡單／普通／困難）。</summary>
    public static bool IsHarborTrainingGroundBattle { get; private set; }

    public static void BeginIntroTutorialBattleLaunch()
    {
        IsIntroTutorialBattle = true;
        IsHarborTrainingGroundBattle = false;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void BeginHarborTrainingGroundBattleLaunch()
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = true;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void SetPendingDifficultyLabelZh(string labelZh)
    {
        PendingDifficultyLabelZh = string.IsNullOrWhiteSpace(labelZh) ? null : labelZh.Trim();
        if (!string.IsNullOrWhiteSpace(PendingDifficultyLabelZh))
            BattleDifficultyRuntime.SetCurrentLabelZh(PendingDifficultyLabelZh);
    }

    public static void ConfirmActiveBattleDifficulty(string labelZh)
    {
        ActiveBattleDifficultyLabelZh = string.IsNullOrWhiteSpace(labelZh) ? null : labelZh.Trim();
        if (!string.IsNullOrWhiteSpace(ActiveBattleDifficultyLabelZh))
            BattleDifficultyRuntime.SetCurrentLabelZh(ActiveBattleDifficultyLabelZh);
    }

    public static void ClearActiveBattle()
    {
        ActiveBattleDifficultyLabelZh = null;
        PendingDifficultyLabelZh = null;
        ReturnToStoryProgressAfterBattle = false;
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = false;
    }

    public static string PeekDifficultyLabelZh() => PendingDifficultyLabelZh;

    public static string GetActiveBattleDifficultyLabelZh() => ActiveBattleDifficultyLabelZh;

    public static string ConsumeDifficultyLabelZh()
    {
        string label = PendingDifficultyLabelZh;
        PendingDifficultyLabelZh = null;
        return label;
    }

    public static string ResolveForBattleRecord()
    {
        if (!string.IsNullOrWhiteSpace(ActiveBattleDifficultyLabelZh))
            return ActiveBattleDifficultyLabelZh;
        if (!string.IsNullOrWhiteSpace(PendingDifficultyLabelZh))
            return PendingDifficultyLabelZh;
        return BattleDifficultyRuntime.CurrentLabelZh;
    }
}

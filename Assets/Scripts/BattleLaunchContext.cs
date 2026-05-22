/// <summary>Persists selected battle difficulty across scene load and through battle end.</summary>
public static class BattleLaunchContext
{
    public static string PendingDifficultyLabelZh { get; private set; }
    public static string ActiveBattleDifficultyLabelZh { get; private set; }

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

/// <summary>Current battle difficulty label for profile battle records (set before battle starts).</summary>
public static class BattleDifficultyRuntime
{
    public const string DefaultLabelZh = "入門";

    public static string CurrentLabelZh { get; private set; } = DefaultLabelZh;

    public static void SetCurrentLabelZh(string labelZh)
    {
        CurrentLabelZh = string.IsNullOrWhiteSpace(labelZh) ? DefaultLabelZh : labelZh.Trim();
    }

    public static void ResetToDefault() => CurrentLabelZh = DefaultLabelZh;

    /// <summary>Resolve label for profile battle records (manager AI / launch context / static cache).</summary>
    public static string ResolveForBattleRecord()
    {
        BattleSimulationManager manager = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager != null)
            return manager.GetBattleDifficultyLabelForRecord();
        return BattleLaunchContext.ResolveForBattleRecord();
    }
}

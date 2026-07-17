/// <summary>單一對戰模式的靜態設定（表驅動，供 Launch / BGM / 教練 / 克制 UI 集中查詢）。</summary>
public readonly struct BattleModeProfile
{
    public BattleModeKind Kind { get; }
    public bool ReturnToStoryProgressAfterBattle { get; }
    public bool ShowCombatRoleTriangle { get; }
    public bool WeatherEnabled { get; }
    public bool UsesLinKeCoachUi { get; }

    public BattleModeProfile(
        BattleModeKind kind,
        bool returnToStoryProgressAfterBattle,
        bool showCombatRoleTriangle,
        bool weatherEnabled,
        bool usesLinKeCoachUi)
    {
        Kind = kind;
        ReturnToStoryProgressAfterBattle = returnToStoryProgressAfterBattle;
        ShowCombatRoleTriangle = showCombatRoleTriangle;
        WeatherEnabled = weatherEnabled;
        UsesLinKeCoachUi = usesLinKeCoachUi;
    }
}

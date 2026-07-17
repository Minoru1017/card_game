/// <summary><see cref="BattleModeKind"/> 對應的靜態 Profile 表。</summary>
public static class BattleModeCatalog
{
    private static readonly BattleModeProfile NoneProfile = new(
        BattleModeKind.None,
        returnToStoryProgressAfterBattle: false,
        showCombatRoleTriangle: false,
        weatherEnabled: false,
        usesLinKeCoachUi: false);

    private static readonly BattleModeProfile IntroTutorialProfile = new(
        BattleModeKind.IntroTutorial,
        returnToStoryProgressAfterBattle: true,
        showCombatRoleTriangle: false,
        weatherEnabled: false,
        usesLinKeCoachUi: true);

    private static readonly BattleModeProfile HarborTrainingProfile = new(
        BattleModeKind.HarborTraining,
        returnToStoryProgressAfterBattle: true,
        showCombatRoleTriangle: false,
        weatherEnabled: false,
        usesLinKeCoachUi: true);

    private static readonly BattleModeProfile FreeBattleProfile = new(
        BattleModeKind.FreeBattle,
        returnToStoryProgressAfterBattle: false,
        showCombatRoleTriangle: true,
        weatherEnabled: false,
        usesLinKeCoachUi: false);

    private static readonly BattleModeProfile M12PhaseAProfile = new(
        BattleModeKind.M12PhaseA,
        returnToStoryProgressAfterBattle: true,
        showCombatRoleTriangle: false,
        weatherEnabled: false,
        usesLinKeCoachUi: true);

    private static readonly BattleModeProfile M12PhaseBProfile = new(
        BattleModeKind.M12PhaseB,
        returnToStoryProgressAfterBattle: true,
        showCombatRoleTriangle: true,
        weatherEnabled: false,
        usesLinKeCoachUi: true);

    private static readonly BattleModeProfile M13WeatherProfile = new(
        BattleModeKind.M13Weather,
        returnToStoryProgressAfterBattle: true,
        showCombatRoleTriangle: true,
        weatherEnabled: true,
        usesLinKeCoachUi: true);

    private static readonly BattleModeProfile M13RivalDuelProfile = new(
        BattleModeKind.M13RivalDuel,
        returnToStoryProgressAfterBattle: true,
        showCombatRoleTriangle: true,
        weatherEnabled: true,
        usesLinKeCoachUi: true);

    public static BattleModeProfile Get(BattleModeKind kind) =>
        kind switch
        {
            BattleModeKind.IntroTutorial => IntroTutorialProfile,
            BattleModeKind.HarborTraining => HarborTrainingProfile,
            BattleModeKind.FreeBattle => FreeBattleProfile,
            BattleModeKind.M12PhaseA => M12PhaseAProfile,
            BattleModeKind.M12PhaseB => M12PhaseBProfile,
            BattleModeKind.M13Weather => M13WeatherProfile,
            BattleModeKind.M13RivalDuel => M13RivalDuelProfile,
            _ => NoneProfile,
        };

    public static BattleModeKind ResolveFromContext(
        bool isIntroTutorial,
        bool isHarborTraining,
        bool isFreeBattle,
        bool isM12Trio,
        bool isM12Coach,
        bool isM13Weather,
        bool isM13Rival)
    {
        if (isIntroTutorial) return BattleModeKind.IntroTutorial;
        if (isHarborTraining) return BattleModeKind.HarborTraining;
        if (isFreeBattle) return BattleModeKind.FreeBattle;
        if (isM12Trio) return BattleModeKind.M12PhaseA;
        if (isM12Coach) return BattleModeKind.M12PhaseB;
        if (isM13Weather) return BattleModeKind.M13Weather;
        if (isM13Rival) return BattleModeKind.M13RivalDuel;
        return BattleModeKind.None;
    }
}

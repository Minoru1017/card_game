/// <summary>以 <see cref="BattleModeKind"/> 集中設定開局旗標（與既有 bool 並存）。</summary>
public static partial class BattleLaunchContext
{
    public static BattleModeKind ActiveModeKind { get; private set; } = BattleModeKind.None;

    public static BattleModeProfile ActiveProfile => BattleModeCatalog.Get(ActiveModeKind);

    public static bool IsAnyBattleModeActive => ActiveModeKind != BattleModeKind.None;

    private static void ActivateMode(BattleModeKind kind, EnemyAiPlayStyle freeBattleAi = EnemyAiPlayStyle.Balanced)
    {
        ActiveModeKind = kind;
        BattleModeProfile profile = BattleModeCatalog.Get(kind);

        IsIntroTutorialBattle = kind == BattleModeKind.IntroTutorial;
        IsHarborTrainingGroundBattle = kind == BattleModeKind.HarborTraining;
        IsFreeBattle = kind == BattleModeKind.FreeBattle;
        FreeBattleAiStyle = kind == BattleModeKind.FreeBattle ? freeBattleAi : EnemyAiPlayStyle.Balanced;
        IsM12TrioTutorialBattle = kind == BattleModeKind.M12PhaseA;
        IsM12CoachPracticeBattle = kind == BattleModeKind.M12PhaseB;
        IsM13WeatherTutorialBattle = kind == BattleModeKind.M13Weather;
        IsM13RivalDuelBattle = kind == BattleModeKind.M13RivalDuel;
        ReturnToStoryProgressAfterBattle = profile.ReturnToStoryProgressAfterBattle;
    }
}

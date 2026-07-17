/// <summary>各教學／訓練場戰鬥的啟動旗標（由 Story progress 等場景寫入）。</summary>
public static partial class BattleLaunchContext
{
    public static void BeginIntroTutorialBattleLaunch() =>
        ActivateMode(BattleModeKind.IntroTutorial);

    public static void BeginHarborTrainingGroundBattleLaunch() =>
        ActivateMode(BattleModeKind.HarborTraining);

    public static void BeginFreeBattleLaunch(EnemyAiPlayStyle aiStyle) =>
        ActivateMode(BattleModeKind.FreeBattle, aiStyle);

    public static void BeginM12TrioTutorialBattleLaunch() =>
        ActivateMode(BattleModeKind.M12PhaseA);

    public static void BeginM12CoachPracticeBattleLaunch() =>
        ActivateMode(BattleModeKind.M12PhaseB);

    public static void BeginM13WeatherTutorialBattleLaunch() =>
        ActivateMode(BattleModeKind.M13Weather);

    public static void BeginM13RivalDuelBattleLaunch() =>
        ActivateMode(BattleModeKind.M13RivalDuel);
}

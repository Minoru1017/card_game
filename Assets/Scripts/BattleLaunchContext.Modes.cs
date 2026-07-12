/// <summary>各教學／訓練場戰鬥的啟動旗標（由 Story progress 等場景寫入）。</summary>
public static partial class BattleLaunchContext
{
    public static void BeginIntroTutorialBattleLaunch()
    {
        IsIntroTutorialBattle = true;
        IsHarborTrainingGroundBattle = false;
        IsFreeBattle = false;
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = false;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void BeginHarborTrainingGroundBattleLaunch()
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = true;
        IsFreeBattle = false;
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = false;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void BeginFreeBattleLaunch(EnemyAiPlayStyle aiStyle)
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = false;
        IsFreeBattle = true;
        FreeBattleAiStyle = aiStyle;
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = false;
        ReturnToStoryProgressAfterBattle = false;
    }

    public static void BeginM12TrioTutorialBattleLaunch()
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = false;
        IsFreeBattle = false;
        IsM12TrioTutorialBattle = true;
        IsM12CoachPracticeBattle = false;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void BeginM12CoachPracticeBattleLaunch()
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = false;
        IsFreeBattle = false;
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = true;
        IsM13WeatherTutorialBattle = false;
        IsM13RivalDuelBattle = false;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void BeginM13WeatherTutorialBattleLaunch()
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = false;
        IsFreeBattle = false;
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = false;
        IsM13WeatherTutorialBattle = true;
        IsM13RivalDuelBattle = false;
        ReturnToStoryProgressAfterBattle = true;
    }

    public static void BeginM13RivalDuelBattleLaunch()
    {
        IsIntroTutorialBattle = false;
        IsHarborTrainingGroundBattle = false;
        IsFreeBattle = false;
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = false;
        IsM13WeatherTutorialBattle = false;
        IsM13RivalDuelBattle = true;
        ReturnToStoryProgressAfterBattle = true;
    }
}

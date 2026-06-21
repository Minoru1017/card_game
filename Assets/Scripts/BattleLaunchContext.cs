/// <summary>Persists selected battle difficulty across scene load and through battle end.</summary>
public static partial class BattleLaunchContext
{
    public static string PendingDifficultyLabelZh { get; private set; }
    public static string ActiveBattleDifficultyLabelZh { get; private set; }
    public static bool ReturnToStoryProgressAfterBattle { get; private set; }
    public static bool IsIntroTutorialBattle { get; private set; }
    public static bool IsHarborTrainingGroundBattle { get; private set; }
    public static bool IsM12TrioTutorialBattle { get; private set; }
    public static bool IsM12CoachPracticeBattle { get; private set; }
    public static string EnemyHeroId { get; private set; }
    public static string EnemyHeroDisplayName { get; private set; }

    public static bool IsM12TrioMasteryBattle => IsM12TrioTutorialBattle || IsM12CoachPracticeBattle;

    public static void SetEnemyHero(string heroId, string displayName)
    {
        EnemyHeroId = string.IsNullOrWhiteSpace(heroId) ? null : heroId.Trim();
        EnemyHeroDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    public static string ResolveEnemyHeroHudLabel() =>
        string.IsNullOrWhiteSpace(EnemyHeroDisplayName) ? "敵方英雄" : EnemyHeroDisplayName;

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
        IsM12TrioTutorialBattle = false;
        IsM12CoachPracticeBattle = false;
        EnemyHeroId = null;
        EnemyHeroDisplayName = null;
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

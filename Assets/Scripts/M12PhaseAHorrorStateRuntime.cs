using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// M-1-2 階段 A 段考：第二種對戰氛圍「恐怖狀態」。
/// 第 7 回合起進入；本局開局隨機決定持續 3～5 回合（不超過 5 回合）；重玩重新擲骰。
/// </summary>
public static class M12PhaseAHorrorStateRuntime
{
    public const int HorrorStartRound = 7;
    public const int MinDurationRounds = 3;
    public const int MaxDurationRounds = 5;

    private static bool rollsInitialized;
    private static int horrorLastRoundInclusive = int.MinValue;
    private static bool lastAppliedHorror;
    private static bool roundSixTransitionSfxPlayed;

    public static void ResetForNewBattle()
    {
        lastAppliedHorror = false;
        roundSixTransitionSfxPlayed = false;
        M12PhaseAHorrorTextScrambleUi.SetActiveForBattle(false);
        if (!BattleLaunchContext.IsM12TrioTutorialBattle)
        {
            rollsInitialized = false;
            horrorLastRoundInclusive = int.MinValue;
            return;
        }

        int durationSpan = MaxDurationRounds - MinDurationRounds + 1;
        int roll = Random.Range(0, durationSpan);
        horrorLastRoundInclusive = HorrorStartRound + MinDurationRounds - 1 + roll;

        rollsInitialized = true;
    }

    public static bool IsHorrorActive(int currentRound, bool battleOver)
    {
        if (!BattleLaunchContext.IsM12TrioTutorialBattle || !rollsInitialized)
            return false;
        if (currentRound < HorrorStartRound)
            return false;
        return currentRound <= horrorLastRoundInclusive;
    }

    public static void OnRoundAdvanced(int currentRound, bool battleOver)
    {
        M12PhaseAHorrorStateRunner runner = M12PhaseAHorrorStateRunner.EnsureInActiveBattleScene();
        if (runner != null)
            runner.QueueAtmosphereRefresh(currentRound, battleOver);
        else
            RefreshAtmosphereImmediate(currentRound, battleOver);
    }

    /// <summary>第 6 回合最後一次攻擊開始時播放 1-2 Transition。</summary>
    public static bool ShouldPlayRoundSixLastAttackTransitionSfx(
        BattleSimulationManager manager,
        bool attackerIsPlayer)
    {
        if (roundSixTransitionSfxPlayed || manager == null || BattleAutoSimPlugin.IsRunning)
            return false;
        if (!BattleLaunchContext.IsM12TrioTutorialBattle)
            return false;
        if (manager.GetCurrentRound() != HorrorStartRound - 1)
            return false;
        if (!attackerIsPlayer)
            return true;
        return !manager.WillEnemyPerformAttackThisRoundIfPossible();
    }

    public static void MarkRoundSixTransitionSfxPlayed()
    {
        roundSixTransitionSfxPlayed = true;
    }

    /// <summary>若氛圍將變更，回傳 true；enteringHorror 表示進入（非離開）恐怖狀態。</summary>
    public static bool TryBeginAtmosphereTransition(int currentRound, bool battleOver, out bool enteringHorror)
    {
        enteringHorror = false;
        if (!BattleLaunchContext.IsM12TrioTutorialBattle)
            return false;

        bool horror = IsHorrorActive(currentRound, battleOver);
        if (horror == lastAppliedHorror)
            return false;

        enteringHorror = horror;
        lastAppliedHorror = horror;
        return true;
    }

    public static void ApplyHorrorAtmosphereImmediate()
    {
        HarborTrainingBattleBackground.ApplyM12PhaseAHorrorBackground();
        ResolveMusicPlayer()?.PlayM12PhaseAHorrorBgm();
        M12PhaseAHorrorTextScrambleUi.SetActiveForBattle(true);
    }

    public static void ApplyNormalAtmosphereImmediate()
    {
        HarborTrainingBattleBackground.ApplyClassroomBackground();
        ResolveMusicPlayer()?.PlayM12PhaseABattleBgm();
        M12PhaseAHorrorTextScrambleUi.SetActiveForBattle(false);
    }

    private static void RefreshAtmosphereImmediate(int currentRound, bool battleOver)
    {
        if (!TryBeginAtmosphereTransition(currentRound, battleOver, out bool enteringHorror))
            return;

        if (enteringHorror)
            ApplyHorrorAtmosphereImmediate();
        else
            ApplyNormalAtmosphereImmediate();
    }

    private static TutorialBattleBackgroundMusicPlayer ResolveMusicPlayer()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return null;
        return TutorialBattleBackgroundMusicPlayer.FindInScene(scene);
    }
}

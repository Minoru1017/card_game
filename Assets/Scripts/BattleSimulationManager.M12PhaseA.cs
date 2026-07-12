using UnityEngine;

/// <summary>M-1-2 段考 A：回合上限結算（與 <see cref="M12PhaseABattleRules.MaxRoundsInclusive"/> 對齊）。</summary>
public partial class BattleSimulationManager
{
    private void TryM12PhaseARoundCapVictoryAtRoundAdvance()
    {
        if (!ShouldForceM12PhaseARoundCapVictory())
            return;
        ForceM12PhaseARoundCapVictory();
    }

    private void TryM12PhaseARoundCapVictoryAtBattleCheck()
    {
        if (!ShouldForceM12PhaseARoundCapVictory())
            return;
        ForceM12PhaseARoundCapVictory();
    }

    private bool ShouldForceM12PhaseARoundCapVictory()
    {
        return BattleLaunchContext.IsM12TrioTutorialBattle &&
               currentRound > M12PhaseABattleRules.MaxRoundsInclusive;
    }

    private void ForceM12PhaseARoundCapVictory()
    {
        if (battleOver)
            return;

        string prefix = "段考 A 第 " + M12PhaseABattleRules.MaxRoundsInclusive + " 回合結束";
        int result;
        string msg;
        if (playerHp > enemyHp)
        {
            result = 1;
            msg = prefix + "，我方 HP 較高，判定獲勝。";
        }
        else if (enemyHp > playerHp)
        {
            result = -1;
            msg = prefix + "，敵方 HP 較高，判定戰敗。";
        }
        else
        {
            result = 2;
            msg = prefix + "，雙方 HP 相同，判定平手。";
        }

        if (!BattleAutoSimPlugin.IsRunning)
            ShowBattleToast(msg, 3.2f);
        LogBattleHistory(msg);
        CompleteBattle(result, BattleAutoSimPlugin.IsRunning ? string.Empty : msg);
    }
}

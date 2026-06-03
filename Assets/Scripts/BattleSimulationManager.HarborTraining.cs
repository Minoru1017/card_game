using UnityEngine;

/// <summary>港灣訓練場：回合上限勝利與難度執行期掛點（與 <see cref="HarborTrainingDifficultyRuntime"/> 搭配）。</summary>
public partial class BattleSimulationManager
{
    private void TryHarborEasyRoundCapVictoryAtRoundAdvance()
    {
        if (!HarborTrainingDifficultyRuntime.ShouldForcePlayerVictoryAfterRound(currentRound))
            return;
        ForceHarborEasyRoundCapVictory();
    }

    private void TryHarborEasyRoundCapVictoryAtBattleCheck()
    {
        if (!HarborTrainingDifficultyRuntime.ShouldForcePlayerVictoryAfterRound(currentRound))
            return;
        ForceHarborEasyRoundCapVictory();
    }

    private void ForceHarborEasyRoundCapVictory()
    {
        if (battleOver) return;

        string msg = "港灣訓練（簡單）第 " + HarborTrainingEasyBattleRules.MaxRoundsInclusive +
                       " 回合結束，判定獲勝";
        ShowBattleToast(msg, 3.2f);
        LogBattleHistory(msg + "。");
        CompleteBattle(1, msg + "。");
    }
}

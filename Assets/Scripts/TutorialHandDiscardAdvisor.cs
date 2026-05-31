using System.Collections.Generic;

/// <summary>1-1 教學戰：棄牌階段建議棄掉的手牌索引（與玩家自動棄牌邏輯一致）。</summary>
public static class TutorialHandDiscardAdvisor
{
    public static bool TryGetRecommendedDiscardHandIndices(BattleSimulationManager manager, List<int> output)
    {
        output.Clear();
        if (manager == null || !TutorialBattleCoachUi.IsActiveForCurrentBattle) return false;
        if (!manager.IsPlayerTurn() || !manager.IsPlayerInDiscardSelection()) return false;

        int index = manager.GetRecommendedPlayerDiscardHandIndex();
        if (index < 0 || index >= manager.GetPlayerHandCount()) return false;

        output.Add(index);
        return true;
    }
}

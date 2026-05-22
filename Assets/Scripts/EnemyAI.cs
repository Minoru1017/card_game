using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Timing")]
    public float playDelay = 0.2f;
    public float attackDelay = 0.2f;

    /// <summary>出牌決策委由 <see cref="BattleSimulationManager.ChooseEnemyHandCardToPlayIndex"/>（含稀有度加權）。</summary>
    public void ExecutePlay(BattleSimulationManager battle)
    {
        if (battle == null) return;
        int chosen = battle.ChooseEnemyHandCardToPlayIndex();
        if (chosen >= 0) battle.EnemyPlayCardFromHand(chosen);
    }

    public void ExecuteAttack(BattleSimulationManager battle)
    {
        if (battle == null) return;
        battle.EnemyAttack();
    }
}

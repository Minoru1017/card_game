using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Timing")]
    public float playDelay = 0.2f;
    public float attackDelay = 0.2f;

    // 第一版：只考慮攻/防數值，優先下怪再攻擊
    public void ExecutePlay(BattleSimulationManager battle)
    {
        if (battle == null) return;
        if (battle.GetEnemyHandCount() <= 0) return;

        int chosen = -1;
        MonsterCard playerField = battle.GetPlayerFieldCard() as MonsterCard;

        // Tactical rule:
        // If player's field monster is glass-cannon style (ATK > current HP),
        // prioritize an enemy monster that can finish it in one hit.
        if (!battle.EnemyHasFieldMonster() && playerField != null && playerField.attack > playerField.healthPoint)
        {
            for (int i = 0; i < battle.GetEnemyHandCount(); i++)
            {
                MonsterCard enemyMonster = battle.GetEnemyHandCard(i) as MonsterCard;
                if (enemyMonster == null) continue;
                if (enemyMonster.attack >= playerField.healthPoint)
                {
                    chosen = i;
                    break;
                }
            }
        }

        // 場上沒怪 -> 優先找怪獸
        if (!battle.EnemyHasFieldMonster())
        {
            for (int i = 0; i < battle.GetEnemyHandCount() && chosen < 0; i++)
            {
                if (battle.GetEnemyHandCard(i) is MonsterCard)
                {
                    chosen = i;
                    break;
                }
            }
        }

        // 沒找到怪，出第一張法術
        if (chosen < 0)
        {
            for (int i = 0; i < battle.GetEnemyHandCount(); i++)
            {
                if (battle.GetEnemyHandCard(i) is SpellCard)
                {
                    chosen = i;
                    break;
                }
            }
        }

        if (chosen >= 0) battle.EnemyPlayCardFromHand(chosen);
    }

    public void ExecuteAttack(BattleSimulationManager battle)
    {
        if (battle == null) return;
        battle.EnemyAttack();
    }
}

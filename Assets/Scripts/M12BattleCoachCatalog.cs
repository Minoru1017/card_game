/// <summary>M-1-2 教練台詞表（L1-2-009）；階段 A 段考不提示，階段 B 先浮窗講克制再帶練習。</summary>
public static class M12BattleCoachCatalog
{
    public const string SpeakerName = TutorialPlotScriptFactory.LinKeSpeaker;

    private static readonly (string key, string message)[] CombatRoleLessonSteps =
    {
        (
            "lesson_roles",
            "這場加練教" + StoryTextStyle.Em("戰位克制") + " 每張怪獸牌會標" +
            StoryTextStyle.Em("先鋒") + " " + StoryTextStyle.Em("守陣") + " " +
            StoryTextStyle.Em("策應") + " 或" + StoryTextStyle.Em("定式")
        ),
        (
            "lesson_triangle",
            "記一句三角 " + StoryTextStyle.Em("先鋒克策應") + " " +
            StoryTextStyle.Em("策應克守陣") + " " + StoryTextStyle.Em("守陣克先鋒")
        ),
        (
            "lesson_matchup_fx",
            "克制成立攻擊有加成 場上會跳" + StoryTextStyle.Hi("克制") +
            " 被克則吃虧 會標" + StoryTextStyle.Hi("被克")
        ),
        (
            "lesson_finisher",
            StoryTextStyle.Em("定式") + "在敵英雄半血以下有追擊加成 別急著亂換位"
        ),
        (
            "lesson_start_practice",
            "好 概念先到這 接下來看場上戰位 我帶你練 先出一張怪再說"
        ),
    };

    public static int LessonStepCount => CombatRoleLessonSteps.Length;

    public static bool TryGetLessonStep(int index, out string key, out string message)
    {
        key = string.Empty;
        message = string.Empty;
        if (index < 0 || index >= CombatRoleLessonSteps.Length)
            return false;

        (string k, string m) = CombatRoleLessonSteps[index];
        key = k;
        message = m;
        return true;
    }

    public static bool TryEvaluatePhaseB(BattleSimulationManager manager, out string key, out string message)
    {
        key = string.Empty;
        message = string.Empty;
        if (manager == null || !BattleLaunchContext.IsM12CoachPracticeBattle)
            return false;
        if (!manager.IsPlayerTurn() || manager.IsBattleOver())
            return false;
        if (manager.IsOpeningPresentationInProgress() ||
            manager.IsTurnSequenceInProgress() ||
            manager.IsSpellCastPresentationActive())
            return false;

        if (manager.IsPlayerInDiscardSelection() || manager.GetPlayerPendingDiscardCount() > 0)
        {
            key = "discard";
            message = "手牌滿了先棄牌 港灣實戰節奏不能拖";
            return true;
        }

        MonsterCard enemyField = manager.GetEnemyFieldCard() as MonsterCard;
        MonsterCard playerField = manager.GetPlayerFieldCard() as MonsterCard;

        if (enemyField != null)
        {
            string enemyRoleLabel = CombatRoleUtility.GetDisplayName(enemyField.combatRole);

            if (TryFindHandMonsterWithAdvantage(manager, enemyField.combatRole, out MonsterCard counterCard) &&
                (playerField == null ||
                 CombatRoleBattleRules.GetTriangleMatchup(playerField.combatRole, enemyField.combatRole) !=
                 CombatRoleMatchup.Advantage))
            {
                key = "counter_play_" + counterCard.id;
                message = "對面是" + StoryTextStyle.Em(enemyRoleLabel) + " 出" +
                          StoryTextStyle.Em(CombatRoleUtility.GetDisplayName(counterCard.combatRole)) +
                          " 有克制加成";
                return true;
            }

            if (playerField != null &&
                CombatRoleBattleRules.GetTriangleMatchup(playerField.combatRole, enemyField.combatRole) ==
                CombatRoleMatchup.Disadvantage)
            {
                key = "disadvantage_" + playerField.id + "_" + enemyField.id;
                message = "我們" + StoryTextStyle.Em(CombatRoleUtility.GetDisplayName(playerField.combatRole)) +
                          " 被" + StoryTextStyle.Em(enemyRoleLabel) + " 克了 換場或先忍一回合";
                return true;
            }

            if (playerField != null &&
                CombatRoleBattleRules.GetTriangleMatchup(playerField.combatRole, enemyField.combatRole) ==
                CombatRoleMatchup.Advantage)
            {
                key = "advantage_attack";
                message = StoryTextStyle.Em("克制成立") + " 可以攻擊換血";
                return true;
            }
        }

        if (playerField != null &&
            playerField.combatRole == CombatRole.Finisher &&
            manager.GetEnemyHeroHp() <= CombatRoleBattleRules.FinisherHeroHpThreshold)
        {
            key = "finisher_window";
            message = "敵英雄半血以下 " + StoryTextStyle.Em("定式") + " 有追擊加成 可以收局";
            return true;
        }

        if (playerField == null)
        {
            key = "play_monster";
            message = "先出一張怪 看對面" + StoryTextStyle.Em("戰位") + " 再決定要不要換克制";
            return true;
        }

        key = "end_turn";
        message = "記住三角 穩住就結束回合";
        return true;
    }

    private static bool TryFindHandMonsterWithAdvantage(
        BattleSimulationManager manager,
        CombatRole enemyRole,
        out MonsterCard best)
    {
        best = null;
        if (manager == null)
            return false;

        for (int i = 0; i < manager.GetPlayerHandCount(); i++)
        {
            if (manager.GetPlayerHandCard(i) is MonsterCard handMonster &&
                CombatRoleBattleRules.GetTriangleMatchup(handMonster.combatRole, enemyRole) ==
                CombatRoleMatchup.Advantage)
            {
                best = handMonster;
                return true;
            }
        }

        return false;
    }
}

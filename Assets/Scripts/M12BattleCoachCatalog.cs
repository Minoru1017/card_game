/// <summary>M-1-2 段考教練台詞表（L1-2-009 · 新 M12 表）。</summary>
public static class M12BattleCoachCatalog
{
    public const string SpeakerName = TutorialPlotScriptFactory.LinKeSpeaker;

    public static bool TryEvaluatePhaseA(BattleSimulationManager manager, out string key, out string message)
    {
        key = string.Empty;
        message = string.Empty;
        if (manager == null || !BattleLaunchContext.IsM12TrioTutorialBattle)
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
            message = "手牌滿了先棄牌 段考別被卡死";
            return true;
        }

        if (!M12TrioMasteryBattleTracker.QueryMilitiaTriggered())
        {
            key = "trio_militia";
            message = "先上" + StoryTextStyle.Em("民兵") + " 觸發" + StoryTextStyle.Hi("列陣") + " 這是段考第一項";
            return true;
        }

        if (!M12TrioMasteryBattleTracker.QueryQueenTriggered())
        {
            key = "trio_queen";
            message = "換" + StoryTextStyle.Em("王后") + " 上場讓她吃到第一刀 觸發" + StoryTextStyle.Hi("王室庇護");
            return true;
        }

        if (!M12TrioMasteryBattleTracker.QueryKingTriggered())
        {
            key = "trio_king";
            message = "讓" + StoryTextStyle.Em("國王") + " 在場 敵方直擊英雄時會觸發" + StoryTextStyle.Hi("庭訓號令");
            return true;
        }

        if (manager.GetPlayerFieldCard() == null)
        {
            key = "play";
            message = "三項戰技都觸發過了 穩住節奏拿勝利";
            return true;
        }

        key = "end_turn";
        message = "段考達標了 按結束回合收尾";
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

        Card field = manager.GetPlayerFieldCard();
        if (field == null)
        {
            key = "church_play";
            message = "這段考看" + StoryTextStyle.Em("修女") + " " + StoryTextStyle.Em("主教") + " " +
                      StoryTextStyle.Em("城堡") + " 的搭配時機 先出一張教會怪";
            return true;
        }

        if (field.id == MonsterSkillIds.Nun)
        {
            key = "nun";
            message = StoryTextStyle.Em("修女") + " 聖療共鳴要配合低血或治療 別空放";
            return true;
        }

        if (field.id == MonsterSkillIds.Bishop)
        {
            key = "bishop";
            message = StoryTextStyle.Em("主教") + " 祝聖綁下一隻上場怪 想想誰最值得強化";
            return true;
        }

        if (field.id == MonsterSkillIds.Castle)
        {
            key = "castle";
            message = StoryTextStyle.Em("城堡") + " 堅城駐守擋直擊 敵方快攻時特別有用";
            return true;
        }

        key = "end_turn";
        message = "教會三張輪著用 穩住就按結束回合";
        return true;
    }
}

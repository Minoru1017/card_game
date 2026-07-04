/// <summary>M-1-2 教練台詞表（L1-2-009 · 新 M12 表）；階段 A 段考不提示（含棄牌），僅階段 B 加練有教練。</summary>
public static class M12BattleCoachCatalog
{
    public const string SpeakerName = TutorialPlotScriptFactory.LinKeSpeaker;

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
            message = "這場加練看" + StoryTextStyle.Em("修女") + " " + StoryTextStyle.Em("主教") + " " +
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

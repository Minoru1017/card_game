/// <summary>M-1-3 分波對決教練台詞；「我也要看奇蹟」時前 3 回合沉默。</summary>
public static class M13BattleCoachCatalog
{
    public const string SpeakerName = TutorialPlotScriptFactory.LinKeSpeaker;
    public const int CoachSilenceUntilRoundExclusive = 4;

    public static bool TryEvaluate(BattleSimulationManager manager, out string key, out string message)
    {
        key = string.Empty;
        message = string.Empty;
        if (manager == null || !BattleLaunchContext.IsM13RivalDuelBattle)
            return false;
        if (!manager.IsPlayerTurn() || manager.IsBattleOver())
            return false;
        if (manager.IsOpeningPresentationInProgress() ||
            manager.IsTurnSequenceInProgress() ||
            manager.IsSpellCastPresentationActive())
            return false;

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M13RoseTrialOutcome.ShouldSilenceCoachEarlyRounds(slot) &&
            manager.GetCurrentRound() < CoachSilenceUntilRoundExclusive)
            return false;

        if (manager.IsPlayerInDiscardSelection() || manager.GetPlayerPendingDiscardCount() > 0)
        {
            key = "discard";
            message = "手牌滿了先棄牌 分波對決節奏不等人";
            return true;
        }

        Card field = manager.GetPlayerFieldCard();
        if (field == null)
        {
            key = "play";
            message = "先出一張場怪 讓牌局跟上分波的節奏";
            return true;
        }

        if (field.id == MonsterSkillIds.Bishop)
        {
            key = "bishop";
            message = StoryTextStyle.Em("主教") + " 祝聖綁" + StoryTextStyle.Em("修女") + " 再初級治療 任務欄會亮";
            return true;
        }

        if (field.id == MonsterSkillIds.Nun)
        {
            key = "nun";
            message = StoryTextStyle.Em("修女") + " 在場時 初級治療能連攬祝聖";
            return true;
        }

        key = "burst";
        message = "找一回合直擊 ≥8 證明你走的路";
        return true;
    }
}

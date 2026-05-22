/// <summary>對戰結算：單張怪物牌熟練度條播報資料。</summary>

public readonly struct BattleProficiencySettlementEntry

{

    public readonly int monsterId;

    public readonly string cardName;

    public readonly float fillBefore01;

    public readonly float fillAfter01;

    public readonly CardSkillRevealStage stageBefore;

    public readonly CardSkillRevealStage stageAfter;

    public readonly float progressDelta;



    public BattleProficiencySettlementEntry(

        int monsterId,

        string cardName,

        float fillBefore01,

        float fillAfter01,

        CardSkillRevealStage stageBefore,

        CardSkillRevealStage stageAfter,

        float progressDelta)

    {

        this.monsterId = monsterId;

        this.cardName = cardName;

        this.fillBefore01 = fillBefore01;

        this.fillAfter01 = fillAfter01;

        this.stageBefore = stageBefore;

        this.stageAfter = stageAfter;

        this.progressDelta = progressDelta;

    }



    public bool HadProgressChange =>

        progressDelta > 0.0001f || stageAfter != stageBefore;

}



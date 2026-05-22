/// <summary>怪物戰技揭露階段（對齊 CARD_PROFICIENCY_GDD / 卡牌技能階段式揭露）。</summary>
public enum CardSkillRevealStage
{
    /// <summary>A：未解放（僅模糊提示）。</summary>
    LockedA = 0,
    /// <summary>B：基礎解放（一行摘要，效果生效）。</summary>
    BasicB = 1,
    /// <summary>C：完整解放（圖鑑／詳情完整條文）。</summary>
    FullC = 2
}

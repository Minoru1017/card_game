/// <summary>港灣訓練場單一難度檔的開戰設定（供 SceneLoader 與文件對照）。</summary>
public readonly struct HarborTrainingTierConfig
{
    public readonly string LabelZh;
    public readonly int[] FixedEnemyDeckCardIds;
    public readonly int EnemyOverLimitAllowance;
    public readonly int MinEnemySpellsInDeck;
    public readonly EnemyAiPlayStyle AiPlayStyle;

    public HarborTrainingTierConfig(
        string labelZh,
        int[] fixedEnemyDeckCardIds,
        int enemyOverLimitAllowance,
        int minEnemySpellsInDeck,
        EnemyAiPlayStyle aiPlayStyle = EnemyAiPlayStyle.FastAttack)
    {
        LabelZh = labelZh;
        FixedEnemyDeckCardIds = fixedEnemyDeckCardIds;
        EnemyOverLimitAllowance = enemyOverLimitAllowance;
        MinEnemySpellsInDeck = minEnemySpellsInDeck;
        AiPlayStyle = aiPlayStyle;
    }
}

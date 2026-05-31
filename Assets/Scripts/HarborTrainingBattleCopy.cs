/// <summary>Story progress 港灣訓練場（1-1 入門通關後）戰前預覽與關卡說明文案。</summary>
public static class HarborTrainingBattleCopy
{
    public static bool IsUnlockedForActivePlayer() =>
        TutorialProgressState.IsAcademyIntroGraduatedForActivePlayer();

    public static BattleDifficultyTier TierFromLabelZh(string labelZh)
    {
        if (string.IsNullOrWhiteSpace(labelZh))
            return BattleDifficultyTier.Normal;
        string label = labelZh.Trim();
        if (label.StartsWith("簡單", System.StringComparison.Ordinal))
            return BattleDifficultyTier.Easy;
        if (label.StartsWith("困難", System.StringComparison.Ordinal))
            return BattleDifficultyTier.Hard;
        return BattleDifficultyTier.Normal;
    }

    public const string PreviewHeaderRich = "<size=115%><b>港灣訓練場 選擇難易度</b></size>";
    public const string PreviewGoalRich =
        "<color=#6C533D>練習目標 運用<color=#43573A><b>防守牌</b></color>與<color=#43573A><b>法術</b></color>擊敗對手</color>";
    public const string PreviewLeftTitleRich = "<b>訓練提示</b>";
    public const string PreviewLeftDetailRich =
        "<color=#43573A>簡單級前段較緩 約 10 回合內收尾 普通級可練可過、節奏略升 仍宜保留治療與拆場法術 穩血線再反擊</color>";
    public const string PreviewRightTitleRich = "<b>敵方特色</b>";
    public const string PreviewRightDetailRich =
        "<color=#43573A><b>快攻型</b> 傾向早出場怪與直傷 壓迫感強 需善用防守與法術化解</color>";
}

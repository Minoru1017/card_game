/// <summary>M-1-2 段考備忘靜態文案（第 2 次落敗解鎖；非戰中教練）。</summary>
public static class M12PhaseAExamMemoCopy
{
    public const string SpeakerName = TutorialPlotScriptFactory.LinKeSpeaker;
    public const string PanelTitle = "段考備忘";

    public static string BuildBodyRichText(bool firstUnlockReveal)
    {
        string intro = firstUnlockReveal
            ? "你第二次卡在段考 我把" + StoryTextStyle.Em("觸發條件") + "整理成備忘 場內仍不會出聲提示"
            : "只記" + StoryTextStyle.Em("怎樣算觸發") + " 不教每一手怎麼出 段考仍靠你自己打";

        return intro + "\n\n" +
               FormatRow("取得勝利", "敵方英雄歸零") + "\n" +
               FormatRow("民兵·列陣", "本局我方" + StoryTextStyle.Em("民兵") + "首次攻擊時") + "\n" +
               FormatRow("王后·王室庇護", "王后在場時" + StoryTextStyle.Em("首次") + "受到傷害並減傷") + "\n" +
               FormatRow("國王·庭訓號令", "國王在場敵直擊減傷 或 庭訓剩餘次數減少") + "\n\n" +
               "對照右上角" + StoryTextStyle.Hi("任務欄") + " 四項全綠才算段考通過";
    }

    public static string ResolveDismissLabel(bool firstUnlockReveal) =>
        firstUnlockReveal ? "記住了" : "關閉";

    private static string FormatRow(string label, string detail) =>
        StoryTextStyle.Em(label) + " — " + detail;
}

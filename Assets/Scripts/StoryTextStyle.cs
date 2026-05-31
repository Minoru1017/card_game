/// <summary>教學／劇情文案：暫不使用標點，以空格與 Rich Text 粗體／顏色標示重點。</summary>
public static class StoryTextStyle
{
    public const string EmphasisHex = "#9A7A55";
    public const string HighlightHex = "#5F8F72";
    public const string MutedHex = "#6B5F58";

    public static string Em(string text) => "<color=" + EmphasisHex + "><b>" + text + "</b></color>";

    public static string Hi(string text) => "<color=" + HighlightHex + "><b>" + text + "</b></color>";

    public static string Mu(string text) => "<color=" + MutedHex + ">" + text + "</color>";

    public static UnityEngine.Color HexToColor(string hex) => BattleUiColors.Hex(hex);
}

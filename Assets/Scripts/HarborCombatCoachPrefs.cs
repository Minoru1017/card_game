using UnityEngine;

/// <summary>港灣實戰戰術教練玩家設定（PlayerPrefs）。</summary>
public static class HarborCombatCoachPrefs
{
    private const string KeyTacticalHints = "harbor_coach_tactical_hints";
    private const string KeyHandHighlight = "harbor_coach_hand_highlight";

    public static bool AreTacticalHintsEnabled() =>
        PlayerPrefs.GetInt(KeyTacticalHints, 1) != 0;

    public static bool IsHandHighlightEnabled() =>
        PlayerPrefs.GetInt(KeyHandHighlight, 1) != 0;

    public static void SetTacticalHintsEnabled(bool enabled) =>
        PlayerPrefs.SetInt(KeyTacticalHints, enabled ? 1 : 0);

    public static void SetHandHighlightEnabled(bool enabled) =>
        PlayerPrefs.SetInt(KeyHandHighlight, enabled ? 1 : 0);
}

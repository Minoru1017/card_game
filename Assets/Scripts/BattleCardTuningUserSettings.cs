using UnityEngine;

/// <summary>
/// 玩家選擇的戰鬥卡牌調校預設（PlayerPrefs），供 Settings 場景寫入、對戰場景讀取。
/// </summary>
public static class BattleCardTuningUserSettings
{
    private const string PresetIdPrefsKey = "battle_card_tuning_preset_id";
    private const string QualityLevelPrefsKey = "settings_quality_level";

    public static string GetSelectedPresetId() =>
        PlayerPrefs.GetString(PresetIdPrefsKey, BattleCardTuningPresetLibrary.Preset1Id);

    public static void SetSelectedPresetId(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId)) return;
        PlayerPrefs.SetString(PresetIdPrefsKey, presetId);
        PlayerPrefs.Save();
    }

    public static bool TryApplySelectedPreset(BattleSimulationManager manager) =>
        manager != null && BattleCardTuningPresetLibrary.TryApplyPreset(manager, GetSelectedPresetId());

    public static int GetSavedQualityLevel()
    {
        int saved = PlayerPrefs.GetInt(QualityLevelPrefsKey, QualitySettings.GetQualityLevel());
        return Mathf.Clamp(saved, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    public static void SetQualityLevel(int level)
    {
        int clamped = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        QualitySettings.SetQualityLevel(clamped, true);
        PlayerPrefs.SetInt(QualityLevelPrefsKey, clamped);
        PlayerPrefs.Save();
    }

    public static void ApplySavedQualityLevel()
    {
        if (QualitySettings.names == null || QualitySettings.names.Length == 0) return;
        QualitySettings.SetQualityLevel(GetSavedQualityLevel(), true);
    }
}

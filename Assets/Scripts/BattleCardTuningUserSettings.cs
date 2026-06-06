using UnityEngine;

/// <summary>
/// 玩家選擇的戰鬥卡牌調校預設（PlayerPrefs），供 Settings 場景寫入、對戰場景讀取。
/// </summary>
public static class BattleCardTuningUserSettings
{
    private const string PresetIdPrefsKey = "battle_card_tuning_preset_id";
    private const string TargetFpsPrefsKey = "settings_target_fps";
    private const int Fps30 = 30;
    private const int Fps60 = 60;

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

    public static int GetSavedTargetFps()
    {
        int saved = PlayerPrefs.GetInt(TargetFpsPrefsKey, Fps60);
        return saved <= Fps30 ? Fps30 : Fps60;
    }

    public static void SetTargetFps(int fps)
    {
        int clamped = fps <= Fps30 ? Fps30 : Fps60;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = clamped;
        PlayerPrefs.SetInt(TargetFpsPrefsKey, clamped);
        PlayerPrefs.Save();
    }

    public static void ApplySavedTargetFps()
    {
        SetTargetFps(GetSavedTargetFps());
    }
}

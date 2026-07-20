using UnityEngine;
using UnityEngine.Audio;

/// <summary>執行期載入 Game Audio Mixer，並依 GameAudioUserSettings 更新 exposed 參數。</summary>
public static class GameAudioMixerCatalog
{
    private const float MinDb = -80f;

    private static GameAudioMixerRegistry registry;
    private static bool registryLoaded;

    public static bool IsActive => Registry != null && Registry.Mixer != null;

    private static GameAudioMixerRegistry Registry
    {
        get
        {
            if (registryLoaded)
                return registry;

            registryLoaded = true;
            registry = Resources.Load<GameAudioMixerRegistry>(GameAudioMixerRegistry.DefaultResourcesPath);
            if (registry == null)
            {
                Debug.LogWarning(
                    "GameAudioMixerCatalog: 找不到 Resources/" + GameAudioMixerRegistry.DefaultResourcesPath +
                    ".asset，請執行 Tools/Audio/Create or Refresh Game Audio Mixer。");
            }

            return registry;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnStartup() => ApplyUserSettings();

    public static AudioMixerGroup GetGroup(GameAudioChannel channel) =>
        Registry != null ? Registry.GetGroup(channel) : null;

    public static void Bind(AudioSource source, GameAudioChannel channel)
    {
        if (source == null)
            return;

        AudioMixerGroup group = GetGroup(channel);
        if (group != null)
            source.outputAudioMixerGroup = group;
    }

    public static void ApplyUserSettings()
    {
        GameAudioMixerRegistry reg = Registry;
        AudioMixer mixer = reg != null ? reg.Mixer : null;
        if (mixer == null)
            return;

        float masterScale = GameAudioUserSettings.IsMasterEnabled() ? 1f : 0f;
        SetLinearVolume(mixer, reg.BgmVolumeParam, masterScale * GameAudioUserSettings.GetBgmVolume());
        SetLinearVolume(mixer, reg.NpcVoiceVolumeParam, masterScale * GameAudioUserSettings.GetNpcVoiceVolume());
        SetLinearVolume(mixer, reg.ButtonSfxVolumeParam, masterScale * GameAudioUserSettings.GetButtonSfxVolume());
        SetLinearVolume(mixer, reg.BattleSfxVolumeParam, masterScale * GameAudioUserSettings.GetBattleSfxVolume());

        if (!string.IsNullOrEmpty(reg.MasterVolumeParam))
            SetLinearVolume(mixer, reg.MasterVolumeParam, masterScale);
    }

    private static void SetLinearVolume(AudioMixer mixer, string paramName, float linear01)
    {
        if (string.IsNullOrEmpty(paramName))
            return;

        float db = Linear01ToDb(linear01);
        if (!mixer.SetFloat(paramName, db))
        {
            Debug.LogWarning("GameAudioMixerCatalog: 無法設定 mixer 參數 '" + paramName +
                             "'（請重跑 Tools/Audio/Create or Refresh Game Audio Mixer）。");
        }
    }

    public static float Linear01ToDb(float linear01)
    {
        if (linear01 <= 0.0001f)
            return MinDb;

        return Mathf.Log10(Mathf.Clamp01(linear01)) * 20f;
    }
}

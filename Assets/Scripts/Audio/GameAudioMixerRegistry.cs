using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 指向 Resources/Audio/GameAudio.mixer 的分軌設定。
/// 由 Tools/Audio/Create or Refresh Game Audio Mixer 建立／更新。
/// </summary>
[CreateAssetMenu(fileName = "GameAudioMixerRegistry", menuName = "Card Game/Game Audio Mixer Registry")]
public sealed class GameAudioMixerRegistry : ScriptableObject
{
    public const string DefaultResourcesPath = "Audio/GameAudioMixerRegistry";

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup npcVoiceGroup;
    [SerializeField] private AudioMixerGroup buttonSfxGroup;
    [SerializeField] private AudioMixerGroup battleSfxGroup;

    [Header("Exposed Parameters")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string bgmVolumeParam = "BgmVolume";
    [SerializeField] private string npcVoiceVolumeParam = "NpcVoiceVolume";
    [SerializeField] private string buttonSfxVolumeParam = "ButtonSfxVolume";
    [SerializeField] private string battleSfxVolumeParam = "BattleSfxVolume";

    public AudioMixer Mixer => mixer;
    public string MasterVolumeParam => masterVolumeParam;
    public string BgmVolumeParam => bgmVolumeParam;
    public string NpcVoiceVolumeParam => npcVoiceVolumeParam;
    public string ButtonSfxVolumeParam => buttonSfxVolumeParam;
    public string BattleSfxVolumeParam => battleSfxVolumeParam;

    public AudioMixerGroup GetGroup(GameAudioChannel channel)
    {
        switch (channel)
        {
            case GameAudioChannel.Bgm: return bgmGroup;
            case GameAudioChannel.NpcVoice: return npcVoiceGroup;
            case GameAudioChannel.ButtonSfx: return buttonSfxGroup;
            case GameAudioChannel.BattleSfx: return battleSfxGroup;
            default: return null;
        }
    }

    public string GetVolumeParam(GameAudioChannel channel)
    {
        switch (channel)
        {
            case GameAudioChannel.Bgm: return bgmVolumeParam;
            case GameAudioChannel.NpcVoice: return npcVoiceVolumeParam;
            case GameAudioChannel.ButtonSfx: return buttonSfxVolumeParam;
            case GameAudioChannel.BattleSfx: return battleSfxVolumeParam;
            default: return null;
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(
        AudioMixer audioMixer,
        AudioMixerGroup bgm,
        AudioMixerGroup npcVoice,
        AudioMixerGroup buttonSfx,
        AudioMixerGroup battleSfx)
    {
        mixer = audioMixer;
        bgmGroup = bgm;
        npcVoiceGroup = npcVoice;
        buttonSfxGroup = buttonSfx;
        battleSfxGroup = battleSfx;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

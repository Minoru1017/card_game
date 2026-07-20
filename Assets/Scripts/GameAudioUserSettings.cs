using System;
using UnityEngine;

/// <summary>Settings 場景總開關＋四軌音量（PlayerPrefs），供 BGM／NPC 語音／按鈕／戰鬥音效讀寫。</summary>
public static class GameAudioUserSettings
{
    public const float DefaultVolume = 1f;

    private const string MasterEnabledKey = "audio_master_enabled";
    private const string BgmVolumeKey = "audio_bgm_volume";
    private const string NpcVoiceVolumeKey = "audio_npc_voice_volume";
    private const string ButtonSfxVolumeKey = "audio_button_sfx_volume";
    private const string BattleSfxVolumeKey = "audio_battle_sfx_volume";

    public static event Action VolumeChanged;

    public static bool IsMasterEnabled() => PlayerPrefs.GetInt(MasterEnabledKey, 1) != 0;

    public static void SetMasterEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MasterEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        NotifyVolumeChanged();
    }

    public static float GetBgmVolume() => ReadVolume(BgmVolumeKey);

    public static float GetNpcVoiceVolume() => ReadVolume(NpcVoiceVolumeKey);

    public static float GetButtonSfxVolume() => ReadVolume(ButtonSfxVolumeKey);

    public static float GetBattleSfxVolume() => ReadVolume(BattleSfxVolumeKey);

    public static void SetBgmVolume(float volume)
    {
        WriteVolume(BgmVolumeKey, volume);
        NotifyVolumeChanged();
    }

    public static void SetNpcVoiceVolume(float volume)
    {
        WriteVolume(NpcVoiceVolumeKey, volume);
        NotifyVolumeChanged();
    }

    public static void SetButtonSfxVolume(float volume)
    {
        WriteVolume(ButtonSfxVolumeKey, volume);
        NotifyVolumeChanged();
    }

    public static void SetBattleSfxVolume(float volume)
    {
        WriteVolume(BattleSfxVolumeKey, volume);
        NotifyVolumeChanged();
    }

    public static float ScaleBgm(float baseVolume) =>
        ScaleChannelVolume(GameAudioChannel.Bgm, baseVolume);

    public static float ScaleNpcVoice(float baseVolume) =>
        ScaleChannelVolume(GameAudioChannel.NpcVoice, baseVolume);

    public static float ScaleButtonSfx(float baseVolume) =>
        ScaleChannelVolume(GameAudioChannel.ButtonSfx, baseVolume);

    public static float ScaleBattleSfx(float baseVolume) =>
        ScaleChannelVolume(GameAudioChannel.BattleSfx, baseVolume);

    /// <summary>Mixer 啟用時只保留本地音量；否則乘上 Settings 滑桿。</summary>
    public static float ScaleChannelVolume(GameAudioChannel channel, float baseVolume)
    {
        if (!IsMasterEnabled())
            return 0f;

        if (GameAudioMixerCatalog.IsActive)
            return Mathf.Clamp01(baseVolume);

        switch (channel)
        {
            case GameAudioChannel.Bgm:
                return ScaleChannel(baseVolume, GetBgmVolume());
            case GameAudioChannel.NpcVoice:
                return ScaleChannel(baseVolume, GetNpcVoiceVolume());
            case GameAudioChannel.ButtonSfx:
                return ScaleChannel(baseVolume, GetButtonSfxVolume());
            case GameAudioChannel.BattleSfx:
                return ScaleChannel(baseVolume, GetBattleSfxVolume());
            default:
                return Mathf.Clamp01(baseVolume);
        }
    }

    private static float ScaleChannel(float baseVolume, float channelVolume)
    {
        if (!IsMasterEnabled())
            return 0f;
        return Mathf.Clamp01(baseVolume) * channelVolume;
    }

    public static void NotifyVolumeChanged()
    {
        GameAudioMixerCatalog.ApplyUserSettings();
        VolumeChanged?.Invoke();
    }

    public static void RefreshActiveBgmVolumes()
    {
        GameAudioMixerCatalog.ApplyUserSettings();
        HallBackgroundMusicPlayer[] hall = UnityEngine.Object.FindObjectsByType<HallBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < hall.Length; i++)
            hall[i].ApplyUserBgmVolume();

        BuildbeckBackgroundMusicPlayer[] buildbeck =
            UnityEngine.Object.FindObjectsByType<BuildbeckBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < buildbeck.Length; i++)
            buildbeck[i].ApplyUserBgmVolume();

        CardStoreBackgroundMusicPlayer[] cardStore =
            UnityEngine.Object.FindObjectsByType<CardStoreBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < cardStore.Length; i++)
            cardStore[i].ApplyUserBgmVolume();

        StoryProgressBackgroundMusicPlayer[] story =
            UnityEngine.Object.FindObjectsByType<StoryProgressBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < story.Length; i++)
            story[i].ApplyUserBgmVolume();

        TutorialBattleBackgroundMusicPlayer[] battle =
            UnityEngine.Object.FindObjectsByType<TutorialBattleBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < battle.Length; i++)
            battle[i].ApplyUserBgmVolume();

        FreeBattleBackgroundMusicPlayer[] freeBattle =
            UnityEngine.Object.FindObjectsByType<FreeBattleBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < freeBattle.Length; i++)
            freeBattle[i].ApplyUserBgmVolume();

        PlotBackgroundMusicPlayer[] plot =
            UnityEngine.Object.FindObjectsByType<PlotBackgroundMusicPlayer>(FindObjectsSortMode.None);
        for (int i = 0; i < plot.Length; i++)
            plot[i].ApplyUserBgmVolume();

        FightingBirdGameSceneController[] bird =
            UnityEngine.Object.FindObjectsByType<FightingBirdGameSceneController>(FindObjectsSortMode.None);
        for (int i = 0; i < bird.Length; i++)
            bird[i].ApplyUserBgmVolume();
    }

    private static float ReadVolume(string key) =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));

    private static void WriteVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }
}

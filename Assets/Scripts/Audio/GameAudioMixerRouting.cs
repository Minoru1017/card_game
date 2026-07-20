using UnityEngine;

/// <summary>AudioSource 分軌接線與本地音量（Mixer 啟用時由 group 控制玩家設定）。</summary>
public static class GameAudioMixerRouting
{
    public static void ConfigureSource(AudioSource source, GameAudioChannel channel)
    {
        if (source == null)
            return;

        GameAudioMixerCatalog.Bind(source, channel);
    }

    public static float ResolveLocalVolume(GameAudioChannel channel, float localVolume) =>
        GameAudioUserSettings.ScaleChannelVolume(channel, localVolume);
}

using UnityEngine;

/// <summary>A-1 overlay 語音：走 AudioLibrary，無 clip 時靜默。</summary>
public static class SideQuestA1OverlayVoice
{
    private static AudioSource source;

    public static void Play(string clipId)
    {
        if (string.IsNullOrWhiteSpace(clipId))
            return;

        AudioLibrary library = AudioLibrary.Instance;
        if (library == null)
            return;

        AudioClip clip = library.GetVoice(clipId.Trim());
        if (clip == null)
            return;

        EnsureSource();
        source.Stop();
        source.clip = clip;
        source.volume = GameAudioUserSettings.ScaleNpcVoice(1f);
        source.Play();
    }

    public static void Stop()
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }

    private static void EnsureSource()
    {
        if (source != null)
            return;

        GameObject host = new GameObject("SideQuestA1OverlayVoice");
        Object.DontDestroyOnLoad(host);
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }
}

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>致死預警「You will die」圖示出現時播放的音效。</summary>
public static class YouWillDieWarningSfx
{
    public const string ResourcesPath = "Music/You will die";

#if UNITY_EDITOR
    public const string AssetPath = "Assets/Music/You will die.mp3";
#endif

    public const float DefaultVolume = 1f;

    private static AudioClip cachedClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCachedClipOnPlayModeEnter()
    {
        cachedClip = null;
    }

    public static void Play()
    {
        if (BattleAutoSimPlugin.IsRunning)
            return;

        MonoBehaviour host = Object.FindFirstObjectByType<BattleSimulationDebugUI>();
        if (host == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                host = cam.GetComponent<BattleSimulationDebugUI>();
        }

        if (host == null)
            return;

        AudioClip clip = ResolveClip();
        if (clip == null)
        {
            Debug.LogWarning("YouWillDieWarningSfx: 找不到 You will die 音效。");
            return;
        }

        AudioSource source = EnsureSource(host);
        if (source == null)
            return;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        source.PlayOneShot(clip, GameAudioUserSettings.ScaleBattleSfx(DefaultVolume));
    }

    public static AudioClip ResolveClip()
    {
        if (cachedClip != null)
            return cachedClip;

        cachedClip = Resources.Load<AudioClip>(ResourcesPath);

#if UNITY_EDITOR
        if (cachedClip == null)
            cachedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetPath);
#endif
        return cachedClip;
    }

    private static AudioSource EnsureSource(MonoBehaviour host)
    {
        Transform child = host.transform.Find("YouWillDieWarningSfxSource");
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null)
                return existing;
        }

        var sourceGo = new GameObject("YouWillDieWarningSfxSource");
        sourceGo.transform.SetParent(host.transform, false);
        AudioSource source = sourceGo.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 64;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.ignoreListenerPause = true;
        return source;
    }
}

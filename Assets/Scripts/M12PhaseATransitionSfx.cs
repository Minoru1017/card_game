using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>M-1-2 段考 A：第 6 回合最後一次攻擊時播放的轉場音效（1-2 Transition）。</summary>
public static class M12PhaseATransitionSfx
{
    public const string ResourcesPath = "Music/1-2 Transition";

#if UNITY_EDITOR
    public const string AssetPath = "Assets/Music/1-2 Transition.mp3";
#endif

    public const float DefaultVolume = 1f;

    private static AudioClip cachedClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCachedClipOnPlayModeEnter()
    {
        cachedClip = null;
    }

    public static void Play(MonoBehaviour host)
    {
        if (host == null || BattleAutoSimPlugin.IsRunning)
            return;

        AudioClip clip = ResolveClip();
        if (clip == null)
        {
            Debug.LogWarning("M12PhaseATransitionSfx: 找不到 1-2 Transition 音效。");
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
        Transform child = host.transform.Find("M12PhaseATransitionSfxSource");
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null)
                return existing;
        }

        var sourceGo = new GameObject("M12PhaseATransitionSfxSource");
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

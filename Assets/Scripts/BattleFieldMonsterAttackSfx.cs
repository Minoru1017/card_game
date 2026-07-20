using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>戰鬥中場地牌攻擊命中時使用的 Monster Card Attack 音效。</summary>
public static class BattleFieldMonsterAttackSfx
{
    public const string ResourcesPath = "Music/Monster Card Attack";

#if UNITY_EDITOR
    public const string AssetPath = "Assets/Music/Monster Card Attack.mp3";
#endif

    public const float DefaultVolume = 0.92f;

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
            Debug.LogWarning("BattleFieldMonsterAttackSfx: 找不到 Monster Card Attack 音效。");
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

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null && library.MonsterCardAttackSfx != null)
            cachedClip = library.MonsterCardAttackSfx;

        if (cachedClip == null)
            cachedClip = Resources.Load<AudioClip>(ResourcesPath);

#if UNITY_EDITOR
        if (cachedClip == null)
            cachedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetPath);
#endif
        return cachedClip;
    }

#if UNITY_EDITOR
    /// <summary>音檔更新後由 Editor 工具呼叫，避免 Play Mode 內沿用舊 clip 參考。</summary>
    public static void EditorInvalidateCachedClip()
    {
        cachedClip = null;
    }
#endif

    private static AudioSource EnsureSource(MonoBehaviour host)
    {
        Transform child = host.transform.Find("BattleFieldAttackSfxSource");
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null)
                return existing;
        }

        var sourceGo = new GameObject("BattleFieldAttackSfxSource");
        sourceGo.transform.SetParent(host.transform, false);
        AudioSource source = sourceGo.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 64;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.ignoreListenerPause = true;
        GameAudioMixerRouting.ConfigureSource(source, GameAudioChannel.BattleSfx);
        return source;
    }
}

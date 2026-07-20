using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>戰鬥中場地牌反擊時使用的 Monster Card Counterattack 音效（單檔多段，每次隨機片段）。</summary>
public static class BattleFieldMonsterCounterattackSfx
{
    public const string ResourcesPath = "Music/Monster Card Counterattack";

#if UNITY_EDITOR
    public const string AssetPath = "Assets/Music/Monster Card Counterattack.mp3";
#endif

    public const float DefaultVolume = 1.12f;

    /// <summary>估算每段反擊音效長度，用於從合輯音檔中切換不同段落。</summary>
    private const float EstimatedVariantSeconds = 0.42f;

    private const float MinPlaySeconds = 0.28f;
    private const float MaxPlaySeconds = 0.62f;

    private static AudioClip cachedClip;
    private static int playGeneration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCachedClipOnPlayModeEnter()
    {
        cachedClip = null;
        playGeneration = 0;
    }

    public static void Play(MonoBehaviour host)
    {
        if (host == null || BattleAutoSimPlugin.IsRunning)
            return;

        AudioClip clip = ResolveClip();
        if (clip == null)
        {
            Debug.LogWarning("BattleFieldMonsterCounterattackSfx: 找不到 Monster Card Counterattack 音效。");
            return;
        }

        host.StartCoroutine(CoPlayRandomVariant(host, clip));
    }

    public static AudioClip ResolveClip()
    {
        if (cachedClip != null)
            return cachedClip;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null && library.MonsterCardCounterattackSfx != null)
            cachedClip = library.MonsterCardCounterattackSfx;

        if (cachedClip == null)
            cachedClip = Resources.Load<AudioClip>(ResourcesPath);

#if UNITY_EDITOR
        if (cachedClip == null)
            cachedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetPath);
#endif
        return cachedClip;
    }

#if UNITY_EDITOR
    public static void EditorInvalidateCachedClip()
    {
        cachedClip = null;
    }
#endif

    private static IEnumerator CoPlayRandomVariant(MonoBehaviour host, AudioClip clip)
    {
        yield return CoEnsureClipLoaded(clip);
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("BattleFieldMonsterCounterattackSfx: clip 尚未載入完成。");
            yield break;
        }

        AudioSource source = EnsureSource(host);
        if (source == null)
            yield break;

        int myGeneration = ++playGeneration;
        float scaledVolume = GameAudioUserSettings.ScaleBattleSfx(DefaultVolume);

        if (clip.length <= MaxPlaySeconds + 0.15f)
        {
            source.pitch = Random.Range(0.94f, 1.06f);
            source.PlayOneShot(clip, scaledVolume);
            source.pitch = 1f;
            yield break;
        }

        float playSeconds = Random.Range(MinPlaySeconds, MaxPlaySeconds);
        float startTime = PickRandomVariantStartSeconds(clip, playSeconds);
        playSeconds = Mathf.Min(playSeconds, Mathf.Max(0.08f, clip.length - startTime));

        source.clip = clip;
        source.volume = scaledVolume;
        source.time = startTime;
        source.Play();

        yield return null;

        if (!source.isPlaying)
        {
            source.pitch = Random.Range(0.94f, 1.06f);
            source.PlayOneShot(clip, scaledVolume);
            source.pitch = 1f;
            yield break;
        }

        float endAt = Time.unscaledTime + playSeconds;
        while (Time.unscaledTime < endAt)
            yield return null;

        if (myGeneration == playGeneration && source.clip == clip && source.isPlaying)
            source.Stop();
    }

    private static IEnumerator CoEnsureClipLoaded(AudioClip clip)
    {
        if (clip.loadState == AudioDataLoadState.Loaded)
            yield break;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        for (int i = 0; i < 120 && clip.loadState == AudioDataLoadState.Loading; i++)
            yield return null;
    }

    private static float PickRandomVariantStartSeconds(AudioClip clip, float playSeconds)
    {
        if (clip.length <= playSeconds + 0.05f)
            return 0f;

        int variantCount = Mathf.Max(1, Mathf.RoundToInt(clip.length / EstimatedVariantSeconds));
        int slot = Random.Range(0, variantCount);
        float slotLength = clip.length / variantCount;
        float slotStart = slot * slotLength;
        float slotEnd = slot == variantCount - 1 ? clip.length : slotStart + slotLength;
        float innerStart = slotStart + slotLength * 0.08f;
        float innerEnd = Mathf.Max(innerStart, slotEnd - playSeconds - slotLength * 0.08f);
        if (innerEnd <= innerStart)
            return Mathf.Clamp(slotStart, 0f, Mathf.Max(0f, clip.length - playSeconds));

        return Random.Range(innerStart, innerEnd);
    }

    private static AudioSource EnsureSource(MonoBehaviour host)
    {
        Transform child = host.transform.Find("BattleFieldCounterattackSfxSource");
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null)
                return existing;
        }

        var sourceGo = new GameObject("BattleFieldCounterattackSfxSource");
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

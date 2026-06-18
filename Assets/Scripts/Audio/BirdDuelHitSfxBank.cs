using UnityEngine;

/// <summary>
/// 鬥鳥節奏按鈕打擊音效：自 <c>Open Hi Hat 4 Bars 100 Bpm</c> 依拍點切片，
/// 第一小節四拍分別對應 Perfect／Good／Guard／Miss。
/// </summary>
public sealed class BirdDuelHitSfxBank
{
    public const string SourceAssetPath = "Assets/SFX/Open Hi Hat 4 Bars 100 Bpm.mp3";
    public const float SourceBpm = 100f;
    public const int BeatsPerBar = 4;

    private const int PerfectBeatIndex = 0;
    private const int GoodBeatIndex = 1;
    private const int GuardBeatIndex = 2;
    private const int MissBeatIndex = 3;
    private const float SliceLengthBeats = 0.42f;

    private readonly AudioClip perfectClip;
    private readonly AudioClip goodClip;
    private readonly AudioClip guardClip;
    private readonly AudioClip missClip;

    private BirdDuelHitSfxBank(AudioClip perfect, AudioClip good, AudioClip guard, AudioClip miss)
    {
        perfectClip = perfect;
        goodClip = good;
        guardClip = guard;
        missClip = miss;
    }

    public bool IsReady =>
        perfectClip != null && goodClip != null && guardClip != null && missClip != null;

    public static BirdDuelHitSfxBank TryCreate(AudioClip source)
    {
        if (source == null)
            return null;

        if (source.loadState != AudioDataLoadState.Loaded)
            source.LoadAudioData();

        if (source.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning("BirdDuelHitSfxBank: 無法載入打擊音效母帶 → " + source.name);
            return null;
        }

        float secondsPerBeat = 60f / SourceBpm;
        float sliceSeconds = secondsPerBeat * SliceLengthBeats;

        AudioClip perfect = SliceBeat(source, PerfectBeatIndex, secondsPerBeat, sliceSeconds, "BirdDuelHit_Perfect");
        AudioClip good = SliceBeat(source, GoodBeatIndex, secondsPerBeat, sliceSeconds, "BirdDuelHit_Good");
        AudioClip guard = SliceBeat(source, GuardBeatIndex, secondsPerBeat, sliceSeconds, "BirdDuelHit_Guard");
        AudioClip miss = SliceBeat(source, MissBeatIndex, secondsPerBeat, sliceSeconds, "BirdDuelHit_Miss");

        if (perfect == null || good == null || guard == null || miss == null)
        {
            ReleaseClip(ref perfect);
            ReleaseClip(ref good);
            ReleaseClip(ref guard);
            ReleaseClip(ref miss);
            return null;
        }

        return new BirdDuelHitSfxBank(perfect, good, guard, miss);
    }

    public AudioClip ResolveClip(BirdBeatOutcome outcome)
    {
        switch (outcome)
        {
            case BirdBeatOutcome.Perfect: return perfectClip;
            case BirdBeatOutcome.Good: return goodClip;
            case BirdBeatOutcome.Guard: return guardClip;
            default: return missClip;
        }
    }

    public static float ResolveVolume(BirdBeatOutcome outcome)
    {
        switch (outcome)
        {
            case BirdBeatOutcome.Perfect: return 1f;
            case BirdBeatOutcome.Good: return 0.88f;
            case BirdBeatOutcome.Guard: return 0.76f;
            default: return 0.68f;
        }
    }

    public void Release()
    {
        DestroyClip(perfectClip);
        DestroyClip(goodClip);
        DestroyClip(guardClip);
        DestroyClip(missClip);
    }

    private static AudioClip SliceBeat(
        AudioClip source,
        int beatIndex,
        float secondsPerBeat,
        float sliceSeconds,
        string clipName)
    {
        int channels = Mathf.Max(1, source.channels);
        int sampleRate = source.frequency;
        int startSample = Mathf.RoundToInt(beatIndex * secondsPerBeat * sampleRate);
        int sliceSamples = Mathf.Max(1, Mathf.RoundToInt(sliceSeconds * sampleRate));
        int totalSamples = source.samples;

        if (startSample >= totalSamples)
        {
            Debug.LogWarning("BirdDuelHitSfxBank: 拍點超出母帶長度 beat=" + beatIndex + " clip=" + source.name);
            return null;
        }

        sliceSamples = Mathf.Min(sliceSamples, totalSamples - startSample);
        var buffer = new float[sliceSamples * channels];
        var window = new float[totalSamples * channels];
        if (!source.GetData(window, 0))
            return null;

        int copyCount = sliceSamples * channels;
        int srcOffset = startSample * channels;
        for (int i = 0; i < copyCount; i++)
            buffer[i] = window[srcOffset + i];

        AudioClip slice = AudioClip.Create(clipName, sliceSamples, channels, sampleRate, false);
        slice.SetData(buffer, 0);
        return slice;
    }

    private static void ReleaseClip(ref AudioClip clip)
    {
        if (clip == null)
            return;
        Object.Destroy(clip);
        clip = null;
    }

    private static void DestroyClip(AudioClip clip)
    {
        if (clip != null)
            Object.Destroy(clip);
    }
}

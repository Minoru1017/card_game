using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 量測鬥鳥 BGM 的 BPM 與第一下拍偏移，寫入 <c>Assets/Resources/BirdDuelRhythmSync.asset</c>。
/// 庭訓進行曲（Risian - Stampede）額外標記 8／12 分音小節交替格。
/// 選單：Tools/Audio/Analyze Bird Duel BGM Tempo
/// </summary>
public static class BirdDuelBgmTempoAnalyzer
{
    private const string SyncAssetPath = "Assets/Resources/BirdDuelRhythmSync.asset";
    private const string CourtMarchCdId = "court_march";
    private const float MinBpm = 80f;
    private const float MaxBpm = 170f;
    private const int Win = 1024;
    private const int Hop = 512;

    [MenuItem("Tools/Audio/Analyze Bird Duel BGM Tempo (Default / Come Again)")]
    public static void AnalyzeDefault()
    {
        AnalyzeClip(
            FightingBirdGameSceneController.ComeAgainAssetPath,
            writeDefault: true,
            cdId: null,
            gridMode: BirdDuelRhythmSync.GridMode.QuarterBeat);
    }

    [MenuItem("Tools/Audio/Analyze Bird Duel BGM Tempo (Court March / Stampede)")]
    public static void AnalyzeCourtMarch()
    {
        AnalyzeClip(
            FightingBirdGameSceneController.StampedeAssetPath,
            writeDefault: false,
            cdId: CourtMarchCdId,
            gridMode: BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth);
    }

    private static void AnalyzeClip(
        string clipPath,
        bool writeDefault,
        string cdId,
        BirdDuelRhythmSync.GridMode gridMode)
    {
        var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
        if (importer == null)
        {
            Debug.LogError("BirdDuelBgmTempoAnalyzer: 找不到 BGM 匯入器 → " + clipPath);
            return;
        }

        AudioImporterSampleSettings original = importer.defaultSampleSettings;
        bool originalForceMono = importer.forceToMono;
        bool needRestore = original.loadType != AudioClipLoadType.DecompressOnLoad
            || original.compressionFormat != AudioCompressionFormat.PCM
            || !originalForceMono;

        if (needRestore)
        {
            AudioImporterSampleSettings temp = original;
            temp.loadType = AudioClipLoadType.DecompressOnLoad;
            temp.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = temp;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        try
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                Debug.LogError("BirdDuelBgmTempoAnalyzer: 載入 AudioClip 失敗。");
                return;
            }

            clip.LoadAudioData();
            int channels = Mathf.Max(1, clip.channels);
            int sr = clip.frequency;
            float[] raw = new float[clip.samples * channels];
            if (!clip.GetData(raw, 0))
            {
                Debug.LogError("BirdDuelBgmTempoAnalyzer: GetData 失敗（請確認匯入為 Decompress On Load）。");
                return;
            }

            float[] mono = ToMono(raw, clip.samples, channels);
            float framesPerSec = (float)sr / Hop;
            float[] onset = OnsetEnvelope(mono);
            if (onset.Length < 8)
            {
                Debug.LogError("BirdDuelBgmTempoAnalyzer: 音檔過短，無法分析。");
                return;
            }

            float bpm = EstimateBpm(onset, framesPerSec);
            float offset = EstimateFirstDownbeatOffset(onset, framesPerSec, bpm);
            float eighthScore = ScoreSubdivision(onset, framesPerSec, bpm, 2);
            float twelfthScore = ScoreSubdivision(onset, framesPerSec, bpm, 3);

            float referencePerformanceSeconds = MeasureReferencePerformanceSeconds();
            float clipSeconds = clip.samples / (float)Mathf.Max(1, clip.frequency);
            float loopStart = 0f;
            float loopLength = 0f;
            if (!writeDefault)
            {
                loopLength = referencePerformanceSeconds;
                loopStart = EstimateBestLoopStart(onset, framesPerSec, loopLength, clipSeconds);
            }

            Debug.Log(
                $"[BirdDuel] 量測 {clipPath}：BPM≈{bpm:F1}，第一下拍≈{offset:F3}s；" +
                $"8分格={eighthScore:F1}，12分格={twelfthScore:F1}；" +
                $"演奏循環={loopLength:F2}s @ {loopStart:F2}s。");

            if (writeDefault)
                WriteDefaultSync(bpm, offset);
            else
                WriteCdEntry(cdId, bpm, offset, gridMode, loopStart, loopLength);
        }
        finally
        {
            if (needRestore)
            {
                importer.defaultSampleSettings = original;
                importer.forceToMono = originalForceMono;
                importer.SaveAndReimport();
            }
        }
    }

    /// <summary>8 分音=每拍 2 格；12 分音=每拍 3 格（一節循環由執行期交替）。</summary>
    private static float ScoreSubdivision(float[] onset, float framesPerSec, float bpm, int divisionsPerBeat)
    {
        float secPerBeat = 60f / bpm;
        int periodFrames = Mathf.Max(1, Mathf.RoundToInt(secPerBeat * framesPerSec / divisionsPerBeat));
        double sum = 0d;
        for (int p = 0; p < periodFrames; p++)
        {
            for (int f = p; f < onset.Length; f += periodFrames)
                sum += onset[f];
        }
        return (float)sum;
    }

    private static float[] ToMono(float[] raw, int samples, int channels)
    {
        if (channels <= 1)
            return raw;

        float[] mono = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float sum = 0f;
            int baseIdx = i * channels;
            for (int c = 0; c < channels; c++)
                sum += raw[baseIdx + c];
            mono[i] = sum / channels;
        }
        return mono;
    }

    private static float[] OnsetEnvelope(float[] mono)
    {
        int frames = Mathf.Max(0, (mono.Length - Win) / Hop);
        float[] energy = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            int start = f * Hop;
            double e = 0d;
            for (int j = 0; j < Win; j++)
            {
                float v = mono[start + j];
                e += v * v;
            }
            energy[f] = (float)Math.Sqrt(e / Win);
        }

        float[] onset = new float[frames];
        for (int f = 1; f < frames; f++)
        {
            float d = energy[f] - energy[f - 1];
            onset[f] = d > 0f ? d : 0f;
        }
        return onset;
    }

    private static float EstimateBpm(float[] onset, float framesPerSec)
    {
        int minLag = Mathf.Max(1, Mathf.RoundToInt(framesPerSec * 60f / MaxBpm));
        int maxLag = Mathf.RoundToInt(framesPerSec * 60f / MinBpm);
        maxLag = Mathf.Min(maxLag, onset.Length - 1);

        double best = -1d;
        int bestLag = minLag;
        for (int lag = minLag; lag <= maxLag; lag++)
        {
            double s = 0d;
            for (int f = lag; f < onset.Length; f++)
                s += onset[f] * (double)onset[f - lag];
            if (s > best)
            {
                best = s;
                bestLag = lag;
            }
        }

        float bpm = 60f * framesPerSec / bestLag;
        while (bpm < 90f) bpm *= 2f;
        while (bpm > 180f) bpm /= 2f;
        return bpm;
    }

    private static float EstimateFirstDownbeatOffset(float[] onset, float framesPerSec, float bpm)
    {
        float secPerBeat = 60f / bpm;
        int periodFrames = Mathf.Max(1, Mathf.RoundToInt(secPerBeat * framesPerSec));

        double best = -1d;
        int bestPhase = 0;
        for (int p = 0; p < periodFrames; p++)
        {
            double s = 0d;
            for (int f = p; f < onset.Length; f += periodFrames)
                s += onset[f];
            if (s > best)
            {
                best = s;
                bestPhase = p;
            }
        }

        float offset = bestPhase / framesPerSec;
        return offset % secPerBeat;
    }

    private static float MeasureReferencePerformanceSeconds()
    {
        var reference = AssetDatabase.LoadAssetAtPath<AudioClip>(FightingBirdGameSceneController.ComeAgainAssetPath);
        if (reference != null && reference.length > 0.01f)
            return reference.length;
        return BirdDuelRhythmSync.HarborPracticePerformanceSeconds;
    }

    /// <summary>在 onset 包絡中找與港灣練習帶等長、鼓點最密集的起點。</summary>
    private static float EstimateBestLoopStart(
        float[] onset,
        float framesPerSec,
        float loopLengthSeconds,
        float clipLengthSeconds)
    {
        if (onset.Length < 8 || loopLengthSeconds <= 0.01f)
            return 0f;

        int windowFrames = Mathf.Max(1, Mathf.RoundToInt(loopLengthSeconds * framesPerSec));
        windowFrames = Mathf.Min(windowFrames, onset.Length);
        int maxStart = Mathf.Max(1, onset.Length - windowFrames);

        double bestScore = -1d;
        int bestStartFrame = 0;
        for (int start = 0; start < maxStart; start++)
        {
            double sum = 0d;
            for (int f = start; f < start + windowFrames; f++)
                sum += onset[f];
            if (sum > bestScore)
            {
                bestScore = sum;
                bestStartFrame = start;
            }
        }

        float startSec = bestStartFrame / framesPerSec;
        float maxStartSec = Mathf.Max(0f, clipLengthSeconds - loopLengthSeconds);
        return Mathf.Clamp(startSec, 0f, maxStartSec);
    }

    private static void WriteDefaultSync(float bpm, float offset)
    {
        var sync = LoadOrCreateSyncAsset();
        sync.EditorSet(Mathf.Round(bpm * 10f) / 10f, offset);
        SaveSync(sync, "預設（港灣練習帶）");
    }

    private static void WriteCdEntry(
        string cdId,
        float bpm,
        float offset,
        BirdDuelRhythmSync.GridMode gridMode,
        float loopStartSeconds,
        float loopLengthSeconds)
    {
        var sync = LoadOrCreateSyncAsset();
        var entry = sync.EditorTryGetCdEntry(cdId, out var existing)
            ? existing
            : CreateDefaultCourtMarchDifficultyEntry(cdId);
        entry.cdId = cdId;
        entry.bpm = Mathf.Round(bpm * 10f) / 10f;
        entry.firstDownbeatOffset = offset;
        entry.gridMode = gridMode;
        entry.bgmLoopStartSeconds = Mathf.Max(0f, loopStartSeconds);
        entry.bgmLoopLengthSeconds = Mathf.Max(0f, loopLengthSeconds);
        sync.EditorSetCdEntry(entry);
        SaveSync(sync, "CD=" + cdId);
    }

    /// <summary>庭訓進行曲：比港灣練習帶略難（量測 BPM 後仍保留此梯度）。</summary>
    private static BirdDuelRhythmSync.CdEntry CreateDefaultCourtMarchDifficultyEntry(string cdId) =>
        new BirdDuelRhythmSync.CdEntry
        {
            cdId = cdId,
            decisivePerfectWindowMul = 0.68f,
            decisiveGoodWindowMul = 0.73f,
            decisiveTelegraphLeadMul = 0.55f,
            basePerfectWindowMul = 0.92f,
            baseGoodWindowMul = 0.94f,
            baseTelegraphLeadMul = 0.88f,
            closeToWinScoreMargin = 5,
            normalBeatsPerStep = 3,
            decisiveMinTriplets = 5,
            decisiveMaxTriplets = 20
        };

    private static BirdDuelRhythmSync LoadOrCreateSyncAsset()
    {
        var sync = AssetDatabase.LoadAssetAtPath<BirdDuelRhythmSync>(SyncAssetPath);
        if (sync != null)
            return sync;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        sync = ScriptableObject.CreateInstance<BirdDuelRhythmSync>();
        AssetDatabase.CreateAsset(sync, SyncAssetPath);
        return sync;
    }

    private static void SaveSync(BirdDuelRhythmSync sync, string label)
    {
        EditorUtility.SetDirty(sync);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BirdDuel] 已寫入 " + SyncAssetPath + "（" + label + "）。");
    }
}

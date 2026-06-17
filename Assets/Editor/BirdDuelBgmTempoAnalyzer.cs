using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 量測鬥鳥 BGM（feinsmecker - Come Again）的 BPM 與第一下拍偏移，寫入
/// <c>Assets/Resources/BirdDuelRhythmSync.asset</c>，供執行期對齊節拍。
///
/// 作法：暫時把音檔匯入設為 PCM / Decompress On Load 以讀取 PCM 取樣，
/// 計算能量包絡的 onset 強度 → 自相關求週期（BPM）→ 相位掃描求第一下拍偏移，量測後還原匯入設定。
/// 對於有清楚鼓點的曲目（如此曲）相當穩定；數值可於 BirdDuelRhythmSync 資產微調。
/// 選單：Tools/Audio/Analyze Bird Duel BGM Tempo
/// </summary>
public static class BirdDuelBgmTempoAnalyzer
{
    private const string SyncAssetPath = "Assets/Resources/BirdDuelRhythmSync.asset";
    private const float MinBpm = 80f;
    private const float MaxBpm = 170f;
    private const int Win = 1024;
    private const int Hop = 512;

    [MenuItem("Tools/Audio/Analyze Bird Duel BGM Tempo")]
    public static void Analyze()
    {
        string clipPath = FightingBirdGameSceneController.ComeAgainAssetPath;
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

            Debug.Log($"[BirdDuel] 量測 BGM：BPM≈{bpm:F1}，第一下拍偏移≈{offset:F3}s（每拍 {60f / bpm:F3}s）。");
            WriteSyncAsset(bpm, offset);
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

    /// <summary>能量包絡的半波整流差分作為 onset 強度（frame 率 = sampleRate / Hop，由呼叫端計算）。</summary>
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
        while (bpm < 90f) bpm *= 2f;   // 折回常見範圍，避免抓到半速
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

    private static void WriteSyncAsset(float bpm, float offset)
    {
        var sync = AssetDatabase.LoadAssetAtPath<BirdDuelRhythmSync>(SyncAssetPath);
        if (sync == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            sync = ScriptableObject.CreateInstance<BirdDuelRhythmSync>();
            AssetDatabase.CreateAsset(sync, SyncAssetPath);
        }

        sync.EditorSet(Mathf.Round(bpm * 10f) / 10f, offset);
        EditorUtility.SetDirty(sync);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BirdDuel] 已寫入 " + SyncAssetPath + "，執行期將自動套用對拍設定。");
    }
}

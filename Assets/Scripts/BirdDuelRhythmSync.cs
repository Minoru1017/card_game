using UnityEngine;

/// <summary>
/// 鬥鳥節奏對拍設定：BGM 的 BPM、第一下拍偏移，以及可選的 8／12 分音小節交替格。
/// 由 Editor 量測工具寫入 Resources；執行期依所選 CD 解析。
/// 難度梯度：港灣練習帶（預設列）最簡單 → 庭訓進行曲略難。
/// </summary>
[CreateAssetMenu(fileName = "BirdDuelRhythmSync", menuName = "Card Game/Bird Duel Rhythm Sync", order = 1)]
public sealed class BirdDuelRhythmSync : ScriptableObject
{
    public const string ResourcesPath = "BirdDuelRhythmSync";
    public const float DefaultBpm = 120f;
    public const float DefaultFirstDownbeatOffset = 0f;
    /// <summary>港灣練習帶（Come Again）量測演奏長度；其他 CD 循環區以此為基準。</summary>
    public const float HarborPracticePerformanceSeconds = 45.348571428571425f;

    public const int HarborCloseToWinScoreMargin = 4;
    public const int HarborNormalBeatsPerStep = 4;
    public const int HarborDecisiveMinTriplets = 6;
    public const int HarborDecisiveMaxTriplets = 24;

    /// <summary>節拍采音格：整拍，或 8 分／12 分音小節交替。</summary>
    public enum GridMode
    {
        QuarterBeat = 0,
        AlternatingEighthTwelfth = 1
    }

    [System.Serializable]
    public struct CdEntry
    {
        public string cdId;
        [Range(40f, 220f)] public float bpm;
        [Min(0f)] public float firstDownbeatOffset;
        public GridMode gridMode;
        [Range(0.45f, 1f)] public float decisivePerfectWindowMul;
        [Range(0.45f, 1f)] public float decisiveGoodWindowMul;
        [Range(0.45f, 1f)] public float decisiveTelegraphLeadMul;
        [Min(0f)] public float bgmLoopStartSeconds;
        [Min(0f)] public float bgmLoopLengthSeconds;
        [Header("難度（0＝沿用港灣預設）")]
        [Range(0.65f, 1.05f)] public float basePerfectWindowMul;
        [Range(0.65f, 1.05f)] public float baseGoodWindowMul;
        [Range(0.65f, 1.05f)] public float baseTelegraphLeadMul;
        [Range(2, 10)] public int closeToWinScoreMargin;
        [Range(2, 8)] public int normalBeatsPerStep;
        [Range(3, 18)] public int decisiveMinTriplets;
        [Range(6, 30)] public int decisiveMaxTriplets;
    }

    /// <summary>執行期解析結果（值型，避免到處傳 ScriptableObject）。</summary>
    public readonly struct Profile
    {
        public float Bpm { get; }
        public float FirstDownbeatOffset { get; }
        public GridMode Grid { get; }
        public float DecisivePerfectWindowMul { get; }
        public float DecisiveGoodWindowMul { get; }
        public float DecisiveTelegraphLeadMul { get; }
        public float BgmLoopStartSeconds { get; }
        public float BgmLoopLengthSeconds { get; }
        public float BasePerfectWindowMul { get; }
        public float BaseGoodWindowMul { get; }
        public float BaseTelegraphLeadMul { get; }
        public int CloseToWinScoreMargin { get; }
        public int NormalBeatsPerStep { get; }
        public int DecisiveMinTriplets { get; }
        public int DecisiveMaxTriplets { get; }

        public Profile(
            float bpm,
            float firstDownbeatOffset,
            GridMode grid,
            float decisivePerfectWindowMul,
            float decisiveGoodWindowMul,
            float decisiveTelegraphLeadMul,
            float bgmLoopStartSeconds,
            float bgmLoopLengthSeconds,
            float basePerfectWindowMul,
            float baseGoodWindowMul,
            float baseTelegraphLeadMul,
            int closeToWinScoreMargin,
            int normalBeatsPerStep,
            int decisiveMinTriplets,
            int decisiveMaxTriplets)
        {
            Bpm = bpm;
            FirstDownbeatOffset = firstDownbeatOffset;
            Grid = grid;
            DecisivePerfectWindowMul = decisivePerfectWindowMul;
            DecisiveGoodWindowMul = decisiveGoodWindowMul;
            DecisiveTelegraphLeadMul = decisiveTelegraphLeadMul;
            BgmLoopStartSeconds = bgmLoopStartSeconds;
            BgmLoopLengthSeconds = bgmLoopLengthSeconds;
            BasePerfectWindowMul = basePerfectWindowMul;
            BaseGoodWindowMul = baseGoodWindowMul;
            BaseTelegraphLeadMul = baseTelegraphLeadMul;
            CloseToWinScoreMargin = closeToWinScoreMargin;
            NormalBeatsPerStep = normalBeatsPerStep;
            DecisiveMinTriplets = decisiveMinTriplets;
            DecisiveMaxTriplets = decisiveMaxTriplets;
        }

        public bool UsesCustomBgmLoop => BgmLoopLengthSeconds > 0.01f;

        /// <summary>港灣練習帶基準難度（最簡單）。</summary>
        public static Profile HarborPractice =>
            BuildHarborPractice(DefaultBpm, DefaultFirstDownbeatOffset);

        public static Profile Default => HarborPractice;

        public static Profile BuildHarborPractice(float bpm, float firstDownbeatOffset) =>
            new Profile(
                bpm,
                firstDownbeatOffset,
                GridMode.QuarterBeat,
                0.82f,
                0.85f,
                0.70f,
                0f,
                0f,
                1f,
                1f,
                1f,
                HarborCloseToWinScoreMargin,
                HarborNormalBeatsPerStep,
                HarborDecisiveMinTriplets,
                HarborDecisiveMaxTriplets);
    }

    [SerializeField] [Range(40f, 220f)] private float bpm = DefaultBpm;
    [SerializeField] [Min(0f)] private float firstDownbeatOffset = DefaultFirstDownbeatOffset;
    [SerializeField] private CdEntry[] cdEntries = new CdEntry[0];

    public float Bpm => bpm;
    public float FirstDownbeatOffset => firstDownbeatOffset;

    private static BirdDuelRhythmSync instance;
    private static bool loaded;

    public static BirdDuelRhythmSync Instance
    {
        get
        {
            if (!loaded)
            {
                instance = Resources.Load<BirdDuelRhythmSync>(ResourcesPath);
                loaded = true;
            }
            return instance;
        }
    }

    /// <summary>依 CD id 解析節奏設定；找不到 CD 專用列時退回資產預設（港灣練習帶）。</summary>
    public static Profile ResolveForCd(string cdId)
    {
        BirdDuelRhythmSync sync = Instance;
        if (sync == null)
            return Profile.Default;

        if (!string.IsNullOrWhiteSpace(cdId) && sync.cdEntries != null)
        {
            string key = cdId.Trim();
            for (int i = 0; i < sync.cdEntries.Length; i++)
            {
                CdEntry entry = sync.cdEntries[i];
                if (string.IsNullOrWhiteSpace(entry.cdId)) continue;
                if (!string.Equals(entry.cdId.Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                return BuildProfileFromEntry(entry);
            }
        }

        return Profile.BuildHarborPractice(sync.bpm, sync.firstDownbeatOffset);
    }

    private static Profile BuildProfileFromEntry(CdEntry entry)
    {
        return new Profile(
            entry.bpm,
            entry.firstDownbeatOffset,
            entry.gridMode,
            entry.decisivePerfectWindowMul > 0f ? entry.decisivePerfectWindowMul : 0.82f,
            entry.decisiveGoodWindowMul > 0f ? entry.decisiveGoodWindowMul : 0.85f,
            entry.decisiveTelegraphLeadMul > 0f ? entry.decisiveTelegraphLeadMul : 0.70f,
            Mathf.Max(0f, entry.bgmLoopStartSeconds),
            Mathf.Max(0f, entry.bgmLoopLengthSeconds),
            entry.basePerfectWindowMul > 0f ? entry.basePerfectWindowMul : 1f,
            entry.baseGoodWindowMul > 0f ? entry.baseGoodWindowMul : 1f,
            entry.baseTelegraphLeadMul > 0f ? entry.baseTelegraphLeadMul : 1f,
            entry.closeToWinScoreMargin > 0 ? entry.closeToWinScoreMargin : HarborCloseToWinScoreMargin,
            entry.normalBeatsPerStep > 0 ? entry.normalBeatsPerStep : HarborNormalBeatsPerStep,
            entry.decisiveMinTriplets > 0 ? entry.decisiveMinTriplets : HarborDecisiveMinTriplets,
            entry.decisiveMaxTriplets > 0 ? entry.decisiveMaxTriplets : HarborDecisiveMaxTriplets);
    }

#if UNITY_EDITOR
    public void EditorSet(float newBpm, float newFirstDownbeatOffset)
    {
        bpm = Mathf.Clamp(newBpm, 40f, 220f);
        firstDownbeatOffset = Mathf.Max(0f, newFirstDownbeatOffset);
    }

    public bool EditorTryGetCdEntry(string cdId, out CdEntry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(cdId) || cdEntries == null)
            return false;

        string key = cdId.Trim();
        for (int i = 0; i < cdEntries.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(cdEntries[i].cdId)) continue;
            if (!string.Equals(cdEntries[i].cdId.Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                continue;

            entry = cdEntries[i];
            return true;
        }

        return false;
    }

    public void EditorSetCdEntry(CdEntry entry)
    {
        if (cdEntries == null || cdEntries.Length == 0)
        {
            cdEntries = new[] { entry };
            return;
        }

        string key = entry.cdId?.Trim();
        for (int i = 0; i < cdEntries.Length; i++)
        {
            if (!string.Equals(cdEntries[i].cdId?.Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                continue;
            cdEntries[i] = entry;
            return;
        }

        var expanded = new CdEntry[cdEntries.Length + 1];
        cdEntries.CopyTo(expanded, 0);
        expanded[expanded.Length - 1] = entry;
        cdEntries = expanded;
    }
#endif
}

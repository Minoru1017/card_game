using UnityEngine;

/// <summary>
/// 鬥鳥節奏對拍設定：BGM（feinsmecker - Come Again）的 BPM 與第一個下拍偏移（秒）。
/// 由 <c>Tools/Audio/Analyze Bird Duel BGM Tempo</c> 量測寫入 Resources；
/// 執行期由 <see cref="FightingBirdGameSceneController"/> 讀取以對齊節拍。找不到資產時使用預設值。
/// </summary>
[CreateAssetMenu(fileName = "BirdDuelRhythmSync", menuName = "Card Game/Bird Duel Rhythm Sync", order = 1)]
public sealed class BirdDuelRhythmSync : ScriptableObject
{
    public const string ResourcesPath = "BirdDuelRhythmSync";
    public const float DefaultBpm = 120f;
    public const float DefaultFirstDownbeatOffset = 0f;

    [SerializeField] [Range(40f, 220f)] private float bpm = DefaultBpm;
    [SerializeField] [Min(0f)] private float firstDownbeatOffset = DefaultFirstDownbeatOffset;

    public float Bpm => bpm;
    public float FirstDownbeatOffset => firstDownbeatOffset;

    private static BirdDuelRhythmSync instance;
    private static bool loaded;

    /// <summary>一次性載入並快取；找不到回傳 null（呼叫端退回程式預設）。</summary>
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

#if UNITY_EDITOR
    /// <summary>供 Editor 量測工具寫入，請勿在執行期呼叫。</summary>
    public void EditorSet(float newBpm, float newFirstDownbeatOffset)
    {
        bpm = Mathf.Clamp(newBpm, 40f, 220f);
        firstDownbeatOffset = Mathf.Max(0f, newFirstDownbeatOffset);
    }
#endif
}

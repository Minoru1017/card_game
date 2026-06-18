using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音頻註冊表（直接引用，取代字串式 Resources.Load）。
///
/// 兩種用法：
///   - 鍵值型：NPC 語音以 id（例 1-1_4）對應 AudioClip，供 PlotNpcVoicePlayer 查詢。
///   - 單一指定型：劇情 BGM / 選單點擊 / 打字音效，直接欄位引用。
///
/// 取得方式：AudioLibrary.Instance（一次性從 Resources/AudioLibrary.asset 載入並快取）。
/// 這是整個資源系統唯一保留的字串載入點（中央化 bootstrap），其餘改為直接引用，
/// 讓 Unity 自動追蹤依賴、缺檔在編輯期即報錯。
/// </summary>
[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Card Game/Audio Library", order = 0)]
public sealed class AudioLibrary : ScriptableObject
{
    public const string ResourcesPath = "AudioLibrary";

    [System.Serializable]
    public struct NamedAudioClip
    {
        public string id;
        public AudioClip clip;
    }

    [Header("Main Plot NPC 語音（id 例 1-1_4）")]
    [SerializeField] private NamedAudioClip[] npcVoices = new NamedAudioClip[0];

    [Header("Main Plot 單一音軌")]
    [SerializeField] private AudioClip plotBgm;
    [SerializeField] private AudioClip hallBgm;
    [SerializeField] private AudioClip buildbeckBgm;
    [SerializeField] private AudioClip cardStoreBgm;
    [SerializeField] private AudioClip birdDuelBgm;
    [SerializeField] private AudioClip birdDuelHitSfxSource;
    [SerializeField] private AudioClip menuClickSfx;
    [SerializeField] private AudioClip typingSfx;

    [Header("Bird Duel CD BGM（id = cdId，例 court_march）")]
    [SerializeField] private NamedAudioClip[] birdDuelCdBgms = new NamedAudioClip[0];

    public AudioClip PlotBgm => plotBgm;
    public AudioClip HallBgm => hallBgm;
    public AudioClip BuildbeckBgm => buildbeckBgm;
    public AudioClip CardStoreBgm => cardStoreBgm;
    public AudioClip BirdDuelBgm => birdDuelBgm;
    public AudioClip BirdDuelHitSfxSource => birdDuelHitSfxSource;
    public AudioClip MenuClickSfx => menuClickSfx;
    public AudioClip TypingSfx => typingSfx;

    private Dictionary<string, AudioClip> voiceLookup;
    private Dictionary<string, AudioClip> birdDuelCdBgmLookup;

    private static AudioLibrary instance;
    private static bool instanceLoaded;

    /// <summary>一次性載入並快取的單例；找不到資產時回傳 null（呼叫端應退回舊載入方式）。</summary>
    public static AudioLibrary Instance
    {
        get
        {
            if (!instanceLoaded)
            {
                instance = Resources.Load<AudioLibrary>(ResourcesPath);
                instanceLoaded = true;
                if (instance == null)
                {
                    Debug.LogWarning(
                        $"AudioLibrary: 找不到 Resources/{ResourcesPath}.asset，" +
                        "請執行 Tools/Audio/Create or Refresh Audio Library；暫時回退舊載入方式。");
                }
            }
            return instance;
        }
    }

    /// <summary>依 id 取得 NPC 語音 clip，找不到回傳 null。</summary>
    public AudioClip GetVoice(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        EnsureVoiceLookup();
        return voiceLookup.TryGetValue(id.Trim(), out AudioClip clip) ? clip : null;
    }

    /// <summary>依 CD id 取得鬥鳥 BGM；找不到時回傳預設 <see cref="BirdDuelBgm"/>。</summary>
    public AudioClip GetBirdDuelCdBgm(string cdId)
    {
        if (string.IsNullOrWhiteSpace(cdId))
            return birdDuelBgm;

        EnsureBirdDuelCdBgmLookup();
        return birdDuelCdBgmLookup.TryGetValue(cdId.Trim(), out AudioClip clip) && clip != null
            ? clip
            : birdDuelBgm;
    }

    private void EnsureBirdDuelCdBgmLookup()
    {
        if (birdDuelCdBgmLookup != null)
            return;

        birdDuelCdBgmLookup = new Dictionary<string, AudioClip>();
        if (birdDuelCdBgms == null)
            return;

        foreach (NamedAudioClip entry in birdDuelCdBgms)
        {
            if (string.IsNullOrWhiteSpace(entry.id) || entry.clip == null)
                continue;
            birdDuelCdBgmLookup[entry.id.Trim()] = entry.clip;
        }
    }

    private void EnsureVoiceLookup()
    {
        if (voiceLookup != null)
            return;

        voiceLookup = new Dictionary<string, AudioClip>();
        if (npcVoices == null)
            return;

        foreach (NamedAudioClip entry in npcVoices)
        {
            if (string.IsNullOrWhiteSpace(entry.id) || entry.clip == null)
                continue;
            voiceLookup[entry.id.Trim()] = entry.clip;
        }
    }

#if UNITY_EDITOR
    // Inspector 內修改（例如清空某語音欄位）時，使快取失效以即時反映。
    private void OnValidate()
    {
        voiceLookup = null;
        birdDuelCdBgmLookup = null;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetVoices(NamedAudioClip[] voices)
    {
        npcVoices = voices ?? new NamedAudioClip[0];
        voiceLookup = null;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetSingletons(AudioClip bgm, AudioClip menuClick, AudioClip typing)
    {
        plotBgm = bgm;
        menuClickSfx = menuClick;
        typingSfx = typing;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetHallBgm(AudioClip bgm)
    {
        hallBgm = bgm;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetBuildbeckBgm(AudioClip bgm)
    {
        buildbeckBgm = bgm;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetCardStoreBgm(AudioClip bgm)
    {
        cardStoreBgm = bgm;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetBirdDuelBgm(AudioClip bgm)
    {
        birdDuelBgm = bgm;
    }

    public void EditorSetBirdDuelHitSfxSource(AudioClip clip)
    {
        birdDuelHitSfxSource = clip;
    }

    /// <summary>供 Editor 自動填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetBirdDuelCdBgms(NamedAudioClip[] entries)
    {
        birdDuelCdBgms = entries ?? new NamedAudioClip[0];
        birdDuelCdBgmLookup = null;
    }
#endif
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌美術註冊表（直接引用，取代字串式 Resources.Load）。
/// 以 card.id 對應「立繪 artwork」與「組牌縮圖 deckThumb」的直接 Sprite 引用。
///
/// 資料來源：由 Tools/Card Art/Create or Refresh Card Art Library 從
/// CardStore.artworkOverrides（AutoBinder 維護）匯入。AutoBinder 仍負責把美術綁進 overrides，
/// 美術更新後重跑該選單即可把最新直接引用同步進本註冊表。
///
/// 取得方式：CardArtLibrary.Instance（一次性從 Resources/CardArtLibrary.asset 載入並快取）。
/// </summary>
[CreateAssetMenu(fileName = "CardArtLibrary", menuName = "Card Game/Card Art Library", order = 1)]
public sealed class CardArtLibrary : ScriptableObject
{
    public const string ResourcesPath = "CardArtLibrary";

    [System.Serializable]
    public struct Entry
    {
        public int id;
        public Sprite artwork;
        public Sprite deckThumb;
    }

    [SerializeField] private Entry[] entries = new Entry[0];

    private Dictionary<int, Entry> lookup;

    private static CardArtLibrary instance;
    private static bool instanceLoaded;

    /// <summary>一次性載入並快取的單例；找不到資產時回傳 null（呼叫端應退回舊載入方式）。</summary>
    public static CardArtLibrary Instance
    {
        get
        {
            if (!instanceLoaded)
            {
                instance = Resources.Load<CardArtLibrary>(ResourcesPath);
                instanceLoaded = true;
                if (instance == null)
                {
                    Debug.LogWarning(
                        $"CardArtLibrary: 找不到 Resources/{ResourcesPath}.asset，" +
                        "請執行 Tools/Card Art/Create or Refresh Card Art Library；暫時回退舊載入方式。");
                }
            }
            return instance;
        }
    }

    /// <summary>依 id 取得立繪，找不到回傳 null。</summary>
    public Sprite GetArtwork(int id)
    {
        EnsureLookup();
        return lookup.TryGetValue(id, out Entry e) ? e.artwork : null;
    }

    /// <summary>依 id 取得組牌縮圖，找不到回傳 null。</summary>
    public Sprite GetDeckThumb(int id)
    {
        EnsureLookup();
        return lookup.TryGetValue(id, out Entry e) ? e.deckThumb : null;
    }

    private void EnsureLookup()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<int, Entry>();
        if (entries == null)
            return;

        foreach (Entry e in entries)
            lookup[e.id] = e;
    }

#if UNITY_EDITOR
    // Inspector 內修改時使快取失效以即時反映。
    private void OnValidate()
    {
        lookup = null;
    }

    /// <summary>供 Editor 填表工具使用，請勿在執行期呼叫。</summary>
    public void EditorSetEntries(Entry[] newEntries)
    {
        entries = newEntries ?? new Entry[0];
        lookup = null;
    }
#endif
}

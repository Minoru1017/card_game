using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 建立 / 重新整理 Assets/Resources/CardArtLibrary.asset：
/// 從 DataManager.prefab 上 CardStore.artworkOverrides（由 CardArtworkAutoBinder 維護）
/// 匯入每個 id 的立繪 / 縮圖直接 Sprite 引用。
/// 美術更新（AutoBinder 重綁 overrides）後，重跑本選單即可同步。
/// 選單：Tools/Card Art/Create or Refresh Card Art Library
/// </summary>
public static class CardArtLibraryPopulator
{
    private const string LibraryAssetPath = "Assets/Resources/CardArtLibrary.asset";
    private const string DataManagerPrefabPath = "Assets/prefabs/DataManager.prefab";

    [MenuItem("Tools/Card Art/Create or Refresh Card Art Library")]
    public static void CreateOrRefresh()
    {
        CardArtLibrary library = AssetDatabase.LoadAssetAtPath<CardArtLibrary>(LibraryAssetPath);
        if (library == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            library = ScriptableObject.CreateInstance<CardArtLibrary>();
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
            Debug.Log($"CardArtLibraryPopulator: 已建立新資產 {LibraryAssetPath}");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataManagerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"CardArtLibraryPopulator: 找不到 {DataManagerPrefabPath}");
            return;
        }

        CardStore store = prefab.GetComponentInChildren<CardStore>(true);
        if (store == null)
        {
            Debug.LogError("CardArtLibraryPopulator: DataManager.prefab 上找不到 CardStore 元件");
            return;
        }

        var byId = new Dictionary<int, CardArtLibrary.Entry>();
        foreach (CardStore.CardArtworkOverride ov in store.artworkOverrides)
        {
            if (ov == null)
                continue;

            Sprite art = ov.artworkSprite;
            if (art == null && !string.IsNullOrWhiteSpace(ov.artworkResourcePath))
                art = Resources.Load<Sprite>(ov.artworkResourcePath.Trim());

            Sprite thumb = ov.deckThumbSprite;
            if (thumb == null && !string.IsNullOrWhiteSpace(ov.deckThumbResourcePath))
                thumb = Resources.Load<Sprite>(ov.deckThumbResourcePath.Trim());

            byId[ov.id] = new CardArtLibrary.Entry { id = ov.id, artwork = art, deckThumb = thumb };
        }

        CardArtLibrary.Entry[] entries = byId.Values.OrderBy(e => e.id).ToArray();
        library.EditorSetEntries(entries);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int withArt = entries.Count(e => e.artwork != null);
        int withThumb = entries.Count(e => e.deckThumb != null);
        Debug.Log(
            $"CardArtLibraryPopulator: 匯入 {entries.Length} 筆（artwork={withArt}, deckThumb={withThumb}）→ {LibraryAssetPath}");
    }
}

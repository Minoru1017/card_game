using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Auto-binds card artwork sprite overrides when artists drop/update images under Assets/UI.
/// 兩類美術：<see cref="CardArtKind.CardArt"/>（本體立繪）、<see cref="CardArtKind.DeckThumb"/>（組牌縮圖）。
/// </summary>
public sealed class CardArtworkAutoBinder : AssetPostprocessor
{
    private enum CardArtKind
    {
        CardArt,
        DeckThumb,
    }

    private const string UiFolderPrefix = "Assets/UI/";
    /// <summary>卡牌本體立繪目錄（檔名對 CardList 卡名）。</summary>
    private const string CardArtFolderPrefix = "Assets/UI/CardArt/";
    /// <summary>組建牌組／館藏縮圖目錄（檔名對 CardList 卡名）。</summary>
    private const string DeckThumbFolderPrefix = "Assets/UI/DeckThumb/";
    private const string CardCsvPath = "Assets/Assets/Datas/CardList.csv";
    private const string DataManagerPrefabPath = "Assets/prefabs/DataManager.prefab";
    private const string CardStoreScenePath = "Assets/Scenes/CardStore.unity";
    // 刪除卡圖後自動綁定的預設圖（可自行改成你想要的圖檔路徑）。
    private const string DefaultFallbackArtPath = "Assets/UI/Card preset images.png";
    private const string LogPrefix = "CardArt AutoBinder";
    private const string ColorInfo = "#7ED957";
    private const string ColorWarn = "#FFB347";

    private static readonly HashSet<string> SupportedImageExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
    };

    private static readonly char[] CsvComma = { ',' };

    /// <summary>匯入管線進行中排隊，於 <see cref="EditorApplication.delayCall"/> 一次處理，避免改名時開場景／SaveAssets 導致 Unity 當機。</summary>
    private static readonly HashSet<string> PendingBindPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PendingClearPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static bool deferredFlushScheduled;
    private static bool isFlushingDeferred;

    [MenuItem("Tools/Card Art/Rescan UI Images And Rebind")]
    private static void RescanAllUiImages()
    {
        // 手動觸發入口：掃描 Assets/UI 內所有 Sprite，逐一嘗試自動綁定。
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/UI" });
        int boundCount = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!TryBindByImagePath(path, syncSceneStores: true))
            {
                continue;
            }

            boundCount++;
        }

        if (boundCount > 0)
        {
            AssetDatabase.SaveAssets();
        }

        // 重掃後補齊：所有未綁定卡牌套用預設圖，避免顯示空圖。
        ApplyFallbackToUnboundCards();
        LogInfo($"rescanned UI images, updated {boundCount} match(es).");
    }

    [MenuItem("Tools/Card Art/Apply Card Preset To Unbound Cards")]
    private static void ApplyFallbackToUnboundCards()
    {
        Sprite fallbackSprite = LoadFallbackSprite();
        if (fallbackSprite == null)
        {
            LogWarn($"找不到預設圖，無法套用：{DefaultFallbackArtPath}");
            return;
        }

        HashSet<int> allCardKeys = CollectAllCardKeysFromCsv();
        if (allCardKeys.Count == 0)
        {
            LogWarn("CardList.csv 內沒有可用卡牌資料，略過補預設圖。");
            return;
        }

        bool prefabUpdated = TryApplyFallbackPrefabCardStore(allCardKeys, fallbackSprite);
        bool sceneUpdated = TryApplyFallbackSceneCardStore(CardStoreScenePath, allCardKeys, fallbackSprite);
        bool changed = prefabUpdated || sceneUpdated;

        if (changed)
        {
            AssetDatabase.SaveAssets();
            LogInfo($"已將未綁定卡牌套用預設圖：{DefaultFallbackArtPath}");
        }
        else
        {
            LogInfo("已檢查完成：所有卡牌皆已有綁定，無需補預設圖。");
        }
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (isFlushingDeferred)
            return;

        EnqueuePostprocessPaths(importedAssets, PendingBindPaths);
        EnqueuePostprocessPaths(movedAssets, PendingBindPaths);
        EnqueuePostprocessPaths(deletedAssets, PendingClearPaths);
        EnqueuePostprocessPaths(movedFromAssetPaths, PendingClearPaths);

        if (PendingBindPaths.Count == 0 && PendingClearPaths.Count == 0)
            return;

        ScheduleDeferredFlush();
    }

    private static void EnqueuePostprocessPaths(string[] paths, HashSet<string> target)
    {
        if (paths == null || paths.Length == 0)
            return;

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (!IsUiImagePath(path))
                continue;
            target.Add(path);
        }
    }

    private static void ScheduleDeferredFlush()
    {
        if (deferredFlushScheduled)
            return;

        deferredFlushScheduled = true;
        EditorApplication.delayCall += FlushDeferredPostprocess;
    }

    private static void FlushDeferredPostprocess()
    {
        deferredFlushScheduled = false;
        if (PendingBindPaths.Count == 0 && PendingClearPaths.Count == 0)
            return;

        string[] clearPaths = new string[PendingClearPaths.Count];
        PendingClearPaths.CopyTo(clearPaths);
        PendingClearPaths.Clear();

        string[] bindPaths = new string[PendingBindPaths.Count];
        PendingBindPaths.CopyTo(bindPaths);
        PendingBindPaths.Clear();

        isFlushingDeferred = true;
        bool changed = false;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < clearPaths.Length; i++)
            {
                if (TryClearBindByImagePath(clearPaths[i], syncSceneStores: false))
                    changed = true;
            }

            for (int i = 0; i < bindPaths.Length; i++)
            {
                if (TryBindByImagePath(bindPaths[i], syncSceneStores: false))
                    changed = true;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            isFlushingDeferred = false;
        }

        if (changed)
            AssetDatabase.SaveAssets();
    }

    private static bool TryBindByImagePath(string assetPath, bool syncSceneStores = true)
    {
        // 自動綁定主流程：路徑/副檔名過濾 -> 取 Sprite -> 檔名比對卡牌 -> 寫回 Prefab/Scene。
        if (!IsUiImagePath(assetPath))
        {
            return false;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            LogWarn($"已讀取到美術資源，但目前不是可綁定的 Sprite：{assetPath}");
            return false;
        }

        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        CardArtKind kind = ClassifyArtKind(assetPath, fileName);
        string lookupName = StripArtKindSuffixFromFileName(fileName, kind);
        if (!TryGetCardKeyByFileName(lookupName, out int cardKey))
        {
            LogWarn($"已讀取到你的美術資源，但找不到同名卡牌：{lookupName}");
            return false;
        }

        string kindLabel = kind == CardArtKind.DeckThumb ? "組牌縮圖" : "卡牌立繪";
        LogInfo($"正在將{kindLabel}套至對應卡牌（cardKey={cardKey}）：{assetPath}");
        bool prefabUpdated = TryUpdatePrefabCardStore(cardKey, sprite, kind);
        bool sceneUpdated = syncSceneStores && TryUpdateSceneCardStore(CardStoreScenePath, cardKey, sprite, kind);
        bool anyUpdated = prefabUpdated || sceneUpdated;

        if (anyUpdated)
        {
            LogInfo($"套用成功，請Play查看新的美術圖是否正確（cardKey={cardKey}）：{assetPath}");
        }
        else
        {
            LogInfo($"已檢查完成：目前綁定內容無變更（cardKey={cardKey}）。");
        }

        return anyUpdated;
    }

    private static bool IsUiImagePath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) ||
            !assetPath.StartsWith(UiFolderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string ext = Path.GetExtension(assetPath);
        return SupportedImageExt.Contains(ext);
    }

    private static CardArtKind ClassifyArtKind(string assetPath, string fileNameWithoutExt)
    {
        if (!string.IsNullOrEmpty(assetPath))
        {
            string p = assetPath.Replace('\\', '/');
            if (p.StartsWith(DeckThumbFolderPrefix, StringComparison.OrdinalIgnoreCase))
                return CardArtKind.DeckThumb;
            if (p.StartsWith(CardArtFolderPrefix, StringComparison.OrdinalIgnoreCase))
                return CardArtKind.CardArt;
        }

        if (HasDeckThumbFileNameSuffix(fileNameWithoutExt))
            return CardArtKind.DeckThumb;

        return CardArtKind.CardArt;
    }

    private static bool HasDeckThumbFileNameSuffix(string fileNameWithoutExt)
    {
        if (string.IsNullOrWhiteSpace(fileNameWithoutExt)) return false;
        string n = fileNameWithoutExt.Trim();
        string[] suffixes = { "_deck", "_thumb", "_縮圖", "_deckthumb" };
        for (int i = 0; i < suffixes.Length; i++)
        {
            if (n.EndsWith(suffixes[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>比對卡名前去掉縮圖專用尾碼（資料夾已區分時檔名可與卡名相同）。</summary>
    private static string StripArtKindSuffixFromFileName(string fileNameWithoutExt, CardArtKind kind)
    {
        if (kind != CardArtKind.DeckThumb || string.IsNullOrWhiteSpace(fileNameWithoutExt))
            return fileNameWithoutExt;

        string n = fileNameWithoutExt.Trim();
        string[] suffixes = { "_deckthumb", "_deck", "_thumb", "_縮圖" };
        for (int i = 0; i < suffixes.Length; i++)
        {
            if (n.EndsWith(suffixes[i], StringComparison.OrdinalIgnoreCase))
                return n.Substring(0, n.Length - suffixes[i].Length);
        }
        return n;
    }

    private static bool TryClearBindByImagePath(string assetPath, bool syncSceneStores = true)
    {
        if (!IsUiImagePath(assetPath))
        {
            return false;
        }

        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        CardArtKind kind = ClassifyArtKind(assetPath, fileName);
        string lookupName = StripArtKindSuffixFromFileName(fileName, kind);
        if (!TryGetCardKeyByFileName(lookupName, out int cardKey))
        {
            LogWarn($"已讀取到刪除/移出事件，但找不到同名卡牌：{lookupName}");
            return false;
        }

        Sprite fallbackSprite = LoadFallbackSprite();
        if (fallbackSprite != null)
        {
            LogInfo($"正在將刪除的美術改綁預設圖（cardKey={cardKey}）：{assetPath}");
        }
        else
        {
            LogWarn($"預設圖不存在或不可用，將改為清空對應欄位：{DefaultFallbackArtPath}");
        }

        bool prefabUpdated = TryClearPrefabCardStore(cardKey, fallbackSprite, kind);
        bool sceneUpdated = syncSceneStores && TryClearSceneCardStore(CardStoreScenePath, cardKey, fallbackSprite, kind);
        bool anyUpdated = prefabUpdated || sceneUpdated;

        if (anyUpdated)
        {
            if (fallbackSprite != null)
            {
                LogInfo($"套用成功，已改綁預設圖，請Play確認是否正確（cardKey={cardKey}）。");
            }
            else
            {
                LogInfo($"清空成功，請Play確認卡牌已移除該美術圖（cardKey={cardKey}）。");
            }
        }
        else
        {
            LogInfo($"已檢查完成：未找到需更新的綁定（cardKey={cardKey}）。");
        }

        return anyUpdated;
    }

    private static bool TryGetCardKeyByFileName(string fileName, out int cardKey)
    {
        // 依圖片檔名（正規化後）與 CardList.csv 卡名／英文名完全相符才綁定。
        cardKey = 0;
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CardCsvPath);
        if (csv == null || string.IsNullOrWhiteSpace(csv.text))
        {
            LogWarn($"cannot load card csv at '{CardCsvPath}'.");
            return false;
        }

        string target = NormalizeName(fileName);
        string[] lines = csv.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] cols = line.Split(CsvComma);
            if (cols.Length < 3)
            {
                continue;
            }

            string type = cols[0].Trim();
            if (!type.Equals("monster", StringComparison.OrdinalIgnoreCase) &&
                !type.Equals("spell", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawId))
            {
                continue;
            }

            string nameZh = cols[2].Trim();
            string nameEn = cols.Length >= 4 ? cols[3].Trim() : string.Empty;
            string normalizedZh = NormalizeName(nameZh);
            string normalizedEn = NormalizeName(nameEn);

            if (!target.Equals(normalizedZh, StringComparison.OrdinalIgnoreCase) &&
                !target.Equals(normalizedEn, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            cardKey = type.Equals("spell", StringComparison.OrdinalIgnoreCase)
                // 法術牌在系統中使用負值 key，需由 ordinal 轉換。
                ? DeckCardId.SpellKeyFromOrdinal(rawId)
                : rawId;
            return true;
        }

        return false;
    }

    private static HashSet<int> CollectAllCardKeysFromCsv()
    {
        var keys = new HashSet<int>();
        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CardCsvPath);
        if (csv == null || string.IsNullOrWhiteSpace(csv.text))
        {
            LogWarn($"cannot load card csv at '{CardCsvPath}'.");
            return keys;
        }

        string[] lines = csv.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] cols = line.Split(CsvComma);
            if (cols.Length < 3)
            {
                continue;
            }

            string type = cols[0].Trim();
            if (!type.Equals("monster", StringComparison.OrdinalIgnoreCase) &&
                !type.Equals("spell", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawId))
            {
                continue;
            }

            int key = type.Equals("spell", StringComparison.OrdinalIgnoreCase)
                ? DeckCardId.SpellKeyFromOrdinal(rawId)
                : rawId;
            keys.Add(key);
        }

        return keys;
    }

    private static string NormalizeName(string value)
    {
        // 名稱正規化：降低命名差異影響（空白/底線/連字號/引號）。
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string s = value.Trim().ToLowerInvariant();
        s = s.Replace(" ", string.Empty);
        s = s.Replace("_", string.Empty);
        s = s.Replace("-", string.Empty);
        s = s.Replace("'", string.Empty);
        s = s.Replace("\"", string.Empty);
        return s;
    }

    private static bool TryUpdatePrefabCardStore(int cardKey, Sprite sprite, CardArtKind kind)
    {
        // 將綁定結果寫回 DataManager.prefab 內的 CardStore。
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataManagerPrefabPath);
        if (prefab == null)
        {
            return false;
        }

        CardStore store = prefab.GetComponent<CardStore>();
        if (store == null)
        {
            return false;
        }

        bool changed = kind == CardArtKind.DeckThumb
            ? UpsertDeckThumbOverride(store, cardKey, sprite)
            : UpsertArtworkOverride(store, cardKey, sprite);
        if (changed)
        {
            EditorUtility.SetDirty(store);
            EditorUtility.SetDirty(prefab);
        }

        return changed;
    }

    private static bool TryApplyFallbackPrefabCardStore(HashSet<int> allCardKeys, Sprite fallbackSprite)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataManagerPrefabPath);
        if (prefab == null)
        {
            return false;
        }

        CardStore store = prefab.GetComponent<CardStore>();
        if (store == null)
        {
            return false;
        }

        bool changed = EnsureFallbackForMissingOverrides(store, allCardKeys, fallbackSprite);
        if (changed)
        {
            EditorUtility.SetDirty(store);
            EditorUtility.SetDirty(prefab);
        }

        return changed;
    }

    private static bool TryClearPrefabCardStore(int cardKey, Sprite fallbackSprite, CardArtKind kind)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataManagerPrefabPath);
        if (prefab == null)
        {
            return false;
        }

        CardStore store = prefab.GetComponent<CardStore>();
        if (store == null)
        {
            return false;
        }

        bool changed = kind == CardArtKind.DeckThumb
            ? ClearDeckThumbOverride(store, cardKey, fallbackSprite)
            : ClearArtworkOverride(store, cardKey, fallbackSprite);
        if (changed)
        {
            EditorUtility.SetDirty(store);
            EditorUtility.SetDirty(prefab);
        }

        return changed;
    }

    private static bool TryUpdateSceneCardStore(string scenePath, int cardKey, Sprite sprite, CardArtKind kind)
    {
        // 將綁定結果同步到 CardStore.unity 場景中的 CardStore。
        // 若場景未開啟，會以 Additive 方式暫開、儲存後再關閉。
        if (!File.Exists(scenePath))
        {
            return false;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        bool changed = false;
        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                CardStore[] stores = roots[i].GetComponentsInChildren<CardStore>(true);
                for (int j = 0; j < stores.Length; j++)
                {
                    bool storeChanged = kind == CardArtKind.DeckThumb
                        ? UpsertDeckThumbOverride(stores[j], cardKey, sprite)
                        : UpsertArtworkOverride(stores[j], cardKey, sprite);
                    if (!storeChanged)
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(stores[j]);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            if (!wasLoaded && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        return changed;
    }

    private static bool TryApplyFallbackSceneCardStore(string scenePath, HashSet<int> allCardKeys, Sprite fallbackSprite)
    {
        if (!File.Exists(scenePath))
        {
            return false;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        bool changed = false;
        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                CardStore[] stores = roots[i].GetComponentsInChildren<CardStore>(true);
                for (int j = 0; j < stores.Length; j++)
                {
                    if (!EnsureFallbackForMissingOverrides(stores[j], allCardKeys, fallbackSprite))
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(stores[j]);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            if (!wasLoaded && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        return changed;
    }

    private static bool TryClearSceneCardStore(string scenePath, int cardKey, Sprite fallbackSprite, CardArtKind kind)
    {
        if (!File.Exists(scenePath))
        {
            return false;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasLoaded)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        bool changed = false;
        try
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                CardStore[] stores = roots[i].GetComponentsInChildren<CardStore>(true);
                for (int j = 0; j < stores.Length; j++)
                {
                    bool storeChanged = kind == CardArtKind.DeckThumb
                        ? ClearDeckThumbOverride(stores[j], cardKey, fallbackSprite)
                        : ClearArtworkOverride(stores[j], cardKey, fallbackSprite);
                    if (!storeChanged)
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(stores[j]);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            if (!wasLoaded && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        return changed;
    }

    private static bool UpsertArtworkOverride(CardStore store, int cardKey, Sprite sprite)
    {
        // 寫入策略：有同 id 就覆蓋；沒有就新增一筆 override。
        if (store == null || sprite == null)
        {
            return false;
        }

        if (store.artworkOverrides == null)
        {
            store.artworkOverrides = new List<CardStore.CardArtworkOverride>();
        }

        for (int i = 0; i < store.artworkOverrides.Count; i++)
        {
            CardStore.CardArtworkOverride entry = store.artworkOverrides[i];
            if (entry == null || entry.id != cardKey)
            {
                continue;
            }

            if (entry.artworkSprite == sprite && string.IsNullOrWhiteSpace(entry.artworkResourcePath))
            {
                return false;
            }

            entry.artworkResourcePath = string.Empty;
            entry.artworkSprite = sprite;
            return true;
        }

        store.artworkOverrides.Add(new CardStore.CardArtworkOverride
        {
            id = cardKey,
            artworkResourcePath = string.Empty,
            artworkSprite = sprite,
        });

        return true;
    }

    private static bool UpsertDeckThumbOverride(CardStore store, int cardKey, Sprite sprite)
    {
        if (store == null || sprite == null)
            return false;

        if (store.artworkOverrides == null)
            store.artworkOverrides = new List<CardStore.CardArtworkOverride>();

        for (int i = 0; i < store.artworkOverrides.Count; i++)
        {
            CardStore.CardArtworkOverride entry = store.artworkOverrides[i];
            if (entry == null || entry.id != cardKey)
                continue;

            if (entry.deckThumbSprite == sprite && string.IsNullOrWhiteSpace(entry.deckThumbResourcePath))
                return false;

            entry.deckThumbResourcePath = string.Empty;
            entry.deckThumbSprite = sprite;
            return true;
        }

        store.artworkOverrides.Add(new CardStore.CardArtworkOverride
        {
            id = cardKey,
            deckThumbResourcePath = string.Empty,
            deckThumbSprite = sprite,
        });

        return true;
    }

    private static bool ClearArtworkOverride(CardStore store, int cardKey, Sprite fallbackSprite)
    {
        if (store == null || store.artworkOverrides == null)
        {
            return false;
        }

        for (int i = 0; i < store.artworkOverrides.Count; i++)
        {
            CardStore.CardArtworkOverride entry = store.artworkOverrides[i];
            if (entry == null || entry.id != cardKey)
            {
                continue;
            }

            bool shouldAssignFallback = fallbackSprite != null;
            bool alreadyExpected =
                shouldAssignFallback
                    ? (entry.artworkSprite == fallbackSprite && string.IsNullOrWhiteSpace(entry.artworkResourcePath))
                    : (entry.artworkSprite == null && string.IsNullOrWhiteSpace(entry.artworkResourcePath));
            if (alreadyExpected)
            {
                return false;
            }

            entry.artworkSprite = fallbackSprite;
            entry.artworkResourcePath = string.Empty;
            return true;
        }

        return false;
    }

    private static bool ClearDeckThumbOverride(CardStore store, int cardKey, Sprite fallbackSprite)
    {
        if (store == null || store.artworkOverrides == null)
            return false;

        for (int i = 0; i < store.artworkOverrides.Count; i++)
        {
            CardStore.CardArtworkOverride entry = store.artworkOverrides[i];
            if (entry == null || entry.id != cardKey)
                continue;

            bool shouldAssignFallback = fallbackSprite != null;
            bool alreadyExpected = shouldAssignFallback
                ? (entry.deckThumbSprite == fallbackSprite && string.IsNullOrWhiteSpace(entry.deckThumbResourcePath))
                : (entry.deckThumbSprite == null && string.IsNullOrWhiteSpace(entry.deckThumbResourcePath));
            if (alreadyExpected)
                return false;

            entry.deckThumbSprite = fallbackSprite;
            entry.deckThumbResourcePath = string.Empty;
            return true;
        }

        return false;
    }

    private static bool EnsureFallbackForMissingOverrides(CardStore store, HashSet<int> allCardKeys, Sprite fallbackSprite)
    {
        if (store == null || allCardKeys == null || allCardKeys.Count == 0 || fallbackSprite == null)
        {
            return false;
        }

        if (store.artworkOverrides == null)
        {
            store.artworkOverrides = new List<CardStore.CardArtworkOverride>();
        }

        bool changed = false;
        for (int i = 0; i < store.artworkOverrides.Count; i++)
        {
            CardStore.CardArtworkOverride entry = store.artworkOverrides[i];
            if (entry == null) continue;
            if (!allCardKeys.Contains(entry.id)) continue;

            bool hasAnyBinding = entry.artworkSprite != null || !string.IsNullOrWhiteSpace(entry.artworkResourcePath);
            if (hasAnyBinding) continue;

            entry.artworkResourcePath = string.Empty;
            entry.artworkSprite = fallbackSprite;
            changed = true;
        }

        foreach (int key in allCardKeys)
        {
            bool exists = false;
            for (int i = 0; i < store.artworkOverrides.Count; i++)
            {
                CardStore.CardArtworkOverride entry = store.artworkOverrides[i];
                if (entry != null && entry.id == key)
                {
                    exists = true;
                    break;
                }
            }

            if (exists) continue;

            store.artworkOverrides.Add(new CardStore.CardArtworkOverride
            {
                id = key,
                artworkResourcePath = string.Empty,
                artworkSprite = fallbackSprite,
            });
            changed = true;
        }

        return changed;
    }

    private static Sprite LoadFallbackSprite()
    {
        if (string.IsNullOrWhiteSpace(DefaultFallbackArtPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(DefaultFallbackArtPath);
    }

    private static void LogInfo(string message)
    {
        Debug.Log(FormatLog(message, ColorInfo));
    }

    private static void LogWarn(string message)
    {
        Debug.LogWarning(FormatLog(message, ColorWarn));
    }

    private static string FormatLog(string message, string colorHex)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return $"[{timestamp}] <color={colorHex}>[{LogPrefix}] {message}</color>";
    }
}

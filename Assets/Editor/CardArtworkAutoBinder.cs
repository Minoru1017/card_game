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
/// File name is matched against CardList.csv card names (Chinese/English).
/// </summary>
public sealed class CardArtworkAutoBinder : AssetPostprocessor
{
    private const string UiFolderPrefix = "Assets/UI/";
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

    [MenuItem("Tools/Card Art/Rescan UI Images And Rebind")]
    private static void RescanAllUiImages()
    {
        // 手動觸發入口：掃描 Assets/UI 內所有 Sprite，逐一嘗試自動綁定。
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/UI" });
        int boundCount = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!TryBindByImagePath(path))
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
        // 自動觸發入口：Unity 匯入/刪除/搬移資產後會呼叫這裡。
        bool changed = false;

        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (IsUiImagePath(importedAssets[i]))
            {
                LogInfo($"已讀取到你的美術資源：{importedAssets[i]}");
            }

            if (TryBindByImagePath(importedAssets[i]))
            {
                changed = true;
            }
        }

        for (int i = 0; i < deletedAssets.Length; i++)
        {
            if (!IsUiImagePath(deletedAssets[i])) continue;
            LogInfo($"已讀取到你的美術資源異動（刪除）：{deletedAssets[i]}");
            if (TryClearBindByImagePath(deletedAssets[i]))
            {
                changed = true;
            }
        }

        for (int i = 0; i < movedAssets.Length; i++)
        {
            if (IsUiImagePath(movedAssets[i]))
            {
                LogInfo($"已讀取到你的美術資源異動（新增/移動）：{movedAssets[i]}");
            }

            if (TryBindByImagePath(movedAssets[i]))
            {
                changed = true;
            }
        }

        for (int i = 0; i < movedFromAssetPaths.Length; i++)
        {
            if (!IsUiImagePath(movedFromAssetPaths[i])) continue;
            LogInfo($"已讀取到你的美術資源異動（原路徑）：{movedFromAssetPaths[i]}");
            // 檔案搬家/改名時，舊檔名對應的卡牌綁定要先清掉，避免殘留錯誤綁定。
            if (TryClearBindByImagePath(movedFromAssetPaths[i]))
            {
                changed = true;
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static bool TryBindByImagePath(string assetPath)
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

        if (!TryGetCardKeyByFileName(fileName, out int cardKey))
        {
            LogWarn($"已讀取到你的美術資源，但找不到同名卡牌：{fileName}");
            return false;
        }

        LogInfo($"正在將美術圖套至對應卡牌（cardKey={cardKey}）：{assetPath}");
        bool prefabUpdated = TryUpdatePrefabCardStore(cardKey, sprite);
        bool sceneUpdated = TryUpdateSceneCardStore(CardStoreScenePath, cardKey, sprite);
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

    private static bool TryClearBindByImagePath(string assetPath)
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

        if (!TryGetCardKeyByFileName(fileName, out int cardKey))
        {
            LogWarn($"已讀取到刪除/移出事件，但找不到同名卡牌：{fileName}");
            return false;
        }

        Sprite fallbackSprite = LoadFallbackSprite();
        if (fallbackSprite != null)
        {
            LogInfo($"正在將刪除卡圖改綁預設圖（cardKey={cardKey}）：{assetPath}");
        }
        else
        {
            LogWarn($"預設圖不存在或不可用，將改為清空 artworkSprite：{DefaultFallbackArtPath}");
        }

        bool prefabUpdated = TryClearPrefabCardStore(cardKey, fallbackSprite);
        bool sceneUpdated = TryClearSceneCardStore(CardStoreScenePath, cardKey, fallbackSprite);
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
        // 依圖片檔名比對 CardList.csv（中文名/英文名皆可），回傳遊戲內使用的 cardKey。
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

    private static bool TryUpdatePrefabCardStore(int cardKey, Sprite sprite)
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

        bool changed = UpsertArtworkOverride(store, cardKey, sprite);
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

    private static bool TryClearPrefabCardStore(int cardKey, Sprite fallbackSprite)
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

        bool changed = ClearArtworkOverride(store, cardKey, fallbackSprite);
        if (changed)
        {
            EditorUtility.SetDirty(store);
            EditorUtility.SetDirty(prefab);
        }

        return changed;
    }

    private static bool TryUpdateSceneCardStore(string scenePath, int cardKey, Sprite sprite)
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
                    if (!UpsertArtworkOverride(stores[j], cardKey, sprite))
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

    private static bool TryClearSceneCardStore(string scenePath, int cardKey, Sprite fallbackSprite)
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
                    if (!ClearArtworkOverride(stores[j], cardKey, fallbackSprite))
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

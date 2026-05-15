using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 從「Deck Card」prefab 抽出 name / atk / dtf 至獨立 prefab，並在 Deck Card 內改為單一子樹（含 DeckCardInfoStrip）。
/// </summary>
public static class DeckCardInfoStripPrefabBaker
{
    private const string DeckCardPrefabFileName = "Deck Card.prefab";
    private const string StripPrefabFileName = "DeckCardInfoStrip.prefab";

    private static string ResolveDeckCardPrefabPath()
    {
        foreach (string guid in AssetDatabase.FindAssets("Deck Card"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (Path.GetFileName(path).Equals(DeckCardPrefabFileName, System.StringComparison.OrdinalIgnoreCase))
                return path.Replace('\\', '/');
        }

        return null;
    }

    private static string StripPrefabPathNextTo(string deckCardPrefabPath)
    {
        string dir = Path.GetDirectoryName(deckCardPrefabPath);
        if (string.IsNullOrEmpty(dir)) return null;
        return (dir.Replace('\\', '/') + "/" + StripPrefabFileName).Replace("//", "/");
    }

    [MenuItem("Tools/Deck/Bake DeckCardInfoStrip Prefab")]
    public static void Bake()
    {
        string deckCardPrefabPath = ResolveDeckCardPrefabPath();
        if (string.IsNullOrEmpty(deckCardPrefabPath))
        {
            Debug.LogError(
                "DeckCardInfoStripPrefabBaker: 找不到 '" + DeckCardPrefabFileName +
                "'。請確認 prefab 存在於專案 Assets 下（資料夾名稱大小寫不拘）。");
            return;
        }

        string stripPrefabPath = StripPrefabPathNextTo(deckCardPrefabPath);
        if (string.IsNullOrEmpty(stripPrefabPath))
        {
            Debug.LogError("DeckCardInfoStripPrefabBaker: 無法決定 DeckCardInfoStrip 輸出路徑。");
            return;
        }

        GameObject deckRoot = null;
        try
        {
            deckRoot = PrefabUtility.LoadPrefabContents(deckCardPrefabPath);
            if (deckRoot.transform.Find("DeckCardInfoStrip") != null)
            {
                Debug.LogWarning("DeckCardInfoStripPrefabBaker: Deck Card already contains 'DeckCardInfoStrip'. Remove it to re-bake.");
                return;
            }

            CardDisplay cardDisplay = deckRoot.GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                Debug.LogError("DeckCardInfoStripPrefabBaker: Deck Card root has no CardDisplay.");
                return;
            }

            Transform name = deckRoot.transform.Find("name");
            Transform atk = deckRoot.transform.Find("atk");
            Transform hp = deckRoot.transform.Find("dtf");
            if (name == null || atk == null || hp == null)
            {
                Debug.LogError(
                    "DeckCardInfoStripPrefabBaker: 在 Deck Card 根下找不到 'name'、'atk'、'dtf'（若已包在 DeckCardInfoStrip 內，請先刪除 Strip 再烘焙）。");
                return;
            }

            GameObject stripRoot = new GameObject("DeckCardInfoStrip", typeof(RectTransform), typeof(DeckCardInfoStrip));
            RectTransform stripRt = stripRoot.GetComponent<RectTransform>();
            stripRt.SetParent(deckRoot.transform, false);
            stripRt.SetAsFirstSibling();
            stripRt.anchorMin = Vector2.zero;
            stripRt.anchorMax = Vector2.one;
            stripRt.offsetMin = Vector2.zero;
            stripRt.offsetMax = Vector2.zero;
            stripRt.pivot = new Vector2(0.5f, 0.5f);
            stripRt.localScale = Vector3.one;
            stripRt.localRotation = Quaternion.identity;

            name.SetParent(stripRoot.transform, true);
            atk.SetParent(stripRoot.transform, true);
            hp.SetParent(stripRoot.transform, true);

            DeckCardInfoStrip strip = stripRoot.GetComponent<DeckCardInfoStrip>();
            strip.AssignReferences(
                name.GetComponent<TextMeshProUGUI>(),
                atk.GetComponent<TextMeshProUGUI>(),
                hp.GetComponent<TextMeshProUGUI>());
            strip.BindToCardDisplay(cardDisplay);

            PrefabUtility.SaveAsPrefabAsset(stripRoot, stripPrefabPath);

            PrefabUtility.SaveAsPrefabAsset(deckRoot, deckCardPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("DeckCardInfoStripPrefabBaker: Saved '" + stripPrefabPath + "' and updated '" + deckCardPrefabPath + "'.");
        }
        finally
        {
            if (deckRoot != null)
                PrefabUtility.UnloadPrefabContents(deckRoot);
        }
    }
}

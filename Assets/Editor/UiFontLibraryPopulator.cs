using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 建立 / 重新整理 Assets/Resources/UiFontLibrary.asset：
/// 以 AssetDatabase 找出 CJK 主字型（Noto Sans TC）與預設 UI 字型（LiberationSans），存成直接引用。
/// 字型更新後重跑本選單即可同步。
/// 選單：Tools/UI/Create or Refresh UI Font Library
/// </summary>
public static class UiFontLibraryPopulator
{
    private const string LibraryAssetPath = "Assets/Resources/UiFontLibrary.asset";

    [MenuItem("Tools/UI/Create or Refresh UI Font Library")]
    public static void CreateOrRefresh()
    {
        UiFontLibrary library = AssetDatabase.LoadAssetAtPath<UiFontLibrary>(LibraryAssetPath);
        if (library == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            library = ScriptableObject.CreateInstance<UiFontLibrary>();
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
            Debug.Log($"UiFontLibraryPopulator: 已建立新資產 {LibraryAssetPath}");
        }

        TMP_FontAsset cjk = FindFontAsset("noto", "sourcehan", "cjk");
        Font legacySource = AssetDatabase.LoadAssetAtPath<Font>("Assets/NotoSansTC-VariableFont_wght.ttf");
        library.EditorSetFonts(cjk, cjk, legacySource);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"UiFontLibraryPopulator: cjk={(cjk != null ? cjk.name : "<null>")}, " +
            $"legacy={(legacySource != null ? legacySource.name : "<null>")} → {LibraryAssetPath}");
    }

    // 依名稱關鍵字找 TMP_FontAsset；優先非 "fallback" 的主檔。
    private static TMP_FontAsset FindFontAsset(params string[] nameKeywords)
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        TMP_FontAsset fallbackMatch = null;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) continue;

            string lower = font.name.ToLowerInvariant();
            bool matched = false;
            for (int k = 0; k < nameKeywords.Length; k++)
            {
                if (lower.Contains(nameKeywords[k]))
                {
                    matched = true;
                    break;
                }
            }
            if (!matched) continue;

            if (lower.Contains("fallback"))
            {
                if (fallbackMatch == null) fallbackMatch = font;
                continue;
            }
            return font;
        }
        return fallbackMatch;
    }
}

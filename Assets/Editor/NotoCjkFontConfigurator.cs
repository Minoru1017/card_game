using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 將 Noto Sans TC SDF 設為動態圖集並加入 LiberationSans 後備，避免靜態圖集缺字。
/// 選單：Tools/UI/Configure Noto CJK Font (Dynamic)
/// </summary>
public static class NotoCjkFontConfigurator
{
    private const string NotoSdfPath = "Assets/Assets/NotoSansTC-VariableFont_wght SDF.asset";
    private const string NotoTtfPath = "Assets/NotoSansTC-VariableFont_wght.ttf";
    private const string LiberationFallbackPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

    [MenuItem("Tools/UI/Configure Noto CJK Font (Dynamic)")]
    public static void Configure()
    {
        TMP_FontAsset noto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSdfPath);
        if (noto == null)
        {
            Debug.LogError($"NotoCjkFontConfigurator: 找不到 {NotoSdfPath}");
            return;
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>(NotoTtfPath);
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);

        SerializedObject so = new SerializedObject(noto);
        SerializedProperty population = so.FindProperty("m_AtlasPopulationMode");
        SerializedProperty sourceFile = so.FindProperty("m_SourceFontFile");
        SerializedProperty fallbackTable = so.FindProperty("m_FallbackFontAssetTable");

        if (population != null)
            population.intValue = 1;

        if (sourceFile != null && source != null)
            sourceFile.objectReferenceValue = source;

        if (fallbackTable != null && fallback != null)
        {
            fallbackTable.ClearArray();
            fallbackTable.InsertArrayElementAtIndex(0);
            fallbackTable.GetArrayElementAtIndex(0).objectReferenceValue = fallback;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(noto);
        AssetDatabase.SaveAssets();

        Debug.Log("NotoCjkFontConfigurator: Noto 已設為動態圖集並加入 LiberationSans 後備字型。");
    }
}

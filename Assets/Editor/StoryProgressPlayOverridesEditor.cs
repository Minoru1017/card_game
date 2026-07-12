#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoryProgressPlayOverrides))]
public sealed class StoryProgressPlayOverridesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "勾選＝該里程碑已完成；未勾選＝Play 時從該段重新開始。\n" +
            "1-3 需先勾選 1-2 全關通關，地圖才會解鎖 M-1-3。\n" +
            "需將資產放在 Assets/Resources/StoryProgressPlayOverrides.asset，或場景掛 StoryProgressPlayOverridesHost。",
            MessageType.Info);

        var asset = (StoryProgressPlayOverrides)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("立即套用到存檔槽（Play Mode）"))
                asset.ApplyToActiveSlot();
        }

        if (GUILayout.Button("建立／定位 Resources 預設資產"))
            StoryProgressPlayOverridesAssetUtility.CreateOrSelectDefaultAsset();
    }
}

[CustomEditor(typeof(StoryProgressPlayOverridesHost))]
public sealed class StoryProgressPlayOverridesHostEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "未指定 Overrides Asset 時，使用 Inline Overrides。\n" +
            "進入 Play Mode 會自動寫入存檔（若 applyOnEnterPlayMode 已勾選）。",
            MessageType.Info);
    }
}

public static class StoryProgressPlayOverridesAssetUtility
{
    private const string DefaultAssetPath = "Assets/Resources/StoryProgressPlayOverrides.asset";

    [MenuItem("Card Game/Story Progress/Create Play Overrides Asset")]
    public static void CreateOrSelectDefaultAsset()
    {
        StoryProgressPlayOverrides existing = AssetDatabase.LoadAssetAtPath<StoryProgressPlayOverrides>(DefaultAssetPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        StoryProgressPlayOverrides asset = ScriptableObject.CreateInstance<StoryProgressPlayOverrides>();
        AssetDatabase.CreateAsset(asset, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
#endif

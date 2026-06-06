using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一鍵在畫布上佈署 <see cref="ResponsiveBaseboardLayout"/>：
/// 掛元件、建立左右底板容器（Baseboard_Left / Baseboard_Right，置於最底層＝渲染於內容之下）、
/// 建立中央保險區（SafeArea_1440），並完成接線與貼高度設定。
///
/// 選單：Tools/UI/Setup Responsive Baseboards (selected or active Canvas)
/// 底板容器內請自行放入底板美術（Image，anchor 設 stretch 充滿容器）。
/// </summary>
public static class ResponsiveBaseboardSetup
{
    private const string LeftName = "Baseboard_Left";
    private const string RightName = "Baseboard_Right";
    private const string SafeName = "SafeArea_1440";

    [MenuItem("Tools/UI/Setup Responsive Baseboards (selected or active Canvas)")]
    public static void Setup()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Responsive Baseboards",
                "找不到 Canvas。請先選取一個 Canvas（或其子物件），或確認目前場景有 Canvas。",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Setup Responsive Baseboards");
        int group = Undo.GetCurrentGroup();

        ResponsiveBaseboardLayout layout = canvas.GetComponent<ResponsiveBaseboardLayout>();
        if (layout == null)
            layout = Undo.AddComponent<ResponsiveBaseboardLayout>(canvas.gameObject);

        RectTransform left = EnsureBoard(canvas, LeftName);
        RectTransform right = EnsureBoard(canvas, RightName);
        RectTransform safe = EnsureSafeArea(canvas);

        // 底板置於最底層，確保渲染在其他內容之下。
        left.SetSiblingIndex(0);
        right.SetSiblingIndex(1);

        SerializedObject so = new SerializedObject(layout);
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("scaler").objectReferenceValue = canvas.GetComponent<CanvasScaler>();
        so.FindProperty("leftBaseboard").objectReferenceValue = left;
        so.FindProperty("rightBaseboard").objectReferenceValue = right;
        so.FindProperty("safeArea").objectReferenceValue = safe;
        so.ApplyModifiedPropertiesWithoutUndo();

        layout.Apply();

        EditorUtility.SetDirty(layout);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Undo.CollapseUndoOperations(group);
        Selection.activeObject = layout;

        Debug.Log(
            $"ResponsiveBaseboardSetup: 已在 '{canvas.name}' 佈署底板（match=1 貼高度）。" +
            "請在 Baseboard_Left / Baseboard_Right 內放入底板美術（Image，anchor 設 stretch）。");
    }

    private static Canvas ResolveCanvas()
    {
        GameObject sel = Selection.activeGameObject;
        if (sel != null)
        {
            Canvas c = sel.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
        }
        return Object.FindFirstObjectByType<Canvas>();
    }

    private static RectTransform EnsureBoard(Canvas canvas, string name)
    {
        Transform existing = canvas.transform.Find(name);
        if (existing != null) return existing as RectTransform;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create Baseboard");
        go.transform.SetParent(canvas.transform, false);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.08f, 0.10f, 0.16f, 1f); // 佔位色，請替換為底板美術
        img.raycastTarget = false;

        return go.GetComponent<RectTransform>();
    }

    private static RectTransform EnsureSafeArea(Canvas canvas)
    {
        Transform existing = canvas.transform.Find(SafeName);
        if (existing != null) return existing as RectTransform;

        GameObject go = new GameObject(SafeName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create SafeArea");
        go.transform.SetParent(canvas.transform, false);
        return go.GetComponent<RectTransform>();
    }
}

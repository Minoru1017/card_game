#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Main Plot 場景音訊元件一鍵補掛（Main Camera）。</summary>
public static class MainPlotSceneSetup
{
    private const string MainPlotScenePath = "Assets/Scenes/Main Plot.unity";
    private const string MainCameraName = "Main Camera";

    [MenuItem("Tools/Main Plot/Ensure Plot NPC Voice Player")]
    public static void EnsurePlotNpcVoicePlayer()
    {
        EnsureComponentOnMainCamera<PlotNpcVoicePlayer>((player, added) =>
        {
            if (!added)
                return;

            SerializedObject so = new SerializedObject(player);
            SerializedProperty volumeProp = so.FindProperty("volume");
            if (volumeProp != null)
            {
                volumeProp.floatValue = 1f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        });
    }

    [MenuItem("Tools/Main Plot/Select Main Camera Audio Components")]
    public static void SelectMainCameraAudioComponents()
    {
        PlotNpcVoicePlayer player = FindMainCameraComponent<PlotNpcVoicePlayer>();
        if (player != null)
        {
            Selection.activeObject = player;
            EditorGUIUtility.PingObject(player);
            return;
        }

        PlotBackgroundMusicPlayer bgm = FindMainCameraComponent<PlotBackgroundMusicPlayer>();
        if (bgm != null)
        {
            Selection.activeObject = bgm;
            EditorGUIUtility.PingObject(bgm);
            return;
        }

        Debug.LogWarning("MainPlotSceneSetup: 請先開啟 Main Plot 場景，或執行 Ensure Plot NPC Voice Player。");
    }

    private static void EnsureComponentOnMainCamera<T>(System.Action<T, bool> configure = null)
        where T : Component
    {
        if (!System.IO.File.Exists(MainPlotScenePath))
        {
            Debug.LogError("MainPlotSceneSetup: 找不到場景 " + MainPlotScenePath);
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        bool wasMainPlotOpen = active.IsValid() && active.path == MainPlotScenePath;

        if (!wasMainPlotOpen)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            active = EditorSceneManager.OpenScene(MainPlotScenePath, OpenSceneMode.Single);
        }

        GameObject mainCamera = GameObject.Find(MainCameraName);
        if (mainCamera == null)
        {
            Debug.LogError("MainPlotSceneSetup: Main Plot 場景找不到 Main Camera。");
            return;
        }

        T component = mainCamera.GetComponent<T>();
        bool added = component == null;
        if (added)
            component = Undo.AddComponent<T>(mainCamera.gameObject);

        configure?.Invoke(component, added);
        EditorUtility.SetDirty(mainCamera);
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        Selection.activeObject = component;
        EditorGUIUtility.PingObject(component);
        Debug.Log("MainPlotSceneSetup: 已在 Main Camera 掛上 " + typeof(T).Name + " 並保存場景。");
    }

    private static T FindMainCameraComponent<T>() where T : Component
    {
        GameObject mainCamera = GameObject.Find(MainCameraName);
        return mainCamera != null ? mainCamera.GetComponent<T>() : null;
    }
}
#endif

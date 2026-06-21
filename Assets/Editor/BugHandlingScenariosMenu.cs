#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Bug handling scenarios 場景捷徑。</summary>
public static class BugHandlingScenariosMenu
{
    private const string ScenePath = "Assets/Scenes/Bug handling scenarios.unity";

    [MenuItem("Tools/Bug Scenarios/Open Deck Slot Name Test Scene")]
    private static void OpenDeckSlotNameTestScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/Bug Scenarios/Add Deck Slot Name Scenario To Open Scene")]
    private static void AddScenarioToOpenScene()
    {
        if (Object.FindFirstObjectByType<BugHandlingDeckSlotNameScenario>() != null)
        {
            Debug.Log("BugHandlingDeckSlotNameScenario already exists in scene.");
            return;
        }

        GameObject root = new GameObject("BugHandlingDeckSlotNameScenario");
        root.AddComponent<BugHandlingDeckSlotNameScenario>();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
    }

    [MenuItem("Tools/Bug Scenarios/Repair Deck Slot Name Pollution In Save")]
    private static void RepairDeckSlotNamePollutionInSave()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "清理存檔污染",
                "請進入 Play Mode 後再執行，以便同步 profile 摘要。",
                "確定");
            return;
        }

        var report = PlayerDeckSlotNameStorage.RepairPersistedDeckSlotNamePollution(syncActiveSlotProfile: true);
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd != null) pd.LoadPlayerData();

        Debug.Log(
            "[BugHandlingScenariosMenu] Repair complete: scanned=" + report.DeckSlotNameRowsScanned +
            " repaired=" + report.DeckSlotNameRowsRepaired +
            " profile_removed=" + report.ProfileDecksRowsRemoved);
    }
}
#endif

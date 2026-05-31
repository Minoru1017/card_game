#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor：港灣普通 × 入門預設牌組勝率批次模擬。</summary>
public static class HarborNormalWinRateSim
{
    private const string BattleScenePath = "Assets/Scenes/BattleSimulation.unity";

    [MenuItem("Tools/Harbor/Win Rate Sim (Normal, Tutorial Deck, 200 games)")]
    public static void RunFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "港灣普通勝率模擬",
                "將進入 Play Mode 並自動跑 200 局（約 1～3 分鐘）。\n" +
                "條件：入門 30 張預設牌組 vs 港灣普通 FastAttack 敵方構築。\n\n繼續？",
                "開始",
                "取消"))
            return;

        RunEditorPlayMode(200, HarborNormalWinRateSimBootstrap.DefaultBaseSeed);
    }

    [MenuItem("Tools/Harbor/Win Rate Sim (Normal, 50 games — quick)")]
    public static void RunQuickFromMenu() => RunEditorPlayMode(50, HarborNormalWinRateSimBootstrap.DefaultBaseSeed);

    public static void RunEditorPlayMode(int games, int baseSeed)
    {
        EditorSceneManager.OpenScene(BattleScenePath);
        HarborNormalWinRateSimBootstrap.ArmForEditorPlayMode(games, baseSeed);
        EditorApplication.EnterPlaymode();
    }

    /// <summary>命令列：Unity -batchmode -projectPath ... -executeMethod HarborNormalWinRateSim.RunBatchMode -harborNormalWinRateSim</summary>
    public static void RunBatchMode()
    {
        EditorSceneManager.OpenScene(BattleScenePath);
        EditorApplication.EnterPlaymode();
    }
}
#endif

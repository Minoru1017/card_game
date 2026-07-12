#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor：M-1-2 段考 A 勝率／通關率批次模擬。</summary>
public static class M12PhaseAWinRateSim
{
    [MenuItem("Tools/M-1-2/Win Rate Sim (Phase A Exam, 200 games)")]
    public static void RunFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "M-1-2 段考 A 批次模擬",
                "將進入 Play Mode 並自動跑 200 局（約 1～3 分鐘）。\n" +
                "條件：段考 A 15 張牌組 vs 鏡像敵方 · 統計勝率與「勝+三戰技」通關率。\n\n繼續？",
                "開始",
                "取消"))
            return;

        RunEditorPlayMode(M12PhaseAWinRateSimBootstrap.DefaultGameCount, M12PhaseAWinRateSimBootstrap.DefaultBaseSeed);
    }

    [MenuItem("Tools/M-1-2/Win Rate Sim (Phase A Exam, 50 games — quick)")]
    public static void RunQuickFromMenu() =>
        RunEditorPlayMode(M12PhaseAWinRateSimBootstrap.DefaultQuickGameCount, M12PhaseAWinRateSimBootstrap.DefaultBaseSeed);

    public static void RunEditorPlayMode(int games, int baseSeed)
    {
        EditorSceneManager.OpenScene(M12PhaseAWinRateSimBootstrap.BattleScenePath);
        M12PhaseAWinRateSimBootstrap.ArmForEditorPlayMode(games, baseSeed);
        EditorApplication.EnterPlaymode();
    }

    /// <summary>命令列：Unity -batchmode -projectPath ... -executeMethod M12PhaseAWinRateSim.RunBatchMode -m12PhaseAWinRateSim</summary>
    public static void RunBatchMode()
    {
        EditorSceneManager.OpenScene(M12PhaseAWinRateSimBootstrap.BattleScenePath);
        EditorApplication.EnterPlaymode();
    }
}
#endif

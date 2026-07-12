using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>命令列／Editor 批次：M-1-2 段考 A（御三家戰技 + 勝利）勝率／通關率模擬。</summary>
public sealed class M12PhaseAWinRateSimBootstrap : MonoBehaviour
{
    public const string BattleScenePath = "Assets/Scenes/BattleSimulation.unity";
    public const string CommandLineFlag = "-m12PhaseAWinRateSim";
    public const int DefaultGameCount = 200;
    public const int DefaultQuickGameCount = 50;
    public const int DefaultBaseSeed = 20260708;
    private const float BatchTimeScale = 80f;
    private const float OpeningPresentationStallSeconds = 3f;
    private const int ProgressLogInterval = 25;

    private static bool pendingFromCommandLine;

    public static bool IsBatchRunning { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TrySpawnFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], CommandLineFlag, StringComparison.OrdinalIgnoreCase))
            {
                pendingFromCommandLine = true;
                break;
            }
        }

        bool editorArmed = PlayerPrefs.GetInt(PrefArmed, 0) == 1;
        if (!pendingFromCommandLine && !editorArmed)
            return;

        BattleSimulationManager manager = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager != null)
            manager.autoStartOnPlay = false;

        GameObject host = new GameObject(nameof(M12PhaseAWinRateSimBootstrap));
        host.AddComponent<M12PhaseAWinRateSimBootstrap>();
    }

    private const string PrefArmed = "m12_phase_a_sim_armed";
    private const string PrefGames = "m12_phase_a_sim_games";
    private const string PrefSeed = "m12_phase_a_sim_seed";

    public static void ArmForEditorPlayMode(int games = DefaultGameCount, int baseSeed = DefaultBaseSeed)
    {
        PlayerPrefs.SetInt(PrefGames, games);
        PlayerPrefs.SetInt(PrefSeed, baseSeed);
        PlayerPrefs.SetInt(PrefArmed, 1);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        bool armed = PlayerPrefs.GetInt(PrefArmed, 0) == 1 || pendingFromCommandLine;
        if (!armed)
            return;

        int games = Mathf.Max(1, PlayerPrefs.GetInt(PrefGames, DefaultGameCount));
        int baseSeed = PlayerPrefs.GetInt(PrefSeed, DefaultBaseSeed);
        PlayerPrefs.DeleteKey(PrefArmed);
        PlayerPrefs.Save();

        StartCoroutine(RunBatch(games, baseSeed));
    }

    private IEnumerator RunBatch(int games, int baseSeed)
    {
        IsBatchRunning = true;
        BattleSimulationManager manager = null;
        float wait = 0f;
        while (manager == null && wait < 8f)
        {
            manager = FindFirstObjectByType<BattleSimulationManager>();
            if (manager != null)
                break;
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (manager == null)
        {
            Debug.LogError("M12PhaseAWinRateSim: BattleSimulationManager not found.");
            IsBatchRunning = false;
            QuitEditor();
            yield break;
        }

        manager.autoStartOnPlay = false;
        float savedOpening = manager.GetOpeningPresentationSeconds();
        manager.SetOpeningPresentationSeconds(0f);

        SetupM12PhaseA(manager);

        int wins = 0;
        int losses = 0;
        int draws = 0;
        int examPasses = 0;
        int winsWithoutTrio = 0;
        int trioMilitia = 0;
        int trioQueen = 0;
        int trioKing = 0;
        int aborted = 0;
        int totalRounds = 0;
        int finishedGames = 0;

        BattleAutoSimPlugin.ForceBatchRunning(true);
        float savedTimeScale = Time.timeScale;

        try
        {
            Time.timeScale = Mathf.Max(1f, BatchTimeScale);

            for (int g = 0; g < games; g++)
            {
                UnityEngine.Random.InitState(baseSeed + g * 7919);

                if (g > 0)
                    manager.StartBattle();

                float openingStall = 0f;
                while (manager.IsOpeningPresentationInProgress() && openingStall < OpeningPresentationStallSeconds)
                {
                    openingStall += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (manager.IsOpeningPresentationInProgress())
                    manager.ForceFinishOpeningPresentationForBatchSim();

                int steps = 0;
                float battleStall = 0f;
                while (!manager.IsBattleOver() && steps < BattleAutoSimPlugin.MaxStepsPerBattle)
                {
                    steps++;
                    battleStall += Time.unscaledDeltaTime;

                    if (manager.IsOpeningPresentationInProgress())
                    {
                        if (battleStall >= OpeningPresentationStallSeconds)
                            manager.ForceFinishOpeningPresentationForBatchSim();
                        yield return null;
                        continue;
                    }

                    if (manager.IsTurnSequenceInProgress() || manager.IsSpellCastPresentationActive())
                    {
                        yield return null;
                        continue;
                    }

                    battleStall = 0f;
                    int pumps = Mathf.Max(1, BattleAutoSimPlugin.BatchSimMaxPumpsPerFrame);
                    for (int p = 0; p < pumps && !manager.IsBattleOver(); p++)
                    {
                        if (manager.IsOpeningPresentationInProgress() ||
                            manager.IsTurnSequenceInProgress() ||
                            manager.IsSpellCastPresentationActive())
                            break;

                        if (!manager.IsPlayerTurn())
                            break;

                        HarborNormalWinRateSimPump.TryAutoPlayOneCard(manager);
                        if (manager.IsPlayerTurn() && !manager.IsBattleOver() &&
                            !manager.IsTurnSequenceInProgress() && !manager.IsSpellCastPresentationActive())
                        {
                            manager.EndPlayerTurn();
                        }

                        if (manager.IsTurnSequenceInProgress() || manager.IsSpellCastPresentationActive())
                            break;
                    }

                    yield return null;
                }

                if (steps >= BattleAutoSimPlugin.MaxStepsPerBattle && !manager.IsBattleOver())
                {
                    aborted++;
                    Debug.LogWarning("M12PhaseAWinRateSim: game " + (g + 1) + " hit step limit; counting as draw.");
                    draws++;
                    finishedGames++;
                    totalRounds += manager.GetCurrentRound();
                    if ((g + 1) % ProgressLogInterval == 0)
                    {
                        Debug.Log("M12PhaseAWinRateSim progress: " + (g + 1) + "/" + games +
                                  " W=" + wins + " L=" + losses + " D=" + draws + " examPass=" + examPasses +
                                  " aborted=" + aborted);
                    }
                    continue;
                }

                int r = manager.GetBattleResult();
                if (r == 1)
                {
                    wins++;
                    if (M12TrioMasteryBattleTracker.QueryAllTrioSkillsTriggered())
                        examPasses++;
                    else
                        winsWithoutTrio++;
                }
                else if (r == -1)
                {
                    losses++;
                }
                else
                {
                    draws++;
                }

                if (M12TrioMasteryBattleTracker.QueryMilitiaTriggered()) trioMilitia++;
                if (M12TrioMasteryBattleTracker.QueryQueenTriggered()) trioQueen++;
                if (M12TrioMasteryBattleTracker.QueryKingTriggered()) trioKing++;

                finishedGames++;
                totalRounds += manager.GetCurrentRound();

                if ((g + 1) % ProgressLogInterval == 0)
                {
                    Debug.Log("M12PhaseAWinRateSim progress: " + (g + 1) + "/" + games +
                              " W=" + wins + " L=" + losses + " D=" + draws + " examPass=" + examPasses +
                              " aborted=" + aborted);
                }
            }
        }
        finally
        {
            Time.timeScale = savedTimeScale;
            BattleAutoSimPlugin.ForceBatchRunning(false);
            manager.SetOpeningPresentationSeconds(savedOpening);
            IsBatchRunning = false;
        }

        float avgRounds = finishedGames > 0 ? (float)totalRounds / finishedGames : 0f;
        WriteReport(games, wins, losses, draws, examPasses, winsWithoutTrio, trioMilitia, trioQueen, trioKing,
            aborted, baseSeed, avgRounds);
        Debug.Log(BuildSummaryLine(games, wins, losses, draws, examPasses, winsWithoutTrio, aborted));
        QuitEditor();
    }

    private static void SetupM12PhaseA(BattleSimulationManager manager)
    {
        PlayerData playerData = manager.playerData != null
            ? manager.playerData
            : PlayerData.ResolveCanonical();
        if (playerData != null)
            playerData.LoadPlayerData();

        SceneLoader.PrepareM12PhaseABattleLaunch();
        SceneLoader.ApplyM12RuntimeConfigToManager(manager);
        manager.StartBattle();
    }

    private static void WriteReport(
        int games,
        int wins,
        int losses,
        int draws,
        int examPasses,
        int winsWithoutTrio,
        int trioMilitia,
        int trioQueen,
        int trioKing,
        int aborted,
        int baseSeed,
        float avgRounds)
    {
        int finished = wins + losses + draws;
        float winRate = finished > 0 ? (float)wins / finished : 0f;
        float examPassRate = finished > 0 ? (float)examPasses / finished : 0f;

        var sb = new StringBuilder();
        sb.AppendLine("# M-1-2 Phase A Exam Win Rate (段考 A 批次模擬)");
        sb.AppendLine();
        sb.AppendLine("- Player deck: M12PhaseDeckApplicator Phase A (15 cards)");
        sb.AppendLine("- Enemy: mirror Phase A deck · Balanced AI · 段考A · HP 15 · max 12 rounds");
        sb.AppendLine("- Exam pass: **win + militia + queen + king skills all triggered**");
        sb.AppendLine("- Auto-play: HarborNormalWinRateSimPump / BattleAutoSimPlugin heuristics");
        sb.AppendLine("- Horror presentation: **skipped** during batch (`BattleAutoSimPlugin.IsRunning`); damage freeze still applies");
        sb.AppendLine("- Round cap: after round " + M12PhaseABattleRules.MaxRoundsInclusive +
                       ", winner by hero HP (tie = draw)");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine("| Games requested | " + games + " |");
        sb.AppendLine("| Games finished | " + finished + " |");
        sb.AppendLine("| Wins | " + wins + " |");
        sb.AppendLine("| Losses | " + losses + " |");
        sb.AppendLine("| Draws | " + draws + " |");
        sb.AppendLine("| **Exam passes** (win + trio) | " + examPasses + " |");
        sb.AppendLine("| Wins without trio | " + winsWithoutTrio + " |");
        sb.AppendLine("| Win rate | " + (winRate * 100f).ToString("F1") + "% |");
        sb.AppendLine("| **Exam pass rate** | " + (examPassRate * 100f).ToString("F1") + "% |");
        sb.AppendLine("| Games with militia skill | " + trioMilitia + " |");
        sb.AppendLine("| Games with queen skill | " + trioQueen + " |");
        sb.AppendLine("| Games with king skill | " + trioKing + " |");
        sb.AppendLine("| Aborted (step limit) | " + aborted + " |");
        sb.AppendLine("| Avg rounds (finished) | " + avgRounds.ToString("F1") + " |");
        sb.AppendLine("| Base seed | " + baseSeed + " |");
        sb.AppendLine();
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        string dir = Path.Combine(Application.dataPath, "SimResults");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "m12_phase_a_exam_winrate.md");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log("M12PhaseAWinRateSim: wrote " + path);
    }

    private static string BuildSummaryLine(
        int games,
        int wins,
        int losses,
        int draws,
        int examPasses,
        int winsWithoutTrio,
        int aborted)
    {
        int finished = wins + losses + draws;
        float winRate = finished > 0 ? (float)wins / finished : 0f;
        float examPassRate = finished > 0 ? (float)examPasses / finished : 0f;
        return "M12PhaseAWinRateSim: " + finished + "/" + games + " games | W=" + wins + " L=" + losses +
               " D=" + draws + " examPass=" + examPasses + " winNoTrio=" + winsWithoutTrio +
               " aborted=" + aborted + " | win=" + (winRate * 100f).ToString("F1") + "% examPass=" +
               (examPassRate * 100f).ToString("F1") + "%";
    }

    private static void QuitEditor()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#endif
        Application.Quit(0);
    }
}

using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>命令列／Editor 批次：港灣普通難度 × 入門預設 30 張牌組勝率模擬。</summary>
public sealed class HarborNormalWinRateSimBootstrap : MonoBehaviour
{
    public const string CommandLineFlag = "-harborNormalWinRateSim";
    public const int DefaultGameCount = 200;
    public const int DefaultBaseSeed = 20260531;

    private static bool _pendingFromCommandLine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TrySpawnFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], CommandLineFlag, StringComparison.OrdinalIgnoreCase))
            {
                _pendingFromCommandLine = true;
                break;
            }
        }

        bool editorArmed = PlayerPrefs.GetInt("harbor_sim_armed", 0) == 1;
        if (!_pendingFromCommandLine && !editorArmed) return;

        // Before BattleSimulationManager.Start → autoStartOnPlay, disable stray default battles.
        BattleSimulationManager manager = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager != null)
            manager.autoStartOnPlay = false;

        GameObject host = new GameObject(nameof(HarborNormalWinRateSimBootstrap));
        host.AddComponent<HarborNormalWinRateSimBootstrap>();
    }

    public static void ArmForEditorPlayMode(int games = DefaultGameCount, int baseSeed = DefaultBaseSeed)
    {
        PlayerPrefs.SetInt("harbor_sim_games", games);
        PlayerPrefs.SetInt("harbor_sim_seed", baseSeed);
        PlayerPrefs.SetInt("harbor_sim_armed", 1);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        bool armed = PlayerPrefs.GetInt("harbor_sim_armed", 0) == 1 || _pendingFromCommandLine;
        if (!armed) return;

        int games = Mathf.Max(1, PlayerPrefs.GetInt("harbor_sim_games", DefaultGameCount));
        int baseSeed = PlayerPrefs.GetInt("harbor_sim_seed", DefaultBaseSeed);
        PlayerPrefs.DeleteKey("harbor_sim_armed");
        PlayerPrefs.Save();

        StartCoroutine(RunBatch(games, baseSeed));
    }

    private IEnumerator RunBatch(int games, int baseSeed)
    {
        BattleSimulationManager manager = null;
        float wait = 0f;
        while (manager == null && wait < 8f)
        {
            manager = FindFirstObjectByType<BattleSimulationManager>();
            if (manager != null) break;
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (manager == null)
        {
            Debug.LogError("HarborNormalWinRateSim: BattleSimulationManager not found.");
            QuitEditor();
            yield break;
        }

        manager.autoStartOnPlay = false;
        float savedOpening = manager.GetOpeningPresentationSeconds();
        manager.SetOpeningPresentationSeconds(0f);

        SetupHarborNormal(manager);

        int wins = 0;
        int losses = 0;
        int draws = 0;
        int aborted = 0;
        int totalRounds = 0;
        int finishedGames = 0;

        BattleAutoSimPlugin.ForceBatchRunning(true);

        try
        {
            for (int g = 0; g < games; g++)
            {
                UnityEngine.Random.InitState(baseSeed + g * 7919);

                if (g > 0)
                    manager.StartBattle();

                float stall = 0f;
                while (manager.IsOpeningPresentationInProgress() && stall < 2f)
                {
                    stall += Time.unscaledDeltaTime;
                    yield return null;
                }

                int steps = 0;
                while (!manager.IsBattleOver() && steps < BattleAutoSimPlugin.MaxStepsPerBattle)
                {
                    steps++;
                    if (manager.IsOpeningPresentationInProgress())
                    {
                        yield return null;
                        continue;
                    }

                    if (manager.IsTurnSequenceInProgress() || manager.IsSpellCastPresentationActive())
                    {
                        yield return null;
                        continue;
                    }

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

                if (steps >= BattleAutoSimPlugin.MaxStepsPerBattle)
                {
                    aborted++;
                    break;
                }

                int r = manager.GetBattleResult();
                if (r == 1) wins++;
                else if (r == -1) losses++;
                else draws++;

                finishedGames++;
                totalRounds += manager.GetCurrentRound();
            }
        }
        finally
        {
            BattleAutoSimPlugin.ForceBatchRunning(false);
            manager.SetOpeningPresentationSeconds(savedOpening);
        }

        float avgRounds = finishedGames > 0 ? (float)totalRounds / finishedGames : 0f;
        WriteReport(games, wins, losses, draws, aborted, baseSeed, avgRounds);
        Debug.Log(BuildSummaryLine(games, wins, losses, draws, aborted));
        QuitEditor();
    }

    private static void SetupHarborNormal(BattleSimulationManager manager)
    {
        PlayerData playerData = manager.playerData != null
            ? manager.playerData
            : PlayerData.ResolveCanonical();
        if (playerData != null)
        {
            playerData.LoadPlayerData();
            TutorialDeckApplicator.ApplyToActivePlayerDeck(playerData);
            playerData.LoadPlayerData();
        }

        BattleLaunchContext.BeginHarborTrainingGroundBattleLaunch();
        BattleLaunchContext.SetPendingDifficultyLabelZh("普通");
        BattleLaunchContext.ConfirmActiveBattleDifficulty("普通");
        SceneLoader.PrepareHarborTrainingBattleLaunch(BattleDifficultyTier.Normal);
        SceneLoader.ApplyHarborTrainingRuntimeConfigToManager(manager);
        manager.StartBattle();
    }

    private static void WriteReport(int games, int wins, int losses, int draws, int aborted, int baseSeed, float avgRounds)
    {
        int finished = wins + losses + draws;
        float winRate = finished > 0 ? (float)wins / finished : 0f;

        var sb = new StringBuilder();
        sb.AppendLine("# Harbor Normal Win Rate (Tutorial default deck)");
        sb.AppendLine();
        sb.AppendLine("- Player deck: TutorialDeckApplicator (30 cards, intro preset)");
        sb.AppendLine("- Enemy: Harbor Normal · FastAttack · HarborTrainingNormalBattleRules (fixed deck)");
        sb.AppendLine("- Hero HP: 20 / 19 · Weather: on · KPI target ~60% first clear");
        sb.AppendLine("- Auto-play: BattleAutoSimPlugin heuristics (spell-first chance ~0.22)");
        sb.AppendLine("- Setup: harbor launch context applied before first StartBattle (no scene auto-start leak)");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine("| Games requested | " + games + " |");
        sb.AppendLine("| Games finished | " + finished + " |");
        sb.AppendLine("| Wins | " + wins + " |");
        sb.AppendLine("| Losses | " + losses + " |");
        sb.AppendLine("| Draws | " + draws + " |");
        sb.AppendLine("| Aborted (step limit) | " + aborted + " |");
        sb.AppendLine("| Win rate | " + (winRate * 100f).ToString("F1") + "% |");
        sb.AppendLine("| Avg rounds (finished) | " + avgRounds.ToString("F1") + " |");
        sb.AppendLine("| Base seed | " + baseSeed + " |");
        sb.AppendLine();
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        string dir = Path.Combine(Application.dataPath, "SimResults");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "harbor_normal_tutorial_deck_winrate.md");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log("HarborNormalWinRateSim: wrote " + path);
    }

    private static string BuildSummaryLine(int games, int wins, int losses, int draws, int aborted)
    {
        int finished = wins + losses + draws;
        float winRate = finished > 0 ? (float)wins / finished : 0f;
        return "HarborNormalWinRateSim: " + finished + "/" + games + " games | W=" + wins + " L=" + losses +
               " D=" + draws + " aborted=" + aborted + " | win rate=" + (winRate * 100f).ToString("F1") + "%";
    }

    private static void QuitEditor()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#endif
        Application.Quit(0);
    }
}

/// <summary>批次模擬用出牌邏輯（與 BattleAutoSimPlugin 一致）。</summary>
public static class HarborNormalWinRateSimPump
{
    public static void TryAutoPlayOneCard(BattleSimulationManager b)
    {
        if (b == null || !b.IsPlayerTurn()) return;
        if (b.GetPlayerPendingDiscardCount() > 0)
        {
            b.AutoDiscardOneForPlayer();
            return;
        }

        if (b.PlayerHasFieldMonster())
        {
            for (int i = 0; i < b.GetPlayerHandCount(); i++)
            {
                if (b.GetPlayerHandCard(i) is SpellCard sp && !IsPlayerSpellUnplayableNow(b, sp))
                {
                    b.PlayerPlayCardFromHand(i);
                    return;
                }
            }

            return;
        }

        bool spellFirst = UnityEngine.Random.value < b.AutoSimPlayerSpellFirstChance;
        if (spellFirst)
        {
            for (int i = 0; i < b.GetPlayerHandCount(); i++)
            {
                if (b.GetPlayerHandCard(i) is SpellCard sp && !IsPlayerSpellUnplayableNow(b, sp))
                {
                    b.PlayerPlayCardFromHand(i);
                    return;
                }
            }

            for (int i = 0; i < b.GetPlayerHandCount(); i++)
            {
                if (b.GetPlayerHandCard(i) is MonsterCard)
                {
                    b.PlayerPlayCardFromHand(i);
                    return;
                }
            }
        }
        else
        {
            for (int i = 0; i < b.GetPlayerHandCount(); i++)
            {
                if (b.GetPlayerHandCard(i) is MonsterCard)
                {
                    b.PlayerPlayCardFromHand(i);
                    return;
                }
            }

            for (int i = 0; i < b.GetPlayerHandCount(); i++)
            {
                if (b.GetPlayerHandCard(i) is SpellCard sp2 && !IsPlayerSpellUnplayableNow(b, sp2))
                {
                    b.PlayerPlayCardFromHand(i);
                    return;
                }
            }
        }
    }

    private static bool IsPlayerSpellUnplayableNow(BattleSimulationManager b, SpellCard sp)
    {
        if (sp == null) return true;
        if (b.PlayerHasFieldMonster())
        {
            if (sp.SpellOrdinal == 1) return false;
            return true;
        }

        if (sp.SpellOrdinal == 1) return true;
        if (sp.SpellOrdinal == 2 && !b.CanPlayerCastLinGazeNow()) return true;
        return false;
    }
}

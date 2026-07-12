using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MCP / 本機 Play Mode 測試自動化入口（僅 Unity Editor）。
/// 用法（MCP execute_code）：<c>return DevAutomation.TryAdvanceStep();</c>
/// </summary>
public static class DevAutomation
{
#if UNITY_EDITOR
    public static string GetStatus()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        var sb = new StringBuilder(512);
        sb.Append("scene=").Append(SceneManager.GetActiveScene().name);
        sb.Append(" playing=").Append(Application.isPlaying);
        sb.Append(" entryTransition=").Append(StoryLevelEntryTransition.IsPlaying);
        sb.Append(" selectedNode=").Append(StoryProgressWorldMapRuntime.SelectedStageNodeId);
        sb.Append(" | academyGrad=").Append(TutorialProgressState.IsAcademyIntroGraduated(slot));
        sb.Append(" harborClear=").Append(HarborTrainingProgressState.IsHarborCombatCleared(slot));
        sb.Append(" m12Avail=").Append(M12SeawallPatrolProgressState.IsNodeAvailable(slot));
        sb.Append(" m12Clear=").Append(M12SeawallPatrolProgressState.IsNodeCleared(slot));
        sb.Append(" m12Intro=").Append(TutorialProgressState.IsM12IntroSeen(slot));
        sb.Append(" m12PhaseA=").Append(M12SeawallPatrolProgressState.IsPhaseAComplete(slot));
        sb.Append(" m12Mid=").Append(M12SeawallPatrolProgressState.IsMidPatrolComplete(slot));

        BattleSimulationManager battle = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (battle != null)
            sb.Append(" | battleOver=").Append(battle.IsBattleOver())
                .Append(" result=").Append(battle.GetBattleResult());

        return sb.ToString();
    }

    public static string GoToHall()
    {
        EnsurePlaying();
        SceneManager.LoadScene(StoryProgressSession.HallSceneName);
        return "loaded hall";
    }

    public static string GoToStoryProgress()
    {
        EnsurePlaying();
        SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
        return "loaded Story progress";
    }

    /// <summary>解鎖 Story progress 地圖上的 M-1-2（港灣實戰首通 + 學院畢業）。</summary>
    public static string UnlockM12OnMap(bool refreshPresentation = true)
    {
        EnsurePlaying();
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.PersistAcademyIntroGraduated(slot, true);
        HarborTrainingProgressState.SetHarborCombatCleared(slot, true);
        HarborTrainingProgressState.EnsureSlotHarborProgressConsistent(slot);
        TutorialProgressState.EnsureSlotIntroProgressConsistent(slot);
        StoryProgressWorldMapRuntime.SetSelectedStageNodeId(StoryProgressSession.SeawallPatrolNodeId);
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        if (refreshPresentation)
            StoryProgressSceneController.RequestRefreshPresentation();
        return "unlocked M-1-2 map node; " + GetStatus();
    }

    public static string SelectStoryNode(string nodeId)
    {
        EnsurePlaying();
        if (string.IsNullOrWhiteSpace(nodeId))
            return "nodeId required";
        StoryProgressWorldMapRuntime.SetSelectedStageNodeId(nodeId.Trim());
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        StoryProgressSceneController.RequestRefreshPresentation();
        return "selected " + nodeId + "; " + GetStatus();
    }

    public static string EnterSelectedStoryStage()
    {
        EnsurePlaying();
        return InvokeButtonExact(StoryProgressSceneController.EnterStageButtonObjectName, requireInteractable: true);
    }

    public static string LaunchM12FromStoryProgress(bool unlockIfNeeded = true)
    {
        EnsurePlaying();
        if (unlockIfNeeded)
            UnlockM12OnMap(refreshPresentation: false);

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        StoryProgressWorldMapRuntime.SetSelectedStageNodeId(StoryProgressSession.SeawallPatrolNodeId);
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        StoryProgressSceneController.RequestRefreshPresentation();

        if (!M12SeawallPatrolFlow.CanEnterFromStoryProgress(slot))
            return "M-1-2 still locked; call UnlockM12OnMap first";

        M12SeawallPatrolFlow.LaunchFromStoryProgress();
        return "launched M-1-2 flow";
    }

    /// <summary>清除 M-1-2 中途進度（保留節點通關與獎勵旗標）。</summary>
    public static string ResetM12MidRunProgress()
    {
        EnsurePlaying();
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        TutorialProgressState.SetM12IntroSeen(slot, false);
        TutorialProgressState.SetM12PhaseAComplete(slot, false);
        TutorialProgressState.SetM12PhaseATrioMilitia(slot, false);
        TutorialProgressState.SetM12PhaseATrioQueen(slot, false);
        TutorialProgressState.SetM12PhaseATrioKing(slot, false);
        TutorialProgressState.SetM12MidPatrolComplete(slot, false);
        TutorialProgressState.SetM12SealedSpellFound(slot, false);
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        StoryProgressSceneController.RequestRefreshPresentation();
        return "reset M-1-2 mid-run flags";
    }

    public static string InvokeButton(string nameFragment, bool requireInteractable = false)
    {
        EnsurePlaying();
        if (string.IsNullOrWhiteSpace(nameFragment))
            return "nameFragment required";

        Button found = null;
        foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b == null || !b.gameObject.name.Contains(nameFragment))
                continue;
            found = b;
            break;
        }

        if (found == null)
            return "button not found: " + nameFragment;

        if (requireInteractable && !found.interactable)
            return "button disabled: " + found.gameObject.name;

        found.onClick.Invoke();
        return "clicked " + found.gameObject.name;
    }

    public static string InvokeButtonExact(string objectName, bool requireInteractable = false)
    {
        EnsurePlaying();
        if (string.IsNullOrWhiteSpace(objectName))
            return "objectName required";

        Button found = null;
        foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b != null && b.gameObject.name == objectName)
            {
                found = b;
                break;
            }
        }

        if (found == null)
            return "button not found: " + objectName;

        if (requireInteractable && !found.interactable)
            return "button disabled: " + objectName;

        found.onClick.Invoke();
        return "clicked " + objectName;
    }

    public static string SkipPlot()
    {
        EnsurePlaying();
        string skipped = InvokeButton("略過", requireInteractable: true);
        if (!skipped.StartsWith("clicked"))
            skipped = InvokeButtonExact("略過本段劇情", requireInteractable: true);
        if (skipped.StartsWith("clicked"))
            return skipped;
        return AdvancePlotTap();
    }

    public static string AdvancePlotTap()
    {
        EnsurePlaying();
        string tap = InvokeButtonExact("PlotTapToContinue", requireInteractable: false);
        if (tap.StartsWith("clicked"))
            return tap;
        return InvokeButton("PlotTapToContinue", requireInteractable: false);
    }

    public static string AdvanceM12Stroll()
    {
        EnsurePlaying();
        if (UnityEngine.Object.FindFirstObjectByType<M12SeawallStrollOverlay>() == null)
            return "M12SeawallStrollOverlay not active";

        foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b == null || !b.gameObject.activeInHierarchy || !b.interactable)
                continue;
            if (b.gameObject.name.StartsWith("Hotspot_"))
            {
                b.onClick.Invoke();
                return "clicked stroll hotspot " + b.gameObject.name;
            }
        }

        return InvokeButtonExact("ContinueButton", requireInteractable: true);
    }

    public static string ForceBattleWin()
    {
        EnsurePlaying();
        BattleSimulationManager mgr = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (mgr == null)
            return "BattleSimulationManager not found";

        if (mgr.IsBattleOver())
            return "battle already over; result=" + mgr.GetBattleResult();

        if (BattleLaunchContext.IsM12TrioMasteryBattle)
            ForceM12TrioTriggers();

        MethodInfo complete = typeof(BattleSimulationManager).GetMethod(
            "CompleteBattle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (complete == null)
            return "CompleteBattle reflection failed";

        complete.Invoke(mgr, new object[] { 1, string.Empty, false });
        return "forced battle win";
    }

    public static string ForceBattleLoss()
    {
        EnsurePlaying();
        BattleSimulationManager mgr = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (mgr == null)
            return "BattleSimulationManager not found";

        if (mgr.IsBattleOver())
            return "battle already over; result=" + mgr.GetBattleResult();

        MethodInfo complete = typeof(BattleSimulationManager).GetMethod(
            "CompleteBattle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (complete == null)
            return "CompleteBattle reflection failed";

        complete.Invoke(mgr, new object[] { -1, string.Empty, false });
        return "forced battle loss";
    }

    private static void ForceM12TrioTriggers()
    {
        M12TrioMasteryBattleTracker.NotifyMilitiaFormationTriggered(isPlayerSide: true);
        M12TrioMasteryBattleTracker.NotifyQueenShelterTriggered(isPlayerSide: true);
        M12TrioMasteryBattleTracker.NotifyKingDecreeTriggered(isPlayerSide: true);
    }

    /// <summary>1-1 學院入門：進關演出 → 劇情 → 教學對戰 → 結尾劇情。</summary>
    public static string LaunchM11FromStoryProgress()
    {
        EnsurePlaying();
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (TutorialProgressState.IsAcademyIntroGraduated(slot))
            return "M-1-1 already graduated; " + GetStatus();

        StoryProgressWorldMapRuntime.SetSelectedStageNodeId("M-1-1");
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        StoryProgressSceneController.RequestRefreshPresentation();
        StoryLevelEntryTransition.PlayToAcademyIntroPlot(replay: false);
        return "launched M-1-1 intro transition";
    }

    /// <summary>1-1 實戰區（港灣訓練場）：進關演出 → 難度預覽 → 對戰。</summary>
    public static string LaunchM11HarborFromStoryProgress()
    {
        EnsurePlaying();
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (!TutorialProgressState.IsAcademyIntroGraduated(slot))
            return "M-1-1 harbor locked — complete academy intro first; " + GetStatus();

        StoryProgressWorldMapRuntime.SetSelectedStageNodeId("M-1-1");
        StoryProgressWorldMapRuntime.RequestRefreshProgress();
        StoryProgressSceneController.RequestRefreshPresentation();
        StoryLevelEntryTransition.PlayToHarborTrainingPreview();
        return "launched M-1-1 harbor preview transition";
    }

    /// <summary>在 Play Mode 背景協程自動跑完 1-1 實戰區一局（可輸可贏，需數十秒）。</summary>
    public static string StartM11HarborCombatRoutine()
    {
        EnsurePlaying();
        return DevAutomationRoutineHost.Ensure().StartM11HarborCombat();
    }

    public static string GetM11HarborCombatRoutineStatus() =>
        DevAutomationRoutineHost.Ensure().GetHarborStatusText();

    public static bool IsM11HarborCombatRoutineRunning() =>
        DevAutomationRoutineHost.Ensure().IsHarborRunning;

    /// <summary>在 Play Mode 背景協程跑完 M-1-2 一局（可輸可贏，需數分鐘）。</summary>
    public static string StartM12PlayOnceRoutine(bool unlockIfNeeded = true)
    {
        EnsurePlaying();
        return DevAutomationRoutineHost.Ensure().StartM12PlayOnce(unlockIfNeeded);
    }

    public static string GetM12PlayOnceRoutineStatus() =>
        DevAutomationRoutineHost.Ensure().GetM12StatusText();

    public static bool IsM12PlayOnceRoutineRunning() =>
        DevAutomationRoutineHost.Ensure().IsM12Running;

    /// <summary>Editor：開 BattleSimulation、Arm 段考 A 批次模擬、進 Play Mode（結束自動 Stop）。</summary>
    public static string StartM12PhaseAWinRateSim(
        int games = M12PhaseAWinRateSimBootstrap.DefaultQuickGameCount,
        int baseSeed = M12PhaseAWinRateSimBootstrap.DefaultBaseSeed)
    {
        if (M12PhaseAWinRateSimBootstrap.IsBatchRunning)
            return "M12 Phase A win rate sim already running";

        if (Application.isPlaying)
            return "Exit Play Mode first, then call StartM12PhaseAWinRateSim (batch arms on next Play entry)";

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(M12PhaseAWinRateSimBootstrap.BattleScenePath);
        M12PhaseAWinRateSimBootstrap.ArmForEditorPlayMode(games, baseSeed);
        UnityEditor.EditorApplication.EnterPlaymode();
        return "starting M12 Phase A win rate sim: games=" + games + " seed=" + baseSeed;
    }

    public static string GetM12PhaseAWinRateSimStatus() =>
        "batchRunning=" + M12PhaseAWinRateSimBootstrap.IsBatchRunning + " | " + GetStatus();

    /// <summary>Editor：將 Resources 預設一套用到目前場景的 BattleSimulationManager（勿用 execute_menu_item 子選單路徑）。</summary>
    public static string ApplyBattleCardTuningPreset1ToOpenScene()
    {
        BattleSimulationManager manager = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (manager == null)
            return "error: open BattleSimulation scene first (no BattleSimulationManager)";

        if (!BattleCardTuningPresetLibrary.TryApplyPreset(manager, BattleCardTuningPresetLibrary.Preset1Id))
            return "error: preset1 missing in Resources/BattleCardTuningPresets.json";

        UnityEditor.EditorUtility.SetDirty(manager);
        return "ok: applied " + BattleCardTuningPresetLibrary.Preset1DisplayName;
    }

    /// <summary>在 Play Mode 背景協程自動跑完 1-1（需數十秒）。</summary>
    public static string StartM11ClearRoutine()
    {
        EnsurePlaying();
        return DevAutomationRoutineHost.Ensure().StartM11Clear();
    }

    public static string GetM11ClearRoutineStatus() =>
        DevAutomationRoutineHost.Ensure().GetStatusText();

    public static bool IsM11ClearRoutineRunning() =>
        DevAutomationRoutineHost.Ensure().IsRunning;

    /// <summary>依目前場景推進一步（劇情略過／散策／結算／對戰強制勝利等）。</summary>
    public static string TryAdvanceStep()
    {
        EnsurePlaying();

        if (StoryLevelEntryTransition.IsPlaying &&
            GameObject.Find("BattlePreviewOverlay") == null &&
            GameObject.Find("EnemyHeroPortraitBridge") == null &&
            GameObject.Find("M12SettlementOverlay") == null &&
            GameObject.Find("M12PhaseAExamMemoOverlay") == null)
            return "waiting: entry transition playing";

        string deckNotify = TryDismissStarterDeckNotify();
        if (deckNotify != null)
            return deckNotify;

        if (GameObject.Find(TutorialPlotStarterDeckNotify.OverlayRootName) != null)
            return "waiting: starter deck notify visible";

        string harborUi = TryAdvanceHarborCombatUi();
        if (harborUi != null)
            return harborUi;

        string memo = TryDismissM12ExamMemo();
        if (memo != null)
            return memo;

        BattleSimulationManager battle = UnityEngine.Object.FindFirstObjectByType<BattleSimulationManager>();
        if (battle != null && !battle.IsBattleOver())
            return ForceBattleWin();

        string[] settlementButtons = {
            "ReturnBuildbeckButton", "繼續Button", "Btn_繼續", "Btn_繼續散策",
            "Btn_前往加練", "Btn_返回地圖", "ContinueButton"
        };
        for (int i = 0; i < settlementButtons.Length; i++)
        {
            string result = InvokeButtonExact(settlementButtons[i], requireInteractable: true);
            if (result.StartsWith("clicked"))
                return "settlement: " + result;
        }

        if (UnityEngine.Object.FindFirstObjectByType<M12SeawallStrollOverlay>() != null)
        {
            string stroll = AdvanceM12Stroll();
            if (!stroll.Contains("not active"))
                return "stroll: " + stroll;
        }

        if (SceneManager.GetActiveScene().name == StoryProgressSession.MainPlotSceneName)
        {
            Button skipBtn = FindPlotSkipButton();
            if (skipBtn != null && skipBtn.interactable && skipBtn.gameObject.activeInHierarchy)
            {
                string skip = SkipPlot();
                if (!skip.Contains("not found"))
                    return "plot: " + skip;
            }
        }

        string hallNav = InvokeButton("遊戲進度", requireInteractable: true);
        if (hallNav.StartsWith("clicked"))
            return "nav: " + hallNav;

        return "idle: no action taken; " + GetStatus();
    }

    private static string TryDismissM12ExamMemo()
    {
        GameObject overlay = GameObject.Find("M12PhaseAExamMemoOverlay");
        if (overlay == null)
            return null;

        Transform dismiss = overlay.transform.Find("Panel/Dismiss");
        if (dismiss == null)
        {
            foreach (Transform t in overlay.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == "Dismiss")
                {
                    dismiss = t;
                    break;
                }
            }
        }

        Button dismissBtn = dismiss != null ? dismiss.GetComponent<Button>() : null;
        if (dismissBtn != null)
        {
            dismissBtn.onClick.Invoke();
            return "M12 exam memo dismissed";
        }

        return "waiting: M12 exam memo visible";
    }

    private static string TryAdvanceHarborCombatUi()
    {
        if (GameObject.Find("BattlePreviewOverlay") != null)
        {
            string start = InvokeButtonExact("StartBattleButton", requireInteractable: true);
            if (start.StartsWith("clicked"))
                return "harbor preview: " + start;

            foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b == null || !b.gameObject.activeInHierarchy || !b.interactable)
                    continue;
                if (!b.gameObject.name.StartsWith("DiffArch_0_"))
                    continue;
                b.onClick.Invoke();
                return "harbor preview: selected " + b.gameObject.name;
            }
        }

        string portrait = InvokeButtonExact("ContinueBtn", requireInteractable: true);
        if (portrait.StartsWith("clicked"))
            return "harbor portrait: " + portrait;

        string direct = InvokeButtonExact("DirectBtn", requireInteractable: true);
        if (direct.StartsWith("clicked"))
            return "harbor duel: " + direct;

        return null;
    }

    private static Button FindPlotSkipButton()
    {
        foreach (Button b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b != null && b.gameObject.name == "略過本段劇情")
                return b;
        }

        return null;
    }

    private static string TryDismissStarterDeckNotify()
    {
        GameObject overlay = GameObject.Find(TutorialPlotStarterDeckNotify.OverlayRootName);
        if (overlay == null)
            return null;

        Transform dim = overlay.transform.Find("Dim");
        Button dimBtn = dim != null ? dim.GetComponent<Button>() : null;
        if (dimBtn != null)
        {
            dimBtn.onClick.Invoke();
            return "starter deck notify dismissed";
        }

        TutorialPlotStarterDeckNotify.DismissExisting();
        return "starter deck notify force dismissed";
    }

    private static void EnsurePlaying()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("DevAutomation: enter Play Mode first.");
    }

    internal sealed class DevAutomationRoutineHost : MonoBehaviour
    {
        private static DevAutomationRoutineHost instance;
        private string statusText = "idle";
        private string harborStatusText = "idle";
        private string m12StatusText = "idle";
        private bool isRunning;
        private bool isHarborRunning;
        private bool isM12Running;

        public bool IsRunning => isRunning;
        public bool IsHarborRunning => isHarborRunning;
        public bool IsM12Running => isM12Running;

        public static DevAutomationRoutineHost Ensure()
        {
            if (instance != null)
                return instance;

            var go = new GameObject("__DevAutomationRoutineHost");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DevAutomationRoutineHost>();
            return instance;
        }

        public string StartM11Clear()
        {
            if (isRunning)
                return "M11 clear routine already running; " + statusText;

            StartCoroutine(CoM11Clear());
            return "M11 clear routine started";
        }

        public string GetStatusText() =>
            (isRunning ? "running: " : "done: ") + statusText + " | " + GetStatus();

        public string GetHarborStatusText() =>
            (isHarborRunning ? "running: " : "done: ") + harborStatusText + " | " + GetStatus();

        public string GetM12StatusText() =>
            (isM12Running ? "running: " : "done: ") + m12StatusText + " | " + GetStatus();

        public string StartM12PlayOnce(bool unlockIfNeeded)
        {
            if (isM12Running)
                return "M12 play-once routine already running; " + m12StatusText;

            StartCoroutine(CoM12PlayOnce(unlockIfNeeded));
            return "M12 play-once routine started";
        }

        private IEnumerator CoM12PlayOnce(bool unlockIfNeeded)
        {
            isM12Running = true;
            try
            {
                int slot = PlayerData.GetActivePlayerSlotOrDefault();
                if (unlockIfNeeded && !M12SeawallPatrolFlow.CanEnterFromStoryProgress(slot))
                    UnlockM12OnMap(refreshPresentation: false);

                if (!M12SeawallPatrolFlow.CanEnterFromStoryProgress(slot))
                {
                    m12StatusText = "M-1-2 locked — clear M-1-1 harbor first";
                    yield break;
                }

                if (SceneManager.GetActiveScene().name != StoryProgressSession.StoryProgressSceneName)
                {
                    m12StatusText = "loading Story progress";
                    SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
                    yield return new WaitForSecondsRealtime(1f);
                }

                m12StatusText = "launch M-1-2";
                LaunchM12FromStoryProgress(unlockIfNeeded: false);

                float transitionDeadline = Time.realtimeSinceStartup + 15f;
                while (StoryLevelEntryTransition.IsPlaying && Time.realtimeSinceStartup < transitionDeadline)
                {
                    m12StatusText = "entry transition";
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.5f);

                bool sawM12Battle = false;
                bool launchedM12 = true;
                float deadline = Time.realtimeSinceStartup + 600f;
                int guard = 0;
                while (Time.realtimeSinceStartup < deadline && guard++ < 3000)
                {
                    string scene = SceneManager.GetActiveScene().name;
                    if (scene == "BattleSimulation" && BattleLaunchContext.IsM12TrioMasteryBattle)
                        sawM12Battle = true;

                    if (launchedM12 && sawM12Battle &&
                        scene == StoryProgressSession.StoryProgressSceneName &&
                        !StoryLevelEntryTransition.IsPlaying)
                    {
                        m12StatusText = "M-1-2 played once";
                        yield break;
                    }

                    m12StatusText = TryAdvanceStep();
                    yield return new WaitForSecondsRealtime(0.4f);
                }

                m12StatusText = "timeout";
            }
            finally
            {
                isM12Running = false;
            }
        }

        public string StartM11HarborCombat()
        {
            if (isHarborRunning)
                return "M11 harbor routine already running; " + harborStatusText;

            StartCoroutine(CoM11HarborCombat());
            return "M11 harbor combat routine started";
        }

        private IEnumerator CoM11HarborCombat()
        {
            isHarborRunning = true;
            try
            {
                int slot = PlayerData.GetActivePlayerSlotOrDefault();
                if (!TutorialProgressState.IsAcademyIntroGraduated(slot))
                {
                    harborStatusText = "harbor locked — academy intro not graduated";
                    yield break;
                }

                if (SceneManager.GetActiveScene().name != StoryProgressSession.StoryProgressSceneName)
                {
                    harborStatusText = "loading Story progress";
                    SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
                    yield return new WaitForSecondsRealtime(1f);
                }

                harborStatusText = "launch harbor preview";
                LaunchM11HarborFromStoryProgress();

                float transitionDeadline = Time.realtimeSinceStartup + 12f;
                while (StoryLevelEntryTransition.IsPlaying && Time.realtimeSinceStartup < transitionDeadline)
                {
                    harborStatusText = "entry transition";
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.5f);

                bool sawHarborBattle = false;
                float deadline = Time.realtimeSinceStartup + 300f;
                int guard = 0;
                while (Time.realtimeSinceStartup < deadline && guard++ < 1500)
                {
                    string scene = SceneManager.GetActiveScene().name;
                    if (scene == "BattleSimulation")
                        sawHarborBattle = true;

                    if (sawHarborBattle &&
                        scene == StoryProgressSession.StoryProgressSceneName &&
                        !StoryLevelEntryTransition.IsPlaying)
                    {
                        harborStatusText = "M-1-1 harbor combat played once";
                        yield break;
                    }

                    harborStatusText = TryAdvanceStep();
                    yield return new WaitForSecondsRealtime(0.4f);
                }

                harborStatusText = "timeout";
            }
            finally
            {
                isHarborRunning = false;
            }
        }

        private IEnumerator CoM11Clear()
        {
            isRunning = true;
            try
            {
                int slot = PlayerData.GetActivePlayerSlotOrDefault();
                if (TutorialProgressState.IsAcademyIntroGraduated(slot))
                {
                    statusText = "already graduated";
                    yield break;
                }

                if (SceneManager.GetActiveScene().name != StoryProgressSession.StoryProgressSceneName)
                {
                    statusText = "loading Story progress";
                    SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
                    yield return new WaitForSecondsRealtime(1f);
                }

                statusText = "launch M-1-1";
                LaunchM11FromStoryProgress();

                float transitionDeadline = Time.realtimeSinceStartup + 12f;
                while (StoryLevelEntryTransition.IsPlaying && Time.realtimeSinceStartup < transitionDeadline)
                {
                    statusText = "entry transition";
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.5f);

                float deadline = Time.realtimeSinceStartup + 240f;
                int guard = 0;
                while (Time.realtimeSinceStartup < deadline && guard++ < 1200)
                {
                    if (TutorialProgressState.IsAcademyIntroGraduated(slot))
                    {
                        statusText = "graduated — M-1-1 cleared";
                        yield break;
                    }

                    statusText = TryAdvanceStep();
                    yield return new WaitForSecondsRealtime(0.4f);
                }

                statusText = "timeout";
            }
            finally
            {
                isRunning = false;
            }
        }
    }
#else
    public static string GetStatus() => "DevAutomation is editor-only.";

    public static string TryAdvanceStep() => GetStatus();
#endif
}

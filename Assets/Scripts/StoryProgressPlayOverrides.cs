using UnityEngine;

/// <summary>
/// Play Mode 進度覆寫：在 Inspector 勾選 1-2／1-3 等通關旗標，進入 Play 時寫入目前存檔槽。
/// 未勾選的里程碑視為尚未達成，可從該段重新體驗。
/// </summary>
[CreateAssetMenu(
    fileName = "StoryProgressPlayOverrides",
    menuName = "Card Game/Story Progress Play Overrides")]
public sealed class StoryProgressPlayOverrides : ScriptableObject
{
    [Header("套用")]
    [Tooltip("進入 Play Mode 時，將下方旗標寫入目前作用中存檔槽。")]
    public bool applyOnEnterPlayMode = true;

    [Tooltip("勾選時才會 Flush 到 playerdata.csv；測試預設關閉以免覆蓋真實存檔。")]
    public bool persistOverridesToSaveFile;

    [Tooltip("0 = 使用存檔目前作用中槽位；1–3 = 指定槽位。")]
    [Range(0, PlayerData.MaxPlayerSlots)]
    public int targetPlayerSlot;

    [Header("前置（M-1-2 節點解鎖）")]
    [Tooltip("港灣實戰首通；勾選後 Story progress 地圖才會開放 1-2。")]
    public bool harborCombatCleared = true;

    [Header("1-2 海牆巡邏")]
    public M12SeawallPatrolPlayOverrides seawallPatrol = new M12SeawallPatrolPlayOverrides();

    [Header("1-3 河岔分波")]
    [Tooltip("需勾選 1-2 全關通關，Story progress 才會解鎖 M-1-3 節點。")]
    public M13RiverForkPlayOverrides riverFork = new M13RiverForkPlayOverrides();

    [Header("A-1 潮間島")]
    [Tooltip("需 1-2 通關且散策拾取封印法術，Story progress 才會解鎖 S-A-1。")]
    public SideQuestA1PlayOverrides tideIsland = new SideQuestA1PlayOverrides();

    [System.Serializable]
    public sealed class SideQuestA1PlayOverrides
    {
        [Tooltip("A-1 節點已通關（三畦完成）")]
        public bool nodeCleared;

        [Tooltip("潮印已解封")]
        public bool tideMarkUnsealed;

        [Tooltip("休耕畦選擇留海蓬種")]
        public bool seaPurslaneSeedKept;
    }

    [System.Serializable]
    public sealed class M12SeawallPatrolPlayOverrides
    {
        [Tooltip("已看過 M-1-2 開場劇情")]
        public bool introSeen;

        [Tooltip("階段 A 段考通關")]
        public bool phaseAComplete;

        [Tooltip("段考 A 本局觸發過 民兵·列陣")]
        public bool phaseATrioMilitia;

        [Tooltip("段考 A 本局觸發過 王后·王室庇護")]
        public bool phaseATrioQueen;

        [Tooltip("段考 A 本局觸發過 國王·庭訓號令")]
        public bool phaseATrioKing;

        [Tooltip("中段海牆散策已完成")]
        public bool midPatrolComplete;

        [Tooltip("散策拾取封印法術")]
        public bool sealedSpellFound;

        [Tooltip("1-2 全關通關（A+B 達標）")]
        public bool nodeCleared;

        [Tooltip("教會三張首通獎勵已發放")]
        public bool religiousLineRewardGranted;
    }

    [System.Serializable]
    public sealed class M13RiverForkPlayOverrides
    {
        [Tooltip("已看過 M-1-3 開場邊燈夜話劇情")]
        public bool openingSeen;

        [Tooltip("分波鬥鳥已完成")]
        public bool birdDuelComplete;

        [Tooltip("鬥鳥略過（未實際遊玩）")]
        public bool birdDuelSkipped;

        [Tooltip("鬥鳥 S 評")]
        public bool birdDuelSRank;

        [Tooltip("岔路散策已完成")]
        public bool forkStrollComplete;

        [Tooltip("散策路線（僅在岔路散策已完成時有效）")]
        public M13ForkPathOverride forkPath = M13ForkPathOverride.Steady;

        [Tooltip("冷爐迎測 Phase A 通關")]
        public bool phaseAComplete;

        [Tooltip("已看過玫瑰試煉劇情")]
        public bool roseTrialSeen;

        [Tooltip("玫瑰試煉結局（僅在玫瑰試煉已看過時有效）")]
        public M13RoseTrialOutcomeOverride roseOutcome = M13RoseTrialOutcomeOverride.None;

        [Tooltip("S 評開局天氣已選（需鬥鳥 S 評）")]
        public bool openingWeatherPickConfigured;

        [Tooltip("S 評開局天氣選項")]
        public M13OpeningWeatherPick openingWeatherPick = M13OpeningWeatherPick.FireRain;

        [Tooltip("1-3 全關通關")]
        public bool nodeCleared;

        [Tooltip("潮印發光（通關後地圖節點特效）")]
        public bool tideMarkGlimmer;
    }

    public enum M13ForkPathOverride
    {
        Steady,
        Rapid,
    }

    public enum M13RoseTrialOutcomeOverride
    {
        None,
        Intact,
        Burned,
        DemandedMiracle,
    }

    public void ApplyToSlot(int slot)
    {
        slot = ResolveTargetSlot(slot);
        PlayerData.EnsureWritable().LoadPlayerData();

        HarborTrainingProgressState.SetHarborCombatCleared(slot, harborCombatCleared);
        HarborTrainingProgressState.EnsureSlotHarborProgressConsistent(slot);

        M12SeawallPatrolPlayOverrides m12 = seawallPatrol ?? new M12SeawallPatrolPlayOverrides();
        TutorialProgressState.SetM12IntroSeen(slot, m12.introSeen);
        TutorialProgressState.SetM12PhaseAComplete(slot, m12.phaseAComplete);
        TutorialProgressState.SetM12PhaseATrioMilitia(slot, m12.phaseATrioMilitia);
        TutorialProgressState.SetM12PhaseATrioQueen(slot, m12.phaseATrioQueen);
        TutorialProgressState.SetM12PhaseATrioKing(slot, m12.phaseATrioKing);
        TutorialProgressState.SetM12MidPatrolComplete(slot, m12.midPatrolComplete);
        TutorialProgressState.SetM12SealedSpellFound(slot, m12.sealedSpellFound);
        TutorialProgressState.SetM12TrioMasteryCleared(slot, m12.nodeCleared);
        TutorialProgressState.SetM12ReligiousLineRewardGranted(slot, m12.religiousLineRewardGranted);

        if (!m12.phaseAComplete)
            TutorialProgressState.SetM12PhaseADefeatCount(slot, 0);

        M13RiverForkPlayOverrides m13 = riverFork ?? new M13RiverForkPlayOverrides();
        ApplyM13RiverForkOverrides(slot, m13);

        SideQuestA1PlayOverrides a1 = tideIsland ?? new SideQuestA1PlayOverrides();
        TutorialProgressState.SetA1TideIslandCleared(slot, a1.nodeCleared);
        TutorialProgressState.SetA1TideMarkUnsealed(slot, a1.tideMarkUnsealed);
        TutorialProgressState.SetA1SeaPurslaneSeedKept(slot, a1.seaPurslaneSeedKept);

        if (m12.sealedSpellFound)
            ValuablesVaultCatalog.TrySyncSealedSpellRelicToVault(slot);

        if (persistOverridesToSaveFile)
            PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
        StoryProgressSceneController.RequestRefreshPresentation();
    }

    public void ApplyToActiveSlot() =>
        ApplyToSlot(targetPlayerSlot > 0 ? targetPlayerSlot : PlayerData.GetActivePlayerSlotOrDefault());

    private static void ApplyM13RiverForkOverrides(int slot, M13RiverForkPlayOverrides m13)
    {
        TutorialProgressState.SetM13OpeningSeen(slot, m13.openingSeen);
        TutorialProgressState.SetM13BirdDuelProgress(
            slot,
            m13.birdDuelComplete,
            m13.birdDuelComplete && m13.birdDuelSkipped,
            m13.birdDuelComplete && !m13.birdDuelSkipped && m13.birdDuelSRank);
        TutorialProgressState.SetM13ForkStrollProgress(
            slot,
            m13.forkStrollComplete,
            m13.forkStrollComplete && m13.forkPath == M13ForkPathOverride.Steady);
        TutorialProgressState.SetM13PhaseAComplete(slot, m13.phaseAComplete);
        TutorialProgressState.SetM13RoseTrialSeen(slot, m13.roseTrialSeen);
        ApplyM13RoseTrialOutcome(slot, m13);
        TutorialProgressState.SetM13RiverForkCleared(slot, m13.nodeCleared);
        TutorialProgressState.SetM13TideMarkGlimmer(slot, m13.tideMarkGlimmer);

        if (m13.openingWeatherPickConfigured &&
            m13.birdDuelComplete &&
            !m13.birdDuelSkipped &&
            m13.birdDuelSRank &&
            (int)m13.openingWeatherPick > 0)
        {
            TutorialProgressState.SetM13OpeningWeatherPick(slot, m13.openingWeatherPick);
        }
        else
        {
            TutorialProgressState.SetM13OpeningWeatherPick(slot, M13OpeningWeatherPick.DefaultFog);
        }
    }

    private static void ApplyM13RoseTrialOutcome(int slot, M13RiverForkPlayOverrides m13)
    {
        TutorialProgressState.SetM13RoseIntact(slot, false);
        TutorialProgressState.SetM13RoseBurned(slot, false);
        TutorialProgressState.SetM13PlayerDemandedMiracle(slot, false);

        if (!m13.roseTrialSeen)
            return;

        switch (m13.roseOutcome)
        {
            case M13RoseTrialOutcomeOverride.Intact:
                TutorialProgressState.SetM13RoseIntact(slot, true);
                break;
            case M13RoseTrialOutcomeOverride.Burned:
                TutorialProgressState.SetM13RoseBurned(slot, true);
                break;
            case M13RoseTrialOutcomeOverride.DemandedMiracle:
                TutorialProgressState.SetM13RoseBurned(slot, true);
                TutorialProgressState.SetM13PlayerDemandedMiracle(slot, true);
                break;
        }
    }

    private static int ResolveTargetSlot(int slot) =>
        Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
}

/// <summary>進入 Play Mode 時自動套用 <see cref="StoryProgressPlayOverrides"/>。</summary>
public static class StoryProgressPlayOverrideApplicator
{
    private static bool appliedThisPlaySession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => appliedThisPlaySession = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TryApplyAfterFirstSceneLoad() => TryApplyOnce();

    public static bool TryApplyOnce()
    {
        if (appliedThisPlaySession)
            return false;

        StoryProgressPlayOverrides asset = ResolveOverridesAsset();
        if (asset == null || !asset.applyOnEnterPlayMode)
            return false;

        appliedThisPlaySession = true;
        asset.ApplyToActiveSlot();
        Debug.Log(
            "StoryProgressPlayOverrides: applied inspector flags to slot " +
            (asset.targetPlayerSlot > 0 ? asset.targetPlayerSlot : PlayerData.GetActivePlayerSlotOrDefault()) +
            " (1-2 / 1-3 / A-1 story flags).");
        return true;
    }

    private static StoryProgressPlayOverrides ResolveOverridesAsset()
    {
        StoryProgressPlayOverrides fromResources =
            Resources.Load<StoryProgressPlayOverrides>("StoryProgressPlayOverrides");
        if (fromResources != null)
            return fromResources;

        return Object.FindFirstObjectByType<StoryProgressPlayOverridesHost>()?.Overrides;
    }
}

/// <summary>可掛在場景物件上，指向 Resources 或任意 Play Overrides 資產。</summary>
public sealed class StoryProgressPlayOverridesHost : MonoBehaviour
{
    [SerializeField] private StoryProgressPlayOverrides overridesAsset;

    public StoryProgressPlayOverrides Overrides => overridesAsset;
}

/// <summary>M-1-3 河岔分波進度（持久旗標）。</summary>

public static class M13RiverForkProgressState

{

    public static bool IsNodeAvailable(int slot) =>

        M12SeawallPatrolProgressState.IsNodeCleared(slot);



    public static bool IsOpeningSeen(int slot) =>

        TutorialProgressState.IsM13OpeningSeen(slot);



    public static bool IsBirdDuelComplete(int slot) =>

        TutorialProgressState.IsM13BirdDuelComplete(slot);



    public static bool IsBirdDuelSkipped(int slot) =>

        TutorialProgressState.IsM13BirdDuelSkipped(slot);



    public static bool HasBirdDuelSRank(int slot) =>

        TutorialProgressState.IsM13BirdDuelSRank(slot);



    public static bool IsForkStrollComplete(int slot) =>

        TutorialProgressState.IsM13ForkStrollComplete(slot);



    public static M13RiverForkPathChoice GetForkPath(int slot)

    {

        if (!IsForkStrollComplete(slot))

            return M13RiverForkPathChoice.None;

        return TutorialProgressState.IsM13ForkSteadyPath(slot)

            ? M13RiverForkPathChoice.Steady

            : M13RiverForkPathChoice.Rapid;

    }



    public static bool IsPhaseAComplete(int slot) =>

        TutorialProgressState.IsM13PhaseAComplete(slot);



    public static bool HasOpeningWeatherPick(int slot) =>

        TutorialProgressState.ReadM13OpeningWeatherPick(slot) > 0;



    public static M13OpeningWeatherPick GetOpeningWeatherPick(int slot)

    {

        int raw = TutorialProgressState.ReadM13OpeningWeatherPick(slot);

        if (raw <= 0)

            return M13OpeningWeatherPick.DefaultFog;

        return (M13OpeningWeatherPick)raw;

    }



    public static bool IsRoseTrialSeen(int slot) =>

        TutorialProgressState.IsM13RoseTrialSeen(slot);



    public static bool IsRoseIntact(int slot) =>

        TutorialProgressState.IsM13RoseIntact(slot);



    public static bool IsRoseBurned(int slot) =>

        TutorialProgressState.IsM13RoseBurned(slot);



    public static bool IsPlayerDemandedMiracle(int slot) =>

        TutorialProgressState.IsM13PlayerDemandedMiracle(slot);



    public static bool IsNodeCleared(int slot) =>

        TutorialProgressState.IsM13RiverForkCleared(slot);



    public static bool IsTideMarkGlimmer(int slot) =>

        TutorialProgressState.IsM13TideMarkGlimmer(slot);



    public static void MarkOpeningSeen(int slot) =>

        TutorialProgressState.SetM13OpeningSeen(slot, true);



    public static void MarkBirdDuelPlayed(int slot, bool sRank) =>

        TutorialProgressState.SetM13BirdDuelComplete(slot, skipped: false, sRank: sRank);



    public static void MarkBirdDuelSkipped(int slot) =>

        TutorialProgressState.SetM13BirdDuelComplete(slot, skipped: true, sRank: false);



    public static void MarkForkStrollComplete(int slot, M13RiverForkPathChoice path) =>

        TutorialProgressState.SetM13ForkStrollComplete(slot, path == M13RiverForkPathChoice.Steady);



    public static void SetOpeningWeatherPick(int slot, M13OpeningWeatherPick pick) =>

        TutorialProgressState.SetM13OpeningWeatherPick(slot, pick);



    public static void MarkPhaseAComplete(int slot) =>

        TutorialProgressState.SetM13PhaseAComplete(slot, true);



    public static void MarkRoseTrialSeen(int slot) =>

        TutorialProgressState.SetM13RoseTrialSeen(slot, true);



    public static void MarkNodeCleared(int slot)

    {

        TutorialProgressState.SetM13RiverForkCleared(slot, true);

        TutorialProgressState.SetM13TideMarkGlimmer(slot, true);

    }

    public static void ResetProgressFlagsForReplay(int slot) =>
        TutorialProgressState.ResetM13ReplayRunProgress(slot);

}


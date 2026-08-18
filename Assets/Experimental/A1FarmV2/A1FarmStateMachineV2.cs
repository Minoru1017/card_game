#if CARDGAME_A1_FARM_V2
using System;

namespace CardGame.Experimental.A1
{
    public enum A1FarmCrop
    {
        Rye,
        Fallow,
        Bean
    }

    public enum A1FarmStepKind
    {
        PlowDrag,
        SeedPlotClick,
        RhythmCompact,
        WaitGrowth,
        ScytheSwipe,
        ClickSoilBlocks,
        NetDrag,
        WaitFallow,
        ClickHarvestPlants,
        PurslaneChoice,
        SoakTap,
        ClickSeedCells,
        WaterChannelDrag,
        ClickWeeds,
        ClickPods
    }

    public enum A1FarmRunOutcome
    {
        InProgress,
        Completed,
        Skipped
    }

    public enum A1FarmFeedback
    {
        Rejected,
        Progress,
        StepAdvanced,
        CropAdvanced,
        RhythmMiss,
        ScytheIncomplete,
        SeedChoiceRecorded,
        RunCompleted,
        RunSkipped
    }

    public sealed class A1FarmConfig
    {
        public int PlotCellCount { get; }
        public int PlowCellsRequired { get; }
        public int NetCellsRequired { get; }
        public int WaterCellsRequired { get; }
        public int RhythmHitsRequired { get; }
        public float RhythmGoodWindowSeconds { get; }
        public float ScytheSwipeThreshold { get; }
        public float WaitDurationSeconds { get; }

        public static A1FarmConfig LegacyMvp =>
            new A1FarmConfig(
                plotCellCount: 20,
                plowCellsRequired: 16,
                netCellsRequired: 14,
                waterCellsRequired: 6,
                rhythmHitsRequired: 2,
                rhythmGoodWindowSeconds: 0.35f,
                scytheSwipeThreshold: 80f,
                waitDurationSeconds: 1.6f);

        public A1FarmConfig(
            int plotCellCount,
            int plowCellsRequired,
            int netCellsRequired,
            int waterCellsRequired,
            int rhythmHitsRequired,
            float rhythmGoodWindowSeconds,
            float scytheSwipeThreshold,
            float waitDurationSeconds)
        {
            if (plotCellCount <= 0 || plotCellCount > 64)
                throw new ArgumentOutOfRangeException(nameof(plotCellCount));
            if (plowCellsRequired <= 0 || plowCellsRequired > plotCellCount)
                throw new ArgumentOutOfRangeException(nameof(plowCellsRequired));
            if (netCellsRequired <= 0 || netCellsRequired > plotCellCount)
                throw new ArgumentOutOfRangeException(nameof(netCellsRequired));
            if (waterCellsRequired <= 0 || waterCellsRequired > plotCellCount)
                throw new ArgumentOutOfRangeException(nameof(waterCellsRequired));
            if (rhythmHitsRequired <= 0)
                throw new ArgumentOutOfRangeException(nameof(rhythmHitsRequired));
            if (rhythmGoodWindowSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(rhythmGoodWindowSeconds));
            if (scytheSwipeThreshold <= 0f)
                throw new ArgumentOutOfRangeException(nameof(scytheSwipeThreshold));
            if (waitDurationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(waitDurationSeconds));

            PlotCellCount = plotCellCount;
            PlowCellsRequired = plowCellsRequired;
            NetCellsRequired = netCellsRequired;
            WaterCellsRequired = waterCellsRequired;
            RhythmHitsRequired = rhythmHitsRequired;
            RhythmGoodWindowSeconds = rhythmGoodWindowSeconds;
            ScytheSwipeThreshold = scytheSwipeThreshold;
            WaitDurationSeconds = waitDurationSeconds;
        }
    }

    public readonly struct A1FarmSnapshot
    {
        public A1FarmCrop Crop { get; }
        public int StepIndex { get; }
        public A1FarmStepKind StepKind { get; }
        public A1FarmRunOutcome Outcome { get; }
        public bool KeptSeaPurslaneSeed { get; }
        public int ProgressCount { get; }
        public bool ScytheLeftComplete { get; }
        public bool ScytheRightComplete { get; }
        public float WaitElapsedSeconds { get; }

        public A1FarmSnapshot(
            A1FarmCrop crop,
            int stepIndex,
            A1FarmStepKind stepKind,
            A1FarmRunOutcome outcome,
            bool keptSeaPurslaneSeed,
            int progressCount,
            bool scytheLeftComplete,
            bool scytheRightComplete,
            float waitElapsedSeconds)
        {
            Crop = crop;
            StepIndex = stepIndex;
            StepKind = stepKind;
            Outcome = outcome;
            KeptSeaPurslaneSeed = keptSeaPurslaneSeed;
            ProgressCount = progressCount;
            ScytheLeftComplete = scytheLeftComplete;
            ScytheRightComplete = scytheRightComplete;
            WaitElapsedSeconds = waitElapsedSeconds;
        }
    }

    public readonly struct A1FarmTransition
    {
        public bool Accepted { get; }
        public A1FarmFeedback Feedback { get; }
        public A1FarmSnapshot Snapshot { get; }

        public A1FarmTransition(
            bool accepted,
            A1FarmFeedback feedback,
            A1FarmSnapshot snapshot)
        {
            Accepted = accepted;
            Feedback = feedback;
            Snapshot = snapshot;
        }
    }

    /// <summary>
    /// Experimental pure-C# state machine for the A-1 three-plot farm.
    /// It intentionally has no Unity, UI, reward, save, or scene dependencies.
    /// The legacy overlay remains the only runtime implementation.
    /// </summary>
    public sealed class A1FarmStateMachineV2
    {
        private static readonly A1FarmStepKind[] RyeSteps =
        {
            A1FarmStepKind.PlowDrag,
            A1FarmStepKind.SeedPlotClick,
            A1FarmStepKind.RhythmCompact,
            A1FarmStepKind.WaitGrowth,
            A1FarmStepKind.ScytheSwipe
        };

        private static readonly A1FarmStepKind[] FallowSteps =
        {
            A1FarmStepKind.ClickSoilBlocks,
            A1FarmStepKind.NetDrag,
            A1FarmStepKind.WaitFallow,
            A1FarmStepKind.ClickHarvestPlants,
            A1FarmStepKind.PurslaneChoice
        };

        private static readonly A1FarmStepKind[] BeanSteps =
        {
            A1FarmStepKind.SoakTap,
            A1FarmStepKind.ClickSeedCells,
            A1FarmStepKind.WaterChannelDrag,
            A1FarmStepKind.ClickWeeds,
            A1FarmStepKind.ClickPods
        };

        private readonly A1FarmConfig config;
        private A1FarmCrop crop;
        private int stepIndex;
        private A1FarmRunOutcome outcome;
        private bool keptSeaPurslaneSeed;
        private ulong progressMask;
        private int progressCounter;
        private bool scytheLeftComplete;
        private bool scytheRightComplete;
        private float waitElapsedSeconds;

        public A1FarmStateMachineV2(A1FarmConfig config = null)
        {
            this.config = config ?? A1FarmConfig.LegacyMvp;
            crop = A1FarmCrop.Rye;
            stepIndex = 0;
            outcome = A1FarmRunOutcome.InProgress;
        }

        public A1FarmSnapshot Snapshot => CreateSnapshot();
        public bool IsTerminal => outcome != A1FarmRunOutcome.InProgress;

        public A1FarmTransition Skip()
        {
            if (IsTerminal)
                return Reject();

            outcome = A1FarmRunOutcome.Skipped;
            return Result(true, A1FarmFeedback.RunSkipped);
        }

        public A1FarmTransition ApplyPlotCell(int cellIndex)
        {
            if (IsTerminal || cellIndex < 0 || cellIndex >= config.PlotCellCount)
                return Reject();

            int required;
            switch (CurrentStep)
            {
                case A1FarmStepKind.PlowDrag:
                    required = config.PlowCellsRequired;
                    break;
                case A1FarmStepKind.NetDrag:
                    required = config.NetCellsRequired;
                    break;
                case A1FarmStepKind.WaterChannelDrag:
                    required = config.WaterCellsRequired;
                    break;
                default:
                    return Reject();
            }

            progressMask |= 1UL << cellIndex;
            if (CountBits(progressMask) >= required)
                return Advance();

            return Result(true, A1FarmFeedback.Progress);
        }

        public A1FarmTransition ApplyTargetClick(int targetId)
        {
            if (IsTerminal || targetId < 0 || targetId >= 64)
                return Reject();

            int required;
            bool countRepeatedClicks = false;
            switch (CurrentStep)
            {
                case A1FarmStepKind.SeedPlotClick:
                    required = 1;
                    break;
                case A1FarmStepKind.ClickSoilBlocks:
                    required = 3;
                    break;
                case A1FarmStepKind.ClickHarvestPlants:
                    required = 5;
                    break;
                case A1FarmStepKind.SoakTap:
                    required = 3;
                    countRepeatedClicks = true;
                    break;
                case A1FarmStepKind.ClickSeedCells:
                    required = 6;
                    break;
                case A1FarmStepKind.ClickWeeds:
                    required = 2;
                    break;
                case A1FarmStepKind.ClickPods:
                    required = 3;
                    break;
                default:
                    return Reject();
            }

            if (countRepeatedClicks)
            {
                progressCounter++;
            }
            else
            {
                ulong targetBit = 1UL << targetId;
                if ((progressMask & targetBit) != 0UL)
                    return Result(true, A1FarmFeedback.Progress);

                progressMask |= targetBit;
                progressCounter = CountBits(progressMask);
            }

            if (progressCounter >= required)
                return Advance();

            return Result(true, A1FarmFeedback.Progress);
        }

        public A1FarmTransition ApplyRhythmTap(float absoluteBeatOffsetSeconds)
        {
            if (IsTerminal || CurrentStep != A1FarmStepKind.RhythmCompact)
                return Reject();

            if (Math.Abs(absoluteBeatOffsetSeconds) > config.RhythmGoodWindowSeconds)
                return Result(true, A1FarmFeedback.RhythmMiss);

            progressCounter++;
            if (progressCounter >= config.RhythmHitsRequired)
                return Advance();

            return Result(true, A1FarmFeedback.Progress);
        }

        public A1FarmTransition ApplyWait(float elapsedSeconds)
        {
            if (IsTerminal ||
                (CurrentStep != A1FarmStepKind.WaitGrowth &&
                 CurrentStep != A1FarmStepKind.WaitFallow) ||
                elapsedSeconds < 0f)
            {
                return Reject();
            }

            waitElapsedSeconds += elapsedSeconds;
            if (waitElapsedSeconds >= config.WaitDurationSeconds)
                return Advance();

            return Result(true, A1FarmFeedback.Progress);
        }

        public A1FarmTransition ApplyScytheDrag(float horizontalDelta, bool endDrag)
        {
            if (IsTerminal || CurrentStep != A1FarmStepKind.ScytheSwipe)
                return Reject();

            if (horizontalDelta <= -config.ScytheSwipeThreshold)
                scytheLeftComplete = true;
            if (horizontalDelta >= config.ScytheSwipeThreshold)
                scytheRightComplete = true;

            if (!endDrag)
                return Result(true, A1FarmFeedback.Progress);
            if (!scytheLeftComplete || !scytheRightComplete)
                return Result(true, A1FarmFeedback.ScytheIncomplete);

            return Advance();
        }

        public A1FarmTransition ChoosePurslaneSeed(bool keepSeed)
        {
            if (IsTerminal || CurrentStep != A1FarmStepKind.PurslaneChoice)
                return Reject();

            keptSeaPurslaneSeed = keepSeed;
            A1FarmTransition transition = Advance();
            return transition.Feedback == A1FarmFeedback.CropAdvanced
                ? new A1FarmTransition(
                    true,
                    A1FarmFeedback.SeedChoiceRecorded,
                    transition.Snapshot)
                : transition;
        }

        private A1FarmStepKind CurrentStep => GetSteps(crop)[stepIndex];

        private A1FarmTransition Advance()
        {
            stepIndex++;
            A1FarmStepKind[] steps = GetSteps(crop);
            if (stepIndex < steps.Length)
            {
                ResetStepProgress();
                return Result(true, A1FarmFeedback.StepAdvanced);
            }

            if (crop == A1FarmCrop.Rye)
            {
                crop = A1FarmCrop.Fallow;
                stepIndex = 0;
                ResetStepProgress();
                return Result(true, A1FarmFeedback.CropAdvanced);
            }

            if (crop == A1FarmCrop.Fallow)
            {
                crop = A1FarmCrop.Bean;
                stepIndex = 0;
                ResetStepProgress();
                return Result(true, A1FarmFeedback.CropAdvanced);
            }

            stepIndex = steps.Length - 1;
            outcome = A1FarmRunOutcome.Completed;
            ResetStepProgress();
            return Result(true, A1FarmFeedback.RunCompleted);
        }

        private void ResetStepProgress()
        {
            progressMask = 0UL;
            progressCounter = 0;
            scytheLeftComplete = false;
            scytheRightComplete = false;
            waitElapsedSeconds = 0f;
        }

        private A1FarmSnapshot CreateSnapshot()
        {
            return new A1FarmSnapshot(
                crop,
                stepIndex,
                CurrentStep,
                outcome,
                keptSeaPurslaneSeed,
                progressCounter > 0 ? progressCounter : CountBits(progressMask),
                scytheLeftComplete,
                scytheRightComplete,
                waitElapsedSeconds);
        }

        private A1FarmTransition Reject() =>
            Result(false, A1FarmFeedback.Rejected);

        private A1FarmTransition Result(bool accepted, A1FarmFeedback feedback) =>
            new A1FarmTransition(accepted, feedback, CreateSnapshot());

        private static A1FarmStepKind[] GetSteps(A1FarmCrop currentCrop)
        {
            switch (currentCrop)
            {
                case A1FarmCrop.Rye:
                    return RyeSteps;
                case A1FarmCrop.Fallow:
                    return FallowSteps;
                default:
                    return BeanSteps;
            }
        }

        private static int CountBits(ulong value)
        {
            int count = 0;
            while (value != 0UL)
            {
                value &= value - 1UL;
                count++;
            }

            return count;
        }
    }
}
#endif

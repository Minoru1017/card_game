#if CARDGAME_A1_FARM_V2
using CardGame.Experimental.A1;
using NUnit.Framework;

namespace CardGame.Experimental.Tests.A1
{
    public sealed class A1FarmStateMachineV2Tests
    {
        [Test]
        public void LegacyHappyPath_CompletesAllThreeCrops()
        {
            var machine = new A1FarmStateMachineV2();

            CompleteRye(machine);
            Assert.That(machine.Snapshot.Crop, Is.EqualTo(A1FarmCrop.Fallow));

            CompleteFallow(machine, keepSeed: true);
            Assert.That(machine.Snapshot.Crop, Is.EqualTo(A1FarmCrop.Bean));
            Assert.That(machine.Snapshot.KeptSeaPurslaneSeed, Is.True);

            CompleteBean(machine);
            Assert.That(machine.Snapshot.Outcome, Is.EqualTo(A1FarmRunOutcome.Completed));
            Assert.That(machine.Snapshot.StepKind, Is.EqualTo(A1FarmStepKind.Terminal));
            Assert.That(machine.Snapshot.StepIndex, Is.EqualTo(5));
            Assert.That(machine.IsTerminal, Is.True);
        }

        [Test]
        public void PlowCoverage_RequiresSixteenUniqueCells()
        {
            var machine = new A1FarmStateMachineV2();

            for (int i = 0; i < 15; i++)
                machine.ApplyPlotCell(i);

            Assert.That(machine.Snapshot.StepKind, Is.EqualTo(A1FarmStepKind.PlowDrag));
            Assert.That(machine.Snapshot.ProgressCount, Is.EqualTo(15));

            machine.ApplyPlotCell(0);
            Assert.That(machine.Snapshot.ProgressCount, Is.EqualTo(15));

            A1FarmTransition transition = machine.ApplyPlotCell(15);
            Assert.That(transition.Feedback, Is.EqualTo(A1FarmFeedback.StepAdvanced));
            Assert.That(machine.Snapshot.StepKind, Is.EqualTo(A1FarmStepKind.SeedPlotClick));
        }

        [Test]
        public void CorrectedRhythmWindow_RejectsOffsetOutsideWindow()
        {
            var machine = new A1FarmStateMachineV2();
            AdvanceToRhythm(machine);

            A1FarmTransition miss = machine.ApplyRhythmTap(0.36f);

            Assert.That(miss.Accepted, Is.True);
            Assert.That(miss.Feedback, Is.EqualTo(A1FarmFeedback.RhythmMiss));
            Assert.That(machine.Snapshot.ProgressCount, Is.Zero);
            Assert.That(machine.Snapshot.StepKind, Is.EqualTo(A1FarmStepKind.RhythmCompact));
        }

        [Test]
        public void Scythe_RequiresBothDirectionsAcrossMultipleInputs()
        {
            var machine = new A1FarmStateMachineV2();
            AdvanceToScythe(machine);

            A1FarmTransition exactBoundary = machine.ApplyScytheDrag(-80f, endDrag: true);
            Assert.That(exactBoundary.Feedback, Is.EqualTo(A1FarmFeedback.ScytheIncomplete));
            Assert.That(machine.Snapshot.ScytheLeftComplete, Is.False);

            machine.ApplyScytheDrag(-80.1f, endDrag: false);
            A1FarmTransition incomplete = machine.ApplyScytheDrag(80f, endDrag: true);
            Assert.That(incomplete.Feedback, Is.EqualTo(A1FarmFeedback.ScytheIncomplete));
            Assert.That(machine.Snapshot.ScytheLeftComplete, Is.True);
            Assert.That(machine.Snapshot.ScytheRightComplete, Is.False);

            A1FarmTransition complete = machine.ApplyScytheDrag(80.1f, endDrag: true);
            Assert.That(complete.Feedback, Is.EqualTo(A1FarmFeedback.CropAdvanced));
            Assert.That(machine.Snapshot.Crop, Is.EqualTo(A1FarmCrop.Fallow));
        }

        [Test]
        public void Skip_IsTerminalAndRejectsFurtherInput()
        {
            var machine = new A1FarmStateMachineV2();

            A1FarmTransition skipped = machine.Skip();
            A1FarmTransition rejected = machine.ApplyPlotCell(0);

            Assert.That(skipped.Feedback, Is.EqualTo(A1FarmFeedback.RunSkipped));
            Assert.That(machine.Snapshot.Outcome, Is.EqualTo(A1FarmRunOutcome.Skipped));
            Assert.That(rejected.Accepted, Is.False);
        }

        [Test]
        public void WaterChannel_RequiresSixUniqueCells()
        {
            var machine = new A1FarmStateMachineV2();
            CompleteRye(machine);
            CompleteFallow(machine, keepSeed: false);

            for (int i = 0; i < 3; i++)
                machine.ApplyTargetClick(0);
            for (int i = 0; i < 6; i++)
                machine.ApplyTargetClick(i);

            for (int i = 0; i < 5; i++)
                machine.ApplyPlotCell(i);

            Assert.That(machine.Snapshot.StepKind, Is.EqualTo(A1FarmStepKind.WaterChannelDrag));
            Assert.That(machine.Snapshot.ProgressCount, Is.EqualTo(5));

            machine.ApplyPlotCell(5);
            Assert.That(machine.Snapshot.StepKind, Is.EqualTo(A1FarmStepKind.ClickWeeds));
        }

        private static void AdvanceToRhythm(A1FarmStateMachineV2 machine)
        {
            for (int i = 0; i < 16; i++)
                machine.ApplyPlotCell(i);
            machine.ApplyTargetClick(0);
        }

        private static void AdvanceToScythe(A1FarmStateMachineV2 machine)
        {
            AdvanceToRhythm(machine);
            machine.ApplyRhythmTap(0f);
            machine.ApplyRhythmTap(0f);
            machine.ApplyWait(1.6f);
        }

        private static void CompleteRye(A1FarmStateMachineV2 machine)
        {
            AdvanceToScythe(machine);
            machine.ApplyScytheDrag(-80.1f, endDrag: false);
            machine.ApplyScytheDrag(80.1f, endDrag: true);
        }

        private static void CompleteFallow(A1FarmStateMachineV2 machine, bool keepSeed)
        {
            for (int i = 0; i < 3; i++)
                machine.ApplyTargetClick(i);
            for (int i = 0; i < 14; i++)
                machine.ApplyPlotCell(i);
            machine.ApplyWait(1.6f);
            for (int i = 0; i < 5; i++)
                machine.ApplyTargetClick(i);
            machine.ChoosePurslaneSeed(keepSeed);
        }

        private static void CompleteBean(A1FarmStateMachineV2 machine)
        {
            for (int i = 0; i < 3; i++)
                machine.ApplyTargetClick(0);
            for (int i = 0; i < 6; i++)
                machine.ApplyTargetClick(i);
            for (int i = 0; i < 6; i++)
                machine.ApplyPlotCell(i);
            for (int i = 0; i < 2; i++)
                machine.ApplyTargetClick(i);
            for (int i = 0; i < 3; i++)
                machine.ApplyTargetClick(i);
        }
    }
}
#endif

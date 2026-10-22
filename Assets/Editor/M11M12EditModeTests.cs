using NUnit.Framework;

/// <summary>港灣 M-1-1 與海牆巡邏 M-1-2 的無存檔純邏輯回歸測試。</summary>
public class M11M12EditModeTests
{
    [SetUp]
    public void SetUp()
    {
        BattleLaunchContext.ClearActiveBattle();
        M12TrioMasteryBattleTracker.ResetForNewBattle();
    }

    [TearDown]
    public void TearDown()
    {
        M12TrioMasteryBattleTracker.ResetForNewBattle();
        BattleLaunchContext.ClearActiveBattle();
    }

    [TestCase(BattleDifficultyTier.Easy, 40)]
    [TestCase(BattleDifficultyTier.Normal, 60)]
    [TestCase(BattleDifficultyTier.Hard, 80)]
    public void HarborFirstClearGemAmount_MatchesDifficulty(
        BattleDifficultyTier tier,
        int expected)
    {
        Assert.AreEqual(expected, HarborTrainingProgressState.GetHarborFirstClearGemAmount(tier));
    }

    [Test]
    public void M12PhaseA_DisablesCombatRoleMechanicsAndScaling()
    {
        BattleLaunchContext.BeginM12TrioTutorialBattleLaunch();

        Assert.IsFalse(CombatRoleBattleRules.IsMechanicsEnabled());
        Assert.AreEqual(
            10,
            CombatRoleBattleRules.ScaleMonsterVsMonster(
                10,
                CombatRole.Strike,
                CombatRole.Support,
                10,
                10));
    }

    [Test]
    public void M12PhaseB_EnablesCombatRoleMechanicsAndScaling()
    {
        BattleLaunchContext.BeginM12CoachPracticeBattleLaunch();

        Assert.IsTrue(CombatRoleBattleRules.IsMechanicsEnabled());
        Assert.Greater(
            CombatRoleBattleRules.ScaleMonsterVsMonster(
                10,
                CombatRole.Strike,
                CombatRole.Support,
                10,
                10),
            10);
    }

    [Test]
    public void CombatRoleTriangle_ResolvesThreeAdvantagesAndFinisherAsNeutral()
    {
        Assert.AreEqual(
            CombatRoleMatchup.Advantage,
            CombatRoleBattleRules.GetTriangleMatchup(CombatRole.Strike, CombatRole.Support));
        Assert.AreEqual(
            CombatRoleMatchup.Advantage,
            CombatRoleBattleRules.GetTriangleMatchup(CombatRole.Support, CombatRole.Tank));
        Assert.AreEqual(
            CombatRoleMatchup.Advantage,
            CombatRoleBattleRules.GetTriangleMatchup(CombatRole.Tank, CombatRole.Strike));
        Assert.AreEqual(
            CombatRoleMatchup.Neutral,
            CombatRoleBattleRules.GetTriangleMatchup(CombatRole.Finisher, CombatRole.Strike));
    }

    [Test]
    public void M12PhaseA_CountsOnlyPlayerSideTrioTriggers()
    {
        BattleLaunchContext.BeginM12TrioTutorialBattleLaunch();

        M12TrioMasteryBattleTracker.NotifyMilitiaFormationTriggered(false);
        M12TrioMasteryBattleTracker.NotifyQueenShelterTriggered(false);
        M12TrioMasteryBattleTracker.NotifyKingDecreeTriggered(false);
        Assert.AreEqual(0, M12TrioMasteryBattleTracker.QueryTriggeredSkillCount());

        M12TrioMasteryBattleTracker.NotifyMilitiaFormationTriggered(true);
        M12TrioMasteryBattleTracker.NotifyQueenShelterTriggered(true);
        M12TrioMasteryBattleTracker.NotifyKingDecreeTriggered(true);
        Assert.IsTrue(M12TrioMasteryBattleTracker.QueryAllTrioSkillsTriggered());
    }

    [Test]
    public void M12PhaseB_CountsTrioTriggersFromEitherSide()
    {
        BattleLaunchContext.BeginM12CoachPracticeBattleLaunch();

        M12TrioMasteryBattleTracker.NotifyMilitiaFormationTriggered(false);
        M12TrioMasteryBattleTracker.NotifyQueenShelterTriggered(true);
        M12TrioMasteryBattleTracker.NotifyKingDecreeTriggered(false);

        Assert.IsTrue(M12TrioMasteryBattleTracker.QueryAllTrioSkillsTriggered());
    }

    [Test]
    public void PhaseANearMissMessage_NamesMissingKingSkill()
    {
        BattleLaunchContext.BeginM12TrioTutorialBattleLaunch();
        M12TrioMasteryBattleTracker.NotifyMilitiaFormationTriggered(true);
        M12TrioMasteryBattleTracker.NotifyQueenShelterTriggered(true);

        StringAssert.Contains(
            "國王·庭訓號令",
            M12TrioMasteryBattleTracker.BuildPhaseANearMissMessage(true));
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>P2：核心規則 EditMode 煙霧測試（Test Runner）。</summary>
public class CardGameEditModeTests
{
    [Test]
    public void ComputeBarFill01FromWins_ZeroProgress_ReturnsZero()
    {
        const int monsterId = 5;
        var wins = default(CardProficiencyWins);
        float fill = CardSkillProficiencyService.ComputeBarFill01FromWins(monsterId, wins);
        Assert.AreEqual(0f, fill, 0.001f);
    }

    [Test]
    public void RecordBattleOutcome_Win_AddsSettlementEntry()
    {
        var host = new GameObject("TestPlayerData");
        try
        {
            var pd = host.AddComponent<PlayerData>();
            var deck = new Dictionary<int, int> { { 5, 1 } };
            CardSkillProficiencyService.RecordBattleOutcome(pd, deck, 1, "普通");
            Assert.IsNotNull(CardSkillProficiencyService.LastSettlementEntries);
            Assert.Greater(CardSkillProficiencyService.LastSettlementEntries.Count, 0);
            Assert.AreEqual(5, CardSkillProficiencyService.LastSettlementEntries[0].monsterId);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void DeckSlotDisplayName_RoundTrip_FiveSlots()
    {
        var host = new GameObject("TestPlayerData");
        try
        {
            var pd = host.AddComponent<PlayerData>();
            string[] expected = { "教會隊", "試玩 A", "第三組", "Deck Four", "最後槽" };
            for (int i = 0; i < expected.Length; i++)
                pd.SetDeckSlotDisplayName(i, expected[i]);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], pd.GetDeckSlotDisplayName(i));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void CountsTowardStageC_NormalAndAbove_CountsIntroDoesNot()
    {
        Assert.IsTrue(CardSkillProficiencyService.CountsTowardStageCWin("普通"));
        Assert.IsTrue(CardSkillProficiencyService.CountsTowardStageCWin("困難"));
        Assert.IsFalse(CardSkillProficiencyService.CountsTowardStageCWin("入門"));
        Assert.IsFalse(CardSkillProficiencyService.CountsTowardStageCWin("簡單"));
    }
}

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
                PlayerDeckSlotNameStorage.SetCustomName(pd, i, expected[i]);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], PlayerDeckSlotNameStorage.GetDisplayName(pd, i));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void DeckSlotDisplayName_RawStorage_DoesNotPersistUiFallback()
    {
        var host = new GameObject("TestPlayerData");
        try
        {
            var pd = host.AddComponent<PlayerData>();
            pd.EnsureMinimumDeckSlotCount();
            Assert.AreEqual("牌組1", PlayerDeckSlotNameStorage.GetDisplayName(pd, 0));
            Assert.AreEqual(string.Empty, PlayerDeckSlotNameStorage.GetRawName(pd, 0));
            PlayerDeckSlotNameStorage.SetCustomName(pd, 0, "自訂名稱");
            Assert.AreEqual("自訂名稱", PlayerDeckSlotNameStorage.GetRawName(pd, 0));
            Assert.AreEqual("自訂名稱", PlayerDeckSlotNameStorage.GetDisplayName(pd, 0));
            PlayerDeckSlotNameStorage.SetCustomName(pd, 1, string.Empty);
            Assert.AreEqual(string.Empty, PlayerDeckSlotNameStorage.GetRawName(pd, 1));
            Assert.AreEqual("牌組2", PlayerDeckSlotNameStorage.GetDisplayName(pd, 1));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void DeckSlotDisplayName_DefaultNames_ArePerDeckIndex()
    {
        Assert.AreEqual("牌組1", PlayerDeckSlotNameStorage.FormatDefaultDisplayName(0));
        Assert.AreEqual("牌組5", PlayerDeckSlotNameStorage.FormatDefaultDisplayName(4));
    }

    [Test]
    public void DeckSlotDisplayName_LegacyPersistedDefaultName_NormalizesToEmpty()
    {
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsLegacyPersistedDefaultName(0, "牌組1"));
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsLegacyPersistedDefaultName(4, "牌組5"));
        Assert.IsFalse(PlayerDeckSlotNameStorage.IsLegacyPersistedDefaultName(0, "自訂名稱"));

        var host = new GameObject("TestPlayerData");
        try
        {
            var pd = host.AddComponent<PlayerData>();
            pd.EnsureMinimumDeckSlotCount();
            PlayerDeckSlotNameStorage.ApplyLoadRow(pd, new[] { "deck_slot_name", "0", "牌組1" });
            Assert.AreEqual(string.Empty, PlayerDeckSlotNameStorage.GetRawName(pd, 0));
            Assert.AreEqual("牌組1", PlayerDeckSlotNameStorage.GetDisplayName(pd, 0));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void DeckSlotDisplayName_ResetToDefault_ClearsRawName()
    {
        var host = new GameObject("TestPlayerData");
        try
        {
            var pd = host.AddComponent<PlayerData>();
            pd.EnsureMinimumDeckSlotCount();
            pd.selectedDeckSlot = 2;
            PlayerDeckSlotNameStorage.SetCustomName(pd, 2, "解散前名稱");
            PlayerDeckSlotNameStorage.ResetSelectedDeckSlotNameToDefault(pd);
            Assert.AreEqual(string.Empty, PlayerDeckSlotNameStorage.GetRawName(pd, 2));
            Assert.AreEqual("牌組3", PlayerDeckSlotNameStorage.GetDisplayName(pd, 2));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void DeckSlotDisplayName_PollutionPatterns_AreDetectedAndNormalized()
    {
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsPollutedDeckSlotNameRaw(0, "-"));
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsPollutedDeckSlotNameRaw(4, "?5"));
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsPollutedDeckSlotNameRaw(4, "?\uf5fc?5"));
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsPollutedDeckSlotNameRaw(0, "撠曈?slot 3 deck_slot_na"));
        Assert.IsFalse(PlayerDeckSlotNameStorage.IsPollutedDeckSlotNameRaw(0, "測試A"));
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsPollutedProfileDecksSummary(
            "撠曈?slot 3 deck_slot_na:30張 | 牌組2:30張"));
        Assert.IsTrue(PlayerDeckSlotNameStorage.IsPollutedProfileDecksSummary(
            "DSDDDAD3333232:30張 | KFLDLDLDLJKJJFKJ:30張 | 牌組3:11張 | 牌組4:0張 | ?\uf5fc?5:1張"));

        var host = new GameObject("TestPlayerData");
        try
        {
            var pd = host.AddComponent<PlayerData>();
            pd.EnsureMinimumDeckSlotCount();
            PlayerDeckSlotNameStorage.ApplyLoadRow(pd, new[] { "deck_slot_name", "4", "?\uf5fc?5" });
            Assert.AreEqual(string.Empty, PlayerDeckSlotNameStorage.GetRawName(pd, 4));
            Assert.AreEqual("牌組5", PlayerDeckSlotNameStorage.GetDisplayName(pd, 4));

            string pollutedRow = "slot,3,deck_slot_name,4,?\uf5fc?5";
            Assert.IsTrue(PlayerDeckSlotNameStorage.TrySanitizeDeckSlotNameCsvRow(
                pollutedRow, out string sanitizedRow));
            Assert.AreEqual("slot,3,deck_slot_name,4,", sanitizedRow);
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

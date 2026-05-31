using System.Collections.Generic;
using UnityEngine;

/// <summary>背包詳情：熟練度狀態條顯示資料。</summary>
public readonly struct ProficiencyBarViewModel
{
    public readonly bool show;
    public readonly string label;
    public readonly float fill01;
    public readonly string statusText;

    public ProficiencyBarViewModel(bool show, string label, float fill01, string statusText)
    {
        this.show = show;
        this.label = label;
        this.fill01 = fill01;
        this.statusText = statusText;
    }
}

/// <summary>單張怪物牌熟練度進度（帳號／存檔）。progressAny 滿 2 → B；winsNormal 滿 10 → C。</summary>
public struct CardProficiencyWins
{
    public float progressAny;
    public int winsNormalDifficulty;
}

/// <summary>
/// 卡牌熟練度與戰技階段。結算增量：勝 +1/2、平 +2/3×1/2、敗 +1/3×1/2（御三家不累加；方案 B：約 10 場勝到 B）。
/// </summary>
public static class CardSkillProficiencyService
{
    public const int WinsRequiredForStageB = 2;
    public const int WinsRequiredForStageC = 10;
    public const string NormalDifficultyLabelZh = "普通";
    /// <summary>B→C 進度條與說明用：普通、困難、魔王難度勝利均計入。</summary>
    public const string StageCDifficultyRequirementLabelZh = "普通以上";

    public static IReadOnlyList<BattleProficiencySettlementEntry> LastSettlementEntries { get; private set; }

    public static bool ShouldShowProficiencyBar(int monsterId) => IsMonsterCardId(monsterId);

    public static float GetOutcomeProgressDelta(int battleResult)
    {
        float step = 1f / WinsRequiredForStageB;
        switch (battleResult)
        {
            case 1: return step;
            case 2: return step * (2f / 3f);
            case -1: return step * (1f / 3f);
            default: return 0f;
        }
    }

    public static CardSkillRevealStage GetUnlockedStage(int monsterId)
    {
        if (!IsMonsterCardId(monsterId))
            return CardSkillRevealStage.LockedA;
        return ResolveStageFromWins(monsterId, GetWins(monsterId));
    }

    public static CardSkillRevealStage ResolveStageFromWins(int monsterId, CardProficiencyWins wins)
    {
        if (!IsMonsterCardId(monsterId))
            return CardSkillRevealStage.LockedA;

        if (IsStarterTrio(monsterId))
            return CardSkillRevealStage.FullC;

        if (wins.winsNormalDifficulty >= WinsRequiredForStageC)
            return CardSkillRevealStage.FullC;
        if (wins.progressAny >= WinsRequiredForStageB - 0.001f)
            return CardSkillRevealStage.BasicB;
        return CardSkillRevealStage.LockedA;
    }

    public static bool IsStageUnlocked(int monsterId, CardSkillRevealStage stage) =>
        (int)GetUnlockedStage(monsterId) >= (int)stage;

    public static bool IsStarterTrio(int monsterId) =>
        monsterId == MonsterSkillIds.King ||
        monsterId == MonsterSkillIds.Queen ||
        monsterId == MonsterSkillIds.Militia;

    /// <summary>「普通」難度及以上（普通、困難、魔王）之勝利計入 toward-C；入門、簡單不算。</summary>
    public static bool CountsTowardStageCWin(string difficultyLabelZh)
    {
        int idx = PlayerProfileCsvService.GetStandardDifficultyIndex(difficultyLabelZh);
        int normalIdx = System.Array.IndexOf(PlayerProfileCsvService.StandardDifficultyLabelsZh, NormalDifficultyLabelZh);
        return normalIdx >= 0 && idx >= normalIdx;
    }

    public static bool IsNormalDifficultyLabel(string difficultyLabelZh) => CountsTowardStageCWin(difficultyLabelZh);

    /// <summary>對戰結束：依勝／平／敗累加進度，並產生結算播報快照。</summary>
    public static void RecordBattleOutcome(
        PlayerData playerData,
        IReadOnlyDictionary<int, int> deckMap,
        int battleResult,
        string difficultyLabelZh)
    {
        var entries = new List<BattleProficiencySettlementEntry>();
        LastSettlementEntries = entries;

        if (playerData == null || deckMap == null) return;

        float delta = GetOutcomeProgressDelta(battleResult);
        bool addNormalWin = battleResult == 1 && CountsTowardStageCWin(difficultyLabelZh);
        var credited = new HashSet<int>();

        foreach (var kv in deckMap)
        {
            if (kv.Value <= 0) continue;
            int id = kv.Key;
            if (DeckCardId.IsSpellKey(id)) continue;
            if (!IsMonsterCardId(id)) continue;
            if (!credited.Add(id)) continue;

            CardProficiencyWins before = playerData.GetCardProficiencyWins(id);
            float fillBefore = ComputeBarFill01FromWins(id, before);
            CardSkillRevealStage stageBefore = ResolveStageFromWins(id, before);
            float progressDelta = 0f;

            if (!IsStarterTrio(id) && delta > 0f)
            {
                playerData.AddCardProficiencyProgress(id, delta, addNormalWin);
                progressDelta = delta;
            }

            CardProficiencyWins after = playerData.GetCardProficiencyWins(id);
            float fillAfter = ComputeBarFill01FromWins(id, after);
            CardSkillRevealStage stageAfter = ResolveStageFromWins(id, after);

            string name = ResolveMonsterName(playerData, id);
            entries.Add(new BattleProficiencySettlementEntry(
                id, name, fillBefore, fillAfter, stageBefore, stageAfter, progressDelta));
        }
    }

    public static CardProficiencyWins GetWins(int monsterId)
    {
        PlayerData pd = ResolvePlayerData();
        return pd != null ? pd.GetCardProficiencyWins(monsterId) : default;
    }

    public static string FormatProgressDelta(float delta)
    {
        if (delta <= 0.0001f) return string.Empty;
        return "+" + delta.ToString("0.#");
    }

    public static string GetStageShortLabel(CardSkillRevealStage stage)
    {
        switch (stage)
        {
            case CardSkillRevealStage.BasicB: return "B · 基礎";
            case CardSkillRevealStage.FullC: return "C · 完整";
            default: return "A · 未解放";
        }
    }

    public static ProficiencyBarViewModel GetProficiencyBarForCard(Card card)
    {
        if (card is MonsterCard monster)
            return GetProficiencyBar(monster.id);
        return default;
    }

    public static ProficiencyBarViewModel GetProficiencyBar(int monsterId)
    {
        if (!ShouldShowProficiencyBar(monsterId))
            return default;

        CardProficiencyWins wins = GetWins(monsterId);
        CardSkillRevealStage unlocked = ResolveStageFromWins(monsterId, wins);
        bool starterTrio = IsStarterTrio(monsterId);
        float fill = ComputeBarFill01FromWins(monsterId, wins);
        string status = BuildBarStatusText(unlocked, wins, starterTrio);
        return new ProficiencyBarViewModel(true, "怪物牌 熟練度", Mathf.Clamp01(fill), status);
    }

    public static float ComputeBarFill01FromWins(int monsterId, CardProficiencyWins wins)
    {
        if (IsStarterTrio(monsterId))
            return 1f;

        CardSkillRevealStage unlocked = ResolveStageFromWins(monsterId, wins);
        if (unlocked == CardSkillRevealStage.FullC)
            return 1f;

        if (unlocked == CardSkillRevealStage.BasicB)
        {
            if (WinsRequiredForStageC <= 0) return 1f;
            return Mathf.Clamp01((float)wins.winsNormalDifficulty / WinsRequiredForStageC);
        }

        if (WinsRequiredForStageB <= 0) return 0f;
        return Mathf.Clamp01(wins.progressAny / WinsRequiredForStageB);
    }

    public static string BuildProficiencyStepperRich(CardSkillRevealStage unlocked)
    {
        string Pill(CardSkillRevealStage stage, string label)
        {
            bool done = (int)unlocked > (int)stage;
            bool current = unlocked == stage;
            if (done)
                return "<color=#8BCB9A>■ " + label + "</color>";
            if (current)
                return "<color=#F8D878>▶ " + label + "</color>";
            return "<color=#5A6A7A>□ " + label + "</color>";
        }

        return Pill(CardSkillRevealStage.LockedA, "A 未解放") + "  —  " +
               Pill(CardSkillRevealStage.BasicB, "B 基礎") + "  —  " +
               Pill(CardSkillRevealStage.FullC, "C 完整");
    }

    private static string BuildBarStatusText(CardSkillRevealStage unlocked, CardProficiencyWins wins, bool starterTrio)
    {
        if (starterTrio)
            return GetStageShortLabel(CardSkillRevealStage.FullC);

        if (unlocked == CardSkillRevealStage.FullC)
            return GetStageShortLabel(unlocked);

        if (unlocked == CardSkillRevealStage.BasicB)
            return GetStageShortLabel(unlocked) + "  " + FormatProgressTowardC(wins.winsNormalDifficulty);

        return FormatProgressTowardB(wins.progressAny);
    }

    /// <summary>B 階段 toward-C：累計「普通以上」難度勝場（入門、簡單不計入此數字）。</summary>
    public static string FormatProgressTowardC(int winsNormalDifficulty)
    {
        int shown = Mathf.Clamp(winsNormalDifficulty, 0, WinsRequiredForStageC);
        return shown + "/" + WinsRequiredForStageC + " " + StageCDifficultyRequirementLabelZh;
    }

    public static string FormatProgressTowardB(float progressAny)
    {
        float shown = Mathf.Min(progressAny, WinsRequiredForStageB);
        if (shown >= WinsRequiredForStageB - 0.05f)
            return WinsRequiredForStageB + "/" + WinsRequiredForStageB;
        return shown.ToString("0.#") + "/" + WinsRequiredForStageB;
    }

    private static string ResolveMonsterName(PlayerData playerData, int monsterId)
    {
        Card card = playerData?.CardStore?.GetCardById(monsterId);
        if (card != null && !string.IsNullOrWhiteSpace(card.cardName))
            return card.cardName.Trim();
        return "怪物 #" + monsterId;
    }

    private static bool IsMonsterCardId(int cardId)
    {
        if (DeckCardId.IsSpellKey(cardId)) return false;
        PlayerData pd = ResolvePlayerData();
        if (pd != null && pd.CardStore != null)
            return pd.CardStore.GetCardById(cardId) is MonsterCard;
        return cardId > 0;
    }

    /// <summary>測試用：清除所有非御三家怪物牌的熟練度進度（記憶體＋可選存檔）。</summary>
    public static int ResetAllProficiencyForTesting(PlayerData playerData, bool saveAfter = true)
    {
        if (playerData == null) return 0;
        CardStore store = playerData.CardStore;
        if (saveAfter)
            return CardProficiencyDebugReset.PerformFullReset(playerData, store, reloadAfterSave: true);
        return CardProficiencyDebugReset.ClearRuntimeProficiency(playerData, store);
    }

    private static PlayerData ResolvePlayerData() => PlayerData.ResolveCanonical();
}

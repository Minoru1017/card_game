/// <summary>
/// 戰前預覽謎題索引常數 — 對照 <c>BATTLE_PREVIEW_PUZZLE_INDEX.md</c>。
/// </summary>
public static class BattlePreviewPuzzleIndex
{
    // §1 謎題 ID
    public const string Pz01TrainingGroundBossUnlock = "PZ01";
    public const string Pz02FindHardDifficulty = "PZ02";
    /// <summary>Story progress 港灣訓練場：僅簡單／普通／困難，無謎題解鎖。</summary>
    public const string HarborTrainingGround = "HARBOR_M11";

    /// <summary>戰前預覽隨機抽選：PZ01 權重（PZ02 = 1 - 此值）。</summary>
    public const float RandomPreviewPuzzlePz01Weight = 0.5f;

    public static readonly string[] RandomPreviewPuzzlePool =
    {
        Pz01TrainingGroundBossUnlock,
        Pz02FindHardDifficulty
    };

    /// <summary>以 <see cref="RandomPreviewPuzzlePz01Weight"/> 抽 PZ01，否則 PZ02。</summary>
    public static string RollRandomPreviewPuzzleId()
    {
        return UnityEngine.Random.value < RandomPreviewPuzzlePz01Weight
            ? Pz01TrainingGroundBossUnlock
            : Pz02FindHardDifficulty;
    }

    public const string TagTrainingGround = "#訓練場";
    public const string TagFindHard = "#找出困難級";

    public readonly struct AuthoredArchSlot
    {
        public readonly BattleDifficultyTier DisplayTier;
        public readonly BattleDifficultyTier ActualTier;

        public AuthoredArchSlot(BattleDifficultyTier displayTier, BattleDifficultyTier actualTier)
        {
            DisplayTier = displayTier;
            ActualTier = actualTier;
        }
    }

    /// <summary>PZ01：魔王級解鎖點擊序（簡單 → 普通 → 入門 → 困難，各選取一次）。</summary>
    public static readonly BattleDifficultyTier[] BossUnlockClickSequence =
    {
        BattleDifficultyTier.Easy,
        BattleDifficultyTier.Normal,
        BattleDifficultyTier.Intro,
        BattleDifficultyTier.Hard
    };

    /// <summary>PZ01：四拱門由左至右（顯示與實際一致）。</summary>
    public static readonly AuthoredArchSlot[] Pz01ArchSlotsLeftToRight =
    {
        new AuthoredArchSlot(BattleDifficultyTier.Intro, BattleDifficultyTier.Intro),
        new AuthoredArchSlot(BattleDifficultyTier.Easy, BattleDifficultyTier.Easy),
        new AuthoredArchSlot(BattleDifficultyTier.Normal, BattleDifficultyTier.Normal),
        new AuthoredArchSlot(BattleDifficultyTier.Hard, BattleDifficultyTier.Hard)
    };

    /// <summary>PZ02：解鎖點擊序（入門 → 簡單 → 普通 → 普通，須選取回饋）。</summary>
    public static readonly BattleDifficultyTier[] Pz02HardUnlockClickSequence =
    {
        BattleDifficultyTier.Intro,
        BattleDifficultyTier.Easy,
        BattleDifficultyTier.Normal,
        BattleDifficultyTier.Normal
    };

    /// <summary>港灣訓練場：由左至右 簡單、普通、困難。</summary>
    public static readonly AuthoredArchSlot[] HarborTrainingArchSlotsLeftToRight =
    {
        new AuthoredArchSlot(BattleDifficultyTier.Easy, BattleDifficultyTier.Easy),
        new AuthoredArchSlot(BattleDifficultyTier.Normal, BattleDifficultyTier.Normal),
        new AuthoredArchSlot(BattleDifficultyTier.Hard, BattleDifficultyTier.Hard)
    };

    /// <summary>PZ02：由左至右顯示 普通、簡單、入門、入門（解鎖後第 4 欄改為困難級）。</summary>
    public static readonly AuthoredArchSlot[] Pz02ArchSlotsLeftToRight =
    {
        new AuthoredArchSlot(BattleDifficultyTier.Normal, BattleDifficultyTier.Normal),
        new AuthoredArchSlot(BattleDifficultyTier.Easy, BattleDifficultyTier.Easy),
        new AuthoredArchSlot(BattleDifficultyTier.Intro, BattleDifficultyTier.Intro),
        new AuthoredArchSlot(BattleDifficultyTier.Intro, BattleDifficultyTier.Intro)
    };

    /// <summary>PZ02 困難級顯現的拱門欄位（0-based，第 4 欄 = 3）。</summary>
    public const int Pz02HardUnlockArchSlotIndex = 3;

    public const int UnlockClickStepCount = 4;
    public const int BossUnlockStepCount = UnlockClickStepCount;

    public static readonly BattleDifficultyTier[] ArchTiersLeftToRight =
    {
        BattleDifficultyTier.Intro,
        BattleDifficultyTier.Easy,
        BattleDifficultyTier.Normal,
        BattleDifficultyTier.Hard
    };

    // PZ01 文案
    public const string Pz01PuzzleTitleLockedRich = "<b>謎題</b> <color=#8A6B3A>#訓練場</color>";
    public const string Pz01PuzzleHintLocked = "請找出魔王級並通關一次";
    public const string Pz01PuzzleTitleUnlockedRich = "<size=110%><b>魔王級</b></size>";

    // PZ02 文案
    public const string Pz02PuzzleTitleLockedRich = "<b>謎題</b> <color=#8A6B3A>#找出困難級</color>";
    public const string Pz02PuzzleHintLocked = "請找出困難級";
    public const string Pz02PuzzleTitleUnlockedRich = "<size=110%><b>困難級</b></size>";

    // 共用
    public const string HeaderSelectDifficultyRich = "<size=115%><b>選擇難易度</b></size>";
    public const string LeftColumnTitle = "初次通關獎勵";
    public const string LeftColumnDetailPlaceholder = "將獲得...";
    public const string RightColumnTitle = "放棄解謎";
    public const string RightColumnDetailPlaceholder = "將損失...";

    public static AuthoredArchSlot[] GetArchSlotsForPuzzle(string puzzleId)
    {
        if (puzzleId == HarborTrainingGround)
            return HarborTrainingArchSlotsLeftToRight;
        if (puzzleId == Pz02FindHardDifficulty)
            return Pz02ArchSlotsLeftToRight;
        return Pz01ArchSlotsLeftToRight;
    }

    public static BattleDifficultyTier GetAuthoredRevealTier(string puzzleId)
    {
        return puzzleId == Pz02FindHardDifficulty
            ? BattleDifficultyTier.Hard
            : BattleDifficultyTier.Boss;
    }

    public static string GetPuzzleTag(string puzzleId)
    {
        return puzzleId == Pz02FindHardDifficulty ? TagFindHard : TagTrainingGround;
    }

    public static string GetPuzzleTitleLockedRich(string puzzleId) =>
        puzzleId == Pz02FindHardDifficulty ? Pz02PuzzleTitleLockedRich : Pz01PuzzleTitleLockedRich;

    public static string GetPuzzleHintLocked(string puzzleId) =>
        puzzleId == Pz02FindHardDifficulty ? Pz02PuzzleHintLocked : Pz01PuzzleHintLocked;

    public static string GetPuzzleTitleUnlockedRich(string puzzleId) =>
        puzzleId == Pz02FindHardDifficulty ? Pz02PuzzleTitleUnlockedRich : Pz01PuzzleTitleUnlockedRich;

    public static BattleDifficultyTier[] GetUnlockClickSequence(string puzzleId)
    {
        if (puzzleId == Pz02FindHardDifficulty)
            return Pz02HardUnlockClickSequence;
        return BossUnlockClickSequence;
    }

    public static bool UsesUnlockClickSequence(string puzzleId) =>
        puzzleId == Pz01TrainingGroundBossUnlock || puzzleId == Pz02FindHardDifficulty;

    /// <summary>PZ02 解鎖後困難級出現於第 4 欄拱門（非揭示區大按鈕）。</summary>
    public static bool RevealsHiddenTierInFourthArchSlot(string puzzleId) =>
        puzzleId == Pz02FindHardDifficulty;

    public static string BuildPuzzleHintUnlockedRich(string difficultyLabelZh)
    {
        string label = string.IsNullOrWhiteSpace(difficultyLabelZh) ? "?" : difficultyLabelZh.Trim();
        return $"<color=#6C533D>隱藏難度已顯現</color>\n<color=#43573A>目前選擇: {label}</color>";
    }

    public static string GetBossUnlockStepLabelZh(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= BossUnlockClickSequence.Length)
            return "?";
        return GetDifficultyTierDisplayZh(BossUnlockClickSequence[stepIndex]);
    }

    public static string GetDifficultyTierDisplayZh(BattleDifficultyTier tier)
    {
        switch (tier)
        {
            case BattleDifficultyTier.Intro: return "入門";
            case BattleDifficultyTier.Easy: return "簡單";
            case BattleDifficultyTier.Normal: return "普通";
            case BattleDifficultyTier.Hard: return "困難";
            case BattleDifficultyTier.Boss: return "魔王";
            default: return tier.ToString();
        }
    }

    public static bool IsCorrectUnlockStep(string puzzleId, BattleDifficultyTier tier, int currentStep)
    {
        BattleDifficultyTier[] sequence = GetUnlockClickSequence(puzzleId);
        return currentStep >= 0
            && currentStep < sequence.Length
            && tier == sequence[currentStep];
    }

    public static bool IsUnlockSequenceComplete(int currentStep) =>
        currentStep >= UnlockClickStepCount;

    public static bool IsCorrectBossUnlockStep(BattleDifficultyTier tier, int currentStep) =>
        IsCorrectUnlockStep(Pz01TrainingGroundBossUnlock, tier, currentStep);

    public static bool IsBossUnlockSequenceComplete(int currentStep) =>
        IsUnlockSequenceComplete(currentStep);
}

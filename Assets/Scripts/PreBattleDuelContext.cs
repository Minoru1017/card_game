/// <summary>
/// 戰前鬥鳥流程的跨場景情境：戰前預覽 →（Fighting bird game）→ 戰鬥。
/// 取代原本的戰前謎題：鬥鳥勝利解鎖並直接挑戰隱藏難度，平手／落敗則打預覽所選難度。
/// </summary>
public static class PreBattleDuelContext
{
    /// <summary>是否為戰前鬥鳥（true：結束後接戰鬥；false：從 hall 進入的單機練習，結束返回 hall）。</summary>
    public static bool IsActive { get; private set; }

    /// <summary>是否為港灣訓練場戰鬥（決定設定還原路徑）。</summary>
    public static bool IsHarborTraining { get; private set; }

    /// <summary>鬥鳥結束後要載入的戰鬥場景名稱。</summary>
    public static string BattleSceneName { get; private set; }

    /// <summary>玩家在戰前預覽選擇的難度（平手／落敗時採用）。</summary>
    public static BattleDifficultyTier SelectedTier { get; private set; }

    /// <summary>本戰是否提供隱藏難度（鬥鳥勝利可挑戰）。</summary>
    public static bool HasHiddenTier { get; private set; }

    /// <summary>隱藏難度（通常為魔王級）。</summary>
    public static BattleDifficultyTier HiddenTier { get; private set; }

    /// <summary>鬥鳥結束後要帶入戰鬥顯示的戰前情報文字。</summary>
    public static string IntelText { get; private set; }

    /// <summary>本場敵方英雄 id（港灣 v1：熱血同學）。</summary>
    public static string HeroId { get; private set; }

    /// <summary>本場敵方英雄顯示名。</summary>
    public static string EnemyHeroDisplayName { get; private set; }

    /// <summary>由戰前預覽寫入：開始一段戰前鬥鳥。</summary>
    public static void Begin(
        string battleSceneName,
        bool isHarborTraining,
        BattleDifficultyTier selectedTier,
        bool hasHiddenTier,
        BattleDifficultyTier hiddenTier,
        string heroId = null,
        string enemyHeroDisplayName = null)
    {
        IsActive = true;
        BattleSceneName = string.IsNullOrWhiteSpace(battleSceneName) ? null : battleSceneName.Trim();
        IsHarborTraining = isHarborTraining;
        SelectedTier = selectedTier;
        HasHiddenTier = hasHiddenTier;
        HiddenTier = hiddenTier;
        IntelText = null;
        HeroId = string.IsNullOrWhiteSpace(heroId) ? null : heroId.Trim();
        EnemyHeroDisplayName = string.IsNullOrWhiteSpace(enemyHeroDisplayName)
            ? null
            : enemyHeroDisplayName.Trim();
    }

    /// <summary>由戰鬥場景載入時讀取情報並清空，避免重複顯示。</summary>
    public static string ConsumeIntelText()
    {
        string text = IntelText;
        IntelText = null;
        return text;
    }

    public static void SetIntelText(string text)
    {
        IntelText = string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>結束戰前鬥鳥流程（保留 IntelText 供戰鬥讀取）。</summary>
    public static void ClearActive()
    {
        IsActive = false;
        IsHarborTraining = false;
        BattleSceneName = null;
        HasHiddenTier = false;
        HeroId = null;
        EnemyHeroDisplayName = null;
    }
}

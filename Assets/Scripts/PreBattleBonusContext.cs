using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>鬥鳥加成可指定的開局天氣（避免外部依賴 BattleSimulationManager 的私有列舉）。</summary>
public enum BirdDuelOpeningWeather
{
    None,
    Gale, // 穿堂微風：首張法術 +20%
    Fog   // 訓練薄霧：英雄受擊 −50%
}

/// <summary>鬥鳥加成聚合後的開局效果，由 <c>BattleSimulationManager.StartBattle</c> 快照並套用。</summary>
public struct BirdDuelBonusEffects
{
    public int PlayerHpDelta;            // 起始 HP 加值（相加）
    public int PlayerHpAbsolute;         // >0 時直接覆寫玩家起始 HP（背水）
    public int OpeningExtraDraw;         // 開局多抽張數
    public int PlayerExtraDrawPerTurn;   // 我方每回合多抽張數（整場）
    public int EnemyHpDelta;             // 敵方起始 HP 加值
    public int EnemyExtraOpeningDraw;    // 敵方首回合多抽張數
    public float EnemyDamageMultiplier;  // 敵方傷害倍率（整場，預設 1）
    public bool UnlockPlayerOpeningAttack; // 解除我方首回合禁攻
    public int RevealEnemyHandCount;     // 開戰揭示敵方手牌張數（看破）
    public BirdDuelOpeningWeather OpeningWeather; // 指定開局天氣
}

/// <summary>
/// 戰前鬥鳥 roguelike 分支的「本場加成」跨場景情境：
/// 鬥鳥結算選定加成後寫入，戰鬥 <c>StartBattle</c> 讀取並套用（單場有效）。
/// 見 Docs/鬥鳥手勢小遊戲企劃.md 第九章。
/// </summary>
public static class PreBattleBonusContext
{
    private static readonly List<BirdDuelBonusId> playerBonuses = new List<BirdDuelBonusId>();
    private static BirdDuelBonusEffects pendingEffects;
    private static string pendingAnnouncement;

    public static bool IsActive { get; private set; }
    public static BirdDuelBonusId EnemyBuff { get; private set; }
    public static IReadOnlyList<BirdDuelBonusId> PlayerBonuses => playerBonuses;

    /// <summary>由鬥鳥結算寫入：本場玩家加成清單與（風險 B）敵方小強化。</summary>
    public static void Begin(IEnumerable<BirdDuelBonusId> bonuses, BirdDuelBonusId enemyBuff)
    {
        playerBonuses.Clear();
        if (bonuses != null)
        {
            foreach (BirdDuelBonusId b in bonuses)
            {
                if (b == BirdDuelBonusId.None || playerBonuses.Contains(b)) continue;
                playerBonuses.Add(b);
            }
        }

        EnemyBuff = enemyBuff;
        IsActive = playerBonuses.Count > 0 || enemyBuff != BirdDuelBonusId.None;
        pendingEffects = IsActive ? BuildEffectsInternal() : DefaultEffects();
        pendingAnnouncement = IsActive ? BuildAnnouncementTextInternal() : null;
    }

    /// <summary>清空（戰鬥 StartBattle 快照後、或「直接進入對戰」未挑戰鬥鳥時呼叫）。</summary>
    public static void Clear()
    {
        playerBonuses.Clear();
        EnemyBuff = BirdDuelBonusId.None;
        pendingEffects = DefaultEffects();
        pendingAnnouncement = null;
        IsActive = false;
    }

    /// <summary>戰鬥 <c>StartBattle</c> 取用並清空本場加成（避免重複 StartBattle 或外溢）。</summary>
    public static bool TryConsumeForBattle(out BirdDuelBonusEffects effects, out string announcement)
    {
        if (!IsActive)
        {
            effects = DefaultEffects();
            announcement = null;
            return false;
        }

        effects = pendingEffects;
        announcement = pendingAnnouncement;
        Clear();
        return true;
    }

    /// <summary>聚合目前加成成單一效果結構。未啟用時回傳「無效果」。</summary>
    public static BirdDuelBonusEffects BuildEffects()
    {
        if (!IsActive) return DefaultEffects();
        return pendingEffects;
    }

    /// <summary>組裝開戰前播報文字（僅當本場有鬥鳥加成／詛咒／敵方強化時）。</summary>
    public static string BuildAnnouncementText()
    {
        if (!IsActive) return null;
        return pendingAnnouncement;
    }

    private static BirdDuelBonusEffects DefaultEffects()
    {
        return new BirdDuelBonusEffects { EnemyDamageMultiplier = 1f };
    }

    private static BirdDuelBonusEffects BuildEffectsInternal()
    {
        BirdDuelBonusEffects e = DefaultEffects();
        for (int i = 0; i < playerBonuses.Count; i++)
            ApplyOne(ref e, playerBonuses[i]);
        ApplyOne(ref e, EnemyBuff);
        return e;
    }

    private static string BuildAnnouncementTextInternal()
    {
        if (playerBonuses.Count == 0 && EnemyBuff == BirdDuelBonusId.None) return null;

        StringBuilder sb = new StringBuilder();
        AppendBonusSection(sb, "【鬥鳥加成】", false);
        AppendBonusSection(sb, "【詛咒】", true);

        if (EnemyBuff != BirdDuelBonusId.None)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("【敵方強化】\n");
            AppendBonusLine(sb, EnemyBuff);
        }

        return sb.Length > 0 ? sb.ToString().Trim() : null;
    }

    private static void AppendBonusSection(StringBuilder sb, string header, bool curseOnly)
    {
        bool wroteHeader = false;
        for (int i = 0; i < playerBonuses.Count; i++)
        {
            BirdDuelBonusId id = playerBonuses[i];
            if (BirdDuelBonusCatalog.IsCurse(id) != curseOnly) continue;
            if (!wroteHeader)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(header).Append('\n');
                wroteHeader = true;
            }
            AppendBonusLine(sb, id);
        }
    }

    private static void AppendBonusLine(StringBuilder sb, BirdDuelBonusId id)
    {
        BirdDuelBonusInfo info = BirdDuelBonusCatalog.Get(id);
        if (info.Id == BirdDuelBonusId.None) return;
        sb.Append('·').Append(' ').Append(info.DisplayName).Append('：').Append(info.Description).Append('\n');
    }

    private static void ApplyOne(ref BirdDuelBonusEffects e, BirdDuelBonusId id)
    {
        switch (id)
        {
            case BirdDuelBonusId.MorningPractice: e.PlayerHpDelta += 3; break;
            case BirdDuelBonusId.ExtraCard: e.OpeningExtraDraw += 1; break;
            case BirdDuelBonusId.SteadyStance: e.EnemyDamageMultiplier *= 0.85f; break;
            case BirdDuelBonusId.Tailwind: e.OpeningWeather = BirdDuelOpeningWeather.Gale; break;
            case BirdDuelBonusId.InsightOpening:
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 1); break;

            case BirdDuelBonusId.DeepRest: e.PlayerHpDelta += 6; break;
            case BirdDuelBonusId.FirstStrike: e.UnlockPlayerOpeningAttack = true; break;
            case BirdDuelBonusId.DoubleDraw: e.OpeningExtraDraw += 2; break;
            case BirdDuelBonusId.Regroup: e.PlayerExtraDrawPerTurn += 1; break;
            case BirdDuelBonusId.Suppress: e.EnemyDamageMultiplier *= 0.8f; break;
            case BirdDuelBonusId.InsightFull:
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 2);
                e.EnemyDamageMultiplier *= 0.9f;
                break;

            case BirdDuelBonusId.Providence: e.OpeningWeather = BirdDuelOpeningWeather.Fog; break;
            case BirdDuelBonusId.FullDraw:
                e.PlayerHpDelta += 6;
                e.OpeningExtraDraw += 2;
                e.UnlockPlayerOpeningAttack = true;
                break;
            case BirdDuelBonusId.LastStand:
                e.PlayerHpAbsolute = 12;
                e.OpeningExtraDraw += 2;
                e.UnlockPlayerOpeningAttack = true;
                e.OpeningWeather = BirdDuelOpeningWeather.Gale;
                break;

            case BirdDuelBonusId.CourtDecree:
                e.UnlockPlayerOpeningAttack = true;
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 1);
                break;
            case BirdDuelBonusId.RoyalPhalanx:
                e.PlayerHpDelta += 4;
                e.EnemyDamageMultiplier *= 0.88f;
                break;
            case BirdDuelBonusId.VanguardRecon:
                e.OpeningExtraDraw += 1;
                e.RevealEnemyHandCount = Mathf.Max(e.RevealEnemyHandCount, 2);
                break;
            case BirdDuelBonusId.CrownGuard:
                e.PlayerHpDelta += 3;
                e.OpeningWeather = BirdDuelOpeningWeather.Fog;
                break;
            case BirdDuelBonusId.WarDrumCharge:
                e.UnlockPlayerOpeningAttack = true;
                e.OpeningExtraDraw += 1;
                break;

            case BirdDuelBonusId.EnemyMorale: e.EnemyHpDelta += 2; break;
            case BirdDuelBonusId.EnemyDraw: e.EnemyExtraOpeningDraw += 1; break;
            case BirdDuelBonusId.EnemyOffense: e.EnemyDamageMultiplier *= 1.1f; break;
        }
    }
}

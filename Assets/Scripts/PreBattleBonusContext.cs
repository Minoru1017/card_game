using System.Collections.Generic;
using System.Text;

/// <summary>
/// 戰前鬥鳥 roguelike 分支的「本場加成」跨場景情境：
/// 鬥鳥結算選定加成後寫入，戰鬥 StartBattle 讀取並套用（單場有效）。
/// </summary>
public static partial class PreBattleBonusContext
{
    private static readonly List<BirdDuelBonusId> playerBonuses = new List<BirdDuelBonusId>();
    private static BirdDuelBonusEffects pendingEffects;
    private static string pendingAnnouncement;

    public static bool IsActive { get; private set; }
    public static BirdDuelBonusId EnemyBuff { get; private set; }
    public static IReadOnlyList<BirdDuelBonusId> PlayerBonuses => playerBonuses;

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

    public static void Clear()
    {
        playerBonuses.Clear();
        EnemyBuff = BirdDuelBonusId.None;
        pendingEffects = DefaultEffects();
        pendingAnnouncement = null;
        IsActive = false;
    }

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

    public static BirdDuelBonusEffects BuildEffects() =>
        IsActive ? pendingEffects : DefaultEffects();

    public static string BuildAnnouncementText() =>
        IsActive ? pendingAnnouncement : null;

    private static BirdDuelBonusEffects DefaultEffects() =>
        new BirdDuelBonusEffects { EnemyDamageMultiplier = 1f };

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
}

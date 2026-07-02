using UnityEngine;

/// <summary>
/// Free Battle → Buildbeck：帶入所選 AI 風格，組牌後由 <see cref="SceneLoader.EnterBattle"/> 開自由對戰預覽。
/// </summary>
public static class FreeBattleViewSession
{
    private static bool hasPendingAiStyle;
    private static EnemyAiPlayStyle pendingAiStyle = EnemyAiPlayStyle.Balanced;

    public static bool HasPendingAiStyle => hasPendingAiStyle;

    public static void Begin(EnemyAiPlayStyle aiStyle)
    {
        hasPendingAiStyle = true;
        pendingAiStyle = aiStyle;
    }

    public static bool TryGetPendingAiStyle(out EnemyAiPlayStyle aiStyle)
    {
        if (!hasPendingAiStyle)
        {
            aiStyle = EnemyAiPlayStyle.Balanced;
            return false;
        }

        aiStyle = pendingAiStyle;
        return true;
    }

    public static void Clear()
    {
        hasPendingAiStyle = false;
        pendingAiStyle = EnemyAiPlayStyle.Balanced;
    }

    public static string GetBuildbeckEntryToast()
    {
        if (!hasPendingAiStyle)
            return null;
        return "自由對戰 · " + FreeBattleBattleCopy.GetAiStyleDisplayZh(pendingAiStyle) + "\n組好牌組後按「準備完成」選擇難度";
    }
}

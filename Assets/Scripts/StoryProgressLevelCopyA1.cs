/// <summary>Story progress 面板文案：S-A-1 潮間島。</summary>
public static class StoryProgressLevelCopyA1
{
    public const string LevelTitle = "潮間島";

    public static string BuildScenarioIntro(int slot)
    {
        if (SideQuestA1ProgressState.IsNodeCleared(slot))
            return "潮間島 · 草奶奶的三畦輪作已完成。可重溫農事，解封僅首次有效。";
        if (!SideQuestA1ProgressState.IsSealedSpellReady(slot))
            return "潮間島 · 需先在 M-1-2 海牆散策取得封印的法術。";
        return "潮間島 · 搭舵叔的舢板登島，幫草奶奶完成三畦輪作，解封潮印。";
    }

    public static string BuildScenarioRewards(int slot)
    {
        if (SideQuestA1ProgressState.IsTideMarkUnsealed(slot))
        {
            string rewards = "已完成：潮印解封 · 法術「潮印」入收藏";
            if (SideQuestA1ProgressState.IsSeaPurslaneSeedKept(slot))
                rewards += " · 海蓬種";
            return rewards;
        }

        return "通關獎：解封潮印 · 金幣 · 三畦農事（可跳過，跳過不解封）";
    }

    public static string ResolveEnterButtonLabel(int slot) =>
        SideQuestA1ProgressState.IsNodeCleared(slot) ? "重訪潮間島" : "前往潮間島";
}

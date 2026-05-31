using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>測試用：將卡牌熟練度清零（御三家除外）。</summary>
public static class CardProficiencyTestResetTools
{
    [MenuItem("Card Game/Testing/Reset All Card Proficiency (Keep Starter Trio)")]
    public static void ResetRuntimeAndPersistentSave()
    {
        PlayerData pd = PlayerData.ResolveCanonical();
        CardStore store = pd != null ? pd.CardStore : null;
        if (Application.isPlaying && pd != null)
            CardProficiencyDebugReset.PerformFullReset(pd, store, reloadAfterSave: true);

        int strippedPersistent = CardProficiencyDebugReset.StripAllNonStarterProficiencyRows(
            CardProficiencyDebugReset.GetPersistentPlayerDataCsvPath());
        int strippedMirror = CardProficiencyDebugReset.StripAllNonStarterProficiencyRows(
            Path.Combine(Application.dataPath, "PlayerDataSnapshots/playerdata.profile_mirror.csv"));

        AssetDatabase.Refresh();
        Debug.Log(
            "[CardProficiencyTestReset] 完成。persistent 刪除 " + strippedPersistent +
            " 列；鏡像刪除 " + strippedMirror + " 列。");
    }

    [MenuItem("Card Game/Testing/Reset Snapshot Mirror Proficiency Only")]
    public static void ResetMirrorSnapshotOnly()
    {
        string path = Path.Combine(Application.dataPath, "PlayerDataSnapshots/playerdata.profile_mirror.csv");
        int n = CardProficiencyDebugReset.StripAllNonStarterProficiencyRows(path);
        AssetDatabase.Refresh();
        Debug.Log("[CardProficiencyTestReset] 鏡像快照刪除 proficiency 列數=" + n);
    }
}

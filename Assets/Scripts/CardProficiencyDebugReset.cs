using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>測試／Debug：清空卡牌熟練度存檔與 CSV（御三家進度條仍依規則顯示完整）。</summary>
public static class CardProficiencyDebugReset
{
    private const float FillEpsilon = 0.001f;

    /// <summary>記憶體、磁碟存檔一次清空；回傳清除的怪物 id 筆數。</summary>
    public static int PerformFullReset(PlayerData playerData, CardStore cardStore, bool reloadAfterSave = true)
    {
        if (playerData == null) return 0;

        int removed = ClearRuntimeProficiency(playerData, cardStore);
        int stripped = StripAllNonStarterProficiencyRows(GetPersistentPlayerDataCsvPath());
        playerData.SavePlayerData();

        if (reloadAfterSave)
            playerData.LoadPlayerData();

        Debug.Log(
            "[CardProficiencyDebugReset] 已清空熟練度。執行期移除 " + removed + " 筆；CSV 刪除 " + stripped +
            " 列。御三家 (id " + MonsterSkillIds.Militia + "/" + MonsterSkillIds.Queen + "/" +
            MonsterSkillIds.King + ") 仍為完整戰技。");
        return removed;
    }

    public static string GetPersistentPlayerDataCsvPath() =>
        Path.Combine(Application.persistentDataPath, "playerdata.csv");

    public static int ClearRuntimeProficiency(PlayerData playerData, CardStore cardStore)
    {
        var removeIds = new HashSet<int>();

        foreach (var kv in playerData.GetAllCardProficiencyWinsSnapshot())
        {
            if (!CardSkillProficiencyService.IsStarterTrio(kv.Key))
                removeIds.Add(kv.Key);
        }

        if (cardStore != null && cardStore.cardList != null)
        {
            for (int i = 0; i < cardStore.cardList.Count; i++)
            {
                if (cardStore.cardList[i] is MonsterCard monster && !CardSkillProficiencyService.IsStarterTrio(monster.id))
                    removeIds.Add(monster.id);
            }
        }

        foreach (int id in removeIds)
            playerData.RemoveCardProficiencyWins(id);

        return removeIds.Count;
    }

    public static int StripAllNonStarterProficiencyRows(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
            return 0;

        string[] lines = File.ReadAllLines(csvPath);
        var kept = new List<string>(lines.Length);
        int removed = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (TryParseProficiencyMonsterId(lines[i], out int monsterId) &&
                !CardSkillProficiencyService.IsStarterTrio(monsterId))
            {
                removed++;
                continue;
            }

            kept.Add(lines[i]);
        }

        if (removed > 0)
            File.WriteAllLines(csvPath, kept);

        return removed;
    }

    public static bool TryParseProficiencyMonsterId(string line, out int monsterId)
    {
        monsterId = -1;
        if (string.IsNullOrWhiteSpace(line)) return false;

        string[] parts = line.Split(',');
        if (parts.Length >= 5 &&
            parts[0].Trim() == "slot" &&
            parts[2].Trim() == "proficiency" &&
            parts[3].Trim() == "m" &&
            int.TryParse(parts[4].Trim(), out monsterId))
            return true;

        if (parts.Length >= 3 &&
            parts[0].Trim() == "proficiency" &&
            parts[1].Trim() == "m" &&
            int.TryParse(parts[2].Trim(), out monsterId))
            return true;

        monsterId = -1;
        return false;
    }

    public static void ApplyBackpackMasteryFill(RectTransform fillRt, float fill01)
    {
        if (fillRt == null) return;
        float fill = Mathf.Clamp01(fill01);
        bool visible = fill > FillEpsilon;
        Image fillImg = fillRt.GetComponent<Image>();
        if (fillImg != null)
            fillImg.enabled = visible;
        fillRt.anchorMax = new Vector2(visible ? fill : 0f, 1f);
    }
}

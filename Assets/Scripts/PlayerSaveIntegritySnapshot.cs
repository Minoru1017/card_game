using System;
using System.Collections.Generic;

/// <summary>playerdata.csv 作用中槽位的可監測摘要（供存檔完整性比對）。</summary>
[Serializable]
public sealed class PlayerSaveIntegritySnapshot
{
    public string capturedUtc;
    public int activeSlot = 1;
    public string slotName = string.Empty;
    public int coins;
    public int gems;
    public int cardStackTotal;
    public int cardTypeCount;
    public int battleRecordCount;
    public int valuablesCellCount;
    public int profileWins;
    public int profileLosses;

    public static PlayerSaveIntegritySnapshot CaptureFromCsvLines(IReadOnlyList<string> lines)
    {
        var snap = new PlayerSaveIntegritySnapshot
        {
            capturedUtc = DateTime.UtcNow.ToString("o"),
        };

        if (lines == null || lines.Count == 0)
            return snap;

        snap.activeSlot = ReadActiveSlot(lines);
        int slot = snap.activeSlot;

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            string[] cols = line.Split(',');
            if (cols.Length < 2)
                continue;

            string key0 = cols[0].Trim();
            if (string.Equals(key0, "slot", StringComparison.OrdinalIgnoreCase))
            {
                if (cols.Length < 4 || !int.TryParse(cols[1].Trim(), out int rowSlot) || rowSlot != slot)
                    continue;

                string slotKey = cols[2].Trim();
                if (string.Equals(slotKey, "coins", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(cols[3].Trim(), out int coins))
                    snap.coins = coins;
                else if (string.Equals(slotKey, "gems", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(cols[3].Trim(), out int gems))
                    snap.gems = gems;
                else if (string.Equals(slotKey, "slot_name", StringComparison.OrdinalIgnoreCase))
                    snap.slotName = cols[3].Trim();
                else if (string.Equals(slotKey, "card", StringComparison.OrdinalIgnoreCase) && cols.Length >= 5
                         && int.TryParse(cols[4].Trim(), out int cardQty))
                {
                    snap.cardTypeCount++;
                    snap.cardStackTotal += Math.Max(0, cardQty);
                }
                else if (string.Equals(slotKey, "battle_record", StringComparison.OrdinalIgnoreCase))
                    snap.battleRecordCount++;
                else if (string.Equals(slotKey, ValuablesVaultState.SaveKey, StringComparison.OrdinalIgnoreCase))
                    snap.valuablesCellCount++;
                else if (string.Equals(slotKey, "profile_wins", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(cols[3].Trim(), out int wins))
                    snap.profileWins = wins;
                else if (string.Equals(slotKey, "profile_losses", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(cols[3].Trim(), out int losses))
                    snap.profileLosses = losses;
            }
            else if (string.Equals(key0, "coins", StringComparison.OrdinalIgnoreCase) && cols.Length >= 2
                     && int.TryParse(cols[1].Trim(), out int legacyCoins))
            {
                snap.coins = legacyCoins;
            }
            else if (string.Equals(key0, "gems", StringComparison.OrdinalIgnoreCase) && cols.Length >= 2
                     && int.TryParse(cols[1].Trim(), out int legacyGems))
            {
                snap.gems = legacyGems;
            }
        }

        return snap;
    }

    private static int ReadActiveSlot(IReadOnlyList<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cols = line.Split(',');
            if (cols.Length < 2)
                continue;
            if (!string.Equals(cols[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase))
                continue;
            if (int.TryParse(cols[1].Trim(), out int slot))
                return UnityEngine.Mathf.Clamp(slot, 1, PlayerData.MaxPlayerSlots);
        }

        return 1;
    }

    public string BuildMetricLine() =>
        "槽位 " + activeSlot + " " + (string.IsNullOrWhiteSpace(slotName) ? string.Empty : slotName + " ") +
        "| 金幣 " + coins + " 寶石 " + gems +
        " | 背包 " + cardStackTotal + " (" + cardTypeCount + " 種)" +
        " | 戰績 " + battleRecordCount + " (勝 " + profileWins + " 負 " + profileLosses + ")" +
        " | 貴重品庫 " + valuablesCellCount + " 格";
}

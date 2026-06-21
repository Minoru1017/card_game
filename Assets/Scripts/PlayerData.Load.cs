using System;
using System.IO;
using UnityEngine;

public partial class PlayerData
{
    public void LoadPlayerData()
    {
        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
        {
            canonical.LoadPlayerData();
            return;
        }

        EnsureMinimumDeckSlotCount();
        EnsureDeckSlotMaps();
        saveHydratedFromDisk = false;

        playerCollection.Clear();
        for (int s = 0; s < deckSlotMaps.Length; s++)
            deckSlotMaps[s].Clear();
        cardProficiencyWins.Clear();
        ResetDeckSlotDisplayNamesForLoad();

        string path = GetPlayerDataPath();

        if (!PlayerPersistSafeIO.ExistsAnyWithBackups(path))
        {
            playerCoins = 100;
            totalCoins = playerCoins;
            playerGems = 300;
            saveHydratedFromDisk = true;
            SavePlayerData();
            return;
        }

        PlayerDeckSlotNameStorage.DeckSlotNameRepairReport pollutionRepair =
            PlayerDeckSlotNameStorage.RepairPersistedDeckSlotNamePollutionIfNeeded();

        foreach (string candidatePath in PlayerPersistSafeIO.EnumerateLoadCandidates(path))
        {
            if (!File.Exists(candidatePath)) continue;
            string[] dataRow;
            try
            {
                dataRow = PlayerPersistSafeIO.ReadAllLines(candidatePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("PlayerData: could not read " + candidatePath + " -> " + ex.Message);
                continue;
            }

            if (!PlayerPersistSafeIO.LooksLikePlayerDataCsv(dataRow)) continue;

            playerCollection.Clear();
            for (int s = 0; s < deckSlotMaps.Length; s++)
                deckSlotMaps[s].Clear();
            cardProficiencyWins.Clear();
            ResetDeckSlotDisplayNamesForLoad();

            if (!TryApplyLoadedPlayerDataRows(dataRow)) continue;

            PlayerBirdDuelCdState.EnsureNewPlayerDefaults(activePlayerSlot);
            saveHydratedFromDisk = true;
            FinishLoadVaultCacheRefresh(pollutionRepair);

            Debug.Log("Load from persistent: " + candidatePath);
            Debug.Log("Loaded coins=" + playerCoins);
            return;
        }

        Debug.LogError("PlayerData: all save candidates failed to load; recreating defaults.");
        playerCoins = 100;
        totalCoins = playerCoins;
        playerGems = 300;
        PlayerBirdDuelCdState.EnsureNewPlayerDefaults(activePlayerSlot);
        saveHydratedFromDisk = true;
        SavePlayerData();
    }

    private void FinishLoadVaultCacheRefresh(
        PlayerDeckSlotNameStorage.DeckSlotNameRepairReport pollutionRepair = default)
    {
        if (pollutionRepair.AnyChanges && pollutionRepair.RepairedActivePlayerSlot)
            PlayerProfileCsvService.SyncDeckSummaryFromRuntime();

        if (ValuablesVaultState.HasPendingChanges)
            SavePlayerData();
        ValuablesVaultState.InvalidateAllCaches();
    }

    private bool TryApplyLoadedPlayerDataRows(string[] dataRow)
    {
        try
        {
            activePlayerSlot = Mathf.Clamp(ReadActiveSlotFromRows(dataRow), 1, MaxPlayerSlots);
            cachedOtherSlotRows.Clear();
            PlayerBirdDuelCdState.ClearAllCaches();
            bool hasDeckSlotData = false;
            bool hasSlotRows = false;

            foreach (var row in dataRow)
            {
                string[] rowArray = row.Split(',');
                if (rowArray == null || rowArray.Length == 0) continue;
                if (rowArray[0] == "#") continue;

                if (rowArray[0] == "slot")
                {
                    hasSlotRows = true;
                    if (rowArray.Length < 4) continue;
                    if (!int.TryParse(rowArray[1].Trim(), out int slotIndex)) continue;
                    if (slotIndex != activePlayerSlot)
                    {
                        string cachedRow = PlayerDeckSlotNameStorage.SanitizePersistedCsvRow(row);
                        if (cachedRow != null)
                            cachedOtherSlotRows.Add(cachedRow);
                        continue;
                    }
                    string[] scoped = new string[rowArray.Length - 2];
                    Array.Copy(rowArray, 2, scoped, 0, scoped.Length);
                    ParsePlayerRow(scoped, ref hasDeckSlotData);
                    continue;
                }

                if (!hasSlotRows)
                    ParsePlayerRow(rowArray, ref hasDeckSlotData);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerData: parse failed for candidate save -> " + ex.Message);
            return false;
        }
    }

    private void ParsePlayerRow(string[] rowArray, ref bool hasDeckSlotData)
    {
        if (rowArray == null || rowArray.Length == 0) return;
        if (rowArray[0] == "coins")
        {
            if (rowArray.Length < 2) return;
            playerCoins = int.Parse(rowArray[1].Trim());
            totalCoins = playerCoins;
        }
        else if (rowArray[0] == "gems")
        {
            if (rowArray.Length < 2) return;
            playerGems = int.Parse(rowArray[1].Trim());
        }
        else if (rowArray[0] == "bird_cd")
        {
            PlayerBirdDuelCdState.ParseScopedRow(activePlayerSlot, rowArray);
        }
        else if (rowArray[0] == "card")
        {
            if (!TryParseCollectionRow(rowArray, out int key, out int num)) return;
            SetCollectionCount(key, num);
        }
        else if (rowArray[0] == "deck")
        {
            if (hasDeckSlotData) return;
            if (!TryParseDeckRow(rowArray, out int key, out int num)) return;
            SetDeckCount(0, key, num);
        }
        else if (rowArray[0] == "deckslot")
        {
            if (rowArray.Length < 4) return;
            int slot = int.Parse(rowArray[1].Trim());
            if (slot < 0 || slot >= deckSlotCount) return;

            if (rowArray.Length >= 5 && (rowArray[2] == "m" || rowArray[2] == "s"))
            {
                hasDeckSlotData = true;
                if (!TryParseTypedDeckslotRow(rowArray, out int key, out int num)) return;
                SetDeckCount(slot, key, num);
            }
            else
            {
                hasDeckSlotData = true;
                int legacyId = int.Parse(rowArray[2].Trim());
                int num = int.Parse(rowArray[3].Trim());
                int key = NormalizeLegacyUnifiedRowId(legacyId);
                SetDeckCount(slot, key, num);
            }
        }
        else if (rowArray[0] == "selected_deck_slot")
        {
            if (rowArray.Length < 2) return;
            selectedDeckSlot = Mathf.Clamp(int.Parse(rowArray[1].Trim()), 0, deckSlotCount - 1);
        }
        else if (rowArray[0] == "deck_slot_name")
        {
            ApplyDeckSlotNameLoadRow(rowArray);
        }
        else if (rowArray[0] == "slot_name")
        {
            if (rowArray.Length < 2) return;
            activePlayerSlotName = string.IsNullOrWhiteSpace(rowArray[1]) ? ("玩家" + activePlayerSlot) : rowArray[1].Trim();
        }
        else if (rowArray[0] == "proficiency")
        {
            if (rowArray.Length < 5) return;
            if (rowArray[1] != "m") return;
            if (!int.TryParse(rowArray[2].Trim(), out int monsterId)) return;
            if (!float.TryParse(rowArray[3].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float progressAny))
                return;
            if (!int.TryParse(rowArray[4].Trim(), out int winsNormal)) return;
            SetCardProficiencyWins(monsterId, progressAny, winsNormal);
        }
    }

    private int NormalizeLegacyUnifiedRowId(int legacyId)
    {
        if (CardStore != null && CardStore.GetCardById(legacyId) is MonsterCard)
            return legacyId;
        return DeckCardId.NormalizeLegacyUnifiedId(legacyId);
    }

    private bool TryParseCollectionRow(string[] rowArray, out int key, out int num)
    {
        key = 0;
        num = 0;
        if (rowArray.Length >= 4 && rowArray[1] == "m")
        {
            key = int.Parse(rowArray[2].Trim());
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 4 && rowArray[1] == "s")
        {
            int ord = int.Parse(rowArray[2].Trim());
            key = DeckCardId.SpellKeyFromOrdinal(ord);
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 3)
        {
            int legacyId = int.Parse(rowArray[1].Trim());
            num = int.Parse(rowArray[2].Trim());
            key = NormalizeLegacyUnifiedRowId(legacyId);
            return true;
        }
        return false;
    }

    private bool TryParseDeckRow(string[] rowArray, out int key, out int num)
    {
        key = 0;
        num = 0;
        if (rowArray.Length >= 4 && rowArray[1] == "m")
        {
            key = int.Parse(rowArray[2].Trim());
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 4 && rowArray[1] == "s")
        {
            int ord = int.Parse(rowArray[2].Trim());
            key = DeckCardId.SpellKeyFromOrdinal(ord);
            num = int.Parse(rowArray[3].Trim());
            return true;
        }
        if (rowArray.Length >= 3)
        {
            int legacyId = int.Parse(rowArray[1].Trim());
            num = int.Parse(rowArray[2].Trim());
            key = NormalizeLegacyUnifiedRowId(legacyId);
            return true;
        }
        return false;
    }

    private static bool TryParseTypedDeckslotRow(string[] rowArray, out int key, out int num)
    {
        key = 0;
        num = 0;
        if (rowArray[2] == "m")
        {
            key = int.Parse(rowArray[3].Trim());
            num = int.Parse(rowArray[4].Trim());
            return true;
        }
        if (rowArray[2] == "s")
        {
            int ord = int.Parse(rowArray[3].Trim());
            key = DeckCardId.SpellKeyFromOrdinal(ord);
            num = int.Parse(rowArray[4].Trim());
            return true;
        }
        return false;
    }
}

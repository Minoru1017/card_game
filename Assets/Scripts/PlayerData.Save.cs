using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class PlayerData
{
    public void SavePlayerDataDebounced()
    {
        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
        {
            canonical.SavePlayerDataDebounced();
            return;
        }

        PlayerSaveDebouncer.RequestDebouncedSave(this);
    }

    public void SavePlayerData()
    {
        PlayerSaveDebouncer.CancelPending();

        PlayerData canonical = ResolveCanonical();
        if (canonical != null && canonical != this)
        {
            canonical.SavePlayerData();
            return;
        }

        if (!saveHydratedFromDisk)
        {
            LoadPlayerData();
            if (!saveHydratedFromDisk)
            {
                Debug.LogWarning(
                    "PlayerData: SavePlayerData aborted — save not hydrated from disk; " +
                    "refusing to overwrite playerdata.csv with defaults.");
                return;
            }
        }

        EnsureMinimumDeckSlotCount();
        EnsureDeckSlotMaps();
        EnsureActivePlayerSlotSyncedFromDisk();

        string dir = Application.persistentDataPath;
        string path = GetPlayerDataPath();
        Directory.CreateDirectory(dir);

        var preservedActiveSlotExtraRows = new List<string>(64);
        if (PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] existingLines, out _))
        {
            for (int li = 0; li < existingLines.Length; li++)
            {
                string line = existingLines[li];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] cols = line.Split(',');
                if (cols.Length < 4) continue;
                if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(cols[1].Trim(), out int slotNum) || slotNum != activePlayerSlot) continue;
                string slotKey = cols[2].Trim();
                if (ValuablesVaultState.IsValuableCsvRow(line))
                    continue;
                if (ShouldPreserveActiveSlotRowOnPlayerSave(slotKey, cols.Length))
                    preservedActiveSlotExtraRows.Add(line);
            }
        }

        var datas = new List<string>();
        datas.Add($"active_slot,{activePlayerSlot}");

        for (int i = 0; i < cachedOtherSlotRows.Count; i++)
        {
            string row = PlayerDeckSlotNameStorage.SanitizePersistedCsvRow(cachedOtherSlotRows[i]);
            if (row == null) continue;
            if (ValuablesVaultState.IsValuableCsvRow(row))
                continue;
            datas.Add(row);
        }

        var current = new List<string>();
        current.Add($"slot,{activePlayerSlot},coins,{playerCoins}");
        current.Add($"slot,{activePlayerSlot},gems,{playerGems}");
        current.Add($"slot,{activePlayerSlot},selected_deck_slot,{selectedDeckSlot}");
        current.Add($"slot,{activePlayerSlot},slot_name,{SanitizeSlotName(activePlayerSlotName, activePlayerSlot)}");

        EnsureDeckSlotMaps();
        AppendDeckSlotNameSaveRows(current);

        foreach (var kv in playerCollection)
        {
            if (kv.Value == 0) continue;
            if (DeckCardId.IsSpellKey(kv.Key))
                current.Add($"slot,{activePlayerSlot},card,s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
            else
                current.Add($"slot,{activePlayerSlot},card,m,{kv.Key},{kv.Value}");
        }

        EnsureDeckSlotMaps();
        for (int slot = 0; slot < deckSlotMaps.Length; slot++)
        {
            foreach (var kv in deckSlotMaps[slot])
            {
                if (kv.Value == 0) continue;
                if (DeckCardId.IsSpellKey(kv.Key))
                    current.Add($"slot,{activePlayerSlot},deckslot,{slot},s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
                else
                    current.Add($"slot,{activePlayerSlot},deckslot,{slot},m,{kv.Key},{kv.Value}");
            }
        }

        foreach (var kv in deckSlotMaps[selectedDeckSlot])
        {
            if (kv.Value == 0) continue;
            if (DeckCardId.IsSpellKey(kv.Key))
                current.Add($"slot,{activePlayerSlot},deck,s,{DeckCardId.SpellOrdinalFromKey(kv.Key)},{kv.Value}");
            else
                current.Add($"slot,{activePlayerSlot},deck,m,{kv.Key},{kv.Value}");
        }

        foreach (var kv in cardProficiencyWins)
        {
            if (kv.Value.progressAny <= 0.001f && kv.Value.winsNormalDifficulty <= 0) continue;
            string progressText = kv.Value.progressAny.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            current.Add($"slot,{activePlayerSlot},proficiency,m,{kv.Key},{progressText},{kv.Value.winsNormalDifficulty}");
        }

        PlayerBirdDuelCdState.AppendSaveRows(activePlayerSlot, current);

        for (int i = 0; i < current.Count; i++) datas.Add(current[i]);
        EnsureAllSlotContainers(datas);
        for (int pi = 0; pi < preservedActiveSlotExtraRows.Count; pi++)
            datas.Add(preservedActiveSlotExtraRows[pi]);

        TutorialProgressState.EnsureGraduationFlagRowsInPlayerSave(datas, activePlayerSlot, playerCollection);
        ValuablesVaultState.AppendAllSlotsSerializedRows(datas);
        ValuablesVaultState.MarkPersisted();

        PlayerSaveCoordinator.WritePlayerDataCsv(datas);
        RebuildCachedOtherSlotRowsFromDisk(path);
        Debug.Log("Save path: " + path);
    }

    private static bool ShouldPreserveActiveSlotRowOnPlayerSave(string slotKey, int columnCount)
    {
        if (string.IsNullOrWhiteSpace(slotKey)) return false;
        if (slotKey.StartsWith("profile_", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(slotKey, "battle_record", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(slotKey, ValuablesVaultState.SaveKey, StringComparison.OrdinalIgnoreCase))
            return true;
        if (columnCount != 4) return false;

        switch (slotKey)
        {
            case "coins":
            case "gems":
            case "bird_cd":
            case "selected_deck_slot":
            case "slot_name":
            case "card":
            case "deck":
            case "deckslot":
            case "deck_slot_name":
            case "proficiency":
                return false;
            default:
                return true;
        }
    }

    private void RebuildCachedOtherSlotRowsFromDisk(string path)
    {
        cachedOtherSlotRows.Clear();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row) || row.StartsWith("#", StringComparison.Ordinal)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 4) continue;
            if (!string.Equals(cols[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slotIndex)) continue;
            if (slotIndex == activePlayerSlot) continue;
            string cachedRow = PlayerDeckSlotNameStorage.SanitizePersistedCsvRow(row);
            if (cachedRow != null)
                cachedOtherSlotRows.Add(cachedRow);
        }
    }

    private static int ReadActiveSlotFromRows(string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 2) continue;
            if (!string.Equals(c[0].Trim(), "active_slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(c[1].Trim(), out int slot)) continue;
            return Mathf.Clamp(slot, 1, MaxPlayerSlots);
        }
        return 1;
    }

    private static void EnsureAllSlotContainers(List<string> rows)
    {
        bool[] hasSlotCoins = new bool[MaxPlayerSlots + 1];
        bool[] hasSlotGems = new bool[MaxPlayerSlots + 1];
        bool[] hasSlotSelect = new bool[MaxPlayerSlots + 1];
        bool[] hasSlotName = new bool[MaxPlayerSlots + 1];
        for (int i = 0; i < rows.Count; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 4) continue;
            if (!string.Equals(c[0].Trim(), "slot", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(c[1].Trim(), out int slot)) continue;
            if (slot < 1 || slot > MaxPlayerSlots) continue;
            string key = c[2].Trim();
            if (key == "coins") hasSlotCoins[slot] = true;
            else if (key == "gems") hasSlotGems[slot] = true;
            else if (key == "selected_deck_slot") hasSlotSelect[slot] = true;
            else if (key == "slot_name") hasSlotName[slot] = true;
        }
        for (int slot = 1; slot <= MaxPlayerSlots; slot++)
        {
            if (!hasSlotCoins[slot]) rows.Add($"slot,{slot},coins,100");
            if (!hasSlotGems[slot]) rows.Add($"slot,{slot},gems,300");
            if (!hasSlotSelect[slot]) rows.Add($"slot,{slot},selected_deck_slot,0");
            if (!hasSlotName[slot]) rows.Add($"slot,{slot},slot_name,玩家{slot}");
        }
    }
}

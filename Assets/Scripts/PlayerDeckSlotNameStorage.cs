using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 3 玩家槽 × 5 牌組槽：顯示名稱讀寫、CSV、Buildbeck 改名、存檔污染清理、玩家資訊摘要。
/// </summary>
public static class PlayerDeckSlotNameStorage
{
    public const string CsvRowKey = "deck_slot_name";
    public const int PlayerSlotCount = PlayerData.MaxPlayerSlots;
    public const int DeckSlotsPerPlayer = PlayerData.MinDeckSlotCount;

    // ── 預設名稱（僅 UI 顯示，不寫入 CSV）────────────────────────────────

    public static string FormatDefaultDisplayName(int deckSlotIndex0Based)
    {
        deckSlotIndex0Based = Mathf.Clamp(deckSlotIndex0Based, 0, DeckSlotsPerPlayer - 1);
        return "牌組" + (deckSlotIndex0Based + 1);
    }

    public static string SanitizeCustomName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        string n = name.Trim().Replace("\n", " ").Replace("\r", " ").Replace(",", " ");
        if (n.Length > 24) n = n.Substring(0, 24);
        return n;
    }

    // ── 記憶體讀寫（目前作用中玩家槽的 5 個牌組名）────────────────────────

    public static void ClearForLoad(PlayerData pd)
    {
        if (pd == null) return;
        pd.SetDeckSlotDisplayNamesInternal(null);
    }

    public static string GetRawName(PlayerData pd, int deckSlotIndex0Based)
    {
        if (pd == null) return string.Empty;
        EnsureBuffer(pd);
        deckSlotIndex0Based = ClampDeckSlot(pd, deckSlotIndex0Based);
        string[] buf = pd.GetDeckSlotDisplayNamesInternal();
        string s = buf[deckSlotIndex0Based];
        return string.IsNullOrEmpty(s) ? string.Empty : s;
    }

    /// <summary>UI 用：空 stored 值時回傳「牌組N」。</summary>
    public static string GetDisplayName(PlayerData pd, int deckSlotIndex0Based)
    {
        string raw = GetRawName(pd, deckSlotIndex0Based);
        if (!string.IsNullOrWhiteSpace(raw)) return raw;
        return FormatDefaultDisplayName(deckSlotIndex0Based);
    }

    public static void SetCustomName(PlayerData pd, int deckSlotIndex0Based, string name)
    {
        if (pd == null) return;

        PlayerData canonical = PlayerData.ResolveCanonical();
        if (canonical != null && canonical != pd)
        {
            SetCustomName(canonical, deckSlotIndex0Based, name);
            return;
        }

        pd.EnsureMinimumDeckSlotCount();
        pd.EnsureDeckSlotMapsInternal();
        EnsureBuffer(pd);
        deckSlotIndex0Based = ClampDeckSlot(pd, deckSlotIndex0Based);
        string[] buf = pd.GetDeckSlotDisplayNamesInternal();
        buf[deckSlotIndex0Based] = SanitizeCustomName(name);
    }

    /// <summary>流程 3：解散牌組時還原為預設（清空 stored 名稱）。</summary>
    public static void ResetDeckSlotNameToDefault(PlayerData pd, int deckSlotIndex0Based)
    {
        SetCustomName(pd, deckSlotIndex0Based, string.Empty);
    }

    public static void ResetSelectedDeckSlotNameToDefault(PlayerData pd)
    {
        if (pd == null) return;
        ResetDeckSlotNameToDefault(pd, pd.selectedDeckSlot);
    }

    /// <summary>切換 active 玩家槽並自 CSV 重載（正規流程）。</summary>
    public static void SelectActivePlayerSlotAndReload(int playerSlot)
    {
        PlayerData.SelectActivePlayerSlot(playerSlot);
    }

    /// <summary>套用自訂名稱、存檔、同步 profile 摘要。</summary>
    public static string ConfirmRenameAndPersist(PlayerData pd, int deckSlotIndex0Based, string newNameText)
    {
        if (pd == null) return null;

        EnsureActivePlayerSlotSyncedFromDisk(pd);
        pd.EnsureMinimumDeckSlotCount();
        deckSlotIndex0Based = ClampDeckSlot(pd, deckSlotIndex0Based);
        SetCustomName(pd, deckSlotIndex0Based, newNameText ?? string.Empty);
        pd.SavePlayerData();
        return PlayerProfileCsvService.SyncDeckSummaryFromRuntime();
    }

    /// <summary>解散牌組後還原名稱並存檔。</summary>
    public static string DisbandSelectedDeckSlotNameAndPersist(PlayerData pd)
    {
        if (pd == null) return null;
        ResetSelectedDeckSlotNameToDefault(pd);
        pd.SavePlayerData();
        return PlayerProfileCsvService.SyncDeckSummaryFromRuntime();
    }

    public static bool IsLegacyPersistedDefaultName(int deckSlotIndex0Based, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        deckSlotIndex0Based = Mathf.Clamp(deckSlotIndex0Based, 0, DeckSlotsPerPlayer - 1);
        return string.Equals(raw.Trim(), FormatDefaultDisplayName(deckSlotIndex0Based), System.StringComparison.Ordinal);
    }

    /// <summary>CSV label 是否為已知污染（亂碼、截斷 key、占位符等）。</summary>
    public static bool IsPollutedDeckSlotNameRaw(int deckSlotIndex0Based, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        deckSlotIndex0Based = Mathf.Clamp(deckSlotIndex0Based, 0, DeckSlotsPerPlayer - 1);
        string t = raw.Trim();

        if (t == "-") return true;
        if (IsLegacyPersistedDefaultName(deckSlotIndex0Based, t)) return true;
        if (IsCorruptedDefaultDisplayName(deckSlotIndex0Based, t)) return true;
        if (t.IndexOf("deck_slot_na", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (t.IndexOf("deck_slot_name", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(
                t, @"slot\s*\d+\s*deck_slot", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return true;
        if (t.Length <= 12 && ContainsMojibakeMarkers(t) && !ContainsIntentionalNameCharacters(t))
            return true;
        return false;
    }

    public static bool IsPollutedProfileDecksSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return false;
        if (summary.IndexOf("deck_slot_na", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (summary.IndexOf("deck_slot_name", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(
                summary, @"slot\s*\d+\s*deck_slot", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return true;

        string[] parts = summary.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            int colon = part.LastIndexOf(':');
            if (colon <= 0) continue;
            string label = part.Substring(0, colon).Trim();
            int deckIdx = Mathf.Clamp(i, 0, DeckSlotsPerPlayer - 1);
            if (IsPollutedDeckSlotNameRaw(deckIdx, label)) return true;
        }
        return false;
    }

    /// <summary>存檔列若為污染 profile_decks 則略過（不寫回）。</summary>
    public static bool ShouldDropPersistedCsvRow(string row)
    {
        if (!TryParseProfileDecksCsvRow(row, out _, out string summary)) return false;
        return IsPollutedProfileDecksSummary(summary);
    }

    /// <summary>清理單一 CSV 列（deck_slot_name 正規化；污染 profile_decks 回傳 null）。</summary>
    public static string SanitizePersistedCsvRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row)) return row;
        if (ShouldDropPersistedCsvRow(row)) return null;
        return TrySanitizeDeckSlotNameCsvRow(row, out string sanitized) ? sanitized : row;
    }

    public readonly struct DeckSlotNameRepairReport
    {
        public readonly int DeckSlotNameRowsScanned;
        public readonly int DeckSlotNameRowsRepaired;
        public readonly int ProfileDecksRowsRemoved;
        public readonly bool RepairedActivePlayerSlot;
        public bool AnyChanges => DeckSlotNameRowsRepaired > 0 || ProfileDecksRowsRemoved > 0;

        public DeckSlotNameRepairReport(int scanned, int repaired, int profileRemoved, bool repairedActive)
        {
            DeckSlotNameRowsScanned = scanned;
            DeckSlotNameRowsRepaired = repaired;
            ProfileDecksRowsRemoved = profileRemoved;
            RepairedActivePlayerSlot = repairedActive;
        }
    }

    /// <summary>掃描 playerdata.csv，清除 deck_slot_name / profile_decks 污染列並寫回。</summary>
    public static DeckSlotNameRepairReport RepairPersistedDeckSlotNamePollution(bool syncActiveSlotProfile = false)
    {
        string path = PlayerData.GetPlayerSaveCsvPath();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return default;

        int activeSlot = ReadActiveSlotFromCsv(rows);
        var output = new List<string>(rows.Length);
        int scanned = 0;
        int repaired = 0;
        int profileRemoved = 0;
        bool repairedActive = false;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row))
            {
                output.Add(row);
                continue;
            }

            if (TryParseDeckSlotNameCsvRow(row, out int playerSlot, out int deckIdx, out string rawLabel))
            {
                scanned++;
                deckIdx = Mathf.Clamp(deckIdx, 0, DeckSlotsPerPlayer - 1);
                string cleaned = NormalizeLoadedRawName(deckIdx, rawLabel);
                string newRow = $"slot,{playerSlot},{CsvRowKey},{deckIdx},{cleaned}";
                if (!string.Equals(newRow, row, System.StringComparison.Ordinal))
                {
                    repaired++;
                    if (playerSlot == activeSlot) repairedActive = true;
                }
                output.Add(newRow);
                continue;
            }

            if (TryParseProfileDecksCsvRow(row, out playerSlot, out string profileSummary))
            {
                if (IsPollutedProfileDecksSummary(profileSummary))
                {
                    profileRemoved++;
                    if (playerSlot == activeSlot) repairedActive = true;
                    continue;
                }
            }

            output.Add(row);
        }

        var report = new DeckSlotNameRepairReport(scanned, repaired, profileRemoved, repairedActive);
        if (!report.AnyChanges) return report;

        PlayerSaveCoordinator.WritePlayerDataCsv(output);
        ValuablesVaultState.InvalidateAllCaches();
        Debug.Log(
            "[PlayerDeckSlotNameStorage] Repaired save pollution: deck_slot_name=" + repaired +
            " profile_decks_removed=" + profileRemoved);

        if (syncActiveSlotProfile && repairedActive)
            PlayerProfileCsvService.SyncDeckSummaryFromRuntime();

        return report;
    }

    /// <summary>載入前自動清理；有變更時寫回 CSV。</summary>
    public static DeckSlotNameRepairReport RepairPersistedDeckSlotNamePollutionIfNeeded() =>
        RepairPersistedDeckSlotNamePollution(syncActiveSlotProfile: false);

    private static bool IsCorruptedDefaultDisplayName(int deckSlotIndex0Based, string trimmed)
    {
        int n = deckSlotIndex0Based + 1;
        string digit = n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!trimmed.EndsWith(digit, System.StringComparison.Ordinal)) return false;
        if (trimmed.Length > 12) return false;

        string prefix = trimmed.Substring(0, trimmed.Length - digit.Length);
        if (prefix.Length == 0) return false;
        if (ContainsIntentionalNameCharacters(prefix)) return false;
        return ContainsMojibakeMarkers(prefix);
    }

    public static bool TrySanitizeDeckSlotNameCsvRow(string row, out string sanitizedRow)
    {
        sanitizedRow = row;
        if (!TryParseDeckSlotNameCsvRow(row, out int playerSlot, out int deckIdx, out string rawLabel))
            return false;

        deckIdx = Mathf.Clamp(deckIdx, 0, DeckSlotsPerPlayer - 1);
        string cleaned = NormalizeLoadedRawName(deckIdx, rawLabel);
        sanitizedRow = $"slot,{playerSlot},{CsvRowKey},{deckIdx},{cleaned}";
        return !string.Equals(sanitizedRow, row, System.StringComparison.Ordinal);
    }

    private static bool TryParseDeckSlotNameCsvRow(
        string row, out int playerSlot, out int deckIdx, out string rawLabel)
    {
        playerSlot = 0;
        deckIdx = 0;
        rawLabel = string.Empty;
        if (string.IsNullOrWhiteSpace(row)) return false;

        string[] cols = row.Split(',');
        if (cols.Length < 5) return false;
        if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (!int.TryParse(cols[1].Trim(), out playerSlot)) return false;
        if (!IsDeckSlotNameCsvKey(cols[2].Trim())) return false;
        if (!int.TryParse(cols[3].Trim(), out deckIdx)) return false;

        rawLabel = cols[4];
        if (cols.Length > 5)
        {
            for (int ri = 5; ri < cols.Length; ri++)
                rawLabel = rawLabel + "," + cols[ri];
        }
        return true;
    }

    private static bool TryParseProfileDecksCsvRow(string row, out int playerSlot, out string summary)
    {
        playerSlot = 0;
        summary = string.Empty;
        if (string.IsNullOrWhiteSpace(row)) return false;

        string[] cols = row.Split(',');
        if (cols.Length < 4) return false;
        if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (!int.TryParse(cols[1].Trim(), out playerSlot)) return false;
        if (!string.Equals(cols[2].Trim(), "profile_decks", System.StringComparison.OrdinalIgnoreCase)) return false;
        summary = string.Join(",", cols, 3, cols.Length - 3);
        return true;
    }

    private static bool ContainsMojibakeMarkers(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '?' || c == '\uFFFD' || char.IsControl(c)) return true;
            if (c >= '\uE000' && c <= '\uF8FF') return true;
        }
        return false;
    }

    private static bool ContainsIntentionalNameCharacters(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= '\u4E00' && c <= '\u9FFF') return true;
            if (c >= '\u3400' && c <= '\u4DBF') return true;
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= '\u3040' && c <= '\u30FF') return true;
            if (c >= '\uAC00' && c <= '\uD7AF') return true;
        }
        return false;
    }

    // ── CSV 載入 / 存檔 ────────────────────────────────────────────────

    /// <summary>Parse scoped row: <c>deck_slot_name,{deckIndex},{label...}</c></summary>
    public static void ApplyLoadRow(PlayerData pd, string[] scopedRowArray)
    {
        if (pd == null || scopedRowArray == null || scopedRowArray.Length < 3) return;
        if (!string.Equals(scopedRowArray[0], CsvRowKey, System.StringComparison.OrdinalIgnoreCase)) return;
        if (!int.TryParse(scopedRowArray[1].Trim(), out int deckSlotIdx)) return;

        pd.EnsureDeckSlotMapsInternal();
        EnsureBuffer(pd);
        deckSlotIdx = ClampDeckSlot(pd, deckSlotIdx);

        string rawLabel = scopedRowArray[2];
        if (scopedRowArray.Length > 3)
        {
            for (int ri = 3; ri < scopedRowArray.Length; ri++)
                rawLabel = rawLabel + "," + scopedRowArray[ri];
        }

        string[] buf = pd.GetDeckSlotDisplayNamesInternal();
        buf[deckSlotIdx] = NormalizeLoadedRawName(deckSlotIdx, rawLabel);
    }

    private static string NormalizeLoadedRawName(int deckSlotIndex0Based, string raw)
    {
        if (IsPollutedDeckSlotNameRaw(deckSlotIndex0Based, raw))
            return string.Empty;
        string sanitized = SanitizeCustomName(raw);
        if (IsLegacyPersistedDefaultName(deckSlotIndex0Based, sanitized))
            return string.Empty;
        return sanitized;
    }

    /// <summary>
    /// Append <c>slot,{activePlayerSlot},deck_slot_name,{0..4},{rawOrEmpty}</c> rows.
    /// Uses raw stored values only — never UI fallback defaults.
    /// </summary>
    public static void AppendSaveRows(PlayerData pd, List<string> saveRows)
    {
        if (pd == null || saveRows == null) return;
        EnsureBuffer(pd);
        int playerSlot = pd.activePlayerSlot;
        for (int s = 0; s < pd.deckSlotCount; s++)
        {
            string label = GetRawName(pd, s);
            saveRows.Add($"slot,{playerSlot},{CsvRowKey},{s},{label}");
        }
    }

    public static bool IsDeckSlotNameCsvKey(string slotKey) =>
        string.Equals(slotKey, CsvRowKey, System.StringComparison.OrdinalIgnoreCase);

    // ── 玩家槽同步（避免 A 槽名稱寫入 B 槽）──────────────────────────────

    public static void EnsureActivePlayerSlotSyncedFromDisk(PlayerData pd)
    {
        if (pd == null || !pd.IsSaveHydratedInternal()) return;

        string path = PlayerData.GetPlayerSaveCsvPath();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return;

        int diskActive = Mathf.Clamp(ReadActiveSlotFromCsv(rows), 1, PlayerSlotCount);
        if (diskActive == pd.activePlayerSlot) return;

        Debug.LogWarning(
            "[PlayerDeckSlotNameStorage] activePlayerSlot memory=" + pd.activePlayerSlot +
            " disk=" + diskActive + "; reloading before deck-name save/sync.");
        pd.LoadPlayerData();
    }

    /// <summary>Bug 重現用：只更新 CSV active_slot，不 LoadPlayerData。</summary>
    public static void DebugWriteActivePlayerSlotCsvOnly(int slot)
    {
        slot = Mathf.Clamp(slot, 1, PlayerSlotCount);
        PlayerSaveCoordinator.EnsurePersistedBeforeDiskMerge();
        string path = PlayerData.GetPlayerSaveCsvPath();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] existing, out _))
            existing = System.Array.Empty<string>();

        var rows = new List<string>(existing.Length + 2);
        bool activeWritten = false;
        for (int i = 0; i < existing.Length; i++)
        {
            string row = existing[i];
            if (string.IsNullOrWhiteSpace(row))
            {
                rows.Add(row);
                continue;
            }

            string[] c = row.Split(',');
            if (c.Length > 0 && string.Equals(c[0].Trim(), "active_slot", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!activeWritten)
                {
                    rows.Add("active_slot," + slot);
                    activeWritten = true;
                }
                continue;
            }
            rows.Add(row);
        }

        if (!activeWritten) rows.Insert(0, "active_slot," + slot);
        PlayerSaveCoordinator.WritePlayerDataCsv(rows);
        ValuablesVaultState.InvalidateAllCaches();
    }

    // ── 除錯：逐步驗證用 ────────────────────────────────────────────────

    public static string DescribeInMemory(PlayerData pd)
    {
        if (pd == null) return "(null PlayerData)";
        var sb = new StringBuilder(128);
        sb.Append("playerSlot=").Append(pd.activePlayerSlot).Append(" names=[");
        for (int i = 0; i < DeckSlotsPerPlayer; i++)
        {
            if (i > 0) sb.Append(" | ");
            string raw = GetRawName(pd, i);
            sb.Append(i).Append(':');
            sb.Append(string.IsNullOrEmpty(raw) ? FormatDefaultDisplayName(i) + "(default)" : raw);
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>從磁碟 CSV 讀取指定玩家槽的 raw 名稱（Bug 場景檢視非 active 槽用）。</summary>
    public static string GetDiskRawName(int playerSlot, int deckSlotIndex0Based)
    {
        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerSlotCount);
        deckSlotIndex0Based = Mathf.Clamp(deckSlotIndex0Based, 0, DeckSlotsPerPlayer - 1);
        string path = PlayerData.GetPlayerSaveCsvPath();
        if (!PlayerPersistSafeIO.TryReadPlayerDataLines(path, out string[] rows, out _))
            return string.Empty;

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i];
            if (string.IsNullOrWhiteSpace(row)) continue;
            string[] cols = row.Split(',');
            if (cols.Length < 5) continue;
            if (!string.Equals(cols[0].Trim(), "slot", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(cols[1].Trim(), out int slot) || slot != playerSlot) continue;
            if (!IsDeckSlotNameCsvKey(cols[2].Trim())) continue;
            if (!int.TryParse(cols[3].Trim(), out int deckIdx) || deckIdx != deckSlotIndex0Based) continue;

            string rawLabel = cols[4];
            if (cols.Length > 5)
            {
                for (int ri = 5; ri < cols.Length; ri++)
                    rawLabel = rawLabel + "," + cols[ri];
            }
            return NormalizeLoadedRawName(deckSlotIndex0Based, rawLabel);
        }
        return string.Empty;
    }

    public static string GetDiskDisplayName(int playerSlot, int deckSlotIndex0Based)
    {
        string raw = GetDiskRawName(playerSlot, deckSlotIndex0Based);
        if (!string.IsNullOrWhiteSpace(raw)) return raw;
        return FormatDefaultDisplayName(deckSlotIndex0Based);
    }

    public static string DescribeDiskRowsForPlayerSlot(int playerSlot)
    {
        playerSlot = Mathf.Clamp(playerSlot, 1, PlayerSlotCount);
        if (!System.IO.File.Exists(PlayerData.GetPlayerSaveCsvPath()))
            return "playerSlot=" + playerSlot + " (no save file)";

        var sb = new StringBuilder(128);
        sb.Append("playerSlot=").Append(playerSlot).Append(" csv=[");
        for (int s = 0; s < DeckSlotsPerPlayer; s++)
        {
            if (s > 0) sb.Append(" | ");
            string raw = GetDiskRawName(playerSlot, s);
            sb.Append(s).Append(':');
            sb.Append(string.IsNullOrEmpty(raw) ? FormatDefaultDisplayName(s) + "(default)" : raw);
        }
        sb.Append(']');
        return sb.ToString();
    }

    // ── Buildbeck 自訂改名流程 ──────────────────────────────────────────

    public static void ConfirmBuildbeckRename(DeckManager deckManager, string newNameText)
    {
        if (deckManager == null) return;

        deckManager.EnsureCoreRefsForInspect();
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd == null)
        {
            deckManager.HideDeckNameEditPanel();
            return;
        }

        pd.EnsureMinimumDeckSlotCount();
        int nameSlot = Mathf.Clamp(pd.selectedDeckSlot, 0, pd.deckSlotCount - 1);

        Debug.Log("[PlayerDeckSlotNameStorage] ConfirmBuildbeckRename before: " + DescribeInMemory(pd));

        string deckSummary = ConfirmRenameAndPersist(pd, nameSlot, newNameText);
        deckManager.FinishDeckNameEditUi(deckSummary);

        Debug.Log("[PlayerDeckSlotNameStorage] ConfirmBuildbeckRename after save: " + DescribeInMemory(pd));
        Debug.Log("[PlayerDeckSlotNameStorage] disk: " + DescribeDiskRowsForPlayerSlot(pd.activePlayerSlot));
    }

    public static string ReadBuildbeckEditDialogCurrentName(PlayerData pd)
    {
        if (pd == null) return FormatDefaultDisplayName(0);
        return GetDisplayName(pd, pd.selectedDeckSlot);
    }

    // ── 玩家資訊 / profile 摘要 ─────────────────────────────────────────

    public static class ProfileBridge
    {
        public static string BuildDeckSummaryLine(PlayerData playerData)
        {
            if (playerData == null || playerData.deckSlotCount <= 0) return "尚無牌組資料";
            var parts = new List<string>(playerData.deckSlotCount);
            for (int slot = 0; slot < playerData.deckSlotCount; slot++)
            {
                int total = 0;
                IReadOnlyDictionary<int, int> map = playerData.GetDeckMap(slot);
                foreach (KeyValuePair<int, int> kv in map)
                {
                    if (kv.Value > 0) total += kv.Value;
                }
                string deckLabel = GetDisplayName(playerData, slot);
                parts.Add(deckLabel + ":" + total + "張");
            }
            return parts.Count > 0 ? string.Join(" | ", parts) : "尚無牌組資料";
        }

        /// <summary>Rewrite active player slot <c>deck_slot_name</c> rows during profile mirror merge.</summary>
        public static void RewriteActiveSlotNameRows(
            PlayerData runtimePd,
            List<string> merged,
            System.Func<string, string> escapeCsv)
        {
            if (runtimePd == null || merged == null) return;

            int playerSlot = Mathf.Clamp(runtimePd.activePlayerSlot, 1, PlayerSlotCount);
            int deckSlots = Mathf.Max(1, runtimePd.deckSlotCount);
            System.Func<string, string> esc = escapeCsv ?? (s => s ?? string.Empty);

            for (int s = 0; s < deckSlots; s++)
            {
                string label = GetRawName(runtimePd, s);
                merged.Add($"slot,{playerSlot},{CsvRowKey},{s},{esc(label)}");
            }
        }
    }

    // ── private helpers ─────────────────────────────────────────────────

    private static void EnsureBuffer(PlayerData pd)
    {
        pd.EnsureDeckSlotMapsInternal();
        string[] buf = pd.GetDeckSlotDisplayNamesInternal();
        if (buf != null && buf.Length == pd.deckSlotCount) return;

        var prev = buf;
        buf = new string[pd.deckSlotCount];
        if (prev != null)
        {
            int copy = Mathf.Min(prev.Length, pd.deckSlotCount);
            for (int i = 0; i < copy; i++)
                buf[i] = prev[i];
        }
        pd.SetDeckSlotDisplayNamesInternal(buf);
    }

    private static int ClampDeckSlot(PlayerData pd, int deckSlotIndex0Based) =>
        Mathf.Clamp(deckSlotIndex0Based, 0, pd.deckSlotCount - 1);

    private static int ReadActiveSlotFromCsv(string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 2) continue;
            if (!string.Equals(c[0].Trim(), "active_slot", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(c[1].Trim(), out int slot)) continue;
            return slot;
        }
        return 1;
    }
}

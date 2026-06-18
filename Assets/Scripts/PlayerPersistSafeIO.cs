using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Atomic-style writes, rotating backups, and read fallback for local player CSV saves.
/// 一律使用 UTF-8（含 BOM）讀寫，避免 Windows 預設編碼導致牌組名稱等中文亂碼。
/// </summary>
public static class PlayerPersistSafeIO
{
    public const int BackupTierCount = 3;
    public const string TempSuffix = ".tmp";

    /// <summary>playerdata / profile CSV 固定 UTF-8 BOM。</summary>
    public static readonly Encoding SaveFileEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public static IEnumerable<string> EnumerateLoadCandidates(string primaryPath)
    {
        yield return primaryPath;
        for (int i = 1; i <= BackupTierCount; i++)
            yield return GetBackupPath(primaryPath, i);
    }

    public static string GetBackupPath(string primaryPath, int backupIndex)
    {
        if (backupIndex < 1 || backupIndex > BackupTierCount)
            throw new ArgumentOutOfRangeException(nameof(backupIndex));
        return primaryPath + ".bak" + backupIndex;
    }

    public static bool ExistsAnyWithBackups(string primaryPath)
    {
        if (File.Exists(primaryPath)) return true;
        for (int i = 1; i <= BackupTierCount; i++)
        {
            if (File.Exists(GetBackupPath(primaryPath, i))) return true;
        }
        return false;
    }

    public static bool LooksLikePlayerDataCsv(string[] lines)
    {
        if (lines == null || lines.Length == 0) return false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            string[] c = line.Split(',');
            if (c.Length < 2) continue;
            string k = c[0].Trim();
            if (string.Equals(k, "active_slot", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(k, "slot", StringComparison.OrdinalIgnoreCase) && c.Length >= 4)
                return true;
            if (string.Equals(k, "coins", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static bool LooksLikePlayerProfileCsv(string[] lines)
    {
        if (lines == null || lines.Length == 0) return false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] c = line.Split(',');
            if (c.Length < 2) continue;
            if (string.Equals(c[0].Trim(), "uuid", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static bool TryReadPlayerDataLines(string primaryPath, out string[] lines, out string resolvedPath)
    {
        foreach (string p in EnumerateLoadCandidates(primaryPath))
        {
            if (!File.Exists(p)) continue;
            try
            {
                lines = ReadAllLines(p);
                if (LooksLikePlayerDataCsv(lines))
                {
                    resolvedPath = p;
                    if (p != primaryPath)
                        Debug.LogWarning("PlayerPersistSafeIO: using fallback save file -> " + p);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("PlayerPersistSafeIO: read failed for " + p + " -> " + ex.Message);
            }
        }
        lines = null;
        resolvedPath = null;
        return false;
    }

    public static bool TryReadProfileLines(string primaryPath, out string[] lines, out string resolvedPath)
    {
        foreach (string p in EnumerateLoadCandidates(primaryPath))
        {
            if (!File.Exists(p)) continue;
            try
            {
                lines = ReadAllLines(p);
                if (LooksLikePlayerProfileCsv(lines))
                {
                    resolvedPath = p;
                    if (p != primaryPath)
                        Debug.LogWarning("PlayerPersistSafeIO: using fallback profile file -> " + p);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("PlayerPersistSafeIO: read profile failed for " + p + " -> " + ex.Message);
            }
        }
        lines = null;
        resolvedPath = null;
        return false;
    }

    /// <summary>
    /// 原子寫入與備份輪替。寫入 <c>playerdata.csv</c> 時請改經 <see cref="PlayerSaveCoordinator.WritePlayerDataCsv"/>。
    /// </summary>
    public static void WriteAllLinesWithAtomicRotateBackups(string primaryPath, IReadOnlyList<string> lines)
    {
        if (lines == null) throw new ArgumentNullException(nameof(lines));

        string dir = Path.GetDirectoryName(primaryPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        string tmp = primaryPath + TempSuffix;
        try
        {
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: could not clear stale temp -> " + ex.Message);
        }

        WriteAllLines(tmp, lines);
        RotateBackupsBeforeReplace(primaryPath);
        TryDeleteFile(primaryPath);
        File.Move(tmp, primaryPath);
    }

    public static string[] ReadAllLines(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return Array.Empty<string>();

        string[] utf8Lines = File.ReadAllLines(path, SaveFileEncoding);
        if (LooksLikePlayerDataCsv(utf8Lines) || LooksLikePlayerProfileCsv(utf8Lines))
            return utf8Lines;

        try
        {
            string[] legacyLines = File.ReadAllLines(path, Encoding.Default);
            if (LooksLikePlayerDataCsv(legacyLines) || LooksLikePlayerProfileCsv(legacyLines))
            {
                Debug.LogWarning(
                    "PlayerPersistSafeIO: loaded with legacy system encoding (will re-save as UTF-8) -> " + path);
                return legacyLines;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: legacy encoding fallback failed -> " + ex.Message);
        }

        return utf8Lines;
    }

    public static void WriteAllLines(string path, IReadOnlyList<string> lines) =>
        File.WriteAllLines(path, lines, SaveFileEncoding);

    private static void RotateBackupsBeforeReplace(string primaryPath)
    {
        if (!File.Exists(primaryPath)) return;

        string b1 = GetBackupPath(primaryPath, 1);
        string b2 = GetBackupPath(primaryPath, 2);
        string b3 = GetBackupPath(primaryPath, 3);

        try
        {
            if (File.Exists(b3))
                File.Delete(b3);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: delete oldest backup failed -> " + ex.Message);
        }

        try
        {
            if (File.Exists(b2))
                File.Move(b2, b3);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: rotate backup 2->3 failed -> " + ex.Message);
        }

        try
        {
            if (File.Exists(b1))
                File.Move(b1, b2);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: rotate backup 1->2 failed -> " + ex.Message);
        }

        try
        {
            File.Copy(primaryPath, b1, true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: snapshot to .bak1 failed -> " + ex.Message);
        }
    }

    private static void TryDeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PlayerPersistSafeIO: delete before replace failed -> " + ex.Message);
            throw;
        }
    }
}

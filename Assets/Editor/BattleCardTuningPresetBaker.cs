#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 將 BattleSimulation 場景內 BattleSimulationManager 的調校寫入 Resources JSON 預設。
/// </summary>
public static class BattleCardTuningPresetBaker
{
    private const string JsonAssetPath = "Assets/Resources/BattleCardTuningPresets.json";

    [MenuItem("Tools/Battle/Card Tuning/Capture Preset 1 (預設一) from Open Scene")]
    public static void CapturePreset1FromOpenScene()
    {
        BattleSimulationManager manager = UnityEngine.Object.FindObjectOfType<BattleSimulationManager>();
        if (manager == null)
        {
            Debug.LogError("BattleCardTuningPresetBaker: 目前場景找不到 BattleSimulationManager。");
            return;
        }

        BattleCardTuningPresetCatalog catalog = LoadCatalogFromDisk();
        BattleCardTuningPresetEntry entry = FindOrCreatePreset(catalog, BattleCardTuningPresetLibrary.Preset1Id);
        entry.displayName = BattleCardTuningPresetLibrary.Preset1DisplayName;
        BattleCardTuningPresetLibrary.CaptureFromManager(entry, manager);
        WriteCatalogToDisk(catalog);
        BattleCardTuningPresetLibrary.InvalidateCache();
        AssetDatabase.Refresh();
        Debug.Log($"BattleCardTuningPresetBaker: 已將目前場景調校寫入「{BattleCardTuningPresetLibrary.Preset1DisplayName}」→ {JsonAssetPath}");
    }

    [MenuItem("Tools/Battle/Card Tuning/Write Control Group (對照組) Max Values to JSON")]
    public static void WriteControlGroupMaxValuesToJson()
    {
        BattleCardTuningPresetCatalog catalog = LoadCatalogFromDisk();
        BattleCardTuningPresetEntry entry = FindOrCreatePreset(catalog, BattleCardTuningPresetLibrary.ControlGroupId);
        BattleCardTuningPresetEntry maxEntry = BattleCardTuningPresetLibrary.CreateControlGroupMaxEntry();
        entry.displayName = maxEntry.displayName;
        BattleCardTuningCopy.Copy(entry.layout, maxEntry.layout);
        BattleCardTuningCopy.Copy(entry.text, maxEntry.text);
        BattleCardTuningCopy.Copy(entry.field, maxEntry.field);
        WriteCatalogToDisk(catalog);
        BattleCardTuningPresetLibrary.InvalidateCache();
        AssetDatabase.Refresh();
        Debug.Log($"BattleCardTuningPresetBaker: 已寫入「{BattleCardTuningPresetLibrary.ControlGroupDisplayName}」（全滑桿上限）→ {JsonAssetPath}");
    }

    [MenuItem("Tools/Battle/Card Tuning/Apply Preset 1 (預設一) to Open Scene")]
    public static void ApplyPreset1ToOpenScene()
    {
        BattleSimulationManager manager = UnityEngine.Object.FindObjectOfType<BattleSimulationManager>();
        if (manager == null)
        {
            Debug.LogError("BattleCardTuningPresetBaker: 目前場景找不到 BattleSimulationManager。");
            return;
        }

        BattleCardTuningPresetCatalog catalog = LoadCatalogFromDisk();
        if (!TryFindPreset(catalog, BattleCardTuningPresetLibrary.Preset1Id, out BattleCardTuningPresetEntry entry))
        {
            Debug.LogError("BattleCardTuningPresetBaker: JSON 中找不到 preset1。");
            return;
        }

        BattleCardTuningPresetLibrary.ApplyEntry(manager, entry);
        EditorUtility.SetDirty(manager);
        Debug.Log($"BattleCardTuningPresetBaker: 已將「{entry.displayName}」套用到場景中的 BattleSimulationManager。");
    }

    [MenuItem("Tools/Battle/Card Tuning/Apply Control Group (對照組) to Open Scene")]
    public static void ApplyControlGroupToOpenScene()
    {
        BattleSimulationManager manager = UnityEngine.Object.FindObjectOfType<BattleSimulationManager>();
        if (manager == null)
        {
            Debug.LogError("BattleCardTuningPresetBaker: 目前場景找不到 BattleSimulationManager。");
            return;
        }

        BattleCardTuningPresetCatalog catalog = LoadCatalogFromDisk();
        if (!TryFindPreset(catalog, BattleCardTuningPresetLibrary.ControlGroupId, out BattleCardTuningPresetEntry entry))
        {
            Debug.LogError("BattleCardTuningPresetBaker: JSON 中找不到 control_group。");
            return;
        }

        BattleCardTuningPresetLibrary.ApplyEntry(manager, entry);
        EditorUtility.SetDirty(manager);
        Debug.Log($"BattleCardTuningPresetBaker: 已將「{entry.displayName}」套用到場景中的 BattleSimulationManager。");
    }

    private static BattleCardTuningPresetCatalog LoadCatalogFromDisk()
    {
        if (!File.Exists(JsonAssetPath))
            return new BattleCardTuningPresetCatalog();

        string json = File.ReadAllText(JsonAssetPath);
        if (string.IsNullOrWhiteSpace(json))
            return new BattleCardTuningPresetCatalog();

        try
        {
            return JsonUtility.FromJson<BattleCardTuningPresetCatalog>(json)
                   ?? new BattleCardTuningPresetCatalog();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BattleCardTuningPresetBaker: 讀取 JSON 失敗，將建立新檔。{ex.Message}");
            return new BattleCardTuningPresetCatalog();
        }
    }

    private static void WriteCatalogToDisk(BattleCardTuningPresetCatalog catalog)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        string json = JsonUtility.ToJson(catalog, true);
        File.WriteAllText(JsonAssetPath, json);
    }

    private static BattleCardTuningPresetEntry FindOrCreatePreset(
        BattleCardTuningPresetCatalog catalog, string presetId)
    {
        if (TryFindPreset(catalog, presetId, out BattleCardTuningPresetEntry existing))
            return existing;

        int oldLen = catalog.presets?.Length ?? 0;
        var next = new BattleCardTuningPresetEntry[oldLen + 1];
        if (oldLen > 0)
            Array.Copy(catalog.presets, next, oldLen);

        var created = new BattleCardTuningPresetEntry { presetId = presetId };
        next[oldLen] = created;
        catalog.presets = next;
        return created;
    }

    private static bool TryFindPreset(
        BattleCardTuningPresetCatalog catalog, string presetId, out BattleCardTuningPresetEntry entry)
    {
        entry = null;
        if (catalog?.presets == null || string.IsNullOrWhiteSpace(presetId)) return false;

        for (int i = 0; i < catalog.presets.Length; i++)
        {
            BattleCardTuningPresetEntry candidate = catalog.presets[i];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.presetId)) continue;
            if (!string.Equals(candidate.presetId, presetId, StringComparison.OrdinalIgnoreCase)) continue;
            entry = candidate;
            return true;
        }

        return false;
    }
}
#endif

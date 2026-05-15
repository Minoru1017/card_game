using System;
using UnityEngine;

/// <summary>
/// 單一戰鬥卡牌調校預設（手牌版面、手牌文字、場上卡牌）。
/// </summary>
[Serializable]
public class BattleCardTuningPresetEntry
{
    public string presetId;
    public string displayName;
    public BattleCardLayoutTuning layout = new BattleCardLayoutTuning();
    public BattleCardTextTuning text = new BattleCardTextTuning();
    public BattleFieldCardTuning field = new BattleFieldCardTuning();
}

[Serializable]
public class BattleCardTuningPresetCatalog
{
    public BattleCardTuningPresetEntry[] presets = Array.Empty<BattleCardTuningPresetEntry>();
}

/// <summary>
/// 以 JsonUtility 複製可序列化調校物件（欄位對欄位）。
/// </summary>
public static class BattleCardTuningCopy
{
    public static void Copy<T>(T destination, T source) where T : class
    {
        if (destination == null || source == null) return;
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), destination);
    }
}

/// <summary>
/// 從 <c>Resources/BattleCardTuningPresets.json</c> 載入預設，供設定與對戰場景套用。
/// </summary>
public static class BattleCardTuningPresetLibrary
{
    public const string ResourcePath = "BattleCardTuningPresets";
    public const string Preset1Id = "preset1";
    public const string Preset1DisplayName = "預設一";
    public const string ControlGroupId = "control_group";
    public const string ControlGroupDisplayName = "對照組";

    private static BattleCardTuningPresetCatalog cachedCatalog;

    /// <summary>各滑桿上限值，供與「預設一」對照驗證 API 是否生效。</summary>
    public static BattleCardTuningPresetEntry CreateControlGroupMaxEntry()
    {
        return new BattleCardTuningPresetEntry
        {
            presetId = ControlGroupId,
            displayName = ControlGroupDisplayName,
            layout = new BattleCardLayoutTuning
            {
                handCardSizeMultiplier = BattleCardLayoutTuning.HandCardSizeMultiplierMax,
                handCardSpacing = 80f,
                handAreaAnchoredYCanPlay = 200f,
                handAreaAnchoredYCantPlay = 200f,
                enemyHandAreaAnchoredYCanPlay = 200f,
                enemyHandAreaAnchoredYCantPlay = 200f
            },
            text = new BattleCardTextTuning
            {
                handCardTextScale = 2.5f,
                handCardNameScale = 2.5f,
                handCardBackplateScale = 2f
            },
            field = new BattleFieldCardTuning
            {
                fieldMonsterCardSizeMultiplier = BattleFieldCardTuning.FieldCardSizeMultiplierMax,
                fieldSpellCardSizeMultiplier = BattleFieldCardTuning.FieldCardSizeMultiplierMax,
                enemyFieldSizeMultiplier = 1.5f,
                fieldAreaOffsetY = 200f,
                playerMonsterFieldX = -600f,
                enemyMonsterFieldX = 600f,
                monsterSpellSpacingX = 400f,
                fieldAttackHealthTextScale = 2.5f,
                fieldSpellTextScale = 2.5f
            }
        };
    }

    public static bool TryGetControlGroup(out BattleCardTuningPresetEntry entry) =>
        TryGetPreset(ControlGroupId, out entry);

    public static BattleCardTuningPresetCatalog LoadCatalog(bool forceReload = false)
    {
        if (!forceReload && cachedCatalog != null) return cachedCatalog;

        TextAsset json = Resources.Load<TextAsset>(ResourcePath);
        if (json == null || string.IsNullOrWhiteSpace(json.text))
        {
            cachedCatalog = new BattleCardTuningPresetCatalog();
            return cachedCatalog;
        }

        try
        {
            cachedCatalog = JsonUtility.FromJson<BattleCardTuningPresetCatalog>(json.text)
                            ?? new BattleCardTuningPresetCatalog();
        }
        catch
        {
            cachedCatalog = new BattleCardTuningPresetCatalog();
        }

        return cachedCatalog;
    }

    public static bool TryGetPreset(string presetId, out BattleCardTuningPresetEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(presetId)) return false;

        BattleCardTuningPresetCatalog catalog = LoadCatalog();
        if (catalog.presets == null) return false;

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

    public static bool TryGetPreset1(out BattleCardTuningPresetEntry entry) =>
        TryGetPreset(Preset1Id, out entry);

    public static void ApplyEntry(BattleSimulationManager manager, BattleCardTuningPresetEntry entry)
    {
        if (manager == null || entry == null) return;
        manager.ApplyCardTuning(entry.layout, entry.text, entry.field);
    }

    public static bool TryApplyPreset(BattleSimulationManager manager, string presetId)
    {
        if (!TryGetPreset(presetId, out BattleCardTuningPresetEntry entry)) return false;
        ApplyEntry(manager, entry);
        return true;
    }

    public static void CaptureFromManager(BattleCardTuningPresetEntry entry, BattleSimulationManager manager)
    {
        if (entry == null || manager == null) return;
        if (manager.CardLayout != null) BattleCardTuningCopy.Copy(entry.layout, manager.CardLayout);
        if (manager.CardText != null) BattleCardTuningCopy.Copy(entry.text, manager.CardText);
        if (manager.CardField != null) BattleCardTuningCopy.Copy(entry.field, manager.CardField);
    }

#if UNITY_EDITOR
    public static void InvalidateCache() => cachedCatalog = null;
#endif
}

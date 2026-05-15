using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>將戰鬥卡牌調校預設格式化成 Settings Parameter details 對照表。</summary>
public static class BattleCardTuningPresetDisplay
{
    /// <summary>字型探測用，涵蓋對照表常用字。</summary>
    public const string CjkFontProbe =
        "參數對照類別預設一對照組手牌版面文字場上怪獸法術大小倍率間距槽位攻血整體卡名底圖敵方我方像素";

    private const string UnitMultiplier = "倍";
    private const string UnitPixel = "px";

    public static string BuildComparisonTitle() => "參數對照";

    public static string BuildComparisonSubtitle() =>
        "預設一與對照組\n手牌版面 手牌文字 場上";

    public static string BuildTitle(BattleCardTuningPresetEntry entry) => BuildComparisonTitle();

    public static string BuildSubtitle(BattleCardTuningPresetEntry entry) => BuildComparisonSubtitle();

    public static string BuildAppliedToastMessage(BattleCardTuningPresetEntry entry)
    {
        string name = entry != null && !string.IsNullOrWhiteSpace(entry.displayName)
            ? entry.displayName
            : "預設";
        return "已套用" + name;
    }

    public static IReadOnlyList<ParameterComparisonRow> GetComparisonRows()
    {
        if (!TryGetComparisonPresets(out BattleCardTuningPresetEntry preset1, out BattleCardTuningPresetEntry control))
            return new[] { new ParameterComparisonRow("", "無預設資料", "", "", false) };

        var rows = new List<ParameterComparisonRow>(16)
        {
            new ParameterComparisonRow(
                "類別", "參數",
                BattleCardTuningPresetLibrary.Preset1DisplayName,
                BattleCardTuningPresetLibrary.ControlGroupDisplayName,
                true)
        };

        rows.Add(new ParameterComparisonRow("手牌版面", "大小倍率",
            FmtMult(preset1.layout?.handCardSizeMultiplier),
            FmtMult(control.layout?.handCardSizeMultiplier)));
        rows.Add(new ParameterComparisonRow("", "間距",
            FmtPx(preset1.layout?.handCardSpacing),
            FmtPx(control.layout?.handCardSpacing)));
        rows.Add(new ParameterComparisonRow("", "我方/敵方 Y",
            FormatHandAreaY(preset1.layout),
            FormatHandAreaY(control.layout)));

        rows.Add(new ParameterComparisonRow("手牌文字", "整體/卡名/底圖",
            FormatTextTriple(preset1.text),
            FormatTextTriple(control.text)));

        rows.Add(new ParameterComparisonRow("場上", "怪獸/法術大小",
            FmtMult(preset1.field?.fieldMonsterCardSizeMultiplier),
            FmtMult(control.field?.fieldMonsterCardSizeMultiplier)));
        rows.Add(new ParameterComparisonRow("", "敵方倍率",
            FmtMult(preset1.field?.enemyFieldSizeMultiplier),
            FmtMult(control.field?.enemyFieldSizeMultiplier)));
        rows.Add(new ParameterComparisonRow("", "槽位 Y / 間距 X",
            FormatFieldSlot(preset1.field),
            FormatFieldSlot(control.field)));
        rows.Add(new ParameterComparisonRow("", "我方/敵方 X",
            FormatFieldX(preset1.field),
            FormatFieldX(control.field)));
        rows.Add(new ParameterComparisonRow("", "攻血/法術字",
            FmtMult(preset1.field?.fieldAttackHealthTextScale),
            FmtMult(control.field?.fieldAttackHealthTextScale)));

        return rows;
    }

    public static string BuildComparisonTable()
    {
        IReadOnlyList<ParameterComparisonRow> rows = GetComparisonRows();
        var sb = new StringBuilder(512);
        const char tab = '\t';
        for (int i = 0; i < rows.Count; i++)
        {
            ParameterComparisonRow row = rows[i];
            sb.Append(row.category).Append(tab).Append(row.parameter).Append(tab)
                .Append(row.preset1).Append(tab).Append(row.control).AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildBody(BattleCardTuningPresetEntry entry) => BuildComparisonTable();

    private static bool TryGetComparisonPresets(
        out BattleCardTuningPresetEntry preset1,
        out BattleCardTuningPresetEntry control)
    {
        bool ok1 = BattleCardTuningPresetLibrary.TryGetPreset1(out preset1);
        bool ok2 = BattleCardTuningPresetLibrary.TryGetControlGroup(out control);
        return ok1 && ok2 && preset1 != null && control != null;
    }

    private static string FormatHandAreaY(BattleCardLayoutTuning layout)
    {
        if (layout == null) return "";

        float player = layout.handAreaAnchoredYCanPlay;
        float enemy = layout.enemyHandAreaAnchoredYCanPlay;
        if (Mathf.Approximately(Mathf.Abs(player), Mathf.Abs(enemy)))
            return "±" + FmtPx(Mathf.Abs(player));

        return FmtPxPair(player, enemy);
    }

    private static string FormatTextTriple(BattleCardTextTuning text)
    {
        if (text == null) return "";
        return FmtMultTriple(text.handCardTextScale, text.handCardNameScale, text.handCardBackplateScale);
    }

    private static string FormatFieldSlot(BattleFieldCardTuning field)
    {
        if (field == null) return "";
        return FmtPxPair(field.fieldAreaOffsetY, field.monsterSpellSpacingX);
    }

    private static string FormatFieldX(BattleFieldCardTuning field)
    {
        if (field == null) return "";
        return FmtPxPair(field.playerMonsterFieldX, field.enemyMonsterFieldX);
    }

    private static string FmtMultTriple(float a, float b, float c) =>
        $"{FmtMult(a)} / {FmtMult(b)} / {FmtMult(c)}";

    private static string FmtPxPair(float a, float b) => $"{FmtPx(a)} / {FmtPx(b)}";

    private static string FmtMult(float? value) =>
        value.HasValue ? Fmt(value.Value) + UnitMultiplier : "";

    private static string FmtMult(float value) => Fmt(value) + UnitMultiplier;

    private static string FmtPx(float? value) =>
        value.HasValue ? FmtPx(value.Value) : "";

    private static string FmtPx(float value) => Fmt(value) + UnitPixel;

    private static string Fmt(float value) => value.ToString("0.##");
}

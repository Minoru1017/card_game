using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>對戰歷史事件類型（結構化紀錄用）。</summary>
public enum BattleHistoryKind
{
    General,
    Opening,
    Combat,
    Spell,
    Monster,
    Hero,
    Weather,
    Discard,
    Skill,
    Outcome
}

/// <summary>單則對戰歷史事件。</summary>
public readonly struct BattleHistoryEntry
{
    public readonly int SequenceId;
    public readonly int Round;
    public readonly BattleHistoryKind Kind;
    public readonly string Text;
    public readonly bool IsPlayerPerspective;

    public BattleHistoryEntry(
        int sequenceId,
        int round,
        BattleHistoryKind kind,
        string text,
        bool isPlayerPerspective)
    {
        SequenceId = sequenceId;
        Round = round;
        Kind = kind;
        Text = text ?? string.Empty;
        IsPlayerPerspective = isPlayerPerspective;
    }
}

/// <summary>對戰歷史推斷與戰報摘要。</summary>
public static class BattleHistoryReport
{
    public static BattleHistoryKind InferKind(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return BattleHistoryKind.General;

        string t = message.Trim();
        if (t == "對戰開始" || t.StartsWith("我方骰", StringComparison.Ordinal) ||
            t == "我方先手" || t == "敵方先手")
            return BattleHistoryKind.Opening;
        if (t == "對戰結束" || t == "我方勝利" || t == "我方戰敗" || t == "平手")
            return BattleHistoryKind.Outcome;
        if (t.StartsWith("天氣預報:", StringComparison.Ordinal) ||
            t.StartsWith("天氣結算", StringComparison.Ordinal))
            return BattleHistoryKind.Weather;
        if (t.Contains("棄牌"))
            return BattleHistoryKind.Discard;
        if (t.Contains("法術牌"))
            return BattleHistoryKind.Spell;
        if (t.Contains("怪物牌") || t.Contains("怪獸"))
            return BattleHistoryKind.Monster;
        if (t.Contains("我方英雄") || t.Contains("敵方英雄"))
            return BattleHistoryKind.Hero;
        if (t.Contains("列陣") || t.Contains("技能"))
            return BattleHistoryKind.Skill;
        if (t.Contains("點傷害") || t.Contains("反擊") || t.Contains("造成"))
            return BattleHistoryKind.Combat;
        return BattleHistoryKind.General;
    }

    public static bool InferIsPlayerPerspective(string message)
    {
        if (string.IsNullOrEmpty(message)) return false;
        return message.Contains("我方");
    }

    public static string BuildSummary(
        IReadOnlyList<BattleHistoryEntry> entries,
        int battleResult,
        int finalRound,
        int maxLines = 5)
    {
        if (entries == null || entries.Count == 0)
            return "（尚無對戰紀錄）";

        var lines = new List<string>(maxLines);
        string outcome = battleResult switch
        {
            1 => "勝利",
            -1 => "戰敗",
            2 => "平手",
            _ => "進行中"
        };
        lines.Add("結果：" + outcome + "　共 " + Mathf.Max(1, finalRound) + " 回合");

        int heroHits = 0;
        int totalHeroDamage = 0;
        string lastCombat = null;
        string lastWeather = null;
        string lastSkill = null;

        for (int i = 0; i < entries.Count; i++)
        {
            BattleHistoryEntry e = entries[i];
            string t = e.Text;
            if (e.Kind == BattleHistoryKind.Hero && t.Contains("我方英雄受到"))
            {
                heroHits++;
                int dmg = ExtractFirstDamageValue(t);
                if (dmg > 0) totalHeroDamage += dmg;
            }
            if (e.Kind == BattleHistoryKind.Combat || t.Contains("點傷害"))
                lastCombat = t;
            if (e.Kind == BattleHistoryKind.Weather)
                lastWeather = t;
            if (e.Kind == BattleHistoryKind.Skill)
                lastSkill = t;
        }

        if (heroHits > 0)
            lines.Add("我方英雄共受到 " + totalHeroDamage + " 點傷害（" + heroHits + " 次）");
        if (!string.IsNullOrEmpty(lastCombat))
            lines.Add(TrimForSummary(lastCombat, 42));
        if (!string.IsNullOrEmpty(lastWeather))
            lines.Add(TrimForSummary(lastWeather, 42));
        else if (!string.IsNullOrEmpty(lastSkill))
            lines.Add(TrimForSummary(lastSkill, 42));

        while (lines.Count > maxLines)
            lines.RemoveAt(lines.Count - 1);

        return string.Join("\n", lines);
    }

    private static int ExtractFirstDamageValue(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var sb = new StringBuilder();
        bool inNumber = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsDigit(c))
            {
                sb.Append(c);
                inNumber = true;
            }
            else if (inNumber)
                break;
        }

        return sb.Length > 0 && int.TryParse(sb.ToString(), out int v) ? v : 0;
    }

    private static string TrimForSummary(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string t = text.Trim();
        if (t.Length <= maxChars) return t;
        return t.Substring(0, maxChars - 1) + "…";
    }
}

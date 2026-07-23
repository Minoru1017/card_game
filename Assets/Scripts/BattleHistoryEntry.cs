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

    /// <summary>分析本局勝負關鍵（供 DevAutomation 戰報匯出）。</summary>
    public static string BuildOutcomeAnalysis(
        IReadOnlyList<BattleHistoryEntry> entries,
        int battleResult,
        int finalRound,
        int playerHeroHp,
        int enemyHeroHp,
        string outcomeReason,
        bool surrendered)
    {
        var sb = new StringBuilder(512);
        string outcomeLabel = battleResult switch
        {
            1 => "我方勝利",
            -1 => "我方戰敗",
            2 => "平手",
            _ => "進行中／未知"
        };
        sb.Append("結果：").Append(outcomeLabel).Append("（code=").Append(battleResult).Append("）\n");
        sb.Append("回合數：").Append(Mathf.Max(1, finalRound)).Append('\n');
        sb.Append("終局 HP：我方英雄 ").Append(playerHeroHp).Append("／敵方英雄 ").Append(enemyHeroHp).Append('\n');

        if (surrendered)
            sb.Append("勝負點：玩家放棄對戰（ForcePlayerSurrender）\n");
        else if (!string.IsNullOrWhiteSpace(outcomeReason))
            sb.Append("勝負點：").Append(outcomeReason.Trim()).Append('\n');
        else
            sb.Append("勝負點：").Append(InferOutcomeReasonFromState(battleResult, playerHeroHp, enemyHeroHp)).Append('\n');

        AppendKeyMoments(sb, entries, battleResult);
        return sb.ToString().TrimEnd();
    }

    private static string InferOutcomeReasonFromState(int battleResult, int playerHeroHp, int enemyHeroHp)
    {
        return battleResult switch
        {
            1 when enemyHeroHp <= 0 => "敵方英雄 HP≤0",
            1 => "敵方符合戰敗條件（英雄 HP≤0 或手牌＋場上無牌）",
            -1 when playerHeroHp <= 0 => "我方英雄 HP≤0",
            -1 => "我方符合戰敗條件（英雄 HP≤0 或手牌＋場上無牌）",
            2 => "雙方皆未分出勝負或同歸於盡",
            _ => "（無詳細判斷）"
        };
    }

    private static void AppendKeyMoments(StringBuilder sb, IReadOnlyList<BattleHistoryEntry> entries, int battleResult)
    {
        if (entries == null || entries.Count == 0)
        {
            sb.Append("關鍵事件：（本局無歷史紀錄）");
            return;
        }

        sb.Append("關鍵事件：\n");
        int listed = 0;
        const int maxList = 8;
        for (int i = 0; i < entries.Count && listed < maxList; i++)
        {
            string t = entries[i].Text;
            if (string.IsNullOrWhiteSpace(t)) continue;
            if (!IsKeyMomentLine(t)) continue;
            sb.Append("- [R").Append(entries[i].Round).Append("] ").Append(t.Trim()).Append('\n');
            listed++;
        }

        if (listed == 0)
        {
            int take = Mathf.Min(5, entries.Count);
            for (int i = entries.Count - take; i < entries.Count; i++)
            {
                sb.Append("- [R").Append(entries[i].Round).Append("] ").Append(entries[i].Text.Trim()).Append('\n');
            }
        }
    }

    private static bool IsKeyMomentLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.Contains("對戰結束") ||
               text.Contains("我方勝利") ||
               text.Contains("我方戰敗") ||
               text.Contains("平手") ||
               text.Contains("我方英雄死亡") ||
               text.Contains("我方英雄受到") ||
               text.Contains("對敵方英雄造成") ||
               text.Contains("對我方英雄造成") ||
               text.Contains("教學戰限時") ||
               text.Contains("港灣") ||
               text.Contains("段考") ||
               text.Contains("限時") ||
               text.Contains("判定獲勝") ||
               text.Contains("判定我方獲勝");
    }

    /// <summary>完整對戰紀錄（含回合標記），供匯出檔案。</summary>
    public static string BuildFullExportText(
        IReadOnlyList<BattleHistoryEntry> entries,
        int battleResult,
        int finalRound,
        int playerHeroHp,
        int enemyHeroHp,
        string outcomeReason,
        bool surrendered,
        string sceneName)
    {
        var sb = new StringBuilder(Mathf.Max(256, (entries?.Count ?? 0) * 48));
        sb.Append("# DevAutomation 對戰紀錄\n\n");
        sb.Append("- 場景：").Append(string.IsNullOrEmpty(sceneName) ? "?" : sceneName).Append('\n');
        sb.Append("- 匯出時間：").Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\n\n");
        sb.Append("## 勝負分析\n\n");
        sb.Append(BuildOutcomeAnalysis(entries, battleResult, finalRound, playerHeroHp, enemyHeroHp, outcomeReason, surrendered));
        sb.Append("\n\n## 完整歷程\n\n");
        if (entries == null || entries.Count == 0)
        {
            sb.Append("（本局尚無對戰歷史紀錄）\n");
            return sb.ToString();
        }

        int lastRound = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            BattleHistoryEntry e = entries[i];
            if (e.Round != lastRound)
            {
                if (lastRound >= 0) sb.Append('\n');
                sb.Append("### 第 ").Append(e.Round).Append(" 回合\n\n");
                lastRound = e.Round;
            }
            sb.Append("- ").Append(e.Text).Append('\n');
        }
        return sb.ToString();
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

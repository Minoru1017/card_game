using System;
using UnityEngine;

/// <summary>可玩 v1：代表卡 B 階戰技文案與結算（熟練度 UI／存檔尚未實作，場上卡一律視為 B 生效）。</summary>
public static class MonsterSkillRegistry
{
    /// <summary>戰技文案數值／機制重點色（TMP／UGUI Rich Text）。</summary>
    public const string HighlightNumericHex = "#F8D878";
    /// <summary>戰技文案關鍵詞重點色。</summary>
    public const string HighlightKeywordHex = "#9AD4FF";

    /// <summary>B 階一行摘要是否顯示於卡面／懸停（暫時關閉；長按仍顯示完整戰技介紹）。</summary>
    public static bool ShowSkillLineBInUi;

    public static bool TryGetSkillLineB(int monsterId, out string line)
    {
        if (!TryGetSkillLineBPlain(monsterId, out line)) return false;
        line = ToRichSkillLineB(monsterId, line);
        return true;
    }

    public static bool TryGetSkillLineBPlain(int monsterId, out string line)
    {
        if (!ShowSkillLineBInUi)
        {
            line = null;
            return false;
        }
        if (!TryGetSkillEntry(monsterId, out SkillEntry entry))
        {
            line = null;
            return false;
        }
        line = entry.lineB;
        return true;
    }

    /// <summary>對戰手牌長按浮窗（含顏色標示）。</summary>
    public static bool TryGetBattleHandLongPressTooltip(int monsterId, out string message)
    {
        if (!TryGetBattleHandLongPressModel(monsterId, out HandLongPressTooltipModel model))
        {
            message = null;
            return false;
        }
        message = model.heading + "\n" + model.subtitleRich + "\n" + model.bodyRich;
        return true;
    }

    public static bool TryGetBattleHandLongPressModel(int monsterId, out HandLongPressTooltipModel model)
    {
        if (!TryGetBackpackSkillIntro(monsterId, out string skillName, out string description))
        {
            model = default;
            return false;
        }
        model = new HandLongPressTooltipModel
        {
            heading = "戰技介紹",
            subtitleRich = FormatSkillNameRich(skillName),
            bodyRich = description
        };
        return model.HasContent;
    }

    public static bool HasSkillTrack(int monsterId) => TryGetSkillEntry(monsterId, out _);

    public static bool TryGetSkillName(int monsterId, out string skillName)
    {
        if (!TryGetSkillEntry(monsterId, out SkillEntry entry))
        {
            skillName = null;
            return false;
        }
        skillName = entry.skillName;
        return !string.IsNullOrWhiteSpace(skillName);
    }

    public static string GetLockedStagePlaceholder(CardSkillRevealStage stage)
    {
        switch (stage)
        {
            case CardSkillRevealStage.BasicB:
                return "🔒 達成<color=#9AD4FF>基礎熟練度</color>後，將顯示戰技一行摘要。";
            case CardSkillRevealStage.FullC:
                return "🔒 達成<color=#9AD4FF>進階熟練度</color>後，可閱讀完整戰技條文（時機、對象、疊加順序）。";
            default:
                return "🔒 <color=#F8D878>熟練後解鎖戰技</color>\n與此牌對戰並納入牌組後將逐步揭露戰技內容";
        }
    }

    /// <summary>背包詳情：依階段回傳已解放文案（Rich Text）。</summary>
    public static bool TryGetSkillStageBodyRich(int monsterId, CardSkillRevealStage stage, out string bodyRich)
    {
        bodyRich = null;
        if (!TryGetSkillEntry(monsterId, out SkillEntry entry))
            return false;

        switch (stage)
        {
            case CardSkillRevealStage.LockedA:
                bodyRich = "🔒 <color=#F8D878>熟練後解鎖戰技</color>\n" + entry.lineAFuzzy;
                return true;
            case CardSkillRevealStage.BasicB:
                if (string.IsNullOrWhiteSpace(entry.lineB))
                    return false;
                bodyRich = ToRichSkillLineB(monsterId, entry.lineB.Trim());
                return true;
            case CardSkillRevealStage.FullC:
                if (string.IsNullOrWhiteSpace(entry.backpackIntro))
                    return false;
                bodyRich = ToRichSkillIntro(monsterId, entry.backpackIntro);
                return true;
            default:
                return false;
        }
    }

    /// <summary>背包檢視浮窗：戰技名稱與完整介紹（對齊可玩 v1 結算）。</summary>
    public static bool TryGetBackpackSkillIntro(int monsterId, out string skillName, out string description)
    {
        if (!TryGetSkillEntry(monsterId, out SkillEntry entry))
        {
            skillName = null;
            description = null;
            return false;
        }
        skillName = entry.skillName;
        description = ToRichSkillIntro(monsterId, entry.backpackIntro);
        return true;
    }

    public static string FormatSkillNameRich(string skillName) =>
        string.IsNullOrEmpty(skillName) ? skillName : WrapColor(HighlightKeywordHex, skillName);

    public static string ToRichSkillIntro(int monsterId, string plainIntro)
    {
        if (string.IsNullOrEmpty(plainIntro)) return plainIntro;
        switch (monsterId)
        {
            case MonsterSkillIds.King:
                return HighlightPhrases(plainIntro,
                    new[] { "減傷5點", "最少1點", "最多3次" },
                    new[] { "次數共用", "訓練薄霧效果時" });
            case MonsterSkillIds.Queen:
                return HighlightPhrases(plainIntro,
                    new[] { "減傷3點", "最少1點", "僅1次" },
                    new[] { "首次受到傷害時", "不再觸發" });
            case MonsterSkillIds.Militia:
                return HighlightPhrases(plainIntro,
                    new[] { "攻擊力+5點", "僅1次" },
                    new[] { "首次置於場上時", "離場或對局結束" });
            default:
                return plainIntro;
        }
    }

    public static string ToRichSkillLineB(int monsterId, string plainLineB)
    {
        if (string.IsNullOrEmpty(plainLineB)) return plainLineB;
        switch (monsterId)
        {
            case MonsterSkillIds.King:
                return HighlightPhrases(plainLineB,
                    new[] { "在場減5", "最少1", "全場3次" },
                    new[] { "打英雄共用", "薄霧先場後技" });
            case MonsterSkillIds.Queen:
                return HighlightPhrases(plainLineB,
                    new[] { "首次減3", "最少1", "全場1次" },
                    new[] { "不再觸發" });
            case MonsterSkillIds.Militia:
                return HighlightPhrases(plainLineB,
                    new[] { "攻擊+5", "全場1次" },
                    new[] { "留至離場" });
            default:
                return plainLineB;
        }
    }

    private static string HighlightPhrases(string text, string[] numericPhrases, string[] keywordPhrases)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string result = text;
        result = ApplyPhraseColor(result, numericPhrases, HighlightNumericHex);
        result = ApplyPhraseColor(result, keywordPhrases, HighlightKeywordHex);
        return result;
    }

    private static string ApplyPhraseColor(string text, string[] phrases, string colorHex)
    {
        if (string.IsNullOrEmpty(text) || phrases == null || phrases.Length == 0) return text;
        string result = text;
        for (int i = 0; i < phrases.Length; i++)
        {
            string phrase = phrases[i];
            if (string.IsNullOrEmpty(phrase) || result.IndexOf(phrase, StringComparison.Ordinal) < 0) continue;
            result = result.Replace(phrase, WrapColor(colorHex, phrase));
        }
        return result;
    }

    private static string WrapColor(string hex, string inner) => "<color=" + hex + ">" + inner + "</color>";

    private readonly struct SkillEntry
    {
        public readonly string skillName;
        public readonly string lineAFuzzy;
        public readonly string lineB;
        public readonly string backpackIntro;

        public SkillEntry(string skillName, string lineAFuzzy, string lineB, string backpackIntro)
        {
            this.skillName = skillName;
            this.lineAFuzzy = lineAFuzzy;
            this.lineB = lineB;
            this.backpackIntro = backpackIntro;
        }
    }

    private static bool TryGetSkillEntry(int monsterId, out SkillEntry entry)
    {
        switch (monsterId)
        {
            case MonsterSkillIds.Militia:
                entry = new SkillEntry(
                    "列陣",
                    "據說能在列隊上場時短暫提振陣腳",
                    "列陣 首次置場 攻擊+5 全場1次 留至離場",
                    "首次置於場上時 民兵攻擊力+5點 對戰內僅1次 加成留至該民兵離場或對局結束");
                return true;
            case MonsterSkillIds.Queen:
                entry = new SkillEntry(
                    "王室庇護",
                    "據說能在危急時為王室撐起第一道護盾",
                    "王室庇護 首次減3 最少1 全場1次 之後不再觸發",
                    "首次受到傷害時 王后減傷3點 最少1點 對戰內僅1次 其後不再觸發");
                return true;
            case MonsterSkillIds.King:
                entry = new SkillEntry(
                    "庭訓號令",
                    "據說能在訓練廳裡號令隊形保護己方導師",
                    "庭訓號令 在場減5 最少1 全場3次 無場怪打英雄共用 薄霧先場後技",
                    "在場時 國王減傷5點 最少1點 對戰內最多3次 我方無場怪時 敵方直擊我方英雄或對英雄施放火球時 次數共用 若場地為訓練薄霧效果時 先套用場地效果 再套用此戰技效果");
                return true;
            default:
                entry = default;
                return false;
        }
    }

    /// <summary>國王·庭訓號令：減傷後至少 1；每局最多觸發 maxCharges 次。</summary>
    public static int ApplyTrainingCourtDecree(ref int chargesRemaining, int incomingDamage, Action<string> logHistory)
    {
        if (chargesRemaining <= 0 || incomingDamage <= 0)
            return incomingDamage;
        int reduced = Mathf.Max(1, incomingDamage - 5);
        if (reduced >= incomingDamage)
            return incomingDamage;
        chargesRemaining--;
        logHistory?.Invoke("庭訓號令：這次傷害少 5 點 本局還可觸發 " + chargesRemaining + " 次");
        return reduced;
    }

    /// <summary>王后·王室庇護：每局首次受到傷害時 −3（減後至少 1）。</summary>
    public static int ApplyQueenShelter(ref bool firstHitConsumed, int incomingDamage, Action<string> logHistory)
    {
        if (firstHitConsumed || incomingDamage <= 0)
            return incomingDamage;
        int reduced = Mathf.Max(1, incomingDamage - 3);
        if (reduced >= incomingDamage)
            return incomingDamage;
        firstHitConsumed = true;
        logHistory?.Invoke("王室庇護：這次傷害少 3 點 本局不再觸發");
        return reduced;
    }
}

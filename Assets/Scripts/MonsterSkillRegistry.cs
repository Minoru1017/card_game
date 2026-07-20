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
                return "達成<color=#9AD4FF>基礎熟練度</color>後，將顯示戰技一行摘要。";
            case CardSkillRevealStage.FullC:
                return "達成<color=#9AD4FF>進階熟練度</color>後，可閱讀完整戰技條文（時機、對象、疊加順序）。";
            default:
                return "<color=#F8D878>熟練後解鎖戰技</color>\n與此牌對戰並納入牌組後將逐步揭露戰技內容";
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
                bodyRich = "<color=#F8D878>熟練後解鎖戰技</color>\n" + entry.lineAFuzzy;
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
            case MonsterSkillIds.Nun:
                return HighlightPhrases(plainIntro,
                    new[] { "溢出轉補英雄", "英雄+10點", "英雄+12點", "僅1次" },
                    new[] { "初級治療", "修女在場", "無溢出時", "聖療連攜" });
            case MonsterSkillIds.Bishop:
                return HighlightPhrases(plainIntro,
                    new[] { "首傷減3點", "宗教減4點", "治療+5點", "最少1點", "僅1次" },
                    new[] { "祝聖預留", "宗教連攜", "聖療連攜", "下隻場怪" });
            case MonsterSkillIds.Castle:
                return HighlightPhrases(plainIntro,
                    new[] { "首次減5點", "最少1點", "僅1次" },
                    new[] { "堅城駐守", "城堡在場", "祝聖不疊加" });
            case MonsterSkillIds.SanctumKnight:
                return HighlightPhrases(plainIntro,
                    new[] { "僅1次" },
                    new[] { "首上場", "0攻友軍", "敵本回合禁直擊", "火球直擊" });
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
            case MonsterSkillIds.Nun:
                return HighlightPhrases(plainLineB,
                    new[] { "溢出轉英雄", "無溢+10", "連攜+12", "全場1次" },
                    new[] { "修女在場", "首次治療" });
            case MonsterSkillIds.Bishop:
                return HighlightPhrases(plainLineB,
                    new[] { "首傷減3", "宗教減4", "全場1次" },
                    new[] { "祝聖預留", "置場", "下隻場怪" });
            case MonsterSkillIds.Castle:
                return HighlightPhrases(plainLineB,
                    new[] { "首次減5", "最少1", "全場1次" },
                    new[] { "堅城駐守", "城堡在場" });
            case MonsterSkillIds.SanctumKnight:
                return HighlightPhrases(plainLineB,
                    new[] { "全場1次" },
                    new[] { "首上場", "0攻友軍", "敵本回合禁直擊" });
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
            case MonsterSkillIds.Nun:
                entry = new SkillEntry(
                    "聖療共鳴",
                    "據說能在聖光治療時將多餘生命力分送給導師",
                    "聖療共鳴 修女在場 首次治療 溢出轉英雄 無溢+10 連攜+12 全場1次",
                    "場上為修女時 本局首次對其結算初級治療後 若修女HP超過上限 溢出轉補英雄 並將修女HP設為上限 若無溢出 英雄+10點 若已觸發主教聖療連攜則改+12點 對戰內僅1次");
                return true;
            case MonsterSkillIds.Bishop:
                entry = new SkillEntry(
                    "祝聖預留",
                    "據說能在聖壇前為下一位上場者預先祝聖；對同袍效果更佳",
                    "祝聖預留 首次置場 下隻首傷減3 宗教連攜減4 全場1次",
                    "本局首次將主教置於場上時 獲得祝聖 可選擇綁定主教本人或下一張打出的場怪 綁定後該怪首次受到傷害時減3點最少1點 若為宗教派系則改減4點 若為修女則本局該修女第一次初級治療多5點 僅觸發1次");
                return true;
            case MonsterSkillIds.Castle:
                entry = new SkillEntry(
                    "堅城駐守",
                    "據說能在海牆前像城堡一樣擋下第一波衝擊",
                    "堅城駐守 城堡在場 首次受傷減5 最少1 全場1次",
                    "場上為城堡時 本局首次對其結算傷害減5點最少1點 對戰內僅1次 若該次已觸發主教祝聖預留減傷則不疊加");
                return true;
            case MonsterSkillIds.SanctumKnight:
                entry = new SkillEntry(
                    "護聖",
                    "據說與無法廝殺的同伴同列時，會暫時斷絕敵人直取導師的路",
                    "護聖 首上場有0攻友軍 敵本回合禁直擊 全場1次",
                    "本局首次將聖院騎士置入場上時 若替換下場的友軍攻擊力為0 則敵方本回合無法以場怪直擊我方英雄 亦無法以火球術直擊我方英雄 對戰內僅1次 與國王庭訓號令減傷可並存但本效果為完全阻擋直擊");
                return true;
            default:
                entry = default;
                return false;
        }
    }

    /// <summary>國王·庭訓號令：減傷後至少 1；每局最多觸發 maxCharges 次。</summary>
    public static int ApplyTrainingCourtDecree(
        ref int chargesRemaining,
        int incomingDamage,
        Action<string> logHistory,
        bool isPlayerSide)
    {
        if (chargesRemaining <= 0 || incomingDamage <= 0)
            return incomingDamage;
        int reduced = Mathf.Max(1, incomingDamage - 5);
        if (reduced >= incomingDamage)
            return incomingDamage;
        chargesRemaining--;
        M12TrioMasteryBattleTracker.NotifyKingDecreeTriggered(isPlayerSide);
        logHistory?.Invoke("庭訓號令：這次傷害少 5 點 本局還可觸發 " + chargesRemaining + " 次");
        return reduced;
    }

    /// <summary>修女·聖療共鳴：場上修女首次初級治療後，溢出轉英雄；無溢出則 heroBonusWhenNoOverflow（預設 10，聖療連攜 12）。</summary>
    public static int ApplyNunHolyResonance(
        ref bool resonanceUsed,
        int monsterId,
        ref int fieldCurrentHp,
        int fieldMaxHp,
        ref int heroHp,
        Action<string> logHistory,
        int heroBonusWhenNoOverflow = 10)
    {
        if (resonanceUsed || monsterId != MonsterSkillIds.Nun || fieldMaxHp <= 0)
            return 0;

        int overflow = Mathf.Max(0, fieldCurrentHp - fieldMaxHp);
        int toHero;
        if (overflow > 0)
        {
            fieldCurrentHp = fieldMaxHp;
            toHero = overflow;
        }
        else
        {
            toHero = Mathf.Max(1, heroBonusWhenNoOverflow);
        }

        if (toHero <= 0)
            return 0;

        resonanceUsed = true;
        heroHp += toHero;
        if (overflow > 0)
            logHistory?.Invoke("聖療共鳴：溢出 " + overflow + " 點轉補英雄 本局僅1次");
        else
            logHistory?.Invoke("聖療共鳴：無溢出 英雄額外 +" + toHero + " 點 本局僅1次");
        return toHero;
    }

    /// <summary>主教·祝聖預留：本局首次置場主教。回傳 true 表示本次為授予祝聖（不綁定當前這次置場）。</summary>
    /// <param name="deferBindTargetChoice">true=玩家稍後在 UI 選擇綁主教或下一張場怪。</param>
    public static bool TryGrantBishopConsecrationReserve(
        ref BishopConsecrationBattleState state,
        Action<string> logHistory,
        Action<string, float> showToast,
        bool deferBindTargetChoice = false)
    {
        if (state.reserveGrantedThisBattle)
            return false;
        state.reserveGrantedThisBattle = true;
        if (deferBindTargetChoice)
        {
            state.awaitingPlayerBindChoice = true;
            state.awaitingNextSummon = false;
            logHistory?.Invoke("祝聖預留：請選擇綁定主教本人或下一張打出的場怪 本局僅1次");
            showToast?.Invoke("主教·祝聖預留：請選擇祝聖綁定對象", 2.8f);
        }
        else
        {
            state.awaitingPlayerBindChoice = false;
            state.awaitingNextSummon = true;
            logHistory?.Invoke("祝聖預留：下一隻場怪已預祝 本局僅1次");
            showToast?.Invoke("主教·祝聖預留：下一隻場怪首傷減 3（宗教減 4）", 2.4f);
        }
        return true;
    }

    /// <summary>玩家選擇「下一張打出的場怪」承載祝聖。</summary>
    public static void ApplyConsecrationBindToNextMonsterChoice(
        ref BishopConsecrationBattleState state,
        Action<string> logHistory)
    {
        state.awaitingPlayerBindChoice = false;
        state.awaitingNextSummon = true;
        logHistory?.Invoke("祝聖預留：已選擇綁定下一張打出的場怪");
    }

    /// <summary>將祝聖綁定至本局下一隻已置場怪獸（含僅主教在場時改綁主教自身）。回傳是否本次有綁定。</summary>
    public static bool TryBindConsecrationToFieldMonster(
        ref BishopConsecrationBattleState state,
        int monsterId,
        string boundDisplayName,
        Action<string> logHistory)
    {
        if (!state.awaitingNextSummon)
            return false;

        state.awaitingNextSummon = false;
        state.awaitingFirstHit = true;
        state.religiousSynergy = MonsterSkillReligion.IsReligiousMonsterId(monsterId);
        state.holyTherapyLinkOnNun = monsterId == MonsterSkillIds.Nun;

        string name = string.IsNullOrWhiteSpace(boundDisplayName) ? "場上怪獸" : boundDisplayName.Trim();
        if (state.holyTherapyLinkOnNun)
        {
            logHistory?.Invoke("祝聖 · 聖療連攜：已綁定 " + name + " 本局僅1次");
            logHistory?.Invoke("聖療連攜：此修女首次初級治療將多 5 點");
        }
        else if (state.religiousSynergy)
            logHistory?.Invoke("宗教連攜：祝聖已綁定 " + name + " 首傷減 4 本局僅1次");
        else
            logHistory?.Invoke("祝聖：已綁定 " + name + " 首傷減 3 本局僅1次");

        return true;
    }

    /// <summary>祝聖首次受傷減傷（先於其他場上減傷之外、王后之後由呼叫端控制順序）。</summary>
    public static int ApplyConsecrationFirstHit(
        ref BishopConsecrationBattleState state,
        int incomingDamage,
        Action<string> logHistory)
    {
        if (!state.awaitingFirstHit || incomingDamage <= 0)
            return incomingDamage;

        int reduction = state.religiousSynergy ? 4 : 3;
        int reduced = Mathf.Max(1, incomingDamage - reduction);
        if (reduced >= incomingDamage)
            return incomingDamage;

        state.awaitingFirstHit = false;
        if (state.religiousSynergy)
            logHistory?.Invoke("宗教連攜：祝聖這次傷害少 " + reduction + " 點 本局僅1次");
        else
            logHistory?.Invoke("祝聖：這次傷害少 " + reduction + " 點 本局僅1次");
        return reduced;
    }

    /// <summary>聖療連攜：祝聖後下隻為修女時，首次初級治療 +5（在加 HP 前）。</summary>
    public static int TryApplyHolyTherapyHealBonus(
        ref BishopConsecrationBattleState state,
        int monsterId,
        ref int healAmount,
        Action<string> logHistory,
        Action<string, float> showToast)
    {
        if (!state.holyTherapyLinkOnNun || state.holyTherapyHealBonusUsed ||
            monsterId != MonsterSkillIds.Nun || healAmount <= 0)
            return 0;

        state.holyTherapyHealBonusUsed = true;
        healAmount += 5;
        logHistory?.Invoke("聖療連攜：初級治療多 5 點 本局僅1次");
        showToast?.Invoke("聖療連攜：初級治療多 5 點", 2.2f);
        return 5;
    }

    /// <summary>城堡·堅城駐守：場上城堡首次受到傷害時 −5（減後至少 1）。每局 1 次。</summary>
    public static int ApplyCastleFortressStand(ref bool fortressUsed, int incomingDamage, Action<string> logHistory)
    {
        if (fortressUsed || incomingDamage <= 0)
            return incomingDamage;
        int reduced = Mathf.Max(1, incomingDamage - 5);
        if (reduced >= incomingDamage)
            return incomingDamage;
        fortressUsed = true;
        logHistory?.Invoke("堅城駐守：這次傷害少 5 點 本局僅1次");
        return reduced;
    }

    /// <summary>
    /// 聖院騎士·護聖：本局首次置場，且替換下場的友軍 ATK=0 時觸發（單格場上僅能經祝聖替換達成）。
    /// </summary>
    public static bool TryTriggerHolySanctuaryOnSanctumKnightSummon(
        ref bool holyGuardUsed,
        bool skillActive,
        bool replacedZeroAttackAlly)
    {
        if (holyGuardUsed || !skillActive || !replacedZeroAttackAlly)
            return false;
        holyGuardUsed = true;
        return true;
    }

    /// <summary>王后·王室庇護：每局首次受到傷害時 −3（減後至少 1）。</summary>
    public static int ApplyQueenShelter(
        ref bool firstHitConsumed,
        int incomingDamage,
        Action<string> logHistory,
        bool isPlayerSide)
    {
        if (firstHitConsumed || incomingDamage <= 0)
            return incomingDamage;
        int reduced = Mathf.Max(1, incomingDamage - 3);
        if (reduced >= incomingDamage)
            return incomingDamage;
        firstHitConsumed = true;
        M12TrioMasteryBattleTracker.NotifyQueenShelterTriggered(isPlayerSide);
        logHistory?.Invoke("王室庇護：這次傷害少 3 點 本局不再觸發");
        return reduced;
    }
}

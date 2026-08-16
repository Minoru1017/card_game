using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime plot steps for main plot (see TUTORIAL_PLOT_SCRIPT.md: 1-1 §四, 1-2 §八; M-1-3 see M13_PLOT_SCRIPT.md).</summary>
public static class TutorialPlotScriptFactory
{
    /// <summary>開場劇情「教學期間先給你一副基礎牌組」步驟索引（0-based）。</summary>
    public const int IntroStarterDeckGrantStepIndex = 19;

    public const string StarterDeckGrantDialogueMarker = "基礎牌組";

    public static bool IsStarterDeckGrantPlotStep(MainPlotSceneController.PlotStep step)
    {
        if (step == null || string.IsNullOrEmpty(step.dialogueText)) return false;
        return step.dialogueText.IndexOf(StarterDeckGrantDialogueMarker, System.StringComparison.Ordinal) >= 0;
    }

    public const string LinKeSpeaker = "林可姐";
    public const string MentorSpeaker = "導師";
    public const string NarratorSpeaker = "旁白";
    public const string SelSpeaker = "燈守·賽爾";
    public const string AChaoSpeaker = "阿潮";
    public const string PlayerSpeaker = "你";
    public const string HelmsmanSpeaker = "舵叔";
    public const string GrandmaGrassSpeaker = "草奶奶";

    /// <summary>A-1 碼頭「去／改天」選項步驟索引（0-based）。</summary>
    public const int A1HarborLaunchChoiceStepIndex = 2;

    /// <summary>A-1 登島「我試試／您來」選項步驟索引（0-based）。</summary>
    public const int A1IslandFarmChoiceStepIndex = 3;

    /// <summary>M-1-3 開場誓約選項步驟索引（0-based）。</summary>
    public const int M13OpeningOathChoiceStepIndex = 7;

    /// <summary>M-1-3 玫瑰試煉選項步驟索引（0-based）。</summary>
    public const int M13RoseTrialChoiceStepIndex = 7;
    /// <summary>與 CardList spell 002、<c>Assets/UI/CardArt/林可的凝視</c> 同源。</summary>
    private const int LinKeGazeSpellId = 2;
    private const string LinKeGazeCardArtResourcePath = "CardArt/林可的凝視";

    private static Sprite _linKePortrait;

    private static void IntroTapStep(
        List<MainPlotSceneController.PlotStep> steps,
        string speaker,
        string text,
        int nextIndex)
        => TapStep(steps, speaker, text, nextIndex, assignIntroVoice: true);

    private static void IntroChoiceStep(
        List<MainPlotSceneController.PlotStep> steps,
        string speaker,
        string text,
        string c1,
        int n1,
        string c2 = null,
        int n2 = -1,
        string c3 = null,
        int n3 = -1)
        => ChoiceStep(steps, speaker, text, c1, n1, c2, n2, c3, n3, assignIntroVoice: true);

    public static List<MainPlotSceneController.PlotStep> BuildTutorialPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(30);
        IntroChoiceStep(steps, MentorSpeaker,
            "歡迎來到舊校舍對戰館 從今天起 你不再只是看客 你要" + StoryTextStyle.Em("親自舉牌"),
            "我準備好了", 1);
        IntroTapStep(steps, MentorSpeaker,
            "館內燈光壓低了一檔 對戰桌一字排開 你以前多半只在欄外看別人出牌吧",
            2);
        IntroTapStep(steps, MentorSpeaker,
            "從今天起你正式登記入座 這裡的規則與勝負 都照對戰館的帳來算 不講人情",
            3);
        IntroTapStep(steps, MentorSpeaker,
            "實戰前的入門與戰況紀錄 我交給副館長 " + StoryTextStyle.Em("林可") + " 她會帶你走過該知道的事",
            4);
        IntroTapStep(steps, LinKeSpeaker,
            "……聽到了吧 導師說的就是我 戰況簿在我這 場上出了什麼岔 都會記一筆",
            5);
        IntroTapStep(steps, LinKeSpeaker,
            "叫我林可姐就好 接下來帶你走一條" + StoryTextStyle.Em("能開打") + "的最短路 別走神",
            6);
        IntroTapStep(steps, LinKeSpeaker,
            "這裡的對戰 不比誰牌比較華麗 看的是誰先讓對手" + StoryTextStyle.Hi("英雄生命") + "歸零",
            7);
        IntroTapStep(steps, LinKeSpeaker,
            "雙方英雄都是 " + StoryTextStyle.Em("20") + " 點生命 歸零就輸 跟你場上還有沒有怪獸無關 記清楚",
            8);
        IntroTapStep(steps, LinKeSpeaker,
            "開場擲骰定" + StoryTextStyle.Em("先手") + " 別緊張 先手只是先出牌 又不代表你會贏",
            9);
        IntroTapStep(steps, LinKeSpeaker,
            "牌就兩類 " + StoryTextStyle.Hi("怪獸") + " 跟 " + StoryTextStyle.Hi("法術") + " 怪獸站場上 法術多半打完進棄牌",
            10);
        IntroTapStep(steps, LinKeSpeaker,
            "同一時間 場上通常只留一隻怪獸 新的來舊的就走 別指望排一整排",
            11);
        IntroTapStep(steps, LinKeSpeaker,
            "法術有條件 例如" + StoryTextStyle.Em("初級治療") + "治的是場上怪獸 場上已有怪時 有些牌就不能從手牌打出",
            12);
        IntroTapStep(steps, LinKeSpeaker,
            StoryTextStyle.Em("火球術") + "多半拿來拆對手場上的怪 對手場上沒怪 傷害才會落到英雄身上",
            13);
        IntroChoiceStep(steps, LinKeSpeaker,
            "小測驗 想打英雄 對手場上卻有怪擋著 穩一點的做法是",
            "先出火球清場", 14, "先放治療", 15, "棄掉所有手牌", 15);
        IntroTapStep(steps, LinKeSpeaker,
            "嗯 先清場再說 直攻英雄是下一步 火球常用在這種時候",
            16);
        IntroTapStep(steps, LinKeSpeaker,
            "再想一下 治療只能救己方怪獸 " + StoryTextStyle.Em("清對手場上的怪") + "不是治療的活 回去重選",
            13);
        IntroTapStep(steps, LinKeSpeaker,
            "牌組最多 " + StoryTextStyle.Em("30") + " 張 只能放你已持有的卡 學院會先幫你備好能開打的牌",
            17);
        IntroTapStep(steps, LinKeSpeaker,
            "之後到大廳的 " + StoryTextStyle.Em("Buildbeck") + " 可以自己調換 記得按" + StoryTextStyle.Hi("儲存") +
            " 現在先跟林可姐把規則走完",
            18);
        IntroTapStep(steps, LinKeSpeaker,
            "列表是小圖 詳情才是大立繪 別以為壞掉 只是用途不同",
            19);
        IntroTapStep(steps, LinKeSpeaker,
            "教學期間先給你一副基礎牌組 民兵 長弓 治療 火球都有 能開打再說 風格慢慢換",
            20);
        IntroTapStep(steps, LinKeSpeaker,
            "進戰鬥起手抽 " + StoryTextStyle.Em("5") + " 張 手牌最多 " + StoryTextStyle.Em("7") + " 張 塞太滿就得棄牌或先打完 別拖",
            21);
        IntroTapStep(steps, LinKeSpeaker,
            "你的回合通常就三件事 " + StoryTextStyle.Hi("出牌") + " " + StoryTextStyle.Hi("攻擊") + " 最後按" + StoryTextStyle.Em("結束回合") + " 不按結束 對手不會動",
            22);
        IntroTapStep(steps, LinKeSpeaker,
            "怪獸上場記得叫它攻擊 很多人輸在 " + StoryTextStyle.Em("放了怪卻忘記打") + " 別當其中一個",
            23);
        IntroTapStep(steps, LinKeSpeaker,
            "右側有" + StoryTextStyle.Hi("最近戰況") + " 剛剛發生什麼都記在那 結算看不懂 先翻那裡",
            24);
        IntroChoiceStep(steps, LinKeSpeaker,
            "再考你一次 場上已有一隻民兵 還沒攻擊 這時該先做什麼",
            "按結束回合", 25, "讓民兵攻擊", 26, "立刻再上一隻怪", 25);
        IntroTapStep(steps, LinKeSpeaker,
            "場上通常只留一隻怪 再上一隻會頂掉舊的 剛才那步等於白做 先攻擊 或先想清楚",
            24);
        IntroTapStep(steps, LinKeSpeaker,
            "這就對了 先攻擊 再視情況結束回合 最基本的節奏就是這樣",
            27);
        IntroTapStep(steps, LinKeSpeaker,
            "進訓練場選" + StoryTextStyle.Em("入門級") + " 給第一次實戰用的 這級不會有天氣 專心練出牌跟攻擊",
            28);
        IntroTapStep(steps, LinKeSpeaker,
            "天氣 隱藏難度 那些特殊法術 贏幾場再說 現在目標只有一個 " + StoryTextStyle.Em("打完第一場教學戰"),
            29);
        IntroChoiceStep(steps, LinKeSpeaker,
            "戰況簿我會幫你記 準備好了就點出發 " + StoryTextStyle.Em("直接進教學對戰"),
            "出發", -1, "再看一次組牌說明", 18);
        return steps;
    }

    /// <summary>教學戰勝利後結尾劇情（接在結算「繼續」之後）。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildTutorialPlotEpilogueSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(6);
        TapStep(steps, LinKeSpeaker,
            "……回來了 第一場" + StoryTextStyle.Em("教學戰") + "算你過關 出牌跟攻擊的節奏有跟上",
            1,
            voiceClipId: "1-1_30");
        TapStep(steps, LinKeSpeaker,
            StoryTextStyle.Em("國王") + " " + StoryTextStyle.Em("王后") + " " + StoryTextStyle.Em("民兵") +
            " 各一張已放進背包 之後在 " + StoryTextStyle.Hi("Buildbeck") + " 或館藏都能看",
            2,
            voiceClipId: "1-1_31");
        TapStep(steps, LinKeSpeaker,
            "想再練就回" + StoryTextStyle.Em("遊戲進度") + " 開入門級 熟了也能從那裡" + StoryTextStyle.Em("前往大廳") + " 自己逛",
            3,
            voiceClipId: "1-1_32");
        TapStepEndPlot(steps, LinKeSpeaker,
            "今天的引導先到這 戰況簿我會繼續幫你記 按下回到遊戲進度",
            voiceClipId: "1-1_33");
        return steps;
    }

    /// <summary>港灣實戰首通後銜接（L1-2-002）：短台詞 → 回 Story progress 並聚焦 M-1-2。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildHarborCombatClearBridgeSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(4);
        TapStep(steps, LinKeSpeaker,
            "港灣那一仗過關了 地圖上 " + StoryTextStyle.Em("海牆巡邏") + " 已解鎖",
            1);
        TapStep(steps, LinKeSpeaker,
            "下一段是御三家戰技段考 不在學院館內了 跟我到海堤上去",
            2);
        TapStepEndPlot(steps, LinKeSpeaker,
            "我先帶你看節點 按下回到遊戲進度");
        return steps;
    }

    /// <summary>M-1-2 開場：段考說明 → 平安符 → 階段 A。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildM12IntroPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(6);
        TapStep(steps, LinKeSpeaker,
            "海堤上的風比館內硬 這一段叫" + StoryTextStyle.Em("海牆巡邏") + " 先考御三家戰技 再練教會三張",
            1,
            voiceClipId: "2-1_A0");
        TapStep(steps, LinKeSpeaker,
            "牌組我幫你鎖好了 只要" + StoryTextStyle.Hi("國王") + " " + StoryTextStyle.Hi("王后") + " " +
            StoryTextStyle.Hi("民兵") + " 三項戰技本局各觸發一次 再加勝利",
            2,
            voiceClipId: "2-1_A1");
        TapStep(steps, LinKeSpeaker,
            "臨上場前 我把這枚" + StoryTextStyle.Em("平安符") + "貼在你英雄名旁 " +
            StoryTextStyle.Hi("英雄護盾") + " 能" + StoryTextStyle.Hi("擋一次對英雄的傷害") + " 用了就沒了 別浪費",
            3,
            voiceClipId: "2-1_A2");
        TapStepEndPlot(steps, LinKeSpeaker,
            "準備好了就進" + StoryTextStyle.Em("段考 A") + " 這一場我不出聲 看你自己打",
            voiceClipId: "2-1_A3");
        return steps;
    }

    /// <summary>中段開場短劇情（§3.3.1）→ 海牆散策熱區場景（M12SeawallStrollOverlay）→ 階段 B。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildM12MidPatrolPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(10);
        TapStep(steps, LinKeSpeaker,
            "段考剛結束 你怎麼" + StoryTextStyle.Em("臉色蒼白") + " 是考試壓力太大 還是教室裡出了什麼事",
            1,
            voiceClipId: "2-1_B0");
        ChoiceStep(steps, PlayerSpeaker,
            "——",
            "壓力是有點 剛才手還在抖", 2,
            "教室裡好像不太對勁", 3,
            "沒事 只是海風吹的", 4);
        TapStep(steps, LinKeSpeaker,
            "贏都贏了 別把自己逼太狠 先" + StoryTextStyle.Hi("深呼吸") + " 兩口",
            5,
            voiceClipId: "2-1_B1");
        TapStep(steps, LinKeSpeaker,
            "……先別在這說 跟我出去走一段 " + StoryTextStyle.Em("海牆") + " 上透口氣",
            5);
        TapStep(steps, LinKeSpeaker,
            "海風能把你吹成這副樣子 行 不想說就不逼你",
            5);
        TapStep(steps, LinKeSpeaker,
            "段考 A 過關 " + StoryTextStyle.Em("御三家戰技") + " 三項都觸發了 別急著馬上開第二場",
            6);
        TapStep(steps, LinKeSpeaker,
            "先沿" + StoryTextStyle.Em("海牆") + " 走一段 這才叫巡邏 順便讓腦袋喘口氣",
            7);
        TapStepEndPlot(steps, LinKeSpeaker,
            "看到什麼想看的就點一點 巡過一處 我們就去" + StoryTextStyle.Em("戰位克制") + " 加練");
        return steps;
    }

    /// <summary>B 勝且通關後短結尾 → Story progress。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildM12VictoryEpilogueSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(3);
        TapStep(steps, LinKeSpeaker,
            StoryTextStyle.Em("修女") + " " + StoryTextStyle.Em("主教") + " " + StoryTextStyle.Em("城堡") +
            " 各一張入收藏了 熟練度到 B 戰技會照規則生效",
            1);
        TapStep(steps, LinKeSpeaker,
            "下游" + StoryTextStyle.Em("河岔分波") + " 的" + StoryTextStyle.Em("邊燈") + " 也亮了 準備好再去",
            2);
        TapStepEndPlot(steps, LinKeSpeaker,
            "海牆巡邏通關 按下回到遊戲進度");
        return steps;
    }

    /// <summary>M-1-3 開場：邊燈夜話 → 誓約三選一（改寫帕拉塞爾蘇斯之玫瑰）。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildM13OpeningPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(16);
        TapStep(steps, NarratorSpeaker,
            "夜幕垂在河岔 邊燈廳前的爐火晃著 火光似乎敵不過這股冷潮",
            1);
        TapStep(steps, SelSpeaker,
            "神啊 求您賜我一位願意" + StoryTextStyle.Em("走這條路") + "的弟子",
            2);
        TapStep(steps, NarratorSpeaker,
            "燈守欲起身點鐵燈 椅子一離身就變冷 他終究沒動",
            3);
        TapStep(steps, LinKeSpeaker,
            "……賽爾師父 學院把" + StoryTextStyle.Em("迎潮實測") + "排在這裡 不是為了再收一位學徒",
            4);
        TapStep(steps, LinKeSpeaker,
            "這位是今次實測生 段考過了 教會三張也用過了",
            5);
        TapStep(steps, SelSpeaker,
            "我得西方的面孔 也得東方的面孔 你的面孔我記不起來 你是誰 你希望我做什麼",
            6);
        TapStep(steps, LinKeSpeaker,
            "他問的是" + StoryTextStyle.Em("誓約") + " 不是名子 把你為什麼來說清楚",
            7);
        ChoiceStep(steps, PlayerSpeaker,
            "——",
            "我來學" + StoryTextStyle.Em("迎潮") + " 讓牌桌會變天", 8,
            "我來證明段考不是運氣", 9,
            "我只是跟著地圖來的", 10);
        TapStep(steps, SelSpeaker,
            "好 變天不是懲罰 是路的一部分",
            11);
        TapStep(steps, SelSpeaker,
            "證明給" + StoryTextStyle.Em("對手") + "看 別向燈求把戲",
            11);
        TapStep(steps, SelSpeaker,
            "那更要小心 漫無目的的人最愛要奇蹟",
            11);
        TapStep(steps, SelSpeaker,
            "這條路就是" + StoryTextStyle.Em("分波") + " 起點也是分波 你若只追終點 就還沒開始",
            12);
        TapStep(steps, SelSpeaker,
            "廳裡那盆" + StoryTextStyle.Em("迎潮玫瑰") + " 是舊日學院留下的 別用手去試 用" + StoryTextStyle.Hi("牌") + "去試",
            13);
        TapStepEndPlot(steps, LinKeSpeaker,
            "先做" + StoryTextStyle.Em("分波鬥鳥") + " 讓節奏與水流對齊 不想玩可以直接迎測 戰鬥段陸續接入");
        return steps;
    }

    /// <summary>階段 A 後：玫瑰試煉（阿潮要當場證明）。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildM13RoseTrialPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(20);
        TapStep(steps, NarratorSpeaker,
            "門外分波聲急了一陣 有人踏進來 同樣滿臉疲憊",
            1);
        TapStep(steps, AChaoSpeaker,
            "我聽說你在這裡 段考過了 教會三張也會了 那" + StoryTextStyle.Em("現在") + "給我看啊",
            2);
        TapStep(steps, AChaoSpeaker,
            "傳說賽爾師父能令玫瑰成灰 再令灰成玫瑰 我只要一個" + StoryTextStyle.Em("證明"),
            3);
        TapStep(steps, SelSpeaker,
            "你太天真了 我不需要你的輕信 我需要的是" + StoryTextStyle.Em("信仰"),
            4);
        TapStep(steps, AChaoSpeaker,
            "正因我不輕信 我才要親眼看 玫瑰的毀滅與重生",
            5);
        TapStep(steps, SelSpeaker,
            "若我照做 你只會說那是把戲 爐子是冷的 蒸餾器上覆蓋灰塵 這段路我用的是" + StoryTextStyle.Em("別的工具"),
            6);
        TapStep(steps, LinKeSpeaker,
            "（對你）" + StoryTextStyle.Em("別讓他替你做選擇"),
            7);
        ChoiceStep(steps, PlayerSpeaker,
            "——",
            "阻下阿潮", 8,
            "沉默旁觀", 12,
            "我也想看奇蹟", 12);
        TapStep(steps, PlayerSpeaker,
            "要證明去牌桌上 別動那盆花",
            9);
        TapStep(steps, AChaoSpeaker,
            "……行 那就在牌桌上見",
            10);
        TapStep(steps, SelSpeaker,
            "玫瑰是永恆的 只有" + StoryTextStyle.Em("外貌") + "會變 像" + StoryTextStyle.Hi("天氣") + " 像" + StoryTextStyle.Hi("牌局"),
            18);
        TapStep(steps, NarratorSpeaker,
            "玫瑰的顏色在炭火裡一瞬間沒了 只剩細灰",
            13);
        TapStep(steps, AChaoSpeaker,
            "看 什麼也沒有",
            14);
        TapStep(steps, SelSpeaker,
            "這些曾是玫瑰的灰 " + StoryTextStyle.Em("在這裡") + " 不會再開花",
            15);
        TapStep(steps, AChaoSpeaker,
            "……騙子",
            16);
        TapStep(steps, LinKeSpeaker,
            "他錯了 但你若也只信眼睛 下一場會打得很難",
            17);
        TapStepEndPlot(steps, SelSpeaker,
            "去牌桌上吧 若你仍願走這條路 " + StoryTextStyle.Em("不用向我證明") + " 分波對決在等你");
        return steps;
    }

    /// <summary>B 段勝利後終幕：灰與一字「迎潮」。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildM13EpiloguePlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(8);
        TapStep(steps, NarratorSpeaker,
            "阿潮離去時沒有回頭 分波聲又慢了下來",
            1);
        TapStep(steps, SelSpeaker,
            "你以為他看見的是空 其實他看見的是" + StoryTextStyle.Em("自己的急"),
            2);
        TapStep(steps, SelSpeaker,
            "巴塞爾的醫生說我是騙子 或許他們對 或許我也騙自己 但我知道這是一條路",
            3);
        TapStep(steps, NarratorSpeaker,
            "燈守將掌中細灰傾入另一掌 低聲一個字 " + StoryTextStyle.Em("迎潮"),
            4);
        TapStep(steps, NarratorSpeaker,
            "灰燼裡再無玫瑰 但貴重品庫裡那道" + StoryTextStyle.Em("封印") + " 似乎亮了一下",
            5);
        TapStepEndPlot(steps, LinKeSpeaker,
            "別問剛才那是不是魔法 問你" + StoryTextStyle.Hi("下一張牌") + "要怎麼出 按下回到遊戲進度");
        return steps;
    }

    public static Sprite GetLinKePortraitSprite() => ResolveLinKePortrait();

    private static Sprite ResolveLinKePortrait()
    {
        if (_linKePortrait != null) return _linKePortrait;

        _linKePortrait = TryLoadLinKeGazeFromCardStore();
        if (_linKePortrait != null) return _linKePortrait;

        _linKePortrait = LoadSpriteFromResources(LinKeGazeCardArtResourcePath);
        return _linKePortrait;
    }

    private static Sprite TryLoadLinKeGazeFromCardStore()
    {
        CardStore store = Object.FindFirstObjectByType<CardStore>();
        if (store == null) return null;

        if (store.cardList == null || store.cardList.Count == 0)
            store.LoadCardData();

        Card card = store.GetCardById(LinKeGazeSpellId);
        return card?.ResolveCardArtSprite();
    }

    private static Sprite LoadSpriteFromResources(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;

        Sprite[] slices = Resources.LoadAll<Sprite>(resourcePath);
        if (slices == null || slices.Length == 0) return null;

        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i] != null)
                return slices[i];
        }

        return null;
    }

    private static void TapStepEndPlot(
        List<MainPlotSceneController.PlotStep> steps,
        string speaker,
        string text,
        string voiceClipId = null)
    {
        var step = new MainPlotSceneController.PlotStep
        {
            speakerName = speaker,
            dialogueText = text,
            advanceKind = MainPlotSceneController.PlotAdvanceKind.TapToContinue,
            tapNextStepIndex = -1,
            tapEndsPlot = true,
            choice1Text = string.Empty,
            choice2Text = string.Empty,
            choice3Text = string.Empty,
            choice1Next = -1,
            choice2Next = -1,
            choice3Next = -1
        };
        ApplySpeakerPortrait(step, speaker);
        ApplyExplicitVoiceClipId(step, voiceClipId);
        steps.Add(step);
    }

    private const string IntroPlotVoiceIdPrefix = "1-1";

    private static void ApplyIntroPlotVoiceClipId(
        List<MainPlotSceneController.PlotStep> steps,
        MainPlotSceneController.PlotStep step,
        string speaker)
    {
        if (speaker != LinKeSpeaker || step == null || steps == null)
            return;

        step.npcVoiceClipId = IntroPlotVoiceIdPrefix + "_" + steps.Count;
    }

    private static void TapStep(
        List<MainPlotSceneController.PlotStep> steps,
        string speaker,
        string text,
        int nextIndex,
        bool assignIntroVoice = false,
        string voiceClipId = null)
    {
        var step = new MainPlotSceneController.PlotStep
        {
            speakerName = speaker,
            dialogueText = text,
            advanceKind = MainPlotSceneController.PlotAdvanceKind.TapToContinue,
            tapNextStepIndex = nextIndex,
            choice1Text = string.Empty,
            choice2Text = string.Empty,
            choice3Text = string.Empty,
            choice1Next = -1,
            choice2Next = -1,
            choice3Next = -1
        };
        ApplySpeakerPortrait(step, speaker);
        if (assignIntroVoice)
            ApplyIntroPlotVoiceClipId(steps, step, speaker);
        ApplyExplicitVoiceClipId(step, voiceClipId);
        steps.Add(step);
    }

    private static void ApplyExplicitVoiceClipId(MainPlotSceneController.PlotStep step, string voiceClipId)
    {
        if (step == null || string.IsNullOrWhiteSpace(voiceClipId))
            return;

        step.npcVoiceClipId = voiceClipId.Trim();
    }

    private static void ChoiceStep(
        List<MainPlotSceneController.PlotStep> steps,
        string speaker,
        string text,
        string c1,
        int n1,
        string c2 = null,
        int n2 = -1,
        string c3 = null,
        int n3 = -1,
        bool assignIntroVoice = false)
    {
        var step = new MainPlotSceneController.PlotStep
        {
            speakerName = speaker,
            dialogueText = text,
            advanceKind = MainPlotSceneController.PlotAdvanceKind.PlayerChoice,
            choice1Text = c1,
            choice1Next = n1,
            choice2Text = c2 ?? string.Empty,
            choice2Next = n2,
            choice3Text = c3 ?? string.Empty,
            choice3Next = n3
        };
        ApplySpeakerPortrait(step, speaker);
        if (assignIntroVoice)
            ApplyIntroPlotVoiceClipId(steps, step, speaker);
        steps.Add(step);
    }

    private static void ApplySpeakerPortrait(MainPlotSceneController.PlotStep step, string speaker)
    {
        if (speaker != LinKeSpeaker) return;
        step.characterASprite = ResolveLinKePortrait();
    }

    /// <summary>A-1 碼頭短劇（§A-1.3 幕 1）→ 航程 overlay。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildA1HarborPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(4);
        TapStep(steps, HelmsmanSpeaker,
            SideQuestA1PlotCopy.HarborLaunchLines[0],
            1,
            voiceClipId: SideQuestA1PlotCopy.Voice.Harbor0);
        TapStep(steps, HelmsmanSpeaker,
            SideQuestA1PlotCopy.HarborLaunchLines[1],
            2,
            voiceClipId: SideQuestA1PlotCopy.Voice.Harbor1);
        ChoiceStep(steps, HelmsmanSpeaker,
            SideQuestA1PlotCopy.HarborChoicePrompt,
            "去", 3,
            "改天", -1);
        TapStepEndPlot(steps, HelmsmanSpeaker,
            SideQuestA1PlotCopy.HarborLaunchLines[2],
            voiceClipId: SideQuestA1PlotCopy.Voice.Harbor2);
        return steps;
    }

    /// <summary>A-1 登島短劇（§A-1.3 幕 2）→ 三畦農事 overlay。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildA1IslandIntroPlotSteps(int slot)
    {
        var steps = new List<MainPlotSceneController.PlotStep>(6);
        TapStep(steps, NarratorSpeaker,
            SideQuestA1PlotCopy.IslandNarration,
            1,
            voiceClipId: SideQuestA1PlotCopy.Voice.Island0);
        TapStep(steps, GrandmaGrassSpeaker,
            SideQuestA1PlotCopy.IslandGrandmaOpen,
            2,
            voiceClipId: SideQuestA1PlotCopy.Voice.Island1);
        TapStep(steps, GrandmaGrassSpeaker,
            SideQuestA1PlotCopy.IslandGrandmaOrder,
            3,
            voiceClipId: SideQuestA1PlotCopy.Voice.Island2);

        if (SideQuestA1ProgressState.IsSealedSpellReady(slot))
        {
            ChoiceStep(steps, PlayerSpeaker,
                "——",
                "我試試", 4,
                "您來", -1);
            TapStepEndPlot(steps, GrandmaGrassSpeaker,
                SideQuestA1PlotCopy.IslandSealedSpellHint,
                voiceClipId: SideQuestA1PlotCopy.Voice.IslandSealedHint);
        }
        else
        {
            ChoiceStep(steps, PlayerSpeaker,
                "——",
                "我試試", -1,
                "您來", -1);
        }

        return steps;
    }

    /// <summary>A-1 解封儀式（§A-1.3 幕 4）→ 回港短劇。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildA1UnsealPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(4);
        TapStep(steps, GrandmaGrassSpeaker,
            SideQuestA1PlotCopy.UnsealGrandma1,
            1,
            voiceClipId: SideQuestA1PlotCopy.Voice.Unseal1);
        TapStep(steps, NarratorSpeaker,
            SideQuestA1PlotCopy.UnsealNarration,
            2);
        TapStep(steps, GrandmaGrassSpeaker,
            SideQuestA1PlotCopy.UnsealGrandma2,
            3,
            voiceClipId: SideQuestA1PlotCopy.Voice.Unseal3);
        TapStepEndPlot(steps, GrandmaGrassSpeaker,
            SideQuestA1PlotCopy.UnsealGrandma3,
            voiceClipId: SideQuestA1PlotCopy.Voice.Unseal4);
        return steps;
    }

    /// <summary>A-1 回港短劇（§A-1.3 幕 5）→ 回 Story progress。</summary>
    public static List<MainPlotSceneController.PlotStep> BuildA1ReturnPlotSteps(bool keptSeaPurslaneSeed)
    {
        var steps = new List<MainPlotSceneController.PlotStep>(1);
        string text = SideQuestA1PlotCopy.ReturnDefault;
        if (keptSeaPurslaneSeed)
            text += "\n\n" + SideQuestA1PlotCopy.ReturnWithSeed;
        TapStepEndPlot(steps, HelmsmanSpeaker,
            text,
            voiceClipId: SideQuestA1PlotCopy.Voice.Return0);
        return steps;
    }
}

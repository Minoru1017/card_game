using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime plot steps for tutorial (see TUTORIAL_PLOT_SCRIPT.md).</summary>
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
    /// <summary>與 CardList spell 002、<c>Assets/UI/CardArt/林可的凝視</c> 同源。</summary>
    private const int LinKeGazeSpellId = 2;
    private const string LinKeGazeCardArtResourcePath = "CardArt/林可的凝視";

    private static Sprite _linKePortrait;

    public static List<MainPlotSceneController.PlotStep> BuildTutorialPlotSteps()
    {
        var steps = new List<MainPlotSceneController.PlotStep>(30);
        ChoiceStep(steps, MentorSpeaker,
            "歡迎來到舊校舍對戰館 從今天起 你不再只是看客 你要" + StoryTextStyle.Em("親自舉牌"),
            "我準備好了", 1);
        TapStep(steps, MentorSpeaker,
            "館內燈光壓低了一檔 對戰桌一字排開 你以前多半只在欄外看別人出牌吧",
            2);
        TapStep(steps, MentorSpeaker,
            "從今天起你正式登記入座 這裡的規則與勝負 都照對戰館的帳來算 不講人情",
            3);
        TapStep(steps, MentorSpeaker,
            "實戰前的入門與戰況紀錄 我交給副館長 " + StoryTextStyle.Em("林可") + " 她會帶你走過該知道的事",
            4);
        TapStep(steps, LinKeSpeaker,
            "……聽到了吧 導師說的就是我 戰況簿在我這 場上出了什麼岔 都會記一筆",
            5);
        TapStep(steps, LinKeSpeaker,
            "叫我林可姐就好 接下來帶你走一條" + StoryTextStyle.Em("能開打") + "的最短路 別走神",
            6);
        TapStep(steps, LinKeSpeaker,
            "這裡的對戰 不比誰牌比較華麗 看的是誰先讓對手" + StoryTextStyle.Hi("英雄生命") + "歸零",
            7);
        TapStep(steps, LinKeSpeaker,
            "雙方英雄都是 " + StoryTextStyle.Em("20") + " 點生命 歸零就輸 跟你場上還有沒有怪獸無關 記清楚",
            8);
        TapStep(steps, LinKeSpeaker,
            "開場擲骰定" + StoryTextStyle.Em("先手") + " 別緊張 先手只是先出牌 又不代表你會贏",
            9);
        TapStep(steps, LinKeSpeaker,
            "牌就兩類 " + StoryTextStyle.Hi("怪獸") + " 跟 " + StoryTextStyle.Hi("法術") + " 怪獸站場上 法術多半打完進棄牌",
            10);
        TapStep(steps, LinKeSpeaker,
            "同一時間 場上通常只留一隻怪獸 新的來舊的就走 別指望排一整排",
            11);
        TapStep(steps, LinKeSpeaker,
            "法術有條件 例如" + StoryTextStyle.Em("初級治療") + "治的是場上怪獸 場上已有怪時 有些牌就不能從手牌打出",
            12);
        TapStep(steps, LinKeSpeaker,
            StoryTextStyle.Em("火球術") + "多半拿來拆對手場上的怪 對手場上沒怪 傷害才會落到英雄身上",
            13);
        ChoiceStep(steps, LinKeSpeaker,
            "小測驗 想打英雄 對手場上卻有怪擋著 穩一點的做法是",
            "先出火球清場", 14, "先放治療", 15, "棄掉所有手牌", 15);
        TapStep(steps, LinKeSpeaker,
            "嗯 先清場再說 直攻英雄是下一步 火球常用在這種時候",
            16);
        TapStep(steps, LinKeSpeaker,
            "再想一下 治療只能救己方怪獸 " + StoryTextStyle.Em("清對手場上的怪") + "不是治療的活 回去重選",
            13);
        TapStep(steps, LinKeSpeaker,
            "牌組最多 " + StoryTextStyle.Em("30") + " 張 只能放你已持有的卡 學院會先幫你備好能開打的牌",
            17);
        TapStep(steps, LinKeSpeaker,
            "之後到大廳的 " + StoryTextStyle.Em("Buildbeck") + " 可以自己調換 記得按" + StoryTextStyle.Hi("儲存") +
            " 現在先跟林可姐把規則走完",
            18);
        TapStep(steps, LinKeSpeaker,
            "列表是小圖 詳情才是大立繪 別以為壞掉 只是用途不同",
            19);
        TapStep(steps, LinKeSpeaker,
            "教學期間先給你一副基礎牌組 民兵 長弓 治療 火球都有 能開打再說 風格慢慢換",
            20);
        TapStep(steps, LinKeSpeaker,
            "進戰鬥起手抽 " + StoryTextStyle.Em("5") + " 張 手牌最多 " + StoryTextStyle.Em("7") + " 張 塞太滿就得棄牌或先打完 別拖",
            21);
        TapStep(steps, LinKeSpeaker,
            "你的回合通常就三件事 " + StoryTextStyle.Hi("出牌") + " " + StoryTextStyle.Hi("攻擊") + " 最後按" + StoryTextStyle.Em("結束回合") + " 不按結束 對手不會動",
            22);
        TapStep(steps, LinKeSpeaker,
            "怪獸上場記得叫它攻擊 很多人輸在 " + StoryTextStyle.Em("放了怪卻忘記打") + " 別當其中一個",
            23);
        TapStep(steps, LinKeSpeaker,
            "右側有" + StoryTextStyle.Hi("最近戰況") + " 剛剛發生什麼都記在那 結算看不懂 先翻那裡",
            24);
        ChoiceStep(steps, LinKeSpeaker,
            "再考你一次 場上已有一隻民兵 還沒攻擊 這時該先做什麼",
            "按結束回合", 25, "讓民兵攻擊", 26, "立刻再上一隻怪", 25);
        TapStep(steps, LinKeSpeaker,
            "場上通常只留一隻怪 再上一隻會頂掉舊的 剛才那步等於白做 先攻擊 或先想清楚",
            24);
        TapStep(steps, LinKeSpeaker,
            "這就對了 先攻擊 再視情況結束回合 最基本的節奏就是這樣",
            27);
        TapStep(steps, LinKeSpeaker,
            "進訓練場選" + StoryTextStyle.Em("入門級") + " 給第一次實戰用的 這級不會有天氣 專心練出牌跟攻擊",
            28);
        TapStep(steps, LinKeSpeaker,
            "天氣 隱藏難度 那些特殊法術 贏幾場再說 現在目標只有一個 " + StoryTextStyle.Em("打完第一場教學戰"),
            29);
        ChoiceStep(steps, LinKeSpeaker,
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
            1);
        TapStep(steps, LinKeSpeaker,
            StoryTextStyle.Em("國王") + " " + StoryTextStyle.Em("王后") + " " + StoryTextStyle.Em("民兵") +
            " 各一張已放進背包 之後在 " + StoryTextStyle.Hi("Buildbeck") + " 或館藏都能看",
            2);
        TapStep(steps, LinKeSpeaker,
            "想再練就回" + StoryTextStyle.Em("遊戲進度") + " 開入門級 熟了也能從那裡" + StoryTextStyle.Em("前往大廳") + " 自己逛",
            3);
        TapStepEndPlot(steps, LinKeSpeaker,
            "今天的引導先到這 戰況簿我會繼續幫你記 按下回到遊戲進度");
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
        string text)
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
        steps.Add(step);
    }

    private static void TapStep(
        List<MainPlotSceneController.PlotStep> steps,
        string speaker,
        string text,
        int nextIndex)
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
        steps.Add(step);
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
        int n3 = -1)
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
        steps.Add(step);
    }

    private static void ApplySpeakerPortrait(MainPlotSceneController.PlotStep step, string speaker)
    {
        if (speaker != LinKeSpeaker) return;
        step.characterASprite = ResolveLinKePortrait();
    }
}

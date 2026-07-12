using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FightingBirdGameSceneController
{
    private static readonly Color M13RiverBg = new Color(0.08f, 0.14f, 0.20f, 1f);
    private static readonly Color M13LeftForkAccent = new Color(0.35f, 0.72f, 0.86f, 1f);
    private static readonly Color M13RightForkAccent = new Color(0.90f, 0.48f, 0.38f, 1f);
    private static readonly Color M13ForkSplitAccent = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Vector2 OpponentPadCenter = new Vector2(0f, -370f);

    private void ConfigureM13StoryUi()
    {
        if (replayButtonRoot != null)
            replayButtonRoot.SetActive(false);

        if (leaveButtonLabel != null)
            leaveButtonLabel.text = "繼續迎測";

        RectTransform leaveRt = leaveButtonLabel != null
            ? leaveButtonLabel.transform.parent as RectTransform
            : null;
        if (leaveRt != null)
            leaveRt.anchoredPosition = new Vector2(0f, 80f);

        if (titleText != null)
        {
            titleText.text = "分波鬥鳥";
            titleText.color = M13ForkSplitAccent;
        }

        if (subtitleText != null && npc != null)
        {
            subtitleText.text = npc.introLine;
            subtitleText.color = M13LeftForkAccent;
        }

        if (uiRoot != null)
        {
            Image bg = uiRoot.Find("BG")?.GetComponent<Image>();
            if (bg != null)
                bg.color = M13RiverBg;
        }

        BuildM13ForkBadges();
        UpdateM13ForkBadgeHighlight(-1);
        UpdateM13BarLabels();
    }

    private void BuildM13ForkBadges()
    {
        if (uiRoot == null)
            return;

        m13ForkLeftBadge = CreateForkBadge("M13ForkLeft", "左汊", M13LeftForkAccent,
            new Vector2(0.08f, 0.62f));
        m13ForkRightBadge = CreateForkBadge("M13ForkRight", "右汊", M13RightForkAccent,
            new Vector2(0.92f, 0.62f));
    }

    private TextMeshProUGUI CreateForkBadge(string name, string label, Color color, Vector2 anchor)
    {
        TextMeshProUGUI tmp = CreateText(name, uiRoot, label, 34f, TextAlignmentOptions.Center,
            anchor, anchor, Vector2.zero, new Vector2(120f, 48f), color);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alpha = 0.42f;
        return tmp;
    }

    private void UpdateM13ForkBadgeHighlight(int stepIndex)
    {
        if (m13ForkLeftBadge != null)
            m13ForkLeftBadge.alpha = stepIndex < 0 || BirdDuelRhythmChart.IsLeftForkStep(stepIndex) ? 1f : 0.35f;
        if (m13ForkRightBadge != null)
            m13ForkRightBadge.alpha = stepIndex >= 0 && !BirdDuelRhythmChart.IsLeftForkStep(stepIndex) ? 1f : 0.35f;
    }

    private void UpdateM13BarLabels()
    {
        if (scoreLabel != null)
            scoreLabel.text = "同頻 " + score;
        if (insightLabel != null)
            insightLabel.text = "聽波 " + insight;
    }

    private string ResolveM13CountInText(int countIndex)
    {
        switch (countIndex)
        {
            case 0: return "聽遠处分波…";
            case 1: return "左汊…";
            case 2: return "右汊…";
            default: return "同頻！";
        }
    }

    private void ApplyM13ForkStep(int stepIndex)
    {
        if (!m13StoryMode || !BirdDuelRhythmChart.IsRiverForkWave(activeCdId))
            return;

        UpdateM13ForkBadgeHighlight(stepIndex);

        if (BirdDuelRhythmChart.TryGetForkBeatCaption(stepIndex, out string caption) && subtitleText != null)
        {
            subtitleText.text = caption;
            subtitleText.color = BirdDuelRhythmChart.IsLeftForkStep(stepIndex)
                ? M13LeftForkAccent
                : M13RightForkAccent;
        }

        if (opponentPad != null)
        {
            float laneX = BirdDuelRhythmChart.ResolveForkLaneOffsetX(stepIndex);
            opponentPad.rectTransform.anchoredPosition = OpponentPadCenter + new Vector2(laneX, 0f);
        }

        if (beatPad != null)
        {
            beatPad.color = BirdDuelRhythmChart.IsLeftForkStep(stepIndex)
                ? Color.Lerp(ColorBeatPadIdle, M13LeftForkAccent, 0.35f)
                : Color.Lerp(ColorBeatPadIdle, M13RightForkAccent, 0.35f);
        }
    }

    private void ApplyM13SuspensePresentation(string subtitle)
    {
        if (!m13StoryMode || subtitleText == null)
            return;

        subtitleText.text = subtitle;
        subtitleText.color = M13ForkSplitAccent;

        if (opponentPad != null)
            opponentPad.rectTransform.anchoredPosition = OpponentPadCenter;

        if (beatPad != null)
            beatPad.color = M13ForkSplitAccent;

        UpdateM13ForkBadgeHighlight(-1);
        if (m13ForkLeftBadge != null) m13ForkLeftBadge.alpha = 0.85f;
        if (m13ForkRightBadge != null) m13ForkRightBadge.alpha = 0.85f;
    }

    private void ShowM13Telegraph(BirdGesture opp, bool peek, int stepIndex)
    {
        if (opponentPad != null)
            opponentPad.color = GestureColor(opp);
        if (opponentGlyph != null)
            opponentGlyph.text = BirdDuelCore.ShortName(opp);

        if (peekHintText == null)
            return;

        string branch = BirdDuelRhythmChart.IsLeftForkStep(stepIndex) ? "左汊" : "右汊";
        if (peek)
            peekHintText.text = "聽波：" + branch + " " + BirdDuelCore.DisplayName(opp);
        else
            peekHintText.text = branch + "鳥勢";
        peekHintText.color = BirdDuelRhythmChart.IsLeftForkStep(stepIndex)
            ? M13LeftForkAccent
            : M13RightForkAccent;
    }

    private void ShowM13Feedback(BirdBeatJudgement judgement)
    {
        if (feedbackText == null)
            return;

        string main;
        Color color;
        switch (judgement.outcome)
        {
            case BirdBeatOutcome.Perfect:
                main = "同頻！";
                color = M13LeftForkAccent;
                break;
            case BirdBeatOutcome.Good:
                main = "跟上";
                color = BirdDuelUiColors.GoodFeedback;
                break;
            case BirdBeatOutcome.Guard:
                main = "守住";
                color = ColorPass;
                break;
            default:
                main = "離拍";
                color = M13RightForkAccent;
                break;
        }

        if (judgement.scoreDelta > 0)
            main += $" +{judgement.scoreDelta}";
        if (judgement.insightDelta > 0)
            main += "　聽波 +1";

        feedbackText.text = main;
        feedbackText.color = color;
    }

    private void FinishM13StoryDuel()
    {
        SceneLoader.CompleteM13RiverForkBirdDuel(score, lastResult);
    }

    private void ApplyM13StoryResultCopy(BirdDuelResult result)
    {
        if (resultTitle != null)
        {
            bool sRank = M13BirdDuelGrading.IsSRank(score, result);
            if (sRank)
            {
                resultTitle.text = "S · 分波手";
                resultTitle.color = M13ForkSplitAccent;
            }
            else if (result == BirdDuelResult.Win)
            {
                resultTitle.text = "分波同頻";
                resultTitle.color = ColorScoreFill;
            }
            else if (result == BirdDuelResult.Draw)
            {
                resultTitle.text = "尚可";
                resultTitle.color = ColorNest;
            }
            else
            {
                resultTitle.text = "再聽分波";
                resultTitle.color = ColorPass;
            }
        }

        if (resultIntel != null)
            resultIntel.text = M13BirdDuelGrading.BuildRewardLine(M13BirdDuelGrading.IsSRank(score, result));
    }
}

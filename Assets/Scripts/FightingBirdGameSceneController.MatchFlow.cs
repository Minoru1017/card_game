using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- match flow

    private void StartMatch()
    {
        if (matchRoutine != null) StopCoroutine(matchRoutine);
        matchRoutine = StartCoroutine(RunMatch());
    }

    private IEnumerator RunMatch()
    {
        ResetState();
        if (resultOverlayRoot != null) resultOverlayRoot.SetActive(false);
        SetBeatFxVisible(true);
        SetGestureButtonsVisible(true);
        SetButtonsInteractable(true);

        // 歌曲從頭重播並重設節拍時鐘（「再練一次」也會重播）。
        RestartSongAndClock();

        // 錨定到 anchor 之後的下一個整拍（beat 0 = BGM 第一下拍點）。
        double anchor = songStartDsp + firstDownbeatOffset;
        double aheadBeats = (AudioSettings.dspTime - anchor) / SecondsPerBeat;
        matchFirstBeat = Mathf.Max(0, Mathf.CeilToInt((float)aheadBeats));
        lastTickBeat = matchFirstBeat - 1;
        clockRunning = true;

        // 數拍預備（count-in），落在 BGM 拍點上；tick/脈動由 UpdateMetronome 處理。
        for (int c = 0; c < CountInBeats; c++)
        {
            if (subtitleText != null)
                subtitleText.text = c < CountInBeats - 1 ? "預備…" : "開始！";
            double beatDsp = BeatDsp(matchFirstBeat + c);
            while (AudioSettings.dspTime < beatDsp)
                yield return null;
        }
        if (subtitleText != null)
            subtitleText.text = DefaultSubtitle();

        IReadOnlyList<BirdGesture> pattern = BirdDuelRhythmChart.ResolveBeatPattern(activeCdId, npc.beatPattern);
        // 首步判定需与 count-in「開始！」拉开整段 NormalBeatsPerStep，否则玩家跟 GO 抢按会固定 Miss。
        // count-in 结束在 beat (matchFirstBeat + CountInBeats - 1)；首步命中在其后 NormalBeatsPerStep 拍。
        double beatCursor = matchFirstBeat + CountInBeats - 1 + activeNormalBeatsPerStep;
        for (int step = 0; step < pattern.Count; step++)
        {
            if (UsesDecisiveTripletGrid())
                beatCursor = SnapToTripletGrid(beatCursor);

            BirdGesture opp = pattern[step];
            double hitDsp = BeatFractionDsp(beatCursor);
            currentBeatDsp = hitDsp;

            // 提前 TelegraphLeadBeats 拍揭露對手鳥勢。
            double telegraphDsp = hitDsp - telegraphLeadBeats * SecondsPerBeat;
            while (AudioSettings.dspTime < telegraphDsp)
                yield return null;

            bool peek = insightPeekActive;
            insightPeekActive = false;
            ShowTelegraph(opp, peek);

            pendingInput = default;
            inputLocked = false;
            inputWindowOpen = false;
            beatWindowOpen = true;

            // 預告期間只顯示收束提示，不接受輸入（避免一看到鳥勢就搶按 → 固定 Miss）。
            double inputOpenDsp = hitDsp - goodWindow;
            while (AudioSettings.dspTime < inputOpenDsp)
                yield return null;

            inputWindowOpen = true;

            double inputCloseDsp = hitDsp + goodWindow;
            while (AudioSettings.dspTime < inputCloseDsp && !inputLocked)
                yield return null;

            beatWindowOpen = false;
            inputWindowOpen = false;

            float timingError = inputLocked ? Mathf.Abs((float)(pendingInputDsp - hitDsp)) : 999f;
            bool passRewardAvailable = passUsed < npc.passLimit;
            BirdGesture? input = inputLocked ? pendingInput : (BirdGesture?)null;

            BirdBeatJudgement judgement = BirdDuelCore.Judge(
                opp, input, timingError, passRewardAvailable, perfectWindow, goodWindow);

            ApplyJudgement(opp, input, judgement);
            ShowFeedback(judgement);
            ClearTelegraph();

            // 玩家快要贏：首次跨過門檻時給視覺提示，並開始拉長／隨機化步距。
            if (!decisiveMode && step < pattern.Count - 1 && score >= npc.winThreshold - activeCloseToWinScoreMargin)
                EnterDecisiveMode();

            // 依目前分數決定到下一步的間隔（快要贏 → 2~8 拍或 8／12 分格隨機）。
            if (step < pattern.Count - 1)
            {
                if (BirdDuelRhythmChart.ShouldSuspenseAfterStep(activeCdId, step))
                {
                    yield return RunMorningPrayerSuspense(beatCursor);
                    beatCursor += BirdDuelRhythmChart.MorningPrayerSuspenseBeats;
                }

                double nextGapBeats = ResolveNextStepBeatDelta(step);
                beatCursor += nextGapBeats;
                double nextHitDsp = BeatFractionDsp(beatCursor);

                if (TryPickCourtMarchFakeScareHit(
                        hitDsp, nextHitDsp, nextGapBeats, pattern.Count - 1 - step, out double fakeHitDsp))
                {
                    yield return RunCourtMarchFakeScare(fakeHitDsp);
                }
            }
        }

        clockRunning = false;
        ShowResult();
        matchRoutine = null;
    }

    /// <summary>進入決勝拍：視覺提示並收緊判定窗口／預告時間。</summary>
    private void EnterDecisiveMode()
    {
        decisiveMode = true;
        ApplyDecisiveDifficulty();

        if (subtitleText != null)
        {
            subtitleText.text = UsesDecisiveTripletGrid() || rhythmGrid == BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth
                ? "決勝拍！3 連音——抓準節奏！"
                : "決勝拍！節奏開始變化——抓準鼓點！";
            subtitleText.color = ColorDecisive;
        }
        if (beatPad != null)
            beatPad.color = ColorDecisive;
        if (shrinkIndicatorImage != null)
        {
            Color c = ColorDecisive;
            c.a = shrinkIndicatorImage.color.a; // 透明度仍由 UpdateBeatVisual 控制
            shrinkIndicatorImage.color = c;
        }
        PulseBeatPad();
        lastTickSubdivision = -1;
    }

    private void ApplyDecisiveDifficulty()
    {
        perfectWindow = BasePerfectWindow * rhythmProfile.BasePerfectWindowMul * rhythmProfile.DecisivePerfectWindowMul;
        goodWindow = BaseGoodWindow * rhythmProfile.BaseGoodWindowMul * rhythmProfile.DecisiveGoodWindowMul;
        telegraphLeadBeats = BaseTelegraphLeadBeats * rhythmProfile.BaseTelegraphLeadMul * rhythmProfile.DecisiveTelegraphLeadMul;
    }

    private void ResetJudgementWindows()
    {
        perfectWindow = BasePerfectWindow * rhythmProfile.BasePerfectWindowMul;
        goodWindow = BaseGoodWindow * rhythmProfile.BaseGoodWindowMul;
        telegraphLeadBeats = BaseTelegraphLeadBeats * rhythmProfile.BaseTelegraphLeadMul;
    }

    /// <summary>到下一判定步的音樂拍長。晨禱用固定步距表；庭訓決勝段以 3 連音格隨機。</summary>
    private double ResolveNextStepBeatDelta(int stepIndex)
    {
        bool closeToWin = score >= npc.winThreshold - activeCloseToWinScoreMargin;
        if (!closeToWin)
        {
            if (BirdDuelRhythmChart.TryGetNormalStepGap(activeCdId, stepIndex, out double chartGap))
                return chartGap;
            return activeNormalBeatsPerStep;
        }

        if (rhythmGrid == BirdDuelRhythmSync.GridMode.AlternatingEighthTwelfth)
        {
            int triplets = UnityEngine.Random.Range(activeDecisiveMinTriplets, activeDecisiveMaxTriplets + 1);
            return triplets * TripletUnitBeats;
        }

        return UnityEngine.Random.Range(CloseToWinMinBeats, CloseToWinMaxBeats + 1);
    }

    /// <summary>庭訓決勝 3 連音長休息：假 scare 大光圈收束（無判定），每局至少 1 次。</summary>
    private bool TryPickCourtMarchFakeScareHit(
        double lastHitDsp,
        double nextHitDsp,
        double gapBeats,
        int stepsUntilLast,
        out double fakeHitDsp)
    {
        fakeHitDsp = 0d;
        if (!UsesDecisiveTripletGrid() || !IsCourtMarchCd())
            return false;

        float leadSec = FakeScareLeadBeats * SecondsPerBeat;
        double restStart = lastHitDsp + goodWindow;
        double restEnd = nextHitDsp - telegraphLeadBeats * SecondsPerBeat;
        if (restEnd - restStart < leadSec + 0.12d)
            return false;

        double gapTriplets = gapBeats / TripletUnitBeats;
        bool mustPlay = courtMarchFakeScaresRemaining > 0;
        bool longRest = gapTriplets >= FakeScareMinGapTriplets;
        bool lastChance = mustPlay && stepsUntilLast <= 2;
        if (!longRest && !lastChance)
            return false;
        if (!mustPlay && UnityEngine.Random.value > 0.28f)
            return false;

        double earliest = restStart + leadSec;
        double latest = restEnd - 0.06d;
        if (latest <= earliest)
            fakeHitDsp = (restStart + restEnd) * 0.5d;
        else
            fakeHitDsp = restStart + (restEnd - restStart) * UnityEngine.Random.Range(0.34f, 0.56f);

        fakeHitDsp = System.Math.Max(earliest, System.Math.Min(latest, fakeHitDsp));
        if (mustPlay)
            courtMarchFakeScaresRemaining--;
        return true;
    }

    private IEnumerator RunCourtMarchFakeScare(double fakeHitDsp)
    {
        fakeScareLeadSeconds = FakeScareLeadBeats * SecondsPerBeat;
        double startDsp = fakeHitDsp - fakeScareLeadSeconds;

        while (AudioSettings.dspTime < startDsp)
            yield return null;

        fakeScareActive = true;
        fakeScareHitDsp = fakeHitDsp;
        fakeScareImpactFired = false;
        fakeScareEdgeScale = ResolveFakeScareEdgeScale();
        if (fakeScareRing != null)
        {
            fakeScareRing.gameObject.SetActive(true);
            fakeScareRing.localScale = Vector3.one * fakeScareEdgeScale;
            fakeScareAnimScale = fakeScareEdgeScale;
        }
        if (fakeScareRingImage != null)
        {
            Color c = ColorFakeScareRing;
            c.a = 0.92f;
            fakeScareRingImage.color = c;
            fakeScareAnimAlpha = 0.92f;
        }

        while (AudioSettings.dspTime < fakeHitDsp + 0.14d)
            yield return null;

        fakeScareActive = false;
        fakeScareHitDsp = 0d;
        HideFakeScareRing();
        if (beatPad != null && decisiveMode)
            beatPad.color = ColorDecisive;
    }

    private void HideFakeScareRing()
    {
        if (fakeScareRing == null) return;
        fakeScareRing.gameObject.SetActive(false);
        fakeScareRing.localScale = Vector3.one;
        fakeScareAnimScale = -1f;
        fakeScareAnimAlpha = -1f;
        if (fakeScareRingImage != null)
            fakeScareRingImage.color = ColorFakeScareRing;
    }

    private float ResolveFakeScareEdgeScale()
    {
        Canvas canvas = beatFxCanvas != null ? beatFxCanvas : gameCanvas;
        if (canvas == null || beatPad == null)
            return 14f;

        RectTransform canvasRt = canvas.transform as RectTransform;
        Vector2 beatPos = beatPad.rectTransform.anchoredPosition;
        float halfW = canvasRt.rect.width * 0.5f;
        float halfH = canvasRt.rect.height * 0.5f;
        Vector2[] corners =
        {
            new Vector2(-halfW, -halfH),
            new Vector2(-halfW, halfH),
            new Vector2(halfW, halfH),
            new Vector2(halfW, -halfH),
        };

        float maxHalfSide = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            float dx = Mathf.Abs(beatPos.x - corners[i].x);
            float dy = Mathf.Abs(beatPos.y - corners[i].y);
            maxHalfSide = Mathf.Max(maxHalfSide, Mathf.Max(dx, dy));
        }

        return maxHalfSide * 2.08f / (FakeScareFrameSize * 0.5f);
    }

    private static Sprite GetFakeScareFrameSprite()
    {
        if (fakeScareRingSprite != null)
            return fakeScareRingSprite;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float outerHalf = center - 1.5f;
        float innerHalf = outerHalf - FakeScareFrameThicknessPx;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float ax = Mathf.Abs(x + 0.5f - center);
                float ay = Mathf.Abs(y + 0.5f - center);
                bool inOuter = ax <= outerHalf && ay <= outerHalf;
                bool inInner = ax <= innerHalf && ay <= innerHalf;
                tex.SetPixel(x, y, inOuter && !inInner ? Color.white : Color.clear);
            }
        }

        tex.Apply(false, true);
        fakeScareRingSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        return fakeScareRingSprite;
    }

    private bool IsCourtMarchCd() =>
        string.Equals(activeCdId, "court_march", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>晨禱段末屏息：tick 繼續、無判定，製造「被窺視」的緊張感。</summary>
    private IEnumerator RunMorningPrayerSuspense(double beatCursor)
    {
        if (subtitleText != null)
        {
            subtitleText.text = "聆聽…";
            subtitleText.color = ColorDecisive;
        }

        double endDsp = BeatFractionDsp(beatCursor + BirdDuelRhythmChart.MorningPrayerSuspenseBeats);
        while (AudioSettings.dspTime < endDsp)
            yield return null;

        if (subtitleText != null && !decisiveMode)
        {
            subtitleText.text = DefaultSubtitle();
            subtitleText.color = ColorSubtitle;
        }
    }

    private void ResetState()
    {
        score = 0;
        insight = 0;
        passUsed = 0;
        letNestThroughCount = 0;
        insightPeekActive = false;
        beatWindowOpen = false;
        inputWindowOpen = false;
        inputLocked = false;
        clockRunning = false;
        currentBeatDsp = 0d;
        lastTickBeat = -1;
        lastTickSubdivision = -1;
        decisiveMode = false;
        ResetJudgementWindows();
        courtMarchFakeScaresRemaining = IsCourtMarchCd() ? CourtMarchFakeScaresPerMatch : 0;
        fakeScareActive = false;
        fakeScareHitDsp = 0d;
        fakeScareImpactFired = false;
        HideFakeScareRing();
        StopBeatPadShake();
        InvalidateBeatFxAnimCache();
        shrinkIdle = false; // 強制下一幀重設閒置 transform
        if (subtitleText != null) subtitleText.color = ColorSubtitle;
        if (beatPad != null) beatPad.color = ColorBeatPadIdle;
        if (shrinkIndicatorImage != null) shrinkIndicatorImage.color = ColorShrinkIdle;
        if (feedbackText != null) feedbackText.text = "";
        if (peekHintText != null) peekHintText.text = "";
        UpdateBars();
        ClearTelegraph();
    }

    private void ApplyJudgement(BirdGesture opp, BirdGesture? input, BirdBeatJudgement judgement)
    {
        score = Mathf.Max(0, score + judgement.scoreDelta);
        insight += judgement.insightDelta;
        if (judgement.letNestThrough) letNestThroughCount++;
        if (input.HasValue && input.Value == BirdGesture.Pass) passUsed++;

        // 成功築巢（反制振翅）→ 取得看破，下一拍提早揭露對手鳥勢。
        if (judgement.isBestCounter && opp == BirdGesture.Wing)
            insightPeekActive = true;

        if (judgement.outcome == BirdBeatOutcome.Perfect)
            PlayPerfectBeatShake();

        PlayHitOutcomeSfx(judgement.outcome);

        UpdateBars();
    }

    private void ShowResult()
    {
        clockRunning = false;
        SetButtonsInteractable(false);
        ClearTelegraph();
        if (feedbackText != null) feedbackText.text = "";

        BirdDuelResult result = BirdDuelCore.ResolveResult(score, npc.winThreshold, npc.drawThreshold);
        int tier = BirdDuelCore.ResolveIntelTier(insight, letNestThroughCount, passUsed, npc.passLimit);
        lastResult = result;
        lastIntelText = npc.ResolveIntelText(tier);

        if (resultTitle != null)
        {
            switch (result)
            {
                case BirdDuelResult.Win: resultTitle.text = "鬥鳥勝利"; resultTitle.color = ColorScoreFill; break;
                case BirdDuelResult.Draw: resultTitle.text = "平手"; resultTitle.color = ColorNest; break;
                default: resultTitle.text = "再練一次"; resultTitle.color = ColorPass; break;
            }
        }

        if (resultLine != null)
            resultLine.text = $"{npc.ResolveResultLine(result)}\n分數 {score} / {scoreBarMax}　看破 {insight}";
        if (resultIntel != null)
            resultIntel.text = lastIntelText;

        SetBeatFxVisible(false);
        SetGestureButtonsVisible(false);
        if (resultOverlayRoot != null)
            resultOverlayRoot.SetActive(true);
    }

    private string DefaultSubtitle()
    {
        const string baseLine = "看對手鳥勢，在鼓點按正確反制：啄擊←巢　振翅←啄　築巢←翅";
        if (preBattleMode && PreBattleDuelContext.HasHiddenTier)
            return baseLine + "　｜　勝出鬥鳥可挑戰魔王級";
        return baseLine;
    }

    private void OnLeavePressed()
    {
        if (preBattleMode)
        {
            BeginBonusDraft();
            return;
        }
        ReturnToHall();
    }
}

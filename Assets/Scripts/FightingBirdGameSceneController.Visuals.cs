using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- visuals

    private void ShowTelegraph(BirdGesture opp, bool peek)
    {
        if (opponentPad != null) opponentPad.color = GestureColor(opp);
        if (opponentGlyph != null) opponentGlyph.text = BirdDuelCore.ShortName(opp);

        if (peekHintText == null) return;
        if (peek)
            peekHintText.text = $"看破：對手準備 {BirdDuelCore.DisplayName(opp)}";
        else
            peekHintText.text = "";
    }

    private void ClearTelegraph()
    {
        if (opponentPad != null) opponentPad.color = ColorIdle;
        if (opponentGlyph != null) opponentGlyph.text = "?";
        if (peekHintText != null) peekHintText.text = "";
    }

    private void ShowFeedback(BirdBeatJudgement judgement)
    {
        if (feedbackText == null) return;

        string main;
        Color color;
        switch (judgement.outcome)
        {
            case BirdBeatOutcome.Perfect: main = "Perfect"; color = ColorScoreFill; break;
            case BirdBeatOutcome.Good: main = "Good"; color = BirdDuelUiColors.GoodFeedback; break;
            case BirdBeatOutcome.Guard: main = "Guard"; color = ColorPass; break;
            default: main = "Miss"; color = ColorPeck; break;
        }

        if (judgement.scoreDelta > 0) main += $" +{judgement.scoreDelta}";
        if (judgement.insightDelta > 0) main += "　看破 +1";

        feedbackText.text = main;
        feedbackText.color = color;
    }

    private void UpdateBeatVisual()
    {
        if (fakeScareActive && fakeScareHitDsp > 0d && fakeScareRing != null)
        {
            float lead = Mathf.Max(0.0001f, fakeScareLeadSeconds);
            float remaining = (float)(fakeScareHitDsp - AudioSettings.dspTime);
            float linearT = Mathf.Clamp01(remaining / lead);
            float easedT = linearT * linearT * linearT; // 後段加速收束，營造「猛然」感
            float scale = Mathf.Lerp(FakeScareMinScale, fakeScareEdgeScale, easedT);
            ApplyUniformScale(fakeScareRing, scale, ref fakeScareAnimScale);

            if (fakeScareRingImage != null)
            {
                float alpha = Mathf.Lerp(0.18f, 0.98f, Mathf.Pow(linearT, 0.55f));
                ApplyImageAlpha(fakeScareRingImage, alpha, ref fakeScareAnimAlpha);
            }

            if (remaining <= 0f && !fakeScareImpactFired)
            {
                fakeScareImpactFired = true;
                PulseBeatPad();
                PlayTick(true);
                if (beatPad != null)
                    beatPad.color = Color.white;
            }

            return;
        }

        if (shrinkIndicator == null) return;

        if (beatWindowOpen && currentBeatDsp > 0d)
        {
            float lead = telegraphLeadBeats * SecondsPerBeat;
            float remaining = (float)(currentBeatDsp - AudioSettings.dspTime);
            float t = Mathf.Clamp01(remaining / Mathf.Max(0.0001f, lead)); // 1 → 0
            float scale = Mathf.Lerp(1f, 2.4f, t);
            ApplyUniformScale(shrinkIndicator, scale, ref shrinkAnimScale);
            shrinkIdle = false;
            if (shrinkIndicatorImage != null)
            {
                float alpha = Mathf.Lerp(0.55f, 0.12f, t);
                ApplyImageAlpha(shrinkIndicatorImage, alpha, ref shrinkAnimAlpha);
            }
        }
        else if (!shrinkIdle)
        {
            // 只在進入閒置時寫一次，避免每幀變更 transform 而反覆觸發 Canvas 重新批次。
            ApplyUniformScale(shrinkIndicator, 2.4f, ref shrinkAnimScale);
            shrinkIdle = true;
            if (shrinkIndicatorImage != null && decisiveMode)
            {
                shrinkAnimAlpha = -1f;
                Color c = ColorDecisive;
                c.a = 0.20f;
                shrinkIndicatorImage.color = c;
            }
            else if (shrinkIndicatorImage != null)
            {
                shrinkAnimAlpha = -1f;
                shrinkIndicatorImage.color = ColorShrinkIdle;
            }
            if (beatPad != null && decisiveMode)
                beatPad.color = ColorDecisive;
        }
    }

    private static void ApplyUniformScale(RectTransform rt, float scale, ref float cached)
    {
        if (rt == null || Mathf.Approximately(cached, scale)) return;
        cached = scale;
        rt.localScale = new Vector3(scale, scale, 1f);
    }

    private static void ApplyImageAlpha(Image img, float alpha, ref float cached)
    {
        if (img == null || Mathf.Approximately(cached, alpha)) return;
        cached = alpha;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private void InvalidateBeatFxAnimCache()
    {
        shrinkAnimScale = -1f;
        shrinkAnimAlpha = -1f;
        fakeScareAnimScale = -1f;
        fakeScareAnimAlpha = -1f;
    }

    private void SetBeatFxVisible(bool visible)
    {
        if (beatFxCanvas != null)
            beatFxCanvas.gameObject.SetActive(visible);
    }

    private void SetGestureButtonsVisible(bool visible)
    {
        foreach (KeyValuePair<BirdGesture, Image> pair in buttonImages)
        {
            if (pair.Value != null)
                pair.Value.gameObject.SetActive(visible);
        }
    }

    private void PulseBeatPad()
    {
        if (beatPad != null) StartCoroutine(PulseRoutine(beatPad.rectTransform));
    }

    private void PlayPerfectBeatShake()
    {
        if (beatPad == null) return;
        if (beatPadShakeRoutine != null)
            StopCoroutine(beatPadShakeRoutine);
        beatPadShakeRoutine = StartCoroutine(PerfectBeatShakeRoutine(beatPad.rectTransform));
    }

    private void StopBeatPadShake()
    {
        if (beatPadShakeRoutine != null)
        {
            StopCoroutine(beatPadShakeRoutine);
            beatPadShakeRoutine = null;
        }

        if (beatPad != null)
            beatPad.rectTransform.anchoredPosition = BeatPadAnchor;
    }

    private IEnumerator PerfectBeatShakeRoutine(RectTransform rt)
    {
        if (rt == null) yield break;

        Vector2 origin = BeatPadAnchor;
        rt.anchoredPosition = origin;
        float t = 0f;
        while (t < PerfectBeatShakeDuration && rt != null)
        {
            t += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(t / PerfectBeatShakeDuration);
            float phase = t * 52f;
            float x = Mathf.Sin(phase) * PerfectBeatShakeStrength * damper;
            float y = Mathf.Cos(phase * 1.15f) * PerfectBeatShakeStrength * 0.38f * damper;
            rt.anchoredPosition = origin + new Vector2(x, y);
            yield return null;
        }

        if (rt != null)
            rt.anchoredPosition = origin;
        beatPadShakeRoutine = null;
    }

    private IEnumerator PulseRoutine(RectTransform rt)
    {
        if (rt == null) yield break;
        float duration = 0.18f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f + 0.25f * (1f - t / duration);
            rt.localScale = new Vector3(k, k, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private void FlashButton(BirdGesture gesture)
    {
        if (buttonImages.TryGetValue(gesture, out Image img) && img != null)
            StartCoroutine(FlashRoutine(img));
    }

    private IEnumerator FlashRoutine(Image img)
    {
        if (img == null) yield break;
        Color baseColor = img.color;
        img.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (img != null) img.color = baseColor;
    }

    private void UpdateBars()
    {
        if (scoreFill != null)
            scoreFill.anchorMax = new Vector2(Mathf.Clamp01((float)score / scoreBarMax), 1f);
        if (insightFill != null)
            insightFill.anchorMax = new Vector2(Mathf.Clamp01((float)insight / insightBarMax), 1f);
        if (scoreLabel != null)
            scoreLabel.text = m13StoryMode ? $"同頻 {score}" : $"分數 {score}";
        if (insightLabel != null)
            insightLabel.text = m13StoryMode ? $"聽波 {insight}" : $"看破 {insight}";
    }

    private static Color GestureColor(BirdGesture gesture)
    {
        switch (gesture)
        {
            case BirdGesture.Peck: return ColorPeck;
            case BirdGesture.Wing: return ColorWing;
            case BirdGesture.Nest: return ColorNest;
            default: return ColorPass;
        }
    }
}

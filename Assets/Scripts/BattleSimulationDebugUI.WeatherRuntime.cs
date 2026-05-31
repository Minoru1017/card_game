using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private TextMeshProUGUI weatherBadgeTmp;
    private RectTransform weatherForecastOverlayRt;
    private CanvasGroup weatherForecastOverlayCg;
    private TextMeshProUGUI weatherForecastTitleTmp;
    private TextMeshProUGUI weatherForecastBodyTmp;
    private Coroutine weatherForecastOverlayRoutine;
    private TextMeshProUGUI weatherHintTmp;
    private TextMeshProUGUI weatherRemainTmp;
    private Button activeWeatherEffectButton;
    private RectTransform activeWeatherEffectPanelRt;
    private TextMeshProUGUI activeWeatherEffectPanelSummaryTmp;
    private TextMeshProUGUI activeWeatherEffectPanelTextTmp;
    private RectTransform weatherScreenFxRoot;
    private RectTransform weatherFireRainFxRt;
    private RectTransform weatherHolyLightFxRt;
    private RectTransform weatherFogFxRt;
    private RectTransform weatherGaleFxRt;
    private readonly List<Image> weatherHolyLightEdgeImgs = new List<Image>();
    private readonly List<float> weatherHolyLightEdgeBaseAlphas = new List<float>();
    private Image weatherHolyLightTopEdgeImg;
    private Image weatherHolyLightBottomEdgeImg;
    private Image weatherHolyLightLeftEdgeImg;
    private Image weatherHolyLightRightEdgeImg;
    private readonly List<Image> weatherHolyLightDustImages = new List<Image>();
    private readonly List<RectTransform> weatherHolyLightDustRects = new List<RectTransform>();
    private readonly List<float> weatherHolyLightDustSpeeds = new List<float>();
    private readonly List<float> weatherHolyLightDustPhases = new List<float>();
    private readonly List<Color> weatherHolyLightDustBaseColors = new List<Color>();
    private readonly List<RectTransform> weatherFireRainStreaks = new List<RectTransform>();
    private readonly List<float> weatherFireRainStreakSpeeds = new List<float>();
    private readonly List<Image> weatherFireRainStreakImages = new List<Image>();
    private readonly List<float> weatherFireRainStreakPhases = new List<float>();
    private readonly List<RectTransform> weatherFogBands = new List<RectTransform>();
    private readonly List<Image> weatherFogBandImages = new List<Image>();
    private readonly List<float> weatherFogBandSpeeds = new List<float>();
    private readonly List<float> weatherFogBandPhases = new List<float>();
    private readonly List<Image> weatherFogEdgeImgs = new List<Image>();
    private readonly List<float> weatherFogEdgeBaseAlphas = new List<float>();
    private readonly List<RectTransform> weatherFogFoamDots = new List<RectTransform>();
    private readonly List<Image> weatherFogFoamDotImages = new List<Image>();
    private readonly List<float> weatherFogFoamDotSpeeds = new List<float>();
    private RectTransform weatherFogBoatRt;
    private Image weatherFogBoatHullImg;
    private float weatherFogBoatBaseY;
    private readonly List<Image> weatherGaleNightEdgeImgs = new List<Image>();
    private readonly List<float> weatherGaleNightEdgeBaseAlphas = new List<float>();
    private readonly List<RectTransform> weatherGaleLeafRects = new List<RectTransform>();
    private readonly List<Image> weatherGaleLeafImgs = new List<Image>();
    private readonly List<float> weatherGaleLeafSpeeds = new List<float>();
    private readonly List<float> weatherGaleLeafPhases = new List<float>();
    private readonly List<RectTransform> weatherGaleWindLineRects = new List<RectTransform>();
    private readonly List<Image> weatherGaleWindLineImgs = new List<Image>();
    private readonly List<float> weatherGaleWindLineSpeeds = new List<float>();

    private void OnWeatherForecastStarted(string weatherName, string effectText)
    {
        if (weatherForecastOverlayRt == null) return;
        if (weatherForecastOverlayRoutine != null)
        {
            StopCoroutine(weatherForecastOverlayRoutine);
            weatherForecastOverlayRoutine = null;
        }
        if (activeWeatherEffectButton != null) activeWeatherEffectButton.gameObject.SetActive(true);
        weatherForecastOverlayRoutine = StartCoroutine(CoShowWeatherForecastOverlay(weatherName, effectText));
    }

    private string SafeWeatherText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("：", ": ")
            .Replace("｜", "|")
            .Replace("，", ", ")
            .Replace("。", "")
            .Replace("（", "(")
            .Replace("）", ")")
            .Replace("＋", "+")
            .Replace("－", "-");
    }

    private void OnWeatherForecastFinished()
    {
        if (weatherForecastOverlayRoutine != null)
        {
            StopCoroutine(weatherForecastOverlayRoutine);
            weatherForecastOverlayRoutine = null;
        }
        if (weatherForecastOverlayRt == null) return;
        weatherForecastOverlayRoutine = StartCoroutine(CoHideWeatherForecastOverlay());
    }

    private IEnumerator CoShowWeatherForecastOverlay(string weatherName, string effectText)
    {
        if (weatherForecastOverlayRt == null) yield break;
        weatherForecastOverlayRt.gameObject.SetActive(true);
        weatherForecastOverlayRt.SetAsLastSibling();
        int remain = battleManager != null ? battleManager.GetCurrentWeatherRemainingRoundsForUi() : 0;
        string finalTitle = "天氣預報: " + weatherName + (remain > 0 ? " (剩餘 " + remain + " 回合)" : string.Empty);
        if (weatherForecastTitleTmp != null)
            weatherForecastTitleTmp.text = SafeWeatherText("天氣預報抽選中...");
        if (weatherForecastBodyTmp != null)
            weatherForecastBodyTmp.text = SafeWeatherText(
                "即將生效的卡牌效果\n" +
                effectText +
                "\n\n下一次預報提示: " + (battleManager != null ? battleManager.GetNextWeatherForecastHintForUi() : "-"));
        if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = 0f;

        float t = 0f;
        const float fade = 0.18f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fade);
            if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = p;
            yield return null;
        }
        if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = 1f;
        yield return StartCoroutine(CoPlayWeatherForecastRollAnimation(weatherName, finalTitle));
        weatherForecastOverlayRoutine = null;
    }

    private IEnumerator CoPlayWeatherForecastRollAnimation(string weatherName, string finalTitle)
    {
        if (weatherForecastTitleTmp == null)
            yield break;
        string[] pool = BattleWeatherLabels.ForecastRollPool;
        int start = Random.Range(0, pool.Length);
        float elapsed = 0f;
        const float total = 0.92f;
        const float step = 0.07f;
        float tick = 0f;
        int idx = start;
        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;
            tick += Time.unscaledDeltaTime;
            if (tick >= step)
            {
                tick = 0f;
                weatherForecastTitleTmp.text = SafeWeatherText("天氣預報抽選: " + pool[idx]);
                idx = (idx + 1) % pool.Length;
            }
            yield return null;
        }
        weatherForecastTitleTmp.text = SafeWeatherText(finalTitle);
        if (!string.IsNullOrEmpty(weatherName))
            yield return new WaitForSecondsRealtime(0.08f);
    }

    private IEnumerator CoHideWeatherForecastOverlay()
    {
        if (weatherForecastOverlayRt == null) yield break;
        if (!weatherForecastOverlayRt.gameObject.activeSelf)
        {
            weatherForecastOverlayRoutine = null;
            yield break;
        }

        float t = 0f;
        const float fade = 0.18f;
        float startAlpha = weatherForecastOverlayCg != null ? weatherForecastOverlayCg.alpha : 1f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fade);
            if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = Mathf.Lerp(startAlpha, 0f, p);
            yield return null;
        }

        if (weatherForecastOverlayCg != null) weatherForecastOverlayCg.alpha = 0f;
        weatherForecastOverlayRt.gameObject.SetActive(false);
        weatherForecastOverlayRoutine = null;
    }

    private void ToggleActiveWeatherEffectPanel()
    {
        if (activeWeatherEffectPanelRt == null) return;
        bool next = !activeWeatherEffectPanelRt.gameObject.activeSelf;
        activeWeatherEffectPanelRt.gameObject.SetActive(next);
        if (!next) return;
        activeWeatherEffectPanelRt.SetAsLastSibling();
        RefreshActiveWeatherEffectPanelText();
    }

    private void RefreshActiveWeatherEffectPanelText()
    {
        if (battleManager == null) return;
        string weather = battleManager.GetCurrentWeatherLabelForUi();
        int remain = battleManager.GetCurrentWeatherRemainingRoundsForUi();
        string status = remain > 0 ? "作用中" : "未作用";
        string queued = battleManager.GetQueuedWeatherForNextRoundLabelForUi();
        string detail = BuildAllWeatherHudText(weather);
        if (activeWeatherEffectPanelSummaryTmp != null)
        {
            activeWeatherEffectPanelSummaryTmp.text = SafeWeatherText(
                "<size=132%><b>場地效果摘要</b></size>\n\n" +
                "<size=108%>名稱</size>\n<size=118%><b>" + weather + "</b></size>\n\n" +
                "<size=108%>剩餘回合</size>\n<size=118%><b>" + remain + "</b></size>\n\n" +
                "<size=108%>狀態</size>\n<size=118%><b>" + status + "</b></size>");
        }
        if (activeWeatherEffectPanelTextTmp != null)
        {
            activeWeatherEffectPanelTextTmp.text = SafeWeatherText(
                "<size=132%><b>場地效果總覽</b></size>\n\n" +
                "<size=108%><b>下一回合將套用</b>: " + queued + "</size>\n\n" +
                "<size=100%>" + detail + "</size>");
        }
    }

    private string BuildAllWeatherHudText(string activeWeatherLabel)
    {
        return
            FormatWeatherLine(BattleWeatherLabels.EmberHearth, activeWeatherLabel == BattleWeatherLabels.EmberHearth, "回合結束雙方場上怪獸各受 5 點傷害") + "\n\n" +
            FormatWeatherLine(BattleWeatherLabels.WarmLamplight, activeWeatherLabel == BattleWeatherLabels.WarmLamplight, "所有治療效果增加 10") + "\n\n" +
            FormatWeatherLine(BattleWeatherLabels.TrainingMist, activeWeatherLabel == BattleWeatherLabels.TrainingMist, "直接攻擊英雄傷害減少 50%") + "\n\n" +
            FormatWeatherLine(BattleWeatherLabels.HallDraft, activeWeatherLabel == BattleWeatherLabels.HallDraft, "雙方首張法術效果增加 20%");
    }

    private static string FormatWeatherLine(string name, bool active, string effect)
    {
        if (active)
            return "<color=#8F5A16><b>● " + name + " (生效中)</b></color>\n<color=#3A2A1A><b>" + effect + "</b></color>";
        return "<color=#4A5A39><b>○ " + name + "</b></color>\n<color=#2F271E>" + effect + "</color>";
    }

    private void UpdateWeatherScreenEffects()
    {
        if (battleManager == null || weatherScreenFxRoot == null) return;

        bool active = battleManager.GetCurrentWeatherRemainingRoundsForUi() > 0;
        bool showFire = active && battleManager.IsCurrentWeatherFxActive(BattleWeatherKind.FireRain);
        bool showHoly = active && battleManager.IsCurrentWeatherFxActive(BattleWeatherKind.HolyLight);
        bool showFog = active && battleManager.IsCurrentWeatherFxActive(BattleWeatherKind.Fog);
        bool showGale = active && battleManager.IsCurrentWeatherFxActive(BattleWeatherKind.Gale);

        if (weatherFireRainFxRt != null) weatherFireRainFxRt.gameObject.SetActive(showFire);
        if (weatherHolyLightFxRt != null) weatherHolyLightFxRt.gameObject.SetActive(showHoly);
        if (weatherFogFxRt != null) weatherFogFxRt.gameObject.SetActive(showFog);
        if (weatherGaleFxRt != null) weatherGaleFxRt.gameObject.SetActive(showGale);
        if (!showFire && !showHoly && !showFog && !showGale) return;

        float dt = Time.unscaledDeltaTime;
        if (showFire) AnimateFireRainFx(dt);
        if (showHoly) AnimateHolyLightFx();
        if (showFog) AnimateFogFx(dt);
        if (showGale) AnimateGaleFx(dt);
    }

    private void AnimateHolyLightFx()
    {
        float edgePulseFactor = 0.84f + Mathf.Sin(Time.unscaledTime * 0.82f) * 0.14f;
        for (int i = 0; i < weatherHolyLightEdgeImgs.Count; i++)
        {
            Image edgeImg = weatherHolyLightEdgeImgs[i];
            if (edgeImg == null) continue;
            float baseAlpha = i < weatherHolyLightEdgeBaseAlphas.Count ? weatherHolyLightEdgeBaseAlphas[i] : 0.08f;
            Color ec = edgeImg.color;
            ec.a = Mathf.Clamp01(baseAlpha * edgePulseFactor);
            edgeImg.color = ec;
        }

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < weatherHolyLightDustRects.Count; i++)
        {
            RectTransform dustRt = weatherHolyLightDustRects[i];
            if (dustRt == null) continue;
            float sp = i < weatherHolyLightDustSpeeds.Count ? weatherHolyLightDustSpeeds[i] : 13f;
            float phase = i < weatherHolyLightDustPhases.Count ? weatherHolyLightDustPhases[i] : 0f;
            Vector2 p = dustRt.anchoredPosition;
            p.y += sp * dt;
            p.x += Mathf.Sin(Time.unscaledTime * 1.15f + phase) * 14f * dt;
            if (p.y > 330f)
            {
                p.y = Random.Range(-260f, -120f);
                p.x = Random.Range(-420f, 420f);
                if (i < weatherHolyLightDustPhases.Count)
                    weatherHolyLightDustPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            dustRt.anchoredPosition = p;

            if (i < weatherHolyLightDustImages.Count)
            {
                Image dustImg = weatherHolyLightDustImages[i];
                if (dustImg != null)
                {
                    Color baseColor = i < weatherHolyLightDustBaseColors.Count
                        ? weatherHolyLightDustBaseColors[i]
                        : dustImg.color;
                    float tintPulse = 0.5f + Mathf.Sin(Time.unscaledTime * 0.95f + phase) * 0.5f;
                    Color shimmer = Color.Lerp(
                        baseColor,
                        BattleFxColors.WithAlpha(BattleFxColors.WeatherHolyDustGoldRgb, baseColor.a),
                        0.14f * tintPulse);
                    Color dc = shimmer;
                    dc.a = Mathf.Clamp01(baseColor.a + Mathf.Sin(Time.unscaledTime * 1.35f + phase) * 0.045f);
                    dustImg.color = dc;
                }
            }
            float scalePulse = 0.92f + Mathf.Sin(Time.unscaledTime * 1.05f + phase) * 0.12f;
            dustRt.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }
    }

    private void AnimateFireRainFx(float dt)
    {
        if (weatherFireRainFxRt == null || weatherFireRainStreaks.Count == 0) return;
        float h = Mathf.Max(300f, weatherFireRainFxRt.rect.height);
        float w = Mathf.Max(500f, weatherFireRainFxRt.rect.width);
        float top = h * 0.5f + 80f;
        float bottom = -h * 0.5f - 80f;
        float left = -w * 0.5f - 50f;
        float right = w * 0.5f + 50f;
        for (int i = 0; i < weatherFireRainStreaks.Count; i++)
        {
            RectTransform rt = weatherFireRainStreaks[i];
            float sp = weatherFireRainStreakSpeeds[i];
            float phase = i < weatherFireRainStreakPhases.Count ? weatherFireRainStreakPhases[i] : 0f;
            Vector2 p = rt.anchoredPosition;
            p.x -= sp * 0.18f * dt;
            p.x += Mathf.Sin(Time.unscaledTime * 2.4f + phase) * 14f * dt;
            p.y -= sp * 0.72f * dt;
            if (p.y < bottom || p.x < left)
            {
                p.y = top + Random.Range(0f, 120f);
                p.x = Random.Range(left + 120f, right);
                if (i < weatherFireRainStreakPhases.Count)
                    weatherFireRainStreakPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            rt.anchoredPosition = p;

            if (i < weatherFireRainStreakImages.Count)
            {
                Image img = weatherFireRainStreakImages[i];
                if (img != null)
                {
                    float pulse = 0.2f + Mathf.Sin(Time.unscaledTime * 7.5f + phase) * 0.08f;
                    Color c = img.color;
                    c.a = Mathf.Clamp01(pulse);
                    img.color = c;
                }
            }
        }
    }

    private void AnimateFogFx(float dt)
    {
        float edgePulse = 0.88f + Mathf.Sin(Time.unscaledTime * 0.72f) * 0.14f;
        for (int i = 0; i < weatherFogEdgeImgs.Count; i++)
        {
            Image edge = weatherFogEdgeImgs[i];
            if (edge == null) continue;
            float baseAlpha = i < weatherFogEdgeBaseAlphas.Count ? weatherFogEdgeBaseAlphas[i] : 0.08f;
            Color c = edge.color;
            c.a = Mathf.Clamp01(baseAlpha * edgePulse);
            edge.color = c;
        }

        if (weatherFogFxRt == null) return;
        float w = Mathf.Max(560f, weatherFogFxRt.rect.width);
        float left = -w * 0.5f - 180f;
        float right = w * 0.5f + 180f;
        for (int i = 0; i < weatherFogBands.Count; i++)
        {
            RectTransform bandRt = weatherFogBands[i];
            if (bandRt == null) continue;
            float sp = i < weatherFogBandSpeeds.Count ? weatherFogBandSpeeds[i] : 14f;
            float phase = i < weatherFogBandPhases.Count ? weatherFogBandPhases[i] : 0f;
            Vector2 p = bandRt.anchoredPosition;
            p.x -= sp * dt;
            p.y += Mathf.Sin(Time.unscaledTime * 0.95f + phase) * 8f * dt;
            if (p.x < left)
            {
                p.x = right + Random.Range(-20f, 40f);
                p.y = Random.Range(-300f, 300f);
                if (i < weatherFogBandPhases.Count)
                    weatherFogBandPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            bandRt.anchoredPosition = p;

            if (i < weatherFogBandImages.Count)
            {
                Image img = weatherFogBandImages[i];
                if (img != null)
                {
                    Color bc = img.color;
                    bc.a = Mathf.Clamp01(0.09f + Mathf.Sin(Time.unscaledTime * 1.2f + phase) * 0.04f);
                    img.color = bc;
                }
            }
        }

        for (int i = 0; i < weatherFogFoamDots.Count; i++)
        {
            RectTransform foamRt = weatherFogFoamDots[i];
            if (foamRt == null) continue;
            float sp = i < weatherFogFoamDotSpeeds.Count ? weatherFogFoamDotSpeeds[i] : 30f;
            Vector2 p = foamRt.anchoredPosition;
            p.x -= sp * dt;
            p.y += Mathf.Sin(Time.unscaledTime * 2.1f + i * 0.8f) * 10f * dt;
            if (p.x < left)
            {
                p.x = right + Random.Range(0f, 60f);
                p.y = Random.Range(-240f, 240f);
            }
            foamRt.anchoredPosition = p;
            if (i < weatherFogFoamDotImages.Count)
            {
                Image img = weatherFogFoamDotImages[i];
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Clamp01(0.10f + Mathf.Sin(Time.unscaledTime * 2.6f + i * 0.65f) * 0.05f);
                    img.color = c;
                }
            }
        }

        if (weatherFogBoatRt != null)
        {
            Vector2 bp = weatherFogBoatRt.anchoredPosition;
            bp.x -= 22f * dt;
            bp.y = weatherFogBoatBaseY + Mathf.Sin(Time.unscaledTime * 1.35f) * 7.5f;
            if (bp.x < left + 120f) bp.x = right - 140f;
            weatherFogBoatRt.anchoredPosition = bp;
            weatherFogBoatRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.8f) * 5.5f);
            if (weatherFogBoatHullImg != null)
            {
                Color hc = weatherFogBoatHullImg.color;
                hc.a = Mathf.Clamp01(0.28f + Mathf.Sin(Time.unscaledTime * 1.1f) * 0.06f);
                weatherFogBoatHullImg.color = hc;
            }
        }
    }

    private void AnimateGaleFx(float dt)
    {
        float edgePulse = 0.92f + Mathf.Sin(Time.unscaledTime * 1.25f) * 0.15f;
        for (int i = 0; i < weatherGaleNightEdgeImgs.Count; i++)
        {
            Image img = weatherGaleNightEdgeImgs[i];
            if (img == null) continue;
            float baseAlpha = i < weatherGaleNightEdgeBaseAlphas.Count ? weatherGaleNightEdgeBaseAlphas[i] : 0.08f;
            Color c = img.color;
            c.a = Mathf.Clamp01(baseAlpha * edgePulse);
            img.color = c;
        }

        if (weatherGaleFxRt == null) return;
        float w = Mathf.Max(620f, weatherGaleFxRt.rect.width);
        float h = Mathf.Max(380f, weatherGaleFxRt.rect.height);
        float left = -w * 0.5f - 180f;
        float right = w * 0.5f + 180f;
        float top = h * 0.5f + 120f;
        float bottom = -h * 0.5f - 120f;

        for (int i = 0; i < weatherGaleLeafRects.Count; i++)
        {
            RectTransform rt = weatherGaleLeafRects[i];
            if (rt == null) continue;
            float sp = i < weatherGaleLeafSpeeds.Count ? weatherGaleLeafSpeeds[i] : 90f;
            float phase = i < weatherGaleLeafPhases.Count ? weatherGaleLeafPhases[i] : 0f;
            Vector2 p = rt.anchoredPosition;
            p.x -= sp * dt;
            p.y += Mathf.Sin(Time.unscaledTime * 4.2f + phase) * 20f * dt;
            if (p.x < left || p.y < bottom || p.y > top)
            {
                p.x = right + Random.Range(20f, 120f);
                p.y = Random.Range(bottom + 60f, top - 30f);
                if (i < weatherGaleLeafPhases.Count) weatherGaleLeafPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            rt.anchoredPosition = p;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 8f + phase) * 30f);
            if (i < weatherGaleLeafImgs.Count)
            {
                Image img = weatherGaleLeafImgs[i];
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Clamp01(0.28f + Mathf.Sin(Time.unscaledTime * 3.1f + phase) * 0.12f);
                    img.color = c;
                }
            }
        }

        for (int i = 0; i < weatherGaleWindLineRects.Count; i++)
        {
            RectTransform rt = weatherGaleWindLineRects[i];
            if (rt == null) continue;
            float sp = i < weatherGaleWindLineSpeeds.Count ? weatherGaleWindLineSpeeds[i] : 140f;
            Vector2 p = rt.anchoredPosition;
            p.x -= sp * dt;
            if (p.x < left) p.x = right + Random.Range(40f, 120f);
            rt.anchoredPosition = p;
            if (i < weatherGaleWindLineImgs.Count)
            {
                Image img = weatherGaleWindLineImgs[i];
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Clamp01(0.1f + Mathf.Sin(Time.unscaledTime * 5f + i * 0.4f) * 0.05f);
                    img.color = c;
                }
            }
        }
    }
}

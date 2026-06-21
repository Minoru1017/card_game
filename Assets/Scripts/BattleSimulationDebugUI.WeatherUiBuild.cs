using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI : MonoBehaviour
{
    // --- WeatherUiBuild ---
    private void CreateWeatherScreenFx(Transform parent)
    {
        GameObject fxRootObj = new GameObject("WeatherScreenFxRoot", typeof(RectTransform), typeof(CanvasGroup));
        fxRootObj.transform.SetParent(parent, false);
        weatherScreenFxRoot = fxRootObj.GetComponent<RectTransform>();
        weatherScreenFxRoot.anchorMin = Vector2.zero;
        weatherScreenFxRoot.anchorMax = Vector2.one;
        weatherScreenFxRoot.offsetMin = Vector2.zero;
        weatherScreenFxRoot.offsetMax = Vector2.zero;
        CanvasGroup fxCg = fxRootObj.GetComponent<CanvasGroup>();
        fxCg.blocksRaycasts = false;
        fxCg.interactable = false;

        weatherFireRainFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherEmberHearthFx", BattleFxColors.WeatherFireBase);
        weatherHolyLightFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherWarmLamplightFx", BattleFxColors.WeatherHolyBase);
        weatherFogFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherTrainingMistFx", BattleFxColors.WeatherFogBase);
        weatherGaleFxRt = CreateWeatherFxLayer(weatherScreenFxRoot, "WeatherHallDraftFx", BattleFxColors.WeatherGaleBase);

        if (weatherHolyLightFxRt != null)
        {
            weatherHolyLightEdgeImgs.Clear();
            weatherHolyLightEdgeBaseAlphas.Clear();
            weatherHolyLightDustImages.Clear();
            weatherHolyLightDustRects.Clear();
            weatherHolyLightDustSpeeds.Clear();
            weatherHolyLightDustPhases.Clear();
            weatherHolyLightDustBaseColors.Clear();
            weatherHolyLightTopEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightTopEdgeOuter", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 170f), 0.11f);
            weatherHolyLightBottomEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightBottomEdgeOuter", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 150f), 0.09f);
            weatherHolyLightLeftEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightLeftEdgeOuter", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(126f, 0f), 0.08f);
            weatherHolyLightRightEdgeImg = CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightRightEdgeOuter", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(126f, 0f), 0.08f);
            AddHolyLightEdgeLayer(weatherHolyLightTopEdgeImg, 0.11f);
            AddHolyLightEdgeLayer(weatherHolyLightBottomEdgeImg, 0.09f);
            AddHolyLightEdgeLayer(weatherHolyLightLeftEdgeImg, 0.08f);
            AddHolyLightEdgeLayer(weatherHolyLightRightEdgeImg, 0.08f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightTopEdgeMid", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 114f), 0.06f), 0.06f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightBottomEdgeMid", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 98f), 0.05f), 0.05f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightLeftEdgeMid", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(88f, 0f), 0.043f), 0.043f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightRightEdgeMid", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(88f, 0f), 0.043f), 0.043f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightTopEdgeInner", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 66f), 0.02f), 0.02f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightBottomEdgeInner", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 56f), 0.016f), 0.016f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightLeftEdgeInner", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(54f, 0f), 0.015f), 0.015f);
            AddHolyLightEdgeLayer(CreateHolyLightEdge(weatherHolyLightFxRt, "HolyLightRightEdgeInner", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(54f, 0f), 0.015f), 0.015f);

            for (int i = 0; i < 16; i++)
            {
                GameObject dustObj = new GameObject("LamplightMote_" + i, typeof(RectTransform), typeof(Image));
                dustObj.transform.SetParent(weatherHolyLightFxRt, false);
                RectTransform dustRt = dustObj.GetComponent<RectTransform>();
                dustRt.anchorMin = new Vector2(0.5f, 0.5f);
                dustRt.anchorMax = new Vector2(0.5f, 0.5f);
                dustRt.pivot = new Vector2(0.5f, 0.5f);
                float size = Random.Range(4.5f, 10f);
                dustRt.sizeDelta = new Vector2(size, size);
                dustRt.anchoredPosition = new Vector2(Random.Range(-420f, 420f), Random.Range(-260f, 300f));
                Image dustImg = dustObj.GetComponent<Image>();
                dustImg.sprite = GetUnitWhiteSprite();
                Color baseColor = BattleFxColors.RandomHolyDust();
                dustImg.color = baseColor;
                dustImg.raycastTarget = false;
                weatherHolyLightDustRects.Add(dustRt);
                weatherHolyLightDustImages.Add(dustImg);
                weatherHolyLightDustSpeeds.Add(Random.Range(13f, 25f));
                weatherHolyLightDustPhases.Add(Random.Range(0f, Mathf.PI * 2f));
                weatherHolyLightDustBaseColors.Add(baseColor);
            }
        }

        if (weatherFogFxRt != null)
        {
            weatherFogBands.Clear();
            weatherFogBandImages.Clear();
            weatherFogBandSpeeds.Clear();
            weatherFogBandPhases.Clear();
            weatherFogEdgeImgs.Clear();
            weatherFogEdgeBaseAlphas.Clear();
            weatherFogFoamDots.Clear();
            weatherFogFoamDotImages.Clear();
            weatherFogFoamDotSpeeds.Clear();
            weatherFogBoatRt = null;
            weatherFogBoatHullImg = null;
            weatherFogBoatBaseY = -120f;

            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistTopOuter", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 150f), 0.1f), 0.1f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistBottomOuter", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 220f), 0.18f), 0.18f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistLeftOuter", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(124f, 0f), 0.11f), 0.11f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistRightOuter", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(124f, 0f), 0.11f), 0.11f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistBottomInner", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 128f), 0.11f), 0.11f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistSideInnerL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(84f, 0f), 0.075f), 0.075f);
            AddFogEdgeLayer(CreateHolyLightEdge(weatherFogFxRt, "MistSideInnerR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(84f, 0f), 0.075f), 0.075f);

            for (int i = 0; i < 7; i++)
            {
                GameObject fogBandObj = new GameObject("TrainingMistWisp_" + i, typeof(RectTransform), typeof(Image));
                fogBandObj.transform.SetParent(weatherFogFxRt, false);
                RectTransform fogBandRt = fogBandObj.GetComponent<RectTransform>();
                fogBandRt.anchorMin = new Vector2(0.5f, 0.5f);
                fogBandRt.anchorMax = new Vector2(0.5f, 0.5f);
                fogBandRt.pivot = new Vector2(0.5f, 0.5f);
                fogBandRt.sizeDelta = new Vector2(Random.Range(560f, 980f), Random.Range(70f, 140f));
                fogBandRt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(-300f, 300f));
                Image fogBandImg = fogBandObj.GetComponent<Image>();
                fogBandImg.sprite = GetUnitWhiteSprite();
                fogBandImg.color = BattleFxColors.RandomFogWave();
                fogBandImg.raycastTarget = false;
                weatherFogBands.Add(fogBandRt);
                weatherFogBandImages.Add(fogBandImg);
                weatherFogBandSpeeds.Add(Random.Range(30f, 56f));
                weatherFogBandPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }

            for (int i = 0; i < 18; i++)
            {
                GameObject foamDotObj = new GameObject("TrainingMistSpeck_" + i, typeof(RectTransform), typeof(Image));
                foamDotObj.transform.SetParent(weatherFogFxRt, false);
                RectTransform foamRt = foamDotObj.GetComponent<RectTransform>();
                foamRt.anchorMin = new Vector2(0.5f, 0.5f);
                foamRt.anchorMax = new Vector2(0.5f, 0.5f);
                foamRt.pivot = new Vector2(0.5f, 0.5f);
                float size = Random.Range(3.5f, 8f);
                foamRt.sizeDelta = new Vector2(size, size);
                foamRt.anchoredPosition = new Vector2(Random.Range(-560f, 560f), Random.Range(-240f, 240f));
                Image foamImg = foamDotObj.GetComponent<Image>();
                foamImg.sprite = GetUnitWhiteSprite();
                foamImg.color = BattleFxColors.RandomFogFoam();
                foamImg.raycastTarget = false;
                weatherFogFoamDots.Add(foamRt);
                weatherFogFoamDotImages.Add(foamImg);
                weatherFogFoamDotSpeeds.Add(Random.Range(36f, 78f));
            }

            weatherFogBoatRt = null;
            weatherFogBoatHullImg = null;
            for (int i = 0; i < 5; i++)
            {
                GameObject pillarObj = new GameObject("TrainingHallPillar_" + i, typeof(RectTransform), typeof(Image));
                pillarObj.transform.SetParent(weatherFogFxRt, false);
                RectTransform pillarRt = pillarObj.GetComponent<RectTransform>();
                pillarRt.anchorMin = new Vector2(0.5f, 0.5f);
                pillarRt.anchorMax = new Vector2(0.5f, 0.5f);
                pillarRt.pivot = new Vector2(0.5f, 0f);
                float pw = Random.Range(12f, 22f);
                float ph = Random.Range(100f, 180f);
                pillarRt.sizeDelta = new Vector2(pw, ph);
                pillarRt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(-200f, -60f));
                Image pillarImg = pillarObj.GetComponent<Image>();
                pillarImg.sprite = GetUnitWhiteSprite();
                pillarImg.color = BattleFxColors.WithAlpha(BattleFxColors.WeatherFogSilhouetteRgb, Random.Range(0.16f, 0.26f));
                pillarImg.raycastTarget = false;
            }
        }

        if (weatherGaleFxRt != null)
        {
            weatherGaleNightEdgeImgs.Clear();
            weatherGaleNightEdgeBaseAlphas.Clear();
            weatherGaleLeafRects.Clear();
            weatherGaleLeafImgs.Clear();
            weatherGaleLeafSpeeds.Clear();
            weatherGaleLeafPhases.Clear();
            weatherGaleWindLineRects.Clear();
            weatherGaleWindLineImgs.Clear();
            weatherGaleWindLineSpeeds.Clear();

            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 220f), 0.16f), 0.16f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 160f), 0.12f), 0.12f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(180f, 0f), 0.15f), 0.15f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(180f, 0f), 0.15f), 0.15f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightTopMid", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 140f), 0.1f), 0.1f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightBottomMid", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 110f), 0.08f), 0.08f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightVignetteL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(120f, 0f), 0.11f), 0.11f);
            AddGaleNightLayer(CreateHolyLightEdge(weatherGaleFxRt, "GaleNightVignetteR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(120f, 0f), 0.11f), 0.11f);

            for (int i = 0; i < 14; i++)
            {
                GameObject leafObj = new GameObject("HallDraftPaper_" + i, typeof(RectTransform), typeof(Image));
                leafObj.transform.SetParent(weatherGaleFxRt, false);
                RectTransform leafRt = leafObj.GetComponent<RectTransform>();
                leafRt.anchorMin = new Vector2(0.5f, 0.5f);
                leafRt.anchorMax = new Vector2(0.5f, 0.5f);
                leafRt.pivot = new Vector2(0.5f, 0.5f);
                float s = Random.Range(6f, 11f);
                leafRt.sizeDelta = new Vector2(s * 1.85f, s * 0.5f);
                leafRt.anchoredPosition = new Vector2(Random.Range(-420f, 760f), Random.Range(-240f, 300f));
                Image leafImg = leafObj.GetComponent<Image>();
                leafImg.sprite = GetUnitWhiteSprite();
                Color leafColor = BattleFxColors.RandomHallDraftPaper();
                leafImg.color = leafColor;
                leafImg.raycastTarget = false;
                weatherGaleLeafRects.Add(leafRt);
                weatherGaleLeafImgs.Add(leafImg);
                weatherGaleLeafSpeeds.Add(Random.Range(90f, 180f));
                weatherGaleLeafPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }

            for (int i = 0; i < 11; i++)
            {
                GameObject windObj = new GameObject("HallDraftBreeze_" + i, typeof(RectTransform), typeof(Image));
                windObj.transform.SetParent(weatherGaleFxRt, false);
                RectTransform windRt = windObj.GetComponent<RectTransform>();
                windRt.anchorMin = new Vector2(0.5f, 0.5f);
                windRt.anchorMax = new Vector2(0.5f, 0.5f);
                windRt.pivot = new Vector2(0.5f, 0.5f);
                windRt.sizeDelta = new Vector2(Random.Range(90f, 170f), Random.Range(2.4f, 4.2f));
                windRt.anchoredPosition = new Vector2(Random.Range(-520f, 760f), Random.Range(-260f, 280f));
                windRt.rotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 6f));
                Image windImg = windObj.GetComponent<Image>();
                windImg.sprite = GetUnitWhiteSprite();
                windImg.color = BattleFxColors.RandomGaleWind();
                windImg.raycastTarget = false;
                weatherGaleWindLineRects.Add(windRt);
                weatherGaleWindLineImgs.Add(windImg);
                weatherGaleWindLineSpeeds.Add(Random.Range(130f, 240f));
            }
        }

        if (weatherFireRainFxRt != null)
        {
            weatherFireRainStreaks.Clear();
            weatherFireRainStreakSpeeds.Clear();
            weatherFireRainStreakImages.Clear();
            weatherFireRainStreakPhases.Clear();
            for (int i = 0; i < 22; i++)
            {
                GameObject dropObj = new GameObject("HearthEmber_" + i, typeof(RectTransform), typeof(Image));
                dropObj.transform.SetParent(weatherFireRainFxRt, false);
                RectTransform dropRt = dropObj.GetComponent<RectTransform>();
                dropRt.anchorMin = new Vector2(0.5f, 0.5f);
                dropRt.anchorMax = new Vector2(0.5f, 0.5f);
                dropRt.pivot = new Vector2(0.5f, 0.5f);
                dropRt.sizeDelta = new Vector2(Random.Range(3f, 7f), Random.Range(10f, 24f));
                dropRt.rotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
                dropRt.anchoredPosition = new Vector2(Random.Range(-960f, 960f), Random.Range(-560f, 560f));
                Image dropImg = dropObj.GetComponent<Image>();
                dropImg.sprite = GetUnitWhiteSprite();
                dropImg.color = BattleFxColors.RandomFireDrop();
                dropImg.raycastTarget = false;
                weatherFireRainStreaks.Add(dropRt);
                weatherFireRainStreakSpeeds.Add(Random.Range(95f, 185f));
                weatherFireRainStreakImages.Add(dropImg);
                weatherFireRainStreakPhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }
        }

        if (weatherFireRainFxRt != null) weatherFireRainFxRt.gameObject.SetActive(false);
        if (weatherHolyLightFxRt != null) weatherHolyLightFxRt.gameObject.SetActive(false);
        if (weatherFogFxRt != null) weatherFogFxRt.gameObject.SetActive(false);
        if (weatherGaleFxRt != null) weatherGaleFxRt.gameObject.SetActive(false);
    }

    private RectTransform CreateWeatherFxLayer(Transform parent, string name, Color tint)
    {
        GameObject layerObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        layerObj.transform.SetParent(parent, false);
        RectTransform layerRt = layerObj.GetComponent<RectTransform>();
        layerRt.anchorMin = Vector2.zero;
        layerRt.anchorMax = Vector2.one;
        layerRt.offsetMin = Vector2.zero;
        layerRt.offsetMax = Vector2.zero;
        Image layerImg = layerObj.GetComponent<Image>();
        layerImg.sprite = GetUnitWhiteSprite();
        layerImg.color = tint;
        layerImg.raycastTarget = false;
        return layerRt;
    }

    private Image CreateHolyLightEdge(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        float alpha)
    {
        GameObject edgeObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        edgeObj.transform.SetParent(parent, false);
        RectTransform edgeRt = edgeObj.GetComponent<RectTransform>();
        edgeRt.anchorMin = anchorMin;
        edgeRt.anchorMax = anchorMax;
        edgeRt.pivot = pivot;
        edgeRt.anchoredPosition = anchoredPos;
        edgeRt.sizeDelta = sizeDelta;
        Image edgeImg = edgeObj.GetComponent<Image>();
        edgeImg.sprite = GetUnitWhiteSprite();
        edgeImg.color = BattleFxColors.HolyEdge(alpha);
        edgeImg.raycastTarget = false;
        return edgeImg;
    }

    private void AddHolyLightEdgeLayer(Image img, float baseAlpha)
    {
        if (img == null) return;
        weatherHolyLightEdgeImgs.Add(img);
        weatherHolyLightEdgeBaseAlphas.Add(baseAlpha);
    }

    private void AddFogEdgeLayer(Image img, float baseAlpha)
    {
        if (img == null) return;
        img.color = BattleFxColors.FogEdge(baseAlpha);
        weatherFogEdgeImgs.Add(img);
        weatherFogEdgeBaseAlphas.Add(baseAlpha);
    }

    private void AddGaleNightLayer(Image img, float baseAlpha)
    {
        if (img == null) return;
        img.color = BattleFxColors.GaleNightEdge(baseAlpha);
        weatherGaleNightEdgeImgs.Add(img);
        weatherGaleNightEdgeBaseAlphas.Add(baseAlpha);
    }
}

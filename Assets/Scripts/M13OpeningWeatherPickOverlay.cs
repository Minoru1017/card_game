using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>S 評獎勵：冷爐迎測開局天氣三選一（第 4 回合首次預報）。</summary>
public static class M13OpeningWeatherPickOverlay
{
    public static void Show(Action<M13OpeningWeatherPick> onPick)
    {
        M13StoryOverlayHost.EnsureEventSystem();

        GameObject overlayRoot = M13StoryOverlayHost.CreateDimOverlay("M13OpeningWeatherPickOverlay");
        TMP_FontAsset font = ResolveFont();
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot.transform);

        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "S 分波手", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform, "開局天氣三選一", font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);
        BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            "冷爐前 3 回合無天氣。\n第 4 回合首次預報將使用你選的場地效果。",
            font,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            BirdDuelOverlayUiBuild.ComputeInfoCardBottom());

        AddWeatherButton(panel.transform, font, 0, BattleWeatherLabels.EmberHearth,
            M13OpeningWeatherPick.FireRain, onPick, overlayRoot);
        AddWeatherButton(panel.transform, font, 1, BattleWeatherLabels.WarmLamplight,
            M13OpeningWeatherPick.HolyLight, onPick, overlayRoot);
        AddWeatherButton(panel.transform, font, 2, BattleWeatherLabels.TrainingMist,
            M13OpeningWeatherPick.Fog, onPick, overlayRoot);
    }

    private static void AddWeatherButton(
        Transform panel,
        TMP_FontAsset font,
        int index,
        string label,
        M13OpeningWeatherPick weather,
        Action<M13OpeningWeatherPick> onPick,
        GameObject overlayRoot)
    {
        Button btn = BirdDuelOverlayUiBuild.CreateSecondaryButton(
            panel.transform, "Weather_" + index, label, font);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(btn.GetComponent<RectTransform>(), index);
        btn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(overlayRoot);
            onPick?.Invoke(weather);
        });
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset settings = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settings != null) return settings;
        UiFontLibrary library = UiFontLibrary.Instance;
        if (library != null && library.DefaultUiFont != null) return library.DefaultUiFont;
        return TMP_Settings.defaultFontAsset;
    }
}

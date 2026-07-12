using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-3 開場後分波鬥鳥進入選擇（§五：可「直接迎測」跳過）。</summary>
public static class M13BirdDuelEntryOverlay
{
    public static void Show(System.Action onPlay, System.Action onSkip)
    {
        M13StoryOverlayHost.EnsureEventSystem();

        GameObject overlayRoot = M13StoryOverlayHost.CreateDimOverlay("M13BirdDuelEntryOverlay");
        TMP_FontAsset font = ResolveFont();
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot.transform, "Panel");

        RectTransform headerRt = BirdDuelOverlayUiBuild.CreateHeaderBand(
            panel.transform, "迎潮實測", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform, "分波鬥鳥", font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            "林可姐：讓節奏與水流對齊。\n" +
            "左汊右汊各八拍，跟鼓點反制鳥勢。\n" +
            "S 評可三選一開局天氣，分波對決多抽 1 張。\n" +
            "不想玩可按「直接迎測」。",
            font,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            BirdDuelOverlayUiBuild.ComputeInfoCardBottom());

        Button playBtn = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform, "PlayBtn", "開始分波鬥鳥", font);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(playBtn.GetComponent<RectTransform>(), 0);
        playBtn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(overlayRoot);
            onPlay?.Invoke();
        });

        Button skipBtn = BirdDuelOverlayUiBuild.CreateSecondaryButton(
            panel.transform, "SkipBtn", "直接迎測", font);
        BirdDuelMobileOverlayLayout.PlaceStackedButton(skipBtn.GetComponent<RectTransform>(), 1);
        skipBtn.onClick.AddListener(() =>
        {
            UnityEngine.Object.Destroy(overlayRoot);
            onSkip?.Invoke();
        });

        Button backBtn = BirdDuelOverlayUiBuild.CreateGhostBackButton(headerRt, font);
        backBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(overlayRoot));
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset settings = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settings != null)
            return settings;

        UiFontLibrary library = UiFontLibrary.Instance;
        if (library != null && library.DefaultUiFont != null)
            return library.DefaultUiFont;

        return TMP_Settings.defaultFontAsset;
    }
}

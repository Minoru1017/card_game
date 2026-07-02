using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SettingsSceneController
{
    private const string SoundSettingsName = "Sound Settings";
    private const string SoundVolumeRowsName = "SoundVolumeRows";

    private GameObject soundSettingsRoot;
    private GameObject soundSettingsDetailBg;
    private bool soundSettingsTierOpen;
    private bool soundVolumeUiBuilt;

    private void CacheSoundSettingsRefs()
    {
        if (soundSettingsRoot == null)
            soundSettingsRoot = GameObject.Find(SoundSettingsName);

        if (soundSettingsRoot != null && soundSettingsDetailBg == null)
            soundSettingsDetailBg = FindDirectChild(soundSettingsRoot.transform, "BG");
    }

    private void WireSoundSettingsNavigation()
    {
        if (soundSettingsRoot != null)
            BindButton(ResolveNavHitArea(soundSettingsRoot), OnSoundSettingsNavClicked);
    }

    private void BuildSoundVolumeSliders()
    {
        CacheSoundSettingsRefs();
        if (soundSettingsDetailBg == null || soundVolumeUiBuilt)
            return;

        soundVolumeUiBuilt = true;
        Image detailBgImage = soundSettingsDetailBg.GetComponent<Image>();
        if (detailBgImage != null)
            detailBgImage.raycastTarget = false;

        Transform rowsRoot = soundSettingsDetailBg.transform.Find(SoundVolumeRowsName);
        if (rowsRoot == null)
        {
            GameObject rowsGo = new GameObject(SoundVolumeRowsName, typeof(RectTransform));
            rowsGo.transform.SetParent(soundSettingsDetailBg.transform, false);
            RectTransform rowsRt = rowsGo.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0f, 0f);
            rowsRt.anchorMax = new Vector2(1f, 1f);
            rowsRt.offsetMin = new Vector2(48f, 48f);
            rowsRt.offsetMax = new Vector2(-48f, -80f);
            rowsRoot = rowsGo.transform;
        }

        CreateSoundMasterToggleRow(rowsRoot);
        CreateSoundVolumeSliderRow(rowsRoot, "BGM", GameAudioUserSettings.GetBgmVolume, GameAudioUserSettings.SetBgmVolume, 1);
        CreateSoundVolumeSliderRow(rowsRoot, "NPC 語音", GameAudioUserSettings.GetNpcVoiceVolume, GameAudioUserSettings.SetNpcVoiceVolume, 2);
        CreateSoundVolumeSliderRow(
            rowsRoot,
            "按鈕音效",
            GameAudioUserSettings.GetButtonSfxVolume,
            v =>
            {
                GameAudioUserSettings.SetButtonSfxVolume(v);
                PreviewButtonSfx();
            },
            3);
        CreateSoundVolumeSliderRow(rowsRoot, "戰鬥音效", GameAudioUserSettings.GetBattleSfxVolume, GameAudioUserSettings.SetBattleSfxVolume, 4);
    }

    private void CreateSoundMasterToggleRow(Transform parent)
    {
        const float rowHeight = 88f;

        GameObject row = new GameObject("SoundRow_Master", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = Vector2.zero;
        rowRt.sizeDelta = new Vector2(0f, rowHeight);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(row.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.5f);
        labelRt.anchorMax = new Vector2(0f, 0.5f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.sizeDelta = new Vector2(220f, 48f);
        TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = "音量總開關";
        labelTmp.fontSize = 32f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = new Color(0.97f, 0.85f, 0.47f, 1f);
        labelTmp.raycastTarget = false;
        ApplySettingsLabelFont(labelTmp);

        GameObject toggleGo = new GameObject("MasterToggle", typeof(RectTransform), typeof(Toggle));
        toggleGo.transform.SetParent(row.transform, false);
        RectTransform toggleRt = toggleGo.GetComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(1f, 0.5f);
        toggleRt.anchorMax = new Vector2(1f, 0.5f);
        toggleRt.pivot = new Vector2(1f, 0.5f);
        toggleRt.anchoredPosition = new Vector2(0f, 0f);
        toggleRt.sizeDelta = new Vector2(120f, 48f);

        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(toggleGo.transform, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.18f, 0.2f, 0.26f, 1f);

        GameObject checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGo.transform.SetParent(toggleGo.transform, false);
        RectTransform checkRt = checkGo.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0f, 0.5f);
        checkRt.anchorMax = new Vector2(0f, 0.5f);
        checkRt.pivot = new Vector2(0f, 0.5f);
        checkRt.anchoredPosition = new Vector2(12f, 0f);
        checkRt.sizeDelta = new Vector2(36f, 36f);
        Image checkImg = checkGo.GetComponent<Image>();
        checkImg.color = new Color(0.2f, 0.75f, 0.45f, 1f);

        GameObject stateGo = new GameObject("StateLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        stateGo.transform.SetParent(toggleGo.transform, false);
        RectTransform stateRt = stateGo.GetComponent<RectTransform>();
        stateRt.anchorMin = new Vector2(0f, 0f);
        stateRt.anchorMax = new Vector2(1f, 1f);
        stateRt.offsetMin = new Vector2(56f, 0f);
        stateRt.offsetMax = new Vector2(-8f, 0f);
        TextMeshProUGUI stateTmp = stateGo.GetComponent<TextMeshProUGUI>();
        stateTmp.fontSize = 26f;
        stateTmp.alignment = TextAlignmentOptions.MidlineLeft;
        stateTmp.color = Color.white;
        stateTmp.raycastTarget = false;
        ApplySettingsLabelFont(stateTmp);

        Toggle toggle = toggleGo.GetComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = GameAudioUserSettings.IsMasterEnabled();
        UpdateSoundMasterToggleLabel(stateTmp, toggle.isOn);
        toggle.onValueChanged.AddListener(isOn =>
        {
            GameAudioUserSettings.SetMasterEnabled(isOn);
            UpdateSoundMasterToggleLabel(stateTmp, isOn);
            GameAudioUserSettings.RefreshActiveBgmVolumes();
            if (isOn)
                PreviewButtonSfx();
        });
    }

    private static void UpdateSoundMasterToggleLabel(TextMeshProUGUI stateTmp, bool enabled)
    {
        if (stateTmp != null)
            stateTmp.text = enabled ? "開啟" : "關閉";
    }

    private void CreateSoundVolumeSliderRow(
        Transform parent,
        string label,
        Func<float> readVolume,
        Action<float> writeVolume,
        int rowIndex)
    {
        const float rowHeight = 96f;
        const float rowGap = 16f;
        const float masterRowHeight = 88f;
        const float masterRowGap = 24f;
        float topY = -(masterRowHeight + masterRowGap) - rowIndex * (rowHeight + rowGap);

        GameObject row = new GameObject("SoundRow_" + rowIndex, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, topY);
        rowRt.sizeDelta = new Vector2(0f, rowHeight);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(row.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.5f);
        labelRt.anchorMax = new Vector2(0f, 0.5f);
        labelRt.pivot = new Vector2(0f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0f, 0f);
        labelRt.sizeDelta = new Vector2(220f, 48f);
        TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 30f;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;
        ApplySettingsLabelFont(labelTmp);

        GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 0.5f);
        sliderRt.anchorMax = new Vector2(1f, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(110f, 0f);
        sliderRt.sizeDelta = new Vector2(-250f, 36f);

        GameObject trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trackGo.transform.SetParent(sliderGo.transform, false);
        RectTransform trackRt = trackGo.GetComponent<RectTransform>();
        trackRt.anchorMin = Vector2.zero;
        trackRt.anchorMax = Vector2.one;
        trackRt.offsetMin = Vector2.zero;
        trackRt.offsetMax = Vector2.zero;
        Image trackImg = trackGo.GetComponent<Image>();
        trackImg.color = new Color(0.18f, 0.2f, 0.26f, 1f);

        GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(8f, 8f);
        fillAreaRt.offsetMax = new Vector2(-8f, -8f);

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillGo.GetComponent<Image>();
        fillImg.color = new Color(0.28f, 0.62f, 0.88f, 1f);

        GameObject handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGo.transform.SetParent(sliderGo.transform, false);
        RectTransform handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(8f, 0f);
        handleAreaRt.offsetMax = new Vector2(-8f, 0f);

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        RectTransform handleRt = handleGo.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(28f, 28f);
        Image handleImg = handleGo.GetComponent<Image>();
        handleImg.color = new Color(0.97f, 0.85f, 0.47f, 1f);

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = readVolume();
        slider.onValueChanged.AddListener(value =>
        {
            writeVolume(value);
            GameAudioUserSettings.RefreshActiveBgmVolumes();
        });
    }

    private void OnSoundSettingsNavClicked()
    {
        soundSettingsTierOpen = !soundSettingsTierOpen;
        ApplyTierVisibility();
    }

    private void ApplySoundSettingsTierVisibility()
    {
        SetActive(soundSettingsDetailBg, soundSettingsTierOpen);
        if (soundSettingsDetailBg != null && soundSettingsTierOpen)
            soundSettingsDetailBg.transform.SetAsLastSibling();
    }

    private static void PreviewButtonSfx()
    {
        AudioLibrary library = AudioLibrary.Instance;
        AudioClip clip = library != null ? library.MenuClickSfx : null;
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, Vector3.zero, GameAudioUserSettings.ScaleButtonSfx(1f));
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-3 岔路散策：穩流道／急流道（LEVEL_DESIGN_M-1-3.md §六）。</summary>
public sealed class M13RiverForkStrollOverlay : MonoBehaviour
{
    private const float PathButtonRowHeight = 104f;

    private static readonly Color SteadyColor = new Color(0.22f, 0.48f, 0.58f, 1f);
    private static readonly Color SteadyColorH = new Color(0.28f, 0.55f, 0.66f, 1f);
    private static readonly Color SteadyColorP = new Color(0.18f, 0.40f, 0.50f, 1f);
    private static readonly Color RapidColor = new Color(0.58f, 0.28f, 0.24f, 1f);
    private static readonly Color RapidColorH = new Color(0.66f, 0.34f, 0.28f, 1f);
    private static readonly Color RapidColorP = new Color(0.48f, 0.22f, 0.18f, 1f);

    private Action<M13RiverForkPathChoice> onChosen;
    private TMP_FontAsset font;

    public static void Show(Action<M13RiverForkPathChoice> onChosen)
    {
        M13StoryOverlayHost.EnsureEventSystem();
        GameObject overlayRoot = M13StoryOverlayHost.CreateDimOverlay("M13RiverForkStrollOverlay");
        M13RiverForkStrollOverlay overlay = overlayRoot.AddComponent<M13RiverForkStrollOverlay>();
        overlay.onChosen = onChosen;
        overlay.BuildUi(overlayRoot.transform);
    }

    private void BuildUi(Transform overlayRoot)
    {
        font = ResolveFont();

        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");
        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "河岔分波", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform,
            "岔路散策",
            font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        float bodyBottom = BirdDuelMobileOverlayLayout.ButtonAreaPadBottom
            + PathButtonRowHeight
            + BirdDuelMobileOverlayLayout.SectionGap;
        BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            "林可姐：河岔在前 選一條路走\n" +
            "穩流道像賽爾說的 先穩再迎測\n" +
            "急流道像阿潮 急著證明也要走完全程",
            font,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            bodyBottom);

        GameObject row = new GameObject("PathButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(panel.transform, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, BirdDuelMobileOverlayLayout.ButtonAreaPadBottom);
        rowRt.sizeDelta = new Vector2(
            -BirdDuelMobileOverlayLayout.ContentPadH * 2f,
            PathButtonRowHeight);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        Button steadyBtn = CreatePathButton(
            row.transform,
            "SteadyBtn",
            "穩流道\n像賽爾說的 先穩",
            SteadyColor,
            SteadyColorH,
            SteadyColorP);
        steadyBtn.onClick.AddListener(() => Choose(M13RiverForkPathChoice.Steady));

        Button rapidBtn = CreatePathButton(
            row.transform,
            "RapidBtn",
            "急流道\n像要證明的人",
            RapidColor,
            RapidColorH,
            RapidColorP);
        rapidBtn.onClick.AddListener(() => Choose(M13RiverForkPathChoice.Rapid));
    }

    private void Choose(M13RiverForkPathChoice path)
    {
        Action<M13RiverForkPathChoice> cb = onChosen;
        onChosen = null;
        Destroy(gameObject);
        cb?.Invoke(path);
    }

    private Button CreatePathButton(
        Transform parent,
        string name,
        string label,
        Color normal,
        Color highlighted,
        Color pressed)
    {
        Button btn = BirdDuelOverlayUiBuild.CreateButton(
            parent,
            name,
            label,
            normal,
            highlighted,
            pressed,
            Color.white,
            28f,
            font);

        LayoutElement layout = btn.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minHeight = PathButtonRowHeight;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.enableWordWrapping = true;
            tmp.lineSpacing = -4f;
        }

        return btn;
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

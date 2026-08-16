using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A-1 解封儀式（企劃 §A-1.3 幕 4）。</summary>
public sealed class SideQuestA1UnsealRitualOverlay : MonoBehaviour
{
    private enum Phase
    {
        Grandma1,
        Narration,
        Grandma2,
        Grandma3
    }

    private Action onFinished;
    private Phase phase = Phase.Grandma1;
    private TMP_FontAsset font;
    private TextMeshProUGUI speakerTmp;
    private TextMeshProUGUI bodyTmp;

    public static void Show(Action onFinished)
    {
        M13StoryOverlayHost.EnsureEventSystem();
        GameObject root = M13StoryOverlayHost.CreateDimOverlay("SideQuestA1UnsealRitualOverlay");
        SideQuestA1UnsealRitualOverlay overlay = root.AddComponent<SideQuestA1UnsealRitualOverlay>();
        overlay.onFinished = onFinished;
        overlay.BuildUi(root.transform);
        overlay.RefreshPhase();
        SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Unseal1);
    }

    private void BuildUi(Transform overlayRoot)
    {
        font = ResolveFont();
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");
        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "解封", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform,
            "潮根滷",
            font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        float bodyBottom = BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + 96f;
        bodyTmp = BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            string.Empty,
            font,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            bodyBottom);
        speakerTmp = CreateSpeakerLabel(bodyTmp.transform.parent, bodyBottom);

        Button done = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform,
            "DoneBtn",
            "繼續",
            font);
        done.onClick.AddListener(OnContinue);
    }

    private void OnContinue()
    {
        switch (phase)
        {
            case Phase.Grandma1:
                phase = Phase.Narration;
                SideQuestA1OverlayVoice.Stop();
                break;
            case Phase.Narration:
                phase = Phase.Grandma2;
                SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Unseal3);
                break;
            case Phase.Grandma2:
                phase = Phase.Grandma3;
                SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Unseal4);
                break;
            default:
                Finish();
                return;
        }

        RefreshPhase();
    }

    private void RefreshPhase()
    {
        switch (phase)
        {
            case Phase.Grandma1:
                speakerTmp.text = "草奶奶";
                bodyTmp.text = SideQuestA1PlotCopy.UnsealGrandma1;
                break;
            case Phase.Narration:
                speakerTmp.text = "旁白";
                bodyTmp.text = SideQuestA1PlotCopy.UnsealNarration;
                break;
            case Phase.Grandma2:
                speakerTmp.text = "草奶奶";
                bodyTmp.text = SideQuestA1PlotCopy.UnsealGrandma2;
                break;
            case Phase.Grandma3:
                speakerTmp.text = "草奶奶";
                bodyTmp.text = SideQuestA1PlotCopy.UnsealGrandma3;
                break;
        }
    }

    private void Finish()
    {
        Action cb = onFinished;
        onFinished = null;
        SideQuestA1OverlayVoice.Stop();
        Destroy(gameObject);
        cb?.Invoke();
    }

    private static TextMeshProUGUI CreateSpeakerLabel(Transform parent, float bodyBottom)
    {
        GameObject go = new GameObject("Speaker", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bodyBottom + 8f);
        rt.sizeDelta = new Vector2(760f, 34f);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 28f;
        tmp.color = new Color(0.97f, 0.85f, 0.47f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        BirdDuelOverlayUiBuild.ApplyFont(tmp, UiFontResolver.ResolveUiFont());
        return tmp;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = UiFontResolver.ResolveUiFont();
        return font != null
            ? font
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }
}

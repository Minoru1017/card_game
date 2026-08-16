using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A-1 登島短劇（企劃 §A-1.3 幕 2）。</summary>
public sealed class SideQuestA1IslandIntroOverlay : MonoBehaviour
{
    private enum Phase
    {
        Narration,
        GrandmaOpen,
        GrandmaOrder,
        Choice,
        SealedHint,
        Done
    }

    private Action<bool> onFinished;
    private Phase phase = Phase.Narration;
    private TMP_FontAsset font;
    private TextMeshProUGUI speakerTmp;
    private TextMeshProUGUI bodyTmp;
    private Button primaryButton;
    private Button secondaryButton;
    private int slot;

    public static void Show(int playerSlot, Action<bool> onFinished)
    {
        M13StoryOverlayHost.EnsureEventSystem();
        GameObject root = M13StoryOverlayHost.CreateDimOverlay("SideQuestA1IslandIntroOverlay");
        SideQuestA1IslandIntroOverlay overlay = root.AddComponent<SideQuestA1IslandIntroOverlay>();
        overlay.onFinished = onFinished;
        overlay.slot = playerSlot;
        overlay.BuildUi(root.transform);
        overlay.RefreshPhase();
        SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Island0);
    }

    private void BuildUi(Transform overlayRoot)
    {
        font = ResolveFont();
        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");
        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "潮間島", font);
        BirdDuelOverlayUiBuild.CreateTitle(
            panel.transform,
            "三畦",
            font,
            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);

        float bodyBottom = BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + 120f;
        bodyTmp = BirdDuelOverlayUiBuild.CreateInfoCard(
            panel.transform,
            string.Empty,
            font,
            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),
            bodyBottom);
        speakerTmp = CreateSpeakerLabel(bodyTmp.transform.parent, bodyBottom);

        primaryButton = BirdDuelOverlayUiBuild.CreatePrimaryButton(
            panel.transform,
            "PrimaryBtn",
            "繼續",
            font);
        primaryButton.onClick.AddListener(OnPrimaryClicked);

        secondaryButton = BirdDuelOverlayUiBuild.CreateSecondaryButton(
            panel.transform,
            "SecondaryBtn",
            "草奶奶代勞",
            font);
        secondaryButton.onClick.AddListener(() => Finish(false));
        LayoutButtons();
    }

    private void OnPrimaryClicked()
    {
        switch (phase)
        {
            case Phase.Narration:
                phase = Phase.GrandmaOpen;
                SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Island1);
                break;
            case Phase.GrandmaOpen:
                phase = Phase.GrandmaOrder;
                SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Island2);
                break;
            case Phase.GrandmaOrder:
                phase = Phase.Choice;
                break;
            case Phase.Choice:
                Finish(true);
                return;
            case Phase.SealedHint:
                phase = Phase.Done;
                Finish(true);
                return;
            default:
                Finish(true);
                return;
        }

        RefreshPhase();
    }

    private void RefreshPhase()
    {
        secondaryButton.gameObject.SetActive(phase == Phase.Choice);

        switch (phase)
        {
            case Phase.Narration:
                speakerTmp.text = "旁白";
                bodyTmp.text = SideQuestA1PlotCopy.IslandNarration;
                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "繼續";
                break;
            case Phase.GrandmaOpen:
                speakerTmp.text = "草奶奶";
                bodyTmp.text = SideQuestA1PlotCopy.IslandGrandmaOpen;
                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "繼續";
                break;
            case Phase.GrandmaOrder:
                speakerTmp.text = "草奶奶";
                bodyTmp.text = SideQuestA1PlotCopy.IslandGrandmaOrder;
                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "繼續";
                break;
            case Phase.Choice:
                speakerTmp.text = string.Empty;
                bodyTmp.text = SideQuestA1PlotCopy.HarborChoicePrompt;
                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "我試試";
                secondaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "您來";
                break;
            case Phase.SealedHint:
                speakerTmp.text = "草奶奶";
                bodyTmp.text = SideQuestA1PlotCopy.IslandSealedSpellHint;
                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "開始耕種";
                break;
        }
    }

    private void Finish(bool playFarm)
    {
        if (playFarm &&
            phase == Phase.Choice &&
            SideQuestA1ProgressState.IsSealedSpellReady(slot))
        {
            phase = Phase.SealedHint;
            SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.IslandSealedHint);
            RefreshPhase();
            return;
        }

        Action<bool> cb = onFinished;
        onFinished = null;
        Destroy(gameObject);
        cb?.Invoke(playFarm);
    }

    private void LayoutButtons()
    {
        RectTransform primaryRt = primaryButton.GetComponent<RectTransform>();
        primaryRt.anchorMin = new Vector2(0.52f, 0f);
        primaryRt.anchorMax = new Vector2(0.98f, 0f);
        primaryRt.pivot = new Vector2(0.5f, 0f);
        primaryRt.anchoredPosition = new Vector2(0f, BirdDuelMobileOverlayLayout.ButtonAreaPadBottom);
        primaryRt.sizeDelta = new Vector2(0f, 96f);

        RectTransform secondaryRt = secondaryButton.GetComponent<RectTransform>();
        secondaryRt.anchorMin = new Vector2(0.02f, 0f);
        secondaryRt.anchorMax = new Vector2(0.48f, 0f);
        secondaryRt.pivot = new Vector2(0.5f, 0f);
        secondaryRt.anchoredPosition = new Vector2(0f, BirdDuelMobileOverlayLayout.ButtonAreaPadBottom);
        secondaryRt.sizeDelta = new Vector2(0f, 96f);
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

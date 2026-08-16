using System;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



/// <summary>A-1 碼頭：舵叔短劇情（企劃發想.md §A-1.3 幕 1／5）。</summary>

public sealed class SideQuestA1HarborOverlay : MonoBehaviour

{

    private enum LaunchPhase

    {

        Line0,

        Line1,

        Choice,

        Line2

    }



    private Action<bool> onFinished;

    private Action onReturnFinished;

    private bool returnMode;

    private bool returnWithSeed;

    private TMP_FontAsset font;

    private LaunchPhase launchPhase = LaunchPhase.Line0;

    private TextMeshProUGUI bodyTmp;

    private Button primaryButton;

    private Button secondaryButton;



    public static void ShowLaunchDialogue(Action<bool> onFinished)

    {

        M13StoryOverlayHost.EnsureEventSystem();

        GameObject root = M13StoryOverlayHost.CreateDimOverlay("SideQuestA1HarborOverlay");

        SideQuestA1HarborOverlay overlay = root.AddComponent<SideQuestA1HarborOverlay>();

        overlay.onFinished = onFinished;

        overlay.BuildLaunchUi(root.transform);

        SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Harbor0);

    }



    public static void ShowReturnEpilogue(bool keptSeaPurslaneSeed, Action onFinished)

    {

        M13StoryOverlayHost.EnsureEventSystem();

        GameObject root = M13StoryOverlayHost.CreateDimOverlay("SideQuestA1ReturnOverlay");

        SideQuestA1HarborOverlay overlay = root.AddComponent<SideQuestA1HarborOverlay>();

        overlay.returnMode = true;

        overlay.returnWithSeed = keptSeaPurslaneSeed;

        overlay.onReturnFinished = onFinished;

        overlay.BuildReturnUi(root.transform);

        SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Return0);

    }



    private void BuildLaunchUi(Transform overlayRoot)

    {

        font = ResolveFont();

        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");

        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "港灣碼頭", font);

        BirdDuelOverlayUiBuild.CreateTitle(

            panel.transform,

            "潮間島",

            font,

            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);



        float bodyBottom = BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + 120f;

        bodyTmp = BirdDuelOverlayUiBuild.CreateInfoCard(

            panel.transform,

            string.Empty,

            font,

            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),

            bodyBottom);

        bodyTmp.text = "舵叔\n\n" + SideQuestA1PlotCopy.HarborLaunchLines[0];



        primaryButton = BirdDuelOverlayUiBuild.CreatePrimaryButton(

            panel.transform,

            "PrimaryBtn",

            "繼續",

            font);

        primaryButton.onClick.AddListener(OnLaunchPrimaryClicked);



        secondaryButton = BirdDuelOverlayUiBuild.CreateSecondaryButton(

            panel.transform,

            "SecondaryBtn",

            "改天",

            font);

        secondaryButton.onClick.AddListener(() => FinishLaunch(false));

        LayoutLaunchButtons();

        RefreshLaunchPhase();

    }



    private void OnLaunchPrimaryClicked()

    {

        switch (launchPhase)

        {

            case LaunchPhase.Line0:

                launchPhase = LaunchPhase.Line1;

                SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Harbor1);

                break;

            case LaunchPhase.Line1:

                launchPhase = LaunchPhase.Choice;

                break;

            case LaunchPhase.Choice:

                launchPhase = LaunchPhase.Line2;

                SideQuestA1OverlayVoice.Play(SideQuestA1PlotCopy.Voice.Harbor2);

                break;

            case LaunchPhase.Line2:

                FinishLaunch(true);

                return;

        }



        RefreshLaunchPhase();

    }



    private void RefreshLaunchPhase()

    {

        secondaryButton.gameObject.SetActive(launchPhase == LaunchPhase.Choice);



        switch (launchPhase)

        {

            case LaunchPhase.Line0:

                bodyTmp.text = "舵叔\n\n" + SideQuestA1PlotCopy.HarborLaunchLines[0];

                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "繼續";

                break;

            case LaunchPhase.Line1:

                bodyTmp.text = "舵叔\n\n" + SideQuestA1PlotCopy.HarborLaunchLines[1];

                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "繼續";

                break;

            case LaunchPhase.Choice:

                bodyTmp.text = "舵叔\n\n" + SideQuestA1PlotCopy.HarborChoicePrompt;

                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "去";

                secondaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "改天";

                break;

            case LaunchPhase.Line2:

                bodyTmp.text = "舵叔\n\n" + SideQuestA1PlotCopy.HarborLaunchLines[2];

                primaryButton.GetComponentInChildren<TextMeshProUGUI>().text = "出發";

                break;

        }

    }



    private void BuildReturnUi(Transform overlayRoot)

    {

        font = ResolveFont();

        GameObject panel = BirdDuelOverlayUiBuild.CreateMobilePanel(overlayRoot, "Panel");

        BirdDuelOverlayUiBuild.CreateHeaderBand(panel.transform, "回港", font);

        BirdDuelOverlayUiBuild.CreateTitle(

            panel.transform,

            "碼頭",

            font,

            BirdDuelMobileOverlayLayout.HeaderHeight + 8f);



        string text = SideQuestA1PlotCopy.ReturnDefault;

        if (returnWithSeed)

            text += "\n\n" + SideQuestA1PlotCopy.ReturnWithSeed;



        BirdDuelOverlayUiBuild.CreateInfoCard(

            panel.transform,

            "舵叔\n\n" + text,

            font,

            BirdDuelOverlayUiBuild.ComputeInfoCardTop(),

            BirdDuelMobileOverlayLayout.ButtonAreaPadBottom + 96f);



        Button done = BirdDuelOverlayUiBuild.CreatePrimaryButton(

            panel.transform,

            "DoneBtn",

            "回到地圖",

            font);

        done.onClick.AddListener(FinishReturn);

    }



    private void FinishLaunch(bool proceed)

    {

        Action<bool> cb = onFinished;

        onFinished = null;

        SideQuestA1OverlayVoice.Stop();

        Destroy(gameObject);

        cb?.Invoke(proceed);

    }



    private void FinishReturn()

    {

        Action cb = onReturnFinished;

        onReturnFinished = null;

        SideQuestA1OverlayVoice.Stop();

        Destroy(gameObject);

        cb?.Invoke();

    }



    private void LayoutLaunchButtons()

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



    private static TMP_FontAsset ResolveFont()

    {

        TMP_FontAsset font = UiFontResolver.ResolveUiFont();

        return font != null

            ? font

            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

    }

}



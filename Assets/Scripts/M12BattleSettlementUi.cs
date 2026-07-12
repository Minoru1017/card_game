using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-2 專用結算（A 段考／B 教會三張加練；戰技達標 + 發獎流程）。</summary>
public sealed class M12BattleSettlementUi : MonoBehaviour
{
    private const float PanelWidth = 640f;
    private const float PanelHeight = 480f;
    private const float TitleBandHeight = 88f;
    private const float FooterBandHeight = 112f;
    private const float BodyHorizontalPad = 36f;

    private BattleSimulationManager _manager;
    private Transform _canvasRoot;
    private TMP_FontAsset _font;
    private GameObject _overlayRoot;
    private GameObject _panelRoot;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _bodyText;
    private RectTransform _buttonRowRt;
    private bool _uiBuilt;
    private bool _showing;
    private bool _eventsBound;
    private Coroutine _showRoutine;

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsM12TrioMasteryBattle;

    public void Initialize(BattleSimulationManager manager, Transform canvasRoot, TMP_FontAsset font = null)
    {
        _manager = manager;
        _canvasRoot = canvasRoot;
        _font = font;
        if (_manager != null && !_eventsBound)
        {
            _manager.BattleEnded += OnBattleEnded;
            _eventsBound = true;
        }
    }

    private void OnDestroy()
    {
        if (_eventsBound && _manager != null)
            _manager.BattleEnded -= OnBattleEnded;
        _eventsBound = false;
    }

    private void OnBattleEnded(int result)
    {
        if (!IsActiveForCurrentBattle || BattleAutoSimPlugin.IsRunning || _showing)
            return;
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(CoShowSettlement(result));
    }

    private IEnumerator CoShowSettlement(int result)
    {
        _showing = true;
        yield return BattleSimulationDebugUI.CoWaitForVictoryPresentationIfNeeded(result);
        yield return null;
        yield return new WaitForEndOfFrame();

        HideCoach();
        EnsureUi();
        if (_overlayRoot == null)
        {
            _showing = false;
            _showRoutine = null;
            yield break;
        }

        if (ShouldRecordPhaseADefeat(result, out int slot))
        {
            M12SeawallPatrolProgressState.RecordPhaseADefeatAttempt(slot, out bool firstMemoUnlock);
            if (firstMemoUnlock)
            {
                bool memoDismissed = false;
                M12PhaseAExamMemoOverlay.Show(firstUnlockReveal: true, () => memoDismissed = true);
                while (!memoDismissed)
                    yield return null;
            }
        }

        ApplyContent(result);
        _overlayRoot.SetActive(true);
        _overlayRoot.transform.SetAsLastSibling();
        _showRoutine = null;
    }

    private static bool ShouldRecordPhaseADefeat(int result, out int slot)
    {
        slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (!BattleLaunchContext.IsM12TrioTutorialBattle)
            return false;

        bool won = result == 1;
        if (won && M12TrioMasteryBattleTracker.QueryAllTrioSkillsTriggered())
            return false;

        return true;
    }

    private static void HideCoach()
    {
        M12BattleCoachUi coach = Object.FindFirstObjectByType<M12BattleCoachUi>();
        coach?.HideForSettlement();
        M12BattleMissionBarUi missionBar = Object.FindFirstObjectByType<M12BattleMissionBarUi>();
        missionBar?.HideForSettlement();
    }

    private void ApplyContent(int result)
    {
        ClearButtons();
        ResetBodyPresentation();
        if (_bodyText != null)
            ApplyBodyRectLayout(_bodyText.rectTransform);
        bool won = result == 1;
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        bool phaseA = BattleLaunchContext.IsM12TrioTutorialBattle;

        if (phaseA)
        {
            if (won && M12TrioMasteryBattleTracker.QueryAllTrioSkillsTriggered())
            {
                _titleText.text = "御三家應用 · 通過";
                _bodyText.text = "本局三戰技皆已觸發\n\n接下來海牆散策後進入加練";
                CreateFooterButton("繼續散策", true, OnClickPhaseAContinue);
                return;
            }

            if (won)
            {
                _titleText.text = "勝利 · 戰技未達標";
                ApplyPhaseAIncompleteBody(won: true);
                AddPhaseAIncompleteFooterButtons(slot);
                return;
            }

            _titleText.text = "階段 A 落敗";
            ApplyPhaseAIncompleteBody(won: false);
            AddPhaseAIncompleteFooterButtons(slot);
            return;
        }

        if (won && M12SeawallPatrolProgressState.QueryCombinedTrioSatisfied(slot))
        {
            bool granted = M12ReligiousLineRewardService.TryGrantReligiousLineReward();
            M12SeawallPatrolProgressState.MarkNodeCleared(slot);
            string cards = M12ReligiousLineRewardService.FormatRewardCardNames(_manager?.cardStore);
            _titleText.text = "海牆巡邏通關";
            _bodyText.text = granted
                ? "獲得 " + cards + "\n熟練度 B"
                : "教會三張加練完成\n" + cards + " 已於首通取得";
            CreateFooterButton("繼續", true, OnClickPhaseBContinue);
            return;
        }

        if (won)
        {
            _titleText.text = "勝利 · 戰技合計未達標";
            _bodyText.text = M12SeawallPatrolProgressState.BuildCombinedTrioMissingHint(slot) +
                             "\n\n請重打階段 B";
            CreateFooterButton("再試一次", true, OnClickRetry);
            CreateFooterButton("返回地圖", false, OnClickReturnStory);
            return;
        }

        _titleText.text = "階段 B 落敗";
        _bodyText.text = "再試一次或先回地圖";
        CreateFooterButton("再試一次", true, OnClickRetry);
        CreateFooterButton("返回地圖", false, OnClickReturnStory);
    }

    private void ResetBodyPresentation()
    {
        if (_bodyText == null)
            return;

        _bodyText.richText = false;
        _bodyText.alignment = TextAlignmentOptions.Center;
        _bodyText.fontSize = 24f;
        _bodyText.lineSpacing = 2f;
    }

    private void ApplyPhaseAIncompleteBody(bool won)
    {
        if (_bodyText == null)
            return;

        _bodyText.richText = true;
        _bodyText.alignment = TextAlignmentOptions.TopLeft;
        _bodyText.fontSize = 23f;
        _bodyText.lineSpacing = 4f;
        _bodyText.text = M12TrioMasteryBattleTracker.BuildPhaseAIncompleteSettlementBody(won);
    }

    private void AddPhaseAIncompleteFooterButtons(int slot)
    {
        if (M12SeawallPatrolProgressState.IsPhaseAExamMemoUnlocked(slot))
            CreateFooterButton("段考备忘", false, OnClickShowExamMemo);
        CreateFooterButton("再試一次", true, OnClickRetry);
        CreateFooterButton("返回地圖", false, OnClickReturnStory);
    }

    private void OnClickShowExamMemo()
    {
        M12PhaseAExamMemoOverlay.Show(firstUnlockReveal: false, onDismiss: null);
    }

    private void OnClickPhaseAContinue()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M12SeawallPatrolProgressState.RecordPhaseAVictoryWithTrio(slot);
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressSession.LaunchM12MidPatrolPlotScene();
    }

    private void OnClickPhaseBContinue()
    {
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressSession.LaunchM12VictoryEpiloguePlotScene();
    }

    private void OnClickRetry()
    {
        if (BattleLaunchContext.IsM12TrioTutorialBattle)
            StoryProgressBattleReturn.RetryM12PhaseABattle();
        else
            StoryProgressBattleReturn.RetryM12PhaseBBattle();
    }

    private void OnClickReturnStory()
    {
        StoryProgressBattleReturn.CompleteReturnToStoryProgress(won: false);
    }

    private void EnsureUi()
    {
        if (_uiBuilt || _canvasRoot == null)
            return;

        _uiBuilt = true;
        _overlayRoot = new GameObject("M12SettlementOverlay", typeof(RectTransform), typeof(CanvasGroup));
        _overlayRoot.transform.SetParent(_canvasRoot, false);
        RectTransform overlayRt = _overlayRoot.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        Image dim = _overlayRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        _panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panelRoot.transform.SetParent(_overlayRoot.transform, false);
        RectTransform panelRt = _panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image panelBg = _panelRoot.GetComponent<Image>();
        panelBg.color = new Color(0.16f, 0.14f, 0.12f, 0.96f);

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(_panelRoot.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        titleRt.sizeDelta = new Vector2(-48f, 56f);
        _titleText = titleGo.GetComponent<TextMeshProUGUI>();
        _titleText.fontSize = 34f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.color = new Color(0.97f, 0.85f, 0.47f, 1f);
        ApplyFont(_titleText);

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(_panelRoot.transform, false);
        RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
        ApplyBodyRectLayout(bodyRt);
        bodyGo.AddComponent<RectMask2D>();
        _bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
        _bodyText.fontSize = 24f;
        _bodyText.lineSpacing = 2f;
        _bodyText.alignment = TextAlignmentOptions.Top;
        _bodyText.color = new Color(0.94f, 0.92f, 0.86f, 1f);
        _bodyText.enableWordWrapping = true;
        _bodyText.overflowMode = TextOverflowModes.Overflow;
        ApplyFont(_bodyText);

        GameObject rowGo = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(_panelRoot.transform, false);
        _buttonRowRt = rowGo.GetComponent<RectTransform>();
        _buttonRowRt.anchorMin = new Vector2(0f, 0f);
        _buttonRowRt.anchorMax = new Vector2(1f, 0f);
        _buttonRowRt.pivot = new Vector2(0.5f, 0f);
        _buttonRowRt.anchoredPosition = new Vector2(0f, 24f);
        _buttonRowRt.sizeDelta = new Vector2(-48f, 72f);
        HorizontalLayoutGroup hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        _overlayRoot.SetActive(false);
    }

    private static void ApplyBodyRectLayout(RectTransform bodyRt)
    {
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(BodyHorizontalPad, FooterBandHeight);
        bodyRt.offsetMax = new Vector2(-BodyHorizontalPad, -TitleBandHeight);
    }

    private void ClearButtons()
    {
        if (_buttonRowRt == null) return;
        for (int i = _buttonRowRt.childCount - 1; i >= 0; i--)
            Destroy(_buttonRowRt.GetChild(i).gameObject);
    }

    private void CreateFooterButton(string label, bool primary, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGo = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(_buttonRowRt, false);
        Button btn = btnGo.GetComponent<Button>();
        btn.onClick.AddListener(action);
        if (primary)
            BattleUiColors.ApplyButtonStyle(btn, "EndTurnButton");
        else
            BattleUiColors.ApplyHallWineButton(btn);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        ApplyFont(tmp);
    }

    private void ApplyFont(TMP_Text tmp)
    {
        if (tmp == null) return;
        TMP_FontAsset font = _font ?? ResolveFont();
        if (font != null)
            tmp.font = font;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset settings = SettingsUiFonts.ResolveParameterDetailsFont();
        if (settings != null) return settings;
        return BuildbeckUiFonts.ResolveBuildbeckButtonFont();
    }
}

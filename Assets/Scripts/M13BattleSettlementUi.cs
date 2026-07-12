using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-3 戰鬥結算：Phase A 冷爐迎測、Phase B 分波對決。</summary>
public sealed class M13BattleSettlementUi : MonoBehaviour
{
    private const float PanelWidth = 640f;
    private const float PanelHeight = 420f;

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
        BattleLaunchContext.IsM13WeatherTutorialBattle ||
        BattleLaunchContext.IsM13RivalDuelBattle;

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

        EnsureUi();
        if (_overlayRoot == null)
        {
            _showing = false;
            _showRoutine = null;
            yield break;
        }

        bool won = result == 1;
        ApplyContent(won);
        _overlayRoot.SetActive(true);
        _overlayRoot.transform.SetAsLastSibling();
        _showRoutine = null;
    }

    private void ApplyContent(bool won)
    {
        ClearButtons();
        if (BattleLaunchContext.IsM13RivalDuelBattle)
        {
            ApplyPhaseBContent(won);
            return;
        }

        ApplyPhaseAContent(won);
    }

    private void ApplyPhaseAContent(bool won)
    {
        if (_titleText != null)
        {
            _titleText.text = won ? "冷爐迎測 · 通過" : "冷爐迎測 · 落敗";
            _titleText.color = won
                ? new Color(0.55f, 0.92f, 0.62f, 1f)
                : new Color(0.95f, 0.72f, 0.55f, 1f);
        }

        if (_bodyText != null)
        {
            _bodyText.text = won
                ? "前 3 回合爐冷無天氣 第 4 回合起預報已驗\n下一步：玫瑰試煉"
                : "再試一次 或回大地圖調整節奏";
        }

        if (won)
            AddButton("ContinueBtn", "前往玫瑰試煉", OnClickPhaseAContinue, new Color(0.17f, 0.45f, 0.58f, 1f));
        else
        {
            AddButton("RetryBtn", "再試一次", OnClickPhaseARetry, new Color(0.22f, 0.48f, 0.58f, 1f));
            AddButton("ReturnBtn", "回大地圖", OnClickReturn, new Color(0.35f, 0.33f, 0.34f, 1f));
        }
    }

    private void ApplyPhaseBContent(bool won)
    {
        if (_titleText != null)
        {
            _titleText.text = won ? "分波對決 · 勝利" : "分波對決 · 落敗";
            _titleText.color = won
                ? new Color(0.55f, 0.92f, 0.62f, 1f)
                : new Color(0.95f, 0.72f, 0.55f, 1f);
        }

        if (_bodyText != null)
        {
            if (won && M13RivalDuelBattleTracker.QueryAllMissionGoalsMet())
                _bodyText.text = "任務全綠 阿潮離場\n下一步：迎潮終幕";
            else if (won)
                _bodyText.text = "已勝利但任務未全達標\n" +
                                 M13RivalDuelBattleTracker.BuildMissingMissionHint() +
                                 "\n\n請重打分波對決";
            else
                _bodyText.text = "再試一次 或回大地圖";
        }

        if (won && M13RivalDuelBattleTracker.QueryAllMissionGoalsMet())
            AddButton("EpilogueBtn", "迎潮終幕", OnClickPhaseBEpilogue, new Color(0.17f, 0.45f, 0.58f, 1f));
        else if (won)
        {
            AddButton("RetryBtn", "再試一次", OnClickPhaseBRetry, new Color(0.22f, 0.48f, 0.58f, 1f));
            AddButton("ReturnBtn", "回大地圖", OnClickReturn, new Color(0.35f, 0.33f, 0.34f, 1f));
        }
        else
        {
            AddButton("RetryBtn", "再試一次", OnClickPhaseBRetry, new Color(0.22f, 0.48f, 0.58f, 1f));
            AddButton("ReturnBtn", "回大地圖", OnClickReturn, new Color(0.35f, 0.33f, 0.34f, 1f));
        }
    }

    private void OnClickPhaseAContinue()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        M13RiverForkProgressState.MarkPhaseAComplete(slot);
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressSession.LaunchM13RoseTrialPlotScene();
    }

    private void OnClickPhaseARetry() => StoryProgressBattleReturn.RetryM13PhaseABattle();

    private void OnClickPhaseBRetry() => StoryProgressBattleReturn.RetryM13PhaseBBattle();

    private void OnClickPhaseBEpilogue()
    {
        BattleLaunchContext.ClearActiveBattle();
        StoryProgressSession.LaunchM13EpiloguePlotScene();
    }

    private void OnClickReturn() => StoryProgressBattleReturn.CompleteReturnToStoryProgress(won: false);

    private void EnsureUi()
    {
        if (_uiBuilt || _canvasRoot == null)
            return;

        _uiBuilt = true;
        _overlayRoot = new GameObject("M13SettlementOverlay", typeof(RectTransform), typeof(CanvasGroup));
        _overlayRoot.transform.SetParent(_canvasRoot, false);
        StretchFull(_overlayRoot.GetComponent<RectTransform>());
        _overlayRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        _panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panelRoot.transform.SetParent(_overlayRoot.transform, false);
        RectTransform panelRt = _panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        _panelRoot.GetComponent<Image>().color = new Color(0.16f, 0.14f, 0.12f, 0.96f);

        _titleText = CreateText(_panelRoot.transform, "Title", "", 34f, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -24f), new Vector2(-48f, 56f));
        _bodyText = CreateText(_panelRoot.transform, "Body", "", 24f, TextAlignmentOptions.Top,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(-48f, 180f));
        _bodyText.enableWordWrapping = true;

        GameObject row = new GameObject("Buttons", typeof(RectTransform));
        row.transform.SetParent(_panelRoot.transform, false);
        _buttonRowRt = row.GetComponent<RectTransform>();
        _buttonRowRt.anchorMin = new Vector2(0f, 0f);
        _buttonRowRt.anchorMax = new Vector2(1f, 0f);
        _buttonRowRt.pivot = new Vector2(0.5f, 0f);
        _buttonRowRt.anchoredPosition = new Vector2(0f, 28f);
        _buttonRowRt.sizeDelta = new Vector2(-48f, 72f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        _overlayRoot.SetActive(false);
    }

    private void ClearButtons()
    {
        if (_buttonRowRt == null)
            return;
        for (int i = _buttonRowRt.childCount - 1; i >= 0; i--)
            Destroy(_buttonRowRt.GetChild(i).gameObject);
    }

    private void AddButton(string name, string label, UnityEngine.Events.UnityAction onClick, Color bg)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_buttonRowRt, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = 64f;
        Image img = go.GetComponent<Image>();
        img.color = bg;
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        TextMeshProUGUI tmp = CreateText(go.transform, "Label", label, 28f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tmp.color = Color.white;
    }

    private TextMeshProUGUI CreateText(
        Transform parent, string name, string text, float size, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, anchorMin.y > 0.5f ? 1f : 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = new Color(0.94f, 0.92f, 0.86f, 1f);
        tmp.raycastTarget = false;
        return tmp;
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (_font != null) tmp.font = _font;
        else SettingsUiFonts.ApplyTo(tmp);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

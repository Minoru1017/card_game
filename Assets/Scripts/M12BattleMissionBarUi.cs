using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M-1-2 任務欄（LEVEL_DESIGN_M-1-2.md §3.0）：
/// 階段 A「御三家應用」顯示本局三戰技即時進度；階段 B「戰位克制教學」顯示 A+B 合計進度。
/// </summary>
public sealed class M12BattleMissionBarUi : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.5f;
    private const float BarWidth = 434f;
    private const float BarRightMarginPx = 22f;
    private const float BarTopOffsetPx = -156f;
    private const float TitleRowHeight = 44f;
    private const float SkillRowHeight = 36f;
    private const float RowPaddingPx = 10f;

    private static readonly Color PanelBgColor = new Color(0.10f, 0.13f, 0.15f, 0.88f);
    private static readonly Color PanelOutlineColor = new Color(0.45f, 0.72f, 0.78f, 0.9f);
    private static readonly Color TitleColor = new Color(0.97f, 0.85f, 0.47f, 1f);
    private static readonly Color DoneColor = new Color(0.55f, 0.92f, 0.62f, 1f);
    private static readonly Color PendingColor = new Color(0.86f, 0.88f, 0.86f, 0.92f);

    private BattleSimulationManager _manager;
    private Transform _canvasRoot;
    private TMP_FontAsset _preferredFont;
    private GameObject _root;
    private TMP_Text _titleText;
    private TMP_Text _winRowText;
    private TMP_Text _militiaRowText;
    private TMP_Text _queenRowText;
    private TMP_Text _kingRowText;
    private float _nextRefreshUnscaled;
    private bool _uiBuilt;
    private bool _eventsBound;

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsM12TrioMasteryBattle;

    public void Initialize(BattleSimulationManager manager, Transform canvasRoot, TMP_FontAsset uiFont = null)
    {
        _manager = manager;
        _canvasRoot = canvasRoot;
        _preferredFont = uiFont;
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

    private void Update()
    {
        if (!IsActiveForCurrentBattle || _manager == null || BattleAutoSimPlugin.IsRunning)
        {
            if (_root != null) _root.SetActive(false);
            return;
        }

        if (_manager.IsBattleOver())
        {
            if (_root != null) _root.SetActive(false);
            return;
        }

        if (Time.unscaledTime < _nextRefreshUnscaled)
            return;
        _nextRefreshUnscaled = Time.unscaledTime + RefreshIntervalSeconds;

        EnsureUi();
        if (_root == null) return;
        if (!_root.activeSelf)
            _root.SetActive(true);
        RefreshRows();
    }

    private void OnBattleEnded(int result)
    {
        if (_root != null) _root.SetActive(false);
    }

    public void HideForSettlement()
    {
        if (_root != null) _root.SetActive(false);
    }

    public void ForceRefreshNow()
    {
        _nextRefreshUnscaled = 0f;
        if (_root != null && _root.activeSelf)
            RefreshRows();
    }

    private void RefreshRows()
    {
        bool phaseA = BattleLaunchContext.IsM12TrioTutorialBattle;
        if (_titleText != null)
            _titleText.text = phaseA ? "關卡目標：御三家應用" : "關卡目標：戰位克制教學";
        if (_winRowText != null)
        {
            _winRowText.text = "○ 取得勝利";
            _winRowText.color = PendingColor;
        }

        bool militia;
        bool queen;
        bool king;
        string suffix;
        if (phaseA)
        {
            militia = M12TrioMasteryBattleTracker.QueryMilitiaTriggered();
            queen = M12TrioMasteryBattleTracker.QueryQueenTriggered();
            king = M12TrioMasteryBattleTracker.QueryKingTriggered();
            suffix = "（本局）";
        }
        else
        {
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            militia = M12SeawallPatrolProgressState.QueryCombinedMilitiaTriggered(slot);
            queen = M12SeawallPatrolProgressState.QueryCombinedQueenTriggered(slot);
            king = M12SeawallPatrolProgressState.QueryCombinedKingTriggered(slot);
            suffix = "（A+B 合計）";
        }

        ApplySkillRow(_militiaRowText, "民兵·列陣" + suffix, militia);
        ApplySkillRow(_queenRowText, "王后·王室庇護" + suffix, queen);
        ApplySkillRow(_kingRowText, "國王·庭訓號令" + suffix, king);
    }

    private static void ApplySkillRow(TMP_Text row, string label, bool done)
    {
        if (row == null) return;
        row.text = (done ? "● " : "○ ") + label;
        row.color = done ? DoneColor : PendingColor;
    }

    private void EnsureUi()
    {
        if (_uiBuilt || _canvasRoot == null)
            return;

        _uiBuilt = true;
        float panelHeight = RowPaddingPx * 2f + TitleRowHeight + SkillRowHeight * 4f;
        _root = new GameObject("M12MissionBar", typeof(RectTransform));
        _root.transform.SetParent(_canvasRoot, false);
        RectTransform rootRt = _root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 1f);
        rootRt.anchoredPosition = new Vector2(-BarRightMarginPx, BarTopOffsetPx);
        rootRt.sizeDelta = new Vector2(BarWidth, panelHeight);

        Image bg = _root.AddComponent<Image>();
        bg.color = PanelBgColor;
        bg.raycastTarget = false;
        Outline outline = _root.AddComponent<Outline>();
        outline.effectColor = PanelOutlineColor;
        outline.effectDistance = new Vector2(2f, -2f);

        float y = -RowPaddingPx;
        _titleText = CreateRow("Title", ref y, TitleRowHeight, 26f, FontStyles.Bold, TitleColor);
        _winRowText = CreateRow("WinRow", ref y, SkillRowHeight, 22f, FontStyles.Normal, PendingColor);
        _militiaRowText = CreateRow("MilitiaRow", ref y, SkillRowHeight, 22f, FontStyles.Normal, PendingColor);
        _queenRowText = CreateRow("QueenRow", ref y, SkillRowHeight, 22f, FontStyles.Normal, PendingColor);
        _kingRowText = CreateRow("KingRow", ref y, SkillRowHeight, 22f, FontStyles.Normal, PendingColor);

        _root.SetActive(false);
    }

    private TMP_Text CreateRow(string name, ref float y, float height, float fontSize, FontStyles style, Color color)
    {
        GameObject rowGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        rowGo.transform.SetParent(_root.transform, false);
        RectTransform rt = rowGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-32f, height);
        y -= height;

        TextMeshProUGUI tmp = rowGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        return tmp;
    }

    private void ApplyFont(TMP_Text tmp)
    {
        if (tmp == null) return;
        TMP_FontAsset font = _preferredFont ?? ResolveFont();
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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>M-1-3 分波對決任務欄（LEVEL_DESIGN_M-1-3.md §八）。</summary>
public sealed class M13BattleMissionBarUi : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.5f;
    private const float BarWidth = 434f;
    private const float BarRightMarginPx = 22f;
    private const float BarTopOffsetPx = -156f;
    private const float TitleRowHeight = 44f;
    private const float SkillRowHeight = 36f;
    private const float RowPaddingPx = 10f;

    private static readonly Color PanelBgColor = new Color(0.10f, 0.13f, 0.15f, 0.88f);
    private static readonly Color PanelOutlineColor = new Color(0.45f, 0.72f, 0.88f, 0.9f);
    private static readonly Color TitleColor = new Color(0.75f, 0.92f, 0.98f, 1f);
    private static readonly Color DoneColor = new Color(0.55f, 0.92f, 0.62f, 1f);
    private static readonly Color PendingColor = new Color(0.86f, 0.88f, 0.86f, 0.92f);

    private BattleSimulationManager _manager;
    private Transform _canvasRoot;
    private TMP_FontAsset _preferredFont;
    private GameObject _root;
    private TMP_Text _titleText;
    private TMP_Text _winRowText;
    private TMP_Text _burstRowText;
    private TMP_Text _chainRowText;
    private float _nextRefreshUnscaled;
    private bool _uiBuilt;
    private bool _eventsBound;

    public static bool IsActiveForCurrentBattle =>
        BattleLaunchContext.IsM13RivalDuelBattle;

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

    private void RefreshRows()
    {
        if (_titleText != null)
            _titleText.text = "關卡目標：分波對決";
        if (_winRowText != null)
        {
            _winRowText.text = "○ 取得勝利";
            _winRowText.color = PendingColor;
        }

        ApplySkillRow(_burstRowText, "單回合對敵英雄傷害 ≥8",
            M13RivalDuelBattleTracker.QuerySingleTurnHeroDamageAtLeastEight());
        ApplySkillRow(_chainRowText, "祝聖→修女→初級治療",
            M13RivalDuelBattleTracker.QueryHolyTherapyChainComplete());
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
        float panelHeight = RowPaddingPx * 2f + TitleRowHeight + SkillRowHeight * 3f;
        _root = new GameObject("M13MissionBar", typeof(RectTransform));
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
        _burstRowText = CreateRow("BurstRow", ref y, SkillRowHeight, 22f, FontStyles.Normal, PendingColor);
        _chainRowText = CreateRow("ChainRow", ref y, SkillRowHeight, 22f, FontStyles.Normal, PendingColor);
        _root.SetActive(false);
    }

    private TMP_Text CreateRow(string name, ref float y, float height, float fontSize, FontStyles style, Color color)
    {
        GameObject rowGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        rowGo.transform.SetParent(_root.transform, false);
        RectTransform rowRt = rowGo.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0f, 1f);
        rowRt.anchoredPosition = new Vector2(RowPaddingPx, y);
        rowRt.sizeDelta = new Vector2(-RowPaddingPx * 2f, height);
        y -= height;

        TextMeshProUGUI tmp = rowGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        return tmp;
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        if (_preferredFont != null)
            tmp.font = _preferredFont;
        else
            SettingsUiFonts.ApplyTo(tmp);
    }
}

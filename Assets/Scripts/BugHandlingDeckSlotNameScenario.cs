using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bug handling scenarios 場景：牌組名稱回歸測試（<see cref="PlayerDeckSlotNameStorage"/>）。
/// </summary>
[DisallowMultipleComponent]
public class BugHandlingDeckSlotNameScenario : MonoBehaviour
{
    public const string SceneName = "Bug handling scenarios";

    private PlayerData playerData;
    private int inspectPlayerSlot = 1;
    private int selectedDeckSlot;

    private TextMeshProUGUI statusText;
    private TMP_InputField nameInput;
    private TextMeshProUGUI deckTabLabel;
    private readonly Button[] playerSlotButtons = new Button[PlayerDeckSlotNameStorage.PlayerSlotCount];
    private readonly Button[] deckSlotButtons = new Button[PlayerDeckSlotNameStorage.DeckSlotsPerPlayer];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureScenarioInBugScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;
        if (FindFirstObjectByType<BugHandlingDeckSlotNameScenario>() != null)
            return;

        GameObject root = new GameObject("BugHandlingDeckSlotNameScenario");
        root.AddComponent<BugHandlingDeckSlotNameScenario>();
    }

    private void Awake()
    {
        if (!SceneManager.GetActiveScene().name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            Destroy(gameObject);
            return;
        }

        EnsureEventSystem();
        EnsurePlayerDataHost();
        BuildUi();
        SelectInspectPlayerSlot(Mathf.Clamp(PlayerData.GetActivePlayerSlotOrDefault(), 1, PlayerDeckSlotNameStorage.PlayerSlotCount));
        SelectDeckSlot(0);
        RefreshStatus();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    private void EnsurePlayerDataHost()
    {
        playerData = PlayerData.ResolveCanonical();
        if (playerData == null)
        {
            GameObject host = new GameObject("DataManager");
            playerData = host.AddComponent<PlayerData>();
        }

        playerData = PlayerData.EnsureWritable();
        if (!playerData.IsSaveHydratedFromDisk)
            playerData.LoadPlayerData();
        playerData.EnsureMinimumDeckSlotCount();
    }

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("BugDeckNameTestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        RectTransform panel = CreatePanel(canvasGo.transform, "MainPanel", new Vector2(0.5f, 0.5f), new Vector2(920f, 680f), new Color(0.12f, 0.1f, 0.09f, 0.94f));

        float y = -24f;
        CreateLabel(panel, "Title", "Bug 測試：牌組名稱（3 玩家槽 × 5 牌組）", 22, ref y, 36f, FontStyles.Bold);
        CreateLabel(panel, "ModuleHint", "模組：PlayerDeckSlotNameStorage.cs", 16, ref y, 24f, FontStyles.Italic);

        y -= 8f;
        CreateLabel(panel, "PlayerRowLabel", "檢視玩家槽", 17, ref y, 22f, FontStyles.Normal);
        CreateButtonRow(panel, ref y, playerSlotButtons, new[] { "玩家 1", "玩家 2", "玩家 3" }, OnClickPlayerSlotButton);

        CreateLabel(panel, "DeckRowLabel", "牌組槽（0-based 顯示於狀態列）", 17, ref y, 22f, FontStyles.Normal);
        CreateButtonRow(panel, ref y, deckSlotButtons, new[] { "槽 1", "槽 2", "槽 3", "槽 4", "槽 5" }, OnClickDeckSlotButton);

        y -= 4f;
        deckTabLabel = CreateLabel(panel, "DeckSelection", string.Empty, 16, ref y, 22f, FontStyles.Normal);

        GameObject inputRow = new GameObject("NameInputRow", typeof(RectTransform));
        inputRow.transform.SetParent(panel, false);
        RectTransform inputRowRt = inputRow.GetComponent<RectTransform>();
        inputRowRt.anchorMin = new Vector2(0f, 1f);
        inputRowRt.anchorMax = new Vector2(1f, 1f);
        inputRowRt.pivot = new Vector2(0.5f, 1f);
        inputRowRt.anchoredPosition = new Vector2(0f, y);
        inputRowRt.sizeDelta = new Vector2(-40f, 40f);
        y -= 48f;

        nameInput = CreateInputField(inputRow.transform);
        CreateChildButton(inputRow.transform, "ApplyName", "僅記憶體", new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(100f, 36f), OnApplyCustomName);
        CreateChildButton(inputRow.transform, "ConfirmName", "確定（流程2）", new Vector2(1f, 0.5f), new Vector2(-116f, 0f), new Vector2(120f, 36f), OnConfirmRename);
        CreateChildButton(inputRow.transform, "DiscardInput", "捨棄輸入（流程1）", new Vector2(1f, 0.5f), new Vector2(-244f, 0f), new Vector2(140f, 36f), OnDiscardInput);

        CreateActionButton(panel, "AlignActive", "對齊 active 槽（SelectActivePlayerSlot）", ref y, OnAlignActiveSlot);
        CreateActionButton(panel, "RepairPollution", "清理存檔污染（deck_slot_name）", ref y, OnRepairSavePollution);
        CreateActionButton(panel, "DisbandReset", "解散還原（流程3）", ref y, OnDisbandReset);
        CreateActionButton(panel, "Save", "存檔 SavePlayerData", ref y, OnSave);
        CreateActionButton(panel, "Reload", "重載 LoadPlayerData", ref y, OnReload);
        CreateActionButton(panel, "SwitchCsvOnly", "切換玩家槽（僅改 CSV active_slot）", ref y, OnSwitchPlayerSlotCsvOnly);
        CreateActionButton(panel, "SwitchAndReload", "切換玩家槽（SelectActivePlayerSlot 正規流程）", ref y, OnSwitchPlayerSlotWithReload);
        CreateActionButton(panel, "SyncProfile", "SyncDeckSummaryFromRuntime", ref y, OnSyncProfile);
        CreateActionButton(panel, "Dump", "Dump 三槽 memory + disk 到 Console", ref y, OnDumpAllSlots);

        statusText = CreateScrollStatus(panel, ref y, 260f);
    }

    private void OnClickPlayerSlotButton(int index)
    {
        SelectInspectPlayerSlot(index + 1);
    }

    private void OnClickDeckSlotButton(int index)
    {
        SelectDeckSlot(index);
    }

    private void SelectInspectPlayerSlot(int playerSlot)
    {
        inspectPlayerSlot = Mathf.Clamp(playerSlot, 1, PlayerDeckSlotNameStorage.PlayerSlotCount);
        for (int i = 0; i < playerSlotButtons.Length; i++)
            SetButtonSelected(playerSlotButtons[i], i + 1 == inspectPlayerSlot);
        RefreshStatus();
    }

    private void SelectDeckSlot(int deckSlot)
    {
        selectedDeckSlot = Mathf.Clamp(deckSlot, 0, PlayerDeckSlotNameStorage.DeckSlotsPerPlayer - 1);
        for (int i = 0; i < deckSlotButtons.Length; i++)
            SetButtonSelected(deckSlotButtons[i], i == selectedDeckSlot);

        if (playerData != null && nameInput != null)
        {
            string display = GetInspectDisplayName(selectedDeckSlot);
            nameInput.SetTextWithoutNotify(display);
        }

        if (deckTabLabel != null && playerData != null)
        {
            bool activeMismatch = playerData.activePlayerSlot != inspectPlayerSlot;
            deckTabLabel.text =
                "目前編輯：玩家槽 " + inspectPlayerSlot +
                (activeMismatch ? "（active=" + playerData.activePlayerSlot + "，請先對齊）" : string.Empty) +
                " / 牌組槽 " + (selectedDeckSlot + 1) +
                " ｜ raw=\"" + GetInspectRawName(selectedDeckSlot) + "\"";
        }
    }

    private string GetInspectRawName(int deckSlot)
    {
        if (playerData == null) return string.Empty;
        if (playerData.activePlayerSlot == inspectPlayerSlot)
            return PlayerDeckSlotNameStorage.GetRawName(playerData, deckSlot);
        return PlayerDeckSlotNameStorage.GetDiskRawName(inspectPlayerSlot, deckSlot);
    }

    private string GetInspectDisplayName(int deckSlot)
    {
        if (playerData == null) return PlayerDeckSlotNameStorage.FormatDefaultDisplayName(deckSlot);
        if (playerData.activePlayerSlot == inspectPlayerSlot)
            return PlayerDeckSlotNameStorage.GetDisplayName(playerData, deckSlot);
        return PlayerDeckSlotNameStorage.GetDiskDisplayName(inspectPlayerSlot, deckSlot);
    }

    private bool EnsureActiveMatchesInspect(string operationLabel)
    {
        if (playerData == null) return false;
        if (playerData.activePlayerSlot == inspectPlayerSlot) return true;

        AppendStatusNote(operationLabel + "：請先按「對齊 active 槽」或切換玩家槽（目前 active="
            + playerData.activePlayerSlot + "，檢視=" + inspectPlayerSlot + "）");
        return false;
    }

    private void OnApplyCustomName()
    {
        if (playerData == null) return;
        if (!EnsureActiveMatchesInspect("僅記憶體套用")) return;

        string text = nameInput != null ? nameInput.text : string.Empty;
        PlayerDeckSlotNameStorage.SetCustomName(playerData, selectedDeckSlot, text);
        SelectDeckSlot(selectedDeckSlot);
        RefreshStatus("已 SetCustomName（未 SavePlayerData）");
    }

    private void OnConfirmRename()
    {
        if (playerData == null) return;
        if (!EnsureActiveMatchesInspect("確定改名")) return;

        playerData.selectedDeckSlot = selectedDeckSlot;
        string text = nameInput != null ? nameInput.text : string.Empty;
        string summary = PlayerDeckSlotNameStorage.ConfirmRenameAndPersist(playerData, selectedDeckSlot, text);
        SelectDeckSlot(selectedDeckSlot);
        RefreshStatus("流程2 ConfirmRenameAndPersist\n" + summary);
    }

    private void OnDiscardInput()
    {
        if (playerData == null || nameInput == null) return;
        string stored = GetInspectDisplayName(selectedDeckSlot);
        nameInput.SetTextWithoutNotify(stored);
        RefreshStatus("流程1：已還原輸入框為 stored 顯示名「" + stored + "」（未寫入）");
    }

    private void OnAlignActiveSlot()
    {
        PlayerDeckSlotNameStorage.SelectActivePlayerSlotAndReload(inspectPlayerSlot);
        playerData = PlayerData.ResolveCanonical();
        if (playerData != null)
        {
            inspectPlayerSlot = playerData.activePlayerSlot;
            SelectInspectPlayerSlot(inspectPlayerSlot);
            SelectDeckSlot(selectedDeckSlot);
        }
        RefreshStatus("active 槽已對齊至 " + inspectPlayerSlot);
    }

    private void OnDisbandReset()
    {
        if (playerData == null) return;
        if (!EnsureActiveMatchesInspect("解散還原")) return;

        playerData.selectedDeckSlot = selectedDeckSlot;
        string summary = PlayerDeckSlotNameStorage.DisbandSelectedDeckSlotNameAndPersist(playerData);
        SelectDeckSlot(selectedDeckSlot);
        RefreshStatus("流程3 DisbandSelectedDeckSlotNameAndPersist\n" + summary);
    }

    private void OnRepairSavePollution()
    {
        var report = PlayerDeckSlotNameStorage.RepairPersistedDeckSlotNamePollution(syncActiveSlotProfile: true);
        if (playerData != null)
        {
            playerData.LoadPlayerData();
            playerData = PlayerData.ResolveCanonical();
            inspectPlayerSlot = Mathf.Clamp(playerData.activePlayerSlot, 1, PlayerDeckSlotNameStorage.PlayerSlotCount);
            SelectInspectPlayerSlot(inspectPlayerSlot);
            SelectDeckSlot(selectedDeckSlot);
        }

        RefreshStatus(
            "清理存檔污染完成\n" +
            "deck_slot_name 掃描=" + report.DeckSlotNameRowsScanned +
            " 修復=" + report.DeckSlotNameRowsRepaired +
            " profile_decks 移除=" + report.ProfileDecksRowsRemoved);
    }

    private void OnSave()
    {
        if (playerData == null) return;
        playerData.SavePlayerData();
        RefreshStatus("已 SavePlayerData");
    }

    private void OnReload()
    {
        if (playerData == null) return;
        playerData.LoadPlayerData();
        inspectPlayerSlot = Mathf.Clamp(playerData.activePlayerSlot, 1, PlayerDeckSlotNameStorage.PlayerSlotCount);
        SelectInspectPlayerSlot(inspectPlayerSlot);
        SelectDeckSlot(selectedDeckSlot);
        RefreshStatus("已 LoadPlayerData");
    }

    private void OnSwitchPlayerSlotCsvOnly()
    {
        PlayerDeckSlotNameStorage.DebugWriteActivePlayerSlotCsvOnly(inspectPlayerSlot);
        RefreshStatus("已寫入 active_slot=" + inspectPlayerSlot + "（未 LoadPlayerData，可重現跨槽污染）");
    }

    private void OnSwitchPlayerSlotWithReload()
    {
        PlayerData.SelectActivePlayerSlot(inspectPlayerSlot);
        playerData = PlayerData.ResolveCanonical();
        if (playerData != null)
        {
            inspectPlayerSlot = playerData.activePlayerSlot;
            SelectInspectPlayerSlot(inspectPlayerSlot);
            SelectDeckSlot(selectedDeckSlot);
        }
        RefreshStatus("SelectActivePlayerSlot + LoadPlayerData 完成");
    }

    private void OnSyncProfile()
    {
        string summary = PlayerProfileCsvService.SyncDeckSummaryFromRuntime();
        RefreshStatus("SyncDeckSummaryFromRuntime\n" + summary);
    }

    private void OnDumpAllSlots()
    {
        if (playerData == null) return;
        Debug.Log("[BugHandlingDeckSlotNameScenario] active memory: " + PlayerDeckSlotNameStorage.DescribeInMemory(playerData));
        for (int slot = 1; slot <= PlayerDeckSlotNameStorage.PlayerSlotCount; slot++)
            Debug.Log("[BugHandlingDeckSlotNameScenario] " + PlayerDeckSlotNameStorage.DescribeDiskRowsForPlayerSlot(slot));
        RefreshStatus("已輸出到 Console（三槽 disk + 目前 memory）");
    }

    private void RefreshStatus(string headerNote = null)
    {
        if (statusText == null) return;
        if (playerData == null)
        {
            statusText.text = "PlayerData 未就緒";
            return;
        }

        var sb = new System.Text.StringBuilder(512);
        if (!string.IsNullOrEmpty(headerNote))
        {
            sb.AppendLine(headerNote);
            sb.AppendLine();
        }

        sb.AppendLine("【記憶體 active 槽】");
        sb.AppendLine(PlayerDeckSlotNameStorage.DescribeInMemory(playerData));
        sb.AppendLine();
        sb.AppendLine("【磁碟 CSV 各玩家槽 deck_slot_name】");
        for (int slot = 1; slot <= PlayerDeckSlotNameStorage.PlayerSlotCount; slot++)
            sb.AppendLine(PlayerDeckSlotNameStorage.DescribeDiskRowsForPlayerSlot(slot));

        statusText.text = sb.ToString();
        SelectDeckSlot(selectedDeckSlot);
    }

    private void AppendStatusNote(string note) => RefreshStatus(note);

    // ── UI helpers ──────────────────────────────────────────────────────

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorCenter, Vector2 size, Color bg)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorCenter;
        rt.anchorMax = anchorCenter;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        return rt;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize, ref float y, float height, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, height);
        y -= height + 6f;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        UiFontResolver.ApplyTo(tmp);
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = new Color(0.95f, 0.92f, 0.86f, 1f);
        tmp.text = text;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void CreateButtonRow(Transform parent, ref float y, Button[] buttons, string[] labels, System.Action<int> onClick)
    {
        GameObject row = new GameObject("ButtonRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, y);
        rowRt.sizeDelta = new Vector2(-40f, 34f);
        y -= 40f;

        float width = 1f / labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            int captured = i;
            buttons[i] = CreateChildButton(
                row.transform,
                "Btn" + i,
                labels[i],
                new Vector2(width * i + width * 0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-6f + (rowRt.rect.width > 0 ? 0 : 120f), 30f),
                () => onClick(captured));
            RectTransform btnRt = buttons[i].GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(width * i + 0.01f, 0f);
            btnRt.anchorMax = new Vector2(width * (i + 1) - 0.01f, 1f);
            btnRt.offsetMin = new Vector2(2f, 0f);
            btnRt.offsetMax = new Vector2(-2f, 0f);
        }
    }

    private static void CreateActionButton(Transform parent, string name, string label, ref float y, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-40f, 34f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.28f, 0.42f, 0.36f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 2f);
        textRt.offsetMax = new Vector2(-8f, -2f);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        UiFontResolver.ApplyTo(tmp);
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.color = Color.white;
        tmp.text = label;

        y -= 40f;
    }

    private static Button CreateChildButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.28f, 0.42f, 0.36f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 2f);
        textRt.offsetMax = new Vector2(-6f, -2f);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        UiFontResolver.ApplyTo(tmp);
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.color = Color.white;
        tmp.text = label;
        return btn;
    }

    private static TMP_InputField CreateInputField(Transform parent)
    {
        GameObject root = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 0.5f);
        rootRt.anchorMax = new Vector2(1f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = new Vector2(-200f, 0f);
        rootRt.sizeDelta = new Vector2(-420f, 36f);
        root.GetComponent<Image>().color = new Color(0.95f, 0.93f, 0.88f, 1f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);
        RectTransform areaRt = textArea.GetComponent<RectTransform>();
        areaRt.anchorMin = Vector2.zero;
        areaRt.anchorMax = Vector2.one;
        areaRt.offsetMin = new Vector2(8f, 4f);
        areaRt.offsetMax = new Vector2(-8f, -4f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(textArea.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI inputText = textGo.GetComponent<TextMeshProUGUI>();
        UiFontResolver.ApplyTo(inputText);
        inputText.fontSize = 18;
        inputText.color = Color.black;

        TMP_InputField field = root.GetComponent<TMP_InputField>();
        field.textViewport = areaRt;
        field.textComponent = inputText;
        field.lineType = TMP_InputField.LineType.SingleLine;
        return field;
    }

    private static void SetButtonSelected(Button btn, bool selected)
    {
        if (btn == null) return;
        Image img = btn.targetGraphic as Image;
        if (img != null)
            img.color = selected ? new Color(0.85f, 0.62f, 0.22f, 1f) : new Color(0.28f, 0.42f, 0.36f, 1f);
    }

    private static TextMeshProUGUI CreateScrollStatus(Transform parent, ref float y, float height)
    {
        GameObject scrollGo = new GameObject("StatusScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 1f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.pivot = new Vector2(0.5f, 1f);
        scrollRt.anchoredPosition = new Vector2(0f, y);
        scrollRt.sizeDelta = new Vector2(-40f, height);
        scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.06f, 0.85f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(4f, 4f);
        viewportRt.offsetMax = new Vector2(-4f, -4f);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, height);

        GameObject textGo = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(content.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 1f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(-8f, height);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        UiFontResolver.ApplyTo(tmp);
        tmp.fontSize = 13;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = new Color(0.95f, 0.92f, 0.86f, 1f);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRt;
        scroll.viewport = viewportRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        y -= height + 6f;
        return tmp;
    }
}

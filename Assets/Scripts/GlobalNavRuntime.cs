using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class GlobalNavConfigData
{
    public string homeSceneName = "hall";
    public string backpackSceneName = "Persistent";
    public string settingsSceneName = "Settings";
    public List<string> hideInSceneNameContains = new List<string> { "battle", "buildbeck", "builddeck" };
    public float triggerSize = 128f;
    public float triggerTopRightMargin = 28f;
}

/// <summary>
/// Global, cross-scene navigation UI runtime.
/// Single instance + config file driven.
/// </summary>
public partial class GlobalNavRuntime : MonoBehaviour
{
    /*
     * MASTER INDEX
     * - GlobalNavRuntime.cs: shared state, bootstrap, config loading
     * - GlobalNavRuntime.UiBuild.cs: nav canvas/buttons construction and font helpers
     * - GlobalNavRuntime.Navigation.cs: scene visibility, routing, scene-name resolution
     * - GlobalNavRuntime.Vault.cs: valuables vault open/close/build bridge
     * - GlobalNavRuntime.PlayerInfoOverlay.cs: player info open/close and content refresh
     * - GlobalNavRuntime.PlayerInfoLayout.cs: player info overlay build and shared layout helpers
     * - GlobalNavRuntime.PlayerInfoRecord.cs: player info battle-record filter and summary UI
     */

    private const string RootName = "GlobalNavRuntimeRoot";
    private const string ConfigResourcePath = "GlobalNavConfig";

    private static GlobalNavRuntime instance;
    private static GlobalNavConfigData config;
    private static TMP_FontAsset navLabelFont;

    private GlobalNavView view;
    private GameObject playerInfoOverlayRoot;
    private TextMeshProUGUI playerInfoUuidText;
    private TextMeshProUGUI playerInfoRoleText;
    private TextMeshProUGUI playerInfoStartDateText;
    private TextMeshProUGUI playerInfoCoinsText;
    private TextMeshProUGUI playerInfoDeckSummaryText;
    private TextMeshProUGUI playerInfoHeroSummaryText;
    private TextMeshProUGUI playerInfoLastResultText;
    private TextMeshProUGUI playerInfoProgressText;
    private TextMeshProUGUI playerInfoRecordTotalText;
    private const int PlayerInfoOverlayLayoutVersion = 3;
    private int builtPlayerInfoLayoutVersion;
    private int playerInfoActiveRecordFilter = 1;
    private readonly Button[] playerInfoRecordFilterButtons = new Button[4];
    private readonly Image[] playerInfoRecordFilterButtonBgs = new Image[4];
    private readonly TextMeshProUGUI[] playerInfoRecordFilterLabels = new TextMeshProUGUI[4];
    private PlayerInfoRecordColumnUi[] playerInfoRecordColumns;
    private static readonly int[] PlayerInfoRecordFilterCodes = { 1, -1, 2, 3 };
    private static readonly string[] PlayerInfoRecordFilterLabels = { "W", "L", "D", "Q" };
    private static readonly Color[] PlayerInfoDifficultyBadgeColors =
    {
        new Color(0.36f, 0.78f, 0.44f, 1f),
        new Color(0.45f, 0.72f, 0.95f, 1f),
        new Color(0.95f, 0.78f, 0.28f, 1f),
        new Color(0.95f, 0.42f, 0.50f, 1f),
        new Color(0.62f, 0.38f, 0.88f, 1f)
    };

    private sealed class PlayerInfoRecordColumnUi
    {
        public Image badgeImage;
        public TextMeshProUGUI badgeText;
        public TextMeshProUGUI countText;
    }

    private const float PlayerInfoPadH = 28f;
    private const float PlayerInfoSectionGap = 18f;
    private const float PlayerInfoLineGap = 10f;
    private const float PlayerInfoHeaderHeight = 76f;
    private const float PlayerInfoFooterHeight = 60f;
    private static readonly Color PlayerInfoTextPrimary = new Color(0.2f, 0.16f, 0.12f, 1f);
    private static readonly Color PlayerInfoTextMuted = new Color(0.48f, 0.42f, 0.36f, 1f);
    private static readonly Color PlayerInfoSectionBg = new Color(0.98f, 0.96f, 0.92f, 0.98f);
    private static readonly Color PlayerInfoSectionTitle = new Color(0.32f, 0.27f, 0.22f, 1f);

    private RectTransform playerInfoScrollContentRt;
    private float playerInfoLayoutY;
    private float playerInfoContentWidth;
    private TMP_InputField playerSlotNameInput;
    private Button backpackButton;
    private Button valuablesVaultButton;
    private Button settingsButton;
    private Button goLoginButton;
    private GlobalNavValuablesVaultOverlay valuablesVaultOverlay;
    private const float TabPanelRightMargin = 28f;
    private const float TabPanelTopMargin = 176f;
    private const float TabPanelLeftMargin = 24f;
    private const float TabPanelBottomMargin = 24f;

    public static void EnsureInitialized()
    {
        if (instance != null) return;
        config = LoadConfig();

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        instance = root.AddComponent<GlobalNavRuntime>();
        instance.BuildUiRuntime();
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        instance.ApplySceneState(SceneManager.GetActiveScene().name);
    }

    /// <summary>重新套用當前場景的全局導覽顯示，並把 ≡ 觸發鈕提到 GlobalNavCanvas 最上層。</summary>
    public static void RefreshActiveSceneNav()
    {
        EnsureInitialized();
        if (instance == null || instance.view == null)
            return;

        instance.ApplySceneState(SceneManager.GetActiveScene().name);
        if (instance.view.triggerButtonObject != null)
            instance.view.triggerButtonObject.transform.SetAsLastSibling();
    }

    public static bool TryOpenPlayerInfoOverlay()
    {
        EnsureInitialized();
        if (instance == null) return false;
        return instance.OpenPlayerInfoOverlay();
    }

    public static bool TryOpenValuablesVaultOverlay()
    {
        EnsureInitialized();
        if (instance == null) return false;
        return instance.OpenValuablesVaultOverlay();
    }

    /// <summary>玩家資訊面板已開啟時，刷新「牌組」摘要列（Buildbeck 改名後用）。</summary>
    public static void TryRefreshPlayerInfoDeckSummaryIfOpen(string deckSummary)
    {
        if (string.IsNullOrWhiteSpace(deckSummary)) return;
        EnsureInitialized();
        if (instance == null || instance.playerInfoOverlayRoot == null || !instance.playerInfoOverlayRoot.activeSelf)
            return;
        if (instance.playerInfoDeckSummaryText == null) return;
        instance.playerInfoDeckSummaryText.text = FormatDeckSummaryForDisplay(deckSummary);
        FitProfileValueTextHeight(instance.playerInfoDeckSummaryText, 26f, 30f);
    }

    private static GlobalNavConfigData LoadConfig()
    {
        TextAsset json = Resources.Load<TextAsset>(ConfigResourcePath);
        if (json == null || string.IsNullOrWhiteSpace(json.text))
            return new GlobalNavConfigData();
        try
        {
            GlobalNavConfigData loaded = JsonUtility.FromJson<GlobalNavConfigData>(json.text);
            return loaded ?? new GlobalNavConfigData();
        }
        catch
        {
            return new GlobalNavConfigData();
        }
    }
}

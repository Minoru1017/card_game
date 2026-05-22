using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public partial class DeckManager : MonoBehaviour, ICardInspectPanelHost
{
    public int maxDeckCards = 30;
    public TextMeshProUGUI deckHintText;
    public Text deckHintLegacyText;
    public Image deckHintPanel;
    private Coroutine deckHintRoutine;
    private Font hintDynamicFont;
    private TMP_FontAsset hintTMPFont;
    private GameObject resetConfirmPanel;
    private GameObject deckNameEditPanel;
    private TMP_InputField deckNameEditInput;
    private TextMeshProUGUI deckNameEditCharCounterTmp;
    private RectTransform deckNameEditCardRt;
    private Coroutine _deckNameEditFocusCo;
    /// <summary>開啟對話後短暫忽略遮罩 onClick，避免「編輯鈕同一筆點擊」在放開時誤觸全螢幕 dim 而立刻關閉。</summary>
    private float _deckNameEditDimClickIgnoreUntilUnscaled;

    public Transform deckPanel;
    public Transform libraryPanel;
    [Header("Optional Scene Deck Slot Buttons")]
    public Button deckSlotButton1;
    public Button deckSlotButton2;
    public Button deckSlotButton3;
    public Button deckSlotButton4;
    public Button deckSlotButton5;

    [Header("Buildbeck — Save Deck")]
    [Tooltip("儲存牌組：拖入含 Image 美術與 Button 的 UI。可放在 Deck Grid 底下（ClearPanels 會保留含此按鈕的子樹）。請從 Hierarchy 拖入場景實例，勿拖 Project 裡的 Prefab 資產。")]
    public Button saveDeckButton;

    [Tooltip("清除／解散牌組：拖入 Button。會走 OnClickResetDeckButton（先確認對話框再清空當前槽牌組）。可與儲存鍵同列；ClearPanels 會保留含此按鈕的子樹。")]
    public Button disbandDeckButton;

    [Tooltip("編輯「目前選中」牌組槽的顯示名稱（彈窗輸入）。可選；亦可由場景物件命名自動綁定。")]
    public Button editDeckNameButton;

    [Tooltip("顯示「目前選中槽」的牌組名稱（與編輯／換槽同步）。拖入 TextMeshProUGUI（或任一 TMP_Text）。")]
    public TMP_Text currentDeckDisplayNameText;

    [Tooltip("若顯示區使用舊版 Unity UI Text，拖於此（與上一欄二選一即可）。")]
    public Text currentDeckDisplayNameLegacyText;

    [Header("Buildbeck — Library 卡片外殼 (DeckGen df/oi)")]
    [Tooltip("使用新版 df/oi 外殼時，是否顯示卡面上的「卡片名稱」。變更後可呼叫 RefreshLibraryDeckGenCardNameVisibility() 立即重刷館藏。")]
    public bool buildbeckLibraryDeckGenShowCardName = true;

    public GameObject deckCardPrefab;
    public GameObject librarycardPrefab;

    public GameObject DataManager;

    public bool showDeck = true;
    public bool enableInteraction = true;

    private PlayerData PlayerData;
    private CardStore CardStore;
    private GameObject defaultDeckCardPrefab;
    private GameObject defaultLibraryCardPrefab;
    private GameObject runtimeLibraryTemplate;
    private GameObject runtimeDeckTemplate;
    /// <summary>Buildbeck：Canvas 直層上的「DeckGen_Library」場景原型（執行期會關閉，僅供 Instantiate 到各張館藏卡）。</summary>
    private GameObject _buildbeckLibraryDeckGenCanvasPrototype;
    /// <summary>Buildbeck：Canvas 直層上的「DeckGen_Deck」場景原型（執行期會關閉，僅供 Instantiate 到各張牌組列卡片的 <c>DeckCardInfoStrip</c> 下）。</summary>
    private GameObject _buildbeckDeckStripStackCanvasPrototype;

    private Dictionary<int, GameObject> libraryDic = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> deckDic = new Dictionary<int, GameObject>();
    private readonly List<Button> deckSlotButtons = new List<Button>();
    private bool externalSlotButtonsBound;
    private bool deckSlotSelectorExpanded = false;

    private ScrollRect _libraryPanelScrollRect;
    private ScrollRect _deckPanelScrollRect;
    private float _libraryScrollLastNormY = -1f;
    private float _deckScrollLastNormY = -1f;
    private Coroutine _libraryScrollEdgePulseCo;
    private Coroutine _deckScrollEdgePulseCo;
    private DeckArcPresenter _deckArcPresenter;
    private int _libraryScrollPulseGeneration;
    private int _deckScrollPulseGeneration;
    private float _scrollEdgeFeelCooldownUntilUnscaled;
    private bool _deckScrollPointerHeld;
    private bool _deckScrollUserSessionActive;
    /// <summary>While true, deck arc layout and deck force-rebuild are skipped so remove + shift animation can own anchored positions.</summary>
    private bool _deckCardRemoveAnimationActive;
    // #region agent log
    private static int _agentDbgArcBlockCount;
    private static int _agentDbgLateArcSkipCount;
    private static void AgentDbgNdjson(string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            long ts = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
            string line = "{\"sessionId\":\"8cdc3f\",\"hypothesisId\":\"" + hypothesisId + "\",\"location\":\"" + location +
                         "\",\"message\":\"" + message + "\",\"data\":" + dataJson + ",\"timestamp\":" + ts + "}\n";
            File.AppendAllText(Path.Combine(Application.dataPath, "..", "debug-8cdc3f.log"), line);
        }
        catch
        {
        }
    }
    // #endregion
    private Coroutine _sceneBuildbeckReloadCo;
    private bool _buildbeckDiagLogged;
    private DeckDataController _deckDataController;
    private DeckListView _deckListView;
    private DeckArcController _deckArcController;
    private DeckSlotNavigator _deckSlotNavigator;
    private SceneLoader _cachedSceneLoader;
    private Canvas _cachedHintCanvas;
    private TextMeshProUGUI _cachedAnyTmpForFontProbe;

    private const float DeckScrollElasticity = 0.12f;
    private const float DeckScrollDecelerationRate = 0.22f;
    private const float DeckScrollWheelSensitivity = 26f;
    /// <summary>牌組直向列表：慣性衰減（愈低放手後愈快停，阻尼愈重）。</summary>
    private const float DeckListDecelerationRate = 0.04f;
    /// <summary>牌組列表滾輪靈敏度（略低＝單格位移較小，手感較沉）。</summary>
    private const float DeckListWheelSensitivity = 18f;
    /// <summary>牌組刪卡：最底列淡出／非最底列左移淡出共用秒數。</summary>
    private const float DeckListRemoveCardFadeSeconds = 0.18f;
    /// <summary>復歸捲動最短／最長秒數（依實際位移在兩者間內插）。</summary>
    private const float DeckListRecoveryScrollDurationMin = 0.26f;
    private const float DeckListRecoveryScrollDurationMax = 0.52f;
    /// <summary>最底列：復歸捲動從淡出進度的何處開始（0.52 ≈ 與淡出尾段重疊，縮短整體節奏）。</summary>
    private const float DeckListRemoveBottomRecoveryOverlap = 0.52f;
    private const float DeckScrollEdgePulseScale = 1.008f;
    private const float DeckScrollEdgeFeelCooldown = 0.2f;
    private const float DeckArcUserScrollSessionVelocityCutoff = 18f;
    private const float DeckArcUserScrollMinKDuringDrag = 0.12f;
    private const int DeckArcVisibleSlotMin = 3;
    private const int DeckArcVisibleSlotMax = 13;
    private RectTransform deckSlotGuideDotsRoot;
    private readonly List<Image> deckSlotGuideDots = new List<Image>(5);
    private int deckSlotGuideDotsBuiltCount;
    [Header("Deck Card Layout Tuning")]
    [Range(0.6f, 1.6f)] public float namePlateHeightWidthRatio = 1f;
    [Range(0.6f, 2.2f)] public float deckStatSquareScale = 1.28f;
    [Range(0.2f, 1.2f)] public float statUnderlaySquareScale = 0.64f;
    [Range(-120f, 120f)] public float attackStatOffsetX = -30f;
    [Range(-120f, 120f)] public float healthStatOffsetX = 30f;
    [Range(-120f, 120f)] public float statOffsetY = 0f;
    private float _lastNamePlateHeightWidthRatio;
    private float _lastDeckStatSquareScale;
    private float _lastStatUnderlaySquareScale;
    private float _lastAttackStatOffsetX;
    private float _lastHealthStatOffsetX;
    private float _lastStatOffsetY;
    private int _lastDeckListCellSpacingY;
    private int _lastDeckListContentScalePercent;
    private int _lastDeckListStatHalfGapLevel;
    private float _lastDeckArcParentOffsetX;
    private float _lastDeckArcParentOffsetY;
    private float _deckListParentAnchoredYBaseline;
    private bool _deckListParentAnchoredYBaselineCaptured;
    private const float NewLayoutNamePlateRatio = 1f;
    private const float NewLayoutDeckStatSquareScale = 1.0f;
    private const float NewLayoutStatCircleScale = 0.42f;
    private const float NewLayoutAttackOffsetX = -134f;
    private const float NewLayoutHealthOffsetX = 134f;
    private const float NewLayoutStatOffsetY = 0f;
    private const float NewLayoutDeckCellWidth = 430f;
    private const float NewLayoutDeckCellHeight = 140f;
    private const int NewLayoutDeckCellWidthPx = 430;
    /// <summary>
    /// Buildbeck：牌組 Scroll 視窗固定整數邏輯像素（與 CanvasScaler 參考解析度同一 UI 空間），
    /// 以父層右下角為錨靠右對齊；右內縮 231、寬 762 與舊版左緣 927 之矩形在 1920 寬下相同。
    /// </summary>
    private const int BuildbeckDeckViewportWidthPx = 762;
    private const int BuildbeckDeckViewportHeightPx = 788;
    private const int BuildbeckDeckViewportRightInsetPx = 231;
    private const int BuildbeckDeckViewportBottomPx = 151;
    /// <summary>
    /// Buildbeck：左側「Library Grid」ScrollRect 視窗固定邏輯像素（錨定左下 + sizeDelta），
    /// 不經由錨點比例伸縮或額外縮放倍率函式推算尺寸。
    /// </summary>
    public const int BuildbeckLibraryGridViewportWidthPx = 928;
    public const int BuildbeckLibraryGridViewportHeightPx = 1048;
    /// <summary>與父層左緣的基礎內縮（像素）。</summary>
    public const int BuildbeckLibraryGridViewportLeftInsetPx = 16;
    /// <summary>靠左對齊時額外留白（邏輯像素；與 <see cref="BuildbeckLibraryGridViewportLeftInsetPx"/>、<see cref="BuildbeckLibraryGridViewportCenterPointOffsetXPx"/>、<see cref="BuildbeckLibraryGridViewportExtraOffsetXPx"/> 一併加總為 anchoredPosition.x）。</summary>
    public const int BuildbeckLibraryGridViewportLeftBreathingPx = 32;
    /// <summary>控制視窗水平位置：相對於內縮＋呼吸距離後，再將參考中心在 x 軸平移的邏輯像素（目前錨點為左下，等同整體右移）。</summary>
    public const int BuildbeckLibraryGridViewportCenterPointOffsetXPx = 100;
    /// <summary>額外水平右移（邏輯像素；錨左下時 x 增大）。與其餘常數一併加總為 <see cref="RectTransform.anchoredPosition"/>.x。</summary>
    public const int BuildbeckLibraryGridViewportExtraOffsetXPx = 100;
    public const int BuildbeckLibraryGridViewportBottomInsetPx = 16;
    private const string BuildbeckLibraryDeckGenRootName = "DeckGen_Library";
    private const string BuildbeckLibraryDeckGenDefaultName = "DeckGen_Library_df";
    private const string BuildbeckLibraryDeckGenOverlayOiName = "DeckGen_Library_oi";
    private const string BuildbeckLibraryDeckGenOverlayOlLegacyName = "DeckGen_Library_ol";
    private const int BuildbeckLibraryDeckGenStackDisplayMin = 2;
    private const int BuildbeckLibraryDeckGenStackDisplayMax = 99;
    private const string BuildbeckDeckStripPrototypeCanvasName = "DeckGen_Deck";
    /// <summary>Buildbeck：掛在 Canvas 直層、內含 <c>mt_df</c>／<c>mt_oi</c> 的條原型（與卡片 prefab 內同名節點區隔：後者不在 Canvas 直層）。</summary>
    private const string BuildbeckDeckStripCanvasTemplateDirectChildName = "DeckCardInfoStrip";
    private const string BuildbeckDeckStripMtDefaultName = "DeckCardInfoStrip_mt_df";
    private const string BuildbeckDeckStripMtOverlayName = "DeckCardInfoStrip_mt_oi";
    private const int BuildbeckDeckStripStackDisplayMin = 2;
    private const int BuildbeckDeckStripStackDisplayMax = 99;
    /// <summary><see cref="DeckCardInfoStrip_mt_oi"/> 下顯示疊加數（2–99）的區域節點名稱（其下含 TMP）。</summary>
    private const string BuildbeckDeckStripStackOiRegionName = "oi";
    private const string BuildbeckDeckStripMtCardNameChild = "card name";
    private const string BuildbeckDeckStripMtAtkChild = "ATK";
    private const string BuildbeckDeckStripMtHpChild = "HP";
    /// <summary>Instantiate 自 <see cref="BuildbeckDeckStripPrototypeCanvasName"/> 後掛在 <c>DeckCardInfoStrip</c> 下的根節點名稱（避免與遞迴清除的舊名 <c>DeckGen_Deck</c> 衝突）。</summary>
    private const string BuildbeckDeckStripStackInstantiatedRootName = "DeckStripStack";
    private const bool NewLayoutEnableDeckArc = true;
    private static readonly Vector2 NewLayoutDeckViewportAnchorMin = new Vector2(0.46f, 0.14f);
    private static readonly Vector2 NewLayoutDeckViewportAnchorMax = new Vector2(0.99f, 0.87f);
    private static readonly Vector2 NewLayoutDeckViewportOffsetMin = new Vector2(-36f, 0f);
    private static readonly Vector2 NewLayoutDeckViewportOffsetMax = new Vector2(0f, 0f);
    private const float NewLayoutStatBadgeMinSize = 56f;
    private const float NewLayoutStatBadgeSizeByFontRatio = 2.35f;
    private const float NewLayoutStatBadgeYOffset = 20f;
    private const string AtkBadgeName = "AtkValueCircle";
    private const string HpBadgeName = "HpValueCircle";
    [Header("Deck List Layout")]
    [Range(-35, 32)]
    [Tooltip("Vertical gap between deck rows (Grid spacing). Integer only. Negative values pull rows closer / overlap.")]
    public int deckListCellSpacingY = 6;
    [Range(50, 200)]
    [Tooltip("非 Buildbeck：牌組清單整體縮放百分比（50–200）。Buildbeck 固定為 100%（不套用此欄）。")]
    public int deckListContentScalePercent = 100;
    private const float DeckListStatHalfGapPxMin = 115f;
    private const float DeckListStatHalfGapPxMax = 186f;
    private const int DeckListStatHalfGapLevelMin = 1;
    private const int DeckListStatHalfGapLevelMax = 50;
    [Range(1, 50)]
    [Tooltip("Deck list ATK/HP half-gap step (integer 1–50). Maps to pixel half-gap from 115 to 186 (ATK −px, HP +px).")]
    public int deckListStatHalfGapLevel = 25;
    [Header("Deck Arc Slot Layout")]
    [Range(3, 13)]
    [Tooltip("Visible arc slot count (odd only: 3,5,7,9,11,13). Even values are corrected to the next odd.")]
    public int deckArcVisibleSlotCount = 7;
    [Range(80f, 2500f)]
    [Tooltip("True arc circle radius R (pixels). Must be ≥ chord half-width L.")]
    public float deckArcCircleRadius = 520f;
    [Range(40f, 1200f)]
    [Tooltip("Chord half-width L (pixels): half the vertical span from center slot to wing along the straight list.")]
    public float deckArcChordHalfWidth = 260f;
    [Range(0.15f, 1f)]
    [Tooltip("Scales arc offsets (both X and Y) from the circle geometry.")]
    public float deckArcShapeStrength = 0.5f;
    [Range(0f, 3f)]
    [Tooltip("Extend C-arc active zone vertically (in slot units). Larger value pushes arc/linear handoff outside visible deck area.")]
    public float deckArcVisibleRangePaddingSlots = 1.15f;
    [Range(0f, 0.45f)]
    [Tooltip("Base smooth time for horizontal C-arc at low scroll speed (seconds). Velocity blend applies while the user drags or coasts the deck list after a drag; 0 = snap.")]
    public float deckArcHorizontalSmoothTime = 0.12f;
    [Range(400f, 8000f)]
    [Tooltip("|ScrollRect content velocity.y| at which arc smoothing is most aggressive (shortest smooth time, highest max speed).")]
    public float deckArcScrollVelocityRef = 2200f;
    [Header("Deck Arc Parent Offset")]
    [Range(-400f, 400f)]
    [Tooltip("非 Buildbeck：Deck 視窗水平位移（像素），加在 stretch offset 上。Buildbeck 已併入固定視窗常數，此欄不影響 Buildbeck 視窗寬度。")]
    public float deckArcParentOffsetX = 72f;
    [Range(-400f, 400f)]
    [Tooltip("Buildbeck：整數化後加在牌組視窗底邊（BuildbeckDeckViewportBottomPx）。非 Buildbeck：deckPanel.parent 的 anchoredPosition.y 加值。")]
    public float deckArcParentOffsetY = 0f;
    [Tooltip("X offset fine-tune by slot id. Index 0 = Slot#1, Index 1 = Slot#2 ...")]
    public List<float> deckArcSlotXOffsetById = new List<float>();
    private bool runtimeLayoutDefaultsApplied;

    /// <summary>重設牌組確認框等執行期 UI 的父 Canvas；避免每次 FindObjectsOfTypeAll。</summary>
    private Canvas cachedRuntimeUiCanvas;

    private Canvas GetCachedRuntimeUiCanvas()
    {
        if (cachedRuntimeUiCanvas != null && cachedRuntimeUiCanvas)
            return cachedRuntimeUiCanvas;

        cachedRuntimeUiCanvas = null;
        Canvas[] activeCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < activeCanvases.Length; i++)
        {
            Canvas c = activeCanvases[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            cachedRuntimeUiCanvas = c;
            return cachedRuntimeUiCanvas;
        }

        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < allCanvases.Length; i++)
        {
            Canvas c = allCanvases[i];
            if (c == null || c.gameObject == null) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            cachedRuntimeUiCanvas = c;
            c.gameObject.SetActive(true);
            c.enabled = true;
            return cachedRuntimeUiCanvas;
        }

        return null;
    }

    private static Transform FindDeepChildByName(Transform root, string exactName)
    {
        if (root == null) return null;
        if (root.name == exactName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChildByName(root.GetChild(i), exactName);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// 在 <paramref name="deckGen"/> 底下，回傳 <paramref name="node"/> 往上直到父為 <paramref name="deckGen"/> 的那一層（用於 df/oi 外包一層如 <c>DeckGen_Library_堆疊數為1</c> 時整段啟停）。
    /// </summary>
    private static Transform LibraryDeckGenDirectBranchUnder(Transform deckGen, Transform node)
    {
        if (deckGen == null || node == null) return null;
        Transform t = node;
        while (t.parent != null && t.parent != deckGen)
            t = t.parent;
        return t;
    }

    /// <summary>若統計 TMP 在 <see cref="DeckCardInfoStrip_mt_df"/> 內，不可改父節點（舊邏輯會拉到 Strip 根而離開底圖／被遮擋）。</summary>
    private static bool IsUnderDeckCardInfoStripMtDf(Transform t)
    {
        for (Transform c = t; c != null; c = c.parent)
        {
            if (c.name == BuildbeckDeckStripMtDefaultName) return true;
        }

        return false;
    }

    private static readonly string[] LibraryPanelPreferredNames = { "Library Grid", "Library" };

    private void TryResolveLibraryPanelByName()
    {
        if (libraryPanel != null) return;
        Scene scene = SceneManager.GetActiveScene();
        for (int i = 0; i < LibraryPanelPreferredNames.Length; i++)
        {
            GameObject lib = SceneSearchUtil.FindSceneObject(scene, LibraryPanelPreferredNames[i]);
            if (lib != null)
            {
                libraryPanel = lib.transform;
                return;
            }
        }
    }

    private void TryResolveLibraryPanelUnderCanvas()
    {
        if (libraryPanel != null) return;
        Canvas canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null) return;
        for (int i = 0; i < LibraryPanelPreferredNames.Length; i++)
        {
            Transform t = FindDeepChildByName(canvas.transform, LibraryPanelPreferredNames[i]);
            if (t != null)
            {
                libraryPanel = t;
                return;
            }
        }
    }

    private void TryResolveDeckPanelByName()
    {
        if (deckPanel != null) return;
        GameObject dk = SceneSearchUtil.FindSceneObject(SceneManager.GetActiveScene(), "Deck Grid");
        if (dk != null)
            deckPanel = dk.transform;
    }

    private void TryResolveDeckPanelUnderCanvas()
    {
        if (deckPanel != null) return;
        Canvas canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null) return;
        Transform t = FindDeepChildByName(canvas.transform, "Deck Grid");
        if (t != null)
            deckPanel = t;
    }

    void Awake()
    {
        // 與 DataManager 同場景時，牌組 UI 只應由 DataManager（與 PlayerData 同物件）上的 DeckManager 負責。
        // 使用 Destroy(gameObject) 會延遲到影格末，本元件的 Start 仍會跑一輪並印 panel=null；改 DestroyImmediate。
        EnsureCoreRefs();
        if (DataManager == null)
        {
            PlayerData pdRoot = Object.FindFirstObjectByType<PlayerData>();
            if (pdRoot != null)
                DataManager = pdRoot.gameObject;
            else
            {
                GameObject dm = GameObject.Find("DataManager");
                if (dm != null)
                    DataManager = dm;
            }
        }

        DeckManager primary = null;
        if (DataManager != null)
            primary = DataManager.GetComponent<DeckManager>();

        if (primary == null)
        {
            DeckManager[] all = Object.FindObjectsByType<DeckManager>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].GetComponent<PlayerData>() != null)
                {
                    primary = all[i];
                    break;
                }
            }
        }

        if (primary != null && primary != this)
            DestroyImmediate(gameObject);
        else
            deckArcVisibleSlotCount = NormalizeDeckArcVisibleSlotCount(deckArcVisibleSlotCount);
    }

    private void OnValidate()
    {
        deckArcVisibleSlotCount = NormalizeDeckArcVisibleSlotCount(deckArcVisibleSlotCount);
        deckArcChordHalfWidth = Mathf.Max(1f, deckArcChordHalfWidth);
        deckArcCircleRadius = Mathf.Max(deckArcCircleRadius, deckArcChordHalfWidth + 0.5f);
    }

    private static int NormalizeDeckArcVisibleSlotCount(int value)
    {
        int v = Mathf.Clamp(value, DeckArcVisibleSlotMin, DeckArcVisibleSlotMax);
        if ((v & 1) == 0)
            v = Mathf.Min(v + 1, DeckArcVisibleSlotMax);
        return v;
    }

    private DeckDataController DataController => _deckDataController ??= new DeckDataController(this);
    private DeckListView ListViewController => _deckListView ??= new DeckListView(this);
    private DeckArcController ArcController => _deckArcController ??= new DeckArcController(this);
    private DeckSlotNavigator SlotNavigator => _deckSlotNavigator ??= new DeckSlotNavigator(this);

    private SceneLoader GetCachedSceneLoader()
    {
        if (_cachedSceneLoader == null)
            _cachedSceneLoader = Object.FindFirstObjectByType<SceneLoader>();
        return _cachedSceneLoader;
    }

    private Canvas ResolvePrimaryUiCanvas()
    {
        if (deckPanel != null)
        {
            Canvas deckCanvas = deckPanel.GetComponentInParent<Canvas>(true);
            if (deckCanvas != null) return deckCanvas;
        }
        if (libraryPanel != null)
        {
            Canvas libCanvas = libraryPanel.GetComponentInParent<Canvas>(true);
            if (libCanvas != null) return libCanvas;
        }
        if (_cachedHintCanvas != null) return _cachedHintCanvas;
        _cachedHintCanvas = Object.FindFirstObjectByType<Canvas>();
        return _cachedHintCanvas;
    }

    /// <summary>DataManager 上的唯一 DeckManager；場景內重複實例銷毀時不得解除全域場景訂閱。</summary>
    private bool IsCanonicalDeckManagerInstance()
    {
        if (DataManager == null)
        {
            PlayerData pd = PlayerData.ResolveCanonical();
            if (pd != null) DataManager = pd.gameObject;
        }
        if (DataManager == null) return true;
        DeckManager primary = DataManager.GetComponent<DeckManager>();
        return primary == null || primary == this;
    }

    private void OnDestroy()
    {
        if (IsCanonicalDeckManagerInstance())
        {
            SceneManager.sceneLoaded -= OnSceneLoadedEnsureBuildbeckDeckUi;
            SceneManager.sceneUnloaded -= OnSceneUnloadedFlushBuildbeckSave;
        }

        UnwireDeckScrollEdgeFeel(_libraryPanelScrollRect, OnLibraryPanelScrollFeel);
        UnwireDeckScrollEdgeFeel(_deckPanelScrollRect, OnDeckPanelScrollFeel);
        if (_libraryScrollEdgePulseCo != null) StopCoroutine(_libraryScrollEdgePulseCo);
        if (_deckScrollEdgePulseCo != null) StopCoroutine(_deckScrollEdgePulseCo);
        _libraryScrollEdgePulseCo = null;
        _deckScrollEdgePulseCo = null;
    }

    private void OnEnable()
    {
        if (!IsCanonicalDeckManagerInstance()) return;
        SceneManager.sceneLoaded -= OnSceneLoadedEnsureBuildbeckDeckUi;
        SceneManager.sceneLoaded += OnSceneLoadedEnsureBuildbeckDeckUi;
        SceneManager.sceneUnloaded -= OnSceneUnloadedFlushBuildbeckSave;
        SceneManager.sceneUnloaded += OnSceneUnloadedFlushBuildbeckSave;
    }

    private void OnDisable()
    {
        if (!IsCanonicalDeckManagerInstance()) return;
        SceneManager.sceneLoaded -= OnSceneLoadedEnsureBuildbeckDeckUi;
        SceneManager.sceneUnloaded -= OnSceneUnloadedFlushBuildbeckSave;
    }

    /// <summary>玩家資訊等流程更新存檔後，若仍在 Buildbeck 則刷新牌組分頁／名稱顯示。</summary>
    public void RefreshBuildbeckDeckNameLabelsIfActive()
    {
        if (!IsBuildbeckSceneActive()) return;
        EnsureCoreRefs();
        BuildbeckLayoutAutoBinder.InvalidateAndRewire(this);
        BindExternalSlotButtonsIfNeeded();
        RefreshDeckSlotTabVisual();
    }

    private static void OnSceneUnloadedFlushBuildbeckSave(Scene scene)
    {
        if (!scene.IsValid() || !scene.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase))
            return;
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd != null) pd.SavePlayerData();
    }

    private void OnSceneLoadedEnsureBuildbeckDeckUi(Scene scene, LoadSceneMode mode)
    {
        RequestBuildbeckUiReload();
    }

    public void TriggerBuildbeckUiReload()
    {
        RequestBuildbeckUiReload();
    }

    /// <summary>Buildbeck UI 重新綁定前呼叫，確保牌組分頁會重新掛上場景按鈕。</summary>
    public void NotifyBuildbeckUiRewireStarting()
    {
        externalSlotButtonsBound = false;
    }

    private void RequestBuildbeckUiReload()
    {
        if (!IsBuildbeckSceneActive())
        {
            _buildbeckDiagLogged = false;
            CleanupDeckSlotGuideDots();
            return;
        }

        EnsureCoreRefs();
        if (DataManager != null)
        {
            DeckManager primary = DataManager.GetComponent<DeckManager>();
            if (primary != null && primary != this) return;
        }

        // Debounce: avoid restarting reload while one is already running (causes visible UI flicker).
        if (_sceneBuildbeckReloadCo != null) return;
        _sceneBuildbeckReloadCo = StartCoroutine(CoReloadBuildbeckDeckUiAfterSceneLoad());
    }

    private IEnumerator CoReloadBuildbeckDeckUiAfterSceneLoad()
    {
        try
        {
            // Wait 1-2 frames so Buildbeck scaffold/binder can create/wire nodes first.
            yield return null;
            yield return null;

            if (IsBuildbeckSceneActive())
                BuildbeckSceneAutoScaffold.EnsureScaffoldNow();

            EnsureCoreRefs();
            ResetBuildbeckSceneUiWiring();
            NotifyBuildbeckUiRewireStarting();
            BuildbeckLayoutAutoBinder.InvalidateAndRewire(this);

            PlayerData pd = PlayerData.ResolveCanonical();
            if (pd != null)
            {
                pd.EnsureMinimumDeckSlotCount();
                pd.LoadPlayerData();
            }

            EnsureDeckUIRefs();
            BindExternalSlotButtonsIfNeeded();
            EnsureDeckSlotGuideDots();
            RefreshDeckSlotTabVisual();

            // 先前在 Buildbeck 分支 yield break 且未清除 _sceneBuildbeckReloadCo，會永久擋住後續 RequestBuildbeckUiReload。
            ClearPanels();
            UpdateLibrary();
            if (showDeck) UpdateDeck();
            DestroyBackpackProficiencyHelpFloatingButton();
            RefreshScrollablePanels();
            ForcePanelsScrollToTop();
            ForceRebuildPanelsLayout();
            LogBuildbeckUiDiagOnce();

            if (IsBuildbeckSceneActive())
            {
                BuildbeckLayoutAutoBinder.TryWireReadyBattleButton();
                SceneLoader loader = GetCachedSceneLoader();
                if (loader != null) loader.RefreshEnterBattleState(false);
                BuildbeckLayoutAutoBinder.BringBuildbeckReturnButtonToFront();
            }

            DestroyBackpackProficiencyHelpFloatingButton();
        }
        finally
        {
            _sceneBuildbeckReloadCo = null;
        }
    }

    private static bool IsBuildbeckSceneActive()
    {
        Scene s = SceneManager.GetActiveScene();
        return s.IsValid() && s.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase);
    }

    private Canvas ResolveDeckUiCanvas()
    {
        if (deckPanel != null)
        {
            Canvas onDeck = deckPanel.GetComponentInParent<Canvas>(true);
            if (onDeck != null) return onDeck;
        }
        return GetCachedRuntimeUiCanvas();
    }

    private void CleanupDeckSlotGuideDots()
    {
        if (deckSlotGuideDotsRoot != null)
            Destroy(deckSlotGuideDotsRoot.gameObject);
        deckSlotGuideDotsRoot = null;
        deckSlotGuideDots.Clear();
        deckSlotGuideDotsBuiltCount = 0;
    }

    private static void EnsureHierarchyActive(Transform leaf)
    {
        Transform t = leaf;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    /// <summary>
    /// Buildbeck: draw disband UI above the deck ScrollRect viewport (sibling order), or use a child Canvas override when hierarchies differ.
    /// </summary>
    /// <param name="disband">If null, resolves via <see cref="BuildbeckLayoutAutoBinder.ResolveDisbandDeckButton"/>.</param>
    public void EnsureDisbandDeckButtonDrawOrder(Button disband)
    {
        if (!IsBuildbeckSceneActive() || deckPanel == null) return;

        Button b = disband != null ? disband : BuildbeckLayoutAutoBinder.ResolveDisbandDeckButton(this);
        if (b == null) return;

        RectTransform deckViewportRt = deckPanel.parent as RectTransform;
        if (deckViewportRt == null) return;

        Transform vpParent = deckViewportRt.parent;
        if (vpParent == null) return;

        Transform walk = b.transform;
        while (walk.parent != null && walk.parent != vpParent)
            walk = walk.parent;

        if (walk.parent == vpParent)
        {
            int vIdx = deckViewportRt.GetSiblingIndex();
            int idx = Mathf.Clamp(vIdx + 1, 0, Mathf.Max(0, vpParent.childCount - 1));
            if (walk.GetSiblingIndex() != idx)
                walk.SetSiblingIndex(idx);
            return;
        }

        EnsureDisbandOverlaySortingCanvas(b.gameObject);
    }

    private static void EnsureDisbandOverlaySortingCanvas(GameObject disbandRoot)
    {
        if (disbandRoot == null) return;
        Canvas refCanvas = null;
        Transform walkParent = disbandRoot.transform.parent;
        while (walkParent != null)
        {
            refCanvas = walkParent.GetComponent<Canvas>();
            if (refCanvas != null) break;
            walkParent = walkParent.parent;
        }

        int baseOrder = refCanvas != null ? refCanvas.sortingOrder : 0;
        Canvas c = disbandRoot.GetComponent<Canvas>();
        if (c == null) c = disbandRoot.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = baseOrder + 30;
        if (disbandRoot.GetComponent<GraphicRaycaster>() == null)
            disbandRoot.AddComponent<GraphicRaycaster>();
    }

    private void LogBuildbeckUiDiagOnce()
    {
        if (_buildbeckDiagLogged) return;
        _buildbeckDiagLogged = true;

        Scene s = SceneManager.GetActiveScene();
        Canvas deckCanvas = deckPanel != null ? deckPanel.GetComponentInParent<Canvas>(true) : null;
        RectTransform deckParent = deckPanel != null ? deckPanel.parent as RectTransform : null;
        RectTransform libParent = libraryPanel != null ? libraryPanel.parent as RectTransform : null;

        string msg =
            "[BuildbeckDiag] scene=" + s.name +
            " | dataManager=" + (DataManager != null ? DataManager.name : "null") +
            " | playerData=" + (PlayerData != null ? "ok" : "null") +
            " | libraryPanel=" + (libraryPanel != null ? libraryPanel.name : "null") +
            " activeSelf=" + (libraryPanel != null && libraryPanel.gameObject.activeSelf) +
            " inHierarchy=" + (libraryPanel != null && libraryPanel.gameObject.activeInHierarchy) +
            " parent=" + (libParent != null ? libParent.name : "null") +
            " | deckPanel=" + (deckPanel != null ? deckPanel.name : "null") +
            " activeSelf=" + (deckPanel != null && deckPanel.gameObject.activeSelf) +
            " inHierarchy=" + (deckPanel != null && deckPanel.gameObject.activeInHierarchy) +
            " parent=" + (deckParent != null ? deckParent.name : "null") +
            " | deckCanvas=" + (deckCanvas != null ? deckCanvas.name : "null") +
            " canvasEnabled=" + (deckCanvas != null && deckCanvas.enabled) +
            " canvasActive=" + (deckCanvas != null && deckCanvas.gameObject.activeInHierarchy) +
            " sortOrder=" + (deckCanvas != null ? deckCanvas.sortingOrder : -999) +
            " | libChildren=" + (libraryPanel != null ? libraryPanel.childCount : -1) +
            " deckChildren=" + (deckPanel != null ? deckPanel.childCount : -1);

        Debug.Log(msg);
    }

    private static void UnwireDeckScrollEdgeFeel(ScrollRect sr, UnityAction<Vector2> handler)
    {
        if (sr == null || handler == null) return;
        sr.onValueChanged.RemoveListener(handler);
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Awake 若因執行順序未先刪到重複實例，這裡再擋一次（Destroy 非 Immediate 時）
        if (this == null)
            yield break;

        EnsureCoreRefs();
        if (DataManager != null)
        {
            DeckManager other = DataManager.GetComponent<DeckManager>();
            if (other != null && other != this)
                yield break;
        }

        EnsureMinimumDeckSlotCount();
        AutoSelectFirstNonEmptyDeckSlotIfNeeded();

        EnsureDeckUIRefs();
        ApplyNewLayoutRuntimeDefaults();
        ApplyDeckParentLayoutOffsets();
        EnsureDeckPanelSingleColumnLayout();

        yield return null; // �� PlayerData Awake/Start �]��

        EnsureTMPHintReady();
        defaultDeckCardPrefab = deckCardPrefab;
        defaultLibraryCardPrefab = librarycardPrefab;
        CaptureRuntimeTemplatesIfNeeded();
        EnsureDeckUIRefs();
        ApplyNewLayoutRuntimeDefaults();
        ApplyDeckParentLayoutOffsets();
        EnsureDeckPanelSingleColumnLayout();

        AttachWheelScroll(libraryPanel);
        AttachWheelScroll(deckPanel);
        BindExternalSlotButtonsIfNeeded();
        EnsureDeckSlotGuideDots();
        RefreshDeckSlotTabVisual();

        ClearPanels();
        UpdateLibrary();
        if (showDeck) UpdateDeck();
        DestroyBackpackProficiencyHelpFloatingButton();
        RefreshScrollablePanels();
        ForcePanelsScrollToTop();
    }

    private void AutoSelectFirstNonEmptyDeckSlotIfNeeded()
    {
        EnsureCoreRefs();
        if (PlayerData == null) return;
        if (PlayerData.deckSlotCount <= 1) return;
        if (PlayerData.GetSelectedDeckTotalCount() > 0) return;

        for (int slot = 0; slot < PlayerData.deckSlotCount; slot++)
        {
            if (slot == PlayerData.selectedDeckSlot) continue;
            int total = 0;
            var map = PlayerData.GetDeckMap(slot);
            foreach (var kv in map)
            {
                if (kv.Value > 0) total += kv.Value;
            }

            if (total <= 0) continue;

            PlayerData.SetSelectedDeckSlot(slot);
            PlayerData.SavePlayerData();
            return;
        }

        // All slots are empty -> fallback to slot 1 (index 0).
        PlayerData.SetSelectedDeckSlot(0);
        PlayerData.SavePlayerData();
    }

    private void EnsureMinimumDeckSlotCount()
    {
        EnsureCoreRefs();
        if (PlayerData == null) return;
        if (PlayerData.deckSlotCount >= PlayerData.MinDeckSlotCount) return;
        PlayerData.EnsureMinimumDeckSlotCount();
        PlayerData.SavePlayerData();
    }


    // Update is called once per frame
    void Update()
    {
        TickDeckScrollUserSessionEnd();
        SlotNavigator.UpdateFrame();
        if (GetBackpackProficiencyHelpDialog().IsOpen && Input.GetKeyDown(KeyCode.Escape))
            HideBackpackProficiencyHelp();
        else if (backpackInspectPanel != null && backpackInspectPanel.IsOpen && Input.GetKeyDown(KeyCode.Escape))
            HideBackpackCardInspect();
        backpackInspectPanel?.TickSwipeInput();
        if (deckNameEditPanel != null && deckNameEditPanel.activeSelf)
            RefreshDeckNameEditCharCounter();
    }

    private void LateUpdate()
    {
        ArcController.LateUpdateArc();
    }

    private bool DeckArcSmoothUsesLateUpdate()
    {
        return NewLayoutEnableDeckArc && deckArcHorizontalSmoothTime > 0f;
    }

    private void RequestDeckArcLayoutUnlessDeferredToLateUpdate(RectTransform content, bool oncePerFrame)
    {
        if (DeckArcSmoothUsesLateUpdate()) return;
        RequestDeckArcLayout(content, oncePerFrame);
    }

    private void TryApplyDeckLayoutLiveTuning()
    {
        ApplyNewLayoutRuntimeDefaults();
        if (!HasDeckLayoutTuningChanged()) return;
        bool spacingChanged = _lastDeckListCellSpacingY != deckListCellSpacingY;
        bool scaleChanged = !IsBuildbeckSceneActive() && _lastDeckListContentScalePercent != deckListContentScalePercent;
        bool parentOffsetChanged = IsBuildbeckSceneActive()
            ? Mathf.RoundToInt(_lastDeckArcParentOffsetY) != Mathf.RoundToInt(deckArcParentOffsetY)
            : (!Mathf.Approximately(_lastDeckArcParentOffsetX, deckArcParentOffsetX) ||
               !Mathf.Approximately(_lastDeckArcParentOffsetY, deckArcParentOffsetY));
        CacheDeckLayoutTuningValues();
        if (parentOffsetChanged)
            ApplyDeckParentLayoutOffsets();
        if (spacingChanged || scaleChanged)
        {
            if (scaleChanged)
                ApplyDeckListContentScale();
            EnsureDeckPanelSingleColumnLayout();
            RefreshScrollablePanels();
            RectTransform deckRt = deckPanel as RectTransform;
            if (deckRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(deckRt);
                RequestDeckArcLayoutUnlessDeferredToLateUpdate(deckRt, false);
            }
        }
        ApplyDeckLayoutToExistingCards();
    }

    private void ApplyNewLayoutRuntimeDefaults()
    {
        // Ignore legacy scene serialized tuning and force new layout baseline.
        namePlateHeightWidthRatio = NewLayoutNamePlateRatio;
        deckStatSquareScale = NewLayoutDeckStatSquareScale;
        statUnderlaySquareScale = NewLayoutStatCircleScale;
        attackStatOffsetX = NewLayoutAttackOffsetX;
        healthStatOffsetX = NewLayoutHealthOffsetX;
        statOffsetY = NewLayoutStatOffsetY;

        if (!runtimeLayoutDefaultsApplied)
        {
            CacheDeckLayoutTuningValues();
            runtimeLayoutDefaultsApplied = true;
        }
    }

    private bool HasDeckLayoutTuningChanged()
    {
        return
            !Mathf.Approximately(_lastNamePlateHeightWidthRatio, namePlateHeightWidthRatio) ||
            !Mathf.Approximately(_lastDeckStatSquareScale, deckStatSquareScale) ||
            !Mathf.Approximately(_lastStatUnderlaySquareScale, statUnderlaySquareScale) ||
            !Mathf.Approximately(_lastAttackStatOffsetX, attackStatOffsetX) ||
            !Mathf.Approximately(_lastHealthStatOffsetX, healthStatOffsetX) ||
            !Mathf.Approximately(_lastStatOffsetY, statOffsetY) ||
            _lastDeckListCellSpacingY != deckListCellSpacingY ||
            (!IsBuildbeckSceneActive() && _lastDeckListContentScalePercent != deckListContentScalePercent) ||
            _lastDeckListStatHalfGapLevel != deckListStatHalfGapLevel ||
            (IsBuildbeckSceneActive()
                ? Mathf.RoundToInt(_lastDeckArcParentOffsetY) != Mathf.RoundToInt(deckArcParentOffsetY)
                : (!Mathf.Approximately(_lastDeckArcParentOffsetX, deckArcParentOffsetX) ||
                   !Mathf.Approximately(_lastDeckArcParentOffsetY, deckArcParentOffsetY)));
    }

    private void CacheDeckLayoutTuningValues()
    {
        _lastNamePlateHeightWidthRatio = namePlateHeightWidthRatio;
        _lastDeckStatSquareScale = deckStatSquareScale;
        _lastStatUnderlaySquareScale = statUnderlaySquareScale;
        _lastAttackStatOffsetX = attackStatOffsetX;
        _lastHealthStatOffsetX = healthStatOffsetX;
        _lastStatOffsetY = statOffsetY;
        _lastDeckListCellSpacingY = deckListCellSpacingY;
        _lastDeckListContentScalePercent = deckListContentScalePercent;
        _lastDeckListStatHalfGapLevel = deckListStatHalfGapLevel;
        _lastDeckArcParentOffsetX = deckArcParentOffsetX;
        _lastDeckArcParentOffsetY = deckArcParentOffsetY;
    }

    private void ApplyDeckLayoutToExistingCards()
    {
        if (deckPanel == null) return;
        int n = deckPanel.childCount;
        for (int i = 0; i < n; i++)
        {
            Transform child = deckPanel.GetChild(i);
            CardDisplay display = FindCardDisplayOnRow(child);
            if (display == null) continue;

            Card c = display.card;
            EnsureDeckBottomSquareAndStatsLayout(display, c);

            if (c is SpellCard)
            {
                RemoveDeckStatBars(display);
            }
            else
            {
                EnsureDeckAttackValueRedBar(display);
                EnsureDeckHealthValueGreenBar(display);
            }
        }
    }

    private static void RemoveDeckStatBars(CardDisplay display)
    {
        if (display == null) return;
        Transform atkParent = display.attackText != null ? display.attackText.transform.parent : null;
        Transform hpParent = display.healthText != null ? display.healthText.transform.parent : null;
        if (atkParent != null)
        {
            Transform atkBar = atkParent.Find("AtkValueRedBar");
            if (atkBar != null) Destroy(atkBar.gameObject);
            Transform atkCircle = atkParent.Find("AtkValueCircle");
            if (atkCircle != null) Destroy(atkCircle.gameObject);
        }
        if (hpParent != null)
        {
            Transform hpBar = hpParent.Find("HpValueGreenBar");
            if (hpBar != null) Destroy(hpBar.gameObject);
            Transform hpCircle = hpParent.Find("HpValueCircle");
            if (hpCircle != null) Destroy(hpCircle.gameObject);
        }
    }

    public void SelectDeckSlot(int slotIndex)
    {
        EnsureCoreRefs();
        if (PlayerData == null) return;
        PlayerData.SetSelectedDeckSlot(slotIndex);
        PlayerData.SavePlayerData();

        ClearPanels();
        UpdateLibrary();
        if (showDeck) UpdateDeck();
        RefreshScrollablePanels();
        ForcePanelsScrollToTop();
        ForceRebuildPanelsLayout();
        RefreshDeckSlotTabVisual();

        SceneLoader loader = GetCachedSceneLoader();
        if (loader != null) loader.RefreshEnterBattleState(false);
    }

    public void SelectDeckSlot0() { SelectDeckSlot(0); }
    public void SelectDeckSlot1() { SelectDeckSlot(1); }
    public void SelectDeckSlot2() { SelectDeckSlot(2); }
    public void SelectDeckSlot3() { SelectDeckSlot(3); }
    public void SelectDeckSlot4() { SelectDeckSlot(4); }

    // Bind this to the "保存牌組" button.
    public void OnClickSaveDeckButton()
    {
        SaveDeckAndShowHint();
    }

    public void SaveDeckAndShowHint()
    {
        EnsureCoreRefs();
        if (PlayerData == null)
        {
            ShowDeckHint("保存失敗：找不到玩家資料");
            return;
        }

        PlayerData.SavePlayerData();
        ShowDeckHint("牌組已保存");
        SceneLoader loader = GetCachedSceneLoader();
        if (loader != null) loader.RefreshEnterBattleState(false);
    }

    public void ClearPanels()
    {
        DataController.ClearPanels();
    }

    public void UpdateLibrary()
    {
        DataController.UpdateLibrary();
    }

    public void UpdateDeck()
    {
        DataController.UpdateDeck();
    }

    public void UpdataCard(CardState state, int id)
    {
        DataController.UpdataCard(state, id);
    }

    private void ShowDeckHint(string message)
    {
        Debug.LogWarning(message);
        if (deckHintRoutine != null) StopCoroutine(deckHintRoutine);
        deckHintRoutine = StartCoroutine(ShowDeckHintRoutine(message));
    }

    private IEnumerator ShowDeckHintRoutine(string message)
    {
        EnsureTMPHintReady();
        EnsureLegacyHintTextReady();
        EnsureHintPanelReady();

        if (deckHintText != null)
        {
            if (hintTMPFont != null) deckHintText.font = hintTMPFont;
            deckHintText.text = message;
            deckHintText.color = Color.white;
            deckHintText.gameObject.SetActive(true);
        }
        else if (deckHintLegacyText != null)
        {
            if (hintDynamicFont != null) deckHintLegacyText.font = hintDynamicFont;
            else if (deckHintLegacyText.font == null) deckHintLegacyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            deckHintLegacyText.text = message;
            deckHintLegacyText.color = Color.white;
            deckHintLegacyText.gameObject.SetActive(true);
        }
        if (deckHintPanel != null) deckHintPanel.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        if (deckHintText != null) deckHintText.gameObject.SetActive(false);
        if (deckHintLegacyText != null) deckHintLegacyText.gameObject.SetActive(false);
        if (deckHintPanel != null) deckHintPanel.gameObject.SetActive(false);
        deckHintRoutine = null;
    }

    private void EnsureTMPHintReady()
    {
        if (hintTMPFont == null)
        {
            hintTMPFont = ResolveHintTMPFont();
        }

        if (deckHintText == null)
        {
            Canvas canvas = ResolvePrimaryUiCanvas();
            if (canvas != null)
            {
                GameObject obj = new GameObject("DeckHintTMPText", typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.transform.SetParent(canvas.transform, false);
                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(860f, 110f);
                deckHintText = obj.GetComponent<TextMeshProUGUI>();
                if (hintTMPFont != null) deckHintText.font = hintTMPFont;
                deckHintText.fontSize = 46;
                deckHintText.alignment = TextAlignmentOptions.Center;
                deckHintText.color = Color.white;
                deckHintText.gameObject.SetActive(false);
            }
        }
    }

    private TMP_FontAsset ResolveHintTMPFont()
    {
        CardDisplay fromDeck = deckCardPrefab != null ? deckCardPrefab.GetComponentInChildren<CardDisplay>(true) : null;
        if (fromDeck != null && fromDeck.nameText != null && fromDeck.nameText.font != null) return fromDeck.nameText.font;

        CardDisplay fromLibrary = librarycardPrefab != null ? librarycardPrefab.GetComponentInChildren<CardDisplay>(true) : null;
        if (fromLibrary != null && fromLibrary.nameText != null && fromLibrary.nameText.font != null) return fromLibrary.nameText.font;

        TextMeshProUGUI anyTMP = _cachedAnyTmpForFontProbe;
        if (anyTMP == null)
        {
            anyTMP = Object.FindFirstObjectByType<TextMeshProUGUI>();
            _cachedAnyTmpForFontProbe = anyTMP;
        }
        if (anyTMP != null && anyTMP.font != null) return anyTMP.font;
        return TMP_Settings.defaultFontAsset;
    }

    private void EnsureLegacyHintTextReady()
    {
        if (deckHintLegacyText == null)
        {
            Canvas canvas = ResolvePrimaryUiCanvas();
            if (canvas != null)
            {
                GameObject obj = new GameObject("DeckHintLegacyText", typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(canvas.transform, false);
                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(860f, 110f);
                deckHintLegacyText = obj.GetComponent<Text>();
                deckHintLegacyText.alignment = TextAnchor.MiddleCenter;
                deckHintLegacyText.fontSize = 44;
                deckHintLegacyText.color = Color.white;
                deckHintLegacyText.gameObject.SetActive(false);
            }
        }

        if (hintDynamicFont == null)
        {
            hintDynamicFont = Font.CreateDynamicFontFromOSFont(
                new string[] { "Microsoft JhengHei", "Microsoft YaHei", "PMingLiU", "MingLiU", "SimHei", "Arial Unicode MS" },
                36
            );
        }
    }

    private void EnsureHintPanelReady()
    {
        if (deckHintPanel != null) return;
        Canvas canvas = ResolvePrimaryUiCanvas();
        if (canvas == null) return;

        GameObject obj = new GameObject("DeckHintPanel", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(980f, 150f);

        deckHintPanel = obj.GetComponent<Image>();
        deckHintPanel.color = new Color(0f, 0f, 0f, 0.75f);
        deckHintPanel.raycastTarget = false;
        deckHintPanel.gameObject.SetActive(false);

        if (deckHintText != null) deckHintText.transform.SetParent(obj.transform, false);
        if (deckHintLegacyText != null) deckHintLegacyText.transform.SetParent(obj.transform, false);
    }

    /// <summary>
    /// Buildbeck：Canvas 直層上的場景用「DeckGen_Library」僅作為 prototype；進入 UI 後關閉，避免與複製到各卡上的實例重疊顯示。
    /// </summary>
    public void HideBuildbeckLibraryDeckGenCanvasPrototypeForRuntime()
    {
        if (!IsBuildbeckSceneActive()) return;
        GameObject proto = ResolveBuildbeckLibraryDeckGenCanvasPrototype();
        if (proto == null) return;
        DestroyDeckGenLibraryLegacyIdUnder(proto.transform);
        proto.SetActive(false);
    }

    private GameObject ResolveBuildbeckLibraryDeckGenCanvasPrototype()
    {
        if (_buildbeckLibraryDeckGenCanvasPrototype != null) return _buildbeckLibraryDeckGenCanvasPrototype;
        Canvas canvas = null;
        if (libraryPanel != null)
            canvas = libraryPanel.GetComponentInParent<Canvas>(true)?.rootCanvas;
        if (canvas == null)
            canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null) return null;
        Transform root = canvas.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c == null) continue;
            if (c.name != BuildbeckLibraryDeckGenRootName) continue;
            _buildbeckLibraryDeckGenCanvasPrototype = c.gameObject;
            return _buildbeckLibraryDeckGenCanvasPrototype;
        }

        return null;
    }

    private void EnsureLibraryCardDeckGenHierarchyForBuildbeck(GameObject libraryCardRoot)
    {
        if (!IsBuildbeckSceneActive() || libraryCardRoot == null) return;
        if (FindDeepChildByName(libraryCardRoot.transform, BuildbeckLibraryDeckGenRootName) != null) return;

        GameObject proto = ResolveBuildbeckLibraryDeckGenCanvasPrototype();
        if (proto == null) return;

        GameObject inst = Instantiate(proto, libraryCardRoot.transform, false);
        inst.name = BuildbeckLibraryDeckGenRootName;
        inst.SetActive(true);
        inst.transform.SetAsLastSibling();
        DestroyDeckGenLibraryLegacyIdUnder(inst.transform);
    }

    private static void CollectTransformsNamedExact(Transform t, string exactName, List<Transform> acc)
    {
        if (t == null || string.IsNullOrEmpty(exactName)) return;
        if (t.name == exactName) acc.Add(t);
        for (int i = 0; i < t.childCount; i++)
            CollectTransformsNamedExact(t.GetChild(i), exactName, acc);
    }

    /// <summary>
    /// 已棄用節點 <c>DeckGen_Library_id</c>（舊縮圖層）；於整棵子樹遞迴尋找並銷毀（不限直層）。請只使用 df/oi。
    /// </summary>
    private static void DestroyDeckGenLibraryLegacyIdUnder(Transform searchRoot)
    {
        if (searchRoot == null) return;
        const string legacyName = "DeckGen_Library_id";
        List<Transform> hits = new List<Transform>(2);
        CollectTransformsNamedExact(searchRoot, legacyName, hits);
        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i] == null) continue;
            Object.Destroy(hits[i].gameObject);
        }
    }

    /// <summary>
    /// 已棄用節點 <c>DeckGen_Deck</c>（舊牌組條縮圖層）；於整棵子樹遞迴尋找並銷毀。請使用 <c>DeckCardInfoStrip_mt_df</c>／<c>DeckCardInfoStrip_mt_oi</c>。
    /// </summary>
    private static void DestroyDeckGenDeckLegacyUnder(Transform searchRoot)
    {
        if (searchRoot == null) return;
        const string legacyName = "DeckGen_Deck";
        List<Transform> hits = new List<Transform>(4);
        CollectTransformsNamedExact(searchRoot, legacyName, hits);
        for (int i = 0; i < hits.Count; i++)
        {
            if (hits[i] == null) continue;
            Object.Destroy(hits[i].gameObject);
        }
    }

    /// <summary>
    /// 館藏卡：依擁有數切換 <c>DeckGen_Library_df</c>（擁有數 &lt; 2，含 1 張／單張分支如 <c>DeckGen_Library_堆疊數為1</c>）與 <c>DeckGen_Library_oi</c>（疊加≥2，數字顯示於 oi 子階層 TMP，2–99）。
    /// </summary>
    public void ApplyLibraryDeckGenStackPresentation(GameObject libraryCardRoot, int ownedCount)
    {
        if (libraryCardRoot == null) return;
        DestroyDeckGenLibraryLegacyIdUnder(libraryCardRoot.transform);

        Transform deckGen = FindDeepChildByName(libraryCardRoot.transform, BuildbeckLibraryDeckGenRootName);
        if (deckGen == null) return;

        DestroyDeckGenLibraryLegacyIdUnder(deckGen);

        Transform df = FindDeepChildByName(deckGen, BuildbeckLibraryDeckGenDefaultName);
        Transform oi = FindDeepChildByName(deckGen, BuildbeckLibraryDeckGenOverlayOiName);
        if (oi == null) oi = FindDeepChildByName(deckGen, BuildbeckLibraryDeckGenOverlayOlLegacyName);

        bool hasNewChrome = df != null || oi != null;
        CardCounter cc = libraryCardRoot.GetComponentInChildren<CardCounter>(true);
        if (cc != null && cc.counterText != null)
        {
            if (hasNewChrome)
                cc.counterText.gameObject.SetActive(false);
            else
                cc.counterText.gameObject.SetActive(true);
        }

        if (!hasNewChrome)
            return;

        CardDisplay cardDisplay = libraryCardRoot.GetComponentInChildren<CardDisplay>(true);
        if (cardDisplay != null && cardDisplay.nameText != null)
            cardDisplay.nameText.gameObject.SetActive(buildbeckLibraryDeckGenShowCardName);

        // 僅 ≥2 張使用 oi；=1 張（含設計師將 df 包在「堆疊數為1」等子階層）一律走 df 分支。
        bool useStackOverlayOi = ownedCount >= BuildbeckLibraryDeckGenStackDisplayMin;

        if (df != null)
        {
            Transform dfBranch = LibraryDeckGenDirectBranchUnder(deckGen, df);
            if (!useStackOverlayOi)
            {
                if (dfBranch != null) dfBranch.gameObject.SetActive(true);
                df.gameObject.SetActive(true);
            }
            else if (dfBranch != null)
            {
                dfBranch.gameObject.SetActive(false);
            }
        }

        if (oi != null)
        {
            Transform oiBranch = LibraryDeckGenDirectBranchUnder(deckGen, oi);
            if (useStackOverlayOi)
            {
                if (oiBranch != null) oiBranch.gameObject.SetActive(true);
                oi.gameObject.SetActive(true);
                int shown = Mathf.Clamp(ownedCount, BuildbeckLibraryDeckGenStackDisplayMin, BuildbeckLibraryDeckGenStackDisplayMax);
                TextMeshProUGUI tmp = oi.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) tmp.text = shown.ToString();
            }
            else if (oiBranch != null)
            {
                oiBranch.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 在變更 <see cref="buildbeckLibraryDeckGenShowCardName"/> 後，重新套用所有館藏卡的名稱顯示（不重載資料）。
    /// </summary>
    public void RefreshLibraryDeckGenCardNameVisibility()
    {
        if (!IsBuildbeckSceneActive() || PlayerData == null) return;
        foreach (var kv in libraryDic)
        {
            if (kv.Value == null) continue;
            int n = PlayerData.GetCollectionCount(kv.Key);
            ApplyLibraryDeckGenStackPresentation(kv.Value, n);
        }
    }

    /// <summary>
    /// Buildbeck：Canvas 直層上的牌組條原型（<c>DeckGen_Deck</c> 或含 mt 的 <c>DeckCardInfoStrip</c>）僅供複製；進入 UI 後關閉，避免與畫面重疊。
    /// </summary>
    public void HideBuildbeckDeckStripStackCanvasPrototypeForRuntime()
    {
        if (!IsBuildbeckSceneActive()) return;
        GameObject proto = ResolveBuildbeckDeckStripStackCanvasPrototype();
        if (proto == null) return;
        proto.SetActive(false);
    }

    private GameObject ResolveBuildbeckDeckStripStackCanvasPrototype()
    {
        if (_buildbeckDeckStripStackCanvasPrototype != null) return _buildbeckDeckStripStackCanvasPrototype;
        Canvas canvas = null;
        if (deckPanel != null)
            canvas = deckPanel.GetComponentInParent<Canvas>(true)?.rootCanvas;
        if (canvas == null)
            canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null) return null;
        Transform root = canvas.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c == null) continue;
            if (c.name != BuildbeckDeckStripPrototypeCanvasName) continue;
            _buildbeckDeckStripStackCanvasPrototype = c.gameObject;
            return _buildbeckDeckStripStackCanvasPrototype;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c == null) continue;
            if (c.name != BuildbeckDeckStripCanvasTemplateDirectChildName) continue;
            if (FindDeepChildByName(c, BuildbeckDeckStripMtDefaultName) == null) continue;
            if (FindDeepChildByName(c, BuildbeckDeckStripMtOverlayName) == null) continue;
            _buildbeckDeckStripStackCanvasPrototype = c.gameObject;
            return _buildbeckDeckStripStackCanvasPrototype;
        }

        return null;
    }

    private void EnsureDeckCardStripStackHierarchyForBuildbeck(GameObject deckCardRoot)
    {
        if (!IsBuildbeckSceneActive() || deckCardRoot == null) return;
        Transform strip = FindDeepChildByName(deckCardRoot.transform, BuildbeckDeckStripCanvasTemplateDirectChildName);
        if (strip == null) return;

        Transform existingDf = FindDeepChildByName(strip, BuildbeckDeckStripMtDefaultName);
        Transform existingOi = FindDeepChildByName(strip, BuildbeckDeckStripMtOverlayName);
        if (existingDf != null && existingOi != null)
        {
            DestroyDeckGenDeckLegacyUnder(deckCardRoot.transform);
            DestroyDeckGenDeckLegacyUnder(strip);
            return;
        }

        DestroyDeckGenDeckLegacyUnder(deckCardRoot.transform);
        DestroyDeckGenDeckLegacyUnder(strip);

        GameObject proto = ResolveBuildbeckDeckStripStackCanvasPrototype();
        if (proto == null) return;

        if (proto.name == BuildbeckDeckStripCanvasTemplateDirectChildName)
        {
            Transform protoDf = FindDeepChildByName(proto.transform, BuildbeckDeckStripMtDefaultName);
            Transform protoOi = FindDeepChildByName(proto.transform, BuildbeckDeckStripMtOverlayName);
            if (protoDf == null || protoOi == null) return;
            GameObject dfInst = Instantiate(protoDf.gameObject, strip, false);
            dfInst.name = BuildbeckDeckStripMtDefaultName;
            GameObject oiInst = Instantiate(protoOi.gameObject, strip, false);
            oiInst.name = BuildbeckDeckStripMtOverlayName;
            dfInst.SetActive(true);
            oiInst.SetActive(true);
            dfInst.transform.SetAsLastSibling();
            oiInst.transform.SetAsLastSibling();
            return;
        }

        GameObject inst = Instantiate(proto, strip, false);
        inst.name = BuildbeckDeckStripStackInstantiatedRootName;
        inst.SetActive(true);
        inst.transform.SetAsLastSibling();
        DestroyDeckGenDeckLegacyUnder(inst.transform);
    }

    private static TextMeshProUGUI ResolveDeckStripStackCountText(Transform mtOiRoot)
    {
        if (mtOiRoot == null) return null;
        Transform region = FindDeepChildByName(mtOiRoot, BuildbeckDeckStripStackOiRegionName);
        if (region != null)
        {
            TextMeshProUGUI t = region.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) return t;
        }

        TextMeshProUGUI[] tmps = mtOiRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            TextMeshProUGUI u = tmps[i];
            if (u == null) continue;
            Transform par = u.transform.parent;
            if (par != null && par.name == BuildbeckDeckStripStackOiRegionName)
                return u;
        }

        for (int i = 0; i < tmps.Length; i++)
        {
            TextMeshProUGUI u = tmps[i];
            if (u == null) continue;
            string n = u.gameObject.name;
            if (n == BuildbeckDeckStripMtCardNameChild || n == BuildbeckDeckStripMtAtkChild || n == BuildbeckDeckStripMtHpChild)
                continue;
            return u;
        }

        return null;
    }

    private static void CopyTmpDisplayState(TextMeshProUGUI src, TextMeshProUGUI dst)
    {
        if (src == null || dst == null) return;
        dst.text = src.text;
        dst.richText = src.richText;
        dst.overflowMode = src.overflowMode;
        dst.color = src.color;
        dst.fontSize = src.fontSize;
        dst.fontWeight = src.fontWeight;
        dst.fontStyle = src.fontStyle;
        dst.horizontalAlignment = src.horizontalAlignment;
        dst.verticalAlignment = src.verticalAlignment;
        dst.enableWordWrapping = src.enableWordWrapping;
        dst.gameObject.SetActive(src.gameObject.activeSelf);
    }

    /// <summary>將 <c>mt_df</c> 上名稱／攻防 TMP 狀態複製到 <c>mt_oi</c> 對應節點（疊加顯示時內容一致）。</summary>
    public void SyncBuildbeckDeckStripMtOiPrimaryTexts(GameObject deckCardRoot)
    {
        if (deckCardRoot == null || !IsBuildbeckSceneActive()) return;
        Transform strip = FindDeepChildByName(deckCardRoot.transform, BuildbeckDeckStripCanvasTemplateDirectChildName);
        if (strip == null) return;
        Transform df = FindDeepChildByName(strip, BuildbeckDeckStripMtDefaultName);
        Transform oi = FindDeepChildByName(strip, BuildbeckDeckStripMtOverlayName);
        if (df == null || oi == null) return;

        TextMeshProUGUI dfName = FindDeepChildByName(df, BuildbeckDeckStripMtCardNameChild)?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI oiName = FindDeepChildByName(oi, BuildbeckDeckStripMtCardNameChild)?.GetComponent<TextMeshProUGUI>();
        CopyTmpDisplayState(dfName, oiName);

        TextMeshProUGUI dfAtk = FindDeepChildByName(df, BuildbeckDeckStripMtAtkChild)?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI oiAtk = FindDeepChildByName(oi, BuildbeckDeckStripMtAtkChild)?.GetComponent<TextMeshProUGUI>();
        CopyTmpDisplayState(dfAtk, oiAtk);

        TextMeshProUGUI dfHp = FindDeepChildByName(df, BuildbeckDeckStripMtHpChild)?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI oiHp = FindDeepChildByName(oi, BuildbeckDeckStripMtHpChild)?.GetComponent<TextMeshProUGUI>();
        CopyTmpDisplayState(dfHp, oiHp);
    }

    private void RewireBuildbeckDeckCardStripTextRefs(GameObject deckCardRoot)
    {
        if (deckCardRoot == null || !IsBuildbeckSceneActive()) return;
        Transform mtDf = FindDeepChildByName(deckCardRoot.transform, BuildbeckDeckStripMtDefaultName);
        if (mtDf == null) return;

        TextMeshProUGUI nameTmp = FindDeepChildByName(mtDf, BuildbeckDeckStripMtCardNameChild)?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI atkTmp = FindDeepChildByName(mtDf, BuildbeckDeckStripMtAtkChild)?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI hpTmp = FindDeepChildByName(mtDf, BuildbeckDeckStripMtHpChild)?.GetComponent<TextMeshProUGUI>();

        CardDisplay display = deckCardRoot.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.nameText = nameTmp;
            display.attackText = atkTmp;
            display.healthText = hpTmp;
        }

        DeckCardInfoStrip stripComp = deckCardRoot.GetComponentInChildren<DeckCardInfoStrip>(true);
        if (stripComp != null)
            stripComp.AssignReferences(nameTmp, atkTmp, hpTmp);
    }

    /// <summary>
    /// 牌組列：依張數切換 <c>DeckCardInfoStrip_mt_df</c>（預設）與 <c>DeckCardInfoStrip_mt_oi</c>（疊加≥2，子階層 TMP 顯示 2–99）。
    /// </summary>
    public void ApplyDeckStripStackPresentation(GameObject deckCardRoot, int deckCount)
    {
        if (deckCardRoot == null || !IsBuildbeckSceneActive()) return;

        DestroyDeckGenDeckLegacyUnder(deckCardRoot.transform);

        Transform strip = FindDeepChildByName(deckCardRoot.transform, BuildbeckDeckStripCanvasTemplateDirectChildName);
        if (strip == null) return;

        DestroyDeckGenDeckLegacyUnder(strip);

        Transform df = FindDeepChildByName(strip, BuildbeckDeckStripMtDefaultName);
        Transform oi = FindDeepChildByName(strip, BuildbeckDeckStripMtOverlayName);
        bool hasNewChrome = df != null || oi != null;

        CardCounter cc = deckCardRoot.GetComponentInChildren<CardCounter>(true);
        if (cc != null && cc.counterText != null)
        {
            if (hasNewChrome)
                cc.counterText.gameObject.SetActive(false);
            else
                cc.counterText.gameObject.SetActive(true);
        }

        if (!hasNewChrome)
            return;

        bool stacks = deckCount >= BuildbeckDeckStripStackDisplayMin;
        if (df != null) df.gameObject.SetActive(!stacks);
        if (oi != null)
        {
            oi.gameObject.SetActive(stacks);
            if (stacks)
            {
                int shown = Mathf.Clamp(deckCount, BuildbeckDeckStripStackDisplayMin, BuildbeckDeckStripStackDisplayMax);
                TextMeshProUGUI stackTmp = ResolveDeckStripStackCountText(oi);
                if (stackTmp != null) stackTmp.text = shown.ToString();
            }
        }

        RefreshDeckStripThumbPortrait(deckCardRoot);
    }

    private void RefreshDeckStripThumbPortrait(GameObject deckCardRoot)
    {
        if (deckCardRoot == null) return;
        CardDisplay display = deckCardRoot.GetComponentInChildren<CardDisplay>(true);
        if (display == null || display.card == null) return;
        display.ShowCard();
    }

    public void CreateCard(int id, CardState state)
    {
        EnsureCoreRefs();
        EnsureDeckUIRefs();
        if (PlayerData == null || CardStore == null)
        {
            Debug.LogWarning("DeckManager.CreateCard: missing PlayerData/CardStore.");
            return;
        }

        Transform targetPanel;
        GameObject targetPrefab;
        Dictionary<int, GameObject> targetDic;

        if (state == CardState.Library)
        {
            targetPanel = libraryPanel;
            targetPrefab = librarycardPrefab;
            targetDic = libraryDic;
        }
        else
        {
            targetPanel = deckPanel;
            targetPrefab = deckCardPrefab;
            targetDic = deckDic;
        }

        if (targetPanel == null || targetPrefab == null)
        {
            string panelName = targetPanel == null ? "null" : targetPanel.name;
            string prefabName = targetPrefab == null ? "null" : targetPrefab.name;
            Debug.LogWarning("DeckManager.CreateCard: panel/prefab missing for state=" + state + " panel=" + panelName + " prefab=" + prefabName);
            return;
        }

        int countForId = state == CardState.Library
            ? PlayerData.GetCollectionCount(id)
            : PlayerData.GetSelectedDeckCount(id);
        if (countForId <= 0)
        {
            Debug.LogWarning("DeckManager.CreateCard: no cards for id=" + id + " state=" + state);
            return;
        }
        if (CardStore.GetCardById(id) == null)
        {
            Debug.LogWarning("DeckManager.CreateCard: unknown card id=" + id);
            return;
        }
        if (targetDic.ContainsKey(id) && targetDic[id] != null)
        {
            if (state == CardState.Deck)
            {
                GameObject existing = targetDic[id];
                CardCounter existingCounter = existing.GetComponentInChildren<CardCounter>(true);
                if (existingCounter != null) existingCounter.SetCounter(countForId);
                if (IsBuildbeckSceneActive())
                {
                    ApplyDeckStripStackPresentation(existing, countForId);
                    SyncBuildbeckDeckStripMtOiPrimaryTexts(existing);
                }
                else
                    RefreshDeckStripThumbPortrait(existing);
            }

            return;
        }

        GameObject newCard = Instantiate(targetPrefab, targetPanel);
        newCard.name = $"DeckGen_{state}_id{id}";
        RectTransform newCardRt = newCard.GetComponent<RectTransform>();
        if (newCardRt != null)
        {
            newCardRt.localScale = Vector3.one;
            newCardRt.localRotation = Quaternion.identity;
            newCardRt.anchoredPosition = Vector2.zero;
        }
        CanvasGroup newCg = newCard.GetComponent<CanvasGroup>();
        if (newCg != null) newCg.alpha = 1f;

        ZoomUI zoom = newCard.GetComponentInChildren<ZoomUI>(true);
        if (zoom != null)
        {
            if (state == CardState.Library)
            {
                // Library cards: handled by ClickCard (move up + fade), no zoom.
                zoom.clickToToggle = false;
                zoom.hoverToPreview = false;
                zoom.clickPulseOnly = false;
                zoom.raiseToFrontWhenZoomed = false;
                zoom.enabled = false;
            }
            else
            {
                // Deck cards: use ZoomUI click pulse only.
                zoom.clickToToggle = false;
                zoom.hoverToPreview = false;
                zoom.clickPulseOnly = true;
                zoom.raiseToFrontWhenZoomed = false;
                zoom.enabled = true;
            }
        }

        var click = newCard.GetComponentInChildren<ClickCard>();
        if (click != null)
        {
            click.state = state;
            click.deckManager = enableInteraction ? this : null;
            click.enabled = enableInteraction;
        }

        CardCounter counter = newCard.GetComponentInChildren<CardCounter>();
        if (counter != null) counter.SetCounter(countForId);

        if (state == CardState.Library && IsBuildbeckSceneActive())
            EnsureLibraryCardDeckGenHierarchyForBuildbeck(newCard);

        if (state == CardState.Deck && IsBuildbeckSceneActive())
        {
            EnsureDeckCardStripStackHierarchyForBuildbeck(newCard);
            RewireBuildbeckDeckCardStripTextRefs(newCard);
        }

        var display = newCard.GetComponentInChildren<CardDisplay>();
        if (display != null)
        {
            Card c = CardStore.GetCardById(id);
            if (c != null) display.SetCard(c);
            if (state == CardState.Deck)
            {
                EnsureDeckBottomSquareAndStatsLayout(display, c);
                // Spell cards in deck panel should not render ATK/HP color plates.
                if (!(c is SpellCard))
                {
                    EnsureDeckAttackValueRedBar(display);
                    EnsureDeckHealthValueGreenBar(display);
                }

                if (IsBuildbeckSceneActive())
                    SyncBuildbeckDeckStripMtOiPrimaryTexts(newCard);
            }
        }

        if (state == CardState.Library)
            ApplyLibraryDeckGenStackPresentation(newCard, countForId);
        else if (state == CardState.Deck && IsBuildbeckSceneActive())
        {
            ApplyDeckStripStackPresentation(newCard, countForId);
        }

        targetDic[id] = newCard;
    }

    private void EnsureDeckBottomSquareAndStatsLayout(CardDisplay display, Card card)
    {
        if (display == null) return;
        ApplyDeckNameTextStyle(display);

        // New layout rule: spell cards don't render ATK/HP badges.
        if (card is SpellCard)
        {
            if (display.attackText != null) display.attackText.gameObject.SetActive(false);
            if (display.healthText != null) display.healthText.gameObject.SetActive(false);
            RemoveDeckStatBars(display);
            return;
        }

        if (display.attackText != null) display.attackText.gameObject.SetActive(true);
        if (display.healthText != null) display.healthText.gameObject.SetActive(true);
    }

    private static void ApplyDeckNameTextStyle(CardDisplay display)
    {
        if (display == null || display.nameText == null) return;
        display.nameText.fontSize = Mathf.Clamp(display.nameText.fontSize, 20f, 30f);
        display.nameText.alignment = TextAlignmentOptions.Center;
        display.nameText.color = new Color(0.16f, 0.16f, 0.16f, 1f);
    }

    private static float GetSharedStatBadgeSize(TextMeshProUGUI statText)
    {
        float fontSize = statText != null ? Mathf.Max(1f, statText.fontSize) : 24f;
        // Use one shared sizing rule for ATK/HP; large enough for up to 3 digits.
        return Mathf.Max(NewLayoutStatBadgeMinSize, fontSize * NewLayoutStatBadgeSizeByFontRatio);
    }

    private float GetDeckListStatHalfGapPixels()
    {
        int L = Mathf.Clamp(deckListStatHalfGapLevel, DeckListStatHalfGapLevelMin, DeckListStatHalfGapLevelMax);
        int span = DeckListStatHalfGapLevelMax - DeckListStatHalfGapLevelMin;
        if (span <= 0) return DeckListStatHalfGapPxMin;
        return Mathf.Lerp(DeckListStatHalfGapPxMin, DeckListStatHalfGapPxMax, (L - DeckListStatHalfGapLevelMin) / (float)span);
    }

    private static RectTransform ResolveDeckCardInfoLayoutRoot(CardDisplay display)
    {
        if (display == null) return null;
        DeckCardInfoStrip strip = display.GetComponentInChildren<DeckCardInfoStrip>(true);
        if (strip != null)
        {
            RectTransform rt = strip.transform as RectTransform;
            if (rt != null) return rt;
        }

        return display.GetComponent<RectTransform>();
    }

    private void EnsureDeckAttackValueRedBar(CardDisplay display)
    {
        if (display == null || display.attackText == null) return;
        RectTransform attackRt = display.attackText.rectTransform;
        if (attackRt == null) return;
        if (IsUnderDeckCardInfoStripMtDf(attackRt))
        {
            display.attackText.alignment = TextAlignmentOptions.Center;
            return;
        }

        RectTransform layoutRoot = ResolveDeckCardInfoLayoutRoot(display);
        if (layoutRoot == null) return;

        Transform existingBar = FindDeepChildByName(display.transform, AtkBadgeName);
        RectTransform barRt;
        Image barImg;

        if (existingBar != null)
        {
            barRt = existingBar as RectTransform;
            barImg = existingBar.GetComponent<Image>();
        }
        else
        {
            GameObject barObj = new GameObject(AtkBadgeName, typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(layoutRoot, false);
            barRt = barObj.GetComponent<RectTransform>();
            barImg = barObj.GetComponent<Image>();
        }

        if (barRt == null || barImg == null) return;

        if (attackRt.parent != layoutRoot)
            attackRt.SetParent(layoutRoot, false);
        if (barRt.parent != layoutRoot)
            barRt.SetParent(layoutRoot, false);

        attackRt.anchorMin = new Vector2(0.5f, 0.5f);
        attackRt.anchorMax = new Vector2(0.5f, 0.5f);
        attackRt.pivot = new Vector2(0.5f, 0.5f);
        float halfGapPx = GetDeckListStatHalfGapPixels();
        attackRt.anchoredPosition = new Vector2(-halfGapPx, NewLayoutStatBadgeYOffset);
        float badgeSize = GetSharedStatBadgeSize(display.attackText);
        attackRt.sizeDelta = new Vector2(badgeSize, badgeSize);

        barRt.anchorMin = attackRt.anchorMin;
        barRt.anchorMax = attackRt.anchorMax;
        barRt.pivot = attackRt.pivot;
        barRt.sizeDelta = new Vector2(badgeSize, badgeSize);
        barRt.anchoredPosition = attackRt.anchoredPosition;
        barRt.localScale = Vector3.one;
        barRt.localRotation = Quaternion.identity;

        // ATK uses white circle style from new layout.
        barImg.color = new Color(1f, 1f, 1f, 0.95f);
        barImg.type = Image.Type.Simple;
        barImg.raycastTarget = false;
        display.attackText.alignment = TextAlignmentOptions.Center;
        display.attackText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        display.attackText.fontSize = 24f;
        attackRt.sizeDelta = new Vector2(badgeSize, badgeSize);

        // Keep bar beneath ATK value text by sibling order in same parent.
        int attackSibling = attackRt.GetSiblingIndex();
        barRt.SetSiblingIndex(Mathf.Max(0, attackSibling));
    }

    private void EnsureDeckHealthValueGreenBar(CardDisplay display)
    {
        if (display == null || display.healthText == null) return;
        RectTransform healthRt = display.healthText.rectTransform;
        if (healthRt == null) return;
        if (IsUnderDeckCardInfoStripMtDf(healthRt))
        {
            display.healthText.alignment = TextAlignmentOptions.Center;
            return;
        }

        RectTransform layoutRoot = ResolveDeckCardInfoLayoutRoot(display);
        if (layoutRoot == null) return;

        Transform existingBar = FindDeepChildByName(display.transform, HpBadgeName);
        RectTransform barRt;
        Image barImg;

        if (existingBar != null)
        {
            barRt = existingBar as RectTransform;
            barImg = existingBar.GetComponent<Image>();
        }
        else
        {
            GameObject barObj = new GameObject(HpBadgeName, typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(layoutRoot, false);
            barRt = barObj.GetComponent<RectTransform>();
            barImg = barObj.GetComponent<Image>();
        }

        if (barRt == null || barImg == null) return;

        if (healthRt.parent != layoutRoot)
            healthRt.SetParent(layoutRoot, false);
        if (barRt.parent != layoutRoot)
            barRt.SetParent(layoutRoot, false);

        healthRt.anchorMin = new Vector2(0.5f, 0.5f);
        healthRt.anchorMax = new Vector2(0.5f, 0.5f);
        healthRt.pivot = new Vector2(0.5f, 0.5f);
        float halfGapPx = GetDeckListStatHalfGapPixels();
        healthRt.anchoredPosition = new Vector2(halfGapPx, NewLayoutStatBadgeYOffset);
        float badgeSize = GetSharedStatBadgeSize(display.healthText);
        healthRt.sizeDelta = new Vector2(badgeSize, badgeSize);

        barRt.anchorMin = healthRt.anchorMin;
        barRt.anchorMax = healthRt.anchorMax;
        barRt.pivot = healthRt.pivot;
        barRt.sizeDelta = new Vector2(badgeSize, badgeSize);
        barRt.anchoredPosition = healthRt.anchoredPosition;
        barRt.localScale = Vector3.one;
        barRt.localRotation = Quaternion.identity;

        // HP uses white circle style from new layout.
        barImg.color = new Color(1f, 1f, 1f, 0.95f);
        barImg.type = Image.Type.Simple;
        barImg.raycastTarget = false;
        display.healthText.alignment = TextAlignmentOptions.Center;
        display.healthText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        display.healthText.fontSize = 24f;
        healthRt.sizeDelta = new Vector2(badgeSize, badgeSize);

        int healthSibling = healthRt.GetSiblingIndex();
        barRt.SetSiblingIndex(Mathf.Max(0, healthSibling));
    }

    private void EnsureCoreRefs()
    {
        if (DataManager == null)
        {
            GameObject dm = SceneSearchUtil.FindSceneObject(SceneManager.GetActiveScene(), "DataManager");
            if (dm == null) dm = GameObject.Find("DataManager");
            if (dm != null) DataManager = dm;
        }

        if (CardStore == null && DataManager != null)
            CardStore = DataManager.GetComponent<CardStore>();

        PlayerData canonical = PlayerData.ResolveCanonical();
        if (canonical != null)
            PlayerData = canonical;
        else if (PlayerData == null && DataManager != null)
            PlayerData = DataManager.GetComponent<PlayerData>();

        if (CardStore == null)
            CardStore = UnityEngine.Object.FindFirstObjectByType<CardStore>();

        if (DataManager == null && PlayerData != null)
            DataManager = PlayerData.gameObject;
    }

    private void EnsureDeckUIRefs()
    {
        ListViewController.EnsureDeckUIRefs();
    }

    private void ApplyDeckListContentScale()
    {
        if (deckPanel == null) return;
        if (IsBuildbeckSceneActive())
        {
            deckPanel.localScale = Vector3.one;
            return;
        }

        int p = Mathf.Clamp(deckListContentScalePercent, 50, 200);
        float s = p / 100f;
        deckPanel.localScale = new Vector3(s, s, 1f);
    }

    private void EnsureDeckArcPresenter()
    {
        if (_deckArcPresenter != null) return;
        _deckArcPresenter = GetComponent<DeckArcPresenter>();
        if (_deckArcPresenter == null)
            _deckArcPresenter = gameObject.AddComponent<DeckArcPresenter>();
    }

    private void EnsureDeckPanelSingleColumnLayout()
    {
        if (deckPanel == null) return;

        GridLayoutGroup grid = deckPanel.GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.childAlignment = TextAnchor.UpperCenter;
        float cellH = ComputeDeckPanelGridCellHeight(grid);
        int cellHInt = Mathf.Max(1, Mathf.RoundToInt(cellH));
        grid.cellSize = new Vector2(NewLayoutDeckCellWidthPx, cellHInt);
        grid.spacing = new Vector2(0f, deckListCellSpacingY);
        // Grid handles list layout + spacing in container (including Y distribution).
        grid.enabled = true;
    }

    /// <summary>
    /// Deck list row height: prefer the layout constant, but never larger than the scroll viewport
    /// (minus grid padding) so a row fits inside the visible deck area on small layouts.
    /// </summary>
    private float ComputeDeckPanelGridCellHeight(GridLayoutGroup grid)
    {
        const float minCell = 56f;
        const float inset = 8f;
        float preferred = NewLayoutDeckCellHeight;
        float vh = GetDeckListViewportHeight();
        if (vh < 2f)
            return preferred;

        float padY = grid != null ? grid.padding.top + grid.padding.bottom : 0f;
        float byViewport = vh - padY - inset;
        if (byViewport < minCell)
            return Mathf.Min(preferred, Mathf.Max(1f, vh - padY - 1f));

        return Mathf.Min(preferred, Mathf.Max(minCell, byViewport));
    }

    private float GetDeckListViewportHeight()
    {
        if (_deckPanelScrollRect != null && _deckPanelScrollRect.viewport != null)
            return Mathf.Max(0f, _deckPanelScrollRect.viewport.rect.height);
        if (deckPanel != null && deckPanel.parent is RectTransform vp)
            return Mathf.Max(0f, vp.rect.height);
        return 0f;
    }

    private static Vector2 ResolveDeckViewportAnchorMaxForScene()
    {
        return NewLayoutDeckViewportAnchorMax;
    }

    private void ApplyDeckViewportForNewLayout()
    {
        RectTransform viewport = null;
        if (_deckPanelScrollRect != null && _deckPanelScrollRect.viewport != null)
            viewport = _deckPanelScrollRect.viewport;
        else if (deckPanel != null)
            viewport = deckPanel.parent as RectTransform;
        if (viewport == null) return;

        if (IsBuildbeckSceneActive())
        {
            viewport.anchorMin = new Vector2(1f, 0f);
            viewport.anchorMax = new Vector2(1f, 0f);
            viewport.pivot = new Vector2(1f, 0f);
            int yTune = Mathf.RoundToInt(deckArcParentOffsetY);
            viewport.anchoredPosition = new Vector2(-BuildbeckDeckViewportRightInsetPx, BuildbeckDeckViewportBottomPx + yTune);
            viewport.sizeDelta = new Vector2(BuildbeckDeckViewportWidthPx, BuildbeckDeckViewportHeightPx);
            viewport.localEulerAngles = Vector3.zero;
            viewport.localScale = Vector3.one;
            return;
        }

        viewport.anchorMin = NewLayoutDeckViewportAnchorMin;
        viewport.anchorMax = ResolveDeckViewportAnchorMaxForScene();
        Vector2 parentShift = new Vector2(deckArcParentOffsetX, 0f);
        viewport.offsetMin = NewLayoutDeckViewportOffsetMin + parentShift;
        viewport.offsetMax = NewLayoutDeckViewportOffsetMax + parentShift;
    }

    private void ApplyDeckListParentVerticalOffset()
    {
        if (deckPanel == null) return;
        if (IsBuildbeckSceneActive()) return;

        RectTransform parentRt = deckPanel.parent as RectTransform;
        if (parentRt == null) return;
        if (!_deckListParentAnchoredYBaselineCaptured)
        {
            _deckListParentAnchoredYBaseline = parentRt.anchoredPosition.y;
            _deckListParentAnchoredYBaselineCaptured = true;
        }

        Vector2 ap = parentRt.anchoredPosition;
        ap.y = _deckListParentAnchoredYBaseline + deckArcParentOffsetY;
        parentRt.anchoredPosition = ap;
    }

    private void ApplyDeckParentLayoutOffsets()
    {
        ApplyDeckViewportForNewLayout();
        ApplyDeckListParentVerticalOffset();
        ApplyDeckListContentScale();
        ApplyBuildbeckLibraryGridViewportLayoutIfNeeded();
    }

    /// <summary>
    /// Buildbeck：將「Library Grid Viewport」強制為<strong>真理座標</strong>——錨定左下單點、僅用
    /// <see cref="RectTransform.anchoredPosition"/> 與 <see cref="RectTransform.sizeDelta"/> 的常數邏輯像素，
    /// 不經比例伸縮、不寫 offsetMin/offsetMax 混用 stretch；並固定 <c>localScale</c>、<c>rotation</c>、<c>z</c>。
    /// </summary>
    public static void ApplyBuildbeckLibraryGridViewportTo(RectTransform viewport)
    {
        if (viewport == null) return;
        viewport.anchorMin = new Vector2(0f, 0f);
        viewport.anchorMax = new Vector2(0f, 0f);
        viewport.pivot = new Vector2(0f, 0f);
        viewport.localScale = Vector3.one;
        viewport.localRotation = Quaternion.identity;
        float x = BuildbeckLibraryGridViewportLeftInsetPx +
                  BuildbeckLibraryGridViewportLeftBreathingPx +
                  BuildbeckLibraryGridViewportCenterPointOffsetXPx +
                  BuildbeckLibraryGridViewportExtraOffsetXPx;
        float y = BuildbeckLibraryGridViewportBottomInsetPx;
        viewport.anchoredPosition3D = new Vector3(x, y, 0f);
        viewport.sizeDelta = new Vector2(BuildbeckLibraryGridViewportWidthPx, BuildbeckLibraryGridViewportHeightPx);
        // 父層若有 LayoutGroup，避免每幀覆寫本視窗的 Rect；仍只信 sizeDelta / anchoredPosition。
        LayoutElement le = viewport.GetComponent<LayoutElement>();
        if (le == null)
            le = viewport.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    private void ApplyBuildbeckLibraryGridViewportLayoutIfNeeded()
    {
        if (!IsBuildbeckSceneActive()) return;
        if (libraryPanel != null && IsHorizontalBagPanelName(libraryPanel)) return;
        if (_libraryPanelScrollRect == null || _libraryPanelScrollRect.viewport == null) return;
        ApplyBuildbeckLibraryGridViewportTo(_libraryPanelScrollRect.viewport);
    }

    private void AlignLibraryBottomToGoButtonTop()
    {
        RectTransform libraryGridRt = libraryPanel as RectTransform;
        if (libraryGridRt == null) return;
        RectTransform libraryRootRt = libraryGridRt.parent as RectTransform;
        if (libraryRootRt == null) return;
        RectTransform goButtonRt = ResolveGoButtonRectTransform();
        if (goButtonRt == null) return;

        Canvas canvas = GetCachedRuntimeUiCanvas();
        RectTransform canvasRt = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRt == null) return;

        Vector3[] corners = new Vector3[4];
        libraryRootRt.GetWorldCorners(corners);
        float libTopY = canvasRt.InverseTransformPoint(corners[1]).y;
        goButtonRt.GetWorldCorners(corners);
        float goTopY = canvasRt.InverseTransformPoint(corners[1]).y;

        float newHeight = Mathf.Max(120f, libTopY - goTopY);
        float newCenterY = goTopY + (newHeight * 0.5f);

        // Buildbeck uses fixed anchors (0.5/0.5). Resize by top+bottom target.
        Vector2 size = libraryRootRt.sizeDelta;
        size.y = newHeight;
        libraryRootRt.sizeDelta = size;

        Vector2 anchored = libraryRootRt.anchoredPosition;
        anchored.y = newCenterY;
        libraryRootRt.anchoredPosition = anchored;
    }

    private static RectTransform ResolveGoButtonRectTransform()
    {
        string[] goObjectNames = { "進入對戰", "ready", "Ready" };
        for (int n = 0; n < goObjectNames.Length; n++)
        {
            GameObject named = GameObject.Find(goObjectNames[n]);
            if (named != null)
            {
                RectTransform rt = named.GetComponent<RectTransform>();
                if (rt != null) return rt;
            }
        }

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            if (btn == null) continue;

            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null && (tmp.text.Contains("GO") || tmp.text.Contains("進入對戰") ||
                                tmp.text.Contains("準備好了") || tmp.text.Contains("準備完成")))
                return btn.GetComponent<RectTransform>();

            Text txt = btn.GetComponentInChildren<Text>(true);
            if (txt != null && (txt.text.Contains("GO") || txt.text.Contains("進入對戰")))
                return btn.GetComponent<RectTransform>();
        }

        return null;
    }

    private static CardDisplay FindCardDisplayOnRow(Transform child)
    {
        if (child == null) return null;
        CardDisplay d = child.GetComponent<CardDisplay>();
        return d != null ? d : child.GetComponentInChildren<CardDisplay>(true);
    }

    private GameObject FindCardTemplateInPanel(Transform panel)
    {
        if (panel == null) return null;
        int n = panel.childCount;
        for (int i = 0; i < n; i++)
        {
            Transform child = panel.GetChild(i);
            if (FindCardDisplayOnRow(child) != null)
                return child.gameObject;
        }
        return null;
    }

    private void RefreshDeckSlotTabVisual()
    {
        EnsureCoreRefs();
        if (PlayerData == null) return;
        for (int i = 0; i < deckSlotButtons.Count; i++)
        {
            Button btn = deckSlotButtons[i];
            if (btn == null) continue;
            SetSlotButtonLabel(btn, i + 1);
            bool active = i == PlayerData.selectedDeckSlot;
            Image bg = btn.GetComponent<Image>();
            if (bg != null)
            {
                // Selected: white background. Unselected: scene wine-red background.
                bg.color = active ? Color.white : new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);
            }

            // Buttons in this scene use TMP labels, but keep legacy Text compatibility.
            TMP_Text tmpLabel = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null)
            {
                tmpLabel.color = active ? Color.black : Color.white;
            }
            else
            {
                Text label = btn.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.color = active ? Color.black : Color.white;
                }
            }
        }
        UpdateDeckSlotGuideDotsVisual();
        RefreshCurrentDeckDisplayName();
    }

    /// <summary>Updates <see cref="currentDeckDisplayNameText"/> / legacy Text from <see cref="PlayerData"/>.</summary>
    public void RefreshCurrentDeckDisplayName()
    {
        EnsureCoreRefs();
        if (PlayerData == null) return;
        string name = PlayerData.GetDeckSlotDisplayName(PlayerData.selectedDeckSlot);
        if (currentDeckDisplayNameText != null)
            currentDeckDisplayNameText.text = name;
        if (currentDeckDisplayNameLegacyText != null)
            currentDeckDisplayNameLegacyText.text = name;
    }

    private void SwitchDeckSlotByDelta(int direction)
    {
        if (direction == 0) return;
        EnsureCoreRefs();
        if (PlayerData == null || PlayerData.deckSlotCount <= 0) return;

        int n = Mathf.Max(1, PlayerData.deckSlotCount);
        int next = ((PlayerData.selectedDeckSlot + direction) % n + n) % n;
        if (next == PlayerData.selectedDeckSlot) return;
        SelectDeckSlot(next);
    }

    private void EnsureDeckSlotGuideDots()
    {
        if (!IsBuildbeckSceneActive())
        {
            CleanupDeckSlotGuideDots();
            return;
        }

        int dotCount = Mathf.Max(5, PlayerData != null ? PlayerData.deckSlotCount : 5);
        if (deckSlotGuideDotsRoot != null && deckSlotGuideDotsBuiltCount == dotCount) return;

        Canvas canvas = ResolveDeckUiCanvas();
        if (canvas == null) return;

        Transform existing = FindDeepChildByName(canvas.transform, "DeckSlotGuideDots");
        if (existing != null)
        {
            deckSlotGuideDotsRoot = existing as RectTransform;
        }
        else
        {
            GameObject rootPf = Resources.Load<GameObject>(BuildbeckUiResourcePaths.GuideDotsRoot);
            if (rootPf != null)
            {
                GameObject rootObj = Instantiate(rootPf, canvas.transform, false);
                rootObj.name = "DeckSlotGuideDots";
                deckSlotGuideDotsRoot = rootObj.GetComponent<RectTransform>();
            }
            else
            {
                GameObject rootObj = BuildbeckUiHierarchyBuilder.CreateDeckSlotGuideDotsRoot(canvas.transform);
                deckSlotGuideDotsRoot = rootObj.GetComponent<RectTransform>();
            }
        }

        for (int i = deckSlotGuideDotsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = deckSlotGuideDotsRoot.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }

        // Top button -> previous slot.
        Button prevBtn = InstantiateDeckSlotGuideNavButton(deckSlotGuideDotsRoot, "DeckSlotPrevButton", "˄");
        if (prevBtn != null)
        {
            prevBtn.onClick.RemoveAllListeners();
            prevBtn.onClick.AddListener(() => SwitchDeckSlotByDelta(-1));
        }

        deckSlotGuideDots.Clear();
        GameObject dotPf = Resources.Load<GameObject>(BuildbeckUiResourcePaths.GuideDot);
        for (int i = 0; i < dotCount; i++)
        {
            GameObject dotObj;
            if (dotPf != null)
            {
                dotObj = Instantiate(dotPf, deckSlotGuideDotsRoot, false);
                dotObj.name = $"Dot_{i + 1}";
            }
            else
            {
                dotObj = BuildbeckUiHierarchyBuilder.CreateDeckSlotGuideDot(deckSlotGuideDotsRoot, i + 1);
            }

            Image dot = dotObj.GetComponent<Image>();
            if (dot != null)
                deckSlotGuideDots.Add(dot);
        }

        // Bottom button -> next slot.
        Button nextBtn = InstantiateDeckSlotGuideNavButton(deckSlotGuideDotsRoot, "DeckSlotNextButton", "˅");
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(() => SwitchDeckSlotByDelta(1));
        }

        deckSlotGuideDotsBuiltCount = dotCount;
        UpdateDeckSlotGuideDotsVisual();
    }

    private void UpdateDeckSlotGuideDotsVisual()
    {
        if (deckSlotGuideDots.Count <= 0) return;
        if (PlayerData == null) return;

        int activeIndex = Mathf.Clamp(PlayerData.selectedDeckSlot, 0, deckSlotGuideDots.Count - 1);
        for (int i = 0; i < deckSlotGuideDots.Count; i++)
        {
            Image dot = deckSlotGuideDots[i];
            if (dot == null) continue;
            bool active = i == activeIndex;
            dot.color = active ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.38f);
            RectTransform rt = dot.rectTransform;
            rt.sizeDelta = active ? new Vector2(22f, 22f) : new Vector2(18f, 18f);
        }
    }

    private Button InstantiateDeckSlotGuideNavButton(RectTransform parent, string objectName, string symbol)
    {
        if (parent == null) return null;
        GameObject navPf = Resources.Load<GameObject>(BuildbeckUiResourcePaths.GuideNavButton);
        if (navPf != null)
        {
            GameObject inst = Instantiate(navPf, parent, false);
            inst.name = objectName;
            TMP_Text tmp = inst.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = symbol;
                if (hintTMPFont != null) tmp.font = hintTMPFont;
            }

            return inst.GetComponent<Button>();
        }

        return BuildbeckUiHierarchyBuilder.CreateDeckSlotGuideNavButton(parent, objectName, symbol, hintTMPFont);
    }

    private void ResetBuildbeckSceneUiWiring()
    {
        externalSlotButtonsBound = false;
        deckSlotButtons.Clear();
        deckSlotButton1 = null;
        deckSlotButton2 = null;
        deckSlotButton3 = null;
        deckSlotButton4 = null;
        deckSlotButton5 = null;
        currentDeckDisplayNameText = null;
        currentDeckDisplayNameLegacyText = null;
        libraryPanel = null;
        deckPanel = null;
    }

    private void BindExternalSlotButtonsIfNeeded()
    {
        if (externalSlotButtonsBound)
        {
            RefreshDeckSlotTabVisual();
            RefreshCurrentDeckDisplayName();
            return;
        }

        bool hasAny = deckSlotButton1 != null || deckSlotButton2 != null || deckSlotButton3 != null ||
                      deckSlotButton4 != null || deckSlotButton5 != null;
        if (!hasAny) return;

        deckSlotButtons.Clear();
        if (deckSlotButton1 != null)
        {
            deckSlotButton1.onClick.RemoveAllListeners();
            deckSlotButton1.onClick.AddListener(() => SelectDeckSlot(0));
            deckSlotButtons.Add(deckSlotButton1);
            SetSlotButtonLabel(deckSlotButton1, 1);
        }
        if (deckSlotButton2 != null)
        {
            deckSlotButton2.onClick.RemoveAllListeners();
            deckSlotButton2.onClick.AddListener(() => SelectDeckSlot(1));
            deckSlotButtons.Add(deckSlotButton2);
            SetSlotButtonLabel(deckSlotButton2, 2);
        }
        if (deckSlotButton3 != null)
        {
            deckSlotButton3.onClick.RemoveAllListeners();
            deckSlotButton3.onClick.AddListener(() => SelectDeckSlot(2));
            deckSlotButtons.Add(deckSlotButton3);
            SetSlotButtonLabel(deckSlotButton3, 3);
        }
        if (deckSlotButton4 != null)
        {
            deckSlotButton4.onClick.RemoveAllListeners();
            deckSlotButton4.onClick.AddListener(() => SelectDeckSlot(3));
            deckSlotButtons.Add(deckSlotButton4);
            SetSlotButtonLabel(deckSlotButton4, 4);
        }
        if (deckSlotButton5 != null)
        {
            deckSlotButton5.onClick.RemoveAllListeners();
            deckSlotButton5.onClick.AddListener(() => SelectDeckSlot(4));
            deckSlotButtons.Add(deckSlotButton5);
            SetSlotButtonLabel(deckSlotButton5, 5);
        }

        // New layout controls slot button placement; skip legacy nested toggle layout.
        externalSlotButtonsBound = true;
        RefreshDeckSlotTabVisual();
    }

    private void EnsureDeckSlotButtonsVerticalNestedLayout()
    {
        if (deckSlotButtons.Count <= 1) return;
        RectTransform firstRt = deckSlotButtons[0] != null ? deckSlotButtons[0].transform as RectTransform : null;
        if (firstRt == null) return;
        RectTransform originalParent = firstRt.parent as RectTransform;
        if (originalParent == null) return;

        const string RootName = "DeckSlotSelectorGroup";
        const string ToggleName = "DeckSlotSelectorToggle";
        const string NestName = "DeckSlotButtonsNest";

        Transform existingRoot = originalParent.Find(RootName);
        GameObject rootObj = existingRoot != null
            ? existingRoot.gameObject
            : new GameObject(RootName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        if (existingRoot == null)
            rootObj.transform.SetParent(originalParent, false);

        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        VerticalLayoutGroup rootLayout = rootObj.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter rootFitter = rootObj.GetComponent<ContentSizeFitter>();
        if (rootRt == null || rootLayout == null || rootFitter == null) return;

        rootRt.anchorMin = firstRt.anchorMin;
        rootRt.anchorMax = firstRt.anchorMax;
        // Keep top edge fixed so toggle position won't jump after expand/collapse.
        rootRt.pivot = new Vector2(firstRt.pivot.x, 1f);
        rootRt.anchoredPosition = firstRt.anchoredPosition;
        rootRt.localScale = Vector3.one;
        rootRt.localRotation = Quaternion.identity;

        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;
        rootLayout.spacing = 8f;
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform toggleT = rootObj.transform.Find(ToggleName);
        Button toggleBtn;
        RectTransform toggleRt;
        TextMeshProUGUI toggleLabelTmp = null;
        if (toggleT != null)
        {
            toggleBtn = toggleT.GetComponent<Button>();
            toggleRt = toggleT as RectTransform;
            if (toggleT != null) toggleLabelTmp = toggleT.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        else
        {
            GameObject toggleObj = new GameObject(ToggleName, typeof(RectTransform), typeof(Image), typeof(Button));
            toggleObj.transform.SetParent(rootObj.transform, false);
            toggleBtn = toggleObj.GetComponent<Button>();
            toggleRt = toggleObj.GetComponent<RectTransform>();
            Image toggleBg = toggleObj.GetComponent<Image>();
            if (toggleBg != null) toggleBg.color = new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(toggleObj.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontSize = 32f;
            labelTmp.color = Color.white;
            labelTmp.text = "牌組選擇";
            toggleLabelTmp = labelTmp;
        }

        if (toggleBtn == null || toggleRt == null) return;
        TMP_Text sampleSlotLabel = deckSlotButtons[0] != null ? deckSlotButtons[0].GetComponentInChildren<TMP_Text>(true) : null;
        if (toggleLabelTmp != null)
        {
            if (sampleSlotLabel != null)
            {
                toggleLabelTmp.font = sampleSlotLabel.font;
                toggleLabelTmp.fontMaterial = sampleSlotLabel.fontMaterial;
                toggleLabelTmp.isRightToLeftText = sampleSlotLabel.isRightToLeftText;
                toggleLabelTmp.fontSize = Mathf.Max(18f, sampleSlotLabel.fontSize * 0.82f);
            }
            else if (toggleLabelTmp.fontSize > 24f)
            {
                toggleLabelTmp.fontSize = 24f;
            }
        }
        toggleRt.sizeDelta = new Vector2(
            firstRt.rect.width > 1f ? firstRt.rect.width : 180f,
            firstRt.rect.height > 1f ? firstRt.rect.height : 64f);
        toggleBtn.onClick.RemoveAllListeners();

        Transform existingNest = rootObj.transform.Find(NestName);
        GameObject nestObj = existingNest != null
            ? existingNest.gameObject
            : new GameObject(NestName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        if (existingNest == null)
            nestObj.transform.SetParent(rootObj.transform, false);

        RectTransform nestRt = nestObj.GetComponent<RectTransform>();
        VerticalLayoutGroup vlg = nestObj.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter fitter = nestObj.GetComponent<ContentSizeFitter>();
        if (nestRt == null || vlg == null || fitter == null) return;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 10f;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < deckSlotButtons.Count; i++)
        {
            Button btn = deckSlotButtons[i];
            if (btn == null) continue;
            RectTransform btnRt = btn.transform as RectTransform;
            if (btnRt == null) continue;
            btnRt.SetParent(nestRt, false);

            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
            if (btnRt.rect.width > 1f) le.preferredWidth = btnRt.rect.width;
            if (btnRt.rect.height > 1f) le.preferredHeight = btnRt.rect.height;
        }

        if (toggleLabelTmp != null)
            toggleLabelTmp.text = deckSlotSelectorExpanded ? "牌組選擇▼" : "牌組選擇►";
        nestObj.SetActive(deckSlotSelectorExpanded);
        toggleBtn.onClick.AddListener(() =>
        {
            deckSlotSelectorExpanded = !deckSlotSelectorExpanded;
            if (nestObj != null) nestObj.SetActive(deckSlotSelectorExpanded);
            if (toggleLabelTmp != null)
                toggleLabelTmp.text = deckSlotSelectorExpanded ? "牌組選擇▼" : "牌組選擇►";
        });
    }

    private void SetSlotButtonLabel(Button button, int indexOneBased)
    {
        if (button == null) return;
        int slot0 = indexOneBased - 1;
        string labelText = PlayerData != null && slot0 >= 0
            ? PlayerData.GetDeckSlotDisplayName(slot0)
            : ("牌組" + indexOneBased.ToString());
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = labelText;
            return;
        }

        Text txt = button.GetComponentInChildren<Text>(true);
        if (txt != null) txt.text = labelText;
    }

    public int GetLibraryCardCount(int id)
    {
        if (PlayerData == null) return 0;
        return PlayerData.GetCollectionCount(id);
    }

    private int GetDeckTotalCount()
    {
        if (PlayerData == null) return 0;
        return PlayerData.GetSelectedDeckTotalCount();
    }

    private void AttachWheelScroll(Transform panel)
    {
        if (panel == null) return;
        RectTransform viewport = null;
        RectTransform panelRect = panel as RectTransform;
        bool isLibraryPanel = ReferenceEquals(panel, libraryPanel);
        bool isDeckPanel = ReferenceEquals(panel, deckPanel);
        // Only the Persistent scene's BagUIHorizontalRow uses the horizontal swipe layout.
        // Buildbeck's left-side library (any other name) keeps the original vertical scroll behavior.
        bool isHorizontalLibrary = isLibraryPanel && IsHorizontalBagPanelName(panel);
        ScrollRect sr = null;

        if (isHorizontalLibrary && panelRect != null)
        {
            viewport = EnsureLibraryHorizontalViewport(panelRect);
            sr = viewport != null ? viewport.GetComponent<ScrollRect>() : null;
        }

        if (sr == null)
        {
            sr = panel.GetComponent<ScrollRect>();
            if (!isHorizontalLibrary && sr == null) sr = panel.GetComponentInParent<ScrollRect>();
        }

        if (panelRect != null)
        {
            if (isHorizontalLibrary)
            {
                // BagUIHorizontalRow is a horizontal row, so keep the content left-anchored for left-right swipe.
                panelRect.anchorMin = new Vector2(0f, 0.5f);
                panelRect.anchorMax = new Vector2(0f, 0.5f);
                panelRect.pivot = new Vector2(0f, 0.5f);
                GridLayoutGroup grid = panelRect.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                    grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    grid.constraintCount = 1;
                }
            }
            else
            {
                // Normalize to top-anchored content model to keep vertical scroll range correct.
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(1f, 1f);
                panelRect.pivot = new Vector2(0.5f, 1f);
            }
        }
        if (!isHorizontalLibrary && sr == null)
        {
            RectTransform fallbackViewport = panel.parent as RectTransform;
            if (fallbackViewport != null && panelRect != null)
            {
                // Create a standard ScrollRect runtime when scene has only content panel.
                sr = fallbackViewport.GetComponent<ScrollRect>();
                if (sr == null) sr = fallbackViewport.gameObject.AddComponent<ScrollRect>();
                sr.content = panelRect;
                sr.viewport = fallbackViewport;
            }
        }

        if (sr != null)
        {
            // Auto-wire ScrollRect bindings if scene setup is incomplete.
            if (isHorizontalLibrary && panelRect != null)
            {
                sr.content = panelRect;
                if (viewport != null) sr.viewport = viewport;
            }
            else if (sr.content == null && panelRect != null) sr.content = panelRect;
            if (sr.viewport == null)
            {
                RectTransform candidate = panel.parent as RectTransform;
                if (candidate != null) sr.viewport = candidate;
            }
            sr.horizontal = isHorizontalLibrary;
            sr.vertical = !isHorizontalLibrary;
            if (isLibraryPanel)
            {
                _libraryPanelScrollRect = sr;
                if (isHorizontalLibrary)
                {
                    sr.verticalScrollbar = null;
                    sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                }
            }
            else if (isDeckPanel)
            {
                _deckPanelScrollRect = sr;
                EnsureDeckScrollUserGestureRelay(sr);
            }
            ApplyDeckPanelScrollFeel(sr, panel);
            viewport = sr.viewport != null ? sr.viewport : panelRect;
            if (!isHorizontalLibrary)
                EnsureVerticalScrollbar(sr);
            if (isDeckPanel)
            {
                _deckListParentAnchoredYBaselineCaptured = false;
                ApplyDeckParentLayoutOffsets();
            }
        }
        else viewport = panel.parent as RectTransform;

        // Ensure rectangular clipping mask so cards outside viewport are hidden.
        if (viewport != null)
        {
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage == null)
            {
                viewportImage = viewport.gameObject.AddComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.001f); // invisible but raycastable region
            }
        }

        if (isLibraryPanel && !isHorizontalLibrary && IsBuildbeckSceneActive() && viewport != null)
            ApplyBuildbeckLibraryGridViewportTo(viewport);
    }

    /// <summary>
    /// True only when the library panel is the Persistent scene's <c>BagUIHorizontalRow</c> (horizontal swipe row).
    /// Any other library panel name (e.g. Buildbeck's <c>Library Grid</c>) keeps the legacy vertical scroll layout.
    /// </summary>
    private static bool IsHorizontalBagPanelName(Transform panel)
    {
        if (panel == null) return false;
        string n = panel.name;
        if (string.IsNullOrEmpty(n)) return false;
        return n.Equals("BagUIHorizontalRow", System.StringComparison.OrdinalIgnoreCase);
    }

    private RectTransform EnsureLibraryHorizontalViewport(RectTransform content)
    {
        if (content == null) return null;
        const string ViewportName = "RuntimeLibraryHorizontalViewport";
        RectTransform currentParent = content.parent as RectTransform;
        if (currentParent == null) return null;

        if (currentParent.name == ViewportName)
        {
            EnsureLibraryHorizontalScrollRect(currentParent, content);
            return currentParent;
        }

        Transform originalParent = content.parent;
        int originalSibling = content.GetSiblingIndex();
        GameObject viewportObj = new GameObject(ViewportName, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObj.transform.SetParent(originalParent, false);
        viewportObj.transform.SetSiblingIndex(originalSibling);

        RectTransform viewport = viewportObj.GetComponent<RectTransform>();
        viewport.anchorMin = content.anchorMin;
        viewport.anchorMax = content.anchorMax;
        viewport.pivot = content.pivot;
        viewport.anchoredPosition = content.anchoredPosition;
        viewport.sizeDelta = content.sizeDelta;
        viewport.localScale = content.localScale;
        viewport.localRotation = content.localRotation;

        Image viewportImage = viewportObj.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;

        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 0.5f);
        content.anchorMax = new Vector2(0f, 0.5f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;
        content.localRotation = Quaternion.identity;

        EnsureLibraryHorizontalScrollRect(viewport, content);
        return viewport;
    }

    private static void EnsureLibraryHorizontalScrollRect(RectTransform viewport, RectTransform content)
    {
        if (viewport == null || content == null) return;
        ScrollRect sr = viewport.GetComponent<ScrollRect>();
        if (sr == null) sr = viewport.gameObject.AddComponent<ScrollRect>();
        sr.content = content;
        sr.viewport = viewport;
        sr.horizontal = true;
        sr.vertical = false;
        sr.horizontalScrollbar = null;
        sr.verticalScrollbar = null;
        sr.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private void ApplyDeckPanelScrollFeel(ScrollRect sr, Transform panel)
    {
        if (sr == null) return;
        bool isDeck = ReferenceEquals(panel, deckPanel);
        bool isLibrary = ReferenceEquals(panel, libraryPanel);
        // Buildbeck 左側直向館藏：移除 Elastic 橡皮筋，僅保留慣性捲動；觸底回饋見 HandleScrollEdgeFeel。
        bool buildbeckVerticalLibrary =
            isLibrary && !IsHorizontalBagPanelName(panel) && IsBuildbeckSceneActive();

        if (isDeck)
        {
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.decelerationRate = DeckListDecelerationRate;
            sr.scrollSensitivity = DeckListWheelSensitivity;
        }
        else if (buildbeckVerticalLibrary)
        {
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.decelerationRate = DeckScrollDecelerationRate;
            sr.scrollSensitivity = DeckScrollWheelSensitivity;
        }
        else
        {
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = DeckScrollElasticity;
            sr.decelerationRate = DeckScrollDecelerationRate;
            sr.scrollSensitivity = DeckScrollWheelSensitivity;
        }
        sr.inertia = true;

        if (isLibrary)
        {
            sr.onValueChanged.RemoveListener(OnLibraryPanelScrollFeel);
            sr.onValueChanged.AddListener(OnLibraryPanelScrollFeel);
        }
        else if (isDeck)
        {
            sr.onValueChanged.RemoveListener(OnDeckPanelScrollFeel);
            sr.onValueChanged.AddListener(OnDeckPanelScrollFeel);
        }
    }

    private void OnLibraryPanelScrollFeel(Vector2 normalized) =>
        HandleScrollEdgeFeel(true, _libraryPanelScrollRect, ref _libraryScrollLastNormY, ref _libraryScrollEdgePulseCo, ref _libraryScrollPulseGeneration, normalized);

    private void OnDeckPanelScrollFeel(Vector2 normalized)
    {
        HandleScrollEdgeFeel(false, _deckPanelScrollRect, ref _deckScrollLastNormY, ref _deckScrollEdgePulseCo, ref _deckScrollPulseGeneration, normalized);
        RectTransform deckContent = deckPanel as RectTransform;
        if (deckContent != null)
            RequestDeckArcLayoutUnlessDeferredToLateUpdate(deckContent, false);
    }

    private void HandleScrollEdgeFeel(
        bool isLibrary,
        ScrollRect sr,
        ref float lastNormY,
        ref Coroutine pulseCo,
        ref int pulseGeneration,
        Vector2 normalized)
    {
        if (sr == null) return;
        bool useHorizontalAxis = sr.horizontal && !sr.vertical;
        float position = useHorizontalAxis ? normalized.x : normalized.y;
        // Buildbeck 直向館藏：僅保留「捲到底」脈衝，不觸發捲到頂的回饋。
        bool buildbeckVerticalLibrary =
            isLibrary && ReferenceEquals(sr, _libraryPanelScrollRect) && IsBuildbeckSceneActive() &&
            sr.vertical && !sr.horizontal;

        if (lastNormY >= 0f)
        {
            const float edgeEps = 0.004f;
            const float inner = 0.035f;
            bool hitMax = lastNormY < 1f - inner && position >= 1f - edgeEps;
            bool hitMin = lastNormY > inner && position <= edgeEps;
            bool edgeHit = hitMax || hitMin;
            if (buildbeckVerticalLibrary)
                edgeHit = hitMin;

            if (edgeHit)
            {
                if (Time.unscaledTime >= _scrollEdgeFeelCooldownUntilUnscaled)
                {
                    _scrollEdgeFeelCooldownUntilUnscaled = Time.unscaledTime + DeckScrollEdgeFeelCooldown;
                    RectTransform pulseTarget = ResolveScrollEdgePulseTarget(sr);
                    if (pulseTarget != null)
                    {
                        if (pulseCo != null) StopCoroutine(pulseCo);
                        pulseGeneration++;
                        int gen = pulseGeneration;
                        pulseCo = StartCoroutine(CoViewportScrollEdgePulse(pulseTarget, gen, isLibrary));
                    }
                }
            }
        }
        lastNormY = position;
    }

    /// <summary>
    /// 捲動邊緣「手感」脈衝只作用在捲動內容（<see cref="ScrollRect.content"/>），必要時才用 viewport 的第一個子 Rect；不縮放 viewport 父物件。
    /// </summary>
    private static RectTransform ResolveScrollEdgePulseTarget(ScrollRect sr)
    {
        if (sr == null) return null;
        if (sr.content != null) return sr.content;
        RectTransform vp = sr.viewport;
        if (vp == null || vp.childCount <= 0) return null;
        return vp.GetChild(0) as RectTransform;
    }

    private IEnumerator CoViewportScrollEdgePulse(RectTransform pulseTarget, int generation, bool isLibrary)
    {
        try
        {
            if (pulseTarget == null) yield break;
            Vector3 s0 = pulseTarget.localScale;
            Vector3 peak = s0 * DeckScrollEdgePulseScale;
            float t = 0f;
            const float dur = 0.085f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float eased = Mathf.Sin(u * Mathf.PI * 0.5f);
                pulseTarget.localScale = Vector3.Lerp(s0, peak, eased);
                yield return null;
            }
            t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float eased = Mathf.Sin(u * Mathf.PI * 0.5f);
                pulseTarget.localScale = Vector3.Lerp(peak, s0, eased);
                yield return null;
            }
            pulseTarget.localScale = s0;
        }
        finally
        {
            if (isLibrary)
            {
                if (generation == _libraryScrollPulseGeneration)
                    _libraryScrollEdgePulseCo = null;
            }
            else if (generation == _deckScrollPulseGeneration)
            {
                _deckScrollEdgePulseCo = null;
            }
        }
    }

    private void EnsureVerticalScrollbar(ScrollRect sr)
    {
        if (sr == null || sr.viewport == null) return;
        if (sr.verticalScrollbar != null)
        {
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            ApplyInvisibleScrollbarStyle(sr.verticalScrollbar);
            return;
        }

        RectTransform viewport = sr.viewport;
        GameObject barObj = new GameObject("RuntimeVerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        // Keep scrollbar inside viewport bounds, pinned to right edge.
        barObj.transform.SetParent(viewport, false);

        RectTransform barRect = barObj.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(1f, 0f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(1f, 0.5f);
        barRect.sizeDelta = new Vector2(18f, 0f);
        barRect.anchoredPosition = new Vector2(-1f, 0f);
        barObj.transform.SetAsLastSibling();

        Image barBg = barObj.GetComponent<Image>();
        barBg.color = new Color(0f, 0f, 0f, 0f);
        barBg.raycastTarget = true;

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(barObj.transform, false);
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.75f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.offsetMin = new Vector2(2f, 2f);
        handleRect.offsetMax = new Vector2(-2f, -2f);

        Image handleImage = handleObj.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0f);
        handleImage.raycastTarget = true;

        Scrollbar sb = barObj.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.targetGraphic = handleImage;
        sb.handleRect = handleRect;
        sb.value = 1f;
        sb.size = 0.2f;

        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        sr.verticalScrollbarSpacing = 0f;
        ApplyInvisibleScrollbarStyle(sb);
    }

    private static void ApplyInvisibleScrollbarStyle(Scrollbar scrollbar)
    {
        if (scrollbar == null) return;
        Image bg = scrollbar.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;
        }

        if (scrollbar.handleRect != null)
        {
            Image handle = scrollbar.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = new Color(1f, 1f, 1f, 0f);
                handle.raycastTarget = true;
            }
        }
    }

    private void RefreshScrollablePanels()
    {
        ListViewController.RefreshScrollablePanels();
    }

    private void ForceRebuildPanelsLayout(bool includeDeck = true)
    {
        ListViewController.ForceRebuildPanelsLayout(includeDeck);
    }

    private void NormalizeChildrenVisualState(RectTransform panelRt)
    {
        int n = panelRt.childCount;
        for (int i = 0; i < n; i++)
        {
            RectTransform c = panelRt.GetChild(i) as RectTransform;
            if (c == null) continue;
            if ((c.localScale - Vector3.one).sqrMagnitude > 1e-8f)
                c.localScale = Vector3.one;
            if (Quaternion.Angle(c.localRotation, Quaternion.identity) > 0.05f)
                c.localRotation = Quaternion.identity;
            CanvasGroup cg = c.GetComponent<CanvasGroup>();
            if (cg != null && !Mathf.Approximately(cg.alpha, 1f))
                cg.alpha = 1f;
        }
    }

    private void ForcePanelsScrollToTop()
    {
        ListViewController.ForcePanelsScrollToTop();
    }

    private void ForcePanelScrollToTop(Transform panel)
    {
        if (panel == null) return;
        ScrollRect sr = panel.GetComponent<ScrollRect>();
        if (sr == null) sr = panel.GetComponentInParent<ScrollRect>();
        if (sr == null) return;
        Canvas.ForceUpdateCanvases();
        sr.StopMovement();
        bool useHorizontalAxis = sr.horizontal && !sr.vertical;
        if (useHorizontalAxis)
            sr.horizontalNormalizedPosition = 0f;
        else
            sr.verticalNormalizedPosition = 1f;
        if (sr.content != null)
        {
            Vector2 p = sr.content.anchoredPosition;
            if (useHorizontalAxis) p.x = 0f;
            else p.y = 0f;
            sr.content.anchoredPosition = p;
        }
    }

    /// <summary>
    /// 是否為牌組清單最底一列（依 sibling 順序）。必須在從 <see cref="deckDic"/> 移除該卡之前呼叫。
    /// </summary>
    private bool ComputeDeckCardIsBottomListRow(GameObject cardObj)
    {
        if (deckPanel == null || cardObj == null) return false;
        Transform row = cardObj.transform;
        if (row.parent != deckPanel) return false;
        int my = row.GetSiblingIndex();
        int highest = my;
        foreach (var kv in deckDic)
        {
            if (kv.Value == null) continue;
            Transform t = kv.Value.transform;
            if (t.parent != deckPanel) continue;
            highest = Mathf.Max(highest, t.GetSiblingIndex());
        }

        return my == highest;
    }

    private void CompleteDeckCardRemovePostDestroy()
    {
        if (_deckPanelScrollRect != null)
        {
            _deckPanelScrollRect.StopMovement();
            _deckPanelScrollRect.velocity = Vector2.zero;
        }

        _deckCardRemoveAnimationActive = false;
        // #region agent log
        AgentDbgNdjson("H4", "DeckManager.CompleteDeckCardRemovePostDestroy", "anim_flag_cleared",
            "{\"arcApplyBlocked\":" + _agentDbgArcBlockCount + ",\"lateArcSkipped\":" + _agentDbgLateArcSkipCount + "}");
        _agentDbgArcBlockCount = 0;
        _agentDbgLateArcSkipCount = 0;
        // #endregion
        RefreshScrollablePanels();
        RectTransform deckRt = deckPanel as RectTransform;
        if (deckRt != null)
        {
            NormalizeChildrenVisualState(deckRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(deckRt);
            EnsureDeckArcPresenter();
            if (_deckArcPresenter != null) _deckArcPresenter.InvalidateLayoutState();
            RequestDeckArcLayout(deckRt, false);
        }

        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// 最底一列移除：淡出與復歸捲動部分重疊、捲動時長依位移自適應，最後銷毀實體。
    /// </summary>
    private IEnumerator AnimateDeckCardRemoveBottomListRow(GameObject cardObj)
    {
        if (cardObj == null) yield break;
        // #region agent log
        _agentDbgArcBlockCount = 0;
        _agentDbgLateArcSkipCount = 0;
        AgentDbgNdjson("H3", "DeckManager.AnimateDeckCardRemoveBottomListRow", "bottom_remove_begin", "{}");
        // #endregion
        _deckCardRemoveAnimationActive = true;
        if (_deckPanelScrollRect != null)
        {
            _deckPanelScrollRect.StopMovement();
            _deckPanelScrollRect.velocity = Vector2.zero;
        }

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        RectTransform contentRt = deckPanel as RectTransform;
        GridLayoutGroup grid = contentRt != null ? contentRt.GetComponent<GridLayoutGroup>() : null;
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        float stepY = grid != null
            ? grid.cellSize.y + grid.spacing.y
            : (rt != null ? rt.rect.height + 20f : NewLayoutDeckCellHeight + deckListCellSpacingY);

        float startAlpha = cg.alpha <= 0f ? 1f : cg.alpha;

        float scrollStartY = 0f;
        float scrollEndY = 0f;
        float maxY = 0f;
        bool doScroll = false;
        if (contentRt != null && contentRt.gameObject.activeInHierarchy)
        {
            DeckListGetRecoveryScrollTargets(contentRt, stepY, out maxY, out scrollEndY);
            scrollStartY = contentRt.anchoredPosition.y;
            doScroll = Mathf.Abs(scrollEndY - scrollStartY) >= 1.25f;
        }

        ScrollRect sr = _deckPanelScrollRect;
        float fadeDur = DeckListRemoveCardFadeSeconds;
        float scrollPhaseStart = fadeDur * DeckListRemoveBottomRecoveryOverlap;
        float scrollDur = doScroll
            ? DeckListRecoveryScrollComputeDuration(Mathf.Abs(scrollEndY - scrollStartY), stepY)
            : 0f;
        float totalT = Mathf.Max(fadeDur, scrollPhaseStart + scrollDur);

        float t = 0f;
        while (t < totalT && cardObj != null && contentRt != null)
        {
            t += Time.unscaledDeltaTime;

            float fadeU = Mathf.Clamp01(t / fadeDur);
            float fadeE = fadeU * fadeU * (3f - 2f * fadeU);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, fadeE);

            if (doScroll && t >= scrollPhaseStart && scrollDur > 1e-4f)
            {
                float su = Mathf.Clamp01((t - scrollPhaseStart) / scrollDur);
                float se = 1f - Mathf.Pow(1f - su, 4f);
                float y = Mathf.Lerp(scrollStartY, scrollEndY, se);
                DeckListApplyScrollY(contentRt, y, sr, maxY);
            }

            yield return null;
        }

        if (cardObj != null)
            cg.alpha = 0f;
        if (doScroll && contentRt != null)
            DeckListApplyScrollY(contentRt, scrollEndY, sr, maxY);

        if (cardObj != null)
            Destroy(cardObj);
        CompleteDeckCardRemovePostDestroy();
    }

    private static void DeckListApplyScrollY(RectTransform contentRt, float y, ScrollRect sr, float maxY)
    {
        Vector2 p = contentRt.anchoredPosition;
        p.y = y;
        contentRt.anchoredPosition = p;
        if (sr != null && maxY > 1e-4f)
            sr.verticalNormalizedPosition = 1f - Mathf.Clamp01(y / maxY);
    }

    private static float DeckListRecoveryScrollComputeDuration(float absDy, float stepY)
    {
        float span = Mathf.Max(stepY, 48f);
        return Mathf.Clamp(
            DeckListRecoveryScrollDurationMin + (absDy / span) * 0.2f,
            DeckListRecoveryScrollDurationMin,
            DeckListRecoveryScrollDurationMax);
    }

    /// <summary>
    /// 依目前內容高度計算復歸捲動目標（優先往上約一列；已在頂端則改往下約一列）。
    /// </summary>
    private static void DeckListGetRecoveryScrollTargets(
        RectTransform contentRt,
        float stepY,
        out float maxY,
        out float endY)
    {
        RectTransform vp = contentRt.parent as RectTransform;
        float viewH = vp != null ? Mathf.Max(0f, vp.rect.height) : 0f;
        maxY = Mathf.Max(0f, contentRt.sizeDelta.y - viewH);
        float startY = contentRt.anchoredPosition.y;
        endY = Mathf.Clamp(startY - stepY, 0f, maxY);
        if (Mathf.Abs(endY - startY) < 1f)
            endY = Mathf.Clamp(startY + stepY, 0f, maxY);
    }

    private IEnumerator AnimateDeckCardRemove(GameObject cardObj, bool bottomListRow)
    {
        if (cardObj == null) yield break;
        if (bottomListRow)
        {
            yield return AnimateDeckCardRemoveBottomListRow(cardObj);
            yield break;
        }

        // 非最底一列：左移淡出 → 刪除並版面重建（無復歸捲動；最底列仍保留復歸）。
        _deckCardRemoveAnimationActive = true;
        if (_deckPanelScrollRect != null)
        {
            _deckPanelScrollRect.StopMovement();
            _deckPanelScrollRect.velocity = Vector2.zero;
        }

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        RectTransform parent = rt != null ? rt.parent as RectTransform : null;
        GridLayoutGroup grid = parent != null ? parent.GetComponent<GridLayoutGroup>() : null;

        float startAlpha = cg.alpha <= 0f ? 1f : cg.alpha;
        Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 endPos = startPos + new Vector2(-160f, 0f);

        float t = 0f;
        while (t < DeckListRemoveCardFadeSeconds && cardObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / DeckListRemoveCardFadeSeconds);
            float eased = p * p * (3f - 2f * p);
            if (rt != null)
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        if (cardObj != null)
            Destroy(cardObj);
        if (parent != null && grid != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

        CompleteDeckCardRemovePostDestroy();
    }

    /// <summary>
    /// 內容區頂緣到「最上層」卡牌頂緣的距離（與捲到頂時視覺留白一致），用於尾端多留同等空間。
    /// </summary>
    private static float MeasureContentTopToTopmostCardTopGap(RectTransform content)
    {
        if (content == null || content.childCount == 0) return 0f;
        float contentTopY = content.rect.yMax;
        float topmostCardTop = float.NegativeInfinity;
        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform ch = content.GetChild(i) as RectTransform;
            if (ch == null || !ch.gameObject.activeInHierarchy) continue;
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(content, ch);
            if (b.max.y > topmostCardTop) topmostCardTop = b.max.y;
        }
        if (topmostCardTop <= float.NegativeInfinity + 1f) return 0f;
        return Mathf.Max(0f, contentTopY - topmostCardTop);
    }

    /// <summary>Persistent 橫向背包列：依子卡寬度與 spacing 重算 content 寬並夾住 X。</summary>
    private static void RefreshLibraryHorizontalBagGridContent(
        RectTransform content, GridLayoutGroup grid, float viewHeight, float viewWidth)
    {
        int libChildCount = content.childCount;
        float contentWidth = grid.padding.left + grid.padding.right;
        if (libChildCount > 0)
        {
            contentWidth += libChildCount * grid.cellSize.x;
            contentWidth += Mathf.Max(0, libChildCount - 1) * grid.spacing.x;
        }

        Vector2 libSize = content.sizeDelta;
        libSize.x = libChildCount <= 0 ? viewWidth : Mathf.Max(viewWidth, contentWidth);
        libSize.y = Mathf.Max(viewHeight, grid.padding.top + grid.padding.bottom + grid.cellSize.y);
        content.sizeDelta = libSize;

        Vector2 libPos = content.anchoredPosition;
        float minX = -Mathf.Max(0f, libSize.x - viewWidth);
        libPos.x = Mathf.Clamp(libPos.x, minX, 0f);
        libPos.y = 0f;
        content.anchoredPosition = libPos;
    }

    /// <summary>直向 Grid（Buildbeck 館藏多欄 + 牌組單欄）共用：重算 content 高度與捲動夾位。</summary>
    private static void RefreshVerticalGridLayoutContentHeight(
        RectTransform content, GridLayoutGroup grid, float viewHeight, bool singleColumnDeckLayout)
    {
        float width = Mathf.Max(1f, content.rect.width);
        float usableWidth = Mathf.Max(1f, width - grid.padding.left - grid.padding.right);
        float unitW = grid.cellSize.x + grid.spacing.x;
        int childCount = content.childCount;
        int columns = singleColumnDeckLayout
            ? 1
            : Mathf.Max(1, Mathf.FloorToInt((usableWidth + grid.spacing.x) / Mathf.Max(1f, unitW)));
        int rows = childCount <= 0
            ? 0
            : (singleColumnDeckLayout ? childCount : Mathf.Max(1, Mathf.CeilToInt(childCount / (float)columns)));
        float contentHeight =
            grid.padding.top + grid.padding.bottom +
            rows * grid.cellSize.y +
            Mathf.Max(0, rows - 1) * grid.spacing.y;
        // 不要用視窗高度當下限：否則卡牌少時仍可滑到底，底下會空一大塊。
        float topComfortGap = MeasureContentTopToTopmostCardTopGap(content);
        if (topComfortGap < 1f && childCount > 0)
            topComfortGap = Mathf.Max(topComfortGap, grid.padding.top);
        float targetH = childCount <= 0
            ? viewHeight
            : Mathf.Max(1f, contentHeight + topComfortGap);

        Vector2 size = content.sizeDelta;
        size.y = targetH;
        content.sizeDelta = size;
        Vector2 pos = content.anchoredPosition;
        float maxY = Mathf.Max(0f, size.y - viewHeight);
        pos.y = Mathf.Clamp(pos.y, 0f, maxY);
        content.anchoredPosition = pos;
    }

    private static void RefreshPanelContentHeightWithoutGrid(RectTransform content, float viewHeight)
    {
        float maxY = 0f;
        int n = content.childCount;
        for (int i = 0; i < n; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null) continue;
            float y = Mathf.Abs(child.anchoredPosition.y) + child.rect.height;
            if (y > maxY) maxY = y;
        }

        Vector2 size = content.sizeDelta;
        const float bottomPad = 20f;
        float topComfortGap = n > 0 ? MeasureContentTopToTopmostCardTopGap(content) : 0f;
        size.y = n <= 0 ? viewHeight : Mathf.Max(1f, maxY + bottomPad + topComfortGap);
        content.sizeDelta = size;
        Vector2 pos = content.anchoredPosition;
        float clampMaxY = Mathf.Max(0f, size.y - viewHeight);
        pos.y = Mathf.Clamp(pos.y, 0f, clampMaxY);
        content.anchoredPosition = pos;
    }

    private void RefreshPanelContentHeight(Transform panel)
    {
        if (panel == null) return;
        RectTransform content = panel as RectTransform;
        if (content == null) return;

        RectTransform viewport = content.parent as RectTransform;
        bool isLibrary = ReferenceEquals(panel, libraryPanel);
        bool isDeck = ReferenceEquals(panel, deckPanel);
        bool isHorizontalLibrary = isLibrary && IsHorizontalBagPanelName(panel);
        float viewHeight = viewport != null ? viewport.rect.height : 0f;
        float viewWidth = viewport != null ? viewport.rect.width : 0f;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            if (isHorizontalLibrary)
                RefreshLibraryHorizontalBagGridContent(content, grid, viewHeight, viewWidth);
            else
                RefreshVerticalGridLayoutContentHeight(content, grid, viewHeight, isDeck);
        }
        else
            RefreshPanelContentHeightWithoutGrid(content, viewHeight);

        if (isDeck)
            RequestDeckArcLayoutUnlessDeferredToLateUpdate(content, false);
    }

    /// <summary>與 <see cref="EnsureDeckPanelSingleColumnLayout"/> 對稱：館藏／牌組捲動區高度一次更新（含 Buildbeck Viewport）。</summary>
    private void RefreshScrollablePanelHeightsLibraryThenDeck()
    {
        RefreshPanelContentHeight(libraryPanel);
        if (IsBuildbeckSceneActive())
            ApplyBuildbeckLibraryGridViewportLayoutIfNeeded();
        RefreshPanelContentHeight(deckPanel);
    }

    private void ForceRebuildLibraryPanelLayout()
    {
        RectTransform libRt = libraryPanel as RectTransform;
        if (libRt == null) return;
        NormalizeChildrenVisualState(libRt);
        LayoutRebuilder.ForceRebuildLayoutImmediate(libRt);
        if (IsBuildbeckSceneActive())
            ApplyBuildbeckLibraryGridViewportLayoutIfNeeded();
    }

    private void ForceRebuildDeckPanelLayoutAfterNormalize()
    {
        RectTransform deckRt = deckPanel as RectTransform;
        if (deckRt == null) return;
        NormalizeChildrenVisualState(deckRt);
        LayoutRebuilder.ForceRebuildLayoutImmediate(deckRt);
        RequestDeckArcLayoutUnlessDeferredToLateUpdate(deckRt, false);
    }

    private void RequestDeckArcLayout(RectTransform content, bool oncePerFrame)
    {
        if (content == null) return;
        if (_deckCardRemoveAnimationActive && deckPanel != null && ReferenceEquals(content, deckPanel))
        {
            // #region agent log
            _agentDbgArcBlockCount++;
            // #endregion
            return;
        }
        EnsureDeckArcPresenter();
        if (_deckArcPresenter == null) return;

        int visibleSlotCount = NormalizeDeckArcVisibleSlotCount(deckArcVisibleSlotCount);
        EnsureDeckArcSlotOffsetListSize(visibleSlotCount);

        ComputeDeckArcHorizontalSmoothParams(out float arcSmoothT, out float arcMaxSpeed);

        float chordL = Mathf.Max(1f, deckArcChordHalfWidth);
        float radiusR = Mathf.Max(deckArcCircleRadius, chordL + 0.5f);

        _deckArcPresenter.ApplyLayout(
            content,
            NewLayoutEnableDeckArc,
            visibleSlotCount,
            radiusR,
            chordL,
            deckArcShapeStrength,
            deckArcVisibleRangePaddingSlots,
            NewLayoutDeckCellHeight,
            deckListCellSpacingY,
            deckArcSlotXOffsetById,
            arcSmoothT,
            arcMaxSpeed,
            oncePerFrame
        );
    }

    /// <summary>
    /// Velocity-based arc tightening applies while the user is dragging the deck list or coasting after release (see gesture relay + TickDeckScrollUserSessionEnd).
    /// </summary>
    private void ComputeDeckArcHorizontalSmoothParams(out float smoothTime, out float maxSpeed)
    {
        if (deckArcHorizontalSmoothTime <= 0f)
        {
            smoothTime = 0f;
            maxSpeed = Mathf.Infinity;
            return;
        }

        float vel = 0f;
        if (_deckPanelScrollRect != null)
            vel = Mathf.Abs(_deckPanelScrollRect.velocity.y);

        float denom = Mathf.Max(1f, deckArcScrollVelocityRef);
        float kVel = Mathf.Clamp01(vel / denom);

        float k = 0f;
        if (_deckScrollUserSessionActive)
            k = _deckScrollPointerHeld ? Mathf.Max(kVel, DeckArcUserScrollMinKDuringDrag) : kVel;

        smoothTime = Mathf.Lerp(deckArcHorizontalSmoothTime, deckArcHorizontalSmoothTime * 0.18f, k);
        maxSpeed = Mathf.Lerp(900f, 11000f, k);
    }

    private void EnsureDeckScrollUserGestureRelay(ScrollRect sr)
    {
        if (sr == null) return;
        DeckScrollUserGestureRelay relay = sr.GetComponent<DeckScrollUserGestureRelay>();
        if (relay == null)
            relay = sr.gameObject.AddComponent<DeckScrollUserGestureRelay>();
        relay.Setup(this);
    }

    private void NotifyDeckScrollPointerHeld(bool held)
    {
        _deckScrollPointerHeld = held;
        if (held)
            _deckScrollUserSessionActive = true;
    }

    private void TickDeckScrollUserSessionEnd()
    {
        if (!_deckScrollUserSessionActive)
            return;
        if (_deckScrollPointerHeld)
            return;
        if (_deckPanelScrollRect == null)
        {
            _deckScrollUserSessionActive = false;
            return;
        }

        if (Mathf.Abs(_deckPanelScrollRect.velocity.y) < DeckArcUserScrollSessionVelocityCutoff)
            _deckScrollUserSessionActive = false;
    }

    private void EnsureDeckArcSlotOffsetListSize(int slotCount)
    {
        if (deckArcSlotXOffsetById == null)
            deckArcSlotXOffsetById = new List<float>();
        while (deckArcSlotXOffsetById.Count < slotCount)
            deckArcSlotXOffsetById.Add(0f);
        if (deckArcSlotXOffsetById.Count > slotCount)
            deckArcSlotXOffsetById.RemoveRange(slotCount, deckArcSlotXOffsetById.Count - slotCount);
    }

    // Buildbeck "????" button hook:
    // open a confirm dialog before resetting deck.
    public void ResetDeckForRebuild()
    {
        Debug.Log("DeckManager: ResetDeckForRebuild clicked.");
        EnsureDeckUIRefs();
        PlayerData canonical = PlayerData.ResolveCanonical();
        if (canonical != null) PlayerData = canonical;
        else if (PlayerData == null && DataManager != null) PlayerData = DataManager.GetComponent<PlayerData>();
        if (PlayerData != null && GetDeckTotalCount() <= 0)
        {
            ShowDeckHint("\u76EE\u524D\u724C\u7D44\u6C92\u6709\u5361\u724C");
            StartCoroutine(RebuildPanelsAfterReset());
            return;
        }
        ShowResetDeckConfirm();
    }

    // Dedicated hook for deck-container reset button.
    public void OnClickResetDeckButton()
    {
        ResetDeckForRebuild();
    }

    private void PerformResetDeckForRebuild()
    {
        EnsureCoreRefs();

        if (PlayerData == null)
        {
            Debug.LogWarning("DeckManager.ResetDeckForRebuild: PlayerData not found.");
            return;
        }
        var deckMap = PlayerData.GetDeckMap(PlayerData.selectedDeckSlot);
        var keys = new List<int>(deckMap.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int key = keys[i];
            int deckCount = PlayerData.GetDeckCount(PlayerData.selectedDeckSlot, key);
            if (deckCount <= 0) continue;
            PlayerData.AddCollection(key, deckCount);
            PlayerData.SetSelectedDeckCount(key, 0);
        }

        PlayerData.SavePlayerData();
        StartCoroutine(RebuildPanelsAfterReset());

        SceneLoader loader = GetCachedSceneLoader();
        if (loader != null) loader.RefreshEnterBattleState(false);
    }

    private IEnumerator RebuildPanelsAfterReset()
    {
        // Snapshot critical refs before clear, so runtime fallback won't be lost.
        EnsureDeckUIRefs();
        CaptureRuntimeTemplatesIfNeeded();
        Transform cachedLibraryPanel = libraryPanel;
        Transform cachedDeckPanel = deckPanel;
        GameObject cachedLibraryPrefab = librarycardPrefab;
        GameObject cachedDeckPrefab = deckCardPrefab;
        if (cachedLibraryPrefab == null) cachedLibraryPrefab = defaultLibraryCardPrefab;
        if (cachedDeckPrefab == null) cachedDeckPrefab = defaultDeckCardPrefab;
        if (cachedLibraryPrefab == null) cachedLibraryPrefab = runtimeLibraryTemplate;
        if (cachedDeckPrefab == null) cachedDeckPrefab = runtimeDeckTemplate;

        ClearPanels();
        // Destroy() takes effect end-of-frame; rebuild next frame for reliable immediate UI refresh.
        yield return null;

        if (libraryPanel == null) libraryPanel = cachedLibraryPanel;
        if (deckPanel == null) deckPanel = cachedDeckPanel;
        if (librarycardPrefab == null) librarycardPrefab = cachedLibraryPrefab;
        if (deckCardPrefab == null) deckCardPrefab = cachedDeckPrefab;

        EnsureDeckUIRefs();
        if (libraryPanel == null)
        {
            Debug.LogError("DeckManager.RebuildPanelsAfterReset: libraryPanel missing after reset rebuild.");
            yield break;
        }
        if (librarycardPrefab == null) librarycardPrefab = deckCardPrefab;
        if (deckCardPrefab == null) deckCardPrefab = librarycardPrefab;
        if (librarycardPrefab == null)
        {
            Debug.LogError("DeckManager.RebuildPanelsAfterReset: librarycardPrefab missing after reset rebuild.");
            yield break;
        }
        UpdateLibrary();
        if (showDeck) UpdateDeck();
        RefreshScrollablePanels();
        ForcePanelsScrollToTop();

        if (libraryPanel is RectTransform libRt) LayoutRebuilder.ForceRebuildLayoutImmediate(libRt);
        if (deckPanel is RectTransform deckRt) LayoutRebuilder.ForceRebuildLayoutImmediate(deckRt);
        Canvas.ForceUpdateCanvases();
    }

    private void CaptureRuntimeTemplatesIfNeeded()
    {
        if (runtimeLibraryTemplate == null)
        {
            GameObject src = FindAnyCardSource();
            if (src != null)
            {
                runtimeLibraryTemplate = Instantiate(src, transform);
                runtimeLibraryTemplate.name = "RuntimeLibraryTemplate";
                runtimeLibraryTemplate.SetActive(false);
            }
        }
        if (runtimeDeckTemplate == null)
        {
            GameObject src = FindAnyCardSource();
            if (src != null)
            {
                runtimeDeckTemplate = Instantiate(src, transform);
                runtimeDeckTemplate.name = "RuntimeDeckTemplate";
                runtimeDeckTemplate.SetActive(false);
            }
        }
    }

    private GameObject FindAnyCardSource()
    {
        foreach (var kv in libraryDic) if (kv.Value != null) return kv.Value;
        foreach (var kv in deckDic) if (kv.Value != null) return kv.Value;
        if (libraryPanel != null && libraryPanel.childCount > 0) return libraryPanel.GetChild(0).gameObject;
        if (deckPanel != null && deckPanel.childCount > 0) return deckPanel.GetChild(0).gameObject;
        return null;
    }

    public void ShowResetDeckConfirm()
    {
        EnsureResetConfirmPanel();
        if (resetConfirmPanel != null)
        {
            resetConfirmPanel.SetActive(true);
            resetConfirmPanel.transform.SetAsLastSibling();
        }
        else
        {
            // Hard fallback: if confirm panel cannot be created, still allow reset.
            Debug.LogWarning("DeckManager: confirm panel unavailable, resetting deck directly.");
            PerformResetDeckForRebuild();
        }
    }

    public void HideResetDeckConfirm()
    {
        if (resetConfirmPanel != null) resetConfirmPanel.SetActive(false);
    }

    public void ConfirmResetDeckForRebuild()
    {
        HideResetDeckConfirm();
        PerformResetDeckForRebuild();
    }

    private void EnsureResetConfirmPanel()
    {
        if (resetConfirmPanel != null) return;

        Canvas canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("DeckManager: cannot show confirm, Canvas not found.");
            return;
        }

        resetConfirmPanel = new GameObject("ResetDeckConfirmPanel", typeof(RectTransform), typeof(Image));
        resetConfirmPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = resetConfirmPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 260f);
        Image panelBg = resetConfirmPanel.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject msgObj = new GameObject("Message", typeof(RectTransform), typeof(Text));
        msgObj.transform.SetParent(resetConfirmPanel.transform, false);
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0f, 1f);
        msgRect.anchorMax = new Vector2(1f, 1f);
        msgRect.pivot = new Vector2(0.5f, 1f);
        msgRect.offsetMin = new Vector2(24f, -140f);
        msgRect.offsetMax = new Vector2(-24f, -24f);
        Text msgText = msgObj.GetComponent<Text>();
        msgText.font = ResolveCjkUIFont();
        msgText.fontSize = 38;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.color = Color.white;
        msgText.text = "\u78BA\u8A8D\u91CD\u65B0\u7D44\u724C\uFF1F\n\u76EE\u524D\u724C\u7D44\u6703\u88AB\u6E05\u7A7A\u3002";

        CreateConfirmButton(resetConfirmPanel.transform, "ConfirmYesButton", "\u78BA\u8A8D", new Vector2(-120f, -88f), ConfirmResetDeckForRebuild);
        CreateConfirmButton(resetConfirmPanel.transform, "ConfirmNoButton", "\u53D6\u6D88", new Vector2(120f, -88f), HideResetDeckConfirm);
        resetConfirmPanel.SetActive(false);
    }

    public void OnClickEditDeckNameButton()
    {
        ShowDeckNameEditDialog();
    }

    public void ShowDeckNameEditDialog()
    {
        Input.imeCompositionMode = IMECompositionMode.On;
        EnsureCoreRefs();
        if (PlayerData == null)
        {
            ShowDeckHint("無法編輯：找不到玩家資料");
            return;
        }

        EnsureDeckNameEditPanel();
        if (deckNameEditPanel == null) return;

        string cur = PlayerData.GetDeckSlotDisplayName(PlayerData.selectedDeckSlot);
        if (deckNameEditInput != null)
        {
            deckNameEditInput.text = cur;
            OnDeckNameEditValueChanged(cur ?? string.Empty);
        }

        deckNameEditPanel.SetActive(true);
        deckNameEditPanel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutDeckNameEditPanelBelowEditButton();
        _deckNameEditDimClickIgnoreUntilUnscaled = Time.unscaledTime + 0.4f;

        if (_deckNameEditFocusCo != null)
        {
            StopCoroutine(_deckNameEditFocusCo);
            _deckNameEditFocusCo = null;
        }
        _deckNameEditFocusCo = StartCoroutine(CoFocusDeckNameInputSoon());
    }

    private void OnDeckNameEditDimClicked()
    {
        if (Time.unscaledTime < _deckNameEditDimClickIgnoreUntilUnscaled) return;
        HideDeckNameEditPanel();
    }

    public void HideDeckNameEditPanel()
    {
        if (_deckNameEditFocusCo != null)
        {
            StopCoroutine(_deckNameEditFocusCo);
            _deckNameEditFocusCo = null;
        }
        if (deckNameEditInput != null)
            deckNameEditInput.DeactivateInputField(false);
        if (EventSystem.current != null &&
            deckNameEditInput != null &&
            EventSystem.current.currentSelectedGameObject == deckNameEditInput.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
        if (deckNameEditPanel != null) deckNameEditPanel.SetActive(false);
    }

    public void ConfirmDeckNameEdit()
    {
        EnsureCoreRefs();
        PlayerData pd = PlayerData.ResolveCanonical();
        if (pd == null)
        {
            HideDeckNameEditPanel();
            return;
        }

        PlayerData = pd;
        pd.EnsureMinimumDeckSlotCount();
        int nameSlot = Mathf.Clamp(pd.selectedDeckSlot, 0, pd.deckSlotCount - 1);
        string text = deckNameEditInput != null ? deckNameEditInput.text : string.Empty;
        pd.SetDeckSlotDisplayName(nameSlot, text);
        pd.SavePlayerData();
        HideDeckNameEditPanel();
        RefreshDeckSlotTabVisual();
        ShowDeckHint("牌組名稱已更新");
        SceneLoader loader = GetCachedSceneLoader();
        if (loader != null) loader.RefreshEnterBattleState(false);
    }

    private void EnsureDeckNameEditPanel()
    {
        if (deckNameEditPanel != null) return;

        Canvas canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("DeckManager: cannot create deck name editor, Canvas not found.");
            return;
        }

        TMP_FontAsset uiFont = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (uiFont == null) uiFont = hintTMPFont;
        if (uiFont == null) uiFont = TMP_Settings.defaultFontAsset;

        deckNameEditPanel = new GameObject("DeckNameEditModal", typeof(RectTransform));
        deckNameEditPanel.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = deckNameEditPanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        DeckNameEditDismissOnEscape esc = deckNameEditPanel.AddComponent<DeckNameEditDismissOnEscape>();
        esc.Init(this);

        GameObject dimObj = new GameObject("DeckNameEditDim", typeof(RectTransform), typeof(Image), typeof(Button));
        dimObj.transform.SetParent(deckNameEditPanel.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0.12f, 0.09f, 0.08f, 0.52f);
        dimImg.raycastTarget = true;
        Button dimBtn = dimObj.GetComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        ColorBlock dimColors = dimBtn.colors;
        dimColors.highlightedColor = dimImg.color;
        dimColors.pressedColor = dimImg.color;
        dimColors.selectedColor = dimImg.color;
        dimBtn.colors = dimColors;
        dimBtn.onClick.AddListener(OnDeckNameEditDimClicked);

        GameObject cardObj = new GameObject("DeckNameEditCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(deckNameEditPanel.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(608f, 278f);
        Image cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.96f, 0.92f, 0.84f, 1f);
        cardImg.raycastTarget = true;
        deckNameEditCardRt = cardRt;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(28f, -46f);
        titleRt.offsetMax = new Vector2(-28f, -12f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) titleTmp.font = uiFont;
        titleTmp.fontSize = 26f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.28f, 0.25f, 0.2f, 0.92f);
        titleTmp.text = "\u7DE8\u8F2F\u724C\u7D44\u540d\u7A31";

        GameObject inputBgObj = new GameObject("DeckNameInputBg", typeof(RectTransform), typeof(Image));
        inputBgObj.transform.SetParent(cardObj.transform, false);
        RectTransform inputBgRt = inputBgObj.GetComponent<RectTransform>();
        inputBgRt.anchorMin = new Vector2(0.5f, 0.5f);
        inputBgRt.anchorMax = new Vector2(0.5f, 0.5f);
        inputBgRt.pivot = new Vector2(0.5f, 0.5f);
        inputBgRt.anchoredPosition = new Vector2(0f, 2f);
        inputBgRt.sizeDelta = new Vector2(548f, 58f);
        Image inputBgImg = inputBgObj.GetComponent<Image>();
        inputBgImg.color = Color.white;
        inputBgImg.raycastTarget = true;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(inputBgObj.transform, false);
        RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(14f, 10f);
        viewportRt.offsetMax = new Vector2(-14f, -10f);

        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObj.transform.SetParent(viewportObj.transform, false);
        RectTransform phRt = placeholderObj.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        TextMeshProUGUI placeholder = placeholderObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) placeholder.font = uiFont;
        placeholder.fontSize = 26f;
        placeholder.color = new Color(0.42f, 0.4f, 0.38f, 0.58f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.richText = false;
        placeholder.text = "\u70BA\u60A8\u7684\u724C\u7D44\u69CB\u60F3\u540d\u7A31\u5427\uFF01";

        GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inputTextObj.transform.SetParent(viewportObj.transform, false);
        RectTransform inputTextRt = inputTextObj.GetComponent<RectTransform>();
        inputTextRt.anchorMin = Vector2.zero;
        inputTextRt.anchorMax = Vector2.one;
        inputTextRt.offsetMin = Vector2.zero;
        inputTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI inputText = inputTextObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) inputText.font = uiFont;
        inputText.fontSize = 26f;
        inputText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.richText = false;
        inputText.overflowMode = TextOverflowModes.Overflow;
        inputText.enableWordWrapping = false;

        deckNameEditInput = inputBgObj.AddComponent<TmpInputFieldImeRedraw>();
        deckNameEditInput.textViewport = viewportRt;
        deckNameEditInput.textComponent = inputText;
        deckNameEditInput.placeholder = placeholder;
        deckNameEditInput.characterLimit = 24;
        deckNameEditInput.lineType = TMP_InputField.LineType.SingleLine;
        deckNameEditInput.characterValidation = TMP_InputField.CharacterValidation.None;
        // 預設 m_RichText=true 時，IME 組字會插入 <u></u>；若 text 元件 richText=false 會變成畫面上看到標籤字串。
        deckNameEditInput.richText = false;
        deckNameEditInput.onValueChanged.AddListener(OnDeckNameEditValueChanged);
        deckNameEditInput.onSubmit.AddListener(_ => ConfirmDeckNameEdit());

        GameObject counterObj = new GameObject("CharCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
        counterObj.transform.SetParent(cardObj.transform, false);
        RectTransform ctrRt = counterObj.GetComponent<RectTransform>();
        ctrRt.anchorMin = new Vector2(1f, 1f);
        ctrRt.anchorMax = new Vector2(1f, 1f);
        ctrRt.pivot = new Vector2(1f, 1f);
        ctrRt.anchoredPosition = new Vector2(-28f, -128f);
        ctrRt.sizeDelta = new Vector2(96f, 28f);
        deckNameEditCharCounterTmp = counterObj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) deckNameEditCharCounterTmp.font = uiFont;
        deckNameEditCharCounterTmp.fontSize = 18f;
        deckNameEditCharCounterTmp.alignment = TextAlignmentOptions.MidlineRight;
        deckNameEditCharCounterTmp.color = new Color(0.35f, 0.32f, 0.27f, 0.9f);
        deckNameEditCharCounterTmp.text = "0/24";

        CreateDeckNameModalActionButton(cardObj.transform, "DeckNameCancelButton", "\u53D6\u6D88", new Vector2(-118f, 24f), new Vector2(172f, 52f), uiFont, HideDeckNameEditPanel, false);
        CreateDeckNameModalActionButton(cardObj.transform, "DeckNameConfirmButton", "\u78BA\u8A8D", new Vector2(118f, 24f), new Vector2(172f, 52f), uiFont, ConfirmDeckNameEdit, true);

        deckNameEditPanel.SetActive(false);
    }

    /// <summary>將名稱編輯卡片置於 <see cref="editDeckNameButton"/>（或自動解析的編輯鈕）底緣正中下方。</summary>
    private void LayoutDeckNameEditPanelBelowEditButton()
    {
        if (deckNameEditPanel == null) return;
        if (deckNameEditCardRt == null)
        {
            Transform t = deckNameEditPanel.transform.Find("DeckNameEditCard");
            if (t != null) deckNameEditCardRt = t.GetComponent<RectTransform>();
        }
        if (deckNameEditCardRt == null) return;

        RectTransform rootRt = deckNameEditPanel.transform as RectTransform;
        Canvas canvas = deckNameEditPanel.GetComponentInParent<Canvas>();
        if (rootRt == null || canvas == null) return;

        Camera eventCam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            eventCam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        Button editBtn = editDeckNameButton != null
            ? editDeckNameButton
            : BuildbeckLayoutAutoBinder.ResolveEditDeckNameButton(this);

        if (editBtn == null)
        {
            deckNameEditCardRt.anchorMin = deckNameEditCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            deckNameEditCardRt.pivot = new Vector2(0.5f, 0.5f);
            deckNameEditCardRt.anchoredPosition = Vector2.zero;
            return;
        }

        RectTransform buttonRt = editBtn.transform as RectTransform;
        if (buttonRt == null) return;

        Vector3[] corners = new Vector3[4];
        buttonRt.GetWorldCorners(corners);
        Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCam, bottomCenterWorld);
        const float gapPx = 10f;
        screenPoint.y -= gapPx;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screenPoint, eventCam, out Vector2 localPoint))
        {
            deckNameEditCardRt.anchorMin = deckNameEditCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            deckNameEditCardRt.pivot = new Vector2(0.5f, 0.5f);
            deckNameEditCardRt.anchoredPosition = Vector2.zero;
            return;
        }

        deckNameEditCardRt.anchorMin = deckNameEditCardRt.anchorMax = new Vector2(0.5f, 0.5f);
        deckNameEditCardRt.pivot = new Vector2(0.5f, 1f);
        deckNameEditCardRt.anchoredPosition = localPoint;

        float halfW = rootRt.rect.width * 0.5f;
        float halfH = rootRt.rect.height * 0.5f;
        float cardW = deckNameEditCardRt.rect.width;
        float cardH = deckNameEditCardRt.rect.height;
        const float margin = 8f;
        float maxX = Mathf.Max(0f, halfW - cardW * 0.5f - margin);
        Vector2 ap = deckNameEditCardRt.anchoredPosition;
        ap.x = Mathf.Clamp(ap.x, -maxX, maxX);

        float minTopY = -halfH + margin + cardH;
        float maxTopY = halfH - margin;
        if (minTopY <= maxTopY)
            ap.y = Mathf.Clamp(ap.y, minTopY, maxTopY);

        deckNameEditCardRt.anchoredPosition = ap;
    }

    private void OnDeckNameEditValueChanged(string s)
    {
        RefreshDeckNameEditCharCounter();
    }

    private void RefreshDeckNameEditCharCounter()
    {
        if (deckNameEditCharCounterTmp == null || deckNameEditInput == null) return;
        if (deckNameEditPanel == null || !deckNameEditPanel.activeSelf) return;
        int n = deckNameEditInput.text != null ? deckNameEditInput.text.Length : 0;
        if (deckNameEditInput.isFocused)
            n += GetImeCompositionStringLength();
        n = Mathf.Min(24, n);
        deckNameEditCharCounterTmp.text = n + "/24";
    }

    private static int GetImeCompositionStringLength()
    {
        if (EventSystem.current != null &&
            EventSystem.current.currentInputModule is StandaloneInputModule sim &&
            sim.input != null &&
            !string.IsNullOrEmpty(sim.input.compositionString))
            return sim.input.compositionString.Length;
        return string.IsNullOrEmpty(Input.compositionString) ? 0 : Input.compositionString.Length;
    }

    private IEnumerator CoFocusDeckNameInputSoon()
    {
        yield return null;
        yield return null;
        if (deckNameEditInput != null && deckNameEditPanel != null && deckNameEditPanel.activeSelf)
        {
            EventSystem.current?.SetSelectedGameObject(deckNameEditInput.gameObject);
            deckNameEditInput.Select();
            deckNameEditInput.ActivateInputField();
            int len = deckNameEditInput.text != null ? deckNameEditInput.text.Length : 0;
            deckNameEditInput.caretPosition = len;
        }
        _deckNameEditFocusCo = null;
    }

    private static void CreateDeckNameModalActionButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, TMP_FontAsset font, UnityAction onClick, bool primary)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = anchoredPosition;
        btnRect.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = primary
            ? new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f)
            : new Color(0.93f, 0.9f, 0.82f, 1f);

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 26f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = primary ? Color.white : new Color(0.22f, 0.18f, 0.14f, 1f);
        tmp.text = label;
    }

    private void CreateConfirmButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = new Vector2(180f, 62f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text t = textObj.GetComponent<Text>();
        t.font = ResolveCjkUIFont();
        t.fontSize = 30;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.black;
        t.text = label;
    }

    private Font ResolveCjkUIFont()
    {
        if (hintDynamicFont == null)
        {
            hintDynamicFont = Font.CreateDynamicFontFromOSFont(
                new string[] { "Microsoft JhengHei", "Microsoft YaHei", "PMingLiU", "MingLiU", "SimHei", "Arial Unicode MS" },
                32
            );
        }
        if (hintDynamicFont != null) return hintDynamicFont;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // --- 背包場景（showDeck == false）：點卡開浮動檢視，不加入牌組 ---

    private BackpackCardInspectPanel backpackInspectPanel;
    private BackpackProficiencyHelpDialog backpackProficiencyHelpDialog;

    private BackpackCardInspectPanel InspectPanel
    {
        get
        {
            if (backpackInspectPanel == null)
            {
                backpackInspectPanel = GetComponent<BackpackCardInspectPanel>();
                if (backpackInspectPanel == null)
                    backpackInspectPanel = gameObject.AddComponent<BackpackCardInspectPanel>();
                backpackInspectPanel.BindHost(this);
            }
            return backpackInspectPanel;
        }
    }

    public Canvas BackpackInspectResolveCanvas()
    {
        if (libraryPanel != null)
        {
            Canvas libCanvas = libraryPanel.GetComponentInParent<Canvas>(true);
            if (libCanvas != null) return libCanvas;
        }
        return ResolvePrimaryUiCanvas();
    }

    public TMP_FontAsset BackpackInspectResolveFont()
    {
        EnsureTMPHintReady();
        if (hintTMPFont != null && BuildbeckUiFonts.FontSupportsText(hintTMPFont, "戰技"))
            return hintTMPFont;
        TMP_FontAsset fromPrefabs = ResolveHintTMPFont();
        if (fromPrefabs != null && BuildbeckUiFonts.FontSupportsText(fromPrefabs, "戰技"))
            return fromPrefabs;
        TMP_FontAsset buildbeck = BuildbeckUiFonts.ResolveBuildbeckButtonFont();
        if (buildbeck != null) return buildbeck;
        return TMP_Settings.defaultFontAsset;
    }

    internal GameObject BackpackInspectLibraryPrefab => librarycardPrefab;
    internal GameObject BackpackInspectDeckPrefab => deckCardPrefab;

    public Card BackpackInspectGetCard(int cardId) =>
        CardStore != null ? CardStore.GetCardById(cardId) : null;

    public void BackpackInspectFillCollectionIds(List<int> ids)
    {
        if (ids == null || PlayerData == null) return;
        foreach (var kv in PlayerData.playerCollection)
        {
            if (kv.Value <= 0) continue;
            if (CardStore != null && CardStore.GetCardById(kv.Key) == null) continue;
            ids.Add(kv.Key);
        }
    }

    public int BackpackInspectCollectionCount(int cardId) =>
        PlayerData != null ? PlayerData.GetCollectionCount(cardId) : 0;

    internal int BackpackInspectDeckCount(int cardId) =>
        PlayerData != null ? PlayerData.GetSelectedDeckCount(cardId) : 0;

    public string BackpackInspectDeckInclusionText(int cardId)
    {
        if (PlayerData == null) return "尚未加入牌組";
        if (PlayerData.GetSelectedDeckCount(cardId) <= 0) return "尚未加入目前牌組";
        string deckName = PlayerData.GetDeckSlotDisplayName(PlayerData.selectedDeckSlot);
        if (string.IsNullOrWhiteSpace(deckName)) deckName = "牌組";
        return $"已含在牌組 {deckName.Trim()}";
    }

    public void EnsureCoreRefsForInspect() => EnsureCoreRefs();

    public void ShowBackpackCardInspect(Card card, CardDisplay sourceDisplay = null)
    {
        if (card == null) return;
        EnsureCoreRefs();
        if (showDeck) return;
        ShowCardInspect(card, sourceDisplay);
    }

    /// <summary>開啟卡牌詳情（不受 showDeck 限制；商店開包長按等）。</summary>
    public void ShowCardInspect(Card card, CardDisplay sourceDisplay = null)
    {
        if (card == null) return;
        EnsureCoreRefs();
        InspectPanel.Show(card, sourceDisplay);
    }

    public void HideBackpackCardInspect() => HideCardInspect();

    public void HideCardInspect()
    {
        if (backpackInspectPanel != null)
            backpackInspectPanel.Hide();
    }

    private BackpackProficiencyHelpDialog GetBackpackProficiencyHelpDialog()
    {
        if (backpackProficiencyHelpDialog == null)
        {
            backpackProficiencyHelpDialog = GetComponent<BackpackProficiencyHelpDialog>();
            if (backpackProficiencyHelpDialog == null)
                backpackProficiencyHelpDialog = gameObject.AddComponent<BackpackProficiencyHelpDialog>();
        }
        return backpackProficiencyHelpDialog;
    }

    public void ShowBackpackProficiencyHelp()
    {
        Canvas canvas = BackpackInspectResolveCanvas();
        if (canvas == null) return;
        GetBackpackProficiencyHelpDialog().Show(canvas, BackpackInspectResolveFont());
    }

    public void HideBackpackProficiencyHelp() => GetBackpackProficiencyHelpDialog().Hide();

    /// <summary>移除舊版掛在圖書館角落的全域「?」按鈕（說明僅在卡牌詳情內開啟）。</summary>
    private void DestroyBackpackProficiencyHelpFloatingButton()
    {
        if (libraryPanel != null)
        {
            Transform libParent = libraryPanel.parent;
            if (libParent != null)
            {
                Transform legacy = libParent.Find("BackpackProficiencyHelpButton");
                if (legacy != null)
                    Destroy(legacy.gameObject);
            }
        }

        Canvas canvas = BackpackInspectResolveCanvas();
        if (canvas == null) return;
        Transform onCanvas = canvas.transform.Find("BackpackProficiencyHelpButton");
        if (onCanvas != null)
            Destroy(onCanvas.gameObject);
    }

    private sealed class DeckScrollUserGestureRelay : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private DeckManager _host;

        public void Setup(DeckManager host)
        {
            _host = host;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_host != null)
                _host.NotifyDeckScrollPointerHeld(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_host != null)
                _host.NotifyDeckScrollPointerHeld(false);
        }
    }

    private sealed class DeckNameEditDismissOnEscape : MonoBehaviour
    {
        private DeckManager _owner;

        public void Init(DeckManager owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            if (_owner == null || !gameObject.activeInHierarchy) return;
            if (Input.GetKeyDown(KeyCode.Escape))
                _owner.HideDeckNameEditPanel();
        }
    }
}

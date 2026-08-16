using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Story progress world-map runtime:
/// 1) drag map in any direction (mouse/touch) with elastic edge feedback
/// 2) build and render stage nodes (main + side branches) from StoryProgressNodeDatabase
/// 3) in map focus mode, zoom with [ (out) and ] (in) only
/// </summary>
public sealed class StoryProgressWorldMapRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "StoryProgressWorldMapRuntime";
    private const string NodeLayerName = "WorldMapNodeLayer";
    private const string EdgeLayerName = "WorldMapEdgeLayer";
    /// <summary>一般實戰節點 icon 邊長（地圖本地 UI 單位；@2× 出圖建議 80×80）。</summary>
    private const float NodeSize = 40f;
    /// <summary>魔王節點 icon 邊長（@2× 出圖建議 88×88）。</summary>
    private const float BossNodeSize = 44f;
    private const float EdgeThickness = 5f;
    /// <summary>入門主節點 M-1-1 icon 邊長（與一般節點相同；@2× 出圖建議 80×80）。</summary>
    private const float TutorialNodeSize = NodeSize;
    private const string TutorialRootNodeId = "M-1-1";
    private const string SeawallPatrolNodeId = StoryProgressSession.SeawallPatrolNodeId;
    private const string RiverForkNodeId = StoryProgressSession.RiverForkNodeId;
    private const string TutorialRootDisplayName = "港灣訓練場";
    private const string SeawallPatrolDisplayName = "海牆巡邏";
    private const string RiverForkDisplayName = "河岔分波";
    /// <summary>新玩家預設對焦：節點落在 viewport 寬度此比例處（0.5 = 正中）。</summary>
    private const float NewPlayerMapNodeViewportXFromLeft = 0.28f;
    [Header("World Map Zoom (focus mode: [ / ] only)")]
    [SerializeField] private float maxZoom = 4.5f;
    [SerializeField] private float bracketZoomStep = 0.22f;

    /// <summary>Content scale floor: cover-filled layout at 1.0 — zooming out further exposes viewport margins.</summary>
    private const float MapCoverMinZoom = 1f;

    private const float DefaultMapZoom = 3f;

    private static bool sceneHookInstalled;

    private RectTransform viewportRt;
    private GameObject viewportObject;
    private Transform viewportParent;
    private RectTransform mapContentRt;
    /// <summary>Authored map image (child of scroll content). Nodes are parented here so x/y match artwork.</summary>
    private RectTransform mapGraphicRt;
    private ScrollRect scrollRect;
    private Image viewportBgImage;
    private int viewportSiblingIndex = -1;
    private Vector2 mapBaseSize;
    private float currentZoom = DefaultMapZoom;
    private bool mapFocusMode;
    private bool isManualDraggingMap;
    private Vector2 lastManualMousePos;
    private float lastFocusToggleUnscaledTime = -999f;
    private float blockM11PointerToggleUntilUnscaledTime = -999f;
    private int lastBracketZoomAppliedFrame = -1;
    private const float FocusToggleDebounceSeconds = 0.22f;
    private const float M11PointerToggleBlockSeconds = 0.12f;
    private readonly List<UiVisibilityRecord> hiddenUiRecords = new List<UiVisibilityRecord>(16);
    private readonly Dictionary<string, RectTransform> nodeRects = new Dictionary<string, RectTransform>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> clearedNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private string preferredFocusNodeId;
    private static string selectedStageNodeId = TutorialRootNodeId;
    private StoryProgressNodeDatabase nodeDb;
    private static Sprite whiteSprite;
    private bool pendingProgressRefresh;
    private string graphBuiltForSelectedStageNodeId;

    private enum NodeState
    {
        Locked,
        Available,
        Cleared
    }

    private struct UiVisibilityRecord
    {
        public GameObject go;
        public bool wasActive;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallSceneHook()
    {
        if (sceneHookInstalled) return;
        sceneHookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsureRuntime(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryEnsureRuntime(scene);

    private static void TryEnsureRuntime(Scene scene)
    {
        if (!scene.IsValid() || scene.name != StoryProgressSession.StoryProgressSceneName) return;
        if (FindFirstObjectByType<StoryProgressWorldMapRuntime>() != null) return;

        GameObject host = new GameObject(RuntimeObjectName);
        host.AddComponent<StoryProgressWorldMapRuntime>();
    }

    private IEnumerator Start()
    {
        yield return null; // wait one frame so scene UI layout is settled
        Canvas.ForceUpdateCanvases();
        yield return null; // some authored layouts settle on second frame
        Canvas.ForceUpdateCanvases();
        if (!TryBuild())
            enabled = false;
    }

    private bool TryBuild()
    {
        if (!TryResolveMapImage(out Image mapImage))
        {
            Debug.LogWarning("StoryProgressWorldMapRuntime: map background image not found.");
            return false;
        }

        mapContentRt = mapImage.rectTransform;
        if (!BuildScrollViewport(mapImage.transform.parent as RectTransform))
            return false;
        if (maxZoom < DefaultMapZoom)
            maxZoom = DefaultMapZoom * 1.5f;
        SetMapZoom(DefaultMapZoom);

        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        StoryProgressSlotConsistency.EnsureAll(slot);
        LoadClearedNodeProgress();
        nodeDb = StoryProgressNodeDatabaseLibrary.Load();
        BuildNodeGraphVisuals();
        FocusInitialNode();
        return true;
    }

    private bool TryResolveMapImage(out Image best)
    {
        best = null;
        float bestScore = -1f;

        Image[] allImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allImages.Length; i++)
        {
            Image img = allImages[i];
            if (img == null || img.sprite == null) continue;
            if (!img.gameObject.scene.IsValid() || img.gameObject.scene.name != StoryProgressSession.StoryProgressSceneName) continue;
            if (img.GetComponentInParent<Button>() != null) continue;
            if (IsUnderNamedAncestor(img.transform, "Panel")) continue;
            if (IsUnderNamedAncestor(img.transform, StoryProgressLevelCopy.ViewLevelFlowPanelName)) continue;
            if (img.rectTransform.rect.width < 800f || img.rectTransform.rect.height < 450f) continue;

            float area = img.rectTransform.rect.width * img.rectTransform.rect.height;
            float score = area;
            string n = img.gameObject.name;
            if (n.IndexOf("map", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("bg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0)
                score *= 1.2f;

            if (score <= bestScore) continue;
            bestScore = score;
            best = img;
        }

        return best != null;
    }

    private bool BuildScrollViewport(RectTransform originalParent)
    {
        if (mapContentRt == null || originalParent == null) return false;
        int originalSibling = mapContentRt.GetSiblingIndex();
        Image mapImage = mapContentRt.GetComponent<Image>();

        GameObject viewportObj = new GameObject(
            "StoryProgressMapViewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(RectMask2D),
            typeof(ScrollRect));
        viewportObj.transform.SetParent(originalParent, false);
        viewportObj.transform.SetSiblingIndex(originalSibling);
        viewportObject = viewportObj;
        viewportParent = originalParent;
        viewportSiblingIndex = originalSibling;

        viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.pivot = new Vector2(0.5f, 0.5f);
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportRt.anchoredPosition = Vector2.zero;
        viewportRt.localScale = Vector3.one;

        Image viewportImage = viewportObj.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;
        viewportBgImage = viewportImage;

        GameObject contentObj = new GameObject("StoryProgressMapContent", typeof(RectTransform));
        contentObj.transform.SetParent(viewportRt, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.pivot = new Vector2(0.5f, 0.5f);

        mapBaseSize = ResolveNativeMapSize(mapImage, mapContentRt);

        mapGraphicRt = mapContentRt;
        mapGraphicRt.SetParent(contentRt, false);
        mapGraphicRt.SetAsFirstSibling();
        mapGraphicRt.anchorMin = new Vector2(0.5f, 0.5f);
        mapGraphicRt.anchorMax = new Vector2(0.5f, 0.5f);
        mapGraphicRt.pivot = new Vector2(0.5f, 0.5f);
        mapGraphicRt.anchoredPosition = Vector2.zero;

        mapContentRt = contentRt;

        ApplyMapCoverFillLayout();

        scrollRect = viewportObj.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = mapContentRt;
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.16f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.125f;
        scrollRect.scrollSensitivity = 1.1f;
        scrollRect.onValueChanged.AddListener(_ => ApplyBoundaryFeedbackFromOverscroll());
        ApplyMapFocusInteractivity();

        return true;
    }

    private void ApplyBoundaryFeedbackFromOverscroll()
    {
        if (scrollRect == null || mapContentRt == null || viewportRt == null) return;
        Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewportRt, mapContentRt);
        Rect viewRect = viewportRt.rect;

        float left = Mathf.Max(0f, viewRect.xMin - contentBounds.min.x);
        float right = Mathf.Max(0f, contentBounds.max.x - viewRect.xMax);
        float bottom = Mathf.Max(0f, viewRect.yMin - contentBounds.min.y);
        float top = Mathf.Max(0f, contentBounds.max.y - viewRect.yMax);
        float overscroll = Mathf.Max(left, right, top, bottom);

        // Only show subtle feedback when actually pushing into map boundaries.
        float t = Mathf.Clamp01(overscroll / 26f);
        if (viewportBgImage != null)
            viewportBgImage.color = new Color(0f, 0f, 0f, 0.01f + 0.08f * t);
    }

    private void LateUpdate()
    {
        if (pendingProgressRefresh)
        {
            pendingProgressRefresh = false;
            RefreshProgressFromSaveImmediate();
        }

        if (mapFocusMode && viewportRt != null && viewportRt.GetSiblingIndex() != viewportRt.parent.childCount - 1)
            viewportRt.SetAsLastSibling();
        if (viewportRt != null && viewportSiblingIndex >= 0 && viewportRt.GetSiblingIndex() != viewportSiblingIndex)
        {
            if (!mapFocusMode)
                viewportRt.SetSiblingIndex(viewportSiblingIndex);
        }

        if (viewportRt != null && viewportRt.localScale != Vector3.one)
            viewportRt.localScale = Vector3.one;
    }

    /// <summary>等比放大至填滿 viewport（cover）；超出部分靠 ScrollRect 滑動。</summary>
    private void ApplyMapCoverFillLayout()
    {
        if (viewportRt == null || mapGraphicRt == null || mapContentRt == null)
            return;
        if (mapBaseSize.x < 1f || mapBaseSize.y < 1f)
            return;

        Canvas.ForceUpdateCanvases();
        Vector2 viewportSize = viewportRt.rect.size;
        if (viewportSize.x < 1f || viewportSize.y < 1f)
            return;

        float fillScale = Mathf.Max(
            viewportSize.x / mapBaseSize.x,
            viewportSize.y / mapBaseSize.y);

        Vector2 filledSize = mapBaseSize * fillScale;
        mapContentRt.sizeDelta = filledSize;
        mapContentRt.anchoredPosition = Vector2.zero;
        mapGraphicRt.sizeDelta = mapBaseSize;
        mapGraphicRt.localScale = new Vector3(fillScale, fillScale, 1f);
        ApplyNativeMapImageLayout(mapGraphicRt.GetComponent<Image>());
        SyncCurrentZoomFromContent();
        if (currentZoom < MapCoverMinZoom)
            SetMapZoom(MapCoverMinZoom);
        ClampContentAnchoredPosition();
        ReapplyMapFocusOnPreferredNode();
    }

    private void Update()
    {
        HandleFocusModeDirectInput();
        HandleBracketKeyboardZoom();
    }

    private void HandleFocusModeDirectInput()
    {
        if (!mapFocusMode || mapContentRt == null || viewportRt == null) return;

        Camera uiCam = GetUiEventCamera();
        Vector2 mouse = Input.mousePosition;
        bool insideViewport = RectTransformUtility.RectangleContainsScreenPoint(viewportRt, mouse, uiCam);

        // Fallback: second click on 1-1 exits focus (skip same click as Button.onClick).
        if (Time.unscaledTime >= blockM11PointerToggleUntilUnscaledTime &&
            Input.GetMouseButtonDown(0) &&
            nodeRects.TryGetValue("M-1-1", out RectTransform nodeRt) &&
            RectTransformUtility.RectangleContainsScreenPoint(nodeRt, mouse, uiCam))
        {
            TryToggleMapFocusMode();
            return;
        }

        if (!insideViewport)
            isManualDraggingMap = false;

        if (insideViewport && Input.GetMouseButtonDown(0))
        {
            isManualDraggingMap = true;
            lastManualMousePos = mouse;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isManualDraggingMap = false;
        }

        if (insideViewport && isManualDraggingMap && Input.GetMouseButton(0))
        {
            Vector2 delta = mouse - lastManualMousePos;
            lastManualMousePos = mouse;
            mapContentRt.anchoredPosition += delta;
            ClampContentAnchoredPosition();
        }

    }

    private void HandleBracketKeyboardZoom()
    {
        if (!mapFocusMode || mapContentRt == null) return;
        TryApplyBracketZoom(ReadBracketZoomDirection());
    }

    private void TryApplyBracketZoom(int dir)
    {
        if (dir == 0) return;
        int frame = Time.frameCount;
        if (frame == lastBracketZoomAppliedFrame) return;
        lastBracketZoomAppliedFrame = frame;
        TryApplyZoomStep(dir, GetZoomPivotScreenPoint());
    }

    private static int BracketKeyCodeToZoomDirection(KeyCode keyCode)
    {
        if (keyCode == KeyCode.RightBracket) return 1;
        if (keyCode == KeyCode.LeftBracket) return -1;
        return 0;
    }

    private static int ReadBracketZoomDirection()
    {
        if (Input.GetKeyDown(KeyCode.RightBracket)) return 1;
        if (Input.GetKeyDown(KeyCode.LeftBracket)) return -1;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rightBracketKey.wasPressedThisFrame) return 1;
            if (kb.leftBracketKey.wasPressedThisFrame) return -1;
        }
#endif
        return 0;
    }

    private Vector2 GetZoomPivotScreenPoint()
    {
        if (viewportRt == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Camera uiCam = GetUiEventCamera();
        Vector2 mouse = Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(viewportRt, mouse, uiCam))
            return mouse;
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void TryApplyZoomStep(int direction, Vector2 screenPivot)
    {
        if (direction == 0) return;
        TryApplyZoomSignedDelta(direction * bracketZoomStep, screenPivot);
    }

    private void TryApplyZoomSignedDelta(float signedDelta, Vector2 screenPivot)
    {
        if (mapContentRt == null) return;
        SyncCurrentZoomFromContent();

        float targetZoom = Mathf.Clamp(currentZoom + signedDelta, MapCoverMinZoom, maxZoom);
        if (Mathf.Approximately(targetZoom, currentZoom)) return;
        ApplyZoom(targetZoom, screenPivot);
    }

    private void SyncCurrentZoomFromContent()
    {
        if (mapContentRt == null) return;
        currentZoom = Mathf.Max(0.0001f, mapContentRt.localScale.x);
    }

    private void ApplyZoom(float targetZoom, Vector2 screenPivot)
    {
        if (mapContentRt == null || viewportRt == null) return;
        SyncCurrentZoomFromContent();
        if (Mathf.Approximately(targetZoom, currentZoom)) return;

        Camera eventCamera = GetUiEventCamera();
        RectTransform zoomTarget = mapContentRt;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(zoomTarget, screenPivot, eventCamera, out Vector3 before);

        currentZoom = targetZoom;
        zoomTarget.localScale = new Vector3(currentZoom, currentZoom, 1f);

        RectTransformUtility.ScreenPointToWorldPointInRectangle(zoomTarget, screenPivot, eventCamera, out Vector3 after);
        zoomTarget.position -= (after - before);
        ClampContentAnchoredPosition();
    }

    private Camera GetUiEventCamera()
    {
        Canvas canvas = viewportRt != null ? viewportRt.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return null;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void ClampContentAnchoredPosition()
    {
        if (mapContentRt == null || viewportRt == null) return;

        Rect vp = viewportRt.rect;
        float scaledWidth = mapContentRt.rect.width * Mathf.Max(0.0001f, mapContentRt.localScale.x);
        float scaledHeight = mapContentRt.rect.height * Mathf.Max(0.0001f, mapContentRt.localScale.y);
        float maxOffsetX = Mathf.Max(0f, (scaledWidth - vp.width) * 0.5f);
        float maxOffsetY = Mathf.Max(0f, (scaledHeight - vp.height) * 0.5f);

        Vector2 anchored = mapContentRt.anchoredPosition;
        anchored.x = Mathf.Clamp(anchored.x, -maxOffsetX, maxOffsetX);
        anchored.y = Mathf.Clamp(anchored.y, -maxOffsetY, maxOffsetY);
        mapContentRt.anchoredPosition = anchored;
    }

    private static Vector2 ResolveNativeMapSize(Image mapImage, RectTransform mapRt)
    {
        if (mapImage != null && mapImage.sprite != null)
            return mapImage.sprite.rect.size;

        if (mapRt == null)
            return new Vector2(1672f, 941f);

        Vector2 size = mapRt.sizeDelta;
        if (size.x < 8f || size.y < 8f)
            size = mapRt.rect.size;
        if (size.x >= 8f && size.y >= 8f)
            return size;

        return new Vector2(1672f, 941f);
    }

    private static void ApplyNativeMapImageLayout(Image mapImage)
    {
        if (mapImage == null)
            return;

        mapImage.preserveAspect = true;
        mapImage.type = Image.Type.Simple;
    }

    private static bool IsUnderNamedAncestor(Transform t, string ancestorName)
    {
        while (t != null)
        {
            if (string.Equals(t.name, ancestorName, StringComparison.Ordinal))
                return true;
            t = t.parent;
        }

        return false;
    }

    private void LoadClearedNodeProgress()
    {
        clearedNodeIds.Clear();
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (HarborTrainingProgressState.IsHarborCombatCleared(slot))
            clearedNodeIds.Add("M-1-1");
        if (TutorialProgressState.IsM12TrioMasteryCleared(slot))
            clearedNodeIds.Add(SeawallPatrolNodeId);
        if (SideQuestA1ProgressState.IsNodeCleared(slot))
            clearedNodeIds.Add(SideQuestA1ProgressState.NodeId);
    }

    private void BuildNodeGraphVisuals()
    {
        RectTransform nodeParent = mapGraphicRt != null ? mapGraphicRt : mapContentRt;
        if (nodeParent == null || nodeDb == null || nodeDb.nodes == null) return;

        Transform oldEdge = nodeParent.Find(EdgeLayerName);
        if (oldEdge != null) Destroy(oldEdge.gameObject);
        Transform oldNode = nodeParent.Find(NodeLayerName);
        if (oldNode != null) Destroy(oldNode.gameObject);

        GameObject edgeLayer = new GameObject(EdgeLayerName, typeof(RectTransform));
        edgeLayer.transform.SetParent(nodeParent, false);
        RectTransform edgeLayerRt = edgeLayer.GetComponent<RectTransform>();
        Stretch(edgeLayerRt);

        GameObject nodeLayer = new GameObject(NodeLayerName, typeof(RectTransform));
        nodeLayer.transform.SetParent(nodeParent, false);
        RectTransform nodeLayerRt = nodeLayer.GetComponent<RectTransform>();
        Stretch(nodeLayerRt);

        nodeRects.Clear();
        for (int i = 0; i < nodeDb.nodes.Length; i++)
        {
            StoryProgressNodeEntry node = nodeDb.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId)) continue;
            RectTransform rt = CreateNodeUi(nodeLayerRt, node, ResolveNodeState(node));
            nodeRects[node.nodeId] = rt;
        }

        if (nodeDb.edges == null) return;
        for (int i = 0; i < nodeDb.edges.Length; i++)
        {
            StoryProgressEdgeEntry edge = nodeDb.edges[i];
            if (edge == null) continue;
            if (!nodeRects.TryGetValue(edge.fromNodeId, out RectTransform fromRt)) continue;
            if (!nodeRects.TryGetValue(edge.toNodeId, out RectTransform toRt)) continue;
            CreateEdgeUi(edgeLayerRt, fromRt.anchoredPosition, toRt.anchoredPosition, edge.pathType);
        }

        graphBuiltForSelectedStageNodeId = selectedStageNodeId;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private RectTransform CreateNodeUi(RectTransform parent, StoryProgressNodeEntry node, NodeState state)
    {
        bool isTutorialRootNode = node != null &&
                                  string.Equals(node.nodeId, TutorialRootNodeId, StringComparison.OrdinalIgnoreCase);
        bool isSelectedSeawallNode = node != null &&
                                     string.Equals(node.nodeId, SeawallPatrolNodeId, StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(selectedStageNodeId, SeawallPatrolNodeId, StringComparison.OrdinalIgnoreCase);
        bool isSelectedRiverForkNode = node != null &&
                                       string.Equals(node.nodeId, RiverForkNodeId, StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(selectedStageNodeId, RiverForkNodeId, StringComparison.OrdinalIgnoreCase);

        GameObject go = new GameObject(
            "Node_" + node.nodeId,
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float size = ResolveNodeIconSize(node);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = NodeToAnchored(node);

        Image image = go.GetComponent<Image>();
        bool useCustomNodeIcon = false;
        Sprite customIcon = ResolveStoryMapNodeIcon(node, state);
        if (customIcon != null)
        {
            useCustomNodeIcon = true;
            image.sprite = customIcon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            float aspect = customIcon.rect.width / Mathf.Max(1f, customIcon.rect.height);
            float height = ResolveCustomNodeIconHeight(node);
            rt.sizeDelta = new Vector2(height * aspect, height);
        }

        if (!useCustomNodeIcon)
        {
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = isTutorialRootNode ? new Color(1f, 1f, 1f, 0.98f) : ResolveNodeColor(state, node);
        }

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        Color baseColor = image.color;
        ColorBlock cb = button.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.2f);
        cb.pressedColor = Color.Lerp(baseColor, Color.black, 0.16f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = baseColor;
        cb.colorMultiplier = 1f;
        button.colors = cb;
        button.interactable = state != NodeState.Locked;
        button.onClick.AddListener(() => OnNodeClicked(node));

        GameObject textGo = new GameObject("StageCode", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0f);
        textRt.anchorMax = new Vector2(0.5f, 0f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = new Vector2(0f, node.isTutorial ? -10f : -10f);
        textRt.sizeDelta = new Vector2(96f, 30f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = node.stageCode;
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = node.isTutorial ? new Color(0.94f, 0.98f, 1f, 1f) : new Color(0.95f, 0.93f, 0.88f, 1f);
        tmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(tmp);

        if (isTutorialRootNode)
        {
            AddTutorialRootStatusBadge(rt);
            AddTutorialRootName(rt);
            AddTutorialRootSubtitle(rt);
        }
        else if (isSelectedSeawallNode)
        {
            AddSelectedSeawallStatusBadge(rt);
            AddSelectedSeawallName(rt);
            AddSelectedSeawallSubtitle(rt);
        }
        else if (isSelectedRiverForkNode)
        {
            AddSelectedRiverForkStatusBadge(rt);
            AddSelectedRiverForkName(rt);
            AddSelectedRiverForkSubtitle(rt);
        }

        return rt;
    }

    private static bool ShouldUseIntro11InstructionIcon()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        return !TutorialProgressState.IsAcademyIntroGraduated(slot);
    }

    private static bool ShouldUseIntro11PracticalApplicationIcon()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        return TutorialProgressState.IsAcademyIntroGraduated(slot);
    }

    private static Sprite ResolveTutorialRootNodeIcon()
    {
        if (ShouldUseIntro11InstructionIcon())
            return StoryProgressUiSprites.GetIntro11InstructionIcon();
        if (ShouldUseIntro11PracticalApplicationIcon())
            return StoryProgressUiSprites.GetIntro11PracticalApplicationIcon();
        return null;
    }

    private static Sprite ResolveStoryMapNodeIcon(StoryProgressNodeEntry node, NodeState state)
    {
        if (node == null)
            return null;

        bool isTutorialRootNode =
            string.Equals(node.nodeId, TutorialRootNodeId, StringComparison.OrdinalIgnoreCase);
        bool isSeawallNode =
            string.Equals(node.nodeId, SeawallPatrolNodeId, StringComparison.OrdinalIgnoreCase);
        bool isRiverForkNode =
            string.Equals(node.nodeId, RiverForkNodeId, StringComparison.OrdinalIgnoreCase);
        if (!isTutorialRootNode && !isSeawallNode && !isRiverForkNode)
            return null;

        if (state == NodeState.Cleared)
            return StoryProgressUiSprites.GetClearNodeIcon();

        if (isTutorialRootNode)
            return ResolveTutorialRootNodeIcon();

        return null;
    }

    private static float ResolveCustomNodeIconHeight(StoryProgressNodeEntry node)
    {
        if (node != null &&
            (string.Equals(node.nodeId, TutorialRootNodeId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(node.nodeId, SeawallPatrolNodeId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(node.nodeId, RiverForkNodeId, StringComparison.OrdinalIgnoreCase)))
            return TutorialNodeSize * 1.35f;

        return ResolveNodeIconSize(node);
    }

    private void AddTutorialRootStatusBadge(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        string statusText = ResolveTutorialRootStatusText();
        Color statusColor = ResolveTutorialRootStatusColor(statusText);

        GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(nodeRt, false);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 1f);
        badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 0f);
        badgeRt.anchoredPosition = new Vector2(0f, 6f);
        badgeRt.sizeDelta = new Vector2(92f, 28f);

        Image badgeBg = badgeGo.GetComponent<Image>();
        badgeBg.sprite = GetWhiteSprite();
        badgeBg.type = Image.Type.Sliced;
        badgeBg.color = statusColor;
        badgeBg.raycastTarget = false;

        GameObject textGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(badgeGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = statusText;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(tmp);
    }

    private void AddTutorialRootName(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        GameObject nameGo = new GameObject("StageName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(nodeRt, false);
        RectTransform nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0f);
        nameRt.anchorMax = new Vector2(0.5f, 0f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -44f);
        nameRt.sizeDelta = new Vector2(180f, 34f);

        TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = TutorialRootDisplayName;
        nameTmp.fontSize = 20f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = new Color(0.96f, 0.96f, 0.96f, 1f);
        nameTmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(nameTmp);
    }

    private void AddTutorialRootSubtitle(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        GameObject subGo = new GameObject("StageSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(nodeRt, false);
        RectTransform subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0f);
        subRt.anchorMax = new Vector2(0.5f, 0f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -78f);
        subRt.sizeDelta = new Vector2(200f, 26f);

        TextMeshProUGUI subTmp = subGo.GetComponent<TextMeshProUGUI>();
        subTmp.text = ResolveTutorialRootSubtitleText();
        subTmp.fontSize = 15f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.82f, 0.88f, 0.84f, 0.95f);
        subTmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(subTmp);
    }

    private static string ResolveTutorialRootSubtitleText()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (TutorialProgressState.IsAcademyIntroGraduated(slot))
            return "簡單・普通・困難";
        TutorialProgressState.GetAcademyIntroProgressForDisplay(slot, out bool plotCompleted, out bool battleCompleted);
        if (plotCompleted && battleCompleted)
            return "簡單・普通・困難";
        return "入門課 · 學院內";
    }

    private static string ResolveTutorialRootStatusText() =>
        StoryProgressLevelCopy.ResolveMapStatusLabelForActiveSlot();

    private void AddSelectedSeawallStatusBadge(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        string statusText = ResolveSeawallPatrolStatusText();
        Color statusColor = ResolveTutorialRootStatusColor(statusText);

        GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(nodeRt, false);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 1f);
        badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 0f);
        badgeRt.anchoredPosition = new Vector2(0f, 6f);
        badgeRt.sizeDelta = new Vector2(92f, 28f);

        Image badgeBg = badgeGo.GetComponent<Image>();
        badgeBg.sprite = GetWhiteSprite();
        badgeBg.type = Image.Type.Sliced;
        badgeBg.color = statusColor;
        badgeBg.raycastTarget = false;

        GameObject textGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(badgeGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = statusText;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(tmp);
    }

    private void AddSelectedSeawallName(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        GameObject nameGo = new GameObject("StageName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(nodeRt, false);
        RectTransform nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0f);
        nameRt.anchorMax = new Vector2(0.5f, 0f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -44f);
        nameRt.sizeDelta = new Vector2(180f, 34f);

        TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = SeawallPatrolDisplayName;
        nameTmp.fontSize = 20f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = new Color(0.96f, 0.96f, 0.96f, 1f);
        nameTmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(nameTmp);
    }

    private void AddSelectedSeawallSubtitle(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        GameObject subGo = new GameObject("StageSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(nodeRt, false);
        RectTransform subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0f);
        subRt.anchorMax = new Vector2(0.5f, 0f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -78f);
        subRt.sizeDelta = new Vector2(200f, 26f);

        TextMeshProUGUI subTmp = subGo.GetComponent<TextMeshProUGUI>();
        subTmp.text = ResolveSeawallPatrolSubtitleText();
        subTmp.fontSize = 15f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.82f, 0.88f, 0.84f, 0.95f);
        subTmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(subTmp);
    }

    private static string ResolveSeawallPatrolStatusText()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
            return "Clear";
        if (M12SeawallPatrolProgressState.IsMidPatrolComplete(slot) ||
            M12SeawallPatrolProgressState.IsPhaseAComplete(slot))
            return "進行中";
        return "NEW";
    }

    private static string ResolveSeawallPatrolSubtitleText()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M12SeawallPatrolProgressState.IsNodeCleared(slot))
            return "可重溫關卡";
        if (M12SeawallPatrolProgressState.IsMidPatrolComplete(slot))
            return "階段 B 進行中";
        if (M12SeawallPatrolProgressState.IsPhaseAComplete(slot))
            return "沿海散策中";
        return "御三家段考";
    }

    private void AddSelectedRiverForkStatusBadge(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        string statusText = ResolveRiverForkStatusText();
        Color statusColor = ResolveTutorialRootStatusColor(statusText);

        GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        badgeGo.transform.SetParent(nodeRt, false);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 1f);
        badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 0f);
        badgeRt.anchoredPosition = new Vector2(0f, 6f);
        badgeRt.sizeDelta = new Vector2(92f, 28f);

        Image badgeBg = badgeGo.GetComponent<Image>();
        badgeBg.sprite = GetWhiteSprite();
        badgeBg.type = Image.Type.Sliced;
        badgeBg.color = statusColor;
        badgeBg.raycastTarget = false;

        GameObject textGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(badgeGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = statusText;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(tmp);
    }

    private void AddSelectedRiverForkName(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        GameObject nameGo = new GameObject("StageName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(nodeRt, false);
        RectTransform nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 0f);
        nameRt.anchorMax = new Vector2(0.5f, 0f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, -44f);
        nameRt.sizeDelta = new Vector2(180f, 34f);

        TextMeshProUGUI nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = RiverForkDisplayName;
        nameTmp.fontSize = 20f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = new Color(0.96f, 0.96f, 0.96f, 1f);
        nameTmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(nameTmp);
    }

    private void AddSelectedRiverForkSubtitle(RectTransform nodeRt)
    {
        if (nodeRt == null) return;

        GameObject subGo = new GameObject("StageSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(nodeRt, false);
        RectTransform subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0f);
        subRt.anchorMax = new Vector2(0.5f, 0f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -78f);
        subRt.sizeDelta = new Vector2(200f, 26f);

        TextMeshProUGUI subTmp = subGo.GetComponent<TextMeshProUGUI>();
        subTmp.text = ResolveRiverForkSubtitleText();
        subTmp.fontSize = 15f;
        subTmp.fontStyle = FontStyles.Normal;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.82f, 0.88f, 0.84f, 0.95f);
        subTmp.raycastTarget = false;
        SettingsUiFonts.ApplyTo(subTmp);
    }

    private static string ResolveRiverForkStatusText()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M13RiverForkProgressState.IsNodeCleared(slot))
            return "Clear";
        if (M13RiverForkProgressState.IsRoseTrialSeen(slot) ||
            M13RiverForkProgressState.IsBirdDuelComplete(slot) ||
            M13RiverForkProgressState.IsOpeningSeen(slot))
            return "進行中";
        return "NEW";
    }

    private static string ResolveRiverForkSubtitleText()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        if (M13RiverForkProgressState.IsNodeCleared(slot))
            return "可重溫關卡";
        if (M13RiverForkProgressState.IsRoseTrialSeen(slot))
            return "分波對決";
        if (M13RiverForkProgressState.IsPhaseAComplete(slot))
            return "玫瑰試煉";
        if (M13RiverForkProgressState.IsForkStrollComplete(slot))
            return "冷爐迎測";
        if (M13RiverForkProgressState.IsBirdDuelComplete(slot))
            return M13RiverForkProgressState.HasBirdDuelSRank(slot) ? "S 評 · 岔路散策" : "岔路散策";
        if (M13RiverForkProgressState.IsOpeningSeen(slot))
            return "分波鬥鳥";
        return "迎潮實測 · 燈下之詢";
    }

    public static string SelectedStageNodeId => selectedStageNodeId;

    public static void SetSelectedStageNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return;
        selectedStageNodeId = nodeId;
    }

    /// <summary>入門勝利／港灣通關後刷新地圖節點與 M-1-1 狀態徽章（與關卡面板、玩家資訊一致）。</summary>
    public static void RequestRefreshProgress()
    {
        StoryProgressWorldMapRuntime runtime =
            UnityEngine.Object.FindFirstObjectByType<StoryProgressWorldMapRuntime>();
        if (runtime == null) return;
        runtime.pendingProgressRefresh = true;
    }

    public void RefreshProgressFromSave()
    {
        pendingProgressRefresh = true;
    }

    private void RefreshProgressFromSaveImmediate()
    {
        int slot = PlayerData.GetActivePlayerSlotOrDefault();
        StoryProgressSlotConsistency.EnsureAll(slot);
        LoadClearedNodeProgress();
        if (nodeDb == null)
            nodeDb = StoryProgressNodeDatabaseLibrary.Load();

        if (!TryApplyProgressToExistingGraph())
            BuildNodeGraphVisuals();

        ApplyProgressBadgesToExistingNodes();
        ReapplyMapFocusOnPreferredNode();
    }

    private void ApplyProgressBadgesToExistingNodes()
    {
        ApplyTutorialRootStatusBadgeToExistingNode();
        ApplySeawallPatrolStatusBadgeToExistingNode();
        ApplyRiverForkStatusBadgeToExistingNode();
    }

    private void ApplyTutorialRootStatusBadgeToExistingNode()
    {
        if (!nodeRects.TryGetValue(TutorialRootNodeId, out RectTransform nodeRt) || nodeRt == null)
            return;

        string statusText = ResolveTutorialRootStatusText();
        ApplyStatusBadgeToExistingNode(nodeRt, statusText, ResolveTutorialRootStatusColor(statusText));
        ApplySubtitleToExistingNode(nodeRt, ResolveTutorialRootSubtitleText());
    }

    private void ApplySeawallPatrolStatusBadgeToExistingNode()
    {
        if (!string.Equals(selectedStageNodeId, SeawallPatrolNodeId, StringComparison.OrdinalIgnoreCase))
            return;
        if (!nodeRects.TryGetValue(SeawallPatrolNodeId, out RectTransform nodeRt) || nodeRt == null)
            return;

        string statusText = ResolveSeawallPatrolStatusText();
        ApplyStatusBadgeToExistingNode(nodeRt, statusText, ResolveTutorialRootStatusColor(statusText));
        ApplySubtitleToExistingNode(nodeRt, ResolveSeawallPatrolSubtitleText());
    }

    private void ApplyRiverForkStatusBadgeToExistingNode()
    {
        if (!string.Equals(selectedStageNodeId, RiverForkNodeId, StringComparison.OrdinalIgnoreCase))
            return;
        if (!nodeRects.TryGetValue(RiverForkNodeId, out RectTransform nodeRt) || nodeRt == null)
            return;

        string statusText = ResolveRiverForkStatusText();
        ApplyStatusBadgeToExistingNode(nodeRt, statusText, ResolveTutorialRootStatusColor(statusText));
        ApplySubtitleToExistingNode(nodeRt, ResolveRiverForkSubtitleText());
    }

    private static void ApplyStatusBadgeToExistingNode(RectTransform nodeRt, string statusText, Color statusColor)
    {
        Transform statusTextTf = nodeRt.Find("StatusBadge/StatusText");
        if (statusTextTf == null)
            return;

        if (statusTextTf.parent != null)
        {
            Image badgeBg = statusTextTf.parent.GetComponent<Image>();
            if (badgeBg != null)
                badgeBg.color = statusColor;
        }

        TextMeshProUGUI tmp = statusTextTf.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = statusText;
    }

    private static void ApplySubtitleToExistingNode(RectTransform nodeRt, string subtitle)
    {
        Transform subtitleTf = nodeRt.Find("StageSubtitle");
        if (subtitleTf == null)
            return;

        TextMeshProUGUI tmp = subtitleTf.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = subtitle;
    }

    private bool TryApplyProgressToExistingGraph()
    {
        RectTransform nodeParent = mapGraphicRt != null ? mapGraphicRt : mapContentRt;
        if (nodeParent == null || nodeDb == null || nodeDb.nodes == null)
            return false;

        Transform nodeLayer = nodeParent.Find(NodeLayerName);
        if (nodeLayer == null || nodeRects.Count == 0)
            return false;
        if (!string.Equals(graphBuiltForSelectedStageNodeId, selectedStageNodeId, StringComparison.OrdinalIgnoreCase))
            return false;

        for (int i = 0; i < nodeDb.nodes.Length; i++)
        {
            StoryProgressNodeEntry node = nodeDb.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                continue;
            if (!nodeRects.TryGetValue(node.nodeId, out RectTransform nodeRt) || nodeRt == null)
                return false;

            NodeState state = ResolveNodeState(node);
            if (!ApplyNodeProgressVisual(nodeRt, node, state))
                return false;
        }

        return true;
    }

    private bool ApplyNodeProgressVisual(RectTransform nodeRt, StoryProgressNodeEntry node, NodeState state)
    {
        Image image = nodeRt.GetComponent<Image>();
        Button button = nodeRt.GetComponent<Button>();
        if (image == null || button == null)
            return false;

        bool isTutorialRootNode = string.Equals(node.nodeId, TutorialRootNodeId, StringComparison.OrdinalIgnoreCase);
        Sprite customIcon = ResolveStoryMapNodeIcon(node, state);
        if (customIcon != null)
        {
            if (isTutorialRootNode)
            {
                Sprite expectedTutorialIcon = ResolveTutorialRootNodeIcon();
                if (expectedTutorialIcon != null && image.sprite != expectedTutorialIcon)
                    return false;
            }

            image.sprite = customIcon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            float aspect = customIcon.rect.width / Mathf.Max(1f, customIcon.rect.height);
            float height = ResolveCustomNodeIconHeight(node);
            nodeRt.sizeDelta = new Vector2(height * aspect, height);
        }
        else
        {
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            Color baseColor = isTutorialRootNode
                ? new Color(1f, 1f, 1f, 0.98f)
                : ResolveNodeColor(state, node);
            image.color = baseColor;

            ColorBlock cb = button.colors;
            cb.normalColor = baseColor;
            cb.highlightedColor = Color.Lerp(baseColor, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(baseColor, Color.black, 0.16f);
            cb.selectedColor = cb.highlightedColor;
            cb.disabledColor = baseColor;
            button.colors = cb;
        }

        button.interactable = state != NodeState.Locked;
        return true;
    }

    private static Color ResolveTutorialRootStatusColor(string statusText)
    {
        if (string.Equals(statusText, "Clear", StringComparison.OrdinalIgnoreCase))
            return new Color(0.34f, 0.72f, 0.44f, 0.98f);
        if (string.Equals(statusText, "進行中", StringComparison.Ordinal))
            return new Color(0.24f, 0.57f, 0.90f, 0.98f);
        return new Color(0.90f, 0.60f, 0.19f, 0.98f); // NEW
    }

    private void CreateEdgeUi(RectTransform parent, Vector2 from, Vector2 to, string pathType)
    {
        GameObject go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        Vector2 dir = to - from;
        float len = dir.magnitude;
        if (len < 1f)
        {
            Destroy(go);
            return;
        }

        rt.anchoredPosition = from;
        rt.sizeDelta = new Vector2(len, EdgeThickness);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image img = go.GetComponent<Image>();
        img.color = ResolveEdgeColor(pathType);
        img.raycastTarget = false;
    }

    private static float ResolveNodeIconSize(StoryProgressNodeEntry node)
    {
        if (node == null) return NodeSize;
        if (node.isTutorial) return TutorialNodeSize;
        if (node.isBoss) return BossNodeSize;
        return NodeSize;
    }

    private Vector2 NodeToAnchored(StoryProgressNodeEntry node)
    {
        RectTransform mapRt = mapGraphicRt != null ? mapGraphicRt : mapContentRt;
        Rect r = mapRt != null ? mapRt.rect : new Rect(0f, 0f, mapBaseSize.x, mapBaseSize.y);
        float x = Mathf.Clamp01(node.x) * r.width;
        float y = Mathf.Clamp01(node.y) * r.height;
        return new Vector2(x - r.width * 0.5f, y - r.height * 0.5f);
    }

    private NodeState ResolveNodeState(StoryProgressNodeEntry node)
    {
        if (node == null) return NodeState.Locked;
        if (string.Equals(node.nodeId, SideQuestA1ProgressState.NodeId, StringComparison.OrdinalIgnoreCase))
        {
            int slot = PlayerData.GetActivePlayerSlotOrDefault();
            if (SideQuestA1ProgressState.IsNodeCleared(slot)) return NodeState.Cleared;
            if (!SideQuestA1ProgressState.IsNodeAvailable(slot)) return NodeState.Locked;
            return NodeState.Available;
        }

        if (clearedNodeIds.Contains(node.nodeId)) return NodeState.Cleared;

        bool allReady = true;
        if (node.unlockRequiresAllOf != null)
        {
            for (int i = 0; i < node.unlockRequiresAllOf.Length; i++)
            {
                string req = node.unlockRequiresAllOf[i];
                if (string.IsNullOrWhiteSpace(req)) continue;
                if (!clearedNodeIds.Contains(req))
                {
                    allReady = false;
                    break;
                }
            }
        }

        if (!allReady) return NodeState.Locked;

        bool anyReady = node.unlockRequiresAnyOf == null || node.unlockRequiresAnyOf.Length == 0;
        if (!anyReady)
        {
            for (int i = 0; i < node.unlockRequiresAnyOf.Length; i++)
            {
                string req = node.unlockRequiresAnyOf[i];
                if (string.IsNullOrWhiteSpace(req)) continue;
                if (clearedNodeIds.Contains(req))
                {
                    anyReady = true;
                    break;
                }
            }
        }

        return anyReady ? NodeState.Available : NodeState.Locked;
    }

    private static Color ResolveNodeColor(NodeState state, StoryProgressNodeEntry node)
    {
        switch (state)
        {
            case NodeState.Cleared:
                return new Color(0.38f, 0.78f, 0.48f, 0.95f);
            case NodeState.Available:
                if (node != null && node.isBoss)
                    return new Color(0.94f, 0.42f, 0.46f, 0.98f);
                if (node != null && node.isTutorial)
                    return new Color(0.50f, 0.80f, 0.96f, 0.98f);
                return new Color(0.95f, 0.82f, 0.48f, 0.98f);
            default:
                return new Color(0.45f, 0.42f, 0.40f, 0.88f);
        }
    }

    private static Color ResolveEdgeColor(string pathType)
    {
        if (string.Equals(pathType, "main", StringComparison.OrdinalIgnoreCase))
            return new Color(0.95f, 0.86f, 0.62f, 0.55f);
        if (string.Equals(pathType, "side_return", StringComparison.OrdinalIgnoreCase))
            return new Color(0.63f, 0.80f, 0.95f, 0.52f);
        return new Color(0.82f, 0.76f, 0.67f, 0.42f);
    }

    private void OnNodeClicked(StoryProgressNodeEntry node)
    {
        if (node == null) return;
        NodeState state = ResolveNodeState(node);
        if (string.Equals(node.nodeId, TutorialRootNodeId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(node.nodeId, SeawallPatrolNodeId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(node.nodeId, RiverForkNodeId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(node.nodeId, SideQuestA1ProgressState.NodeId, StringComparison.OrdinalIgnoreCase))
        {
            if (state == NodeState.Locked)
            {
                GameDevLog.Log("Story map node locked: " + node.nodeId);
                return;
            }

            SetSelectedStageNodeId(node.nodeId);
            preferredFocusNodeId = node.nodeId;
            StoryProgressSceneController.RequestRefreshPresentation();
            if (mapFocusMode)
                ExitMapFocusMode();
            return;
        }
        string stateText = state == NodeState.Cleared ? "已通關" : (state == NodeState.Available ? "可挑戰" : "未解鎖");
        GameDevLog.Log("Story map node selected: " + node.nodeId + " " + node.title + " (" + stateText + ")");
    }

    private void TryToggleMapFocusMode()
    {
        float now = Time.unscaledTime;
        if (now - lastFocusToggleUnscaledTime < FocusToggleDebounceSeconds)
            return;
        lastFocusToggleUnscaledTime = now;
        ToggleMapFocusMode();
    }

    private void ToggleMapFocusMode()
    {
        if (viewportParent == null || viewportObject == null) return;
        if (!mapFocusMode)
            EnterMapFocusMode();
        else
            ExitMapFocusMode();
    }

    private void EnterMapFocusMode()
    {
        hiddenUiRecords.Clear();
        for (int i = 0; i < viewportParent.childCount; i++)
        {
            Transform child = viewportParent.GetChild(i);
            if (child == null) continue;
            GameObject go = child.gameObject;
            if (go == viewportObject) continue;

            hiddenUiRecords.Add(new UiVisibilityRecord { go = go, wasActive = go.activeSelf });
            if (go.activeSelf) go.SetActive(false);
        }

        mapFocusMode = true;
        ApplyMapFocusInteractivity();
        if (viewportRt != null) viewportRt.SetAsLastSibling();
        FocusInitialNode();
    }

    private void ExitMapFocusMode()
    {
        for (int i = 0; i < hiddenUiRecords.Count; i++)
        {
            UiVisibilityRecord record = hiddenUiRecords[i];
            if (record.go == null) continue;
            record.go.SetActive(record.wasActive);
        }
        hiddenUiRecords.Clear();
        mapFocusMode = false;
        ApplyMapFocusInteractivity();
        if (viewportRt != null && viewportSiblingIndex >= 0)
            viewportRt.SetSiblingIndex(viewportSiblingIndex);
    }

    private void ApplyMapFocusInteractivity()
    {
        if (scrollRect != null)
        {
            scrollRect.horizontal = mapFocusMode;
            scrollRect.vertical = mapFocusMode;
            scrollRect.inertia = mapFocusMode;
            scrollRect.enabled = mapFocusMode;
            scrollRect.scrollSensitivity = mapFocusMode ? 0f : 1.1f;
        }
        isManualDraggingMap = false;
    }

    private void FocusInitialNode()
    {
        if (scrollRect == null || mapContentRt == null || viewportRt == null) return;

        SetMapZoom(DefaultMapZoom);
        ReapplyMapFocusOnPreferredNode();
    }

    /// <summary>Pan viewport so the preferred node is centered (M-1-1 default; M-1-2 after harbor first clear).</summary>
    private void ReapplyMapFocusOnPreferredNode()
    {
        if (mapContentRt == null || viewportRt == null) return;

        string nodeId = ResolvePreferredFocusNodeId();
        if (!nodeRects.TryGetValue(nodeId, out RectTransform rt))
        {
            if (!nodeRects.TryGetValue(TutorialRootNodeId, out rt))
                return;
            nodeId = TutorialRootNodeId;
        }

        rt.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();

        bool newPlayerMapFraming = string.Equals(nodeId, TutorialRootNodeId, StringComparison.OrdinalIgnoreCase) &&
                                   TutorialProgressState.NeedsTutorialFlowForActivePlayer();
        CenterOnNode(rt, newPlayerMapFraming ? GetNewPlayerMapViewportAlignOffset() : Vector2.zero);

        if (StoryProgressSession.TryConsumePendingEnterMapFocusMode() && !mapFocusMode)
            EnterMapFocusMode();
    }

    private string ResolvePreferredFocusNodeId()
    {
        if (string.IsNullOrWhiteSpace(preferredFocusNodeId) &&
            StoryProgressSession.TryConsumePendingMapFocusNodeId(out string pending))
            preferredFocusNodeId = pending;

        return string.IsNullOrWhiteSpace(preferredFocusNodeId) ? TutorialRootNodeId : preferredFocusNodeId;
    }

    private void SetMapZoom(float zoom)
    {
        if (mapContentRt == null) return;
        currentZoom = Mathf.Clamp(zoom, MapCoverMinZoom, maxZoom);
        mapContentRt.localScale = new Vector3(currentZoom, currentZoom, 1f);
        ClampContentAnchoredPosition();
    }

    private Vector2 GetNewPlayerMapViewportAlignOffset()
    {
        if (viewportRt == null) return Vector2.zero;
        Rect vp = viewportRt.rect;
        return new Vector2(vp.width * (NewPlayerMapNodeViewportXFromLeft - 0.5f), 0f);
    }

    /// <summary>
    /// Pan map so <paramref name="nodeRt"/> sits at the given viewport-local offset from viewport center.
    /// Uses live screen positions so zoom scale is accounted for.
    /// </summary>
    private void CenterOnNode(RectTransform nodeRt, Vector2 viewportAlignOffsetFromCenter = default)
    {
        if (nodeRt == null || mapContentRt == null || viewportRt == null) return;

        Camera uiCam = GetUiEventCamera();
        Vector2 nodeScreen = RectTransformUtility.WorldToScreenPoint(uiCam, nodeRt.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRt, nodeScreen, uiCam, out Vector2 nodeInViewport))
            return;

        Vector2 correction = viewportAlignOffsetFromCenter - nodeInViewport;
        mapContentRt.anchoredPosition += correction;
        ClampContentAnchoredPosition();
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels32(px);
        tex.Apply(false, true);
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Deck Pack 場景：點擊牌組圖示 → 選中效果 + 浮動選單（查看／編輯）。
/// </summary>
public sealed class DeckPackSceneController : MonoBehaviour
{
    public const string SceneName = "Deck Pack";
    private const string BackpackSceneName = "Persistent";
    private const string EditSceneName = "Buildbeck";
    private const string EmptyDeckMessage = "該牌組為空";

    private static readonly Color UnselectedWine = new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);
    private static readonly Color SelectedTint = new Color(1f, 0.98f, 0.92f, 1f);
    private static readonly Color SelectedOutline = new Color(1f, 0.84f, 0.45f, 0.95f);

    private const float SelectedLiftMinPx = 200f;
    private const float SelectedLiftHeightRatio = 0.34f;
    private const float SlotMoveDuration = 0.32f;
    private const float PopupFadeDuration = 0.24f;
    private const float PopupGapBelowDeckPx = 28f;

    private static bool subscribed;

    private readonly List<DeckSlotView> slots = new List<DeckSlotView>(5);
    private Canvas rootCanvas;
    private PlayerData playerData;
    private int selectedSlotIndex = -1;
    private GameObject overlayRoot;
    private RectTransform popupPanelRt;
    private CanvasGroup popupPanelGroup;
    private TextMeshProUGUI popupTitleText;
    private TMP_FontAsset uiFont;
    private Coroutine selectionAnimRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            subscribed = true;
        }

        TryWireScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryWireScene(scene);
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (!scene.IsValid()) return;
        if (!scene.name.Equals(SceneName, StringComparison.OrdinalIgnoreCase)) return;
        DestroyStrayDeckPackOverlays();
    }

    private static void DestroyStrayDeckPackOverlays()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go == null) continue;
            if (!string.Equals(go.name, "DeckPackActionOverlay", StringComparison.Ordinal)) continue;
            Destroy(go);
        }
    }

    private static void TryWireScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.name.Equals(SceneName, StringComparison.OrdinalIgnoreCase))
            return;

        DestroyStrayDeckPackOverlays();

        DeckPackSceneController existing = FindFirstObjectByType<DeckPackSceneController>();
        if (existing != null)
        {
            existing.Initialize();
            return;
        }

        GameObject host = new GameObject(nameof(DeckPackSceneController));
        host.AddComponent<DeckPackSceneController>();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        TeardownOverlayImmediate();
    }

    private void Initialize()
    {
        rootCanvas = ResolveDeckPackCanvas();
        if (rootCanvas == null)
        {
            Debug.LogError("DeckPackSceneController: Canvas not found.");
            return;
        }

        TeardownMisplacedOverlay();

        playerData = PlayerData.ResolveCanonical();
        if (playerData != null)
            playerData.LoadPlayerData();

        uiFont = SettingsUiFonts.ResolveParameterDetailsFont();
        if (uiFont == null)
            uiFont = BuildbeckUiFonts.ResolveBuildbeckButtonFont();

        BindDeckSlots();
        RefreshAllSlotVisuals();
        HideActionPopup();
        GlobalNavRuntime.RefreshActiveSceneNav();
    }

    private void BindDeckSlots()
    {
        slots.Clear();
        for (int i = 0; i < PlayerData.MinDeckSlotCount; i++)
        {
            string objName = "My Deck " + (i + 1);
            GameObject go = GameObject.Find(objName);
            if (go == null)
            {
                Debug.LogWarning("DeckPackSceneController: missing object -> " + objName);
                continue;
            }

            Image image = go.GetComponent<Image>();
            if (image == null)
                image = go.AddComponent<Image>();
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            if (button == null)
                button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Outline outline = go.GetComponent<Outline>();
            if (outline == null)
                outline = go.AddComponent<Outline>();
            outline.effectColor = SelectedOutline;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            int slotIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnDeckSlotClicked(slotIndex));

            RectTransform rt = go.GetComponent<RectTransform>();
            Vector2 basePos = rt != null ? rt.anchoredPosition : Vector2.zero;
            slots.Add(new DeckSlotView
            {
                slotIndex = slotIndex,
                root = go,
                rect = rt,
                image = image,
                outline = outline,
                label = label,
                baseColor = image.color,
                baseScale = rt != null ? rt.localScale : Vector3.one,
                baseAnchoredPosition = basePos
            });
        }
    }

    private void OnDeckSlotClicked(int slotIndex)
    {
        if (playerData == null)
            playerData = PlayerData.ResolveCanonical();
        if (playerData == null) return;

        if (selectionAnimRoutine != null)
        {
            StopCoroutine(selectionAnimRoutine);
            selectionAnimRoutine = null;
        }

        selectedSlotIndex = slotIndex;
        RefreshAllSlotVisuals();
        selectionAnimRoutine = StartCoroutine(CoAnimateSelectionAndShowPopup(slotIndex));
    }

    private IEnumerator CoAnimateSelectionAndShowPopup(int slotIndex)
    {
        EnsureActionPopup();
        if (overlayRoot == null || popupPanelRt == null || playerData == null)
        {
            selectionAnimRoutine = null;
            yield break;
        }

        DeckSlotView selected = FindSlot(slotIndex);
        if (selected?.rect == null)
        {
            selectionAnimRoutine = null;
            yield break;
        }

        float liftPx = ComputeLiftOffsetPx(selected.rect);
        Vector2 liftedPos = selected.baseAnchoredPosition + new Vector2(0f, liftPx);

        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();

        string deckName = playerData.GetDeckSlotDisplayName(slotIndex);
        if (popupTitleText != null)
            popupTitleText.text = deckName;

        if (popupPanelGroup != null)
        {
            popupPanelGroup.alpha = 0f;
            popupPanelRt.localScale = Vector3.one * 0.94f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(popupPanelRt);
        PositionPopupBelowDeck(selected.rect, liftedPos);
        popupPanelRt.gameObject.SetActive(false);

        List<Coroutine> moves = new List<Coroutine>(slots.Count);
        for (int i = 0; i < slots.Count; i++)
        {
            DeckSlotView slot = slots[i];
            if (slot.rect == null) continue;
            Vector2 target = slot.slotIndex == slotIndex ? liftedPos : slot.baseAnchoredPosition;
            StartCoroutine(AnimateSlotAnchoredPosition(slot, target, SlotMoveDuration));
        }

        yield return new WaitForSecondsRealtime(SlotMoveDuration);

        popupPanelRt.gameObject.SetActive(true);
        PositionPopupBelowDeck(selected.rect, liftedPos);
        yield return FadePopupPanel(1f, PopupFadeDuration);

        WirePopupButtons(slotIndex);
        selectionAnimRoutine = null;
    }

    private void WirePopupButtons(int slotIndex)
    {
        if (overlayRoot == null) return;

        Button viewBtn = overlayRoot.transform.Find("PopupPanel/ViewDeckButton")?.GetComponent<Button>();
        Button editBtn = overlayRoot.transform.Find("PopupPanel/EditDeckButton")?.GetComponent<Button>();
        if (viewBtn != null)
        {
            viewBtn.onClick.RemoveAllListeners();
            viewBtn.onClick.AddListener(() => OnClickViewDeck(slotIndex));
        }

        if (editBtn != null)
        {
            editBtn.onClick.RemoveAllListeners();
            editBtn.onClick.AddListener(() => OnClickEditDeck(slotIndex));
        }
    }

    private static float ComputeLiftOffsetPx(RectTransform deckRt)
    {
        float h = deckRt != null ? deckRt.rect.height : 640f;
        return Mathf.Max(SelectedLiftMinPx, h * SelectedLiftHeightRatio);
    }

    private IEnumerator AnimateSlotAnchoredPosition(DeckSlotView slot, Vector2 target, float duration)
    {
        if (slot?.rect == null) yield break;

        Vector2 start = slot.rect.anchoredPosition;
        if ((start - target).sqrMagnitude < 0.5f)
        {
            slot.rect.anchoredPosition = target;
            yield break;
        }

        float t = 0f;
        while (t < duration && slot.rect != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            slot.rect.anchoredPosition = Vector2.Lerp(start, target, eased);
            yield return null;
        }

        if (slot.rect != null)
            slot.rect.anchoredPosition = target;
    }

    private IEnumerator FadePopupPanel(float targetAlpha, float duration)
    {
        if (popupPanelGroup == null || popupPanelRt == null) yield break;

        float startAlpha = popupPanelGroup.alpha;
        Vector3 startScale = popupPanelRt.localScale;
        Vector3 endScale = Vector3.one;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            popupPanelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            popupPanelRt.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }

        popupPanelGroup.alpha = targetAlpha;
        popupPanelRt.localScale = endScale;
    }

    private IEnumerator CoHideActionPopupAnimated()
    {
        if (selectionAnimRoutine != null)
        {
            StopCoroutine(selectionAnimRoutine);
            selectionAnimRoutine = null;
        }

        if (popupPanelGroup != null && overlayRoot != null && overlayRoot.activeSelf)
            yield return FadePopupPanel(0f, PopupFadeDuration * 0.85f);

        for (int i = 0; i < slots.Count; i++)
        {
            DeckSlotView slot = slots[i];
            if (slot.rect == null) continue;
            StartCoroutine(AnimateSlotAnchoredPosition(slot, slot.baseAnchoredPosition, SlotMoveDuration * 0.9f));
        }

        yield return new WaitForSecondsRealtime(SlotMoveDuration * 0.9f);

        selectedSlotIndex = -1;
        RefreshAllSlotVisuals();
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void RefreshAllSlotVisuals()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            DeckSlotView slot = slots[i];
            if (slot.root == null) continue;

            bool selected = slot.slotIndex == selectedSlotIndex;
            if (slot.image != null)
                slot.image.color = selected ? SelectedTint : slot.baseColor;

            if (slot.outline != null)
                slot.outline.enabled = selected;

            if (slot.rect != null)
                slot.rect.localScale = selected ? slot.baseScale * 1.06f : slot.baseScale;

            if (slot.label != null && playerData != null)
            {
                string deckName = playerData.GetDeckSlotDisplayName(slot.slotIndex);
                slot.label.text = deckName;
                slot.label.color = selected ? new Color(0.12f, 0.1f, 0.08f, 1f) : Color.white;
            }
        }
    }

    private void ShowActionPopup(int slotIndex)
    {
        OnDeckSlotClicked(slotIndex);
    }

    private void HideActionPopup()
    {
        if (!gameObject.activeInHierarchy)
        {
            ResetSlotsImmediate();
            if (overlayRoot != null)
                overlayRoot.SetActive(false);
            return;
        }

        StartCoroutine(CoHideActionPopupAnimated());
    }

    private void ResetSlotsImmediate()
    {
        selectedSlotIndex = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].rect != null)
                slots[i].rect.anchoredPosition = slots[i].baseAnchoredPosition;
        }
        RefreshAllSlotVisuals();
    }

    private void OnClickViewDeck(int slotIndex)
    {
        if (!TrySelectDeckSlot(slotIndex)) return;

        if (playerData.GetDeckSlotTotalCount(slotIndex) <= 0)
        {
            SceneToast.Show(EmptyDeckMessage);
            return;
        }

        TeardownOverlayBeforeSceneChange();
        DeckPackViewSession.BeginViewSelectedDeckInBackpack();
        LoadSceneOrLog(BackpackSceneName);
    }

    private void OnClickEditDeck(int slotIndex)
    {
        if (playerData == null)
            playerData = PlayerData.ResolveCanonical();
        if (playerData == null) return;

        playerData.SetSelectedDeckSlot(slotIndex);
        PlayerSaveCoordinator.FlushDebouncedThenSavePlayerData();
        DeckPackViewSession.BeginEditSelectedDeckInBuildbeck(slotIndex);
        selectedSlotIndex = slotIndex;
        RefreshAllSlotVisuals();
        TeardownOverlayBeforeSceneChange();
        LoadSceneOrLog(EditSceneName);
    }

    private bool TrySelectDeckSlot(int slotIndex)
    {
        if (playerData == null)
            playerData = PlayerData.ResolveCanonical();
        if (playerData == null) return false;

        playerData.SetSelectedDeckSlot(slotIndex);
        playerData.SavePlayerDataDebounced();
        selectedSlotIndex = slotIndex;
        RefreshAllSlotVisuals();
        return true;
    }

    private static void LoadSceneOrLog(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("DeckPackSceneController: scene not in Build Settings -> " + sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private DeckSlotView FindSlot(int slotIndex)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotIndex == slotIndex)
                return slots[i];
        }

        return null;
    }

    private void EnsureActionPopup()
    {
        if (overlayRoot != null)
        {
            if (rootCanvas != null && overlayRoot.transform.parent == rootCanvas.transform)
                return;

            TeardownOverlayImmediate();
        }

        if (rootCanvas == null) return;

        overlayRoot = new GameObject("DeckPackActionOverlay", typeof(RectTransform), typeof(CanvasGroup));
        overlayRoot.transform.SetParent(rootCanvas.transform, false);
        RectTransform overlayRt = overlayRoot.GetComponent<RectTransform>();
        StretchFull(overlayRt);

        CanvasGroup group = overlayRoot.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(overlayRoot.transform, false);
        RectTransform dimRt = dim.GetComponent<RectTransform>();
        StretchFull(dimRt);
        Image dimImg = dim.GetComponent<Image>();
        dimImg.color = new Color(0.04f, 0.02f, 0.03f, 0.55f);
        dimImg.raycastTarget = true;
        Button dimBtn = dim.GetComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(HideActionPopup);

        GameObject panel = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup));
        panel.transform.SetParent(overlayRoot.transform, false);
        popupPanelRt = panel.GetComponent<RectTransform>();
        popupPanelRt.anchorMin = new Vector2(0.5f, 0.5f);
        popupPanelRt.anchorMax = new Vector2(0.5f, 0.5f);
        popupPanelRt.sizeDelta = new Vector2(420f, 0f);
        popupPanelRt.pivot = new Vector2(0.5f, 1f);
        popupPanelGroup = panel.GetComponent<CanvasGroup>();
        popupPanelGroup.alpha = 0f;
        popupPanelGroup.blocksRaycasts = true;

        Image panelBg = panel.GetComponent<Image>();
        panelBg.sprite = GetWhiteSprite();
        panelBg.type = Image.Type.Sliced;
        panelBg.color = BattleUiColors.PanelCream96;

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 24, 24);
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = panel.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        popupTitleText = CreatePopupLabel(panel.transform, "DeckPackPopupTitle", 30f, FontStyles.Bold, BattleUiColors.Ink);
        popupTitleText.alignment = TextAlignmentOptions.Center;
        popupTitleText.text = "牌組";

        CreatePopupHint(panel.transform, "要查看牌組內容，還是編輯牌組？");

        CreatePopupActionButton(panel.transform, "ViewDeckButton", "查看牌組", primary: true);
        CreatePopupActionButton(panel.transform, "EditDeckButton", "編輯牌組", primary: false);
    }

    private void PositionPopupBelowDeck(RectTransform deckRt, Vector2 deckAnchoredPosition)
    {
        if (popupPanelRt == null || rootCanvas == null || deckRt == null) return;

        RectTransform canvasRt = rootCanvas.transform as RectTransform;
        if (canvasRt == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(popupPanelRt);
        float panelW = popupPanelRt.sizeDelta.x;
        float panelH = Mathf.Max(popupPanelRt.rect.height, 220f);

        float deckHalfH = deckRt.rect.height * 0.5f;
        float x = Mathf.Clamp(
            deckAnchoredPosition.x,
            -canvasRt.rect.width * 0.5f + panelW * 0.5f + 20f,
            canvasRt.rect.width * 0.5f - panelW * 0.5f - 20f);

        float deckBottomY = deckAnchoredPosition.y - deckHalfH;
        float y = deckBottomY - PopupGapBelowDeckPx;
        float minBottom = -canvasRt.rect.height * 0.5f + 24f;
        if (y - panelH < minBottom)
            y = minBottom + panelH;

        popupPanelRt.anchoredPosition = new Vector2(x, y);
    }

    private static Canvas ResolveDeckPackCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.name.Equals(SceneName, StringComparison.OrdinalIgnoreCase))
            return null;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        int bestOrder = int.MinValue;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            if (!c.gameObject.scene.IsValid()) continue;
            if (c.gameObject.scene != activeScene) continue;
            if (string.Equals(c.gameObject.name, "GlobalNavCanvas", StringComparison.Ordinal)) continue;
            if (c.sortingOrder < bestOrder) continue;
            best = c;
            bestOrder = c.sortingOrder;
        }

        return best;
    }

    private void TeardownMisplacedOverlay()
    {
        if (overlayRoot == null || rootCanvas == null) return;
        if (overlayRoot.transform.parent == rootCanvas.transform) return;
        TeardownOverlayImmediate();
    }

    private void TeardownOverlayBeforeSceneChange()
    {
        if (selectionAnimRoutine != null)
        {
            StopCoroutine(selectionAnimRoutine);
            selectionAnimRoutine = null;
        }

        selectedSlotIndex = -1;
        ResetSlotsImmediate();
        TeardownOverlayImmediate();
    }

    private void TeardownOverlayImmediate()
    {
        if (overlayRoot == null) return;

        Destroy(overlayRoot);
        overlayRoot = null;
        popupPanelRt = null;
        popupPanelGroup = null;
        popupTitleText = null;
    }

    private TextMeshProUGUI CreatePopupLabel(Transform parent, string name, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = fontSize + 12f;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) tmp.font = uiFont;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void CreatePopupHint(Transform parent, string text)
    {
        TextMeshProUGUI tmp = CreatePopupLabel(parent, "Hint", 24f, FontStyles.Normal, BattleUiColors.InkSoft);
        tmp.text = text;
    }

    private void CreatePopupActionButton(Transform parent, string name, string label, bool primary)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = 64f;
        le.preferredHeight = 64f;

        Button btn = go.GetComponent<Button>();
        if (primary)
            BattleUiColors.ApplyButtonStyle(btn, "EndTurnButton");
        else
            BattleUiColors.ApplyHallWineButton(btn);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        StretchFull(textRt);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (uiFont != null) tmp.font = uiFont;
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = BattleUiColors.BtnPrimaryText;
        tmp.raycastTarget = false;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite cachedWhiteSprite;

    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;
        Texture2D tex = Texture2D.whiteTexture;
        cachedWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return cachedWhiteSprite;
    }

    private sealed class DeckSlotView
    {
        public int slotIndex;
        public GameObject root;
        public RectTransform rect;
        public Image image;
        public Outline outline;
        public TMP_Text label;
        public Color baseColor;
        public Vector3 baseScale;
        public Vector2 baseAnchoredPosition;
    }
}

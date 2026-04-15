using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckManager : MonoBehaviour
{
    public int maxDeckCards = 30;
    public TextMeshProUGUI deckHintText;
    public Text deckHintLegacyText;
    public Image deckHintPanel;
    private Coroutine deckHintRoutine;
    private Font hintDynamicFont;
    private TMP_FontAsset hintTMPFont;
    private GameObject resetConfirmPanel;

    public Transform deckPanel;
    public Transform libraryPanel;
    [Header("Optional Scene Deck Slot Buttons")]
    public Button deckSlotButton1;
    public Button deckSlotButton2;
    public Button deckSlotButton3;

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

    private Dictionary<int, GameObject> libraryDic = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> deckDic = new Dictionary<int, GameObject>();
    private readonly List<Button> deckSlotButtons = new List<Button>();
    private bool externalSlotButtonsBound;

    private ScrollRect _libraryPanelScrollRect;
    private ScrollRect _deckPanelScrollRect;
    private float _libraryScrollLastNormY = -1f;
    private float _deckScrollLastNormY = -1f;
    private Coroutine _libraryScrollEdgePulseCo;
    private Coroutine _deckScrollEdgePulseCo;
    private int _libraryScrollPulseGeneration;
    private int _deckScrollPulseGeneration;
    private float _scrollEdgeFeelCooldownUntilUnscaled;

    private const float DeckScrollElasticity = 0.12f;
    private const float DeckScrollDecelerationRate = 0.22f;
    private const float DeckScrollWheelSensitivity = 26f;
    private const float DeckScrollEdgePulseScale = 1.008f;
    private const float DeckScrollEdgeFeelCooldown = 0.2f;

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

    private void TryResolveLibraryPanelByName()
    {
        if (libraryPanel != null) return;
        GameObject lib = GameObject.Find("Library Grid");
        if (lib != null)
        {
            libraryPanel = lib.transform;
            return;
        }
        lib = GameObject.Find("Library");
        if (lib != null)
            libraryPanel = lib.transform;
    }

    private void TryResolveLibraryPanelUnderCanvas()
    {
        if (libraryPanel != null) return;
        Canvas canvas = GetCachedRuntimeUiCanvas();
        if (canvas == null) return;
        Transform t = FindDeepChildByName(canvas.transform, "Library Grid");
        if (t != null)
        {
            libraryPanel = t;
            return;
        }
        t = FindDeepChildByName(canvas.transform, "Library");
        if (t != null)
            libraryPanel = t;
    }

    private void TryResolveDeckPanelByName()
    {
        if (deckPanel != null) return;
        GameObject dk = GameObject.Find("Deck Grid");
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
    }

    private void OnDestroy()
    {
        UnwireDeckScrollEdgeFeel(_libraryPanelScrollRect, OnLibraryPanelScrollFeel);
        UnwireDeckScrollEdgeFeel(_deckPanelScrollRect, OnDeckPanelScrollFeel);
        if (_libraryScrollEdgePulseCo != null) StopCoroutine(_libraryScrollEdgePulseCo);
        if (_deckScrollEdgePulseCo != null) StopCoroutine(_deckScrollEdgePulseCo);
        _libraryScrollEdgePulseCo = null;
        _deckScrollEdgePulseCo = null;
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

        EnsureDeckUIRefs();

        yield return null; // �� PlayerData Awake/Start �]��

        EnsureTMPHintReady();
        defaultDeckCardPrefab = deckCardPrefab;
        defaultLibraryCardPrefab = librarycardPrefab;
        CaptureRuntimeTemplatesIfNeeded();
        EnsureDeckUIRefs();

        AttachWheelScroll(libraryPanel);
        AttachWheelScroll(deckPanel);
        BindExternalSlotButtonsIfNeeded();
        RefreshDeckSlotTabVisual();

        ClearPanels();
        UpdateLibrary();
        if (showDeck) UpdateDeck();
        RefreshScrollablePanels();
        ForcePanelsScrollToTop();
        
    }


    // Update is called once per frame
    void Update()
    {
        BindExternalSlotButtonsIfNeeded();
        if (backpackInspectRoot != null && backpackInspectRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            HideBackpackCardInspect();
        TickBackpackInspectSwipeInput();
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

        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader != null) loader.RefreshEnterBattleState();
    }

    public void SelectDeckSlot0() { SelectDeckSlot(0); }
    public void SelectDeckSlot1() { SelectDeckSlot(1); }
    public void SelectDeckSlot2() { SelectDeckSlot(2); }

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
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader != null) loader.RefreshEnterBattleState();
    }

    public void ClearPanels()
    {
        if (libraryPanel != null)
            foreach (Transform t in libraryPanel) Destroy(t.gameObject);

        if (deckPanel != null)
            foreach (Transform t in deckPanel) Destroy(t.gameObject);

        libraryDic.Clear();
        deckDic.Clear();
    }

    public void UpdateLibrary()
    {
        EnsureCoreRefs();
        EnsureDeckUIRefs();
        if (PlayerData == null) return;
        foreach (var kv in PlayerData.playerCollection)
        {
            if (kv.Value > 0)
                CreateCard(kv.Key, CardState.Library);
        }
    }

    public void UpdateDeck()
    {
        EnsureCoreRefs();
        EnsureDeckUIRefs();
        if (PlayerData == null) return;
        foreach (var kv in PlayerData.GetDeckMap(PlayerData.selectedDeckSlot))
        {
            if (kv.Value > 0)
                CreateCard(kv.Key, CardState.Deck);
        }
    }

    public void UpdataCard(CardState state, int id)
    {
        if (state == CardState.Deck)
        {
            if (!deckDic.ContainsKey(id))
            {
                return;
            }

            PlayerData.AddSelectedDeckCount(id, -1);
            PlayerData.AddCollection(id, 1);

            if (PlayerData.GetSelectedDeckCount(id) <= 0)
            {
                GameObject removingObj = deckDic[id];
                deckDic.Remove(id);
                StartCoroutine(AnimateDeckCardRemove(removingObj));
            }
            else
            {
                deckDic[id].GetComponent<CardCounter>().SetCounter(PlayerData.GetSelectedDeckCount(id));
            }

            if (libraryDic.ContainsKey(id))
                libraryDic[id].GetComponent<CardCounter>().SetCounter(PlayerData.GetCollectionCount(id));
            else
                CreateCard(id, CardState.Library);
        }
        else if (state == CardState.Library)
        {
            if (!libraryDic.ContainsKey(id))
            {
                return;
            }

            if (PlayerData.GetSelectedDeckTotalCount() >= maxDeckCards)
            {
                ShowDeckHint("\u724C\u7D44\u4E0A\u9650\u70BA30\u5F35\u724C");
                return;
            }

            PlayerData.AddSelectedDeckCount(id, 1);
            PlayerData.AddCollection(id, -1);

            if (deckDic.ContainsKey(id))
                deckDic[id].GetComponent<CardCounter>().SetCounter(PlayerData.GetSelectedDeckCount(id));
            else
                CreateCard(id, CardState.Deck);

            if (PlayerData.GetCollectionCount(id) <= 0)
            {
                Destroy(libraryDic[id]);
                libraryDic.Remove(id);
            }
            else
            {
                libraryDic[id].GetComponent<CardCounter>().SetCounter(PlayerData.GetCollectionCount(id));
            }
        }
        PlayerData.SavePlayerData();
        RefreshScrollablePanels();
        ForceRebuildPanelsLayout();
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
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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

        TextMeshProUGUI anyTMP = Object.FindFirstObjectByType<TextMeshProUGUI>();
        if (anyTMP != null && anyTMP.font != null) return anyTMP.font;
        return TMP_Settings.defaultFontAsset;
    }

    private void EnsureLegacyHintTextReady()
    {
        if (deckHintLegacyText == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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
            // Guard against duplicate UI card generation on re-entry/rebuild.
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

        var display = newCard.GetComponentInChildren<CardDisplay>();
        if (display != null)
        {
            Card c = CardStore.GetCardById(id);
            if (c != null) display.SetCard(c);
        }

        targetDic[id] = newCard;
    }

    private void EnsureCoreRefs()
    {
        if (DataManager == null)
        {
            GameObject dm = GameObject.Find("DataManager");
            if (dm != null) DataManager = dm;
        }

        if (PlayerData == null && DataManager != null)
            PlayerData = DataManager.GetComponent<PlayerData>();
        if (CardStore == null && DataManager != null)
            CardStore = DataManager.GetComponent<CardStore>();

        if (PlayerData == null)
            PlayerData = Object.FindFirstObjectByType<PlayerData>();
        if (CardStore == null)
            CardStore = Object.FindFirstObjectByType<CardStore>();

        if (DataManager == null && PlayerData != null)
            DataManager = PlayerData.gameObject;
    }

    private void EnsureDeckUIRefs()
    {
        if (libraryPanel == null)
        {
            TryResolveLibraryPanelByName();
            TryResolveLibraryPanelUnderCanvas();
        }

        if (deckPanel == null)
        {
            TryResolveDeckPanelByName();
            TryResolveDeckPanelUnderCanvas();
        }

        // Try to recover missing prefabs from scene templates before fallback chain.
        if (librarycardPrefab == null)
        {
            GameObject fromLib = FindCardTemplateInPanel(libraryPanel);
            if (fromLib != null) librarycardPrefab = fromLib;
        }
        if (deckCardPrefab == null)
        {
            GameObject fromDeck = FindCardTemplateInPanel(deckPanel);
            if (fromDeck != null) deckCardPrefab = fromDeck;
        }

        // Keep prefab refs stable; never overwrite with runtime scene cards.
        if (deckCardPrefab == null && defaultDeckCardPrefab != null) deckCardPrefab = defaultDeckCardPrefab;
        if (librarycardPrefab == null && defaultLibraryCardPrefab != null) librarycardPrefab = defaultLibraryCardPrefab;
        if (librarycardPrefab == null && runtimeLibraryTemplate != null) librarycardPrefab = runtimeLibraryTemplate;
        if (deckCardPrefab == null && runtimeDeckTemplate != null) deckCardPrefab = runtimeDeckTemplate;
        if (librarycardPrefab == null) librarycardPrefab = deckCardPrefab;
        if (deckCardPrefab == null) deckCardPrefab = librarycardPrefab;
    }

    private GameObject FindCardTemplateInPanel(Transform panel)
    {
        if (panel == null) return null;
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child == null) continue;
            if (child.GetComponentInChildren<CardDisplay>(true) != null)
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
            bool active = i == PlayerData.selectedDeckSlot;
            Image bg = btn.GetComponent<Image>();
            if (bg != null)
            {
                // Match Persistent scene "商店" button tone.
                bg.color = active ? new Color(1f, 0.86939186f, 0.5226415f, 1f) : new Color(0.78f, 0.78f, 0.78f, 0.98f);
            }

            Text label = btn.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = active ? Color.white : Color.black;
            }
        }
    }

    private void BindExternalSlotButtonsIfNeeded()
    {
        if (externalSlotButtonsBound) return;

        bool hasAny = deckSlotButton1 != null || deckSlotButton2 != null || deckSlotButton3 != null;
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

        externalSlotButtonsBound = true;
        RefreshDeckSlotTabVisual();
    }

    private void SetSlotButtonLabel(Button button, int index)
    {
        if (button == null) return;
        Text txt = button.GetComponentInChildren<Text>(true);
        if (txt != null) txt.text = "牌組" + ToFullWidthNumber(index);
    }

    private string ToFullWidthNumber(int number)
    {
        string s = number.ToString();
        char[] chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
            {
                chars[i] = (char)('０' + (chars[i] - '0'));
            }
        }
        return new string(chars);
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
        ScrollRect sr = panel.GetComponent<ScrollRect>();
        if (sr == null) sr = panel.GetComponentInParent<ScrollRect>();
        RectTransform viewport = null;
        RectTransform panelRect = panel as RectTransform;
        if (panelRect != null)
        {
            // Normalize to top-anchored content model to keep scroll range correct.
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            Vector2 pos = panelRect.anchoredPosition;
            pos.y = 0f;
            panelRect.anchoredPosition = pos;
        }
        if (sr == null)
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
            if (sr.content == null && panelRect != null) sr.content = panelRect;
            if (sr.viewport == null)
            {
                RectTransform candidate = panel.parent as RectTransform;
                if (candidate != null) sr.viewport = candidate;
            }
            sr.horizontal = false;
            sr.vertical = true;
            if (ReferenceEquals(panel, libraryPanel))
                _libraryPanelScrollRect = sr;
            else if (ReferenceEquals(panel, deckPanel))
                _deckPanelScrollRect = sr;
            ApplyDeckPanelScrollFeel(sr, panel);
            viewport = sr.viewport != null ? sr.viewport : panelRect;
            EnsureVerticalScrollbar(sr);
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

    }

    private void ApplyDeckPanelScrollFeel(ScrollRect sr, Transform panel)
    {
        if (sr == null) return;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = DeckScrollElasticity;
        sr.inertia = true;
        sr.decelerationRate = DeckScrollDecelerationRate;
        sr.scrollSensitivity = DeckScrollWheelSensitivity;

        if (ReferenceEquals(panel, libraryPanel))
        {
            sr.onValueChanged.RemoveListener(OnLibraryPanelScrollFeel);
            sr.onValueChanged.AddListener(OnLibraryPanelScrollFeel);
        }
        else if (ReferenceEquals(panel, deckPanel))
        {
            sr.onValueChanged.RemoveListener(OnDeckPanelScrollFeel);
            sr.onValueChanged.AddListener(OnDeckPanelScrollFeel);
        }
    }

    private void OnLibraryPanelScrollFeel(Vector2 normalized) =>
        HandleScrollEdgeFeel(true, _libraryPanelScrollRect, ref _libraryScrollLastNormY, ref _libraryScrollEdgePulseCo, ref _libraryScrollPulseGeneration, normalized);

    private void OnDeckPanelScrollFeel(Vector2 normalized) =>
        HandleScrollEdgeFeel(false, _deckPanelScrollRect, ref _deckScrollLastNormY, ref _deckScrollEdgePulseCo, ref _deckScrollPulseGeneration, normalized);

    private void HandleScrollEdgeFeel(
        bool isLibrary,
        ScrollRect sr,
        ref float lastNormY,
        ref Coroutine pulseCo,
        ref int pulseGeneration,
        Vector2 normalized)
    {
        if (sr == null) return;
        float y = normalized.y;
        if (lastNormY >= 0f)
        {
            const float edgeEps = 0.004f;
            const float inner = 0.035f;
            bool hitTop = lastNormY < 1f - inner && y >= 1f - edgeEps;
            bool hitBottom = lastNormY > inner && y <= edgeEps;
            if (hitTop || hitBottom)
            {
                if (Time.unscaledTime >= _scrollEdgeFeelCooldownUntilUnscaled)
                {
                    _scrollEdgeFeelCooldownUntilUnscaled = Time.unscaledTime + DeckScrollEdgeFeelCooldown;
                    RectTransform vp = sr.viewport;
                    if (vp != null)
                    {
                        if (pulseCo != null) StopCoroutine(pulseCo);
                        pulseGeneration++;
                        int gen = pulseGeneration;
                        pulseCo = StartCoroutine(CoViewportScrollEdgePulse(vp, gen, isLibrary));
                    }
                }
            }
        }
        lastNormY = y;
    }

    private IEnumerator CoViewportScrollEdgePulse(RectTransform viewport, int generation, bool isLibrary)
    {
        try
        {
            if (viewport == null) yield break;
            Vector3 s0 = viewport.localScale;
            Vector3 peak = s0 * DeckScrollEdgePulseScale;
            float t = 0f;
            const float dur = 0.085f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float eased = Mathf.Sin(u * Mathf.PI * 0.5f);
                viewport.localScale = Vector3.Lerp(s0, peak, eased);
                yield return null;
            }
            t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float eased = Mathf.Sin(u * Mathf.PI * 0.5f);
                viewport.localScale = Vector3.Lerp(peak, s0, eased);
                yield return null;
            }
            viewport.localScale = s0;
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
        barBg.color = new Color(0f, 0f, 0f, 0.35f);

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(barObj.transform, false);
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.75f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.offsetMin = new Vector2(2f, 2f);
        handleRect.offsetMax = new Vector2(-2f, -2f);

        Image handleImage = handleObj.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.9f);

        Scrollbar sb = barObj.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.targetGraphic = handleImage;
        sb.handleRect = handleRect;
        sb.value = 1f;
        sb.size = 0.2f;

        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        sr.verticalScrollbarSpacing = 0f;
    }

    private void RefreshScrollablePanels()
    {
        RefreshPanelContentHeight(libraryPanel);
        RefreshPanelContentHeight(deckPanel);
    }

    private void ForceRebuildPanelsLayout()
    {
        RectTransform libRt = libraryPanel as RectTransform;
        if (libRt != null)
        {
            NormalizeChildrenVisualState(libRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(libRt);
        }
        RectTransform deckRt = deckPanel as RectTransform;
        if (deckRt != null)
        {
            NormalizeChildrenVisualState(deckRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(deckRt);
        }
        Canvas.ForceUpdateCanvases();
    }

    private void NormalizeChildrenVisualState(RectTransform panelRt)
    {
        for (int i = 0; i < panelRt.childCount; i++)
        {
            RectTransform c = panelRt.GetChild(i) as RectTransform;
            if (c == null) continue;
            c.localScale = Vector3.one;
            c.localRotation = Quaternion.identity;
            CanvasGroup cg = c.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    private void ForcePanelsScrollToTop()
    {
        ForcePanelScrollToTop(libraryPanel);
        ForcePanelScrollToTop(deckPanel);
    }

    private void ForcePanelScrollToTop(Transform panel)
    {
        if (panel == null) return;
        ScrollRect sr = panel.GetComponent<ScrollRect>();
        if (sr == null) sr = panel.GetComponentInParent<ScrollRect>();
        if (sr == null) return;
        Canvas.ForceUpdateCanvases();
        sr.StopMovement();
        sr.verticalNormalizedPosition = 1f;
        if (sr.content != null)
        {
            Vector2 p = sr.content.anchoredPosition;
            p.y = 0f;
            sr.content.anchoredPosition = p;
        }
    }

    private IEnumerator AnimateDeckCardRemove(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        RectTransform rt = cardObj.GetComponent<RectTransform>();
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();

        RectTransform parent = rt != null ? rt.parent as RectTransform : null;
        GridLayoutGroup grid = parent != null ? parent.GetComponent<GridLayoutGroup>() : null;
        int removedIndex = rt != null ? rt.GetSiblingIndex() : -1;

        List<RectTransform> belowCards = new List<RectTransform>();
        if (parent != null && removedIndex >= 0)
        {
            for (int i = removedIndex + 1; i < parent.childCount; i++)
            {
                RectTransform child = parent.GetChild(i) as RectTransform;
                if (child == null) continue;
                belowCards.Add(child);
            }
        }

        if (grid != null) grid.enabled = false;

        float startAlpha = cg.alpha <= 0f ? 1f : cg.alpha;
        Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 endPos = startPos + new Vector2(-160f, 0f);
        float shiftUp = 0f;
        if (grid != null) shiftUp = grid.cellSize.y + grid.spacing.y;
        else if (rt != null) shiftUp = rt.rect.height + 20f;

        List<Vector2> belowStartPos = new List<Vector2>(belowCards.Count);
        for (int i = 0; i < belowCards.Count; i++) belowStartPos.Add(belowCards[i].anchoredPosition);

        float t = 0f;
        const float duration = 0.2f;
        while (t < duration && cardObj != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p); // smoothstep
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            }
            for (int i = 0; i < belowCards.Count; i++)
            {
                RectTransform c = belowCards[i];
                if (c == null) continue;
                Vector2 to = belowStartPos[i] + new Vector2(0f, shiftUp);
                c.anchoredPosition = Vector2.Lerp(belowStartPos[i], to, eased);
            }
            cg.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        if (cardObj != null) Destroy(cardObj);
        if (grid != null)
        {
            grid.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
        RefreshScrollablePanels();
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

    private void RefreshPanelContentHeight(Transform panel)
    {
        if (panel == null) return;
        RectTransform content = panel as RectTransform;
        if (content == null) return;

        RectTransform viewport = content.parent as RectTransform;
        float viewHeight = viewport != null ? viewport.rect.height : 0f;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            float width = Mathf.Max(1f, content.rect.width);
            float usableWidth = Mathf.Max(1f, width - grid.padding.left - grid.padding.right);
            float unitW = grid.cellSize.x + grid.spacing.x;
            int columns = Mathf.Max(1, Mathf.FloorToInt((usableWidth + grid.spacing.x) / Mathf.Max(1f, unitW)));
            int childCount = content.childCount;
            int rows = childCount <= 0
                ? 0
                : Mathf.Max(1, Mathf.CeilToInt(childCount / (float)columns));
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
        else
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
    }

    // Buildbeck "????" button hook:
    // open a confirm dialog before resetting deck.
    public void ResetDeckForRebuild()
    {
        Debug.Log("DeckManager: ResetDeckForRebuild clicked.");
        EnsureDeckUIRefs();
        if (PlayerData == null && DataManager != null) PlayerData = DataManager.GetComponent<PlayerData>();
        if (PlayerData == null) PlayerData = Object.FindFirstObjectByType<PlayerData>();
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

        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();
        if (loader != null) loader.RefreshEnterBattleState();
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
    // 浮動視窗：左側完整卡圖、右側結構化資訊（標題/副標/小標/內文）。

    private GameObject backpackInspectRoot;
    private RectTransform backpackInspectPanelRt;
    private RectTransform backpackInspectPrefabMount;
    private Image backpackInspectArtImage;
    private TextMeshProUGUI backpackInspectTitleTmp;
    private TextMeshProUGUI backpackInspectSubtitleTmp;
    private TextMeshProUGUI backpackInspectMetaTmp;
    private TextMeshProUGUI backpackInspectSwipeHintTmp;
    private TextMeshProUGUI backpackInspectPageTmp;
    private TextMeshProUGUI backpackInspectLeftArrowTmp;
    private TextMeshProUGUI backpackInspectRightArrowTmp;
    private GameObject backpackInspectCardInstance;
    private RectTransform backpackInspectImageRegionRt;
    private RectTransform backpackInspectInfoRegionRt;
    private CanvasGroup backpackInspectImageCg;
    private CanvasGroup backpackInspectInfoCg;
    private readonly List<int> backpackInspectCardIds = new List<int>();
    private int backpackInspectIndex = -1;
    private bool backpackInspectSwipeTrackingMouse;
    private Vector2 backpackInspectSwipeMouseStart;
    private int backpackInspectSwipeTouchId = -1;
    private Vector2 backpackInspectSwipeTouchStart;
    private Coroutine backpackInspectTransitionCo;
    private float backpackInspectIgnoreSwipeUntilUnscaled;

    public void ShowBackpackCardInspect(Card card)
    {
        if (card == null) return;
        EnsureCoreRefs();
        if (showDeck) return;

        EnsureBackpackInspectUi();
        if (backpackInspectRoot == null) return;

        RebuildBackpackInspectCardList();
        backpackInspectIndex = backpackInspectCardIds.IndexOf(card.id);
        if (backpackInspectIndex < 0)
        {
            backpackInspectCardIds.Add(card.id);
            backpackInspectIndex = backpackInspectCardIds.Count - 1;
        }

        backpackInspectRoot.SetActive(true);
        backpackInspectRoot.transform.SetAsLastSibling();
        if (backpackInspectPanelRt != null)
            backpackInspectPanelRt.transform.SetAsLastSibling();
        backpackInspectSwipeTrackingMouse = false;
        backpackInspectSwipeTouchId = -1;
        backpackInspectIgnoreSwipeUntilUnscaled = Time.unscaledTime + 0.2f;
        RefreshBackpackInspectByIndex(0);
    }

    public void HideBackpackCardInspect()
    {
        if (backpackInspectRoot != null)
            backpackInspectRoot.SetActive(false);
        if (backpackInspectTransitionCo != null)
        {
            StopCoroutine(backpackInspectTransitionCo);
            backpackInspectTransitionCo = null;
        }
        if (backpackInspectCardInstance != null)
        {
            Destroy(backpackInspectCardInstance);
            backpackInspectCardInstance = null;
        }
        backpackInspectCardIds.Clear();
        backpackInspectIndex = -1;
    }

    private void RebuildBackpackInspectCardList()
    {
        backpackInspectCardIds.Clear();
        if (PlayerData == null) return;

        foreach (var kv in PlayerData.playerCollection)
        {
            if (kv.Value <= 0) continue;
            if (CardStore != null && CardStore.GetCardById(kv.Key) == null) continue;
            backpackInspectCardIds.Add(kv.Key);
        }
        backpackInspectCardIds.Sort();
    }

    private void RefreshBackpackInspectByIndex(int swipeDirection)
    {
        if (backpackInspectCardIds.Count == 0 || backpackInspectIndex < 0 || backpackInspectIndex >= backpackInspectCardIds.Count)
            return;
        Card card = CardStore != null ? CardStore.GetCardById(backpackInspectCardIds[backpackInspectIndex]) : null;
        if (card == null) return;
        RenderBackpackInspectCard(card);
        RefreshBackpackSwipeGuides();
        PlayBackpackInspectTransition(swipeDirection);
    }

    private void RenderBackpackInspectCard(Card card)
    {
        if (backpackInspectCardInstance != null)
        {
            Destroy(backpackInspectCardInstance);
            backpackInspectCardInstance = null;
        }

        Sprite portrait = TryLoadInspectPortraitSprite(card.id);
        if (portrait != null && backpackInspectArtImage != null)
        {
            backpackInspectArtImage.sprite = portrait;
            backpackInspectArtImage.gameObject.SetActive(true);
            if (backpackInspectPrefabMount != null)
                backpackInspectPrefabMount.gameObject.SetActive(false);
        }
        else
        {
            if (backpackInspectArtImage != null)
            {
                backpackInspectArtImage.sprite = null;
                backpackInspectArtImage.gameObject.SetActive(false);
            }

            GameObject prefab = librarycardPrefab != null ? librarycardPrefab : deckCardPrefab;
            if (prefab != null && backpackInspectPrefabMount != null)
            {
                backpackInspectPrefabMount.gameObject.SetActive(true);
                backpackInspectCardInstance = Instantiate(prefab, backpackInspectPrefabMount);
                backpackInspectCardInstance.name = "BackpackInspectCard";
                RectTransform crt = backpackInspectCardInstance.GetComponent<RectTransform>();
                if (crt != null)
                {
                    crt.anchorMin = Vector2.zero;
                    crt.anchorMax = Vector2.one;
                    crt.pivot = new Vector2(0.5f, 0.5f);
                    crt.offsetMin = crt.offsetMax = Vector2.zero;
                    crt.localRotation = Quaternion.identity;
                    crt.localScale = Vector3.one;
                }

                foreach (ClickCard cc in backpackInspectCardInstance.GetComponentsInChildren<ClickCard>(true))
                    cc.enabled = false;
                foreach (ZoomUI zu in backpackInspectCardInstance.GetComponentsInChildren<ZoomUI>(true))
                    zu.enabled = false;
                foreach (Button b in backpackInspectCardInstance.GetComponentsInChildren<Button>(true))
                    b.interactable = false;
                foreach (TextMeshProUGUI tmp in backpackInspectCardInstance.GetComponentsInChildren<TextMeshProUGUI>(true))
                    tmp.enabled = false;
                foreach (Text leg in backpackInspectCardInstance.GetComponentsInChildren<Text>(true))
                    leg.enabled = false;

                CardDisplay cd = backpackInspectCardInstance.GetComponentInChildren<CardDisplay>(true);
                if (cd != null)
                {
                    cd.SetCard(card);
                    ConfigureInspectCardImageOnly(cd);
                }
            }
        }

        if (backpackInspectTitleTmp != null)
            backpackInspectTitleTmp.text = BuildBackpackInspectTitle(card);
        if (backpackInspectSubtitleTmp != null)
            backpackInspectSubtitleTmp.text = BuildBackpackInspectSubtitle(card);
        if (backpackInspectMetaTmp != null)
            backpackInspectMetaTmp.text = BuildBackpackInspectMetaText(card);
    }

    private void OnBackpackInspectSwipe(float dragDeltaX)
    {
        if (backpackInspectRoot == null || !backpackInspectRoot.activeSelf) return;
        if (backpackInspectCardIds.Count <= 1) return;

        const float swipeThreshold = 50f;
        if (Mathf.Abs(dragDeltaX) < swipeThreshold) return;

        int swipeDirection;
        if (dragDeltaX < 0f)
        {
            backpackInspectIndex++;
            swipeDirection = 1;
        }
        else
        {
            backpackInspectIndex--;
            swipeDirection = -1;
        }

        if (backpackInspectIndex < 0) backpackInspectIndex = backpackInspectCardIds.Count - 1;
        else if (backpackInspectIndex >= backpackInspectCardIds.Count) backpackInspectIndex = 0;

        RefreshBackpackInspectByIndex(swipeDirection);
    }

    private void RefreshBackpackSwipeGuides()
    {
        bool canSwipe = backpackInspectCardIds.Count > 1;
        if (backpackInspectLeftArrowTmp != null)
            backpackInspectLeftArrowTmp.text = canSwipe ? "<" : string.Empty;
        if (backpackInspectRightArrowTmp != null)
            backpackInspectRightArrowTmp.text = canSwipe ? ">" : string.Empty;
        if (backpackInspectSwipeHintTmp != null)
            backpackInspectSwipeHintTmp.text = canSwipe ? "左右滑動切換卡牌" : "僅此一張卡牌";
        if (backpackInspectPageTmp != null)
        {
            if (backpackInspectCardIds.Count <= 0 || backpackInspectIndex < 0)
                backpackInspectPageTmp.text = string.Empty;
            else
                backpackInspectPageTmp.text = $"第 {backpackInspectIndex + 1} / {backpackInspectCardIds.Count} 張";
        }
    }

    private void PlayBackpackInspectTransition(int swipeDirection)
    {
        if (backpackInspectImageRegionRt == null || backpackInspectInfoRegionRt == null) return;
        if (backpackInspectTransitionCo != null) StopCoroutine(backpackInspectTransitionCo);
        backpackInspectTransitionCo = StartCoroutine(CoBackpackInspectTransition(swipeDirection));
    }

    private IEnumerator CoBackpackInspectTransition(int swipeDirection)
    {
        Vector2 imageBase = new Vector2(6f, 0f);
        Vector2 infoBase = new Vector2(0f, 0f);
        float direction = swipeDirection == 0 ? 0f : (swipeDirection > 0 ? 1f : -1f);
        float startOffset = 26f * direction;

        if (backpackInspectImageCg != null) backpackInspectImageCg.alpha = 0f;
        if (backpackInspectInfoCg != null) backpackInspectInfoCg.alpha = 0f;
        backpackInspectImageRegionRt.anchoredPosition = imageBase + new Vector2(startOffset, 0f);
        backpackInspectInfoRegionRt.anchoredPosition = infoBase + new Vector2(startOffset, 0f);

        const float duration = 0.18f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            float x = Mathf.Lerp(startOffset, 0f, eased);

            backpackInspectImageRegionRt.anchoredPosition = imageBase + new Vector2(x, 0f);
            backpackInspectInfoRegionRt.anchoredPosition = infoBase + new Vector2(x, 0f);
            if (backpackInspectImageCg != null) backpackInspectImageCg.alpha = eased;
            if (backpackInspectInfoCg != null) backpackInspectInfoCg.alpha = eased;

            yield return null;
        }

        backpackInspectImageRegionRt.anchoredPosition = imageBase;
        backpackInspectInfoRegionRt.anchoredPosition = infoBase;
        if (backpackInspectImageCg != null) backpackInspectImageCg.alpha = 1f;
        if (backpackInspectInfoCg != null) backpackInspectInfoCg.alpha = 1f;
        backpackInspectTransitionCo = null;
    }

    private void TickBackpackInspectSwipeInput()
    {
        if (backpackInspectRoot == null || !backpackInspectRoot.activeSelf) return;
        if (backpackInspectCardIds.Count <= 1) return;
        if (Time.unscaledTime < backpackInspectIgnoreSwipeUntilUnscaled) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 downPos = Input.mousePosition;
            backpackInspectSwipeTrackingMouse = IsScreenPointInsideInspectPanel(downPos);
            backpackInspectSwipeMouseStart = downPos;
        }
        if (backpackInspectSwipeTrackingMouse && Input.GetMouseButtonUp(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - backpackInspectSwipeMouseStart;
            backpackInspectSwipeTrackingMouse = false;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                OnBackpackInspectSwipe(delta.x);
        }

        if (Input.touchCount <= 0)
        {
            backpackInspectSwipeTouchId = -1;
            return;
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began && backpackInspectSwipeTouchId < 0)
            {
                if (IsScreenPointInsideInspectPanel(t.position))
                {
                    backpackInspectSwipeTouchId = t.fingerId;
                    backpackInspectSwipeTouchStart = t.position;
                }
            }
            else if (t.fingerId == backpackInspectSwipeTouchId &&
                     (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            {
                Vector2 delta = t.position - backpackInspectSwipeTouchStart;
                backpackInspectSwipeTouchId = -1;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    OnBackpackInspectSwipe(delta.x);
            }
        }
    }

    private bool IsScreenPointInsideInspectPanel(Vector2 screenPoint)
    {
        if (backpackInspectPanelRt == null || !backpackInspectPanelRt.gameObject.activeInHierarchy) return false;
        Canvas canvas = backpackInspectPanelRt.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(backpackInspectPanelRt, screenPoint, cam);
    }

    private static Sprite TryLoadInspectPortraitSprite(int cardId)
    {
        string[] paths =
        {
            $"CardArt/{cardId}",
            $"Cards/{cardId}",
            $"UI/CardFaces/{cardId}",
            $"CardImages/{cardId}",
        };
        foreach (string p in paths)
        {
            Sprite s = Resources.Load<Sprite>(p);
            if (s != null) return s;
        }

        return null;
    }

    private string BuildBackpackInspectTitle(Card card)
    {
        if (card == null) return string.Empty;
        return string.IsNullOrWhiteSpace(card.cardName) ? "未命名卡牌" : card.cardName.Trim();
    }

    private string BuildBackpackInspectSubtitle(Card card)
    {
        if (card == null) return string.Empty;
        string en = card.cardNameEnglish != null ? card.cardNameEnglish.Trim() : string.Empty;
        if (string.IsNullOrEmpty(en)) en = "No English Name";
        string cardType = card is SpellCard ? "法術牌" : (card is MonsterCard ? "怪物牌" : "未分類");
        return $"Subtitle: {cardType} / {en}";
    }

    private string BuildBackpackInspectMetaText(Card card)
    {
        if (card == null) return string.Empty;

        int owned = PlayerData != null ? PlayerData.GetCollectionCount(card.id) : 0;
        int inDeck = PlayerData != null ? PlayerData.GetSelectedDeckCount(card.id) : 0;

        var sb = new StringBuilder(320);
        sb.AppendLine("<size=26><b>收藏資訊</b></size>");
        sb.AppendLine($"<size=23>已持有: <mark=#274E79CC>{owned}</mark>  目前牌組: <mark=#445B2FCC>{inDeck}</mark></size>");
        sb.AppendLine();

        sb.AppendLine("<size=26><b>戰鬥數值</b></size>");
        if (card is MonsterCard m)
        {
            sb.AppendLine($"<size=23>攻擊力: {m.attack}</size>");
            sb.AppendLine($"<size=23>生命值: {m.healthPointMax}</size>");
        }
        else if (card is SpellCard sp)
        {
            sb.AppendLine("<size=23>攻擊力: -</size>");
            sb.AppendLine("<size=23>生命值: -</size>");
            sb.AppendLine();
            sb.AppendLine("<size=26><b>效果說明</b></size>");
            sb.AppendLine("<size=23>" + (string.IsNullOrWhiteSpace(sp.effect) ? "此法術暫無效果描述。" : sp.effect.Trim()) + "</size>");
        }
        else
        {
            sb.AppendLine("<size=23>攻擊力: -</size>");
            sb.AppendLine("<size=23>生命值: -</size>");
            sb.AppendLine();
            sb.AppendLine("<size=26><b>說明</b></size>");
            sb.AppendLine("<size=23>此卡牌資料格式不完整。</size>");
        }

        return sb.ToString();
    }

    private TextMeshProUGUI CreateBackpackInspectBodyScroll(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject col = new GameObject(name, typeof(RectTransform));
        col.transform.SetParent(parent, false);
        RectTransform colRt = col.GetComponent<RectTransform>();
        colRt.anchorMin = anchorMin;
        colRt.anchorMax = anchorMax;
        colRt.offsetMin = offsetMin;
        colRt.offsetMax = offsetMax;

        GameObject scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(col.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(2f, 2f);
        scrollRt.offsetMax = new Vector2(-2f, -2f);
        ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 28f;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        Image vpImg = viewportObj.GetComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.004f);
        vpImg.raycastTarget = true;
        Mask mask = viewportObj.GetComponent<Mask>();
        mask.showMaskGraphic = false;
        sr.viewport = vpRt;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRt;

        GameObject metaObj = new GameObject("MetaText", typeof(RectTransform), typeof(TextMeshProUGUI));
        metaObj.transform.SetParent(contentObj.transform, false);
        RectTransform metaRt = metaObj.GetComponent<RectTransform>();
        metaRt.anchorMin = new Vector2(0f, 1f);
        metaRt.anchorMax = new Vector2(1f, 1f);
        metaRt.pivot = new Vector2(0.5f, 1f);
        metaRt.anchoredPosition = Vector2.zero;
        metaRt.sizeDelta = new Vector2(0f, 0f);
        metaRt.offsetMin = new Vector2(6f, 0f);
        metaRt.offsetMax = new Vector2(-6f, 0f);
        TextMeshProUGUI metaTmp = metaObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) metaTmp.font = hintTMPFont;
        metaTmp.richText = true;
        metaTmp.fontStyle = FontStyles.Normal;
        metaTmp.fontSize = 24f;
        metaTmp.lineSpacing = 10f;
        metaTmp.paragraphSpacing = 6f;
        metaTmp.alignment = TextAlignmentOptions.TopLeft;
        metaTmp.color = new Color(0.9f, 0.93f, 0.98f, 1f);
        metaTmp.enableWordWrapping = true;
        metaTmp.overflowMode = TextOverflowModes.Overflow;

        return metaTmp;
    }

    private static void ConfigureInspectCardImageOnly(CardDisplay display)
    {
        if (display == null) return;
        if (display.nameText != null) display.nameText.gameObject.SetActive(false);
        if (display.attackText != null) display.attackText.gameObject.SetActive(false);
        if (display.healthText != null) display.healthText.gameObject.SetActive(false);
        if (display.effectText != null) display.effectText.gameObject.SetActive(false);

        // Keep only the main art image; hide frame/mask/dots and other decorative layers.
        Image keepImage = display.backgroundImage;
        Image[] images = display.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null) continue;
            if (keepImage != null && ReferenceEquals(img, keepImage))
            {
                img.gameObject.SetActive(true);
                continue;
            }
            img.gameObject.SetActive(false);
        }

        RawImage[] rawImages = display.GetComponentsInChildren<RawImage>(true);
        for (int i = 0; i < rawImages.Length; i++)
        {
            RawImage raw = rawImages[i];
            if (raw == null) continue;
            raw.gameObject.SetActive(false);
        }
    }

    private void EnsureBackpackInspectUi()
    {
        if (backpackInspectRoot != null) return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        EnsureTMPHintReady();

        backpackInspectRoot = new GameObject("BackpackCardInspectRoot", typeof(RectTransform), typeof(CanvasGroup));
        backpackInspectRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = backpackInspectRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        CanvasGroup rootCg = backpackInspectRoot.GetComponent<CanvasGroup>();
        rootCg.blocksRaycasts = true;
        rootCg.interactable = true;

        GameObject dimObj = new GameObject("InspectDim", typeof(RectTransform), typeof(Image), typeof(Button));
        dimObj.transform.SetParent(backpackInspectRoot.transform, false);
        RectTransform dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
        Image dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.6f);
        dimImg.raycastTarget = true;
        Button dimBtn = dimObj.GetComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.onClick.AddListener(HideBackpackCardInspect);

        GameObject panel = new GameObject("BackpackInspectFloatingPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(backpackInspectRoot.transform, false);
        backpackInspectPanelRt = panel.GetComponent<RectTransform>();
        backpackInspectPanelRt.anchorMin = new Vector2(0.1f, 0.1f);
        backpackInspectPanelRt.anchorMax = new Vector2(0.9f, 0.9f);
        backpackInspectPanelRt.pivot = new Vector2(0.5f, 0.5f);
        backpackInspectPanelRt.offsetMin = Vector2.zero;
        backpackInspectPanelRt.offsetMax = Vector2.zero;
        backpackInspectPanelRt.anchoredPosition = Vector2.zero;
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        panelBg.raycastTarget = true;

        GameObject imageRegionObj = new GameObject("InspectImageRegion", typeof(RectTransform), typeof(Image));
        imageRegionObj.transform.SetParent(panel.transform, false);
        RectTransform imageRegionRt = imageRegionObj.GetComponent<RectTransform>();
        backpackInspectImageRegionRt = imageRegionRt;
        imageRegionRt.anchorMin = new Vector2(0f, 0f);
        imageRegionRt.anchorMax = new Vector2(0.44f, 1f);
        imageRegionRt.offsetMin = new Vector2(22f, 22f);
        imageRegionRt.offsetMax = new Vector2(-10f, -22f);
        Image imageRegionBg = imageRegionObj.GetComponent<Image>();
        imageRegionBg.color = new Color(0.12f, 0.14f, 0.19f, 0.9f);
        backpackInspectImageCg = imageRegionObj.GetComponent<CanvasGroup>();
        if (backpackInspectImageCg == null) backpackInspectImageCg = imageRegionObj.AddComponent<CanvasGroup>();
        backpackInspectImageCg.alpha = 1f;

        GameObject artObj = new GameObject("InspectPortraitImage", typeof(RectTransform), typeof(Image));
        artObj.transform.SetParent(imageRegionRt, false);
        RectTransform artRt = artObj.GetComponent<RectTransform>();
        artRt.anchorMin = Vector2.zero;
        artRt.anchorMax = Vector2.one;
        artRt.offsetMin = artRt.offsetMax = Vector2.zero;
        backpackInspectArtImage = artObj.GetComponent<Image>();
        backpackInspectArtImage.preserveAspect = true;
        backpackInspectArtImage.raycastTarget = false;
        backpackInspectArtImage.color = Color.white;
        backpackInspectArtImage.gameObject.SetActive(false);

        GameObject mountObj = new GameObject("InspectPrefabMount", typeof(RectTransform));
        mountObj.transform.SetParent(imageRegionRt, false);
        backpackInspectPrefabMount = mountObj.GetComponent<RectTransform>();
        backpackInspectPrefabMount.anchorMin = Vector2.zero;
        backpackInspectPrefabMount.anchorMax = Vector2.one;
        backpackInspectPrefabMount.offsetMin = backpackInspectPrefabMount.offsetMax = Vector2.zero;
        backpackInspectPrefabMount.gameObject.SetActive(false);

        GameObject rightRegionObj = new GameObject("InspectInfoRegion", typeof(RectTransform), typeof(Image));
        rightRegionObj.transform.SetParent(panel.transform, false);
        RectTransform rightRegionRt = rightRegionObj.GetComponent<RectTransform>();
        backpackInspectInfoRegionRt = rightRegionRt;
        rightRegionRt.anchorMin = new Vector2(0.44f, 0f);
        rightRegionRt.anchorMax = new Vector2(1f, 1f);
        rightRegionRt.offsetMin = new Vector2(12f, 22f);
        rightRegionRt.offsetMax = new Vector2(-22f, -22f);
        Image rightRegionBg = rightRegionObj.GetComponent<Image>();
        rightRegionBg.color = new Color(0.1f, 0.12f, 0.18f, 0.86f);
        rightRegionBg.raycastTarget = false;
        backpackInspectInfoCg = rightRegionObj.GetComponent<CanvasGroup>();
        if (backpackInspectInfoCg == null) backpackInspectInfoCg = rightRegionObj.AddComponent<CanvasGroup>();
        backpackInspectInfoCg.alpha = 1f;

        GameObject titleObj = new GameObject("InspectTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(rightRegionObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(18f, -86f);
        titleRt.offsetMax = new Vector2(-18f, -14f);
        backpackInspectTitleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) backpackInspectTitleTmp.font = hintTMPFont;
        backpackInspectTitleTmp.fontSize = 40f;
        backpackInspectTitleTmp.fontStyle = FontStyles.Bold;
        backpackInspectTitleTmp.alignment = TextAlignmentOptions.TopLeft;
        backpackInspectTitleTmp.color = Color.white;
        backpackInspectTitleTmp.enableWordWrapping = true;
        backpackInspectTitleTmp.text = "卡牌名稱";

        GameObject subtitleObj = new GameObject("InspectSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleObj.transform.SetParent(rightRegionObj.transform, false);
        RectTransform subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0f, 1f);
        subtitleRt.anchorMax = new Vector2(1f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.offsetMin = new Vector2(18f, -140f);
        subtitleRt.offsetMax = new Vector2(-18f, -92f);
        backpackInspectSubtitleTmp = subtitleObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) backpackInspectSubtitleTmp.font = hintTMPFont;
        backpackInspectSubtitleTmp.fontSize = 24f;
        backpackInspectSubtitleTmp.alignment = TextAlignmentOptions.TopLeft;
        backpackInspectSubtitleTmp.color = new Color(0.69f, 0.81f, 1f, 1f);
        backpackInspectSubtitleTmp.enableWordWrapping = true;
        backpackInspectSubtitleTmp.text = "Subtitle";

        backpackInspectMetaTmp = CreateBackpackInspectBodyScroll(
            rightRegionObj.transform,
            "InspectMetaBody",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(14f, 14f),
            new Vector2(-14f, -146f));

        GameObject swipeHintObj = new GameObject("InspectSwipeHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        swipeHintObj.transform.SetParent(panel.transform, false);
        RectTransform swipeHintRt = swipeHintObj.GetComponent<RectTransform>();
        swipeHintRt.anchorMin = new Vector2(0.5f, 0f);
        swipeHintRt.anchorMax = new Vector2(0.5f, 0f);
        swipeHintRt.pivot = new Vector2(0.5f, 0f);
        swipeHintRt.anchoredPosition = new Vector2(0f, 10f);
        swipeHintRt.sizeDelta = new Vector2(360f, 36f);
        backpackInspectSwipeHintTmp = swipeHintObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) backpackInspectSwipeHintTmp.font = hintTMPFont;
        backpackInspectSwipeHintTmp.fontSize = 22f;
        backpackInspectSwipeHintTmp.alignment = TextAlignmentOptions.Center;
        backpackInspectSwipeHintTmp.color = new Color(0.78f, 0.86f, 1f, 0.95f);
        backpackInspectSwipeHintTmp.text = "左右滑動切換卡牌";

        GameObject pageObj = new GameObject("InspectPageIndex", typeof(RectTransform), typeof(TextMeshProUGUI));
        pageObj.transform.SetParent(panel.transform, false);
        RectTransform pageRt = pageObj.GetComponent<RectTransform>();
        pageRt.anchorMin = new Vector2(0.5f, 0f);
        pageRt.anchorMax = new Vector2(0.5f, 0f);
        pageRt.pivot = new Vector2(0.5f, 0f);
        pageRt.anchoredPosition = new Vector2(0f, 42f);
        pageRt.sizeDelta = new Vector2(240f, 32f);
        backpackInspectPageTmp = pageObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) backpackInspectPageTmp.font = hintTMPFont;
        backpackInspectPageTmp.fontSize = 20f;
        backpackInspectPageTmp.alignment = TextAlignmentOptions.Center;
        backpackInspectPageTmp.color = new Color(0.92f, 0.96f, 1f, 0.9f);
        backpackInspectPageTmp.text = string.Empty;

        GameObject leftArrowObj = new GameObject("InspectLeftArrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        leftArrowObj.transform.SetParent(panel.transform, false);
        RectTransform leftArrowRt = leftArrowObj.GetComponent<RectTransform>();
        leftArrowRt.anchorMin = new Vector2(0f, 0.5f);
        leftArrowRt.anchorMax = new Vector2(0f, 0.5f);
        leftArrowRt.pivot = new Vector2(0f, 0.5f);
        leftArrowRt.anchoredPosition = new Vector2(10f, 0f);
        leftArrowRt.sizeDelta = new Vector2(50f, 90f);
        backpackInspectLeftArrowTmp = leftArrowObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) backpackInspectLeftArrowTmp.font = hintTMPFont;
        backpackInspectLeftArrowTmp.fontSize = 56f;
        backpackInspectLeftArrowTmp.alignment = TextAlignmentOptions.Center;
        backpackInspectLeftArrowTmp.color = new Color(0.85f, 0.9f, 1f, 0.9f);
        backpackInspectLeftArrowTmp.text = "<";

        GameObject rightArrowObj = new GameObject("InspectRightArrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        rightArrowObj.transform.SetParent(panel.transform, false);
        RectTransform rightArrowRt = rightArrowObj.GetComponent<RectTransform>();
        rightArrowRt.anchorMin = new Vector2(1f, 0.5f);
        rightArrowRt.anchorMax = new Vector2(1f, 0.5f);
        rightArrowRt.pivot = new Vector2(1f, 0.5f);
        rightArrowRt.anchoredPosition = new Vector2(-10f, 0f);
        rightArrowRt.sizeDelta = new Vector2(50f, 90f);
        backpackInspectRightArrowTmp = rightArrowObj.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) backpackInspectRightArrowTmp.font = hintTMPFont;
        backpackInspectRightArrowTmp.fontSize = 56f;
        backpackInspectRightArrowTmp.alignment = TextAlignmentOptions.Center;
        backpackInspectRightArrowTmp.color = new Color(0.85f, 0.9f, 1f, 0.9f);
        backpackInspectRightArrowTmp.text = ">";

        GameObject swipeCaptureObj = new GameObject("InspectSwipeCapture", typeof(RectTransform), typeof(Image), typeof(BackpackInspectSwipeCapture));
        swipeCaptureObj.transform.SetParent(panel.transform, false);
        RectTransform swipeCaptureRt = swipeCaptureObj.GetComponent<RectTransform>();
        swipeCaptureRt.anchorMin = Vector2.zero;
        swipeCaptureRt.anchorMax = Vector2.one;
        swipeCaptureRt.offsetMin = swipeCaptureRt.offsetMax = Vector2.zero;
        Image swipeCaptureImg = swipeCaptureObj.GetComponent<Image>();
        swipeCaptureImg.color = new Color(1f, 1f, 1f, 0.001f);
        swipeCaptureImg.raycastTarget = true;
        BackpackInspectSwipeCapture swipeCapture = swipeCaptureObj.GetComponent<BackpackInspectSwipeCapture>();
        swipeCapture.Bind(this);

        GameObject closeBtnObj = new GameObject("InspectCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(panel.transform, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-18f, -18f);
        closeRt.sizeDelta = new Vector2(48f, 48f);
        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.24f, 0.26f, 0.33f, 1f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(HideBackpackCardInspect);

        GameObject closeLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeLabel.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeLabelRt = closeLabel.GetComponent<RectTransform>();
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = closeLabelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTmp = closeLabel.GetComponent<TextMeshProUGUI>();
        if (hintTMPFont != null) closeTmp.font = hintTMPFont;
        closeTmp.fontSize = 32f;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.white;
        closeTmp.text = "X";

        closeBtnObj.transform.SetAsLastSibling();

        backpackInspectRoot.SetActive(false);
    }

    private sealed class BackpackInspectSwipeCapture : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private DeckManager host;
        private Vector2 beginPos;

        public void Bind(DeckManager owner)
        {
            host = owner;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            beginPos = eventData != null ? eventData.position : Vector2.zero;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (host == null || eventData == null) return;
            float dx = eventData.position.x - beginPos.x;
            host.OnBackpackInspectSwipe(dx);
        }
    }
}

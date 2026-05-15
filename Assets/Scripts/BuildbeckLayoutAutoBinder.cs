using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Buildbeck scene UI auto-wiring bridge for layout refactor.
/// Keeps gameplay logic in DeckManager unchanged while allowing renamed/new UI nodes.
/// </summary>
public static class BuildbeckLayoutAutoBinder
{
    private const string SceneName = "Buildbeck";
    private static DeckManager cachedDeckManager;
    private static SceneLoader cachedSceneLoader;
    private static bool sceneHooked;
    private static readonly string[] LibraryPanelNames =
    {
        "Library Grid",
        "LibraryGrid",
        "Library",
        "LeftLibraryGrid",
        "LeftLibraryPanel",
    };

    private static readonly string[] DeckPanelNames =
    {
        "Deck Grid",
        "DeckGrid",
        "Deck",
        "RightDeckGrid",
        "RightDeckPanel",
    };

    private static readonly string[] DeckSlot1Names = { "DeckSlot1", "Deck Slot 1", "牌組1", "Slot1" };
    private static readonly string[] DeckSlot2Names = { "DeckSlot2", "Deck Slot 2", "牌組2", "Slot2" };
    private static readonly string[] DeckSlot3Names = { "DeckSlot3", "Deck Slot 3", "牌組3", "Slot3" };
    private static readonly string[] DeckSlot4Names = { "DeckSlot4", "Deck Slot 4", "牌組4", "Slot4" };
    private static readonly string[] DeckSlot5Names = { "DeckSlot5", "Deck Slot 5", "牌組5", "Slot5" };

    private static readonly string[] BackButtonNames =
    {
        "return",
        "Return",
        "返回",
        "返回按鈕",
        "回首頁",
    };

    private static readonly string[] SaveDeckButtonNames =
    {
        "SaveDeckButton",
        "SaveDeck",
        "Save deck button",
        "SaveDeckButtonArt",
        "牌組保存",
        "保存牌組",
    };

    private static readonly string[] DisbandDeckButtonNames =
    {
        "Disband the deck",
        "DisbandTheDeck",
        "Disband deck",
        "ResetDeckButton",
        "ClearDeckButton",
        "解散牌組",
        "清除牌組",
        "清空牌組",
    };

    private static readonly string[] EditDeckNameButtonNames =
    {
        "EditDeckNameButton",
        "Edit deck name",
        "編輯牌組名稱",
        "RenameDeckButton",
        "DeckNameEditButton",
    };

    /// <summary>戰鬥準備完成 → 開啟 <see cref="SceneLoader.EnterBattle"/>（戰前預覽／戰鬥資訊浮窗）。</summary>
    private static readonly string[] ReadyBattleButtonNames =
    {
        "ready",
        "Ready",
        "戰鬥準備完成",
        "準備完成",
        "進入對戰",
        "BattleReadyButton",
        "GoBattle",
    };

    private static readonly string[] CurrentDeckDisplayNameObjectNames =
    {
        "CurrentDeckDisplayName",
        "DeckNameDisplay",
        "DeckDisplayNameText",
        "CurrentDeckNameText",
        "牌組名稱顯示",
        "牌組名稱",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterBuildbeckSceneHook()
    {
        if (sceneHooked) return;
        SceneManager.sceneLoaded += OnBuildbeckSceneLoaded;
        sceneHooked = true;
    }

    private static void OnBuildbeckSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;
        TryWireDeckManagerForScene(scene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void WireBuildbeckLayout()
    {
        RegisterBuildbeckSceneHook();
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        TryWireDeckManagerForScene(active);
    }

    private static void TryWireDeckManagerForScene(Scene scene)
    {
        DeckManager deckManager = cachedDeckManager;
        if (deckManager == null) deckManager = Object.FindFirstObjectByType<DeckManager>();
        cachedDeckManager = deckManager;
        if (deckManager == null)
        {
            Debug.LogWarning("BuildbeckLayoutAutoBinder: DeckManager not found, skip auto-wire.");
            return;
        }

        TryWireDeckManager(deckManager, scene);
    }

    /// <summary>
    /// After you change Buildbeck UI prefabs / hierarchy names, call this so <see cref="DeckManager"/>
    /// picks up new transforms. Clears cached panel and optional slot button refs, then re-runs the same
    /// name-based wiring as scene load (Buildbeck scene only).
    /// </summary>
    public static void InvalidateAndRewire(DeckManager deckManager, bool clearOptionalSlotButtons = true)
    {
        if (deckManager == null) return;
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        deckManager.libraryPanel = null;
        deckManager.deckPanel = null;
        if (clearOptionalSlotButtons)
        {
            deckManager.deckSlotButton1 = null;
            deckManager.deckSlotButton2 = null;
            deckManager.deckSlotButton3 = null;
            deckManager.deckSlotButton4 = null;
            deckManager.deckSlotButton5 = null;
        }

        TryWireDeckManager(deckManager, SceneManager.GetActiveScene());
    }

    private static void TryWireDeckManager(DeckManager deckManager, Scene buildbeckScene)
    {
        if (deckManager == null) return;

        if (deckManager.libraryPanel == null)
        {
            Transform libraryPanel = FindFirstTransformByNames(buildbeckScene, LibraryPanelNames);
            if (libraryPanel != null) deckManager.libraryPanel = libraryPanel;
        }

        if (deckManager.deckPanel == null)
        {
            Transform deckPanel = FindFirstTransformByNames(buildbeckScene, DeckPanelNames);
            if (deckPanel != null) deckManager.deckPanel = deckPanel;
        }

        if (deckManager.deckSlotButton1 == null) deckManager.deckSlotButton1 = FindFirstButtonByNames(buildbeckScene, DeckSlot1Names);
        if (deckManager.deckSlotButton2 == null) deckManager.deckSlotButton2 = FindFirstButtonByNames(buildbeckScene, DeckSlot2Names);
        if (deckManager.deckSlotButton3 == null) deckManager.deckSlotButton3 = FindFirstButtonByNames(buildbeckScene, DeckSlot3Names);
        if (deckManager.deckSlotButton4 == null) deckManager.deckSlotButton4 = FindFirstButtonByNames(buildbeckScene, DeckSlot4Names);
        if (deckManager.deckSlotButton5 == null) deckManager.deckSlotButton5 = FindFirstButtonByNames(buildbeckScene, DeckSlot5Names);

        TryWireBackButtonToPersistent(buildbeckScene);
        TryWireReadyBattleButton();
        TryWireSaveDeckButton(deckManager);
        TryWireDisbandDeckButton(deckManager);
        TryWireEditDeckNameButton(deckManager);
        TryBindCurrentDeckNameDisplay(deckManager);
    }

    /// <summary>返回／return → <see cref="SceneLoader.EnterPersistent"/>。含僅有 <see cref="Graphic"/> 無 <see cref="Button"/> 時自動補 <c>Button</c>。</summary>
    public static void TryWireBackButtonToPersistent(Scene? buildbeckScene = null)
    {
        Scene scene = buildbeckScene ?? SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        Button back = ResolveBackButton(scene);
        if (back == null) return;

        BringReturnButtonOntoCanvasTop(back);

        SceneLoader loader = cachedSceneLoader;
        if (loader == null) loader = Object.FindFirstObjectByType<SceneLoader>();
        cachedSceneLoader = loader;
        if (loader == null)
        {
            return;
        }

        UnityEngine.Events.UnityAction persist = loader.EnterPersistent;
        back.onClick.RemoveListener(persist);

        ReturnButtonClickFeedback fx = back.GetComponent<ReturnButtonClickFeedback>();
        if (fx == null) fx = back.gameObject.AddComponent<ReturnButtonClickFeedback>();
        fx.Configure(persist);
    }

    /// <summary>
    /// 將返回鈕掛到所屬 <see cref="Canvas"/> 根下最後一個子節點（同 Canvas 內最上層繪製）。編牌 UI 動態建立後可再呼叫。
    /// </summary>
    public static void BringBuildbeckReturnButtonToFront(Scene? buildbeckScene = null)
    {
        Scene scene = buildbeckScene ?? SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        Button back = ResolveBackButton(scene);
        BringReturnButtonOntoCanvasTop(back);
    }

    private static void BringReturnButtonOntoCanvasTop(Button back)
    {
        if (back == null) return;
        RectTransform rt = back.transform as RectTransform;
        if (rt == null) return;
        Canvas canvas = back.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Transform canvasTransform = canvas.transform;
        if (rt.parent != canvasTransform)
            rt.SetParent(canvasTransform, true);
        rt.SetAsLastSibling();
    }

    /// <summary>
    /// 對戰準備／ready 鈕 → <see cref="SceneLoader.EnterBattle"/>。場景重載或 UI 重整後應再呼叫，並會更新 <see cref="SceneLoader.enterBattleButton"/>。
    /// </summary>
    public static void TryWireReadyBattleButton()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        Button ready = FindFirstButtonByNames(active, ReadyBattleButtonNames);
        if (ready == null) return;

        SceneLoader loader = cachedSceneLoader;
        if (loader == null || loader.gameObject == null)
            loader = Object.FindFirstObjectByType<SceneLoader>();
        cachedSceneLoader = loader;
        if (loader == null) return;

        UnityEngine.Events.UnityAction enter = loader.EnterBattle;
        ready.onClick.RemoveListener(enter);
        ready.onClick.AddListener(enter);

        // 場景重載後務必指到新實例；僅在 null 時指派會留下舊引用，導致 onClick 失效或 interactable 錯位。
        loader.enterBattleButton = ready;
    }

    /// <summary>Wires scene or scaffold-created save button to <see cref="DeckManager.OnClickSaveDeckButton"/>.</summary>
    public static void TryWireSaveDeckButton(DeckManager deckManager)
    {
        if (deckManager == null) return;

        Button save = deckManager.saveDeckButton;
        if (save == null)
            save = FindFirstButtonByNames(SceneManager.GetActiveScene(), SaveDeckButtonNames);
        if (save == null) return;

        UnityEngine.Events.UnityAction handler = deckManager.OnClickSaveDeckButton;
        save.onClick.RemoveListener(handler);
        save.onClick.AddListener(handler);
    }

    /// <summary>Inspector reference or first name match in the Buildbeck scene.</summary>
    public static Button ResolveDisbandDeckButton(DeckManager deckManager)
    {
        if (deckManager == null) return null;
        if (deckManager.disbandDeckButton != null) return deckManager.disbandDeckButton;
        return FindFirstButtonByNames(SceneManager.GetActiveScene(), DisbandDeckButtonNames);
    }

    /// <summary>Wires disband/clear deck UI to <see cref="DeckManager.OnClickResetDeckButton"/> (confirm dialog then clear).</summary>
    public static void TryWireDisbandDeckButton(DeckManager deckManager)
    {
        if (deckManager == null) return;

        Button disband = ResolveDisbandDeckButton(deckManager);
        if (disband == null) return;

        UnityEngine.Events.UnityAction handler = deckManager.OnClickResetDeckButton;
        disband.onClick.RemoveListener(handler);
        disband.onClick.AddListener(handler);
        deckManager.EnsureDisbandDeckButtonDrawOrder(disband);
    }

    public static Button ResolveEditDeckNameButton(DeckManager deckManager)
    {
        if (deckManager == null) return null;
        if (deckManager.editDeckNameButton != null) return deckManager.editDeckNameButton;
        return FindFirstButtonByNames(SceneManager.GetActiveScene(), EditDeckNameButtonNames);
    }

    public static void TryWireEditDeckNameButton(DeckManager deckManager)
    {
        if (deckManager == null) return;
        Button edit = ResolveEditDeckNameButton(deckManager);
        if (edit == null) return;
        UnityEngine.Events.UnityAction handler = deckManager.OnClickEditDeckNameButton;
        edit.onClick.RemoveListener(handler);
        edit.onClick.AddListener(handler);
    }

    /// <summary>
    /// If no display reference is set, find a TMP_Text or legacy <see cref="Text"/> on a named object in the Buildbeck scene (runtime only).
    /// </summary>
    public static void TryBindCurrentDeckNameDisplay(DeckManager deckManager)
    {
        if (deckManager == null) return;
        if (deckManager.currentDeckDisplayNameText != null || deckManager.currentDeckDisplayNameLegacyText != null)
        {
            deckManager.RefreshCurrentDeckDisplayName();
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        for (int i = 0; i < CurrentDeckDisplayNameObjectNames.Length; i++)
        {
            GameObject go = SceneSearchUtil.FindSceneObject(active, CurrentDeckDisplayNameObjectNames[i]);
            if (go == null) continue;
            TMP_Text tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) tmp = go.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                deckManager.currentDeckDisplayNameText = tmp;
                deckManager.RefreshCurrentDeckDisplayName();
                return;
            }

            Text leg = go.GetComponent<Text>();
            if (leg == null) leg = go.GetComponentInChildren<Text>(true);
            if (leg != null)
            {
                deckManager.currentDeckDisplayNameLegacyText = leg;
                deckManager.RefreshCurrentDeckDisplayName();
                return;
            }
        }
    }

    private static Transform FindFirstTransformByNames(Scene buildbeckScene, string[] names)
    {
        if (!buildbeckScene.IsValid() || !buildbeckScene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return null;
        for (int i = 0; i < names.Length; i++)
        {
            GameObject go = SceneSearchUtil.FindSceneObject(buildbeckScene, names[i]);
            if (go != null) return go.transform;
        }

        return null;
    }

    private static Button FindFirstButtonByNames(Scene buildbeckScene, string[] names)
    {
        if (!buildbeckScene.IsValid() || !buildbeckScene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return null;
        for (int i = 0; i < names.Length; i++)
        {
            GameObject go = SceneSearchUtil.FindSceneObject(buildbeckScene, names[i]);
            if (go == null) continue;
            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.GetComponentInChildren<Button>(true);
            if (btn != null) return btn;
        }

        return null;
    }

    /// <summary>Designer 可能只做 Image 未掛 Button；補上後才能綁 <c>onClick</c>。</summary>
    private static Button ResolveBackButton(Scene buildbeckScene)
    {
        if (!buildbeckScene.IsValid() || !buildbeckScene.name.Equals(SceneName, System.StringComparison.OrdinalIgnoreCase))
            return null;

        for (int i = 0; i < BackButtonNames.Length; i++)
        {
            GameObject go = SceneSearchUtil.FindSceneObject(buildbeckScene, BackButtonNames[i]);
            if (go == null) continue;

            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.GetComponentInChildren<Button>(true);
            if (btn != null) return btn;

            Graphic g = go.GetComponent<Graphic>();
            if (g == null) g = go.GetComponentInChildren<Graphic>(true);
            if (g == null || !g.raycastTarget) continue;

            btn = g.GetComponent<Button>();
            if (btn == null)
            {
                btn = g.gameObject.AddComponent<Button>();
                btn.targetGraphic = g;
                btn.transition = Selectable.Transition.None;
            }

            return btn;
        }

        return null;
    }

}

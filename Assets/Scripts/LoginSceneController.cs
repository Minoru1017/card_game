using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

public class LoginSceneController : MonoBehaviour
{
    private sealed class LoginSceneAnimRunner : MonoBehaviour { }

    private struct EntranceItem
    {
        public RectTransform rt;
        public Vector2 baseAnchoredPos;
        public CanvasGroup cg;
        public bool temporaryCanvasGroup;
        public float baseAlpha;
        public float delay;
        public float duration;
        public float offsetY;
    }
    private enum LoginCellType
    {
        Empty = 0,
        Player = 1,
        Create = 2
    }

    private struct LoginDisplayCell
    {
        public LoginCellType type;
        public int sourceSlot;
    }

    private const string LoginSceneName = "login";
    private const string HomeSceneName = "hall";
    private const string ExistingPlayerObjectName = "玩家頭像";
    private const string CreateNewPlayerObjectName = "創建新玩家";
    private const string EditRecordPointObjectName = "編輯記錄點";
    private static bool subscribed;
    private static readonly float[] SlotAnchoredX = { -566f, 0f, 566f };
    private static Coroutine entranceAnimationRoutine;
    private static LoginSceneAnimRunner animRunner;
    private static bool deleteModeEnabled;
    private static readonly List<GameObject> runtimeDeleteBadges = new List<GameObject>(8);
    private static GameObject deleteConfirmRoot;
    private static int preferredCreateSlot = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnSceneLoaded()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }
        TryBindLoginScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindLoginScene(scene);
    }

    private static void TryBindLoginScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != LoginSceneName) return;
        deleteModeEnabled = false;
        HideDeleteConfirmDialog();
        BuildDynamicLoginSlots();
    }

    private static void BuildDynamicLoginSlots()
    {
        CleanupRuntimeSlots();
        CleanupDeleteBadges();

        GameObject existingTemplate = FindTemplateObject(ExistingPlayerObjectName, -566f);
        GameObject createTemplate = FindTemplateObject(CreateNewPlayerObjectName, 0f);
        if (existingTemplate == null || createTemplate == null)
        {
            Debug.LogWarning("LoginSceneController: template objects not found in login scene.");
            return;
        }
        DisableDuplicateNamedTemplates(ExistingPlayerObjectName, existingTemplate);
        DisableDuplicateNamedTemplates(CreateNewPlayerObjectName, createTemplate);

        PlayerData.SlotSnapshot[] snapshots = PlayerData.GetSlotSnapshots();
        int preferredSlot = preferredCreateSlot;
        LoginDisplayCell[] layout = BuildDisplayLayout(snapshots, preferredSlot);
        int createTargetSlot = ResolveCreateTargetSlot(snapshots, preferredSlot);
        preferredCreateSlot = -1;

        bool usedExistingTemplate = false;
        bool usedCreateTemplate = false;

        for (int displaySlot = 1; displaySlot <= PlayerData.MaxPlayerSlots; displaySlot++)
        {
            LoginDisplayCell cell = layout[displaySlot - 1];
            int sourceSlot = Mathf.Clamp(cell.sourceSlot, 1, PlayerData.MaxPlayerSlots);

            if (cell.type == LoginCellType.Player)
            {
                GameObject item;
                if (!usedExistingTemplate)
                {
                    item = existingTemplate;
                    item.name = ExistingPlayerObjectName;
                    usedExistingTemplate = true;
                }
                else
                {
                    item = Object.Instantiate(existingTemplate, existingTemplate.transform.parent);
                    item.name = "LoginSlotRuntime_Player_" + sourceSlot + "_Pos_" + displaySlot;
                }
                PlaceAtSlot(item, displaySlot);
                SetCardLabel(item, snapshots[sourceSlot - 1].slotName + "\n金幣: " + snapshots[sourceSlot - 1].coins);
                int sourceSlotLocal = sourceSlot;
                BindLoginObjectAsButton(item, () => OnSelectExistingPlayer(sourceSlotLocal));
                EnsureDeleteBadge(item, sourceSlotLocal);
                item.SetActive(true);
            }
            else if (cell.type == LoginCellType.Create)
            {
                GameObject item;
                if (!usedCreateTemplate)
                {
                    item = createTemplate;
                    item.name = CreateNewPlayerObjectName;
                    usedCreateTemplate = true;
                }
                else
                {
                    item = Object.Instantiate(createTemplate, createTemplate.transform.parent);
                    item.name = "LoginSlotRuntime_Create_Pos_" + displaySlot;
                }
                PlaceAtSlot(item, displaySlot);
                SetCardLabel(item, "創建新玩家");
                int createTargetLocal = createTargetSlot;
                BindLoginObjectAsButton(item, () => OnCreateNewPlayer(createTargetLocal));
                item.SetActive(true);
            }
        }

        existingTemplate.SetActive(usedExistingTemplate);
        createTemplate.SetActive(usedCreateTemplate);

        BindEditRecordButton();
        UpdateDeleteBadgeVisibility();
        PlaySceneEntranceAnimation();
    }

    private static GameObject FindTemplateObject(string objectName, float preferredX)
    {
        // Prefer scene object with Button and matching name (closest X to desired slot).
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        GameObject bestButton = null;
        float bestButtonDx = float.MaxValue;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null) continue;
            GameObject go = b.gameObject;
            if (!go.scene.IsValid()) continue;
            if (!string.Equals(go.name, objectName, System.StringComparison.Ordinal)) continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;
            float dx = Mathf.Abs(rt.anchoredPosition.x - preferredX);
            if (bestButton == null || dx < bestButtonDx)
            {
                bestButton = go;
                bestButtonDx = dx;
            }
        }
        if (bestButton != null) return bestButton;

        // Fallback: scene object with TMP label child and closest X to desired slot.
        GameObject best = null;
        float bestDx = float.MaxValue;
        TextMeshProUGUI[] labels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI tmp = labels[i];
            if (tmp == null) continue;
            Transform p = tmp.transform.parent;
            if (p == null) continue;
            GameObject go = p.gameObject;
            if (!go.scene.IsValid()) continue;
            if (!string.Equals(go.name, objectName, System.StringComparison.Ordinal)) continue;
            if (go.GetComponent<RectTransform>() == null) continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            float dx = Mathf.Abs(rt.anchoredPosition.x - preferredX);
            if (best == null || dx < bestDx)
            {
                best = go;
                bestDx = dx;
            }
        }
        if (best != null) return best;

        GameObject fallback = GameObject.Find(objectName);
        if (fallback != null) return fallback;

        // Last fallback: scan all scene GameObjects with same name.
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.scene.IsValid()) continue;
            if (string.Equals(go.name, objectName, System.StringComparison.Ordinal))
                return go;
        }
        return null;
    }

    private static void DisableDuplicateNamedTemplates(string objectName, GameObject keep)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.scene.IsValid()) continue;
            if (!string.Equals(go.name, objectName, System.StringComparison.Ordinal)) continue;
            if (go == keep) continue;
            if (go.transform.IsChildOf(keep.transform) || keep.transform.IsChildOf(go.transform)) continue;
            if (!go.activeSelf) continue;
            go.SetActive(false);
        }
    }

    private static void CleanupRuntimeSlots()
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.scene.IsValid()) continue;
            if (!go.scene.name.Equals(LoginSceneName, StringComparison.Ordinal)) continue;
            if (go.name.StartsWith("LoginSlotRuntime_Player_") || go.name.StartsWith("LoginSlotRuntime_Create_"))
                Object.Destroy(go);
        }
    }

    private static void CleanupDeleteBadges()
    {
        for (int i = 0; i < runtimeDeleteBadges.Count; i++)
        {
            GameObject badge = runtimeDeleteBadges[i];
            if (badge != null) Object.Destroy(badge);
        }
        runtimeDeleteBadges.Clear();
    }

    private static void PlaceAtSlot(GameObject go, int slot)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        int index = Mathf.Clamp(slot - 1, 0, SlotAnchoredX.Length - 1);
        rt.anchoredPosition = new Vector2(SlotAnchoredX[index], 0f);
    }

    private static void SetCardLabel(GameObject go, string text)
    {
        TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) tmp.text = text;
    }

    private static void BindLoginObjectAsButton(GameObject go, UnityEngine.Events.UnityAction action)
    {
        if (go == null) return;
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);

        // Keep existing color tint feedback; remove deprecated ZoomUI click scaling.
        UIButtonYellowOutlineFeedback oldOutline = go.GetComponent<UIButtonYellowOutlineFeedback>();
        if (oldOutline != null) Object.Destroy(oldOutline);
        UIButtonPressScaleFeedback scaleFeedback = go.GetComponent<UIButtonPressScaleFeedback>();
        if (scaleFeedback != null) Object.Destroy(scaleFeedback);
        ZoomUI zoomUi = go.GetComponent<ZoomUI>();
        if (zoomUi != null) Object.Destroy(zoomUi);
        UISelectionFrameEffect selectionFrame = go.GetComponent<UISelectionFrameEffect>();
        if (selectionFrame == null) go.AddComponent<UISelectionFrameEffect>();
    }

    private static void BindEditRecordButton()
    {
        GameObject editObj = FindNamedSceneObject(EditRecordPointObjectName);
        if (editObj == null) return;

        Image img = editObj.GetComponent<Image>();
        if (img == null) img = editObj.AddComponent<Image>();
        img.raycastTarget = true;

        Button btn = editObj.GetComponent<Button>();
        if (btn == null) btn = editObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            deleteModeEnabled = !deleteModeEnabled;
            HideDeleteConfirmDialog();
            UpdateDeleteBadgeVisibility();
        });
    }

    private static void EnsureDeleteBadge(GameObject slotObject, int slot)
    {
        if (slotObject == null) return;
        RectTransform slotRt = slotObject.GetComponent<RectTransform>();
        if (slotRt == null) return;

        // Remove inherited delete badges from cloned templates.
        Transform[] children = slotObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform t = children[i];
            if (t == null || t == slotObject.transform) continue;
            if (!t.name.StartsWith("DeleteBadge_Slot_")) continue;
            if (t.parent != slotObject.transform) continue;
            Object.Destroy(t.gameObject);
        }

        string badgeName = "DeleteBadge_Slot_" + slot;
        Transform existing = slotObject.transform.Find(badgeName);
        GameObject badgeObj = existing != null ? existing.gameObject : null;
        if (badgeObj == null)
        {
            badgeObj = new GameObject(badgeName, typeof(RectTransform), typeof(Image), typeof(Button));
            badgeObj.transform.SetParent(slotObject.transform, false);
        }

        RectTransform rt = badgeObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(46f, 46f);
        rt.anchoredPosition = new Vector2(-12f, -10f);

        Image img = badgeObj.GetComponent<Image>();
        img.color = new Color(0.95f, 0.18f, 0.2f, 0.98f);
        img.raycastTarget = true;

        Button btn = badgeObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowDeleteConfirmDialog(slot));

        TextMeshProUGUI label = badgeObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badgeObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            label = textObj.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.raycastTarget = false;
        }
        label.text = "－";
        label.color = Color.white;
        TMP_FontAsset uiFont = ResolveLoginSceneFont();
        if (uiFont != null) label.font = uiFont;

        if (!runtimeDeleteBadges.Contains(badgeObj))
            runtimeDeleteBadges.Add(badgeObj);
        badgeObj.SetActive(deleteModeEnabled);
    }

    private static void UpdateDeleteBadgeVisibility()
    {
        for (int i = 0; i < runtimeDeleteBadges.Count; i++)
        {
            GameObject badge = runtimeDeleteBadges[i];
            if (badge == null) continue;
            badge.SetActive(deleteModeEnabled);
        }
    }

    private static void ShowDeleteConfirmDialog(int slot)
    {
        Scene loginScene = SceneManager.GetActiveScene();
        if (!loginScene.IsValid() || loginScene.name != LoginSceneName) return;

        if (deleteConfirmRoot == null || deleteConfirmRoot.scene != loginScene)
            deleteConfirmRoot = CreateDeleteConfirmDialog(loginScene);
        if (deleteConfirmRoot == null) return;
        ApplyFontToDialog(deleteConfirmRoot);
        UpdateDeleteConfirmDialogContent(slot);

        Button[] buttons = deleteConfirmRoot.GetComponentsInChildren<Button>(true);
        Button confirmBtn = null;
        Button cancelBtn = null;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            if (buttons[i].name == "ConfirmDeleteButton") confirmBtn = buttons[i];
            else if (buttons[i].name == "CancelDeleteButton") cancelBtn = buttons[i];
        }

        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                preferredCreateSlot = slot;
                PlayerData.DeleteSlotData(slot, 100);
                deleteModeEnabled = false;
                HideDeleteConfirmDialog();
                SceneManager.LoadScene(LoginSceneName);
            });
        }
        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(HideDeleteConfirmDialog);
        }

        deleteConfirmRoot.SetActive(true);
    }

    private static GameObject CreateDeleteConfirmDialog(Scene loginScene)
    {
        Canvas canvas = FindSceneCanvas(loginScene);
        if (canvas == null) return null;
        TMP_FontAsset uiFont = ResolveLoginSceneFont();

        GameObject root = new GameObject("LoginDeleteConfirmDialog", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(860f, 620f);
        rootRt.anchoredPosition = Vector2.zero;
        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);
        bg.raycastTarget = true;

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(root.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.06f, 0.86f);
        titleRt.anchorMax = new Vector2(0.94f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "是否要刪除該玩家的資料?";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 38f;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
        if (uiFont != null) titleText.font = uiFont;

        GameObject leftObj = new GameObject("LeftSummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        leftObj.transform.SetParent(root.transform, false);
        RectTransform leftRt = leftObj.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0.06f, 0.35f);
        leftRt.anchorMax = new Vector2(0.48f, 0.84f);
        leftRt.offsetMin = Vector2.zero;
        leftRt.offsetMax = Vector2.zero;
        TextMeshProUGUI leftText = leftObj.GetComponent<TextMeshProUGUI>();
        leftText.text = "[玩家資料]";
        leftText.alignment = TextAlignmentOptions.TopLeft;
        leftText.fontSize = 30f;
        leftText.color = Color.white;
        leftText.raycastTarget = false;
        leftText.enableWordWrapping = true;
        leftText.lineSpacing = 6f;
        if (uiFont != null) leftText.font = uiFont;

        GameObject rightObj = new GameObject("RightSummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rightObj.transform.SetParent(root.transform, false);
        RectTransform rightRt = rightObj.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.52f, 0.35f);
        rightRt.anchorMax = new Vector2(0.94f, 0.84f);
        rightRt.offsetMin = Vector2.zero;
        rightRt.offsetMax = Vector2.zero;
        TextMeshProUGUI rightText = rightObj.GetComponent<TextMeshProUGUI>();
        rightText.text = "[對戰資訊]";
        rightText.alignment = TextAlignmentOptions.TopLeft;
        rightText.fontSize = 30f;
        rightText.color = Color.white;
        rightText.raycastTarget = false;
        rightText.enableWordWrapping = true;
        rightText.lineSpacing = 6f;
        if (uiFont != null) rightText.font = uiFont;

        GameObject dividerObj = new GameObject("ColumnDivider", typeof(RectTransform), typeof(Image));
        dividerObj.transform.SetParent(root.transform, false);
        RectTransform dividerRt = dividerObj.GetComponent<RectTransform>();
        dividerRt.anchorMin = new Vector2(0.5f, 0.35f);
        dividerRt.anchorMax = new Vector2(0.5f, 0.84f);
        dividerRt.sizeDelta = new Vector2(2f, 0f);
        dividerRt.anchoredPosition = Vector2.zero;
        Image dividerImage = dividerObj.GetComponent<Image>();
        dividerImage.color = new Color(1f, 1f, 1f, 0.18f);
        dividerImage.raycastTarget = false;

        GameObject warnObj = new GameObject("WarningText", typeof(RectTransform), typeof(TextMeshProUGUI));
        warnObj.transform.SetParent(root.transform, false);
        RectTransform warnRt = warnObj.GetComponent<RectTransform>();
        warnRt.anchorMin = new Vector2(0.06f, 0.21f);
        warnRt.anchorMax = new Vector2(0.94f, 0.29f);
        warnRt.offsetMin = Vector2.zero;
        warnRt.offsetMax = Vector2.zero;
        TextMeshProUGUI warn = warnObj.GetComponent<TextMeshProUGUI>();
        warn.text = "此操作不可還原";
        warn.alignment = TextAlignmentOptions.Center;
        warn.fontSize = 36f;
        warn.color = new Color(1f, 0.35f, 0.35f, 1f);
        warn.raycastTarget = false;
        if (uiFont != null) warn.font = uiFont;

        CreateDialogButton(root.transform, "ConfirmDeleteButton", "刪除", new Vector2(-170f, -230f), new Color(0.9f, 0.22f, 0.24f, 1f), uiFont);
        CreateDialogButton(root.transform, "CancelDeleteButton", "取消", new Vector2(170f, -230f), new Color(0.35f, 0.38f, 0.42f, 1f), uiFont);
        return root;
    }

    private static Button CreateDialogButton(Transform parent, string name, string label, Vector2 anchoredPos, Color bgColor, TMP_FontAsset uiFont)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220f, 78f);
        rt.anchoredPosition = anchoredPos;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 34f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (uiFont != null) tmp.font = uiFont;
        return btn;
    }

    private static void HideDeleteConfirmDialog()
    {
        if (deleteConfirmRoot != null) deleteConfirmRoot.SetActive(false);
    }

    private static Canvas FindSceneCanvas(Scene scene)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.gameObject.scene.IsValid()) continue;
            if (c.gameObject.scene != scene) continue;
            if (c.GetComponentInParent<GlobalNavRuntime>() != null) continue;
            return c;
        }
        return null;
    }

    private static GameObject FindNamedSceneObject(string objectName)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || !go.scene.IsValid()) continue;
            if (go.scene.name != LoginSceneName) continue;
            if (string.Equals(go.name, objectName, System.StringComparison.Ordinal)) return go;
        }
        return null;
    }

    private static TMP_FontAsset ResolveLoginSceneFont()
    {
        const string required = "是否要刪除該玩家的資料刪除取消";

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset f = fonts[i];
            if (!FontSupportsText(f, required)) continue;
            if (FontNameLikelySupportsCjk(f.name)) return f;
        }

        TextMeshProUGUI[] all = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            TextMeshProUGUI tmp = all[i];
            if (tmp == null || !tmp.gameObject.scene.IsValid()) continue;
            if (tmp.gameObject.scene.name != LoginSceneName) continue;
            if (tmp.font == null) continue;
            if (FontSupportsText(tmp.font, required)) return tmp.font;
        }

        TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
        if (FontSupportsText(fallback, required)) return fallback;
        return null;
    }

    private static void ApplyFontToDialog(GameObject dialogRoot)
    {
        if (dialogRoot == null) return;
        TMP_FontAsset f = ResolveLoginSceneFont();
        if (f == null) return;
        TextMeshProUGUI[] tmps = dialogRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] == null) continue;
            tmps[i].font = f;
        }
    }

    private static void UpdateDeleteConfirmDialogContent(int slot)
    {
        if (deleteConfirmRoot == null) return;
        Transform leftTf = deleteConfirmRoot.transform.Find("LeftSummaryText");
        Transform rightTf = deleteConfirmRoot.transform.Find("RightSummaryText");
        if (leftTf == null || rightTf == null) return;
        TextMeshProUGUI leftText = leftTf.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI rightText = rightTf.GetComponent<TextMeshProUGUI>();
        if (leftText == null || rightText == null) return;

        PlayerData.SlotDeleteSummary s = PlayerData.GetSlotDeleteSummary(slot);
        leftText.text =
            "[玩家資料]\n" +
            "名稱: " + SafeUiValue(s.slotName) + "\n" +
            "UUID: " + SafeUiValue(s.uuid) + "\n" +
            "遊玩起始日期: " + SafeUiValue(s.startDate);

        rightText.text =
            "[對戰資訊]\n" +
            "W " + s.wins + " / L " + s.losses + "\n" +
            "D " + s.draws + " / Q " + s.quits + "\n\n" +
            "[牌組]\n" +
            SafeUiValue(s.deckSummary);
    }

    private static string SafeUiValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        return value.Trim();
    }

    private static bool FontSupportsText(TMP_FontAsset font, string required)
    {
        if (font == null || string.IsNullOrEmpty(required)) return false;
        for (int i = 0; i < required.Length; i++)
        {
            char ch = required[i];
            if (char.IsWhiteSpace(ch)) continue;
            if (!font.HasCharacter(ch, true)) return false;
        }
        return true;
    }

    private static bool FontNameLikelySupportsCjk(string fontAssetName)
    {
        if (string.IsNullOrEmpty(fontAssetName)) return false;
        string n = fontAssetName.ToLowerInvariant();
        return n.Contains("noto") ||
               n.Contains("cjk") ||
               n.Contains("sourcehan") ||
               n.Contains("source han") ||
               n.Contains("jhenghei") ||
               n.Contains("yahei") ||
               n.Contains("pingfang") ||
               n.Contains("heiti") ||
               n.Contains("simhei") ||
               n.Contains("simsun") ||
               n.Contains("msjh") ||
               n.Contains("mingliu");
    }

    private static void OnSelectExistingPlayer(int slot)
    {
        PlayerData.SelectActivePlayerSlot(slot);
        PlayerProfileCsvService.RefreshProfileFromRuntime();
        LoadHomeOrTutorialStoryProgress(slot);
    }

    private static void OnCreateNewPlayer(int slot)
    {
        PlayerData.SelectActivePlayerSlot(slot);
        TutorialProgressState.ResetTutorialForSlot(slot);
        PlayerProfileCsvService.CreateNewPlayerDefaults(100, "一般玩家");
        LoadHomeOrTutorialStoryProgress(slot);
    }

    private static void LoadHomeOrTutorialStoryProgress(int slot)
    {
        if (TutorialProgressState.NeedsTutorialFlow(slot) &&
            Application.CanStreamedLevelBeLoaded(StoryProgressSession.StoryProgressSceneName))
        {
            SceneManager.LoadScene(StoryProgressSession.StoryProgressSceneName);
            return;
        }

        SceneManager.LoadScene(HomeSceneName);
    }

    private static void PlaySceneEntranceAnimation()
    {
        Scene loginScene = SceneManager.GetActiveScene();
        if (!loginScene.IsValid() || loginScene.name != LoginSceneName) return;
        EnsureAnimRunner(loginScene);
        if (animRunner == null) return;

        if (entranceAnimationRoutine != null)
        {
            animRunner.StopCoroutine(entranceAnimationRoutine);
            entranceAnimationRoutine = null;
        }
        entranceAnimationRoutine = animRunner.StartCoroutine(PlayEntranceAnimationRoutine(loginScene));
    }

    private static void EnsureAnimRunner(Scene loginScene)
    {
        if (animRunner != null && animRunner.gameObject != null && animRunner.gameObject.scene == loginScene)
            return;

        GameObject existing = GameObject.Find("LoginSceneAnimRunner");
        if (existing != null && existing.scene == loginScene)
        {
            animRunner = existing.GetComponent<LoginSceneAnimRunner>();
            if (animRunner == null) animRunner = existing.AddComponent<LoginSceneAnimRunner>();
            return;
        }

        GameObject go = new GameObject("LoginSceneAnimRunner");
        SceneManager.MoveGameObjectToScene(go, loginScene);
        animRunner = go.AddComponent<LoginSceneAnimRunner>();
    }

    private static IEnumerator PlayEntranceAnimationRoutine(Scene loginScene)
    {
        // Wait one frame so all runtime-created login slots are active and positioned.
        yield return null;

        RectTransform[] allRects = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        List<EntranceItem> items = new List<EntranceItem>(allRects.Length);
        for (int i = 0; i < allRects.Length; i++)
        {
            RectTransform rt = allRects[i];
            if (rt == null || !rt.gameObject.scene.IsValid()) continue;
            if (rt.gameObject.scene != loginScene) continue;
            if (!rt.gameObject.activeInHierarchy) continue;
            if (rt.GetComponentInParent<GlobalNavRuntime>() != null) continue;
            if (rt.gameObject.name.StartsWith("SelectionRing_")) continue;
            if (rt.GetComponent<Canvas>() != null) continue;
            if (rt.GetComponent<Graphic>() == null && rt.GetComponentInChildren<Graphic>(true) == null) continue;

            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            bool temp = false;
            if (cg == null)
            {
                cg = rt.gameObject.AddComponent<CanvasGroup>();
                temp = true;
            }

            bool isBackground = IsBackgroundCandidate(rt);
            bool isMainCard = IsMainCardCandidate(rt);
            float groupBaseDelay = isBackground ? 0f : (isMainCard ? 0.20f : 0.34f);
            float intraGroupDelay = isBackground ? 0.01f : (isMainCard ? 0.045f : 0.018f);

            EntranceItem item = new EntranceItem
            {
                rt = rt,
                baseAnchoredPos = rt.anchoredPosition,
                cg = cg,
                temporaryCanvasGroup = temp,
                baseAlpha = Mathf.Clamp01(cg.alpha),
                delay = groupBaseDelay + Mathf.Clamp(i * intraGroupDelay, 0f, isBackground ? 0.08f : (isMainCard ? 0.20f : 0.14f)),
                duration = isBackground ? 0.52f : (isMainCard ? 0.44f : 0.30f),
                offsetY = isBackground ? 14f : (isMainCard ? 62f : 22f)
            };
            items.Add(item);
        }

        float elapsed = 0f;
        float endTime = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            EntranceItem it = items[i];
            if (it.rt == null || it.cg == null) continue;
            it.rt.anchoredPosition = it.baseAnchoredPos + new Vector2(0f, -it.offsetY);
            it.cg.alpha = 0f;
            endTime = Mathf.Max(endTime, it.delay + it.duration);
            items[i] = it;
        }

        endTime += 0.05f;

        while (elapsed < endTime)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < items.Count; i++)
            {
                EntranceItem it = items[i];
                if (it.rt == null || it.cg == null) continue;
                float local = Mathf.Clamp01((elapsed - it.delay) / Mathf.Max(0.01f, it.duration));
                if (local <= 0f) continue;
                float eased = EaseOutBack(local);
                it.rt.anchoredPosition = Vector2.LerpUnclamped(
                    it.baseAnchoredPos + new Vector2(0f, -it.offsetY),
                    it.baseAnchoredPos,
                    eased);
                float alphaT = 1f - Mathf.Pow(1f - local, 3f);
                it.cg.alpha = Mathf.Lerp(0f, it.baseAlpha, alphaT);
            }
            yield return null;
        }

        for (int i = 0; i < items.Count; i++)
        {
            EntranceItem it = items[i];
            if (it.rt != null) it.rt.anchoredPosition = it.baseAnchoredPos;
            if (it.cg != null) it.cg.alpha = it.baseAlpha;
            if (it.temporaryCanvasGroup && it.cg != null)
                Object.Destroy(it.cg);
        }

        entranceAnimationRoutine = null;
    }

    private static bool IsBackgroundCandidate(RectTransform rt)
    {
        if (rt == null) return false;
        string n = rt.gameObject.name;
        if (!string.IsNullOrEmpty(n) &&
            (n.IndexOf("背景", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             n.IndexOf("background", System.StringComparison.OrdinalIgnoreCase) >= 0))
            return true;

        Vector2 size = rt.rect.size;
        return size.x >= 1200f && size.y >= 450f;
    }

    private static bool IsMainCardCandidate(RectTransform rt)
    {
        if (rt == null) return false;
        GameObject go = rt.gameObject;
        if (go.GetComponent<Button>() == null) return false;
        string n = go.name;
        return n == ExistingPlayerObjectName ||
               n == CreateNewPlayerObjectName ||
               n.StartsWith("LoginSlotRuntime_Player_") ||
               n.StartsWith("LoginSlotRuntime_Create_");
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private static LoginDisplayCell[] BuildDisplayLayout(PlayerData.SlotSnapshot[] snapshots, int preferredSlot)
    {
        var result = new LoginDisplayCell[PlayerData.MaxPlayerSlots];
        if (snapshots == null || snapshots.Length < PlayerData.MaxPlayerSlots)
            return result;

        List<int> playerSlots = GetPlayerSlotsSortedByStartDate(snapshots);
        int count = playerSlots.Count;
        int createTargetSlot = ResolveCreateTargetSlot(snapshots, preferredSlot);

        if (count >= 3)
        {
            result[0] = new LoginDisplayCell { type = LoginCellType.Player, sourceSlot = playerSlots[0] };
            result[1] = new LoginDisplayCell { type = LoginCellType.Player, sourceSlot = playerSlots[1] };
            result[2] = new LoginDisplayCell { type = LoginCellType.Player, sourceSlot = playerSlots[2] };
            return result;
        }

        if (count == 2)
        {
            result[0] = new LoginDisplayCell { type = LoginCellType.Player, sourceSlot = playerSlots[0] };
            result[1] = new LoginDisplayCell { type = LoginCellType.Player, sourceSlot = playerSlots[1] };
            result[2] = new LoginDisplayCell { type = LoginCellType.Create, sourceSlot = createTargetSlot };
            return result;
        }

        if (count == 1)
        {
            result[0] = new LoginDisplayCell { type = LoginCellType.Player, sourceSlot = playerSlots[0] };
            result[1] = new LoginDisplayCell { type = LoginCellType.Create, sourceSlot = createTargetSlot };
            return result;
        }

        result[1] = new LoginDisplayCell { type = LoginCellType.Create, sourceSlot = createTargetSlot };
        return result;
    }

    private static List<int> GetPlayerSlotsSortedByStartDate(PlayerData.SlotSnapshot[] snapshots)
    {
        var playerSlots = new List<int>(PlayerData.MaxPlayerSlots);
        for (int slot = 1; slot <= PlayerData.MaxPlayerSlots; slot++)
        {
            if (snapshots[slot - 1].hasData) playerSlots.Add(slot);
        }

        playerSlots.Sort((a, b) =>
        {
            DateTime da = ParseSlotStartDate(PlayerData.GetSlotDeleteSummary(a).startDate);
            DateTime db = ParseSlotStartDate(PlayerData.GetSlotDeleteSummary(b).startDate);
            int cmp = da.CompareTo(db);
            if (cmp != 0) return cmp;
            return a.CompareTo(b);
        });
        return playerSlots;
    }

    private static DateTime ParseSlotStartDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return DateTime.MaxValue;
        string t = text.Trim();
        if (DateTime.TryParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            return d;
        if (DateTime.TryParse(t, out DateTime fallback))
            return fallback;
        return DateTime.MaxValue;
    }

    private static int ResolveCreateTargetSlot(PlayerData.SlotSnapshot[] snapshots, int preferredSlot)
    {
        if (snapshots != null &&
            preferredSlot >= 1 &&
            preferredSlot <= PlayerData.MaxPlayerSlots &&
            !snapshots[preferredSlot - 1].hasData)
            return preferredSlot;
        return Mathf.Clamp(PlayerData.FindFirstEmptySlot(), 1, PlayerData.MaxPlayerSlots);
    }

}

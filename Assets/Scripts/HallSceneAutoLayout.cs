using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a themed lobby UI when entering hall scene.
/// Non-destructive: if root already exists, it does nothing.
/// </summary>
public class HallSceneAutoLayout : MonoBehaviour
{
    private const string TargetSceneName = "hall";
    private const string RootName = "HallUILayoutRoot";
    private static TMP_FontAsset cachedTcFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildOnHallSceneLoad()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != TargetSceneName) return;
        // If designer-authored lobby objects already exist, skip auto-generated layout.
        if (GameObject.Find("英雄狀態") != null || GameObject.Find("任務容器") != null) return;
        if (GameObject.Find(RootName) != null) return;

        EnsureEventSystem();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject autoCanvasObj = new GameObject("HallAutoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = autoCanvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = autoCanvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject rootObj = new GameObject(RootName, typeof(RectTransform));
        rootObj.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        CreateBackground(rootObj.transform);
        CreateHeader(rootObj.transform);
        CreateHeroStatusPanel(rootObj.transform);
        CreateActionButtons(rootObj.transform);
        CreateQuestPanel(rootObj.transform);
        CreateSettingsButton(rootObj.transform);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject bg = CreateUiObj("HallBG", parent, typeof(Image));
        Stretch(bg);
        Image img = bg.GetComponent<Image>();
        img.color = new Color(0.93f, 0.89f, 0.82f, 1f);

        GameObject tone = CreateUiObj("HallBGTone", parent, typeof(Image));
        Stretch(tone);
        Image toneImg = tone.GetComponent<Image>();
        toneImg.color = new Color(0.52f, 0.59f, 0.44f, 0.18f);
    }

    private static void CreateHeader(Transform parent)
    {
        GameObject bar = CreateUiObj("TopBar", parent, typeof(Image));
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 118f);
        rt.anchoredPosition = Vector2.zero;
        bar.GetComponent<Image>().color = new Color(0.52f, 0.59f, 0.44f, 0.95f);

        TextMeshProUGUI title = CreateLabel("LobbyTitle", bar.transform, "冒險者大廳", 54, TextAlignmentOptions.Center);
        RectTransform tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        title.color = new Color(0.16f, 0.13f, 0.1f, 1f);
    }

    private static void CreateHeroStatusPanel(Transform parent)
    {
        GameObject panel = CreateCardPanel("HeroStatusPanel", parent, new Vector2(32f, -144f), new Vector2(560f, 320f));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        CreateLabel("HeroTitle", panel.transform, "玩家英雄狀態", 34, TextAlignmentOptions.TopLeft, new Vector2(24f, -20f));
        CreateLabel("HeroInfo", panel.transform,
            "英雄名稱: 森語守護者\n等級: 12\n生命: 86 / 100\n魔力: 42 / 60\n金幣: 1,250\n稱號: 新芽戰略家",
            28, TextAlignmentOptions.TopLeft, new Vector2(24f, -78f));
    }

    private static void CreateActionButtons(Transform parent)
    {
        GameObject deckBtn = CreatePrimaryButton("BuildDeckButton", parent, "組建牌組", new Vector2(32f, -500f), new Vector2(320f, 94f));
        SetAnchorTopLeft(deckBtn.GetComponent<RectTransform>());
        GameObject bagBtn = CreateSecondaryButton("BagButton", parent, "背包", new Vector2(372f, -500f), new Vector2(180f, 94f));
        SetAnchorTopLeft(bagBtn.GetComponent<RectTransform>());
        GameObject storeBtn = CreateSecondaryButton("StoreButton", parent, "商店", new Vector2(566f, -500f), new Vector2(180f, 94f));
        SetAnchorTopLeft(storeBtn.GetComponent<RectTransform>());
    }

    private static void CreateQuestPanel(Transform parent)
    {
        GameObject panel = CreateCardPanel("QuestPanel", parent, new Vector2(-32f, -144f), new Vector2(860f, 560f));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);

        CreateLabel("QuestTitle", panel.transform, "任務區塊", 36, TextAlignmentOptions.TopLeft, new Vector2(28f, -20f));
        CreateLabel("QuestBody", panel.transform,
            "每日任務\n- 完成 1 場對戰\n- 施放 3 次法術\n- 組建並保存 1 套牌組\n\n主線任務\n- 擊敗「霧潮守門人」\n- 收集 10 張不同怪獸卡",
            29, TextAlignmentOptions.TopLeft, new Vector2(28f, -86f));
    }

    private static void CreateSettingsButton(Transform parent)
    {
        GameObject btn = CreateSecondaryButton("SettingsButton", parent, "設定", new Vector2(-32f, -132f), new Vector2(170f, 70f));
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
    }

    private static GameObject CreateCardPanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
    {
        GameObject panel = CreateUiObj(name, parent, typeof(Image), typeof(Outline));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = panel.GetComponent<Image>();
        img.color = new Color(0.94f, 0.9f, 0.83f, 0.98f);
        img.type = Image.Type.Sliced;
        Outline ol = panel.GetComponent<Outline>();
        ol.effectColor = new Color(0.43f, 0.34f, 0.24f, 0.45f);
        ol.effectDistance = new Vector2(2f, -2f);
        return panel;
    }

    private static GameObject CreatePrimaryButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject buttonObj = CreateUiObj(name, parent, typeof(Image), typeof(Button));
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image bg = buttonObj.GetComponent<Image>();
        bg.color = new Color(0.4431373f, 0.28235295f, 0.24705884f, 1f);
        bg.type = Image.Type.Sliced;
        CreateButtonLabel(buttonObj.transform, label, Color.white, 34);
        return buttonObj;
    }

    private static GameObject CreateSecondaryButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject buttonObj = CreateUiObj(name, parent, typeof(Image), typeof(Button));
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image bg = buttonObj.GetComponent<Image>();
        bg.color = new Color(0.9f, 0.93f, 0.86f, 1f);
        bg.type = Image.Type.Sliced;
        CreateButtonLabel(buttonObj.transform, label, new Color(0.23f, 0.18f, 0.14f, 1f), 30);
        return buttonObj;
    }

    private static void CreateButtonLabel(Transform parent, string text, Color color, int fontSize)
    {
        TextMeshProUGUI label = CreateLabel("Label", parent, text, fontSize, TextAlignmentOptions.Center);
        RectTransform rt = label.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        label.color = color;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, int fontSize, TextAlignmentOptions align, Vector2? anchoredPos = null)
    {
        GameObject obj = CreateUiObj(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset tcFont = ResolveProjectTcFont();
        if (tcFont != null) tmp.font = tcFont;
        tmp.text = SanitizeLobbyText(text);
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.color = new Color(0.2f, 0.16f, 0.12f, 1f);

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(0f, 0f);
        if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
        return tmp;
    }

    private static GameObject CreateUiObj(string name, Transform parent, params System.Type[] components)
    {
        GameObject obj = new GameObject(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void Stretch(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchorTopLeft(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
    }

    private static string SanitizeLobbyText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        // Normalize punctuation/symbols that are commonly missing in some TMP font assets.
        return raw
            .Replace('：', ':')
            .Replace('，', ',')
            .Replace('。', '.')
            .Replace('（', '(')
            .Replace('）', ')')
            .Replace('？', '?')
            .Replace('！', '!')
            .Replace('「', '"')
            .Replace('」', '"')
            .Replace('、', ',')
            .Replace('—', '-')
            .Replace('…', '.');
    }

    private static TMP_FontAsset ResolveProjectTcFont()
    {
        if (cachedTcFont != null) return cachedTcFont;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            TMP_FontAsset f = fonts[i];
            if (f == null || string.IsNullOrEmpty(f.name)) continue;
            // Prefer project TC font asset.
            if (f.name.StartsWith("NotoSansTC") || f.name.StartsWith("TC"))
            {
                cachedTcFont = f;
                return cachedTcFont;
            }
        }

        if (TMP_Settings.defaultFontAsset != null)
            cachedTcFont = TMP_Settings.defaultFontAsset;
        return cachedTcFont;
    }
}

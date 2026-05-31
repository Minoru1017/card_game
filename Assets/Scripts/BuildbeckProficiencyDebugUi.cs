using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Buildbeck 背包場景：一鍵清空熟練度（測試用）。</summary>
[DisallowMultipleComponent]
public class BuildbeckProficiencyDebugUi : MonoBehaviour
{
    private const string RootName = "BuildbeckProficiencyDebugPanel";

    [SerializeField] private bool showInPlayMode = true;

    private GameObject root;
    private DeckManager deckManager;

    private void OnEnable()
    {
        deckManager = GetComponent<DeckManager>();
        if (deckManager == null)
            deckManager = FindFirstObjectByType<DeckManager>();
        TryEnsureUi();
    }

    private void OnDisable()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void TryEnsureUi()
    {
        if (!showInPlayMode || !Application.isPlaying) return;
        if (!IsBuildbeckActive()) return;

        Canvas canvas = ResolveCanvas();
        if (canvas == null) return;

        if (root == null)
            BuildUi(canvas.transform);
        else
            root.transform.SetParent(canvas.transform, false);

        root.SetActive(true);
        root.transform.SetAsLastSibling();
    }

    private void BuildUi(Transform parent)
    {
        root = new GameObject(RootName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        RectTransform panelRt = root.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 0f);
        panelRt.pivot = new Vector2(0f, 0f);
        panelRt.anchoredPosition = new Vector2(18f, 18f);
        panelRt.sizeDelta = new Vector2(320f, 88f);
        Image panelBg = root.GetComponent<Image>();
        panelBg.sprite = UiWhiteSprite();
        panelBg.color = new Color(0.12f, 0.1f, 0.09f, 0.88f);

        GameObject btnGo = new GameObject("ResetProficiencyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(root.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = Vector2.zero;
        btnRt.anchorMax = Vector2.one;
        btnRt.offsetMin = new Vector2(10f, 10f);
        btnRt.offsetMax = new Vector2(-10f, -10f);
        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = UiWhiteSprite();
        btnImg.color = new Color(0.75f, 0.45f, 0.22f, 1f);
        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnClickResetAllProficiency);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.font = ResolveFont();
        label.fontSize = 26;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.98f, 0.96f, 0.92f, 1f);
        label.text = "Clear Proficiency (Test)";
        label.raycastTarget = false;
    }

    private void OnClickResetAllProficiency()
    {
        if (deckManager == null)
            deckManager = FindFirstObjectByType<DeckManager>();
        deckManager?.EnsureCoreRefsForInspect();

        PlayerData pd = PlayerData.ResolveCanonical();
        CardStore store = pd != null ? pd.CardStore : null;
        if (store == null && deckManager != null)
            store = deckManager.GetComponent<CardStore>();

        if (pd == null)
        {
            Debug.LogWarning("[BuildbeckProficiencyDebugUi] 找不到 PlayerData。");
            return;
        }

        CardProficiencyDebugReset.PerformFullReset(pd, store, reloadAfterSave: true);

        BackpackCardInspectPanel inspect = deckManager != null
            ? deckManager.GetComponent<BackpackCardInspectPanel>()
            : null;
        if (inspect == null && deckManager != null)
            inspect = deckManager.gameObject.GetComponent<BackpackCardInspectPanel>();
        inspect?.RefreshMasteryBarIfOpen();
    }

    private static bool IsBuildbeckActive()
    {
        Scene s = SceneManager.GetActiveScene();
        return s.IsValid() && s.name.Equals("Buildbeck", System.StringComparison.OrdinalIgnoreCase);
    }

    private Canvas ResolveCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas best = null;
        int bestOrder = int.MinValue;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.sortingOrder >= bestOrder)
            {
                bestOrder = c.sortingOrder;
                best = c;
            }
        }
        return best;
    }

    private static Sprite UiWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        Texture2D tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
    }

    private static Sprite _whiteSprite;

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) return font;
        return TMP_Settings.defaultFontAsset;
    }
}

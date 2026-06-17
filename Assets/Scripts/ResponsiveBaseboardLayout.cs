using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 響應式底板版面（下一版響應式設計）。
///
/// 原則：畫面佈局維持 1920×1080，背景依「高度」縮放貼齊螢幕上下緣（CanvasScaler match = 1）。
/// 16:9 內容置中；左右兩側若有空隙（裝置比例寬於 16:9），由底板貼螢幕外緣補滿。
///
/// 設計目標範圍：4:3（最方）～ 22:9（最寬）。
/// - 每側底板寬（design px）＝ clamp(540·R − 960, 0, 360)，R = 裝置寬/高；≤16:9 時為 0（隱藏）。
/// - 中央保險區 1440×1080：critical UI 一律放這裡，最方到 4:3 仍完整。
///
/// 用法：掛在 UI Canvas 上（或其子物件），指定 leftBaseboard / rightBaseboard（兩個容器，
/// 底板美術由 <see cref="UiSpriteLibrary.ResponsiveBasePlate"/> 自動套用；右側水平翻轉共用）。
/// 可選 safeArea、background 由本元件統一定位。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public sealed class ResponsiveBaseboardLayout : MonoBehaviour
{
    [Header("目標畫布")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasScaler scaler;
    [Tooltip("勾選後本元件會把 CanvasScaler 設成 ScaleWithScreenSize + 貼高度（match=1）。")]
    [SerializeField] private bool forceHeightMatch = true;

    [Header("參考解析度")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("底板（左右側空隙填補；請放在 16:9 內容之下）")]
    [SerializeField] private RectTransform leftBaseboard;
    [SerializeField] private RectTransform rightBaseboard;
    [Tooltip("每側底板最大寬（design px）。保到 22:9 = 360。")]
    [SerializeField] private float baseboardMaxWidth = 360f;
    [Tooltip("空隙為 0（≤16:9）時隱藏底板物件。")]
    [SerializeField] private bool hideWhenNoGap = true;

    [Header("中央保險區（選填，critical UI 容器）")]
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private float safeWidth = 1440f;

    [Header("背景（選填，依高度等比縮放置中；絕不左右拉伸）")]
    [SerializeField] private RectTransform background;

    [Header("底板美術")]
    [Tooltip("留空則使用 UiSpriteLibrary.ResponsiveBasePlate（base plate.png，左右共用）。")]
    [SerializeField] private Sprite basePlateSpriteOverride;

    private RectTransform rt;
    private RectTransform Rt => rt != null ? rt : (rt = GetComponent<RectTransform>());

    /// <summary>目前每側底板寬（design px），供外部查詢。</summary>
    public float CurrentBaseboardWidth { get; private set; }

    private void Reset()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) scaler = canvas.GetComponent<CanvasScaler>();
    }

    private void OnEnable() => Apply();

    private void OnRectTransformDimensionsChange() => Apply();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        // 延後到 OnValidate 之外執行，避免在序列化回呼中呼叫 SetActive 觸發警告。
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Apply();
        };
    }
#endif

    /// <summary>重新計算並套用底板/保險區/背景版面。</summary>
    public void Apply()
    {
        EnsureScaler();

        float designWidth = ResolveDesignWidth();
        if (designWidth <= 0f) return;

        float gap = Mathf.Clamp((designWidth - referenceResolution.x) * 0.5f, 0f, baseboardMaxWidth);
        CurrentBaseboardWidth = gap;

        ApplyEdgeBoard(leftBaseboard, true, gap);
        ApplyEdgeBoard(rightBaseboard, false, gap);
        ApplySafeArea();
        ApplyBackground();
    }

    private void EnsureScaler()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (scaler == null && canvas != null) scaler = canvas.GetComponent<CanvasScaler>();
        if (!forceHeightMatch || scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 1f; // 貼高度：1 design 單位 = 螢幕高/1080
    }

    private float ResolveDesignWidth()
    {
        if (canvas != null)
        {
            RectTransform crt = canvas.transform as RectTransform;
            if (crt != null && crt.rect.width > 0f) return crt.rect.width;
        }
        return Rt.rect.width;
    }

    private void ApplyEdgeBoard(RectTransform board, bool left, float gap)
    {
        if (board == null) return;

        if (hideWhenNoGap)
        {
            bool active = gap > 0.5f;
            if (board.gameObject.activeSelf != active) board.gameObject.SetActive(active);
            if (!active) return;
        }

        float x = left ? 0f : 1f;
        board.anchorMin = new Vector2(x, 0f);
        board.anchorMax = new Vector2(x, 1f);
        board.pivot = new Vector2(x, 0.5f);
        board.localScale = Vector3.one;
        board.sizeDelta = new Vector2(gap, 0f);
        board.anchoredPosition = Vector2.zero;
        ApplyBasePlateVisual(board, left);
    }

    private const string BaseboardArtChildName = "BaseboardArt";

    private Sprite ResolveBasePlateSprite()
    {
        if (basePlateSpriteOverride != null)
            return basePlateSpriteOverride;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        return library != null ? library.ResponsiveBasePlate : null;
    }

    private void ApplyBasePlateVisual(RectTransform board, bool left)
    {
        if (board == null)
            return;

        Sprite sprite = ResolveBasePlateSprite();
        if (sprite == null)
            return;

        Image parentImage = board.GetComponent<Image>();
        if (parentImage != null)
        {
            parentImage.sprite = null;
            parentImage.enabled = false;
        }

        RectTransform art = EnsureBaseboardArtChild(board);
        Image image = art.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.pivot = new Vector2(0.5f, 0.5f);
        art.offsetMin = Vector2.zero;
        art.offsetMax = Vector2.zero;
        art.anchoredPosition = Vector2.zero;
        art.localScale = new Vector3(left ? 1f : -1f, 1f, 1f);
    }

    private static RectTransform EnsureBaseboardArtChild(RectTransform board)
    {
        Transform existing = board.Find(BaseboardArtChildName);
        if (existing != null)
            return existing as RectTransform;

        GameObject go = new GameObject(BaseboardArtChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(board, false);
        return go.GetComponent<RectTransform>();
    }

    private void ApplySafeArea()
    {
        if (safeArea == null) return;
        safeArea.anchorMin = new Vector2(0.5f, 0f);
        safeArea.anchorMax = new Vector2(0.5f, 1f);
        safeArea.pivot = new Vector2(0.5f, 0.5f);
        safeArea.sizeDelta = new Vector2(safeWidth, 0f);
        safeArea.anchoredPosition = Vector2.zero;
    }

    private void ApplyBackground()
    {
        if (background == null) return;

        background.anchorMin = new Vector2(0.5f, 0.5f);
        background.anchorMax = new Vector2(0.5f, 0.5f);
        background.pivot = new Vector2(0.5f, 0.5f);
        background.anchoredPosition = Vector2.zero;

        // 只依高度等比縮放，絕不左右拉伸：鎖定高度 = 參考高，寬度依圖片原生比例推算。
        // 視窗較方（<16:9）時背景寬於畫面 → 左右自然裁切；較寬時 → 兩側空隙由底板補滿。
        float height = referenceResolution.y;
        float width = referenceResolution.x;

        Image bgImage = background.GetComponent<Image>();
        if (bgImage != null && bgImage.sprite != null)
        {
            Rect spriteRect = bgImage.sprite.rect;
            if (spriteRect.height > 0f)
                width = height * (spriteRect.width / spriteRect.height);
            bgImage.preserveAspect = true; // 雙重保險：即使外部改了尺寸也不會變形
        }

        background.sizeDelta = new Vector2(width, height);
    }
}

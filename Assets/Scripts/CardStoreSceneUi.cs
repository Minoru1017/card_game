using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>CardStore 場景：修正開包按鈕觸控（TMP 擋 raycast、層級、Safe Area）。</summary>
public static class CardStoreSceneUi
{
    private const string OpenButtonObjectName = "Open";
    private const float OpenButtonDesignBottomY = 118.89f;

    public static void ApplyNow(Scene scene = default)
    {
        Scene target = scene.IsValid() ? scene : SceneManager.GetActiveScene();
        if (!target.IsValid() || target.name != "CardStore")
            return;

        EnsureEventSystem();
        FixOpenPackButtonTouch();
        DisableVideoScreenRaycastsOnly();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(es);
    }

    private static void FixOpenPackButtonTouch()
    {
        GameObject openGo = GameObject.Find(OpenButtonObjectName);
        if (openGo == null)
            return;

        Image buttonImage = openGo.GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.raycastTarget = true;

        Graphic[] graphics = openGo.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null || g == buttonImage)
                continue;
            g.raycastTarget = false;
        }

        OpenPackge opener = Object.FindFirstObjectByType<OpenPackge>();
        Button btn = openGo.GetComponent<Button>();
        if (btn != null && opener != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveListener(opener.OnClickOpen);
            btn.onClick.AddListener(opener.OnClickOpen);
        }

        GameObject screen = GameObject.Find("Screen");
        if (screen == null || !screen.activeInHierarchy)
            openGo.transform.SetAsLastSibling();

        ApplyOpenButtonSafeArea(openGo);
    }

    private static void ApplyOpenButtonSafeArea(GameObject openGo)
    {
        RectTransform openRt = openGo.GetComponent<RectTransform>();
        if (openRt == null)
            return;

        Canvas canvas = openRt.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        MobileUiLayoutPolicy.CanvasSafeInsets safe = MobileUiLayoutPolicy.GetCanvasSafeInsets(canvas);
        Vector2 pos = openRt.anchoredPosition;
        pos.y = OpenButtonDesignBottomY + Mathf.Max(0f, safe.Bottom);
        if (MobileUiLayoutPolicy.UseMobileLayout)
            pos.x = 0f;
        openRt.anchoredPosition = pos;
    }

    private static void DisableVideoScreenRaycastsOnly()
    {
        GameObject screen = GameObject.Find("Screen");
        if (screen == null)
            return;

        RawImage raw = screen.GetComponent<RawImage>();
        if (raw != null)
            raw.raycastTarget = false;
    }
}

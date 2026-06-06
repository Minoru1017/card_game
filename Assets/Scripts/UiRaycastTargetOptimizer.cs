using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 因素2：UI 上每個 raycastTarget=true 的 Graphic 都會被 GraphicRaycaster 逐一射線測試，
/// 純裝飾的文字 / 圖片（標題、說明、背板、圖示）開著 raycastTarget 只是白白吃 CPU、
/// 還可能擋住底下真正可點的元件。本類別在每次場景載入後掃描一次，把「不在任何
/// 可互動元件（Selectable：Button / Toggle / Slider / Scrollbar / InputField...）底下」
/// 的 Graphic 關閉 raycastTarget。對互動元件本身與其子物件一律保留，避免破壞點擊。
/// </summary>
public static class UiRaycastTargetOptimizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 第一個場景在 AfterSceneLoad 時可能已載入，補跑一次。
        Optimize();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Optimize();

    private static void Optimize()
    {
        // 文字幾乎不需要當射線目標，且 TMP 是最常見的浪費來源；圖片較可能是 Button 背板，保守處理。
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int changed = 0;
        for (int i = 0; i < texts.Length; i++)
        {
            var graphic = texts[i] as Graphic;
            if (graphic == null || !graphic.raycastTarget)
                continue;
            if (IsInteractiveOrTarget(graphic))
                continue;
            graphic.raycastTarget = false;
            changed++;
        }

        // 同樣處理 UI Text（舊版），規則一致。
        var uiTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < uiTexts.Length; i++)
        {
            var graphic = uiTexts[i] as Graphic;
            if (graphic == null || !graphic.raycastTarget)
                continue;
            if (IsInteractiveOrTarget(graphic))
                continue;
            graphic.raycastTarget = false;
            changed++;
        }

        if (changed > 0)
            GameDevLog.Log($"UiRaycastTargetOptimizer: 關閉 {changed} 個非互動文字的 raycastTarget。");
    }

    /// <summary>
    /// 若此 Graphic 本身或祖先掛有 Selectable（Button / Toggle / Slider / Scrollbar / InputField...），
    /// 視為互動相關，保留 raycast，避免關掉後按鈕標籤無法觸發點擊。
    /// </summary>
    private static bool IsInteractiveOrTarget(Graphic graphic)
    {
        return graphic.GetComponentInParent<Selectable>(true) != null;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- ui helpers

    private static Image CreateImage(
        string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        Color color, bool raycastTarget)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = raycastTarget;
        return img;
    }

    private static TextMeshProUGUI CreateText(
        string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        UiFontResolver.ApplyTo(tmp, text);
        return tmp;
    }

    /// <summary>建立水平進度條，回傳「填充」RectTransform（以 anchorMax.x 表示進度）。</summary>
    private static RectTransform CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        Image bg = CreateImage(name + "Bg", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero,
            new Color(0.08f, 0.09f, 0.12f, 0.9f), false);
        RectTransform bgRt = bg.rectTransform;
        bgRt.pivot = new Vector2(0f, 1f);
        bgRt.sizeDelta = size;
        bgRt.anchoredPosition = anchoredPos;

        Image fill = CreateImage(name + "Fill", bg.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, fillColor, false);
        RectTransform fillRt = fill.rectTransform;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        return fillRt;
    }
}

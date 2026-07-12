using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSimulationDebugUI
{
    private const float HandTooltipTagRowPadH = 32f;
    private const float HandTooltipTagRowHeight = 38f;
    private const float HandTooltipTagChipSpacing = 10f;
    private const float HandTooltipTagRowGapBelow = 12f;
    private const int HandTooltipTagFontSize = 22;
    private const float HandTooltipTagChipPadH = 12f;
    private const float HandTooltipTagChipPadV = 6f;

    private static readonly Color HandTooltipTagChipBg = new Color(0.16f, 0.18f, 0.22f, 0.96f);
    private static readonly Color HandTooltipTagLabelColor = new Color(0.68f, 0.74f, 0.80f, 1f);
    private static readonly Color HandTooltipTagValueColor = new Color(0.94f, 0.96f, 0.98f, 1f);
    private static readonly Color HandTooltipTagBorderFallback = new Color(0.42f, 0.48f, 0.54f, 0.85f);

    private RectTransform handTooltipTagRowRt;
    private readonly List<HandTooltipTagChipView> handTooltipTagChipPool = new List<HandTooltipTagChipView>(6);

    private sealed class HandTooltipTagChipView
    {
        public GameObject root;
        public Image background;
        public Outline outline;
        public TextMeshProUGUI labelTmp;
    }

    private void RefreshHandTooltipAttributeTags(HandLongPressAttributeTag[] tags)
    {
        EnsureTooltip();
        if (handTooltipTagRowRt == null) return;

        bool show = tags != null && tags.Length > 0;
        handTooltipTagRowRt.gameObject.SetActive(show);
        if (!show)
        {
            HideUnusedTagChips(0);
            ApplyHandTooltipBodyTopInset(CalculateHandTooltipBodyTopInset(false));
            return;
        }

        TMP_FontAsset font = ResolveUIFont();
        for (int i = 0; i < tags.Length; i++)
        {
            HandTooltipTagChipView chip = EnsureTagChip(i);
            ApplyTagChip(chip, tags[i], font);
            chip.root.SetActive(true);
        }

        HideUnusedTagChips(tags.Length);
        ApplyHandTooltipBodyTopInset(CalculateHandTooltipBodyTopInset(true));
    }

    private void HideHandTooltipAttributeTags()
    {
        RefreshHandTooltipAttributeTags(null);
    }

    private float CalculateHandTooltipBodyTopInset(bool hasTags)
    {
        const float padTop = 30f;
        const float titleH = 54f;
        const float titleGap = 8f;
        const float subtitleH = 46f;
        const float subtitleGap = 14f;
        float tagBlock = hasTags ? HandTooltipTagRowHeight + HandTooltipTagRowGapBelow : 0f;
        return padTop + titleH + titleGap + subtitleH + subtitleGap + tagBlock;
    }

    private void ApplyHandTooltipBodyTopInset(float bodyTop)
    {
        if (handTooltipBodyTmp == null) return;
        const float padH = 32f;
        const float padBottom = 28f;
        RectTransform bodyRt = handTooltipBodyTmp.rectTransform;
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(padH, padBottom);
        bodyRt.offsetMax = new Vector2(-padH, -bodyTop);
    }

    private HandTooltipTagChipView EnsureTagChip(int index)
    {
        while (handTooltipTagChipPool.Count <= index)
        {
            HandTooltipTagChipView chip = CreateTagChip(handTooltipTagRowRt);
            chip.root.SetActive(false);
            handTooltipTagChipPool.Add(chip);
        }

        return handTooltipTagChipPool[index];
    }

    private HandTooltipTagChipView CreateTagChip(Transform parent)
    {
        GameObject root = new GameObject("AttributeTag", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(0f, HandTooltipTagRowHeight);

        Image bg = root.GetComponent<Image>();
        bg.color = HandTooltipTagChipBg;
        bg.raycastTarget = false;

        Outline outline = root.AddComponent<Outline>();
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.effectColor = HandTooltipTagBorderFallback;

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(
            Mathf.RoundToInt(HandTooltipTagChipPadH),
            Mathf.RoundToInt(HandTooltipTagChipPadH),
            Mathf.RoundToInt(HandTooltipTagChipPadV),
            Mathf.RoundToInt(HandTooltipTagChipPadV));
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement selfLayout = root.AddComponent<LayoutElement>();
        selfLayout.minHeight = HandTooltipTagRowHeight;
        selfLayout.preferredHeight = HandTooltipTagRowHeight;

        TextMeshProUGUI labelTmp = CreateTagChipText(root.transform, ResolveUIFont());
        return new HandTooltipTagChipView
        {
            root = root,
            background = bg,
            outline = outline,
            labelTmp = labelTmp
        };
    }

    private static TextMeshProUGUI CreateTagChipText(Transform parent, TMP_FontAsset font)
    {
        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = HandTooltipTagFontSize;
        tmp.richText = true;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        LayoutElement layout = textGo.GetComponent<LayoutElement>();
        layout.minWidth = 24f;
        layout.preferredHeight = HandTooltipTagRowHeight - HandTooltipTagChipPadV * 2f;
        return tmp;
    }

    private static void ApplyTagChip(HandTooltipTagChipView chip, HandLongPressAttributeTag tag, TMP_FontAsset font)
    {
        if (chip?.labelTmp == null) return;
        if (font != null) chip.labelTmp.font = font;

        string label = string.IsNullOrWhiteSpace(tag.label) ? string.Empty : tag.label.Trim();
        string value = string.IsNullOrWhiteSpace(tag.value) ? string.Empty : tag.value.Trim();
        chip.labelTmp.text =
            "<color=#" + ColorUtility.ToHtmlStringRGB(HandTooltipTagLabelColor) + ">" + label + "</color> " +
            "<color=#" + ColorUtility.ToHtmlStringRGB(HandTooltipTagValueColor) + "><b>" + value + "</b></color>";

        Color border = tag.HasAccent ? Color.Lerp(tag.accentColor, Color.white, 0.18f) : HandTooltipTagBorderFallback;
        if (chip.outline != null)
            chip.outline.effectColor = border;
        if (chip.background != null)
        {
            Color bg = tag.HasAccent
                ? Color.Lerp(HandTooltipTagChipBg, tag.accentColor, 0.18f)
                : HandTooltipTagChipBg;
            bg.a = HandTooltipTagChipBg.a;
            chip.background.color = bg;
        }

        chip.labelTmp.ForceMeshUpdate();
        float textWidth = chip.labelTmp.preferredWidth + HandTooltipTagChipPadH * 2f;
        RectTransform rt = chip.root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Mathf.Max(72f, textWidth), HandTooltipTagRowHeight);
    }

    private void HideUnusedTagChips(int activeCount)
    {
        for (int i = activeCount; i < handTooltipTagChipPool.Count; i++)
        {
            if (handTooltipTagChipPool[i]?.root != null)
                handTooltipTagChipPool[i].root.SetActive(false);
        }
    }

    private void EnsureHandTooltipTagRow()
    {
        if (handTooltipTagRowRt != null || tooltipPanel == null) return;

        GameObject rowGo = new GameObject("AttributeTags", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(tooltipPanel, false);
        handTooltipTagRowRt = rowGo.GetComponent<RectTransform>();
        handTooltipTagRowRt.anchorMin = new Vector2(0f, 1f);
        handTooltipTagRowRt.anchorMax = new Vector2(1f, 1f);
        handTooltipTagRowRt.pivot = new Vector2(0.5f, 1f);

        const float padTop = 30f;
        const float titleH = 54f;
        const float titleGap = 8f;
        const float subtitleH = 46f;
        const float subtitleGap = 14f;
        float rowTop = padTop + titleH + titleGap + subtitleH + subtitleGap;
        handTooltipTagRowRt.offsetMin = new Vector2(HandTooltipTagRowPadH, -(rowTop + HandTooltipTagRowHeight));
        handTooltipTagRowRt.offsetMax = new Vector2(-HandTooltipTagRowPadH, -rowTop);

        HorizontalLayoutGroup rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = HandTooltipTagChipSpacing;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(0, 0, 0, 0);

        rowGo.SetActive(false);
    }
}

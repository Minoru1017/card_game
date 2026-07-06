using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>港灣實戰：林可姐教練立繪表情（見 HARBOR_COMBAT_COACH_GDD §3.5）。</summary>
public enum HarborCoachExpression
{
    Neutral,
    Alert,
    Serious,
    Encourage
}

public static class HarborCombatCoachExpressionCatalog
{
    private const string CoachNeutralResourcesPath = "UI/LinKeCoach/Linkk_Smile.jpeg";
    private const string CoachNeutralSliceName = "Linkk_Smile.jpeg_0";

    private static readonly Dictionary<HarborCoachExpression, Sprite> CachedSprites =
        new Dictionary<HarborCoachExpression, Sprite>();

    private static readonly Dictionary<string, HarborCoachExpression> HintToExpression =
        new Dictionary<string, HarborCoachExpression>
        {
            { "lethal_next_turn", HarborCoachExpression.Alert },
            { "discard_required", HarborCoachExpression.Neutral },
            { "weather_fire_rain", HarborCoachExpression.Serious },
            { "weather_holy_light", HarborCoachExpression.Encourage },
            { "weather_fog", HarborCoachExpression.Neutral },
            { "weather_gale", HarborCoachExpression.Encourage },
            { "hand_near_cap", HarborCoachExpression.Serious },
            { "threat_field", HarborCoachExpression.Alert },
            { "no_field_before_end", HarborCoachExpression.Encourage },
            { "heal_before_end", HarborCoachExpression.Encourage },
            { "harbor_pressure", HarborCoachExpression.Serious }
        };

    public static HarborCoachExpression ResolveExpression(string hintKey)
    {
        if (string.IsNullOrWhiteSpace(hintKey)) return HarborCoachExpression.Neutral;
        return HintToExpression.TryGetValue(hintKey, out HarborCoachExpression expression)
            ? expression
            : HarborCoachExpression.Neutral;
    }

    /// <summary>對戰教練立繪的預設圖：第一表情（Neutral，Linkk_Smile）；缺檔退回劇情立繪。</summary>
    public static Sprite ResolveNeutralOrFallback()
    {
        Sprite sprite = ResolveSprite(HarborCoachExpression.Neutral);
        return sprite != null ? sprite : TutorialPlotScriptFactory.GetLinKePortraitSprite();
    }

    public static void ApplyToPortrait(Image portraitImage, string hintKey)
    {
        if (portraitImage == null) return;

        HarborCoachExpression expression = ResolveExpression(hintKey);
        Sprite sprite = ResolveSprite(expression);
        if (sprite == null && expression != HarborCoachExpression.Neutral)
            sprite = ResolveSprite(HarborCoachExpression.Neutral);
        if (sprite == null)
            sprite = TutorialPlotScriptFactory.GetLinKePortraitSprite();

        portraitImage.sprite = sprite;
        portraitImage.color = Color.white;
        portraitImage.preserveAspect = true;
    }

    private static Sprite ResolveSprite(HarborCoachExpression expression)
    {
        if (CachedSprites.TryGetValue(expression, out Sprite cached) && cached != null)
            return cached;

        Sprite loaded = null;

        UiSpriteLibrary library = UiSpriteLibrary.Instance;
        if (library != null)
            loaded = library.GetCoachExpression(expression);

        if (loaded == null && expression == HarborCoachExpression.Neutral)
            loaded = ResolveCoachNeutralSlice(Resources.LoadAll<Sprite>(CoachNeutralResourcesPath));

        if (loaded == null)
            Debug.LogWarning(
                $"HarborCombatCoachExpressionCatalog: 教練表情 '{expression}' 不在 UiSpriteLibrary，" +
                "請重跑 Tools/UI/Create or Refresh UI Sprite Library。");

        CachedSprites[expression] = loaded;
        return loaded;
    }

    private static Sprite ResolveCoachNeutralSlice(Sprite[] slices)
    {
        if (slices == null || slices.Length == 0)
            return null;

        Sprite fallback = null;
        for (int i = 0; i < slices.Length; i++)
        {
            Sprite slice = slices[i];
            if (slice == null)
                continue;
            fallback ??= slice;
            if (string.Equals(slice.name, CoachNeutralSliceName, System.StringComparison.Ordinal) ||
                string.Equals(slice.name, "Linkk_Smile.jpeg", System.StringComparison.Ordinal))
                return slice;
        }

        return fallback;
    }
}

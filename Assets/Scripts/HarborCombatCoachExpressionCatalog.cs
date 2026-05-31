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
    private const string ResourcePrefix = "UI/LinKeCoach/linke_";

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

        string fileName = expression switch
        {
            HarborCoachExpression.Alert => "alert",
            HarborCoachExpression.Serious => "serious",
            HarborCoachExpression.Encourage => "encourage",
            _ => "neutral"
        };

        Sprite loaded = Resources.Load<Sprite>(ResourcePrefix + fileName);
        CachedSprites[expression] = loaded;
        return loaded;
    }
}

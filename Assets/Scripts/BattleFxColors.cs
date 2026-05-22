using UnityEngine;

/// <summary>對戰場景特效配色（<see cref="BATTLE_FX_COLOR_SPEC.md"/>）。</summary>
public static class BattleFxColors
{
    // §3.1 — global / hurt / dim
    public static readonly Color FxDim = BattleUiColors.WithAlpha(BattleUiColors.Hex("#0A0305"), 0.52f);
    /// <summary>法術 overlay 全螢幕 dim（與 Pause／天氣預報同 <see cref="BattleUiColors.DimHeavy"/>）。</summary>
    public static readonly Color SpellCastDimImage = BattleUiColors.DimHeavy;
    public static readonly Color VignetteWarm = BattleUiColors.WithAlpha(BattleUiColors.Hex("#1A1412"), 0.82f);
    public static readonly Color VignetteWarmTopBottom = BattleUiColors.WithAlpha(BattleUiColors.Hex("#1A1412"), 0.85f);
    public static readonly Color VignetteWarmSoft = BattleUiColors.WithAlpha(BattleUiColors.Hex("#2A221E"), 0.72f);

    public const float HurtMonoFlashPeakAlpha = 0.90f;

    public static readonly Color HurtFlash = BattleUiColors.Hex("#C07060");
    public static readonly Color HurtFlashPeak = BattleUiColors.WithAlpha(HurtFlash, 0.82f);
    public static readonly Color CounterFlash = BattleUiColors.Hex("#9AD4FF");
    public static readonly Color CounterFlashPeak = BattleUiColors.WithAlpha(CounterFlash, 0.78f);
    public static readonly Color CounterSlashCore = BattleUiColors.WithAlpha(BattleUiColors.Hex("#B8E4FF"), 0.95f);
    public static readonly Color CounterRingPeak = BattleUiColors.WithAlpha(CounterFlash, 0.62f);
    public static readonly Color CounterLabelBg = BattleUiColors.WithAlpha(BattleUiColors.Hex("#050A12"), 0.97f);
    public static readonly Color CounterLabelBorder = BattleUiColors.Hex("#FFFFFF");
    public static readonly Color CounterLabelText = BattleUiColors.Hex("#FFFFFF");
    public static readonly Color CounterLabelShadow = BattleUiColors.WithAlpha(Color.black, 0.62f);
    public static readonly Color CounterLabelGlow = BattleUiColors.WithAlpha(CounterFlash, 0.55f);

    public static readonly Color DamageLabelBg = BattleUiColors.WithAlpha(BattleUiColors.Hex("#1A0808"), 0.96f);
    public static readonly Color DamageLabelBorder = BattleUiColors.Hex("#FFD4CC");
    public static readonly Color DamageLabelText = BattleUiColors.Hex("#FFF5F0");
    public static readonly Color DamageLabelGlow = BattleUiColors.WithAlpha(HurtFlash, 0.58f);
    public static readonly Color DamageLabelShadow = BattleUiColors.WithAlpha(Color.black, 0.65f);
    public static readonly Color CounterDamageLabelBorder = BattleUiColors.Hex("#D8F4FF");
    public static readonly Color CounterDamageLabelText = CounterLabelText;
    public static readonly Color CounterDamageLabelGlow = CounterLabelGlow;

    public static readonly Color RestrictionBadgeBg = BattleUiColors.WithAlpha(BattleUiColors.Hex("#1C1408"), 0.94f);
    public static readonly Color RestrictionBadgeBorderAttack = BattleUiColors.Hex("#FFD98A");
    public static readonly Color RestrictionBadgeBorderCounter = BattleUiColors.Hex("#C9B4FF");
    public static readonly Color RestrictionBadgePrimary = BattleUiColors.Hex("#FFF6E8");
    public static readonly Color RestrictionBadgeSecondary = BattleUiColors.Hex("#FFE0A8");
    public static readonly Color RestrictionBadgeGlow = BattleUiColors.WithAlpha(BattleUiColors.Hex("#E8BB6A"), 0.42f);
    public static readonly Color RestrictionBadgeShadow = BattleUiColors.WithAlpha(Color.black, 0.58f);

    public static readonly Color HurtMonoPeak = BattleUiColors.WithAlpha(BattleUiColors.Hex("#3D3835"), 0.90f);

    public static readonly Color HealFloat = BattleUiColors.AllyHp;
    public static readonly Color HealGlowOuter = BattleUiColors.WithAlpha(HealFloat, 0.18f);
    public static readonly Color HealGlowInner = BattleUiColors.WithAlpha(HealFloat, 0.28f);
    public static readonly Color FxShadow = BattleUiColors.WithAlpha(Color.black, 0.32f);

    // §3.2 — weather base tints
    public static readonly Color WeatherHolyBase = BattleUiColors.WithAlpha(BattleUiColors.PanelMilk, 0.04f);
    public static readonly Color WeatherFogBase = BattleUiColors.WithAlpha(BattleUiColors.DeckTop, 0.10f);
    public static readonly Color WeatherGaleBase = BattleUiColors.WithAlpha(BattleUiColors.Hex("#2E3542"), 0.09f);
    public static readonly Color WeatherFireBase = BattleUiColors.WithAlpha(BattleUiColors.Hex("#E8BB6A"), 0.03f);

    // §3.3 — weather edges / particles (RGB anchors; use WithAlpha / Random* at runtime)
    public static readonly Color WeatherHolyEdgeRgb = BattleUiColors.PanelMilk;
    public static readonly Color WeatherHolyDustGoldRgb = BattleUiColors.TurnPlayer;
    public static readonly Color WeatherHolyDustMilkRgb = BattleUiColors.PanelMilk;

    public static readonly Color WeatherFogEdgeRgb = BattleUiColors.BtnSecondary;
    public static readonly Color WeatherFogWaveRgb = BattleUiColors.DeckTop;
    public static readonly Color WeatherFogFoamRgb = BattleUiColors.BtnSecondaryText;
    public static readonly Color WeatherFogSilhouetteRgb = BattleUiColors.Hex("#2E3542");

    public static readonly Color WeatherGaleNightRgb = BattleUiColors.Hex("#1F2824");
    public static readonly Color WeatherGaleLeafGreenRgb = BattleUiColors.AllyHp;
    public static readonly Color WeatherGaleLeafBrownRgb = BattleUiColors.BtnPrimary;
    public static readonly Color WeatherGaleLeafAmberRgb = BattleUiColors.Hex("#E8BB6A");
    public static readonly Color WeatherGaleLeafRustRgb = BattleUiColors.FoeHp;
    public static readonly Color WeatherGaleWindRgb = BattleUiColors.BtnSecondaryText;

    public static readonly Color WeatherFireDropRgb = BattleUiColors.Hex("#D49458");

    // §3.4 — opening, draw, field, gaze
    public static readonly Color DiceFace = BattleUiColors.PanelMilk;
    public static readonly Color DicePipOff = BattleUiColors.WithAlpha(BattleUiColors.Hex("#8B7355"), 0.35f);
    public static readonly Color DicePipOn = BattleUiColors.Ink;

    public static readonly Color DrawGhostBack = BattleUiColors.DeckTop;

    public static readonly Color FieldHaloOuter = BattleUiColors.WithAlpha(BattleUiColors.BtnSecondary, 0.16f);
    public static readonly Color FieldHaloInner = BattleUiColors.WithAlpha(BattleUiColors.AllyLabel, 0.23f);
    public static readonly Color FieldHaloCore = BattleUiColors.WithAlpha(BattleUiColors.BtnSecondaryText, 0.065f);

    public static readonly Color FieldHpHurt = HurtFlash;

    public static readonly Color GazeGlareRgb = BattleUiColors.Hex("#8A7A9E");
    public static readonly Color GazeScleraRgb = BattleUiColors.PanelMilk;
    public static readonly Color GazePupilRgb = BattleUiColors.Hex("#382624");

    public const float GazeGlarePeakAlpha = 0.45f;
    public const float GazeScleraPeakAlpha = 0.96f;

    public static Color WithAlpha(Color c, float a) => BattleUiColors.WithAlpha(c, a);

    public static Color GazeGlare(float alpha) =>
        WithAlpha(GazeGlareRgb, Mathf.Min(alpha, GazeGlarePeakAlpha));

    public static Color GazeSclera(float alpha) =>
        WithAlpha(GazeScleraRgb, Mathf.Min(alpha, GazeScleraPeakAlpha));

    public static Color GazeScleraStrike(float strike01) =>
        Color.Lerp(GazeSclera(GazeScleraPeakAlpha), WithAlpha(HurtFlash, GazeScleraPeakAlpha), strike01 * 0.35f);

    public static Color HolyEdge(float alpha) => WithAlpha(WeatherHolyEdgeRgb, alpha);

    public static Color FogEdge(float alpha) => WithAlpha(WeatherFogEdgeRgb, alpha);

    public static Color GaleNightEdge(float alpha) => WithAlpha(WeatherGaleNightRgb, alpha);

    public static Color RandomHolyDust()
    {
        Color rgb = Random.value < 0.82f ? WeatherHolyDustGoldRgb : WeatherHolyDustMilkRgb;
        return WithAlpha(rgb, Random.Range(0.04f, 0.095f));
    }

    public static Color RandomFogWave() => WithAlpha(WeatherFogWaveRgb, Random.Range(0.08f, 0.14f));

    public static Color RandomFogFoam() => WithAlpha(WeatherFogFoamRgb, Random.Range(0.08f, 0.16f));

    public static Color RandomFogSilhouette() => WithAlpha(WeatherFogSilhouetteRgb, Random.Range(0.22f, 0.32f));

    public static Color RandomGaleWind() => WithAlpha(WeatherGaleWindRgb, Random.Range(0.08f, 0.16f));

    public static Color RandomFireDrop() => WithAlpha(WeatherFireDropRgb, Random.Range(0.17f, 0.32f));

    /// <summary>穿堂微風：紙屑／卷軸碎屑（奶油／軟木色系）。</summary>
    public static Color RandomHallDraftPaper()
    {
        float roll = Random.value;
        Color rgb;
        float aMin;
        float aMax;
        if (roll < 0.45f)
        {
            rgb = BattleUiColors.PanelMilk;
            aMin = 0.20f;
            aMax = 0.38f;
        }
        else if (roll < 0.78f)
        {
            rgb = BattleUiColors.PanelCream;
            aMin = 0.18f;
            aMax = 0.34f;
        }
        else
        {
            rgb = BattleUiColors.BtnPrimaryText;
            aMin = 0.16f;
            aMax = 0.30f;
        }

        return WithAlpha(rgb, Random.Range(aMin, aMax));
    }

    /// <summary>§5.2.1 烈風葉色權重：綠 42%／褐 28%／琥珀 22%／赭 8%。</summary>
    public static Color RandomGaleLeaf()
    {
        float roll = Random.value;
        Color rgb;
        float aMin;
        float aMax;
        if (roll < 0.42f)
        {
            rgb = WeatherGaleLeafGreenRgb;
            aMin = 0.22f;
            aMax = 0.42f;
        }
        else if (roll < 0.70f)
        {
            rgb = WeatherGaleLeafBrownRgb;
            aMin = 0.20f;
            aMax = 0.38f;
        }
        else if (roll < 0.92f)
        {
            rgb = WeatherGaleLeafAmberRgb;
            aMin = 0.20f;
            aMax = 0.36f;
        }
        else
        {
            rgb = WeatherGaleLeafRustRgb;
            aMin = 0.18f;
            aMax = 0.34f;
        }

        return WithAlpha(rgb, Random.Range(aMin, aMax));
    }
}

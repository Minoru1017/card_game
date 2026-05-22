/// <summary>訓練廳室內「場地效果」顯示名稱（對應 <see cref="BattleSimulationManager"/> 內之四種效果，規則不變）。</summary>
public static class BattleWeatherLabels
{
    /// <summary>原 FireRain：爐火餘燼飄落。</summary>
    public const string EmberHearth = "爐心飛燼";

    /// <summary>原 HolyLight：枝形燈暖光與浮塵。</summary>
    public const string WarmLamplight = "暖燈浮塵";

    /// <summary>原 Fog：訓練廳薄霧／薰香霧氣。</summary>
    public const string TrainingMist = "訓練薄霧";

    /// <summary>原 Gale：穿堂風、紙屑飛揚。</summary>
    public const string HallDraft = "穿堂微風";

    public static readonly string[] ForecastRollPool =
    {
        EmberHearth,
        WarmLamplight,
        TrainingMist,
        HallDraft
    };
}

/// <summary>與 Manager 內 private enum 順序一致，供 FX 比對。</summary>
public static class BattleWeatherKind
{
    public const int None = 0;
    public const int FireRain = 1;
    public const int HolyLight = 2;
    public const int Fog = 3;
    public const int Gale = 4;
}

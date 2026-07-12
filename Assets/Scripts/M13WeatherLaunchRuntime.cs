/// <summary>M-1-3 冷爐戰開局天氣偏好（由 SceneLoader 寫入，BattleSimulationManager 開戰時消費）。</summary>
public static class M13WeatherLaunchRuntime
{
    public static bool PreferFireRain { get; private set; }
    public static bool PreferHolyLight { get; private set; }
    public static bool PreferFog { get; private set; }

    public static void Clear()
    {
        PreferFireRain = false;
        PreferHolyLight = false;
        PreferFog = false;
    }

    public static void SetFirstWeatherFireRain()
    {
        Clear();
        PreferFireRain = true;
    }

    public static void SetFirstWeatherHolyLight()
    {
        Clear();
        PreferHolyLight = true;
    }

    public static void SetFirstWeatherFog()
    {
        Clear();
        PreferFog = true;
    }
}

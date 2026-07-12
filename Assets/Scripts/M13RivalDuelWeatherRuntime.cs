/// <summary>M-1-3 分波對決天氣腳本：R3 預報→R4 穿堂微風、R7 預報→R8 蒼潮。</summary>
public static class M13RivalDuelWeatherRuntime
{
    public enum ScriptedWeather
    {
        None,
        Gale,
        Fog
    }

    public static bool TryPickScriptedForecast(int currentRound, out ScriptedWeather weather)
    {
        switch (currentRound)
        {
            case 3:
                weather = ScriptedWeather.Gale;
                return true;
            case 7:
                weather = ScriptedWeather.Fog;
                return true;
            default:
                weather = ScriptedWeather.None;
                return false;
        }
    }
}

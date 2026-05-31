using UnityEngine;

/// <summary>
/// Main Plot 為 Single 載入時場內可能沒有 DataManager；教學牌組發放前確保可讀寫 PlayerData。
/// </summary>
public static class TutorialPlotPlayerDataBridge
{
    private const string HostName = "TutorialPlotPlayerDataHost";
    private static GameObject host;

    public static PlayerData EnsureWritable()
    {
        PlayerData canonical = PlayerData.ResolveCanonical();
        if (canonical != null)
        {
            canonical.LoadPlayerData();
            return canonical;
        }

        if (host == null)
        {
            host = GameObject.Find(HostName);
            if (host == null)
            {
                host = new GameObject(HostName);
                Object.DontDestroyOnLoad(host);
            }
        }

        PlayerData pd = host.GetComponent<PlayerData>();
        if (pd == null)
            pd = host.AddComponent<PlayerData>();

        pd.LoadPlayerData();
        return pd;
    }
}

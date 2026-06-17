using UnityEngine;

/// <summary>
/// Main Plot 為 Single 載入時場內可能沒有 DataManager；教學牌組發放前確保可讀寫 PlayerData。
/// </summary>
public static class TutorialPlotPlayerDataBridge
{
    public static PlayerData EnsureWritable() => PlayerData.EnsureWritable();
}

using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>依 CD id 解析鬥鳥 BGM；避免查表失敗時誤播預設《港灣練習帶》。</summary>
public static class BirdDuelBgmResolver
{
    public static AudioClip Resolve(string cdId)
    {
        if (TryResolve(cdId, out AudioClip clip))
            return clip;
        return null;
    }

    public static bool TryResolve(string cdId, out AudioClip clip)
    {
        clip = null;
        if (string.IsNullOrWhiteSpace(cdId))
            cdId = BirdDuelCdCatalog.DefaultCdId;
        else
            cdId = cdId.Trim();

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null && library.TryGetBirdDuelCdBgm(cdId, out clip))
            return clip != null;

#if UNITY_EDITOR
        clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ResolveEditorAssetPath(cdId));
        return clip != null;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    public static string ResolveEditorAssetPath(string cdId)
    {
        if (string.Equals(cdId, "court_march", StringComparison.OrdinalIgnoreCase))
            return FightingBirdGameSceneController.StampedeAssetPath;
        if (BirdDuelRhythmChart.IsMorningPrayer(cdId))
            return FightingBirdGameSceneController.MorningPrayerAssetPath;
        if (BirdDuelRhythmChart.IsRiverForkWave(cdId))
            return FightingBirdGameSceneController.RiverForkWaveAssetPath;
        return FightingBirdGameSceneController.ComeAgainAssetPath;
    }
#endif
}

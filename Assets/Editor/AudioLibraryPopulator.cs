using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 自動建立 / 重新整理 Assets/Resources/AudioLibrary.asset：
///   - 掃描 Resources/NPC voice/ 下所有 AudioClip，以檔名為 id 填入 npcVoices。
///   - 依各播放器的 Resources 路徑常數填入 BGM / 選單點擊 / 打字音效。
/// 省去手動拖曳；之後新增語音再重跑一次即可。
/// 選單：Tools/Audio/Create or Refresh Audio Library
/// </summary>
public static class AudioLibraryPopulator
{
    private const string LibraryAssetPath = "Assets/Resources/AudioLibrary.asset";
    private const string NpcVoiceResourcesFolder = "NPC voice";

    [MenuItem("Tools/Audio/Create or Refresh Audio Library")]
    public static void CreateOrRefresh()
    {
        AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryAssetPath);
        if (library == null)
        {
            EnsureResourcesFolder();
            library = ScriptableObject.CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryAssetPath);
            Debug.Log($"AudioLibraryPopulator: 已建立新資產 {LibraryAssetPath}");
        }

        AudioLibrary.NamedAudioClip[] voices = CollectNpcVoices();
        library.EditorSetVoices(voices);

        // BGM/SFX 實體檔在 Assets/Music/（不在 Resources），用 AssetDatabase 依資產路徑載入。
        AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(PlotBackgroundMusicPlayer.EnchantedValleyAssetPath);
        AudioClip hallBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(HallBackgroundMusicPlayer.WhatFloorAssetPath);
        AudioClip buildbeckBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BuildbeckBackgroundMusicPlayer.EtherealDreamsAssetPath);
        AudioClip cardStoreBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(CardStoreBackgroundMusicPlayer.AdventuryMoodyAssetPath);
        AudioClip menuClick = AssetDatabase.LoadAssetAtPath<AudioClip>(PlotMenuClickSfx.MenuClickClipAssetPath);
        AudioClip typing = AssetDatabase.LoadAssetAtPath<AudioClip>(PlotDialogueTypewriterSfx.TypingClipAssetPath);
        library.EditorSetSingletons(bgm, menuClick, typing);
        library.EditorSetHallBgm(hallBgm);
        library.EditorSetBuildbeckBgm(buildbeckBgm);
        library.EditorSetCardStoreBgm(cardStoreBgm);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"AudioLibraryPopulator: 填入 {voices.Length} 個 NPC 語音；" +
            $"BGM={(bgm != null)}, HallBGM={(hallBgm != null)}, BuildbeckBGM={(buildbeckBgm != null)}, " +
            $"CardStoreBGM={(cardStoreBgm != null)}, " +
            $"MenuClick={(menuClick != null)}, Typing={(typing != null)} → {LibraryAssetPath}");

        if (bgm == null || hallBgm == null || buildbeckBgm == null || cardStoreBgm == null || menuClick == null || typing == null)
            Debug.LogWarning("AudioLibraryPopulator: 有單一音軌找不到，請確認對應 Resources 路徑常數是否正確。");
    }

    private static AudioLibrary.NamedAudioClip[] CollectNpcVoices()
    {
        var voices = new List<AudioLibrary.NamedAudioClip>();
        string folderToken = "/Resources/" + NpcVoiceResourcesFolder + "/";

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf(folderToken, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                continue;

            voices.Add(new AudioLibrary.NamedAudioClip
            {
                id = Path.GetFileNameWithoutExtension(path),
                clip = clip,
            });
        }

        return voices
            .OrderBy(v => v.id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }
}

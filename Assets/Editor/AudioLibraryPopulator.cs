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
        AudioClip birdDuelBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(FightingBirdGameSceneController.ComeAgainAssetPath);
        AudioClip courtMarchBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(FightingBirdGameSceneController.StampedeAssetPath);
        AudioClip morningPrayerBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(FightingBirdGameSceneController.MorningPrayerAssetPath);
        AudioClip birdDuelHitSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(BirdDuelHitSfxBank.SourceAssetPath);
        AudioClip menuClick = AssetDatabase.LoadAssetAtPath<AudioClip>(PlotMenuClickSfx.MenuClickClipAssetPath);
        AudioClip typing = AssetDatabase.LoadAssetAtPath<AudioClip>(PlotDialogueTypewriterSfx.TypingClipAssetPath);
        AudioClip monsterCardAttack = AssetDatabase.LoadAssetAtPath<AudioClip>(BattleFieldMonsterAttackSfx.AssetPath);
        AudioClip monsterCardCounterattack = AssetDatabase.LoadAssetAtPath<AudioClip>(BattleFieldMonsterCounterattackSfx.AssetPath);
        library.EditorSetSingletons(bgm, menuClick, typing);
        library.EditorSetHallBgm(hallBgm);
        library.EditorSetBuildbeckBgm(buildbeckBgm);
        library.EditorSetCardStoreBgm(cardStoreBgm);
        library.EditorSetBirdDuelBgm(birdDuelBgm);
        library.EditorSetBirdDuelHitSfxSource(birdDuelHitSfx);
        library.EditorSetMonsterCardAttackSfx(monsterCardAttack);
        library.EditorSetMonsterCardCounterattackSfx(monsterCardCounterattack);
        SyncMonsterCardAttackResourcesCopy();
        SyncMonsterCardCounterattackResourcesCopy();
        BattleFieldMonsterAttackSfx.EditorInvalidateCachedClip();
        BattleFieldMonsterCounterattackSfx.EditorInvalidateCachedClip();
        library.EditorSetBirdDuelCdBgms(new[]
        {
            new AudioLibrary.NamedAudioClip { id = "court_march", clip = courtMarchBgm },
            new AudioLibrary.NamedAudioClip { id = BirdDuelRhythmChart.MorningPrayerCdId, clip = morningPrayerBgm }
        });

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"AudioLibraryPopulator: 填入 {voices.Length} 個 NPC 語音；" +
            $"BGM={(bgm != null)}, HallBGM={(hallBgm != null)}, BuildbeckBGM={(buildbeckBgm != null)}, " +
            $"CardStoreBGM={(cardStoreBgm != null)}, BirdDuelBGM={(birdDuelBgm != null)}, " +
            $"CourtMarchBGM={(courtMarchBgm != null)}, MorningPrayerBGM={(morningPrayerBgm != null)}, BirdDuelHitSfx={(birdDuelHitSfx != null)}, " +
            $"MonsterCardAttack={(monsterCardAttack != null)}, MonsterCardCounterattack={(monsterCardCounterattack != null)}, " +
            $"MenuClick={(menuClick != null)}, Typing={(typing != null)} → {LibraryAssetPath}");

        if (bgm == null || hallBgm == null || buildbeckBgm == null || cardStoreBgm == null || birdDuelBgm == null || menuClick == null || typing == null)
            Debug.LogWarning("AudioLibraryPopulator: 有單一音軌找不到，請確認對應 Resources 路徑常數是否正確。");
    }

    [MenuItem("Tools/Audio/Sync Monster Card Attack SFX")]
    public static void SyncMonsterCardAttackSfxOnly()
    {
        SyncMonsterCardAttackResourcesCopy();
        BattleFieldMonsterAttackSfx.EditorInvalidateCachedClip();
        AssetDatabase.Refresh();
        Debug.Log("AudioLibraryPopulator: 已同步 Monster Card Attack → Resources/Music。");
    }

    [MenuItem("Tools/Audio/Sync Monster Card Counterattack SFX")]
    public static void SyncMonsterCardCounterattackSfxOnly()
    {
        SyncMonsterCardCounterattackResourcesCopy();
        BattleFieldMonsterCounterattackSfx.EditorInvalidateCachedClip();
        AssetDatabase.Refresh();
        Debug.Log("AudioLibraryPopulator: 已同步 Monster Card Counterattack → Resources/Music。");
    }

    private static void SyncMonsterCardCounterattackResourcesCopy()
    {
        const string destinationPath = "Assets/Resources/Music/Monster Card Counterattack.mp3";
        string sourceFullPath = Path.GetFullPath(BattleFieldMonsterCounterattackSfx.AssetPath);
        string destinationFullPath = Path.GetFullPath(destinationPath);
        if (!File.Exists(sourceFullPath))
        {
            Debug.LogWarning($"AudioLibraryPopulator: 找不到來源音檔 {BattleFieldMonsterCounterattackSfx.AssetPath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationFullPath) ?? string.Empty);
        File.Copy(sourceFullPath, destinationFullPath, overwrite: true);
    }

    private static void SyncMonsterCardAttackResourcesCopy()
    {
        const string destinationPath = "Assets/Resources/Music/Monster Card Attack.mp3";
        string sourceFullPath = Path.GetFullPath(BattleFieldMonsterAttackSfx.AssetPath);
        string destinationFullPath = Path.GetFullPath(destinationPath);
        if (!File.Exists(sourceFullPath))
        {
            Debug.LogWarning($"AudioLibraryPopulator: 找不到來源音檔 {BattleFieldMonsterAttackSfx.AssetPath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationFullPath) ?? string.Empty);
        File.Copy(sourceFullPath, destinationFullPath, overwrite: true);
    }

    private static AudioLibrary.NamedAudioClip[] CollectNpcVoices()
    {
        // 掃描任何 NPC voice 資料夾（含 Resources 與工作資料夾），避免只放在非 Resources 的語音漏登記。
        // AudioLibrary 以直接資產參考序列化，毋須位於 Resources 即可在執行期取用。
        string folderToken = "/" + NpcVoiceResourcesFolder + "/";
        string resourcesToken = "/Resources/" + NpcVoiceResourcesFolder + "/";

        var clipById = new Dictionary<string, AudioClip>();
        var idIsFromResources = new Dictionary<string, bool>();

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf(folderToken, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                continue;

            string id = Path.GetFileNameWithoutExtension(path);
            bool isResources = path.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase) >= 0;

            // 同一 id 有重複時優先採用 Resources 副本；否則沿用先找到的。
            bool exists = clipById.TryGetValue(id, out _);
            if (!exists || (isResources && !idIsFromResources[id]))
            {
                clipById[id] = clip;
                idIsFromResources[id] = isResources;
            }
        }

        var voices = new List<AudioLibrary.NamedAudioClip>(clipById.Count);
        foreach (KeyValuePair<string, AudioClip> entry in clipById)
            voices.Add(new AudioLibrary.NamedAudioClip { id = entry.Key, clip = entry.Value });

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

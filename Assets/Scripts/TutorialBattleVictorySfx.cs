using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>1-1 教學對戰玩家勝利時播放的音效（Battle victory）。</summary>
public class TutorialBattleVictorySfx : MonoBehaviour
{
    public const string VictoryClipResourcesPath = "Music/Battle victory";

#if UNITY_EDITOR
    public const string VictoryClipAssetPath = "Assets/Music/Battle victory.mp3";
#endif

    [SerializeField] private AudioClip victoryClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource sfxSource;

    public static void PlayIfIntroTutorialBattle()
    {
        if (!BattleLaunchContext.IsIntroTutorialBattle)
            return;

        TutorialBattleVictorySfx player = EnsureOnMainCamera();
        player?.PlayVictory();
    }

    public static TutorialBattleVictorySfx EnsureOnMainCamera()
    {
        TutorialBattleVictorySfx existing = FindInLoadedBattleScene();
        if (existing != null)
            return existing;

        Camera cam = Camera.main;
        if (cam == null || !TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(cam.gameObject.scene.name))
            return null;

        return cam.gameObject.AddComponent<TutorialBattleVictorySfx>();
    }

    public static TutorialBattleVictorySfx FindInLoadedBattleScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(scene.name))
                continue;

            TutorialBattleVictorySfx sfx = FindInScene(scene);
            if (sfx != null)
                return sfx;
        }

        return null;
    }

    public static TutorialBattleVictorySfx FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            TutorialBattleVictorySfx sfx = roots[r].GetComponentInChildren<TutorialBattleVictorySfx>(true);
            if (sfx != null)
                return sfx;
        }

        return null;
    }

    public void PlayVictory()
    {
        EnsureClipResolved();
        EnsureSfxSource();
        if (victoryClip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(victoryClip, volume);
    }

    private void Awake()
    {
        EnsureClipResolved();
        EnsureSfxSource();
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
            return;

        Transform child = transform.Find("TutorialBattleVictorySfxSource");
        if (child != null)
        {
            sfxSource = child.GetComponent<AudioSource>();
            if (sfxSource != null)
                return;
        }

        var sourceGo = new GameObject("TutorialBattleVictorySfxSource");
        sourceGo.transform.SetParent(transform, false);
        sfxSource = sourceGo.AddComponent<AudioSource>();
        ConfigureSfxSource(sfxSource);
    }

    private static void ConfigureSfxSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 64;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.ignoreListenerPause = true;
        source.mute = false;
    }

    private void EnsureClipResolved()
    {
        if (victoryClip != null)
            return;

        victoryClip = Resources.Load<AudioClip>(VictoryClipResourcesPath);
#if UNITY_EDITOR
        if (victoryClip == null)
            victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>(VictoryClipAssetPath);
#endif
    }
}

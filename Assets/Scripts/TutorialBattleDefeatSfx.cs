using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>1-1 教學對戰玩家失敗時播放的音效（Battle failed）。</summary>
public class TutorialBattleDefeatSfx : MonoBehaviour
{
    public const string DefeatClipResourcesPath = "Music/Battle failed";

#if UNITY_EDITOR
    public const string DefeatClipAssetPath = "Assets/Music/Battle failed.mp3";
#endif

    [SerializeField] private AudioClip defeatClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource sfxSource;

    public static void PlayIfIntroTutorialBattle()
    {
        if (!BattleLaunchContext.IsIntroTutorialBattle)
            return;

        TutorialBattleDefeatSfx player = EnsureOnMainCamera();
        player?.PlayDefeat();
    }

    public static TutorialBattleDefeatSfx EnsureOnMainCamera()
    {
        TutorialBattleDefeatSfx existing = FindInLoadedBattleScene();
        if (existing != null)
            return existing;

        Camera cam = Camera.main;
        if (cam == null || !TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(cam.gameObject.scene.name))
            return null;

        return cam.gameObject.AddComponent<TutorialBattleDefeatSfx>();
    }

    public static TutorialBattleDefeatSfx FindInLoadedBattleScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || !TutorialBattleBackgroundMusicPlayer.IsSupportedBattleScene(scene.name))
                continue;

            TutorialBattleDefeatSfx sfx = FindInScene(scene);
            if (sfx != null)
                return sfx;
        }

        return null;
    }

    public static TutorialBattleDefeatSfx FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            TutorialBattleDefeatSfx sfx = roots[r].GetComponentInChildren<TutorialBattleDefeatSfx>(true);
            if (sfx != null)
                return sfx;
        }

        return null;
    }

    public void PlayDefeat()
    {
        EnsureClipResolved();
        EnsureSfxSource();
        if (defeatClip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(defeatClip, volume);
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

        Transform child = transform.Find("TutorialBattleDefeatSfxSource");
        if (child != null)
        {
            sfxSource = child.GetComponent<AudioSource>();
            if (sfxSource != null)
                return;
        }

        var sourceGo = new GameObject("TutorialBattleDefeatSfxSource");
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
        if (defeatClip != null)
            return;

        defeatClip = Resources.Load<AudioClip>(DefeatClipResourcesPath);
#if UNITY_EDITOR
        if (defeatClip == null)
            defeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefeatClipAssetPath);
#endif
    }
}

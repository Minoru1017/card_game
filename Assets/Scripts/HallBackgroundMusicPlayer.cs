using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Hall scene BGM player（idokay - What Floor）。
/// Adds itself to the hall Main Camera at runtime and plays the clip registered in AudioLibrary.
/// </summary>
public sealed class HallBackgroundMusicPlayer : MonoBehaviour
{
#if UNITY_EDITOR
    // BGM/SFX 實體檔在 Assets/Music/，不放 Resources；AudioLibraryPopulator 依此路徑填表。
    public const string WhatFloorAssetPath = "Assets/Music/idokay - What Floor.mp3";
#endif

    [SerializeField] private AudioClip hallBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterHallBgmSceneGuard()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.isLoaded)
            OnSceneLoaded(active, LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsHallScene(scene.name))
        {
            StoryProgressBackgroundMusicPlayer.StopAll();
            TutorialBattleBackgroundMusicPlayer.StopAll();
            PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
            EnsureInScene(scene)?.PlayHallBgm();
            return;
        }

        StopAll();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (IsHallScene(scene.name))
            StopAll();
    }

    public static void StopAll()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
                StopInScene(scene);
        }
    }

    public static void StopInScene(Scene scene)
    {
        HallBackgroundMusicPlayer player = FindInScene(scene);
        if (player != null)
            player.StopHallBgm();
    }

    public static HallBackgroundMusicPlayer EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsHallScene(scene.name))
            return null;

        HallBackgroundMusicPlayer existing = FindInScene(scene);
        if (existing != null)
            return existing;

        Camera cam = FindMainCameraInScene(scene);
        if (cam == null)
            return null;

        return cam.gameObject.AddComponent<HallBackgroundMusicPlayer>();
    }

    public static HallBackgroundMusicPlayer FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            HallBackgroundMusicPlayer player =
                roots[r].GetComponentInChildren<HallBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public static bool IsHallScene(string sceneName) => sceneName == StoryProgressSession.HallSceneName;

    public void PlayHallBgm()
    {
        if (!IsOnHallScene())
            return;

        shouldKeepPlaying = true;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayWhenReady());
    }

    public void StopHallBgm()
    {
        shouldKeepPlaying = false;
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void Awake()
    {
        EnsureBgmSource();
        ResolveClipIfMissing();
    }

    private void OnDestroy() => StopHallBgm();

    private IEnumerator CoPlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();
        EnsureBgmSource();
        ResolveClipIfMissing();

        AudioClip clip = hallBgmClip;
        if (clip == null)
        {
            Debug.LogWarning("HallBackgroundMusicPlayer: no Hall BGM clip assigned.");
            yield break;
        }

        EnsureClipLoaded(clip);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.time = 0f;

        bool started = false;
        for (int i = 0; i < 60; i++)
        {
            audioSource.Play();
            if (audioSource.isPlaying)
            {
                started = true;
                break;
            }

            if (i == 1 || i == 15)
                EnsureClipLoaded(clip);

            yield return null;
        }

        if (!started)
            Debug.LogWarning("HallBackgroundMusicPlayer: Play() did not start. loadState=" + clip.loadState);

        playRoutine = null;
    }

    private void EnsureBgmSource()
    {
        if (audioSource != null)
            return;

        Transform child = transform.Find("HallBgmSource");
        if (child != null)
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                ConfigureBgmSource(audioSource);
                return;
            }
        }

        var sourceGo = new GameObject("HallBgmSource");
        sourceGo.transform.SetParent(transform, false);
        audioSource = sourceGo.AddComponent<AudioSource>();
        ConfigureBgmSource(audioSource);
    }

    private static void ConfigureBgmSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.priority = 0;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.ignoreListenerPause = true;
        source.mute = false;
    }

    private static void EnsureClipLoaded(AudioClip clip)
    {
        if (clip == null || clip.loadState == AudioDataLoadState.Loaded)
            return;

        if (clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogWarning("HallBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private void ResolveClipIfMissing()
    {
        if (hallBgmClip != null)
            return;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null)
            hallBgmClip = library.HallBgm;

#if UNITY_EDITOR
        if (hallBgmClip == null)
            hallBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(WhatFloorAssetPath);
#endif
    }

    private bool IsOnHallScene() =>
        gameObject.scene.IsValid() && IsHallScene(gameObject.scene.name);

    private static Camera FindMainCameraInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Camera[] cameras = roots[r].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < cameras.Length; c++)
            {
                if (cameras[c] != null && cameras[c].CompareTag("MainCamera"))
                    return cameras[c];
            }
        }

        return null;
    }

    private void EnsureListenerActive()
    {
        AudioListener listener = GetComponent<AudioListener>();
        if (listener == null)
            listener = Object.FindFirstObjectByType<AudioListener>();

        if (listener != null)
            listener.enabled = true;

        AudioListener.pause = false;
        if (AudioListener.volume < 0.01f)
            AudioListener.volume = 1f;
    }

    private static void LogEditorAudioMuteHint()
    {
#if UNITY_EDITOR
        if (!EditorUtility.audioMasterMute)
            return;

        EditorUtility.audioMasterMute = false;
        Debug.Log("HallBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」以便播放 Hall BGM。");
#endif
    }
}

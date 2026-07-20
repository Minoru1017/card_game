using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Hall BGM player（idokay - What Floor）。
/// 使用 DontDestroyOnLoad 宿主跨場景延續播放；從 hall 進入的子場景（Buildbeck、Deck Pack 等）共用，
/// Story progress 與 CardStore 除外。
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
    private static HallBackgroundMusicPlayer persistentInstance;

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
        if (ShouldContinueHallBgm(scene.name))
        {
            if (IsHallScene(scene.name))
            {
                StoryProgressBackgroundMusicPlayer.StopAll();
                CardStoreBackgroundMusicPlayer.StopAll();
                BuildbeckBackgroundMusicPlayer.StopAll();
                TutorialBattleBackgroundMusicPlayer.StopAll();
                FreeBattleBackgroundMusicPlayer.StopAll();
                PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
            }

            EnsurePersistentHost()?.PlayHallBgm();
            return;
        }

        StopAll();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (!IsHallScene(scene.name))
            return;

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && ShouldContinueHallBgm(active.name))
            return;

        StopAll();
    }

    /// <summary>從 hall 進入的子場景延續 hall BGM；Story progress、CardStore 除外。</summary>
    public static bool ShouldContinueHallBgm(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;
        if (IsHallScene(sceneName))
            return true;
        if (sceneName == "Buildbeck")
            return true;
        if (sceneName == DeckPackSceneController.SceneName)
            return true;
        if (sceneName == "Persistent")
            return true;
        if (sceneName == "Settings")
            return true;
        if (sceneName == FreeBattleBattleCopy.SceneName)
            return true;
        return false;
    }

    private static HallBackgroundMusicPlayer EnsurePersistentHost()
    {
        if (persistentInstance != null)
            return persistentInstance;

        var host = new GameObject("[HallBgmHost]");
        Object.DontDestroyOnLoad(host);
        persistentInstance = host.AddComponent<HallBackgroundMusicPlayer>();
        return persistentInstance;
    }

    public static void StopAll()
    {
        if (persistentInstance != null)
            persistentInstance.StopHallBgm();

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
        shouldKeepPlaying = true;
        EnsureBgmSource();
        ResolveClipIfMissing();

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == hallBgmClip)
        {
            ApplyUserBgmVolume();
            return;
        }

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

    private void OnDestroy()
    {
        StopHallBgm();
        if (persistentInstance == this)
            persistentInstance = null;
    }

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
        bool resumeSameClip = audioSource.clip == clip && audioSource.time > 0f;
        audioSource.clip = clip;
        audioSource.volume = GameAudioUserSettings.ScaleBgm(volume);
        audioSource.loop = true;
        if (!resumeSameClip)
            audioSource.time = 0f;

        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            playRoutine = null;
            yield break;
        }

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

    public void ApplyUserBgmVolume()
    {
        if (audioSource != null)
            audioSource.volume = GameAudioUserSettings.ScaleBgm(volume);
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
        GameAudioMixerRouting.ConfigureSource(source, GameAudioChannel.Bgm);
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

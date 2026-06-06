using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Buildbeck scene BGM player（Dor-c - Ethereal Dreams - Instrumental version）。
/// Adds itself to the Buildbeck Main Camera at runtime and plays the clip registered in AudioLibrary.
/// </summary>
public sealed class BuildbeckBackgroundMusicPlayer : MonoBehaviour
{
    private const string BuildbeckSceneName = "Buildbeck";

#if UNITY_EDITOR
    // BGM/SFX 實體檔在 Assets/Music/，不放 Resources；AudioLibraryPopulator 依此路徑填表。
    public const string EtherealDreamsAssetPath = "Assets/Music/Dor-c - Ethereal Dreams - Instrumental version.mp3";
#endif

    [SerializeField] private AudioClip buildbeckBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterBuildbeckBgmSceneGuard()
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
        if (IsBuildbeckScene(scene.name))
        {
            HallBackgroundMusicPlayer.StopAll();
            StoryProgressBackgroundMusicPlayer.StopAll();
            TutorialBattleBackgroundMusicPlayer.StopAll();
            PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
            EnsureInScene(scene)?.PlayBuildbeckBgm();
            return;
        }

        StopAll();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (IsBuildbeckScene(scene.name))
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
        BuildbeckBackgroundMusicPlayer player = FindInScene(scene);
        if (player != null)
            player.StopBuildbeckBgm();
    }

    public static BuildbeckBackgroundMusicPlayer EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsBuildbeckScene(scene.name))
            return null;

        BuildbeckBackgroundMusicPlayer existing = FindInScene(scene);
        if (existing != null)
            return existing;

        Camera cam = FindMainCameraInScene(scene);
        if (cam == null)
            return null;

        return cam.gameObject.AddComponent<BuildbeckBackgroundMusicPlayer>();
    }

    public static BuildbeckBackgroundMusicPlayer FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            BuildbeckBackgroundMusicPlayer player =
                roots[r].GetComponentInChildren<BuildbeckBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public static bool IsBuildbeckScene(string sceneName) =>
        sceneName == BuildbeckSceneName;

    public void PlayBuildbeckBgm()
    {
        if (!IsOnBuildbeckScene())
            return;

        shouldKeepPlaying = true;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayWhenReady());
    }

    public void StopBuildbeckBgm()
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

    private void OnDestroy() => StopBuildbeckBgm();

    private IEnumerator CoPlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();
        EnsureBgmSource();
        ResolveClipIfMissing();

        AudioClip clip = buildbeckBgmClip;
        if (clip == null)
        {
            Debug.LogWarning("BuildbeckBackgroundMusicPlayer: no Buildbeck BGM clip assigned.");
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
            Debug.LogWarning("BuildbeckBackgroundMusicPlayer: Play() did not start. loadState=" + clip.loadState);

        playRoutine = null;
    }

    private void EnsureBgmSource()
    {
        if (audioSource != null)
            return;

        Transform child = transform.Find("BuildbeckBgmSource");
        if (child != null)
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                ConfigureBgmSource(audioSource);
                return;
            }
        }

        var sourceGo = new GameObject("BuildbeckBgmSource");
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
            Debug.LogWarning("BuildbeckBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private void ResolveClipIfMissing()
    {
        if (buildbeckBgmClip != null)
            return;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null)
            buildbeckBgmClip = library.BuildbeckBgm;

#if UNITY_EDITOR
        if (buildbeckBgmClip == null)
            buildbeckBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EtherealDreamsAssetPath);
#endif
    }

    private bool IsOnBuildbeckScene() =>
        gameObject.scene.IsValid() && IsBuildbeckScene(gameObject.scene.name);

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
        Debug.Log("BuildbeckBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」以便播放 Buildbeck BGM。");
#endif
    }
}

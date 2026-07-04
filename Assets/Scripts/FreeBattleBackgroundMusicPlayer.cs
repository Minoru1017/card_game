using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>自由對戰（Free Battle → BattleSimulation）戰鬥場景 BGM：ES_Light Within - Hara Noda。</summary>
public sealed class FreeBattleBackgroundMusicPlayer : MonoBehaviour
{
    public const bool BgmEnabled = true;

    public const string BgmResourcesPath = "Music/ES_Light Within - Hara Noda";

#if UNITY_EDITOR
    public const string BgmAssetPath = "Assets/Music/ES_Light Within - Hara Noda.mp3";
#endif

    [SerializeField] private AudioClip freeBattleBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 0.78f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterFreeBattleBgmSceneGuard()
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
        if (!BgmEnabled)
        {
            StopAll();
            return;
        }

        if (!IsSupportedBattleScene(scene.name))
        {
            StopAll();
            return;
        }

        if (!BattleLaunchContext.IsFreeBattle)
        {
            StopInScene(scene);
            return;
        }

        TutorialBattleBackgroundMusicPlayer.StopAll();
        EnsureInScene(scene)?.PlayFreeBattleBgm();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (IsSupportedBattleScene(scene.name))
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
        FreeBattleBackgroundMusicPlayer player = FindInScene(scene);
        if (player != null)
            player.StopFreeBattleBgm();
    }

    public static FreeBattleBackgroundMusicPlayer EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsSupportedBattleScene(scene.name))
            return null;

        FreeBattleBackgroundMusicPlayer existing = FindInScene(scene);
        if (existing != null)
            return existing;

        Camera cam = FindMainCameraInScene(scene);
        if (cam == null)
            return null;

        return cam.gameObject.AddComponent<FreeBattleBackgroundMusicPlayer>();
    }

    public static FreeBattleBackgroundMusicPlayer FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            FreeBattleBackgroundMusicPlayer player =
                roots[r].GetComponentInChildren<FreeBattleBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public void PlayFreeBattleBgm()
    {
        if (!BgmEnabled)
            return;

        if (!BattleLaunchContext.IsFreeBattle || !IsOnSupportedBattleScene())
            return;

        shouldKeepPlaying = true;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayWhenReady());
    }

    public void StopFreeBattleBgm()
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

    private void OnDestroy() => StopFreeBattleBgm();

    private IEnumerator CoPlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();
        EnsureBgmSource();

        AudioClip clip = ResolveClip();
        if (clip == null)
        {
            Debug.LogWarning("FreeBattleBackgroundMusicPlayer: no BGM clip assigned.");
            yield break;
        }

        EnsureClipLoaded(clip);
        audioSource.clip = clip;
        audioSource.volume = GameAudioUserSettings.ScaleBgm(volume);
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
            Debug.LogWarning("FreeBattleBackgroundMusicPlayer: Play() did not start. loadState=" + clip.loadState);

        playRoutine = null;
    }

    public void ApplyUserBgmVolume()
    {
        if (audioSource != null)
            audioSource.volume = GameAudioUserSettings.ScaleBgm(volume);
    }

    private AudioClip ResolveClip()
    {
        ResolveClipIfMissing();
        return freeBattleBgmClip;
    }

    private void ResolveClipIfMissing()
    {
        if (freeBattleBgmClip != null)
            return;

        freeBattleBgmClip = Resources.Load<AudioClip>(BgmResourcesPath);
#if UNITY_EDITOR
        if (freeBattleBgmClip == null)
            freeBattleBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmAssetPath);
#endif
    }

    private void EnsureBgmSource()
    {
        if (audioSource != null)
            return;

        Transform child = transform.Find("FreeBattleBgmSource");
        if (child != null)
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                ConfigureBgmSource(audioSource);
                return;
            }
        }

        var sourceGo = new GameObject("FreeBattleBgmSource");
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
            Debug.LogWarning("FreeBattleBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private bool IsOnSupportedBattleScene() =>
        gameObject.scene.IsValid() && IsSupportedBattleScene(gameObject.scene.name);

    public static bool IsSupportedBattleScene(string sceneName) =>
        sceneName == TutorialBattleBackgroundMusicPlayer.DefaultTutorialBattleSceneName ||
        sceneName == "BattleScene";

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

        return Object.FindFirstObjectByType<Camera>();
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
        Debug.Log(
            "FreeBattleBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」以便播放自由對戰 BGM。");
#endif
    }
}

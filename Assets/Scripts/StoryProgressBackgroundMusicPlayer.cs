using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Story progress 場景大地圖 BGM（Master Minded - Amazonian Grounding）。
/// </summary>
public class StoryProgressBackgroundMusicPlayer : MonoBehaviour
{
    public const string AmazonianGroundingResourcesPath = "Music/Master Minded - Amazonian Grounding";

#if UNITY_EDITOR
    public const string AmazonianGroundingAssetPath = "Assets/Music/Master Minded - Amazonian Grounding.mp3";
#endif

    [SerializeField] private AudioClip storyProgressBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterStoryProgressBgmSceneGuard()
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
        if (IsStoryProgressScene(scene.name))
        {
            TutorialBattleBackgroundMusicPlayer.StopAll();
            EnsureInScene(scene)?.PlayStoryProgressBgm();
            return;
        }

        StopAll();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (IsStoryProgressScene(scene.name))
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
        StoryProgressBackgroundMusicPlayer player = FindInScene(scene);
        if (player != null)
            player.StopStoryProgressBgm();
    }

    public static StoryProgressBackgroundMusicPlayer EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsStoryProgressScene(scene.name))
            return null;

        StoryProgressBackgroundMusicPlayer existing = FindInScene(scene);
        if (existing != null)
            return existing;

        Camera cam = FindMainCameraInScene(scene);
        if (cam == null)
            return null;

        return cam.gameObject.AddComponent<StoryProgressBackgroundMusicPlayer>();
    }

    public static StoryProgressBackgroundMusicPlayer FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            StoryProgressBackgroundMusicPlayer player =
                roots[r].GetComponentInChildren<StoryProgressBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public static bool IsStoryProgressScene(string sceneName) =>
        sceneName == StoryProgressSession.StoryProgressSceneName;

    public void PlayStoryProgressBgm()
    {
        if (!IsOnStoryProgressScene())
            return;

        shouldKeepPlaying = true;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayWhenReady());
    }

    public void StopStoryProgressBgm()
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

    private void OnDestroy() => StopStoryProgressBgm();

    private IEnumerator CoPlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();
        EnsureBgmSource();
        ResolveClipIfMissing();

        AudioClip clip = storyProgressBgmClip;
        if (clip == null)
        {
            Debug.LogWarning("StoryProgressBackgroundMusicPlayer: no BGM clip assigned.");
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
            Debug.LogWarning("StoryProgressBackgroundMusicPlayer: Play() did not start. loadState=" +
                             clip.loadState);

        playRoutine = null;
    }

    private void EnsureBgmSource()
    {
        if (audioSource != null)
            return;

        Transform child = transform.Find("StoryProgressBgmSource");
        if (child != null)
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                ConfigureBgmSource(audioSource);
                return;
            }
        }

        var sourceGo = new GameObject("StoryProgressBgmSource");
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
            Debug.LogWarning("StoryProgressBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private void ResolveClipIfMissing()
    {
        if (storyProgressBgmClip != null)
            return;

        storyProgressBgmClip = Resources.Load<AudioClip>(AmazonianGroundingResourcesPath);
#if UNITY_EDITOR
        if (storyProgressBgmClip == null)
            storyProgressBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AmazonianGroundingAssetPath);
#endif
    }

    private bool IsOnStoryProgressScene() =>
        gameObject.scene.IsValid() && IsStoryProgressScene(gameObject.scene.name);

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
        Debug.Log(
            "StoryProgressBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」以便播放 Story progress BGM。");
#endif
    }
}

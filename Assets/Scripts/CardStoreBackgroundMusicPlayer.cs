using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CardStore scene BGM player（Petro A - Adventury Moody）。
/// Adds itself to the CardStore Main Camera at runtime and plays the clip registered in AudioLibrary.
/// </summary>
public sealed class CardStoreBackgroundMusicPlayer : MonoBehaviour
{
    private const string CardStoreSceneName = "CardStore";

#if UNITY_EDITOR
    // BGM/SFX 實體檔在 Assets/Music/，不放 Resources；AudioLibraryPopulator 依此路徑填表。
    public const string AdventuryMoodyAssetPath = "Assets/Music/Petro A - Adventury Moody.mp3";
#endif

    [SerializeField] private AudioClip cardStoreBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterCardStoreBgmSceneGuard()
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
        if (IsCardStoreScene(scene.name))
        {
            HallBackgroundMusicPlayer.StopAll();
            BuildbeckBackgroundMusicPlayer.StopAll();
            StoryProgressBackgroundMusicPlayer.StopAll();
            TutorialBattleBackgroundMusicPlayer.StopAll();
            PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
            EnsureInScene(scene)?.PlayCardStoreBgm();
            return;
        }

        StopAll();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (IsCardStoreScene(scene.name))
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
        CardStoreBackgroundMusicPlayer player = FindInScene(scene);
        if (player != null)
            player.StopCardStoreBgm();
    }

    public static CardStoreBackgroundMusicPlayer EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsCardStoreScene(scene.name))
            return null;

        CardStoreBackgroundMusicPlayer existing = FindInScene(scene);
        if (existing != null)
            return existing;

        Camera cam = FindMainCameraInScene(scene);
        if (cam == null)
            return null;

        return cam.gameObject.AddComponent<CardStoreBackgroundMusicPlayer>();
    }

    public static CardStoreBackgroundMusicPlayer FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            CardStoreBackgroundMusicPlayer player =
                roots[r].GetComponentInChildren<CardStoreBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public static bool IsCardStoreScene(string sceneName) =>
        sceneName == CardStoreSceneName;

    public void PlayCardStoreBgm()
    {
        if (!IsOnCardStoreScene())
            return;

        shouldKeepPlaying = true;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayWhenReady());
    }

    public void StopCardStoreBgm()
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

    private void OnDestroy() => StopCardStoreBgm();

    private IEnumerator CoPlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();
        EnsureBgmSource();
        ResolveClipIfMissing();

        AudioClip clip = cardStoreBgmClip;
        if (clip == null)
        {
            Debug.LogWarning("CardStoreBackgroundMusicPlayer: no CardStore BGM clip assigned.");
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
            Debug.LogWarning("CardStoreBackgroundMusicPlayer: Play() did not start. loadState=" + clip.loadState);

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

        Transform child = transform.Find("CardStoreBgmSource");
        if (child != null)
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                ConfigureBgmSource(audioSource);
                return;
            }
        }

        var sourceGo = new GameObject("CardStoreBgmSource");
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
            Debug.LogWarning("CardStoreBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private void ResolveClipIfMissing()
    {
        if (cardStoreBgmClip != null)
            return;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null)
            cardStoreBgmClip = library.CardStoreBgm;

#if UNITY_EDITOR
        if (cardStoreBgmClip == null)
            cardStoreBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AdventuryMoodyAssetPath);
#endif
    }

    private bool IsOnCardStoreScene() =>
        gameObject.scene.IsValid() && IsCardStoreScene(gameObject.scene.name);

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
        Debug.Log("CardStoreBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」以便播放 CardStore BGM。");
#endif
    }
}

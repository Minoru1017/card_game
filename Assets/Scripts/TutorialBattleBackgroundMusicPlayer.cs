using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 1-1／1-2 對戰 BGM：入門教學戰（Forgotten Dreams）、港灣訓練場實戰（Shades - Mysterious，簡單～困難共用）、
/// M-1-2 階段 A 段考（Lalinea - Cut to the Chase - Instrumental version；恐怖狀態 Whisper）。
/// </summary>
public class TutorialBattleBackgroundMusicPlayer : MonoBehaviour
{
    public const string ForgottenDreamsResourcesPath = "Music/Aves - Forgotten Dreams";
    public const string HarborTrainingBgmResourcesPath = "Music/Ziv Moran - Shades - Mysterious";
    public const string M12PhaseABgmResourcesPath = "Music/Lalinea - Cut to the Chase - Instrumental version";
    public const string M12PhaseAHorrorBgmResourcesPath = "Music/Whisper";
    public const string DefaultTutorialBattleSceneName = "BattleSimulation";

#if UNITY_EDITOR
    public const string ForgottenDreamsAssetPath = "Assets/Resources/Music/Aves - Forgotten Dreams.mp3";
    public const string HarborTrainingBgmAssetPath = "Assets/Music/Ziv Moran - Shades - Mysterious.mp3";
    public const string M12PhaseABgmAssetPath = "Assets/Music/Lalinea - Cut to the Chase - Instrumental version.mp3";
    public const string M12PhaseAHorrorBgmAssetPath = "Assets/Music/Whisper.mp3";
#endif

    private const float M12BattleBgmVolume = 0.78f;

    [SerializeField] private AudioClip introTutorialBgmClip;
    [SerializeField] private AudioClip harborTrainingBgmClip;
    [SerializeField] private AudioClip m12PhaseABgmClip;
    [SerializeField] private AudioClip m12PhaseAHorrorBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1.1f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;
    private AudioClip activeClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterTutorialBattleBgmSceneGuard()
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
        if (!IsSupportedBattleScene(scene.name))
        {
            StopAll();
            return;
        }

        HarborTrainingBattleBackground.ApplyForActiveBattleContext();

        if (BattleLaunchContext.IsFreeBattle)
        {
            StopInScene(scene);
            return;
        }

        FreeBattleBackgroundMusicPlayer.StopAll();

        if (BattleLaunchContext.IsIntroTutorialBattle)
            EnsureInScene(scene)?.PlayIntroTutorialBattleBgm();
        else if (BattleLaunchContext.IsHarborTrainingGroundBattle)
            EnsureInScene(scene)?.PlayHarborTrainingBattleBgm();
        else if (BattleLaunchContext.IsM12TrioTutorialBattle)
            EnsureInScene(scene)?.PlayM12PhaseABattleBgm();
        else if (BattleLaunchContext.IsM12CoachPracticeBattle)
            EnsureInScene(scene)?.PlayHarborTrainingBattleBgm();
        else
            StopInScene(scene);
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
        TutorialBattleBackgroundMusicPlayer player = FindInScene(scene);
        if (player != null)
            player.StopTutorialBattleBgm();
    }

    public static TutorialBattleBackgroundMusicPlayer EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        TutorialBattleBackgroundMusicPlayer existing = FindInScene(scene);
        if (existing != null)
            return existing;

        Camera cam = FindMainCameraInScene(scene);
        if (cam == null)
            return null;

        return cam.gameObject.AddComponent<TutorialBattleBackgroundMusicPlayer>();
    }

    public static TutorialBattleBackgroundMusicPlayer FindInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            TutorialBattleBackgroundMusicPlayer player =
                roots[r].GetComponentInChildren<TutorialBattleBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public void PlayIntroTutorialBattleBgm() => PlayBattleBgm(ResolveIntroTutorialClip());

    /// <summary>港灣訓練場（簡單／普通／困難）共用 BGM。</summary>
    public void PlayHarborTrainingBattleBgm() => PlayBattleBgm(ResolveHarborTrainingClip());

    /// <summary>M-1-2 階段 A 段考 BGM。</summary>
    public void PlayM12PhaseABattleBgm() => PlayBattleBgm(ResolveM12PhaseAClip());

    /// <summary>M-1-2 階段 A 段考恐怖狀態 BGM。</summary>
    public void PlayM12PhaseAHorrorBgm() => PlayBattleBgm(ResolveM12PhaseAHorrorClip());

    public void PlayTutorialBattleBgm() => PlayIntroTutorialBattleBgm();

    private void PlayBattleBgm(AudioClip clip)
    {
        if (!ShouldPlayStoryProgressBattleBgm() || !IsOnSupportedBattleScene())
            return;

        activeClip = clip;
        shouldKeepPlaying = true;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayWhenReady());
    }

    public void StopTutorialBattleBgm()
    {
        shouldKeepPlaying = false;
        activeClip = null;
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
        ResolveIntroTutorialClipIfMissing();
        ResolveHarborTrainingClipIfMissing();
        ResolveM12PhaseAClipIfMissing();
        ResolveM12PhaseAHorrorClipIfMissing();
    }

    private void OnDestroy() => StopTutorialBattleBgm();

    private IEnumerator CoPlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();
        EnsureBgmSource();

        AudioClip clip = activeClip;
        if (clip == null)
        {
            if (BattleLaunchContext.IsHarborTrainingGroundBattle ||
                BattleLaunchContext.IsM12CoachPracticeBattle)
                clip = ResolveHarborTrainingClip();
            else if (BattleLaunchContext.IsM12TrioTutorialBattle)
                clip = ResolveM12PhaseAClip();
            else
                clip = ResolveIntroTutorialClip();
        }

        if (clip == null)
        {
            Debug.LogWarning("TutorialBattleBackgroundMusicPlayer: no BGM clip assigned.");
            yield break;
        }

        EnsureClipLoaded(clip);
        audioSource.clip = clip;
        audioSource.volume = GameAudioUserSettings.ScaleBgm(ResolvePlaybackVolume());
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
            Debug.LogWarning("TutorialBattleBackgroundMusicPlayer: Play() did not start. loadState=" +
                             clip.loadState);

        playRoutine = null;
    }

    public void ApplyUserBgmVolume()
    {
        if (audioSource != null)
            audioSource.volume = GameAudioUserSettings.ScaleBgm(ResolvePlaybackVolume());
    }

    /// <summary>對戰暫停選單：靜音／恢復 BGM（不停止播放進度）。</summary>
    public void SetPauseMuted(bool muted)
    {
        EnsureBgmSource();
        if (audioSource != null)
            audioSource.mute = muted;
    }

    public static void SetBattlePauseMuted(bool muted)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        FindInScene(scene)?.SetPauseMuted(muted);
    }

    private float ResolvePlaybackVolume()
    {
        if (IsM12BattleContext() || IsM12BattleClip(activeClip))
            return M12BattleBgmVolume;
        return volume;
    }

    private static bool IsM12BattleContext() =>
        BattleLaunchContext.IsM12TrioTutorialBattle ||
        BattleLaunchContext.IsM12CoachPracticeBattle;

    private bool IsM12BattleClip(AudioClip clip)
    {
        if (clip == null)
            return false;

        ResolveM12PhaseAClipIfMissing();
        ResolveM12PhaseAHorrorClipIfMissing();
        return clip == m12PhaseABgmClip || clip == m12PhaseAHorrorBgmClip;
    }

    private void EnsureBgmSource()
    {
        if (audioSource != null)
            return;

        Transform child = transform.Find("TutorialBattleBgmSource");
        if (child != null)
        {
            audioSource = child.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                ConfigureBgmSource(audioSource);
                return;
            }
        }

        var sourceGo = new GameObject("TutorialBattleBgmSource");
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
            Debug.LogWarning("TutorialBattleBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private AudioClip ResolveIntroTutorialClip()
    {
        ResolveIntroTutorialClipIfMissing();
        return introTutorialBgmClip;
    }

    private AudioClip ResolveHarborTrainingClip()
    {
        ResolveHarborTrainingClipIfMissing();
        return harborTrainingBgmClip;
    }

    private void ResolveIntroTutorialClipIfMissing()
    {
        if (introTutorialBgmClip != null)
            return;

        introTutorialBgmClip = Resources.Load<AudioClip>(ForgottenDreamsResourcesPath);
#if UNITY_EDITOR
        if (introTutorialBgmClip == null)
            introTutorialBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ForgottenDreamsAssetPath);
#endif
    }

    private void ResolveHarborTrainingClipIfMissing()
    {
        if (harborTrainingBgmClip != null)
            return;

        harborTrainingBgmClip = Resources.Load<AudioClip>(HarborTrainingBgmResourcesPath);
#if UNITY_EDITOR
        if (harborTrainingBgmClip == null)
            harborTrainingBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(HarborTrainingBgmAssetPath);
#endif
    }

    private AudioClip ResolveM12PhaseAClip()
    {
        ResolveM12PhaseAClipIfMissing();
        return m12PhaseABgmClip;
    }

    private void ResolveM12PhaseAClipIfMissing()
    {
        if (m12PhaseABgmClip != null)
            return;

        m12PhaseABgmClip = Resources.Load<AudioClip>(M12PhaseABgmResourcesPath);
#if UNITY_EDITOR
        if (m12PhaseABgmClip == null)
            m12PhaseABgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(M12PhaseABgmAssetPath);
#endif
    }

    private AudioClip ResolveM12PhaseAHorrorClip()
    {
        ResolveM12PhaseAHorrorClipIfMissing();
        return m12PhaseAHorrorBgmClip;
    }

    private void ResolveM12PhaseAHorrorClipIfMissing()
    {
        if (m12PhaseAHorrorBgmClip != null)
            return;

        m12PhaseAHorrorBgmClip = Resources.Load<AudioClip>(M12PhaseAHorrorBgmResourcesPath);
#if UNITY_EDITOR
        if (m12PhaseAHorrorBgmClip == null)
            m12PhaseAHorrorBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(M12PhaseAHorrorBgmAssetPath);
#endif
    }

    private bool IsOnSupportedBattleScene() =>
        gameObject.scene.IsValid() && IsSupportedBattleScene(gameObject.scene.name);

    private static bool ShouldPlayStoryProgressBattleBgm() =>
        BattleLaunchContext.ReturnToStoryProgressAfterBattle &&
        (BattleLaunchContext.IsIntroTutorialBattle ||
         BattleLaunchContext.IsHarborTrainingGroundBattle ||
         BattleLaunchContext.IsM12TrioTutorialBattle ||
         BattleLaunchContext.IsM12CoachPracticeBattle);

    public static bool IsSupportedBattleScene(string sceneName) =>
        sceneName == DefaultTutorialBattleSceneName || sceneName == "BattleScene";

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
            "TutorialBattleBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」以便播放對戰 BGM。");
#endif
    }
}

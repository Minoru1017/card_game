using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main Plot 場景 Main Camera 上的劇情 BGM（1-1 Enchanted Valley、M-1-2 HYPERCRUSH、M-1-3 Bait）。
/// 先完整播完一遍，再開啟循環。
/// </summary>
[RequireComponent(typeof(AudioListener))]
[RequireComponent(typeof(AudioSource))]
public class PlotBackgroundMusicPlayer : MonoBehaviour
{
    // 僅供 AudioLibrary 填表工具定位實體檔（檔案在 Assets/Music/，不在 Resources）。
    public const string EnchantedValleyAssetPath = "Assets/Music/Roie Shpigler - Enchanted Valley.mp3";
    public const string M12SeawallPatrolPlotBgmAssetPath = "Assets/Music/ZISO - HYPERCRUSH.mp3";
    public const string M13RiverForkPlotBgmAssetPath = "Assets/Music/DaniHaDani - Bait.mp3";

    [SerializeField] private AudioClip plotBgmClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1.2f;

    private AudioSource audioSource;
    private Coroutine playRoutine;
    private bool shouldKeepPlaying;
    private bool loopEnabled;

    public static void StopAllInMainPlotIfLoaded()
    {
        PlotBackgroundMusicPlayer player = FindInMainPlotScene();
        if (player != null)
            player.StopPlotBgm();
    }

    public static PlotBackgroundMusicPlayer FindInMainPlotScene()
    {
        Scene plotScene = SceneManager.GetSceneByName(StoryProgressSession.MainPlotSceneName);
        if (!plotScene.IsValid() || !plotScene.isLoaded)
            return null;

        GameObject[] roots = plotScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            PlotBackgroundMusicPlayer player = roots[r].GetComponentInChildren<PlotBackgroundMusicPlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public void PlayPlotBgm()
    {
        if (!IsOnMainPlotScene() || !ShouldPlayPlotBgm())
            return;

        plotBgmClip = ResolveActivePlotBgmClip();
        shouldKeepPlaying = true;
        loopEnabled = false;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayWhenReady());
    }

    public void StopPlotBgm()
    {
        shouldKeepPlaying = false;
        loopEnabled = false;
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
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource(audioSource);
        ResolveClipIfMissing();
        if (plotBgmClip != null)
        {
            EnsureClipLoaded(plotBgmClip);
            audioSource.clip = plotBgmClip;
        }
    }

    private void LateUpdate()
    {
        if (!shouldKeepPlaying || !loopEnabled || audioSource == null || !audioSource.loop)
            return;
        if (!audioSource.isPlaying)
        {
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private IEnumerator PlayWhenReady()
    {
        LogEditorAudioMuteHint();
        EnsureListenerActive();

        ResolveClipIfMissing();
        if (plotBgmClip == null)
        {
            Debug.LogWarning("PlotBackgroundMusicPlayer: no BGM clip assigned on Main Camera.");
            yield break;
        }

        EnsureClipLoaded(plotBgmClip);
        ConfigureAudioSource(audioSource);
        audioSource.clip = plotBgmClip;
        audioSource.volume = GameAudioUserSettings.ScaleBgm(volume);
        audioSource.loop = false;
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
                EnsureClipLoaded(plotBgmClip);

            yield return null;
        }

        if (!started)
        {
            Debug.LogWarning("PlotBackgroundMusicPlayer: Play() did not start. loadState=" + plotBgmClip.loadState);
            yield break;
        }

        yield return WaitForFirstPlaythroughComplete();

        if (!shouldKeepPlaying)
            yield break;

        audioSource.loop = true;
        loopEnabled = true;
        audioSource.time = 0f;
        audioSource.Play();

        GameDevLog.Log("PlotBackgroundMusicPlayer: looping " + plotBgmClip.name + " after full play (" +
                       plotBgmClip.length.ToString("F1") + "s), vol=" + volume.ToString("F2"));
    }

    private IEnumerator WaitForFirstPlaythroughComplete()
    {
        float length = plotBgmClip != null ? plotBgmClip.length : 0f;
        if (length <= 0.05f)
            yield break;

        while (shouldKeepPlaying && audioSource != null && audioSource.isPlaying &&
               audioSource.time < length - 0.05f)
            yield return null;

        while (shouldKeepPlaying && audioSource != null && audioSource.isPlaying)
            yield return null;
    }

    private static void EnsureClipLoaded(AudioClip clip)
    {
        if (clip == null)
            return;

        if (clip.loadState == AudioDataLoadState.Loaded)
            return;

        if (clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogWarning("PlotBackgroundMusicPlayer: clip load failed for " + clip.name);
            return;
        }

        clip.LoadAudioData();
    }

    private bool IsOnMainPlotScene() =>
        gameObject.scene.IsValid() &&
        gameObject.scene.name == StoryProgressSession.MainPlotSceneName;

    private static bool ShouldPlayPlotBgm() =>
        StoryProgressSession.TutorialPlotBgmRequested ||
        StoryProgressSession.M12PlotBgmRequested ||
        StoryProgressSession.M13PlotBgmRequested;

    private AudioClip ResolveActivePlotBgmClip()
    {
        if (StoryProgressSession.M12PlotBgmRequested)
        {
            AudioLibrary library = AudioLibrary.Instance;
            if (library != null && library.M12PlotBgm != null)
                return library.M12PlotBgm;
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<AudioClip>(M12SeawallPatrolPlotBgmAssetPath);
#else
            return null;
#endif
        }

        if (StoryProgressSession.M13PlotBgmRequested)
        {
            AudioLibrary library = AudioLibrary.Instance;
            if (library != null && library.M13PlotBgm != null)
                return library.M13PlotBgm;
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<AudioClip>(M13RiverForkPlotBgmAssetPath);
#else
            return null;
#endif
        }

        AudioLibrary lib = AudioLibrary.Instance;
        if (lib != null && lib.PlotBgm != null)
            return lib.PlotBgm;
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AudioClip>(EnchantedValleyAssetPath);
#else
        return plotBgmClip;
#endif
    }

    public void ApplyUserBgmVolume()
    {
        if (audioSource != null)
            audioSource.volume = GameAudioUserSettings.ScaleBgm(volume);
    }

    private void EnsureListenerActive()
    {
        AudioListener listener = GetComponent<AudioListener>();
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
            "PlotBackgroundMusicPlayer: 已自動取消 Unity 編輯器「遊戲音訊靜音」（Game 視窗喇叭）以便播放劇情 BGM。");
#endif
    }

    private void ResolveClipIfMissing()
    {
        if (plotBgmClip != null)
            return;

        plotBgmClip = ResolveActivePlotBgmClip();
        if (plotBgmClip == null)
        {
            Debug.LogWarning(
                "PlotBackgroundMusicPlayer: 找不到劇情 BGM，請在場景指定 plotBgmClip，" +
                "或重跑 Tools/Audio/Create or Refresh Audio Library 補進 AudioLibrary。");
        }
    }

    private static void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 0;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.ignoreListenerPause = true;
        source.mute = false;
    }

    private void OnDestroy() => StopPlotBgm();
}

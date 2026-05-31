using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main Plot 場景 Main Camera 上的 1-1 劇情 BGM（場景內直接指定 <see cref="plotBgmClip"/>）。
/// 先完整播完一遍，再開啟循環。
/// </summary>
[RequireComponent(typeof(AudioListener))]
[RequireComponent(typeof(AudioSource))]
public class PlotBackgroundMusicPlayer : MonoBehaviour
{
    public const string EnchantedValleyResourcesPath = "Music/Roie Shpigler - Enchanted Valley";

#if UNITY_EDITOR
    public const string EnchantedValleyAssetPath = "Assets/Music/Roie Shpigler - Enchanted Valley.mp3";
#endif

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
            player.StopTutorialPlotBgm();
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

    public void PlayTutorialPlotBgm()
    {
        if (!IsOnMainPlotScene() || !ShouldPlayTutorialPlotBgm())
            return;

        shouldKeepPlaying = true;
        loopEnabled = false;
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayWhenReady());
    }

    public void StopTutorialPlotBgm()
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
        audioSource.volume = volume;
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

    private static bool ShouldPlayTutorialPlotBgm() =>
        StoryProgressSession.TutorialPlotBgmRequested;

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

        plotBgmClip = Resources.Load<AudioClip>(EnchantedValleyResourcesPath);
#if UNITY_EDITOR
        if (plotBgmClip == null)
            plotBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EnchantedValleyAssetPath);
#endif
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

    private void OnDestroy() => StopTutorialPlotBgm();
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main Plot NPC 語音（Resources/NPC voice/，檔名如 1-1_4）。
/// </summary>
public sealed class PlotNpcVoicePlayer : MonoBehaviour
{
    public const string VoiceResourcesFolder = "NPC voice";

    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource voiceSource;
    private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

    public static PlotNpcVoicePlayer FindInMainPlotScene()
    {
        Scene plotScene = SceneManager.GetSceneByName(StoryProgressSession.MainPlotSceneName);
        if (!plotScene.IsValid() || !plotScene.isLoaded)
            return null;

        GameObject[] roots = plotScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            PlotNpcVoicePlayer player = roots[r].GetComponentInChildren<PlotNpcVoicePlayer>(true);
            if (player != null)
                return player;
        }

        return null;
    }

    public static PlotNpcVoicePlayer EnsureOnMainCamera()
    {
        PlotNpcVoicePlayer existing = FindInMainPlotScene();
        if (existing != null)
            return existing;

        Camera cam = Camera.main;
        if (cam == null || cam.gameObject.scene.name != StoryProgressSession.MainPlotSceneName)
            return null;

        return cam.gameObject.AddComponent<PlotNpcVoicePlayer>();
    }

    public void Play(string clipId)
    {
        if (string.IsNullOrWhiteSpace(clipId))
            return;

        AudioClip clip = ResolveClip(clipId.Trim());
        if (clip == null)
        {
            Debug.LogWarning("PlotNpcVoicePlayer: missing clip -> " + VoiceResourcesFolder + "/" + clipId);
            return;
        }

        EnsureVoiceSource();
        Stop();
        voiceSource.clip = clip;
        voiceSource.volume = GameAudioUserSettings.ScaleNpcVoice(volume);
        voiceSource.loop = false;
        voiceSource.time = 0f;
        voiceSource.Play();
    }

    public void Stop()
    {
        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Stop();
    }

    private void OnDestroy() => Stop();

    private void EnsureVoiceSource()
    {
        if (voiceSource != null)
            return;

        Transform child = transform.Find("PlotNpcVoiceSource");
        if (child != null)
            voiceSource = child.GetComponent<AudioSource>();

        if (voiceSource == null)
        {
            var sourceGo = new GameObject("PlotNpcVoiceSource");
            sourceGo.transform.SetParent(transform, false);
            voiceSource = sourceGo.AddComponent<AudioSource>();
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.priority = 64;
        voiceSource.bypassEffects = true;
        voiceSource.bypassListenerEffects = true;
        voiceSource.ignoreListenerPause = true;
    }

    private static AudioClip ResolveClip(string clipId)
    {
        // 編輯器內不吃 static 快取，讓 Inspector 即時修改（例如清空 Library 欄位）可立刻反映。
        bool useCache = !Application.isEditor;
        if (useCache && ClipCache.TryGetValue(clipId, out AudioClip cached))
            return cached;

        AudioClip clip = null;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null)
            clip = library.GetVoice(clipId);

        if (clip == null)
        {
            Debug.LogWarning(
                "PlotNpcVoicePlayer: '" + clipId + "' 不在 AudioLibrary，" +
                "請重跑 Tools/Audio/Create or Refresh Audio Library 補進註冊表。");
        }

        if (clip != null && useCache)
            ClipCache[clipId] = clip;
        return clip;
    }
}

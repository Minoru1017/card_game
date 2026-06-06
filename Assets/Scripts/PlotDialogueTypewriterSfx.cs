using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main Plot 1-1 劇情逐字顯示打字音效：動畫開始播一次，動畫結束（含快轉）即切斷。
/// </summary>
public class PlotDialogueTypewriterSfx : MonoBehaviour
{
#if UNITY_EDITOR
    // 僅供 AudioLibrary 填表工具定位實體檔（檔案在 Assets/Music/，不在 Resources）。
    public const string TypingClipAssetPath = "Assets/Music/typewriter-typing-.mp3";
#endif

    [SerializeField] private AudioClip typingClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 0.9f;

    private AudioSource sfxSource;

    public static PlotDialogueTypewriterSfx FindInMainPlotScene()
    {
        Scene plotScene = SceneManager.GetSceneByName(StoryProgressSession.MainPlotSceneName);
        if (!plotScene.IsValid() || !plotScene.isLoaded)
            return null;

        GameObject[] roots = plotScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            PlotDialogueTypewriterSfx sfx = roots[r].GetComponentInChildren<PlotDialogueTypewriterSfx>(true);
            if (sfx != null)
                return sfx;
        }

        return null;
    }

    public static PlotDialogueTypewriterSfx EnsureOnMainCamera()
    {
        PlotDialogueTypewriterSfx existing = FindInMainPlotScene();
        if (existing != null)
            return existing;

        Camera cam = Camera.main;
        if (cam == null || cam.gameObject.scene.name != StoryProgressSession.MainPlotSceneName)
            return null;

        return cam.gameObject.AddComponent<PlotDialogueTypewriterSfx>();
    }

    public void BeginTypingSound()
    {
        EnsureClipResolved();
        EnsureSfxSource();
        if (typingClip == null || sfxSource == null)
            return;

        StopTypingSound();

        sfxSource.clip = typingClip;
        sfxSource.volume = volume;
        sfxSource.loop = false;
        sfxSource.time = 0f;
        sfxSource.Play();
    }

    public void StopTypingSound()
    {
        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();
    }

    private void OnDestroy() => StopTypingSound();

    private void Awake()
    {
        EnsureClipResolved();
        EnsureSfxSource();
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
            return;

        Transform child = transform.Find("PlotTypewriterSfxSource");
        if (child != null)
        {
            sfxSource = child.GetComponent<AudioSource>();
            if (sfxSource != null)
                return;
        }

        var sourceGo = new GameObject("PlotTypewriterSfxSource");
        sourceGo.transform.SetParent(transform, false);
        sfxSource = sourceGo.AddComponent<AudioSource>();
        ConfigureSfxSource(sfxSource);
    }

    private static void ConfigureSfxSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 128;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.ignoreListenerPause = true;
        source.mute = false;
    }

    private void EnsureClipResolved()
    {
        if (typingClip != null)
            return;

        AudioLibrary library = AudioLibrary.Instance;
        if (library != null)
            typingClip = library.TypingSfx;

        if (typingClip == null)
        {
            Debug.LogWarning(
                "PlotDialogueTypewriterSfx: 找不到打字音效，請在場景指定 typingClip，" +
                "或重跑 Tools/Audio/Create or Refresh Audio Library 補進 AudioLibrary。");
        }
    }
}

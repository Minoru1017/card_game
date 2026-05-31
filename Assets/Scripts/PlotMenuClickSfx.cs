using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main Plot 1-1 劇情選單／按鈕點擊音效（選項、略過、點擊繼續）。
/// </summary>
public class PlotMenuClickSfx : MonoBehaviour
{
    public const string MenuClickClipResourcesPath = "Music/lhzrw-t5u2p";

#if UNITY_EDITOR
    public const string MenuClickClipAssetPath = "Assets/Music/lhzrw-t5u2p.mp3";
#endif

    [SerializeField] private AudioClip menuClickClip;
    [SerializeField] [Range(0f, 1.5f)] private float volume = 1f;

    private AudioSource sfxSource;

    public static PlotMenuClickSfx FindInMainPlotScene()
    {
        Scene plotScene = SceneManager.GetSceneByName(StoryProgressSession.MainPlotSceneName);
        if (!plotScene.IsValid() || !plotScene.isLoaded)
            return null;

        GameObject[] roots = plotScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            PlotMenuClickSfx sfx = roots[r].GetComponentInChildren<PlotMenuClickSfx>(true);
            if (sfx != null)
                return sfx;
        }

        return null;
    }

    public static PlotMenuClickSfx EnsureOnMainCamera()
    {
        PlotMenuClickSfx existing = FindInMainPlotScene();
        if (existing != null)
            return existing;

        Camera cam = Camera.main;
        if (cam == null || cam.gameObject.scene.name != StoryProgressSession.MainPlotSceneName)
            return null;

        return cam.gameObject.AddComponent<PlotMenuClickSfx>();
    }

    public void PlayMenuClick()
    {
        EnsureClipResolved();
        EnsureSfxSource();
        if (menuClickClip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(menuClickClip, volume);
    }

    private void Awake()
    {
        EnsureClipResolved();
        EnsureSfxSource();
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
            return;

        Transform child = transform.Find("PlotMenuClickSfxSource");
        if (child != null)
        {
            sfxSource = child.GetComponent<AudioSource>();
            if (sfxSource != null)
                return;
        }

        var sourceGo = new GameObject("PlotMenuClickSfxSource");
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
        if (menuClickClip != null)
            return;

        menuClickClip = Resources.Load<AudioClip>(MenuClickClipResourcesPath);
#if UNITY_EDITOR
        if (menuClickClip == null)
            menuClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuClickClipAssetPath);
#endif
    }
}

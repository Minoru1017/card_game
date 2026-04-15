using System;
using UnityEngine;
using UnityEngine.Video;

public class PackVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoUIRoot; // 拖「顯示影片的 RawImage/Panel 父物件」

    public event Action Finished;

    void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += HandleFinished;

        if (videoUIRoot != null) videoUIRoot.SetActive(false); // 一開始不要顯示
    }

    void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= HandleFinished;
    }

    public void PlayOnce()
    {
        if (videoUIRoot != null) videoUIRoot.SetActive(true);

        videoPlayer.isLooping = false;
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    private void HandleFinished(VideoPlayer vp)
    {
        if (videoUIRoot != null) videoUIRoot.SetActive(false); // 播完隱藏
        Finished?.Invoke();
    }
}

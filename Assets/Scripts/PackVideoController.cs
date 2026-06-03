using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PackVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoUIRoot;
    [SerializeField] private float prepareTimeoutSeconds = 12f;
    [SerializeField] private float minVisiblePlaySeconds = 0.35f;

    public event Action Finished;

    private RawImage videoRawImage;
    private Coroutine playRoutine;
    private bool finishSignaled;
    private float playStartedUnscaledTime;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += HandleLoopPointReached;
            videoPlayer.errorReceived += HandleVideoError;
            videoPlayer.waitForFirstFrame = true;
        }

        CacheVideoRawImage();
        HideVideoUi();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleLoopPointReached;
            videoPlayer.errorReceived -= HandleVideoError;
        }
    }

    private void Update()
    {
        if (finishSignaled || videoPlayer == null || !videoPlayer.isPlaying)
            return;

        if (Time.unscaledTime - playStartedUnscaledTime < minVisiblePlaySeconds)
            return;

        if (videoPlayer.frame <= 0)
            return;

        // loopPointReached �b���� Android / iOS �˸m�W��Ĳ�o�A��H����i�ק@���ƴ��C
        double length = videoPlayer.length;
        if (length > 0.5 && videoPlayer.time >= length - 0.08)
            SignalFinished();
    }

    public void PlayOnce()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(CoPlayOnce());
    }

    public void StopAndHide()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        finishSignaled = true;
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
        SetOpenButtonInteractable(true);
        HideVideoUi();
    }

    /// <summary>�������v���h raycast�A���������b���񪺵e���C</summary>
    public void EnsureUiDoesNotBlockInput()
    {
        CacheVideoRawImage();
        if (videoPlayer != null && videoPlayer.isPlaying)
            return;

        HideVideoUi();
    }

    private IEnumerator CoPlayOnce()
    {
        finishSignaled = false;
        SetOpenButtonInteractable(false);
        ShowVideoUi();

        videoPlayer.isLooping = false;
        videoPlayer.time = 0;
        videoPlayer.Prepare();

        float waited = 0f;
        while (!videoPlayer.isPrepared && waited < prepareTimeoutSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
            Debug.LogWarning("PackVideoController: Prepare slow; attempting Play() anyway.");

        videoPlayer.Play();
        playStartedUnscaledTime = Time.unscaledTime;

        float playWait = 0f;
        const float startPlayTimeout = 4f;
        while (!videoPlayer.isPlaying && playWait < startPlayTimeout)
        {
            playWait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPlaying)
        {
            Debug.LogWarning("PackVideoController: video did not start; skipping to pack reveal.");
            SignalFinished();
            yield break;
        }

        playRoutine = null;
    }

    private void HandleLoopPointReached(VideoPlayer vp) => SignalFinished();

    private void HandleVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning("PackVideoController: video error -> " + message);
        SignalFinished();
    }

    private void SignalFinished()
    {
        if (finishSignaled)
            return;

        finishSignaled = true;
        HideVideoUi();
        SetOpenButtonInteractable(true);
        Finished?.Invoke();
    }

    private void CacheVideoRawImage()
    {
        if (videoUIRoot == null)
            return;

        if (videoRawImage == null)
            videoRawImage = videoUIRoot.GetComponent<RawImage>();
        if (videoRawImage != null)
            videoRawImage.raycastTarget = false;
    }

    private void HideVideoUi()
    {
        CacheVideoRawImage();
        if (videoUIRoot != null)
            videoUIRoot.SetActive(false);
    }

    private void ShowVideoUi()
    {
        CacheVideoRawImage();
        if (videoUIRoot == null)
            return;

        videoUIRoot.SetActive(true);
        videoUIRoot.transform.SetAsLastSibling();
    }

    private static void SetOpenButtonInteractable(bool interactable)
    {
        GameObject openGo = GameObject.Find("Open");
        if (openGo == null)
            return;

        Button btn = openGo.GetComponent<Button>();
        if (btn != null)
            btn.interactable = interactable;

        Image img = openGo.GetComponent<Image>();
        if (img != null)
            img.raycastTarget = interactable;
    }
}

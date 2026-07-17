using UnityEngine;

public sealed partial class FightingBirdGameSceneController
{
    // ----------------------------------------------------------------- audio

    private void SetupAudio()
    {
        StopCompetingBackgroundMusic();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        tickClip = BuildClickClip(900f, 0.05f);
        downbeatClip = BuildClickClip(500f, 0.07f);

        hitSfxSource = gameObject.AddComponent<AudioSource>();
        hitSfxSource.playOnAwake = false;
        hitSfxSource.spatialBlend = 0f;
        hitSfxSource.bypassListenerEffects = true;
        hitSfxSource.ignoreListenerPause = true;
        hitSfxBank = BirdDuelHitSfxBank.TryCreate(ResolveHitSfxSourceClip());

        SetupBgm();
    }

    private static void StopCompetingBackgroundMusic()
    {
        HallBackgroundMusicPlayer.StopAll();
        StoryProgressBackgroundMusicPlayer.StopAll();
        CardStoreBackgroundMusicPlayer.StopAll();
        BuildbeckBackgroundMusicPlayer.StopAll();
        TutorialBattleBackgroundMusicPlayer.StopAll();
        FreeBattleBackgroundMusicPlayer.StopAll();
        PlotBackgroundMusicPlayer.StopAllInMainPlotIfLoaded();
    }

    private AudioClip ResolveHitSfxSourceClip()
    {
        AudioLibrary library = AudioLibrary.Instance;
        if (library != null && library.BirdDuelHitSfxSource != null)
            return library.BirdDuelHitSfxSource;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(HitSfxAssetPath);
#else
        return null;
#endif
    }

    private void PlayHitOutcomeSfx(BirdBeatOutcome outcome)
    {
        if (hitSfxSource == null || hitSfxBank == null || !hitSfxBank.IsReady)
            return;

        AudioClip clip = hitSfxBank.ResolveClip(outcome);
        if (clip == null)
            return;

        hitSfxSource.PlayOneShot(clip, GameAudioUserSettings.ScaleBattleSfx(BirdDuelHitSfxBank.ResolveVolume(outcome)));
    }

    /// <summary>鬥鳥 BGM：依所選 CD 解析 clip；實際排程於每場開始時 <see cref="RestartSongAndClock"/>。</summary>
    private void SetupBgm()
    {
        ApplyActiveCdAudioProfile();

        if (bgmClip == null)
        {
            Debug.LogWarning(
                "FightingBirdGameSceneController: 找不到鬥鳥 BGM（cd=" + activeCdId + "），節拍將靜音進行。");
            return;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.bypassListenerEffects = true;
            bgmSource.ignoreListenerPause = true;
        }

        bgmSource.loop = !rhythmProfile.UsesCustomBgmLoop;
        bgmSource.volume = GameAudioUserSettings.ScaleBgm(BgmVolume);
        bgmSource.clip = bgmClip;

        if (bgmClip.loadState != AudioDataLoadState.Loaded)
            bgmClip.LoadAudioData();
    }

    /// <summary>每場開始（含「再練一次」）：重載 CD 設定、歌曲從頭重播，並重設節拍時鐘。</summary>
    private void RestartSongAndClock()
    {
        ApplyActiveCdAudioProfile();

        songStartDsp = AudioSettings.dspTime + ScheduleLeadSeconds;

        if (bgmSource == null || bgmClip == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = bgmClip;
        bgmSource.loop = !rhythmProfile.UsesCustomBgmLoop;
        bgmSource.time = rhythmProfile.UsesCustomBgmLoop ? bgmLoopStartSeconds : 0f;
        bgmSource.PlayScheduled(songStartDsp);
    }

    /// <summary>自訂循環區：每圈長度對齊港灣練習帶，避免長曲前奏過短的有效演奏感。</summary>
    private void MaintainBgmLoopRegion()
    {
        if (bgmSource == null || !bgmSource.isPlaying || bgmLoopLengthSeconds <= 0.01f)
            return;

        if (bgmSource.time >= bgmLoopStartSeconds + bgmLoopLengthSeconds)
            bgmSource.time = bgmLoopStartSeconds;
    }

    private void ApplyActiveCdAudioProfile()
    {
        LoadRhythmSync();
        bgmClip = BirdDuelBgmResolver.Resolve(activeCdId);
    }

    private void LoadRhythmSync()
    {
        activeCdId = ResolveActiveCdId();
        rhythmProfile = BirdDuelRhythmSync.ResolveForCd(activeCdId);
        bpm = rhythmProfile.Bpm;
        firstDownbeatOffset = rhythmProfile.FirstDownbeatOffset;
        rhythmGrid = rhythmProfile.Grid;
        bgmLoopStartSeconds = rhythmProfile.BgmLoopStartSeconds;
        bgmLoopLengthSeconds = rhythmProfile.BgmLoopLengthSeconds;
        activeCloseToWinScoreMargin = rhythmProfile.CloseToWinScoreMargin;
        activeNormalBeatsPerStep = rhythmProfile.NormalBeatsPerStep;
        activeDecisiveMinTriplets = rhythmProfile.DecisiveMinTriplets;
        activeDecisiveMaxTriplets = rhythmProfile.DecisiveMaxTriplets;
        ResetJudgementWindows();
    }

    private static string ResolveActiveCdId()
    {
        if (M13StoryDuelContext.IsActive)
            return BirdDuelRhythmChart.RiverForkWaveCdId;

        if (PreBattleDuelContext.IsActive && !string.IsNullOrWhiteSpace(PreBattleDuelContext.BirdDuelCdId))
            return PreBattleDuelContext.BirdDuelCdId;

        if (PreBattleCdContext.HasSelection)
            return PreBattleCdContext.SelectedCdId;

        return BirdDuelCdCatalog.DefaultCdId;
    }

    /// <summary>第 beatIndex 個整拍的 dsp 命中時間（count-in 用）。</summary>
    private double BeatDsp(int beatIndex) =>
        songStartDsp + firstDownbeatOffset + beatIndex * SecondsPerBeat;

    public void ApplyUserBgmVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = GameAudioUserSettings.ScaleBgm(BgmVolume);
    }

    private void OnDestroy()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
        hitSfxBank?.Release();
        hitSfxBank = null;
    }

    private void PlayTick(bool downbeat)
    {
        if (audioSource == null) return;
        AudioClip clip = downbeat ? downbeatClip : tickClip;
        if (clip != null)
        {
            float tickVolume = downbeat ? 0.9f : 0.7f;
            audioSource.PlayOneShot(clip, GameAudioUserSettings.ScaleBattleSfx(tickVolume));
        }
    }

    private static AudioClip BuildClickClip(float frequency, float duration)
    {
        const int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        AudioClip clip = AudioClip.Create("birdTick", samples, 1, sampleRate, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 28f);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.6f;
        }
        clip.SetData(data, 0);
        return clip;
    }
}

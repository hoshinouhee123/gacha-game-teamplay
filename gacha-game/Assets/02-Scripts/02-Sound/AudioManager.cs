using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // =========================================================
    // Mixer Groups
    // =========================================================

    [SerializeField]
    private AudioMixerGroup bgmMixerGroup;

    [SerializeField]
    private AudioMixerGroup sfxMixerGroup;

    [SerializeField]
    private AudioMixerGroup voiceMixerGroup;


    // =========================================================
    // Audio Sources
    // =========================================================

    [SerializeField]
    private AudioSource bgmSource;

    [SerializeField]
    private AudioSource sfxSource;

    [SerializeField]
    private AudioSource voiceSource;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        InitializeSingleton();
        InitializeAudioSources();
    }


    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    private void InitializeAudioSources()
    {
        if (bgmSource != null)
        {
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;

            if (bgmMixerGroup != null)
                bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        }


        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;

            if (sfxMixerGroup != null)
                sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        }


        if (voiceSource != null)
        {
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;

            if (voiceMixerGroup != null)
                voiceSource.outputAudioMixerGroup = voiceMixerGroup;
        }
    }


    // =========================================================
    // BGM
    // =========================================================

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] BGM AudioClip이 null임.");
            return;
        }

        if (bgmSource == null)
            return;


        // 이미 같은 BGM 재생 중이면 다시 시작하지 않음
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;


        bgmSource.Stop();

        bgmSource.clip = clip;
        bgmSource.loop = true;

        bgmSource.Play();
    }


    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }


    public void PauseBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Pause();
    }


    public void ResumeBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.UnPause();
    }


    public bool IsBGMPlaying()
    {
        return bgmSource != null && bgmSource.isPlaying;
    }


    // =========================================================
    // SFX
    // =========================================================

    /// <summary>
    /// 기본 효과음 재생.
    /// PlayOneShot이므로 여러 효과음 동시 재생 가능.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 1f);
    }


    /// <summary>
    /// 개별 볼륨을 지정해서 효과음 재생.
    /// volumeScale : 0 ~ 1
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] SFX AudioClip이 심비오트 null입니다.");
            return;
        }

        if (sfxSource == null)
            return;


        volumeScale = Mathf.Clamp01(volumeScale);

        sfxSource.PlayOneShot(
            clip,
            volumeScale
        );
    }


    public void StopAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
    }


    // =========================================================
    // Voice
    // =========================================================

    /// <summary>
    /// 보이스 재생.
    /// 기존 보이스가 있다면 끊고 새 보이스 재생.
    /// </summary>
    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Voice AudioClip이 null이라고ㅓㅓㅓ.");
            return;
        }

        if (voiceSource == null)
            return;


        voiceSource.Stop();

        voiceSource.clip = clip;
        voiceSource.loop = false;

        voiceSource.Play();
    }


    public void StopVoice()
    {
        if (voiceSource == null)
            return;

        voiceSource.Stop();
        voiceSource.clip = null;
    }


    public void PauseVoice()
    {
        if (voiceSource == null)
            return;

        voiceSource.Pause();
    }


    public void ResumeVoice()
    {
        if (voiceSource == null)
            return;

        voiceSource.UnPause();
    }


    public bool IsVoicePlaying()
    {
        return voiceSource != null && voiceSource.isPlaying;
    }


    // =========================================================
    // Global
    // =========================================================

    public void StopAllAudio()
    {
        StopBGM();
        StopAllSFX();
        StopVoice();
    }
}
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Title("오디오 믹서")]

    [SerializeField] private AudioMixer audioMixer;


    [Title("볼륨 슬라이더")]

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider voiceSlider;

    [Title("음소거 토글")]

    [SerializeField] private Toggle masterMuteToggle;
    [SerializeField] private Toggle bgmMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;
    [SerializeField] private Toggle voiceMuteToggle;

    // AudioMixer Exposed Parameter 이름
    private const string MASTER_VOLUME = "MasterVolume";
    private const string BGM_VOLUME = "BGMVolume";
    private const string SFX_VOLUME = "SFXVolume";
    private const string VOICE_VOLUME = "VoiceVolume";

    // PlayerPrefs Key
    private const string MASTER_VOLUME_KEY = "Audio_MasterVolume";
    private const string BGM_VOLUME_KEY = "Audio_BGMVolume";
    private const string SFX_VOLUME_KEY = "Audio_SFXVolume";
    private const string VOICE_VOLUME_KEY = "Audio_VoiceVolume";

    private const string MASTER_MUTE_KEY = "Audio_MasterMute";
    private const string BGM_MUTE_KEY = "Audio_BGMMute";
    private const string SFX_MUTE_KEY = "Audio_SFXMute";
    private const string VOICE_MUTE_KEY = "Audio_VoiceMute";

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private float voiceVolume = 1f;

    private bool masterMuted;
    private bool bgmMuted;
    private bool sfxMuted;
    private bool voiceMuted;

    private void Awake()
    {
        LoadSettings();
        SetupUI();
        ApplyAllSettings();
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    // =========================
    // 초기화
    // =========================

    private void SetupUI()
    {
        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.value = masterVolume;

            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = bgmVolume;

            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = sfxVolume;

            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (voiceSlider != null)
        {
            voiceSlider.minValue = 0f;
            voiceSlider.maxValue = 1f;
            voiceSlider.value = voiceVolume;

            voiceSlider.onValueChanged.AddListener(SetVoiceVolume);
        }

        if (masterMuteToggle != null)
        {
            masterMuteToggle.isOn = masterMuted;
            masterMuteToggle.onValueChanged.AddListener(SetMasterMute);
        }

        if (bgmMuteToggle != null)
        {
            bgmMuteToggle.isOn = bgmMuted;
            bgmMuteToggle.onValueChanged.AddListener(SetBGMMute);
        }

        if (sfxMuteToggle != null)
        {
            sfxMuteToggle.isOn = sfxMuted;
            sfxMuteToggle.onValueChanged.AddListener(SetSFXMute);
        }

        if (voiceMuteToggle != null)
        {
            voiceMuteToggle.isOn = voiceMuted;
            voiceMuteToggle.onValueChanged.AddListener(SetVoiceMute);
        }
    }

    private void RemoveListeners()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

        if (voiceSlider != null)
            voiceSlider.onValueChanged.RemoveListener(SetVoiceVolume);

        if (masterMuteToggle != null)
            masterMuteToggle.onValueChanged.RemoveListener(SetMasterMute);

        if (bgmMuteToggle != null)
            bgmMuteToggle.onValueChanged.RemoveListener(SetBGMMute);

        if (sfxMuteToggle != null)
            sfxMuteToggle.onValueChanged.RemoveListener(SetSFXMute);

        if (voiceMuteToggle != null)
            voiceMuteToggle.onValueChanged.RemoveListener(SetVoiceMute);
    }

    // =========================
    // Volume
    // =========================

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);

        ApplyVolume(
            MASTER_VOLUME,
            masterVolume,
            masterMuted
        );

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);

        ApplyVolume(
            BGM_VOLUME,
            bgmVolume,
            bgmMuted
        );

        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        ApplyVolume(
            SFX_VOLUME,
            sfxVolume,
            sfxMuted
        );

        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetVoiceVolume(float value)
    {
        voiceVolume = Mathf.Clamp01(value);

        ApplyVolume(
            VOICE_VOLUME,
            voiceVolume,
            voiceMuted
        );

        PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, voiceVolume);
        PlayerPrefs.Save();
    }

    // =========================
    // Mute
    // =========================

    public void SetMasterMute(bool mute)
    {
        masterMuted = mute;

        ApplyVolume(
            MASTER_VOLUME,
            masterVolume,
            masterMuted
        );

        PlayerPrefs.SetInt(
            MASTER_MUTE_KEY,
            masterMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void SetBGMMute(bool mute)
    {
        bgmMuted = mute;

        ApplyVolume(
            BGM_VOLUME,
            bgmVolume,
            bgmMuted
        );

        PlayerPrefs.SetInt(
            BGM_MUTE_KEY,
            bgmMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void SetSFXMute(bool mute)
    {
        sfxMuted = mute;

        ApplyVolume(
            SFX_VOLUME,
            sfxVolume,
            sfxMuted
        );

        PlayerPrefs.SetInt(
            SFX_MUTE_KEY,
            sfxMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void SetVoiceMute(bool mute)
    {
        voiceMuted = mute;

        ApplyVolume(
            VOICE_VOLUME,
            voiceVolume,
            voiceMuted
        );

        PlayerPrefs.SetInt(
            VOICE_MUTE_KEY,
            voiceMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    // =========================
    // 실제 Mixer 적용
    // =========================

    private void ApplyVolume(
        string parameter,
        float volume,
        bool muted
    )
    {
        if (audioMixer == null)
            return;

        if (muted || volume <= 0.0001f)
        {
            audioMixer.SetFloat(parameter, -80f);
            return;
        }

        float db = Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(parameter, db);
    }

    private void ApplyAllSettings()
    {
        ApplyVolume(
            MASTER_VOLUME,
            masterVolume,
            masterMuted
        );

        ApplyVolume(
            BGM_VOLUME,
            bgmVolume,
            bgmMuted
        );

        ApplyVolume(
            SFX_VOLUME,
            sfxVolume,
            sfxMuted
        );

        ApplyVolume(
            VOICE_VOLUME,
            voiceVolume,
            voiceMuted
        );
    }

    // =========================
    // 저장 / 불러오기
    // =========================

    private void LoadSettings()
    {
        masterVolume =
            PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);

        bgmVolume =
            PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);

        sfxVolume =
            PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        voiceVolume =
            PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, 1f);

        masterMuted =
            PlayerPrefs.GetInt(MASTER_MUTE_KEY, 0) == 1;

        bgmMuted =
            PlayerPrefs.GetInt(BGM_MUTE_KEY, 0) == 1;

        sfxMuted =
            PlayerPrefs.GetInt(SFX_MUTE_KEY, 0) == 1;

        voiceMuted =
            PlayerPrefs.GetInt(VOICE_MUTE_KEY, 0) == 1;
    }

    // =========================
    // 기본값 초기화
    // =========================

    public void ResetAudioSettings()
    {
        masterVolume = 1f;
        bgmVolume = 1f;
        sfxVolume = 1f;
        voiceVolume = 1f;

        masterMuted = false;
        bgmMuted = false;
        sfxMuted = false;
        voiceMuted = false;

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(masterVolume);

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(bgmVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxVolume);

        if (voiceSlider != null)
            voiceSlider.SetValueWithoutNotify(voiceVolume);

        if (masterMuteToggle != null)
            masterMuteToggle.SetIsOnWithoutNotify(masterMuted);

        if (bgmMuteToggle != null)
            bgmMuteToggle.SetIsOnWithoutNotify(bgmMuted);

        if (sfxMuteToggle != null)
            sfxMuteToggle.SetIsOnWithoutNotify(sfxMuted);

        if (voiceMuteToggle != null)
            voiceMuteToggle.SetIsOnWithoutNotify(voiceMuted);

        SaveAllSettings();
        ApplyAllSettings();
    }

    private void SaveAllSettings()
    {
        PlayerPrefs.SetFloat(
            MASTER_VOLUME_KEY,
            masterVolume
        );

        PlayerPrefs.SetFloat(
            BGM_VOLUME_KEY,
            bgmVolume
        );

        PlayerPrefs.SetFloat(
            SFX_VOLUME_KEY,
            sfxVolume
        );

        PlayerPrefs.SetFloat(
            VOICE_VOLUME_KEY,
            voiceVolume
        );

        PlayerPrefs.SetInt(
            MASTER_MUTE_KEY,
            masterMuted ? 1 : 0
        );

        PlayerPrefs.SetInt(
            BGM_MUTE_KEY,
            bgmMuted ? 1 : 0
        );

        PlayerPrefs.SetInt(
            SFX_MUTE_KEY,
            sfxMuted ? 1 : 0
        );

        PlayerPrefs.SetInt(
            VOICE_MUTE_KEY,
            voiceMuted ? 1 : 0
        );

        PlayerPrefs.Save();
    }
}
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameSettings : MonoBehaviour
{
    public Slider cameraSensitivitySlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider masterVolumeSlider;
    public AudioMixer audioMixer;

    private const string SensitivityKey = "Settings.CameraSensitivity";
    private const string MusicKey = "Settings.Music";
    private const string SfxKey = "Settings.SFX";
    private const string MasterKey = "Settings.Master";

    public static float CameraSensitivity => Mathf.Lerp(0.2f, 1.8f, Read(SensitivityKey, 0.5f));
    private static float Read(string key, float fallback = 1f) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, fallback));
    private static float Decibels(float value) => value <= 0 ? -80f : Mathf.Log10(value) * 20f;

    private void Start()
    {
        Bind(cameraSensitivitySlider, Read(SensitivityKey, 0.5f), SetCameraSensitivity);
        Bind(musicVolumeSlider, Read(MusicKey), SetMusicVolume);
        Bind(sfxVolumeSlider, Read(SfxKey), SetSfxVolume);
        Bind(masterVolumeSlider, Read(MasterKey), SetMasterVolume);
        ApplyAudio(audioMixer);
    }

    private static void Bind(Slider slider, float value, UnityAction<float> setter)
    {
        if (slider == null) return;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(setter);
    }

    public void SetCameraSensitivity(float value) => PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp01(value));

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        if (audioMixer != null) audioMixer.SetFloat("Music", Decibels(Read(MusicKey)));
    }

    public void SetSfxVolume(float value)
    {
        PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        if (audioMixer == null) return;
        audioMixer.SetFloat("SFX", Decibels(Read(SfxKey)));
        audioMixer.SetFloat("UIMusic", Decibels(Read(SfxKey)));
    }

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
        AudioListener.volume = Read(MasterKey);
    }

    public static void ApplyAudio(AudioMixer mixer)
    {
        AudioListener.volume = Read(MasterKey);
        if (mixer == null) return;
        mixer.SetFloat("Music", Decibels(Read(MusicKey)));
        mixer.SetFloat("SFX", Decibels(Read(SfxKey)));
        mixer.SetFloat("UIMusic", Decibels(Read(SfxKey)));
    }

    private void OnDisable() => PlayerPrefs.Save();

    private void OnDestroy()
    {
        if (cameraSensitivitySlider != null) cameraSensitivitySlider.onValueChanged.RemoveListener(SetCameraSensitivity);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
    }
}

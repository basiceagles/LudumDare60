using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameSettings : MonoBehaviour
{
    // Slider values that are used when nothing has been saved yet. The middle
    // keeps the game's original volumes and camera speed.
    private const float DefaultSliderValue = 0.5f;

    // Slider value 0-1 mapped onto the camera rotation multiplier.
    private const float MinSensitivity = 0.2f;
    private const float MaxSensitivity = 1.8f;

    private const string SensitivityKey = "Settings.CameraSensitivity";
    private const string MusicKey = "Settings.Music";
    private const string SfxKey = "Settings.SFX";
    private const string MasterKey = "Settings.Master";

    // Names of the exposed parameters of Assets/Audio/Master.mixer.
    private const string DefaultMusicParameter = "Music";
    private const string DefaultSfxParameter = "SFX";
    private const string DefaultUiParameter = "UIMusic";

    [Header("Menu")]
    [Tooltip(
        "Panel opened and closed with the Escape key. It is hidden on start. " +
        "Leave this empty if the settings should always stay visible."
    )]
    public GameObject menuPanel;

    [Tooltip("Key that opens and closes the menu.")]
    public Key toggleKey = Key.Escape;

    [Tooltip(
        "Freeze the game and pause every AudioSource that is playing while " +
        "the menu is open."
    )]
    public bool pauseWhileOpen = true;

    [Header("Sliders")]
    public Slider cameraSensitivitySlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider masterVolumeSlider;

    [Header("Audio Mixer")]
    [Tooltip(
        "Mixer whose exposed parameters the volume sliders drive. When left " +
        "empty it is taken from the first AudioSource routed to one."
    )]
    public AudioMixer audioMixer;

    [Tooltip("Exposed parameter of the Music group.")]
    public string musicParameter = DefaultMusicParameter;

    [Tooltip("Exposed parameter of the SFX group.")]
    public string sfxParameter = DefaultSfxParameter;

    [Tooltip(
        "Exposed parameter of the UI group. UI sounds follow the SFX " +
        "slider. Leave empty to skip."
    )]
    public string uiParameter = DefaultUiParameter;

    [Header("Camera")]
    [Tooltip("Camera whose rotation speed the sensitivity slider drives.")]
    public CameraController cameraController;

    private readonly List<AudioSource> pausedSources = new List<AudioSource>();
    private bool menuOpen;
    private float timeScaleBeforePause = 1f;

    // True while the menu is up, so other input handling can stand down.
    public static bool MenuOpen { get; private set; }

    // Slider value 0-1 turned into the multiplier the camera rotates with.
    public static float CameraSensitivity =>
        Mathf.Lerp(MinSensitivity, MaxSensitivity, Read(SensitivityKey));

    private static float Read(string key, float fallback = DefaultSliderValue) =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(key, fallback));

    private static float Decibels(float value) =>
        value <= 0f ? -80f : Mathf.Log10(value) * 20f;

    private void Start()
    {
        ResolveReferences();

        Bind(cameraSensitivitySlider, Read(SensitivityKey), SetCameraSensitivity);
        Bind(musicVolumeSlider, Read(MusicKey), SetMusicVolume);
        Bind(sfxVolumeSlider, Read(SfxKey), SetSfxVolume);
        Bind(masterVolumeSlider, Read(MasterKey), SetMasterVolume);

        // Put the saved values in place straight away.
        ApplyVolumeSettings();
        ApplySensitivity(Read(SensitivityKey));

        if (menuPanel != null)
        {
            if (menuPanel == gameObject)
            {
                Debug.LogWarning(
                    "GameSettings: put the settings menu on a child object. " +
                    "A menu on the same GameObject disables this script " +
                    "whenever it closes.",
                    this
                );
            }

            menuPanel.SetActive(false);
        }

        menuOpen = false;
        MenuOpen = false;
    }

    private void Update()
    {
        if (menuPanel == null || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    // Finds what was left empty in the Inspector.
    private void ResolveReferences()
    {
        if (audioMixer == null)
        {
            foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source != null && source.outputAudioMixerGroup != null)
                {
                    audioMixer = source.outputAudioMixerGroup.audioMixer;
                    break;
                }
            }
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
        }
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

    public void ToggleMenu() => SetMenuOpen(!menuOpen);

    public void OpenMenu() => SetMenuOpen(true);

    public void CloseMenu() => SetMenuOpen(false);

    private void SetMenuOpen(bool open)
    {
        if (menuOpen == open)
        {
            return;
        }

        menuOpen = open;
        MenuOpen = open;

        if (menuPanel != null)
        {
            menuPanel.SetActive(open);
        }

        if (!pauseWhileOpen)
        {
            return;
        }

        if (open)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    // Freeze time and silence whatever is playing right now.
    private void PauseGame()
    {
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        pausedSources.Clear();

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source == null || !source.isPlaying)
            {
                continue;
            }

            source.Pause();
            pausedSources.Add(source);
        }
    }

    // Undo PauseGame(): only the sources that were playing come back, so menu
    // sounds started while it was open keep playing.
    private void ResumeGame()
    {
        foreach (AudioSource source in pausedSources)
        {
            if (source != null)
            {
                source.UnPause();
            }
        }

        pausedSources.Clear();
        Time.timeScale = timeScaleBeforePause <= 0f ? 1f : timeScaleBeforePause;
    }

    public void SetCameraSensitivity(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SensitivityKey, clamped);
        ApplySensitivity(clamped);
    }

    public void SetMusicVolume(float value)
    {
        ApplyVolume(MusicKey, musicParameter, value);
    }

    public void SetSfxVolume(float value)
    {
        ApplyVolume(SfxKey, sfxParameter, value);

        // UI sounds ride along with the SFX slider.
        SetMixerVolume(uiParameter, Mathf.Clamp01(value));
    }

    public void SetMasterVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterKey, clamped);

        // Every source obeys the listener, even the ones that are not routed
        // through the mixer.
        AudioListener.volume = clamped;
    }

    private void ApplyVolume(string key, string parameter, float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(key, clamped);
        SetMixerVolume(parameter, clamped);
    }

    private void SetMixerVolume(string parameter, float value)
    {
        if (audioMixer == null || string.IsNullOrEmpty(parameter))
        {
            return;
        }

        audioMixer.SetFloat(parameter, Decibels(value));
    }

    private void ApplySensitivity(float sliderValue)
    {
        if (cameraController != null)
        {
            cameraController.sensitivity = Mathf.Lerp(
                MinSensitivity,
                MaxSensitivity,
                Mathf.Clamp01(sliderValue)
            );
        }
    }

    private void ApplyVolumeSettings()
    {
        AudioListener.volume = Read(MasterKey);
        SetMixerVolume(musicParameter, Read(MusicKey));
        SetMixerVolume(sfxParameter, Read(SfxKey));
        SetMixerVolume(uiParameter, Read(SfxKey));
    }

    // Used by other systems (the sound manager) to restore the saved mix.
    public static void ApplyAudio(AudioMixer mixer)
    {
        AudioListener.volume = Read(MasterKey);

        if (mixer == null)
        {
            return;
        }

        mixer.SetFloat(DefaultMusicParameter, Decibels(Read(MusicKey)));
        mixer.SetFloat(DefaultSfxParameter, Decibels(Read(SfxKey)));
        mixer.SetFloat(DefaultUiParameter, Decibels(Read(SfxKey)));
    }

    private void OnDisable()
    {
        // Never leave the game frozen or muted when this object goes away.
        if (menuOpen)
        {
            SetMenuOpen(false);
        }

        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        MenuOpen = false;

        if (cameraSensitivitySlider != null) cameraSensitivitySlider.onValueChanged.RemoveListener(SetCameraSensitivity);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
    }
}

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class OptionsMenu : MonoBehaviour
{
    const string PrefMusicVolume = "Options.MusicVolume";
    const string PrefSfxVolume = "Options.SfxVolume";
    const string PrefFullscreen = "Options.Fullscreen";
    const string PrefLightIntensity = "Options.LightIntensity";

    const string MixerMusicVolume = "MusicVolume";
    const string MixerSfxVolume = "SfxVolume";

    [Header("Window")]
    [SerializeField] CanvasGroup windowGroup;
    [SerializeField] CanvasGroup mainMenuContentGroup;

    [Header("Tabs")]
    [SerializeField] GameObject panelControles;
    [SerializeField] GameObject panelAudio;
    [SerializeField] GameObject panelPantalla;
    [SerializeField] GameObject panelIluminacion;

    [Header("Controls")]
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] TextMeshProUGUI controlsListText;

    [Header("Audio")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;

    [Header("Display")]
    [SerializeField] Toggle fullscreenToggle;

    [Header("Lighting")]
    [SerializeField] Light2D globalLight;
    [SerializeField] Slider lightingSlider;

    OptionsTab activeTab = OptionsTab.Controles;

    enum OptionsTab
    {
        Controles,
        Audio,
        Pantalla,
        Iluminacion
    }

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        CampusQuestActiveControls.Changed += OnActiveControlsChanged;
        if (inputActions != null)
            CampusQuestActiveControls.Bind(inputActions);
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        CampusQuestActiveControls.Changed -= OnActiveControlsChanged;
    }

    void OnActiveControlsChanged()
    {
        if (windowGroup != null && windowGroup.alpha > 0.5f && activeTab == OptionsTab.Controles)
            RefreshControlsList();
    }

    void Awake()
    {
        LoadSettings();
    }

    void Start()
    {
        HideImmediate();
        ShowTab(OptionsTab.Controles);
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (windowGroup != null && windowGroup.alpha > 0.5f && activeTab == OptionsTab.Controles)
            RefreshControlsList();
    }

    public void Open()
    {
        if (windowGroup == null)
            return;

        windowGroup.alpha = 1f;
        windowGroup.interactable = true;
        windowGroup.blocksRaycasts = true;

        if (mainMenuContentGroup != null)
        {
            mainMenuContentGroup.interactable = false;
            mainMenuContentGroup.blocksRaycasts = false;
        }

        ShowTab(OptionsTab.Controles);
    }

    public void Close()
    {
        HideImmediate();

        if (mainMenuContentGroup != null)
        {
            mainMenuContentGroup.interactable = true;
            mainMenuContentGroup.blocksRaycasts = true;
        }
    }

    public void ShowTabControles() => ShowTab(OptionsTab.Controles);
    public void ShowTabAudio() => ShowTab(OptionsTab.Audio);
    public void ShowTabPantalla() => ShowTab(OptionsTab.Pantalla);
    public void ShowTabIluminacion() => ShowTab(OptionsTab.Iluminacion);

    void ShowTab(OptionsTab tab)
    {
        activeTab = tab;

        if (panelControles != null)
            panelControles.SetActive(tab == OptionsTab.Controles);
        if (panelAudio != null)
            panelAudio.SetActive(tab == OptionsTab.Audio);
        if (panelPantalla != null)
            panelPantalla.SetActive(tab == OptionsTab.Pantalla);
        if (panelIluminacion != null)
            panelIluminacion.SetActive(tab == OptionsTab.Iluminacion);

        if (tab == OptionsTab.Controles)
            RefreshControlsList();
    }

    void RefreshControlsList()
    {
        if (controlsListText == null || inputActions == null)
            return;

        var map = inputActions.FindActionMap("Player", throwIfNotFound: false);
        if (map == null)
        {
            controlsListText.text = "Sin mapa Player";
            return;
        }

        var group = ResolveControlSchemeGroup();
        var builder = new StringBuilder();
        AppendActionBinding(builder, map, "Move", "Mover", group);
        AppendActionBinding(builder, map, "Sprint", "Correr", group);
        AppendActionBinding(builder, map, "Attack", "Golpe", group);
        AppendActionBinding(builder, map, "Shoot", "Disparar", group);
        AppendActionBinding(builder, map, "Interact", "Interactuar", group);

        controlsListText.text = builder.ToString().TrimEnd();
    }

    static void AppendActionBinding(StringBuilder builder, InputActionMap map, string actionName, string label, string group)
    {
        var action = map.FindAction(actionName, throwIfNotFound: false);
        if (action == null)
            return;

        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(action.GetBindingDisplayString(group: group));
    }

    static string ResolveControlSchemeGroup()
    {
        if (CampusQuestActiveControls.ActiveScheme == CampusQuestActiveControls.GamepadScheme)
            return CampusQuestActiveControls.GamepadScheme;

        if (Application.isMobilePlatform)
            return "Touch";

        return CampusQuestActiveControls.KeyboardMouse;
    }

    void LoadSettings()
    {
        var music = PlayerPrefs.GetFloat(PrefMusicVolume, 0.8f);
        var sfx = PlayerPrefs.GetFloat(PrefSfxVolume, 0.8f);
        var fullscreen = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        var lightDefault = globalLight != null ? globalLight.intensity : 1f;
        var light = PlayerPrefs.GetFloat(PrefLightIntensity, lightDefault);
        if (lightingSlider != null)
            light = Mathf.Clamp(light, lightingSlider.minValue, lightingSlider.maxValue);

        ApplyMixerVolume(MixerMusicVolume, music);
        ApplyMixerVolume(MixerSfxVolume, sfx);

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(music);
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfx);
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            Screen.fullScreen = fullscreen;
        }

        if (lightingSlider != null)
        {
            lightingSlider.SetValueWithoutNotify(light);
            lightingSlider.onValueChanged.RemoveListener(OnLightingChanged);
            lightingSlider.onValueChanged.AddListener(OnLightingChanged);
        }

        ApplyLightIntensity(light);
    }

    void OnMusicSliderChanged(float value)
    {
        ApplyMixerVolume(MixerMusicVolume, value);
        PlayerPrefs.SetFloat(PrefMusicVolume, value);
        PlayerPrefs.Save();
    }

    void OnSfxSliderChanged(float value)
    {
        ApplyMixerVolume(MixerSfxVolume, value);
        PlayerPrefs.SetFloat(PrefSfxVolume, value);
        PlayerPrefs.Save();
    }

    void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(PrefFullscreen, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnLightingChanged(float value)
    {
        ApplyLightIntensity(value);
        PlayerPrefs.SetFloat(PrefLightIntensity, value);
        PlayerPrefs.Save();
    }

    void ApplyMixerVolume(string parameter, float linearVolume)
    {
        if (audioMixer == null)
            return;

        var db = linearVolume <= 0.0001f ? -80f : Mathf.Log10(linearVolume) * 20f;
        audioMixer.SetFloat(parameter, db);
    }

    void ApplyLightIntensity(float value)
    {
        if (globalLight != null)
            globalLight.intensity = value;
    }

    void HideImmediate()
    {
        if (windowGroup == null)
            return;

        windowGroup.alpha = 0f;
        windowGroup.interactable = false;
        windowGroup.blocksRaycasts = false;
    }
}

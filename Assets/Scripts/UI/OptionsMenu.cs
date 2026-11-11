using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
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

    Button btnControles;
    Button btnAudio;
    Button btnPantalla;
    Button btnIluminacion;
    Button btnVolver;
    Button btnOpcionesMainMenu;

    InputAction cancelAction;
    OptionsTab activeTab = OptionsTab.Controles;

    public bool IsWindowOpen =>
        windowGroup != null && windowGroup.alpha > 0.5f && windowGroup.interactable;

    public InputActionAsset InputActions => inputActions;

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

        cancelAction = inputActions != null ? inputActions.FindAction("UI/Cancel", false) : null;
        if (cancelAction != null)
        {
            cancelAction.performed += OnCancelPerformed;
            if (!cancelAction.enabled)
                cancelAction.Enable();
        }
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        CampusQuestActiveControls.Changed -= OnActiveControlsChanged;

        if (cancelAction != null)
            cancelAction.performed -= OnCancelPerformed;
    }

    void OnCancelPerformed(InputAction.CallbackContext _)
    {
        if (IsWindowOpen)
            Close();
    }

    void OnActiveControlsChanged()
    {
        if (IsWindowOpen && activeTab == OptionsTab.Controles)
            RefreshControlsList();
    }

    void Awake()
    {
        LoadSettings();
        CacheOptionsUiRefs();
        ApplyTabRowNavigation();
        ApplySelectableColors();
    }

    void Start()
    {
        HideImmediate();
        ShowTab(OptionsTab.Controles);
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (IsWindowOpen && activeTab == OptionsTab.Controles)
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
        CampusQuestUiGamepad.Focus(btnControles != null ? btnControles.gameObject : null);
    }

    public void Close()
    {
        HideImmediate();

        if (mainMenuContentGroup != null)
        {
            mainMenuContentGroup.interactable = true;
            mainMenuContentGroup.blocksRaycasts = true;
        }

        if (btnOpcionesMainMenu != null)
            CampusQuestUiGamepad.Focus(btnOpcionesMainMenu.gameObject);
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

        RefreshActiveTabNavigation();

        var es = EventSystem.current;
        if (es == null || !IsWindowOpen)
            return;

        var selected = es.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
            FocusTab(tab);
    }

    void FocusTab(OptionsTab tab)
    {
        switch (tab)
        {
            case OptionsTab.Controles:
                CampusQuestUiGamepad.Focus(btnControles != null ? btnControles.gameObject : null);
                break;
            case OptionsTab.Audio:
                CampusQuestUiGamepad.Focus(
                    musicSlider != null ? musicSlider.gameObject
                        : btnAudio != null ? btnAudio.gameObject : null);
                break;
            case OptionsTab.Pantalla:
                CampusQuestUiGamepad.Focus(
                    fullscreenToggle != null ? fullscreenToggle.gameObject
                        : btnPantalla != null ? btnPantalla.gameObject : null);
                break;
            case OptionsTab.Iluminacion:
                CampusQuestUiGamepad.Focus(
                    lightingSlider != null ? lightingSlider.gameObject
                        : btnIluminacion != null ? btnIluminacion.gameObject : null);
                break;
        }
    }

    void CacheOptionsUiRefs()
    {
        var root = transform.Find("OptionsFrame/Fondo_Oscuro");
        if (root == null)
            return;

        btnControles = root.Find("TabRow/Btn_Controles")?.GetComponent<Button>();
        btnAudio = root.Find("TabRow/Btn_Audio")?.GetComponent<Button>();
        btnPantalla = root.Find("TabRow/Btn_Pantalla")?.GetComponent<Button>();
        btnIluminacion = root.Find("TabRow/Btn_Iluminacion")?.GetComponent<Button>();
        btnVolver = root.Find("Btn_Volver")?.GetComponent<Button>();
        btnOpcionesMainMenu = GameObject.Find("Btn_Opciones")?.GetComponent<Button>();
    }

    public static void ApplyTabRowNavigation(
        Button controles,
        Button audio,
        Button pantalla,
        Button iluminacion)
    {
        if (controles == null || audio == null || pantalla == null || iluminacion == null)
            return;

        CampusQuestUiGamepad.SetExplicitHorizontal(controles, audio);
        CampusQuestUiGamepad.SetExplicitHorizontal(audio, pantalla);
        CampusQuestUiGamepad.SetExplicitHorizontal(pantalla, iluminacion);

        var navControles = controles.navigation;
        navControles.selectOnLeft = iluminacion;
        controles.navigation = navControles;

        var navIluminacion = iluminacion.navigation;
        navIluminacion.selectOnRight = controles;
        iluminacion.navigation = navIluminacion;
    }

    void ApplyTabRowNavigation()
    {
        ApplyTabRowNavigation(btnControles, btnAudio, btnPantalla, btnIluminacion);
    }

    void ApplySelectableColors()
    {
        CampusQuestUiGamepad.SetSelectedColor(btnControles);
        CampusQuestUiGamepad.SetSelectedColor(btnAudio);
        CampusQuestUiGamepad.SetSelectedColor(btnPantalla);
        CampusQuestUiGamepad.SetSelectedColor(btnIluminacion);
        CampusQuestUiGamepad.SetSelectedColor(btnVolver);
        CampusQuestUiGamepad.SetSelectedColor(musicSlider);
        CampusQuestUiGamepad.SetSelectedColor(sfxSlider);
        CampusQuestUiGamepad.SetSelectedColor(fullscreenToggle);
        CampusQuestUiGamepad.SetSelectedColor(lightingSlider);
    }

    void RefreshActiveTabNavigation()
    {
        if (btnVolver == null)
            return;

        Button activeTabButton = activeTab switch
        {
            OptionsTab.Controles => btnControles,
            OptionsTab.Audio => btnAudio,
            OptionsTab.Pantalla => btnPantalla,
            OptionsTab.Iluminacion => btnIluminacion,
            _ => btnControles
        };

        Selectable firstControl = activeTab switch
        {
            OptionsTab.Controles => btnControles,
            OptionsTab.Audio => musicSlider,
            OptionsTab.Pantalla => fullscreenToggle,
            OptionsTab.Iluminacion => lightingSlider,
            _ => btnControles
        };

        if (activeTabButton != null)
        {
            var tabNav = activeTabButton.navigation;
            tabNav.mode = Navigation.Mode.Explicit;
            tabNav.selectOnDown = firstControl != activeTabButton ? firstControl : btnVolver;
            activeTabButton.navigation = tabNav;
        }

        switch (activeTab)
        {
            case OptionsTab.Controles:
                if (btnControles != null)
                    CampusQuestUiGamepad.SetExplicitVertical(btnControles, btnVolver);
                break;

            case OptionsTab.Audio:
                if (btnAudio != null && musicSlider != null)
                    CampusQuestUiGamepad.SetExplicitVertical(btnAudio, musicSlider);
                if (musicSlider != null && sfxSlider != null)
                    CampusQuestUiGamepad.SetExplicitVertical(musicSlider, sfxSlider);
                if (sfxSlider != null)
                    CampusQuestUiGamepad.SetExplicitVertical(sfxSlider, btnVolver);
                CampusQuestUiGamepad.ClearHorizontal(musicSlider);
                CampusQuestUiGamepad.ClearHorizontal(sfxSlider);
                break;

            case OptionsTab.Pantalla:
                if (btnPantalla != null && fullscreenToggle != null)
                    CampusQuestUiGamepad.SetExplicitVertical(btnPantalla, fullscreenToggle);
                if (fullscreenToggle != null)
                    CampusQuestUiGamepad.SetExplicitVertical(fullscreenToggle, btnVolver);
                CampusQuestUiGamepad.ClearHorizontal(fullscreenToggle);
                break;

            case OptionsTab.Iluminacion:
                if (btnIluminacion != null && lightingSlider != null)
                    CampusQuestUiGamepad.SetExplicitVertical(btnIluminacion, lightingSlider);
                if (lightingSlider != null)
                    CampusQuestUiGamepad.SetExplicitVertical(lightingSlider, btnVolver);
                CampusQuestUiGamepad.ClearHorizontal(lightingSlider);
                break;
        }
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

using System.Collections;
using CampusQuest.Rectoria;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-50)]
public class MainMenu : MonoBehaviour
{
    const string BtnIniciarName = "Btn_Iniciar";
    const string BtnOpcionesName = "Btn_Opciones";
    const string BtnLevelsName = "Btn_Levels";
    const string BtnSalirName = "Btn_Salir";

    [Header("Splash groups")]
    [SerializeField] CanvasGroup splashLogoGroup;
    [SerializeField] CanvasGroup splashStudioGroup;
    [SerializeField] CanvasGroup mainMenuContentGroup;

    [Header("Logo timing (seconds)")]
    [SerializeField] float logoFadeInDuration = 0.5f;
    [SerializeField] float logoHoldDuration = 1.5f;
    [SerializeField] float logoFadeOutDuration = 0.4f;

    [Header("Studio timing (seconds)")]
    [SerializeField] float studioFadeInDuration = 0.5f;
    [SerializeField] float studioHoldDuration = 2f;
    [SerializeField] float studioFadeOutDuration = 0.4f;
    [SerializeField] float studioSkippedFadeOutDuration = 0.2f;

    [Header("Menu timing (seconds)")]
    [SerializeField] float menuFadeInDuration = 0.4f;

    [Header("Audio")]
    [SerializeField] MenuBackgroundMusic menuBackgroundMusic;

    [Header("Gamepad")]
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] OptionsMenu optionsMenu;
    [SerializeField] LevelsMenu levelsMenu;
    [SerializeField] GameObject btnIniciar;

    bool studioSkipRequested;
    bool mainMenuInteractable;

    void Awake()
    {
        if (optionsMenu == null)
            optionsMenu = Object.FindAnyObjectByType<OptionsMenu>();

        if (levelsMenu == null)
            levelsMenu = Object.FindAnyObjectByType<LevelsMenu>();

        if (levelsMenu == null)
            levelsMenu = EnsureLevelsWindow();

        if (inputActions == null && optionsMenu != null)
            inputActions = optionsMenu.InputActions;

        CampusQuestUiGamepad.ConfigureUiInputModule(inputActions);
        EnsureSalirButton();
        ResizePanelForSalir();
        ResolveMenuButtons();
        ApplyMainMenuNavigation();
        WireStartButton();
        WireLevelsButton();
    }

    LevelsMenu EnsureLevelsWindow()
    {
        var canvas = GameObject.Find("Canvas")?.transform;
        var templateBorder = GameObject.Find("Canvas/MainMenu_Content/Panel/Borde_Dorado");
        var templateBtn = GameObject.Find(BtnOpcionesName);
        return LevelsMenuFactory.Ensure(canvas, templateBorder, templateBtn, mainMenuContentGroup);
    }

    void WireStartButton()
    {
        var iniciar = btnIniciar != null
            ? btnIniciar.GetComponent<Button>()
            : GameObject.Find(BtnIniciarName)?.GetComponent<Button>();
        if (iniciar == null)
            return;

        iniciar.onClick.RemoveListener(StartGame);
        iniciar.onClick.AddListener(StartGame);
    }

    public void StartGame()
    {
        EnrollmentMission.ClearSavedProgress();
        SceneManager.LoadScene(EnrollmentMission.SceneCargaRectoria);
    }

    void WireLevelsButton()
    {
        if (levelsMenu == null)
            return;

        var levelsBtn = GameObject.Find(BtnLevelsName)?.GetComponent<Button>();
        if (levelsBtn == null)
            return;

        levelsBtn.onClick.RemoveListener(levelsMenu.Open);
        levelsBtn.onClick.AddListener(levelsMenu.Open);
    }

    void Start()
    {
        PrepareGroup(splashLogoGroup);
        PrepareGroup(splashStudioGroup);
        PrepareGroup(mainMenuContentGroup);
        mainMenuInteractable = false;
        StartCoroutine(PlayIntroSequence());
    }

    void Update()
    {
        if (!mainMenuInteractable || OverlayWindowOpen())
            return;

        var es = EventSystem.current;
        if (es == null || btnIniciar == null)
            return;

        if (es.currentSelectedGameObject == null)
            CampusQuestUiGamepad.Focus(btnIniciar);
    }

    bool OverlayWindowOpen()
    {
        if (optionsMenu != null && optionsMenu.IsWindowOpen)
            return true;

        return levelsMenu != null && levelsMenu.IsWindowOpen;
    }

    void ResolveMenuButtons()
    {
        if (btnIniciar == null)
            btnIniciar = GameObject.Find(BtnIniciarName);
    }

    void EnsureSalirButton()
    {
        if (GameObject.Find(BtnSalirName) != null)
            return;

        var template = GameObject.Find(BtnOpcionesName);
        if (template == null)
            return;

        var panel = template.transform.parent;
        var salirGo = Instantiate(template, panel);
        salirGo.name = BtnSalirName;

        var label = salirGo.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "SALIR";

        var button = salirGo.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Quit);
        }

        salirGo.transform.SetAsLastSibling();

    }

    static void ResizePanelForSalir()
    {
        if (GameObject.Find(BtnSalirName) == null)
            return;

        var panel = GameObject.Find("Canvas/MainMenu_Content/Panel")?.GetComponent<RectTransform>();
        if (panel == null)
            return;

        panel.sizeDelta = new Vector2(460f, 425f);
        panel.anchoredPosition = new Vector2(0f, -195f);
    }

    public static void ApplyMainMenuNavigation(
        Button iniciar,
        Button opciones,
        Button niveles,
        Button salir)
    {
        if (iniciar == null || opciones == null || niveles == null || salir == null)
            return;

        CampusQuestUiGamepad.SetExplicitVertical(iniciar, opciones);
        CampusQuestUiGamepad.SetExplicitVertical(opciones, niveles);
        CampusQuestUiGamepad.SetExplicitVertical(niveles, salir);

        var navIniciar = iniciar.navigation;
        navIniciar.selectOnUp = null;
        iniciar.navigation = navIniciar;

        var navSalir = salir.navigation;
        navSalir.selectOnDown = null;
        salir.navigation = navSalir;

        CampusQuestUiGamepad.SetSelectedColor(iniciar);
        CampusQuestUiGamepad.SetSelectedColor(opciones);
        CampusQuestUiGamepad.SetSelectedColor(niveles);
        CampusQuestUiGamepad.SetSelectedColor(salir);
    }

    void ApplyMainMenuNavigation()
    {
        var iniciar = GameObject.Find(BtnIniciarName)?.GetComponent<Button>();
        var opciones = GameObject.Find(BtnOpcionesName)?.GetComponent<Button>();
        var niveles = GameObject.Find(BtnLevelsName)?.GetComponent<Button>();
        var salir = GameObject.Find(BtnSalirName)?.GetComponent<Button>();
        ApplyMainMenuNavigation(iniciar, opciones, niveles, salir);
    }

    void FocusMainMenu()
    {
        CampusQuestUiGamepad.Focus(btnIniciar);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void PrepareGroup(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    IEnumerator PlayIntroSequence()
    {
        yield return FadeCanvasGroup(splashLogoGroup, 0f, 1f, logoFadeInDuration);
        yield return new WaitForSeconds(logoHoldDuration);
        yield return FadeCanvasGroup(splashLogoGroup, 1f, 0f, logoFadeOutDuration);

        yield return PlayStudioSplash();

        yield return FadeCanvasGroup(mainMenuContentGroup, 0f, 1f, menuFadeInDuration);
        EnableInteraction(mainMenuContentGroup);
        mainMenuInteractable = true;
        FocusMainMenu();
    }

    IEnumerator PlayStudioSplash()
    {
        if (splashStudioGroup == null)
            yield break;

        var studioTotalDuration = studioFadeInDuration + studioHoldDuration + studioFadeOutDuration;
        var musicStartTime = studioTotalDuration * 0.75f;
        var fadeInEnd = studioFadeInDuration;
        var holdEnd = studioFadeInDuration + studioHoldDuration;
        var elapsed = 0f;
        var musicStarted = false;
        studioSkipRequested = false;

        void TryStartMusic(bool force)
        {
            if (musicStarted || menuBackgroundMusic == null)
                return;

            if (!force && elapsed < musicStartTime)
                return;

            musicStarted = true;
            menuBackgroundMusic.PlayLoop();
        }

        splashStudioGroup.alpha = 0f;

        while (true)
        {
            if (elapsed >= fadeInEnd && elapsed < holdEnd && !studioSkipRequested && WasScreenTapThisFrame())
            {
                studioSkipRequested = true;
                TryStartMusic(force: true);
            }

            if (elapsed < fadeInEnd)
            {
                var t = studioFadeInDuration <= 0f ? 1f : elapsed / studioFadeInDuration;
                splashStudioGroup.alpha = Mathf.Lerp(0f, 1f, t);
            }
            else if (elapsed < holdEnd)
            {
                splashStudioGroup.alpha = 1f;
            }
            else
            {
                break;
            }

            TryStartMusic(force: false);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (studioSkipRequested)
            TryStartMusic(force: true);

        var fadeOutDuration = studioSkipRequested ? studioSkippedFadeOutDuration : studioFadeOutDuration;
        var fadeOutStart = elapsed;
        var fadeOutEnd = fadeOutStart + fadeOutDuration;
        var fadeFrom = splashStudioGroup.alpha;

        while (elapsed < fadeOutEnd)
        {
            TryStartMusic(force: false);

            var t = fadeOutDuration <= 0f
                ? 1f
                : Mathf.Clamp01((elapsed - fadeOutStart) / fadeOutDuration);
            splashStudioGroup.alpha = Mathf.Lerp(fadeFrom, 0f, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        splashStudioGroup.alpha = 0f;
        TryStartMusic(force: false);
    }

    static bool WasScreenTapThisFrame()
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                return true;
        }

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            return true;

        return false;
    }

    static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        var elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    static void EnableInteraction(CanvasGroup group)
    {
        if (group == null)
            return;

        group.interactable = true;
        group.blocksRaycasts = true;
    }
}

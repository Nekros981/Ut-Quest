using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-50)]
public sealed class CampusQuestGameOver : MonoBehaviour
{
    const string RestartButtonName = "Btn_Reiniciar";

    [SerializeField] InputActionAsset inputActions;
    [SerializeField] GameObject restartButton;

    GameObject restartFocus;

    void Awake()
    {
        if (restartButton == null)
        {
            var found = GameObject.Find(RestartButtonName);
            if (found != null)
                restartButton = found;
        }

        restartFocus = restartButton;

        ConfigureUiInputModule();
    }

    void Start()
    {
        if (inputActions != null)
            CampusQuestActiveControls.Bind(inputActions);

        FocusRestart();
    }

    void Update()
    {
        var es = EventSystem.current;
        if (es == null || restartFocus == null)
            return;

        if (es.currentSelectedGameObject == null)
            es.SetSelectedGameObject(restartFocus);
    }

    void ConfigureUiInputModule()
    {
        if (inputActions == null)
            return;

        var module = Object.FindAnyObjectByType<InputSystemUIInputModule>();
        if (module == null)
            return;

        module.actionsAsset = inputActions;
        module.AssignDefaultActions();
    }

    void FocusRestart()
    {
        if (restartFocus == null)
            return;

        var es = EventSystem.current;
        if (es == null)
            return;

        es.firstSelectedGameObject = restartFocus;
        es.SetSelectedGameObject(restartFocus);
    }

    public void Restart()
    {
        CampusQuestSecuazLoot.ResetForRestart();
        CampusQuestAmbushKeycard.ResetForRestart();
        CampusQuestMateoPlayerInput.ResetGameOverFlag();
        CampusQuestRunState.Clear();
        SceneManager.LoadScene(CampusQuestRunState.SceneToReloadAfterGameOver());
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static void ApplyGamepadNavigation(Button restart, Button quit)
    {
        if (restart == null || quit == null)
            return;

        var navRestart = restart.navigation;
        navRestart.mode = Navigation.Mode.Explicit;
        navRestart.selectOnDown = quit;
        navRestart.selectOnUp = null;
        restart.navigation = navRestart;

        var navQuit = quit.navigation;
        navQuit.mode = Navigation.Mode.Explicit;
        navQuit.selectOnUp = restart;
        navQuit.selectOnDown = null;
        quit.navigation = navQuit;

        var selected = new Color(0.1764706f, 0.41568628f, 0.30980393f, 1f);
        SetButtonSelectedColor(restart, selected);
        SetButtonSelectedColor(quit, selected);
    }

    static void SetButtonSelectedColor(Button button, Color selected)
    {
        if (button == null)
            return;

        var colors = button.colors;
        colors.selectedColor = selected;
        button.colors = colors;
    }
}

using CampusQuest.Rectoria;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelsMenu : MonoBehaviour
{
    const string SceneRectoria = "Carga_Rectoria";
    const string SceneSp = "Sp_Bosque";

    [SerializeField] CanvasGroup windowGroup;
    [SerializeField] CanvasGroup mainMenuContentGroup;

    public void Init(CanvasGroup window, CanvasGroup mainMenu)
    {
        windowGroup = window;
        mainMenuContentGroup = mainMenu;
        CacheUiRefs();
        ApplyLevelButtonNavigation();
    }

    Button btnRectoria;
    Button btnSp;
    Button btnIdgs;
    Button btnLevelsMainMenu;

    InputAction cancelAction;

    public bool IsWindowOpen =>
        windowGroup != null && windowGroup.alpha > 0.5f && windowGroup.interactable;

    void OnEnable()
    {
        cancelAction = null;
        var input = Object.FindAnyObjectByType<OptionsMenu>()?.InputActions;
        if (input != null)
        {
            cancelAction = input.FindAction("UI/Cancel", false);
            if (cancelAction != null)
            {
                cancelAction.performed += OnCancelPerformed;
                if (!cancelAction.enabled)
                    cancelAction.Enable();
            }
        }
    }

    void OnDisable()
    {
        if (cancelAction != null)
            cancelAction.performed -= OnCancelPerformed;
    }

    void OnCancelPerformed(InputAction.CallbackContext _)
    {
        if (IsWindowOpen)
            Close();
    }

    void Awake()
    {
        CacheUiRefs();
        ApplyLevelButtonNavigation();
    }

    void Start()
    {
        HideImmediate();
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

        CampusQuestUiGamepad.Focus(btnRectoria != null ? btnRectoria.gameObject : null);
    }

    public void Close()
    {
        HideImmediate();

        if (mainMenuContentGroup != null)
        {
            mainMenuContentGroup.interactable = true;
            mainMenuContentGroup.blocksRaycasts = true;
        }

        if (btnLevelsMainMenu != null)
            CampusQuestUiGamepad.Focus(btnLevelsMainMenu.gameObject);
    }

    public void LoadRectoria()
    {
        EnrollmentMission.ClearSavedProgress();
        SceneManager.LoadScene(SceneRectoria);
    }

    public void LoadSp()
    {
        SceneManager.LoadScene(SceneSp);
    }

    public void LoadIdgs()
    {
    }

    void CacheUiRefs()
    {
        var inner = transform.Find("LevelsFrame/Fondo_Oscuro");
        if (inner != null)
        {
            btnRectoria = inner.Find("Btn_Rectoria")?.GetComponent<Button>();
            btnSp = inner.Find("Btn_SP")?.GetComponent<Button>();
            btnIdgs = inner.Find("Btn_IDGS")?.GetComponent<Button>();
        }

        btnLevelsMainMenu = GameObject.Find("Btn_Levels")?.GetComponent<Button>();
    }

    public static void ApplyLevelButtonNavigation(Button rectoria, Button sp, Button idgs)
    {
        if (rectoria == null || sp == null || idgs == null)
            return;

        CampusQuestUiGamepad.SetExplicitVertical(rectoria, sp);
        CampusQuestUiGamepad.SetExplicitVertical(sp, idgs);

        var navRectoria = rectoria.navigation;
        navRectoria.selectOnUp = null;
        rectoria.navigation = navRectoria;

        var navIdgs = idgs.navigation;
        navIdgs.selectOnDown = null;
        idgs.navigation = navIdgs;

        CampusQuestUiGamepad.SetSelectedColor(rectoria);
        CampusQuestUiGamepad.SetSelectedColor(sp);
        CampusQuestUiGamepad.SetSelectedColor(idgs);
    }

    void ApplyLevelButtonNavigation()
    {
        ApplyLevelButtonNavigation(btnRectoria, btnSp, btnIdgs);
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

using System.Collections.Generic;
using CampusQuest.Dialogue;
using CampusQuest.IDGS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static CampusQuest.IDGS.IDGSDesktopUI;

[DefaultExecutionOrder(-32000)]
public sealed class CampusQuestPauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    static int toggleFrame = -1;
    public static bool BlocksInput => IsPaused || toggleFrame == Time.frameCount;
    public Canvas canvas { get; private set; }
    public GameObject Panel { get; private set; }
    public Button[] Buttons { get; private set; }
    public bool ExitRequested { get; private set; }
#if UNITY_EDITOR
    public static System.Action QuitOverrideForValidation;
#endif
    readonly Dictionary<CanvasGroup, Vector2Int> groups = new Dictionary<CanvasGroup, Vector2Int>();
    readonly Dictionary<Animator, float> animators = new Dictionary<Animator, float>();
    readonly List<ParticleSystem> particles = new List<ParticleSystem>();
    GameObject previousSelection;
    bool cursorVisible, audioPaused;
    CursorLockMode cursorLock;
    float timeScale;
    TMP_Text status;
    Button pauseShortcut;

    static bool PuzzleOpen()
    {
        return Object.FindObjectsByType<IDGSCablesChallenge>(FindObjectsSortMode.None).Length > 0 && Object.FindFirstObjectByType<IDGSCablesChallenge>().IsOpen
            || Object.FindFirstObjectByType<IDGSCircuitChallenge>()?.IsOpen == true
            || Object.FindFirstObjectByType<IDGSMemoryChallenge>()?.IsOpen == true
            || Object.FindFirstObjectByType<IDGSFilesChallenge>()?.IsOpen == true
            || Object.FindFirstObjectByType<IDGSServerChallenge>()?.IsOpen == true
            || Object.FindFirstObjectByType<IDGSVirusChallenge>()?.IsOpen == true
            || Object.FindFirstObjectByType<CampusQuest.Rectoria.ArchiveChallenge>()?.IsOpen == true
            || Object.FindFirstObjectByType<CampusQuest.Rectoria.CredentialChallenge>()?.IsBusy == true
            || Object.FindFirstObjectByType<CampusQuest.Rectoria.CashierChallenge>()?.IsOpen == true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        IsPaused = false; toggleFrame = -1;
#if UNITY_EDITOR
        QuitOverrideForValidation = null;
#endif
    }
    public static string LevelScope(string scene)
    {
        if (scene == "Rectoria_Tutorial_Arte_v01") return "rectoria";
        if (scene == "IDGS_Exterior" || scene == "IDGS_Docencia_Interior" || scene == "IDGS_CMD_Interior") return "idgs";
        if (scene == "Sp_Bosque" || scene == "Sp_Bunker") return "sp";
        return null;
    }
    public void Configure(CampusQuestGameplayConfig config)
    {
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        canvas = new GameObject("Pausa", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main; canvas.planeDistance = .8f; canvas.sortingOrder = 32760;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var shade = Box("Fondo", canvas.transform, Vector2.zero, Vector2.zero, new Color32(3, 10, 18, 242), true);
        Panel = shade.gameObject;
        shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one; shade.rectTransform.sizeDelta = Vector2.zero;
        var frame = Box("Marco", shade.transform, new Vector2(570, 540), Vector2.zero, Gold);
        Box("Panel", frame.transform, new Vector2(564, 534), Vector2.zero, Dark);
        Text("Título", frame.transform, config.font, "PAUSA", new Vector2(500, 70), new Vector2(0, 205), 38, Gold);
        string[] labels = { "Reanudar", "Reiniciar", "Salir", "Menú principal" };
        UnityEngine.Events.UnityAction[] actions = { Resume, RestartLevel, ExitGame, MainMenu };
        Buttons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            Buttons[i] = Button(labels[i], frame.transform, config.font, labels[i], new Vector2(450, 62), new Vector2(0, 105 - i * 83), actions[i]);
            Buttons[i].GetComponentInChildren<TMP_Text>().fontSize = 23;
            CampusQuestUiGamepad.SetSelectedColor(Buttons[i]);
        }
        for (int i = 0; i < 4; i++) Buttons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = Buttons[(i + 3) % 4], selectOnDown = Buttons[(i + 1) % 4] };
        status = Text("Indicaciones", frame.transform, config.font, "P / ESC / START · Reanudar", new Vector2(540, 54), new Vector2(0, -220), 16, Cyan);
        pauseShortcut = Button("Pausa rápida", canvas.transform, config.font, "PAUSA · P", new Vector2(160, 30), Vector2.zero, () => Open());
        var shortcutRect = pauseShortcut.GetComponent<RectTransform>(); shortcutRect.anchorMin = shortcutRect.anchorMax = new Vector2(1, 1); shortcutRect.anchoredPosition = new Vector2(-95, -18);
        Panel.SetActive(false);
    }
    void Update()
    {
        var keyboard = Keyboard.current; var pad = Gamepad.current;
        bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if ((keyboard != null && keyboard.pKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame) || (escape && (IsPaused || !PuzzleOpen())))
        {
            if (IsPaused) Resume(); else Open();
        }
    }
    public bool Open()
    {
        if (IsPaused || Panel == null || IDGSSceneTravel.IsChanging || LevelScope(SceneManager.GetActiveScene().name) == null) return false;
        IsPaused = true; toggleFrame = Time.frameCount;
        CampusQuestGameClock.SetPaused(true);
        timeScale = Time.timeScale; Time.timeScale = 0;
        audioPaused = AudioListener.pause; AudioListener.pause = true;
        cursorVisible = Cursor.visible; cursorLock = Cursor.lockState; Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        groups.Clear(); animators.Clear(); particles.Clear();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == canvas) continue;
            var group = c.GetComponent<CanvasGroup>();
            if (group == null) group = c.gameObject.AddComponent<CanvasGroup>();
            groups[group] = new Vector2Int(group.interactable ? 1 : 0, group.blocksRaycasts ? 1 : 0);
            group.interactable = false; group.blocksRaycasts = false;
        }
        foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None)) { animators[a] = a.speed; a.speed = 0; }
        foreach (var p in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) if (p.isPlaying) { particles.Add(p); p.Pause(false); }
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        Panel.SetActive(true);
        pauseShortcut.gameObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(Buttons[0].gameObject);
        return true;
    }
    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false; toggleFrame = Time.frameCount; CampusQuestGameClock.SetPaused(false);
        Time.timeScale = timeScale; AudioListener.pause = audioPaused;
        Cursor.visible = cursorVisible; Cursor.lockState = cursorLock;
        foreach (var pair in groups) if (pair.Key != null) { pair.Key.interactable = pair.Value.x != 0; pair.Key.blocksRaycasts = pair.Value.y != 0; }
        foreach (var pair in animators) if (pair.Key != null) pair.Key.speed = pair.Value;
        foreach (var p in particles) if (p != null) p.Play(false);
        groups.Clear(); animators.Clear(); particles.Clear();
        if (Panel != null) Panel.SetActive(false);
        if (pauseShortcut != null) pauseShortcut.gameObject.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
    }
    public void RestartLevel()
    {
        string level = LevelScope(SceneManager.GetActiveScene().name);
        if (!GameProgress.ResetLevel(level)) { status.text = "No se pudo guardar el reinicio. Intenta de nuevo."; return; }
        Resume(); ClearTransient(level == "sp");
        SceneManager.LoadScene(level == "idgs" ? "IDGS_Exterior" : level == "sp" ? "Sp_Bosque" : "Rectoria_Tutorial_Arte_v01");
    }
    static void ClearTransient(bool resetSp = false)
    {
        IDGSSceneTravel.ClearPending(); CampusQuestRunState.Clear();
        if (resetSp) { CampusQuestSecuazLoot.ResetForRestart(); CampusQuestAmbushKeycard.ResetForRestart(); CampusQuestMateoPlayerInput.ResetGameOverFlag(); }
    }
    public void MainMenu()
    {
        if (!GameProgress.Save()) { status.text = "No se pudo guardar. Intenta de nuevo."; return; }
        Resume(); ClearTransient(); Cursor.visible = true; Cursor.lockState = CursorLockMode.None; SceneManager.LoadScene("SampleScene");
    }
    public void ExitGame()
    {
        GameProgress.Save(); ExitRequested = true; Resume();
#if UNITY_EDITOR
        if (QuitOverrideForValidation != null) { QuitOverrideForValidation(); return; }
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void OnDestroy() { if (IsPaused) Resume(); }
}

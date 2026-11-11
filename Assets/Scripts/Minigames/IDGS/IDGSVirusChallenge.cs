using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CampusQuest.IDGS.IDGSDesktopUI;

namespace CampusQuest.IDGS
{
    public sealed class IDGSVirusChallenge : MonoBehaviour
    {
        public const string EventId = "idgs.cmd.virus";
        public IDGSInteractionPoint station;
        public IDGSPlayerMotor player;
        public IDGSInteractionController interactor;
        public GameObject gameplayHud;
        public TMP_FontAsset font;
        public TMP_Text stationState;
        public Canvas canvas { get; private set; }
        public GameObject panel { get; private set; }
        public Button[] Cells { get; private set; }
        public Button ResetButton { get; private set; }
        public Button FinishButton { get; private set; }
        public Button ExitButton { get; private set; }
        public TMP_Text Status { get; private set; }
        public IDGSVirusBoard Board { get; private set; }
        public int SelectedIndex { get; private set; }
        public bool IsOpen => modal != null && modal.IsOpen;
        public bool IsSolved { get; private set; }
        public bool HasCompleted => GameProgress.HasEvent(EventId);
        public bool RewardGrantedThisRun { get; private set; }
        readonly Image[] faces = new Image[16];
        readonly Image[] frames = new Image[16];
        readonly TMP_Text[] symbols = new TMP_Text[16];
        IDGSActivityModal modal;
        TMP_Text counter, infections, completion, finishLabel;
        float solvedAt;
        int lastInputFrame = -1;

        void Awake() { BuildInterface(); modal = new IDGSActivityModal(player, interactor, gameplayHud, panel); }
        void OnEnable() { GameProgress.Changed += RefreshStation; GameProgress.NewGameStarted += Close; RefreshStation(); }
        void OnDisable() { GameProgress.Changed -= RefreshStation; GameProgress.NewGameStarted -= Close; Close(); }
        public void OpenFromStation() { TryOpen(); }
        public bool TryOpen()
        {
            if (!isActiveAndEnabled || modal == null || !modal.TryOpen(station)) return false;
            Board = new IDGSVirusBoard(Random.Range(0, int.MaxValue)); SelectedIndex = 0;
            IsSolved = false; RewardGrantedThisRun = false; lastInputFrame = -1;
            FinishButton.gameObject.SetActive(false);
            completion.text = HasCompleted ? "ACTIVIDAD COMPLETADA · REPETICIÓN" : "CMD / LIMPIEZA DEL SISTEMA";
            Status.text = "Limpia las 16 casillas. Cada pulso cambia una cruz.";
            RefreshView(); return true;
        }
        public void PulseFromButton(int index) { TryPulse(index); }
        public bool TryPulse(int index)
        {
            if (!IsOpen || IsSolved || index < 0 || index >= 16 || lastInputFrame == Time.frameCount) return false;
            lastInputFrame = Time.frameCount; SelectedIndex = index; Board.Toggle(index);
            Status.text = "Un pulso cambia la casilla y sus vecinas ortogonales.";
            if (Board.IsSolved) TryComplete();
            RefreshView(); return true;
        }
        public void ResetBoard()
        {
            if (!IsOpen || IsSolved) return;
            Board.Reset(); SelectedIndex = 0; lastInputFrame = Time.frameCount;
            FinishButton.gameObject.SetActive(false);
            Status.text = "Tablero original restaurado. Inténtalo de nuevo.";
            RefreshView();
        }
        void TryComplete()
        {
            if (Board == null || !Board.IsSolved) return;
            bool complete = HasCompleted;
            RewardGrantedThisRun = GameProgress.RegisterEvent(EventId);
            FinishButton.gameObject.SetActive(true);
            if (!complete && !RewardGrantedThisRun)
            {
                Status.text = "No se pudo guardar. Puedes reintentar."; finishLabel.text = "REINTENTAR GUARDADO"; return;
            }
            IsSolved = true; solvedAt = CampusQuestGameClock.Now;
            Status.text = "¡Virus eliminado!"; finishLabel.text = "VOLVER AL CMD";
            completion.text = RewardGrantedThisRun ? "SISTEMA RESTAURADO" : "REPETICIÓN COMPLETADA";
        }
        public void FinishOrClose() { if (IsSolved) Close(); else { TryComplete(); RefreshView(); } }
        public void Close() { modal?.Close(); }
        void Update()
        {
            if (CampusQuestPauseMenu.BlocksInput) return;
            if (!IsOpen) return;
            if (IsSolved && CampusQuestGameClock.Now - solvedAt < 1) RefreshView();
            if (Time.frameCount == modal.OpenedFrame) return;
            if (CampusQuestActiveControls.CancelPressedThisFrame()) { Close(); return; }
            if (IsSolved)
            {
                if (CampusQuestGameClock.Now - solvedAt > 1 && DialogueSession.ConfirmPressedThisFrame()) Close();
                return;
            }
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) { ResetBoard(); return; }
                int x = SelectedIndex % 4, y = SelectedIndex / 4;
                if (keyboard.leftArrowKey.wasPressedThisFrame) x = Mathf.Max(0, x - 1);
                else if (keyboard.rightArrowKey.wasPressedThisFrame) x = Mathf.Min(3, x + 1);
                else if (keyboard.upArrowKey.wasPressedThisFrame) y = Mathf.Max(0, y - 1);
                else if (keyboard.downArrowKey.wasPressedThisFrame) y = Mathf.Min(3, y + 1);
                int next = y * 4 + x;
                if (next != SelectedIndex) { SelectedIndex = next; RefreshView(); }
            }
            if (CampusQuestActiveControls.PuzzleConfirmPressedThisFrame()) TryPulse(SelectedIndex);
        }
        void RefreshView()
        {
            counter.text = "MOVIMIENTOS " + Board.Moves;
            infections.text = "INFECTADOS " + Board.InfectedCount + "/16";
            ResetButton.interactable = !IsSolved;
            float elapsed = CampusQuestGameClock.Now - solvedAt;
            for (int i = 0; i < 16; i++)
            {
                bool infected = Board.IsInfected(i);
                frames[i].color = i == SelectedIndex && !IsSolved ? Gold : infected ? new Color32(145, 57, 71, 255) : new Color32(40, 110, 106, 255);
                Color clean = new Color32(16, 69, 65, 255);
                bool scanning = IsSolved && elapsed < .9f && i % 4 == (int)(elapsed * 8) % 4;
                faces[i].color = infected ? new Color32(166, 48, 66, 255) : scanning ? Cyan : clean;
                symbols[i].text = infected ? "X" : "OK";
                symbols[i].color = scanning ? Dark : infected ? Color.white : Cyan;
                Cells[i].interactable = !IsSolved;
            }
        }
        void RefreshStation()
        {
            bool complete = HasCompleted;
            station.displayName = "Elimina el virus" + (complete ? " · Completado" : "");
            if (stationState != null) { stationState.text = complete ? "[X] COMPLETADO" : "[ ] PENDIENTE"; stationState.color = complete ? Cyan : Gold; }
        }
        void BuildInterface()
        {
            canvas = new GameObject("Minijuego · Elimina el virus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            panel = canvas.gameObject;
            var shade = Box("Fondo", canvas.transform, Vector2.zero, Vector2.zero, new Color32(3, 12, 18, 235), true);
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one; shade.rectTransform.sizeDelta = Vector2.zero;
            var border = Box("Ventana", shade.transform, new Vector2(1100, 644), Vector2.zero, Gold);
            Box("Panel oscuro", border.transform, new Vector2(1092, 636), Vector2.zero, Dark);
            Box("Barra de título", border.transform, new Vector2(1080, 58), new Vector2(0, 284), new Color32(25, 52, 64, 255));
            Text("Título", border.transform, font, "CMD  /  ELIMINA EL VIRUS", new Vector2(850, 48), new Vector2(-60, 284), 26, Gold);
            ExitButton = Button("Salir", border.transform, font, "SALIR · ESC", new Vector2(150, 40), new Vector2(450, 284), Close);
            completion = Text("Estado", border.transform, font, "", new Vector2(1000, 30), new Vector2(0, 233), 15, Cyan);
            var board = Box("Sistema 4x4", border.transform, new Vector2(414, 414), new Vector2(-200, 0), new Color32(12, 33, 43, 255));
            Cells = new Button[16];
            for (int i = 0; i < 16; i++)
            {
                int index = i;
                var button = Button("Fragmento " + (i + 1), board.transform, font, "", new Vector2(92, 92), new Vector2((i % 4 - 1.5f) * 100, (1.5f - i / 4) * 100), () => PulseFromButton(index));
                Cells[i] = button; frames[i] = button.GetComponent<Image>();
                faces[i] = button.transform.Find("Interior").GetComponent<Image>();
                symbols[i] = button.GetComponentInChildren<TMP_Text>(); symbols[i].fontSize = 27;
            }
            var rules = Box("Guía", border.transform, new Vector2(350, 390), new Vector2(280, 7), new Color32(15, 36, 45, 255));
            Text("Objetivo", rules.transform, font, "LIMPIA EL SISTEMA", new Vector2(340, 34), new Vector2(0, 163), 20, Gold);
            Text("Reglas", rules.transform, font, "Un pulso cambia:\nla casilla elegida y\nsus cuatro vecinas.\n\nNo afecta diagonales.", new Vector2(330, 160), new Vector2(0, 59), 18, Color.white);
            Text("Leyenda", rules.transform, font, "X  Infectado\nOK  Limpio", new Vector2(320, 60), new Vector2(0, -58), 19, Cyan);
            Text("Controles", rules.transform, font, "FLECHAS · Seleccionar\nE / ENTER o CLIC · Pulsar", new Vector2(340, 64), new Vector2(0, -139), 16, Gold);
            ResetButton = Button("Reiniciar", border.transform, font, "REINICIAR · R", new Vector2(350, 40), new Vector2(280, -221), ResetBoard);
            Status = Text("Mensaje", border.transform, font, "", new Vector2(1040, 35), new Vector2(0, -251), 18, Color.white);
            counter = Text("Movimientos", border.transform, font, "", new Vector2(350, 28), new Vector2(-330, -289), 17, Cyan);
            infections = Text("Infecciones", border.transform, font, "", new Vector2(350, 28), new Vector2(330, -289), 17, Cyan);
            FinishButton = Button("Finalizar", border.transform, font, "VOLVER AL CMD", new Vector2(290, 40), new Vector2(0, -289), FinishOrClose);
            finishLabel = FinishButton.GetComponentInChildren<TMP_Text>();
        }
    }
}

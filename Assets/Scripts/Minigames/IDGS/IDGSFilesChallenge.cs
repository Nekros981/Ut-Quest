using System;
using System.Linq;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CampusQuest.IDGS.IDGSDesktopUI;

namespace CampusQuest.IDGS
{
    public sealed class IDGSFilesChallenge : MonoBehaviour
    {
        public const string EventId = "idgs.cmd.archivos";
        public static readonly string[] FileNames = { "foto.png", "logo.jpg", "horario.pdf", "reporte.txt", "musica.wav", "efecto.mp3" };
        public static readonly string[] FolderNames = { "Imágenes", "Documentos", "Audio" };
        public IDGSInteractionPoint station;
        public IDGSPlayerMotor player;
        public IDGSInteractionController interactor;
        public GameObject gameplayHud;
        public TMP_FontAsset font;
        public TMP_Text stationState;
        public Canvas canvas { get; private set; }
        public GameObject panel { get; private set; }
        public RectTransform[] Files { get; private set; }
        public RectTransform[] Folders { get; private set; }
        public TMP_Text Status { get; private set; }
        public Button FinishButton { get; private set; }
        public Button ExitButton { get; private set; }
        public bool IsOpen => modal != null && modal.IsOpen;
        public bool IsSolved { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public bool HasCompleted => GameProgress.HasEvent(EventId);
        public int SortedCount => sorted.Count(v => v);
        public int DraggingIndex { get; private set; } = -1;
        readonly bool[] sorted = new bool[6];
        readonly Vector2[] homes = new Vector2[6];
        readonly Image[] folderFaces = new Image[3];
        readonly TMP_Text[] folderCounts = new TMP_Text[3];
        IDGSActivityModal modal;
        RectTransform desktop;
        TMP_Text counter, completion, finishLabel;
        float feedbackUntil, solvedAt;
        int wrongFolder = -1;
        Vector2 dragOffset;

        void Awake()
        {
            BuildDesktop();
            modal = new IDGSActivityModal(player, interactor, gameplayHud, panel);
        }
        void OnEnable() { GameProgress.Changed += RefreshStation; GameProgress.NewGameStarted += Close; RefreshStation(); }
        void OnDisable() { GameProgress.Changed -= RefreshStation; GameProgress.NewGameStarted -= Close; Close(); }
        public void OpenFromStation() { TryOpen(); }
        public bool TryOpen()
        {
            if (!isActiveAndEnabled || modal == null || !modal.TryOpen(station)) return false;
            Array.Clear(sorted, 0, sorted.Length); DraggingIndex = -1; wrongFolder = -1;
            IsSolved = false; RewardGrantedThisRun = false; FinishButton.gameObject.SetActive(false);
            completion.text = HasCompleted ? "ACTIVIDAD COMPLETADA · REPETICIÓN" : "CMD / RECUPERACIÓN DE ARCHIVOS";
            Status.text = "Arrastra cada archivo a su carpeta según la extensión.";
            for (int i = 0; i < Files.Length; i++) { Files[i].gameObject.SetActive(true); Files[i].anchoredPosition = homes[i]; }
            RefreshBoard(); return true;
        }
        public bool BeginDrag(int index, Vector2 screen, Camera camera)
        {
            if (!IsOpen || IsSolved || SortedCount == 6 || index < 0 || index >= 6 || sorted[index] || DraggingIndex >= 0) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(desktop, screen, camera, out var p)) return false;
            DraggingIndex = index; dragOffset = Files[index].anchoredPosition - p;
            Files[index].SetAsLastSibling(); return true;
        }
        public void MoveDrag(Vector2 screen, Camera camera)
        {
            if (DraggingIndex < 0) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(desktop, screen, camera, out var p))
                Files[DraggingIndex].anchoredPosition = new Vector2(Mathf.Clamp(p.x + dragOffset.x, -435, 435), Mathf.Clamp(p.y + dragOffset.y, -170, 170));
        }
        public void EndDrag(Vector2 screen, Camera camera)
        {
            if (DraggingIndex < 0) return;
            int index = DraggingIndex; DraggingIndex = -1;
            int folder = Array.FindIndex(Folders, r => RectTransformUtility.RectangleContainsScreenPoint(r, screen, camera));
            Files[index].anchoredPosition = homes[index];
            if (folder == index / 2)
            {
                sorted[index] = true; Files[index].gameObject.SetActive(false);
                Status.text = FileNames[index] + " → " + FolderNames[folder] + ". Correcto.";
                wrongFolder = -1;
                if (SortedCount == 6) TryComplete();
            }
            else
            {
                wrongFolder = folder; feedbackUntil = CampusQuestGameClock.Now + 1.2f;
                Status.text = folder < 0 ? "Suelta el archivo dentro de una carpeta." : "Esa carpeta no corresponde. Inténtalo de nuevo.";
            }
            RefreshBoard();
        }
        void TryComplete()
        {
            if (SortedCount != 6) return;
            bool completed = HasCompleted;
            RewardGrantedThisRun = GameProgress.RegisterEvent(EventId);
            FinishButton.gameObject.SetActive(true);
            if (!completed && !RewardGrantedThisRun)
            {
                Status.text = "No se pudo guardar. Puedes reintentar.";
                finishLabel.text = "REINTENTAR GUARDADO"; return;
            }
            IsSolved = true; solvedAt = CampusQuestGameClock.Now; Status.text = "¡Archivos organizados!";
            completion.text = RewardGrantedThisRun ? "SISTEMA RESTAURADO" : "REPETICIÓN COMPLETADA";
            finishLabel.text = "VOLVER AL CMD";
        }
        public void FinishOrClose() { if (IsSolved) Close(); else { TryComplete(); RefreshBoard(); } }
        public void Close()
        {
            if (DraggingIndex >= 0) Files[DraggingIndex].anchoredPosition = homes[DraggingIndex];
            DraggingIndex = -1; modal?.Close();
        }
        void Update()
        {
            if (CampusQuestPauseMenu.BlocksInput) return;
            if (!IsOpen) return;
            if (wrongFolder >= 0 && CampusQuestGameClock.Now > feedbackUntil) { wrongFolder = -1; RefreshBoard(); }
            if (IsSolved && CampusQuestGameClock.Now - solvedAt < 1) RefreshBoard();
            if (Time.frameCount == modal.OpenedFrame) return;
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) Close();
            else if (IsSolved && CampusQuestGameClock.Now - solvedAt > 1 && DialogueSession.ConfirmPressedThisFrame()) Close();
        }
        void RefreshBoard()
        {
            counter.text = "ARCHIVOS CLASIFICADOS " + SortedCount + "/6";
            float pulse = IsSolved && CampusQuestGameClock.Now - solvedAt < .9f ? Mathf.PingPong((CampusQuestGameClock.Now - solvedAt) * 5, 1) : 0;
            for (int i = 0; i < 3; i++)
            {
                folderCounts[i].text = (sorted[i * 2] ? 1 : 0) + (sorted[i * 2 + 1] ? 1 : 0) + "/2";
                folderFaces[i].color = wrongFolder == i ? new Color32(232, 94, 96, 255) : Color.Lerp(Gold, Cyan, pulse);
            }
        }
        void RefreshStation()
        {
            bool complete = HasCompleted;
            station.displayName = "Organiza los archivos" + (complete ? " · Completado" : "");
            if (stationState != null) { stationState.text = complete ? "[X] COMPLETADO" : "[ ] PENDIENTE"; stationState.color = complete ? Cyan : Gold; }
        }
        void BuildDesktop()
        {
            canvas = new GameObject("Minijuego · Organiza los archivos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var shade = Box("Escritorio", canvas.transform, Vector2.zero, Vector2.zero, new Color32(3, 12, 18, 235), true);
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one; shade.rectTransform.sizeDelta = Vector2.zero;
            panel = canvas.gameObject;
            var border = Box("Ventana", shade.transform, new Vector2(1100, 644), Vector2.zero, Gold);
            Box("Panel oscuro", border.transform, new Vector2(1092, 636), Vector2.zero, Dark);
            Box("Barra de título", border.transform, new Vector2(1080, 58), new Vector2(0, 284), new Color32(25, 52, 64, 255));
            Text("Título", border.transform, font, "CMD  /  ORGANIZA LOS ARCHIVOS", new Vector2(860, 48), new Vector2(-60, 284), 25, Gold);
            ExitButton = Button("Salir", border.transform, font, "SALIR · ESC", new Vector2(150, 40), new Vector2(450, 284), Close);
            completion = Text("Estado", border.transform, font, "", new Vector2(1000, 30), new Vector2(0, 230), 15, Cyan);
            desktop = Box("Área de trabajo", border.transform, new Vector2(1024, 390), new Vector2(0, 12), new Color32(12, 36, 43, 255)).rectTransform;
            Text("Archivos", desktop, font, "ARCHIVOS SIN CLASIFICAR", new Vector2(680, 30), new Vector2(0, 162), 17, Cyan);
            Files = new RectTransform[6]; Folders = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 320;
                var target = Box(FolderNames[i], desktop, new Vector2(272, 122), new Vector2(x, -112), new Color32(27, 51, 61, 255));
                Folders[i] = target.rectTransform;
                Box("Pestaña", target.transform, new Vector2(48, 12), new Vector2(-24, 35), Gold);
                folderFaces[i] = Box("Carpeta", target.transform, new Vector2(112, 48), new Vector2(0, 7), Gold);
                Text("Símbolo", target.transform, font, new[] { "IMG", "DOC", "SND" }[i], new Vector2(110, 35), new Vector2(0, 8), 17, Dark);
                Text("Nombre", target.transform, font, FolderNames[i], new Vector2(210, 32), new Vector2(-12, -41), 20, Gold);
                folderCounts[i] = Text("Cantidad", target.transform, font, "0/2", new Vector2(48, 25), new Vector2(107, -41), 14, Cyan);
            }
            for (int i = 0; i < 6; i++)
            {
                homes[i] = new Vector2((i % 3 - 1) * 320, 96 - (i / 3) * 82);
                var file = Box(FileNames[i], desktop, new Vector2(262, 64), homes[i], new Color32(43, 63, 77, 255), true);
                Files[i] = file.rectTransform;
                Box("Hoja", file.transform, new Vector2(28, 34), new Vector2(-105, 0), Cyan);
                for (int line = 0; line < 3; line++) Box("Línea", file.transform, new Vector2(18, 3), new Vector2(-105, 8 - 8 * line), Dark);
                Text("Nombre", file.transform, font, FileNames[i], new Vector2(212, 56), new Vector2(21, 0), 20, new Color32(235, 240, 243, 255));
                var handle = file.gameObject.AddComponent<IDGSFileHandle>(); handle.challenge = this; handle.fileIndex = i;
            }
            Status = Text("Mensaje", border.transform, font, "", new Vector2(1020, 42), new Vector2(0, -209), 19, new Color32(235, 240, 243, 255));
            counter = Text("Contador", border.transform, font, "", new Vector2(700, 28), new Vector2(0, -248), 17, Cyan);
            FinishButton = Button("Finalizar", border.transform, font, "VOLVER AL CMD", new Vector2(340, 40), new Vector2(0, -289), FinishOrClose);
            finishLabel = FinishButton.GetComponentInChildren<TMP_Text>();
        }
    }
}

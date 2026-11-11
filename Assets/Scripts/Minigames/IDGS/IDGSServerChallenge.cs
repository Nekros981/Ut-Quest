using System;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CampusQuest.IDGS.IDGSDesktopUI;

namespace CampusQuest.IDGS
{
    public enum IDGSServerPhase { Closed, Preparing, Running, Retry, Solved, SaveRetry }

    public sealed class IDGSServerChallenge : MonoBehaviour
    {
        public const string EventId = "idgs.cmd.servidor";
        public const float ZoneStart = .56f, ZoneEnd = .84f;
        public IDGSInteractionPoint station;
        public IDGSPlayerMotor player;
        public IDGSInteractionController interactor;
        public GameObject gameplayHud;
        public TMP_FontAsset font;
        public TMP_Text stationState;
        public Canvas canvas { get; private set; }
        public GameObject panel { get; private set; }
        public Button CalibrateButton { get; private set; }
        public Button FinishButton { get; private set; }
        public Button ExitButton { get; private set; }
        public TMP_Text Status { get; private set; }
        public IDGSServerPhase Phase { get; private set; } = IDGSServerPhase.Closed;
        public int CalibratedCount { get; private set; }
        public int Attempts { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public bool IsOpen => modal != null && modal.IsOpen;
        public bool HasCompleted => GameProgress.HasEvent(EventId);
        public bool IsSolved => Phase == IDGSServerPhase.Solved;
        public bool CanCalibrate => IsOpen && Phase == IDGSServerPhase.Running;
        public float IndicatorPosition => CanCalibrate ? Mathf.Repeat((CampusQuestGameClock.Now - startedAt) * ModuleSpeed(CalibratedCount), 1) : CalibratedCount < 3 ? positions[CalibratedCount] : positions[2];
        public static float ModuleSpeed(int module) => .26f + .06f * Mathf.Clamp(module, 0, 2);
        public static bool IsInZone(float position) => position >= ZoneStart && position <= ZoneEnd;
        readonly float[] positions = new float[3];
        readonly Image[] frames = new Image[3];
        readonly RectTransform[] markers = new RectTransform[3];
        readonly TMP_Text[] moduleStates = new TMP_Text[3];
        IDGSActivityModal modal;
        TMP_Text counter, completion, finishLabel;
        float startedAt, deadline, solvedAt;
        int lastAttemptFrame = -1;

        void Awake() { BuildInterface(); modal = new IDGSActivityModal(player, interactor, gameplayHud, panel); }
        void OnEnable() { GameProgress.Changed += RefreshStation; GameProgress.NewGameStarted += Close; RefreshStation(); }
        void OnDisable() { GameProgress.Changed -= RefreshStation; GameProgress.NewGameStarted -= Close; Close(); }
        public void OpenFromStation() { TryOpen(); }
        public bool TryOpen()
        {
            if (!isActiveAndEnabled || modal == null || !modal.TryOpen(station)) return false;
            Array.Clear(positions, 0, positions.Length); CalibratedCount = 0; Attempts = 0; RewardGrantedThisRun = false; lastAttemptFrame = -1;
            FinishButton.gameObject.SetActive(false); CalibrateButton.gameObject.SetActive(true);
            completion.text = HasCompleted ? "ACTIVIDAD COMPLETADA · REPETICIÓN" : "CMD / DIAGNÓSTICO DEL SERVIDOR";
            Status.text = "Espera la zona verde y pulsa Espacio o Calibrar.";
            PrepareModule(); return true;
        }
        void PrepareModule()
        {
            Phase = IDGSServerPhase.Preparing; positions[CalibratedCount] = 0;
            deadline = CampusQuestGameClock.Now + .55f; RefreshView();
        }
        public void CalibrateFromButton() { TryCalibrate(); }
        public bool TryCalibrate()
        {
            if (!CanCalibrate || lastAttemptFrame == Time.frameCount) return false;
            lastAttemptFrame = Time.frameCount;
            float position = IndicatorPosition; positions[CalibratedCount] = position; Attempts++;
            if (!IsInZone(position))
            {
                Phase = IDGSServerPhase.Retry; deadline = CampusQuestGameClock.Now + .7f;
                Status.text = "Fuera de rango. Este módulo vuelve a empezar.";
            }
            else
            {
                CalibratedCount++;
                if (CalibratedCount == 3) TryComplete();
                else { Status.text = "¡Módulo calibrado! Vamos con el siguiente."; PrepareModule(); }
            }
            RefreshView(); return true;
        }
        void TryComplete()
        {
            if (CalibratedCount != 3) return;
            bool completed = HasCompleted;
            RewardGrantedThisRun = GameProgress.RegisterEvent(EventId);
            FinishButton.gameObject.SetActive(true); CalibrateButton.gameObject.SetActive(false);
            if (!completed && !RewardGrantedThisRun)
            {
                Phase = IDGSServerPhase.SaveRetry; Status.text = "No se pudo guardar. Puedes reintentar.";
                finishLabel.text = "REINTENTAR GUARDADO"; return;
            }
            Phase = IDGSServerPhase.Solved; solvedAt = CampusQuestGameClock.Now;
            Status.text = "¡Servidor calibrado!"; finishLabel.text = "VOLVER AL CMD";
            completion.text = RewardGrantedThisRun ? "SISTEMA RESTAURADO" : "REPETICIÓN COMPLETADA";
        }
        public void FinishOrClose() { if (IsSolved) Close(); else if (Phase == IDGSServerPhase.SaveRetry) { TryComplete(); RefreshView(); } }
        public void Close() { modal?.Close(); Phase = IDGSServerPhase.Closed; }
        void Update()
        {
            if (CampusQuestPauseMenu.BlocksInput) return;
            if (!IsOpen) return;
            if (Phase == IDGSServerPhase.Preparing && CampusQuestGameClock.Now >= deadline)
            {
                startedAt = CampusQuestGameClock.Now; Phase = IDGSServerPhase.Running;
                Status.text = "Módulo " + (CalibratedCount + 1) + ": pulsa Espacio o Calibrar en la zona verde.";
            }
            else if (Phase == IDGSServerPhase.Retry && CampusQuestGameClock.Now >= deadline) PrepareModule();
            RefreshView();
            if (Time.frameCount == modal.OpenedFrame) return;
            if (CampusQuestActiveControls.CancelPressedThisFrame()) { Close(); return; }
            if (IsSolved && CampusQuestGameClock.Now - solvedAt > 1 && DialogueSession.ConfirmPressedThisFrame()) { Close(); return; }
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) TryCalibrate();
        }
        void RefreshView()
        {
            if (CanCalibrate) positions[CalibratedCount] = IndicatorPosition;
            counter.text = "MÓDULOS CALIBRADOS " + CalibratedCount + "/3";
            CalibrateButton.interactable = CanCalibrate;
            float pulse = IsSolved && CampusQuestGameClock.Now - solvedAt < .9f ? Mathf.PingPong((CampusQuestGameClock.Now - solvedAt) * 5, 1) : 0;
            for (int i = 0; i < 3; i++)
            {
                bool complete = i < CalibratedCount, active = i == CalibratedCount;
                frames[i].color = complete ? Color.Lerp(Cyan, Color.white, pulse * .5f) : active ? (Phase == IDGSServerPhase.Retry ? new Color32(239, 94, 96, 255) : Gold) : new Color32(61, 79, 91, 255);
                markers[i].anchoredPosition = new Vector2((positions[i] - .5f) * 700, 0);
                moduleStates[i].text = complete ? "[X] CALIBRADO" : active ? Phase == IDGSServerPhase.Retry ? "REINTENTANDO" : Phase == IDGSServerPhase.Preparing ? "PREPÁRATE" : "EN CALIBRACIÓN" : "EN ESPERA";
                moduleStates[i].color = complete ? Cyan : Gold;
            }
        }
        void RefreshStation()
        {
            bool complete = HasCompleted;
            station.displayName = "Calibrar servidor" + (complete ? " · Completado" : "");
            if (stationState != null) { stationState.text = complete ? "[X] COMPLETADO" : "[ ] PENDIENTE"; stationState.color = complete ? Cyan : Gold; }
        }
        void BuildInterface()
        {
            canvas = new GameObject("Minijuego · Calibrar servidor", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
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
            Text("Título", border.transform, font, "CMD  /  CALIBRAR SERVIDOR", new Vector2(850, 48), new Vector2(-60, 284), 26, Gold);
            ExitButton = Button("Salir", border.transform, font, "SALIR · ESC", new Vector2(150, 40), new Vector2(450, 284), Close);
            completion = Text("Estado", border.transform, font, "", new Vector2(1000, 30), new Vector2(0, 233), 15, Cyan);
            Text("Ayuda", border.transform, font, "Detén el indicador blanco dentro de la zona verde.", new Vector2(1000, 35), new Vector2(0, 190), 20, Color.white);
            for (int i = 0; i < 3; i++)
            {
                var row = Box("Módulo " + (i + 1), border.transform, new Vector2(1000, 98), new Vector2(0, 110 - i * 110), new Color32(61, 79, 91, 255));
                frames[i] = row;
                Box("Fondo de módulo", row.transform, new Vector2(996, 94), Vector2.zero, new Color32(15, 36, 45, 255));
                Text("Nombre", row.transform, font, "MÓDULO 0" + (i + 1), new Vector2(200, 30), new Vector2(-368, 26), 17, Cyan);
                moduleStates[i] = Text("Estado", row.transform, font, "EN ESPERA", new Vector2(320, 30), new Vector2(300, 26), 15, Gold);
                var track = Box("Indicador", row.transform, new Vector2(706, 30), new Vector2(0, -17), new Color32(67, 88, 103, 255));
                Box("Pista", track.transform, new Vector2(700, 24), Vector2.zero, new Color32(6, 20, 29, 255));
                Box("Zona verde", track.transform, new Vector2((ZoneEnd - ZoneStart) * 700, 24), new Vector2(((ZoneStart + ZoneEnd) / 2 - .5f) * 700, 0), new Color32(49, 157, 99, 255));
                for (int j = 0; j < 9; j++) Box("Marca", track.transform, new Vector2(2, 5), new Vector2((j / 8f - .5f) * 694, -9), new Color32(134, 167, 174, 255));
                markers[i] = Box("Cursor", track.transform, new Vector2(6, 38), new Vector2(-350, 0), Color.white).rectTransform;
            }
            Status = Text("Mensaje", border.transform, font, "", new Vector2(1030, 38), new Vector2(0, -189), 19, Color.white);
            counter = Text("Contador", border.transform, font, "", new Vector2(800, 28), new Vector2(0, -232), 17, Cyan);
            CalibrateButton = Button("Calibrar", border.transform, font, "CALIBRAR · ESPACIO", new Vector2(350, 44), new Vector2(0, -283), CalibrateFromButton);
            FinishButton = Button("Finalizar", border.transform, font, "VOLVER AL CMD", new Vector2(350, 44), new Vector2(0, -283), FinishOrClose);
            finishLabel = FinishButton.GetComponentInChildren<TMP_Text>();
        }
    }
}

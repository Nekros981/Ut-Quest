using System;
using System.Linq;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.IDGS
{
    public sealed class IDGSCablesChallenge : MonoBehaviour
    {
        public const string EventId = "idgs.docencia.cables";
        public static readonly string[] Labels = { "1  ROJO", "2  AZUL", "3  AMARILLO", "4  VERDE" };
        public static readonly Color32[] CableColors = {
            new Color32(239,80,91,255), new Color32(65,163,247,255),
            new Color32(248,209,79,255), new Color32(85,211,139,255)
        };

        public IDGSInteractionPoint station;
        public IDGSInteractionController interactor;
        public IDGSPlayerMotor player;
        public GameObject gameplayHud;
        public GameObject panel;
        public Canvas canvas;
        public IDGSCablesGraphic wires;
        public RectTransform[] rightTargets;
        public TMP_Text[] rightLabels;
        public Image[] leftSockets;
        public Image[] rightSockets;
        public TMP_Text counter;
        public TMP_Text status;
        public TMP_Text completionState;
        public TMP_Text stationState;
        public SpriteRenderer[] stationLights;
        public Button finishButton;
        public TMP_Text finishLabel;

        public bool IsOpen => modal != null && modal.IsOpen;
        public bool IsSolved { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public int DraggingIndex { get; private set; } = -1;
        public Vector2 DragPosition { get; private set; }
        public int ConnectedCount => connected.Count(value => value);
        public bool HasCompleted => GameProgress.HasEvent(EventId);
        public float Electricity { get; private set; }
        private readonly bool[] connected = new bool[4];
        private readonly int[] rightOrder = { 0, 1, 2, 3 };
        private IDGSActivityModal modal;
        private float restoredAt;

        private void Awake() { modal = new IDGSActivityModal(player, interactor, gameplayHud, panel); }
        private void OnEnable()
        {
            GameProgress.Changed += RefreshStation;
            RefreshStation();
        }
        private void OnDisable()
        {
            GameProgress.Changed -= RefreshStation;
            Close();
        }

        public bool IsConnected(int index) => index >= 0 && index < 4 && connected[index];
        public int ColorAtRight(int slot) => rightOrder[slot];
        public int RightSlotFor(int color) => Array.IndexOf(rightOrder, color);

        public void OpenFromStation() { TryOpen(); }

        public bool TryOpen()
        {
            if (!isActiveAndEnabled || modal == null || !modal.TryOpen(station)) return false;
            Array.Clear(connected, 0, connected.Length);
            ShuffleTargets();
            IsSolved = false;
            RewardGrantedThisRun = false;
            DraggingIndex = -1;
            Electricity = 0;
            finishButton.gameObject.SetActive(false);
            status.text = "Arrastra cada cable al conector con el mismo número y color.";
            completionState.text = HasCompleted ? "ACTIVIDAD COMPLETADA · REPETICIÓN" : "REPARACIÓN DE ENERGÍA";
            RefreshBoard();
            return true;
        }

        private void ShuffleTargets()
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                for (int i = 0; i < 4; i++) rightOrder[i] = i;
                for (int i = 3; i > 0; i--)
                {
                    int other = UnityEngine.Random.Range(0, i + 1);
                    int value = rightOrder[i]; rightOrder[i] = rightOrder[other]; rightOrder[other] = value;
                }
                if (Enumerable.Range(0, 4).All(i => rightOrder[i] != i)) return;
            }
            for (int i = 0; i < 4; i++) rightOrder[i] = (i + 1) % 4;
        }

        public bool BeginDrag(int index, Vector2 screenPoint, Camera eventCamera)
        {
            if (!IsOpen || IsSolved || index < 0 || index >= 4 || connected[index] || DraggingIndex >= 0) return false;
            DraggingIndex = index;
            MoveDrag(screenPoint, eventCamera);
            return true;
        }

        public void MoveDrag(Vector2 screenPoint, Camera eventCamera)
        {
            if (DraggingIndex < 0) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(wires.rectTransform, screenPoint, eventCamera, out var local))
            {
                Rect bounds = wires.rectTransform.rect;
                DragPosition = new Vector2(Mathf.Clamp(local.x, bounds.xMin, bounds.xMax), Mathf.Clamp(local.y, bounds.yMin, bounds.yMax));
                wires.SetVerticesDirty();
            }
        }

        public void EndDrag(Vector2 screenPoint, Camera eventCamera)
        {
            if (DraggingIndex < 0) return;
            int source = DraggingIndex;
            DraggingIndex = -1;
            int slot = Array.FindIndex(rightTargets, target => RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, eventCamera));
            if (slot >= 0 && rightOrder[slot] == source)
            {
                connected[source] = true;
                status.text = "Conexión correcta.";
                if (ConnectedCount == 4) TryComplete();
            }
            else status.text = slot < 0 ? "Lleva el cable hasta un conector." : "Ese conector no coincide. Prueba otra vez.";
            RefreshBoard();
        }

        private void TryComplete()
        {
            bool alreadyComplete = HasCompleted;
            RewardGrantedThisRun = GameProgress.RegisterEvent(EventId);
            finishButton.gameObject.SetActive(true);
            if (!RewardGrantedThisRun && !alreadyComplete)
            {
                status.text = "No se pudo guardar la conexión. Puedes reintentar.";
                finishLabel.text = "REINTENTAR GUARDADO";
                return;
            }
            IsSolved = true;
            restoredAt = CampusQuestGameClock.Now;
            status.text = "¡Conexión restablecida!";
            finishLabel.text = "VOLVER A DOCENCIA";
            completionState.text = RewardGrantedThisRun ? "ACTIVIDAD COMPLETADA" : "REPETICIÓN COMPLETADA";
        }

        public void FinishOrClose()
        {
            if (IsSolved) Close();
            else if (ConnectedCount == 4) TryComplete();
        }

        private void Update()
        {
            if (CampusQuestPauseMenu.BlocksInput) return;
            if (!IsOpen) return;
            if (IsSolved)
            {
                float elapsed = CampusQuestGameClock.Now - restoredAt;
                Electricity = elapsed < .8f ? Mathf.PingPong(elapsed * 7, 1) : 0;
                wires.SetVerticesDirty();
            }
            if (Time.frameCount == modal.OpenedFrame) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
            else if (IsSolved && CampusQuestGameClock.Now - restoredAt > .8f && DialogueSession.ConfirmPressedThisFrame()) Close();
        }

        private void RefreshBoard()
        {
            counter.text = "CONEXIONES " + ConnectedCount + "/4";
            for (int row = 0; row < 4; row++)
            {
                int color = rightOrder[row];
                rightLabels[row].text = Labels[color];
                rightLabels[row].color = CableColors[color];
                rightSockets[row].color = CableColors[color];
                leftSockets[row].color = CableColors[row];
            }
            wires.SetVerticesDirty();
        }

        private void RefreshStation()
        {
            bool complete = HasCompleted;
            station.displayName = "Reconecta los cables" + (complete ? " · Completado" : "");
            stationState.text = complete ? "[X] COMPLETADO" : "[ ] PENDIENTE";
            stationState.color = complete ? CableColors[3] : new Color32(225, 195, 118, 255);
            for (int i = 0; i < stationLights.Length; i++) stationLights[i].color = complete ? CableColors[3] : CableColors[i % 4];
        }

        public void Close()
        {
            if (!IsOpen) return;
            DraggingIndex = -1;
            modal.Close();
        }
    }
}

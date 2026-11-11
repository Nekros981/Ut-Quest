using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.IDGS
{
    public sealed class IDGSCircuitChallenge : MonoBehaviour
    {
        public const string EventId = "idgs.docencia.circuito";
        public IDGSInteractionPoint station;
        public IDGSInteractionController interactor;
        public IDGSPlayerMotor player;
        public GameObject gameplayHud;
        public GameObject panel;
        public Canvas canvas;
        public IDGSCircuitTile[] tiles;
        public TMP_Text status;
        public TMP_Text counter;
        public TMP_Text completionState;
        public TMP_Text stationState;
        public SpriteRenderer[] stationLights;
        public Image outputWire;
        public Button finishButton;
        public TMP_Text finishLabel;
        public IDGSCircuitBoard Board { get; private set; }
        public bool IsOpen => modal != null && modal.IsOpen;
        public bool IsSolved { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public bool HasCompleted => GameProgress.HasEvent(EventId);
        public int SelectedIndex { get; private set; }
        public int Moves { get; private set; }
        public float Electricity { get; private set; }
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

        public void OpenFromStation() { TryOpen(); }
        public bool TryOpen()
        {
            if (!isActiveAndEnabled || modal == null || !modal.TryOpen(station)) return false;
            Board = new IDGSCircuitBoard(UnityEngine.Random.Range(0, int.MaxValue));
            SelectedIndex = 4;
            Moves = 0;
            Electricity = 0;
            IsSolved = false;
            RewardGrantedThisRun = false;
            finishButton.gameObject.SetActive(false);
            status.text = "Gira las piezas para unir ENTRADA y SALIDA.";
            completionState.text = HasCompleted ? "ACTIVIDAD COMPLETADA · REPETICIÓN" : "REPARACIÓN DEL CIRCUITO";
            RefreshBoard();
            return true;
        }

        public bool RotateAt(int index)
        {
            if (!IsOpen || IsSolved || index < 0 || index >= 9) return false;
            SelectedIndex = index;
            Board.Rotate(index);
            Moves++;
            status.text = Board.PoweredCount == 0 ? "Orienta la primera pieza hacia la entrada." : "La energía avanza. Conecta la salida.";
            finishButton.gameObject.SetActive(false);
            if (Board.IsSolved) TryComplete();
            RefreshBoard();
            return true;
        }

        private void TryComplete()
        {
            if (!Board.IsSolved) return;
            bool alreadyComplete = HasCompleted;
            RewardGrantedThisRun = GameProgress.RegisterEvent(EventId);
            finishButton.gameObject.SetActive(true);
            if (!RewardGrantedThisRun && !alreadyComplete)
            {
                status.text = "No se pudo guardar el circuito. Puedes reintentar.";
                finishLabel.text = "REINTENTAR GUARDADO";
                return;
            }
            IsSolved = true;
            restoredAt = Time.unscaledTime;
            status.text = "¡Circuito reparado!";
            completionState.text = RewardGrantedThisRun ? "ACTIVIDAD COMPLETADA" : "REPETICIÓN COMPLETADA";
            finishLabel.text = "VOLVER A DOCENCIA";
        }

        public void FinishOrClose()
        {
            if (IsSolved) Close();
            else TryComplete();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (IsSolved)
            {
                float elapsed = Time.unscaledTime - restoredAt;
                Electricity = elapsed < .8f ? Mathf.PingPong(elapsed * 7, 1) : 0;
                RefreshBoard();
            }
            if (Time.frameCount == modal.OpenedFrame) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (IsSolved)
            {
                if (Time.unscaledTime - restoredAt > .8f && DialogueSession.ConfirmPressedThisFrame()) Close();
                return;
            }
            int x = SelectedIndex % 3, y = SelectedIndex / 3;
            if (keyboard.leftArrowKey.wasPressedThisFrame) x = Mathf.Max(0, x - 1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) x = Mathf.Min(2, x + 1);
            else if (keyboard.upArrowKey.wasPressedThisFrame) y = Mathf.Max(0, y - 1);
            else if (keyboard.downArrowKey.wasPressedThisFrame) y = Mathf.Min(2, y + 1);
            SelectedIndex = y * 3 + x;
            if (DialogueSession.ConfirmPressedThisFrame()) RotateAt(SelectedIndex);
            else RefreshBoard();
        }

        private void RefreshBoard()
        {
            counter.text = "GIROS " + Moves;
            outputWire.color = IsSolved ? new Color32(248, 209, 79, 255) : new Color32(76, 94, 106, 255);
            foreach (var tile in tiles) tile.SetVerticesDirty();
        }
        private void RefreshStation()
        {
            bool complete = HasCompleted;
            station.displayName = "Repara el circuito" + (complete ? " · Completado" : "");
            stationState.text = complete ? "[X] COMPLETADO" : "[ ] PENDIENTE";
            stationState.color = complete ? new Color32(85, 211, 139, 255) : new Color32(225, 195, 118, 255);
            for (int i = 0; i < stationLights.Length; i++)
                stationLights[i].color = complete ? new Color32(85, 211, 139, 255) : IDGSCablesChallenge.CableColors[i % 4];
        }
        public void Close() { modal?.Close(); }
    }
}

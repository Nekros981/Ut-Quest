using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusQuest.IDGS
{
    public enum IDGSMemoryPhase { Closed, Preparing, Showing, Recall, RoundPassed, Retry, Solved, SaveRetry }

    public sealed class IDGSMemoryChallenge : MonoBehaviour
    {
        public const string EventId = "idgs.docencia.memoria";
        public static readonly string[] ColorNames = { "ROJO", "AZUL", "VERDE", "AMARILLO" };
        public static readonly Color32[] Colors = {
            new Color32(239,80,91,255), new Color32(65,163,247,255),
            new Color32(85,211,139,255), new Color32(248,209,79,255)
        };
        public IDGSInteractionPoint station;
        public IDGSInteractionController interactor;
        public IDGSPlayerMotor player;
        public GameObject gameplayHud;
        public GameObject panel;
        public Canvas canvas;
        public Button[] pads;
        public Image[] padFaces;
        public Image[] padFrames;
        public TMP_Text[] padLabels;
        public Image[] roundIndicators;
        public TMP_Text status;
        public TMP_Text phaseLabel;
        public TMP_Text counter;
        public TMP_Text completionState;
        public TMP_Text stationState;
        public SpriteRenderer[] stationLights;
        public Button finishButton;
        public TMP_Text finishLabel;
        public bool IsOpen => modal != null && modal.IsOpen;
        public bool HasCompleted => GameProgress.HasEvent(EventId);
        public bool IsSolved => Phase == IDGSMemoryPhase.Solved;
        public bool CanAcceptInput => IsOpen && Phase == IDGSMemoryPhase.Recall && inputArmed;
        public bool RewardGrantedThisRun { get; private set; }
        public IDGSMemoryPhase Phase { get; private set; } = IDGSMemoryPhase.Closed;
        public int Round { get; private set; }
        public int InputIndex { get; private set; }
        public int LitIndex { get; private set; } = -1;
        public int PlaybackStep { get; private set; }
        public int CompletedRounds => IsSolved ? 5 : Phase == IDGSMemoryPhase.RoundPassed ? Round : Mathf.Max(0, Round - 1);
        private readonly int[] sequence = new int[5];
        private IDGSActivityModal modal;
        private float deadline;
        private float litUntil;
        private float solvedAt;
        private bool inputArmed;
        private bool repeating;
        private int lastInputFrame = -1;

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
            for (int i = 0; i < sequence.Length; i++) sequence[i] = Random.Range(0, 4);
            Round = 1;
            repeating = HasCompleted;
            RewardGrantedThisRun = false;
            lastInputFrame = -1;
            finishButton.gameObject.SetActive(false);
            PrepareRound(.65f);
            return true;
        }

        private void PrepareRound(float delay)
        {
            Phase = IDGSMemoryPhase.Preparing;
            InputIndex = 0;
            PlaybackStep = 0;
            LitIndex = -1;
            inputArmed = false;
            deadline = Time.unscaledTime + delay;
            status.text = "Observa la secuencia y después repítela.";
            RefreshView();
        }
        private void StartPlayback()
        {
            Phase = IDGSMemoryPhase.Showing;
            PlaybackStep = 0;
            LitIndex = sequence[0];
            deadline = Time.unscaledTime + .5f;
            RefreshView();
        }
        private void AdvancePlayback()
        {
            if (LitIndex >= 0)
            {
                LitIndex = -1;
                deadline = Time.unscaledTime + .22f;
            }
            else
            {
                PlaybackStep++;
                if (PlaybackStep == Round)
                {
                    Phase = IDGSMemoryPhase.Recall;
                    inputArmed = false;
                    status.text = "Tu turno: repite el patrón en el mismo orden.";
                }
                else
                {
                    LitIndex = sequence[PlaybackStep];
                    deadline = Time.unscaledTime + .5f;
                }
            }
            RefreshView();
        }

        public void ChooseFromButton(int index) { Choose(index); }
        public bool Choose(int index)
        {
            if (!CanAcceptInput || index < 0 || index >= 4 || lastInputFrame == Time.frameCount) return false;
            lastInputFrame = Time.frameCount;
            LitIndex = index;
            litUntil = Time.unscaledTime + .18f;
            if (index != sequence[InputIndex])
            {
                InputIndex = 0;
                Phase = IDGSMemoryPhase.Retry;
                deadline = Time.unscaledTime + .9f;
                status.text = "Inténtalo de nuevo. Repetimos esta ronda.";
            }
            else
            {
                InputIndex++;
                if (InputIndex == Round)
                {
                    if (Round == 5) TryComplete();
                    else
                    {
                        Phase = IDGSMemoryPhase.RoundPassed;
                        deadline = Time.unscaledTime + .65f;
                        status.text = "¡Bien! Ronda completada.";
                    }
                }
            }
            RefreshView();
            return true;
        }

        private void TryComplete()
        {
            bool alreadyComplete = HasCompleted;
            RewardGrantedThisRun = GameProgress.RegisterEvent(EventId);
            finishButton.gameObject.SetActive(true);
            LitIndex = -1;
            if (!RewardGrantedThisRun && !alreadyComplete)
            {
                Phase = IDGSMemoryPhase.SaveRetry;
                status.text = "No se pudo guardar la secuencia. Puedes reintentar.";
                finishLabel.text = "REINTENTAR GUARDADO";
                return;
            }
            Phase = IDGSMemoryPhase.Solved;
            solvedAt = Time.unscaledTime;
            status.text = "¡Secuencia restaurada!";
            finishLabel.text = "VOLVER A DOCENCIA";
        }
        public void FinishOrClose()
        {
            if (IsSolved) Close();
            else if (Phase == IDGSMemoryPhase.SaveRetry) { TryComplete(); RefreshView(); }
        }

        private void Update()
        {
            if (!IsOpen) return;
            float now = Time.unscaledTime;
            if (Phase == IDGSMemoryPhase.Preparing && now >= deadline) StartPlayback();
            else if (Phase == IDGSMemoryPhase.Showing && now >= deadline) AdvancePlayback();
            else if (Phase == IDGSMemoryPhase.RoundPassed && now >= deadline) { Round++; PrepareRound(.45f); }
            else if (Phase == IDGSMemoryPhase.Retry && now >= deadline) PrepareRound(.45f);
            if (Phase != IDGSMemoryPhase.Showing && LitIndex >= 0 && now >= litUntil)
            {
                LitIndex = -1;
                RefreshView();
            }
            var keyboard = Keyboard.current;
            if (Phase == IDGSMemoryPhase.Recall && !inputArmed && Released(keyboard))
            {
                inputArmed = true;
                RefreshView();
            }
            if (IsSolved && now - solvedAt < .9f) RefreshView();
            if (Time.frameCount == modal.OpenedFrame || keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (IsSolved && now - solvedAt > .9f && DialogueSession.ConfirmPressedThisFrame()) { Close(); return; }
            if (!CanAcceptInput) return;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) Choose(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) Choose(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) Choose(2);
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) Choose(3);
        }

        private static bool Released(Keyboard keyboard)
        {
            bool keys = keyboard != null && (keyboard.digit1Key.isPressed || keyboard.digit2Key.isPressed
                || keyboard.digit3Key.isPressed || keyboard.digit4Key.isPressed || keyboard.numpad1Key.isPressed
                || keyboard.numpad2Key.isPressed || keyboard.numpad3Key.isPressed || keyboard.numpad4Key.isPressed);
            return !keys && (Mouse.current == null || !Mouse.current.leftButton.isPressed);
        }

        private void RefreshView()
        {
            completionState.text = "RONDA " + Round + " DE 5" + (repeating ? " · REPETICIÓN" : "");
            counter.text = IsSolved ? "5 RONDAS COMPLETADAS" : "PASOS " + InputIndex + "/" + Round;
            if (Phase == IDGSMemoryPhase.Preparing) phaseLabel.text = "PREPÁRATE";
            else if (Phase == IDGSMemoryPhase.Showing) phaseLabel.text = "OBSERVA";
            else if (Phase == IDGSMemoryPhase.Recall) phaseLabel.text = "TU TURNO";
            else if (Phase == IDGSMemoryPhase.Retry) phaseLabel.text = "OTRA VEZ";
            else if (Phase == IDGSMemoryPhase.RoundPassed) phaseLabel.text = "¡CORRECTO!";
            else phaseLabel.text = IsSolved ? "¡COMPLETADO!" : "GUARDANDO";
            float glow = IsSolved && Time.unscaledTime - solvedAt < .8f
                ? Mathf.PingPong((Time.unscaledTime - solvedAt) * 6, 1) : 0;
            for (int i = 0; i < 4; i++)
            {
                bool lit = LitIndex == i;
                pads[i].interactable = CanAcceptInput;
                Color color = Colors[i];
                Color dim = color * .28f;
                dim.a = 1;
                padFaces[i].color = lit ? Color.Lerp(color, Color.white, .4f) : Color.Lerp(dim, color, glow * .65f);
                padFrames[i].color = Phase == IDGSMemoryPhase.Retry ? new Color32(239,80,91,255)
                    : lit || IsSolved ? new Color32(245,220,150,255) : new Color32(70,90,102,255);
                padLabels[i].color = lit ? new Color32(12,27,36,255) : new Color32(230,237,240,255);
            }
            for (int i = 0; i < 5; i++)
                roundIndicators[i].color = i < CompletedRounds ? new Color32(225,195,118,255) : new Color32(58,74,83,255);
        }
        private void RefreshStation()
        {
            bool complete = HasCompleted;
            station.displayName = "Secuencia de memoria" + (complete ? " · Completado" : "");
            stationState.text = complete ? "[X] COMPLETADO" : "[ ] PENDIENTE";
            stationState.color = complete ? Colors[2] : new Color32(225,195,118,255);
            for (int i = 0; i < stationLights.Length; i++) stationLights[i].color = complete ? Colors[2] : Colors[i % 4];
        }
        public void Close()
        {
            modal?.Close();
            Phase = IDGSMemoryPhase.Closed;
            LitIndex = -1;
        }
#if UNITY_EDITOR
        public int SequenceStepForValidation(int index) => sequence[index];
#endif
    }
}

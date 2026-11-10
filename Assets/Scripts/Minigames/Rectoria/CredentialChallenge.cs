using System.Collections.Generic;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusQuest.Rectoria
{
    public enum CredentialStage { Waiting, Countdown, Flash, Puzzle, Success }

    [DefaultExecutionOrder(-85)]
    public sealed class CredentialChallenge : MonoBehaviour
    {
        public const string IntroId = "rectoria.credential.mony_intro";
        public const string PhotoId = "rectoria.credential.photo_taken";
        public DialogueSession session;
        public EnrollmentMission mission;
        public RectoriaPlayerTest player;
        public ArchiveChallenge archive;
        public RectoriaScenePoint photoMark;
        public DialogueDefinition introDialogue;
        public DialogueDefinition prerequisiteDialogue;
        public DialogueDefinition successDialogue;
        public GameObject capturePanel;
        public TMP_Text countdownLabel;
        public Image flash;
        public GameObject puzzlePanel;
        public TMP_Text statusLabel;
        public TMP_Text movesLabel;
        public TMP_Text rewardLabel;
        public RectTransform restartControl;
        public CredentialTileGraphic[] tiles;
        public Image[] frames;
        public TMP_Text[] numbers;
        public SlidingCredentialBoard Board { get; private set; }
        public CredentialStage Stage { get; private set; }
        public bool IsBusy => Stage != CredentialStage.Waiting;
        public bool RewardGrantedThisRun { get; private set; }
        public int SelectedIndex { get; private set; }
        public float StillSeconds { get; private set; }
        public int CountdownNumber { get; private set; }
        public bool ConsumedInputThisFrame => lastInputFrame == Time.frameCount;
        private readonly Dictionary<Behaviour, bool> behaviours = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<GameObject, bool> objects = new Dictionary<GameObject, bool>();
        private Vector3 lastPosition;
        private float stageSeconds;
        private int lastInputFrame = -1;
        private bool mustLeaveMark;
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private bool Eligible => mission != null && mission.IsActive && session.HasCompleted(IntroId);

        private void Awake()
        {
            Stage = CredentialStage.Waiting;
            capturePanel.SetActive(false); puzzlePanel.SetActive(false); flash.gameObject.SetActive(false);
            lastPosition = player.transform.position;
        }
        private void OnEnable() { if (session != null) session.ProgressChanged += OnProgressChanged; }
        private void OnDisable() { if (session != null) session.ProgressChanged -= OnProgressChanged; Close(false); }
        private void OnProgressChanged()
        {
            if (!Eligible) { if (IsBusy) Close(false); mustLeaveMark = false; }
        }

        public DialogueDefinition DialogueForMony() => mission != null && mission.IsActive ? introDialogue : prerequisiteDialogue;

        private void LateUpdate()
        {
            if (Stage == CredentialStage.Waiting)
            {
                bool centered = photoMark != null && player.map != null
                    && (player.transform.position - player.map.CellCenter(photoMark.cell)).sqrMagnitude < .0001f;
                bool stationary = (player.transform.position - lastPosition).sqrMagnitude < .000001f;
                if (!centered) mustLeaveMark = false;
                if (Eligible && centered && stationary && !mustLeaveMark && player.isActiveAndEnabled
                    && !DialogueSession.IsAnyOpen && (archive == null || !archive.IsOpen))
                {
                    StillSeconds += Time.unscaledDeltaTime;
                    capturePanel.SetActive(true);
                    countdownLabel.text = "FOTO · Quédate quieto\n" + Mathf.Min(1.5f, StillSeconds).ToString("0.0") + " / 1.5 s";
                    if (StillSeconds >= 1.5f) StartCapture();
                }
                else { StillSeconds = 0; capturePanel.SetActive(false); }
            }
            else if (Stage == CredentialStage.Countdown)
            {
                stageSeconds += Time.unscaledDeltaTime;
                CountdownNumber = Mathf.Clamp(3 - Mathf.FloorToInt(stageSeconds), 1, 3);
                countdownLabel.text = "Mira a la cámara\n" + CountdownNumber;
                if (stageSeconds >= 3)
                {
                    Stage = CredentialStage.Flash; stageSeconds = 0;
                    flash.gameObject.SetActive(true); flash.color = Color.white;
                    session.RegisterEvent(PhotoId);
                }
            }
            else if (Stage == CredentialStage.Flash)
            {
                stageSeconds += Time.unscaledDeltaTime;
                flash.color = new Color(1, 1, 1, 1 - Mathf.Clamp01(stageSeconds / .22f));
                if (stageSeconds >= .22f)
                {
                    flash.gameObject.SetActive(false); capturePanel.SetActive(false);
                    Board = new SlidingCredentialBoard(UnityEngine.Random.Range(0, int.MaxValue));
                    Stage = CredentialStage.Puzzle; SelectedIndex = 0; puzzlePanel.SetActive(true); Refresh();
                }
            }
            else if (Stage == CredentialStage.Success && RewardGrantedThisRun)
            {
                stageSeconds += Time.unscaledDeltaTime;
                rewardLabel.rectTransform.localScale = Vector3.one * (1 + .04f * Mathf.Sin(Mathf.Clamp01(stageSeconds / .6f) * Mathf.PI));
            }
            lastPosition = player.transform.position;
        }

        private void StartCapture()
        {
            Stage = CredentialStage.Countdown; stageSeconds = 0; CountdownNumber = 3;
            mustLeaveMark = true;
            RewardGrantedThisRun = false; lastInputFrame = Time.frameCount;
            behaviours.Clear(); objects.Clear();
            foreach (var behaviour in session.suspendWhileOpen)
            {
                if (behaviour == null || behaviour == this || behaviour == session || behaviours.ContainsKey(behaviour)) continue;
                behaviours.Add(behaviour, behaviour.enabled); behaviour.enabled = false;
            }
            foreach (var go in session.hideWhileOpen)
            {
                if (go == null || go == gameObject || objects.ContainsKey(go)) continue;
                objects.Add(go, go.activeSelf); go.SetActive(false);
            }
            session.view.SetPrompt(string.Empty); countdownLabel.text = "Mira a la cámara\n3";
        }

        public bool MoveSelection(Vector2Int direction)
        {
            if (Stage != CredentialStage.Puzzle || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return false;
            SelectedIndex = Mathf.Clamp(SelectedIndex / 3 - direction.y, 0, 2) * 3 + Mathf.Clamp(SelectedIndex % 3 + direction.x, 0, 2);
            Refresh(); return true;
        }
        public bool MoveSelected()
        {
            if (Stage != CredentialStage.Puzzle || !Board.TryMove(SelectedIndex)) return false;
            if (Board.IsSolved)
            {
                Stage = CredentialStage.Success; stageSeconds = 0;
                RewardGrantedThisRun = mission.TryAwardDocument(EnrollmentDocument.StudentCard);
            }
            Refresh(); return true;
        }
        public void RestartPuzzle()
        {
            if (Stage != CredentialStage.Puzzle) return;
            Board.Restart(); SelectedIndex = 0; Refresh();
        }

        private void Update()
        {
            if (!IsBusy || ConsumedInputThisFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Close(false); return; }
            if (Stage != CredentialStage.Puzzle && Stage != CredentialStage.Success) return;
            if (keyboard != null && DialogueSession.ConfirmPressedThisFrame())
            {
                lastInputFrame = Time.frameCount;
                if (Stage == CredentialStage.Success) Close(true); else MoveSelected();
                return;
            }
            if (Stage != CredentialStage.Puzzle) return;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) RestartPuzzle();
                else if (keyboard.leftArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.left);
                else if (keyboard.rightArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.right);
                else if (keyboard.upArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.up);
                else if (keyboard.downArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.down);
            }
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            Vector2 position = mouse.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(restartControl, position)) { RestartPuzzle(); return; }
            for (int i = 0; i < frames.Length; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(frames[i].rectTransform, position))
                { SelectedIndex = i; MoveSelected(); Refresh(); break; }
        }

        private void Refresh()
        {
            bool solved = Stage == CredentialStage.Success;
            statusLabel.text = solved ? "Lic. Mony: ¡Listo! Ya eres oficialmente estudiante" : "Arma el marco de tu credencial";
            movesLabel.text = "Movimientos: " + Board.Moves + "\n" + (solved ? "E / Enter · Hablar con Lic. Mony" : "Flechas · Elegir   E / Enter o clic · Mover\nR / Reiniciar · Empezar de nuevo   Esc · Volver");
            rewardLabel.gameObject.SetActive(solved && RewardGrantedThisRun);
            rewardLabel.text = "CREDENCIAL DE ESTUDIANTE CONSEGUIDA";
            for (int i = 0; i < 9; i++)
            {
                int tile = Board.TileAt(i); tiles[i].SetTile(tile);
                numbers[i].text = tile == 0 ? "" : tile.ToString();
                frames[i].color = i == SelectedIndex ? Gold : new Color32(67, 73, 91, 255);
            }
        }

        public void Close(bool speak)
        {
            bool wasBusy = IsBusy;
            Stage = CredentialStage.Waiting; StillSeconds = 0; lastInputFrame = Time.frameCount;
            if (capturePanel != null) capturePanel.SetActive(false);
            if (puzzlePanel != null) puzzlePanel.SetActive(false);
            if (flash != null) flash.gameObject.SetActive(false);
            foreach (var state in behaviours) if (state.Key != null) state.Key.enabled = state.Value;
            foreach (var state in objects) if (state.Key != null) state.Key.SetActive(state.Value);
            behaviours.Clear(); objects.Clear();
            if (wasBusy && speak && successDialogue != null) session.TryBegin(successDialogue);
        }
    }
}

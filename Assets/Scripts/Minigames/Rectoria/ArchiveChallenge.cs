using System;
using System.Collections.Generic;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusQuest.Rectoria
{
    /// <summary>Archive interactions use the existing dialogue lifecycle and persistent event registry.</summary>
    [DefaultExecutionOrder(-90)]
    public sealed class ArchiveChallenge : MonoBehaviour
    {
        public const string IntroId = "rectoria.archive.don_chuy_intro";
        public const string ReadyId = "rectoria.archive.folders_ready";
        public DialogueSession session;
        public EnrollmentMission mission;
        public ArchiveFolderTable table;
        public DialogueDefinition prerequisiteDialogue;
        public DialogueDefinition introDialogue;
        public DialogueDefinition missingCluesDialogue;
        public DialogueDefinition readyDialogue;
        public DialogueDefinition missingTableDialogue;
        public DialogueDefinition successDialogue;
        public GameObject panel;
        public TMP_Text statusLabel;
        public TMP_Text instructionsLabel;
        public TMP_Text clueLabel;
        public TMP_Text[] folderLabels;
        public Image[] folderFrames;
        public Graphic[] folderIcons;
        public RectTransform rewardBanner;
        public TMP_Text rewardLabel;

        public bool IsOpen { get; private set; }
        public bool IsSolved { get; private set; }
        public int AttemptsRemaining { get; private set; }
        public int SelectedIndex { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public int Round { get; private set; }
        public int CluesRead
        {
            get
            {
                int count = 0;
                for (int i = 1; i <= 4; i++) if (session != null && session.HasCompleted(ClueId(i))) count++;
                return count;
            }
        }
        public bool CanPlay => mission != null && mission.IsActive && session.HasCompleted(IntroId) && CluesRead == 4;
        public static string ClueId(int number) => "rectoria.archive.note_" + number;
        public static readonly string[] Clues =
        {
            "La carpeta no es roja ni verde.", "Es del año 2007 o 2008.",
            "No es amarilla ni morada.", "Es la más reciente de las que quedan."
        };

        private readonly int[] layout = new int[9];
        private readonly Dictionary<Behaviour, bool> behaviours = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<GameObject, bool> objects = new Dictionary<GameObject, bool>();
        private int lastInputFrame = -1;
        private float rewardTime;
        private static readonly Color Gold = new Color32(233, 216, 166, 255);

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (rewardBanner != null) rewardBanner.gameObject.SetActive(false);
        }
        private void OnEnable()
        {
            CampusQuestActiveControls.Changed += OnActiveControlsChanged;
            if (session == null) return;
            session.Completed += OnDialogueCompleted;
            session.ProgressChanged += OnProgressChanged;
        }
        private void OnDisable()
        {
            CampusQuestActiveControls.Changed -= OnActiveControlsChanged;
            if (session != null) { session.Completed -= OnDialogueCompleted; session.ProgressChanged -= OnProgressChanged; }
            Close();
        }

        private void OnActiveControlsChanged()
        {
            if (IsOpen) Refresh(null);
        }

        public DialogueDefinition DialogueFor(RectoriaScenePoint point)
        {
            if (point == null) return null;
            if (point.pointId == "don_chuy")
            {
                if (mission == null || !mission.IsActive) return prerequisiteDialogue;
                if (!session.HasCompleted(IntroId)) return introDialogue;
                if (CluesRead < 4) return missingCluesDialogue;
                return table != null && table.IsValid ? readyDialogue : missingTableDialogue;
            }
            if (point.kind == RectoriaPointKind.Clue && point.pointId.StartsWith("nota_", StringComparison.Ordinal))
                return mission != null && mission.IsActive && session.HasCompleted(IntroId) ? point.dialogue : null;
            return point.dialogue;
        }

        private void OnDialogueCompleted(string id, bool first)
        {
            if (id == ReadyId) TryOpen();
        }
        private void OnProgressChanged()
        {
            // New Game/restore must release modal controls even while a puzzle is on screen.
            if (IsOpen && !CanPlay) Close();
        }

        public bool TryOpen()
        {
            if (!isActiveAndEnabled || IsOpen || DialogueSession.IsAnyOpen || !CanPlay
                || table == null || !table.IsValid || panel == null) return false;
            IsOpen = true; IsSolved = false; RewardGrantedThisRun = false; Round = 0;
            lastInputFrame = Time.frameCount;
            CaptureControls();
            if (session.view != null) session.view.SetPrompt(string.Empty);
            panel.SetActive(true);
            if (rewardBanner != null) rewardBanner.gameObject.SetActive(false);
            for (int i = 0; i < 9; i++) layout[i] = i;
            StartRound(false);
            return true;
        }

        public ArchiveFolder FolderAt(int index) => index >= 0 && index < 9 && table != null && table.IsValid ? table.folders[layout[index]] : null;

        private void StartRound(bool retry)
        {
            // First round follows the script table exactly. Only failed rounds shuffle.
            if (retry)
            {
                int[] previous = (int[])layout.Clone();
                for (int i = 8; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    int swap = layout[i]; layout[i] = layout[j]; layout[j] = swap;
                }
                bool same = true;
                for (int i = 0; i < 9; i++) if (layout[i] != previous[i]) same = false;
                if (same) { int swap = layout[0]; layout[0] = layout[1]; layout[1] = swap; }
            }
            Round++; AttemptsRemaining = 3; SelectedIndex = 0;
            Refresh(retry ? "Sin intentos. Don Chuy reorganizó las carpetas. ¡Prueba de nuevo!" : "Encuentra tu acta con las cuatro pistas.");
        }

        public bool MoveSelection(Vector2Int direction)
        {
            if (!IsOpen || IsSolved || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return false;
            int x = Mathf.Clamp(SelectedIndex % 3 + direction.x, 0, 2);
            int y = Mathf.Clamp(SelectedIndex / 3 - direction.y, 0, 2);
            SelectedIndex = y * 3 + x;
            Refresh(null);
            return true;
        }

        public bool ConfirmSelection()
        {
            if (!IsOpen || IsSolved || !CanPlay || table == null || !table.IsValid) return false;
            if (FolderAt(SelectedIndex).IsAnswer)
            {
                IsSolved = true;
                RewardGrantedThisRun = mission.TryAwardDocument(EnrollmentDocument.BirthCertificate);
                rewardTime = 0;
                if (rewardBanner != null) rewardBanner.gameObject.SetActive(RewardGrantedThisRun);
                Refresh(RewardGrantedThisRun ? "¡Correcto! Encontraste tu acta de nacimiento." : "¡Correcto! Ya tienes esta acta; no se entrega otra.");
                return true;
            }
            AttemptsRemaining--;
            if (AttemptsRemaining == 0) StartRound(true);
            else Refresh("Esa carpeta no cumple las pistas. Revisa el color y el año.");
            return false;
        }

        private void Update()
        {
            if (CampusQuestPauseMenu.BlocksInput) return;
            if (!IsOpen) return;
            if (RewardGrantedThisRun && rewardBanner != null)
            {
                rewardTime += CampusQuestGameClock.Delta;
                rewardBanner.localScale = Vector3.one * (1f + .04f * Mathf.Sin(Mathf.Min(1, rewardTime / .6f) * Mathf.PI));
            }
            if (lastInputFrame == Time.frameCount) return;
            if (CampusQuestActiveControls.CancelPressedThisFrame()) { Close(); return; }
            if (CampusQuestActiveControls.PuzzleConfirmPressedThisFrame())
            {
                lastInputFrame = Time.frameCount;
                if (IsSolved) Close(); else ConfirmSelection();
                return;
            }
            if (CampusQuestActiveControls.MenuDirectionThisFrame(out var direction))
                MoveSelection(direction);
        }

        private void Refresh(string message)
        {
            if (statusLabel != null && message != null) statusLabel.text = message;
            if (instructionsLabel != null)
            {
                var move = CampusQuestActiveControls.BindingLabel("Player", "Move");
                var interact = CampusQuestActiveControls.BindingLabel("Player", "Interact");
                var cancel = CampusQuestActiveControls.BindingLabel("UI", "Cancel");
                instructionsLabel.text = IsSolved
                    ? interact + " · Volver al Archivo"
                    : move + " · Elegir    " + interact + " · Confirmar    " + cancel + " · Volver\nIntentos: "
                        + AttemptsRemaining + "/3    Ronda: " + Round;
            }
            if (clueLabel != null) clueLabel.text = "PISTAS · " + CluesRead + "/4\n\n" + string.Join("\n\n", Clues);
            if (rewardLabel != null) rewardLabel.text = "ACTA DE NACIMIENTO CONSEGUIDA";
            for (int i = 0; i < 9; i++)
            {
                var folder = FolderAt(i);
                if (folder == null) continue;
                if (folderLabels != null && i < folderLabels.Length && folderLabels[i] != null) folderLabels[i].text = folder.Label;
                if (folderIcons != null && i < folderIcons.Length && folderIcons[i] != null) folderIcons[i].color = folder.Tint;
                if (folderFrames != null && i < folderFrames.Length && folderFrames[i] != null)
                    folderFrames[i].color = i == SelectedIndex ? Gold : new Color32(67, 73, 91, 255);
            }
        }

        private void CaptureControls()
        {
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
        }

        public void Close()
        {
            if (!IsOpen) return;
            bool speak = IsSolved && RewardGrantedThisRun && isActiveAndEnabled;
            IsOpen = false; lastInputFrame = Time.frameCount;
            if (panel != null) panel.SetActive(false);
            if (rewardBanner != null) { rewardBanner.gameObject.SetActive(false); rewardBanner.localScale = Vector3.one; }
            foreach (var state in behaviours) if (state.Key != null) state.Key.enabled = state.Value;
            foreach (var state in objects) if (state.Key != null) state.Key.SetActive(state.Value);
            behaviours.Clear(); objects.Clear();
            if (speak && successDialogue != null && session != null) session.TryBegin(successDialogue);
        }
        public bool ConsumedInputThisFrame => lastInputFrame == Time.frameCount;
    }
}

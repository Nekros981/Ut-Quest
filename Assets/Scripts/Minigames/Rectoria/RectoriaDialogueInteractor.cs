using CampusQuest.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.Rectoria
{
    /// <summary>Only the adapter depends on Rectoría's existing cell/facing detection.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RectoriaDialogueInteractor : MonoBehaviour
    {
        public const string WelcomeId = "rectoria.rector_welcome";
        public RectoriaPlayerTest player;
        public RectoriaScenePoint[] points;
        public DialogueSession session;
        public ArchiveChallenge archive;
        public CredentialChallenge credential;
        public CashierChallenge cashier;
        public RockyGateReview rocky;
        public InputActionAsset inputActions;
        public bool HasSpokenToRectora => session != null && session.HasCompleted(WelcomeId);

        void OnEnable()
        {
            if (inputActions != null)
                CampusQuestActiveControls.Bind(inputActions);
        }

        public RectoriaScenePoint AvailableSpeaker()
        {
            if (player == null || !player.isActiveAndEnabled || session == null
                || !session.isActiveAndEnabled || DialogueSession.IsAnyOpen || points == null
                || (archive != null && (archive.IsOpen || archive.ConsumedInputThisFrame))
                || (credential != null && (credential.IsBusy || credential.ConsumedInputThisFrame))
                || (cashier != null && (cashier.IsOpen || cashier.ConsumedInputThisFrame))
                || (rocky != null && (rocky.IsOpening || rocky.ConsumedInputThisFrame))) return null;
            if (player.map == null || player.map.grid == null
                || !player.map.CanEnter(player.map.grid.WorldToCell(player.transform.position))) return null;
            foreach (var point in points)
                if (point != null && (point.kind == RectoriaPointKind.Npc || point.kind == RectoriaPointKind.Clue
                    || (point.kind == RectoriaPointKind.ReceiptsTable && cashier != null))
                    && point.IsFacing(player))
                {
                    var definition = DefinitionFor(point);
                    if (definition != null && definition.IsValid) return point;
                }
            return null;
        }

        public bool TryInteract()
        {
            if (session == null || session.LastInputFrame == Time.frameCount) return false;
            var point = AvailableSpeaker();
            if (point != null && point.kind == RectoriaPointKind.ReceiptsTable && cashier != null && cashier.CanStart)
                return cashier.TryOpen();
            return point != null && session.TryBegin(DefinitionFor(point));
        }

        private DialogueDefinition DefinitionFor(RectoriaScenePoint point)
        {
            if (point.pointId == "rocky" && rocky != null) return rocky.DialogueForRocky();
            if (point.pointId == "lic_mony" && credential != null) return credential.DialogueForMony();
            if (point.pointId == "beto" && cashier != null) return cashier.DialogueForBeto();
            if (point.kind == RectoriaPointKind.ReceiptsTable && cashier != null) return cashier.DialogueForTable();
            return archive != null ? archive.DialogueFor(point) : point.dialogue;
        }

        private void Update()
        {
            if (session == null || session.view == null) return;
            if (DialogueSession.IsAnyOpen) { session.view.SetPrompt(string.Empty); return; }
            var point = AvailableSpeaker();
            string submit = CampusQuestActiveControls.BindingLabel("UI", "Submit");
            session.view.SetPrompt(point != null ? submit + " · "
                + (point.kind == RectoriaPointKind.Clue ? "Leer " : point.kind == RectoriaPointKind.ReceiptsTable ? "Revisar " : "Hablar con ") + point.displayName
                + (point.kind == RectoriaPointKind.Clue && point.dialogue != null && session.HasCompleted(point.dialogue.dialogueId) ? " · Leída" : string.Empty)
                : string.Empty);
            if (DialogueSession.ConfirmPressedThisFrame()) TryInteract();
        }

        private void OnDisable()
        {
            if (session != null && session.view != null) session.view.SetPrompt(string.Empty);
        }
    }
}

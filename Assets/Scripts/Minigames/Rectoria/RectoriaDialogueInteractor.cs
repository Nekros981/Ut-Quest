using CampusQuest.Dialogue;
using UnityEngine;

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
        public bool HasSpokenToRectora => session != null && session.HasCompleted(WelcomeId);

        public RectoriaScenePoint AvailableSpeaker()
        {
            if (player == null || !player.isActiveAndEnabled || session == null
                || !session.isActiveAndEnabled || DialogueSession.IsAnyOpen || points == null) return null;
            if (player.map == null || player.map.grid == null
                || !player.map.CanEnter(player.map.grid.WorldToCell(player.transform.position))) return null;
            foreach (var point in points)
                if (point != null && point.kind == RectoriaPointKind.Npc && point.dialogue != null
                    && point.dialogue.IsValid && point.IsFacing(player)) return point;
            return null;
        }

        public bool TryInteract()
        {
            if (session == null || session.LastInputFrame == Time.frameCount) return false;
            var point = AvailableSpeaker();
            return point != null && session.TryBegin(point.dialogue);
        }

        private void Update()
        {
            if (session == null || session.view == null) return;
            if (DialogueSession.IsAnyOpen) { session.view.SetPrompt(string.Empty); return; }
            var point = AvailableSpeaker();
            session.view.SetPrompt(point != null ? "E / Enter · Hablar con " + point.displayName : string.Empty);
            if (DialogueSession.ConfirmPressedThisFrame()) TryInteract();
        }

        private void OnDisable()
        {
            if (session != null && session.view != null) session.view.SetPrompt(string.Empty);
        }
    }
}

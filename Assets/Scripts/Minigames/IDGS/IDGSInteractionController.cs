using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.IDGS
{
    [DefaultExecutionOrder(100)]
    public sealed class IDGSInteractionController : MonoBehaviour
    {
        public IDGSPlayerMotor player;
        public IDGSInteractionPoint[] points;
        public TMP_Text prompt;
        public TMP_Text notice;
        private const string Controls = "WASD / Flechas · Caminar    Shift · Correr";
        public IDGSInteractionPoint LastInteraction { get; private set; }
        private bool confirmArmed;
        private float noticeUntil;

        public IDGSInteractionPoint AvailablePoint()
        {
            if (IDGSSceneTravel.IsChanging || DialogueSession.IsAnyOpen || player == null || !player.enabled) return null;
            foreach (var point in points)
                if (point != null && point.IsAvailable(player)) return point;
            return null;
        }

        public bool TryInteract()
        {
            var point = AvailablePoint();
            if (point == null) return false;
            if (point.kind != IDGSPointKind.Station)
            {
                if (!IDGSSceneTravel.TryTravel(point)) return false;
            }
            else
            {
                if (point.onInteracted.GetPersistentEventCount() == 0)
                {
                    if (notice != null) notice.text = point.displayName + " · Próximamente";
                    noticeUntil = Time.unscaledTime + 2;
                }
                point.onInteracted.Invoke();
            }
            LastInteraction = point;
            return true;
        }

        private void Update()
        {
            var point = AvailablePoint();
            if (prompt != null) prompt.text = point == null ? string.Empty : point.displayName + "   ·   E / Enter";
            if (notice != null && Time.unscaledTime > noticeUntil) notice.text = Controls;
            var keyboard = Keyboard.current;
            bool held = keyboard != null && (keyboard.eKey.isPressed || keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
            if (!held) confirmArmed = true;
            if (confirmArmed && DialogueSession.ConfirmPressedThisFrame())
            {
                confirmArmed = false;
                TryInteract();
            }
        }
    }
}

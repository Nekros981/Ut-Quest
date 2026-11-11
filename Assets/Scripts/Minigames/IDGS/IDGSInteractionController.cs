using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;

namespace CampusQuest.IDGS
{
    [DefaultExecutionOrder(100)]
    public sealed class IDGSInteractionController : MonoBehaviour
    {
        public IDGSPlayerMotor player;
        public IDGSInteractionPoint[] points;
        public TMP_Text prompt;
        public TMP_Text notice;
        public IDGSInteractionPoint LastInteraction { get; private set; }
        bool confirmArmed;
        float noticeUntil;
        string lastWalkHint;

        static string WalkControlsHint()
        {
            var move = CampusQuestActiveControls.BindingLabel("Player", "Move");
            var sprint = CampusQuestActiveControls.BindingLabel("Player", "Sprint");
            return move + " · Caminar    " + sprint + " · Correr";
        }

        void OnEnable()
        {
            CampusQuestActiveControls.Changed += OnActiveControlsChanged;
            lastWalkHint = null;
        }

        void OnDisable()
        {
            CampusQuestActiveControls.Changed -= OnActiveControlsChanged;
        }

        void OnActiveControlsChanged()
        {
            lastWalkHint = null;
        }

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
            if (prompt != null)
            {
                string interact = CampusQuestActiveControls.BindingLabel("Player", "Interact");
                prompt.text = point == null ? string.Empty : point.displayName + "   ·   " + interact;
            }

            var walkHint = WalkControlsHint();
            if (notice != null && Time.unscaledTime > noticeUntil && walkHint != lastWalkHint)
            {
                notice.text = walkHint;
                lastWalkHint = walkHint;
            }

            if (!IsInteractHeld())
                confirmArmed = true;
            if (confirmArmed && CampusQuestActiveControls.PuzzleConfirmPressedThisFrame())
            {
                confirmArmed = false;
                TryInteract();
            }
        }

        static bool IsInteractHeld()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.eKey.isPressed || keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
        }
    }
}

using CampusQuest.Dialogue;
using UnityEngine;

namespace CampusQuest.IDGS
{
    /// <summary>Shared opening/closing behaviour for the existing IDGS station activities.</summary>
    public sealed class IDGSActivityModal
    {
        public bool IsOpen { get; private set; }
        public int OpenedFrame { get; private set; }
        private readonly IDGSPlayerMotor player;
        private readonly IDGSInteractionController interactor;
        private readonly GameObject hud;
        private readonly GameObject panel;
        private bool playerWasEnabled;
        private bool interactorWasEnabled;
        private bool hudWasActive;
        private bool cursorWasVisible;
        private CursorLockMode cursorLock;

        public IDGSActivityModal(IDGSPlayerMotor player, IDGSInteractionController interactor, GameObject hud, GameObject panel)
        {
            this.player = player;
            this.interactor = interactor;
            this.hud = hud;
            this.panel = panel;
            panel.SetActive(false);
        }

        public bool TryOpen(IDGSInteractionPoint station)
        {
            if (IsOpen || IDGSSceneTravel.IsChanging || DialogueSession.IsAnyOpen
                || player == null || !player.enabled || !station.IsAvailable(player)) return false;
            playerWasEnabled = player.enabled;
            interactorWasEnabled = interactor.enabled;
            hudWasActive = hud.activeSelf;
            cursorWasVisible = Cursor.visible;
            cursorLock = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            player.enabled = false;
            interactor.enabled = false;
            hud.SetActive(false);
            IsOpen = true;
            OpenedFrame = Time.frameCount;
            panel.SetActive(true);
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (player != null) player.enabled = playerWasEnabled;
            if (interactor != null) interactor.enabled = interactorWasEnabled;
            if (hud != null) hud.SetActive(hudWasActive);
            Cursor.visible = cursorWasVisible;
            Cursor.lockState = cursorLock;
        }
    }
}

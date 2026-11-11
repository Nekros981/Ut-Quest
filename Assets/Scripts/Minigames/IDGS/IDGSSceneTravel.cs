using System;
using CampusQuest.Dialogue;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusQuest.IDGS
{
    /// <summary>One-use scene arrival request. Durable progress remains owned by GameProgress.</summary>
    public static class IDGSSceneTravel
    {
        public static bool IsChanging { get; private set; }
        private static string pendingScene;
        private static Vector2Int pendingCell;
        private static Vector2Int pendingFacing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            IsChanging = false;
            pendingScene = null;
        }

        public static void ClearPending() { IsChanging = false; pendingScene = null; }

        public static bool TryTravel(IDGSInteractionPoint portal)
        {
            if (!Application.isPlaying || IsChanging || portal == null || string.IsNullOrEmpty(portal.destinationScene)
                || !Application.CanStreamedLevelBeLoaded(portal.destinationScene)) return false;
            GameProgress.EnsureLoaded();
            if (portal.pointId == "idgs.cmd.exit" && IDGSCompletion.AllActivitiesCompleted && !GameProgress.HasEvent(IDGSCompletion.SealEvent))
            {
                if (!GameProgress.HasEvent(IDGSCompletion.RequestEvent) && !GameProgress.RegisterEvent(IDGSCompletion.RequestEvent)) return false;
            }
            pendingScene = portal.destinationScene;
            pendingCell = portal.destinationCell;
            pendingFacing = portal.destinationFacing;
            IsChanging = true;
            try
            {
                var operation = SceneManager.LoadSceneAsync(pendingScene, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("No se pudo cargar el acceso.");
                operation.completed += OnLoaded;
                return true;
            }
            catch (Exception error)
            {
                IsChanging = false;
                pendingScene = null;
                Debug.LogError(error.Message);
                return false;
            }
        }

        private static void OnLoaded(AsyncOperation operation)
        {
            operation.completed -= OnLoaded;
            IsChanging = false;
            pendingScene = null;
        }

        internal static void ApplyArrival(IDGSPlayerMotor player)
        {
            if (pendingScene != player.gameObject.scene.name) return;
            player.PlaceAtCell(pendingCell, pendingFacing);
            pendingScene = null;
        }
    }
}

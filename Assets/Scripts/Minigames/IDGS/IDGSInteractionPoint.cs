using UnityEngine;
using UnityEngine.Events;

namespace CampusQuest.IDGS
{
    public enum IDGSPointKind { Station, Entrance, Exit }

    public sealed class IDGSInteractionPoint : MonoBehaviour
    {
        public string pointId;
        public string displayName;
        public IDGSPointKind kind;
        public Vector2Int cell;
        public Vector2Int approachCell;
        public string destinationScene;
        public Vector2Int destinationCell;
        public Vector2Int destinationFacing = Vector2Int.up;
        public UnityEvent onInteracted = new UnityEvent();

        public bool IsAvailable(IDGSPlayerMotor player)
        {
            return isActiveAndEnabled && player != null && player.IsAtRest
                && player.Cell == approachCell && player.Facing == cell - approachCell;
        }
    }
}

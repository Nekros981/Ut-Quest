using UnityEngine;

namespace CampusQuest.Rectoria
{
    public enum RectoriaPointKind { Npc, Clue, PhotoMark, ReceiptsTable, LockedOffice, BlocksGate }

    /// <summary>Scene anchors for the tutorial. No mission or reward is granted by an anchor.</summary>
    public sealed class RectoriaScenePoint : MonoBehaviour
    {
        public string pointId;
        public string displayName;
        public RectoriaPointKind kind;
        public Vector3Int cell;
        public Vector3Int approachCell;
        public bool provisionalVisual;
        public CampusQuest.Dialogue.DialogueDefinition dialogue;
        [TextArea] public string designNote;

        public bool IsFacing(RectoriaPlayerTest player)
        {
            if (player == null || player.map == null || player.map.grid == null || !isActiveAndEnabled)
                return false;
            // Only advertise an interaction at a cell centre, never halfway through a step.
            var current = player.map.grid.WorldToCell(player.transform.position);
            if ((player.transform.position - player.map.CellCenter(current)).sqrMagnitude > 0.0001f)
                return false;
            var delta = cell - current;
            return delta.z == 0 && Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1
                && delta.x == player.facing.x && delta.y == player.facing.y;
        }
    }
}

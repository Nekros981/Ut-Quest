using UnityEngine;
using UnityEngine.Tilemaps;

namespace CampusQuest.Rectoria
{
    // Logical walkability; never infer walkability from sprite colors.
    public sealed class RectoriaGridMap : MonoBehaviour
    {
        public Grid grid;
        public Tilemap floor;
        public Tilemap walls;
        public Tilemap obstacles;
        public Tilemap doors;

        public bool CanEnter(Vector3Int cell)
        {
            return floor != null && floor.HasTile(cell)
                && (walls == null || !walls.HasTile(cell))
                && (obstacles == null || !obstacles.HasTile(cell))
                && (doors == null || !doors.HasTile(cell));
        }

        public Vector3 CellCenter(Vector3Int cell)
        {
            return grid != null ? grid.GetCellCenterWorld(cell) : (Vector3)cell + new Vector3(.5f, .5f, 0);
        }
    }
}

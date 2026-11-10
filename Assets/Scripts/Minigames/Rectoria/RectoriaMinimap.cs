using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.Rectoria
{
    public enum MinimapCell { Empty, Walkable, Wall, Blocked }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RectoriaMinimap : MaskableGraphic
    {
        public const int Columns = 44, Rows = 46;
        public RectoriaGridMap map;
        public RectoriaPlayerTest player;
        public ArchiveChallenge archive;
        public CredentialChallenge credential;
        public CashierChallenge cashier;
        public CanvasGroup visibility;
        public RectTransform playerDot;
        public RectTransform[] markers;
        public RectoriaScenePoint[] destinations;

        public void RefreshMap() { SetVerticesDirty(); }

        public MinimapCell KindAt(Vector3Int cell)
        {
            if (map == null) return MinimapCell.Empty;
            if (map.walls != null && map.walls.HasTile(cell)) return MinimapCell.Wall;
            if (map.CanEnter(cell)) return MinimapCell.Walkable;
            if (map.floor != null && map.floor.HasTile(cell)) return MinimapCell.Blocked;
            return MinimapCell.Empty;
        }

        public Vector2 PositionForWorld(Vector3 world)
        {
            Vector3 logical = map.grid.transform.InverseTransformPoint(world);
            Vector3 origin = map.grid.CellToLocal(Vector3Int.zero);
            Vector3 axisX = map.grid.CellToLocal(Vector3Int.right) - origin;
            Vector3 axisY = map.grid.CellToLocal(Vector3Int.up) - origin;
            Vector3 delta = logical - origin;
            var r = rectTransform.rect;
            return new Vector2(r.xMin + Vector3.Dot(delta, axisX) / axisX.sqrMagnitude / Columns * r.width,
                r.yMin + Vector3.Dot(delta, axisY) / axisY.sqrMagnitude / Rows * r.height);
        }

        private void LateUpdate()
        {
            if (map == null || map.grid == null || player == null) return;
            if (visibility != null)
            {
                visibility.alpha = (archive != null && archive.IsOpen) || (credential != null && credential.IsBusy)
                    || (cashier != null && cashier.IsOpen) ? 0 : 1;
                visibility.interactable = false; visibility.blocksRaycasts = false;
            }
            if (playerDot != null) playerDot.anchoredPosition = PositionForWorld(player.transform.position);
            for (int i = 0; destinations != null && markers != null && i < destinations.Length && i < markers.Length; i++)
                if (destinations[i] != null && markers[i] != null) markers[i].anchoredPosition = PositionForWorld(map.CellCenter(destinations[i].cell));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (map == null) return;
            var r = GetPixelAdjustedRect(); float w = r.width / Columns, h = r.height / Rows;
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
            {
                var cell = new Vector3Int(x, y, 0); var kind = KindAt(cell);
                if (kind == MinimapCell.Empty) continue;
                Color tint = kind == MinimapCell.Wall ? new Color32(154, 144, 114, 255)
                    : kind == MinimapCell.Blocked ? new Color32(62, 64, 75, 255)
                    : RectoriaSceneGuide.AreaAt(cell).StartsWith("PASILLO") || y <= 8
                        ? new Color32(77, 89, 110, 255) : new Color32(95, 114, 119, 255);
                int start = vh.currentVertCount; float l = r.xMin + x * w, b = r.yMin + y * h;
                vh.AddVert(new Vector3(l, b), tint, Vector2.zero); vh.AddVert(new Vector3(l, b + h), tint, Vector2.zero);
                vh.AddVert(new Vector3(l + w, b + h), tint, Vector2.zero); vh.AddVert(new Vector3(l + w, b), tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}

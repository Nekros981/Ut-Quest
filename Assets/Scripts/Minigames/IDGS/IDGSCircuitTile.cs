using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CampusQuest.IDGS
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IDGSCircuitTile : MaskableGraphic, IPointerClickHandler
    {
        public IDGSCircuitChallenge challenge;
        public int index;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) challenge.RotateAt(index);
        }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            bool selected = challenge != null && challenge.SelectedIndex == index;
            Draw(vertices, rect, selected ? new Color32(225, 195, 118, 255) : new Color32(55, 76, 88, 255));
            var inner = new Rect(rect.xMin + 3, rect.yMin + 3, rect.width - 6, rect.height - 6);
            Draw(vertices, inner, new Color32(23, 39, 47, 255));
            if (challenge == null || challenge.Board == null) return;
            int mask = challenge.Board.MaskAt(index);
            bool powered = challenge.Board.IsPowered(index);
            Color copper = powered ? new Color32(85, 211, 165, 255) : new Color32(126, 144, 153, 255);
            if (powered && challenge.IsSolved)
                copper = Color.Lerp(new Color32(248, 209, 79, 255), Color.white, challenge.Electricity * .55f);
            Vector2 center = rect.center;
            float halfWidth = rect.width * .5f, halfHeight = rect.height * .5f;
            if ((mask & IDGSCircuitBoard.North) != 0) Draw(vertices, new Rect(center.x - 8, center.y, 16, halfHeight), copper);
            if ((mask & IDGSCircuitBoard.East) != 0) Draw(vertices, new Rect(center.x, center.y - 8, halfWidth, 16), copper);
            if ((mask & IDGSCircuitBoard.South) != 0) Draw(vertices, new Rect(center.x - 8, rect.yMin, 16, halfHeight), copper);
            if ((mask & IDGSCircuitBoard.West) != 0) Draw(vertices, new Rect(rect.xMin, center.y - 8, halfWidth, 16), copper);
            Draw(vertices, new Rect(center.x - 14, center.y - 14, 28, 28), new Color32(10, 21, 28, 255));
            Draw(vertices, new Rect(center.x - 10, center.y - 10, 20, 20), copper);
            Draw(vertices, new Rect(center.x - 4, center.y - 4, 8, 8), new Color32(30, 48, 55, 255));
        }
        private static void Draw(VertexHelper vertices, Rect rect, Color color)
        {
            int index = vertices.currentVertCount;
            vertices.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index, index + 2, index + 3);
        }
    }
}

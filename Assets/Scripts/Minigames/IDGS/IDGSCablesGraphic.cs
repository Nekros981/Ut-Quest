using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.IDGS
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IDGSCablesGraphic : MaskableGraphic
    {
        public IDGSCablesChallenge challenge;
        public static float RowY(int row) => 138 - row * 92;
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (challenge == null) return;
            for (int colorIndex = 0; colorIndex < 4; colorIndex++)
            {
                Vector2 start = new Vector2(-298, RowY(colorIndex));
                Vector2 end = new Vector2(-235, start.y);
                if (challenge.IsConnected(colorIndex)) end = new Vector2(298, RowY(challenge.RightSlotFor(colorIndex)));
                else if (challenge.DraggingIndex == colorIndex) end = challenge.DragPosition;
                Color tint = Color.Lerp(IDGSCablesChallenge.CableColors[colorIndex], Color.white, challenge.Electricity * .6f);
                float bend = challenge.IsConnected(colorIndex) ? -120 + colorIndex * 80 : Mathf.Lerp(start.x, end.x, .5f);
                DrawPath(vertices, start, end, bend, 13, new Color32(8, 15, 21, 255));
                DrawPath(vertices, start, end, bend, 8, tint);
                if (challenge.IsConnected(colorIndex) && challenge.Electricity > .4f)
                {
                    float x = Mathf.Lerp(-200, 210, challenge.Electricity);
                    Quad(vertices, new Rect(x, Mathf.Lerp(start.y, end.y, .5f) - 8, 4, 16), Color.white);
                }
            }
        }

        private static void DrawPath(VertexHelper vertices, Vector2 start, Vector2 end, float bend, float width, Color tint)
        {
            Segment(vertices, start, new Vector2(bend, start.y), width, tint);
            Segment(vertices, new Vector2(bend, start.y), new Vector2(bend, end.y), width, tint);
            Segment(vertices, new Vector2(bend, end.y), end, width, tint);
        }
        private static void Segment(VertexHelper vertices, Vector2 a, Vector2 b, float width, Color tint)
        {
            Quad(vertices, new Rect(Mathf.Min(a.x, b.x) - width * .5f, Mathf.Min(a.y, b.y) - width * .5f,
                Mathf.Abs(a.x - b.x) + width, Mathf.Abs(a.y - b.y) + width), tint);
        }
        private static void Quad(VertexHelper vertices, Rect rect, Color tint)
        {
            int index = vertices.currentVertCount;
            vertices.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.zero);
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index + 2, index + 3, index);
        }
    }
}

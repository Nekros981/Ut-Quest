using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.Rectoria
{
    /// <summary>Code-native pixel folder silhouette; no suitable inventory folder art exists yet.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArchiveFolderGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            Quad(vh, r, .05f, .12f, .95f, .76f, color * new Color(.7f, .7f, .7f, 1));
            Quad(vh, r, .05f, .76f, .43f, .91f, color * new Color(.85f, .85f, .85f, 1));
            Quad(vh, r, 0, .06f, 1, .68f, color);
            Quad(vh, r, .04f, .64f, .96f, .68f, color * new Color(1.15f, 1.15f, 1.15f, 1));
            Quad(vh, r, .08f, .12f, .92f, .16f, color * new Color(.8f, .8f, .8f, 1));
        }

        private static void Quad(VertexHelper vh, Rect r, float x0, float y0, float x1, float y1, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin + r.width * x0, r.yMin + r.height * y0), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin + r.width * x0, r.yMin + r.height * y1), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin + r.width * x1, r.yMin + r.height * y1), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin + r.width * x1, r.yMin + r.height * y0), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}

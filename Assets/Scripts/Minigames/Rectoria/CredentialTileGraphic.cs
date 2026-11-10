using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.Rectoria
{
    /// <summary>A shared code-native provisional ID image, cropped into eight equal pieces.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CredentialTileGraphic : MaskableGraphic
    {
        [SerializeField] private int tile = 1;
        public void SetTile(int value) { tile = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (tile == 0) return;
            Paint(vh, new Rect(0, 0, 3, 3), new Color32(225, 219, 192, 255));
            Paint(vh, new Rect(0, 2.35f, 3, .65f), new Color32(39, 52, 74, 255));
            Paint(vh, new Rect(.14f, 2.48f, 2.72f, .1f), new Color32(207, 169, 81, 255));
            Paint(vh, new Rect(.18f, .68f, 1.02f, 1.44f), new Color32(93, 124, 147, 255));
            Paint(vh, new Rect(.43f, 1.35f, .51f, .57f), new Color32(208, 160, 117, 255));
            Paint(vh, new Rect(.39f, 1.8f, .6f, .26f), new Color32(66, 47, 41, 255));
            Paint(vh, new Rect(.26f, .78f, .87f, .57f), new Color32(69, 91, 137, 255));
            for (int i = 0; i < 4; i++) Paint(vh, new Rect(1.43f, 1.83f - i * .32f, 1.25f - i * .15f, .11f), new Color32(81, 88, 99, 255));
            for (int i = 0; i < 13; i++) Paint(vh, new Rect(1.45f + i * .09f, .32f, .04f, .35f), new Color32(50, 56, 67, 255));
            Paint(vh, new Rect(.17f, .17f, .86f, .16f), new Color32(185, 145, 62, 255));
        }
        private void Paint(VertexHelper vh, Rect shape, Color tint)
        {
            int index = tile - 1;
            var crop = new Rect(index % 3, 2 - index / 3, 1, 1);
            float x0 = Mathf.Max(shape.xMin, crop.xMin), x1 = Mathf.Min(shape.xMax, crop.xMax);
            float y0 = Mathf.Max(shape.yMin, crop.yMin), y1 = Mathf.Min(shape.yMax, crop.yMax);
            if (x0 >= x1 || y0 >= y1) return;
            var r = GetPixelAdjustedRect(); int start = vh.currentVertCount;
            float l = r.xMin + (x0 - crop.xMin) * r.width, rr = r.xMin + (x1 - crop.xMin) * r.width;
            float b = r.yMin + (y0 - crop.yMin) * r.height, t = r.yMin + (y1 - crop.yMin) * r.height;
            vh.AddVert(new Vector3(l, b), tint, Vector2.zero); vh.AddVert(new Vector3(l, t), tint, Vector2.zero);
            vh.AddVert(new Vector3(rr, t), tint, Vector2.zero); vh.AddVert(new Vector3(rr, b), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}

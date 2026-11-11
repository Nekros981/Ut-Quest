#if UNITY_EDITOR
using UnityEngine;

public static class SpBosqueFootColliderUtil
{
    public static void EnsureFootBox(GameObject go, Sprite sprite)
    {
        if (sprite == null)
            return;

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null)
            box = go.AddComponent<BoxCollider2D>();

        var b = sprite.bounds;
        float footHeight = Mathf.Max(b.size.y * 0.35f, 0.12f);
        float footWidth = Mathf.Max(b.size.x * 0.85f, 0.12f);
        box.size = new Vector2(footWidth, footHeight);
        box.offset = new Vector2(b.center.x, b.min.y + footHeight * 0.5f);
    }
}
#endif

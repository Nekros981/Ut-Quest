using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class CampusQuestBullet : MonoBehaviour
{
    const float Speed = 14f;
    const float SpawnOffset = 0.45f;
    const float HitRadius = 0.1f;

    SpriteRenderer spriteRenderer;
    CircleCollider2D circle;
    Vector2 direction;
    Transform ignoreRoot;
    bool fromMateo;

    public static void Spawn(
        Sprite sprite,
        Vector2 facing,
        Vector2 origin,
        bool fromMateo,
        Transform ignoreRoot)
    {
        if (sprite == null || facing.sqrMagnitude < 0.0001f)
            return;

        facing.Normalize();
        var go = new GameObject(fromMateo ? "Bala_Mateo" : "Bala_Secuaz");
        go.transform.position = origin + facing * SpawnOffset;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 55;

        var bullet = go.AddComponent<CampusQuestBullet>();
        bullet.fromMateo = fromMateo;
        bullet.direction = facing;
        bullet.ignoreRoot = ignoreRoot;
        bullet.spriteRenderer = renderer;
        bullet.ApplySpriteOrientation(facing);

        var body = go.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        bullet.circle = go.GetComponent<CircleCollider2D>();
        bullet.circle.isTrigger = true;
        bullet.circle.radius = HitRadius;
    }

    void ApplySpriteOrientation(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            transform.rotation = Quaternion.identity;
            spriteRenderer.flipX = dir.x < 0f;
            return;
        }

        spriteRenderer.flipX = false;
        float z = dir.y > 0f ? 90f : -90f;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void FixedUpdate()
    {
        float step = Speed * Time.fixedDeltaTime;
        transform.position += (Vector3)(direction * step);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsIgnored(other.transform))
            return;

        if (fromMateo)
        {
            var visual = other.GetComponentInParent<CampusQuestSecuazVisualDriver>();
            var brain = other.GetComponentInParent<CampusQuestSecuazBrain>();
            if (brain != null && (visual == null || !visual.IsDefeated))
            {
                brain.RecibirGolpe();
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            if (other.GetComponentInParent<CampusQuestMateoPlayerInput>() != null)
            {
                var hearts = FindAnyObjectByType<CampusQuestHeartBar>();
                hearts?.TakeDamage(1);
                Destroy(gameObject);
                return;
            }
        }

        Destroy(gameObject);
    }

    bool IsIgnored(Transform other)
    {
        if (ignoreRoot == null)
            return false;
        return other == ignoreRoot || other.IsChildOf(ignoreRoot);
    }
}

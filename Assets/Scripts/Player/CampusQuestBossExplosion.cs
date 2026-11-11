using UnityEngine;

public sealed class CampusQuestBossExplosion : MonoBehaviour
{
    const float FrameRate = 12f;
    const float DamageRadius = 3f;

    static Sprite[] cachedFrames;

    [SerializeField] Sprite[] explosionFrames;
    SpriteRenderer spriteRenderer;
    int frameIndex;
    float timer;
    bool damageApplied;

    public static void Spawn(Vector3 position, Sprite[] frames, AudioClip explosionClip)
    {
        if (frames == null || frames.Length == 0)
            frames = LoadDefaultFrames();

        var go = new GameObject("Boss_Explosion");
        go.transform.position = position;
        var fx = go.AddComponent<CampusQuestBossExplosion>();
        fx.explosionFrames = frames;
        fx.spriteRenderer = go.AddComponent<SpriteRenderer>();
        fx.spriteRenderer.sortingOrder = 80;
        if (frames.Length > 0)
        {
            fx.spriteRenderer.sprite = frames[0];
            float cellSize = 1f;
            var bounds = frames[0].bounds.size;
            float target = cellSize * 6f;
            float scale = target / Mathf.Max(bounds.x, bounds.y, 0.01f);
            go.transform.localScale = Vector3.one * scale;
        }

        if (explosionClip != null)
            PlayClipAt(position, explosionClip);
    }

    static Sprite[] LoadDefaultFrames()
    {
        if (cachedFrames != null && cachedFrames.Length > 0)
            return cachedFrames;

        var loaded = Resources.LoadAll<Sprite>("BossExplosion");
        if (loaded != null && loaded.Length > 0)
        {
            cachedFrames = loaded;
            return cachedFrames;
        }

        return System.Array.Empty<Sprite>();
    }

    static void PlayClipAt(Vector3 position, AudioClip clip)
    {
        var go = new GameObject("GrenadeExplosionAudio");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f;
        source.Play();
        Destroy(go, clip.length + 0.1f);
    }

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyDamage();
    }

    void Update()
    {
            if (CampusQuestPauseMenu.BlocksInput) return;
        if (explosionFrames == null || explosionFrames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        if (timer < 1f / FrameRate)
            return;

        timer = 0f;
        frameIndex++;
        if (frameIndex >= explosionFrames.Length)
        {
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite = explosionFrames[frameIndex];
    }

    void ApplyDamage()
    {
        if (damageApplied)
            return;
        damageApplied = true;

        var mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        if (mateo == null)
            return;

        float dist = Vector2.Distance(transform.position, mateo.transform.position);
        if (dist > DamageRadius)
            return;

        FindAnyObjectByType<CampusQuestHeartBar>()?.TakeDamage(1);
    }
}

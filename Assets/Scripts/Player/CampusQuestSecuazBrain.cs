using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class CampusQuestSecuazBrain : MonoBehaviour
{
    const float StopDistance = 0.7f;
    const float PistolHoldDistance = 2.5f;
    const float MeleeAttackRange = 0.9f;
    const float MeleeHitRange = 0.9f;
    const float SprintDistance = 4f;
    const float WalkSpeed = 2.2f;
    const float SprintSpeed = 3.6f;
    const float AttackCooldown = 1.2f;
    const float ShootCooldown = 1.5f;
    const float MeleeHitDelay = 0.35f;
    [SerializeField] CampusQuestSecuazVisualDriver visual;
    [SerializeField] int maxVida = 2;
    [SerializeField] Sprite bulletSprite;

    Rigidbody2D body;
    int vida;
    CampusQuestMateoPlayerInput mateo;
    float cooldown;
    float pendingMeleeHit = -1f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (visual == null)
            visual = GetComponentInChildren<CampusQuestSecuazVisualDriver>(true);
        vida = Mathf.Max(1, maxVida);
    }

    public void RecibirGolpe()
    {
        if (visual == null || visual.IsDefeated)
            return;

        vida = Mathf.Max(0, vida - 1);
        if (vida > 0)
            return;

        pendingMeleeHit = -1f;
        body.linearVelocity = Vector2.zero;
        visual.PlayDerrotado();
        DisablePhysicsWhenDefeated();
        CampusQuestObjectives.Instance?.TryAdvanceAfterFirstDefeat();

        var loot = GetComponent<CampusQuestSecuazLoot>();
        if (loot != null && CampusQuestSecuazLoot.TryClaimFirstDefeat(loot))
            NotifyOtherSecuazToUsePistol();
    }

    void NotifyOtherSecuazToUsePistol()
    {
        foreach (var other in FindObjectsByType<CampusQuestSecuazBrain>(FindObjectsSortMode.None))
        {
            if (other == null || other == this)
                continue;

            var otherVisual = other.GetComponentInChildren<CampusQuestSecuazVisualDriver>(true);
            if (otherVisual == null || otherVisual.IsDefeated)
                continue;

            otherVisual.SetArma(CampusQuestSecuazVisualDriver.ArmaPistola);
            return;
        }
    }

    void Start()
    {
        mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
    }

    void Update()
    {
        if (visual == null || mateo == null)
            return;

        cooldown -= Time.deltaTime;
        TickPendingMeleeHit();

        if (visual.IsDefeated)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (visual.IsActing)
            return;

        float dist = DistanceToMateo();

        if (cooldown <= 0f)
        {
            if (visual.CurrentArma == CampusQuestSecuazVisualDriver.ArmaTolete &&
                dist < MeleeAttackRange)
            {
                if (visual.PlayGolpe())
                {
                    cooldown = AttackCooldown;
                    pendingMeleeHit = MeleeHitDelay;
                    body.linearVelocity = Vector2.zero;
                }
                return;
            }

            if (visual.CurrentArma == CampusQuestSecuazVisualDriver.ArmaPistola)
            {
                var towardMateo = (Vector2)(mateo.transform.position - transform.position);
                visual.LookAt(towardMateo);
                if (visual.PlayDisparo())
                {
                    cooldown = ShootCooldown;
                    SpawnBullet();
                }
                return;
            }
        }
    }

    void FixedUpdate()
    {
        if (visual == null || mateo == null || visual.IsDefeated)
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
            return;
        }

        if (visual.IsActing && visual.CurrentArma == CampusQuestSecuazVisualDriver.ArmaTolete)
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
            return;
        }

        float dist = DistanceToMateo();
        float holdDistance = visual.CurrentArma == CampusQuestSecuazVisualDriver.ArmaPistola
            ? PistolHoldDistance
            : StopDistance;

        if (dist <= holdDistance)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        var delta = mateo.transform.position - transform.position;
        if (delta.sqrMagnitude < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        float speed = dist > SprintDistance ? SprintSpeed : WalkSpeed;
        body.linearVelocity = ((Vector2)delta).normalized * speed;
    }

    float DistanceToMateo() =>
        Vector2.Distance(transform.position, mateo.transform.position);

    void SpawnBullet()
    {
        if (visual == null || mateo == null)
            return;

        var origin = (Vector2)visual.ShooterRoot.position;
        var towardMateo = (Vector2)mateo.transform.position - origin;
        if (towardMateo.sqrMagnitude < 0.0001f)
            return;

        CampusQuestBullet.Spawn(bulletSprite, towardMateo, origin, false, visual.ShooterRoot);
    }

    void DisablePhysicsWhenDefeated()
    {
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.enabled = false;

        if (body != null)
            body.simulated = false;
    }

    void TickPendingMeleeHit()
    {
        if (pendingMeleeHit < 0f)
            return;

        pendingMeleeHit -= Time.deltaTime;
        if (pendingMeleeHit > 0f)
            return;

        pendingMeleeHit = -1f;

        if (visual == null || mateo == null || visual.IsDefeated)
            return;

        if (DistanceToMateo() > MeleeHitRange)
            return;

        var hearts = FindAnyObjectByType<CampusQuestHeartBar>();
        if (hearts != null)
            hearts.TakeDamage(1);
    }
}

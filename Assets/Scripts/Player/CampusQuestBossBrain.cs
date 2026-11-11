using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class CampusQuestBossBrain : MonoBehaviour
{
    const int MaxVida = 10;
    const float MeleeAttackRange = 1.25f;
    const float MeleeHitRange = 1.25f;
    const float MeleeHitDelay = 0.35f;
    const float AttackCooldown = 1.2f;
    const float GrenadeCooldown = 6f;
    const float PhaseTwoHoldDistance = 4.5f;
    const float StopDistance = 0.7f;
    const float SprintDistance = 5f;
    const float WalkSpeed = 2.4f;
    const float SprintSpeed = 4f;

    [SerializeField] CampusQuestBossVisualDriver visual;
    [SerializeField] SpBunkerBossEncounter encounter;
    [SerializeField] Sprite[] explosionFrames;
    [SerializeField] AudioClip grenadeExplosionClip;

    Rigidbody2D body;
    CampusQuestMateoPlayerInput mateo;
    CampusQuestBossIdle bossIdle;
    int vida;
    float attackCooldown;
    float grenadeCooldown;
    float pendingMeleeHit = -1f;
    Vector3 grenadeTarget;
    bool grenadeSpawnedThisThrow;
    bool phaseTwoNotified;
    bool phaseThreeNotified;

    public int HitsTaken => MaxVida - vida;
    public bool IsDefeated => visual != null && visual.IsDefeated;

    public event Action EnteredPhaseTwo;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        if (visual == null)
            visual = GetComponentInChildren<CampusQuestBossVisualDriver>(true);
        bossIdle = GetComponentInChildren<CampusQuestBossIdle>(true);
        vida = MaxVida;
        grenadeCooldown = GrenadeCooldown;
        if (encounter == null)
            encounter = FindAnyObjectByType<SpBunkerBossEncounter>();
    }

    void OnEnable()
    {
        if (bossIdle != null)
            bossIdle.enabled = false;
    }

    void Start()
    {
        mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
    }

    public void RecibirGolpe()
    {
        if (visual == null || visual.IsDefeated)
            return;

        int before = HitsTaken;
        vida = Mathf.Max(0, vida - 1);
        CampusQuestGunshot.PlayDamage(transform.position);

        if (vida <= 0)
        {
            pendingMeleeHit = -1f;
            body.linearVelocity = Vector2.zero;
            visual.PlayDerrotado();
            return;
        }

        if (before < 2 && HitsTaken >= 2)
            NotifyPhaseTwo();

        if (before < 5 && HitsTaken >= 5)
            NotifyPhaseThree();
    }

    void NotifyPhaseTwo()
    {
        if (phaseTwoNotified)
            return;
        phaseTwoNotified = true;
        EnteredPhaseTwo?.Invoke();
        encounter?.SpawnPhaseTwoShooters();
    }

    void NotifyPhaseThree()
    {
        if (phaseThreeNotified)
            return;
        phaseThreeNotified = true;
        encounter?.SpawnPhaseThreeMinions();
    }

    int CurrentPhase()
    {
        int hits = HitsTaken;
        if (hits < 2) return 1;
        if (hits < 5) return 2;
        return 3;
    }

    void Update()
    {
        if (visual == null || mateo == null || visual.IsDefeated)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (CampusQuestCombatHold.EnemiesHeld)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        attackCooldown -= Time.deltaTime;
        grenadeCooldown -= Time.deltaTime;
        TickPendingMeleeHit();
        TickGrenadeThrow();

        if (visual.IsThrowingGrenade)
            body.linearVelocity = Vector2.zero;

        if (visual.IsActing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        int phase = CurrentPhase();
        float dist = DistanceToMateo();

        if (phase == 2 || phase == 3)
        {
            if (grenadeCooldown <= 0f)
            {
                var towardMateo = (Vector2)(mateo.transform.position - transform.position);
                visual.LookAt(towardMateo);
                grenadeTarget = mateo.transform.position;
                if (visual.PlayGranada())
                {
                    grenadeSpawnedThisThrow = false;
                    body.linearVelocity = Vector2.zero;
                }

                return;
            }
        }

        if (phase == 2)
            return;

        if (attackCooldown <= 0f && dist < MeleeAttackRange)
        {
            var towardMateo = (Vector2)(mateo.transform.position - transform.position);
            visual.LookAt(towardMateo);
            if (visual.PlayGolpe())
            {
                attackCooldown = AttackCooldown;
                pendingMeleeHit = MeleeHitDelay;
                body.linearVelocity = Vector2.zero;
                CampusQuestGunshot.PlayMelee(transform.position);
            }
        }
    }

    void TickGrenadeThrow()
    {
        if (grenadeSpawnedThisThrow || visual == null)
            return;

        if (!visual.IsGrenadeReleaseFrame())
            return;

        grenadeSpawnedThisThrow = true;
        grenadeCooldown = GrenadeCooldown;
        var origin = visual.MotionRoot.position;
        CampusQuestBossGrenade.Launch(origin, grenadeTarget, explosionFrames, grenadeExplosionClip);
    }

    void FixedUpdate()
    {
        if (visual == null || mateo == null || visual.IsDefeated)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (CampusQuestCombatHold.EnemiesHeld)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (visual.IsActing || visual.IsThrowingGrenade)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        int phase = CurrentPhase();
        float dist = DistanceToMateo();

        if (phase == 2)
        {
            if (dist <= PhaseTwoHoldDistance)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
        }
        else
        {
            if (dist <= StopDistance)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }
        }

        var delta = mateo.transform.position - transform.position;
        if (delta.sqrMagnitude < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (phase == 2)
        {
            float speed = dist > SprintDistance ? SprintSpeed : WalkSpeed;
            body.linearVelocity = ((Vector2)delta).normalized * speed * 0.65f;
            return;
        }

        float moveSpeed = dist > SprintDistance ? SprintSpeed : WalkSpeed;
        body.linearVelocity = ((Vector2)delta).normalized * moveSpeed;
    }

    float DistanceToMateo() => Vector2.Distance(transform.position, mateo.transform.position);

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

        FindAnyObjectByType<CampusQuestHeartBar>()?.TakeDamage(1);
    }
}

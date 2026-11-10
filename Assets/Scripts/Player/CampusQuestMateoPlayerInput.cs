using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class CampusQuestMateoPlayerInput : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] CampusQuestMateoVisualDriver visualDriver;
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 6.5f;
    [SerializeField] float interactRadius = 1.2f;
    const float MeleeDamageRange = 1.5f;
    [SerializeField] CampusQuestInteractPrompt interactPrompt;
    [SerializeField] Sprite bulletSprite;

    static bool gameOverLoaded;

    Rigidbody2D body;
    InputAction moveAction;
    InputAction sprintAction;
    InputAction attackAction;
    InputAction shootAction;
    InputAction interactAction;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (visualDriver == null)
            visualDriver = GetComponentInChildren<CampusQuestMateoVisualDriver>();
        if (interactPrompt == null)
            interactPrompt = FindAnyObjectByType<CampusQuestInteractPrompt>(FindObjectsInactive.Include);
        if (interactPrompt == null)
        {
            var promptGo = new GameObject("CampusQuest_InteractPrompt");
            interactPrompt = promptGo.AddComponent<CampusQuestInteractPrompt>();
        }
    }

    void OnEnable()
    {
        if (inputActions == null)
            return;

        var map = inputActions.FindActionMap("Player", true);
        moveAction = map.FindAction("Move", true);
        sprintAction = map.FindAction("Sprint", true);
        attackAction = map.FindAction("Attack", true);
        shootAction = map.FindAction("Shoot", true);
        interactAction = map.FindAction("Interact", true);

        map.Enable();
        attackAction.performed += OnAttack;
        shootAction.performed += OnShoot;
        interactAction.performed += OnInteract;
    }

    void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= OnAttack;
        if (shootAction != null)
            shootAction.performed -= OnShoot;
        if (interactAction != null)
            interactAction.performed -= OnInteract;

        if (inputActions != null)
        {
            var map = inputActions.FindActionMap("Player", false);
            map?.Disable();
        }
    }

    public static void ResetGameOverFlag() => gameOverLoaded = false;

    public void Defeat()
    {
        if (visualDriver != null)
            visualDriver.PlayDerrotado();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.enabled = false;

        if (gameOverLoaded)
            return;
        gameOverLoaded = true;
        SceneManager.LoadScene("GameOver");
    }

    void Update()
    {
        if (visualDriver != null && visualDriver.IsDefeated)
        {
            if (interactPrompt != null)
                interactPrompt.Hide();
            return;
        }

        var target = FindBestInteractable();
        if (interactPrompt == null)
            return;
        if (target != null)
            interactPrompt.Show(target);
        else
            interactPrompt.Hide();
    }

    void FixedUpdate()
    {
        if (visualDriver != null && visualDriver.IsDefeated)
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
            return;
        }

        if (moveAction == null || body == null)
            return;

        var move = moveAction.ReadValue<Vector2>();
        if (move.sqrMagnitude < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        move.Normalize();
        float speed = sprintAction != null && sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        body.linearVelocity = move * speed;
    }

    void OnAttack(InputAction.CallbackContext _)
    {
        if (visualDriver != null && visualDriver.IsDefeated)
            return;
        if (visualDriver != null && visualDriver.PlayGolpe())
        {
            CampusQuestGunshot.PlayMelee(visualDriver.ShooterRoot.position);
            TryMeleeDamage();
        }
    }

    void OnShoot(InputAction.CallbackContext _)
    {
        if (visualDriver != null && visualDriver.IsDefeated)
            return;
        if (visualDriver == null || !visualDriver.PlayDisparo())
            return;

        var facing = visualDriver.Facing;
        var origin = (Vector2)visualDriver.ShooterRoot.position;
        CampusQuestGunshot.Play(visualDriver.ShooterRoot.position);
        CampusQuestBullet.Spawn(bulletSprite, facing, origin, true, visualDriver.ShooterRoot);
    }

    void TryMeleeDamage()
    {
        var target = FindClosestSecuaz(MeleeDamageRange);
        target?.RecibirGolpe();
    }

    CampusQuestSecuazBrain FindClosestSecuaz(float maxRange)
    {
        CampusQuestSecuazBrain best = null;
        float bestDistance = maxRange;
        var origin = (Vector2)transform.position;

        foreach (var secuaz in FindObjectsByType<CampusQuestSecuazBrain>(FindObjectsSortMode.None))
        {
            if (secuaz == null || !secuaz.isActiveAndEnabled)
                continue;

            var driver = secuaz.GetComponentInChildren<CampusQuestSecuazVisualDriver>(true);
            if (driver != null && driver.IsDefeated)
                continue;

            float distance = Vector2.Distance(origin, secuaz.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = secuaz;
            }
        }

        return best;
    }

    void OnInteract(InputAction.CallbackContext _) => TryInteract();

    void TryInteract()
    {
        if (visualDriver != null && visualDriver.IsDefeated)
            return;
        FindBestInteractable()?.Interact(gameObject);
    }

    CampusQuestInteractable FindBestInteractable()
    {
        CampusQuestInteractable best = null;
        float bestDistance = interactRadius;
        var origin = (Vector2)transform.position;

        foreach (var target in FindObjectsByType<CampusQuestInteractable>(FindObjectsSortMode.None))
        {
            if (target == null || !target.isActiveAndEnabled || !target.CanInteract(gameObject))
                continue;

            float distance = Vector2.Distance(origin, target.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = target;
            }
        }

        return best;
    }

    public void Configure(InputActionAsset asset, CampusQuestMateoVisualDriver driver, CampusQuestInteractPrompt prompt = null)
    {
        inputActions = asset;
        visualDriver = driver;
        if (prompt != null)
            interactPrompt = prompt;
    }
}

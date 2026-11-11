using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.IDGS
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class IDGSPlayerMotor : MonoBehaviour
    {
        const float MoveDeadzone = 0.2f;

        public float walkSpeed = 4;
        public float runSpeed = 6;
        public Vector2Int initialFacing = Vector2Int.down;
        public Vector2Int Cell { get; private set; }
        public Vector2Int Facing { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsAtRest => !IsMoving && (body.position - Center(Cell)).sqrMagnitude < .0001f;
        public float CurrentSpeed { get; private set; }

        [SerializeField] InputActionAsset inputActions;

        private Rigidbody2D body;
        private CampusQuestMateoVisualDriver visual;
        private Vector2Int requestedDirection;
        private Vector2 target;
        private Vector2Int targetCell;
        private ContactFilter2D collisionFilter;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private InputAction moveAction;
        private InputAction sprintAction;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            visual = GetComponentInChildren<CampusQuestMateoVisualDriver>();
            collisionFilter = new ContactFilter2D { useTriggers = false };
            TryBindDefaultInput();
            PlaceAtCell(Vector2Int.FloorToInt(body.position), initialFacing);
            IDGSSceneTravel.ApplyArrival(this);
        }

        public void TryBindDefaultInput()
        {
            if (inputActions != null)
                return;

            foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
            {
                if (asset != null && asset.name == "InputSystem_Actions")
                {
                    inputActions = asset;
                    break;
                }
            }
        }

        void OnEnable()
        {
            if (inputActions == null)
                return;

            CampusQuestActiveControls.Bind(inputActions);
            var playerMap = inputActions.FindActionMap("Player", throwIfNotFound: false);
            if (playerMap == null)
                return;

            playerMap.Enable();
            moveAction = playerMap.FindAction("Move", throwIfNotFound: false);
            sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: false);
            if (moveAction != null)
                moveAction.Enable();
            if (sprintAction != null)
                sprintAction.Enable();
        }

        public static Vector2 Center(Vector2Int cell) => (Vector2)cell + Vector2.one * .5f;

        public void PlaceAtCell(Vector2Int cell, Vector2Int facing)
        {
            Cell = cell;
            targetCell = cell;
            target = Center(cell);
            body.position = target;
            transform.position = target;
            body.linearVelocity = Vector2.zero;
            requestedDirection = Vector2Int.zero;
            IsMoving = false;
            Facing = facing;
            if (visual != null) visual.Face(facing);
            Physics2D.SyncTransforms();
        }

        private void Update()
        {
            requestedDirection = Vector2Int.zero;
            if (IDGSSceneTravel.IsChanging || CampusQuest.Dialogue.DialogueSession.IsAnyOpen)
                return;

            requestedDirection = ReadMoveDirection();
            CurrentSpeed = ReadSprint() ? runSpeed : walkSpeed;
            if (!IsMoving && requestedDirection != Vector2Int.zero)
            {
                Facing = requestedDirection;
                if (visual != null) visual.Face(Facing);
            }
        }

        Vector2Int ReadMoveDirection()
        {
            if (moveAction != null)
            {
                var fromStick = DirectionFromVector(moveAction.ReadValue<Vector2>());
                if (fromStick != Vector2Int.zero)
                    return fromStick;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2Int.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                return Vector2Int.up;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                return Vector2Int.down;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                return Vector2Int.left;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                return Vector2Int.right;
            return Vector2Int.zero;
        }

        bool ReadSprint()
        {
            if (sprintAction != null && sprintAction.IsPressed())
                return true;

            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        static Vector2Int DirectionFromVector(Vector2 move)
        {
            if (move.sqrMagnitude < MoveDeadzone * MoveDeadzone)
                return Vector2Int.zero;

            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                return move.x > 0f ? Vector2Int.right : Vector2Int.left;

            return move.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        private void FixedUpdate()
        {
            if (IDGSSceneTravel.IsChanging) { body.linearVelocity = Vector2.zero; return; }
            if (!IsMoving)
            {
                body.linearVelocity = Vector2.zero;
                if (requestedDirection == Vector2Int.zero || !CanStep(requestedDirection)) return;
                targetCell = Cell + requestedDirection;
                target = Center(targetCell);
                IsMoving = true;
            }

            Vector2 next = Vector2.MoveTowards(body.position, target, CurrentSpeed * Time.fixedDeltaTime);
            body.MovePosition(next);
            if ((next - target).sqrMagnitude < .000001f)
            {
                Cell = targetCell;
                IsMoving = false;
            }
        }

        public bool CanStep(Vector2Int direction)
        {
            return body.Cast(direction, collisionFilter, hits, 1.01f) == 0;
        }

        private void OnDisable()
        {
            requestedDirection = Vector2Int.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }
}

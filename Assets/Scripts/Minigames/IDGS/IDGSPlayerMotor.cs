using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.IDGS
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class IDGSPlayerMotor : MonoBehaviour
    {
        public float walkSpeed = 4;
        public float runSpeed = 6;
        public Vector2Int initialFacing = Vector2Int.down;
        public Vector2Int Cell { get; private set; }
        public Vector2Int Facing { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsAtRest => !IsMoving && (body.position - Center(Cell)).sqrMagnitude < .0001f;
        public float CurrentSpeed { get; private set; }

        private Rigidbody2D body;
        private CampusQuestMateoVisualDriver visual;
        private Vector2Int requestedDirection;
        private Vector2 target;
        private Vector2Int targetCell;
        private ContactFilter2D collisionFilter;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            visual = GetComponentInChildren<CampusQuestMateoVisualDriver>();
            collisionFilter = new ContactFilter2D { useTriggers = false };
            PlaceAtCell(Vector2Int.FloorToInt(body.position), initialFacing);
            IDGSSceneTravel.ApplyArrival(this);
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
            var keyboard = Keyboard.current;
            if (keyboard == null || IDGSSceneTravel.IsChanging || CampusQuest.Dialogue.DialogueSession.IsAnyOpen) return;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) requestedDirection = Vector2Int.up;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) requestedDirection = Vector2Int.down;
            else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) requestedDirection = Vector2Int.left;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) requestedDirection = Vector2Int.right;

            CurrentSpeed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? runSpeed : walkSpeed;
            if (!IsMoving && requestedDirection != Vector2Int.zero)
            {
                Facing = requestedDirection;
                if (visual != null) visual.Face(Facing);
            }
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

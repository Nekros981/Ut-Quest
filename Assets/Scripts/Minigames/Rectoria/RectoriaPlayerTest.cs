using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.Rectoria
{
    public sealed class RectoriaPlayerTest : MonoBehaviour
    {
        const float MoveDeadzone = 0.2f;

        public RectoriaGridMap map;
        public float walkSecondsPerCell = 0.25f;
        public float runSecondsPerCell = 1f / 6f;
        public Vector2Int facing = Vector2Int.up;

        [SerializeField] InputActionAsset inputActions;

        bool moving;
        CampusQuestMateoVisualDriver visual;
        InputAction moveAction;
        InputAction sprintAction;

        void Awake()
        {
            visual = GetComponentInChildren<CampusQuestMateoVisualDriver>(true);
            if (visual != null)
                visual.Face(facing);

            if (inputActions == null)
            {
                var interactor = FindAnyObjectByType<RectoriaDialogueInteractor>();
                if (interactor != null)
                    inputActions = interactor.inputActions;
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

        void Update()
        {
            if (moving || map == null)
                return;

            var direction = ReadMoveDirection();
            if (direction == Vector2Int.zero)
                return;

            facing = direction;
            if (visual != null)
                visual.Face(direction);

            var current = map.grid.WorldToCell(transform.position);
            var target = current + new Vector3Int(direction.x, direction.y, 0);
            if (!map.CanEnter(target))
                return;

            var sprint = sprintAction != null && sprintAction.IsPressed();
            var seconds = sprint ? runSecondsPerCell : walkSecondsPerCell;
            StartCoroutine(Travel(map.CellCenter(current), map.CellCenter(target), Mathf.Max(0.02f, seconds)));
        }

        Vector2Int ReadMoveDirection()
        {
            if (moveAction != null)
            {
                var stick = moveAction.ReadValue<Vector2>();
                var fromStick = DirectionFromVector(stick);
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

        static Vector2Int DirectionFromVector(Vector2 move)
        {
            if (move.sqrMagnitude < MoveDeadzone * MoveDeadzone)
                return Vector2Int.zero;

            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                return move.x > 0f ? Vector2Int.right : Vector2Int.left;

            return move.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        IEnumerator Travel(Vector3 from, Vector3 to, float seconds)
        {
            moving = true;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            transform.position = to;
            moving = false;
        }
    }
}

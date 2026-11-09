using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.Rectoria
{
    // Keyboard-only test controller, not the final Mateo animator or mobile input.
    public sealed class RectoriaPlayerTest : MonoBehaviour
    {
        public RectoriaGridMap map;
        public float walkSecondsPerCell = 0.25f;
        public float runSecondsPerCell = 1f / 6f;
        public Vector2Int facing = Vector2Int.up;
        private bool moving;
        private CampusQuestMateoVisualDriver visual;

        private void Awake()
        {
            visual = GetComponentInChildren<CampusQuestMateoVisualDriver>(true);
            if (visual != null) visual.Face(facing);
        }

        private void Update()
        {
            if (moving || map == null || Keyboard.current == null) return;
            var k = Keyboard.current;
            Vector2Int direction = Vector2Int.zero;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) direction = Vector2Int.up;
            else if (k.sKey.isPressed || k.downArrowKey.isPressed) direction = Vector2Int.down;
            else if (k.aKey.isPressed || k.leftArrowKey.isPressed) direction = Vector2Int.left;
            else if (k.dKey.isPressed || k.rightArrowKey.isPressed) direction = Vector2Int.right;
            if (direction == Vector2Int.zero) return;
            facing = direction;
            if (visual != null) visual.Face(direction);
            Vector3Int current = map.grid.WorldToCell(transform.position);
            Vector3Int target = current + new Vector3Int(direction.x, direction.y, 0);
            if (!map.CanEnter(target)) return;
            float seconds = (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) ? runSecondsPerCell : walkSecondsPerCell;
            StartCoroutine(Travel(map.CellCenter(current), map.CellCenter(target), Mathf.Max(0.02f, seconds)));
        }

        private IEnumerator Travel(Vector3 from, Vector3 to, float seconds)
        {
            moving = true;
            float elapsed = 0;
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

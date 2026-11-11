using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusQuest.IDGS
{
    /// <summary>Independent, physical movement harness for this vegetation test scene only.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class IDGSVegetationTestPlayer : MonoBehaviour
    {
        public float walkSpeed = 4;
        public float runSpeed = 6;
        private Rigidbody2D body;
        private CampusQuestMateoVisualDriver visual;
        private Vector2 spawn;
        public Vector2 RequestedVelocity { get; private set; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); visual = GetComponentInChildren<CampusQuestMateoVisualDriver>();
            spawn = body.position;
        }
        private void Update()
        {
            var k = Keyboard.current; Vector2 direction = Vector2.zero;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) direction.y = 1;
                else if (k.sKey.isPressed || k.downArrowKey.isPressed) direction.y = -1;
                else if (k.aKey.isPressed || k.leftArrowKey.isPressed) direction.x = -1;
                else if (k.dKey.isPressed || k.rightArrowKey.isPressed) direction.x = 1;
                if (k.rKey.wasPressedThisFrame) { body.position = spawn; body.linearVelocity = Vector2.zero; }
            }
            if (direction != Vector2.zero && visual != null) visual.Face(Vector2Int.RoundToInt(direction));
            RequestedVelocity = direction * (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) ? runSpeed : walkSpeed);
        }
        private void FixedUpdate() { body.linearVelocity = RequestedVelocity; }
        private void OnDisable() { if (body != null) body.linearVelocity = Vector2.zero; }
    }
}

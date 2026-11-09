using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class SpTopDownTestMover : MonoBehaviour
{
    public float speed = 4f;

    Rigidbody2D body;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        var move = new Vector2(x, y);
        if (move.sqrMagnitude < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        move.Normalize();
        body.linearVelocity = move * speed;
    }
}

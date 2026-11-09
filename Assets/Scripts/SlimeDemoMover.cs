using UnityEngine;
using UnityEngine.InputSystem;

// Prototipe gerak top-down untuk demo scene (WASD / panah). Bukan controller final.
// Gerak lewat Rigidbody2D supaya ketahan collider dinding.
[RequireComponent(typeof(SlimeSpriteAnimator), typeof(Rigidbody2D))]
public class SlimeDemoMover : MonoBehaviour
{
    public float speed = 2.5f;
    SlimeSpriteAnimator anim;
    Rigidbody2D rb;
    Vector2 input;

    void Awake()
    {
        anim = GetComponent<SlimeSpriteAnimator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        var kb = Keyboard.current;
        input = Vector2.zero;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
        }

        if (input == Vector2.zero)
        {
            anim.Play("idle", anim.dir, anim.faceLeft);
            return;
        }
        input.Normalize();
        string dir = Mathf.Abs(input.x) > Mathf.Abs(input.y) ? "side" : (input.y > 0 ? "back" : "front");
        bool left = dir == "side" ? input.x < 0 : anim.faceLeft;
        anim.Play("move", dir, left);
    }

    void FixedUpdate() => rb.linearVelocity = input * speed;
}

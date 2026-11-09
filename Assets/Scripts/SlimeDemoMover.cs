using UnityEngine;
using UnityEngine.InputSystem;

// Prototipe gerak top-down untuk demo scene (WASD / panah). Bukan controller final.
[RequireComponent(typeof(SlimeSpriteAnimator))]
public class SlimeDemoMover : MonoBehaviour
{
    public float speed = 2.5f;
    SlimeSpriteAnimator anim;

    void Awake() => anim = GetComponent<SlimeSpriteAnimator>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2 v = Vector2.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;

        if (v == Vector2.zero)
        {
            anim.Play("idle", anim.dir, anim.faceLeft);
            return;
        }

        v.Normalize();
        transform.position += (Vector3)(v * speed * Time.deltaTime);

        string dir = Mathf.Abs(v.x) > Mathf.Abs(v.y) ? "side" : (v.y > 0 ? "back" : "front");
        bool left = dir == "side" ? v.x < 0 : anim.faceLeft;
        anim.Play("move", dir, left);
    }
}

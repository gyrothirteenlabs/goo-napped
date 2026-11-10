using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // Controller slime pemain: gerak top-down + makan bahan. Input dari Input Actions (map "Player").
    // Kecepatan mengikuti state tubuh (habis makan batu jadi lambat).
    // Belah/gabung/Shift menyusul di tahap 2.
    [RequireComponent(typeof(Rigidbody2D), typeof(SlimeSpriteAnimator), typeof(SlimeBody))]
    public class SlimeController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";
        [SerializeField] private string moveAction = "Move";
        [SerializeField] private string eatAction = "Interact";

        [Header("Gerak")]
        [SerializeField] private float baseSpeed = 2.5f;

        [Header("Makan")]
        [Tooltip("Jarak maksimum ke bahan (dari pusat collider).")]
        [SerializeField] private float eatRadius = 0.7f;
        [Tooltip("Lama animasi makan. Selama itu slime diam (risiko ketahuan).")]
        [SerializeField] private float eatDuration = 0.6f;
        [SerializeField] private LayerMask pickupMask = ~0;

        private Rigidbody2D rb;
        private SlimeSpriteAnimator anim;
        private SlimeBody body;
        private Collider2D col;
        private InputActionMap map;
        private InputAction move, eat;
        private Vector2 input;
        private float eatTimer;
        private readonly Collider2D[] hits = new Collider2D[16];

        public bool IsEating => eatTimer > 0f;
        public float CurrentSpeed => baseSpeed * body.State.SpeedMultiplier;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<SlimeSpriteAnimator>();
            body = GetComponent<SlimeBody>();
            col = GetComponent<Collider2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("SlimeController: Input Actions belum diisi (Assets/Settings/InputSystem_Actions).", this);
                enabled = false;
                return;
            }
            map = actions.FindActionMap(actionMap, true);
            move = map.FindAction(moveAction, true);
            eat = map.FindAction(eatAction, true);
            map.Enable();
        }

        private void OnDisable()
        {
            input = Vector2.zero;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            if (eatTimer > 0f)
            {
                eatTimer -= Time.deltaTime;
                input = Vector2.zero;
                anim.Play("eat", anim.dir, anim.faceLeft);
                return;
            }

            if (eat.WasPerformedThisFrame() && TryEat()) return;

            input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            if (input.sqrMagnitude < 0.01f)
            {
                input = Vector2.zero;
                anim.Play("idle", anim.dir, anim.faceLeft);
                return;
            }
            string dir = Mathf.Abs(input.x) > Mathf.Abs(input.y) ? "side" : (input.y > 0 ? "back" : "front");
            bool left = dir == "side" ? input.x < 0 : anim.faceLeft;
            anim.Play("move", dir, left);
        }

        private void FixedUpdate() => rb.linearVelocity = input * CurrentSpeed;

        private Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;

        // Bahan terdekat dalam jangkauan, atau null.
        public IngredientPickup FindPickup()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = pickupMask };
            int n = Physics2D.OverlapCircle(Center, eatRadius, filter, hits);
            IngredientPickup best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var p = hits[i].GetComponentInParent<IngredientPickup>();
                if (p == null || p.Ingredient == null) continue;
                float d = ((Vector2)hits[i].bounds.center - Center).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        public bool TryEat()
        {
            var p = FindPickup();
            if (p == null) return false;
            body.Eat(p.Consume());
            eatTimer = eatDuration;
            input = Vector2.zero;
            anim.Play("eat", anim.dir, anim.faceLeft);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var c = GetComponent<Collider2D>();
            Gizmos.DrawWireSphere(c != null ? c.bounds.center : transform.position, eatRadius);
        }
    }
}

using UnityEngine;

namespace GooNapped
{
    // Satu potongan slime: badan yang bisa digerakkan. TIDAK membaca input.
    // Input ada di PlayerBrain, yang pindah antar potongan (tombol Shift).
    // Potongan yang tidak dikendalikan otomatis nyamar dan diam di tempat.
    [RequireComponent(typeof(SlimeMotor), typeof(SlimeBody), typeof(SlimeSpriteAnimator))]
    public class SlimePiece : MonoBehaviour
    {
        [Header("Makan")]
        [Tooltip("Jarak maksimum ke bahan (dari pusat collider).")]
        [SerializeField] private float eatRadius = 0.7f;
        [Tooltip("Lama animasi makan. Selama itu potongan diam (risiko ketahuan).")]
        [SerializeField] private float eatDuration = 0.6f;
        [SerializeField] private LayerMask pickupMask = ~0;

        [Header("Animasi")]
        [Tooltip("Tempo animasi jalan mengikuti kecepatan: batas bawah dan atas pengali fps.")]
        [SerializeField] private Vector2 moveAnimSpeed = new Vector2(0.6f, 1.4f);

        private SlimeSpriteAnimator anim;
        private Collider2D col;
        private float eatTimer;
        private readonly Collider2D[] hits = new Collider2D[16];

        public SlimeMotor Motor { get; private set; }
        public SlimeBody Body { get; private set; }
        // Arah hadap (4 arah, untuk sprite dan arah belah).
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        public bool IsControlled { get; private set; }
        public bool IsDisguised => !IsControlled;
        public bool IsEating => eatTimer > 0f;
        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        public float Radius => col != null ? col.bounds.extents.x : 0.28f;

        private void Awake()
        {
            Motor = GetComponent<SlimeMotor>();
            Body = GetComponent<SlimeBody>();
            anim = GetComponent<SlimeSpriteAnimator>();
            col = GetComponent<Collider2D>();
            Body.Changed += OnBodyChanged;
        }

        private void OnDestroy()
        {
            if (Body != null) Body.Changed -= OnBodyChanged;
        }

        private void OnBodyChanged(SlimeBody b) => Motor.SpeedMultiplier = b.State.SpeedMultiplier;

        public void SetControlled(bool on)
        {
            IsControlled = on;
            Motor.SetFrozen(!on);
            // Placeholder nyamar: wajah disembunyikan. Sprite "jadi barang" menyusul dari tim desainer.
            if (anim != null && anim.faceRenderer != null) anim.faceRenderer.enabled = on;
        }

        public void Face(Vector2Int dir)
        {
            if (dir != Vector2Int.zero) Facing = dir;
        }

        // Dipanggil PlayerBrain tiap frame untuk potongan yang dikendalikan.
        public void Move(Vector2 input)
        {
            if (!IsControlled || IsEating) { Motor.MoveInput = Vector2.zero; return; }
            Motor.MoveInput = input;
            if (input.sqrMagnitude < 0.04f) return;
            Face(Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0 ? 1 : -1));
        }

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
            if (IsEating) return false;
            var p = FindPickup();
            if (p == null) return false;
            Body.Eat(p.Consume());
            eatTimer = eatDuration;
            Motor.Stop();
            return true;
        }

        private void Update()
        {
            if (eatTimer > 0f) eatTimer -= Time.deltaTime;
            string dir = Facing.x != 0 ? "side" : (Facing.y > 0 ? "back" : "front");
            bool left = Facing.x != 0 ? Facing.x < 0 : anim.faceLeft;
            bool moving = IsControlled && !IsEating && Motor.MoveInput.sqrMagnitude > 0.04f;
            // tempo animasi jalan ikut kecepatan (lambat habis makan bahan berat = animasi ikut lambat)
            anim.speedScale = moving
                ? Mathf.Clamp(Motor.MaxSpeed > 0f ? Motor.SpeedMultiplier : 1f, moveAnimSpeed.x, moveAnimSpeed.y)
                : 1f;
            anim.Play(IsDisguised ? "idle" : IsEating ? "eat" : moving ? "move" : "idle", dir, left);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var c = GetComponent<Collider2D>();
            Gizmos.DrawWireSphere(c != null ? c.bounds.center : transform.position, eatRadius);
        }
    }
}

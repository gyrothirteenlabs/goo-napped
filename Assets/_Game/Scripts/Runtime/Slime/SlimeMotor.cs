using UnityEngine;

namespace GooNapped
{
    // Gerak analog (bebas arah) dengan rasa slime: lengket dan lamban.
    //   - Kecepatan naik/turun bertahap (akselerasi), tidak instan.
    //   - "Denyut": kecepatan mengikuti fase animasi jalan, jadi badan menarik diri lalu meluncur,
    //     bukan meluncur rata seperti es. Kurva denyut bisa diatur animator tim di Inspector.
    [RequireComponent(typeof(Rigidbody2D))]
    public class SlimeMotor : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 2.2f;
        [Tooltip("Seberapa cepat mencapai kecepatan tujuan (unit/detik^2). Kecil = licin, besar = kaku.")]
        [SerializeField] private float acceleration = 14f;
        [Tooltip("Pengali kecepatan sepanjang satu siklus animasi jalan (x: 0-1 fase, y: pengali). Rata-rata sekitar 1.")]
        [SerializeField] private AnimationCurve pulse = new AnimationCurve(
            new Keyframe(0f, 0.35f), new Keyframe(0.3f, 1.7f), new Keyframe(0.65f, 1.0f), new Keyframe(1f, 0.35f));
        [Tooltip("0 = tanpa denyut (kecepatan rata), 1 = denyut penuh.")]
        [SerializeField, Range(0f, 1f)] private float pulseStrength = 1f;

        private Rigidbody2D rb;
        private SlimeSpriteAnimator anim;

        // Arah yang diminta (panjang 0..1). Diisi SlimePiece, bukan langsung dari input.
        public Vector2 MoveInput { get; set; }
        // 1 = normal, 0.6 = lambat (habis makan bahan berat).
        public float SpeedMultiplier { get; set; } = 1f;
        public float MaxSpeed => baseSpeed * SpeedMultiplier;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public bool IsMoving => Velocity.sqrMagnitude > 0.01f;
        // 0..1 terhadap kecepatan normal, untuk menyamakan tempo animasi dengan gerak.
        public float SpeedRatio => baseSpeed > 0f ? Velocity.magnitude / baseSpeed : 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<SlimeSpriteAnimator>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Potongan yang ditinggal (nyamar) tidak boleh terdorong.
        public void SetFrozen(bool frozen)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = frozen ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            if (frozen) MoveInput = Vector2.zero;
        }

        public void Stop()
        {
            MoveInput = Vector2.zero;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;
            var input = Vector2.ClampMagnitude(MoveInput, 1f);
            float p = 1f;
            if (anim != null && input.sqrMagnitude > 0.01f)
                p = Mathf.Lerp(1f, Mathf.Max(0f, pulse.Evaluate(anim.Phase)), pulseStrength);
            var target = input * (MaxSpeed * p);
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }
    }
}

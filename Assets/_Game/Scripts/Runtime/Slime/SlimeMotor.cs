using UnityEngine;

namespace GooNapped
{
    // Gerak slime = lompatan rendah berulang. Rasa slime Terraria, tapi pendek karena ini game stealth:
    //   Ancang (memipih) -> Lompat (memanjang, busur rendah ±5 px, bulat di puncak) -> Mendarat (gepeng + goyang jeli)
    // Tahan arah = lompat berulang. Kecepatan naik-turun HALUS (tidak mati total di antara lompatan, itu yang janky).
    // Lepas arah = lompatan yang sedang di udara diselesaikan, lalu meluncur pelan sampai berhenti (Slide).
    // Di tanah arah bebas, di udara hanya bisa dibelokkan sedikit (terasa lengket).
    // Massa/bahan berat (SpeedMultiplier < 1): lompatan lebih pendek, lebih rendah, ancang lebih lama.
    [RequireComponent(typeof(Rigidbody2D))]
    public class SlimeMotor : MonoBehaviour
    {
        public enum HopPhase { Idle, Windup, Air, Land, Slide }

        [Header("Kecepatan")]
        [Tooltip("Kecepatan dasar (massa 1, tanpa bahan berat). Rata-rata nyata ±86% dari ini karena kecepatan " +
                 "menyusul target secara halus: 2.55 = ±2.2 unit/detik (diukur 11 Nov).")]
        [SerializeField] private float baseSpeed = 2.55f;
        [Tooltip("Bagian kecepatan udara yang tersisa saat menyentuh tanah di antara lompatan. 0 = berhenti total tiap lompat.")]
        [SerializeField, Range(0f, 1f)] private float groundSpeedShare = 0.35f;
        [Tooltip("Waktu (detik) kecepatan menyusul target. Kecil = kaku, besar = licin.")]
        [SerializeField, Range(0.01f, 0.3f)] private float velocitySmoothing = 0.05f;

        [Header("Waktu fase (detik, kecepatan normal)")]
        [SerializeField] private float crouchTime = 0.07f;
        [SerializeField] private float hopTime = 0.26f;
        [SerializeField] private float settleTime = 0.07f;
        [Tooltip("Lama meluncur sampai berhenti setelah tombol arah dilepas.")]
        [SerializeField] private float stopTime = 0.14f;

        [Header("Rasa")]
        [Tooltip("Tinggi busur lompatan (unit). 0.1 = sekitar 5 px. Rendah karena stealth.")]
        [SerializeField] private float hopArc = 0.1f;
        [Tooltip("Derajat/detik belokan di udara. Kecil = lengket, besar = lincah.")]
        [SerializeField] private float steerInAir = 260f;
        [SerializeField] private float crouchSquash = -0.18f;
        [SerializeField] private float takeoffStretch = 0.16f;
        [SerializeField] private float fallStretch = 0.05f;
        [Tooltip("Dorongan pegas jeli saat lepas landas (positif = melonjak).")]
        [SerializeField] private float takeoffKick = 2.5f;
        [Tooltip("Hentakan pegas jeli saat mendarat (negatif = gepeng).")]
        [SerializeField] private float landImpact = -4.5f;
        [Tooltip("Napas saat diam: squash/stretch kecil berulang.")]
        [SerializeField] private float breathe = 0.025f;
        [SerializeField] private float breatheRate = 1.3f;

        // Fase baru dimulai (untuk suara langkah, debu, dll.).
        public event System.Action<HopPhase> PhaseEntered;

        private Rigidbody2D rb;
        private SlimeJelly jelly;
        private HopPhase phase = HopPhase.Idle;
        private float t;
        private Vector2 hopDir = Vector2.down;
        private Vector2 velocity;
        private float breatheOffset;

        // Arah yang diminta (panjang 0..1). Diisi SlimePiece, bukan langsung dari input.
        public Vector2 MoveInput { get; set; }
        // 1 = normal. Di bawah 1 = lambat (massa besar, bahan berat): lompatan lebih pendek dan lebih lama ancangnya.
        public float SpeedMultiplier { get; set; } = 1f;
        public float MaxSpeed => baseSpeed * SpeedMultiplier;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public HopPhase Phase => phase;
        public bool IsHopping => phase != HopPhase.Idle;
        public bool IsAirborne => phase == HopPhase.Air;
        // 0..1 sepanjang satu siklus lompat; dipakai untuk memilih frame sprite "move" supaya sinkron.
        public float CyclePhase { get; private set; }

        private float Mult => Mathf.Max(0.2f, SpeedMultiplier);
        private float Crouch => crouchTime / Mult;
        private float Settle => settleTime / Mult;
        private float Cycle => Crouch + hopTime + Settle;
        // kecepatan di udara supaya rata-rata satu siklus = baseSpeed * Mult
        private float AirSpeed => baseSpeed * Mult * Cycle / (hopTime + groundSpeedShare * (Crouch + Settle));
        private float GroundSpeed => AirSpeed * groundSpeedShare;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            jelly = GetComponent<SlimeJelly>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            breatheOffset = Random.value * 10f;   // potongan-potongan tidak bernapas serempak
        }

        // Potongan yang ditinggal tidak boleh terdorong.
        public void SetFrozen(bool frozen)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            velocity = Vector2.zero;
            rb.bodyType = frozen ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            if (frozen) Stop();
        }

        public void Stop()
        {
            MoveInput = Vector2.zero;
            phase = HopPhase.Idle;
            t = 0f;
            CyclePhase = 0f;
            velocity = Vector2.zero;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = Vector2.zero;
            if (jelly != null) { jelly.Target = 0f; jelly.Lift = 0f; }
        }

        private bool HasInput => MoveInput.sqrMagnitude > 0.04f;

        private void Enter(HopPhase p)
        {
            phase = p;
            t = 0f;
            if (jelly != null)
            {
                if (p == HopPhase.Air) jelly.Kick(takeoffKick);
                if (p == HopPhase.Land) jelly.Kick(landImpact * Mathf.Lerp(0.6f, 1f, Mult));
            }
            PhaseEntered?.Invoke(p);
        }

        private void Update()
        {
            bool dynamic = rb.bodyType == RigidbodyType2D.Dynamic;
            if (!dynamic) { Breathe(); return; }   // potongan yang ditinggal: diam, tetap bernapas
            t += Time.deltaTime;
            switch (phase)
            {
                case HopPhase.Idle:
                    if (HasInput) { hopDir = MoveInput.normalized; Enter(HopPhase.Windup); }
                    break;
                case HopPhase.Windup:
                    if (!HasInput) { Enter(HopPhase.Slide); break; }   // batal sebelum lepas landas (presisi stealth)
                    hopDir = MoveInput.normalized;                     // di tanah: arah bebas
                    if (t >= Crouch) Enter(HopPhase.Air);
                    break;
                case HopPhase.Air:
                    if (HasInput)
                    {
                        float cur = Mathf.Atan2(hopDir.y, hopDir.x) * Mathf.Rad2Deg;
                        float want = Mathf.Atan2(MoveInput.y, MoveInput.x) * Mathf.Rad2Deg;
                        float a = Mathf.MoveTowardsAngle(cur, want, steerInAir * Time.deltaTime) * Mathf.Deg2Rad;
                        hopDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    }
                    if (t >= hopTime) Enter(HopPhase.Land);
                    break;
                case HopPhase.Land:
                    if (t >= Settle) Enter(HasInput ? HopPhase.Windup : HopPhase.Slide);
                    break;
                case HopPhase.Slide:
                    if (HasInput) { hopDir = MoveInput.normalized; Enter(HopPhase.Windup); }
                    else if (t >= stopTime) Enter(HopPhase.Idle);
                    break;
            }
            UpdateVisual();
        }

        private void Breathe()
        {
            if (jelly == null) return;
            jelly.Lift = 0f;
            jelly.Target = breathe * Mathf.Sin((Time.time + breatheOffset) * breatheRate * Mathf.PI * 2f);
        }

        private void UpdateVisual()
        {
            float c = Crouch, h = hopTime, s = Settle, total = Cycle;
            float k;
            switch (phase)
            {
                case HopPhase.Windup: k = Mathf.Clamp01(t / c); CyclePhase = k * c / total; break;
                case HopPhase.Air: k = Mathf.Clamp01(t / h); CyclePhase = (c + k * h) / total; break;
                case HopPhase.Land: k = Mathf.Clamp01(t / s); CyclePhase = (c + h + k * s) / total; break;
                default: k = 0f; CyclePhase = 0f; break;
            }
            if (jelly == null) return;
            switch (phase)
            {
                case HopPhase.Windup:
                    jelly.Target = crouchSquash * (1f - (1f - k) * (1f - k));   // memipih cepat lalu tertahan
                    jelly.Lift = 0f;
                    break;
                case HopPhase.Air:
                    // busur parabola rendah; memanjang saat naik, bulat di puncak, sedikit memanjang lagi saat turun
                    jelly.Lift = hopArc * Mathf.Sqrt(Mult) * 4f * k * (1f - k);
                    jelly.Target = k < 0.5f ? Mathf.Lerp(takeoffStretch, 0f, k * 2f) : Mathf.Lerp(0f, fallStretch, (k - 0.5f) * 2f);
                    break;
                case HopPhase.Land:
                    jelly.Target = crouchSquash * 0.6f;
                    jelly.Lift = 0f;
                    break;
                case HopPhase.Slide:
                    jelly.Target = 0f;
                    jelly.Lift = 0f;
                    break;
                default:
                    Breathe();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;
            float target;
            switch (phase)
            {
                case HopPhase.Air: target = AirSpeed; break;
                case HopPhase.Windup:
                case HopPhase.Land: target = GroundSpeed; break;
                default: target = 0f; break;
            }
            // kecepatan menyusul target secara eksponensial: halus, tanpa sentakan
            float blend = 1f - Mathf.Exp(-Time.fixedDeltaTime / velocitySmoothing);
            velocity = Vector2.Lerp(velocity, hopDir * target, blend);
            rb.linearVelocity = velocity;
        }
    }
}

using UnityEngine;

namespace GooNapped
{
    // Gerak slime = rangkaian lompatan kecil (hop), bukan meluncur rata:
    //   Ancang (memipih, diam) -> Lompat (memanjang, terangkat, cepat) -> Mendarat (gepeng) -> Meluncur (melambat)
    // Tahan arah = lompat berulang. Lepas = lompatan yang sedang berjalan diselesaikan lalu meluncur berhenti.
    // Arah dikunci saat lepas landas (terasa berat/lengket), dengan sedikit belokan di udara.
    [RequireComponent(typeof(Rigidbody2D))]
    public class SlimeMotor : MonoBehaviour
    {
        public enum HopPhase { Idle, Windup, Air, Land, Slide }

        [Header("Kecepatan")]
        [Tooltip("Kecepatan rata-rata (unit/detik) saat terus melompat.")]
        [SerializeField] private float baseSpeed = 2.2f;

        [Header("Waktu tiap fase (detik, kecepatan normal)")]
        [SerializeField] private float windupTime = 0.07f;
        [SerializeField] private float airTime = 0.2f;
        [SerializeField] private float landTime = 0.05f;
        [SerializeField] private float slideTime = 0.12f;
        [Tooltip("Bagian jarak tiap siklus yang ditempuh saat meluncur (sisanya di udara).")]
        [SerializeField, Range(0f, 0.6f)] private float slideShare = 0.22f;

        [Header("Rasa")]
        [Tooltip("Derajat/detik belokan saat di udara. Kecil = arah terkunci (lengket), besar = lincah.")]
        [SerializeField] private float airSteer = 160f;
        [Tooltip("Tinggi lompatan visual (unit). 0.12 = sekitar 6 px.")]
        [SerializeField] private float hopHeight = 0.12f;
        [SerializeField] private float windupSquash = -0.22f;
        [SerializeField] private float airStretch = 0.18f;
        [SerializeField] private float landKick = -7f;

        private Rigidbody2D rb;
        private SlimeJelly jelly;
        private HopPhase phase = HopPhase.Idle;
        private float t;
        private Vector2 hopDir = Vector2.down;
        private float slideStart;

        // Arah yang diminta (panjang 0..1). Diisi SlimePiece, bukan langsung dari input.
        public Vector2 MoveInput { get; set; }
        // 1 = normal, 0.6 = lambat (habis makan bahan berat): lompatan lebih pendek dan lebih lama ancangnya.
        public float SpeedMultiplier { get; set; } = 1f;
        public float MaxSpeed => baseSpeed * SpeedMultiplier;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public HopPhase Phase => phase;
        public bool IsHopping => phase != HopPhase.Idle;
        // 0..1 sepanjang satu siklus lompat; dipakai untuk memilih frame sprite "move" supaya sinkron.
        public float CyclePhase { get; private set; }

        private float Mult => Mathf.Max(0.2f, SpeedMultiplier);
        private float Windup => windupTime / Mult;
        private float Land => landTime / Mult;
        private float CycleTime => Windup + airTime + Land + slideTime;
        private float CycleDistance => baseSpeed * Mult * (windupTime + airTime + landTime + slideTime);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            jelly = GetComponent<SlimeJelly>();
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
            if (frozen) Stop();
        }

        public void Stop()
        {
            MoveInput = Vector2.zero;
            phase = HopPhase.Idle;
            CyclePhase = 0f;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = Vector2.zero;
            if (jelly != null) { jelly.Target = 0f; jelly.Lift = 0f; }
        }

        private bool HasInput => MoveInput.sqrMagnitude > 0.04f;

        private void Enter(HopPhase p)
        {
            phase = p;
            t = 0f;
            if (p == HopPhase.Land && jelly != null) jelly.Kick(landKick);
            if (p == HopPhase.Slide) slideStart = SlideSpeed();
        }

        private float AirSpeed() => CycleDistance * (1f - slideShare) / airTime;
        // kecepatan awal luncuran supaya rata-rata luncuran (v0/2) menempuh bagian slideShare
        private float SlideSpeed() => 2f * CycleDistance * slideShare / slideTime;

        private void Update()
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;
            t += Time.deltaTime;
            switch (phase)
            {
                case HopPhase.Idle:
                    if (HasInput) { hopDir = MoveInput.normalized; Enter(HopPhase.Windup); }
                    break;
                case HopPhase.Windup:
                    if (HasInput) hopDir = MoveInput.normalized;   // masih bisa ganti arah sebelum lepas landas
                    if (t >= Windup) Enter(HopPhase.Air);
                    break;
                case HopPhase.Air:
                    if (HasInput)
                    {
                        float cur = Mathf.Atan2(hopDir.y, hopDir.x) * Mathf.Rad2Deg;
                        float want = Mathf.Atan2(MoveInput.y, MoveInput.x) * Mathf.Rad2Deg;
                        float a = Mathf.MoveTowardsAngle(cur, want, airSteer * Time.deltaTime) * Mathf.Deg2Rad;
                        hopDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    }
                    if (t >= airTime) Enter(HopPhase.Land);
                    break;
                case HopPhase.Land:
                    if (t >= Land) Enter(HopPhase.Slide);
                    break;
                case HopPhase.Slide:
                    if (t >= slideTime)
                    {
                        if (HasInput) { hopDir = MoveInput.normalized; Enter(HopPhase.Windup); }
                        else Enter(HopPhase.Idle);
                    }
                    break;
            }
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            float w = Windup, a = airTime, l = Land, s = slideTime, total = CycleTime;
            float k;
            switch (phase)
            {
                case HopPhase.Windup: k = Mathf.Clamp01(t / w); CyclePhase = k * w / total; break;
                case HopPhase.Air: k = Mathf.Clamp01(t / a); CyclePhase = (w + k * a) / total; break;
                case HopPhase.Land: k = Mathf.Clamp01(t / l); CyclePhase = (w + a + k * l) / total; break;
                case HopPhase.Slide: k = Mathf.Clamp01(t / s); CyclePhase = (w + a + l + k * s) / total; break;
                default: k = 0f; CyclePhase = 0f; break;
            }
            if (jelly == null) return;
            switch (phase)
            {
                case HopPhase.Windup: jelly.Target = windupSquash; jelly.Lift = 0f; break;
                case HopPhase.Air: jelly.Target = airStretch * (1f - k); jelly.Lift = hopHeight * Mathf.Sin(k * Mathf.PI); break;
                case HopPhase.Land: jelly.Target = windupSquash * 0.5f; jelly.Lift = 0f; break;
                default: jelly.Target = 0f; jelly.Lift = 0f; break;
            }
        }

        private void FixedUpdate()
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;
            float v;
            switch (phase)
            {
                case HopPhase.Air: v = AirSpeed(); break;
                case HopPhase.Slide: v = Mathf.Lerp(slideStart, 0f, Mathf.Clamp01(t / slideTime)); break;
                default: v = 0f; break;
            }
            rb.linearVelocity = hopDir * v;
        }
    }
}

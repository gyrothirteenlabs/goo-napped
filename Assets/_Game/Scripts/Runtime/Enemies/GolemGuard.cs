using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Golem Batu: patroli lambat, melihat LURUS ke depan (kerucut sempit, jauh), berbelok pelan.
    // Cara mengakali: nyamar (potongan yang tidak dikendalikan tidak terlihat), lewat di belakangnya,
    // atau pakai potongan umpan supaya dia berhenti dan menoleh.
    [RequireComponent(typeof(Rigidbody2D), typeof(SlimeSpriteAnimator))]
    public class GolemGuard : MonoBehaviour
    {
        private enum State { Patrol, Wait, Investigate, Blocked, Caught }

        [Header("Patroli (posisi dunia)")]
        [SerializeField] private List<Vector2> patrolPoints = new List<Vector2>();
        [SerializeField] private float speed = 0.9f;
        [SerializeField] private float waitAtPoint = 1.2f;
        [Tooltip("Derajat per detik. Golem kaku: berbelok pelan.")]
        [SerializeField] private float turnSpeed = 70f;

        [Header("Penglihatan")]
        [SerializeField] private float viewRange = 3.6f;
        [Tooltip("Lebar kerucut total (derajat). Golem: sempit, 'melihat lurus'.")]
        [SerializeField] private float viewAngle = 40f;
        [Tooltip("Tinggi mata dari titik kaki (unit).")]
        [SerializeField] private float eyeHeight = 0.45f;
        [Tooltip("Slime yang menyala (Ramuan Api) terlihat dari lebih jauh.")]
        [SerializeField] private float glowRangeMultiplier = 1.4f;

        [Header("Curiga")]
        [Tooltip("Detik sampai ketahuan kalau target terlihat di jarak menengah.")]
        [SerializeField] private float fillTime = 1.4f;
        [SerializeField] private float decayPerSecond = 0.3f;
        [Tooltip("Tambahan laju curiga per ciri tiruan yang salah di toples level ini.")]
        [SerializeField] private float mismatchBonus = 0.35f;
        [Tooltip("Lama menatap tempat terakhir target terlihat sebelum kembali patroli.")]
        [SerializeField] private float investigateTime = 2f;

        [Header("Terhalang potongan")]
        [Tooltip("Jarak (pusat ke pusat) saat golem berhenti karena ada potongan slime di depannya.")]
        [SerializeField] private float blockDistance = 0.75f;
        [Tooltip("Lama golem menatap penghalang sebelum putar balik.")]
        [SerializeField] private float blockedStare = 1.5f;

        [Header("Tampilan")]
        [SerializeField] private Material coneMaterial;
        [SerializeField] private int coneSortingOrder = -50;

        private Rigidbody2D rb;
        private SlimeSpriteAnimator anim;
        private PlayerBrain brain;
        private LevelGoal goal;
        private Jar[] jars;
        private VisionCone cone;
        private State state = State.Patrol;
        private int pointIndex;
        private float timer;
        private Vector2 forward = Vector2.down;
        private Vector2 lastSeen;
        private bool moving;

        public float Suspicion { get; private set; }
        public bool SeesTarget { get; private set; }
        public Vector2 Forward => forward;
        public Vector2 Eye => (Vector2)transform.position + Vector2.up * eyeHeight;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            anim = GetComponent<SlimeSpriteAnimator>();
        }

        private void Start()
        {
            brain = FindFirstObjectByType<PlayerBrain>();
            goal = FindFirstObjectByType<LevelGoal>();
            jars = FindObjectsByType<Jar>(FindObjectsSortMode.None);
            if (coneMaterial != null) cone = VisionCone.Create("VisionCone_" + name, coneMaterial, coneSortingOrder);
            if (patrolPoints.Count > 1)
            {
                var d = patrolPoints[1] - patrolPoints[0];
                if (d.sqrMagnitude > 1e-4f) forward = d.normalized;
            }
        }

        private void OnDestroy()
        {
            if (cone != null) Destroy(cone.gameObject);
        }

        private int TotalMismatch()
        {
            int m = 0;
            if (jars != null)
                foreach (var j in jars)
                    if (j != null && j.DecoyMismatch > 0) m += j.DecoyMismatch;
            return m;
        }

        // Jarak tembus pandang ke arah `delta` (berhenti di dinding/prop). Slime, penjaga, dan trigger tidak menghalangi.
        private float Clip(Vector2 from, Vector2 delta)
        {
            float best = delta.magnitude;
            foreach (var h in Physics2D.RaycastAll(from, delta.normalized, best))
            {
                var c = h.collider;
                if (c == null || c.isTrigger) continue;
                if (c.GetComponentInParent<SlimePiece>() != null || c.GetComponentInParent<GolemGuard>() != null) continue;
                best = Mathf.Min(best, h.distance);
            }
            return best;
        }

        private bool CanSee(SlimePiece p, out float distance)
        {
            distance = 0f;
            if (p == null || !p.IsControlled) return false;   // potongan yang nyamar tidak terlihat
            float range = viewRange * (p.Body.State.EmitsLight ? glowRangeMultiplier : 1f);
            var target = p.Center;
            if (!VisionMath.InCone(Eye, forward, target, range, viewAngle)) return false;
            distance = Vector2.Distance(Eye, target);
            return Clip(Eye, target - Eye) >= distance - 0.05f;
        }

        private void Update()
        {
            if (state == State.Caught || (goal != null && (goal.IsComplete || goal.IsFailed)))
            {
                moving = false;
                UpdateVisuals();
                return;
            }

            var target = brain != null ? brain.Active : null;
            SeesTarget = CanSee(target, out float dist);
            if (SeesTarget)
            {
                float range = viewRange * (target.Body.State.EmitsLight ? glowRangeMultiplier : 1f);
                float rate = VisionMath.DistanceFactor(dist, range) * VisionMath.MismatchMultiplier(TotalMismatch(), mismatchBonus);
                Suspicion = Mathf.Min(1f, Suspicion + Time.deltaTime * rate / Mathf.Max(0.05f, fillTime));
                lastSeen = target.Center;
                state = State.Investigate;
                timer = investigateTime;
                if (Suspicion >= 1f)
                {
                    state = State.Caught;
                    if (goal != null) goal.Fail("Ketahuan Golem Batu");
                }
            }
            else
            {
                Suspicion = Mathf.Max(0f, Suspicion - decayPerSecond * Time.deltaTime);
            }

            moving = false;
            switch (state)
            {
                case State.Investigate:
                    TurnTowards(lastSeen - Eye);
                    if (!SeesTarget && (timer -= Time.deltaTime) <= 0f) state = State.Patrol;
                    break;
                case State.Wait:
                    if ((timer -= Time.deltaTime) <= 0f)
                    {
                        pointIndex = (pointIndex + 1) % patrolPoints.Count;
                        state = State.Patrol;
                    }
                    break;
                case State.Patrol:
                    if (patrolPoints.Count == 0) break;
                    var to = patrolPoints[pointIndex] - rb.position;
                    if (to.magnitude < 0.05f)
                    {
                        state = State.Wait;
                        timer = waitAtPoint;
                        break;
                    }
                    TurnTowards(to);
                    // ada potongan slime (nyamar) menghalangi: berhenti, menatap, lalu putar balik
                    var blocker = PieceAhead(to);
                    if (blocker != null)
                    {
                        lastSeen = blocker.Center;
                        state = State.Blocked;
                        timer = blockedStare;
                        break;
                    }
                    // jalan hanya kalau sudah kurang lebih menghadap tujuan (golem kaku)
                    moving = Vector2.Angle(forward, to) < 20f;
                    break;
                case State.Blocked:
                    TurnTowards(lastSeen - Eye);
                    if ((timer -= Time.deltaTime) <= 0f)
                    {
                        // patroli dibalik: kembali ke titik sebelumnya
                        pointIndex = (pointIndex - 1 + patrolPoints.Count) % patrolPoints.Count;
                        state = State.Patrol;
                    }
                    break;
            }
            UpdateVisuals();
        }

        // Potongan slime tepat di jalur menuju tujuan (jarak badan), atau null.
        // Dicek terhadap arah TUJUAN, bukan arah hadap: setelah putar balik, golem tidak tersangkut lagi.
        private SlimePiece PieceAhead(Vector2 toward)
        {
            if (brain == null) return null;
            foreach (var p in brain.Pieces)
            {
                if (p == null) continue;
                var d = p.Center - (rb.position + Vector2.up * 0.22f);
                if (d.magnitude < blockDistance && Vector2.Angle(toward, d) < 50f) return p;
            }
            return null;
        }

        private void FixedUpdate()
        {
            if (!moving || patrolPoints.Count == 0) return;
            var target = patrolPoints[pointIndex];
            rb.MovePosition(Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
        }

        private void TurnTowards(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            float cur = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float want = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float a = Mathf.MoveTowardsAngle(cur, want, turnSpeed * Time.deltaTime) * Mathf.Deg2Rad;
            forward = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        private void UpdateVisuals()
        {
            string dir = Mathf.Abs(forward.x) > Mathf.Abs(forward.y) ? "side" : (forward.y > 0 ? "back" : "front");
            anim.Play(moving ? "move" : "idle", dir, forward.x < 0);
            if (cone != null) cone.Draw(Eye, forward, viewRange, viewAngle, Suspicion, Clip);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                Gizmos.DrawWireSphere(patrolPoints[i], 0.1f);
                if (patrolPoints.Count > 1) Gizmos.DrawLine(patrolPoints[i], patrolPoints[(i + 1) % patrolPoints.Count]);
            }
        }
    }
}

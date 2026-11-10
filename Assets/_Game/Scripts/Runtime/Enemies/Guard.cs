using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Penjaga umum. Tiap musuh = Guard dengan setelan berbeda (prefab):
    //   Golem Batu   : kerucut sempit & jauh ("melihat lurus"), lambat, langkah berat.
    //   Familiar Api : melayang, kerucut lebar, BUTA DI GELAP (needsLightToSee) tapi melihat yang PANAS/menyala
    //                  dari jauh (heatSightRange). Membawa cahaya sendiri, jadi slime di dekatnya ikut terang.
    // Perilaku bersama:
    //   - Lihat slime  : berhenti, menoleh, curiga naik.
    //   - Hilang dari pandangan setelah cukup curiga: datangi tempat terakhir slime terlihat,
    //     diam sambil menyapu pandangan, lalu kembali patroli.
    //   - Radius sadar : slime yang terlalu dekat (termasuk di belakang) membuat penjaga menoleh.
    //   - Cahaya       : slime di cahaya terlihat dari segala arah dalam jarak tertentu.
    //   - Umpan / suara: didatangi dan diperiksa tanpa menaikkan curiga.
    //   - Potongan nyamar menghalangi jalan: berhenti, menatap, lalu putar balik.
    [RequireComponent(typeof(Rigidbody2D), typeof(SlimeSpriteAnimator))]
    public class Guard : MonoBehaviour
    {
        private enum State { Patrol, Wait, Alert, Search, LookAround, Blocked, Caught }

        [Header("Jenis")]
        [Tooltip("Nama untuk pesan 'Ketahuan ...'.")]
        [SerializeField] private string guardName = "Golem Batu";
        [Tooltip("Familiar Api: slime di kerucut hanya terlihat kalau terang atau panas/menyala.")]
        [SerializeField] private bool needsLightToSee;
        [Tooltip("Slime panas/menyala di dalam kerucut terlihat sejauh ini (0 = pakai View Range biasa).")]
        [SerializeField] private float heatSightRange;
        [Tooltip("Melayang naik-turun (unit). 0 = berjalan di tanah.")]
        [SerializeField] private float hoverHeight;
        [SerializeField] private float hoverSpeed = 3f;

        [Header("Patroli (posisi dunia)")]
        [SerializeField] private List<Vector2> patrolPoints = new List<Vector2>();
        [SerializeField] private float speed = 1.0f;
        [SerializeField] private float waitAtPoint = 1.0f;
        [Tooltip("Derajat per detik saat patroli.")]
        [SerializeField] private float turnSpeed = 140f;

        [Header("Penglihatan")]
        [SerializeField] private float viewRange = 3.6f;
        [Tooltip("Lebar kerucut total (derajat). Golem: sempit, 'melihat lurus'.")]
        [SerializeField] private float viewAngle = 40f;
        [Tooltip("Tinggi mata dari titik kaki (unit).")]
        [SerializeField] private float eyeHeight = 0.45f;
        [Tooltip("Slime yang menyala terlihat dari lebih jauh.")]
        [SerializeField] private float glowRangeMultiplier = 1.4f;
        [Tooltip("Radius sadar ke segala arah (termasuk belakang).")]
        [SerializeField] private float awarenessRadius = 1.1f;
        [Tooltip("Laju curiga di radius sadar dibanding terlihat langsung.")]
        [SerializeField] private float awarenessRate = 0.5f;

        [Header("Penciuman (Anjing Lendir)")]
        [Tooltip("Jarak mencium jejak/umpan bau (0 = tidak bisa mencium). Mencium tembus tikungan, tidak butuh cahaya.")]
        [SerializeField] private float smellRange;
        [Tooltip("Laju curiga saat mencium slime berbau di dekatnya.")]
        [SerializeField] private float smellRate = 0.8f;

        [Header("Cahaya")]
        [Tooltip("Slime yang berdiri di cahaya (terang >= ambang) terlihat dari segala arah sejauh ini.")]
        [SerializeField] private float lightSightRange = 4.5f;
        [Tooltip("Ambang terang (0..1) dari LightExposure.")]
        [SerializeField, Range(0f, 1f)] private float litThreshold = 0.35f;
        [Tooltip("Laju curiga karena terlihat di cahaya dibanding di dalam kerucut.")]
        [SerializeField] private float lightRate = 0.7f;

        [Header("Suara")]
        [Tooltip("Jarak antar langkah (detik) saat berjalan.")]
        [SerializeField] private float stepInterval = 0.55f;

        [Header("Curiga")]
        [Tooltip("Detik sampai ketahuan kalau target terlihat di jarak menengah.")]
        [SerializeField] private float fillTime = 1.6f;
        [SerializeField] private float decayPerSecond = 0.2f;
        [Tooltip("Tambahan laju curiga per ciri tiruan yang salah di toples level ini.")]
        [SerializeField] private float mismatchBonus = 0.35f;
        [Tooltip("Di atas nilai ini, golem mendatangi tempat terakhir slime terlihat.")]
        [SerializeField] private float searchThreshold = 0.3f;
        [SerializeField] private float searchSpeedMultiplier = 1.3f;
        [Tooltip("Lama diam sambil menyapu pandangan di tempat terakhir slime terlihat.")]
        [SerializeField] private float lookAroundTime = 3.5f;
        [Tooltip("Sudut sapuan pandangan kiri-kanan (derajat).")]
        [SerializeField] private float lookAroundAngle = 75f;

        [Tooltip("Golem berhenti sejauh ini dari titik yang diperiksa (tidak menimpa potongan/umpan).")]
        [SerializeField] private float searchStopDistance = 0.8f;
        [Tooltip("Umpan yang sudah diperiksa diabaikan selama ini (detik).")]
        [SerializeField] private float baitIgnoreTime = 8f;

        [Header("Terhalang potongan")]
        [SerializeField] private float blockDistance = 0.75f;
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
        private Vector2 lastSeen, lookBase;
        private Vector2 moveTarget;
        private float moveSpeed;
        private bool moving;
        private SlimePiece lastBait;
        private float baitCooldown;
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

        public float Suspicion { get; private set; }
        public bool SeesTarget { get; private set; }
        public bool Senses { get; private set; }
        // Slime terlihat karena berdiri di cahaya (bukan karena kerucut/radius).
        public bool InLight { get; private set; }
        // Slime tercium (penjaga pencium).
        public bool Smells { get; private set; }
        public string GuardName => guardName;
        private float trackedTime = -999f, sniffTimer;
        private bool alerted;
        private float stepTimer;
        public Vector2 Forward => forward;
        public Vector2 Eye => (Vector2)transform.position + Vector2.up * eyeHeight;
        private Vector2 Body => rb.position + Vector2.up * 0.22f;

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

        private static bool IsSeeThrough(Collider2D c) =>
            c == null || c.isTrigger || c.GetComponentInParent<SlimePiece>() != null || c.GetComponentInParent<Guard>() != null;

        // Jarak tembus pandang ke arah `delta` (berhenti di dinding/prop).
        private float Clip(Vector2 from, Vector2 delta)
        {
            float best = delta.magnitude;
            foreach (var h in Physics2D.RaycastAll(from, delta.normalized, best))
                if (!IsSeeThrough(h.collider)) best = Mathf.Min(best, h.distance);
            return best;
        }

        private bool LineOfSight(Vector2 from, Vector2 to) => Clip(from, to - from) >= Vector2.Distance(from, to) - 0.05f;

        // Panas atau menyala (Ramuan Api, Lumut Pendar, ...): dilihat Familiar Api dari jauh.
        private static bool IsHot(SlimePiece p) =>
            p.Body.State.EmitsLight || p.Body.State.Get(TraitKind.Temperature) == "panas";

        private float RangeFor(SlimePiece p)
        {
            float r = viewRange * (p.Body.State.EmitsLight ? glowRangeMultiplier : 1f);
            if (heatSightRange > 0f && IsHot(p)) r = Mathf.Max(r, heatSightRange);
            return r;
        }

        private bool CanSee(SlimePiece p, out float distance)
        {
            distance = 0f;
            if (p == null || p.IsDisguised) return false;   // potongan yang nyamar tidak terlihat
            if (!VisionMath.InCone(Eye, forward, p.Center, RangeFor(p), viewAngle)) return false;
            // buta di gelap: hanya melihat yang terang atau panas
            if (needsLightToSee && !IsHot(p) && LightExposure.At(p.Center) < litThreshold) return false;
            distance = Vector2.Distance(Eye, p.Center);
            return LineOfSight(Eye, p.Center);
        }

        // Suara (gumpalan dilempar, dsb.): datangi dan periksa, curiga tidak naik.
        private void OnNoise(Vector2 position, float radius)
        {
            if (state == State.Caught || state == State.Alert) return;
            if (Vector2.Distance(Body, position) > radius) return;
            lastSeen = position;
            state = State.Search;
        }

        private void OnEnable() => NoiseBus.Noise += OnNoise;
        private void OnDisable() => NoiseBus.Noise -= OnNoise;

        // Umpan (potongan nyamar yang menyala/berbau) di dalam kerucut. Umpan yang baru diperiksa diabaikan sebentar.
        private bool TryNoticeBait(out SlimePiece bait)
        {
            bait = null;
            if (brain == null) return false;
            foreach (var p in brain.Pieces)
            {
                if (p == null || !p.IsBait || (p == lastBait && baitCooldown > 0f)) continue;
                // pencium: umpan berbau tercium dari segala arah (tembus tikungan)
                float smell = p.Body.State.SmellRadius;
                if (smellRange > 0f && smell > 0f && Vector2.Distance(Body, p.Center) <= smellRange + smell * 0.5f)
                {
                    bait = p;
                    return true;
                }
                if (!VisionMath.InCone(Eye, forward, p.Center, RangeFor(p), viewAngle)) continue;
                if (!LineOfSight(Eye, p.Center)) continue;
                bait = p;
                return true;
            }
            return false;
        }

        // Mencium slime berbau yang sedang bergerak di dekatnya (tidak butuh garis pandang atau cahaya).
        private bool CanSmell(SlimePiece p)
        {
            if (smellRange <= 0f || p == null || p.IsDisguised) return false;
            float smell = p.Body.State.SmellRadius;
            return smell > 0f && Vector2.Distance(Body, p.Center) <= (smellRange + smell) * 0.5f;
        }

        // Ikuti jejak: pindah ke titik bau yang lebih baru dari yang terakhir diikuti.
        private bool Sniff()
        {
            if (smellRange <= 0f) return false;
            if ((sniffTimer -= Time.deltaTime) > 0f) return false;
            sniffTimer = 0.3f;
            if (!ScentTrail.Freshest(Body, smellRange, trackedTime, out var m, p => LineOfSight(Body, p))) return false;
            trackedTime = m.time;
            lastSeen = m.position;
            return true;
        }

        private bool CanSense(SlimePiece p)
        {
            if (p == null || p.IsDisguised) return false;
            return Vector2.Distance(Body, p.Center) <= awarenessRadius && LineOfSight(Body, p.Center);
        }

        // Slime yang berdiri di cahaya (atau menyala sendiri) terlihat dari segala arah dalam jarak tertentu,
        // walau tidak di dalam kerucut. Cahaya = bahaya, bukan hiasan.
        private bool SeesInLight(SlimePiece p)
        {
            if (p == null || p.IsDisguised) return false;
            if (Vector2.Distance(Eye, p.Center) > lightSightRange) return false;
            bool lit = p.Body.State.EmitsLight || LightExposure.At(p.Center) >= litThreshold;
            return lit && LineOfSight(Eye, p.Center);
        }

        private void Update()
        {
            moving = false;
            if (state == State.Caught || (goal != null && (goal.IsComplete || goal.IsFailed)))
            {
                UpdateVisuals();
                return;
            }

            if (baitCooldown > 0f) baitCooldown -= Time.deltaTime;
            var target = brain != null ? brain.Active : null;
            SeesTarget = CanSee(target, out float dist);
            bool near = !SeesTarget && CanSense(target);
            InLight = !SeesTarget && !near && SeesInLight(target);
            Smells = !SeesTarget && !near && !InLight && CanSmell(target);
            Senses = near || InLight || Smells;

            if (SeesTarget || Senses)
            {
                float rate = SeesTarget ? VisionMath.DistanceFactor(dist, RangeFor(target))
                           : near ? awarenessRate : InLight ? lightRate : smellRate;
                // bunyi "hm?" sekali tiap kali mulai curiga (diulang lagi setelah curiga turun ke 0)
                if (!alerted) { alerted = true; AudioDirector.Play(AudioDirector.Sfx.Alert, Body); }
                rate *= VisionMath.MismatchMultiplier(TotalMismatch(), mismatchBonus);
                Suspicion = Mathf.Min(1f, Suspicion + Time.deltaTime * rate / Mathf.Max(0.05f, fillTime));
                lastSeen = target.Center;
                state = State.Alert;
                if (Suspicion >= 1f)
                {
                    state = State.Caught;
                    AudioDirector.Play(AudioDirector.Sfx.Caught, Body);
                    if (goal != null) goal.Fail("Ketahuan " + guardName);
                    UpdateVisuals();
                    return;
                }
            }
            else if (state != State.Search && state != State.Blocked && TryNoticeBait(out var bait))
            {
                // umpan menyala di depan mata: datangi dan periksa, curiga ke pemain tidak naik
                lastSeen = bait.Center;
                lastBait = bait;
                baitCooldown = baitIgnoreTime;
                state = State.Search;
            }
            else
            {
                float decay = state == State.Search || state == State.LookAround ? decayPerSecond * 0.4f : decayPerSecond;
                Suspicion = Mathf.Max(0f, Suspicion - decay * Time.deltaTime);
                if (Suspicion <= 0f) alerted = false;
                // pencium menyusuri jejak bau (dari mana pun asalnya: pemain atau potongan berbau)
                if (state != State.Blocked && Sniff()) state = State.Search;
            }

            switch (state)
            {
                case State.Alert:
                    // menoleh cepat ke arah slime
                    TurnTowards(lastSeen - Eye, turnSpeed * 1.6f);
                    if (!SeesTarget && !Senses)
                    {
                        if (Suspicion >= searchThreshold) { state = State.Search; }
                        else { state = State.LookAround; timer = lookAroundTime * 0.5f; lookBase = forward; }
                    }
                    break;

                case State.Search:
                    // datangi tempat terakhir slime terlihat
                    var toSeen = lastSeen - Body;
                    if (toSeen.magnitude < searchStopDistance ||!TryMove(lastSeen - (Vector2.up * 0.22f), speed * searchSpeedMultiplier))
                    {
                        state = State.LookAround;
                        timer = lookAroundTime;
                        lookBase = forward;
                    }
                    break;

                case State.LookAround:
                    // diam sambil menyapu pandangan kiri-kanan
                    timer -= Time.deltaTime;
                    float phase = (lookAroundTime - timer) / lookAroundTime * Mathf.PI * 2f;
                    float baseA = Mathf.Atan2(lookBase.y, lookBase.x) * Mathf.Rad2Deg;
                    float want = baseA + Mathf.Sin(phase) * lookAroundAngle;
                    TurnTowards(new Vector2(Mathf.Cos(want * Mathf.Deg2Rad), Mathf.Sin(want * Mathf.Deg2Rad)), turnSpeed);
                    if (timer <= 0f) state = State.Patrol;
                    break;

                case State.Wait:
                    if ((timer -= Time.deltaTime) <= 0f)
                    {
                        pointIndex = (pointIndex + 1) % patrolPoints.Count;
                        state = State.Patrol;
                    }
                    break;

                case State.Blocked:
                    TurnTowards(lastSeen - Eye, turnSpeed);
                    if ((timer -= Time.deltaTime) <= 0f)
                    {
                        pointIndex = (pointIndex - 1 + patrolPoints.Count) % patrolPoints.Count;
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
                    var blocker = PieceAhead(to);
                    if (blocker != null)
                    {
                        lastSeen = blocker.Center;
                        state = State.Blocked;
                        timer = blockedStare;
                        break;
                    }
                    TryMove(patrolPoints[pointIndex], speed);
                    break;
            }
            UpdateVisuals();
        }

        // Menoleh ke tujuan lalu jalan; melambat saat masih berbelok. false = jalan tertutup tembok.
        private bool TryMove(Vector2 dest, float spd)
        {
            var d = dest - rb.position;
            if (d.sqrMagnitude < 1e-4f) return true;
            TurnTowards(d, turnSpeed);
            float facing = Mathf.Clamp01(Mathf.Cos(Vector2.Angle(forward, d) * Mathf.Deg2Rad));
            if (facing < 0.3f) return true;   // masih berbelok di tempat
            float step = spd * facing * Time.deltaTime;
            if (PathClear(d.normalized, step))
            {
                moveTarget = dest;
            }
            else
            {
                // menyusur tembok: diagonal terhalang sudut -> coba sumbu dominan dulu, lalu sumbu lainnya
                var ax = new Vector2(Mathf.Sign(d.x), 0f);
                var ay = new Vector2(0f, Mathf.Sign(d.y));
                var first = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? ax : ay;
                var second = first == ax ? ay : ax;
                Vector2 dir;
                if (Mathf.Abs(Vector2.Dot(first, d)) > 0.05f && PathClear(first, step)) dir = first;
                else if (Mathf.Abs(Vector2.Dot(second, d)) > 0.05f && PathClear(second, step)) dir = second;
                else return false;
                moveTarget = rb.position + dir * Mathf.Min(0.5f, Mathf.Abs(Vector2.Dot(dir, d)));
            }
            moveSpeed = spd * facing;
            moving = true;
            return true;
        }

        // Cek tembok di arah `dir`. Hit dengan jarak 0 = sudah bersinggungan dari awal (lorong sempit): abaikan,
        // kalau tidak penjaga tersangkut di lorong selebar 1 tile.
        private bool PathClear(Vector2 dir, float step)
        {
            int n = Physics2D.CircleCast(Body, 0.3f, dir, new ContactFilter2D { useTriggers = false }, castHits, step + 0.05f);
            for (int i = 0; i < n; i++)
                if (castHits[i].distance > 0f && !IsSeeThrough(castHits[i].collider)) return false;
            return true;
        }

        // Potongan slime tepat di jalur menuju tujuan (jarak badan), atau null.
        // Dicek terhadap arah TUJUAN, bukan arah hadap: setelah putar balik, golem tidak tersangkut lagi.
        private SlimePiece PieceAhead(Vector2 toward)
        {
            if (brain == null) return null;
            foreach (var p in brain.Pieces)
            {
                if (p == null) continue;
                var d = p.Center - Body;
                if (d.magnitude < blockDistance && Vector2.Angle(toward, d) < 50f) return p;
            }
            return null;
        }

        private void LateUpdate()
        {
            if (hoverHeight > 0f) Hover();
            // langkah (golem). Penjaga melayang: stepInterval 0 = tanpa langkah
            if (!moving || stepInterval <= 0f) { stepTimer = 0f; return; }
            if ((stepTimer -= Time.deltaTime) <= 0f)
            {
                stepTimer = stepInterval;
                AudioDirector.Play(AudioDirector.Sfx.GolemStep, Body, 0.8f);
            }
        }

        private Transform[] hoverParts;
        private Vector3[] hoverBase;

        private void Hover()
        {
            if (hoverParts == null)
            {
                var list = new List<Transform>();
                if (anim.bodyRenderer != null) list.Add(anim.bodyRenderer.transform);
                if (anim.faceRenderer != null && anim.faceRenderer.transform != transform) list.Add(anim.faceRenderer.transform);
                hoverParts = list.ToArray();
                hoverBase = new Vector3[hoverParts.Length];
                for (int i = 0; i < hoverParts.Length; i++) hoverBase[i] = hoverParts[i].localPosition;
            }
            float y = hoverHeight * (1f + Mathf.Sin(Time.time * hoverSpeed)) * 0.5f;
            for (int i = 0; i < hoverParts.Length; i++)
                hoverParts[i].localPosition = hoverBase[i] + Vector3.up * y;
        }

        private void FixedUpdate()
        {
            if (!moving) return;
            rb.MovePosition(Vector2.MoveTowards(rb.position, moveTarget, moveSpeed * Time.fixedDeltaTime));
        }

        private void TurnTowards(Vector2 dir, float degPerSec)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            float cur = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float want = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float a = Mathf.MoveTowardsAngle(cur, want, degPerSec * Time.deltaTime) * Mathf.Deg2Rad;
            forward = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        private void UpdateVisuals()
        {
            string dir = Mathf.Abs(forward.x) > Mathf.Abs(forward.y) ? "side" : (forward.y > 0 ? "back" : "front");
            anim.Play(moving ? "move" : "idle", dir, forward.x < 0);
            if (cone != null) cone.Draw(Eye, forward, viewRange, viewAngle, Suspicion, Clip, Body, awarenessRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                Gizmos.DrawWireSphere(patrolPoints[i], 0.1f);
                if (patrolPoints.Count > 1) Gizmos.DrawLine(patrolPoints[i], patrolPoints[(i + 1) % patrolPoints.Count]);
            }
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.22f, awarenessRadius);
        }
    }
}

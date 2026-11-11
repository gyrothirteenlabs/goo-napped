using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Penjaga umum (desain-v2 bagian 9). Tiap musuh = Guard dengan setelan berbeda (prefab):
    //   Golem Batu   : kerucut sempit & jauh ("melihat lurus"), lambat, langkah berat.
    //   Familiar Api : melayang, BUTA DI GELAP (needsLightToSee) tapi melihat yang PANAS/menyala dari jauh, membawa cahaya.
    //   Anjing Lendir: pandangan pendek, mencium jejak bau/lumpur dan slime berbau (tembus tikungan).
    // Perilaku v2:
    //   - Yang bisa terlihat: potongan yang dikendalikan, potongan yang ditinggal, dan teman yang sedang di luar.
    //   - Terlihat: curiga naik. Penuh = MENGEJAR (berlari lewat A*), berteriak memanggil penjaga lain di dekat.
    //     Menyentuh target = tertangkap (level diulang dari checkpoint). Pandangan putus cukup lama = target lolos:
    //     penjaga mencari di tempat terakhir target terlihat, menyapu pandangan, lalu kembali patroli.
    //   - Transformasi yang terlihat (belah, gabung, potongan bangun, masuk/keluar tempat sembunyi) = curiga melonjak.
    //   - Tempat sembunyi: isinya tidak terlihat. Barang yang ditatap dari dekat makin mencurigakan, penuh = diperiksa.
    //     Terlihat masuk = langsung diperiksa. Pemeriksa (inspectsHidingSpots) memeriksa tempat di dekat jalurnya.
    //   - Radius sadar (segala arah), cahaya = bahaya, suara = didatangi, jejak bau = disusuri,
    //     alarm penyadapan toples = semua penjaga waspada dan mendatangi toples.
    [RequireComponent(typeof(Rigidbody2D), typeof(SlimeSpriteAnimator))]
    public class Guard : MonoBehaviour
    {
        private enum State { Patrol, Wait, Alert, Search, LookAround, Chase, Inspect, Caught }

        [Header("Jenis")]
        [Tooltip("Nama untuk pesan 'Caught by ...' (bahasa Inggris).")]
        [SerializeField] private string guardName = "Stone Golem";
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
        [Tooltip("Jarak mencium jejak/slime berbau (0 = tidak bisa mencium). Mencium tembus tikungan, tidak butuh cahaya.")]
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
        [Tooltip("Jarak antar langkah (detik) saat berjalan. 0 = tanpa langkah (melayang).")]
        [SerializeField] private float stepInterval = 0.55f;

        [Header("Curiga")]
        [Tooltip("Detik sampai mengejar kalau target terlihat di jarak menengah.")]
        [SerializeField] private float fillTime = 1.6f;
        [SerializeField] private float decayPerSecond = 0.2f;
        [Tooltip("Tambahan laju curiga per ciri tiruan yang salah di toples level ini.")]
        [SerializeField] private float mismatchBonus = 0.35f;
        [Tooltip("Di atas nilai ini, penjaga mendatangi tempat terakhir slime terlihat.")]
        [SerializeField] private float searchThreshold = 0.3f;
        [SerializeField] private float searchSpeedMultiplier = 1.3f;
        [Tooltip("Lama diam sambil menyapu pandangan di tempat terakhir slime terlihat.")]
        [SerializeField] private float lookAroundTime = 3.5f;
        [Tooltip("Sudut sapuan pandangan kiri-kanan (derajat).")]
        [SerializeField] private float lookAroundAngle = 75f;
        [Tooltip("Penjaga berhenti sejauh ini dari titik yang diperiksa.")]
        [SerializeField] private float searchStopDistance = 0.8f;
        [Tooltip("Lonjakan curiga saat melihat transformasi (belah, gabung, potongan bangun).")]
        [SerializeField, Range(0f, 1f)] private float transformSuspicion = 0.5f;
        [Tooltip("Setelah alarm penyadapan: pengali waktu curiga penuh (lebih kecil = lebih cepat curiga).")]
        [SerializeField, Range(0.2f, 1f)] private float alarmFillMultiplier = 0.6f;

        [Header("Mengejar (desain-v2 jawaban 5)")]
        [Tooltip("Pengali kecepatan saat mengejar.")]
        [SerializeField] private float chaseSpeedMultiplier = 1.8f;
        [Tooltip("Detik pandangan putus sampai target dianggap lolos.")]
        [SerializeField] private float loseSightTime = 2.5f;
        [Tooltip("Jarak menangkap (dari badan penjaga ke tepi badan target).")]
        [SerializeField] private float catchDistance = 0.3f;
        [Tooltip("Radius teriakan: penjaga lain di dalamnya datang membantu.")]
        [SerializeField] private float callRadius = 7f;

        [Header("Tempat sembunyi (desain-v2 bagian 7)")]
        [Tooltip("Jarak menatap barang yang membuat isinya makin mencurigakan.")]
        [SerializeField] private float stareRange = 2.6f;
        [Tooltip("Pemeriksa (Mata Penyihir, Pelayan Tudung): memeriksa tiap tempat sembunyi di dekat jalurnya.")]
        [SerializeField] private bool inspectsHidingSpots;
        [SerializeField] private float inspectRadius = 1.3f;
        [Tooltip("Lama memeriksa satu tempat sembunyi (detik).")]
        [SerializeField] private float inspectTime = 0.9f;
        [Tooltip("Tempat yang sudah diperiksa dilewati selama ini (detik).")]
        [SerializeField] private float reinspectDelay = 12f;

        [Header("Perilaku khusus (musuh unik, desain-v2 bagian 9)")]
        [Tooltip("Tidak bisa berpindah (Lukisan Bermata, Monster Gerbang).")]
        [SerializeField] private bool stationary;
        [Tooltip("Diam + curiga penuh: true = langsung tertangkap (Monster Gerbang), false = bunyikan alarm memanggil penjaga (Lukisan).")]
        [SerializeField] private bool stationaryCatches;
        [Tooltip("Arah pandang dasar saat diam/patroli satu titik.")]
        [SerializeField] private Vector2 facing = Vector2.down;
        [Tooltip("Sapuan pandangan kiri-kanan saat bertugas (derajat). 0 = tidak menyapu.")]
        [SerializeField] private float sweepAngle;
        [SerializeField] private float sweepSpeed = 0.6f;
        [Tooltip("Pengali radius suara yang terdengar (Zirah Hampa: buta, pendengarannya tajam).")]
        [SerializeField] private float hearingMultiplier = 1f;
        [Tooltip("Mata Penyihir: toples berisi tiruan beraura salah di dekat jalurnya langsung dibongkar (alarm).")]
        [SerializeField] private bool inspectsJars;
        [Tooltip("Pemburu (Alkemis): tiap N detik mendatangi perkiraan posisi pemain. 0 = tidak memburu.")]
        [SerializeField] private float huntInterval;
        [Tooltip("Ketidaktepatan perkiraan posisi pemburu (unit).")]
        [SerializeField] private float huntError = 2.5f;

        [Header("Tampilan")]
        [SerializeField] private Material coneMaterial;
        [SerializeField] private int coneSortingOrder = -50;

        // Penjaga mulai mengejar (memanggil penjaga lain).
        public static event Action<Guard, Vector2> Alerted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Alerted = null;

        private struct Sight
        {
            public bool valid, cone, near, light, smell;
            public float rate;
            public Vector2 pos;
            public SlimePiece piece;
            public FriendSlime friend;
            public Decoy decoy;
        }

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
        private bool alarmed, longSearch;
        private Sight chase;
        private float loseTimer;
        private HidingSpot inspectSpot;
        private bool inspectKnown, inspectArrived;
        private readonly Dictionary<HidingSpot, float> inspectedAt = new Dictionary<HidingSpot, float>();
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

        public float Suspicion { get; private set; }
        public bool SeesTarget { get; private set; }
        public bool Senses { get; private set; }
        // Slime terlihat karena berdiri di cahaya (bukan karena kerucut/radius).
        public bool InLight { get; private set; }
        // Slime tercium (penjaga pencium).
        public bool Smells { get; private set; }
        public string GuardName => guardName;
        // Untuk tool/test QA (cek level): setelan patroli dan penglihatan.
        public IReadOnlyList<Vector2> PatrolPoints => patrolPoints;

        // Ganti rute patroli (pergantian shift jaga). Mulai dari titik pertama rute baru.
        public void SetPatrol(List<Vector2> points)
        {
            if (points == null || points.Count == 0) return;
            patrolPoints = new List<Vector2>(points);
            pointIndex = 0;
            if (state == State.Wait) state = State.Patrol;
        }
        public float ViewRange => viewRange;
        public float ViewAngle => viewAngle;
        public float AwarenessRadius => awarenessRadius;
        public float EyeHeight => eyeHeight;
        public bool NeedsLightToSee => needsLightToSee;
        public float LightSightRange => lightSightRange;
        public float LitThreshold => litThreshold;
        // Untuk indikator di atas kepala: mencari/memeriksa ("?") atau melihat/mengejar ("!").
        public bool IsSearching => state == State.Search || state == State.LookAround || state == State.Inspect;
        public bool IsAlerted => state == State.Alert || state == State.Chase || state == State.Caught;
        public bool IsChasing => state == State.Chase;
        public float HeadHeight => eyeHeight + 0.55f;
        private float trackedTime = -999f, sniffTimer;
        private bool alertedSound;
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
            if (brain != null) brain.Transformed += OnTransformed;
            if (coneMaterial != null) cone = VisionCone.Create("VisionCone_" + name, coneMaterial, coneSortingOrder);
            if (patrolPoints.Count > 1)
            {
                var d = patrolPoints[1] - patrolPoints[0];
                if (d.sqrMagnitude > 1e-4f) forward = d.normalized;
            }
            else if (facing.sqrMagnitude > 1e-4f) forward = facing.normalized;
        }

        private void OnEnable()
        {
            NoiseBus.Noise += OnNoise;
            Alerted += OnAlerted;
            Jar.Alarm += OnJarAlarm;
        }

        private void OnDisable()
        {
            NoiseBus.Noise -= OnNoise;
            Alerted -= OnAlerted;
            Jar.Alarm -= OnJarAlarm;
        }

        private void OnDestroy()
        {
            if (brain != null) brain.Transformed -= OnTransformed;
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

        // Jarak tembus pandang ke arah `delta` (berhenti di dinding/prop). `ignore` = collider yang dianggap tembus.
        private float Clip(Vector2 from, Vector2 delta) => Clip(from, delta, null);

        private float Clip(Vector2 from, Vector2 delta, Collider2D ignore)
        {
            float best = delta.magnitude;
            foreach (var h in Physics2D.RaycastAll(from, delta.normalized, best))
            {
                // jarak 0 = titik mata ada DI DALAM collider (musuh tinggi di lorong sempit): jangan dianggap terhalang
                if (h.distance <= 1e-3f) continue;
                if (h.collider != ignore && !IsSeeThrough(h.collider)) best = Mathf.Min(best, h.distance);
            }
            return best;
        }

        private bool LineOfSight(Vector2 from, Vector2 to, Collider2D ignore = null) =>
            !MistCloud.Blocks(from, to) && Clip(from, to - from, ignore) >= Vector2.Distance(from, to) - 0.05f;

        // ---- indra ----

        private Sight Evaluate()
        {
            var best = default(Sight);
            if (brain != null)
                foreach (var p in brain.Pieces)
                {
                    if (p == null || p.IsHidden) continue;
                    var s = SeePiece(p);
                    if (s.valid && Better(s, best)) best = s;
                }
            foreach (var f in FriendSlime.All)
            {
                if (f == null || !f.IsExposed) continue;
                var s = SeeFriend(f);
                if (s.valid && Better(s, best)) best = s;
            }
            // Umpan Kristal: berkilau, terlihat seperti slime yang menyala
            foreach (var d in Decoy.All)
            {
                if (d == null) continue;
                var s = Sense(d.Center, viewRange * glowRangeMultiplier, true, true, 0f);
                s.decoy = d;
                if (s.valid && Better(s, best)) best = s;
            }
            return best;
        }

        private static bool Better(Sight a, Sight b) => !b.valid || a.rate > b.rate;

        private Sight SeePiece(SlimePiece p)
        {
            var st = p.Body.State;
            bool hot = st.EmitsLight || st.Get(TraitKind.Temperature) == "panas";
            // tubuh besar terlihat lebih jauh, tubuh kecil/gelap (Esens Bayang) lebih dekat (desain-v2 bagian 5)
            float range = viewRange * (st.EmitsLight ? glowRangeMultiplier : 1f) * st.SightMultiplier;
            if (heatSightRange > 0f && hot) range = Mathf.Max(range, heatSightRange);
            var s = Sense(p.Center, range, hot, st.EmitsLight, st.SmellRadius);
            s.piece = p;
            return s;
        }

        private Sight SeeFriend(FriendSlime f)
        {
            float range = viewRange * SlimeBodyState.MassSight(f.Recipe != null ? f.Recipe.Size : 1);
            var s = Sense(f.Center, range, false, false, 0f);
            s.friend = f;
            return s;
        }

        private Sight Sense(Vector2 c, float range, bool hot, bool glows, float smell)
        {
            var s = new Sight { pos = c };
            float distEye = Vector2.Distance(Eye, c);
            // kerucut pandang (Familiar: buta di gelap kecuali yang panas/menyala)
            if (VisionMath.InCone(Eye, forward, c, range, viewAngle)
                && (!needsLightToSee || hot || LightExposure.At(c) >= litThreshold) && LineOfSight(Eye, c))
            {
                s.valid = s.cone = true;
                s.rate = VisionMath.DistanceFactor(distEye, range);
                return s;
            }
            // radius sadar (termasuk belakang)
            if (Vector2.Distance(Body, c) <= awarenessRadius && LineOfSight(Body, c))
            {
                s.valid = s.near = true;
                s.rate = awarenessRate;
                return s;
            }
            // cahaya = bahaya: terlihat dari segala arah dalam jarak tertentu
            if (distEye <= lightSightRange && (glows || LightExposure.At(c) >= litThreshold) && LineOfSight(Eye, c))
            {
                s.valid = s.light = true;
                s.rate = lightRate;
                return s;
            }
            // penciuman (tidak butuh garis pandang atau cahaya)
            if (smellRange > 0f && smell > 0f && Vector2.Distance(Body, c) <= (smellRange + smell) * 0.5f)
            {
                s.valid = s.smell = true;
                s.rate = smellRate;
            }
            return s;
        }

        // Titik ini terlihat (kerucut/radius sadar/cahaya)? Untuk transformasi yang terlihat.
        private bool SeesPoint(Vector2 c)
        {
            if (VisionMath.InCone(Eye, forward, c, viewRange, viewAngle)
                && (!needsLightToSee || LightExposure.At(c) >= litThreshold) && LineOfSight(Eye, c)) return true;
            if (Vector2.Distance(Body, c) <= awarenessRadius && LineOfSight(Body, c)) return true;
            return Vector2.Distance(Eye, c) <= lightSightRange && LightExposure.At(c) >= litThreshold && LineOfSight(Eye, c);
        }

        // ---- kejadian ----

        // Suara (tuas, pintu, langkah berat, Dentum): datangi dan periksa, curiga tidak naik.
        private void OnNoise(Vector2 position, float radius)
        {
            if (state == State.Caught || state == State.Chase || state == State.Inspect || state == State.Alert) return;
            if (Vector2.Distance(Body, position) > radius * hearingMultiplier) return;
            lastSeen = position;
            state = State.Search;
        }

        // Penjaga lain berteriak: datang ke tempat itu.
        private void OnAlerted(Guard caller, Vector2 pos)
        {
            if (caller == this || state == State.Chase || state == State.Caught || state == State.Inspect) return;
            float reach = Mathf.Max(callRadius, caller.callRadius);   // alarm lukisan terdengar lebih jauh dari teriakan
            if (Vector2.Distance(Body, pos) > reach && Vector2.Distance(Body, caller.Body) > reach) return;
            lastSeen = pos;
            Suspicion = Mathf.Max(Suspicion, Mathf.Min(0.9f, searchThreshold + 0.3f));
            state = State.Search;
        }

        // Alarm penyadapan: semua penjaga lebih cepat curiga dan mendatangi toples.
        private void OnJarAlarm(Jar jar)
        {
            alarmed = true;
            if (jar == null || state == State.Chase || state == State.Caught || state == State.Inspect) return;
            lastSeen = jar.transform.position;
            Suspicion = Mathf.Max(Suspicion, searchThreshold);
            state = State.Search;
        }

        // Transformasi (belah, gabung, Shift, masuk/keluar tempat sembunyi). Terlihat = curiga melonjak.
        private void OnTransformed(Vector2 pos, HidingSpot spot, bool entering)
        {
            if (state == State.Caught || (goal != null && (goal.IsComplete || goal.IsFailed))) return;
            if (!SeesPoint(pos)) return;
            if (spot != null && entering && !spot.IsSafe)
            {
                BeginInspect(spot, true);   // terlihat masuk: penjaga tahu persis di mana
                return;
            }
            Suspicion = Mathf.Min(1f, Suspicion + transformSuspicion);
            lastSeen = pos;
            if (!alertedSound) { alertedSound = true; AudioDirector.Play(AudioDirector.Sfx.Alert, Body); }
            if (state == State.Chase || state == State.Inspect) return;
            state = State.Alert;
            if (Suspicion >= 1f) StartChase(new Sight { valid = true, pos = pos, piece = brain != null ? brain.Active : null });
        }

        // ---- loop ----

        private void Update()
        {
            moving = false;
            if (state == State.Caught || (goal != null && (goal.IsComplete || goal.IsFailed)))
            {
                UpdateVisuals();
                return;
            }

            var sight = Evaluate();
            SeesTarget = sight.valid && sight.cone;
            Senses = sight.valid && !sight.cone;
            InLight = sight.valid && sight.light;
            Smells = sight.valid && sight.smell;

            if (state == State.Chase) { ChaseStep(sight); UpdateVisuals(); return; }
            WatchHidingSpots();
            if (state == State.Inspect) { InspectStep(); UpdateVisuals(); return; }

            if (sight.valid)
            {
                float rate = sight.rate * VisionMath.MismatchMultiplier(TotalMismatch(), mismatchBonus);
                // bunyi "hm?" sekali tiap kali mulai curiga (diulang lagi setelah curiga turun ke 0)
                if (!alertedSound) { alertedSound = true; AudioDirector.Play(AudioDirector.Sfx.Alert, Body); }
                float fill = fillTime * (alarmed ? alarmFillMultiplier : 1f);
                Suspicion = Mathf.Min(1f, Suspicion + Time.deltaTime * rate / Mathf.Max(0.05f, fill));
                lastSeen = sight.pos;
                state = State.Alert;
                if (Suspicion >= 1f)
                {
                    StartChase(sight);
                    UpdateVisuals();
                    return;
                }
            }
            else
            {
                float decay = state == State.Search || state == State.LookAround ? decayPerSecond * 0.4f : decayPerSecond;
                Suspicion = Mathf.Max(0f, Suspicion - decay * Time.deltaTime);
                if (Suspicion <= 0f) alertedSound = false;
                // pencium menyusuri jejak bau (dari mana pun asalnya: pemain, potongan, lumpur)
                if (Sniff()) state = State.Search;
            }

            switch (state)
            {
                case State.Alert:
                    // menoleh cepat ke arah slime
                    TurnTowards(lastSeen - Eye, turnSpeed * 1.6f);
                    if (!sight.valid)
                    {
                        if (Suspicion >= searchThreshold) state = State.Search;
                        else { state = State.LookAround; timer = lookAroundTime * 0.5f; lookBase = forward; }
                    }
                    break;

                case State.Search:
                    // datangi tempat terakhir slime terlihat
                    var toSeen = lastSeen - Body;
                    if (toSeen.magnitude < searchStopDistance || !TryMove(lastSeen - FootToBody, speed * searchSpeedMultiplier * (alarmed ? 1.15f : 1f)))
                    {
                        state = State.LookAround;
                        timer = lookAroundTime * (longSearch ? 1.6f : 1f);
                        lookBase = forward;
                    }
                    break;

                case State.LookAround:
                    // diam sambil menyapu pandangan kiri-kanan
                    timer -= Time.deltaTime;
                    float total = lookAroundTime * (longSearch ? 1.6f : 1f);
                    float phase = (total - timer) / total * Mathf.PI * 2f;
                    float baseA = Mathf.Atan2(lookBase.y, lookBase.x) * Mathf.Rad2Deg;
                    float want = baseA + Mathf.Sin(phase) * lookAroundAngle;
                    TurnTowards(new Vector2(Mathf.Cos(want * Mathf.Deg2Rad), Mathf.Sin(want * Mathf.Deg2Rad)), turnSpeed);
                    if (timer <= 0f) { state = State.Patrol; longSearch = false; }
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
                    if (inspectsHidingSpots && TryRoutineInspect()) break;
                    if (inspectsJars) InspectJars();
                    if (huntInterval > 0f && Hunt()) break;
                    var to = patrolPoints[pointIndex] - rb.position;
                    if (to.magnitude < 0.05f)
                    {
                        state = State.Wait;
                        timer = waitAtPoint;
                        break;
                    }
                    TryMove(patrolPoints[pointIndex], speed);
                    break;
            }
            // bertugas di tempat: pandangan menyapu kiri-kanan
            if (sweepAngle > 0f && (state == State.Wait || (state == State.Patrol && !moving)))
            {
                float baseA = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg + Mathf.Sin(Time.time * sweepSpeed * Mathf.PI) * sweepAngle;
                TurnTowards(new Vector2(Mathf.Cos(baseA * Mathf.Deg2Rad), Mathf.Sin(baseA * Mathf.Deg2Rad)), turnSpeed);
            }
            UpdateVisuals();
        }

        // Mata Penyihir: bongkar tiruan beraura salah di dekatnya.
        private void InspectJars()
        {
            if (jars == null) return;
            foreach (var j in jars)
                if (j != null && j.HasDecoy && !j.AlarmRaised && j.DecoyMismatch > 0 && Vector2.Distance(Body, j.transform.position) <= inspectRadius + 0.6f)
                    j.RaiseAlarm();
        }

        // Pemburu: berkala mendatangi perkiraan posisi pemain (tidak tepat), lalu menyapu pandangan di sana.
        private float huntTimer = -1f;
        private bool Hunt()
        {
            if (huntTimer < 0f) huntTimer = huntInterval;
            if ((huntTimer -= Time.deltaTime) > 0f) return false;
            huntTimer = huntInterval;
            var a = brain != null ? brain.Active : null;
            if (a == null) return false;
            var guess = a.Center + UnityEngine.Random.insideUnitCircle * huntError;
            var cell = NavGrid.CellOf(guess);
            if (NavGrid.Available && !NavGrid.Walkable(cell)) guess = a.Center;
            lastSeen = guess;
            state = State.Search;
            return true;
        }

        // ---- mengejar ----

        private float stationaryAlarmAt = -99f;

        private void StartChase(Sight s)
        {
            if (stationary)
            {
                // tidak bisa mengejar: Monster Gerbang menangkap dengan sorotnya, Lukisan membunyikan alarm
                if (stationaryCatches) { Catch(s); return; }
                state = State.Alert;
                lastSeen = s.pos;
                if (Time.time - stationaryAlarmAt < 4f) return;
                stationaryAlarmAt = Time.time;
                AudioDirector.Play(AudioDirector.Sfx.Caught, Body, 0.8f, 0.1f);
                Alerted?.Invoke(this, s.pos);
                return;
            }
            state = State.Chase;
            chase = s;
            loseTimer = 0f;
            Suspicion = 1f;
            lastSeen = s.pos;
            AudioDirector.Play(AudioDirector.Sfx.Alert, Body, 1f);
            AudioDirector.Play(AudioDirector.Sfx.Caught, Body, 0.35f, 0.1f);   // teriakan memanggil penjaga lain
            Alerted?.Invoke(this, lastSeen);
        }

        // Posisi target sekarang kalau masih di luar (belum sembunyi / selamat).
        private bool TargetPosition(out Vector2 pos, out float radius)
        {
            if (chase.piece != null && !chase.piece.IsHidden) { pos = chase.piece.Center; radius = chase.piece.Radius; return true; }
            if (chase.friend != null && chase.friend.IsExposed) { pos = chase.friend.Center; radius = 0.25f * chase.friend.transform.localScale.x; return true; }
            if (chase.decoy != null) { pos = chase.decoy.Center; radius = 0.2f; return true; }
            pos = lastSeen;
            radius = 0f;
            return false;
        }

        private void ChaseStep(Sight sight)
        {
            if (sight.valid)
            {
                // target yang sama, atau target lain kalau target lama sudah lolos dari pandangan
                bool same = (chase.piece != null && sight.piece == chase.piece) || (chase.friend != null && sight.friend == chase.friend)
                         || (chase.decoy != null && sight.decoy == chase.decoy);
                if (same || loseTimer > 0.3f)
                {
                    chase = sight;
                    lastSeen = sight.pos;
                    loseTimer = 0f;
                }
            }
            else loseTimer += Time.deltaTime;

            bool exposed = TargetPosition(out var tp, out float tr);
            if (exposed && Vector2.Distance(Body, tp) <= catchDistance + tr + 0.28f) { Catch(chase); return; }

            // target bersembunyi tepat di depan mata (tidak sempat lolos): periksa tempat itu
            if (!exposed && chase.piece != null && chase.piece.HidingIn != null && !chase.piece.HidingIn.IsSafe && loseTimer < 0.6f)
            {
                BeginInspect(chase.piece.HidingIn, true);
                return;
            }

            if (loseTimer >= loseSightTime)
            {
                // lolos: cari di tempat terakhir terlihat, lebih lama dari biasa
                state = State.Search;
                Suspicion = 0.95f;
                longSearch = true;
                return;
            }
            Vector2 dest = exposed && loseTimer <= 0f ? tp : lastSeen;
            if (!TryMove(dest - FootToBody, speed * chaseSpeedMultiplier) || (dest - Body).magnitude < 0.25f)
                loseTimer += Time.deltaTime;   // sampai di titik terakhir tanpa melihat apa-apa: lebih cepat menyerah
        }

        private void Catch(Sight s)
        {
            if (s.decoy != null)
            {
                // umpan kristal pecah: penjaga bingung, menyapu pandangan, lalu kembali patroli
                s.decoy.Pop();
                chase = default;
                Suspicion = 0.5f;
                state = State.LookAround;
                timer = lookAroundTime;
                lookBase = forward;
                return;
            }
            state = State.Caught;
            AudioDirector.Play(AudioDirector.Sfx.Caught, Body);
            string who = s.friend != null ? s.friend.Name + " was caught by " : "Caught by ";
            if (goal != null) goal.Fail(who + guardName);
            UpdateVisuals();
        }

        // ---- tempat sembunyi ----

        // Barang berisi slime yang ditatap dari dekat makin mencurigakan. Penuh = periksa.
        private void WatchHidingSpots()
        {
            if (state == State.Inspect || state == State.Chase) return;
            foreach (var spot in HidingSpot.All)
            {
                if (spot == null || !spot.Occupied || spot.IsSafe) continue;
                Vector2 c = spot.Center;
                bool staring = VisionMath.InCone(Eye, forward, c, stareRange, viewAngle) && LineOfSight(Eye, c, spot.Collider);
                if (!staring && Vector2.Distance(Body, c) <= awarenessRadius) staring = LineOfSight(Body, c, spot.Collider);
                if (staring && needsLightToSee && LightExposure.At(c) < litThreshold) staring = false;
                if (!staring) continue;
                spot.Stare(Time.deltaTime, alarmed ? 1f / alarmFillMultiplier : 1f);
                if (spot.Suspicion >= 1f) { BeginInspect(spot, false); return; }
            }
        }

        // Pemeriksa: berhenti dan memeriksa tempat sembunyi di dekat jalurnya walau tidak curiga.
        private bool TryRoutineInspect()
        {
            foreach (var spot in HidingSpot.All)
            {
                if (spot == null || spot.IsSafe || Vector2.Distance(Body, spot.Center) > inspectRadius) continue;
                if (inspectedAt.TryGetValue(spot, out var t) && Time.time - t < reinspectDelay) continue;
                BeginInspect(spot, false);
                return true;
            }
            return false;
        }

        private void BeginInspect(HidingSpot spot, bool known)
        {
            if (spot == null) return;
            state = State.Inspect;
            inspectSpot = spot;
            inspectKnown = known;
            inspectArrived = false;
            timer = inspectTime;
            lastSeen = spot.Center;
            if (known) { Suspicion = 1f; AudioDirector.Play(AudioDirector.Sfx.Alert, Body, 1f); }
        }

        private void InspectStep()
        {
            var spot = inspectSpot;
            if (spot == null) { state = State.LookAround; timer = lookAroundTime; lookBase = forward; return; }
            Vector2 front = spot.ExitPoint(rb.position, 0.3f);
            if (!inspectArrived)
            {
                bool far = (front - rb.position).magnitude > 0.3f && (spot.Center - Body).magnitude > inspectRadius * 0.8f;
                if (far && TryMove(front, speed * (inspectKnown ? chaseSpeedMultiplier : searchSpeedMultiplier))) return;
                inspectArrived = true;
            }
            TurnTowards(spot.Center - Eye, turnSpeed * 1.5f);
            if ((timer -= Time.deltaTime) > 0f) return;
            inspectedAt[spot] = Time.time;
            if (spot.Occupied)
            {
                // isi pertama yang ditemukan tertangkap (pemain/potongan, atau teman)
                var s = new Sight { valid = true, pos = spot.Center };
                if (spot.Pieces.Count > 0) s.piece = spot.Pieces[0]; else s.friend = spot.Friends[0];
                Catch(s);
                return;
            }
            spot.ClearSuspicion();
            state = State.LookAround;
            timer = lookAroundTime * 0.6f;
            lookBase = forward;
        }

        // Ikuti jejak: pindah ke titik bau yang lebih baru dari yang terakhir diikuti.
        private bool Sniff()
        {
            if (smellRange <= 0f) return false;
            if ((sniffTimer -= Time.deltaTime) > 0f) return false;
            sniffTimer = 0.3f;
            // Dengan peta jalan, pencium langsung menuju titik bau terbaru (lewat jalur A*).
            // Tanpa peta (sandbox): hanya titik yang terjangkau lurus.
            Func<Vector2, bool> reach = NavGrid.Available ? null : p => LineOfSight(Body, p);
            if (!ScentTrail.Freshest(Body, smellRange, trackedTime, out var m, reach)) return false;
            trackedTime = m.time;
            lastSeen = m.position;
            return true;
        }

        // ---- jalan: lurus kalau bebas, kalau tidak ikuti jalur A* (NavGrid) ----
        private readonly List<Vector2> path = new List<Vector2>();   // titik tengah BADAN (bukan kaki)
        private Vector2 pathGoal;
        private float repathTimer;
        private static readonly Vector2 FootToBody = Vector2.up * 0.22f;

        // Seluruh garis dari badan ke titik `to` bebas tembok untuk badan selebar ini?
        private bool SegmentClear(Vector2 from, Vector2 to)
        {
            var d = to - from;
            float len = d.magnitude;
            if (len < 1e-3f) return true;
            foreach (var h in Physics2D.CircleCastAll(from, 0.28f, d / len, len))
                if (h.distance > 0f && !IsSeeThrough(h.collider)) return false;
            return true;
        }

        // Menoleh ke tujuan lalu jalan; melambat saat masih berbelok. false = tidak ada jalan ke sana.
        private bool TryMove(Vector2 dest, float spd)
        {
            if (stationary) { TurnTowards(dest + FootToBody - Eye, turnSpeed); return false; }
            var bodyDest = dest + FootToBody;
            var d = dest - rb.position;
            if (d.sqrMagnitude < 1e-4f) return true;
            repathTimer -= Time.deltaTime;

            Vector2 waypoint = dest;
            if (!SegmentClear(Body, bodyDest) && NavGrid.Available)
            {
                if (path.Count == 0 || (dest - pathGoal).sqrMagnitude > 0.25f || repathTimer <= 0f)
                {
                    if (!NavGrid.FindPath(Body, bodyDest, path)) return false;
                    pathGoal = dest;
                    repathTimer = 0.8f;
                }
                // haluskan: lompati titik berikutnya kalau titik sesudahnya sudah terlihat langsung
                while (path.Count > 1 && SegmentClear(Body, path[1])) path.RemoveAt(0);
                if (path.Count > 0 && (path[0] - Body).sqrMagnitude < 0.01f) path.RemoveAt(0);
                if (path.Count > 0) waypoint = path[0] - FootToBody;
                d = waypoint - rb.position;
                if (d.sqrMagnitude < 1e-4f) return true;
            }
            else path.Clear();

            float turn = state == State.Chase ? turnSpeed * 2.5f : turnSpeed;
            TurnTowards(d, turn);
            float facing = Mathf.Clamp01(Mathf.Cos(Vector2.Angle(forward, d) * Mathf.Deg2Rad));
            if (facing < 0.3f) return true;   // masih berbelok di tempat
            float step = spd * facing * Time.deltaTime;
            if (PathClear(d.normalized, step))
            {
                moveTarget = waypoint;
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

        private void LateUpdate()
        {
            if (hoverHeight > 0f) Hover();
            // langkah (golem). Penjaga melayang: stepInterval 0 = tanpa langkah
            if (!moving || stepInterval <= 0f) { stepTimer = 0f; return; }
            if ((stepTimer -= Time.deltaTime) <= 0f)
            {
                stepTimer = stepInterval * (state == State.Chase ? 0.6f : 1f);
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

using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Teman yang sudah keluar dari toples (desain-v2 bagian 6).
    //   Menunggu  : gemetar ketakutan di tempat. Tetap bisa terlihat penjaga.
    //   Mengikuti : menyusuri JEJAK pemain persis (tidak memotong sudut), sedikit lebih lambat, berhenti saat pemain diam.
    //   Sembunyi  : ikut masuk tempat sembunyi bersama pemain (HidingSpot).
    //   Selamat   : masuk ventilasi / exit, hilang dari level.
    // Dilihat penjaga sama seperti pemain: curiga, dikejar, tertangkap = ulang dari checkpoint.
    [RequireComponent(typeof(Collider2D))]
    public class FriendSlime : MonoBehaviour
    {
        public enum Mode { Waiting, Following, Hidden, Saved }

        [SerializeField] private RecipeData recipe;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("Jarak di belakang pemain, diukur sepanjang jejak (unit).")]
        [SerializeField] private float followGap = 0.75f;
        [Tooltip("Kecepatan teman dibanding kecepatan pemain saat ini (sedikit lebih lambat).")]
        [SerializeField, Range(0.5f, 1.2f)] private float speedShare = 0.9f;
        [Tooltip("Jarak panggil (F) untuk mengajak teman yang sedang menunggu.")]
        [SerializeField] private float callRadius = 2.5f;

        // Semua teman di level (dipakai penjaga dan HUD).
        public static readonly List<FriendSlime> All = new List<FriendSlime>();

        private readonly List<Vector2> trail = new List<Vector2>();   // titik pivot pemain, lama -> baru
        private Transform visual;
        private Vector3 visualBase;
        private float bob, saveTimer = -1f;
        private bool moved;

        public RecipeData Recipe => recipe;
        public Mode State { get; private set; } = Mode.Waiting;
        public SlimePiece Leader { get; private set; }
        public string Name => recipe != null ? recipe.FriendName : "Friend";
        // Pusat badan (titik yang dilihat penjaga), sama seperti SlimePiece.Center.
        public Vector2 Center => (Vector2)transform.position + Vector2.up * 0.22f * transform.localScale.y;
        // Terlihat penjaga kalau sedang di luar (menunggu atau mengikuti).
        public bool IsExposed => State == Mode.Waiting || State == Mode.Following;
        public bool IsMoving => moved;
        public float CallRadius => callRadius;

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;   // trigger ventilasi/exit butuh Rigidbody2D
            rb.useFullKinematicContacts = true;         // kinematic tanpa ini tidak menyentuh trigger statis (exit)
            rb.gravityScale = 0f;
            if (bodyRenderer != null)
            {
                visual = bodyRenderer.transform.parent != transform && bodyRenderer.transform.parent != null
                    ? bodyRenderer.transform.parent : bodyRenderer.transform;
                visualBase = visual.localPosition;
            }
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        public void Init(RecipeData data)
        {
            recipe = data;
            if (bodyRenderer != null && data != null && data.BodyMaterial != null)
                bodyRenderer.sharedMaterial = data.BodyMaterial;
            // ukuran teman = ukuran slime bermassa sama (prefab = ukuran massa 1), sama dengan skala toplesnya
            if (data != null) transform.localScale = Vector3.one * SlimeBodyState.MassScale(data.Size) / SlimeBodyState.MassScale(1);
        }

        public bool CanHear(SlimePiece p) => p != null && Vector2.Distance(p.Center, Center) <= callRadius;

        // Ikuti potongan ini. Kalau ada tembok di antara, jejak awal dibuat dari jalur A* (NavGrid).
        public void Follow(SlimePiece leader)
        {
            if (leader == null || State == Mode.Saved) return;
            if (Leader != leader || State != Mode.Following) followSince = Time.time;
            Leader = leader;
            State = Mode.Following;
            trail.Clear();
            Vector2 from = transform.position, to = leader.transform.position;
            var lift = Vector2.up * 0.22f;
            var path = new List<Vector2>();
            if (NavGrid.Available && WallBetween(from + lift, to + lift) && NavGrid.FindPath(from + lift, to + lift, path))
                foreach (var p in path) trail.Add(p - lift);
            trail.Add(to);
            AudioDirector.Play(AudioDirector.Sfx.Reveal, Center, 0.5f);
        }

        // Tembok/prop padat di antara dua titik (slime, teman, penjaga, dan trigger diabaikan).
        private static bool WallBetween(Vector2 a, Vector2 b)
        {
            foreach (var h in Physics2D.LinecastAll(a, b))
            {
                var c = h.collider;
                if (c == null || c.isTrigger) continue;
                if (c.GetComponentInParent<SlimePiece>() != null || c.GetComponentInParent<Guard>() != null) continue;
                return true;
            }
            return false;
        }

        public void Wait()
        {
            if (State == Mode.Saved) return;
            State = Mode.Waiting;
            Leader = null;
            trail.Clear();
        }

        // Ikut sembunyi bersama pemain (HidingSpot). Posisi = di dalam/di samping tempat sembunyi.
        public void Hide(Vector2 at)
        {
            if (State == Mode.Saved) return;
            State = Mode.Hidden;
            trail.Clear();
            transform.position = at;
            SetVisible(false);
        }

        // Keluar dari tempat sembunyi, lanjut mengikuti potongan ini (atau menunggu).
        public void Unhide(SlimePiece leader, Vector2 at)
        {
            if (State != Mode.Hidden) return;
            transform.position = at;
            SetVisible(true);
            if (leader != null) Follow(leader); else Wait();
        }

        private void SetVisible(bool on)
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = on;
        }

        // Masuk ventilasi / exit: selamat.
        public void Save()
        {
            if (State == Mode.Saved) return;
            State = Mode.Saved;
            Leader = null;
            trail.Clear();
            saveTimer = 0.25f;
            AudioDirector.Play(AudioDirector.Sfx.Merge, Center, 0.7f);
            var goal = FindFirstObjectByType<LevelGoal>();
            if (goal != null) goal.NotifySaved(this);
        }

        private void Update()
        {
            moved = false;
            if (State == Mode.Saved)
            {
                // tersedot masuk ventilasi
                if (saveTimer > 0f)
                {
                    saveTimer -= Time.deltaTime;
                    transform.localScale *= Mathf.Max(0f, 1f - Time.deltaTime * 6f);
                    if (saveTimer <= 0f) gameObject.SetActive(false);
                }
                return;
            }
            if (State == Mode.Following) FollowStep();
            Animate();
        }

        // Pemain berdiri di exit/ventilasi: teman merapat sampai masuk (dipanggil tiap frame selama itu).
        private float closeInUntil;
        private float followSince;

        // Urutan dalam barisan: teman yang lebih dulu mengikuti pemimpin yang sama berjalan paling dekat.
        private int QueueIndex()
        {
            int k = 0;
            foreach (var f in All)
                if (f != null && f != this && f.State == Mode.Following && f.Leader == Leader && f.followSince < followSince) k++;
            return k;
        }
        public void CloseIn() => closeInUntil = Time.time + 0.25f;

        private void FollowStep()
        {
            if (Leader == null) { Wait(); return; }
            Vector2 lead = Leader.transform.position;
            if (trail.Count == 0 || Vector2.Distance(trail[trail.Count - 1], lead) > 0.06f) trail.Add(lead);

            float speed = Mathf.Max(1.2f, Leader.Motor.MaxSpeed * speedShare);
            float budget = speed * Time.deltaTime;
            float gap = Time.time < closeInUntil ? 0.02f : followGap * (1f + 0.95f * QueueIndex());
            Vector2 pos = transform.position;
            // jalan di jejak selama jarak (sepanjang jejak) ke pemain masih lebih dari gap
            while (budget > 0f && trail.Count > 0 && PathLength(pos) > gap)
            {
                var next = trail[0];
                float d = Vector2.Distance(pos, next);
                float slack = PathLength(pos) - gap;
                float step = Mathf.Min(budget, d, slack);
                if (d <= 1e-4f || step >= d - 1e-4f)
                {
                    pos = next;
                    trail.RemoveAt(0);
                    budget -= d;
                }
                else
                {
                    pos = Vector2.MoveTowards(pos, next, step);
                    budget -= step;
                }
                moved = true;
            }
            if (moved) transform.position = pos;
        }

        private float PathLength(Vector2 from)
        {
            if (Leader == null) return 0f;
            float len = 0f;
            var p = from;
            foreach (var q in trail) { len += Vector2.Distance(p, q); p = q; }
            return len + Vector2.Distance(p, Leader.transform.position);
        }

        // Melompat kecil saat berjalan, gemetar saat menunggu ketakutan.
        private void Animate()
        {
            if (visual == null || State == Mode.Hidden) return;
            if (moved)
            {
                bob += Time.deltaTime * 9f;
                visual.localPosition = visualBase + Vector3.up * (0.06f * Mathf.Abs(Mathf.Sin(bob)));
            }
            else if (State == Mode.Waiting)
            {
                visual.localPosition = visualBase + Vector3.right * (0.012f * Mathf.Sin(Time.time * 40f));
            }
            else visual.localPosition = Vector3.Lerp(visual.localPosition, visualBase, Time.deltaTime * 12f);
        }
    }
}

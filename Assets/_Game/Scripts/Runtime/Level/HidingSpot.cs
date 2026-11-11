using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Tempat sembunyi (desain-v2 bagian 7): tong, peti, toples kosong, lemari, semak, tudung saji, panci, tirai.
    //   C di dekatnya = masuk (±0.35 dtk, selama itu masih bisa terlihat). C lagi = keluar. Di dalam tidak bisa bergerak.
    //   Penjaga tidak melihat isinya. Tapi barang yang DITATAP penjaga dari dekat makin mencurigakan (bergoyang):
    //   saat penuh, penjaga datang memeriksa dan yang di dalam tertangkap. Berdua (dengan teman) = lebih goyang.
    //   Penjaga yang melihat slime masuk langsung memeriksa. Pemeriksa (Guard.inspectsHidingSpots) memeriksa walau tidak curiga.
    // Collider padat (barang menghalangi jalan dan pandangan); masuknya lewat tombol, bukan menabrak.
    [RequireComponent(typeof(Collider2D))]
    public class HidingSpot : MonoBehaviour
    {
        [Tooltip("Muat berapa slime sekaligus (2 = pemain + teman).")]
        [SerializeField, Range(1, 2)] private int capacity = 1;
        [Tooltip("Detik ditatap penjaga dari dekat sampai penjaga memeriksa.")]
        [SerializeField] private float stareFillTime = 3.5f;
        [Tooltip("Pengali laju curiga kalau berdua (barang lebih goyang).")]
        [SerializeField] private float crowdMultiplier = 1.6f;
        [SerializeField] private float decayPerSecond = 0.12f;
        [Tooltip("Bagian yang bergoyang saat curiga naik (sprite barang). Kosong = transform ini.")]
        [SerializeField] private Transform visual;

        public static readonly List<HidingSpot> All = new List<HidingSpot>();

        private readonly List<SlimePiece> pieces = new List<SlimePiece>();
        private readonly List<FriendSlime> friends = new List<FriendSlime>();
        private Collider2D col;
        private Vector3 visualBase;
        private float lastStare = -99f;

        public int Capacity => capacity;
        public int Count => pieces.Count + friends.Count;
        public bool Occupied => Count > 0;
        public bool HasRoom(int n = 1) => Count + n <= capacity;
        public float Suspicion { get; private set; }
        public IReadOnlyList<SlimePiece> Pieces => pieces;
        public IReadOnlyList<FriendSlime> Friends => friends;
        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        public Collider2D Collider => col;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            if (visual == null)
            {
                // anak bersprite yang bergoyang; root tidak ikut digeser (posisi dan collider tetap)
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.transform != transform) visual = sr.transform;
            }
            if (visual != null) visualBase = visual.localPosition;
        }

        // Barang padat: selnya diblok di peta jalan A* supaya penjaga memutar, bukan menabraknya.
        private Vector2Int cell;

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (col == null) col = GetComponent<Collider2D>();
            cell = NavGrid.CellOf(col != null ? (Vector2)col.bounds.center : (Vector2)transform.position);
            NavGrid.SetBlocked(cell, true);
        }

        private void OnDisable()
        {
            All.Remove(this);
            NavGrid.SetBlocked(cell, false);
        }

        public void Enter(SlimePiece p) { if (p != null && !pieces.Contains(p)) pieces.Add(p); }
        public void Leave(SlimePiece p) => pieces.Remove(p);
        public void Enter(FriendSlime f) { if (f != null && !friends.Contains(f)) friends.Add(f); }
        public void Leave(FriendSlime f) => friends.Remove(f);

        // Dipanggil penjaga tiap frame selama ia menatap barang ini dari dekat.
        public void Stare(float dt, float multiplier = 1f)
        {
            if (!Occupied) return;
            float rate = (Count > 1 ? crowdMultiplier : 1f) * multiplier / Mathf.Max(0.1f, stareFillTime);
            Suspicion = Mathf.Min(1f, Suspicion + dt * rate);
            lastStare = Time.time;
        }

        // Penjaga sudah memeriksa dan tidak menemukan apa-apa.
        public void ClearSuspicion() => Suspicion = 0f;

        // Titik keluar di sisi yang menghadap `from` (atau sisi lain kalau terhalang tembok).
        public Vector2 ExitPoint(Vector2 from, float bodyRadius = 0.25f)
        {
            Vector2 c = transform.position;   // pivot (kaki)
            float reach = (col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y) : 0.4f) + bodyRadius + 0.12f;
            var dirs = new List<Vector2> { Vector2.down, Vector2.left, Vector2.right, Vector2.up };
            var toward = from - c;
            if (toward.sqrMagnitude > 1e-4f)
                dirs.Sort((a, b) => Vector2.Dot(b, toward).CompareTo(Vector2.Dot(a, toward)));
            foreach (var d in dirs)
            {
                var p = c + d * reach;
                if (IsFree(p + Vector2.up * 0.22f, bodyRadius)) return p;
            }
            return c + dirs[0] * reach;
        }

        private bool IsFree(Vector2 center, float r)
        {
            foreach (var h in Physics2D.OverlapCircleAll(center, r * 0.9f))
            {
                if (h == null || h.isTrigger || h == col) continue;
                if (h.GetComponentInParent<SlimePiece>() != null || h.GetComponentInParent<FriendSlime>() != null) continue;
                return false;
            }
            return true;
        }

        private void Update()
        {
            if (Time.time - lastStare > 0.2f && Suspicion > 0f) Suspicion = Mathf.Max(0f, Suspicion - decayPerSecond * Time.deltaTime);
            if (!Occupied) Suspicion = 0f;
            // goyang makin keras saat curiga naik (umpan balik untuk pemain)
            if (visual == null) return;
            float amp = Occupied ? 0.004f + 0.03f * Suspicion : 0f;
            visual.localPosition = visualBase + Vector3.right * (amp * Mathf.Sin(Time.time * (18f + 22f * Suspicion)));
        }
    }
}

using UnityEngine;

namespace GooNapped
{
    // Gerak per tile (ala Pokemon): satu langkah = satu tile, tidak bisa berhenti di tengah.
    // Dipakai potongan slime; nanti bisa dipakai penjaga juga.
    public class GridMover : MonoBehaviour
    {
        [Tooltip("Detik per tile pada kecepatan normal. Slime: lengket dan lamban.")]
        [SerializeField] private float stepTime = 0.34f;
        [Tooltip("Layer yang menghalangi langkah (dinding, prop). Collider trigger tidak menghalangi.")]
        [SerializeField] private LayerMask obstacleMask = ~0;

        private readonly Collider2D[] hits = new Collider2D[8];
        private readonly RaycastHit2D[] rays = new RaycastHit2D[8];
        private Vector3 from, to;
        private float t = 1f;

        public Vector2Int Cell { get; private set; }
        public bool IsMoving => t < 1f;
        // 1 = normal, 0.6 = lambat (habis makan bahan berat).
        public float SpeedMultiplier { get; set; } = 1f;
        public float StepTime => stepTime / Mathf.Max(0.05f, SpeedMultiplier);

        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            SnapToGrid();
        }

        public void SnapToGrid() => Teleport(GridUtil.WorldToCell(transform.position));

        public void Teleport(Vector2Int cell)
        {
            Cell = cell;
            t = 1f;
            transform.position = GridUtil.CellToWorld(cell);
        }

        // Tile tujuan terhalang dinding/prop? (GridMover lain tidak dihitung: itu urusan pemanggil.)
        public bool IsBlocked(Vector2Int target)
        {
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = obstacleMask };
            int n = Physics2D.OverlapBox(GridUtil.CellCenter(target), Vector2.one * 0.6f, 0f, filter, hits);
            for (int i = 0; i < n; i++)
                if (Blocks(hits[i])) return true;
            // dinding tipis (edge collider) di antara dua tile
            n = Physics2D.Linecast(GridUtil.CellCenter(Cell), GridUtil.CellCenter(target), filter, rays);
            for (int i = 0; i < n; i++)
                if (Blocks(rays[i].collider)) return true;
            return false;
        }

        private bool Blocks(Collider2D c) => c != null && c.GetComponentInParent<GridMover>() == null;

        public bool TryStep(Vector2Int dir)
        {
            if (IsMoving || dir == Vector2Int.zero) return false;
            var target = Cell + dir;
            if (IsBlocked(target)) return false;
            from = transform.position;
            to = GridUtil.CellToWorld(target);
            Cell = target;          // tile tujuan langsung "dipesan"
            t = 0f;
            return true;
        }

        private void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / StepTime);
            float e = t * t * (3f - 2f * t);   // pelan - cepat - pelan: tarik badan lalu nempel lagi
            transform.position = Vector3.LerpUnclamped(from, to, e);
        }
    }
}

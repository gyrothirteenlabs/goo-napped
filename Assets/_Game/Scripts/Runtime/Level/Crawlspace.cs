using UnityEngine;

namespace GooNapped
{
    // Celah / lorong tikus satu tile: slime bisa menyelinap lewat (badan lunak), penjaga TIDAK (jalur penjaga memutar),
    // dan pandangan penjaga terhalang. Tubuh BERAT (Serbuk Batu) tidak muat: urutan makan jadi penting.
    // Cara kerja: collider padat yang diabaikan oleh collider potongan slime yang tidak berat.
    [RequireComponent(typeof(BoxCollider2D))]
    public class Crawlspace : MonoBehaviour
    {
        private BoxCollider2D box;
        private PlayerBrain brain;
        private Vector2Int cell;
        private float hintCooldown;

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            cell = NavGrid.CellOf(transform.TransformPoint(box.offset));
        }

        private void OnEnable() => NavGrid.SetBlocked(cell, true);
        private void OnDisable() => NavGrid.SetBlocked(cell, false);

        private void Start() => brain = FindFirstObjectByType<PlayerBrain>();

        public static bool Fits(SlimePiece p) => p.Body.State.Get(TraitKind.Weight) != "berat";

        private void FixedUpdate()
        {
            if (hintCooldown > 0f) hintCooldown -= Time.fixedDeltaTime;
            if (brain == null) return;
            foreach (var p in brain.Pieces)
            {
                if (p == null) continue;
                var c = p.Collider;
                if (c != null) Physics2D.IgnoreCollision(box, c, Fits(p));
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            var p = col.collider.GetComponentInParent<SlimePiece>();
            if (p == null || !p.IsControlled || hintCooldown > 0f || brain == null) return;
            brain.Notify("Tubuh terlalu berat untuk menyelinap lewat celah.");
            hintCooldown = 4f;
        }
    }
}

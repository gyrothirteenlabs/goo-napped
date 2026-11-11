using UnityEngine;

namespace GooNapped
{
    // Toples raja di puncak (adegan final). Tertutup sampai semua katup aura terbalik. Setelah terbuka, Alkemis (Guard
    // isBoss) yang menangkap sesuatu DI DALAM zona toples justru tersegel oleh Segel Raja: peran tertukar.
    // Trik pemain: belah, tinggalkan potongan di dalam toples sebagai umpan.
    [RequireComponent(typeof(Collider2D))]
    public class KingJar : MonoBehaviour
    {
        [Tooltip("Jari-jari zona toples (unit, dari pusat collider).")]
        [SerializeField] private float radius = 1.1f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public static KingJar Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsSealed { get; private set; }
        public Vector2 Center => GetComponent<Collider2D>().bounds.center;

        private void Awake()
        {
            Instance = this;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Apply();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool Contains(Vector2 p) => Vector2.Distance(p, Center) <= radius;

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            Apply();
            AudioDirector.Play(AudioDirector.Sfx.Merge, Center, 1f, 0.05f);
        }

        // Alkemis menerkam di dalam toples: tersegel.
        public void Seal(Guard boss)
        {
            if (IsSealed) return;
            IsSealed = true;
            Apply();
            var fs = FindFirstObjectByType<FinalSequence>();
            if (fs != null) fs.OnSealed(boss);
        }

        private void Apply()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.color = IsSealed ? new Color(0.55f, 0.35f, 0.75f) : IsOpen ? new Color(1f, 0.9f, 0.5f) : new Color(0.6f, 0.55f, 0.45f);
        }
    }
}

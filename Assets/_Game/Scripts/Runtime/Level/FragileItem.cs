using UnityEngine;

namespace GooNapped
{
    // Barang rapuh (vas, botol, piring; desain-v2 bagian 10). Tersenggol slime atau teman = jatuh dan pecah, bunyinya
    // memancing penjaga di sekitar. Bisa kecelakaan (lewat terlalu dekat) atau pengalih yang disengaja.
    // Butuh Collider2D (trigger) di sekitar barang.
    [RequireComponent(typeof(Collider2D))]
    public class FragileItem : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Sprite setelah pecah. Kosong = sprite lama digelapkan dan dimiringkan.")]
        [SerializeField] private Sprite brokenSprite;
        [Tooltip("Radius bunyi pecah (penjaga di dalamnya datang memeriksa).")]
        [SerializeField] private float noiseRadius = 4.5f;

        public bool IsBroken { get; private set; }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsBroken) return;
            var p = other.GetComponentInParent<SlimePiece>();
            if (p != null && (p.IsHidden || !p.IsControlled)) return;
            if (p == null)
            {
                var f = other.GetComponentInParent<FriendSlime>();
                if (f == null || !f.IsExposed) return;
            }
            Break();
        }

        public void Break(bool silent = false)
        {
            if (IsBroken) return;
            IsBroken = true;
            if (spriteRenderer != null)
            {
                if (brokenSprite != null) spriteRenderer.sprite = brokenSprite;
                else
                {
                    spriteRenderer.color = new Color(0.55f, 0.55f, 0.6f);
                    spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
                }
            }
            if (silent) return;
            AudioDirector.Play(AudioDirector.Sfx.Caught, transform.position, 0.6f, 0.25f);
            NoiseBus.Emit(transform.position, noiseRadius);
            var brain = FindFirstObjectByType<PlayerBrain>();
            if (brain != null) brain.Notify("Crash! Something broke. Guards heard it.");
        }
    }
}

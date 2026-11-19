using UnityEngine;

namespace GooNapped
{
    // Bahan yang tergeletak di level. Butuh Collider2D (trigger) supaya terdeteksi SlimeController.
    // Tampilan: sprite khas bahan (IngredientData.PickupSprite) yang melayang pelan, bayangan di lantai, dan lapisan
    // glow unlit untuk bahan yang menyala (Pendar, Bara) sehingga tetap terlihat dalam gelap. Tanpa sprite: bulatan
    // diwarnai PlaceholderTint (perilaku lama).
    [RequireComponent(typeof(Collider2D))]
    public class IngredientPickup : MonoBehaviour
    {
        [SerializeField] private IngredientData ingredient;
        [Tooltip("Renderer gambar bahan (anak Visual).")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Renderer bagian menyala (anak Glow, material unlit).")]
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float bobSpeed = 2.2f;

        private Sprite placeholder;
        private Vector3 visualBase, glowBase;
        private float phase;

        public IngredientData Ingredient => ingredient;

        // Untuk bahan yang dimuntahkan / dibuat saat runtime.
        public void Init(IngredientData data)
        {
            ingredient = data;
            ApplyLook();
        }

        // Mengganti sprite langsung di OnValidate memicu peringatan Unity (SendMessage), jadi ditunda satu tick editor.
        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyLook(); };
#endif
        }

        private void Awake()
        {
            if (spriteRenderer != null) visualBase = spriteRenderer.transform.localPosition;
            if (glowRenderer != null) glowBase = glowRenderer.transform.localPosition;
            phase = (transform.position.x * 1.7f + transform.position.y * 2.3f) % 6.28f;
            ApplyLook();
        }

        // Dipanggil juga oleh LevelBlockout setelah mengisi `ingredient`, supaya tampilan di scene langsung benar.
        public void ApplyLook()
        {
            if (spriteRenderer == null || ingredient == null) return;
            if (placeholder == null && spriteRenderer.sprite != null && ingredient.PickupSprite != spriteRenderer.sprite) placeholder = spriteRenderer.sprite;
            bool art = ingredient.PickupSprite != null;
            if (art) spriteRenderer.sprite = ingredient.PickupSprite;
            else if (placeholder != null) spriteRenderer.sprite = placeholder;
            spriteRenderer.color = art ? Color.white : ingredient.PlaceholderTint;
            if (glowRenderer != null)
            {
                glowRenderer.sprite = ingredient.PickupGlow;
                glowRenderer.enabled = art && ingredient.EmitsLight && ingredient.PickupGlow != null;
            }
        }

        private void Update()
        {
            if (spriteRenderer == null) return;
            float s = Mathf.Sin(Time.time * bobSpeed + phase);
            // naik-turun dibulatkan ke 1 pixel layar (1/48) supaya tetap terasa pixel art
            float y = Mathf.Round((s * 0.5f + 0.5f) * bobHeight * 48f) / 48f;
            spriteRenderer.transform.localPosition = visualBase + new Vector3(0f, y, 0f);
            if (glowRenderer != null && glowRenderer.enabled)
            {
                glowRenderer.transform.localPosition = glowBase + new Vector3(0f, y, 0f);
                var c = glowRenderer.color; c.a = 0.75f + 0.25f * Mathf.Sin(Time.time * 3.1f + phase); glowRenderer.color = c;
            }
        }

        // Dimakan: bahan hilang dari level, datanya dikembalikan ke pemakan.
        public IngredientData Consume()
        {
            var data = ingredient;
            Destroy(gameObject);
            return data;
        }
    }
}

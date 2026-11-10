using UnityEngine;

namespace GooNapped
{
    // Bahan yang tergeletak di level. Butuh Collider2D (trigger) supaya terdeteksi SlimeController.
    [RequireComponent(typeof(Collider2D))]
    public class IngredientPickup : MonoBehaviour
    {
        [SerializeField] private IngredientData ingredient;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public IngredientData Ingredient => ingredient;

        // Untuk bahan yang dimuntahkan / dibuat saat runtime.
        public void Init(IngredientData data)
        {
            ingredient = data;
            ApplyTint();
        }

        private void OnValidate() => ApplyTint();
        private void Awake() => ApplyTint();

        private void ApplyTint()
        {
            if (spriteRenderer != null && ingredient != null) spriteRenderer.color = ingredient.PlaceholderTint;
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

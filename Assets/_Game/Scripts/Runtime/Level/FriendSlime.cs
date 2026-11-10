using UnityEngine;

namespace GooNapped
{
    // Slime teman yang sudah keluar dari toples dan menunggu ditelan pemain. Butuh Collider2D (trigger).
    // Hanya untuk lore: tidak memberi skill.
    [RequireComponent(typeof(Collider2D))]
    public class FriendSlime : MonoBehaviour
    {
        [SerializeField] private RecipeData recipe;
        [SerializeField] private SpriteRenderer bodyRenderer;

        public RecipeData Recipe => recipe;

        public void Init(RecipeData data)
        {
            recipe = data;
            if (bodyRenderer != null && data != null && data.BodyMaterial != null)
                bodyRenderer.sharedMaterial = data.BodyMaterial;
        }

        // Ditelan: hilang dari level, datanya dibawa potongan yang menelan.
        public RecipeData Swallow()
        {
            var r = recipe;
            Destroy(gameObject);
            return r;
        }
    }
}

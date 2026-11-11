using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Data satu bahan. Aset disimpan di Assets/_Game/Data/Ingredients/.
    // Bahan memberi ciri ke tubuh (untuk resep) dan efek samping ke stealth (lambat, menyala, berbau).
    [CreateAssetMenu(menuName = "GooNapped/Bahan (Ingredient)", fileName = "Ingredient_Baru")]
    public class IngredientData : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";

        [Tooltip("Ciri yang diberikan ke tubuh saat dimakan. Ciri sejenis dari bahan sebelumnya tertimpa.")]
        [SerializeField] private List<Trait> traits = new List<Trait>();

        [Header("Efek ke stealth (selama bahan ini masih punya ciri di tubuh)")]
        [Tooltip("1 = normal, 0.6 = lambat (batu).")]
        [SerializeField, Range(0.1f, 2f)] private float speedMultiplier = 1f;
        [Tooltip("Tubuh menyala: gampang terlihat, dilihat Familiar Api.")]
        [SerializeField] private bool emitsLight;
        [Tooltip("Radius bau (unit). 0 = tidak berbau. Dicium Anjing Lendir.")]
        [SerializeField, Min(0f)] private float smellRadius;
        [Tooltip("Memadamkan: bahan yang sudah dimakan sebelumnya berhenti menyala (Garam Beku).")]
        [SerializeField] private bool extinguishes;
        [Tooltip("Radius suara tiap mendarat (unit). 0 = langkah senyap seperti biasa. Bahan berat = berisik, terdengar penjaga tembus tembok.")]
        [SerializeField, Min(0f)] private float stepNoiseRadius;
        [Tooltip("Pengali jarak pandang penjaga terhadap tubuh ini. 1 = normal, 0.7 = gelap/lebih sulit terlihat (Esens Bayang).")]
        [SerializeField, Range(0.3f, 2f)] private float sightMultiplier = 1f;

        [Header("Tampilan")]
        [Tooltip("Material palet body (Slime_*) saat bahan ini menentukan warna tubuh. Kosong = tidak mengubah.")]
        [SerializeField] private Material bodyMaterial;
        [Tooltip("Warna sprite pickup sementara, sebelum ada art bahan.")]
        [SerializeField] private Color placeholderTint = Color.white;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public IReadOnlyList<Trait> Traits => traits;
        public float SpeedMultiplier => speedMultiplier;
        public bool EmitsLight => emitsLight;
        public float SmellRadius => smellRadius;
        public bool Extinguishes => extinguishes;
        public float StepNoiseRadius => stepNoiseRadius;
        public float SightMultiplier => sightMultiplier;
        public Material BodyMaterial => bodyMaterial;
        public Color PlaceholderTint => placeholderTint;

        // Untuk test dan tool editor (aset biasa dibuat lewat menu Create).
        public static IngredientData Create(string id, IEnumerable<Trait> traits, float speedMultiplier = 1f,
            bool emitsLight = false, float smellRadius = 0f, Material bodyMaterial = null, Color? tint = null,
            bool extinguishes = false, float stepNoiseRadius = 0f, float sightMultiplier = 1f)
        {
            var d = CreateInstance<IngredientData>();
            d.name = id;
            d.id = id;
            d.displayName = id;
            d.traits = new List<Trait>(traits);
            d.speedMultiplier = speedMultiplier;
            d.emitsLight = emitsLight;
            d.smellRadius = smellRadius;
            d.bodyMaterial = bodyMaterial;
            d.placeholderTint = tint ?? Color.white;
            d.extinguishes = extinguishes;
            d.stepNoiseRadius = stepNoiseRadius;
            d.sightMultiplier = sightMultiplier;
            return d;
        }
    }
}

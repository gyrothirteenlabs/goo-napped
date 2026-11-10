using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // "Resep tiruan": ciri slime teman yang dikurung di satu toples (2-3 ciri).
    // Aset disimpan di Assets/_Game/Data/Recipes/.
    [CreateAssetMenu(menuName = "GooNapped/Resep teman (Recipe)", fileName = "Recipe_Baru")]
    public class RecipeData : ScriptableObject
    {
        [SerializeField] private string friendName = "";
        [Tooltip("Ciri yang harus dimiliki tiruan supaya tidak mencurigakan.")]
        [SerializeField] private List<Trait> traits = new List<Trait>();
        [Tooltip("Material palet body teman (Slime_*). Kosong = material prefab.")]
        [SerializeField] private Material bodyMaterial;

        public string FriendName => string.IsNullOrEmpty(friendName) ? name : friendName;
        public IReadOnlyList<Trait> Traits => traits;
        public Material BodyMaterial => bodyMaterial;

        // Untuk test dan tool editor.
        public static RecipeData Create(string friendName, IEnumerable<Trait> traits, Material bodyMaterial = null)
        {
            var r = CreateInstance<RecipeData>();
            r.name = friendName;
            r.friendName = friendName;
            r.traits = new List<Trait>(traits);
            r.bodyMaterial = bodyMaterial;
            return r;
        }
    }
}

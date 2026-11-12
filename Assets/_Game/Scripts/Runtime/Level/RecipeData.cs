using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Kekuatan teman yang auranya pulih (desain-v2 bagian 8). None = teman biasa.
    public enum FriendPower { None, Thump, Snuff, Decoy, Mist }

    // Data satu teman di toples (nama kelas tetap "RecipeData" supaya aset lama tidak putus).
    // v2: tiruan harus punya AURA teman (ciri Warna) dan MASSA = ukuran teman (desain-v2 bagian 5).
    // Aset v1 (Recipe_*) memakai 2-3 ciri dan tetap jalan di level sandbox v1.
    // Aset disimpan di Assets/_Game/Data/Recipes/ (v2: Friend_*).
    [CreateAssetMenu(menuName = "GooNapped/Teman (Friend)", fileName = "Friend_Baru")]
    public class RecipeData : ScriptableObject
    {
        [SerializeField] private string friendName = "";
        [Tooltip("v2: cukup 1 ciri Warna (= aura teman). v1: 2-3 ciri.")]
        [SerializeField] private List<Trait> traits = new List<Trait>();
        [Tooltip("Ukuran teman = massa tiruan yang dibutuhkan. 1 = kecil (anak, sahabat), 2 = besar (orang tua, tetua).")]
        [SerializeField, Range(1, 3)] private int size = 1;
        [Tooltip("Kekuatan yang bisa dipakai dari ventilasi setelah teman ini diselamatkan (desain-v2 bagian 8).")]
        [SerializeField] private FriendPower power = FriendPower.None;
        [Tooltip("Kalimat pertama teman saat pemain mendekati toplesnya (bahasa Inggris). Kosong = tanpa dialog.")]
        [SerializeField, TextArea] private string jarLine = "";
        [Tooltip("Kalimat teman di markas (bahasa Inggris), bergantian tiap diajak bicara. Placeholder sampai tim menulis cerita.")]
        [SerializeField, TextArea] private string[] hubLines = new string[0];
        [Tooltip("Material palet body teman (Slime_*). Kosong = material prefab.")]
        [SerializeField] private Material bodyMaterial;

        public string FriendName => string.IsNullOrEmpty(friendName) ? name : friendName;
        public IReadOnlyList<Trait> Traits => traits;
        public int Size => Mathf.Max(1, size);
        public FriendPower Power => power;
        public string JarLine => jarLine;
        public string[] HubLines => hubLines;
        public Material BodyMaterial => bodyMaterial;
        // Aura teman = nilai ciri Warna ("" kalau data v1 tanpa warna).
        public string Aura
        {
            get
            {
                foreach (var t in traits) if (t.kind == TraitKind.Color) return Trait.Normalize(t.value);
                return "";
            }
        }

        // Untuk test dan tool editor.
        public static RecipeData Create(string friendName, IEnumerable<Trait> traits, Material bodyMaterial = null, int size = 1,
            FriendPower power = FriendPower.None)
        {
            var r = CreateInstance<RecipeData>();
            r.name = friendName;
            r.friendName = friendName;
            r.traits = new List<Trait>(traits);
            r.bodyMaterial = bodyMaterial;
            r.size = size;
            r.power = power;
            return r;
        }
    }
}

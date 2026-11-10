using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // State tubuh slime (logika murni, tanpa MonoBehaviour, supaya bisa di-test dan disalin saat belah).
    // Aturan: tiap jenis ciri hanya punya satu nilai. Makan bahan menimpa ciri sejenis, jadi URUTAN makan penting.
    // Efek samping (lambat, menyala, bau) hanya berlaku selama bahannya masih "memiliki" minimal satu ciri di tubuh.
    public class SlimeBodyState
    {
        private readonly Dictionary<TraitKind, Trait> traits = new Dictionary<TraitKind, Trait>();
        private readonly Dictionary<TraitKind, IngredientData> owners = new Dictionary<TraitKind, IngredientData>();
        private readonly List<IngredientData> eaten = new List<IngredientData>();

        public IReadOnlyList<IngredientData> Eaten => eaten;
        public IEnumerable<Trait> Traits => traits.Values;
        public int TraitCount => traits.Count;

        public void Eat(IngredientData ingredient)
        {
            if (ingredient == null) return;
            eaten.Add(ingredient);
            foreach (var t in ingredient.Traits)
            {
                traits[t.kind] = t;
                owners[t.kind] = ingredient;
            }
        }

        public bool Has(TraitKind kind) => traits.ContainsKey(kind);

        // Nilai ciri, atau "" kalau tubuh masih netral untuk jenis itu.
        public string Get(TraitKind kind) => traits.TryGetValue(kind, out var t) ? Trait.Normalize(t.value) : "";

        public IngredientData OwnerOf(TraitKind kind) => owners.TryGetValue(kind, out var i) ? i : null;

        // Bahan yang masih punya ciri di tubuh (tanpa duplikat).
        public List<IngredientData> ActiveIngredients()
        {
            var list = new List<IngredientData>();
            foreach (var i in owners.Values)
                if (!list.Contains(i)) list.Add(i);
            return list;
        }

        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var i in ActiveIngredients()) m *= i.SpeedMultiplier;
                return m;
            }
        }

        public bool EmitsLight
        {
            get
            {
                foreach (var i in ActiveIngredients()) if (i.EmitsLight) return true;
                return false;
            }
        }

        public float SmellRadius
        {
            get
            {
                float r = 0f;
                foreach (var i in ActiveIngredients()) r = Mathf.Max(r, i.SmellRadius);
                return r;
            }
        }

        // Material palet body: dari bahan yang menentukan warna. null = pakai material default.
        public Material BodyMaterial
        {
            get
            {
                var i = OwnerOf(TraitKind.Color);
                return i != null ? i.BodyMaterial : null;
            }
        }

        // Berapa ciri resep yang TIDAK cocok dengan tubuh (0 = tiruan sempurna).
        public int MismatchCount(IEnumerable<Trait> recipe)
        {
            int miss = 0;
            foreach (var r in recipe)
                if (!traits.TryGetValue(r.kind, out var mine) || !mine.Equals(r)) miss++;
            return miss;
        }

        public bool Matches(IEnumerable<Trait> recipe) => MismatchCount(recipe) == 0;

        // Salinan lepas: potongan hasil belah mewarisi sifat tubuh saat itu.
        public SlimeBodyState Clone()
        {
            var c = new SlimeBodyState();
            foreach (var kv in traits) c.traits[kv.Key] = kv.Value;
            foreach (var kv in owners) c.owners[kv.Key] = kv.Value;
            c.eaten.AddRange(eaten);
            return c;
        }

        // Gabung dengan potongan lain: hanya mengisi jenis ciri yang BELUM dimiliki tubuh ini.
        // (Aturan sementara. Kalau tim mau "yang diserap menimpa", ubah di sini saja.)
        public void Absorb(SlimeBodyState other)
        {
            if (other == null) return;
            foreach (var kv in other.traits)
            {
                if (traits.ContainsKey(kv.Key)) continue;
                traits[kv.Key] = kv.Value;
                if (other.owners.TryGetValue(kv.Key, out var o)) owners[kv.Key] = o;
            }
        }

        // Muntahkan bahan terakhir: tubuh kembali seperti sebelum memakannya (ciri yang tertimpa pulih).
        // Dikembalikan bahannya (bisa ditaruh lagi di lantai sebagai bahan/umpan). null kalau perut kosong.
        public IngredientData Vomit()
        {
            if (eaten.Count == 0) return null;
            var last = eaten[eaten.Count - 1];
            var rest = new List<IngredientData>(eaten);
            rest.RemoveAt(rest.Count - 1);
            Clear();
            foreach (var i in rest) Eat(i);
            return last;
        }

        public void Clear()
        {
            traits.Clear();
            owners.Clear();
            eaten.Clear();
        }
    }
}

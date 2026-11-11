using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // State tubuh slime (logika murni, tanpa MonoBehaviour, supaya bisa di-test dan dibagi saat belah).
    //
    // v2 (desain-v2 bagian 5): tubuh = susunan UNIT. Unit bening (tubuh asli pemain, tanpa aura) + unit esens yang
    // dimakan, berurutan: [bening x ClearUnits] lalu [esens 1, esens 2, ...]. Massa = jumlah unit (awal 1, maks 4).
    //   - Ciri tiap jenis hanya satu nilai. Esens yang dimakan belakangan menimpa ciri sejenis, jadi URUTAN makan penting.
    //   - Aura tubuh = ciri Warna = esens terakhir yang memberi warna.
    //   - Efek samping (lambat, menyala, bau, berisik, gelap) hanya berlaku selama esensnya masih "memiliki" ciri di tubuh.
    //   - Belah: potongan baru mengambil k unit TERAKHIR (esens yang paling baru dimakan). Jadi esens masuk ke tiruan,
    //     dan pemain kembali bening setelah tiruan masuk toples. Tidak ada esens yang tergandakan.
    //   - Gabung: unit potongan lain di depan, unit potongan aktif di belakang (ciri potongan aktif menang).
    public class SlimeBodyState
    {
        public const int MaxMass = 4;

        private readonly Dictionary<TraitKind, Trait> traits = new Dictionary<TraitKind, Trait>();
        private readonly Dictionary<TraitKind, IngredientData> owners = new Dictionary<TraitKind, IngredientData>();
        private readonly List<IngredientData> eaten = new List<IngredientData>();
        // Bahan yang nyalanya sudah dipadamkan (Esens Beku dimakan sesudahnya). Dimakan lagi = menyala lagi.
        private readonly HashSet<IngredientData> doused = new HashSet<IngredientData>();

        // Unit bening (tanpa esens). Tubuh awal pemain = 1.
        public int ClearUnits { get; private set; } = 1;
        public int Mass => ClearUnits + eaten.Count;
        public bool CanEat => Mass < MaxMass;
        // Muntah butuh sisa minimal 1 unit.
        public bool CanVomit => eaten.Count > 0 && Mass >= 2;
        public bool CanSplit => Mass >= 2;

        public IReadOnlyList<IngredientData> Eaten => eaten;
        public IEnumerable<Trait> Traits => traits.Values;
        public int TraitCount => traits.Count;

        // Aura = ciri warna. "" = bening (belum ada esens pewarna).
        public string Aura => Get(TraitKind.Color);
        public IngredientData AuraSource => OwnerOf(TraitKind.Color);

        // false kalau perut sudah penuh (massa maksimum).
        public bool Eat(IngredientData ingredient)
        {
            if (ingredient == null || !CanEat) return false;
            Apply(ingredient);
            return true;
        }

        private void Apply(IngredientData ingredient)
        {
            eaten.Add(ingredient);
            foreach (var t in ingredient.Traits)
            {
                traits[t.kind] = t;
                owners[t.kind] = ingredient;
            }
            if (ingredient.Extinguishes)
            {
                foreach (var i in owners.Values)
                    if (i != ingredient) doused.Add(i);
            }
            else doused.Remove(ingredient);
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

        // ---- efek massa (desain-v2 bagian 5) ----
        public static float MassSpeed(int mass) => mass <= 1 ? 1f : mass == 2 ? 0.9f : mass == 3 ? 0.8f : 0.7f;
        public static float MassScale(int mass) => mass <= 1 ? 0.8f : mass == 2 ? 1f : mass == 3 ? 1.15f : 1.3f;
        public static float MassSight(int mass) => mass <= 1 ? 0.85f : mass == 2 ? 1f : mass == 3 ? 1.15f : 1.3f;
        // Tubuh besar (massa >= 3) mendarat "dug" walau tanpa esens berat.
        public const float BigStepNoise = 1.5f;

        // Pengali kecepatan dari esens saja (batu = 0.6).
        public float IngredientSpeed
        {
            get
            {
                float m = 1f;
                foreach (var i in ActiveIngredients()) m *= i.SpeedMultiplier;
                return m;
            }
        }

        // Pengali kecepatan total: esens x massa.
        public float SpeedMultiplier => IngredientSpeed * MassSpeed(Mass);

        // Pengali jarak pandang penjaga terhadap tubuh ini: massa x esens gelap (bayang).
        public float SightMultiplier
        {
            get
            {
                float m = MassSight(Mass);
                foreach (var i in ActiveIngredients()) m *= i.SightMultiplier;
                return m;
            }
        }

        // Berat: ciri berat (Esens Batu) atau tubuh besar. Menekan pelat berat, tidak muat celah.
        public bool IsHeavy => Get(TraitKind.Weight) == "berat" || Mass >= 3;

        public bool EmitsLight
        {
            get
            {
                foreach (var i in ActiveIngredients()) if (i.EmitsLight && !doused.Contains(i)) return true;
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

        // Radius suara tiap mendarat (0 = senyap): esens paling berisik, atau tubuh besar.
        public float StepNoiseRadius
        {
            get
            {
                float r = Mass >= 3 ? BigStepNoise : 0f;
                foreach (var i in ActiveIngredients()) r = Mathf.Max(r, i.StepNoiseRadius);
                return r;
            }
        }

        // Material palet body: dari esens yang menentukan warna (aura). null = pakai material default (bening).
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

        // Salinan lepas (untuk toples, test). Belah memakai Split, bukan Clone.
        public SlimeBodyState Clone()
        {
            var c = new SlimeBodyState();
            c.ClearUnits = ClearUnits;
            foreach (var kv in traits) c.traits[kv.Key] = kv.Value;
            foreach (var kv in owners) c.owners[kv.Key] = kv.Value;
            c.eaten.AddRange(eaten);
            c.doused.UnionWith(doused);
            return c;
        }

        // Belah: lepaskan k unit TERAKHIR jadi tubuh baru (esens paling baru dulu, lalu unit bening).
        // Tubuh ini menyimpan sisanya. null kalau tidak bisa (k harus 1..Massa-1).
        public SlimeBodyState Split(int k)
        {
            if (k < 1 || k > Mass - 1) return null;
            int fromEaten = Mathf.Min(k, eaten.Count);
            int fromClear = k - fromEaten;
            var taken = eaten.GetRange(eaten.Count - fromEaten, fromEaten);
            var rest = eaten.GetRange(0, eaten.Count - fromEaten);
            var piece = new SlimeBodyState();
            piece.Rebuild(fromClear, taken);
            Rebuild(ClearUnits - fromClear, rest);
            return piece;
        }

        // Gabung dengan potongan lain: massa dijumlah; unit potongan lain di depan, unit tubuh ini di belakang,
        // jadi ciri (dan aura) tubuh ini yang menang, ciri yang belum dimiliki diisi dari potongan lain.
        public void Absorb(SlimeBodyState other)
        {
            if (other == null) return;
            var all = new List<IngredientData>(other.eaten);
            all.AddRange(eaten);
            Rebuild(ClearUnits + other.ClearUnits, all);
        }

        // Muntahkan esens terakhir: tubuh kembali seperti sebelum memakannya (ciri yang tertimpa pulih), massa -1.
        // Dikembalikan esensnya (jatuh lagi ke lantai). null kalau tidak bisa (tanpa esens, atau tinggal 1 unit).
        public IngredientData Vomit()
        {
            if (!CanVomit) return null;
            var last = eaten[eaten.Count - 1];
            var rest = new List<IngredientData>(eaten);
            rest.RemoveAt(rest.Count - 1);
            Rebuild(ClearUnits, rest);
            return last;
        }

        private void Rebuild(int clear, List<IngredientData> list)
        {
            traits.Clear();
            owners.Clear();
            eaten.Clear();
            doused.Clear();
            ClearUnits = Mathf.Max(0, clear);
            foreach (var i in list) if (i != null) Apply(i);
        }

        // Kembali ke tubuh awal (1 unit bening).
        public void Clear() => Rebuild(1, new List<IngredientData>());
    }
}

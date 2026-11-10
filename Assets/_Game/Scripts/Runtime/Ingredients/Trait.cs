using System;

namespace GooNapped
{
    // Jenis ciri tubuh slime. Resep tiruan = 2-3 ciri (lihat CLAUDE.md bagian Mekanik).
    public enum TraitKind
    {
        Color,        // warna
        Temperature,  // suhu
        Weight,       // berat
        Shine,        // kilau
        Smell,        // bau
    }

    // Satu ciri = jenis + nilai. Nilai berupa teks bebas ("merah", "panas", "berat") supaya
    // daftar bahan/ciri bisa diisi tim di Inspector tanpa mengubah kode.
    [Serializable]
    public struct Trait : IEquatable<Trait>
    {
        public TraitKind kind;
        public string value;

        public Trait(TraitKind kind, string value)
        {
            this.kind = kind;
            this.value = value;
        }

        public bool Equals(Trait other) =>
            kind == other.kind && string.Equals(Normalize(value), Normalize(other.value), StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is Trait t && Equals(t);
        public override int GetHashCode() => ((int)kind * 397) ^ Normalize(value).GetHashCode();
        public override string ToString() => $"{kind}={value}";

        public static string Normalize(string v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim().ToLowerInvariant();
    }
}

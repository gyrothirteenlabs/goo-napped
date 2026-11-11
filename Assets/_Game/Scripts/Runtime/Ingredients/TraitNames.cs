using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Nama ciri untuk teks game (bahasa Inggris, desain-v2 jawaban 66). Nilai ciri di aset tetap teks bebas
    // berbahasa Indonesia ("merah", "berat") karena dipakai untuk mencocokkan; yang tampil di layar diterjemahkan di sini.
    // Nilai baru yang belum ada di daftar tampil apa adanya.
    public static class TraitNames
    {
        // Aura = ciri warna (desain-v2 bagian 5).
        private static readonly Dictionary<string, string> auras = new Dictionary<string, string>
        {
            { "abu", "Stone" }, { "hijau", "Glow" }, { "biru", "Dew" }, { "merah", "Ember" },
            { "kuning", "Sulfur" }, { "putih", "Frost" }, { "ungu", "Shade" }, { "hitam", "Dark" },
        };

        private static readonly Dictionary<string, string> values = new Dictionary<string, string>
        {
            { "panas", "hot" }, { "dingin", "cold" }, { "berat", "heavy" }, { "ringan", "light" },
            { "berkilau", "shiny" }, { "redup", "dull" }, { "belerang", "sulfur" },
        };

        private static readonly Dictionary<string, Color> colors = new Dictionary<string, Color>
        {
            { "merah", new Color(0.9f, 0.25f, 0.25f) }, { "abu", new Color(0.6f, 0.62f, 0.68f) },
            { "hijau", new Color(0.4f, 0.85f, 0.45f) }, { "biru", new Color(0.4f, 0.6f, 1f) },
            { "kuning", new Color(0.95f, 0.85f, 0.3f) }, { "putih", new Color(0.95f, 0.95f, 0.95f) },
            { "ungu", new Color(0.6f, 0.4f, 0.85f) }, { "hitam", new Color(0.15f, 0.15f, 0.2f) },
        };

        // Nama aura dari nilai warna ("merah" -> "Ember"). "" -> "Clear".
        public static string Aura(string colorValue)
        {
            var v = Trait.Normalize(colorValue);
            if (v == "") return "Clear";
            return auras.TryGetValue(v, out var n) ? n : v;
        }

        public static Color AuraColor(string colorValue) =>
            colors.TryGetValue(Trait.Normalize(colorValue), out var c) ? c : new Color(0.75f, 0.85f, 0.9f);

        // Teks ringkas satu ciri: aura untuk warna, kata sifat untuk yang lain ("hot", "heavy", "smelly").
        public static string Of(Trait t)
        {
            var v = Trait.Normalize(t.value);
            switch (t.kind)
            {
                case TraitKind.Color: return Aura(v) + " aura";
                case TraitKind.Smell: return "smelly";
                default: return values.TryGetValue(v, out var n) ? n : v;
            }
        }

        public static string Kind(TraitKind k)
        {
            switch (k)
            {
                case TraitKind.Color: return "Aura";
                case TraitKind.Temperature: return "Temp";
                case TraitKind.Weight: return "Weight";
                case TraitKind.Shine: return "Shine";
                case TraitKind.Smell: return "Smell";
            }
            return k.ToString();
        }
    }
}

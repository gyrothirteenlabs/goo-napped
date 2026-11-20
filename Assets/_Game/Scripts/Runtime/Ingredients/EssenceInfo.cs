using System.Collections.Generic;

namespace GooNapped
{
    // Teks penjelasan esens untuk pemain (bahasa Inggris). Disusun LANGSUNG dari data IngredientData + aturan di
    // SlimeBodyState/Guard/PlayerBrain, jadi teks tidak bisa berbeda dari efek sebenarnya kalau data esens diubah.
    // Dipakai: kartu esens di HUD saat slime dekat esens, dan pesan setelah makan.
    //
    // Fungsi esens (desain-v2):
    //   1. Aura: esens TERAKHIR yang dimakan menentukan aura tubuh. Tiruan di toples harus beraura sama dengan teman.
    //   2. Ukuran +1 per esens: butuh ukuran untuk membelah tiruan, tapi tubuh besar lebih lambat, lebih terlihat, dan
    //      tidak muat celah.
    //   3. Efek samping selama esens masih memiliki ciri di tubuh: bisa merugikan (menyala, berbau, berisik) atau
    //      dipakai sebagai alat (Bayang = lebih sulit terlihat, dingin = padamkan lentera, Batu = tekan pelat berat).
    public static class EssenceInfo
    {
        // Nama aura dari esens ("Ember"), atau "" kalau esens tidak memberi warna.
        public static string AuraName(IngredientData e)
        {
            if (e == null) return "";
            foreach (var t in e.Traits)
                if (t.kind == TraitKind.Color) return TraitNames.Aura(t.value);
            return "";
        }

        // Nilai warna mentah ("merah"), untuk dicocokkan dengan aura toples (RecipeData.Aura).
        public static string AuraValue(IngredientData e)
        {
            if (e == null) return "";
            foreach (var t in e.Traits)
                if (t.kind == TraitKind.Color) return Trait.Normalize(t.value);
            return "";
        }

        // Efek samping esens, satu baris per efek: "glows: guards spot you from farther".
        public static List<string> Effects(IngredientData e)
        {
            var list = new List<string>();
            if (e == null) return list;
            string temp = "", weight = "";
            foreach (var t in e.Traits)
            {
                if (t.kind == TraitKind.Temperature) temp = Trait.Normalize(t.value);
                if (t.kind == TraitKind.Weight) weight = Trait.Normalize(t.value);
            }
            if (e.SightMultiplier < 1f) list.Add("shadowy: guards only see you from closer");
            if (e.SightMultiplier > 1f) list.Add("conspicuous: guards see you from farther");
            if (e.EmitsLight) list.Add("glows: guards spot you from farther, fire familiars see you in the dark");
            else if (temp == "panas") list.Add("hot: fire familiars see you in the dark");
            if (temp == "dingin") list.Add("cold: lets you put out lanterns (E)");
            if (e.Extinguishes) list.Add("douses the glow of essence you ate before");
            if (e.SmellRadius > 0f) list.Add("smelly: leaves a scent trail that slime hounds follow");
            if (weight == "berat") list.Add("heavy: presses heavy plates, creaks old floors, can't squeeze through gaps");
            if (e.SpeedMultiplier < 1f) list.Add("slow");
            if (e.StepNoiseRadius > 0f) list.Add("loud landings: nearby guards hear you, even through walls");
            return list;
        }

        // Ringkasan satu baris untuk pesan setelah makan: "Ate Ember Essence. Ember aura · glows · hot".
        public static string EatenMessage(IngredientData e)
        {
            if (e == null) return "";
            var parts = new List<string>();
            string aura = AuraName(e);
            if (aura != "") parts.Add(aura + " aura");
            foreach (var fx in Effects(e))
            {
                int i = fx.IndexOf(':');
                parts.Add(i > 0 ? fx.Substring(0, i) : fx);
            }
            return "Ate " + e.DisplayName + "." + (parts.Count > 0 ? " " + string.Join(" · ", parts) : "");
        }
    }
}

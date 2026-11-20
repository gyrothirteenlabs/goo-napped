using NUnit.Framework;
using UnityEditor;

namespace GooNapped.Tests
{
    // Teks penjelasan esens untuk pemain (EssenceInfo) harus sesuai data esens, dan semua aset esens punya gambar.
    public class EssenceInfoTests
    {
        static bool Has(System.Collections.Generic.List<string> fx, string start) => fx.Exists(f => f.StartsWith(start));

        [Test]
        public void Batu_BeratLambatBerisik()
        {
            var e = IngredientData.Create("batu", new[] { new Trait(TraitKind.Weight, "berat"), new Trait(TraitKind.Color, "abu") },
                speedMultiplier: 0.6f, stepNoiseRadius: 2.2f);
            var fx = EssenceInfo.Effects(e);
            Assert.AreEqual("Stone", EssenceInfo.AuraName(e));
            Assert.AreEqual("abu", EssenceInfo.AuraValue(e));
            Assert.IsTrue(Has(fx, "heavy"));
            Assert.IsTrue(Has(fx, "slow"));
            Assert.IsTrue(Has(fx, "loud landings"));
            Assert.AreEqual(3, fx.Count);
        }

        [Test]
        public void Bara_MenyalaMenutupiPanas()
        {
            var e = IngredientData.Create("bara", new[] { new Trait(TraitKind.Color, "merah"), new Trait(TraitKind.Temperature, "panas") },
                emitsLight: true);
            var fx = EssenceInfo.Effects(e);
            Assert.IsTrue(Has(fx, "glows"));
            Assert.IsFalse(Has(fx, "hot"), "menyala sudah mencakup terlihat Familiar Api");
            Assert.AreEqual("Ate bara. Ember aura · glows", EssenceInfo.EatenMessage(e));
        }

        [Test]
        public void Bayang_LebihSulitTerlihat_Beku_DinginMemadamkan()
        {
            var bayang = IngredientData.Create("bayang", new[] { new Trait(TraitKind.Color, "ungu") }, sightMultiplier: 0.7f);
            Assert.IsTrue(Has(EssenceInfo.Effects(bayang), "shadowy"));
            var beku = IngredientData.Create("beku", new[] { new Trait(TraitKind.Color, "putih"), new Trait(TraitKind.Temperature, "dingin") },
                extinguishes: true);
            var fx = EssenceInfo.Effects(beku);
            Assert.IsTrue(Has(fx, "cold"));
            Assert.IsTrue(Has(fx, "douses"));
        }

        [Test]
        public void TanpaEfek_DaftarKosong()
        {
            var e = IngredientData.Create("polos", new[] { new Trait(TraitKind.Color, "biru") });
            Assert.AreEqual(0, EssenceInfo.Effects(e).Count);
            Assert.AreEqual("Ate polos. Dew aura", EssenceInfo.EatenMessage(e));
        }

        [Test]
        public void SemuaAsetEsens_PunyaGambarDanGlow_DanEfekTerbaca()
        {
            var guids = AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/_Game/Data/Ingredients" });
            Assert.Greater(guids.Length, 0);
            foreach (var g in guids)
            {
                var e = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
                Assert.IsNotNull(e.PickupSprite, e.name + ": pickupSprite kosong");
                Assert.IsNotNull(e.PickupGlow, e.name + ": pickupGlow kosong");
                Assert.AreNotEqual("", EssenceInfo.AuraName(e), e.name + ": esens tanpa aura");
                StringAssert.StartsWith("Ate ", EssenceInfo.EatenMessage(e));
            }
        }
    }
}

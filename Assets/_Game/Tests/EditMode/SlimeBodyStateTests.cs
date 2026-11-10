using NUnit.Framework;

namespace GooNapped.Tests
{
    public class SlimeBodyStateTests
    {
        static IngredientData RamuanApi() => IngredientData.Create("ramuanapi",
            new[] { new Trait(TraitKind.Color, "merah"), new Trait(TraitKind.Smell, "belerang") }, smellRadius: 3f);

        static IngredientData SerbukBatu() => IngredientData.Create("serbukbatu",
            new[] { new Trait(TraitKind.Weight, "berat"), new Trait(TraitKind.Color, "abu") }, speedMultiplier: 0.6f);

        static IngredientData MinyakNaga() => IngredientData.Create("minyaknaga",
            new[] { new Trait(TraitKind.Temperature, "panas") }, emitsLight: true);

        [Test]
        public void Baru_Netral()
        {
            var s = new SlimeBodyState();
            Assert.AreEqual(0, s.TraitCount);
            Assert.AreEqual("", s.Get(TraitKind.Color));
            Assert.AreEqual(1f, s.SpeedMultiplier);
            Assert.IsFalse(s.EmitsLight);
            Assert.AreEqual(0f, s.SmellRadius);
        }

        [Test]
        public void Makan_MemberiCiriDanEfek()
        {
            var s = new SlimeBodyState();
            s.Eat(RamuanApi());
            s.Eat(MinyakNaga());
            Assert.AreEqual("merah", s.Get(TraitKind.Color));
            Assert.AreEqual("panas", s.Get(TraitKind.Temperature));
            Assert.IsTrue(s.EmitsLight);
            Assert.AreEqual(3f, s.SmellRadius);
        }

        [Test]
        public void UrutanMakan_CiriSejenisTertimpa()
        {
            var s = new SlimeBodyState();
            s.Eat(RamuanApi());
            s.Eat(SerbukBatu());
            Assert.AreEqual("abu", s.Get(TraitKind.Color), "serbuk batu dimakan terakhir, warnanya menang");
            Assert.AreEqual("belerang", s.Get(TraitKind.Smell), "bau ramuan api tidak tertimpa");
            Assert.AreEqual(0.6f, s.SpeedMultiplier, 1e-5f);

            var s2 = new SlimeBodyState();
            s2.Eat(SerbukBatu());
            s2.Eat(RamuanApi());
            Assert.AreEqual("merah", s2.Get(TraitKind.Color));
            Assert.AreEqual("berat", s2.Get(TraitKind.Weight));
        }

        [Test]
        public void EfekHilang_KalauSemuaCiriBahanTertimpa()
        {
            var api = IngredientData.Create("api", new[] { new Trait(TraitKind.Temperature, "panas") }, emitsLight: true);
            var es = IngredientData.Create("es", new[] { new Trait(TraitKind.Temperature, "dingin") });
            var s = new SlimeBodyState();
            s.Eat(api);
            Assert.IsTrue(s.EmitsLight);
            s.Eat(es);
            Assert.IsFalse(s.EmitsLight, "api tidak lagi punya ciri di tubuh");
            Assert.AreEqual(1, s.ActiveIngredients().Count);
        }

        [Test]
        public void Resep_CocokDanSalah()
        {
            var s = new SlimeBodyState();
            s.Eat(RamuanApi());
            s.Eat(MinyakNaga());
            var resep = new[] { new Trait(TraitKind.Color, "Merah "), new Trait(TraitKind.Temperature, "panas") };
            Assert.IsTrue(s.Matches(resep), "huruf besar/spasi diabaikan");
            var resep2 = new[] { new Trait(TraitKind.Color, "biru"), new Trait(TraitKind.Weight, "berat") };
            Assert.AreEqual(2, s.MismatchCount(resep2));
        }

        [Test]
        public void Clone_Lepas()
        {
            var s = new SlimeBodyState();
            s.Eat(RamuanApi());
            var potongan = s.Clone();
            s.Eat(SerbukBatu());
            Assert.AreEqual("merah", potongan.Get(TraitKind.Color));
            Assert.IsFalse(potongan.Has(TraitKind.Weight));
            Assert.AreEqual("abu", s.Get(TraitKind.Color));
        }
        [Test]
        public void Gabung_HanyaMengisiCiriYangBelumAda()
        {
            var a = new SlimeBodyState();
            a.Eat(RamuanApi());
            var b = new SlimeBodyState();
            b.Eat(SerbukBatu());
            a.Absorb(b);
            Assert.AreEqual("merah", a.Get(TraitKind.Color), "warna sendiri tidak tertimpa");
            Assert.AreEqual("berat", a.Get(TraitKind.Weight), "berat diserap dari potongan lain");
            Assert.AreEqual(0.6f, a.SpeedMultiplier, 1e-5f);
        }
        [Test]
        public void Resep_UrutanMakanMenentukanCocok()
        {
            // resep: merah + berat. Serbuk batu ikut memberi warna abu, jadi harus dimakan SEBELUM ramuan api.
            var resep = RecipeData.Create("teman", new[] { new Trait(TraitKind.Color, "merah"), new Trait(TraitKind.Weight, "berat") });
            var benar = new SlimeBodyState();
            benar.Eat(SerbukBatu());
            benar.Eat(RamuanApi());
            Assert.AreEqual(0, benar.MismatchCount(resep.Traits));

            var salah = new SlimeBodyState();
            salah.Eat(RamuanApi());
            salah.Eat(SerbukBatu());
            Assert.AreEqual(1, salah.MismatchCount(resep.Traits), "warna tertimpa abu");
        }
    }
}

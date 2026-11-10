using NUnit.Framework;

namespace GooNapped.Tests
{
    public class SlimeBodyStateTests
    {
        static IngredientData Cabai() => IngredientData.Create("cabai",
            new[] { new Trait(TraitKind.Color, "merah"), new Trait(TraitKind.Smell, "pedas") }, smellRadius: 3f);

        static IngredientData Batu() => IngredientData.Create("batu",
            new[] { new Trait(TraitKind.Weight, "berat"), new Trait(TraitKind.Color, "abu") }, speedMultiplier: 0.6f);

        static IngredientData Bara() => IngredientData.Create("bara",
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
            s.Eat(Cabai());
            s.Eat(Bara());
            Assert.AreEqual("merah", s.Get(TraitKind.Color));
            Assert.AreEqual("panas", s.Get(TraitKind.Temperature));
            Assert.IsTrue(s.EmitsLight);
            Assert.AreEqual(3f, s.SmellRadius);
        }

        [Test]
        public void UrutanMakan_CiriSejenisTertimpa()
        {
            var s = new SlimeBodyState();
            s.Eat(Cabai());
            s.Eat(Batu());
            Assert.AreEqual("abu", s.Get(TraitKind.Color), "batu dimakan terakhir, warnanya menang");
            Assert.AreEqual("pedas", s.Get(TraitKind.Smell), "bau cabai tidak tertimpa");
            Assert.AreEqual(0.6f, s.SpeedMultiplier, 1e-5f);

            var s2 = new SlimeBodyState();
            s2.Eat(Batu());
            s2.Eat(Cabai());
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
            s.Eat(Cabai());
            s.Eat(Bara());
            var resep = new[] { new Trait(TraitKind.Color, "Merah "), new Trait(TraitKind.Temperature, "panas") };
            Assert.IsTrue(s.Matches(resep), "huruf besar/spasi diabaikan");
            var resep2 = new[] { new Trait(TraitKind.Color, "biru"), new Trait(TraitKind.Weight, "berat") };
            Assert.AreEqual(2, s.MismatchCount(resep2));
        }

        [Test]
        public void Clone_Lepas()
        {
            var s = new SlimeBodyState();
            s.Eat(Cabai());
            var potongan = s.Clone();
            s.Eat(Batu());
            Assert.AreEqual("merah", potongan.Get(TraitKind.Color));
            Assert.IsFalse(potongan.Has(TraitKind.Weight));
            Assert.AreEqual("abu", s.Get(TraitKind.Color));
        }
    }
}

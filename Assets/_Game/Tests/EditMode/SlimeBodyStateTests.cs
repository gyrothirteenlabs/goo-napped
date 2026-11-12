using NUnit.Framework;

namespace GooNapped.Tests
{
    // Logika tubuh slime: ciri, urutan makan, efek samping, dan MASSA v2 (desain-v2 bagian 5).
    public class SlimeBodyStateTests
    {
        static IngredientData RamuanApi() => IngredientData.Create("ramuanapi",
            new[] { new Trait(TraitKind.Color, "merah"), new Trait(TraitKind.Smell, "belerang") }, smellRadius: 3f);

        static IngredientData SerbukBatu() => IngredientData.Create("serbukbatu",
            new[] { new Trait(TraitKind.Weight, "berat"), new Trait(TraitKind.Color, "abu") }, speedMultiplier: 0.6f);

        static IngredientData MinyakNaga() => IngredientData.Create("minyaknaga",
            new[] { new Trait(TraitKind.Temperature, "panas") }, emitsLight: true);

        static IngredientData Embun() => IngredientData.Create("embun",
            new[] { new Trait(TraitKind.Color, "biru"), new Trait(TraitKind.Temperature, "dingin") });

        [Test]
        public void Baru_Netral_Massa1()
        {
            var s = new SlimeBodyState();
            Assert.AreEqual(0, s.TraitCount);
            Assert.AreEqual("", s.Get(TraitKind.Color));
            Assert.AreEqual("", s.Aura, "tubuh awal bening");
            Assert.AreEqual(1, s.Mass);
            Assert.AreEqual(1f, s.SpeedMultiplier);
            Assert.IsFalse(s.EmitsLight);
            Assert.AreEqual(0f, s.SmellRadius);
            Assert.IsFalse(s.CanSplit, "massa 1 tidak bisa belah");
            Assert.IsFalse(s.CanVomit);
        }

        [Test]
        public void Makan_MemberiCiriEfekDanMassa()
        {
            var s = new SlimeBodyState();
            Assert.IsTrue(s.Eat(RamuanApi()));
            Assert.IsTrue(s.Eat(MinyakNaga()));
            Assert.AreEqual(3, s.Mass);
            Assert.AreEqual("merah", s.Get(TraitKind.Color));
            Assert.AreEqual("merah", s.Aura);
            Assert.AreEqual("panas", s.Get(TraitKind.Temperature));
            Assert.IsTrue(s.EmitsLight);
            Assert.AreEqual(3f, s.SmellRadius);
        }

        [Test]
        public void PerutPenuh_TidakBisaMakanLagi()
        {
            var s = new SlimeBodyState();
            for (int i = 0; i < SlimeBodyState.MaxMass - 1; i++) Assert.IsTrue(s.Eat(Embun()));
            Assert.AreEqual(SlimeBodyState.MaxMass, s.Mass);
            Assert.IsFalse(s.CanEat);
            Assert.IsFalse(s.Eat(Embun()), "massa maksimum");
            Assert.AreEqual(SlimeBodyState.MaxMass, s.Mass);
        }

        [Test]
        public void Massa_MengaturKecepatanUkuranPandangDanBerat()
        {
            var s = new SlimeBodyState();
            Assert.AreEqual(1f, s.SpeedMultiplier);
            Assert.AreEqual(0.85f, s.SightMultiplier, 1e-5f, "tubuh kecil lebih sulit terlihat");
            s.Eat(Embun());
            s.Eat(Embun());
            Assert.AreEqual(3, s.Mass);
            Assert.AreEqual(0.8f, s.SpeedMultiplier, 1e-5f);
            Assert.AreEqual(1.15f, s.SightMultiplier, 1e-5f);
            Assert.IsTrue(s.IsHeavy, "massa 3 = berat (pelat berat)");
            Assert.AreEqual(SlimeBodyState.BigStepNoise, s.StepNoiseRadius, "tubuh besar mendarat dug");
        }

        [Test]
        public void EsensBayang_LebihSulitTerlihat()
        {
            var bayang = IngredientData.Create("bayang", new[] { new Trait(TraitKind.Color, "ungu") }, sightMultiplier: 0.7f);
            var s = new SlimeBodyState();
            s.Eat(bayang);
            Assert.AreEqual(1f * 0.7f, s.SightMultiplier, 1e-5f, "massa 2 (1.0) x bayang (0.7)");
        }

        [Test]
        public void BahanBerat_LangkahBerisik_SampaiTertimpa()
        {
            var s = new SlimeBodyState();
            Assert.AreEqual(0f, s.StepNoiseRadius);
            var batu = IngredientData.Create("batu", new[] { new Trait(TraitKind.Weight, "berat") }, stepNoiseRadius: 2.2f);
            var ringan = IngredientData.Create("bulu", new[] { new Trait(TraitKind.Weight, "ringan") });
            s.Eat(batu);
            Assert.AreEqual(2.2f, s.StepNoiseRadius);
            Assert.IsTrue(s.Vomit() == batu);
            s.Eat(ringan);   // ciri berat tertimpa bahan lain: batu tidak berisik lagi (massa 2, belum besar)
            Assert.AreEqual(0f, s.StepNoiseRadius);
        }

        [Test]
        public void UrutanMakan_CiriSejenisTertimpa()
        {
            var s = new SlimeBodyState();
            s.Eat(RamuanApi());
            s.Eat(SerbukBatu());
            Assert.AreEqual("abu", s.Get(TraitKind.Color), "serbuk batu dimakan terakhir, warnanya (aura) menang");
            Assert.AreEqual("belerang", s.Get(TraitKind.Smell), "bau ramuan api tidak tertimpa");
            Assert.AreEqual(0.6f, s.IngredientSpeed, 1e-5f);
            Assert.AreEqual(0.6f * 0.8f, s.SpeedMultiplier, 1e-5f, "batu x massa 3");

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
            var salinan = s.Clone();
            s.Eat(SerbukBatu());
            Assert.AreEqual("merah", salinan.Get(TraitKind.Color));
            Assert.IsFalse(salinan.Has(TraitKind.Weight));
            Assert.AreEqual(2, salinan.Mass);
            Assert.AreEqual("abu", s.Get(TraitKind.Color));
        }

        [Test]
        public void Belah_EsensTerakhirIkutPotonganBaru_PemainKembaliBening()
        {
            var s = new SlimeBodyState();
            var api = RamuanApi();
            var batu = SerbukBatu();
            s.Eat(api);
            s.Eat(batu);                          // [bening, api, batu] massa 3
            var tiruan = s.Split(2);              // teman besar: 2 unit terakhir
            Assert.IsNotNull(tiruan);
            Assert.AreEqual(2, tiruan.Mass);
            Assert.AreEqual("abu", tiruan.Aura, "aura tiruan = esens terakhir");
            Assert.AreEqual("berat", tiruan.Get(TraitKind.Weight));
            Assert.AreEqual(1, s.Mass);
            Assert.AreEqual("", s.Aura, "pemain kembali bening");
            Assert.IsFalse(s.EmitsLight || s.SmellRadius > 0f || s.IsHeavy, "efek esens ikut ke tiruan");
            Assert.AreEqual(0, s.Eaten.Count);
        }

        [Test]
        public void Belah_TidakBisaKalauMassaKurang_DanTidakMenggandakanEsens()
        {
            var s = new SlimeBodyState();
            Assert.IsNull(s.Split(1), "massa 1 tidak bisa belah");
            var embun = Embun();
            s.Eat(embun);                         // massa 2
            Assert.IsNull(s.Split(2), "harus tersisa minimal 1 unit");
            var a = s.Split(1);
            Assert.AreEqual(1, a.Mass);
            Assert.AreEqual(1, s.Mass);
            Assert.AreEqual(1, a.Eaten.Count + s.Eaten.Count, "esens tidak tergandakan");
            Assert.IsNull(a.Vomit(), "potongan 1 unit tidak bisa muntah");
        }

        [Test]
        public void Belah_DariUnitBening_KalauEsensKurang()
        {
            var s = new SlimeBodyState();
            s.Eat(Embun());                       // [bening, embun]
            s.Absorb(new SlimeBodyState());       // + 1 bening -> [bening, bening, embun]
            Assert.AreEqual(3, s.Mass);
            var potong = s.Split(2);              // ambil embun + 1 bening
            Assert.AreEqual(2, potong.Mass);
            Assert.AreEqual("biru", potong.Aura);
            Assert.AreEqual(1, s.Mass);
            Assert.AreEqual(1, s.ClearUnits);
        }

        [Test]
        public void Gabung_MassaDijumlah_CiriSendiriMenang()
        {
            var a = new SlimeBodyState();
            a.Eat(RamuanApi());
            var b = new SlimeBodyState();
            b.Eat(SerbukBatu());
            a.Absorb(b);
            Assert.AreEqual(4, a.Mass);
            Assert.AreEqual("merah", a.Get(TraitKind.Color), "warna (aura) sendiri tidak tertimpa");
            Assert.AreEqual("berat", a.Get(TraitKind.Weight), "berat diserap dari potongan lain");
            Assert.AreEqual(0.6f, a.IngredientSpeed, 1e-5f);
        }

        [Test]
        public void Resep_UrutanMakanMenentukanCocok()
        {
            // resep v1: merah + berat. Serbuk batu ikut memberi warna abu, jadi harus dimakan SEBELUM ramuan api.
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

        [Test]
        public void Muntah_TubuhKembaliSebelumEsensTerakhir_MassaBerkurang()
        {
            var s = new SlimeBodyState();
            s.Eat(SerbukBatu());
            s.Eat(RamuanApi());
            Assert.AreEqual("merah", s.Get(TraitKind.Color));
            var keluar = s.Vomit();
            Assert.AreEqual("ramuanapi", keluar.Id);
            Assert.AreEqual(2, s.Mass);
            Assert.AreEqual("abu", s.Get(TraitKind.Color), "warna batu yang tertimpa pulih");
            Assert.AreEqual("", s.Get(TraitKind.Smell), "bau ramuan api ikut hilang");
            Assert.AreEqual(1, s.Eaten.Count);
            s.Vomit();
            Assert.IsNull(s.Vomit(), "perut kosong");
            Assert.AreEqual(0, s.TraitCount);
            Assert.AreEqual(1, s.Mass);
        }

        static IngredientData GaramBeku() => IngredientData.Create("garambeku",
            new[] { new Trait(TraitKind.Color, "putih"), new Trait(TraitKind.Temperature, "dingin") }, extinguishes: true);

        static IngredientData LumutPendar() => IngredientData.Create("lumutpendar",
            new[] { new Trait(TraitKind.Shine, "berkilau"), new Trait(TraitKind.Color, "hijau") }, emitsLight: true);

        [Test]
        public void EsensBeku_MemadamkanYangSudahDimakan()
        {
            var s = new SlimeBodyState();
            s.Eat(LumutPendar());
            Assert.IsTrue(s.EmitsLight);
            s.Eat(GaramBeku());
            Assert.IsFalse(s.EmitsLight, "lumut masih punya ciri kilau, tapi nyalanya padam");
            Assert.AreEqual("berkilau", s.Get(TraitKind.Shine));
            Assert.AreEqual("dingin", s.Get(TraitKind.Temperature));

            var s2 = new SlimeBodyState();
            var lumut = LumutPendar();
            s2.Eat(GaramBeku());
            s2.Eat(lumut);
            Assert.IsTrue(s2.EmitsLight, "garam dimakan SEBELUM lumut: tidak memadamkan");

            var c = s.Clone();
            Assert.IsFalse(c.EmitsLight, "salinan mewarisi status padam");
        }
    }
}

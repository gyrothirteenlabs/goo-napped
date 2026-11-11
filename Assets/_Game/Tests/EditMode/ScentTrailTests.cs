using NUnit.Framework;
using UnityEngine;

namespace GooNapped.Tests
{
    public class ScentTrailTests
    {
        [SetUp] public void Reset() => ScentTrail.Clear();
        [TearDown] public void After() => ScentTrail.Clear();

        [Test]
        public void Freshest_MengikutiTitikTerbaruDalamJangkauan()
        {
            ScentTrail.Drop(new Vector2(0, 0), 2f, 1f);
            ScentTrail.Drop(new Vector2(1, 0), 2f, 2f);
            ScentTrail.Drop(new Vector2(2, 0), 2f, 3f);
            ScentTrail.Drop(new Vector2(20, 0), 2f, 4f);   // terbaru tapi jauh
            Assert.IsTrue(ScentTrail.Freshest(Vector2.zero, 3f, -1f, 4f, out var m));
            Assert.AreEqual(3f, m.time, "titik terbaru yang masih terjangkau");
            Assert.IsFalse(ScentTrail.Freshest(Vector2.zero, 3f, 3f, 4f, out _), "tidak ada yang lebih baru dari 3");
        }

        [Test]
        public void TitikTerhalang_Dilewati_PilihTerbaruYangTerjangkau()
        {
            ScentTrail.Drop(new Vector2(1, 0), 2f, 1f);
            ScentTrail.Drop(new Vector2(2, 0), 2f, 2f);   // di balik "tembok"
            Assert.IsTrue(ScentTrail.Freshest(Vector2.zero, 3f, -1f, 3f, out var m, p => p.x < 1.5f));
            Assert.AreEqual(1f, m.time);
        }

        [Test]
        public void TitikKadaluarsa_Diabaikan()
        {
            ScentTrail.Drop(new Vector2(0, 0), 2f, 0f);
            Assert.IsFalse(ScentTrail.Freshest(Vector2.zero, 3f, -1f, ScentTrail.Lifetime + 1f, out _));
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace GooNapped.Tests
{
    public class VisionMathTests
    {
        [Test]
        public void Kerucut_DepanTerlihat_SampingDanBelakangTidak()
        {
            var o = Vector2.zero;
            var f = Vector2.right;
            Assert.IsTrue(VisionMath.InCone(o, f, new Vector2(2f, 0f), 3f, 40f), "lurus di depan");
            Assert.IsTrue(VisionMath.InCone(o, f, new Vector2(2f, 0.6f), 3f, 40f), "16.7 derajat, masih di dalam 20");
            Assert.IsFalse(VisionMath.InCone(o, f, new Vector2(2f, 1f), 3f, 40f), "26.6 derajat, di luar");
            Assert.IsFalse(VisionMath.InCone(o, f, new Vector2(-1f, 0f), 3f, 40f), "di belakang");
            Assert.IsFalse(VisionMath.InCone(o, f, new Vector2(3.5f, 0f), 3f, 40f), "kejauhan");
        }

        [Test]
        public void SalahResep_CurigaLebihCepat()
        {
            Assert.AreEqual(1f, VisionMath.MismatchMultiplier(0, 0.35f));
            Assert.AreEqual(1f, VisionMath.MismatchMultiplier(-1, 0.35f), "belum ada tiruan (-1) = normal");
            Assert.AreEqual(1.7f, VisionMath.MismatchMultiplier(2, 0.35f), 1e-5f);
        }

        [Test]
        public void MakinDekat_MakinCepatCuriga()
        {
            Assert.Greater(VisionMath.DistanceFactor(0.5f, 3f), VisionMath.DistanceFactor(2.5f, 3f));
            Assert.AreEqual(0.5f, VisionMath.DistanceFactor(10f, 3f), 1e-5f, "di luar jangkauan dijepit ke ujung");
        }
    }
}

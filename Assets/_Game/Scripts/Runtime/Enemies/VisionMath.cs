using UnityEngine;

namespace GooNapped
{
    // Hitungan penglihatan penjaga (logika murni, bisa di-test tanpa scene).
    public static class VisionMath
    {
        // Target di dalam kerucut pandang? fullAngle = lebar kerucut total (derajat).
        public static bool InCone(Vector2 origin, Vector2 forward, Vector2 target, float range, float fullAngle)
        {
            var d = target - origin;
            float m = d.magnitude;
            if (m > range) return false;
            if (m < 1e-4f) return true;
            return Vector2.Angle(forward, d) <= fullAngle * 0.5f;
        }

        // Tiruan salah resep di toples bikin penjaga lebih cepat curiga (CLAUDE.md: "salah resep tidak langsung kalah").
        public static float MismatchMultiplier(int wrongTraits, float bonusPerTrait) =>
            1f + Mathf.Max(0, wrongTraits) * Mathf.Max(0f, bonusPerTrait);

        // Makin dekat makin cepat curiga: near (jarak 0) .. far (ujung jangkauan).
        public static float DistanceFactor(float distance, float range, float near = 1.6f, float far = 0.5f) =>
            Mathf.Lerp(near, far, range > 0f ? Mathf.Clamp01(distance / range) : 1f);
    }
}

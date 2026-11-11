using System;
using UnityEngine;

namespace GooNapped
{
    // Saluran suara untuk penjaga: siapa pun yang membuat bunyi (gumpalan dilempar, benda jatuh, ...) memanggil Emit,
    // penjaga dalam radius mendatangi sumbernya. Suara menembus tembok (beda dengan penglihatan).
    public static class NoiseBus
    {
        public static event Action<Vector2, float> Noise;

        // Domain Reload mati di project ini: buang pendengar sisa Play sebelumnya.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Noise = null;

        public static void Emit(Vector2 position, float radius) => Noise?.Invoke(position, radius);
    }
}

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GooNapped
{
    // Seberapa terang sebuah titik oleh lampu (obor, lentera, kuali): 0 = gelap, 1 = tepat di bawah lampu.
    // Global Light tidak dihitung. Lampu yang ber-bayangan terhalang tembok tidak menerangi.
    // Dipakai penjaga: slime yang berdiri di cahaya bisa ketahuan walau tidak di dalam kerucut pandang.
    public static class LightExposure
    {
        private static Light2D[] lights = new Light2D[0];
        private static float nextRefresh;
        private static bool forceRefresh = true;

        // Daftar lampu dimuat ulang tiap Play dan tiap ganti scene, lalu tiap 2 detik (lampu bisa muncul/hilang).
        // Dulu: scene tanpa lampu titik mencari ulang SETIAP pemanggilan (penjaga memanggil tiap frame).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            lights = new Light2D[0];
            forceRefresh = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => forceRefresh = true;

        private static void Refresh()
        {
            float now = Time.unscaledTime;
            bool stale = forceRefresh || (lights.Length > 0 && lights[0] == null) || nextRefresh - now > 5f;
            if (!stale && now < nextRefresh) return;
            lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            nextRefresh = now + 2f;
            forceRefresh = false;
        }

        public static float At(Vector2 point)
        {
            Refresh();
            float best = 0f;
            foreach (var l in lights)
            {
                if (l == null || !l.isActiveAndEnabled || l.lightType != Light2D.LightType.Point) continue;
                Vector2 lp = l.transform.position;
                float d = Vector2.Distance(lp, point);
                float outer = l.pointLightOuterRadius, inner = l.pointLightInnerRadius;
                if (d >= outer) continue;
                float e = d <= inner ? 1f : 1f - (d - inner) / Mathf.Max(0.01f, outer - inner);
                e *= Mathf.Clamp01(l.intensity);
                if (e <= best) continue;
                if (l.shadowsEnabled && Blocked(l, lp, point)) continue;
                best = e;
            }
            return best;
        }

        private static bool Blocked(Light2D light, Vector2 from, Vector2 to)
        {
            var owner = light.transform.parent != null ? light.transform.parent : light.transform;
            foreach (var h in Physics2D.LinecastAll(from, to))
            {
                var c = h.collider;
                if (c == null || c.isTrigger) continue;
                if (c.transform == owner || c.transform.IsChildOf(owner)) continue;   // tiang lentera sendiri
                if (c.GetComponentInParent<SlimePiece>() != null || c.GetComponentInParent<Guard>() != null) continue;
                return true;
            }
            return false;
        }
    }
}

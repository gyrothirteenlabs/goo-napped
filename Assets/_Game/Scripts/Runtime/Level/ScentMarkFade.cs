using UnityEngine;

namespace GooNapped
{
    // Noda bau di lantai: memudar selama umur jejak (ScentTrail.Lifetime), lalu hilang.
    public class ScentMarkFade : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float born;
        private float startAlpha;

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            born = Time.time;
            startAlpha = sr != null ? sr.color.a : 0f;
        }

        private void Update()
        {
            float k = (Time.time - born) / ScentTrail.Lifetime;
            if (k >= 1f) { Destroy(gameObject); return; }
            if (sr != null)
            {
                var c = sr.color;
                c.a = startAlpha * (1f - k);
                sr.color = c;
            }
        }
    }
}

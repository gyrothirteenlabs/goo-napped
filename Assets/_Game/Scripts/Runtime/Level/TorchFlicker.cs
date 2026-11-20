using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GooNapped
{
    // Obor/lentera (prefab Lantern): api bergoyang (ganti frame sprite secara acak) dan cahaya meredup-terang pelan
    // seperti tertiup angin, sesekali hembusan yang lebih dalam (api ikut bergoyang lebih cepat).
    //
    // Gameplay TIDAK berubah: LightExposure memotong intensitas di 1 (Mathf.Clamp01) dan memakai radius, jadi kedip hanya
    // pada intensitas dengan batas bawah minIntensity >= 1. Radius cahaya tidak disentuh. Padam (Extinguishable) ->
    // sprite padam, animasi berhenti.
    [RequireComponent(typeof(SpriteRenderer))]
    public class TorchFlicker : MonoBehaviour
    {
        [SerializeField] private Light2D lightSource;
        [SerializeField] private SpriteRenderer flame;
        [Tooltip("Frame api menyala (prop_torch frame 0, 2, 3, 4).")]
        [SerializeField] private Sprite[] litFrames = new Sprite[0];
        [Tooltip("Frame padam (prop_torch frame 1).")]
        [SerializeField] private Sprite outSprite;
        [SerializeField, Range(1f, 20f)] private float framesPerSecond = 8f;
        [Tooltip("Besar kedip halus (0.15 = intensitas naik-turun 15%).")]
        [SerializeField, Range(0f, 0.5f)] private float flicker = 0.15f;
        [Tooltip("Rata-rata jeda antar hembusan angin (detik).")]
        [SerializeField, Min(0.5f)] private float gustInterval = 4f;
        [Tooltip("Seberapa dalam cahaya meredup saat hembusan.")]
        [SerializeField, Range(0f, 0.6f)] private float gustDip = 0.3f;
        [SerializeField, Min(0.1f)] private float gustDuration = 0.6f;
        [Tooltip("Batas bawah intensitas. Harus >= 1 supaya aturan 'cahaya = bahaya' (LightExposure) tidak berubah.")]
        [SerializeField, Min(1f)] private float minIntensity = 1.05f;

        private float baseIntensity, seed, nextGust, gustT = -1f, frameT;
        private int frame;

        public float BaseIntensity => baseIntensity;
        public float MinIntensity => minIntensity;

        private void Awake()
        {
            if (lightSource == null) lightSource = GetComponentInChildren<Light2D>();
            if (flame == null) flame = GetComponent<SpriteRenderer>();
            baseIntensity = lightSource != null ? lightSource.intensity : 1f;
            seed = Mathf.Abs(transform.position.x * 3.7f + transform.position.y * 5.3f) % 50f;
            nextGust = Time.time + Random.Range(0.5f, gustInterval * 1.5f);
            frame = litFrames.Length > 0 ? Random.Range(0, litFrames.Length) : 0;
        }

        private void Update()
        {
            bool lit = lightSource == null || lightSource.enabled;
            if (!lit)
            {
                if (outSprite != null && flame != null) flame.sprite = outSprite;
                return;
            }

            // hembusan angin: redup sebentar (kurva sinus), api bergoyang lebih cepat
            if (Time.time >= nextGust)
            {
                gustT = 0f;
                nextGust = Time.time + gustInterval * Random.Range(0.6f, 1.5f);
            }
            float dip = 0f;
            if (gustT >= 0f)
            {
                gustT += Time.deltaTime / gustDuration;
                dip = Mathf.Sin(Mathf.Clamp01(gustT) * Mathf.PI) * gustDip;
                if (gustT >= 1f) gustT = -1f;
            }

            if (flame != null && litFrames.Length > 0)
            {
                frameT += Time.deltaTime * framesPerSecond * (gustT >= 0f ? 1.8f : 1f);
                if (frameT >= 1f)
                {
                    frameT = 0f;
                    if (litFrames.Length > 1)
                    {
                        int next = Random.Range(0, litFrames.Length - 1);
                        frame = next >= frame ? next + 1 : next;   // selalu frame lain
                    }
                    if (litFrames[frame] != null) flame.sprite = litFrames[frame];
                }
            }

            if (lightSource != null)
            {
                float n = Mathf.PerlinNoise(seed, Time.time * 3.2f) * 2f - 1f;   // -1..1
                lightSource.intensity = Mathf.Max(minIntensity, baseIntensity * (1f + flicker * n - dip));
            }
        }
    }
}

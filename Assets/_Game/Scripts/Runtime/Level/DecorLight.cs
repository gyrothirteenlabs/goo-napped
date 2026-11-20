using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GooNapped
{
    // Cahaya kecil dekorasi untuk lilin yang terlihat menyala di sprite (meja, tengkorak berlilin, ceruk lilin tembok).
    // Dipasang otomatis oleh Editor/Level/CandleLights.cs saat level dibangun (prefab Prefabs/Level/CandleLight).
    //
    // MURNI VISUAL: LightExposure mengabaikan lampu yang punya komponen ini, jadi berdiri di dekat lilin TIDAK membuat
    // slime "lit" / terlihat penjaga. Aturan "cahaya = bahaya" tetap hanya dari obor/lentera (L). Karena itu cahayanya
    // sengaja kecil dan redup (radius ~1 sel) supaya tidak terbaca sebagai genangan cahaya berbahaya.
    [RequireComponent(typeof(Light2D))]
    public class DecorLight : MonoBehaviour
    {
        [Tooltip("Besar kedip lilin (0.2 = naik-turun 20%).")]
        [SerializeField, Range(0f, 0.5f)] private float flicker = 0.2f;
        [SerializeField, Min(0.1f)] private float speed = 4f;

        private Light2D lightSource;
        private float baseIntensity, seed;

        private void Awake()
        {
            lightSource = GetComponent<Light2D>();
            baseIntensity = lightSource.intensity;
            seed = Mathf.Abs(transform.position.x * 13.1f + transform.position.y * 7.7f) % 100f;
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
            lightSource.intensity = baseIntensity * (1f + flicker * n);
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GooNapped
{
    // Lampu (lentera, obor) yang bisa dipadamkan slime bertubuh DINGIN (Garam Beku, Air Embun) dengan E.
    // Gelap = aman dari aturan cahaya, tapi desisnya terdengar: penjaga di dekat datang memeriksa.
    public class Extinguishable : MonoBehaviour
    {
        [SerializeField] private Light2D lightSource;
        [SerializeField] private AudioSource crackle;
        [SerializeField] private SpriteRenderer flame;
        [Tooltip("Radius suara desis saat dipadamkan (penjaga di dalamnya datang).")]
        [SerializeField] private float noiseRadius = 3f;

        public bool IsLit => lightSource != null && lightSource.enabled;

        private void Awake()
        {
            if (lightSource == null) lightSource = GetComponentInChildren<Light2D>();
            if (crackle == null) crackle = GetComponent<AudioSource>();
            if (flame == null) flame = GetComponent<SpriteRenderer>();
        }

        // silent = tanpa suara (dipulihkan dari checkpoint).
        public bool Extinguish(bool silent = false)
        {
            if (!IsLit) return false;
            lightSource.enabled = false;
            if (crackle != null) crackle.Stop();
            if (flame != null) flame.color = new Color(0.25f, 0.25f, 0.3f);
            if (silent) return true;
            NoiseBus.Emit(transform.position, noiseRadius);
            AudioDirector.Play(AudioDirector.Sfx.Disguise, transform.position, 0.9f, 0.05f);
            return true;
        }
    }
}

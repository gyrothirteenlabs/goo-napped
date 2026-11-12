using UnityEngine;

namespace GooNapped
{
    // Katup aura mesin penyadap di puncak (adegan final, desain-v2 bagian 9). E di dekatnya = memutar balik katup:
    // pemain diam beberapa saat (rawan ketahuan) dan bunyinya keras (Alkemis datang). Semua katup terbalik = toples
    // raja terbuka (FinalSequence).
    [RequireComponent(typeof(Collider2D))]
    public class AuraValve : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite reversedSprite;
        [Tooltip("Lama memutar katup (detik). Selama itu pemain diam.")]
        [SerializeField] private float turnTime = 1.2f;
        [SerializeField] private float noiseRadius = 6f;

        public bool IsReversed { get; private set; }
        public float TurnTime => turnTime;
        public event System.Action<AuraValve> Reversed;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Apply();
        }

        // Dipanggil PlayerBrain (E). Mengembalikan lama memutar, atau 0 kalau sudah terbalik.
        public float BeginTurn(SlimePiece by)
        {
            if (IsReversed) return 0f;
            StartCoroutine(Turn());
            return turnTime;
        }

        private System.Collections.IEnumerator Turn()
        {
            AudioDirector.Play(AudioDirector.Sfx.GolemStep, transform.position, 0.8f, 0.2f);
            yield return new WaitForSeconds(turnTime);
            IsReversed = true;
            Apply();
            AudioDirector.Play(AudioDirector.Sfx.Caught, transform.position, 0.7f, 0.1f);
            NoiseBus.Emit(transform.position, noiseRadius);
            Reversed?.Invoke(this);
        }

        // Dipulihkan dari checkpoint.
        public void SetReversed()
        {
            IsReversed = true;
            Apply();
        }

        private void Apply()
        {
            if (spriteRenderer == null) return;
            var s = IsReversed ? reversedSprite : closedSprite;
            if (s != null) spriteRenderer.sprite = s;
            else spriteRenderer.color = IsReversed ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.45f, 0.45f);
        }
    }
}

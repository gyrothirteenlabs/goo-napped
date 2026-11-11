using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Pelat tekan: pintu tersambung terbuka SELAMA ada potongan slime di atasnya (termasuk potongan nyamar).
    // Inti tema SHIFT: belah, tinggalkan satu potongan menahan pelat, Shift ke potongan lain dan lewat pintunya.
    // requireHeavy = hanya tubuh berat (ciri berat dari Esens Batu, atau massa >= 3) yang cukup menekan.
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private List<Door> doors = new List<Door>();
        [Tooltip("Hanya tubuh berat (ciri berat) yang bisa menekan pelat ini.")]
        [SerializeField] private bool requireHeavy;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite upSprite;
        [SerializeField] private Sprite downSprite;
        [Tooltip("Jari-jari area tekan dari pusat pelat (unit).")]
        [SerializeField] private float radius = 0.35f;
        [Tooltip("Tinggi pusat pelat dari pivot (pivot sprite di bawah tile).")]
        [SerializeField] private float centerHeight = 0.5f;

        private PlayerBrain brain;
        private bool pressed;
        private float hintCooldown;

        public bool IsPressed => pressed;
        public bool RequireHeavy => requireHeavy;
        public IReadOnlyList<Door> Doors => doors;
        private Vector2 Center => (Vector2)transform.position + Vector2.up * centerHeight;

        public void Setup(IEnumerable<Door> list, bool heavy)
        {
            doors.Clear();
            doors.AddRange(list);
            requireHeavy = heavy;
        }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            ApplySprite();
        }

        private void Start() => brain = FindFirstObjectByType<PlayerBrain>();

        private void Update()
        {
            if (hintCooldown > 0f) hintCooldown -= Time.deltaTime;
            bool now = false;
            bool lightOnPlate = false;
            if (brain != null)
                foreach (var p in brain.Pieces)
                {
                    if (p == null || Vector2.Distance(p.Center, Center) > radius + p.Radius * 0.5f) continue;
                    if (requireHeavy && !p.Body.State.IsHeavy) { lightOnPlate = p.IsControlled; continue; }
                    now = true;
                }
            if (lightOnPlate && !now && hintCooldown <= 0f)
            {
                brain.Notify("The plate won't budge. You need to be heavier (big body or Stone essence).");
                hintCooldown = 4f;
            }
            if (now == pressed) return;
            pressed = now;
            ApplySprite();
            foreach (var d in doors) if (d != null) d.Hold(this, pressed);
            AudioDirector.Play(AudioDirector.Sfx.UiClick, Center, 0.6f, 0.05f);
        }

        private void ApplySprite()
        {
            if (spriteRenderer == null) return;
            var s = pressed ? downSprite : upSprite;
            if (s != null) spriteRenderer.sprite = s;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Center, radius);
            foreach (var d in doors) if (d != null) Gizmos.DrawLine(Center, d.transform.position);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Pintu / gerbang satu tile. Tertutup = menghalangi slime, penjaga, dan pandangan; jalur penjaga memutar (NavGrid).
    // Dibuka oleh Tuas (toggle) dan/atau Pelat Tekan (selama ditekan). Terbuka = tuas XOR pelat, jadi pelat juga bisa
    // MENUTUP pintu yang terbuka (startsOpen).
    // Tidak menutup selama ada slime/penjaga di ambangnya (menunggu sampai kosong).
    [RequireComponent(typeof(BoxCollider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private bool startsOpen;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [Tooltip("Radius suara pintu bergerak (penjaga di dalamnya datang). 0 = senyap.")]
        [SerializeField] private float noiseRadius = 2.5f;

        private BoxCollider2D box;
        private bool toggled;
        private readonly HashSet<Object> holders = new HashSet<Object>();
        private bool isOpen;
        private bool registered;
        private readonly Collider2D[] overlap = new Collider2D[8];

        public bool IsOpen => isOpen;
        public bool StartsOpen => startsOpen;
        // Sel tile yang ditempati (dari pusat collider; bounds collider nonaktif tidak bisa dipakai).
        public Vector2Int Cell => NavGrid.CellOf(Center);
        private Vector2 Center => transform.TransformPoint((box != null ? box : GetComponent<BoxCollider2D>()).offset);

        private void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            toggled = startsOpen;
            Apply(startsOpen, silent: true);
        }

        private void OnEnable() => SetNavBlocked(!isOpen);
        private void OnDisable() => SetNavBlocked(false);

        public void Toggle()
        {
            toggled = !toggled;
            Refresh();
        }

        // Pelat tekan: tahan terbuka selama `holder` menekan.
        public void Hold(Object holder, bool on)
        {
            if (on) holders.Add(holder); else holders.Remove(holder);
            Refresh();
        }

        private bool Wanted => toggled != (holders.Count > 0);

        private void Refresh()
        {
            if (Wanted == isOpen) return;
            if (!Wanted && Occupied()) return;   // dicoba lagi di Update
            Apply(Wanted, silent: false);
        }

        private void Update()
        {
            if (Wanted != isOpen) Refresh();
        }

        // Ada slime/penjaga berdiri di ambang pintu?
        private bool Occupied()
        {
            int n = Physics2D.OverlapBox(Center, box.size * 0.9f, 0f, new ContactFilter2D { useTriggers = false }, overlap);
            for (int i = 0; i < n; i++)
            {
                if (overlap[i] == box) continue;
                if (overlap[i].GetComponentInParent<SlimePiece>() != null || overlap[i].GetComponentInParent<Guard>() != null) return true;
            }
            return false;
        }

        private void Apply(bool open, bool silent)
        {
            isOpen = open;
            if (box == null) box = GetComponent<BoxCollider2D>();
            box.enabled = !open;
            if (spriteRenderer != null)
            {
                var s = open ? openSprite : closedSprite;
                if (s != null) spriteRenderer.sprite = s;
            }
            if (isActiveAndEnabled) SetNavBlocked(!open);
            if (silent) return;
            AudioDirector.Play(AudioDirector.Sfx.GolemStep, transform.position, 0.7f, 0.1f);
            if (noiseRadius > 0f) NoiseBus.Emit(Center, noiseRadius);
        }

        private void SetNavBlocked(bool on)
        {
            if (on == registered) return;
            registered = on;
            NavGrid.SetBlocked(Cell, on);
        }
    }
}

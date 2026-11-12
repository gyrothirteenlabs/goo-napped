using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Tuas: E di dekatnya = buka/tutup pintu yang tersambung. Bunyi "klak" terdengar penjaga (pilihan: buka jalan
    // sekarang, atau tunggu penjaga menjauh). Butuh collider trigger supaya terdeteksi PlayerBrain.
    public class Lever : MonoBehaviour
    {
        [SerializeField] private List<Door> doors = new List<Door>();
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [Tooltip("Radius suara tuas ditarik (penjaga di dalamnya datang). 0 = senyap.")]
        [SerializeField] private float noiseRadius = 3f;
        [SerializeField] private float cooldown = 0.6f;

        private bool on;
        private float nextUse;

        public bool IsOn => on;
        public IReadOnlyList<Door> Doors => doors;
        public string Message { get; private set; } = "";

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            ApplySprite();
        }

        // Untuk tool blockout.
        public void SetDoors(IEnumerable<Door> list)
        {
            doors.Clear();
            doors.AddRange(list);
        }

        public bool Pull()
        {
            if (Time.time < nextUse) { Message = ""; return false; }
            nextUse = Time.time + cooldown;
            on = !on;
            ApplySprite();
            foreach (var d in doors) if (d != null) d.Toggle();
            AudioDirector.Play(AudioDirector.Sfx.UiClick, transform.position, 1f, 0.05f);
            if (noiseRadius > 0f) NoiseBus.Emit(transform.position, noiseRadius);
            Message = doors.Count == 0 ? "This lever isn't connected to anything." : "Clack! The lever moved. Nearby guards heard it.";
            return true;
        }

        private void ApplySprite()
        {
            if (spriteRenderer == null) return;
            var s = on ? onSprite : offSprite;
            if (s != null) spriteRenderer.sprite = s;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            foreach (var d in doors) if (d != null) Gizmos.DrawLine(transform.position, d.transform.position);
        }
    }
}

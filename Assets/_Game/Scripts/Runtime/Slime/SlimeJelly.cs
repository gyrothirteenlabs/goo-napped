using UnityEngine;

namespace GooNapped
{
    // Goyang jeli prosedural di atas sprite: squash/stretch berpegas + terangkat saat melompat.
    // Bekerja dengan sprite apa pun (placeholder sekarang, render model nanti), jadi gerak tetap halus
    // walau jumlah frame sprite sedikit. Skala dihitung dari titik kaki (pivot sprite = tengah bawah).
    public class SlimeJelly : MonoBehaviour
    {
        [Tooltip("Bagian yang ikut goyang dan terangkat (Body, Face, Disguise).")]
        [SerializeField] private Transform[] visuals = new Transform[0];
        [Tooltip("Bayangan: tetap di lantai, mengecil saat terangkat.")]
        [SerializeField] private Transform shadow;
        [SerializeField] private float stiffness = 260f;
        [SerializeField] private float damping = 14f;
        [Tooltip("Batas squash/stretch (0.3 = 30%).")]
        [SerializeField, Range(0.05f, 0.6f)] private float maxStretch = 0.32f;
        [Tooltip("Pelebaran saat memipih (volume jeli kira-kira tetap).")]
        [SerializeField, Range(0f, 1.5f)] private float widen = 0.7f;
        [Tooltip("Tinggi titik kaki dari pivot sprite (unit). Sprite 48px: kaki ±14 px di atas tepi bawah cell = 0.29.")]
        [SerializeField] private float feetHeight = 0.29f;

        private float stretch, velocity;
        private Vector3[] baseScale, basePos;
        private Vector3 shadowScale;
        private bool cached;

        // Tujuan pegas: > 0 memanjang, < 0 memipih. Diatur tiap frame oleh SlimeMotor / animasi.
        public float Target { get; set; }
        // Tinggi badan dari lantai (unit).
        public float Lift { get; set; }
        // Pengali ukuran keseluruhan (dipakai animasi terserap saat gabung).
        public float Size { get; set; } = 1f;

        private void Awake() => Cache();

        private void Cache()
        {
            if (cached) return;
            cached = true;
            baseScale = new Vector3[visuals.Length];
            basePos = new Vector3[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] == null) continue;
                baseScale[i] = visuals[i].localScale;
                basePos[i] = visuals[i].localPosition;
            }
            if (shadow != null) shadowScale = shadow.localScale;
        }

        // Animasi khusus (ancang belah, makan, berubah jadi barang) menahan tujuan pegas; gerak tidak bisa menimpanya.
        private bool hold;
        private float holdValue;
        public void Hold(float target) { hold = true; holdValue = target; }
        public void Release() => hold = false;

        // Dorongan sesaat ke pegas: negatif = gepeng tiba-tiba (mendarat), positif = melonjak.
        public void Kick(float impulse) => velocity += impulse;

        // Kembalikan pose diam (dipanggil sebelum objek di-Instantiate, supaya salinan tidak ikut gepeng).
        public void ResetPose()
        {
            Cache();
            stretch = velocity = Target = Lift = 0f;
            Size = 1f;
            hold = false;
            Apply();
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            float acc = ((hold ? holdValue : Target) - stretch) * stiffness - velocity * damping;
            velocity += acc * dt;
            stretch = Mathf.Clamp(stretch + velocity * dt, -maxStretch, maxStretch);
            Apply();
        }

        private void Apply()
        {
            float sy = (1f + stretch) * Size;
            float sx = (1f - stretch * widen) * Size;
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] == null) continue;
                visuals[i].localScale = new Vector3(baseScale[i].x * sx, baseScale[i].y * sy, baseScale[i].z);
                // skala berporos di kaki: geser supaya titik kaki tetap di tempat
                visuals[i].localPosition = basePos[i] + Vector3.up * (Lift + feetHeight * (1f - sy));
            }
            if (shadow != null)
            {
                float s = Mathf.Clamp(1f - Lift * 1.5f, 0.5f, 1f) * Size * (1f - stretch * widen * 0.5f);
                shadow.localScale = shadowScale * s;
            }
        }
    }
}

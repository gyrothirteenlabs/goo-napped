using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Lubang ventilasi = saluran lendir tua buatan para slime (desain-v2 bagian 3). Monster tidak muat, jadi:
    //   - Aman total: penjaga tidak melihat, tidak mencurigai, tidak memeriksa (HidingSpot dengan safe = true).
    //   - C di lubang = masuk. Checkpoint tersimpan (tertangkap = mulai lagi dari ventilasi terakhir).
    //   - Teman yang mengikuti ikut masuk = selamat (dievakuasi ke markas).
    //   - Di dalam: A/D pilih lubang lain di jaringan yang sama (jalan pintas), C = keluar di lubang terpilih.
    //   - Lubang exit (exitsLevel): masuk = level selesai, kalau semua teman sudah selamat.
    [RequireComponent(typeof(HidingSpot))]
    public class Vent : MonoBehaviour
    {
        [Tooltip("Lubang dengan nama jaringan sama saling tersambung (jalan pintas).")]
        [SerializeField] private string network = "A";
        [Tooltip("Lubang menuju area berikutnya: masuk = level selesai (setelah semua teman selamat).")]
        [SerializeField] private bool exitsLevel;
        [Tooltip("Masuk lubang ini menyimpan checkpoint.")]
        [SerializeField] private bool savesCheckpoint = true;

        public static readonly List<Vent> All = new List<Vent>();

        public HidingSpot Spot { get; private set; }
        public string Network => network;
        public bool ExitsLevel => exitsLevel;
        public bool SavesCheckpoint => savesCheckpoint;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void Awake() => Spot = GetComponent<HidingSpot>();
        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        // Lubang tetangga di jaringan yang sama, urut dari kiri ke kanan (lalu bawah ke atas). dir = -1 kiri, +1 kanan.
        public Vent Neighbor(int dir)
        {
            var list = new List<Vent>();
            foreach (var v in All) if (v != null && v.network == network) list.Add(v);
            if (list.Count < 2) return null;
            list.Sort((a, b) =>
            {
                int c = a.transform.position.x.CompareTo(b.transform.position.x);
                return c != 0 ? c : a.transform.position.y.CompareTo(b.transform.position.y);
            });
            int i = list.IndexOf(this);
            return list[(i + (dir < 0 ? -1 : 1) + list.Count) % list.Count];
        }

        public static Vent Of(HidingSpot spot) => spot != null ? spot.GetComponent<Vent>() : null;
    }
}

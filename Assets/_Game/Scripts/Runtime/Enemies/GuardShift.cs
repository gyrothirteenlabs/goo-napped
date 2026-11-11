using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Pergantian shift jaga (desain-v2 bagian 10–11). Penjaga bertugas di rutenya selama `dutyTime`, lalu pergi istirahat
    // ke `breakPoint` selama `breakTime`: posnya kosong sebentar. Pemain membaca jadwalnya lalu lewat saat pos kosong.
    // Kalau sedang curiga/mengejar, penjaga tidak berangkat istirahat (jadwal menunggu sampai tenang).
    [RequireComponent(typeof(Guard))]
    public class GuardShift : MonoBehaviour
    {
        [Tooltip("Detik bertugas di rute patroli.")]
        [SerializeField] private float dutyTime = 12f;
        [Tooltip("Detik di tempat istirahat (termasuk berjalan ke sana).")]
        [SerializeField] private float breakTime = 8f;
        [Tooltip("Tempat istirahat (posisi dunia, titik kaki).")]
        [SerializeField] private Vector2 breakPoint;
        [Tooltip("Detik pertama tugas saat level dimulai (geser jadwal antar penjaga).")]
        [SerializeField] private float startOffset;

        private Guard guard;
        private List<Vector2> dutyRoute;
        private bool onBreak;
        private float timer;

        public bool OnBreak => onBreak;
        // Detik sampai ganti (untuk HUD/debug).
        public float TimeLeft => timer;

        public void Setup(Vector2 point, float duty, float rest)
        {
            breakPoint = point;
            dutyTime = duty;
            breakTime = rest;
        }

        private void Start()
        {
            guard = GetComponent<Guard>();
            dutyRoute = new List<Vector2>(guard.PatrolPoints);
            timer = Mathf.Max(0.5f, dutyTime - startOffset);
        }

        private void Update()
        {
            if (guard == null) return;
            if (guard.IsAlerted || guard.IsSearching) return;   // tenang dulu baru jadwal jalan lagi
            if ((timer -= Time.deltaTime) > 0f) return;
            onBreak = !onBreak;
            guard.SetPatrol(onBreak ? new List<Vector2> { breakPoint } : dutyRoute);
            timer = onBreak ? breakTime : dutyTime;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f);
            Gizmos.DrawWireCube(breakPoint, Vector3.one * 0.4f);
        }
    }
}

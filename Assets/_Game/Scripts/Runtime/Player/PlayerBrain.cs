using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // "Otak" pemain: satu-satunya tempat input dibaca. Mengendalikan SATU potongan slime pada satu waktu.
    //   Gerak        : analog (WASD / stick)
    //   Makan        : bahan terdekat dalam jangkauan (sekali tekan)
    //   Belah/Gabung : satu tombol. Menyentuh potongan lain = gabung, kalau tidak = belah ke arah hadap
    //   Shift        : pindah kendali ke potongan berikutnya; yang ditinggal otomatis nyamar
    public class PlayerBrain : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";
        [SerializeField] private string moveAction = "Move";
        [SerializeField] private string eatAction = "Interact";
        [SerializeField] private string splitAction = "Jump";
        [SerializeField] private string switchAction = "Sprint";

        [Tooltip("Potongan awal. Kosong = semua SlimePiece di scene.")]
        [SerializeField] private List<SlimePiece> pieces = new List<SlimePiece>();
        [SerializeField, Min(1)] private int maxPieces = 3;
        [Tooltip("Jarak munculnya potongan baru dari potongan aktif (unit).")]
        [SerializeField] private float splitDistance = 0.8f;
        [Tooltip("Celah maksimum antar tepi dua potongan supaya dianggap bersentuhan (unit).")]
        [SerializeField] private float mergeGap = 0.15f;

        private InputAction move, eat, split, switchPiece;
        private int activeIndex;
        private readonly Collider2D[] hits = new Collider2D[8];

        public IReadOnlyList<SlimePiece> Pieces => pieces;
        public SlimePiece Active => pieces.Count > 0 ? pieces[Mathf.Clamp(activeIndex, 0, pieces.Count - 1)] : null;
        public event Action<SlimePiece> ActiveChanged;

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("PlayerBrain: Input Actions belum diisi (Assets/Settings/InputSystem_Actions).", this);
                enabled = false;
                return;
            }
            var map = actions.FindActionMap(actionMap, true);
            move = map.FindAction(moveAction, true);
            eat = map.FindAction(eatAction, true);
            split = map.FindAction(splitAction, true);
            switchPiece = map.FindAction(switchAction, true);
            map.Enable();
        }

        private void Start()
        {
            pieces.RemoveAll(p => p == null);
            if (pieces.Count == 0)
                pieces.AddRange(FindObjectsByType<SlimePiece>(FindObjectsSortMode.None));
            SetActive(0);
        }

        private void Update()
        {
            var a = Active;
            if (a == null) return;
            if (switchPiece.WasPressedThisFrame()) { SwitchNext(); return; }
            if (!a.IsEating)
            {
                if (split.WasPressedThisFrame() && SplitOrMerge()) return;
                if (eat.WasPressedThisFrame() && a.TryEat()) return;
            }
            a.Move(move.ReadValue<Vector2>());
        }

        // Potongan lain yang sedang bersentuhan dengan potongan aktif (paling dekat), atau null.
        public SlimePiece TouchingPiece()
        {
            var a = Active;
            if (a == null) return null;
            SlimePiece best = null;
            float bestD = float.MaxValue;
            foreach (var p in pieces)
            {
                if (p == null || p == a) continue;
                float d = Vector2.Distance(a.Center, p.Center);
                if (d <= a.Radius + p.Radius + mergeGap && d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        public bool SplitOrMerge()
        {
            var other = TouchingPiece();
            return other != null ? Merge(other) : TrySplit();
        }

        public bool TrySplit()
        {
            var a = Active;
            if (a == null || a.IsEating || pieces.Count >= maxPieces) return false;
            Vector2 offset = (Vector2)a.Facing * splitDistance;
            if (IsBlocked(a, a.Center + offset)) return false;

            var go = Instantiate(a.gameObject, a.transform.position + (Vector3)offset, Quaternion.identity, a.transform.parent);
            go.name = "Slime_Potongan_" + pieces.Count;
            var piece = go.GetComponent<SlimePiece>();
            piece.Body.SetState(a.Body.State.Clone());   // mewarisi sifat tubuh saat ini
            piece.Face(a.Facing);
            piece.SetControlled(false);                  // langsung nyamar
            pieces.Add(piece);
            return true;
        }

        // Ada dinding/prop di tempat potongan baru akan muncul?
        private bool IsBlocked(SlimePiece a, Vector2 center)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int n = Physics2D.OverlapCircle(center, a.Radius * 0.9f, filter, hits);
            for (int i = 0; i < n; i++)
                if (hits[i].GetComponentInParent<SlimePiece>() != a) return true;
            // dinding tipis (edge collider) di antara potongan aktif dan tempat muncul
            foreach (var h in Physics2D.LinecastAll(a.Center, center))
                if (h.collider != null && !h.collider.isTrigger && h.collider.GetComponentInParent<SlimePiece>() != a) return true;
            return false;
        }

        // Potongan aktif menyerap potongan lain yang bersentuhan. Ciri potongan aktif yang menang.
        public bool Merge(SlimePiece other)
        {
            var a = Active;
            if (a == null || other == null || other == a) return false;
            if (Vector2.Distance(a.Center, other.Center) > a.Radius + other.Radius + mergeGap) return false;
            a.Body.Absorb(other.Body.State);
            pieces.Remove(other);
            activeIndex = pieces.IndexOf(a);
            Destroy(other.gameObject);
            return true;
        }

        public void SwitchNext()
        {
            if (pieces.Count > 1) SetActive((activeIndex + 1) % pieces.Count);
        }

        public void SetActive(int index)
        {
            if (pieces.Count == 0) return;
            activeIndex = Mathf.Clamp(index, 0, pieces.Count - 1);
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i] != null) pieces[i].SetControlled(i == activeIndex);
            ActiveChanged?.Invoke(Active);
        }
    }
}

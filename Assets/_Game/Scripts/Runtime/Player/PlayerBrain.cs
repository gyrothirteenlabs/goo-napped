using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // "Otak" pemain: satu-satunya tempat input dibaca. Mengendalikan SATU potongan slime pada satu waktu.
    //   Gerak  : per tile (tahan arah = jalan terus)
    //   Makan  : bahan di tile sendiri / di depan
    //   Belah  : potongan baru muncul di tile depan, mewarisi sifat tubuh saat itu
    //   Shift  : pindah kendali ke potongan berikutnya; yang ditinggal otomatis nyamar
    //   Gabung : melangkah ke tile potongan lain (harus bersebelahan)
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

        private InputAction move, eat, split, switchPiece;
        private int activeIndex;

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
            if (a.IsBusy) return;
            if (split.WasPressedThisFrame() && TrySplit()) return;
            if (eat.WasPerformedThisFrame() && a.TryEat()) return;
            var dir = GridUtil.ToDirection(move.ReadValue<Vector2>());
            if (dir != Vector2Int.zero) TryMove(dir);
        }

        public SlimePiece PieceAt(Vector2Int cell)
        {
            foreach (var p in pieces)
                if (p != null && p.Mover.Cell == cell) return p;
            return null;
        }

        public bool TryMove(Vector2Int dir)
        {
            var a = Active;
            if (a == null || a.IsBusy) return false;
            var other = PieceAt(a.Mover.Cell + dir);
            if (other != null && other != a)
            {
                a.Face(dir);
                return Merge(other);
            }
            return a.Step(dir);
        }

        public bool TrySplit()
        {
            var a = Active;
            if (a == null || a.IsBusy || pieces.Count >= maxPieces) return false;
            var target = a.Mover.Cell + a.Facing;
            if (PieceAt(target) != null || a.Mover.IsBlocked(target)) return false;

            var go = Instantiate(a.gameObject, GridUtil.CellToWorld(target), Quaternion.identity, a.transform.parent);
            go.name = "Slime_Potongan_" + pieces.Count;
            var piece = go.GetComponent<SlimePiece>();
            piece.Mover.Teleport(target);
            piece.Body.SetState(a.Body.State.Clone());   // mewarisi sifat tubuh saat ini
            piece.Face(a.Facing);
            piece.SetControlled(false);                  // langsung nyamar
            pieces.Add(piece);
            return true;
        }

        // Potongan aktif menyerap potongan lain yang bersebelahan.
        public bool Merge(SlimePiece other)
        {
            var a = Active;
            if (a == null || other == null || other == a || other.IsBusy) return false;
            var d = other.Mover.Cell - a.Mover.Cell;
            if (Mathf.Abs(d.x) + Mathf.Abs(d.y) != 1) return false;
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

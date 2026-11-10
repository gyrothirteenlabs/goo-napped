using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Objektif level: semua teman ditukar dengan tiruan, lalu dibawa ke exit.
    public class LevelGoal : MonoBehaviour
    {
        [Tooltip("Toples di level ini. Kosong = semua Jar di scene.")]
        [SerializeField] private List<Jar> jars = new List<Jar>();
        [Tooltip("Jumlah teman yang harus dibawa ke exit. 0 = semua toples.")]
        [SerializeField, Min(0)] private int requiredFriends;

        public IReadOnlyList<Jar> Jars => jars;
        public int Required => requiredFriends > 0 ? requiredFriends : jars.Count;
        public bool IsComplete { get; private set; }
        public event Action<LevelGoal> Completed;

        private void Awake()
        {
            jars.RemoveAll(j => j == null);
            if (jars.Count == 0) jars.AddRange(FindObjectsByType<Jar>(FindObjectsSortMode.None));
        }

        // Dipanggil LevelExit saat potongan yang dikendalikan menyentuh exit.
        public bool TryComplete(SlimePiece piece)
        {
            if (IsComplete || piece == null || !piece.IsControlled) return false;
            if (piece.Carried.Count < Required) return false;
            IsComplete = true;
            Debug.Log($"Level selesai: {piece.Carried.Count} teman dibawa kabur.");
            Completed?.Invoke(this);
            return true;
        }
    }
}

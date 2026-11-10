using UnityEngine;

namespace GooNapped
{
    // Pintu keluar level. Butuh Collider2D (trigger).
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private LevelGoal goal;

        private void Awake()
        {
            if (goal == null) goal = FindFirstObjectByType<LevelGoal>();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (goal == null || goal.IsComplete) return;
            var piece = other.GetComponentInParent<SlimePiece>();
            if (piece != null) goal.TryComplete(piece);
        }
    }
}

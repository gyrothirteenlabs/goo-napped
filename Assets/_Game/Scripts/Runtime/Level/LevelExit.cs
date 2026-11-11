using UnityEngine;

namespace GooNapped
{
    // Exit level (ventilasi tujuan). Butuh Collider2D (trigger).
    // Teman yang dikawal masuk = selamat. Potongan yang dikendalikan masuk setelah semua teman selamat = level selesai.
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private LevelGoal goal;
        private PlayerBrain brain;
        private float hintCooldown;

        private void Awake()
        {
            if (goal == null) goal = FindFirstObjectByType<LevelGoal>();
        }

        private void Start() => brain = FindFirstObjectByType<PlayerBrain>();

        private void Update() { if (hintCooldown > 0f) hintCooldown -= Time.deltaTime; }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (goal == null || goal.IsComplete) return;
            var friend = other.GetComponentInParent<FriendSlime>();
            if (friend != null)
            {
                if (friend.State == FriendSlime.Mode.Following) friend.Save();
                return;
            }
            var piece = other.GetComponentInParent<SlimePiece>();
            if (piece == null || !piece.IsControlled) return;
            // pemain menahan lubang exit terbuka: teman yang mengikutinya merapat masuk
            bool escorting = false;
            foreach (var f in FriendSlime.All)
                if (f != null && f.State == FriendSlime.Mode.Following && f.Leader == piece) { f.CloseIn(); escorting = true; }
            if (goal.TryComplete(piece) || escorting || hintCooldown > 0f || brain == null) return;
            int left = goal.Required - goal.Saved;
            if (left > 0)
            {
                brain.Notify(left == 1 ? "Someone is still trapped. Bring them here first." : left + " friends are still trapped.");
                hintCooldown = 3f;
            }
        }
    }
}

using UnityEngine;

namespace GooNapped
{
    // Lumpur (desain-v2 jawaban 56). Slime atau teman yang lewat jadi berlumpur beberapa detik dan meninggalkan jejak bau
    // yang diikuti Anjing Lendir. Dicuci di genangan/air mancur (WashZone). Butuh Collider2D (trigger).
    [RequireComponent(typeof(Collider2D))]
    public class MudZone : MonoBehaviour
    {
        [Tooltip("Lama berlumpur setelah keluar dari lumpur (detik).")]
        [SerializeField] private float muddySeconds = 7f;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerStay2D(Collider2D other)
        {
            var p = other.GetComponentInParent<SlimePiece>();
            if (p != null && !p.IsHidden) { p.MakeMuddy(muddySeconds); return; }
            var f = other.GetComponentInParent<FriendSlime>();
            if (f != null && f.IsExposed) f.MakeMuddy(muddySeconds);
        }
    }
}

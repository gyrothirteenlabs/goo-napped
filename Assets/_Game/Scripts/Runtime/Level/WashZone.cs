using UnityEngine;

namespace GooNapped
{
    // Genangan / air mancur (desain-v2 jawaban 56): mencuci lumpur dari slime dan teman yang lewat.
    // Bau dari esens (Esens Belerang) TIDAK bisa dicuci, hanya lumpur. Butuh Collider2D (trigger).
    [RequireComponent(typeof(Collider2D))]
    public class WashZone : MonoBehaviour
    {
        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerStay2D(Collider2D other)
        {
            var p = other.GetComponentInParent<SlimePiece>();
            if (p != null) { p.Wash(); return; }
            var f = other.GetComponentInParent<FriendSlime>();
            if (f != null) f.Wash();
        }
    }
}

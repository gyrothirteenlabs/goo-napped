using System;
using UnityEngine;

namespace GooNapped
{
    // Toples segel berisi satu slime teman.
    // Alur: intip (lihat resep) -> potongan pemain masuk sebagai tiruan -> teman keluar untuk ditelan dan dibawa kabur.
    // Tiruan yang resepnya salah TIDAK langsung kalah: DecoyMismatch dipakai penjaga untuk lebih cepat curiga.
    public class Jar : MonoBehaviour
    {
        [SerializeField] private RecipeData recipe;
        [SerializeField] private FriendSlime friendPrefab;
        [Tooltip("Renderer isi toples (body), supaya tiruan terlihat sesuai warna tubuhnya.")]
        [SerializeField] private SpriteRenderer contentRenderer;

        public RecipeData Recipe => recipe;
        public bool IsPeeked { get; private set; }
        public bool HasFriend { get; private set; } = true;
        public bool HasDecoy => DecoyState != null;
        public SlimeBodyState DecoyState { get; private set; }
        // Jumlah ciri tiruan yang salah (0 = sempurna). -1 kalau belum ada tiruan.
        public int DecoyMismatch => HasDecoy && recipe != null ? DecoyState.MismatchCount(recipe.Traits) : -1;

        public event Action<Jar> Changed;

        private void Awake()
        {
            if (contentRenderer != null && recipe != null && recipe.BodyMaterial != null)
                contentRenderer.sharedMaterial = recipe.BodyMaterial;
        }

        // Objektif 1: intip toples untuk melihat ciri teman.
        public void Peek()
        {
            if (IsPeeked) return;
            IsPeeked = true;
            Changed?.Invoke(this);
        }

        // Berapa ciri tubuh ini yang masih salah terhadap resep (untuk petunjuk HUD).
        public int MismatchOf(SlimeBodyState body) => recipe != null ? body.MismatchCount(recipe.Traits) : 0;

        // Potongan masuk sebagai tiruan, teman keluar di posisi `exitPosition`. Mengembalikan teman yang keluar.
        public FriendSlime Swap(SlimeBodyState decoy, Vector3 exitPosition)
        {
            if (!HasFriend || decoy == null) return null;
            HasFriend = false;
            DecoyState = decoy.Clone();
            if (contentRenderer != null && DecoyState.BodyMaterial != null)
                contentRenderer.sharedMaterial = DecoyState.BodyMaterial;

            FriendSlime friend = null;
            if (friendPrefab != null)
            {
                friend = Instantiate(friendPrefab, exitPosition, Quaternion.identity, transform.parent);
                friend.Init(recipe);
            }
            Changed?.Invoke(this);
            return friend;
        }
    }
}

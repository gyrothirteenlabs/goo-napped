using UnityEngine;

namespace GooNapped
{
    // Teman di markas (desain-v2 bagian 12): hanya muncul kalau sudah diselamatkan (RescueLog).
    // E di dekatnya = bicara. Kalimat bergantian dari RecipeData.HubLines (placeholder; isi final dari tim).
    [RequireComponent(typeof(Collider2D))]
    public class HubFriend : MonoBehaviour
    {
        [SerializeField] private RecipeData friend;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("Tampil walau belum diselamatkan (untuk uji di editor).")]
        [SerializeField] private bool alwaysShow;

        private int lineIndex;
        private Transform visual;
        private Vector3 visualBase;
        private float bobSeed;

        public RecipeData Friend => friend;

        private void Awake()
        {
            if (!alwaysShow && !RescueLog.IsRescued(friend)) { gameObject.SetActive(false); return; }
            if (friend != null)
            {
                if (bodyRenderer != null && friend.BodyMaterial != null) bodyRenderer.sharedMaterial = friend.BodyMaterial;
                transform.localScale = Vector3.one * SlimeBodyState.MassScale(friend.Size) / SlimeBodyState.MassScale(1);
            }
            if (bodyRenderer != null) { visual = bodyRenderer.transform; visualBase = visual.localPosition; }
            bobSeed = Random.value * 10f;
        }

        private void Update()
        {
            // napas pelan, supaya markas terasa hidup
            if (visual != null) visual.localPosition = visualBase + Vector3.up * (0.015f * Mathf.Sin((Time.time + bobSeed) * 2f));
        }

        // Dipanggil PlayerBrain saat E di dekat teman ini.
        public void Talk()
        {
            if (friend == null) return;
            var lines = friend.HubLines;
            string text = lines != null && lines.Length > 0 ? lines[lineIndex++ % lines.Length] : "...";
            StoryPlayer.Say(friend.FriendName, text);
        }
    }
}

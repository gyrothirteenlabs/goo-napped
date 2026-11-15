using UnityEngine;

namespace GooNapped
{
    // Area tutorial: saat slime yang dikendalikan masuk, prompt melayang di atas slime (TutorialPrompt).
    // Selesai saat tombolnya dipakai cukup lama (holdToComplete detik, mis. WASD = benar-benar bergerak),
    // atau hilang saat slime keluar area. Untuk stage tutorial: taruh beberapa trigger berurutan di sepanjang jalan.
    // Teks tampil = bahasa Inggris, tanpa "press": cukup "TO MOVE", "TO SPLIT", dst. (tombol digambar sebagai keycap).
    [RequireComponent(typeof(Collider2D))]
    public class TutorialTrigger : MonoBehaviour
    {
        [Tooltip("\"WASD\" (susunan keyboard) atau tombol dipisah spasi: \"E\", \"SPACE\", \"SHIFT\", \"C\", \"F\", \"Q\", \"R\".")]
        [SerializeField] private string keys = "WASD";
        [SerializeField] private string text = "to move";
        [Tooltip("Prompt selesai saat tombolnya ditekan selama total detik ini (0 = langsung saat ditekan). Kosongkan centang di bawah untuk tidak pernah selesai lewat tombol.")]
        [SerializeField, Min(0f)] private float holdToComplete = 0.6f;
        [SerializeField] private bool completeOnKey = true;
        [Tooltip("Sekali selesai, trigger ini tidak muncul lagi (juga setelah tertangkap / ulang dari ventilasi).")]
        [SerializeField] private bool once = true;
        [Tooltip("Jeda sebelum prompt muncul setelah masuk area.")]
        [SerializeField, Min(0f)] private float delay = 0.15f;
        [Tooltip("Prompt ini baru muncul setelah trigger ini selesai (urutan beberapa prompt di ruangan yang sama). Kosong = langsung.")]
        [SerializeField] private TutorialTrigger after;

        private int inside;
        private bool done;

        public bool Done => done;
        private float heldFor, enteredAt = -1f;

        private void Reset()
        {
            var c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<SlimePiece>();
            if (p == null || !p.IsControlled) return;
            inside++;
            if (inside == 1) enteredAt = Time.unscaledTime;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var p = other.GetComponentInParent<SlimePiece>();
            if (p == null) return;
            inside = Mathf.Max(0, inside - 1);
            if (inside == 0)
            {
                enteredAt = -1f;
                TutorialPrompt.Get()?.Hide(this);
            }
        }

        private void Update()
        {
            if (inside <= 0 || (done && once)) return;
            if (after != null && !after.Done) return;   // tunggu prompt sebelumnya selesai
            var prompt = TutorialPrompt.Get();
            if (prompt == null) return;
            if (!prompt.IsShowingFor(this))
            {
                if (prompt.IsBusy) return;   // prompt lain masih tampil / sedang hilang
                if (enteredAt >= 0f && Time.unscaledTime - enteredAt >= delay && !done)
                {
                    prompt.Show(this, keys, text);
                    heldFor = 0f;
                }
                return;
            }
            if (!completeOnKey || string.IsNullOrWhiteSpace(keys)) return;   // prompt info saja: tampil selama di area
            if (TutorialPrompt.AnyHeld(keys)) heldFor += Time.unscaledDeltaTime;
            if (heldFor >= holdToComplete && heldFor > 0f)
            {
                done = true;
                prompt.Complete(this);
            }
        }
    }
}

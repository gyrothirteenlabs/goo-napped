using UnityEngine;
using UnityEngine.Events;

namespace GooNapped
{
    // Memutar cerita di level tanpa kode:
    //   OnStart  : saat level dimulai (dialog pembuka level)
    //   OnEnter  : saat potongan slime yang dikendalikan masuk area trigger (butuh Collider2D Is Trigger)
    //   Manual   : dipanggil dari event lain lewat PlayStory() (UnityEvent, script)
    public class StoryTrigger : MonoBehaviour
    {
        public enum When { OnStart, OnEnter, Manual }

        [SerializeField] private StorySequence sequence;
        [SerializeField] private When when = When.OnEnter;
        [SerializeField] private bool once = true;
        [SerializeField] private UnityEvent onFinished;

        private bool played;

        private void Start()
        {
            if (when == When.OnStart) PlayStory();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (when != When.OnEnter) return;
            var p = other.GetComponentInParent<SlimePiece>();
            if (p != null && p.IsControlled) PlayStory();
        }

        public void PlayStory()
        {
            if (once && played) return;
            played = true;
            StoryPlayer.Play(sequence, () => onFinished?.Invoke());
        }
    }
}

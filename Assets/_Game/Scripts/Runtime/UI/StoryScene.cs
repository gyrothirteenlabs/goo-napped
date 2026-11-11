using UnityEngine;

namespace GooNapped
{
    // Untuk scene cerita penuh (Intro, Ending, antar-chapter): putar cerita, lalu pindah scene.
    public class StoryScene : MonoBehaviour
    {
        [SerializeField] private StorySequence sequence;
        [Tooltip("Scene sesudah cerita. Kosong = menu utama.")]
        [SerializeField] private string nextScene = "";

        private void Start()
        {
            StoryPlayer.Play(sequence, () =>
            {
                if (string.IsNullOrEmpty(nextScene)) GameFlow.LoadMainMenu();
                else GameFlow.Load(nextScene);
            });
        }
    }
}

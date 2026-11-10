using UnityEngine;

namespace GooNapped
{
    // Scene Boot (build index 0): inisialisasi global, lalu langsung ke menu utama.
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            GameFlow.LoadMainMenu();
        }
    }
}

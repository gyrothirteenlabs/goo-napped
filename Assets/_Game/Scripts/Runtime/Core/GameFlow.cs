using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Urutan level, progres (level yang sudah terbuka), dan pindah scene.
    // Progres disimpan di PlayerPrefs (cukup untuk jam). Level baru: tambahkan namanya di Levels (urut).
    public static class GameFlow
    {
        public const string MainMenu = "MainMenu";
        public static readonly string[] Levels = { "C1_L01", "C1_L02", "C1_L03", "C1_L04", "C2_L01", "C2_L02", "C2_L03", "C2_L04" };
        const string UnlockKey = "gn_unlocked";

        public static int IndexOf(string scene) => System.Array.IndexOf(Levels, scene);

        // Indeks level tertinggi yang sudah terbuka (minimal level pertama).
        public static int UnlockedIndex => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 0), 0, Levels.Length - 1);

        public static bool IsUnlocked(int index) => index <= UnlockedIndex;

        public static void Unlock(string scene)
        {
            int i = IndexOf(scene);
            if (i > UnlockedIndex) { PlayerPrefs.SetInt(UnlockKey, i); PlayerPrefs.Save(); }
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(UnlockKey);
            PlayerPrefs.Save();
        }

        public static void Load(string scene)
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"GameFlow: scene '{scene}' tidak ada di Build Settings.");
        }

        public static void LoadMainMenu() => Load(MainMenu);

        // Ulang scene yang sedang aktif. Di editor, scene sandbox yang tidak ada di Build Settings dimuat lewat path.
        public static void ReloadCurrent()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }
    }
}

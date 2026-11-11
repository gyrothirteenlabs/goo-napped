using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Urutan level, progres (level yang sudah terbuka), dan pindah scene.
    // Progres disimpan di PlayerPrefs (cukup untuk jam). Level baru: tambahkan namanya di Levels (urut).
    //
    // Desain v2 (11 Nov): level C1_L01–C2_L04 adalah level v1 dan sekarang jadi SANDBOX (jawaban 71), tidak di alur game.
    // Level v2: blockout 11 level (desain-v2 bagian 3) di Scenes/Levels/V2, dibangun GooNapped > Blockout > Semua level v2.
    // Di editor, scene yang belum ada di Build Settings tetap bisa dimuat lewat path (Build Settings = keputusan tim).
    public static class GameFlow
    {
        public const string MainMenu = "MainMenu";
        public const string Intro = "Intro";
        public const string Ending = "Ending";
        // Markas di jaringan ventilasi: muncul di antara level (desain-v2 bagian 12).
        public const string Hub = "Markas";
        // Level yang dituju setelah markas.
        public static string PendingNext { get; private set; } = "";
        public static readonly string[] Levels =
        {
            "L01_Gate", "L02_Hedge", "L03_Mud", "L04_Cells", "L05_CellBlock", "L06_DrainLab",
            "L07_GreatHall", "L08_Dining", "L09_Gallery", "L10_Tower", "L11_Summit",
        };
        // Level v1 (sandbox, tidak dimainkan dari menu). Disimpan untuk referensi tool/test.
        public static readonly string[] V1Levels = { "C1_L01", "C1_L02", "C1_L03", "C1_L04", "C2_L01", "C2_L02", "C2_L03", "C2_L04" };
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
            FriendPowers.ResetUnlocks();
            RescueLog.Reset();
            PlayerPrefs.Save();
        }

        public static void Load(string scene)
        {
            Time.timeScale = 1f;
            Checkpoint.Clear();   // checkpoint hanya berlaku untuk level yang sedang dimainkan
            if (Application.CanStreamedLevelBeLoaded(scene)) { SceneManager.LoadScene(scene); return; }
#if UNITY_EDITOR
            // editor: scene di luar Build Settings (sandbox, level v2 baru) dimuat lewat path
            var path = FindScenePath(scene);
            if (path != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogWarning($"GameFlow: scene '{scene}' tidak ada di Build Settings.");
        }

#if UNITY_EDITOR
        // Path scene bernama `name` di Assets/_Game/Scenes, atau null.
        public static string FindScenePath(string name)
        {
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets(name + " t:Scene", new[] { "Assets/_Game/Scenes" }))
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return p;
            }
            return null;
        }
#endif

        public static void LoadMainMenu() => Load(MainMenu);

        // Setelah level selesai: ke markas dulu kalau tujuan berikutnya level lain; dari markas lanjut ke level itu.
        // Level terakhir (berikutnya Ending) dan scene di luar alur langsung ke tujuan.
        public static void LoadAfterLevel(string current, string next)
        {
            if (current == Hub)
            {
                string go = !string.IsNullOrEmpty(PendingNext) ? PendingNext : (!string.IsNullOrEmpty(next) ? next : MainMenu);
                PendingNext = "";
                Load(go);
                return;
            }
            if (string.IsNullOrEmpty(next)) { LoadMainMenu(); return; }
            if (IndexOf(next) >= 0 && SceneExists(Hub))
            {
                PendingNext = next;
                Load(Hub);
                return;
            }
            Load(next);
        }

        public static bool SceneExists(string name)
        {
            if (Application.CanStreamedLevelBeLoaded(name)) return true;
#if UNITY_EDITOR
            return FindScenePath(name) != null;
#else
            return false;
#endif
        }

        // Ulang scene yang sedang aktif (checkpoint TIDAK dibuang: level dilanjutkan dari ventilasi terakhir).
        // Di editor, scene sandbox yang tidak ada di Build Settings dimuat lewat path.
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

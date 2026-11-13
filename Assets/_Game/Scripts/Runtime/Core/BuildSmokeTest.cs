using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Smoke test untuk BUILD (bukan fitur game). Jalankan .exe dengan argumen -gnSmoke:
    //   Boot -> menu -> Intro (cerita dimajukan otomatis sampai pindah sendiri ke level pertama, alur asli)
    //   -> tiap level + Markas + Ending dimuat satu per satu -> hasil ditulis ke log -> game keluar sendiri.
    // Hasil: baris "SMOKE" di goo-napped_log.txt (DevLog) dan Player.log.
    public class BuildSmokeTest : MonoBehaviour
    {
        private int ok, fail;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Application.isEditor || !DevLog.HasArg("-gnSmoke")) return;
            var go = new GameObject("BuildSmokeTest");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildSmokeTest>();
        }

        private IEnumerator Start()
        {
            Debug.Log("SMOKE mulai");
            yield return WaitScene(GameFlow.MainMenu, 15f, "Boot -> menu utama");

            // alur asli cerita pembuka: Intro selesai harus pindah sendiri ke level pertama (bug layar hitam)
            GameFlow.Load(GameFlow.Intro);
            yield return WaitScene(GameFlow.Intro, 10f, "menu -> Intro");
            float until = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().name == GameFlow.Intro && Time.realtimeSinceStartup < until)
            {
                if (StoryPlayer.IsPlaying) StoryPlayer.Next();
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Check(SceneManager.GetActiveScene().name == GameFlow.Levels[0], "Intro selesai -> " + GameFlow.Levels[0] + " (sekarang: " + SceneManager.GetActiveScene().name + ")");

            foreach (var lv in GameFlow.Levels)
            {
                GameFlow.Load(lv);
                yield return WaitScene(lv, 10f, "muat " + lv);
                yield return new WaitForSecondsRealtime(1.5f);
                if (StoryPlayer.IsPlaying) StoryPlayer.Next();
                Check(FindFirstObjectByType<LevelGoal>() != null && FindFirstObjectByType<PlayerBrain>() != null, lv + ": LevelGoal + pemain ada");
            }
            GameFlow.Load(GameFlow.Hub);
            yield return WaitScene(GameFlow.Hub, 10f, "muat Markas");
            GameFlow.Load(GameFlow.Ending);
            yield return WaitScene(GameFlow.Ending, 10f, "muat Ending");

            Debug.Log($"SMOKE SELESAI: {ok} OK, {fail} GAGAL, error log {DevLog.Errors}");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fail > 0 || DevLog.Errors > 0 ? 1 : 0);
        }

        private IEnumerator WaitScene(string name, float timeout, string what)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < until) yield return null;
            Check(SceneManager.GetActiveScene().name == name, what);
            // screenshot per scene (cek layar tidak hitam): folder smoke_shots di sebelah .exe
            yield return new WaitForSecondsRealtime(0.8f);
            yield return new WaitForEndOfFrame();
            try
            {
                string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "smoke_shots");
                System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, SceneManager.GetActiveScene().name + ".png"));
            }
            catch (System.Exception e) { Debug.LogWarning("SMOKE screenshot gagal: " + e.Message); }
        }

        private void Check(bool cond, string what)
        {
            if (cond) { ok++; Debug.Log("SMOKE OK    " + what); }
            else { fail++; Debug.LogError("SMOKE GAGAL " + what); }
        }
    }
}

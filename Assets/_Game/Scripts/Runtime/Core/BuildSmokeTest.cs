using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Smoke test untuk BUILD (bukan fitur game). Jalankan .exe dengan argumen -gnSmoke (smoke_test.bat di folder build):
    //   Boot -> menu -> Intro (cerita dimajukan otomatis sampai pindah sendiri ke tutorial/level pertama, alur asli)
    //   -> tutorial + tiap level dimuat lewat GameFlow (alur asli, jadi pembuka sinematik ikut diputar), lalu dicek:
    //        pembuka diputar dan selesai, kontrol kembali, kamera normal (zoom + Pixel Perfect Camera + ikut pemain),
    //        tampilan slime normal (badan + wajah, tanpa sprite Disguise sisa v1), slime bisa bergerak, objektif ada
    //   -> Markas + Ending -> hasil ditulis ke log -> game keluar sendiri (exit code 1 kalau ada yang gagal/error).
    // Hasil: baris "SMOKE" di goo-napped_log.txt (DevLog) dan Player.log, screenshot tiap tahap di smoke_shots/.
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

            // alur asli cerita pembuka: Intro selesai harus pindah sendiri ke tutorial / level pertama (bug layar hitam)
            GameFlow.Load(GameFlow.Intro);
            yield return WaitScene(GameFlow.Intro, 10f, "menu -> Intro");
            float until = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().name == GameFlow.Intro && Time.realtimeSinceStartup < until)
            {
                if (StoryPlayer.IsPlaying) StoryPlayer.Next();
                yield return new WaitForSecondsRealtime(0.25f);
            }
            string expect = GameFlow.AfterIntro(GameFlow.Levels[0]);   // tutorial kalau ada, kalau tidak level pertama
            Check(SceneManager.GetActiveScene().name == expect, "Intro selesai -> " + expect + " (sekarang: " + SceneManager.GetActiveScene().name + ")");
            if (SceneManager.GetActiveScene().name == expect) yield return CheckLevel(expect);   // sudah dimuat oleh Intro

            foreach (var lv in GameFlow.Levels)
            {
                GameFlow.Load(lv);
                yield return WaitScene(lv, 10f, "muat " + lv);
                yield return CheckLevel(lv);
            }
            GameFlow.Load(GameFlow.Hub);
            yield return WaitScene(GameFlow.Hub, 10f, "muat Markas");
            GameFlow.Load(GameFlow.Ending);
            yield return WaitScene(GameFlow.Ending, 10f, "muat Ending");

            Debug.Log($"SMOKE SELESAI: {ok} OK, {fail} GAGAL, error log {DevLog.Errors}");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fail > 0 || DevLog.Errors > 0 ? 1 : 0);
        }

        // Satu level yang baru dimuat lewat alur game.
        private IEnumerator CheckLevel(string lv)
        {
            // pembuka sinematik: tunggu mulai, majukan dialognya, tunggu selesai
            float until = Time.realtimeSinceStartup + 3f;
            while (!LevelIntro.Running && Time.realtimeSinceStartup < until) yield return null;
            Check(LevelIntro.Running, lv + ": pembuka sinematik diputar");
            until = Time.realtimeSinceStartup + 60f;
            while (LevelIntro.Running && Time.realtimeSinceStartup < until)
            {
                if (StoryPlayer.IsPlaying) StoryPlayer.Next();
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Check(!LevelIntro.Running, lv + ": pembuka selesai");
            // dialog sesudah pembuka (mis. Raja di L11) juga dimajukan
            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForSecondsRealtime(0.4f);
                if (!StoryPlayer.IsPlaying) break;
                StoryPlayer.Next();
            }

            var brain = FindFirstObjectByType<PlayerBrain>();
            Check(FindFirstObjectByType<LevelGoal>() != null && brain != null, lv + ": LevelGoal + pemain ada");
            if (brain == null) yield break;
            Check(brain.enabled, lv + ": kontrol kembali setelah pembuka");
            var cam = Camera.main;
            if (cam != null)
            {
                Check(Mathf.Abs(cam.orthographicSize - 2.25f) < 0.05f, lv + ": zoom kamera normal (" + cam.orthographicSize.ToString("F2") + ")");
                var ppc = cam.GetComponent("PixelPerfectCamera") as Behaviour;
                Check(ppc == null || ppc.enabled, lv + ": Pixel Perfect Camera menyala");
                var follow = cam.GetComponent<CameraFollow>();
                Check(follow == null || follow.enabled, lv + ": kamera mengikuti pemain");
            }
            var piece = brain.Active;
            Check(piece != null && SlimeLooksNormal(piece), lv + ": tampilan slime normal (badan + wajah, tanpa Disguise)");
            yield return Shot(lv + "_main");

            // gerak asli (SlimeMotor): coba 4 arah, cukup satu yang bergerak (spawn bisa menempel tembok)
            if (piece != null)
            {
                brain.enabled = false;   // PlayerBrain menimpa MoveInput dari keyboard tiap frame
                Vector2 start = piece.transform.position;
                bool moved = false;
                foreach (var d in new[] { Vector2.right, Vector2.down, Vector2.left, Vector2.up })
                {
                    piece.Motor.MoveInput = d;
                    float stop = Time.realtimeSinceStartup + 0.8f;
                    while (Time.realtimeSinceStartup < stop) yield return null;
                    piece.Motor.MoveInput = Vector2.zero;
                    if (Vector2.Distance(start, piece.transform.position) > 0.25f) { moved = true; break; }
                }
                brain.enabled = true;
                Check(moved, lv + ": slime bisa bergerak");
            }
        }

        private static bool SlimeLooksNormal(SlimePiece p)
        {
            bool body = false, face = false;
            foreach (var r in p.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.name == "Disguise" && r.enabled) return false;
                if (r.name == "Body") body = r.enabled;
                if (r.name == "Face") face = r.enabled;
            }
            return body && face;
        }

        private IEnumerator WaitScene(string name, float timeout, string what)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < until) yield return null;
            Check(SceneManager.GetActiveScene().name == name, what);
            // screenshot per scene (cek layar tidak hitam)
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot(SceneManager.GetActiveScene().name);
        }

        // screenshot ke folder smoke_shots di sebelah .exe
        private static IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "smoke_shots");
                System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, name + ".png"));
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

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GooNapped.Tests
{
    // Pembuka level sinematik (LevelIntro): diputar saat masuk lewat alur game, tidak saat Retry.
    // Setelah selesai semua dikembalikan: kontrol, kamera ikut pemain, Pixel Perfect Camera, slime terlihat, tip tutorial muncul.
    public class LevelIntroTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Application.runInBackground = true;
            LevelIntro.Suppressed = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        static IEnumerator LoadViaFlow(string scene)
        {
            GameFlow.Load(scene);
            float t = 0f;
            while (SceneManager.GetActiveScene().name != scene && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "Scene tidak termuat: " + scene);
        }

        static void Shot(string name)
        {
            System.IO.Directory.CreateDirectory("Temp/intro_shots");
            ScreenCapture.CaptureScreenshot("Temp/intro_shots/" + name + ".png");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Tutorial_PembukaSinematik_LaluKontrolKembali()
        {
            yield return LoadViaFlow(GameFlow.Tutorial);
            float t = 0f;
            while (!LevelIntro.Running && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(LevelIntro.Running, "Pembuka tutorial tidak diputar saat masuk lewat GameFlow.");
            var brain = Object.FindFirstObjectByType<PlayerBrain>();
            var cam = Camera.main;
            var ppc = cam.GetComponent("PixelPerfectCamera") as Behaviour;
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(brain.enabled, "Pemain masih bisa dikendalikan selama pembuka.");
            Vector2 start = brain.Active.transform.position;

            // majukan semua dialog; foto beberapa tahap
            int lines = 0;
            bool wasPlaying = false;
            t = 0f;
            while (LevelIntro.Running && t < 60f)
            {
                if (StoryPlayer.IsPlaying && !wasPlaying) { lines++; if (lines <= 6) { yield return new WaitForSeconds(0.9f); Shot("tutorial_line" + lines); } }
                wasPlaying = StoryPlayer.IsPlaying;
                if (StoryPlayer.IsPlaying) StoryPlayer.Next();
                t += Time.unscaledDeltaTime;
                yield return new WaitForSeconds(0.1f);
            }
            Assert.IsFalse(LevelIntro.Running, "Pembuka tidak selesai dalam 60 detik.");
            Assert.GreaterOrEqual(lines, 5, "Dialog pembuka tutorial kurang (4 sorotan + 1 penutup).");
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(brain.enabled, "Kontrol tidak dikembalikan setelah pembuka.");
            if (ppc != null) Assert.IsTrue(ppc.enabled, "Pixel Perfect Camera tidak dinyalakan lagi.");
            Assert.That(cam.orthographicSize, Is.EqualTo(2.25f).Within(0.05f), "Zoom kamera tidak kembali normal.");
            var piece = brain.Active;
            Assert.IsTrue(piece.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled), "Slime tidak terlihat setelah pembuka.");
            Assert.Greater(((Vector2)piece.transform.position - start).x, 1f, "Slime tidak melompat masuk sendiri.");
            Assert.IsFalse(Object.FindObjectsByType<Guard>(FindObjectsSortMode.None).Any(g => g.IsChasing), "Penjaga mengejar selama pembuka.");
            // tip WASD muncul setelah pembuka
            t = 0f;
            while ((TutorialPrompt.Get() == null || !TutorialPrompt.Get().IsBusy) && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(TutorialPrompt.Get() != null && TutorialPrompt.Get().IsBusy, "Tip tutorial tidak muncul setelah pembuka.");
            yield return new WaitForSeconds(0.6f);
            Shot("tutorial_control");

            // Retry (muat ulang scene) tidak memutar pembuka lagi
            GameFlow.ReloadCurrent();
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(LevelIntro.Running, "Pembuka diputar lagi setelah Retry.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LevelBiasa_PembukaObjektif_BisaDilewati()
        {
            yield return LoadViaFlow(GameFlow.Levels[0]);
            float t = 0f;
            while (!LevelIntro.Running && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(LevelIntro.Running, "Pembuka level tidak diputar.");
            t = 0f;
            while (!StoryPlayer.IsPlaying && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSeconds(0.8f);
            Shot("L01_objective");
            int lines = 0;
            bool wasPlaying = false;
            t = 0f;
            while (LevelIntro.Running && t < 30f)
            {
                if (StoryPlayer.IsPlaying && !wasPlaying) lines++;
                wasPlaying = StoryPlayer.IsPlaying;
                if (StoryPlayer.IsPlaying) StoryPlayer.Next();
                t += Time.unscaledDeltaTime;
                yield return new WaitForSeconds(0.1f);
            }
            Assert.IsFalse(LevelIntro.Running, "Pembuka level tidak selesai.");
            Assert.AreEqual(2, lines, "Pembuka level biasa = objektif + kalimat pembuka.");
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerBrain>().enabled, "Kontrol tidak dikembalikan.");
        }
    }
}

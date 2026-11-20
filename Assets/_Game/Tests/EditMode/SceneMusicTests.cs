using NUnit.Framework;
using UnityEngine;

namespace GooNapped.Tests
{
    // Tabel musik per scene (Resources/SceneMusic): tiap scene alur game punya musik, dan klipnya bisa dimuat.
    public class SceneMusicTests
    {
        [Test]
        public void SemuaSceneAlur_PunyaMusik()
        {
            var t = Resources.Load<SceneMusicTable>("SceneMusic");
            Assert.IsNotNull(t, "Assets/_Game/Resources/SceneMusic.asset hilang");
            var scenes = new System.Collections.Generic.List<string> { GameFlow.MainMenu, GameFlow.Intro, GameFlow.Tutorial, GameFlow.Hub, GameFlow.Ending };
            scenes.AddRange(GameFlow.Levels);
            foreach (var s in scenes)
            {
                var e = t.For(s);
                Assert.IsNotNull(e, s + ": belum ada di tabel musik");
                Assert.IsNotNull(e.music, s + ": klip musik kosong");
                Assert.Greater(e.musicVolume, 0f, s + ": volume musik 0");
            }
        }

        [Test]
        public void Entri_TanpaNamaSceneGanda()
        {
            var t = Resources.Load<SceneMusicTable>("SceneMusic");
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var e in t.Entries)
                foreach (var s in e.scenes.Split(','))
                {
                    var n = s.Trim();
                    if (n == "") continue;
                    Assert.IsTrue(seen.Add(n), n + " muncul di dua entri (entri kedua tidak akan pernah dipakai)");
                }
        }
    }
}

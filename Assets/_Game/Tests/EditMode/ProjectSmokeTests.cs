using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GooNapped.Tests
{
    public class ProjectSmokeTests
    {
        [Test]
        public void PixelToonLitShader_Exists()
        {
            Assert.IsNotNull(Shader.Find("GooNapped/PixelToonLit"));
        }

        [Test]
        public void BuildSettings_FirstSceneIsBoot()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.IsTrue(scenes.Length > 0, "Build settings kosong");
            StringAssert.EndsWith("Boot/Boot.unity", scenes[0].path);
        }
    }
}

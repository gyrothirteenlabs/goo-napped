using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GooNapped.EditorTools
{
    // Build Windows tanpa mengubah Build Settings (ProjectSettings = keputusan tim).
    // Daftar scene = scene aktif di Build Settings + semua scene alur game yang belum ada (menu, Intro, Markas, L01..L11, Ending).
    // Hasil: Builds/Windows-Dev (Development Build, log ke goo-napped_log.txt di sebelah .exe, F1 = log di layar)
    //        Builds/Windows (rilis). Keduanya diabaikan git.
    // Smoke test build: jalankan "smoke_test.bat" di folder build (otomatis memuat semua scene lalu keluar, hasil di log).
    public static class GameBuild
    {
        const string Exe = "GooNapped.exe";

        [MenuItem("GooNapped/Build/Windows Dev (dengan log)")]
        public static void BuildDev() => Build(true);

        [MenuItem("GooNapped/Build/Windows Rilis")]
        public static void BuildRelease() => Build(false);

        public static string[] Scenes()
        {
            var list = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled && File.Exists(s.path) && !list.Contains(s.path)) list.Add(s.path);
            var need = new List<string> { "Boot", GameFlow.MainMenu, GameFlow.Intro, GameFlow.Hub };
            need.AddRange(GameFlow.Levels);
            need.Add(GameFlow.Ending);
            foreach (var n in need)
            {
                var p = GameFlow.FindScenePath(n);
                if (p == null) Debug.LogWarning("GameBuild: scene alur '" + n + "' tidak ditemukan.");
                else if (!list.Contains(p)) list.Add(p);
            }
            // Boot harus index 0
            var boot = list.Find(p => Path.GetFileNameWithoutExtension(p) == "Boot");
            if (boot != null) { list.Remove(boot); list.Insert(0, boot); }
            return list.ToArray();
        }

        public static BuildReport Build(bool dev)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", dev ? "Windows-Dev" : "Windows"));
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes(),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                locationPathName = Path.Combine(dir, Exe),
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            Debug.Log("GameBuild: " + opts.scenes.Length + " scene -> " + opts.locationPathName + "\n" + string.Join("\n", opts.scenes));
            // nama game + ikon sementara hanya selama build, lalu dikembalikan (ProjectSettings = keputusan tim)
            BuildReport report;
            using (new Branding.Scope())
                report = BuildPipeline.BuildPlayer(opts);
            var sum = report.summary;
            if (sum.result == BuildResult.Succeeded)
            {
                File.WriteAllText(Path.Combine(dir, "smoke_test.bat"),
                    "@echo off\r\nrem Muat semua scene otomatis lalu keluar. Hasil: goo-napped_log.txt (baris SMOKE).\r\n" +
                    "start /wait \"\" \"%~dp0" + Exe + "\" -gnSmoke -gnLog -screen-fullscreen 0 -screen-width 1280 -screen-height 720\r\n" +
                    "findstr /C:\"SMOKE\" \"%~dp0goo-napped_log.txt\"\r\npause\r\n");
                File.WriteAllText(Path.Combine(dir, "main_dengan_log.bat"),
                    "@echo off\r\nrem Main biasa + log lengkap ke goo-napped_log.txt (F1 di game = lihat log).\r\n" +
                    "start \"\" \"%~dp0" + Exe + "\" -gnLog\r\n");
            }
            Debug.Log($"GameBuild: {sum.result}, {sum.totalErrors} error, {sum.totalWarnings} warning, {sum.totalSize / (1024 * 1024)} MB, {sum.totalTime.TotalSeconds:F0} dtk");
            return report;
        }
    }
}

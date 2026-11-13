using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Log untuk build test (bukan fitur game).
    // Aktif di Development Build, atau build biasa yang dijalankan dengan argumen -gnLog.
    //   - Semua Debug.Log/Warning/Error + pindah scene ditulis ke "goo-napped_log.txt" di sebelah .exe
    //     (Player.log bawaan Unity tetap ada di %USERPROFILE%\AppData\LocalLow\<company>\<product>\).
    //   - F1 = tampilkan/sembunyikan 25 baris terakhir di layar. Kalau ada error, penanda merah muncul di pojok.
    public class DevLog : MonoBehaviour
    {
        public static string FilePath { get; private set; }
        public static int Errors { get; private set; }

        private static readonly Queue<string> lines = new Queue<string>();
        private static readonly object gate = new object();
        private static StreamWriter writer;
        private bool show;
        private GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Application.isEditor) return;
            if (!Debug.isDebugBuild && !HasArg("-gnLog")) return;
            try
            {
                FilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "goo-napped_log.txt");
                writer = new StreamWriter(FilePath, false) { AutoFlush = true };
            }
            catch (Exception e) { Debug.LogWarning("DevLog: file log tidak bisa dibuat: " + e.Message); }
            Application.logMessageReceivedThreaded += OnLog;
            SceneManager.activeSceneChanged += (a, b) => Write("SCENE", "-> " + b.name + " (build index " + b.buildIndex + ")");
            Write("INFO", $"{Application.productName} {Application.version}, Unity {Application.unityVersion}, dev={Debug.isDebugBuild}");
            Write("INFO", $"{SystemInfo.operatingSystem} | {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}) | {Screen.width}x{Screen.height}");
            Write("INFO", "Scene di build: " + SceneManager.sceneCountInBuildSettings);
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                Write("INFO", "  " + i + ": " + System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)));
            var go = new GameObject("DevLog");
            DontDestroyOnLoad(go);
            go.AddComponent<DevLog>();
        }

        public static bool HasArg(string arg)
        {
            foreach (var a in Environment.GetCommandLineArgs()) if (string.Equals(a, arg, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void OnLog(string msg, string stack, LogType type)
        {
            bool bad = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            if (bad) Errors++;
            Write(type.ToString().ToUpperInvariant(), msg + (bad && !string.IsNullOrEmpty(stack) ? "\n    " + stack.Trim().Replace("\n", "\n    ") : ""));
        }

        private static void Write(string tag, string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {tag,-9} {msg}";
            lock (gate)
            {
                lines.Enqueue(line.Split('\n')[0]);
                while (lines.Count > 25) lines.Dequeue();
                try { writer?.WriteLine(line); } catch { }
            }
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) show = !show;
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false };
            if (Errors > 0)
            {
                GUI.color = new Color(1f, 0.35f, 0.35f);
                GUI.Label(new Rect(Screen.width - 230, 4, 226, 20), Errors + " error (F1: log)", style);
                GUI.color = Color.white;
            }
            if (!show) return;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, 25 * 15 + 30), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string[] copy;
            lock (gate) copy = lines.ToArray();
            GUI.Label(new Rect(6, 4, Screen.width - 12, 20), "Log (F1 tutup)  file: " + FilePath, style);
            for (int i = 0; i < copy.Length; i++) GUI.Label(new Rect(6, 24 + i * 15, Screen.width - 12, 16), copy[i], style);
        }

        private void OnApplicationQuit()
        {
            Write("INFO", "Keluar. Error total: " + Errors);
            lock (gate) { try { writer?.Dispose(); } catch { } writer = null; }
        }
    }
}

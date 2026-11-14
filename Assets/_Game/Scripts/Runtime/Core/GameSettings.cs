using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Setelan pemain (menu Options): volume musik/efek, fullscreen, resolusi. Disimpan di PlayerPrefs.
    // Volume disimpan sebagai langkah 0..10 supaya mudah diatur pakai keyboard.
    public static class GameSettings
    {
        public const int MaxLevel = 10;

        private const string KMusic = "gn_vol_music";
        private const string KSfx = "gn_vol_sfx";
        private const string KFull = "gn_fullscreen";
        private const string KResW = "gn_res_w";
        private const string KResH = "gn_res_h";

        // Dipanggil setiap ada setelan yang berubah (AudioDirector menyesuaikan volume loop).
        public static event Action Changed;

        public static int MusicLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KMusic, 8), 0, MaxLevel);
            set { PlayerPrefs.SetInt(KMusic, Mathf.Clamp(value, 0, MaxLevel)); Save(); }
        }

        public static int SfxLevel
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KSfx, 8), 0, MaxLevel);
            set { PlayerPrefs.SetInt(KSfx, Mathf.Clamp(value, 0, MaxLevel)); Save(); }
        }

        // Pengali volume 0..1. Musik = BGM, efek = SFX + ambience.
        public static float MusicVolume => MusicLevel / (float)MaxLevel;
        public static float SfxVolume => SfxLevel / (float)MaxLevel;

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(KFull, 1) == 1;
            set { PlayerPrefs.SetInt(KFull, value ? 1 : 0); Save(); ApplyDisplay(); }
        }

        public static Vector2Int Resolution
        {
            get
            {
                int w = PlayerPrefs.GetInt(KResW, 0), h = PlayerPrefs.GetInt(KResH, 0);
                return w > 0 && h > 0 ? new Vector2Int(w, h) : new Vector2Int(Screen.width, Screen.height);
            }
            set
            {
                PlayerPrefs.SetInt(KResW, value.x);
                PlayerPrefs.SetInt(KResH, value.y);
                Save();
                ApplyDisplay();
            }
        }

        // Resolusi 16:9-ish yang didukung monitor (unik, minimal 960x540). Cadangan kalau daftar kosong.
        public static List<Vector2Int> AvailableResolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.y >= 540 && !list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0)
            {
                list.Add(new Vector2Int(1280, 720));
                list.Add(new Vector2Int(1600, 900));
                list.Add(new Vector2Int(1920, 1080));
            }
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // Layar hanya diubah di build. Di editor ukuran Game view diatur Unity sendiri.
        public static void ApplyDisplay()
        {
            if (Application.isEditor) return;
            var r = Resolution;
            Screen.SetResolution(r.x, r.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        // Pakai setelan layar yang tersimpan saat game mulai (kalau pemain pernah mengubahnya).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStart()
        {
            if (PlayerPrefs.HasKey(KResW) || PlayerPrefs.HasKey(KFull)) ApplyDisplay();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace GooNapped
{
    // Pembuka level sinematik: pemain tidak langsung dilempar ke kontrol + tip WASD.
    //   1. Layar terbuka dari hitam, garis hitam sinematik, HUD disembunyikan.
    //   2. Kamera menyorot beberapa titik (pan + zoom keluar) dengan dialog di tiap titik. Selama itu Pixel Perfect Camera
    //      dimatikan supaya zoom halus (mode Windowbox membuat zoom non-bulat jadi kotak kecil), lalu dinyalakan lagi.
    //   3. Kamera kembali ke slime. Slime masuk (jatuh dari pipa), melompat beberapa langkah sendiri, menengok kiri-kanan.
    //   4. Dialog penutup, lalu kontrol diberikan (tip tutorial baru muncul setelah ini).
    // Hanya diputar saat level dimasuki lewat alur game (GameFlow.Load: menu, cerita, markas, level sebelumnya).
    // Tidak saat Retry / ulang dari checkpoint, tidak di test yang memuat scene langsung, tidak di smoke test build.
    // Esc = lewati. Selama berjalan: penjaga tidak bisa melihat pemain (Guard), pause & tip tutorial ditahan.
    // Dipasang LevelBlockout: level biasa memakai teks LevelCatalog (objektif di titik tujuan + kalimat pembuka),
    // tutorial memakai daftar shot sendiri (LevelBlockout.V2_L00_Tutorial).
    public class LevelIntro : MonoBehaviour
    {
        public enum Entry { None, Drop }

        [Serializable]
        public class Shot
        {
            [Tooltip("Titik yang disorot kamera (koordinat dunia).")]
            public Vector2 focus;
            [Tooltip("Zoom keluar: 1 = normal, 2 = dua kali lebih luas.")]
            [Range(1f, 3f)] public float zoom = 1f;
            [Tooltip("Lama kamera bergerak ke titik ini (detik). 0 = langsung.")]
            public float moveTime = 1.4f;
            [Tooltip("Jeda setelah kamera sampai, sebelum dialog.")]
            public float hold = 0.35f;
            public string speaker;
            [TextArea(2, 4)] public string line;
        }

        [Header("Isi")]
        [Tooltip("Pakai teks LevelCatalog: objektif saat kamera menyorot 'goal', lalu kalimat pembuka di slime. Matikan untuk memakai daftar shots.")]
        [SerializeField] private bool fromCatalog = true;
        [SerializeField] private Vector2 goal;
        [SerializeField] private List<Shot> shots = new List<Shot>();
        [Tooltip("Dialog setelah slime masuk, sebelum pemain bisa bergerak (fromCatalog: diisi otomatis).")]
        [SerializeField] private List<string> afterLines = new List<string>();

        [Header("Masuknya slime")]
        [SerializeField] private Entry entry = Entry.None;
        [Tooltip("Slime melompat sendiri sejauh vektor ini setelah masuk (unit). 0 = diam.")]
        [SerializeField] private Vector2 walkIn = Vector2.zero;
        [SerializeField] private bool lookAround = true;

        [Header("Uji")]
        [Tooltip("Putar juga saat scene dibuka langsung di editor (tanpa lewat menu). Matikan lagi sebelum menjalankan test.")]
        [SerializeField] private bool playWhenOpenedDirectly;

        public static bool Running { get; private set; }
        // Test bot/level memuat scene lewat GameFlow.Load: pembuka dimatikan di sana (test pembuka sendiri menyalakannya).
        public static bool Suppressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Running = false;

        private PlayerBrain brain;
        private CameraFollow follow;
        private Camera cam;
        private PixelPerfectCamera ppc;
        private bool ppcWasOn;
        private float baseOrtho;
        private bool skip, mine;
        private SlimePiece piece;

        private bool ShouldPlay()
        {
            if (Suppressed || Array.IndexOf(Environment.GetCommandLineArgs(), "-gnSmoke") >= 0) return false;
            if (GameFlow.ConsumeArrival(gameObject.scene.name)) return true;
            return Application.isEditor && playWhenOpenedDirectly;
        }

        private IEnumerator Start()
        {
            if (!ShouldPlay()) yield break;
            brain = FindFirstObjectByType<PlayerBrain>();
            follow = FindFirstObjectByType<CameraFollow>();
            cam = Camera.main;
            if (brain == null || cam == null) yield break;
            ppc = cam.GetComponent<PixelPerfectCamera>();
            Running = mine = true;
            StoryPlayer.SetBlack(true);
            yield return null;   // Start komponen lain jalan dulu (PlayerBrain memilih potongan aktif)

            piece = brain.Active;
            brain.enabled = false;
            if (piece != null) piece.Motor.Stop();
            baseOrtho = ppc != null ? ppc.refResolutionY / (2f * ppc.assetsPPU) : cam.orthographicSize;
            if (ppc != null) { ppcWasOn = ppc.enabled; ppc.enabled = false; }
            if (follow != null) follow.enabled = false;
            StoryPlayer.SetCinematic(true);

            var list = BuildShots();
            bool drop = entry == Entry.Drop && piece != null;
            if (drop) Hide(piece);
            SetCam(list.Count > 0 ? list[0].focus : PlayerFocus(), list.Count > 0 ? list[0].zoom : 1f);
            yield return StoryPlayer.FadeScreen(0f, 0.5f);

            foreach (var shot in list)
            {
                if (skip) break;
                yield return MoveCam(shot.focus, shot.zoom, shot.moveTime);
                if (skip) break;
                yield return Wait(shot.hold);
                if (!skip && !string.IsNullOrEmpty(shot.line)) yield return Say(shot.speaker, shot.line);
            }
            if (!skip) yield return MoveCam(PlayerFocus(), 1f, list.Count > 0 ? 1.3f : 0f);

            if (piece != null)
            {
                if (drop) { Show(); if (!skip) yield return Drop(); }
                if (!skip && walkIn.sqrMagnitude > 0.01f) yield return Walk(walkIn);
                if (!skip && lookAround) yield return LookAround();
            }
            foreach (var l in AfterLines())
            {
                if (skip) break;
                yield return Say(null, l);
            }
            Finish();
        }

        private void Update()
        {
            if (!mine || skip) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) skip = true;
        }

        private void OnDisable()
        {
            if (mine) Finish();
        }

        private void Finish()
        {
            if (!mine) return;
            mine = false;
            Running = false;
            StoryPlayer.SetCinematic(false);
            StoryPlayer.SetBlack(false);
            if (cam != null && baseOrtho > 0f) cam.orthographicSize = baseOrtho;
            if (ppc != null && ppcWasOn) ppc.enabled = true;
            if (piece != null)
            {
                Show();
                piece.Motor.enabled = true;
                piece.Motor.Stop();
                if (piece.Jelly != null) { piece.Jelly.Lift = 0f; piece.Jelly.Target = 0f; }
            }
            if (follow != null) follow.enabled = true;
            if (brain != null) brain.enabled = true;
        }

        // ---------------- isi ----------------

        private List<Shot> BuildShots()
        {
            if (!fromCatalog) return shots;
            var list = new List<Shot>();
            int i = GameFlow.IndexOf(gameObject.scene.name);
            if (i >= 0) list.Add(new Shot { focus = goal, zoom = 1.5f, moveTime = 0f, hold = 0.5f, line = LevelCatalog.Objective(i) });
            return list;
        }

        private IEnumerable<string> AfterLines()
        {
            if (!fromCatalog) return afterLines;
            int i = GameFlow.IndexOf(gameObject.scene.name);
            var intro = i >= 0 ? LevelCatalog.Get(i).intro : null;
            return string.IsNullOrEmpty(intro) ? new string[0] : new[] { intro };
        }

        private IEnumerator Say(string who, string text)
        {
            bool done = false;
            var seq = StorySequence.Dialogue(who ?? "", text);
            seq.pauseGame = false;
            seq.skippable = true;
            StoryPlayer.Play(seq, () => done = true);
            while (!done) yield return null;
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !skip; t += Time.deltaTime) yield return null;
        }

        // ---------------- kamera ----------------

        private Vector2 PlayerFocus()
        {
            if (piece == null) return cam.transform.position;
            return (Vector2)piece.transform.position + new Vector2(0f, 0.3f);
        }

        private void SetCam(Vector2 focus, float zoom)
        {
            cam.orthographicSize = baseOrtho * Mathf.Max(1f, zoom);
            var p = new Vector3(focus.x, focus.y, cam.transform.position.z);
            cam.transform.position = follow != null ? follow.ClampToBounds(p) : p;
        }

        private IEnumerator MoveCam(Vector2 to, float zoomTo, float time)
        {
            Vector2 from = cam.transform.position;
            float zoomFrom = cam.orthographicSize / baseOrtho;
            if (time <= 0f) { SetCam(to, zoomTo); yield break; }
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                if (skip) yield break;
                float k = Mathf.SmoothStep(0f, 1f, t / time);
                SetCam(Vector2.Lerp(from, to, k), Mathf.Lerp(zoomFrom, zoomTo, k));
                yield return null;
            }
            SetCam(to, zoomTo);
        }

        // ---------------- slime ----------------

        // Sembunyikan slime sebelum jatuh dari pipa. Hanya renderer yang MEMANG menyala yang dimatikan dan dicatat, lalu
        // hanya itu yang dinyalakan lagi: slime punya renderer yang sengaja mati (mis. "Disguise" sisa v1). Dulu semua
        // renderer dinyalakan saat pembuka selesai, jadi batu samaran ikut tampil menempel di slime.
        private readonly List<SpriteRenderer> hidden = new List<SpriteRenderer>();

        private void Hide(SlimePiece p)
        {
            foreach (var r in p.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.enabled) { r.enabled = false; hidden.Add(r); }
        }

        private void Show()
        {
            foreach (var r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
        }

        // Jatuh dari pipa di langit-langit: badan memanjang saat jatuh, gepeng saat mendarat.
        private IEnumerator Drop()
        {
            var jelly = piece.Jelly;
            piece.Motor.enabled = false;
            const float height = 3.2f, duration = 0.55f;
            for (float t = 0f; t < duration && jelly != null; t += Time.deltaTime)
            {
                float k = t / duration;
                jelly.Lift = height * (1f - k * k);
                jelly.Target = 0.25f;
                yield return null;
            }
            if (jelly != null) { jelly.Lift = 0f; jelly.Target = 0f; jelly.Kick(-7f); }
            AudioDirector.Play(AudioDirector.Sfx.Merge, piece.Center, 0.8f);
            piece.Motor.enabled = true;
            yield return Wait(0.45f);
        }

        // Melompat sendiri memakai gerak asli (SlimeMotor), jadi rasanya sama dengan saat dikendalikan.
        private IEnumerator Walk(Vector2 delta)
        {
            Vector2 start = piece.transform.position;
            var dir = delta.normalized;
            piece.Face(Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? new Vector2Int(dir.x > 0 ? 1 : -1, 0) : new Vector2Int(0, dir.y > 0 ? 1 : -1));
            float until = Time.time + 4f;
            while (!skip && Time.time < until && Vector2.Distance(start, piece.transform.position) < delta.magnitude)
            {
                piece.Motor.MoveInput = dir;
                yield return null;
            }
            piece.Motor.MoveInput = Vector2.zero;
            float settle = Time.time + 1f;
            while (piece.Motor.IsHopping && Time.time < settle) yield return null;
        }

        private IEnumerator LookAround()
        {
            piece.Face(Vector2Int.left); yield return Wait(0.4f);
            piece.Face(Vector2Int.right); yield return Wait(0.4f);
            piece.Face(Vector2Int.down); yield return Wait(0.25f);
        }
    }
}

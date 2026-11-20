using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Musik + ambience per scene (SceneMusicTable di Resources/SceneMusic). Satu objek untuk seluruh game
    // (DontDestroyOnLoad), dibuat otomatis saat game mulai, jadi menu / cerita / markas / level semua bermusik.
    //   - Ganti scene: musik baru di-crossfade. Scene berikutnya memakai klip yang SAMA -> musik lanjut, tidak mulai ulang
    //     (contoh: menu -> Intro, L04 -> L05).
    //   - Scene tidak ada di tabel -> musik & ambience memudar (AudioDirector level memakai klip loop-nya sendiri).
    //   - Volume = volume di tabel x setelan pemain (musik: Music, ambience: SFX), sama seperti AudioDirector.
    //   - Fade memakai waktu tak berskala: tetap jalan saat pause / dialog (Time.timeScale = 0).
    //   - Menu / cerita tidak punya AudioListener: MusicDirector membawa listener sendiri yang hanya menyala kalau scene
    //     tidak punya listener aktif (level punya listener di kamera, dibuat AudioDirector), jadi tidak pernah dobel.
    public class MusicDirector : MonoBehaviour
    {
        private static MusicDirector instance;
        private static SceneMusicTable table;
        private static bool loaded;

        private AudioListener ownListener;
        private AudioSource[] music = new AudioSource[2];
        private AudioSource[] amb = new AudioSource[2];
        private int curMusic, curAmb;
        private float musicBase, ambBase;          // volume tabel untuk klip yang sedang aktif
        private float musicFade = 1f, ambFade = 1f; // 0..1 untuk sumber aktif (sumber lama: 1 - fade)
        private float musicOld, ambOld;             // volume sumber lama saat crossfade dimulai

        public static SceneMusicTable Table
        {
            get
            {
                if (!loaded) { table = Resources.Load<SceneMusicTable>("SceneMusic"); loaded = true; }
                return table;
            }
        }

        // Dipakai AudioDirector: kalau scene ada di tabel, AudioDirector tidak memutar music/ambience-nya sendiri.
        public static bool Covers(string scene) => Table != null && Table.For(scene) != null;

        public static AudioClip CurrentMusic => instance != null ? instance.music[instance.curMusic].clip : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; table = null; loaded = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null || Table == null) return;
            var go = new GameObject("MusicDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<MusicDirector>();
        }

        private void Awake()
        {
            instance = this;
            for (int i = 0; i < 2; i++) { music[i] = Source("Music_" + i); amb[i] = Source("Ambience_" + i); }
            ownListener = gameObject.AddComponent<AudioListener>();
            ownListener.enabled = false;
            UpdateListener();
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameSettings.Changed += Apply;
            Switch(SceneManager.GetActiveScene().name, true);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.Changed -= Apply;
            if (instance == this) instance = null;
        }

        private AudioSource Source(string n)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.ignoreListenerPause = true;
            return s;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Switch(scene.name, false);
            UpdateListener();
        }

        private float nextListenerCheck;

        // Listener sendiri hanya kalau tidak ada listener lain yang aktif.
        private void UpdateListener()
        {
            if (ownListener == null) return;
            bool other = false;
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != ownListener && l.isActiveAndEnabled) { other = true; break; }
            ownListener.enabled = !other;
            nextListenerCheck = Time.unscaledTime + 1f;
        }

        private void Switch(string scene, bool instant)
        {
            var e = Table != null ? Table.For(scene) : null;
            Crossfade(music, ref curMusic, ref musicFade, ref musicBase, ref musicOld, e != null ? e.music : null, e != null ? e.musicVolume : 0f, instant);
            Crossfade(amb, ref curAmb, ref ambFade, ref ambBase, ref ambOld, e != null ? e.ambience : null, e != null ? e.ambienceVolume : 0f, instant);
            Apply();
        }

        private static void Crossfade(AudioSource[] pair, ref int cur, ref float fade, ref float baseVol, ref float oldVol, AudioClip clip, float vol, bool instant)
        {
            var active = pair[cur];
            if (clip != null && active.clip == clip && active.isPlaying) { baseVol = vol; return; }   // sama: lanjut
            if (clip == null && active.clip == null) return;
            pair[1 - cur].Stop();          // crossfade sebelumnya yang belum selesai: langsung berhenti
            oldVol = active.isPlaying ? active.volume : 0f;
            cur = 1 - cur;
            var next = pair[cur];
            next.Stop();
            next.clip = clip;
            if (clip != null) next.Play();
            baseVol = vol;
            fade = instant ? 1f : 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextListenerCheck) UpdateListener();   // listener level bisa muncul belakangan
            float cf = Table != null ? Table.Crossfade : 1.5f;
            float step = cf > 0f ? Time.unscaledDeltaTime / cf : 1f;
            bool changed = false;
            if (musicFade < 1f) { musicFade = Mathf.Min(1f, musicFade + step); changed = true; }
            if (ambFade < 1f) { ambFade = Mathf.Min(1f, ambFade + step); changed = true; }
            if (changed) Apply();
        }

        private void Apply()
        {
            float mv = GameSettings.MusicVolume, sv = GameSettings.SfxVolume;
            Set(music, curMusic, musicFade, musicBase * mv, musicOld);
            Set(amb, curAmb, ambFade, ambBase * sv, ambOld);
        }

        private static void Set(AudioSource[] pair, int cur, float fade, float vol, float oldVol)
        {
            var a = pair[cur]; var old = pair[1 - cur];
            a.volume = vol * fade;
            if (!old.isPlaying) return;
            if (fade >= 1f) { old.Stop(); old.clip = null; }
            else old.volume = oldVol * (1f - fade);
        }
    }
}

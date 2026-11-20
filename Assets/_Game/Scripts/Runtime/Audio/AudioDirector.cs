using UnityEngine;

namespace GooNapped
{
    // Satu per level (di prefab LevelSystems): efek suara posisional, plus BGM + ambience loop CADANGAN.
    // Musik per scene diatur MusicDirector (Resources/SceneMusic). Kalau scene ada di tabel itu, loop di sini tidak
    // diputar; loop di sini hanya untuk scene di luar tabel (sandbox).
    // Klip: sebagian dari Freesound (lihat Assets/_Game/Audio/LISENSI.md), sisanya PLACEHOLDER sintesis
    // (Editor/Audio/ProtoAudioGenerator.cs). Tim audio tinggal mengganti klip di Inspector, kode tidak perlu diubah.
    public class AudioDirector : MonoBehaviour
    {
        public enum Sfx { SlimeStep, GolemStep, Eat, Split, Merge, Disguise, Reveal, Switch, Alert, Caught, Vomit, Splat, UiClick }

        [Header("Loop")]
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
        [SerializeField] private AudioClip ambience;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.5f;

        [Header("Efek (urut sesuai enum Sfx)")]
        [SerializeField] private AudioClip slimeStep;
        [Tooltip("Variasi langkah/lompat slime (opsional). Kalau diisi, tiap langkah memilih acak dari daftar ini.")]
        [SerializeField] private AudioClip[] slimeStepVariants = new AudioClip[0];
        [SerializeField] private AudioClip golemStep;
        [SerializeField] private AudioClip eat;
        [SerializeField] private AudioClip split;
        [SerializeField] private AudioClip merge;
        [SerializeField] private AudioClip disguise;
        [SerializeField] private AudioClip reveal;
        [SerializeField] private AudioClip switchPiece;
        [SerializeField] private AudioClip alert;
        [SerializeField] private AudioClip caught;
        [SerializeField] private AudioClip vomit;
        [SerializeField] private AudioClip splat;
        [SerializeField] private AudioClip uiClick;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        [Header("Jarak dengar (unit)")]
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private float maxDistance = 8f;

        private static AudioDirector instance;
        private AudioSource[] pool;
        private int next;
        private AudioSource musicSource, ambienceSource;

        private void Awake()
        {
            instance = this;
            EnsureListenerOnPlane();
            pool = new AudioSource[12];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Sfx_" + i);
                go.transform.SetParent(transform, false);
                pool[i] = Configure3D(go.AddComponent<AudioSource>());
            }
            bool table = MusicDirector.Covers(gameObject.scene.name);
            musicSource = table ? null : Loop(music, "Music");
            ambienceSource = table ? null : Loop(ambience, "Ambience");
            ApplyVolumes();
            GameSettings.Changed += ApplyVolumes;
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= ApplyVolumes;
            if (instance == this) instance = null;
        }

        // Volume loop = volume dasar di Inspector x setelan pemain (Options). Ambience ikut volume efek.
        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = musicVolume * GameSettings.MusicVolume;
            if (ambienceSource != null) ambienceSource.volume = ambienceVolume * GameSettings.SfxVolume;
        }

        private AudioSource Configure3D(AudioSource s)
        {
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
            return s;
        }

        private AudioSource Loop(AudioClip clip, string name)
        {
            if (clip == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.spatialBlend = 0f;
            s.Play();
            return s;
        }

        // Kamera ada di z = -10, aktor di z = 0: pendengar dipindah ke bidang permainan supaya jarak suara = jarak di layar.
        private static void EnsureListenerOnPlane()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var onCam = cam.GetComponent<AudioListener>();
            if (cam.transform.Find("Listener") != null) return;
            if (onCam != null) onCam.enabled = false;
            var go = new GameObject("Listener");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -cam.transform.position.z);
            go.AddComponent<AudioListener>();
        }

        private AudioClip ClipFor(Sfx s)
        {
            switch (s)
            {
                case Sfx.SlimeStep:
                    if (slimeStepVariants != null && slimeStepVariants.Length > 0)
                    {
                        var v = slimeStepVariants[Random.Range(0, slimeStepVariants.Length)];
                        if (v != null) return v;
                    }
                    return slimeStep;
                case Sfx.GolemStep: return golemStep;
                case Sfx.Eat: return eat;
                case Sfx.Split: return split;
                case Sfx.Merge: return merge;
                case Sfx.Disguise: return disguise;
                case Sfx.Reveal: return reveal;
                case Sfx.Switch: return switchPiece;
                case Sfx.Alert: return alert;
                case Sfx.Caught: return caught;
                case Sfx.Vomit: return vomit;
                case Sfx.Splat: return splat;
                case Sfx.UiClick: return uiClick;
            }
            return null;
        }

        // Mainkan efek di posisi dunia. Aman dipanggil walau tidak ada AudioDirector di scene.
        public static void Play(Sfx sfx, Vector2 position, float volume = 1f, float pitchJitter = 0.08f)
        {
            if (instance == null) return;
            var clip = instance.ClipFor(sfx);
            if (clip == null) return;
            var s = instance.pool[instance.next];
            instance.next = (instance.next + 1) % instance.pool.Length;
            s.transform.position = new Vector3(position.x, position.y, 0f);
            s.clip = clip;
            s.volume = volume * instance.sfxVolume * GameSettings.SfxVolume;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.Play();
        }
    }
}

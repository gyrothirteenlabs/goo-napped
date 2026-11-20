using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Daftar musik + ambience per scene. Aset: Assets/_Game/Resources/SceneMusic.asset (dimuat MusicDirector lewat
    // Resources, jadi juga jalan saat scene dibuka langsung di editor / test). Tim audio tinggal mengganti klip di Inspector.
    // Scene yang tidak ada di daftar: MusicDirector diam, dan AudioDirector level memakai klip loop-nya sendiri (sandbox).
    [CreateAssetMenu(menuName = "GooNapped/Audio/Musik per Scene", fileName = "SceneMusic")]
    public class SceneMusicTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Nama scene (tanpa .unity). Beberapa scene boleh dipisah koma: \"L04_Cells, L05_CellBlock\".")]
            public string scenes = "";
            public AudioClip music;
            [Range(0f, 1f)] public float musicVolume = 0.4f;
            [Tooltip("Ambience loop (opsional). Kosong = tanpa ambience dari tabel ini.")]
            public AudioClip ambience;
            [Range(0f, 1f)] public float ambienceVolume = 0.4f;

            public bool Has(string scene)
            {
                foreach (var s in scenes.Split(','))
                    if (s.Trim() == scene) return true;
                return false;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [Tooltip("Lama crossfade saat musik berganti (detik).")]
        [SerializeField, Min(0f)] private float crossfade = 1.5f;

        public IReadOnlyList<Entry> Entries => entries;
        public float Crossfade => crossfade;

        public Entry For(string scene)
        {
            foreach (var e in entries)
                if (e != null && e.Has(scene)) return e;
            return null;
        }
    }
}

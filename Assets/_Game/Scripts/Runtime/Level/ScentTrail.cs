using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Jejak bau: slime berbau (Minyak Belerang, ...) menjatuhkan titik bau sambil bergerak. Titik memudar dan hilang.
    // Anjing Lendir mengikuti titik yang paling BARU di sekitarnya, jadi ia berjalan menyusuri jejak menuju slime.
    public static class ScentTrail
    {
        public struct Mark
        {
            public Vector2 position;
            public float time;
            public float strength;   // radius bau saat dijatuhkan
        }

        public const float Lifetime = 10f;
        private static readonly List<Mark> marks = new List<Mark>();
        private static readonly List<SpriteRenderer> visuals = new List<SpriteRenderer>();

        // Sprite titik bau (diisi PlayerBrain). null = tanpa visual.
        public static Sprite MarkSprite;
        public static int Count => marks.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            marks.Clear();
            visuals.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene s, LoadSceneMode m) => Clear();

        public static void Clear()
        {
            marks.Clear();
            visuals.Clear();   // objeknya ikut hancur bersama scene lama
        }

        public static void Drop(Vector2 position, float strength) => Drop(position, strength, Time.time);

        public static void Drop(Vector2 position, float strength, float time)
        {
            Prune(time);
            marks.Add(new Mark { position = position, time = time, strength = strength });
            if (MarkSprite != null && Application.isPlaying) SpawnVisual(position);
        }

        private static void Prune(float now)
        {
            int dead = 0;
            while (dead < marks.Count && now - marks[dead].time > Lifetime) dead++;
            if (dead > 0) marks.RemoveRange(0, dead);
        }

        // Titik bau paling baru dalam `range` dari `from` yang lebih baru dari `newerThan`.
        // `reachable` (opsional): titik yang terhalang tembok dilewati, jadi pencium menyusuri jejak lewat tikungan
        // (titik terbaru yang terlihat langsung), bukan menabrak tembok menuju titik paling baru.
        public static bool Freshest(Vector2 from, float range, float newerThan, float now, out Mark result,
            System.Func<Vector2, bool> reachable = null)
        {
            result = default;
            for (int i = marks.Count - 1; i >= 0; i--)   // urut waktu: dari yang terbaru
            {
                var m = marks[i];
                if (now - m.time > Lifetime) break;
                if (m.time <= newerThan) break;
                if (Vector2.Distance(from, m.position) > range + m.strength * 0.5f) continue;
                if (reachable != null && !reachable(m.position)) continue;
                result = m;
                return true;
            }
            return false;
        }

        public static bool Freshest(Vector2 from, float range, float newerThan, out Mark result,
            System.Func<Vector2, bool> reachable = null) =>
            Freshest(from, range, newerThan, Time.time, out result, reachable);

        // Visual: noda kekuningan pudar di lantai.
        private static void SpawnVisual(Vector2 position)
        {
            var go = new GameObject("ScentMark");
            go.transform.position = position + Random.insideUnitCircle * 0.08f;
            go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.26f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MarkSprite;
            sr.sortingOrder = -60;
            sr.color = new Color(0.85f, 0.8f, 0.3f, 0.45f);
            go.AddComponent<ScentMarkFade>();
        }
    }
}

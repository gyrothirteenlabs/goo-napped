using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Checkpoint (desain-v2 jawaban 6). Masuk ventilasi = keadaan level disimpan (di memori, bukan PlayerPrefs).
    // Tertangkap atau tekan R = scene dimuat ulang, lalu keadaan ini dipulihkan:
    //   pemain mulai DI DALAM ventilasi terakhir, dengan tubuh (massa, esens) yang sama;
    //   esens di lantai, toples (tiruan + alarm), teman yang menunggu, teman yang sudah selamat, pintu, dan lentera
    //   kembali seperti saat itu. Penjaga mulai lagi dari awal patrolinya.
    // Dibuang saat level selesai atau pindah ke scene lain (GameFlow).
    // Objek dikenali lewat jalur hierarki (nama + urutan anak), sama persis setelah scene dimuat ulang.
    public static class Checkpoint
    {
        public struct PieceState
        {
            public Vector2 position;
            public int clearUnits;
            public List<IngredientData> eaten;
            public bool active;
            public string spotPath;   // tempat sembunyi/ventilasi yang sedang dipakai, atau null
        }

        private class JarState { public string path; public SlimeBodyState decoy; public float alarmLeft; public bool alarmRaised; }
        private class WaitingFriend { public string jarPath; public Vector2 position; }

        private class Data
        {
            public string scene;
            public readonly List<PieceState> pieces = new List<PieceState>();
            public readonly List<(IngredientData data, Vector2 pos)> pickups = new List<(IngredientData, Vector2)>();
            public readonly List<JarState> jars = new List<JarState>();
            public readonly List<WaitingFriend> waiting = new List<WaitingFriend>();
            public int saved;
            public readonly List<(string path, bool toggled)> doors = new List<(string, bool)>();
            public readonly List<string> lampsOut = new List<string>();
            public readonly List<string> valves = new List<string>();
            public readonly List<string> ventsVisited = new List<string>();
        }

        private static Data current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => current = null;

        public static bool HasFor(string scenePath) => current != null && current.scene == scenePath;
        public static bool HasForActiveScene => HasFor(SceneManager.GetActiveScene().path);
        public static void Clear() => current = null;

        public static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name + "#" + t.GetSiblingIndex());
            return string.Join("/", parts);
        }

        private static T Find<T>(string path) where T : Component
        {
            if (string.IsNullOrEmpty(path)) return null;
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c != null && PathOf(c.transform) == path) return c;
            return null;
        }

        // Simpan keadaan level sekarang (dipanggil saat pemain masuk ventilasi).
        public static void Save(PlayerBrain brain)
        {
            if (brain == null) return;
            var d = new Data { scene = SceneManager.GetActiveScene().path };
            foreach (var p in brain.Pieces)
            {
                if (p == null) continue;
                var st = p.Body.State;
                d.pieces.Add(new PieceState
                {
                    position = p.HidingIn != null ? (Vector2)p.HidingIn.transform.position : (Vector2)p.transform.position,
                    clearUnits = st.ClearUnits,
                    eaten = new List<IngredientData>(st.Eaten),
                    active = p == brain.Active,
                    spotPath = p.HidingIn != null ? PathOf(p.HidingIn.transform) : null,
                });
            }
            foreach (var pk in Object.FindObjectsByType<IngredientPickup>(FindObjectsSortMode.None))
                if (pk != null && pk.Ingredient != null) d.pickups.Add((pk.Ingredient, pk.transform.position));
            foreach (var j in Object.FindObjectsByType<Jar>(FindObjectsSortMode.None))
                if (j.HasDecoy)
                    d.jars.Add(new JarState { path = PathOf(j.transform), decoy = j.DecoyState.Clone(), alarmLeft = j.AlarmTimeLeft, alarmRaised = j.AlarmRaised });
            foreach (var f in FriendSlime.All)
                if (f != null && f.SourceJar != null && (f.State == FriendSlime.Mode.Waiting || f.State == FriendSlime.Mode.Following))
                    d.waiting.Add(new WaitingFriend { jarPath = PathOf(f.SourceJar.transform), position = f.transform.position });
            var goal = Object.FindFirstObjectByType<LevelGoal>();
            d.saved = goal != null ? goal.Saved : 0;
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None)) d.doors.Add((PathOf(door.transform), door.Toggled));
            foreach (var lamp in Object.FindObjectsByType<Extinguishable>(FindObjectsSortMode.None))
                if (!lamp.IsLit) d.lampsOut.Add(PathOf(lamp.transform));
            foreach (var v in Object.FindObjectsByType<AuraValve>(FindObjectsSortMode.None))
                if (v.IsReversed) d.valves.Add(PathOf(v.transform));
            foreach (var v in Vent.All)
                if (v != null && v.Visited) d.ventsVisited.Add(PathOf(v.transform));
            current = d;
        }

        // Pulihkan keadaan untuk scene ini (dipanggil LevelGoal satu frame setelah scene dimuat). false = tidak ada checkpoint.
        public static bool TryRestore(PlayerBrain brain, LevelGoal goal)
        {
            if (!HasForActiveScene || brain == null) return false;
            var d = current;

            // esens: yang asli dicocokkan (data + posisi), sisanya dihapus; esens hasil muntah dibuat ulang
            var live = new List<IngredientPickup>(Object.FindObjectsByType<IngredientPickup>(FindObjectsSortMode.None));
            var template = live.Count > 0 ? live[0] : null;
            var keep = new HashSet<IngredientPickup>();
            foreach (var (data, pos) in d.pickups)
            {
                IngredientPickup match = null;
                foreach (var pk in live)
                    if (!keep.Contains(pk) && pk.Ingredient == data && ((Vector2)pk.transform.position - pos).sqrMagnitude < 0.01f) { match = pk; break; }
                if (match == null && template != null)
                {
                    match = Object.Instantiate(template, pos, Quaternion.identity, template.transform.parent);
                    match.Init(data);
                }
                if (match != null) keep.Add(match);
            }
            foreach (var pk in live) if (!keep.Contains(pk)) Object.Destroy(pk.gameObject);

            foreach (var js in d.jars)
            {
                var j = Find<Jar>(js.path);
                if (j != null) j.RestoreDecoy(js.decoy, js.alarmLeft, js.alarmRaised);
            }
            foreach (var w in d.waiting)
            {
                var j = Find<Jar>(w.jarPath);
                if (j != null) j.SpawnFriend(w.position);
            }
            if (goal != null) goal.RestoreSaved(d.saved);
            foreach (var (path, toggled) in d.doors)
            {
                var door = Find<Door>(path);
                if (door != null) door.SetToggled(toggled);
            }
            foreach (var path in d.lampsOut)
            {
                var lamp = Find<Extinguishable>(path);
                if (lamp != null) lamp.Extinguish(true);
            }

            foreach (var path in d.valves)
            {
                var v = Find<AuraValve>(path);
                if (v != null) v.SetReversed();
            }

            foreach (var path in d.ventsVisited)
            {
                var v = Find<Vent>(path);
                if (v != null) v.MarkVisited();
            }

            var states = new List<PieceState>(d.pieces);
            var spots = new List<HidingSpot>();
            foreach (var s in states) spots.Add(Find<HidingSpot>(s.spotPath));
            brain.RestorePieces(states, spots);
            return true;
        }
    }
}

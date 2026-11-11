using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GooNapped.Tests
{
    // Playtest otomatis (Play mode, scene asli, gerak & collision asli):
    //   Bot_MenyelesaikanLevel : penjaga dimatikan, bot memainkan alur v2 lengkap (makan esens sesuai solver -> belah ke
    //                            toples -> F ajak teman -> bawa ke ventilasi exit) lewat API yang sama dengan tombol
    //                            pemain. Membuktikan level bisa diselesaikan dan tidak ada lorong yang menyangkut badan.
    //   Penjaga_PatroliJalan   : pemain diam di spawn 40 detik game: tidak ketahuan, dan tiap penjaga sampai ke semua
    //                            titik patrolinya (pathfinding tidak macet).
    //   Alur scene cerita/menu : Boot -> MainMenu, Intro -> level pertama, Ending -> MainMenu.
    // Error/exception apa pun di console selama test = test gagal (aturan Unity Test Framework).
    // Progres pemain (PlayerPrefs) disimpan sebelum test dan dikembalikan sesudahnya.
    public class LevelPlaythroughTests
    {
        const string UnlockKey = "gn_unlocked";
        const float BotTimeScale = 3f;
        const float Interact = 0.8f;   // sedikit di bawah interactRadius 0.85 (PlayerBrain)

        static IEnumerable<string> Levels() => GameFlow.Levels;

        private int savedUnlock;
        private bool hadUnlock;
        private string failure;

        const string PowersKey = "gn_powers";
        private string savedPowers;
        private bool hadPowers;

        [OneTimeSetUp]
        public void SaveProgress()
        {
            hadUnlock = PlayerPrefs.HasKey(UnlockKey);
            savedUnlock = PlayerPrefs.GetInt(UnlockKey, 0);
            hadPowers = PlayerPrefs.HasKey(PowersKey);
            savedPowers = PlayerPrefs.GetString(PowersKey, "");
        }

        [OneTimeTearDown]
        public void RestoreProgress()
        {
            if (hadUnlock) PlayerPrefs.SetInt(UnlockKey, savedUnlock); else PlayerPrefs.DeleteKey(UnlockKey);
            if (hadPowers) PlayerPrefs.SetString(PowersKey, savedPowers); else PlayerPrefs.DeleteKey(PowersKey);
            PlayerPrefs.Save();
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Application.runInBackground = true;   // editor tidak berhenti update saat jendelanya di belakang
            failure = null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        static IEnumerator Load(string scene)
        {
            GameFlow.Load(scene);   // di editor juga memuat scene di luar Build Settings (sandbox, level v2 baru)
            float t = 0f;
            while (SceneManager.GetActiveScene().name != scene && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "Scene tidak termuat: " + scene);
            yield return null;
            yield return null;   // Start() semua objek sudah jalan
        }

        static IEnumerator WaitGame(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        static LevelMap Map() => LevelMap.From(SceneManager.GetActiveScene().GetRootGameObjects());

        // Tidak muat celah: tubuh lebih dari massa 1, atau berat (desain-v2 bagian 5).
        static bool Heavy(SlimePiece p) => p.Body.State.Mass > 1 || p.Body.State.Get(TraitKind.Weight) == "berat";

        static bool InRange(SlimePiece a, Component target, float r)
        {
            foreach (var c in Physics2D.OverlapCircleAll(a.Center, r))
                if (c != null && c.transform.IsChildOf(target.transform)) return true;
            return false;
        }

        // ---------- bot ----------

        [UnityTest, Timeout(300000)]
        public IEnumerator Bot_MenyelesaikanLevel([ValueSource(nameof(Levels))] string level)
        {
            yield return RunBot(level);
        }

        private LevelGoal goal;

        IEnumerator RunBot(string level)
        {
            yield return Load(level);
            foreach (var g in Object.FindObjectsByType<Guard>(FindObjectsSortMode.None)) g.gameObject.SetActive(false);
            var brain = Object.FindFirstObjectByType<PlayerBrain>();
            goal = Object.FindFirstObjectByType<LevelGoal>();
            Assert.IsNotNull(brain, "PlayerBrain tidak ada");
            Assert.IsNotNull(goal, "LevelGoal tidak ada");
            brain.enabled = false;   // bot memanggil API yang sama dengan tombol pemain (Interact, TrySplit, SwitchNext)
            Time.timeScale = BotTimeScale;
            float t0 = Time.time;

            var map = Map();
            var pool = map.pickups.Select(p => p.Ingredient).ToList();
            List<RecipeSolver.Result> plan = null;
            List<Jar> jars = null;
            foreach (var order in Orders(map.jars))
            {
                plan = RecipeSolver.SolveSequence(pool, order.Select(j => j.Recipe));
                if (plan != null) { jars = order; break; }
            }
            Assert.IsNotNull(plan, "Resep tidak bisa dibuat dari bahan di level.");
            var route = new List<string>();

            for (int k = 0; k < jars.Count; k++)
            {
                var jar = jars[k];
                foreach (var step in plan[k].steps)
                {
                    var a = brain.Active;
                    if (step.kind == RecipeSolver.StepKind.Vomit)
                    {
                        Assert.IsTrue(a.TryVomit(), "Gagal muntah " + step.ingredient.DisplayName);
                        route.Add("muntah");
                        yield return WaitGame(1f);
                        continue;
                    }
                    var target = Map().pickups.Where(p => p.Ingredient == step.ingredient)
                        .OrderBy(p => Vector2.Distance(p.transform.position, a.Center)).FirstOrDefault();
                    Assert.IsNotNull(target, "Bahan " + step.ingredient.DisplayName + " tidak ada di level.");
                    yield return Walk(brain, LevelMap.CellOfObject(target), 0.3f, "ke " + target.name);
                    if (failure != null) Assert.Fail(failure);
                    Assert.AreSame(target, a.FindPickup(), $"Berdiri di {target.name}, tapi bahan terdekat yang termakan berbeda (bahan terlalu berdempetan?).");
                    int before = a.Body.State.Eaten.Count;
                    Assert.IsTrue(brain.Interact(), "E di depan " + target.name + " tidak melakukan apa-apa. Pesan: " + brain.Message);
                    Assert.AreEqual(before + 1, a.Body.State.Eaten.Count, "Bahan tidak termakan. Pesan: " + brain.Message);
                    route.Add("makan " + step.ingredient.DisplayName);
                    yield return WaitGame(1f);
                }

                // v2: Space di depan toples = membelah langsung ke dalam toples, lalu F mengajak teman ikut
                var carrier = brain.Active;
                yield return WalkToJar(brain, jar);
                if (failure != null) Assert.Fail(failure);
                Assert.IsTrue(brain.SplitOrMerge(), "Space di depan toples tidak memasukkan tiruan. Pesan: " + brain.Message);
                yield return WaitGame(1.2f);
                Assert.IsFalse(jar.HasFriend, "Toples masih berisi teman setelah tiruan masuk. Pesan: " + brain.Message);
                Assert.AreEqual(0, jar.DecoyMismatch, "Tiruan tidak sempurna (solver dan game beda hitung).");
                Assert.AreSame(carrier, brain.Active, "Kendali pindah setelah tiruan masuk toples.");
                route.Add("tiruan " + jar.name);

                var friend = Object.FindObjectsByType<FriendSlime>(FindObjectsSortMode.None)
                    .OrderBy(f => Vector2.Distance(f.transform.position, jar.transform.position)).FirstOrDefault();
                Assert.IsNotNull(friend, "Teman tidak muncul setelah tiruan masuk.");
                Assert.IsTrue(brain.Command(), "F tidak mengajak teman. Pesan: " + brain.Message);
                Assert.AreEqual(FriendSlime.Mode.Following, friend.State, "Teman tidak mengikuti setelah F.");
                route.Add("ajak " + friend.Name);
            }

            var endMap = Map();
            var exitVent = endMap.vents.FirstOrDefault(v => v.ExitsLevel);
            if (exitVent != null)
            {
                // v2: berdiri di lubang exit, tunggu teman merapat, lalu C (masuk). Teman ikut masuk = selamat.
                yield return WalkUntil(brain, LevelMap.CellOfObject(exitVent), () => InRange(brain.Active, exitVent, 0.5f), "ke ventilasi exit");
                if (failure != null) Assert.Fail(failure);
                float until = Time.time + 6f;
                while (Time.time < until && FriendSlime.All.Any(f => f != null && f.State == FriendSlime.Mode.Following &&
                       Vector2.Distance(f.transform.position, brain.Active.transform.position) > 1.4f)) yield return null;
                Assert.IsTrue(brain.ToggleHide(brain.Active), "C di lubang exit tidak masuk ventilasi. Pesan: " + brain.Message);
                yield return WaitGame(1.5f);
                Assert.IsTrue(goal.IsComplete, $"Masuk ventilasi exit tapi level tidak selesai (teman selamat {goal.Saved}/{goal.Required}). Pesan: {brain.Message}");
                route.Add("ventilasi exit");
            }
            else
            {
                var exit = endMap.exits.OrderBy(e => Vector2.Distance(e.transform.position, brain.Active.Center)).First();
                yield return WalkUntil(brain, LevelMap.CellOfObject(exit), () => goal.IsComplete, "ke exit");
                if (failure != null) Assert.Fail(failure);
                yield return WaitGame(0.5f);
                Assert.IsTrue(goal.IsComplete, "Sampai exit tapi level tidak selesai.");
            }
            Debug.Log($"[QA] {level}: bot selesai dalam {Time.time - t0:F0} detik game (tanpa penjaga). Rute: {string.Join(" > ", route)}");
        }

        static IEnumerable<List<Jar>> Orders(List<Jar> jars)
        {
            if (jars.Count <= 1) { yield return new List<Jar>(jars); yield break; }
            for (int i = 0; i < jars.Count; i++)
            {
                var rest = new List<Jar>(jars);
                rest.RemoveAt(i);
                foreach (var o in Orders(rest)) { o.Insert(0, jars[i]); yield return o; }
            }
        }

        IEnumerator WalkToJar(PlayerBrain brain, Jar jar)
        {
            var a = brain.Active;
            var map = Map();
            var jarCell = LevelMap.CellOfObject(jar);
            var stand = map.Neighbors4(jarCell).Where(c => map.SlimeWalkable(c, Heavy(a), false))
                .OrderBy(c => Vector2.Distance(LevelMap.CenterOf(c), a.Center)).ToList();
            if (stand.Count == 0) { failure = jar.name + ": tidak ada sel untuk berdiri di sampingnya."; yield break; }
            foreach (var s in stand)
            {
                if (brain.Pieces.Any(p => p != null && p != a && LevelMap.Cell(p.Center) == s)) continue;
                yield return WalkUntil(brain, s, () => InRange(brain.Active, jar, Interact), "ke " + jar.name);
                if (failure == null) yield break;
                failure = null;
            }
            failure = "Tidak bisa mendekati " + jar.name + " dari sisi mana pun.";
        }

        IEnumerator Split(PlayerBrain brain)
        {
            var a = brain.Active;
            int before = brain.Pieces.Count;
            foreach (var dir in new[] { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up })
            {
                a.Face(dir);
                if (brain.TrySplit()) break;
            }
            if (brain.Pieces.Count != before + 1) { failure = "Tidak bisa belah di " + LevelMap.Cell(a.Center) + " (semua arah terhalang)."; yield break; }
            yield return WaitGame(0.6f);
        }

        IEnumerator Walk(PlayerBrain brain, Vector2Int goal, float tol, string what) =>
            WalkUntil(brain, goal, null, what, tol);

        // Jalan ke sel `goal` lewat A* (gerak asli: lompat-merayap + collision). Berhenti lebih awal kalau `done` terpenuhi.
        IEnumerator WalkUntil(PlayerBrain brain, Vector2Int goal, Func<bool> done, string what, float tol = 0.3f)
        {
            var a = brain.Active;
            float wake = Time.time + 2f;
            while ((a.IsTransforming || a.IsEating) && Time.time < wake) yield return null;
            var history = new List<string>();
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (done != null && done()) { a.Move(Vector2.zero); yield break; }
                var map = Map();
                bool heavy = Heavy(a);
                var occupied = new HashSet<Vector2Int>(brain.Pieces.Where(p => p != null && p != a).Select(p => LevelMap.Cell(p.Center)));
                Func<Vector2Int, bool> walk = c => (c == goal || !occupied.Contains(c)) && map.SlimeWalkable(c, heavy, false);
                var start = LevelMap.Cell(a.Center);
                var path = new List<Vector2Int>();
                if (!GridPath.Find(walk, start, goal, path, 20000))
                {
                    failure = $"Tidak ada jalan {what}: {start} -> {goal}" + (heavy ? " (tubuh berat)" : "");
                    yield break;
                }
                bool stuck = false;
                foreach (var c in path)
                {
                    var wp = LevelMap.CenterOf(c);
                    bool last = c == goal;
                    float best = float.MaxValue, since = 0f;
                    while (Vector2.Distance(a.Center, wp) > (last ? tol : 0.35f))
                    {
                        if (done != null && done()) { a.Move(Vector2.zero); yield break; }
                        a.Move((wp - a.Center).normalized);
                        yield return null;
                        float d = Vector2.Distance(a.Center, wp);
                        if (d < best - 0.03f) { best = d; since = 0f; } else since += Time.deltaTime;
                        if (since > 3f) { stuck = true; break; }
                    }
                    if (stuck) { history.Add($"macet di {a.Center:F2} menuju sel {c}"); break; }
                }
                a.Move(Vector2.zero);
                float settle = Time.time + 1f;
                while (a.Motor.IsHopping && Time.time < settle) { a.Move(Vector2.zero); yield return null; }
                if (!stuck && (done == null || done())) yield break;
                if (!stuck && done != null)
                {
                    // sampai sel tujuan tapi belum dalam jangkauan: dekati pusat objek sedikit
                    float until = Time.time + 1.5f;
                    var center = LevelMap.CenterOf(goal);
                    while (!done() && Time.time < until) { a.Move((center - a.Center).normalized * 0.6f); yield return null; }
                    a.Move(Vector2.zero);
                    if (done()) yield break;
                    history.Add($"sampai {a.Center:F2} tapi belum dalam jangkauan");
                }
            }
            failure = $"Bot gagal {what} (sel {goal}): " + string.Join("; ", history);
        }

        // ---------- penjaga ----------

        [UnityTest, Timeout(180000)]
        public IEnumerator Penjaga_PatroliJalan_SpawnAman([ValueSource(nameof(Levels))] string level)
        {
            yield return Load(level);
            var goal = Object.FindFirstObjectByType<LevelGoal>();
            var guards = Object.FindObjectsByType<Guard>(FindObjectsSortMode.None);
            if (guards.Length == 0) Assert.Pass("Tanpa penjaga.");
            var reached = guards.ToDictionary(g => g, g => new bool[g.PatrolPoints.Count]);
            Time.timeScale = 4f;
            float end = Time.time + 40f;
            while (Time.time < end && !goal.IsFailed)
            {
                foreach (var g in guards)
                    for (int i = 0; i < g.PatrolPoints.Count; i++)
                        if (Vector2.Distance(g.transform.position, g.PatrolPoints[i]) < 0.35f) reached[g][i] = true;
                yield return null;
            }
            Assert.IsFalse(goal.IsFailed, level + ": pemain yang diam di spawn ketahuan (" + goal.FailReason + ").");
            var stuck = guards.Where(g => reached[g].Any(r => !r))
                .Select(g => g.name + " tidak pernah sampai titik " + string.Join(",", Enumerable.Range(0, reached[g].Length).Where(i => !reached[g][i]).Select(i => g.PatrolPoints[i].ToString())))
                .ToList();
            Assert.IsEmpty(stuck, level + ": " + string.Join("; ", stuck));
        }

        // ---------- laporan peta pengawasan ----------

        // LAPORAN (tidak ikut "Run All", tidak pernah gagal): penjaga dijalankan 90 detik game dengan pemain disingkirkan.
        // Tiap sel di rute terpendek (spawn -> bahan -> toples -> exit) dicatat: berapa persen waktu terlihat penjaga
        // (kerucut + garis pandang, radius sadar, atau cahaya), dan jeda aman terpanjang. Sel yang hampir selalu
        // diawasi = butuh trik (nyamar, umpan, gumpalan, rute lain). Pemain diasumsikan tidak menyala/panas.
        [UnityTest, Explicit("Laporan, jalankan manual"), Category("Laporan"), Timeout(300000)]
        public IEnumerator Laporan_PetaPengawasan([ValueSource(nameof(Levels))] string level)
        {
            yield return Load(level);
            var brain = Object.FindFirstObjectByType<PlayerBrain>();
            var piece = brain.Active;
            brain.enabled = false;
            var map = Map();
            var route = Route(map, out string why);   // dihitung sebelum pemain dipindah (butuh posisi spawn)
            Assert.IsNotNull(route, why);
            var far = new Vector2(-200f, -200f);   // pemain disingkirkan supaya penjaga patroli normal
            piece.GetComponent<Rigidbody2D>().position = far;
            piece.transform.position = far;
            var guards = Object.FindObjectsByType<Guard>(FindObjectsSortMode.None);
            var watched = new Dictionary<Vector2Int, int>();
            var gap = new Dictionary<Vector2Int, float>();       // jeda aman yang sedang berjalan
            var bestGap = new Dictionary<Vector2Int, float>();   // jeda aman terpanjang
            foreach (var c in route) { watched[c] = 0; gap[c] = 0f; bestGap[c] = 0f; }
            int samples = 0;
            Time.timeScale = 4f;
            float end = Time.time + 90f;
            while (Time.time < end)
            {
                samples++;
                foreach (var c in route)
                {
                    var p = LevelMap.CenterOf(c);
                    bool lit = LightExposure.At(p) >= 0.35f;
                    bool seen = guards.Any(g => Watches(map, g, p, lit));
                    if (seen) { watched[c]++; gap[c] = 0f; }
                    else { gap[c] += Time.deltaTime; bestGap[c] = Mathf.Max(bestGap[c], gap[c]); }
                }
                yield return null;
            }

            var hot = route.Where(c => bestGap[c] < 1.5f).ToList();         // tidak pernah ada jeda aman ≥1.5 dtk
            var risky = route.Where(c => watched[c] > 0).ToList();
            float maxShare = route.Max(c => watched[c] / (float)samples);
            Debug.Log($"[QA-PENGAWASAN] {level}: rute {route.Count} sel, {risky.Count} sel pernah terlihat penjaga " +
                      $"({100f * risky.Count / route.Count:F0}%), terburuk {100f * maxShare:F0}% waktu diawasi. " +
                      (hot.Count == 0 ? "Semua sel punya jeda aman >= 1.5 dtk."
                                      : $"{hot.Count} sel tanpa jeda aman (butuh nyamar/umpan/gumpalan): " + string.Join(" ", hot.Take(12))));
        }

        static bool Watches(LevelMap map, Guard g, Vector2 p, bool lit)
        {
            if (g == null || !g.isActiveAndEnabled) return false;
            var eye = g.Eye;
            var body = (Vector2)g.transform.position + Vector2.up * 0.22f;
            if (Vector2.Distance(body, p) <= g.AwarenessRadius && map.LineOfSight(body, p)) return true;
            if (lit && Vector2.Distance(eye, p) <= g.LightSightRange && map.LineOfSight(eye, p)) return true;
            if (g.NeedsLightToSee && !lit) return false;
            return VisionMath.InCone(eye, g.Forward, p, g.ViewRange, g.ViewAngle) && map.LineOfSight(eye, p);
        }

        // Rute terpendek di grid: spawn -> bahan (urutan solver) -> samping toples -> ... -> exit. null kalau tidak ada.
        static List<Vector2Int> Route(LevelMap map, out string why)
        {
            why = null;
            var pool = map.pickups.Select(p => p.Ingredient).ToList();
            List<RecipeSolver.Result> plan = null;
            List<Jar> jars = null;
            foreach (var o in Orders(map.jars)) { plan = RecipeSolver.SolveSequence(pool, o.Select(j => j.Recipe)); if (plan != null) { jars = o; break; } }
            if (plan == null) { why = "resep tidak bisa dibuat"; return null; }
            var stops = new List<Vector2Int>();
            var left = new List<IngredientPickup>(map.pickups);
            var eaten = new List<IngredientData>();
            for (int k = 0; k < jars.Count; k++)
            {
                foreach (var s in plan[k].steps)
                {
                    if (s.kind != RecipeSolver.StepKind.Eat) continue;
                    var from = stops.Count > 0 ? stops[stops.Count - 1] : LevelMap.CellOfObject(map.pieces[0]);
                    var pk = left.Where(x => x.Ingredient == s.ingredient).OrderBy(x => Vector2Int.Distance(LevelMap.CellOfObject(x), from)).First();
                    left.Remove(pk);
                    stops.Add(LevelMap.CellOfObject(pk));
                }
                var jc = LevelMap.CellOfObject(jars[k]);
                stops.Add(map.Neighbors4(jc).First(c => map.SlimeWalkable(c, false, true)));
            }
            stops.Add(LevelMap.CellOfObject(map.ExitPoints().First()));
            var route = new List<Vector2Int>();
            var cur = LevelMap.CellOfObject(map.pieces[0]);
            route.Add(cur);
            var seg = new List<Vector2Int>();
            foreach (var s in stops)
            {
                if (!GridPath.Find(c => map.SlimeWalkable(c, false, true), cur, s, seg, 20000)) { why = $"tidak ada jalan {cur} -> {s}"; return null; }
                route.AddRange(seg);
                cur = s;
            }
            return route.Distinct().ToList();
        }

        // ---------- alur scene ----------

        [UnityTest, Timeout(60000)]
        public IEnumerator Alur_BootKeMenuUtama()
        {
            yield return Load("Boot");
            float t = 0f;
            while (SceneManager.GetActiveScene().name != GameFlow.MainMenu && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(GameFlow.MainMenu, SceneManager.GetActiveScene().name);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Alur_IntroKeLevelPertama() => StoryLeadsTo(GameFlow.Intro, GameFlow.Levels[0]);

        [UnityTest, Timeout(120000)]
        public IEnumerator Alur_EndingKeMenuUtama() => StoryLeadsTo(GameFlow.Ending, GameFlow.MainMenu);

        static IEnumerator StoryLeadsTo(string storyScene, string expected)
        {
            yield return Load(storyScene);
            float t = 0f;
            while (!StoryPlayer.IsPlaying && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(StoryPlayer.IsPlaying, storyScene + ": cerita tidak diputar.");
            t = 0f;
            float next = 0f;
            while (SceneManager.GetActiveScene().name == storyScene && t < 90f)
            {
                t += Time.unscaledDeltaTime;
                if ((next -= Time.unscaledDeltaTime) <= 0f) { StoryPlayer.Next(); next = 0.12f; }
                yield return null;
            }
            yield return null;
            Assert.AreEqual(expected, SceneManager.GetActiveScene().name, storyScene + " tidak berlanjut ke " + expected);
            Assert.AreEqual(1f, Time.timeScale, "Time.timeScale tidak kembali normal setelah cerita.");
        }
    }
}

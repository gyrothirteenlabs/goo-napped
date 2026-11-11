using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped.Tests
{
    // QA statis tiap level (tanpa Play): isi wajib, sambungan antar level, jalan bisa ditempuh, resep bisa dibuat
    // dari bahan yang ada, patroli penjaga sah, dan spawn pemain tidak langsung terlihat penjaga.
    // Scene dibuka ADDITIVE lalu ditutup lagi, jadi scene yang sedang kamu edit tidak tersentuh.
    public class LevelValidationTests
    {
        static readonly string[] SandboxScenes = { "Assets/_Game/Scenes/Sandbox/ProtoProps.unity" };

        static IEnumerable<string> LevelPaths()
        {
            foreach (var name in GameFlow.Levels)
            {
                var guid = AssetDatabase.FindAssets(name + " t:Scene").FirstOrDefault(g =>
                    System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == name);
                yield return guid != null ? AssetDatabase.GUIDToAssetPath(guid) : "MISSING:" + name;
            }
            foreach (var s in SandboxScenes) yield return s;
        }

        static string[] BuildSceneNames() => EditorBuildSettings.scenes.Where(s => s.enabled)
            .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();

        [Test]
        public void AlurGame_SemuaSceneAdaDiBuild()
        {
            var build = BuildSceneNames();
            var missing = new List<string>();
            foreach (var n in GameFlow.Levels.Concat(new[] { GameFlow.MainMenu, GameFlow.Intro, GameFlow.Ending, "Boot" }))
                if (!build.Contains(n)) missing.Add(n);
            Assert.IsEmpty(missing, "Scene belum ada/aktif di Build Settings: " + string.Join(", ", missing));
        }

        [TestCaseSource(nameof(LevelPaths))]
        public void Level_Valid(string path)
        {
            Assert.IsFalse(path.StartsWith("MISSING:"), "Scene level tidak ditemukan: " + path);
            var already = SceneManager.GetSceneByPath(path);
            bool openedHere = !already.isLoaded;
            var scene = openedHere ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : already;
            try
            {
                var problems = Check(path, scene);
                Assert.IsEmpty(problems, System.IO.Path.GetFileNameWithoutExtension(path) + ":\n- " + string.Join("\n- ", problems));
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static List<string> Check(string path, Scene scene)
        {
            var p = new List<string>();
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            bool sandbox = SandboxScenes.Contains(path);
            var m = LevelMap.From(scene.GetRootGameObjects());

            // 1. isi wajib
            if (m.walls == null || m.floor == null) p.Add("Tilemap Grid/Floor atau Grid/Walls tidak ada.");
            if (m.brains.Count != 1) p.Add("PlayerBrain harus tepat 1 (ada " + m.brains.Count + ").");
            if (m.goals.Count != 1) p.Add("LevelGoal harus tepat 1 (ada " + m.goals.Count + ").");
            if (m.pieces.Count != 1) p.Add("Slime pemain di awal harus tepat 1 (ada " + m.pieces.Count + ").");
            if (m.exits.Count == 0) p.Add("Tidak ada exit (LevelExit).");
            if (m.jars.Count == 0) p.Add("Tidak ada toples.");
            foreach (var j in m.jars)
            {
                if (j.Recipe == null) p.Add(j.name + ": resep kosong.");
                else if (j.Recipe.Traits.Count < 2 || j.Recipe.Traits.Count > 3)
                    p.Add(j.name + ": resep punya " + j.Recipe.Traits.Count + " ciri (aturan desain 2–3).");
            }
            foreach (var pk in m.pickups) if (pk.Ingredient == null) p.Add(pk.name + ": bahan kosong.");
            if (!scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<SceneNote>(true) != null)) p.Add("_SceneInfo (SceneNote) tidak ada.");
            var cam = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CameraFollow>(true)).FirstOrDefault();
            if (cam == null) p.Add("Main Camera tanpa CameraFollow.");
            else if (new SerializedObject(cam).FindProperty("bounds").objectReferenceValue == null) p.Add("CameraFollow.bounds kosong.");
            if (p.Count > 0) return p;   // sisanya butuh isi wajib

            // 2. sambungan ke level berikutnya
            var next = new SerializedObject(m.goals[0]).FindProperty("nextScene").stringValue;
            if (!sandbox)
            {
                int i = GameFlow.IndexOf(name);
                string want = i >= 0 && i + 1 < GameFlow.Levels.Length ? GameFlow.Levels[i + 1] : GameFlow.Ending;
                if (next != want) p.Add($"LevelGoal.nextScene = '{next}', seharusnya '{want}' (urutan GameFlow.Levels).");
                if (!BuildSceneNames().Contains(next)) p.Add($"Scene berikutnya '{next}' tidak ada di Build Settings.");
            }

            // 3. objek tidak di dalam tembok
            void OnFloor(Component c)
            {
                if (c != null && !m.IsFloor(LevelMap.CellOfObject(c))) p.Add(c.name + " berada di tembok/luar lantai " + LevelMap.CellOfObject(c) + ".");
            }
            foreach (var c in m.pickups) OnFloor(c);
            foreach (var c in m.jars) OnFloor(c);
            foreach (var c in m.exits) OnFloor(c);
            foreach (var c in m.lamps) OnFloor(c);
            OnFloor(m.pieces[0]);

            // 4. resep bisa dibuat dari bahan di level (coba semua urutan toples)
            var pool = m.pickups.Where(x => x.Ingredient != null).Select(x => x.Ingredient).ToList();
            List<RecipeSolver.Result> plan = null;
            List<Jar> jarOrder = null;
            foreach (var order in Permutations(m.jars))
            {
                plan = RecipeSolver.SolveSequence(pool, order.Select(j => j.Recipe));
                if (plan != null) { jarOrder = order; break; }
            }
            if (plan == null)
            {
                p.Add("Resep tidak bisa dibuat dari bahan yang ada: " + string.Join(" / ", m.jars.Select(j => j.Recipe.name)) +
                      " dengan bahan [" + string.Join(", ", pool.Select(x => x.DisplayName)) + "].");
                return p;
            }

            // 5. jalan: spawn -> bahan (urut, berat ikut berubah) -> toples -> ... -> exit
            var spawn = LevelMap.CellOfObject(m.pieces[0]);
            if (!m.SlimeWalkable(spawn, false, true)) p.Add("Sel spawn " + spawn + " tidak bisa diinjak.");
            var eaten = new List<IngredientData>();
            var here = spawn;
            var remaining = new List<IngredientPickup>(m.pickups);
            for (int k = 0; k < plan.Count; k++)
            {
                foreach (var step in plan[k].steps)
                {
                    if (step.kind == RecipeSolver.StepKind.Vomit) { eaten.RemoveAt(eaten.Count - 1); continue; }
                    bool heavy = RecipeSolver.StateOf(eaten).Get(TraitKind.Weight) == "berat";
                    var reach = m.Reachable(here, c => m.SlimeWalkable(c, heavy, true));
                    var target = remaining.Where(x => x.Ingredient == step.ingredient && reach.Contains(LevelMap.CellOfObject(x)))
                        .OrderBy(x => Vector2Int.Distance(LevelMap.CellOfObject(x), here)).FirstOrDefault();
                    if (target == null) { p.Add($"Toples {k + 1}: bahan {step.ingredient.DisplayName} tidak terjangkau dari {here}" + (heavy ? " (tubuh berat)." : ".")); return p; }
                    remaining.Remove(target);
                    here = LevelMap.CellOfObject(target);
                    eaten.Add(step.ingredient);
                }
                bool heavyNow = RecipeSolver.StateOf(eaten).Get(TraitKind.Weight) == "berat";
                var reachJar = m.Reachable(here, c => m.SlimeWalkable(c, heavyNow, true));
                var jarCell = LevelMap.CellOfObject(jarOrder[k]);
                var stand = m.Neighbors4(jarCell).Where(reachJar.Contains).ToList();
                if (stand.Count == 0) { p.Add(jarOrder[k].name + " tidak bisa didekati" + (heavyNow ? " dengan tubuh berat." : ".")); return p; }
                // belah butuh ruang: minimal 2 sel bebas di sekitar toples (satu potongan masuk, satu menunggu)
                if (stand.Count < 2 && m.Neighbors4(stand[0]).Count(c => c != jarCell && reachJar.Contains(c)) == 0)
                    p.Add(jarOrder[k].name + ": tidak ada ruang untuk belah di sebelahnya.");
                here = stand[0];
            }
            bool heavyEnd = RecipeSolver.StateOf(eaten).Get(TraitKind.Weight) == "berat";
            var reachExit = m.Reachable(here, c => m.SlimeWalkable(c, heavyEnd, true));
            if (!m.exits.Any(e => reachExit.Contains(LevelMap.CellOfObject(e))))
                p.Add("Exit tidak terjangkau dari toples terakhir" + (heavyEnd ? " dengan tubuh berat (resep berat + celah?)." : "."));

            // 6. penjaga: patroli di lantai dan tersambung
            foreach (var g in m.guards)
            {
                var pts = g.PatrolPoints;
                if (pts.Count == 0) { p.Add(g.name + ": tanpa titik patroli."); continue; }
                if (Vector2.Distance(g.transform.position, pts[0]) > 0.6f) p.Add(g.name + ": posisi awal jauh dari titik patroli pertama.");
                foreach (var pt in pts)
                    if (!m.GuardWalkable(LevelMap.Cell(pt + Vector2.up * 0.22f)))
                        p.Add(g.name + ": titik patroli " + pt + " tidak bisa diinjak penjaga.");
                // 2 titik = bolak-balik (1 ruas), >2 titik = putaran (kembali ke titik pertama)
                var cells = new List<Vector2Int>();
                int segments = pts.Count == 2 ? 1 : pts.Count > 2 ? pts.Count : 0;
                for (int k = 0; k < segments; k++)
                {
                    var a = LevelMap.Cell(pts[k] + Vector2.up * 0.22f);
                    var b = LevelMap.Cell(pts[(k + 1) % pts.Count] + Vector2.up * 0.22f);
                    if (!GridPath.Find(m.GuardWalkable, a, b, cells)) p.Add($"{g.name}: tidak ada jalan antar titik patroli {a} -> {b}.");
                }
            }

            // 7. spawn pemain aman (tidak langsung terlihat / terasa penjaga)
            // Edit mode: SlimePiece.Center belum siap (Awake belum jalan), pakai bentuk collider
            Vector2 pc = LevelMap.WorldBounds(m.pieces[0].GetComponent<Collider2D>()).center;
            foreach (var g in m.guards)
            {
                var pts = g.PatrolPoints;
                var fwd = pts.Count > 1 && (pts[1] - pts[0]).sqrMagnitude > 1e-4f ? (pts[1] - pts[0]).normalized : Vector2.down;
                var eye = (Vector2)g.transform.position + Vector2.up * g.EyeHeight;
                var body = (Vector2)g.transform.position + Vector2.up * 0.22f;
                if (VisionMath.InCone(eye, fwd, pc, g.ViewRange, g.ViewAngle) && m.LineOfSight(eye, pc))
                    p.Add(g.name + " melihat spawn pemain sejak awal.");
                if (Vector2.Distance(body, pc) < g.AwarenessRadius + 1.5f)
                    p.Add(g.name + " terlalu dekat dengan spawn (" + Vector2.Distance(body, pc).ToString("F1") + " unit).");
            }

            // 8. props: tuas/pelat tersambung, pintu tanpa tuas/pelat = tembok permanen
            foreach (var l in m.levers) if (l.Doors.Count == 0 || l.Doors.Any(d => d == null)) p.Add(l.name + ": tidak tersambung ke pintu.");
            foreach (var pl in m.plates) if (pl.Doors.Count == 0 || pl.Doors.Any(d => d == null)) p.Add(pl.name + ": tidak tersambung ke pintu.");
            foreach (var d in m.doors) if (!d.StartsOpen && !m.switchedDoors.Contains(d)) p.Add(d.name + " " + d.Cell + ": tertutup permanen (tanpa tuas/pelat).");
            return p;
        }

        static IEnumerable<List<Jar>> Permutations(List<Jar> list)
        {
            if (list.Count <= 1) { yield return new List<Jar>(list); yield break; }
            for (int i = 0; i < list.Count; i++)
            {
                var rest = new List<Jar>(list);
                rest.RemoveAt(i);
                foreach (var perm in Permutations(rest))
                {
                    perm.Insert(0, list[i]);
                    yield return perm;
                }
            }
        }

        // Solver resep sendiri juga dites (logika murni).
        [Test]
        public void Solver_MenemukanUrutanMakan()
        {
            var lumut = IngredientData.Create("lumut", new[] { new Trait(TraitKind.Color, "hijau"), new Trait(TraitKind.Shine, "berkilau") });
            var batu = IngredientData.Create("batu", new[] { new Trait(TraitKind.Color, "abu"), new Trait(TraitKind.Weight, "berat") });
            var debu = RecipeData.Create("Debu", new[] { new Trait(TraitKind.Color, "abu"), new Trait(TraitKind.Shine, "berkilau") });
            var r = RecipeSolver.Solve(new IngredientData[0], new[] { batu, lumut }, debu);
            Assert.IsNotNull(r);
            Assert.AreEqual(2, r.steps.Count);
            Assert.AreSame(lumut, r.steps[0].ingredient, "Lumut dulu, baru batu (abu menimpa hijau).");
            Assert.AreSame(batu, r.steps[1].ingredient);
        }
    }
}
